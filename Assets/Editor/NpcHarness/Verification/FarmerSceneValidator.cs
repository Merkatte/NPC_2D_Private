using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

internal static class FarmerSceneValidator
{
    public const string ScenePath = "Assets/Scenes/FarmerTest.unity";

    public static HarnessGateResult EvaluateStructure(string runId)
    {
        return SavedSceneInspection.Evaluate(runId, FarmerSceneGateRunner.Profile,
            FarmerSceneGateRunner.ProfileVersion, ScenePath, Inspect);
    }

    // In-memory fixture seam; production entrypoints always use the saved-scene guard above.
    internal static HarnessGateResult EvaluateScene(Scene scene, string runId)
    {
        HarnessGateResultBuilder builder = new HarnessGateResultBuilder(runId,
            FarmerSceneGateRunner.Profile, FarmerSceneGateRunner.ProfileVersion);
        if (!scene.IsValid() || !scene.isLoaded)
        {
            builder.SetInfrastructureError("Inspected scene is not loaded.");
        }
        else
        {
            Inspect(scene, builder);
        }
        return builder.Build();
    }

    private static void Inspect(Scene scene, HarnessGateResultBuilder builder)
    {
        SceneWiringChecks checks = new SceneWiringChecks(scene, builder);
        checks.CheckMissingScripts();
        NPCManager manager = checks.RequireSingle<NPCManager>("scene.npc-manager");
        WorkerPool pool = checks.RequireSingle<WorkerPool>("scene.worker-pool");
        FarmerActionSelector selector = checks.RequireSingle<FarmerActionSelector>("scene.farmer-selector");
        DestinationDB destinations = checks.RequireSingle<DestinationDB>("scene.destinations");
        InteractableManager interactables = checks.RequireSingle<InteractableManager>("scene.interactables");
        FarmWorkSite farm = checks.RequireSingle<FarmWorkSite>("scene.farm");
        WarehouseDepositPoint deposit = checks.RequireSingle<WarehouseDepositPoint>("scene.deposit");
        DataManager data = checks.RequireSingle<DataManager>("scene.data-manager");

        checks.Reference<WorkerPool>(manager, "_workerPool", "npc.pool", pool, true);
        WorkerNPC prefab = checks.Reference<WorkerNPC>(pool, "_workerPrefab", "npc.prefab");
        checks.Check("npc.prefab-asset", prefab && PrefabUtility.IsPartOfPrefabAsset(prefab),
            "persistent WorkerNPC prefab asset", SceneWiringChecks.Describe(prefab));
        checks.Reference<DestinationDB>(selector, "_destinationDB", "selector.destinations", destinations, true);
        checks.Reference<DataManager>(selector, "dataManager", "selector.data", data, true);
        checks.Reference<ActionPool>(selector, "actionPool", "selector.action-pool", requireSameScene: true);
        checks.Reference<NPCDecisionTuning>(selector, "_decisionTuning", "selector.tuning");
        checks.Reference<InteractableManager>(destinations, "_interactableManager", "destinations.registry", interactables, true);

        ValidateCreationEntries(manager, selector, scene, checks);
        ValidateDestinations(destinations, farm, deposit, scene, checks);
        ValidateProviders(interactables, farm, deposit, scene, checks);
        ValidateFarm(farm, scene, checks);

        WarehouseInventory inventory = checks.Reference<WarehouseInventory>(deposit, "_inventorySource",
            "deposit.inventory", requireSameScene: true);
        checks.Check("deposit.inventory-owner", deposit && inventory && inventory.gameObject == deposit.gameObject,
            "WarehouseInventory on the registered WarehouseDepositPoint object", SceneWiringChecks.Describe(inventory));
        ValidateCatalog(data, checks);
    }

    private static void ValidateCreationEntries(NPCManager manager, FarmerActionSelector selector,
        Scene scene, SceneWiringChecks checks)
    {
        List<string> issues = new List<string>();
        HashSet<int> roles = new HashSet<int>();
        bool hasFarmer = false;
        if (!manager)
        {
            issues.Add("NPCManager missing or ambiguous");
        }
        else
        {
            using (SerializedObject serialized = new SerializedObject(manager))
            {
                SerializedProperty entries = serialized.FindProperty("_creationEntries");
                if (entries == null || !entries.isArray || entries.arraySize == 0)
                {
                    issues.Add("creation entries missing or empty");
                }
                else
                {
                    for (int index = 0; index < entries.arraySize; index++)
                    {
                        SerializedProperty entry = entries.GetArrayElementAtIndex(index);
                        int role = entry.FindPropertyRelative("_npcType").intValue;
                        BaseNPCActionSelector assigned = entry.FindPropertyRelative("_selector").objectReferenceValue as BaseNPCActionSelector;
                        NPCStatDefinition definition = entry.FindPropertyRelative("_statDefinition").objectReferenceValue as NPCStatDefinition;
                        if (!Enum.IsDefined(typeof(NPCType), role) || !roles.Add(role))
                        {
                            issues.Add("invalid or duplicate role " + role);
                        }
                        if (!assigned || assigned.gameObject.scene != scene || !assigned.isActiveAndEnabled || !definition)
                        {
                            issues.Add("entry " + index + " has missing/inactive selector or definition");
                        }
                        if (role == (int)NPCType.Farmer)
                        {
                            hasFarmer = true;
                            if (!selector || assigned != selector)
                            {
                                issues.Add("Farmer entry does not reference the inspected FarmerActionSelector");
                            }
                        }
                    }
                }
            }
        }
        if (!hasFarmer)
        {
            issues.Add("Farmer role missing");
        }
        checks.Check("npc.creation-entries", issues.Count == 0,
            "unique valid roles, assigned stat definitions and scene selectors; Farmer uses inspected selector", Evidence(issues));
    }

    private static void ValidateDestinations(DestinationDB destinations, FarmWorkSite farm,
        WarehouseDepositPoint deposit, Scene scene, SceneWiringChecks checks)
    {
        List<string> issues = new List<string>();
        HashSet<int> keys = new HashSet<int>();
        if (!destinations)
        {
            issues.Add("DestinationDB missing or ambiguous");
        }
        else
        {
            using (SerializedObject serialized = new SerializedObject(destinations))
            {
                SerializedProperty entries = serialized.FindProperty("_destionations");
                if (entries == null || !entries.isArray)
                {
                    issues.Add("destination rows missing");
                }
                else
                {
                    for (int index = 0; index < entries.arraySize; index++)
                    {
                        SerializedProperty entry = entries.GetArrayElementAtIndex(index);
                        int key = entry.FindPropertyRelative("BuildingType").intValue;
                        Transform location = entry.FindPropertyRelative("DestinationLoc").objectReferenceValue as Transform;
                        GameObject owner = entry.FindPropertyRelative("DestinationObject").objectReferenceValue as GameObject;
                        if (!Enum.IsDefined(typeof(BuildingType), key) || !keys.Add(key))
                        {
                            issues.Add("invalid or duplicate destination " + key);
                        }
                        if (!location || location.gameObject.scene != scene || !owner || owner.scene != scene)
                        {
                            issues.Add("destination " + key + " lacks scene location/owner");
                        }
                        if (key == (int)BuildingType.Farm && (!farm || owner != farm.gameObject))
                        {
                            issues.Add("Farm destination points to wrong owner");
                        }
                        if (key == (int)BuildingType.Warehouse && (!deposit || owner != deposit.gameObject))
                        {
                            issues.Add("Warehouse destination points to wrong owner");
                        }
                    }
                }
            }
        }
        if (!keys.Contains((int)BuildingType.Farm) || !keys.Contains((int)BuildingType.Warehouse))
        {
            issues.Add("Farm and Warehouse rows are required");
        }
        checks.Check("destinations.rows", issues.Count == 0,
            "unique destination keys with scene references; Farm/Warehouse match inspected providers", Evidence(issues));
    }

    private static void ValidateProviders(InteractableManager manager, FarmWorkSite farm,
        WarehouseDepositPoint deposit, Scene scene, SceneWiringChecks checks)
    {
        List<string> issues = new List<string>();
        HashSet<UnityEngine.Object> providers = new HashSet<UnityEngine.Object>();
        if (!manager)
        {
            issues.Add("InteractableManager missing or ambiguous");
        }
        else
        {
            using (SerializedObject serialized = new SerializedObject(manager))
            {
                SerializedProperty entries = serialized.FindProperty("_interactables");
                if (entries == null || !entries.isArray)
                {
                    issues.Add("provider entries missing");
                }
                else
                {
                    for (int index = 0; index < entries.arraySize; index++)
                    {
                        BaseInteractionProvider provider = entries.GetArrayElementAtIndex(index).objectReferenceValue as BaseInteractionProvider;
                        if (!provider || provider.gameObject.scene != scene || !provider.isActiveAndEnabled || !providers.Add(provider))
                        {
                            issues.Add("invalid, inactive or duplicate provider at " + index);
                        }
                    }
                }
            }
        }
        if (!farm || !providers.Contains(farm) || !deposit || !providers.Contains(deposit))
        {
            issues.Add("inspected FarmWorkSite and WarehouseDepositPoint must both be registered");
        }
        checks.Check("providers.registration", issues.Count == 0,
            "unique active scene providers including inspected farm and warehouse deposit", Evidence(issues));
    }

    private static void ValidateFarm(FarmWorkSite farm, Scene scene, SceneWiringChecks checks)
    {
        SeededRandomSource yield = checks.Reference<SeededRandomSource>(farm, "_randomSource", "farm.yield-random", requireSameScene: true);
        SeededRandomSource position = checks.Reference<SeededRandomSource>(farm, "_workPositionRandomSource", "farm.position-random", requireSameScene: true);
        checks.Check("farm.independent-random", yield && position && yield != position,
            "separate yield and position random sources", SceneWiringChecks.Describe(yield) + " / " + SceneWiringChecks.Describe(position));
        BoxCollider2D area = checks.Reference<BoxCollider2D>(farm, "_workArea", "farm.work-area", requireSameScene: true);
        checks.Check("farm.work-area-size", area && area.size.x > 0f && area.size.y > 0f,
            "positive work-area size", area ? area.size.ToString() : "missing");
        FarmProductionDefinition starting = SceneWiringChecks.ReadReference(farm, "_startingDefinition") as FarmProductionDefinition;
        checks.Check("farm.optional-starting-crop", !starting || starting.IsValid,
            "empty farm allowed; assigned starting definition must be valid", starting ? SceneWiringChecks.Describe(starting) : "empty farm");
    }

    private static void ValidateCatalog(DataManager data, SceneWiringChecks checks)
    {
        ItemDataContext items = checks.Reference<ItemDataContext>(data, "_itemDataContext", "data.items");
        checks.Reference<TextAsset>(items, "_itemTextData", "data.item-csv");
        // Scene consumers declare the actual catalog; do not silently validate an unrelated hard-coded asset.
        TestFarmProductionWindow[] probes = checks.FindAll<TestFarmProductionWindow>();
        List<string> issues = new List<string>();
        HashSet<CropCatalog> catalogs = new HashSet<CropCatalog>();
        foreach (TestFarmProductionWindow probe in probes)
        {
            CropCatalog catalog = SceneWiringChecks.ReadReference(probe, "_cropCatalog") as CropCatalog;
            if (!catalog || SceneWiringChecks.ReadReference(probe, "_itemDataContext") != items)
            {
                issues.Add(SceneWiringChecks.Describe(probe) + " has missing catalog or mismatched item context");
            }
            else
            {
                catalogs.Add(catalog);
            }
        }
        if (catalogs.Count == 0)
        {
            issues.Add("no catalog connected to the FarmerTest production probe");
        }
        ItemDataContext temporaryItems = items ? UnityEngine.Object.Instantiate(items) : null;
        try
        {
            foreach (CropCatalog catalog in catalogs.OrderBy(AssetDatabase.GetAssetPath, StringComparer.Ordinal))
            {
                if (catalog.Definitions.Count == 0 || !catalog.TryValidate(temporaryItems, out string reason))
                {
                    issues.Add(SceneWiringChecks.Describe(catalog) + " invalid or empty");
                }
            }
        }
        catch (Exception exception)
        {
            issues.Add("catalog/item parsing failed: " + exception.Message);
        }
        finally
        {
            if (temporaryItems)
            {
                UnityEngine.Object.DestroyImmediate(temporaryItems);
            }
        }
        checks.Check("data.crop-catalog", issues.Count == 0,
            "nonempty valid crop catalog; unique crop IDs and output items present in scene item CSV", Evidence(issues));
    }

    private static string Evidence(List<string> issues)
    {
        return issues.Count == 0 ? "valid" : string.Join("; ", issues);
    }
}
