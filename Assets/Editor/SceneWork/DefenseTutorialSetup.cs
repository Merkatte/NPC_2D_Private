using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

// Authoring tool only. Assembly workers execute these menus after the runtime contracts compile.
public static class DefenseTutorialSetup
{
    private const string DefenseScenePath = "Assets/Scenes/DefenseTest.unity";
    private const string TerrainScenePath = "Assets/Scenes/TerrainTest.unity";
    private const string DialoguePath = "Assets/Prefab/UI/SequenceDialogue.prefab";
    private const string DefinitionPath = "Assets/Data/ScriptableObject/Tutorial/DefenseTutorial.asset";
    private const string LocalizationPath = "Assets/Data/ScriptableObject/LocalizeData.asset";
    private const string ClerkSpritePath = "Assets/Art/Generated/NPC/guild-receptionist-silver.png";
    private const string ControllerPath = "Assets/Animation/NPCGirl_Move.controller";
    private const string PanelGuid = "8155c68917ba460292c2aeefe1028551";

    [MenuItem("Tools/NPC/Tutorial/Setup Defense Tutorial")]
    public static void SetupDefense()
    {
        Scene scene = RequireScene(DefenseScenePath);
        Transform systems = RequirePath(scene, "Systems");
        Transform map = RequirePath(scene, "InGameObjects/Map");
        Transform townHall = RequireChild(map, "TownHall");
        Canvas canvas = RequirePath(scene, "Canvas").GetComponent<Canvas>();
        Require(canvas, "Canvas component is missing.");
        Require(!systems.Find("TutorialSystems") && !canvas.transform.Find("TutorialUI") &&
            !map.Find("TutorialActors") && !townHall.Find("TutorialDoorAnchor") &&
            !townHall.Find("TutorialStandingAnchor"), "Tutorial objects already exist; preserve authored edits instead of rebuilding.");
        DefenseGameSession session = RequireSingle<DefenseGameSession>(scene);
        UIManager uiManager = RequireSingle<UIManager>(scene);
        Camera worldCamera = RequirePath(scene, "Main Camera").GetComponent<Camera>();
        Require(worldCamera && worldCamera.orthographic, "Expected the orthographic Main Camera.");
        PointerClickRouter click = RequireSingle<PointerClickRouter>(scene);
        PointerHoverRouter hover = RequireSingle<PointerHoverRouter>(scene);
        TestCameraArrowMove cameraInput = RequireSingle<TestCameraArrowMove>(scene);
        DefenseEventController events = RequireSingle<DefenseEventController>(scene);
        Transform[] facilities = {
            townHall, InitialFacility(scene, "DefensePlot_Farm"), InitialFacility(scene, "DefensePlot_Warehouse"),
            InitialFacility(scene, "DefensePlot_Pub"), InitialFacility(scene, "DefensePlot_Inn"),
            InitialFacility(scene, "DefensePlot_GuardPost"), RequirePath(scene, "Systems/Manager/Defense/NorthWall/WallSegment5"),
            RequireChild(map, "MerchantCaravan")
        };
        Require(!RequireChild(canvas.transform, "DefenseUI").gameObject.activeSelf,
            "Expected the existing inactive DefenseUI; its activation will not be changed.");
        Sprite clerkSprite = LoadClerkSprite();
        RuntimeAnimatorController controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ControllerPath);
        Require(controller, "Missing clerk movement controller.");
        RequireField(session, "_timeController");
        foreach (Object receiver in new Object[] { click, hover, cameraInput, events, uiManager })
            RequireField(receiver, "_inputGate");
        Require(Components<GameTimeController>(scene).Count == 0, "DefenseTest already has a time controller; inspect before assembly.");

        GameObject dialogueAsset = GetOrCreateDialogue();
        SequenceDefinition definition = GetOrCreateDefinition();
        Transform host = CreateTransform("TutorialSystems", systems);
        GameTimeController time = host.gameObject.AddComponent<GameTimeController>();
        GameplayInputGate gate = host.gameObject.AddComponent<GameplayInputGate>();
        SequenceBindings bindings = host.gameObject.AddComponent<SequenceBindings>();
        SequenceCameraDirector cameraDirector = host.gameObject.AddComponent<SequenceCameraDirector>();
        SequenceActorDirector actorDirector = host.gameObject.AddComponent<SequenceActorDirector>();
        SequenceRunner runner = host.gameObject.AddComponent<SequenceRunner>();
        DefenseTutorialStartup startup = host.gameObject.AddComponent<DefenseTutorialStartup>();

        GameObject ui = (GameObject)PrefabUtility.InstantiatePrefab(dialogueAsset, canvas.transform);
        ui.name = "TutorialUI";
        ui.transform.SetAsLastSibling();
        DialogueView dialogue = ui.GetComponent<DialogueView>();
        SetReferences(dialogue, ("_worldCamera", worldCamera), ("_canvas", canvas));
        SetReferences(session, ("_timeController", time));
        SetReferences(gate, ("_uiManager", uiManager));
        SetReferences(cameraDirector, ("_camera", worldCamera));
        SetReferences(runner, ("_timeController", time), ("_inputGate", gate), ("_bindings", bindings),
            ("_cameraDirector", cameraDirector), ("_actorDirector", actorDirector), ("_dialogue", dialogue));
        SetReferences(startup, ("_timeController", time), ("_runner", runner), ("_sequence", definition), ("_gameSession", session));
        foreach (Object receiver in new Object[] { click, hover, cameraInput, events, uiManager })
            SetReferences(receiver, ("_inputGate", gate));
        ConfigureGameplayGroups(gate, canvas.transform, ui.transform);

        Transform door = CreateTransform("TutorialDoorAnchor", townHall);
        door.localPosition = new Vector3(0f, -1.8f, 0f);
        Transform standing = CreateTransform("TutorialStandingAnchor", townHall);
        standing.localPosition = new Vector3(-0.65f, -2.8f, 0f);
        Transform actors = CreateTransform("TutorialActors", map);
        Transform clerk = CreateTransform("Clerk", actors);
        clerk.position = door.position;
        Animator animator = clerk.gameObject.AddComponent<Animator>();
        animator.runtimeAnimatorController = controller;
        Transform visual = CreateTransform("Visual", clerk);
        Transform spriteRoot = CreateTransform("Image", visual);
        spriteRoot.localPosition = new Vector3(-0.008f, 0.117f, 0f);
        spriteRoot.localScale = new Vector3(0.15f, 0.15f, 1f);
        SpriteRenderer renderer = spriteRoot.gameObject.AddComponent<SpriteRenderer>();
        renderer.sprite = clerkSprite;
        // The clerk travels only in front of this facade; keep it above the facade without an NPC script.
        renderer.sortingOrder = Mathf.Clamp(10000 - Mathf.RoundToInt(standing.position.y * 100f), 1000, 19000);
        Transform messageAnchor = CreateTransform("MessageAnchor", clerk);
        messageAnchor.localPosition = new Vector3(0f, 0.85f, 0f);
        clerk.gameObject.SetActive(false);
        ConfigureBindings(bindings, facilities, clerk, animator, renderer, messageAnchor, door, standing, host);
        SaveScene(scene, DefenseScenePath);
        Debug.Log("DefenseTutorialSetup saved DefenseTest, SequenceDialogue prefab and DefenseTutorial definition. TerrainTest uses its separate menu.");
    }

    [MenuItem("Tools/NPC/Tutorial/Setup Terrain Time")]
    public static void SetupTerrainTime()
    {
        Scene scene = RequireScene(TerrainScenePath);
        DefenseGameSession session = RequireSingle<DefenseGameSession>(scene);
        RequireField(session, "_timeController");
        List<GameTimeController> controllers = Components<GameTimeController>(scene);
        Require(controllers.Count <= 1, "Multiple time controllers require manual inspection.");
        GameTimeController time = controllers.Count == 1 ? controllers[0] : session.gameObject.AddComponent<GameTimeController>();
        SetReferences(session, ("_timeController", time));
        SaveScene(scene, TerrainScenePath);
        Debug.Log("DefenseTutorialSetup saved TerrainTest common time reference only.");
    }

    private static GameObject GetOrCreateDialogue()
    {
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(DialoguePath);
        if (existing)
        {
            RequireCleanAsset(existing);
            Require(existing.GetComponent<DialogueView>(), "Existing SequenceDialogue has no DialogueView.");
            return existing;
        }
        LocalizeData localization = AssetDatabase.LoadAssetAtPath<LocalizeData>(LocalizationPath);
        Sprite panel = AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GUIDToAssetPath(PanelGuid));
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        Require(localization && panel && font, "Missing localization, speech panel or legacy font.");
        GameObject root = new GameObject("SequenceDialogue", typeof(RectTransform));
        root.layer = 5;
        try
        {
            Stretch((RectTransform)root.transform);
            DialogueView view = root.AddComponent<DialogueView>();
            RectTransform blocker = Rect("Blocker", root.transform, Vector2.zero);
            Stretch(blocker);
            Image blockerImage = blocker.gameObject.AddComponent<Image>();
            blockerImage.color = Color.clear;
            blockerImage.raycastTarget = true;
            RectTransform bubble = Panel("Bubble", root.transform, new Vector2(600f, 120f), panel);
            bubble.pivot = new Vector2(0.5f, 0f);
            Text bubbleText = Label("Text", bubble, font, 20, TextAnchor.MiddleCenter);
            Inset(bubbleText.rectTransform, 28f, 20f, 28f, 20f);
            RectTransform bottom = Panel("BottomPanel", root.transform, new Vector2(1040f, 190f), panel);
            bottom.anchorMin = bottom.anchorMax = new Vector2(0.5f, 0f);
            bottom.pivot = new Vector2(0.5f, 0f);
            bottom.anchoredPosition = new Vector2(0f, 24f);
            Text speaker = Label("SpeakerText", bottom, font, 22, TextAnchor.UpperLeft);
            Inset(speaker.rectTransform, 32f, 20f, 32f, 128f);
            speaker.fontStyle = FontStyle.Bold;
            Text bottomText = Label("Text", bottom, font, 22, TextAnchor.UpperLeft);
            Inset(bottomText.rectTransform, 32f, 60f, 32f, 24f);
            RectTransform choices = Rect("Choices", root.transform, new Vector2(296f, 46f));
            choices.anchorMin = choices.anchorMax = new Vector2(0.5f, 0f);
            choices.anchoredPosition = new Vector2(0f, 260f);
            HorizontalLayoutGroup layout = choices.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 16f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            RectTransform template = Panel("ChoiceTemplate", choices, new Vector2(140f, 46f), panel);
            Button button = template.gameObject.AddComponent<Button>();
            button.targetGraphic = template.GetComponent<Image>();
            Text choiceText = Label("Text", template, font, 20, TextAnchor.MiddleCenter);
            Inset(choiceText.rectTransform, 10f, 4f, 10f, 4f);
            SetReferences(view, ("_localization", localization), ("_blocker", blocker.gameObject),
                ("_bubble", bubble), ("_bubbleText", bubbleText), ("_bottomPanel", bottom),
                ("_bottomText", bottomText), ("_speakerText", speaker), ("_choiceRoot", choices), ("_choiceTemplate", button));
            template.gameObject.SetActive(false);
            choices.gameObject.SetActive(false);
            bubble.gameObject.SetActive(false);
            bottom.gameObject.SetActive(false);
            blocker.gameObject.SetActive(false);
            GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, DialoguePath);
            Require(saved, "Could not save " + DialoguePath);
            return saved;
        }
        finally { Object.DestroyImmediate(root); }
    }

    private static SequenceDefinition GetOrCreateDefinition()
    {
        SequenceDefinition existing = AssetDatabase.LoadAssetAtPath<SequenceDefinition>(DefinitionPath);
        if (existing) { RequireCleanAsset(existing); return existing; }
        const string folder = "Assets/Data/ScriptableObject/Tutorial";
        if (!AssetDatabase.IsValidFolder(folder))
            Require(!string.IsNullOrEmpty(AssetDatabase.CreateFolder("Assets/Data/ScriptableObject", "Tutorial")), "Cannot create tutorial asset folder.");
        SequenceDefinition definition = ScriptableObject.CreateInstance<SequenceDefinition>();
        SerializedObject serialized = new SerializedObject(definition);
        serialized.FindProperty("_entryStepId").stringValue = "townhall_arrive";
        SerializedProperty steps = serialized.FindProperty("_steps");
        steps.arraySize = 0;
        AddStep(steps, "townhall_arrive", SequenceStepKind.Camera, "clerk_enter", "townhall", 2f, 4.5f);
        AddStep(steps, "clerk_enter", SequenceStepKind.Actor, "welcome", "standing", 1f);
        AddLine(steps, "welcome", "offer", 1201);
        SerializedProperty offer = AddLine(steps, "offer", "", 1202);
        offer.FindPropertyRelative("_kind").intValue = (int)SequenceStepKind.Choice;
        SerializedProperty options = offer.FindPropertyRelative("_choices");
        options.arraySize = 2;
        SetChoice(options.GetArrayElementAtIndex(0), "yes", 1203, "tour_townhall_camera");
        SetChoice(options.GetArrayElementAtIndex(1), "no", 1204, "decline");
        string[] targets = { "townhall", "farm", "warehouse", "restaurant", "inn", "guardpost", "northwall", "merchant_landing" };
        for (int i = 0; i < targets.Length; ++i)
        {
            string id = "tour_" + targets[i];
            AddStep(steps, id + "_camera", SequenceStepKind.Camera, id + "_line", targets[i], 0.8f, i == 6 ? 8f : 4.5f);
            string next = i + 1 < targets.Length ? "tour_" + targets[i + 1] + "_camera" : "clerk_return_shot";
            AddLine(steps, id + "_line", next, 1205 + i, DialoguePresentation.BottomPanel);
        }
        AddStep(steps, "clerk_return_shot", SequenceStepKind.Camera, "finish", "clerk_shot", 0.8f, 2.7f);
        AddLine(steps, "finish", "clerk_leave", 1213);
        AddLine(steps, "decline", "clerk_leave", 1214);
        SerializedProperty leave = AddStep(steps, "clerk_leave", SequenceStepKind.Actor, "clerk_hide", "door", 1f);
        leave.FindPropertyRelative("_faceLeft").boolValue = false;
        SerializedProperty hide = AddStep(steps, "clerk_hide", SequenceStepKind.Actor, "camera_restore");
        hide.FindPropertyRelative("_actorCommand").intValue = (int)SequenceActorCommand.Hide;
        SerializedProperty restore = AddStep(steps, "camera_restore", SequenceStepKind.Camera, "", "", 1.2f);
        restore.FindPropertyRelative("_restoreCamera").boolValue = true;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.CreateAsset(definition, DefinitionPath);
        AssetDatabase.SaveAssetIfDirty(definition);
        return definition;
    }

    private static SerializedProperty AddStep(SerializedProperty steps, string id, SequenceStepKind kind,
        string next, string target = "", float duration = 0.8f, float size = 4.5f)
    {
        int index = steps.arraySize++;
        SerializedProperty row = steps.GetArrayElementAtIndex(index);
        row.FindPropertyRelative("_id").stringValue = id;
        row.FindPropertyRelative("_kind").intValue = (int)kind;
        row.FindPropertyRelative("_nextId").stringValue = next;
        row.FindPropertyRelative("_actorId").stringValue = "clerk";
        row.FindPropertyRelative("_targetId").stringValue = target;
        row.FindPropertyRelative("_duration").floatValue = duration;
        row.FindPropertyRelative("_orthographicSize").floatValue = size;
        row.FindPropertyRelative("_autoAdvanceDelay").floatValue = 3f;
        row.FindPropertyRelative("_ease").intValue = (int)Ease.InOutCubic;
        row.FindPropertyRelative("_restoreCamera").boolValue = false;
        row.FindPropertyRelative("_actorCommand").intValue = (int)SequenceActorCommand.Walk;
        row.FindPropertyRelative("_faceLeft").boolValue = true;
        row.FindPropertyRelative("_textKey").intValue = 1201;
        row.FindPropertyRelative("_speakerKey").intValue = 1215;
        row.FindPropertyRelative("_presentation").intValue = (int)DialoguePresentation.Bubble;
        row.FindPropertyRelative("_actionId").stringValue = "";
        row.FindPropertyRelative("_choices").arraySize = 0;
        return row;
    }

    private static SerializedProperty AddLine(SerializedProperty steps, string id, string next, int key,
        DialoguePresentation presentation = DialoguePresentation.Bubble)
    {
        SerializedProperty row = AddStep(steps, id, SequenceStepKind.Dialogue, next);
        row.FindPropertyRelative("_textKey").intValue = key;
        row.FindPropertyRelative("_presentation").intValue = (int)presentation;
        return row;
    }

    private static void SetChoice(SerializedProperty row, string id, int key, string next)
    {
        row.FindPropertyRelative("_id").stringValue = id;
        row.FindPropertyRelative("_textKey").intValue = key;
        row.FindPropertyRelative("_nextId").stringValue = next;
    }

    private static void ConfigureBindings(SequenceBindings bindings, Transform[] facilities, Transform clerk,
        Animator animator, SpriteRenderer renderer, Transform messageAnchor, Transform door, Transform standing, Transform host)
    {
        Transform anchors = CreateTransform("TutorialCameraAnchors", host);
        Transform landing = CreateTransform("MerchantLanding", anchors);
        landing.position = facilities[7].position;
        facilities[7] = landing; // Stable place, independent of a caravan visit or its enabled state.
        Transform clerkShot = CreateTransform("ClerkShot", anchors);
        clerkShot.position = standing.position + Vector3.up * 0.9f;
        SerializedObject serialized = new SerializedObject(bindings);
        SerializedProperty actors = serialized.FindProperty("_actors");
        actors.arraySize = 1;
        SerializedProperty actor = actors.GetArrayElementAtIndex(0);
        actor.FindPropertyRelative("_id").stringValue = "clerk";
        actor.FindPropertyRelative("_root").objectReferenceValue = clerk;
        actor.FindPropertyRelative("_animator").objectReferenceValue = animator;
        actor.FindPropertyRelative("_renderer").objectReferenceValue = renderer;
        actor.FindPropertyRelative("_messageAnchor").objectReferenceValue = messageAnchor;
        string[] ids = { "townhall", "farm", "warehouse", "restaurant", "inn", "guardpost", "northwall", "merchant_landing", "door", "standing", "clerk_shot" };
        SerializedProperty targets = serialized.FindProperty("_targets");
        targets.arraySize = ids.Length;
        for (int i = 0; i < ids.Length; ++i)
        {
            SerializedProperty row = targets.GetArrayElementAtIndex(i);
            row.FindPropertyRelative("_id").stringValue = ids[i];
            row.FindPropertyRelative("_usePosition").boolValue = false;
            row.FindPropertyRelative("_transform").objectReferenceValue = i < facilities.Length ? facilities[i] : i == 8 ? door : i == 9 ? standing : clerkShot;
        }
        serialized.FindProperty("_actions").arraySize = 0;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ConfigureGameplayGroups(GameplayInputGate gate, Transform canvas, Transform tutorial)
    {
        var groups = new List<CanvasGroup>();
        foreach (Transform child in canvas)
        {
            if (child == tutorial) continue;
            CanvasGroup group = child.GetComponent<CanvasGroup>();
            if (!group) group = child.gameObject.AddComponent<CanvasGroup>();
            groups.Add(group);
        }
        SerializedObject serialized = new SerializedObject(gate);
        SerializedProperty rows = serialized.FindProperty("_gameplayGroups");
        rows.arraySize = groups.Count;
        for (int i = 0; i < groups.Count; ++i) rows.GetArrayElementAtIndex(i).objectReferenceValue = groups[i];
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static RectTransform Rect(string name, Transform parent, Vector2 size)
    {
        GameObject gameObject = new GameObject(name, typeof(RectTransform));
        gameObject.layer = 5;
        RectTransform rect = (RectTransform)gameObject.transform;
        rect.SetParent(parent, false);
        rect.sizeDelta = size;
        return rect;
    }

    private static RectTransform Panel(string name, Transform parent, Vector2 size, Sprite sprite)
    {
        RectTransform rect = Rect(name, parent, size);
        Image image = rect.gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.type = Image.Type.Sliced;
        return rect;
    }

    private static Text Label(string name, Transform parent, Font font, int size, TextAnchor alignment)
    {
        Text text = Rect(name, parent, Vector2.zero).gameObject.AddComponent<Text>();
        text.font = font;
        text.fontSize = size;
        text.color = new Color(0.12f, 0.1f, 0.08f, 1f);
        text.alignment = alignment;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        text.raycastTarget = false;
        return text;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
    }

    private static void Inset(RectTransform rect, float left, float top, float right, float bottom)
    {
        Stretch(rect);
        rect.offsetMin = new Vector2(left, bottom);
        rect.offsetMax = new Vector2(-right, -top);
    }

    private static Transform CreateTransform(string name, Transform parent)
    {
        Transform result = new GameObject(name).transform;
        result.SetParent(parent, false);
        return result;
    }

    private static Sprite LoadClerkSprite()
    {
        foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(ClerkSpritePath))
            if (asset is Sprite sprite && sprite.name == "guild-receptionist-silver_0") return sprite;
        throw new InvalidOperationException("DefenseTutorialSetup: missing approved silver clerk sprite slice.");
    }

    private static Transform InitialFacility(Scene scene, string plotName)
    {
        foreach (BuildingPlot plot in Components<BuildingPlot>(scene))
            if (plot.name == plotName)
            {
                Component facility = new SerializedObject(plot).FindProperty("_initialFacility").objectReferenceValue as Component;
                Require(facility, "Missing initial facility: " + plotName);
                return facility.transform;
            }
        throw new InvalidOperationException("Missing plot: " + plotName);
    }

    private static Transform RequirePath(Scene scene, string path)
    {
        string[] parts = path.Split('/');
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name != parts[0]) continue;
            Transform result = root.transform;
            for (int i = 1; i < parts.Length; ++i) result = RequireChild(result, parts[i]);
            return result;
        }
        throw new InvalidOperationException("Missing scene path: " + path);
    }

    private static Transform RequireChild(Transform parent, string name)
    {
        Transform child = parent.Find(name);
        Require(child, "Missing child " + parent.name + "/" + name);
        return child;
    }

    private static List<T> Components<T>(Scene scene) where T : Component
    {
        var result = new List<T>();
        foreach (GameObject root in scene.GetRootGameObjects()) result.AddRange(root.GetComponentsInChildren<T>(true));
        return result;
    }

    private static T RequireSingle<T>(Scene scene) where T : Component
    {
        List<T> matches = Components<T>(scene);
        Require(matches.Count == 1, "Expected exactly one " + typeof(T).Name + ".");
        return matches[0];
    }

    private static void SetReferences(Object target, params (string Name, Object Value)[] references)
    {
        SerializedObject serialized = new SerializedObject(target);
        foreach (var reference in references)
        {
            SerializedProperty field = serialized.FindProperty(reference.Name);
            Require(field != null, "Missing field " + target.name + "." + reference.Name);
            field.objectReferenceValue = reference.Value;
        }
        serialized.ApplyModifiedPropertiesWithoutUndo();
        if (PrefabUtility.IsPartOfPrefabInstance(target)) PrefabUtility.RecordPrefabInstancePropertyModifications(target);
    }

    private static void RequireField(Object target, string name)
    {
        Require(new SerializedObject(target).FindProperty(name) != null, "Compile required field first: " + target.name + "." + name);
    }

    private static void RequireCleanAsset(Object asset)
    {
        Require(!EditorUtility.IsDirty(asset), "Preserve unsaved edits to " + AssetDatabase.GetAssetPath(asset));
        if (asset is GameObject root)
            foreach (Component component in root.GetComponentsInChildren<Component>(true))
                Require(component && !EditorUtility.IsDirty(component) && !EditorUtility.IsDirty(component.gameObject), "Prefab contains missing components or unsaved edits.");
    }

    private static Scene RequireScene(string path)
    {
        Require(!EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isCompiling &&
            !EditorApplication.isUpdating && !PrefabStageUtility.GetCurrentPrefabStage(), "Requires idle Editor outside Play Mode and Prefab Mode.");
        Scene scene = SceneManager.GetActiveScene();
        Require(scene.IsValid() && scene.isLoaded && scene.path == path && !scene.isDirty,
            "Open the clean target scene first: " + path);
        return scene;
    }

    private static void SaveScene(Scene scene, string path)
    {
        Require(scene.IsValid() && scene.isLoaded && scene.path == path && SceneManager.GetActiveScene() == scene,
            "Target scene changed during assembly; no scene was saved.");
        EditorSceneManager.MarkSceneDirty(scene);
        Require(EditorSceneManager.SaveScene(scene, path), "Could not save " + path);
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException("DefenseTutorialSetup: " + message);
    }
}
