using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>Disposable fixtures exercise production planting without changing the player's farm.</summary>
public static class SeedPlantingTests
{
#if UNITY_EDITOR
    [MenuItem("Tools/NPC/Farming/Run Seed Checks")]
    public static void RunMenuChecks()
    {
        FarmProductionDefinition carrot = AssetDatabase.LoadAssetAtPath<FarmProductionDefinition>(
            "Assets/Data/ScriptableObject/FarmProductionDefinition_Carrot.asset");
        FarmProductionDefinition potato = AssetDatabase.LoadAssetAtPath<FarmProductionDefinition>(
            "Assets/Data/ScriptableObject/FarmProductionDefinition_Potato.asset");
        CropCatalog catalog = AssetDatabase.LoadAssetAtPath<CropCatalog>("Assets/Data/ScriptableObject/CropCatalog.asset");
        string[] paths = AssetDatabase.FindAssets("t:ItemDataContext");
        Require(paths.Length == 1, "Expected one item context.");
        ItemDataContext items = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<ItemDataContext>(
            AssetDatabase.GUIDToAssetPath(paths[0])));
        try
        {
            int checks = RunChecks(carrot, potato, catalog, items);
            Debug.Log($"Seed planting checks passed: {checks}. Player scene and stock were not modified.");
        }
        finally { UnityEngine.Object.DestroyImmediate(items); }
    }
#endif

    public static int RunChecks(FarmProductionDefinition carrot, FarmProductionDefinition potato,
        CropCatalog catalog, ItemDataContext items)
    {
        int checks = 0;
        Require(carrot && potato && carrot.SeedItemId != potato.SeedItemId, "Distinct seed definitions required.");
        Require(catalog.TryValidate(items, out string reason), reason);
        foreach (FarmProductionDefinition crop in new[] { carrot, potato })
        {
            using (var fixture = new Fixture(catalog, items))
            {
                Require(fixture.Source.TryGetClickPopup(out PopupType popup) && popup == PopupType.SeedSelection,
                    "An empty farm must open even without stock.");
                var available = new List<ItemInfo>();
                fixture.Source.GetAvailableSeeds(available);
                Require(available.Count == 0 && !fixture.Farm.HasCrop, "Query mutated an empty farm.");
                Require(!fixture.Source.TryPlantSeed(crop.SeedItemId, out SeedPlantResult result)
                    && result == SeedPlantResult.NotEnoughSeeds && !fixture.Farm.HasCrop, "Empty stock was accepted.");
                checks += 3;

                fixture.Warehouse.TryAdd(crop.SeedItemId, 2, out _);
                fixture.Source.GetAvailableSeeds(available);
                Require(available.Count == 1 && available[0].ID == crop.SeedItemId
                    && fixture.Warehouse.GetQuantity(crop.SeedItemId) == 2 && !fixture.Farm.HasCrop,
                    "Reading available seeds changed state or returned wrong items.");
                int events = 0;
                Action observer = () =>
                {
                    ++events;
                    Require(fixture.Farm.CurrentDefinition == crop && fixture.Warehouse.GetQuantity(crop.SeedItemId) == 1,
                        "Observer saw an incomplete payment/planting transaction.");
                };
                fixture.Farm.StateChanged += observer;
                Require(fixture.Source.TryPlantSeed(crop.SeedItemId, out result) && result == SeedPlantResult.Success,
                    "Valid planting failed.");
                fixture.Farm.StateChanged -= observer;
                Require(events == 1 && fixture.Farm.CurrentProgress == 0f && !fixture.Farm.CanSelectCrop,
                    "Commit notification or zero-progress lock is incorrect.");
                Require(!fixture.Source.TryPlantSeed(crop.SeedItemId, out result) && result == SeedPlantResult.FarmOccupied
                    && fixture.Warehouse.GetQuantity(crop.SeedItemId) == 1, "Repeat press charged a second seed.");
                Require(!fixture.Source.TryGetClickPopup(out _), "An occupied farm reopened seed selection.");
                checks += 5;

                fixture.Farm.TryInteract(new InteractionRequest(ActionType.Farming, strength: 10000f), out _);
                Require(fixture.Farm.Phase == FarmWorkPhase.Harvesting, "Growth did not reach harvest.");
                Require(!fixture.Source.TryPlantSeed(crop.SeedItemId, out result)
                    && result == SeedPlantResult.FarmOccupied, "Harvest allowed replacement.");
                var cargo = new WorkerInventory(10000, _ => { });
                Require(fixture.Farm.TryInteract(new InteractionRequest(ActionType.Harvest, strength: 10000f, cargo: cargo), out _)
                    && !fixture.Farm.HasCrop && fixture.Warehouse.GetQuantity(crop.SeedItemId) == 1,
                    "Final harvest must empty the farm without automatically replanting.");
                Require(cargo.GetQuantity(crop.OutputItemId) >= crop.MinimumYield
                    && cargo.GetQuantity(crop.OutputItemId) <= crop.MaximumYield, "Harvest output mismatch.");
                Require(fixture.Source.TryPlantSeed(crop.SeedItemId, out _)
                    && fixture.Warehouse.GetQuantity(crop.SeedItemId) == 0, "Second cycle failed.");
                checks += 5;
            }
        }

        using (var first = new Fixture(catalog, items))
        using (var second = new Fixture(catalog, items))
        {
            Set(second.Source, "_warehouse", first.Warehouse);
            first.Warehouse.TryAdd(carrot.SeedItemId, 1, out _);
            Require(!first.Source.TryPlantSeed(-1, out SeedPlantResult result) && result == SeedPlantResult.InvalidSeed
                && first.Warehouse.GetQuantity(carrot.SeedItemId) == 1 && !first.Farm.HasCrop, "Invalid seed mutated state.");
            first.Source.enabled = false;
            Require(!first.Source.TryPlantSeed(carrot.SeedItemId, out result) && result == SeedPlantResult.FarmUnavailable,
                "Disabled source allowed planting.");
            first.Source.enabled = true;
            first.Farm.enabled = false;
            Require(!first.Source.TryPlantSeed(carrot.SeedItemId, out result) && result == SeedPlantResult.FarmUnavailable,
                "Disabled farm allowed planting.");
            first.Farm.enabled = true;
            first.Warehouse.enabled = false;
            Require(!first.Source.TryPlantSeed(carrot.SeedItemId, out result), "Disabled warehouse allowed planting.");
            first.Warehouse.enabled = true;
            Require(first.Source.TryPlantSeed(carrot.SeedItemId, out _), "Re-enabled source failed.");
            Require(!second.Source.TryPlantSeed(carrot.SeedItemId, out result) && result == SeedPlantResult.NotEnoughSeeds
                && !second.Farm.HasCrop && first.Farm.CurrentDefinition == carrot, "Two farms consumed the same last seed.");
            first.Root.SetActive(false);
            first.Root.SetActive(true);
            Require(!first.Source.TryPlantSeed(carrot.SeedItemId, out result) && result == SeedPlantResult.FarmOccupied,
                "Re-enable must not erase a planted farm.");
            UnityEngine.Object.DestroyImmediate(second.Farm);
            Require(!second.Source.TryPlantSeed(carrot.SeedItemId, out result) && result == SeedPlantResult.FarmUnavailable,
                "Destroyed farm allowed planting.");
            checks += 8;
        }

        using (var fixture = new Fixture(catalog, items))
        {
            fixture.Warehouse.TryAdd(carrot.SeedItemId, 2, out _);
            Require(!fixture.Warehouse.TryRemove(carrot.SeedItemId, 3)
                && !fixture.Warehouse.TryRemove(carrot.SeedItemId, 0)
                && !fixture.Warehouse.TryRemove(carrot.SeedItemId, -1)
                && fixture.Warehouse.GetQuantity(carrot.SeedItemId) == 2, "Rejected removal changed stock.");
            Require(!fixture.Warehouse.TryRemoveBatch(new Dictionary<int, int>
                { { carrot.SeedItemId, 1 }, { potato.SeedItemId, 1 } })
                && fixture.Warehouse.GetQuantity(carrot.SeedItemId) == 2, "Batch sale lost atomicity.");
            checks += 2;
        }

        CropCatalog invalidCatalog = UnityEngine.Object.Instantiate(catalog);
        FarmProductionDefinition invalidCrop = UnityEngine.Object.Instantiate(potato);
        try
        {
            Set(invalidCrop, "_seedItemId", carrot.SeedItemId);
            Set(invalidCatalog, "_definitions", new List<FarmProductionDefinition> { carrot, invalidCrop });
            Require(!invalidCatalog.TryValidate(items, out _), "Duplicate seed mapping accepted.");
            Set(invalidCrop, "_seedItemId", carrot.OutputItemId);
            Require(!invalidCatalog.TryValidate(items, out _), "A food item was accepted as seed.");
            Set(invalidCrop, "_seedItemId", int.MaxValue);
            Require(!invalidCatalog.TryValidate(items, out _), "Missing seed item accepted.");
            checks += 3;
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(invalidCatalog);
            UnityEngine.Object.DestroyImmediate(invalidCrop);
        }
        return checks;
    }

    private sealed class Fixture : IDisposable
    {
        public readonly GameObject Root;
        public readonly FarmWorkSite Farm;
        public readonly WarehouseInventory Warehouse;
        public readonly FarmSeedSource Source;

        public Fixture(CropCatalog catalog, ItemDataContext items)
        {
            Root = new GameObject("SeedPlantingCheck") { hideFlags = HideFlags.HideAndDontSave };
            Farm = Root.AddComponent<FarmWorkSite>();
            Warehouse = Root.AddComponent<WarehouseInventory>();
            Source = Root.AddComponent<FarmSeedSource>();
            Set(Farm, "_randomSource", Root.AddComponent<SeededRandomSource>());
            Set(Source, "_farm", Farm);
            Set(Source, "_warehouse", Warehouse);
            Set(Source, "_cropCatalog", catalog);
            Set(Source, "_itemDataContext", items);
        }

        public void Dispose() => UnityEngine.Object.DestroyImmediate(Root);
    }

    private static void Set(object target, string name, object value)
    {
        FieldInfo field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
        Require(field != null, "Missing test fixture field: " + name);
        field.SetValue(target, value);
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}
