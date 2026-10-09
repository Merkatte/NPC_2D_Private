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

// Authoring only. The assembly role invokes this entry after the complete candidate compiles.
public static class DefenseSetup
{
    private const string SourceScene = "Assets/Scenes/BuildingTest.unity";
    private const string TargetScene = "Assets/Scenes/DefenseTest.unity";
    private const string PrefabRoot = "Assets/Prefab/Defense/";
    private const string DataRoot = "Assets/Data/ScriptableObject/Defense/";
    private const string AnimationRoot = "Assets/Animation/Defense/";
    private const string ArtRoot = "Assets/Art/Generated/Defense/";
    private const float West = -17f;
    private const float East = 64f;
    private const float WallY = 15f;
    private const float North = 28f;
    private const int WallCount = 9;
    private const float WallVisibleWidth = 9.847f;

    [MenuItem("Tools/NPC/Defense/Setup DefenseTest")]
    public static void Setup()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling ||
            EditorApplication.isUpdating || PrefabStageUtility.GetCurrentPrefabStage() != null)
            throw new InvalidOperationException("Defense setup requires an idle Editor outside Prefab Mode.");
        for (int i = 0; i < SceneManager.sceneCount; ++i)
            if (SceneManager.GetSceneAt(i).isDirty)
                throw new InvalidOperationException("Defense setup refuses unsaved scene changes.");
        if (File.Exists(TargetScene))
            throw new InvalidOperationException("DefenseTest already exists. Review existing changes before another assembly run.");
        Require<GameObject>("Assets/Prefab/InGame/NPCGirl.prefab");
        Require<GameObject>("Assets/Prefab/InGame/Enemy.prefab");
        foreach (string name in new[] { "wall-intact", "wall-damaged", "wall-destroyed", "building-rubble", "archer-bow", "archer-arrow" })
            if (!File.Exists(ArtRoot + name + ".png")) throw new InvalidOperationException("Missing promoted defense art: " + name);
        SceneSetup[] previous = EditorSceneManager.GetSceneManagerSetup();
        Scene scene = default;
        bool succeeded = false;
        try
        {
            EnsureFolder(PrefabRoot.TrimEnd('/')); EnsureFolder(DataRoot.TrimEnd('/'));
            EnsureFolder(AnimationRoot.TrimEnd('/')); EnsureFolder(ArtRoot.TrimEnd('/'));
            ImportArt();
            if (!AssetDatabase.CopyAsset(SourceScene, TargetScene)) throw new InvalidOperationException("Could not copy BuildingTest.");
            scene = EditorSceneManager.OpenScene(TargetScene, OpenSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            Assemble(scene);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, TargetScene)) throw new InvalidOperationException("Could not save DefenseTest.");
            for (int i = SceneManager.sceneCount - 1; i >= 0; --i)
            {
                Scene other = SceneManager.GetSceneAt(i);
                if (other == scene || !other.isLoaded) continue;
                if (other.isDirty) throw new InvalidOperationException("Another scene changed during defense setup; it was not closed or saved.");
                EditorSceneManager.CloseScene(other, true);
            }
            SceneManager.SetActiveScene(scene);
            succeeded = true;
            Debug.Log("DefenseSetup saved DefenseTest and its scoped defense assets. Play and screen behavior remain unverified.");
        }
        finally
        {
            // Only this call's copied scene is discarded on failure; original open scenes are never saved or reloaded.
            if (!succeeded)
            {
                if (scene.IsValid() && scene.isLoaded) EditorSceneManager.CloseScene(scene, true);
                EditorSceneManager.RestoreSceneManagerSetup(previous);
            }
        }
    }

    private static void Assemble(Scene scene)
    {
        TilemapNavigation navigation = One<TilemapNavigation>(scene);
        NPCManager npcs = One<NPCManager>(scene);
        ResourceManager resources = One<ResourceManager>(scene);
        GameObject root = Child(npcs.transform.parent, "Defense");
        DefenseGameSession session = root.AddComponent<DefenseGameSession>();
        DefenseBattlefield battlefield = root.AddComponent<DefenseBattlefield>();
        DefenseMaintenanceRegistry maintenance = root.AddComponent<DefenseMaintenanceRegistry>();
        DefenseCombatSettings combat = NewAsset<DefenseCombatSettings>("CombatSettings.asset");
        DefenseDurabilitySettings durability = NewAsset<DefenseDurabilitySettings>("DurabilitySettings.asset");
        Set(battlefield, "_navigation", navigation);
        BoxCollider2D interior = Bounds(root.transform, "InteriorBounds", new Vector2((West + East) * 0.5f, 1f),
            new Vector2(East - West, 29f));
        Set(battlefield, "_interiorBounds", interior);
        ExtendGround(scene, navigation);
        DefenseWallSegment[] walls = BuildWalls(root.transform, battlefield, maintenance, resources, durability, navigation);
        SetArray(battlefield, "_walls", walls);
        Transform[] guards = { Anchor(root.transform, "GuardWest", new Vector3(5.5f, WallY + 2.5f)),
            Anchor(root.transform, "GuardEast", new Vector3(41.5f, WallY + 2.5f)) };
        SetArray(battlefield, "_guardPositions", guards);
        DefenseProjectile projectile = CreateProjectile();
        WorkerNPC friendly = CreateWorkerVariant("NPCGirl", combat, projectile, false);
        WorkerNPC enemy = CreateWorkerVariant("Enemy", combat, projectile, true);
        Set(One<WorkerPool>(scene), "_workerPrefab", friendly);
        ConfigureActors(scene, root.transform, npcs, battlefield, combat, maintenance);
        ConfigureBuildings(scene, battlefield, maintenance, resources, durability, session);
        DefenseWaveController waves = root.AddComponent<DefenseWaveController>();
        DefenseEventController events = root.AddComponent<DefenseEventController>();
        Transform[] spawnPoints = new Transform[WallCount];
        for (int i = 0; i < spawnPoints.Length; ++i)
            spawnPoints[i] = Anchor(root.transform, "EnemySpawn" + (i + 1), new Vector3(West + 4.5f + i * 9f, 24.5f));
        NPCStatDefinition enemyStat = CopyAsset<NPCStatDefinition>("Assets/Data/ScriptableObject/EnemyStatDefinition_Melee.asset", "EnemyStat.asset");
        waves.Configure(session, battlefield, CreateWaveCatalog(), enemy, enemyStat, One<EnemyActionSelector>(scene), spawnPoints);
        events.Configure(session, waves, CreateEventCatalog());
        CreateDefenseUI(scene, session, waves, events);
        ConfigureRecruitment(scene, battlefield);
        DisableTestEnemySpawns(scene);
        foreach (Object asset in new Object[] { combat, durability }) AssetDatabase.SaveAssetIfDirty(asset);
    }

    private static void ImportArt()
    {
        foreach (string name in new[] { "wall-intact", "wall-damaged", "wall-destroyed", "building-rubble", "archer-bow", "archer-arrow" })
        {
            string path = ArtRoot + name + ".png";
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true; importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear; importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.Compressed; importer.maxTextureSize = 2048;
            importer.spritePixelsPerUnit = name.StartsWith("archer-", StringComparison.Ordinal) ? 900f : 150f;
            var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spritePivot = name == "archer-bow" ? new Vector2(0.551f, 0.5f) : new Vector2(0.5f, 0.5f);
            importer.SetTextureSettings(settings); importer.SaveAndReimport();
        }
    }

    private static void ExtendGround(Scene scene, TilemapNavigation navigation)
    {
        Tilemap map = Read<Tilemap>(navigation, "_tilemap");
        TileNavigationProfile profile = Read<TileNavigationProfile>(navigation, "_profile");
        if (!map || !profile || !profile.TryCreateLookup(out Dictionary<TileBase, int> costs, out string error))
            throw new InvalidOperationException("Defense requires the existing valid ground navigation profile.");
        TileBase ground = costs.Where(pair => pair.Value > 0).OrderBy(pair => pair.Value)
            .ThenBy(pair => pair.Key.name, StringComparer.Ordinal).First().Key;
        Vector3Int min = map.WorldToCell(new Vector3(West, 10f));
        Vector3Int max = map.WorldToCell(new Vector3(East - 0.01f, North));
        for (int y = min.y; y <= max.y; ++y)
            for (int x = min.x; x <= max.x; ++x)
                map.SetTile(new Vector3Int(x, y, min.z), ground);
        // Navigation has no cells outside these finite ground edges, so intact wall ends cannot be bypassed.
        foreach (MonoBehaviour behaviour in All<MonoBehaviour>(scene))
        {
            if (!behaviour || behaviour.GetType().Name != "TestCameraArrowMove") continue;
            Edit(behaviour, data => data.FindProperty("_maxBounds").vector2Value = new Vector2(East, North));
        }
    }

    private static DefenseWallSegment[] BuildWalls(Transform parent, DefenseBattlefield battlefield,
        DefenseMaintenanceRegistry registry, ResourceManager resources, DefenseDurabilitySettings settings,
        TilemapNavigation navigation)
    {
        Transform wallsRoot = Child(parent, "NorthWall").transform;
        var walls = new DefenseWallSegment[WallCount];
        float width = (East - West) / WallCount;
        for (int i = 0; i < walls.Length; ++i)
        {
            GameObject root = Child(wallsRoot, "WallSegment" + (i + 1));
            root.transform.position = new Vector3(West + width * (i + 0.5f), WallY);
            SpriteRenderer visual = Child(root.transform, "Visual").AddComponent<SpriteRenderer>();
            visual.sprite = Sprite("wall-intact"); visual.sortingOrder = 2;
            visual.transform.localScale = Vector3.one * (width / WallVisibleWidth);
            DefenseWallSegment wall = root.AddComponent<DefenseWallSegment>(); walls[i] = wall;
            Transform ground = Anchor(root.transform, "GroundAnchor", root.transform.position + Vector3.down * 1.5f);
            Transform top = Anchor(root.transform, "TopAnchor", root.transform.position + Vector3.down * 0.5f);
            Transform attack = Anchor(root.transform, "AttackAnchor", root.transform.position + Vector3.up * 2.5f);
            Transform entry = Anchor(root.transform, "EntryAnchor", root.transform.position + Vector3.down * 1.5f);
            CombatTarget target = CreateTarget(root.transform, "HitTarget", attack.position, "Friendly", out _);
            BoxCollider2D passage = Bounds(root.transform, "PassageBounds", root.transform.position + Vector3.up, new Vector2(width, 2f));
            var blockers = new List<NavigationObstacle2D>();
            if (i == walls.Length / 2)
            {
                blockers.Add(Obstacle(root.transform, navigation, "WestBlocker", new Vector2(-2.75f, 1f), new Vector2(3.5f, 1f), true));
                blockers.Add(Obstacle(root.transform, navigation, "FriendlyPassage", Vector2.up, new Vector2(2f, 1f), false));
                blockers.Add(Obstacle(root.transform, navigation, "EastBlocker", new Vector2(2.75f, 1f), new Vector2(3.5f, 1f), true));
            }
            else blockers.Add(Obstacle(root.transform, navigation, "Blocker", Vector2.up, new Vector2(width, 1f), true));
            DefenseMaintenanceSite maintenance = root.AddComponent<DefenseMaintenanceSite>();
            Set(maintenance, "_registry", registry); Set(maintenance, "_resources", resources);
            Set(maintenance, "_settings", settings); Set(maintenance, "_wall", wall); Set(maintenance, "_workAnchor", ground);
            Set(wall, "_battlefield", battlefield); Set(wall, "_settings", settings); Set(wall, "_target", target);
            Set(wall, "_maintenance", maintenance); Set(wall, "_passageBounds", passage);
            SetArray(wall, "_obstacles", blockers.ToArray()); Set(wall, "_topAnchor", top); Set(wall, "_groundAnchor", ground);
            Set(wall, "_attackAnchor", attack); Set(wall, "_entryAnchor", entry); Set(wall, "_visual", visual);
            Set(wall, "_intactSprite", Sprite("wall-intact")); Set(wall, "_damagedSprite", Sprite("wall-damaged"));
            Set(wall, "_destroyedSprite", Sprite("wall-destroyed"));
        }
        return walls;
    }

    private static NavigationObstacle2D Obstacle(Transform parent, TilemapNavigation navigation, string name,
        Vector2 offset, Vector2 size, bool blocksFriendly)
    {
        BoxCollider2D bounds = Bounds(parent, name, (Vector2)parent.position + offset, size);
        NavigationObstacle2D obstacle = bounds.gameObject.AddComponent<NavigationObstacle2D>();
        Set(obstacle, "_navigation", navigation); Set(obstacle, "_bounds", bounds);
        Edit(obstacle, data => { data.FindProperty("_blocksFriendly").boolValue = blocksFriendly;
            data.FindProperty("_blocksEnemy").boolValue = true; });
        return obstacle;
    }

    private static DefenseWaveCatalog CreateWaveCatalog()
    {
        int[] counts = { 5, 8, 12 };
        var waves = new DefenseWaveDefinition[counts.Length];
        for (int i = 0; i < counts.Length; ++i)
        {
            waves[i] = NewAsset<DefenseWaveDefinition>("Wave" + (i + 1) + ".asset");
            int count = counts[i]; Edit(waves[i], data => data.FindProperty("_enemyCount").intValue = count);
            AssetDatabase.SaveAssetIfDirty(waves[i]);
        }
        DefenseWaveCatalog catalog = NewAsset<DefenseWaveCatalog>("WaveCatalog.asset");
        SetArray(catalog, "_waves", waves); AssetDatabase.SaveAssetIfDirty(catalog); return catalog;
    }

    private static DefenseEventCatalog CreateEventCatalog()
    {
        DefenseEventDefinition definition = NewAsset<DefenseEventDefinition>("RestEvent.asset");
        Edit(definition, data =>
        {
            data.FindProperty("_eventId").stringValue = "defense.rest.notice";
            data.FindProperty("_notificationText").stringValue = "정비 소식 도착";
            data.FindProperty("_title").stringValue = "다음 습격을 준비하세요";
            data.FindProperty("_body").stringValue = "성벽과 마을을 살피며 다음 습격을 준비할 시간입니다.\n확인하거나 이번 소식을 넘길 수 있습니다.";
            SerializedProperty choices = data.FindProperty("_choices"); choices.arraySize = 2;
            choices.GetArrayElementAtIndex(0).FindPropertyRelative("_label").stringValue = "확인";
            choices.GetArrayElementAtIndex(0).FindPropertyRelative("_resultId").stringValue = "confirm";
            choices.GetArrayElementAtIndex(1).FindPropertyRelative("_label").stringValue = "넘기기";
            choices.GetArrayElementAtIndex(1).FindPropertyRelative("_resultId").stringValue = "skip";
        });
        DefenseEventCatalog catalog = NewAsset<DefenseEventCatalog>("EventCatalog.asset");
        SetArray(catalog, "_events", new[] { definition });
        AssetDatabase.SaveAssetIfDirty(definition); AssetDatabase.SaveAssetIfDirty(catalog); return catalog;
    }

    private static void CreateDefenseUI(Scene scene, DefenseGameSession session, DefenseWaveController waves, DefenseEventController events)
    {
        Canvas canvas = One<Canvas>(scene);
        GameObject reference = Require<GameObject>("Assets/Prefab/UI/ConstructionPopup.prefab");
        Font font = reference.GetComponentsInChildren<Text>(true).First(text => text.font).font;
        GameObject host = Rect("DefenseUI", canvas.transform, Vector2.zero); Stretch((RectTransform)host.transform);
        DefenseHUD hud = host.AddComponent<DefenseHUD>();
        DefenseEventPopup popup = host.AddComponent<DefenseEventPopup>();
        GameObject bar = Rect("DefenseStatus", host.transform, new Vector2(680f, 112f), new Vector2(0f, -12f));
        var barRect = (RectTransform)bar.transform; barRect.anchorMin = barRect.anchorMax = barRect.pivot = new Vector2(0.5f, 1f);
        CopyImage(bar.AddComponent<Image>(), reference.GetComponent<Image>()); bar.GetComponent<Image>().raycastTarget = false;
        Text status = Label(bar.transform, font, "방어 준비 중", new Vector2(640f, 42f), new Vector2(0f, 27f));
        Button next = Button(bar.transform, font, "다음 습격", new Vector2(190f, 42f), new Vector2(-210f, -25f), true);
        Button notification = Button(bar.transform, font, "정비 소식", new Vector2(395f, 42f), new Vector2(100f, -25f), true);
        Text notificationText = notification.GetComponentInChildren<Text>();
        GameObject modal = Modal(host.transform, "EventModal");
        GameObject panel = Rect("EventPanel", modal.transform, new Vector2(720f, 380f));
        CopyImage(panel.AddComponent<Image>(), reference.GetComponent<Image>());
        Text title = Label(panel.transform, font, string.Empty, new Vector2(600f, 55f), new Vector2(-18f, 137f));
        title.fontSize = 26;
        Text body = Label(panel.transform, font, string.Empty, new Vector2(640f, 160f), new Vector2(0f, 16f));
        Button close = Button(panel.transform, font, "X", new Vector2(48f, 48f), Vector2.zero, false);
        var closeRect = (RectTransform)close.transform; closeRect.anchorMin = closeRect.anchorMax = closeRect.pivot = Vector2.one;
        closeRect.anchoredPosition = new Vector2(-12f, -12f);
        Button[] choices = { Button(panel.transform, font, "확인", new Vector2(230f, 50f), new Vector2(-135f, -125f), true),
            Button(panel.transform, font, "넘기기", new Vector2(230f, 50f), new Vector2(135f, -125f), false) };
        GameObject gameOver = Modal(host.transform, "GameOver");
        Text gameOverText = Label(gameOver.transform, font, "게임 오버\n시청이 무너졌습니다.", new Vector2(720f, 180f), Vector2.zero);
        gameOverText.fontSize = 38; gameOverText.color = Color.white;
        modal.SetActive(false); gameOver.SetActive(false);
        hud.Configure(session, waves, events, status, notificationText, next, notification, gameOver);
        popup.Configure(events, modal, title, body, choices, choices.Select(button => button.GetComponentInChildren<Text>()).ToArray(), close);
    }

    private static GameObject Modal(Transform parent, string name)
    {
        GameObject modal = Rect(name, parent, Vector2.zero); Stretch((RectTransform)modal.transform);
        Image blocker = modal.AddComponent<Image>(); blocker.color = new Color(0f, 0f, 0f, 0.7f); blocker.raycastTarget = true;
        return modal;
    }

    private static void DisableTestEnemySpawns(Scene scene)
    {
        foreach (MonoBehaviour component in All<MonoBehaviour>(scene))
            if (component && (component.GetType().Name == "TestEnemyRainSpawner" || component.GetType().Name == "TemporaryGameOverReporter"))
                component.enabled = false;
    }

    private static DefenseProjectile CreateProjectile()
    {
        var root = new GameObject("DefenseArrow");
        try
        {
            root.AddComponent<SpriteRenderer>().sprite = Sprite("archer-arrow");
            root.GetComponent<SpriteRenderer>().sortingOrder = 8;
            root.AddComponent<DefenseProjectile>();
            return PrefabUtility.SaveAsPrefabAsset(root, PrefabRoot + "Arrow.prefab").GetComponent<DefenseProjectile>();
        }
        finally { Object.DestroyImmediate(root); }
    }

    private static WorkerNPC CreateWorkerVariant(string name, DefenseCombatSettings settings, DefenseProjectile projectile, bool isEnemy)
    {
        Scene preview = EditorSceneManager.NewPreviewScene();
        try
        {
            var root = (GameObject)PrefabUtility.InstantiatePrefab(Require<GameObject>("Assets/Prefab/InGame/" + name + ".prefab"), preview);
            // A dedicated copy permits the pose parent while preserving every original component and child.
            PrefabUtility.UnpackPrefabInstance(root, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            root.name = name;
            WorkerNPC worker = root.GetComponent<WorkerNPC>();
            NPCComponent component = worker.Component;
            DefenseActor actor = root.AddComponent<DefenseActor>();
            Set(worker, "_defenseActor", actor); Set(actor, "_worker", worker); Set(actor, "_settings", settings);
            Set(actor, "_projectilePrefab", projectile);
            CombatTarget target = root.GetComponent<CombatTarget>();
            Collider2D hitCollider;
            if (target)
                hitCollider = target.GetComponent<Collider2D>();
            else
                target = CreateTarget(root.transform, "DefenseHitTarget", root.transform.position, "Friendly", out hitCollider);
            Set(actor, "_combatTarget", target);
            Edit(worker, data =>
            {
                SerializedProperty colliders = data.FindProperty("_gameplayColliders");
                bool exists = false;
                for (int i = 0; i < colliders.arraySize; ++i) exists |= colliders.GetArrayElementAtIndex(i).objectReferenceValue == hitCollider;
                if (hitCollider && !exists) colliders.GetArrayElementAtIndex(colliders.arraySize++).objectReferenceValue = hitCollider;
            });
            Transform visual = root.transform.Find("Visual");
            if (!visual) throw new InvalidOperationException(name + " lacks the original Visual hierarchy.");
            Transform pose = Child(root.transform, "BodyPose").transform;
            visual.SetParent(pose, false);
            Animator animator = root.GetComponent<Animator>();
            if (animator && animator.runtimeAnimatorController)
                ConfigurePoseAnimations(name, animator, component);
            DefenseCombatPresentation presentation = root.AddComponent<DefenseCombatPresentation>();
            Set(actor, "_presentation", presentation); Set(presentation, "_body", pose);
            SpriteRenderer tool = Read<SpriteRenderer>(component, "_toolRenderer");
            if (tool)
            {
                Sprite sword = AssetDatabase.LoadAllAssetsAtPath("Assets/Art/Generated/guard-sword.png")
                    .OfType<Sprite>().Single(sprite => sprite.name == "guard-sword_0");
                Set(presentation, "_weapon", tool.transform.parent); Set(presentation, "_weaponRenderer", tool);
                Set(presentation, "_sword", sword); Set(presentation, "_bow", Sprite("archer-bow"));
                Edit(component, data =>
                {
                    SerializedProperty roles = data.FindProperty("_roleTools");
                    SerializedProperty guard = roles.GetArrayElementAtIndex(roles.arraySize++);
                    guard.FindPropertyRelative("_role").intValue = (int)NPCType.Guard;
                    guard.FindPropertyRelative("_sprite").objectReferenceValue = sword;
                    SerializedProperty role = roles.GetArrayElementAtIndex(roles.arraySize++);
                    role.FindPropertyRelative("_role").intValue = (int)NPCType.Archer;
                    role.FindPropertyRelative("_sprite").objectReferenceValue = Sprite("archer-bow");
                });
            }
            if (!isEnemy) CreateFleeSpeech(root, actor);
            return PrefabUtility.SaveAsPrefabAsset(root, PrefabRoot + (isEnemy ? "DefenseEnemy.prefab" : "DefenseNPC.prefab")).GetComponent<WorkerNPC>();
        }
        finally { EditorSceneManager.ClosePreviewScene(preview); }
    }

    private static void ConfigurePoseAnimations(string name, Animator animator, NPCComponent component)
    {
        var controller = new AnimatorOverrideController(animator.runtimeAnimatorController);
        controller.name = name + "Defense";
        var overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>(); controller.GetOverrides(overrides);
        AnimationClip originalLanding = Read<AnimationClip>(component, "_spawnLandingClip");
        for (int i = 0; i < overrides.Count; ++i)
        {
            AnimationClip source = overrides[i].Key;
            AnimationClip clip = Object.Instantiate(source); clip.name = source.name;
            foreach (EditorCurveBinding original in AnimationUtility.GetCurveBindings(clip))
            {
                if (original.path != "Visual" && !original.path.StartsWith("Visual/", StringComparison.Ordinal)) continue;
                AnimationCurve curve = AnimationUtility.GetEditorCurve(clip, original);
                EditorCurveBinding mapped = original; mapped.path = "BodyPose/" + original.path;
                AnimationUtility.SetEditorCurve(clip, original, null); AnimationUtility.SetEditorCurve(clip, mapped, curve);
            }
            foreach (EditorCurveBinding original in AnimationUtility.GetObjectReferenceCurveBindings(clip))
            {
                if (original.path != "Visual" && !original.path.StartsWith("Visual/", StringComparison.Ordinal)) continue;
                ObjectReferenceKeyframe[] curve = AnimationUtility.GetObjectReferenceCurve(clip, original);
                EditorCurveBinding mapped = original; mapped.path = "BodyPose/" + original.path;
                AnimationUtility.SetObjectReferenceCurve(clip, original, null); AnimationUtility.SetObjectReferenceCurve(clip, mapped, curve);
            }
            AssetDatabase.CreateAsset(clip, AnimationRoot + name + "_" + source.name + ".anim");
            overrides[i] = new KeyValuePair<AnimationClip, AnimationClip>(source, clip);
            if (source == originalLanding) Set(component, "_spawnLandingClip", clip);
        }
        controller.ApplyOverrides(overrides);
        AssetDatabase.CreateAsset(controller, AnimationRoot + name + "Defense.overrideController");
        animator.runtimeAnimatorController = controller;
    }

    private static void CreateFleeSpeech(GameObject root, DefenseActor actor)
    {
        Font font = Require<GameObject>("Assets/Prefab/UI/ConstructionPopup.prefab").GetComponentsInChildren<Text>(true).First(text => text.font).font;
        GameObject panel = Rect("DefensePanicSpeech", root.transform, new Vector2(300f, 70f));
        panel.transform.localPosition = new Vector3(0f, 1.7f, 0f); panel.transform.localScale = Vector3.one * 0.01f;
        Canvas canvas = panel.AddComponent<Canvas>(); canvas.renderMode = RenderMode.WorldSpace; canvas.sortingOrder = 100;
        Image background = panel.AddComponent<Image>();
        background.sprite = Require<Sprite>("Assets/Art/Generated/UI/ui-speech-panel-white-9slice.png");
        background.type = Image.Type.Sliced; background.raycastTarget = false;
        Text text = Label(panel.transform, font, string.Empty, new Vector2(280f, 60f), Vector2.zero); text.fontSize = 25;
        DefenseFleeSpeech speech = root.AddComponent<DefenseFleeSpeech>();
        Set(speech, "_actor", actor); Set(speech, "_panel", panel); Set(speech, "_text", text);
        panel.SetActive(false);
    }

    private static void ConfigureActors(Scene scene, Transform parent, NPCManager npcs, DefenseBattlefield battlefield,
        DefenseCombatSettings settings, DefenseMaintenanceRegistry maintenance)
    {
        DefenseResponsePolicy response = Child(parent, "DefenseResponse").AddComponent<DefenseResponsePolicy>();
        Set(response, "_battlefield", battlefield); Set(response, "_settings", settings);
        GuardActionSelector previous = One<GuardActionSelector>(scene);
        DefenseSoldierActionSelector soldier = Child(previous.transform.parent, "DefenseSoldierSelector").AddComponent<DefenseSoldierActionSelector>();
        foreach (string field in new[] { "dataManager", "actionPool", "_destinationDB", "_decisionTuning", "_navigation", "_wanderCost", "_randomSource" })
            Set(soldier, field, Read<Object>(previous, field));
        SerializedProperty costs = new SerializedObject(One<DataManager>(scene)).FindProperty("_costInfos");
        GuardActionCost guardCost = null;
        for (int i = 0; i < costs.arraySize; ++i)
            if (costs.GetArrayElementAtIndex(i).FindPropertyRelative("actionCost").objectReferenceValue is GuardActionCost value)
                guardCost = value;
        if (!guardCost) throw new InvalidOperationException("Defense soldier needs the existing GuardActionCost.");
        Set(soldier, "_guardCost", guardCost);
        foreach (BaseNPCActionSelector selector in All<BaseNPCActionSelector>(scene)) Set(selector, "defenseResponse", response);
        foreach (BuilderActionSelector builder in All<BuilderActionSelector>(scene)) Set(builder, "_maintenance", maintenance);
        previous.enabled = false;
        Set(npcs, "_defenseBattlefield", battlefield);
        GuardStatDefinition guard = CopyAsset<GuardStatDefinition>("Assets/Data/ScriptableObject/GuardStatDefinition.asset", "GuardStat.asset");
        GuardStatDefinition archer = CopyAsset<GuardStatDefinition>("Assets/Data/ScriptableObject/GuardStatDefinition.asset", "ArcherStat.asset");
        Edit(archer, data =>
        {
            data.FindProperty("_name").stringValue = "Archer";
            data.FindProperty("_health").floatValue = 80f; data.FindProperty("_healthMax").floatValue = 80f;
            data.FindProperty("_attackPower").floatValue = 10f; data.FindProperty("_attackRange").floatValue = 6f;
            data.FindProperty("_attackSpeed").floatValue = 1f / 1.5f;
        });
        AssetDatabase.SaveAssetIfDirty(archer);
        Edit(npcs, data =>
        {
            SerializedProperty entries = data.FindProperty("_creationEntries");
            for (int i = 0; i < entries.arraySize; ++i)
            {
                SerializedProperty entry = entries.GetArrayElementAtIndex(i);
                if (entry.FindPropertyRelative("_npcType").intValue != (int)NPCType.Guard) continue;
                entry.FindPropertyRelative("_selector").objectReferenceValue = soldier;
                entry.FindPropertyRelative("_statDefinition").objectReferenceValue = guard;
            }
            SerializedProperty archerEntry = entries.GetArrayElementAtIndex(entries.arraySize++);
            archerEntry.FindPropertyRelative("_npcType").intValue = (int)NPCType.Archer;
            archerEntry.FindPropertyRelative("_selector").objectReferenceValue = soldier;
            archerEntry.FindPropertyRelative("_statDefinition").objectReferenceValue = archer;
        });
        DefenseInitialRoster roster = Child(parent, "InitialDefenseRoster").AddComponent<DefenseInitialRoster>();
        Set(roster, "_npcManager", npcs);
        SetArray(roster, "_guardSpawns", new[] { Anchor(roster.transform, "Guard1", new Vector3(4.5f, 12.5f)),
            Anchor(roster.transform, "Guard2", new Vector3(40.5f, 12.5f)) });
        SetArray(roster, "_archerSpawns", new[] { Anchor(roster.transform, "Archer1", new Vector3(-12.5f, 13.5f)),
            Anchor(roster.transform, "Archer2", new Vector3(-3.5f, 13.5f)) });
    }

    private static void ConfigureRecruitment(Scene scene, DefenseBattlefield battlefield)
    {
        TownHallRecruitment recruitment = One<TownHallRecruitment>(scene);
        Edit(recruitment, data =>
        {
            SerializedProperty settings = data.FindProperty("_recruitments");
            int guardIndex = -1;
            for (int i = 0; i < settings.arraySize; ++i)
                if (settings.GetArrayElementAtIndex(i).FindPropertyRelative("_npcType").intValue == (int)NPCType.Guard) guardIndex = i;
            if (guardIndex < 0) throw new InvalidOperationException("Town hall has no Guard recruitment settings to copy.");
            int index = settings.arraySize++;
            SerializedProperty guard = settings.GetArrayElementAtIndex(guardIndex);
            SerializedProperty archer = settings.GetArrayElementAtIndex(index);
            archer.FindPropertyRelative("_npcType").intValue = (int)NPCType.Archer;
            archer.FindPropertyRelative("_cooldownDuration").floatValue = guard.FindPropertyRelative("_cooldownDuration").floatValue;
            archer.FindPropertyRelative("_settlementCost").intValue = guard.FindPropertyRelative("_settlementCost").intValue;
        });
        TownHallPopup popup = One<TownHallPopup>(scene);
        var data = new SerializedObject(popup); SerializedProperty cards = data.FindProperty("_cards");
        TownHallRecruitCard guardCard = null;
        for (int i = 0; i < cards.arraySize; ++i)
        {
            var card = (TownHallRecruitCard)cards.GetArrayElementAtIndex(i).objectReferenceValue;
            if (card && card.NpcType == NPCType.Guard) guardCard = card;
        }
        if (!guardCard) throw new InvalidOperationException("Town hall popup has no Guard card to copy.");
        TownHallRecruitCard archerCard = Object.Instantiate(guardCard, guardCard.transform.parent);
        archerCard.name = "ArcherCard"; Edit(archerCard, card => card.FindProperty("_npcType").intValue = (int)NPCType.Archer);
        cards.GetArrayElementAtIndex(cards.arraySize++).objectReferenceValue = archerCard;
        data.ApplyModifiedPropertiesWithoutUndo();
        // Existing three manually placed cards become four evenly spaced cards within the same panel.
        var allCards = new List<TownHallRecruitCard>();
        for (int i = 0; i < cards.arraySize; ++i) allCards.Add((TownHallRecruitCard)cards.GetArrayElementAtIndex(i).objectReferenceValue);
        float step = 275f;
        for (int i = 0; i < allCards.Count; ++i)
        {
            RectTransform rect = (RectTransform)allCards[i].transform;
            rect.localScale = Vector3.one * 0.75f;
            rect.anchoredPosition = new Vector2((i - (allCards.Count - 1) * 0.5f) * step, rect.anchoredPosition.y);
        }
    }

    private static void ConfigureBuildings(Scene scene, DefenseBattlefield battlefield, DefenseMaintenanceRegistry registry,
        ResourceManager resources, DefenseDurabilitySettings settings, DefenseGameSession session)
    {
        BuildingFactory factory = One<BuildingFactory>(scene);
        DestinationDB destinations = One<DestinationDB>(scene);
        BuildingDataContext source = Read<BuildingDataContext>(factory, "_buildingDataContext");
        BuildingDataContext catalog = CopyAsset<BuildingDataContext>(AssetDatabase.GetAssetPath(source), "BuildingDataContext.asset");
        var variants = new Dictionary<CompletedBuildingFacility, CompletedBuildingFacility>();
        Edit(catalog, data =>
        {
            SerializedProperty assets = data.FindProperty("_assets");
            for (int i = 0; i < assets.arraySize; ++i)
            {
                SerializedProperty entry = assets.GetArrayElementAtIndex(i);
                var original = (CompletedBuildingFacility)entry.FindPropertyRelative("_prefab").objectReferenceValue;
                if (!original) throw new InvalidOperationException("Missing source completed facility prefab.");
                if (!variants.TryGetValue(original, out CompletedBuildingFacility variant))
                {
                    variant = CreateBuildingVariant(original, settings);
                    variants.Add(original, variant);
                }
                entry.FindPropertyRelative("_prefab").objectReferenceValue = variant;
            }
        });
        Set(factory, "_buildingDataContext", catalog); Set(One<DataManager>(scene), "_buildingDataContext", catalog);
        Set(factory, "_defenseBattlefield", battlefield); Set(factory, "_defenseMaintenance", registry);
        Set(factory, "_defenseSettings", settings); Set(factory, "_defenseSession", session);
        // Housing definition lookup must share the same defense building catalog as its factory.
        HousingDataContext housingSource = Read<HousingDataContext>(factory, "_housingDataContext");
        if (housingSource)
        {
            HousingDataContext housing = CopyAsset<HousingDataContext>(AssetDatabase.GetAssetPath(housingSource), "HousingDataContext.asset");
            Set(housing, "_buildings", catalog); Set(factory, "_housingDataContext", housing);
            Set(One<HousingManager>(scene), "_data", housing);
            foreach (HousePopup popup in All<HousePopup>(scene)) Set(popup, "_housingDataContext", housing);
            AssetDatabase.SaveAssetIfDirty(housing);
        }
        AssetDatabase.SaveAssetIfDirty(catalog);
        BuildingPlot template = All<BuildingPlot>(scene).OrderBy(plot => plot.name, StringComparer.Ordinal).First();
        SerializedProperty entries = new SerializedObject(destinations).FindProperty("_destionations");
        var handled = new HashSet<GameObject>();
        for (int i = 0; i < entries.arraySize; ++i)
        {
            SerializedProperty entry = entries.GetArrayElementAtIndex(i);
            var type = (BuildingType)entry.FindPropertyRelative("BuildingType").intValue;
            var sourceObject = (GameObject)entry.FindPropertyRelative("DestinationObject").objectReferenceValue;
            var entrance = (Transform)entry.FindPropertyRelative("DestinationLoc").objectReferenceValue;
            if (!sourceObject || !entrance) throw new InvalidOperationException("Missing static facility destination.");
            GameObject root = PrefabUtility.GetOutermostPrefabInstanceRoot(sourceObject);
            if (!root) root = sourceObject;
            if (!handled.Add(root)) continue;
            if (PrefabUtility.IsPartOfPrefabInstance(root))
                PrefabUtility.UnpackPrefabInstance(root, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            CompletedBuildingFacility facility = root.GetComponent<CompletedBuildingFacility>();
            if (!facility) facility = root.AddComponent<CompletedBuildingFacility>();
            Set(facility, "_entrance", entrance);
            BaseInteractionProvider provider = sourceObject.GetComponent<BaseInteractionProvider>();
            Set(facility, "_provider", provider);
            Set(facility, "_warehouse", root.GetComponentInChildren<WarehouseDepositPoint>(true));
            Set(facility, "_farm", root.GetComponentInChildren<FarmWorkSite>(true));
            Set(facility, "_seedSource", root.GetComponentInChildren<FarmSeedSource>(true));
            Set(facility, "_guardPost", root.GetComponentInChildren<GuardPost>(true));
            Set(facility, "_pub", root.GetComponentInChildren<Pub>(true));
            BuildingPlot plot = Object.Instantiate(template, template.transform.parent);
            plot.name = "DefensePlot_" + type;
            plot.transform.position += entrance.position - Read<Transform>(plot, "_entrance").position;
            root.transform.SetParent(plot.transform, true);
            Set(plot, "_initialFacility", facility);
            // The static navigation footprint belongs to the permanent plot after rubble is cleared.
            PreserveNavigationGeometry(One<TilemapNavigation>(scene), root.transform, plot.transform);
            DefenseBuildingDurability durability = AddDurability(root, facility, entrance, settings, false);
            durability.Configure(battlefield, registry, resources, settings, session, plot, facility, destinations);
        }
        SetArray(One<BuildingPlotRegistry>(scene), "_plots", All<BuildingPlot>(scene));
        TownHallRecruitment townHall = One<TownHallRecruitment>(scene);
        GameObject townRoot = PrefabUtility.GetOutermostPrefabInstanceRoot(townHall.gameObject);
        if (!townRoot) townRoot = townHall.gameObject;
        Vector2 landing = new SerializedObject(townHall).FindProperty("_landingPoint").vector2Value;
        Transform townApproach = Anchor(townRoot.transform, "DefenseWorkAnchor", townHall.transform.position + (Vector3)landing);
        DefenseBuildingDurability town = AddDurability(townRoot, null, townApproach, settings, true);
        town.Configure(battlefield, registry, resources, settings, session, null, null, destinations);
    }

    private static CompletedBuildingFacility CreateBuildingVariant(CompletedBuildingFacility source, DefenseDurabilitySettings settings)
    {
        Scene preview = EditorSceneManager.NewPreviewScene();
        try
        {
            var root = (GameObject)PrefabUtility.InstantiatePrefab(source.gameObject, preview);
            CompletedBuildingFacility facility = root.GetComponent<CompletedBuildingFacility>();
            AddDurability(root, facility, facility.Entrance, settings, false);
            foreach (MonoBehaviour component in root.GetComponentsInChildren<MonoBehaviour>(true))
                if (component && component.GetType().Name == "TemporaryGameOverReporter") component.enabled = false;
            return PrefabUtility.SaveAsPrefabAsset(root, PrefabRoot + source.name + "Defense.prefab").GetComponent<CompletedBuildingFacility>();
        }
        finally { EditorSceneManager.ClosePreviewScene(preview); }
    }

    private static DefenseBuildingDurability AddDurability(GameObject root, CompletedBuildingFacility facility,
        Transform workAnchor, DefenseDurabilitySettings settings, bool isTownHall)
    {
        if (!workAnchor) throw new InvalidOperationException(root.name + " has no reachable entrance anchor.");
        Behaviour[] functionality = root.GetComponentsInChildren<Behaviour>(true)
            .Where(behaviour => !(behaviour is CombatTarget) && !(behaviour is NavigationObstacle2D)).ToArray();
        Collider2D[] functionalColliders = root.GetComponentsInChildren<Collider2D>(true);
        SpriteRenderer[] visuals = root.GetComponentsInChildren<SpriteRenderer>(true);
        DefenseBuildingDurability durability = root.AddComponent<DefenseBuildingDurability>();
        DefenseMaintenanceSite maintenance = root.AddComponent<DefenseMaintenanceSite>();
        CombatTarget target = CreateTarget(root.transform, "DefenseHitTarget", workAnchor.position, "Friendly", out _);
        GameObject rubble = Child(root.transform, "DefenseRubble");
        SpriteRenderer rubbleRenderer = rubble.AddComponent<SpriteRenderer>(); rubbleRenderer.sprite = Sprite("building-rubble");
        rubbleRenderer.sortingOrder = 3; rubble.transform.position = workAnchor.position + Vector3.up * 1.4f;
        rubble.SetActive(false);
        Set(durability, "_settings", settings); Set(durability, "_facility", facility); Set(durability, "_target", target);
        Set(durability, "_maintenance", maintenance); Set(durability, "_workAnchor", workAnchor); Set(durability, "_rubbleVisual", rubble);
        Edit(durability, data => data.FindProperty("_isTownHall").boolValue = isTownHall);
        SetArray(durability, "_functionalBehaviours", functionality); SetArray(durability, "_functionalColliders", functionalColliders);
        SetArray(durability, "_visuals", visuals); Set(maintenance, "_settings", settings);
        Set(maintenance, "_building", durability); Set(maintenance, "_workAnchor", workAnchor);
        foreach (GuardPost post in root.GetComponentsInChildren<GuardPost>(true))
        {
            Set(post, "_defenseDurability", durability);
            Set(post, "_combatTarget", target);
        }
        return durability;
    }

    private static void PreserveNavigationGeometry(TilemapNavigation navigation, Transform facility, Transform plot)
    {
        var transforms = new HashSet<Transform>();
        var data = new SerializedObject(navigation);
        SerializedProperty blocked = data.FindProperty("_blockedAreas");
        for (int i = 0; i < blocked.arraySize; ++i)
        {
            var bounds = blocked.GetArrayElementAtIndex(i).objectReferenceValue as BoxCollider2D;
            if (bounds && bounds.transform != facility && bounds.transform.IsChildOf(facility)) transforms.Add(bounds.transform);
        }
        SerializedProperty areas = data.FindProperty("_localAreas");
        for (int i = 0; i < areas.arraySize; ++i)
        {
            SerializedProperty area = areas.GetArrayElementAtIndex(i);
            var bounds = area.FindPropertyRelative("_bounds").objectReferenceValue as BoxCollider2D;
            var entrance = area.FindPropertyRelative("_entrance").objectReferenceValue as Transform;
            if (bounds && bounds.transform != facility && bounds.transform.IsChildOf(facility)) transforms.Add(bounds.transform);
            if (entrance && entrance != facility && entrance.IsChildOf(facility)) transforms.Add(entrance);
        }
        foreach (Transform transform in transforms)
        {
            // Only move geometry-only children; provider/click owners must retain their native hierarchy.
            if (transform.GetComponents<MonoBehaviour>().Length == 0) transform.SetParent(plot, true);
        }
    }

    private static GameObject Child(Transform parent, string name)
    { var go = new GameObject(name); go.transform.SetParent(parent, false); return go; }
    private static Transform Anchor(Transform parent, string name, Vector3 position)
    { Transform anchor = Child(parent, name).transform; anchor.position = position; return anchor; }
    private static BoxCollider2D Bounds(Transform parent, string name, Vector2 center, Vector2 size)
    {
        var collider = Child(parent, name).AddComponent<BoxCollider2D>(); collider.transform.position = center;
        collider.isTrigger = true; collider.size = size; collider.enabled = false; return collider;
    }
    private static CombatTarget CreateTarget(Transform parent, string name, Vector3 position, string layerName, out Collider2D collider)
    {
        GameObject go = Child(parent, name); go.transform.position = position;
        int layer = LayerMask.NameToLayer(layerName);
        if (layer < 0) throw new InvalidOperationException("Missing combat layer " + layerName);
        go.layer = layer;
        CircleCollider2D circle = go.AddComponent<CircleCollider2D>(); circle.isTrigger = true; circle.radius = 0.4f;
        collider = circle; CombatTarget target = go.AddComponent<CombatTarget>(); Set(target, "_targetPoint", go.transform); return target;
    }
    private static GameObject Rect(string name, Transform parent, Vector2 size, Vector2 position = default)
    {
        var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false);
        var rect = (RectTransform)go.transform; rect.sizeDelta = size; rect.anchoredPosition = position; return go;
    }
    private static void Stretch(RectTransform rect)
    { rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero; }
    private static Text Label(Transform parent, Font font, string caption, Vector2 size, Vector2 position)
    {
        Text text = Rect("Label", parent, size, position).AddComponent<Text>(); text.font = font; text.text = caption;
        text.fontSize = 21; text.color = new Color(0.22f, 0.13f, 0.06f); text.raycastTarget = false;
        text.alignment = TextAnchor.MiddleCenter; return text;
    }
    private static Button Button(Transform parent, Font font, string caption, Vector2 size, Vector2 position, bool affirmative)
    {
        GameObject go = Rect(caption, parent, size, position); Image image = go.AddComponent<Image>();
        image.color = affirmative ? new Color(0.36f, 0.53f, 0.3f) : new Color(0.55f, 0.31f, 0.23f);
        Button button = go.AddComponent<Button>(); button.targetGraphic = image;
        Label(go.transform, font, caption, size - new Vector2(12f, 4f), Vector2.zero).color = Color.white; return button;
    }
    private static void CopyImage(Image target, Image source)
    { target.sprite = source.sprite; target.type = source.type; target.color = source.color; }
    private static Sprite Sprite(string name) => Require<Sprite>(ArtRoot + name + ".png");
    private static T Require<T>(string path) where T : Object
        => AssetDatabase.LoadAssetAtPath<T>(path) ? AssetDatabase.LoadAssetAtPath<T>(path)
            : throw new InvalidOperationException("Missing defense input: " + path);
    private static T NewAsset<T>(string file) where T : ScriptableObject
    {
        string path = DataRoot + file;
        if (File.Exists(path)) throw new InvalidOperationException("Refusing to replace existing defense data: " + path);
        T asset = ScriptableObject.CreateInstance<T>(); AssetDatabase.CreateAsset(asset, path); return asset;
    }
    private static T CopyAsset<T>(string source, string file) where T : Object
    {
        string path = DataRoot + file;
        if (File.Exists(path) || !AssetDatabase.CopyAsset(source, path))
            throw new InvalidOperationException("Could not create defense data variant: " + path);
        return Require<T>(path);
    }
    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/'); EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }
    private static T[] All<T>(Scene scene) where T : Component
        => scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<T>(true)).ToArray();
    private static T One<T>(Scene scene) where T : Component => All<T>(scene).Single();
    private static void Edit(Object owner, Action<SerializedObject> edit)
    { var data = new SerializedObject(owner); edit(data); data.ApplyModifiedPropertiesWithoutUndo(); }
    private static void Set(Object owner, string field, Object value)
        => Edit(owner, data => data.FindProperty(field).objectReferenceValue = value);
    private static T Read<T>(Object owner, string field) where T : Object
        => (T)new SerializedObject(owner).FindProperty(field).objectReferenceValue;
    private static void SetArray<T>(Object owner, string field, T[] values) where T : Object
        => Edit(owner, data => { SerializedProperty array = data.FindProperty(field); array.arraySize = values.Length;
            for (int i = 0; i < values.Length; ++i) array.GetArrayElementAtIndex(i).objectReferenceValue = values[i]; });
}
