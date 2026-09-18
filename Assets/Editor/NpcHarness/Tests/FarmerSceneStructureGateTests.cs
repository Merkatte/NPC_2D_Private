using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

internal sealed class FarmerSceneStructureGateTests
{
    private const string SavedScenePath = "Assets/Scenes/FarmerTest.unity";

    [Test]
    public void EvaluateScene_SavedProductionScene_PassesWithEvidence()
    {
        WithPreview(scene => AssertPass(Evaluate(scene)));
    }

    [Test]
    public void EvaluateStructure_SavedProductionScene_PreservesDependenciesAndEditorSetup()
    {
        Dictionary<string, string> before = CaptureDependencyHashes();
        string[] setup = CaptureSceneSetup();
        int previewCount = EditorSceneManager.previewSceneCount;

        HarnessGateResult result = FarmerSceneValidator.EvaluateStructure("farmer-read-only-test");

        AssertPass(result);
        HarnessGateResult repeated = FarmerSceneValidator.EvaluateStructure("farmer-read-only-repeat");
        Assert.That(CheckEvidence(repeated), Is.EqualTo(CheckEvidence(result)));
        Assert.That(CaptureSceneSetup(), Is.EqualTo(setup), "Validation changed the user's loaded scene setup.");
        Assert.That(EditorSceneManager.previewSceneCount, Is.EqualTo(previewCount), "Validation leaked a preview scene.");
        Assert.That(CaptureDependencyHashes(), Is.EquivalentTo(before), "Validation modified saved dependencies.");
    }

    [Test]
    public void EvaluateStructure_DirtyLoadedTarget_RejectsWithoutSavingOrClosingIt()
    {
        Assert.That(Application.isBatchMode, Is.True, "This guard test runs only in an isolated batch project.");
        Assert.That(SceneManager.GetSceneByPath(SavedScenePath).isLoaded, Is.False,
            "Refusing to modify a scene loaded before this test.");
        string[] setup = CaptureSceneSetup();
        Dictionary<string, string> hashes = CaptureDependencyHashes();
        Scene ownedScene = EditorSceneManager.OpenScene(SavedScenePath, OpenSceneMode.Additive);
        try
        {
            EditorSceneManager.MarkSceneDirty(ownedScene);
            string[] dirtySetup = CaptureSceneSetup();
            HarnessGateResult result = FarmerSceneValidator.EvaluateStructure("farmer-dirty-scene-test");
            Assert.That(result.success, Is.False);
            Assert.That(result.status, Is.EqualTo("InfrastructureError"));
            Assert.That(ownedScene.isLoaded && ownedScene.isDirty, Is.True);
            Assert.That(CaptureSceneSetup(), Is.EqualTo(dirtySetup));
            Assert.That(CaptureDependencyHashes(), Is.EquivalentTo(hashes));
        }
        finally
        {
            if (ownedScene.IsValid() && ownedScene.isLoaded)
                EditorSceneManager.CloseScene(ownedScene, true);
        }
        Assert.That(CaptureSceneSetup(), Is.EqualTo(setup));
    }

    [Test]
    public void EvaluateScene_RepeatedFreshLoads_ProduceIdenticalChecks()
    {
        string[] first = null;
        WithPreview(scene => first = CheckEvidence(Evaluate(scene)));
        WithPreview(scene => Assert.That(CheckEvidence(Evaluate(scene)), Is.EqualTo(first)));
    }

    [Test]
    public void EvaluateScene_MissingWorkerPool_Fails()
    {
        WithPreview(scene =>
        {
            SetReference(FindOne<NPCManager>(scene), "_workerPool", null);
            AssertFailure(Evaluate(scene));
        });
    }

    [Test]
    public void EvaluateScene_WrongSameTypedWarehouseReference_Fails()
    {
        WithPreview(scene =>
        {
            WarehouseDepositPoint deposit = FindOne<WarehouseDepositPoint>(scene);
            GameObject alternate = new GameObject("WrongWarehouseInventoryFixture");
            alternate.SetActive(false);
            SceneManager.MoveGameObjectToScene(alternate, scene);
            WarehouseInventory inventory = alternate.AddComponent<WarehouseInventory>();
            SetReference(deposit, "_inventorySource", inventory);
            HarnessGateResult result = Evaluate(scene);
            AssertFailure(result);
            Assert.That(result.checks.Single(check => check.id == "deposit.inventory-owner").success,
                Is.False, "A non-null reference of the correct type must still fail when it targets another warehouse.");
        });
    }

    [Test]
    public void EvaluateScene_MissingYieldRandomSource_Fails()
    {
        WithPreview(scene =>
        {
            SetReference(FindOne<FarmWorkSite>(scene), "_randomSource", null);
            AssertFailure(Evaluate(scene));
        });
    }

    [Test]
    public void EvaluateScene_DuplicateCreationRole_Fails()
    {
        WithPreview(scene =>
        {
            DuplicateFirstRow(FindOne<NPCManager>(scene), "_creationEntries");
            AssertFailure(Evaluate(scene));
        });
    }

    [Test]
    public void EvaluateScene_DuplicateDestinationKey_Fails()
    {
        WithPreview(scene =>
        {
            DuplicateFirstRow(FindOne<DestinationDB>(scene), "_destionations");
            AssertFailure(Evaluate(scene));
        });
    }

    [Test]
    public void EvaluateScene_MissingFarmDestination_Fails()
    {
        WithPreview(scene =>
        {
            RemoveDestination(scene, BuildingType.Farm);
            AssertFailure(Evaluate(scene));
        });
    }

    [Test]
    public void EvaluateScene_MissingWarehouseDestination_Fails()
    {
        WithPreview(scene =>
        {
            RemoveDestination(scene, BuildingType.Warehouse);
            AssertFailure(Evaluate(scene));
        });
    }

    [Test]
    public void EvaluateScene_UnregisteredFarmProvider_Fails()
    {
        WithPreview(scene =>
        {
            RemoveProvider(scene, FindOne<FarmWorkSite>(scene));
            AssertFailure(Evaluate(scene));
        });
    }

    [Test]
    public void EvaluateScene_UnregisteredWarehouseProvider_Fails()
    {
        WithPreview(scene =>
        {
            RemoveProvider(scene, FindOne<WarehouseDepositPoint>(scene));
            AssertFailure(Evaluate(scene));
        });
    }

    [Test]
    public void EvaluateScene_DuplicateManager_Fails()
    {
        WithPreview(scene =>
        {
            GameObject duplicate = new GameObject("DuplicateNPCManagerFixture");
            duplicate.SetActive(false);
            SceneManager.MoveGameObjectToScene(duplicate, scene);
            duplicate.AddComponent<NPCManager>();
            AssertFailure(Evaluate(scene));
        });
    }

    [Test]
    public void EvaluateScene_OptionalStartingCropAbsent_Passes()
    {
        WithPreview(scene =>
        {
            SetReference(FindOne<FarmWorkSite>(scene), "_startingDefinition", null);
            AssertPass(Evaluate(scene));
        });
    }

    [Test]
    public void EvaluateScene_DoesNotInitializeRuntimeRegistriesOrFarm()
    {
        WithPreview(scene =>
        {
            FarmWorkSite farm = FindOne<FarmWorkSite>(scene);
            NPCManager manager = FindOne<NPCManager>(scene);
            DestinationDB destinations = FindOne<DestinationDB>(scene);
            string farmState = EditorJsonUtility.ToJson(farm);
            string managerState = EditorJsonUtility.ToJson(manager);
            string destinationState = EditorJsonUtility.ToJson(destinations);
            var runtimeFields = new[]
            {
                (owner: (object)manager, name: "_entryLookup"),
                (owner: (object)destinations, name: "_destinationDB"),
                (owner: (object)farm, name: "_currentDefinition"),
            };
            foreach (var field in runtimeFields)
                Assert.That(ReadPrivateField(field.owner, field.name), Is.Null, "Fixture already initialized runtime state.");

            AssertPass(Evaluate(scene));

            foreach (var field in runtimeFields)
                Assert.That(ReadPrivateField(field.owner, field.name), Is.Null, "Structure validation initialized runtime state.");
            Assert.That(EditorJsonUtility.ToJson(farm), Is.EqualTo(farmState));
            Assert.That(EditorJsonUtility.ToJson(manager), Is.EqualTo(managerState));
            Assert.That(EditorJsonUtility.ToJson(destinations), Is.EqualTo(destinationState));
        });
    }

    private static object ReadPrivateField(object owner, string name)
    {
        var field = owner.GetType().GetField(name,
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, "Runtime observation field is absent: " + name);
        return field.GetValue(owner);
    }

    private static void WithPreview(Action<Scene> assertion)
    {
        Scene scene = EditorSceneManager.OpenPreviewScene(SavedScenePath);
        try
        {
            Assert.That(scene.IsValid() && scene.isLoaded, Is.True);
            assertion(scene);
        }
        finally
        {
            if (scene.IsValid())
                EditorSceneManager.ClosePreviewScene(scene);
        }
    }

    private static T FindOne<T>(Scene scene) where T : Component
    {
        T[] matches = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<T>(true)).ToArray();
        Assert.That(matches.Length, Is.EqualTo(1), "Fixture must contain one " + typeof(T).Name);
        return matches[0];
    }

    private static void SetReference(UnityEngine.Object target, string name, UnityEngine.Object value)
    {
        using (SerializedObject serialized = new SerializedObject(target))
        {
            SerializedProperty property = serialized.FindProperty(name);
            Assert.That(property, Is.Not.Null, "Fixture field not found: " + name);
            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    private static void DuplicateFirstRow(UnityEngine.Object target, string name)
    {
        using (SerializedObject serialized = new SerializedObject(target))
        {
            SerializedProperty rows = serialized.FindProperty(name);
            Assert.That(rows, Is.Not.Null);
            Assert.That(rows.arraySize, Is.GreaterThan(0));
            rows.InsertArrayElementAtIndex(0);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    private static void RemoveDestination(Scene scene, BuildingType building)
    {
        using (SerializedObject serialized = new SerializedObject(FindOne<DestinationDB>(scene)))
        {
            SerializedProperty rows = serialized.FindProperty("_destionations");
            int removed = 0;
            for (int index = rows.arraySize - 1; index >= 0; index--)
            {
                if (rows.GetArrayElementAtIndex(index).FindPropertyRelative("BuildingType").intValue != (int)building)
                    continue;
                rows.DeleteArrayElementAtIndex(index);
                removed++;
            }
            Assert.That(removed, Is.EqualTo(1));
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    private static void RemoveProvider(Scene scene, BaseInteractionProvider provider)
    {
        using (SerializedObject serialized = new SerializedObject(FindOne<InteractableManager>(scene)))
        {
            SerializedProperty rows = serialized.FindProperty("_interactables");
            int removed = 0;
            for (int index = rows.arraySize - 1; index >= 0; index--)
            {
                if (rows.GetArrayElementAtIndex(index).objectReferenceValue != provider)
                    continue;
                rows.GetArrayElementAtIndex(index).objectReferenceValue = null;
                rows.DeleteArrayElementAtIndex(index);
                removed++;
            }
            Assert.That(removed, Is.EqualTo(1));
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    private static HarnessGateResult Evaluate(Scene scene)
    {
        return FarmerSceneValidator.EvaluateScene(scene, "farmer-fixture-test");
    }

    private static void AssertPass(HarnessGateResult result)
    {
        Assert.That(result.success, Is.True, string.Join("\n", result.checks.Where(check => !check.success)
            .Select(check => check.id + ": " + check.message + " / " + check.actual)) + "\n" + result.message);
        Assert.That(result.status, Is.EqualTo("Pass"));
        Assert.That(result.profile, Is.EqualTo("FarmerScene.Structure"));
        Assert.That(result.profileVersion, Is.EqualTo(1));
        Assert.That(result.checks, Is.Not.Empty);
        Assert.That(result.checks.All(check => check.success), Is.True);
        Assert.That(result.checks.Select(check => check.id).Distinct().Count(), Is.EqualTo(result.checks.Length));
    }

    private static void AssertFailure(HarnessGateResult result)
    {
        Assert.That(result.success, Is.False, "Broken scene was accepted.");
        Assert.That(result.status, Is.EqualTo("Fail"), "A scene defect must be a candidate failure, not an infrastructure error.");
        HarnessGateCheckResult[] failures = result.checks.Where(check => !check.success).ToArray();
        Assert.That(failures, Is.Not.Empty);
        Assert.That(failures.All(check => !string.IsNullOrWhiteSpace(check.expected)
            && !string.IsNullOrWhiteSpace(check.actual)), Is.True, "Failure needs expected and actual evidence.");
    }

    private static string[] CheckEvidence(HarnessGateResult result)
    {
        AssertPass(result);
        return result.checks.Select(check => string.Join("|", check.id, check.status, check.expected, check.actual)).ToArray();
    }

    private static string[] CaptureSceneSetup()
    {
        return EditorSceneManager.GetSceneManagerSetup()
            .Select(scene => string.Join("|", scene.path, scene.isLoaded, scene.isActive)).ToArray();
    }

    private static Dictionary<string, string> CaptureDependencyHashes()
    {
        Dictionary<string, string> hashes = new Dictionary<string, string>(StringComparer.Ordinal);
        string[] paths = AssetDatabase.GetDependencies(SavedScenePath, true)
            .SelectMany(path => new[] { path, path + ".meta" }).Where(File.Exists).OrderBy(path => path, StringComparer.Ordinal).ToArray();
        using (SHA256 sha = SHA256.Create())
        {
            foreach (string path in paths)
                hashes.Add(path, BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))));
        }
        Assert.That(hashes.ContainsKey(SavedScenePath), Is.True);
        return hashes;
    }
}
