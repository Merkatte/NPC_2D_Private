using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

// Bounded authoring entry point. Only the assembly worker invokes this method.
public static class HousingSetup
{
    private const string ScenePath = "Assets/Scenes/BuildingTest.unity";
    private const string DataRoot = "Assets/Data/ScriptableObject/";
    private const string HousePath = "Assets/Prefab/InGame/House.prefab";
    private const string PopupPath = "Assets/Prefab/UI/HousePopup.prefab";
    private const string RowPath = "Assets/Prefab/UI/HouseInfoRow.prefab";
    private const string ConstructionPath = "Assets/Prefab/UI/ConstructionPopup.prefab";

    [MenuItem("Tools/NPC/Housing/Setup BuildingTest")]
    public static void Setup()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling
            || EditorApplication.isUpdating || PrefabStageUtility.GetCurrentPrefabStage() != null)
            throw new InvalidOperationException("Housing setup requires an idle Editor outside Prefab Mode.");
        for (int i = 0; i < SceneManager.sceneCount; ++i)
            if (SceneManager.GetSceneAt(i).isDirty)
                throw new InvalidOperationException("Housing setup refuses unsaved scene changes.");
        BuildingDataContext buildings = Require<BuildingDataContext>(DataRoot + "BuildingDataContext.asset");
        GameObject construction = Require<GameObject>(ConstructionPath);
        Font font = construction.GetComponentsInChildren<Text>(true).First(text => text.font).font;
        Sprite[] sprites = AssetDatabase.LoadAllAssetsAtPath("Assets/Art/Generated/Buildings/House/house-tiers.png")
            .OfType<Sprite>().OrderBy(sprite => sprite.name).ToArray();
        for (int i = 0; i < 4; ++i)
            if (sprites.Length != 4 || sprites[i].name != "house-tier-" + (i + 1))
                throw new InvalidOperationException("Expected the four existing house-tier sprites.");
        TextAsset tiers = Require<TextAsset>("Assets/Data/CSV/HouseTierData.csv");
        TextAsset options = Require<TextAsset>("Assets/Data/CSV/HousingOption.csv");
        TextAsset tierOptions = Require<TextAsset>("Assets/Data/CSV/HouseTierOption.csv");
        TextAsset life = Require<TextAsset>("Assets/Data/CSV/HousingLife.csv");
        SceneSetup[] previous = EditorSceneManager.GetSceneManagerSetup();
        Scene scene = SceneManager.GetSceneByPath(ScenePath);
        bool opened = !scene.IsValid() || !scene.isLoaded;
        try
        {
            if (opened) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            NPCManager npc = One<NPCManager>(scene);
            BuildingFactory factory = One<BuildingFactory>(scene);
            TilemapNavigation navigation = One<TilemapNavigation>(scene);
            UIManager ui = One<UIManager>(scene);
            Transform popupRoot = All<Transform>(scene)
                .Single(transform => transform.name == "PopupUI");
            HousingDataContext data = AssetDatabase.LoadAssetAtPath<HousingDataContext>(DataRoot + "HousingDataContext.asset");
            if (!data)
            {
                data = ScriptableObject.CreateInstance<HousingDataContext>();
                AssetDatabase.CreateAsset(data, DataRoot + "HousingDataContext.asset");
            }
            Set(data, "_houseTiers", tiers); Set(data, "_housingOptions", options);
            Set(data, "_tierOptions", tierOptions); Set(data, "_lifeSettings", life); Set(data, "_buildings", buildings);
            CompletedBuildingFacility house = CreateHouse(sprites[0]);
            AppendBuildingAssets(buildings, house, sprites);
            AssetDatabase.SaveAssetIfDirty(buildings);
            AssetDatabase.SaveAssetIfDirty(data);
            HouseInfoRow row = CreateRow(font);
            HousePopup popup = CreatePopup(construction, font, data, row);
            AddConstructionScroll();

            HousingManager manager = All<HousingManager>(scene).SingleOrDefault();
            if (!manager)
            {
                GameObject go = new GameObject("HousingManager");
                SceneManager.MoveGameObjectToScene(go, scene);
                go.transform.SetParent(npc.transform.parent, false);
                manager = go.AddComponent<HousingManager>();
            }
            Set(manager, "_data", data); Set(manager, "_npcManager", npc); Set(manager, "_navigation", navigation);
            Set(npc, "_housingManager", manager);
            Set(factory, "_housingDataContext", data); Set(factory, "_housingManager", manager);
            HousePopup instance = popupRoot.GetComponentsInChildren<HousePopup>(true).SingleOrDefault();
            if (!instance)
                instance = ((GameObject)PrefabUtility.InstantiatePrefab(popup.gameObject, popupRoot)).GetComponent<HousePopup>();
            var serialized = new SerializedObject(ui);
            SerializedProperty popups = serialized.FindProperty("_popups");
            bool registered = false;
            for (int i = 0; i < popups.arraySize; ++i)
                registered |= popups.GetArrayElementAtIndex(i).objectReferenceValue == instance;
            if (!registered) popups.GetArrayElementAtIndex(popups.arraySize++).objectReferenceValue = instance;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Could not save BuildingTest.");
            Debug.Log("HousingSetup applied House, HousingDataContext, dynamic UI and BuildingTest housing references.");
        }
        finally
        {
            // Existing loaded scenes keep their in-memory identity. Only a scene
            // opened by this authoring call is closed/restored.
            if (opened && scene.IsValid() && scene.isLoaded && !scene.isDirty)
                EditorSceneManager.RestoreSceneManagerSetup(previous);
        }
    }

    private static CompletedBuildingFacility CreateHouse(Sprite sprite)
    {
        GameObject root = new GameObject("House");
        root.SetActive(false);
        try
        {
            root.layer = 7;
            BoxCollider2D collider = root.AddComponent<BoxCollider2D>();
            collider.isTrigger = true; collider.size = new Vector2(9f, 6f); collider.offset = new Vector2(0f, 4.5f);
            Transform entrance = Child(root.transform, "EntrancePoint").transform;
            GameObject visual = Child(root.transform, "Visual");
            visual.transform.localPosition = new Vector3(0f, 1.5f, 0f);
            visual.transform.localScale = new Vector3(1.9f, 1.9f, 1f);
            SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite; renderer.sortingOrder = 0;
            House house = root.AddComponent<House>();
            HousePopupSource source = root.AddComponent<HousePopupSource>();
            Set(source, "_house", house);
            CompletedBuildingFacility facility = root.AddComponent<CompletedBuildingFacility>();
            Set(facility, "_entrance", entrance); Set(facility, "_house", house);
            Set(facility, "_houseVisual", renderer); Set(facility, "_housePopupSource", source);
            return PrefabUtility.SaveAsPrefabAsset(root, HousePath).GetComponent<CompletedBuildingFacility>();
        }
        finally { Object.DestroyImmediate(root); }
    }

    private static void AppendBuildingAssets(BuildingDataContext buildings, CompletedBuildingFacility house, Sprite[] sprites)
    {
        var serialized = new SerializedObject(buildings);
        SerializedProperty entries = serialized.FindProperty("_assets");
        for (int tier = 1; tier <= sprites.Length; ++tier)
        {
            int id = tier + 5;
            SerializedProperty entry = null;
            for (int i = 0; i < entries.arraySize; ++i)
                if (entries.GetArrayElementAtIndex(i).FindPropertyRelative("_buildingId").intValue == id)
                    entry = entries.GetArrayElementAtIndex(i);
            if (entry == null) entry = entries.GetArrayElementAtIndex(entries.arraySize++);
            entry.FindPropertyRelative("_buildingId").intValue = id;
            entry.FindPropertyRelative("_prefab").objectReferenceValue = house;
            entry.FindPropertyRelative("_icon").objectReferenceValue = sprites[tier - 1];
        }
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static HouseInfoRow CreateRow(Font font)
    {
        GameObject root = Rect("HouseInfoRow", null, new Vector2(640f, 42f));
        try
        {
            var row = root.AddComponent<HouseInfoRow>();
            var layout = root.AddComponent<LayoutElement>(); layout.preferredHeight = 42f;
            Text label = Label(root.transform, font, string.Empty, new Vector2(630f, 42f), Vector2.zero);
            Set(row, "_label", label);
            return PrefabUtility.SaveAsPrefabAsset(root, RowPath).GetComponent<HouseInfoRow>();
        }
        finally { Object.DestroyImmediate(root); }
    }

    private static HousePopup CreatePopup(GameObject reference, Font font, HousingDataContext data, HouseInfoRow row)
    {
        GameObject root = Rect("HousePopup", null, new Vector2(760f, 660f));
        root.SetActive(false);
        try
        {
            CopyImage(root.AddComponent<Image>(), reference.GetComponent<Image>());
            Transform bodyReference = reference.transform.Find("BodyBackground");
            GameObject body = Rect("BodyBackground", root.transform, new Vector2(710f, 550f), new Vector2(0f, -20f));
            CopyImage(body.AddComponent<Image>(), bodyReference ? bodyReference.GetComponent<Image>() : reference.GetComponent<Image>());
            body.GetComponent<Image>().raycastTarget = false;
            HousePopup view = root.AddComponent<HousePopup>();
            SetInt(view, "_popupType", (int)PopupType.House);
            Set(view, "_housingDataContext", data); Set(view, "_rowPrefab", row);
            Set(view, "_title", Label(root.transform, font, "주택", new Vector2(610f, 50f), new Vector2(-25f, 280f)));
            Button close = Button(root.transform, font, "X", new Vector2(56f, 56f), new Vector2(0f, 0f));
            RectTransform closeRect = (RectTransform)close.transform;
            closeRect.anchorMin = closeRect.anchorMax = closeRect.pivot = Vector2.one;
            closeRect.anchoredPosition = new Vector2(-14f, -14f);
            Set(view, "_closeButton", close);
            ScrollRect scroll = Scroll(root.transform, "Details", new Vector2(660f, 410f), new Vector2(0f, 10f), out var content);
            Set(view, "_scrollRect", scroll); Set(view, "_rowsRoot", content);
            Set(view, "_result", Label(root.transform, font, string.Empty, new Vector2(660f, 42f), new Vector2(0f, -225f)));
            Set(view, "_upgradeButton", Button(root.transform, font, "업그레이드", new Vector2(180f, 46f), new Vector2(-210f, -280f)));
            Set(view, "_cancelButton", Button(root.transform, font, "공사 취소", new Vector2(180f, 46f), new Vector2(0f, -280f)));
            Set(view, "_retryButton", Button(root.transform, font, "완공 재시도", new Vector2(180f, 46f), new Vector2(210f, -280f)));
            return PrefabUtility.SaveAsPrefabAsset(root, PopupPath).GetComponent<HousePopup>();
        }
        finally { Object.DestroyImmediate(root); }
    }

    private static void AddConstructionScroll()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(ConstructionPath);
        try
        {
            ConstructionPopup view = root.GetComponent<ConstructionPopup>();
            var serialized = new SerializedObject(view);
            if (serialized.FindProperty("_choiceScrollRect").objectReferenceValue) return;
            RectTransform rows = (RectTransform)serialized.FindProperty("_rowsRoot").objectReferenceValue;
            ScrollRect scroll = Scroll(rows.parent, "ConstructionChoices", rows.sizeDelta, rows.anchoredPosition, out var content);
            Object.DestroyImmediate(content.gameObject);
            rows.SetParent(scroll.viewport, false);
            ConfigureContent(rows);
            scroll.content = rows;
            Set(view, "_choiceScrollRect", scroll);
            PrefabUtility.SaveAsPrefabAsset(root, ConstructionPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    private static ScrollRect Scroll(Transform parent, string name, Vector2 size, Vector2 position, out RectTransform content)
    {
        GameObject root = Rect(name, parent, size, position);
        ScrollRect scroll = root.AddComponent<ScrollRect>();
        scroll.horizontal = false; scroll.vertical = true; scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 30f;
        GameObject viewport = Rect("Viewport", root.transform, size);
        Image hitArea = viewport.AddComponent<Image>(); hitArea.color = new Color(1f, 1f, 1f, 0.001f);
        viewport.AddComponent<RectMask2D>();
        scroll.viewport = (RectTransform)viewport.transform;
        content = (RectTransform)Rect("Rows", viewport.transform, new Vector2(size.x, 0f)).transform;
        ConfigureContent(content);
        scroll.content = content;
        return scroll;
    }

    private static void ConfigureContent(RectTransform content)
    {
        content.anchorMin = new Vector2(0f, 1f); content.anchorMax = Vector2.one;
        content.pivot = new Vector2(0.5f, 1f); content.anchoredPosition = Vector2.zero;
        content.sizeDelta = Vector2.zero;
        VerticalLayoutGroup layout = content.GetComponent<VerticalLayoutGroup>();
        if (!layout) layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 6f; layout.childControlWidth = true; layout.childControlHeight = false;
        layout.childForceExpandWidth = true; layout.childForceExpandHeight = false;
        ContentSizeFitter fitter = content.GetComponent<ContentSizeFitter>();
        if (!fitter) fitter = content.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    }

    private static GameObject Child(Transform parent, string name)
    {
        var go = new GameObject(name); go.transform.SetParent(parent, false); return go;
    }
    private static GameObject Rect(string name, Transform parent, Vector2 size, Vector2 position = default)
    {
        var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false);
        var rect = (RectTransform)go.transform; rect.sizeDelta = size; rect.anchoredPosition = position; return go;
    }
    private static Text Label(Transform parent, Font font, string text, Vector2 size, Vector2 position)
    {
        Text label = Rect("Label", parent, size, position).AddComponent<Text>();
        label.font = font; label.fontSize = 20; label.text = text; label.raycastTarget = false;
        label.color = new Color(0.25f, 0.15f, 0.08f); label.alignment = TextAnchor.MiddleLeft;
        label.horizontalOverflow = HorizontalWrapMode.Wrap; label.verticalOverflow = VerticalWrapMode.Truncate;
        return label;
    }
    private static Button Button(Transform parent, Font font, string caption, Vector2 size, Vector2 position)
    {
        GameObject go = Rect(caption, parent, size, position);
        Image image = go.AddComponent<Image>(); image.color = new Color(0.78f, 0.69f, 0.47f);
        Button button = go.AddComponent<Button>(); button.targetGraphic = image;
        Label(go.transform, font, caption, size - new Vector2(8f, 4f), Vector2.zero).alignment = TextAnchor.MiddleCenter;
        return button;
    }
    private static void CopyImage(Image target, Image source)
    {
        target.sprite = source.sprite; target.type = source.type; target.color = source.color;
    }
    private static T Require<T>(string path) where T : Object
        => AssetDatabase.LoadAssetAtPath<T>(path) ? AssetDatabase.LoadAssetAtPath<T>(path)
            : throw new InvalidOperationException("Missing housing input: " + path);
    private static T[] All<T>(Scene scene) where T : Component
        => scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<T>(true)).ToArray();
    private static T One<T>(Scene scene) where T : Component => All<T>(scene).Single();
    private static void Set(Object owner, string field, Object value)
    {
        var serialized = new SerializedObject(owner);
        serialized.FindProperty(field).objectReferenceValue = value; serialized.ApplyModifiedPropertiesWithoutUndo();
    }
    private static void SetInt(Object owner, string field, int value)
    {
        var serialized = new SerializedObject(owner);
        serialized.FindProperty(field).intValue = value; serialized.ApplyModifiedPropertiesWithoutUndo();
    }
}
