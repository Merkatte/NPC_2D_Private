using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

// Explicit one-shot authoring entrypoint. Never runs on import or saves unrelated assets.
public static class BuilderCitizenSetup
{
    public const string ScenePath = "Assets/Scenes/FarmerTest.unity";
    public const string HammerPath = "Assets/Art/Generated/builder-hammer.png";
    public const string StatPath = "Assets/Data/ScriptableObject/BuilderStatContext.asset";
    public const string CostPath = "Assets/Data/ScriptableObject/WanderActionCost.asset";
    private const string WorkerPath = "Assets/Prefab/InGame/NPCGirl.prefab";
    private const string TownPath = "Assets/Prefab/InGame/TownHall.prefab";
    private const string PopupPath = "Assets/Prefab/UI/TownHallPopup.prefab";
    private const string EvidenceDirectory = ".harness-runs/builder-citizen-20260926";

    [MenuItem("Tools/NPC/Builder/Setup")]
    public static void Setup()
    {
        EnsureSafeEditor();
        Scene scene = SceneManager.GetSceneByPath(ScenePath);
        if (!scene.isLoaded)
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
        Sprite hammer = ImportHammer();
        DefaultStatContext stat = AssetDatabase.LoadAssetAtPath<DefaultStatContext>(StatPath);
        if (!stat)
        {
            if (!AssetDatabase.CopyAsset("Assets/Data/ScriptableObject/DefaultStatContext.asset", StatPath))
                throw new InvalidOperationException("Cannot create Builder stat definition.");
            stat = AssetDatabase.LoadAssetAtPath<DefaultStatContext>(StatPath);
        }
        SetString(stat, "_name", "건축가");
        AssetDatabase.SaveAssetIfDirty(stat);
        WanderActionCost cost = AssetDatabase.LoadAssetAtPath<WanderActionCost>(CostPath);
        if (!cost)
        {
            cost = ScriptableObject.CreateInstance<WanderActionCost>();
            AssetDatabase.CreateAsset(cost, CostPath);
        }
        AssetDatabase.SaveAssetIfDirty(cost);
        ConfigureWorker(hammer);
        ConfigureTown();
        ConfigurePopup(hammer);

        NPCManager manager = FindOne<NPCManager>(scene);
        FarmerActionSelector farmer = FindOne<FarmerActionSelector>(scene);
        BuilderActionSelector builder = FindOptional<BuilderActionSelector>(scene);
        if (!builder)
        {
            var go = new GameObject("BuilderActionSelector");
            SceneManager.MoveGameObjectToScene(go, scene);
            go.transform.SetParent(farmer.transform.parent, false);
            builder = go.AddComponent<BuilderActionSelector>();
        }
        SeededRandomSource random = builder.GetComponent<SeededRandomSource>();
        if (!random)
            random = builder.gameObject.AddComponent<SeededRandomSource>();
        var randomData = new SerializedObject(random);
        randomData.FindProperty("_seed").intValue = 260926;
        randomData.ApplyModifiedPropertiesWithoutUndo();
        var source = new SerializedObject(farmer);
        var selector = new SerializedObject(builder);
        foreach (string field in new[] { "dataManager", "actionPool", "_destinationDB", "_decisionTuning", "_navigation" })
            selector.FindProperty(field).objectReferenceValue = source.FindProperty(field).objectReferenceValue;
        selector.FindProperty("_wanderCost").objectReferenceValue = cost;
        selector.FindProperty("_randomSource").objectReferenceValue = random;
        selector.ApplyModifiedPropertiesWithoutUndo();

        var managerData = new SerializedObject(manager);
        SerializedProperty entry = FindOrAppendRole(managerData.FindProperty("_creationEntries"), "_npcType");
        entry.FindPropertyRelative("_selector").objectReferenceValue = builder;
        entry.FindPropertyRelative("_statDefinition").objectReferenceValue = stat;
        managerData.ApplyModifiedPropertiesWithoutUndo();
        PrefabUtility.RecordPrefabInstancePropertyModifications(manager);

        // Explicitly preserve scene-specific recruitment references and per-role settings.
        ConfigureRecruitment(FindOne<TownHallRecruitment>(scene));
        TownHallPopup popup = FindOne<TownHallPopup>(scene);
        var popupData = new SerializedObject(popup);
        SerializedProperty cards = popupData.FindProperty("_cards");
        TownHallRecruitCard[] actualCards = popup.GetComponentsInChildren<TownHallRecruitCard>(true);
        cards.arraySize = actualCards.Length;
        for (int i = 0; i < actualCards.Length; ++i)
            cards.GetArrayElementAtIndex(i).objectReferenceValue = actualCards[i];
        popupData.ApplyModifiedPropertiesWithoutUndo();
        PrefabUtility.RecordPrefabInstancePropertyModifications(popup);
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene))
            throw new InvalidOperationException("Could not save FarmerTest.");
        Directory.CreateDirectory(EvidenceDirectory);
        File.WriteAllText(EvidenceDirectory + "/setup-result.json", "{\"success\":true,\"scene\":\"" + ScenePath + "\"}");
        Debug.Log("Builder citizen setup saved only assigned assets and FarmerTest.");
    }

    public static void EnsureSafeEditor()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || PrefabStageUtility.GetCurrentPrefabStage() != null)
            throw new InvalidOperationException("Builder setup requires idle Edit Mode and no Prefab Stage.");
        for (int i = 0; i < SceneManager.sceneCount; ++i)
            if (SceneManager.GetSceneAt(i).isDirty)
                throw new InvalidOperationException("Save your scene before Builder setup; no unsaved work will be overwritten.");
        foreach (string path in new[] { WorkerPath, TownPath, PopupPath, HammerPath, StatPath, CostPath })
        {
            Object asset = AssetDatabase.LoadMainAssetAtPath(path);
            if (asset && EditorUtility.IsDirty(asset))
                throw new InvalidOperationException("Assigned asset has unsaved edits: " + path);
        }
    }

    private static Sprite ImportHammer()
    {
        AssetDatabase.ImportAsset(HammerPath, ImportAssetOptions.ForceSynchronousImport);
        var importer = (TextureImporter)AssetImporter.GetAtPath(HammerPath);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = 900f;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.sRGBTexture = true;
        importer.filterMode = FilterMode.Bilinear;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.textureCompression = TextureImporterCompression.Compressed;
        importer.maxTextureSize = 2048;
        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.Tight;
        importer.SetTextureSettings(settings);
#pragma warning disable CS0618
        importer.spritesheet = new[] { new SpriteMetaData {
            name = "builder-hammer_0", rect = new Rect(65f, 48f, 879f, 1432f),
            alignment = (int)SpriteAlignment.Custom, pivot = new Vector2(0.5085324f, 0.35f)
        } };
#pragma warning restore CS0618
        importer.SaveAndReimport();
        return AssetDatabase.LoadAllAssetsAtPath(HammerPath).OfType<Sprite>().Single();
    }

    private static void ConfigureWorker(Sprite hammer)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(WorkerPath);
        try
        {
            var component = new SerializedObject(root.GetComponent<NPCComponent>());
            var renderer = (SpriteRenderer)component.FindProperty("_toolRenderer").objectReferenceValue;
            SerializedProperty tools = component.FindProperty("_roleTools");
            tools.arraySize = 2;
            SetTool(tools.GetArrayElementAtIndex(0), NPCType.Farmer, renderer.sprite);
            SetTool(tools.GetArrayElementAtIndex(1), NPCType.Builder, hammer);
            component.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, WorkerPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    private static void SetTool(SerializedProperty row, NPCType role, Sprite sprite)
    {
        row.FindPropertyRelative("_role").intValue = (int)role;
        row.FindPropertyRelative("_sprite").objectReferenceValue = sprite;
    }

    private static void ConfigureTown()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(TownPath);
        try
        {
            ConfigureRecruitment(root.GetComponent<TownHallRecruitment>());
            PrefabUtility.SaveAsPrefabAsset(root, TownPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    private static void ConfigureRecruitment(TownHallRecruitment town)
    {
        var data = new SerializedObject(town);
        SerializedProperty row = FindOrAppendRole(data.FindProperty("_recruitments"), "_npcType");
        row.FindPropertyRelative("_cooldownDuration").floatValue = 60f;
        row.FindPropertyRelative("_settlementCost").intValue = 100;
        data.ApplyModifiedPropertiesWithoutUndo();
        PrefabUtility.RecordPrefabInstancePropertyModifications(town);
    }

    private static void ConfigurePopup(Sprite hammer)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(PopupPath);
        try
        {
            TownHallPopup popup = root.GetComponent<TownHallPopup>();
            TownHallRecruitCard[] cards = root.GetComponentsInChildren<TownHallRecruitCard>(true);
            TownHallRecruitCard builder = cards.FirstOrDefault(c => c.NpcType == NPCType.Builder);
            if (!builder)
            {
                TownHallRecruitCard template = cards.Single(c => c.NpcType == NPCType.Guard);
                builder = Object.Instantiate(template, template.transform.parent);
                builder.name = "BuilderCard";
            }
            var builderData = new SerializedObject(builder);
            builderData.FindProperty("_npcType").intValue = (int)NPCType.Builder;
            ((Image)builderData.FindProperty("_portrait").objectReferenceValue).sprite = hammer;
            ((Text)builderData.FindProperty("_nameText").objectReferenceValue).text = "건축가";
            builderData.ApplyModifiedPropertiesWithoutUndo();
            cards = root.GetComponentsInChildren<TownHallRecruitCard>(true).OrderBy(c => (int)c.NpcType).ToArray();
            var popupData = new SerializedObject(popup);
            SerializedProperty list = popupData.FindProperty("_cards");
            list.arraySize = cards.Length;
            for (int i = 0; i < cards.Length; ++i)
            {
                list.GetArrayElementAtIndex(i).objectReferenceValue = cards[i];
                var rect = (RectTransform)cards[i].transform;
                float widthRatio = 330f / rect.sizeDelta.x;
                rect.sizeDelta = new Vector2(330f, rect.sizeDelta.y);
                rect.anchoredPosition = new Vector2((i - 1) * 350f, 0f);
                foreach (RectTransform child in rect)
                {
                    child.sizeDelta = new Vector2(child.sizeDelta.x * widthRatio, child.sizeDelta.y);
                    child.anchoredPosition = new Vector2(child.anchoredPosition.x * widthRatio, child.anchoredPosition.y);
                }
            }
            popupData.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, PopupPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    private static SerializedProperty FindOrAppendRole(SerializedProperty rows, string roleField)
    {
        for (int i = 0; i < rows.arraySize; ++i)
            if (rows.GetArrayElementAtIndex(i).FindPropertyRelative(roleField).intValue == (int)NPCType.Builder)
                return rows.GetArrayElementAtIndex(i);
        int index = rows.arraySize++;
        SerializedProperty row = rows.GetArrayElementAtIndex(index);
        row.FindPropertyRelative(roleField).intValue = (int)NPCType.Builder;
        return row;
    }

    private static void SetString(Object target, string field, string value)
    {
        var data = new SerializedObject(target);
        data.FindProperty(field).stringValue = value;
        data.ApplyModifiedPropertiesWithoutUndo();
    }

    public static T FindOne<T>(Scene scene) where T : Component => scene.GetRootGameObjects()
        .SelectMany(root => root.GetComponentsInChildren<T>(true)).Single();

    private static T FindOptional<T>(Scene scene) where T : Component => scene.GetRootGameObjects()
        .SelectMany(root => root.GetComponentsInChildren<T>(true)).SingleOrDefault();
}
