using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

// Bounded authoring only. Run after compilation, with a clean, active DefenseTest scene.
public static class DepthOrderSetup
{
    private const string ScenePath = "Assets/Scenes/DefenseTest.unity";
    private const string DefenseFolder = "Assets/Prefab/Defense/";
    private const string CaravanPath = "Assets/Prefab/InGame/MerchantCaravan.prefab";
    // Both approved GuardPost visuals use the same centered 1254px sprite at 150 PPU and 0.4 scale.
    // Their root is the image center; the existing scene entrance is a distant patrol-area anchor.
    private const float GuardPostGroundYOffset = -1.672f;
    private static readonly string[] _prefabPaths =
    {
        DefenseFolder + "DefenseNPC.prefab", DefenseFolder + "DefenseEnemy.prefab",
        DefenseFolder + "Enemies/DefenseGoblinArcher.prefab",
        DefenseFolder + "Enemies/DefenseShaman.prefab",
        DefenseFolder + "Enemies/DefenseSalamander.prefab",
        DefenseFolder + "GuardPostDefense.prefab", DefenseFolder + "HouseDefense.prefab",
        DefenseFolder + "InnDefense.prefab", DefenseFolder + "RestaurantDefense.prefab",
        DefenseFolder + "SoilDefense.prefab", DefenseFolder + "WarehouseDefense.prefab",
        CaravanPath, DefenseFolder + "Arrow.prefab",
        DefenseFolder + "Enemies/Projectiles/EnergyOrb.prefab",
        DefenseFolder + "Enemies/Projectiles/Fireball.prefab"
    };
    private static readonly string[] _layerFields = { "_materials", "_lowerFrame", "_upperFrame", "_cover" };
    private static readonly string[] _layerNames = { "Materials", "LowerFrame", "UpperFrame", "Cover" };
    private static readonly string[] _uiNames = { "HUDUI", "HoveringUI", "BackBG", "PopupUI", "DefenseUI" };

    [MenuItem("Tools/NPC/Defense/Apply Depth Order")]
    public static void Setup()
    {
        Scene scene = RequireScene();
        ConfigureScene(scene, false);
        // Resolve every target before writing any asset. Never save inherited source prefabs.
        foreach (string path in _prefabPaths)
        {
            GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Require(asset, "Missing approved prefab: " + path);
            foreach (Component component in asset.GetComponentsInChildren<Component>(true))
                Require(component && !EditorUtility.IsDirty(component) && !EditorUtility.IsDirty(component.gameObject),
                    "Missing component or unsaved prefab edits: " + path);
            ConfigurePrefab(asset, path, false);
        }

        foreach (string path in _prefabPaths)
        {
            GameObject contents = PrefabUtility.LoadPrefabContents(path);
            try
            {
                ConfigurePrefab(contents, path, true);
                Require(PrefabUtility.SaveAsPrefabAsset(contents, path), "Could not save " + path);
                Debug.Log("DepthOrderSetup saved " + path);
            }
            finally { PrefabUtility.UnloadPrefabContents(contents); }
        }

        ConfigureScene(scene, true);
        EditorSceneManager.MarkSceneDirty(scene);
        Require(EditorSceneManager.SaveScene(scene, ScenePath), "Could not save " + ScenePath);
        Debug.Log("DepthOrderSetup saved DefenseTest: 9 walls, 11 construction layer groups, placed facilities, " +
            "21 crop groups and UI sibling order. Only the 15 approved prefabs and DefenseTest were saved.");
    }

    private static Scene RequireScene()
    {
        Require(!EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isCompiling &&
            !EditorApplication.isUpdating && !PrefabStageUtility.GetCurrentPrefabStage(),
            "Depth authoring requires an idle Editor outside Play Mode and Prefab Mode.");
        Scene scene = SceneManager.GetActiveScene();
        Require(scene.IsValid() && scene.isLoaded && scene.path == ScenePath && !scene.isDirty,
            "Open and save DefenseTest before depth authoring; unsaved scene edits are preserved.");
        return scene;
    }

    private static void ConfigureScene(Scene scene, bool apply)
    {
        List<DefenseWallSegment> walls = SceneComponents<DefenseWallSegment>(scene);
        List<BuildingPlot> plots = SceneComponents<BuildingPlot>(scene);
        Require(walls.Count == 9 && plots.Count == 11, "Expected 9 walls and 11 BuildingPlot roots.");
        foreach (DefenseWallSegment wall in walls)
        {
            Transform anchor = Reference<Transform>(wall, "_groundAnchor");
            if (apply) Group(wall.transform, anchor);
        }

        int facilities = 0;
        foreach (BuildingPlot plot in plots)
        {
            ConfigureConstruction(plot, apply);
            Component facility = new SerializedObject(plot).FindProperty("_initialFacility").objectReferenceValue as Component;
            if (!facility) continue;
            Require(plot.name.StartsWith("DefensePlot_", StringComparison.Ordinal) && facility.transform.IsChildOf(plot.transform),
                "Unexpected placed facility owner: " + plot.name);
            ++facilities;
            if (facility.name == "Soil") ConfigureSoil(facility.gameObject, apply);
            else
            {
                Require(facility.name == "Warehouse" || facility.name == "Inn" || facility.name == "Eat" ||
                    facility.name == "GuardPost", "Unexpected placed facility: " + facility.name);
                bool isGuardPost = facility.name == "GuardPost";
                Transform anchor = isGuardPost ? facility.transform : Child(facility.transform, "EntrancePoint");
                if (apply) Group(facility.transform, anchor, isGuardPost ? GuardPostGroundYOffset : 0f);
            }
        }
        Require(facilities == 5, "Expected five placed facilities including Soil.");

        Transform map = SceneTransform(scene, "InGameObjects/Map");
        Transform townHall = Child(map, "TownHall");
        DefenseBuildingDurability townDurability = townHall.GetComponent<DefenseBuildingDurability>();
        Require(townDurability, "TownHall requires its existing durability work anchor.");
        Transform townGround = Reference<Transform>(townDurability, "_workAnchor");
        if (apply) Group(townHall, townGround);

        Transform canvas = null;
        foreach (Canvas candidate in SceneComponents<Canvas>(scene))
            if (candidate.transform.Find("HUDUI") && candidate.transform.Find("PopupUI"))
            {
                Require(!canvas, "More than one main UI Canvas.");
                canvas = candidate.transform;
            }
        Require(canvas, "Main UI Canvas is missing.");
        foreach (string childName in _uiNames) Child(canvas, childName);
        if (apply)
        {
            // Sibling order only: retain activation, RectTransform geometry and all references.
            Undo.RegisterFullObjectHierarchyUndo(canvas.gameObject, "Order Defense UI");
            for (int i = 0; i < _uiNames.Length; ++i)
            {
                Transform child = Child(canvas, _uiNames[i]);
                child.SetSiblingIndex(i);
                Record(child);
            }
        }
    }

    private static void ConfigureConstruction(BuildingPlot plot, bool apply)
    {
        ConstructionVisual visual = plot.GetComponent<ConstructionVisual>();
        Require(visual, "Missing ConstructionVisual on " + plot.name);
        GameObject empty = Reference<GameObject>(visual, "_emptyVisual");
        Require(empty.transform.parent == plot.transform, "EmptyPlot must remain a direct plot child.");
        Transform layerRoot = plot.transform.Find("ConstructionLayers");
        if (layerRoot)
        {
            Require(layerRoot.localPosition == Vector3.zero && layerRoot.localRotation == Quaternion.identity &&
                layerRoot.localScale == Vector3.one && layerRoot.childCount == 4,
                "Existing ConstructionLayers must be an identity container with four layers: " + plot.name);
            foreach (Component component in layerRoot.GetComponents<Component>())
                Require(component is Transform || component is SortingGroup || component is GroundDepthSort,
                    "Unexpected component on ConstructionLayers: " + plot.name);
        }
        SpriteRenderer[] layers = new SpriteRenderer[4];
        for (int i = 0; i < layers.Length; ++i)
        {
            layers[i] = Reference<SpriteRenderer>(visual, _layerFields[i]);
            Require(layers[i].name == _layerNames[i] && layers[i].gameObject != empty &&
                layers[i].transform.parent == (layerRoot ? layerRoot : plot.transform),
                "Unexpected construction layer hierarchy: " + plot.name + "/" + _layerNames[i]);
        }
        Require(!plot.GetComponent<SortingGroup>() && !plot.GetComponent<GroundDepthSort>(),
            "Do not group the whole plot with EmptyPlot/completed facilities: " + plot.name);
        if (!apply) return;
        if (!layerRoot) layerRoot = CreateChild(plot.transform, "ConstructionLayers", Vector3.zero);
        foreach (SpriteRenderer layer in layers)
            if (layer.transform.parent != layerRoot)
                Undo.SetTransformParent(layer.transform, layerRoot, "Group Construction Layers");
        SetReference(visual, "_layersRoot", layerRoot);
        Group(layerRoot, plot.transform);
    }

    private static void ConfigurePrefab(GameObject root, string path, bool apply)
    {
        if (path == CaravanPath) { ConfigureCaravan(root, apply); return; }
        if (path.EndsWith("SoilDefense.prefab", StringComparison.Ordinal)) { ConfigureSoil(root, apply); return; }
        if (path.EndsWith("Arrow.prefab", StringComparison.Ordinal) || path.Contains("/Projectiles/"))
        {
            SpriteRenderer[] renderers = root.GetComponentsInChildren<SpriteRenderer>(true);
            Require(renderers.Length == 1, "Expected one projectile renderer: " + path);
            if (apply)
            {
                renderers[0].sortingOrder = 30000;
                Record(renderers[0]);
            }
            return;
        }
        bool facility = path.EndsWith("PostDefense.prefab", StringComparison.Ordinal) ||
            path.EndsWith("HouseDefense.prefab", StringComparison.Ordinal) ||
            path.EndsWith("InnDefense.prefab", StringComparison.Ordinal) ||
            path.EndsWith("RestaurantDefense.prefab", StringComparison.Ordinal) ||
            path.EndsWith("WarehouseDefense.prefab", StringComparison.Ordinal);
        bool isGuardPost = path == DefenseFolder + "GuardPostDefense.prefab";
        Transform anchor = facility && !isGuardPost ? Child(root.transform, "EntrancePoint") : root.transform;
        float offset = isGuardPost ? GuardPostGroundYOffset :
            path.Contains("DefenseGoblinArcher") || path.Contains("DefenseShaman") ? -0.6f :
            path.Contains("DefenseSalamander") ? -0.35f : 0f;
        if (apply) Group(root.transform, anchor, offset);
        if (path == DefenseFolder + "DefenseNPC.prefab")
        {
            Canvas panic = Child(root.transform, "DefensePanicSpeech").GetComponent<Canvas>();
            Require(panic && panic.renderMode == RenderMode.WorldSpace, "Expected existing world-space panic Canvas.");
            if (apply)
            {
                panic.overrideSorting = true;
                panic.sortingOrder = 25000;
                Record(panic);
            }
        }
    }

    private static void ConfigureSoil(GameObject soil, bool apply)
    {
        Transform presenter = Child(soil.transform, "Visual/CropPresenter");
        Require(presenter.childCount == 21, "Expected 21 crop spots on " + soil.name);
        foreach (Transform spot in presenter)
        {
            Require(spot.name == "Spot" || spot.name.StartsWith("Spot (", StringComparison.Ordinal), "Unexpected crop spot.");
            Transform carrot = Child(spot, "Carrot");
            if (apply) Group(carrot, carrot);
        }
        Transform rubble = Child(soil.transform, "DefenseRubble");
        Require(!soil.GetComponent<SortingGroup>() && !soil.GetComponent<GroundDepthSort>(), "Soil ground must stay outside crop groups.");
        if (apply) Group(rubble, rubble);
    }

    private static void ConfigureCaravan(GameObject root, bool apply)
    {
        MerchantCaravan caravan = root.GetComponent<MerchantCaravan>();
        Require(caravan, "Missing MerchantCaravan director.");
        Transform building = Reference<Transform>(caravan, "_building");
        Transform shop = Child(building, "Shop");
        Animator merchant = Reference<Animator>(caravan, "_merchantAnimator");
        Transform merchantVisual = Child(merchant.transform, "Visual");
        SerializedProperty birds = new SerializedObject(caravan).FindProperty("_birds");
        Require(birds.arraySize == 3, "Expected three caravan birds.");
        TransportBird[] crew = new TransportBird[3];
        for (int i = 0; i < crew.Length; ++i)
        {
            crew[i] = birds.GetArrayElementAtIndex(i).objectReferenceValue as TransportBird;
            Require(crew[i], "Missing caravan bird.");
        }
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Animation/Merchant/MerchantLanding.anim");
        Require(clip, "Missing merchant landing clip.");
        Vector3 restingPosition = merchantVisual.localPosition;
        Vector3 endpoint = new Vector3(LandingCoordinate(clip, "x", restingPosition.x),
            LandingCoordinate(clip, "y", restingPosition.y), LandingCoordinate(clip, "z", restingPosition.z));
        Vector3 groundPosition = root.transform.InverseTransformPoint(merchant.transform.TransformPoint(endpoint));
        Transform ground = root.transform.Find("MerchantGroundAnchor");
        if (ground) Require(ground.childCount == 0 && ground.GetComponents<Component>().Length == 1 &&
            ground.localPosition == groundPosition && ground.localRotation == Quaternion.identity && ground.localScale == Vector3.one,
            "Preserving an unrelated or moved MerchantGroundAnchor.");
        Require(!root.GetComponent<SortingGroup>() && !building.GetComponent<SortingGroup>(),
            "Caravan requires five separate groups, not a shared root/building group.");
        if (!apply) return;
        if (!ground) ground = CreateChild(root.transform, "MerchantGroundAnchor", groundPosition);
        GroundDepthSort[] sorts =
        {
            Group(shop, root.transform, 0f, 0), Group(merchantVisual, ground, 0f, 3),
            Group(crew[0].transform, crew[0].transform, 0f, 1),
            Group(crew[1].transform, crew[1].transform, 0f, 2),
            Group(crew[2].transform, crew[2].transform, 0f, 4)
        };
        SerializedObject data = new SerializedObject(caravan);
        SerializedProperty targets = data.FindProperty("_groundDepthSorts");
        targets.arraySize = sorts.Length;
        for (int i = 0; i < sorts.Length; ++i) targets.GetArrayElementAtIndex(i).objectReferenceValue = sorts[i];
        data.ApplyModifiedPropertiesWithoutUndo();
        Record(caravan);
    }

    private static float LandingCoordinate(AnimationClip clip, string axis, float restingCoordinate)
    {
        AnimationCurve curve = AnimationUtility.GetEditorCurve(clip,
            EditorCurveBinding.FloatCurve("Visual", typeof(Transform), "m_LocalPosition." + axis));
        // Imported clips omit unanimated axes (the current landing clip has no Z curve).
        // Those axes retain their authored pose rather than requiring a fictitious curve.
        if (curve == null) return restingCoordinate;
        Require(curve.length > 0, "Empty landing position curve: " + axis);
        return curve.Evaluate(clip.length);
    }

    private static GroundDepthSort Group(Transform owner, Transform anchor, float groundOffset = 0f, int flightOffset = 0)
    {
        SortingGroup group = owner.GetComponent<SortingGroup>();
        if (!group) group = Add<SortingGroup>(owner.gameObject);
        GroundDepthSort sort = owner.GetComponent<GroundDepthSort>();
        if (!sort) sort = Add<GroundDepthSort>(owner.gameObject);
        group.sortingLayerID = 0;
        group.sortingOrder = 10000 - Mathf.RoundToInt(Mathf.Clamp((anchor.position.y + groundOffset) * 100f, -9000f, 9000f));
        Record(group);
        SerializedObject data = new SerializedObject(sort);
        data.FindProperty("_sortingGroup").objectReferenceValue = group;
        data.FindProperty("_groundAnchor").objectReferenceValue = anchor;
        data.FindProperty("_groundYOffset").floatValue = groundOffset;
        data.FindProperty("_airborneOrderOffset").intValue = flightOffset;
        data.ApplyModifiedPropertiesWithoutUndo();
        Record(sort);
        return sort;
    }

    private static T Add<T>(GameObject owner) where T : Component
    {
        return owner.scene.path == ScenePath ? Undo.AddComponent<T>(owner) : owner.AddComponent<T>();
    }

    private static Transform CreateChild(Transform parent, string name, Vector3 localPosition)
    {
        GameObject child = new GameObject(name);
        child.transform.SetParent(parent, false);
        child.transform.localPosition = localPosition;
        if (parent.gameObject.scene.path == ScenePath) Undo.RegisterCreatedObjectUndo(child, "Create Depth Container");
        return child.transform;
    }

    private static void SetReference(Object owner, string field, Object value)
    {
        SerializedObject data = new SerializedObject(owner);
        data.FindProperty(field).objectReferenceValue = value;
        data.ApplyModifiedPropertiesWithoutUndo();
        Record(owner);
    }

    private static T Reference<T>(Object owner, string field) where T : Object
    {
        T value = new SerializedObject(owner).FindProperty(field).objectReferenceValue as T;
        Require(value, owner.name + " requires " + field);
        return value;
    }

    private static Transform Child(Transform parent, string path)
    {
        Transform child = parent.Find(path);
        Require(child, "Missing " + parent.name + "/" + path);
        return child;
    }

    private static Transform SceneTransform(Scene scene, string path)
    {
        string[] parts = path.Split(new[] { '/' }, 2);
        foreach (GameObject root in scene.GetRootGameObjects())
            if (root.name == parts[0]) return parts.Length == 1 ? root.transform : Child(root.transform, parts[1]);
        throw new InvalidOperationException("Missing scene root: " + path);
    }

    private static List<T> SceneComponents<T>(Scene scene) where T : Component
    {
        List<T> result = new List<T>();
        foreach (GameObject root in scene.GetRootGameObjects()) result.AddRange(root.GetComponentsInChildren<T>(true));
        return result;
    }

    private static void Record(Object target)
    {
        EditorUtility.SetDirty(target);
        if (PrefabUtility.IsPartOfPrefabInstance(target)) PrefabUtility.RecordPrefabInstancePropertyModifications(target);
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
