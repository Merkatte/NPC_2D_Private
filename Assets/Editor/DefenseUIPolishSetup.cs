using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

// Scoped authoring only; the assembly role invokes Apply after compilation.
public static class DefenseUIPolishSetup
{
    private const string ScenePath = "Assets/Scenes/DefenseTest.unity";
    private const string PopupPath = "Assets/Prefab/UI/TownHallPopup.prefab";
    private const string ArtRoot = "Assets/Art/Generated/UI/ui-merchant-";
    private const float CardScale = 0.75f;
    private const float CardStep = 264f;
    private const float ContentInset = 12f;
    private static readonly Vector2 Center = new Vector2(0.5f, 0.5f);
    private static readonly Color Ink = new Color(0.22f, 0.13f, 0.06f);

    [MenuItem("Tools/NPC/Defense/Polish Recruitment and Wave UI")]
    public static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling ||
            EditorApplication.isUpdating || PrefabStageUtility.GetCurrentPrefabStage() != null)
            throw new InvalidOperationException("UI polish requires an idle Editor outside Prefab Mode.");
        Scene scene = SceneManager.GetSceneByPath(ScenePath);
        if (!scene.IsValid() || !scene.isLoaded)
            throw new InvalidOperationException("Open DefenseTest before applying UI polish.");
        for (int i = 0; i < SceneManager.sceneCount; ++i)
            if (SceneManager.GetSceneAt(i).isDirty)
                throw new InvalidOperationException("UI polish refuses unsaved scene changes.");
        // Validate target owners and art before making either persistent change.
        One<TownHallPopup>(scene);
        One<DefenseHUD>(scene);
        foreach (string name in new[] { "board-panel-9slice", "folder-page-9slice", "green-button-9slice", "olive-button-9slice" })
            Sprite(name);
        GameObject prefab = PrefabUtility.LoadPrefabContents(PopupPath);
        try
        {
            ConfigureRecruitmentLayout(prefab.GetComponent<TownHallPopup>());
            PrefabUtility.SaveAsPrefabAsset(prefab, PopupPath, out bool success);
            if (!success) throw new InvalidOperationException("Could not save TownHallPopup UI layout.");
        }
        finally { PrefabUtility.UnloadPrefabContents(prefab); }

        // Saving the prefab first moves inherited cards; only the scene-added Archer needs reparenting here.
        ConfigureRecruitmentLayout(One<TownHallPopup>(scene));
        ConfigureWaveHud(One<DefenseHUD>(scene));
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene, ScenePath))
            throw new InvalidOperationException("Could not save DefenseTest UI layout.");
        Debug.Log("DefenseUIPolishSetup saved TownHallPopup and DefenseTest only. Play and screen behavior remain unverified.");
    }

    public static void ConfigureRecruitmentLayout(TownHallPopup popup)
    {
        if (!popup) throw new InvalidOperationException("UI polish requires TownHallPopup.");
        Transform root = popup.transform;
        Layout((RectTransform)root, new Vector2(920f, 760f), Vector2.zero, Center, Center);
        root.GetComponent<Image>().pixelsPerUnitMultiplier = 2f;
        Record(root.GetComponent<Image>());
        Layout(RequiredRect(root, "InnerBackground"), new Vector2(820f, 600f), new Vector2(0f, -25f), Center, Center);
        Layout(RequiredRect(root, "Title"), new Vector2(420f, 90f), new Vector2(0f, -48f), new Vector2(0.5f, 1f), Center);
        Layout(RequiredRect(root, "Title/TitleText"), new Vector2(360f, 48f), Vector2.zero, Center, Center);
        Layout(RequiredRect(root, "CloseButton"), new Vector2(72f, 72f), new Vector2(-18f, -18f), Vector2.one, Vector2.one);
        Layout(RequiredRect(root, "TownStatusTab"), new Vector2(220f, 64f), new Vector2(-280f, 220f), Center, Center);
        Layout(RequiredRect(root, "RecruitmentTab"), new Vector2(220f, 64f), new Vector2(-50f, 220f), Center, Center);
        Layout(RequiredRect(root, "TownStatusPanel"), new Vector2(760f, 460f), new Vector2(0f, -45f), Center, Center);
        Layout(RequiredRect(root, "ResultText"), new Vector2(740f, 30f), new Vector2(0f, -306f), Center, Center);
        RectTransform panel = RequiredRect(root, "RecruitmentPanel");
        Layout(panel, new Vector2(780f, 480f), new Vector2(0f, -45f), Center, Center);
        RectTransform viewport = ChildRect(panel, "Viewport");
        Layout(viewport, new Vector2(780f, 436f), Vector2.zero, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
        Image surface = GetOrAdd<Image>(viewport);
        surface.color = Color.clear;
        surface.raycastTarget = true;
        Record(surface);
        GetOrAdd<RectMask2D>(viewport);
        RectTransform content = ChildRect(viewport, "Content");
        var data = new SerializedObject(popup);
        SerializedProperty cards = data.FindProperty("_cards");
        float contentWidth = Mathf.Max(780f, ContentInset * 2f + cards.arraySize * CardStep - (CardStep - 330f * CardScale));
        Layout(content, new Vector2(contentWidth, 436f), Vector2.zero, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f));
        for (int i = 0; i < cards.arraySize; ++i)
        {
            var card = cards.GetArrayElementAtIndex(i).objectReferenceValue as TownHallRecruitCard;
            if (!card) throw new InvalidOperationException("TownHallPopup has a missing recruitment card.");
            var rect = (RectTransform)card.transform;
            if (rect.parent != content) rect.SetParent(content, false);
            Layout(rect, new Vector2(330f, 550f), new Vector2(ContentInset + 165f * CardScale + i * CardStep, 0f),
                new Vector2(0f, 0.5f), Center);
            rect.localScale = Vector3.one * CardScale;
            Record(rect);
        }
        Scrollbar scrollbar = ConfigureScrollbar(panel);
        ScrollRect scroll = GetOrAdd<ScrollRect>(panel);
        scroll.content = content;
        scroll.viewport = viewport;
        scroll.horizontal = true;
        scroll.vertical = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.inertia = true;
        scroll.scrollSensitivity = 30f;
        scroll.horizontalScrollbar = scrollbar;
        scroll.horizontalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
        scroll.StopMovement();
        scroll.horizontalNormalizedPosition = 0f;
        Record(scroll);
        data.FindProperty("_recruitmentScrollRect").objectReferenceValue = scroll;
        data.FindProperty("_fitToCanvas").boolValue = true;
        data.FindProperty("_screenMargin").floatValue = 24f;
        data.ApplyModifiedPropertiesWithoutUndo();
        Record(popup);
    }

    private static Scrollbar ConfigureScrollbar(RectTransform panel)
    {
        RectTransform bar = ChildRect(panel, "HorizontalScrollbar");
        Layout(bar, new Vector2(780f, 22f), Vector2.zero, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f));
        Style(GetOrAdd<Image>(bar), "board-panel-9slice", 10f, true);
        RectTransform sliding = ChildRect(bar, "SlidingArea");
        Stretch(sliding, new Vector2(4f, 3f));
        RectTransform handle = ChildRect(sliding, "Handle");
        Stretch(handle, Vector2.zero);
        Image handleImage = GetOrAdd<Image>(handle);
        Style(handleImage, "olive-button-9slice", 8f, true);
        Scrollbar scrollbar = GetOrAdd<Scrollbar>(bar);
        scrollbar.direction = Scrollbar.Direction.LeftToRight;
        scrollbar.handleRect = handle;
        scrollbar.targetGraphic = handleImage;
        scrollbar.value = 0f;
        Record(scrollbar);
        return scrollbar;
    }

    public static void ConfigureWaveHud(DefenseHUD hud)
    {
        Text status = Read<Text>(hud, "_statusText");
        Button next = Read<Button>(hud, "_nextWaveButton");
        Button notification = Read<Button>(hud, "_notificationButton");
        RectTransform bar = (RectTransform)status.transform.parent;
        Layout(bar, new Vector2(620f, 144f), new Vector2(-16f, -16f), Vector2.one, Vector2.one);
        Style(bar.GetComponent<Image>(), "board-panel-9slice", 5f, false);
        RectTransform paper = ChildRect(bar, "Parchment");
        Layout(paper, new Vector2(580f, 108f), Vector2.zero, Center, Center);
        paper.SetAsFirstSibling();
        Style(GetOrAdd<Image>(paper), "folder-page-9slice", 1f, false);
        Layout(status.rectTransform, new Vector2(570f, 32f), new Vector2(0f, 36f), Center, Center);
        status.fontSize = 21;
        status.color = Ink;
        Record(status);
        StyleWaveButton(next, new Vector2(190f, 44f), new Vector2(-180f, -28f), "green-button-9slice", 2f,
            new Color(1f, 0.96f, 0.84f));
        StyleWaveButton(notification, new Vector2(350f, 44f), new Vector2(100f, -28f), "olive-button-9slice", 4f, Ink);
    }

    private static void StyleWaveButton(Button button, Vector2 size, Vector2 position, string sprite, float pixelsPerUnit, Color textColor)
    {
        Layout((RectTransform)button.transform, size, position, Center, Center);
        Style(button.GetComponent<Image>(), sprite, pixelsPerUnit, true);
        Text label = button.GetComponentInChildren<Text>(true);
        Layout(label.rectTransform, size - new Vector2(16f, 4f), Vector2.zero, Center, Center);
        label.fontSize = 18;
        label.color = textColor;
        label.horizontalOverflow = HorizontalWrapMode.Wrap;
        label.verticalOverflow = VerticalWrapMode.Truncate;
        Record(label);
    }

    private static void Style(Image image, string sprite, float pixelsPerUnit, bool raycast)
    {
        image.sprite = Sprite(sprite);
        image.type = Image.Type.Sliced;
        image.color = Color.white;
        image.pixelsPerUnitMultiplier = pixelsPerUnit;
        image.raycastTarget = raycast;
        Record(image);
    }

    private static Sprite Sprite(string name)
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ArtRoot + name + ".png");
        if (!sprite) throw new InvalidOperationException("Missing UI polish sprite: " + name);
        return sprite;
    }

    private static RectTransform RequiredRect(Transform parent, string name)
    {
        var rect = parent.Find(name) as RectTransform;
        return rect ? rect : throw new InvalidOperationException("Missing UI polish RectTransform: " + name);
    }

    private static RectTransform ChildRect(Transform parent, string name)
    {
        Transform child = parent.Find(name);
        if (child) return (RectTransform)child;
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = parent.gameObject.layer;
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    private static void Layout(RectTransform rect, Vector2 size, Vector2 position, Vector2 anchor, Vector2 pivot)
    {
        rect.anchorMin = rect.anchorMax = anchor;
        rect.pivot = pivot;
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        Record(rect);
    }

    private static void Stretch(RectTransform rect, Vector2 inset)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = inset;
        rect.offsetMax = -inset;
        Record(rect);
    }

    private static T GetOrAdd<T>(Component owner) where T : Component
        => owner.GetComponent<T>() ? owner.GetComponent<T>() : owner.gameObject.AddComponent<T>();

    private static T Read<T>(Object owner, string field) where T : Object
        => (T)new SerializedObject(owner).FindProperty(field).objectReferenceValue;

    private static T One<T>(Scene scene) where T : Component
        => scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<T>(true)).Single();

    private static void Record(Object target)
    {
        EditorUtility.SetDirty(target);
        if (PrefabUtility.IsPartOfPrefabInstance(target)) PrefabUtility.RecordPrefabInstancePropertyModifications(target);
    }
}
