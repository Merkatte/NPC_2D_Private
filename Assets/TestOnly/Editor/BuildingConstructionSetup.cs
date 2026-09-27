using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using Object = UnityEngine.Object;

// Explicit asset authoring only; assembly role invokes this after reviewing its exact targets.
public static class BuildingConstructionSetup
{
    private const string DataRoot = "Assets/Data/ScriptableObject/";
    private const string PrefabRoot = "Assets/Prefab/InGame/";
    private const string UiRoot = "Assets/Prefab/UI/";
    private const string BuildingScene = "Assets/Scenes/BuildingTest.unity";
    private const string FarmerScene = "Assets/Scenes/FarmerTest.unity";
    private const string GuardScene = "Assets/Scenes/GuardTest.unity";
    private static readonly string[] FacilityNames = { "Warehouse", "Restaurant", "Inn", "GuardPost", "Soil" };

    [MenuItem("Tools/NPC/Construction/Setup")]
    public static void Setup()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
            throw new InvalidOperationException("Construction setup requires idle Edit Mode.");
        if (PrefabStageUtility.GetCurrentPrefabStage() != null)
            throw new InvalidOperationException("Close the current Prefab Stage before explicit construction setup.");
        for (int i = 0; i < SceneManager.sceneCount; ++i)
            if (SceneManager.GetSceneAt(i).isDirty || string.IsNullOrEmpty(SceneManager.GetSceneAt(i).path))
                throw new InvalidOperationException("Construction setup preserves unsaved scenes; save them before this explicit operation.");
        SceneSetup[] originalSetup = EditorSceneManager.GetSceneManagerSetup();
        try
        {
            WriteCsv();
            ImportArt("material-pile", 300f);
            ImportArt("scaffolding", 150f);
            Scene farmer = EditorSceneManager.OpenScene(FarmerScene, OpenSceneMode.Single);
            EnsureGuardPrefab(farmer);
            ConfigureFacilityPrefabs(All<FarmSeedSource>(farmer).First().gameObject.layer);
            BuildingDataContext buildings = CreateDefinitions();
            BuildActionCost cost = LoadOrCreate<BuildActionCost>(DataRoot + "BuildActionCost.asset");
            Set(cost, "_buildingDataContext", buildings);
            Set(cost, "_decisionTuning", ReadObject(FindOne<BuilderActionSelector>(farmer), "_decisionTuning"));
            AssetDatabase.SaveAssetIfDirty(cost);
            CreateUiPrefabs(buildings);
            MigrateScene(farmer, buildings, cost, false);
            Save(farmer);
            Scene guard = EditorSceneManager.OpenScene(GuardScene, OpenSceneMode.Single);
            MigrateScene(guard, buildings, cost, false);
            Save(guard);
            if (!File.Exists(BuildingScene) && !AssetDatabase.CopyAsset(FarmerScene, BuildingScene))
                throw new InvalidOperationException("Could not create BuildingTest from FarmerTest.");
            Scene building = EditorSceneManager.OpenScene(BuildingScene, OpenSceneMode.Single);
            MigrateScene(building, buildings, cost, true);
            CreatePlots(building, buildings);
            Save(building);
            string warehousePath = PrefabRoot + "Warehouse.prefab";
            GameObject warehousePrefab = PrefabUtility.LoadPrefabContents(warehousePath);
            try { RemoveLegacy(warehousePrefab); PrefabUtility.SaveAsPrefabAsset(warehousePrefab, warehousePath); }
            finally { PrefabUtility.UnloadPrefabContents(warehousePrefab); }
            Directory.CreateDirectory(".harness-runs/building-construction-20260927/assembly");
            File.WriteAllText(".harness-runs/building-construction-20260927/assembly/setup-result.json",
                "{\"success\":true,\"scene\":\"Assets/Scenes/BuildingTest.unity\",\"playMode\":\"NOT_VERIFIED\"}");
        }
        finally
        {
            // All original scenes were clean and saved. Only assigned scenes were saved;
            // restoring the setup neither saves unrelated scenes nor changes their lights.
            EditorSceneManager.RestoreSceneManagerSetup(originalSetup);
        }
    }

    private static void WriteCsv()
    {
        foreach (string path in new[] { "Assets/Data/CSV/ItemData.csv", "Assets/TestOnly/DecisionSmokeItemData.csv" })
        {
            var lines = File.ReadAllLines(path).Where(line => !string.IsNullOrWhiteSpace(line)).ToList();
            if (lines[0].Split(',').Length == 9)
            {
                lines[0] += ",sellPrice";
                for (int i = 1; i < lines.Count; ++i) lines[i] += ",0";
            }
            if (!lines[0].Contains("usesStorage"))
            {
                lines[0] += ",usesStorage,showInWarehouse";
                for (int i = 1; i < lines.Count; ++i) lines[i] += ",true,true";
            }
            if (lines.Any(line => line.Split(',').Length != 12))
                throw new InvalidOperationException("Expected exactly 12 item columns: " + path);
            if (path.Contains("/CSV/"))
            {
                if (!lines.Any(line => line.StartsWith("8,"))) lines.Add("8,Resource,Gold,Common currency,0,0,0,0,0,0,false,false");
                if (!lines.Any(line => line.StartsWith("9,"))) lines.Add("9,Resource,Wood,Construction timber,0,0,0,0,0,0,true,true");
                if (!lines.Any(line => line.StartsWith("10,"))) lines.Add("10,Resource,Stone,Construction stone,0,0,0,0,0,0,true,true");
            }
            File.WriteAllLines(path, lines);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        }
        WriteCsvFile("BuildingData", "buildingId,buildingType,displayName,requiredWork,maxWorkers,providedCapacity\n" +
            "1,Warehouse,창고,100,2,500\n2,Pub,식당,100,2,0\n3,Inn,숙소,100,2,0\n4,GuardPost,초소,100,2,0\n5,Farm,농경지,100,2,0\n");
        var costs = new System.Text.StringBuilder("buildingId,itemId,quantity\n");
        for (int i = 1; i <= 5; ++i) costs.Append($"{i},8,100\n{i},9,20\n{i},10,10\n");
        WriteCsvFile("BuildingCost", costs.ToString());
        WriteCsvFile("BuilderWork", "workPerSecond,hungerPerSecond,thirstPerSecond,fatiguePerSecond\n10,0.3,0.3,0.3\n");
    }
    private static void WriteCsvFile(string name, string text)
    {
        string path = "Assets/Data/CSV/" + name + ".csv";
        File.WriteAllText(path, text);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
    }
    private static void ImportArt(string name, float ppu)
    {
        string path = "Assets/Art/Generated/Buildings/Construction/" + name + ".png";
        if (!File.Exists(path)) throw new InvalidOperationException("Assembly must promote reviewed art before setup: " + path);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = ppu;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.filterMode = FilterMode.Bilinear;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.maxTextureSize = 2048;
        importer.textureCompression = TextureImporterCompression.Compressed;
        importer.compressionQuality = 50;
        importer.sRGBTexture = true;
        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteAlignment = (int)SpriteAlignment.BottomCenter;
        settings.spritePivot = new Vector2(0.5f, 0f);
        settings.spriteMeshType = SpriteMeshType.FullRect;
        importer.SetTextureSettings(settings);
        importer.SaveAndReimport();
    }
    private static void EnsureGuardPrefab(Scene scene)
    {
        string path = PrefabRoot + "GuardPost.prefab";
        if (File.Exists(path)) return;
        GuardPost source = FindOne<GuardPost>(scene);
        GameObject copy = Object.Instantiate(source.gameObject);
        try
        {
            copy.name = "GuardPost";
            copy.transform.position = Vector3.zero;
            Set(copy.GetComponent<GuardPost>(), "_patrolArea", null);
            PrefabUtility.SaveAsPrefabAsset(copy, path);
        }
        finally { Object.DestroyImmediate(copy); }
    }
    private static void ConfigureFacilityPrefabs(int clickLayer)
    {
        foreach (string name in FacilityNames)
        {
            string path = PrefabRoot + name + ".prefab";
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                if (name != "Warehouse") RemoveLegacy(root);
                var adapter = GetOrAdd<CompletedBuildingFacility>(root);
                BaseInteractionProvider provider = root.GetComponentsInChildren<BaseInteractionProvider>(true).FirstOrDefault();
                if (name == "Warehouse") provider = GetOrAdd<WarehouseDepositPoint>(root);
                Transform entrance = root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "EntrancePoint");
                if (!entrance) entrance = Child(root.transform, "EntrancePoint", Vector3.zero);
                if (name == "Soil" || name == "GuardPost")
                {
                    // These synthetic construction anchors have no existing scene navigation
                    // consumers. Place the whole internal visual above the exterior work area.
                    float visualBottom = root.GetComponentsInChildren<SpriteRenderer>(true)
                        .Where(renderer => renderer.sprite).Min(renderer => renderer.bounds.min.y);
                    Vector3 local = entrance.localPosition;
                    local.y = root.transform.InverseTransformPoint(new Vector3(0, visualBottom, 0)).y - 1.5f;
                    entrance.localPosition = local;
                }
                Set(adapter, "_entrance", entrance);
                Set(adapter, "_provider", provider);
                Set(adapter, "_warehouse", root.GetComponentInChildren<WarehouseDepositPoint>(true));
                Set(adapter, "_farm", root.GetComponentInChildren<FarmWorkSite>(true));
                Set(adapter, "_seedSource", root.GetComponentInChildren<FarmSeedSource>(true));
                Set(adapter, "_guardPost", root.GetComponentInChildren<GuardPost>(true));
                Set(adapter, "_pub", root.GetComponentInChildren<Pub>(true));
                if (name == "Soil")
                {
                    FarmWorkSite farm = root.GetComponentInChildren<FarmWorkSite>(true);
                    // Existing scenes inherit their farm area and starting definition. The
                    // runtime factory overrides both only on a newly completed farm instance.
                    Collider2D clickCollider = farm.GetComponent<Collider2D>();
                    if (!clickCollider)
                        throw new InvalidOperationException("Soil requires its existing farm click collider.");
                    clickCollider.gameObject.layer = clickLayer;
                    FarmSeedSource seedSource = GetOrAdd<FarmSeedSource>(clickCollider.gameObject);
                    Set(seedSource, "_farm", farm);
                    Set(seedSource, "_cropCatalog", AssetDatabase.LoadAssetAtPath<CropCatalog>(DataRoot + "CropCatalog.asset"));
                    Set(seedSource, "_itemDataContext", AssetDatabase.LoadAssetAtPath<ItemDataContext>(DataRoot + "ItemDataContext.asset"));
                    Set(adapter, "_seedSource", seedSource);
                    SeededRandomSource[] randoms = root.GetComponentsInChildren<SeededRandomSource>(true);
                    if (randoms.Length < 2)
                    {
                        var positionRandom = Child(root.transform, "WorkPositionRandom", Vector3.zero).gameObject.AddComponent<SeededRandomSource>();
                        SetInt(positionRandom, "_seed", 270927);
                        Set(farm, "_workPositionRandomSource", positionRandom);
                    }
                }
                if (name == "Warehouse")
                {
                    root.layer = clickLayer;
                    if (!root.GetComponent<Collider2D>()) root.AddComponent<BoxCollider2D>();
                }
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
    }
    private static BuildingDataContext CreateDefinitions()
    {
        var context = LoadOrCreate<BuildingDataContext>(DataRoot + "BuildingDataContext.asset");
        Set(context, "_buildingData", AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Data/CSV/BuildingData.csv"));
        Set(context, "_buildingCosts", AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Data/CSV/BuildingCost.csv"));
        Set(context, "_builderWork", AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Data/CSV/BuilderWork.csv"));
        Set(context, "_items", AssetDatabase.LoadAssetAtPath<ItemDataContext>(DataRoot + "ItemDataContext.asset"));
        var serialized = new SerializedObject(context);
        SerializedProperty assets = serialized.FindProperty("_assets");
        assets.arraySize = FacilityNames.Length;
        for (int i = 0; i < FacilityNames.Length; ++i)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabRoot + FacilityNames[i] + ".prefab");
            SerializedProperty entry = assets.GetArrayElementAtIndex(i);
            entry.FindPropertyRelative("_buildingId").intValue = i + 1;
            entry.FindPropertyRelative("_prefab").objectReferenceValue = prefab.GetComponent<CompletedBuildingFacility>();
            entry.FindPropertyRelative("_icon").objectReferenceValue = prefab.GetComponentsInChildren<SpriteRenderer>(true)
                .Where(renderer => renderer.sprite).OrderByDescending(renderer => renderer.bounds.size.sqrMagnitude).First().sprite;
        }
        serialized.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.SaveAssetIfDirty(context);
        return context;
    }

    private static void MigrateScene(Scene scene, BuildingDataContext buildings, BuildActionCost cost, bool constructionTest)
    {
        ReconcileFarmSources(scene);
        DataManager data = FindOne<DataManager>(scene);
        ResourceManager resources = All<ResourceManager>(scene).SingleOrDefault();
        if (!resources)
        {
            Transform parent = data.transform.parent;
            resources = Child(parent, "ResourceManager", Vector3.zero).gameObject.AddComponent<ResourceManager>();
        }
        Set(resources, "_itemDataContext", AssetDatabase.LoadAssetAtPath<ItemDataContext>(DataRoot + "ItemDataContext.asset"));
        PreserveLegacyWarehouseCapacity(scene, resources);
        Set(data, "_buildingDataContext", buildings);
        var dataSo = new SerializedObject(data);
        SerializedProperty costs = dataSo.FindProperty("_costInfos");
        bool hasCost = false;
        for (int i = 0; i < costs.arraySize; ++i)
            hasCost |= costs.GetArrayElementAtIndex(i).FindPropertyRelative("actionCost").objectReferenceValue == cost;
        if (!hasCost) { int i = costs.arraySize++; costs.GetArrayElementAtIndex(i).FindPropertyRelative("actionCost").objectReferenceValue = cost; }
        dataSo.ApplyModifiedPropertiesWithoutUndo();
        var initialStock = new Dictionary<int, int>();
        foreach (MonoBehaviour component in All<MonoBehaviour>(scene))
        {
            if (!component) continue;
            string type = component.GetType().Name;
            if (type == "GoldManager")
                initialStock[ResourceManager.GoldItemId] = new SerializedObject(component).FindProperty("_initialGold").intValue;
            if (type == "WarehouseInventory")
            {
                SerializedProperty stock = new SerializedObject(component).FindProperty("_initialStock");
                for (int i = 0; i < stock.arraySize; ++i)
                {
                    var entry = stock.GetArrayElementAtIndex(i);
                    int id = entry.FindPropertyRelative("_itemId").intValue;
                    initialStock.TryGetValue(id, out int prior);
                    initialStock[id] = prior + entry.FindPropertyRelative("_quantity").intValue;
                }
            }
            var serialized = new SerializedObject(component);
            SerializedProperty iterator = serialized.GetIterator();
            while (iterator.NextVisible(true))
            {
                if (iterator.propertyType == SerializedPropertyType.ObjectReference && iterator.type.Contains("ResourceManager"))
                    iterator.objectReferenceValue = resources;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.RecordPrefabInstancePropertyModifications(component);
        }
        foreach (GameObject root in scene.GetRootGameObjects()) RemoveLegacy(root);
        foreach (BuilderActionSelector selector in All<BuilderActionSelector>(scene)) Set(selector, "_buildCost", cost);
        Transform testRoot = FindTransform(scene, "TestOnly!!");
        BuildingTestBootstrap bootstrap = All<BuildingTestBootstrap>(scene).SingleOrDefault();
        bool hasBootstrap = bootstrap;
        if (!bootstrap) bootstrap = Child(testRoot, "BuildingTestBootstrap", Vector3.zero).gameObject.AddComponent<BuildingTestBootstrap>();
        Set(bootstrap, "_resources", resources);
        Set(bootstrap, "_buildings", buildings);
        Set(bootstrap, "_npcManager", All<NPCManager>(scene).FirstOrDefault());
        SetInt(bootstrap, "_builderCount", constructionTest ? 2 : 0);
        WarehouseDepositPoint[] warehouses = All<WarehouseDepositPoint>(scene).ToArray();
        SetArray(bootstrap, "_initialWarehouses", warehouses);
        if (constructionTest && !ReadObject(bootstrap, "_plots"))
            initialStock = new Dictionary<int, int> { { 8, 1000 }, { 9, 200 }, { 10, 200 }, { 6, 5 }, { 7, 5 } };
        else if (!hasBootstrap && scene.path == FarmerScene && initialStock.Count == 0)
            initialStock = new Dictionary<int, int> { { 6, 5 }, { 7, 5 } };
        if (initialStock.Count > 0)
        {
            var serialized = new SerializedObject(bootstrap);
            SerializedProperty stock = serialized.FindProperty("_initialStock");
            stock.arraySize = initialStock.Count;
            int index = 0;
            foreach (var entry in initialStock)
            {
                var item = stock.GetArrayElementAtIndex(index++);
                item.FindPropertyRelative("_itemId").intValue = entry.Key;
                item.FindPropertyRelative("_quantity").intValue = entry.Value;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
        AddScenePopups(scene);
    }

    private static void PreserveLegacyWarehouseCapacity(Scene scene, ResourceManager resources)
    {
        foreach (MonoBehaviour component in All<MonoBehaviour>(scene).ToArray())
        {
            if (!component || component.GetType().Name != "WarehouseInventory") continue;
            // Native GuardTest Storage had only inventory and a visual. Keep its owner,
            // transform and existing destination configuration; do not add a click target.
            WarehouseDepositPoint point = GetOrAdd<WarehouseDepositPoint>(component.gameObject);
            Set(point, "_inventorySource", resources);
            PrefabUtility.RecordPrefabInstancePropertyModifications(point);
        }
    }

    private static void ReconcileFarmSources(Scene scene)
    {
        foreach (FarmWorkSite farm in All<FarmWorkSite>(scene).ToArray())
        {
            FarmSeedSource[] sources = All<FarmSeedSource>(scene)
                .Where(source => ReadObject(source, "_farm") == farm).ToArray();
            FarmWorkSite prefabFarm = PrefabUtility.GetCorrespondingObjectFromSource(farm);
            bool isSoilInstance = prefabFarm && AssetDatabase.GetAssetPath(prefabFarm) == PrefabRoot + "Soil.prefab";
            if (!isSoilInstance)
            {
                // Existing scene-native farms are providers, not necessarily player seed
                // selection targets. Preserve that scope instead of adding new gameplay.
                if (sources.Length > 1 || (sources.Length == 1 && !sources[0].GetComponent<Collider2D>()))
                    throw new InvalidOperationException("Native farm has ambiguous or unclickable seed sources: " + farm.name);
                continue;
            }
            FarmSeedSource canonical = sources.FirstOrDefault(source =>
                source.GetComponent<Collider2D>() && PrefabUtility.GetCorrespondingObjectFromSource(source));
            if (!canonical)
                throw new InvalidOperationException("Farm prefab source must share its click collider: " + farm.name);
            foreach (FarmSeedSource duplicate in sources)
            {
                if (duplicate == canonical) continue;
                if (PrefabUtility.GetCorrespondingObjectFromSource(duplicate))
                    throw new InvalidOperationException("Unexpected duplicate inherited FarmSeedSource: " + farm.name);
                // Keep scene-specific catalog bindings, then redirect every local serialized
                // consumer before removing the old scene-added component override.
                foreach (string field in new[] { "_cropCatalog", "_itemDataContext" })
                {
                    Object value = ReadObject(duplicate, field);
                    if (value) Set(canonical, field, value);
                }
                foreach (MonoBehaviour consumer in All<MonoBehaviour>(scene).ToArray())
                {
                    if (!consumer || consumer == duplicate) continue;
                    var serialized = new SerializedObject(consumer);
                    var property = serialized.GetIterator();
                    bool changed = false;
                    while (property.NextVisible(true))
                    {
                        if (property.propertyType != SerializedPropertyType.ObjectReference
                            || property.objectReferenceValue != duplicate) continue;
                        property.objectReferenceValue = canonical;
                        changed = true;
                    }
                    if (!changed) continue;
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                    PrefabUtility.RecordPrefabInstancePropertyModifications(consumer);
                }
                Object.DestroyImmediate(duplicate);
            }
            CompletedBuildingFacility adapter = farm.GetComponentInParent<CompletedBuildingFacility>();
            if (adapter) Set(adapter, "_seedSource", canonical);
            PrefabUtility.RecordPrefabInstancePropertyModifications(canonical);
        }
    }

    private static void CreatePlots(Scene scene, BuildingDataContext buildings)
    {
        if (scene.path != BuildingScene) throw new InvalidOperationException("Plots belong only to BuildingTest.");
        if (All<BuildingPlot>(scene).Any()) return;
        DataManager data = FindOne<DataManager>(scene);
        ResourceManager resources = FindOne<ResourceManager>(scene);
        DestinationDB destinations = FindOne<DestinationDB>(scene);
        InteractableManager interactions = FindOne<InteractableManager>(scene);
        BuildingPlotRegistry registry = Child(data.transform.parent, "BuildingPlotRegistry", Vector3.zero).gameObject.AddComponent<BuildingPlotRegistry>();
        BuildingFactory factory = Child(data.transform.parent, "BuildingFactory", Vector3.zero).gameObject.AddComponent<BuildingFactory>();
        Set(factory, "_buildingDataContext", buildings);
        Set(factory, "_resourceManager", resources);
        Set(factory, "_destinationDB", destinations);
        Set(factory, "_interactableManager", interactions);
        Set(factory, "_itemDataContext", resources.ItemData);
        Set(factory, "_cropCatalog", AssetDatabase.LoadAssetAtPath<CropCatalog>(DataRoot + "CropCatalog.asset"));
        TilemapNavigation navigation = FindOne<TilemapNavigation>(scene);
        var nav = new SerializedObject(navigation);
        Tilemap tilemap = (Tilemap)nav.FindProperty("_tilemap").objectReferenceValue;
        TileNavigationProfile profile = (TileNavigationProfile)nav.FindProperty("_profile").objectReferenceValue;
        if (!profile.TryCreateLookup(out var tileCosts, out string reason)) throw new InvalidOperationException(reason);
        var reserved = new List<Rect>();
        SerializedProperty areas = nav.FindProperty("_localAreas");
        SerializedProperty blocked = nav.FindProperty("_blockedAreas");
        for (int i = 0; i < areas.arraySize; ++i) reserved.Add(Rectangle((BoxCollider2D)areas.GetArrayElementAtIndex(i).FindPropertyRelative("_bounds").objectReferenceValue));
        for (int i = 0; i < blocked.arraySize; ++i) reserved.Add(Rectangle((BoxCollider2D)blocked.GetArrayElementAtIndex(i).objectReferenceValue));
        // Fixed test-scene composition, measured against the original 1-unit terrain.
        // The appended strip preserves every original coordinate, tile and facility.
        TileBase extensionTile = tilemap.GetTile(new Vector3Int(30, 0, 0));
        if (!extensionTile || !tileCosts.TryGetValue(extensionTile, out int extensionCost) || extensionCost <= 0
            || tilemap.GetCellCenterWorld(new Vector3Int(30, 0, 0)) != new Vector3(30.5f, 0.5f, 0))
            throw new InvalidOperationException("BuildingTest extension requires its measured unit grid and walkable edge tile.");
        for (int x = 31; x < 64; ++x)
        for (int y = -12; y < 8; ++y)
            if (tilemap.HasTile(new Vector3Int(x, y, 0)))
                throw new InvalidOperationException("BuildingTest extension would overwrite an existing tile.");
        var positions = new List<Vector3>
        {
            new Vector3(36.5f, -10.5f), new Vector3(47.5f, -10.5f), new Vector3(58.5f, -10.5f),
            new Vector3(36.5f, -0.5f), new Vector3(47.5f, -0.5f), new Vector3(58.5f, -0.5f)
        };
        foreach (Vector3 center in positions)
        {
            Rect whole = new Rect(center.x - 5.5f, center.y - 1.5f, 11f, 10f);
            if (reserved.Any(rect => rect.Overlaps(whole)))
                throw new InvalidOperationException("Fixed BuildingTest plot overlaps an existing reserved region.");
            reserved.Add(whole);
        }
        for (int x = 31; x < 64; ++x)
        for (int y = -12; y < 8; ++y)
            tilemap.SetTile(new Vector3Int(x, y, 0), extensionTile);
        var camera = new SerializedObject(FindOne<TestCameraArrowMove>(scene));
        Vector2 maximum = camera.FindProperty("_maxBounds").vector2Value;
        maximum.x = 64f;
        camera.FindProperty("_maxBounds").vector2Value = maximum;
        camera.ApplyModifiedPropertiesWithoutUndo();
        Transform map = FindTransform(scene, "Map");
        var plots = new List<BuildingPlot>();
        foreach (Vector3 position in positions)
        {
            GameObject go = Child(map, "BuildingPlot" + (plots.Count + 1), Vector3.zero).gameObject;
            go.transform.position = position;
            go.layer = All<FarmSeedSource>(scene).First().gameObject.layer;
            var footprint = go.AddComponent<BoxCollider2D>();
            footprint.isTrigger = true; footprint.size = new Vector2(9, 6); footprint.offset = new Vector2(0, 4.5f);
            var plot = go.AddComponent<BuildingPlot>();
            Transform entrance = Child(go.transform, "EntrancePoint", Vector3.zero);
            var workArea = Child(go.transform, "FacilityWorkArea", new Vector3(0, 0.5f)).gameObject.AddComponent<BoxCollider2D>();
            workArea.isTrigger = true; workArea.size = new Vector2(9, 2);
            Transform navigationEntrance = Child(go.transform, "NavigationEntrance", Vector3.zero);
            Transform left = Child(go.transform, "WorkPointLeft", new Vector3(-2, 0.5f));
            Transform right = Child(go.transform, "WorkPointRight", new Vector3(2, 0.5f));
            Set(plot, "_dataManager", data); Set(plot, "_resources", resources); Set(plot, "_factory", factory);
            Set(plot, "_registry", registry); Set(plot, "_entrance", entrance); Set(plot, "_workArea", workArea);
            SetArray(plot, "_workPositions", new[] { left, right });
            Set(plot, "_materialsVisual", Visual(go.transform, "Materials", "material-pile"));
            Set(plot, "_scaffoldingVisual", Visual(go.transform, "Scaffolding", "scaffolding"));
            GameObject marker = Child(go.transform, "EmptyPlot", new Vector3(0, 3f)).gameObject;
            var markerRenderer = marker.AddComponent<SpriteRenderer>();
            markerRenderer.sprite = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabRoot + "Soil.prefab")
                .GetComponentsInChildren<SpriteRenderer>(true).First(renderer => renderer.sprite).sprite;
            markerRenderer.sortingOrder = 1;
            Set(plot, "_emptyVisual", marker);
            int b = blocked.arraySize++; blocked.GetArrayElementAtIndex(b).objectReferenceValue = footprint;
            int a = areas.arraySize++; var area = areas.GetArrayElementAtIndex(a);
            area.FindPropertyRelative("_bounds").objectReferenceValue = workArea;
            area.FindPropertyRelative("_entrance").objectReferenceValue = navigationEntrance;
            plots.Add(plot);
        }
        nav.ApplyModifiedPropertiesWithoutUndo();
        SetArray(registry, "_plots", plots.ToArray());
        foreach (BuilderActionSelector selector in All<BuilderActionSelector>(scene)) Set(selector, "_buildingPlots", registry);
        Set(FindOne<BuildingTestBootstrap>(scene), "_plots", registry);
        if (!navigation.IsReady) throw new InvalidOperationException("BuildingTest navigation snapshot: " + navigation.ConfigurationError);
        Vector3 routeStart = ((Transform)areas.GetArrayElementAtIndex(0).FindPropertyRelative("_entrance").objectReferenceValue).position;
        var route = new List<Vector3>();
        for (int i = 0; i < areas.arraySize; ++i)
        {
            Vector3 target = ((Transform)areas.GetArrayElementAtIndex(i).FindPropertyRelative("_entrance").objectReferenceValue).position;
            if (!navigation.TryBuildPath(routeStart, target, route, out var failure))
                throw new InvalidOperationException("BuildingTest area entrance is unreachable: " + failure);
        }
        foreach (BuildingPlot plot in plots)
        {
            var workers = new SerializedObject(plot).FindProperty("_workPositions");
            for (int i = 0; i < workers.arraySize; ++i)
                if (!navigation.TryBuildPath(routeStart, ((Transform)workers.GetArrayElementAtIndex(i).objectReferenceValue).position, route, out var failure))
                    throw new InvalidOperationException("BuildingTest worker point is unreachable: " + failure);
        }
    }

    private static GameObject Visual(Transform parent, string name, string art)
    {
        GameObject go = Child(parent, name, new Vector3(0, 1.5f)).gameObject;
        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Generated/Buildings/Construction/" + art + ".png");
        renderer.sortingOrder = 5;
        go.SetActive(false);
        return go;
    }
    private static Rect Rectangle(BoxCollider2D box)
    {
        Vector3 center = box.transform.TransformPoint(box.offset);
        Vector2 size = Vector2.Scale(box.size, box.transform.lossyScale);
        return new Rect((Vector2)center - size * 0.5f, size);
    }

    private static void CreateUiPrefabs(BuildingDataContext buildings)
    {
        GameObject reference = AssetDatabase.LoadAssetAtPath<GameObject>(UiRoot + "SeedSelectionPopup.prefab");
        Font font = reference.GetComponentsInChildren<Text>(true).First(text => text.font).font;
        Sprite panel = reference.GetComponentsInChildren<Image>(true).First(image => image.sprite).sprite;
        if (!HasUiPrefab<ConstructionChoiceRow>("ConstructionChoiceRow", "_button", "_label", "_icon"))
        {
            GameObject choice = RectObject("ConstructionChoiceRow", null, new Vector2(270, 70));
            try
            {
                var choiceView = choice.AddComponent<ConstructionChoiceRow>();
                var choiceButton = choice.AddComponent<Button>();
                Image choiceImage = choice.AddComponent<Image>(); choiceImage.sprite = panel; choiceImage.type = Image.Type.Sliced;
                Set(choiceView, "_button", choiceButton);
                Set(choiceView, "_label", Label(choice.transform, font, "시설", new Vector2(35, 0), new Vector2(185, 60)));
                Image icon = RectObject("Icon", choice.transform, new Vector2(60, 60), new Vector2(-100, 0)).AddComponent<Image>();
                Set(choiceView, "_icon", icon);
                PrefabUtility.SaveAsPrefabAsset(choice, UiRoot + "ConstructionChoiceRow.prefab");
            }
            finally { Object.DestroyImmediate(choice); }
        }
        if (!HasUiPrefab<ResourceQuantityRow>("ResourceQuantityRow", "_label"))
        {
            GameObject quantity = RectObject("ResourceQuantityRow", null, new Vector2(570, 38));
            try
            {
                var quantityView = quantity.AddComponent<ResourceQuantityRow>();
                Set(quantityView, "_label", Label(quantity.transform, font, "자원", Vector2.zero, new Vector2(560, 38)));
                PrefabUtility.SaveAsPrefabAsset(quantity, UiRoot + "ResourceQuantityRow.prefab");
            }
            finally { Object.DestroyImmediate(quantity); }
        }
        foreach (bool construction in new[] { true, false })
        {
            string name = construction ? "ConstructionPopup" : "WarehousePopup";
            bool exists = construction
                ? HasUiPrefab<ConstructionPopup>(name, "_buildingDataContext", "_rowPrefab", "_rowsRoot", "_details", "_result", "_confirmButton", "_cancelButton", "_closeButton")
                : HasUiPrefab<WarehousePopup>(name, "_rowPrefab", "_rowsRoot", "_capacityText", "_closeButton");
            if (exists) continue;
            GameObject root = RectObject(name, null, new Vector2(700, 620));
            try
            {
                Image background = root.AddComponent<Image>(); background.sprite = panel; background.type = Image.Type.Sliced;
                PopBase view = construction ? (PopBase)root.AddComponent<ConstructionPopup>() : root.AddComponent<WarehousePopup>();
                SetInt(view, "_popupType", (int)(construction ? PopupType.Construction : PopupType.Warehouse));
                Set(view, "_closeButton", Button(root.transform, font, "닫기", new Vector2(255, 265)));
                Transform rows = RectObject("Rows", root.transform, construction ? new Vector2(280, 420) : new Vector2(580, 460),
                    construction ? new Vector2(-170, 15) : new Vector2(0, -10)).transform;
                var layout = rows.gameObject.AddComponent<VerticalLayoutGroup>(); layout.childControlHeight = false;
                layout.childControlWidth = false; layout.childForceExpandHeight = false; layout.childForceExpandWidth = false; layout.spacing = 8;
                Set(view, "_rowsRoot", rows);
                if (construction)
                {
                    Set(view, "_buildingDataContext", buildings);
                    Set(view, "_rowPrefab", AssetDatabase.LoadAssetAtPath<GameObject>(UiRoot + "ConstructionChoiceRow.prefab").GetComponent<ConstructionChoiceRow>());
                    Set(view, "_details", Label(root.transform, font, "시설 선택", new Vector2(155, 40), new Vector2(290, 380)));
                    Set(view, "_result", Label(root.transform, font, "", new Vector2(0, -220), new Vector2(650, 50)));
                    Set(view, "_confirmButton", Button(root.transform, font, "건설 확정", new Vector2(-115, -275)));
                    Set(view, "_cancelButton", Button(root.transform, font, "공사 취소", new Vector2(115, -275)));
                }
                else
                {
                    Set(view, "_rowPrefab", AssetDatabase.LoadAssetAtPath<GameObject>(UiRoot + "ResourceQuantityRow.prefab").GetComponent<ResourceQuantityRow>());
                    Set(view, "_capacityText", Label(root.transform, font, "공용 창고", new Vector2(-50, 265), new Vector2(420, 50)));
                }
                root.SetActive(false);
                PrefabUtility.SaveAsPrefabAsset(root, UiRoot + name + ".prefab");
            }
            finally { Object.DestroyImmediate(root); }
        }
    }
    private static bool HasUiPrefab<T>(string name, params string[] requiredReferences) where T : Component
    {
        string path = UiRoot + name + ".prefab";
        if (!File.Exists(path)) return false;
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        T view = prefab ? prefab.GetComponent<T>() : null;
        if (!view) throw new InvalidOperationException("Existing UI prefab has the wrong component: " + path);
        foreach (string field in requiredReferences)
            if (!ReadObject(view, field))
                throw new InvalidOperationException("Existing UI prefab has a missing " + field + ": " + path);
        if (view is PopBase popup)
        {
            PopupType expected = typeof(T) == typeof(ConstructionPopup) ? PopupType.Construction : PopupType.Warehouse;
            if (popup.PopupType != expected)
                throw new InvalidOperationException("Existing UI prefab has the wrong popup type: " + path);
        }
        return true;
    }
    private static void AddScenePopups(Scene scene)
    {
        UIManager manager = All<UIManager>(scene).SingleOrDefault();
        if (!manager) return;
        Transform popupRoot = All<Transform>(scene).FirstOrDefault(transform => transform.name == "PopupUI");
        if (!popupRoot) popupRoot = RectObject("PopupUI", FindTransform(scene, "Canvas"), Vector2.zero).transform;
        foreach (string name in new[] { "ConstructionPopup", "WarehousePopup" })
        {
            if (popupRoot.Find(name)) continue;
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(UiRoot + name + ".prefab"), popupRoot);
            instance.name = name;
            var serialized = new SerializedObject(manager);
            var popups = serialized.FindProperty("_popups");
            int index = popups.arraySize++; popups.GetArrayElementAtIndex(index).objectReferenceValue = instance.GetComponent<PopBase>();
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
    private static GameObject RectObject(string name, Transform parent, Vector2 size, Vector2 position = default)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rect = (RectTransform)go.transform; rect.sizeDelta = size; rect.anchoredPosition = position;
        return go;
    }
    private static Text Label(Transform parent, Font font, string value, Vector2 position, Vector2 size)
    {
        var text = RectObject("Label", parent, size, position).AddComponent<Text>();
        text.font = font; text.text = value; text.fontSize = 21; text.color = Color.black;
        text.alignment = TextAnchor.MiddleLeft; text.raycastTarget = false;
        return text;
    }
    private static Button Button(Transform parent, Font font, string caption, Vector2 position)
    {
        var go = RectObject(caption, parent, new Vector2(170, 44), position);
        var image = go.AddComponent<Image>(); image.color = new Color(0.9f, 0.8f, 0.6f);
        var button = go.AddComponent<Button>(); button.targetGraphic = image;
        Label(go.transform, font, caption, Vector2.zero, new Vector2(150, 42)).alignment = TextAnchor.MiddleCenter;
        return button;
    }
    private static void RemoveLegacy(GameObject root)
    {
        foreach (MonoBehaviour component in root.GetComponentsInChildren<MonoBehaviour>(true))
            if (component && (component.GetType().Name == "GoldManager" || component.GetType().Name == "WarehouseInventory"))
                Object.DestroyImmediate(component);
    }
    private static void Save(Scene scene)
    {
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Failed to save " + scene.path);
    }
    private static IEnumerable<T> All<T>(Scene scene) where T : Component
        => scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<T>(true));
    private static T FindOne<T>(Scene scene) where T : Component => All<T>(scene).Single();
    private static Transform FindTransform(Scene scene, string name)
        => All<Transform>(scene).First(transform => transform.name == name);
    private static Transform Child(Transform parent, string name, Vector3 position)
    {
        var go = new GameObject(name); go.transform.SetParent(parent, false); go.transform.localPosition = position; return go.transform;
    }
    private static T GetOrAdd<T>(GameObject go) where T : Component => go.GetComponent<T>() ? go.GetComponent<T>() : go.AddComponent<T>();
    private static T LoadOrCreate<T>(string path) where T : ScriptableObject
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset) return asset;
        asset = ScriptableObject.CreateInstance<T>(); AssetDatabase.CreateAsset(asset, path); return asset;
    }
    private static Object ReadObject(Object owner, string field) => new SerializedObject(owner).FindProperty(field).objectReferenceValue;
    private static void Set(Object owner, string field, Object value)
    {
        if (!owner) throw new InvalidOperationException("Missing owner for " + field);
        var serialized = new SerializedObject(owner); serialized.FindProperty(field).objectReferenceValue = value; serialized.ApplyModifiedPropertiesWithoutUndo();
    }
    private static void SetInt(Object owner, string field, int value)
    {
        var serialized = new SerializedObject(owner); serialized.FindProperty(field).intValue = value; serialized.ApplyModifiedPropertiesWithoutUndo();
    }
    private static void SetArray<T>(Object owner, string field, T[] values) where T : Object
    {
        var serialized = new SerializedObject(owner); var array = serialized.FindProperty(field); array.arraySize = values.Length;
        for (int i = 0; i < values.Length; ++i) array.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }
}
