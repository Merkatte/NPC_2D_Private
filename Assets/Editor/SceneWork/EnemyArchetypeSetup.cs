using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

// Construction only: creates three independent prefab packages without saving scenes or waves.
public static class EnemyArchetypeSetup
{
    private const string ArtFolder = "Assets/Art/Generated/Defense/Enemies/";
    private const string PrefabFolder = "Assets/Prefab/Defense/Enemies";
    private const string DefinitionFolder = "Assets/Data/ScriptableObject/Defense/Enemies";
    private const string EnemySource = "Assets/Prefab/Defense/DefenseEnemy.prefab";
    private const string ArrowSource = "Assets/Prefab/Defense/Arrow.prefab";
    private const string EffectsFolder = PrefabFolder + "/Effects";
    private const string ParticleShader = "Universal Render Pipeline/Particles/Unlit";
    private static readonly string[] _names = { "DefenseGoblinArcher", "DefenseShaman", "DefenseSalamander" };

    [MenuItem("Tools/NPC/Defense/Create Enemy Archetype Prefabs")]
    public static void Setup()
    {
        Create(ArtFolder + "GoblinArcher.png", ArtFolder + "SkullShaman.png", ArtFolder + "FireSalamander.png",
            ArtFolder + "EnergyBall.png", ArtFolder + "FireProjectile.png");
    }

    [MenuItem("Tools/NPC/Defense/Add Enemy Windup Particles")]
    public static void AddWindupParticles()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
            throw new InvalidOperationException("Windup particle assembly requires an idle Editor outside Play Mode.");
        RequireLocalParticleAuthoring();
        string shamanPath = PrefabFolder + "/DefenseShaman.prefab";
        string salamanderPath = PrefabFolder + "/DefenseSalamander.prefab";
        RequireParticleTarget(shamanPath);
        RequireParticleTarget(salamanderPath);
        Texture2D energy = Require<Texture2D>(ArtFolder + "EnergyBall.png");
        Texture2D fire = Require<Texture2D>(ArtFolder + "FireProjectile.png");
        Shader shader = Shader.Find(ParticleShader);
        if (!shader || ShaderUtil.ShaderHasError(shader))
            throw new InvalidOperationException("Windup particles require an error-free URP Particles/Unlit shader.");
        RequireParticleMaterial(EffectsFolder + "/ShamanGather.mat", shader);
        RequireParticleMaterial(EffectsFolder + "/SalamanderInhale.mat", shader);
        EnsureFolder(EffectsFolder);
        Material shamanMaterial = CreateParticleMaterial("ShamanGather", shader, energy);
        Material salamanderMaterial = CreateParticleMaterial("SalamanderInhale", shader, fire);
        AddWindupParticlesToPrefab(shamanPath, shamanMaterial, true);
        AddWindupParticlesToPrefab(salamanderPath, salamanderMaterial, false);
        Debug.Log("Added inward windup particles to DefenseShaman and DefenseSalamander. No scenes, waves, stats or textures were changed.");
    }

    [MenuItem("Tools/NPC/Defense/Extract Enemy Particle Prefabs")]
    public static void ExtractWindupParticlePrefabs()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
            throw new InvalidOperationException("Particle prefab extraction requires an idle Editor outside Play Mode.");
        string shamanPath = PrefabFolder + "/DefenseShaman.prefab";
        string salamanderPath = PrefabFolder + "/DefenseSalamander.prefab";
        string shamanEffectPath = EffectsFolder + "/ShamanGather.prefab";
        string salamanderEffectPath = EffectsFolder + "/SalamanderInhale.prefab";
        RequireUnused(shamanEffectPath);
        RequireUnused(salamanderEffectPath);
        RequireParticleExtractionSource(shamanPath);
        RequireParticleExtractionSource(salamanderPath);
        ExtractParticlePrefab(shamanPath, shamanEffectPath);
        ExtractParticlePrefab(salamanderPath, salamanderEffectPath);
        Debug.Log("Extracted ShamanGather and SalamanderInhale as nested particle prefabs. Existing particle settings and materials were preserved.");
    }

    private static void RequireLocalParticleAuthoring()
    {
        string[] managedPaths = { EffectsFolder + "/ShamanGather.prefab", EffectsFolder + "/SalamanderInhale.prefab" };
        foreach (string path in managedPaths)
            if (File.Exists(path) || File.Exists(path + ".meta") || AssetDatabase.LoadMainAssetAtPath(path))
                throw new InvalidOperationException("Windup particles are managed by effect prefabs. Edit those prefabs directly; Add Enemy Windup Particles will not reset their settings.");
    }

    private static void RequireParticleExtractionSource(string path)
    {
        RequireParticleTarget(path);
        GameObject prefab = Require<GameObject>(path);
        Transform child = prefab.transform.Find("ProjectileOrigin/WindupParticles");
        if (!child || PrefabUtility.IsPartOfPrefabInstance(child.gameObject) ||
            !child.GetComponent<ParticleSystem>() || !child.GetComponent<ParticleSystemRenderer>())
            throw new InvalidOperationException("Particle extraction requires the current local WindupParticles object: " + path);
        foreach (Component component in prefab.GetComponentsInChildren<Component>(true))
        {
            if (!component || EditorUtility.IsDirty(component) || EditorUtility.IsDirty(component.gameObject))
                throw new InvalidOperationException("Preserving missing components or unsaved enemy prefab edits: " + path);
        }
    }

    private static void ExtractParticlePrefab(string actorPath, string effectPath)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(actorPath);
        try
        {
            DefenseCombatPresentation presentation = root.GetComponent<DefenseCombatPresentation>();
            Transform origin = root.transform.Find("ProjectileOrigin");
            Transform source = origin.Find("WindupParticles");
            string childName = source.name;
            int siblingIndex = source.GetSiblingIndex();
            Vector3 position = source.localPosition;
            Quaternion rotation = source.localRotation;
            Vector3 scale = source.localScale;
            // Save the actual authored components. Reconfiguring modules here would discard tuning.
            source.name = Path.GetFileNameWithoutExtension(effectPath);
            GameObject effect = PrefabUtility.SaveAsPrefabAsset(source.gameObject, effectPath);
            if (!effect) throw new InvalidOperationException("Could not extract particle prefab: " + effectPath);
            GameObject nested = PrefabUtility.InstantiatePrefab(effect, root.scene) as GameObject;
            if (!nested) throw new InvalidOperationException("Could not instantiate particle prefab: " + effectPath);
            nested.transform.SetParent(origin, false);
            nested.name = childName;
            nested.transform.localPosition = position;
            nested.transform.localRotation = rotation;
            nested.transform.localScale = scale;
            nested.transform.SetSiblingIndex(siblingIndex);
            // Only instance name/placement may differ. Particle modules and renderer stay inherited.
            PrefabUtility.RecordPrefabInstancePropertyModifications(nested);
            PrefabUtility.RecordPrefabInstancePropertyModifications(nested.transform);
            SerializedObject presentationData = new SerializedObject(presentation);
            presentationData.FindProperty("_windupParticles").objectReferenceValue = nested.GetComponent<ParticleSystem>();
            presentationData.ApplyModifiedPropertiesWithoutUndo();
            Object.DestroyImmediate(source.gameObject);
            if (!PrefabUtility.SaveAsPrefabAsset(root, actorPath))
                throw new InvalidOperationException("Could not save nested particle reference: " + actorPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    private static void RequireParticleTarget(string path)
    {
        PrefabStage stage = PrefabStageUtility.GetCurrentPrefabStage();
        if (stage && stage.assetPath == path)
            throw new InvalidOperationException("Close the target Prefab Stage before adding windup particles: " + path);
        GameObject prefab = Require<GameObject>(path);
        DefenseCombatPresentation presentation = prefab.GetComponent<DefenseCombatPresentation>();
        Transform origin = prefab.transform.Find("ProjectileOrigin");
        if (!presentation || !origin || !prefab.transform.Find("BodyPose/Visual/Image"))
            throw new InvalidOperationException("Enemy prefab requires its existing presentation and visual/origin hierarchy: " + path);
        if (EditorUtility.IsDirty(prefab) || EditorUtility.IsDirty(presentation))
            throw new InvalidOperationException("Preserving unsaved enemy prefab edits: " + path);
        Transform child = origin.Find("WindupParticles");
        ParticleSystem assigned = new SerializedObject(presentation).FindProperty("_windupParticles").objectReferenceValue as ParticleSystem;
        if (child)
        {
            ParticleSystem particles = child.GetComponent<ParticleSystem>();
            if (!particles || assigned != particles || child.childCount != 0 || child.GetComponents<Component>().Length != 3 ||
                PrefabUtility.IsPartOfPrefabInstance(child.gameObject))
                throw new InvalidOperationException("Preserving unrelated WindupParticles child: " + path);
        }
        else if (assigned)
            throw new InvalidOperationException("Preserving a different assigned windup particle system: " + path);
    }

    private static void RequireParticleMaterial(string path, Shader shader)
    {
        Object existing = AssetDatabase.LoadMainAssetAtPath(path);
        if (!existing)
        {
            RequireUnused(path);
            return;
        }
        if (!(existing is Material material) || material.shader != shader || EditorUtility.IsDirty(material))
            throw new InvalidOperationException("Preserving unrelated or unsaved particle material: " + path);
    }

    private static Material CreateParticleMaterial(string name, Shader shader, Texture2D texture)
    {
        string path = EffectsFolder + "/" + name + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        bool isNew = !material;
        if (isNew) material = new Material(shader) { name = name };
        material.SetTexture("_BaseMap", texture);
        material.SetColor("_BaseColor", Color.white);
        material.SetFloat("_Surface", 1f);
        material.SetFloat("_Blend", 0f);
        material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        material.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
        material.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
        material.SetFloat("_ZWrite", 0f);
        material.SetFloat("_Cull", (float)CullMode.Off);
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.SetOverrideTag("RenderType", "Transparent");
        material.renderQueue = (int)RenderQueue.Transparent;
        material.SetShaderPassEnabled("DepthOnly", false);
        material.SetShaderPassEnabled("DepthNormalsOnly", false);
        if (isNew) AssetDatabase.CreateAsset(material, path);
        else EditorUtility.SetDirty(material);
        AssetDatabase.SaveAssetIfDirty(material);
        return material;
    }

    private static void AddWindupParticlesToPrefab(string path, Material material, bool isShaman)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            DefenseCombatPresentation presentation = root.GetComponent<DefenseCombatPresentation>();
            Transform origin = root.transform.Find("ProjectileOrigin");
            SpriteRenderer bodyRenderer = root.transform.Find("BodyPose/Visual/Image").GetComponent<SpriteRenderer>();
            Transform child = origin.Find("WindupParticles");
            if (!child)
            {
                child = new GameObject("WindupParticles").transform;
                child.gameObject.layer = root.layer;
                child.SetParent(origin, false);
                child.gameObject.AddComponent<ParticleSystem>();
            }
            ParticleSystem particles = child.GetComponent<ParticleSystem>();
            ConfigureWindupParticles(particles, material, bodyRenderer, isShaman);
            SerializedObject presentationData = new SerializedObject(presentation);
            presentationData.FindProperty("_windupParticles").objectReferenceValue = particles;
            presentationData.ApplyModifiedPropertiesWithoutUndo();
            if (!PrefabUtility.SaveAsPrefabAsset(root, path))
                throw new InvalidOperationException("Could not save windup particles to " + path);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    private static void ConfigureWindupParticles(ParticleSystem particles, Material material,
        SpriteRenderer bodyRenderer, bool isShaman)
    {
        particles.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
        particles.useAutoRandomSeed = false;
        particles.randomSeed = isShaman ? 113u : 227u;
        ParticleSystem.MainModule main = particles.main;
        main.playOnAwake = false;
        main.loop = true;
        main.prewarm = false;
        main.duration = 1f;
        main.startLifetime = isShaman ? new ParticleSystem.MinMaxCurve(0.34f, 0.43f) : new ParticleSystem.MinMaxCurve(0.36f, 0.4f);
        main.startSpeed = 0f;
        main.startSize = isShaman ? new ParticleSystem.MinMaxCurve(0.1f, 0.18f) : new ParticleSystem.MinMaxCurve(0.1f, 0.16f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.startColor = isShaman ? new Color(0.6f, 1f, 1f, 0.9f) : new Color(1f, 0.9f, 0.7f, 0.75f);
        main.gravityModifier = 0f;
        main.maxParticles = isShaman ? 40 : 32;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;
        main.useUnscaledTime = false;
        main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;
        main.stopAction = ParticleSystemStopAction.None;
        ParticleSystem.EmissionModule emission = particles.emission;
        emission.enabled = true;
        emission.rateOverTime = isShaman ? 45f : 32f;
        emission.rateOverDistance = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)(isShaman ? 8 : 6)) });
        ParticleSystem.ShapeModule shape = particles.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = isShaman ? 0.5f : 0.55f;
        shape.radiusThickness = 0f;
        shape.arc = isShaman ? 360f : 120f;
        shape.arcMode = ParticleSystemShapeMultiModeValue.Random;
        // Native facing is LEFT. The inhale arc starts ahead of the mouth and mirrors with the actor root.
        shape.rotation = isShaman ? Vector3.zero : new Vector3(0f, 0f, 120f);
        ParticleSystem.VelocityOverLifetimeModule velocity = particles.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.Local;
        velocity.radial = isShaman ? -1.1f : -1.35f;
        ParticleSystem.ColorOverLifetimeModule color = particles.colorOverLifetime;
        color.enabled = true;
        Gradient fade = new Gradient();
        fade.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.15f), new GradientAlphaKey(0f, 1f) });
        color.color = fade;
        ParticleSystem.SizeOverLifetimeModule size = particles.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
            new Keyframe(0f, 0.5f), new Keyframe(0.2f, 1f), new Keyframe(1f, 0.15f)));
        ParticleSystemRenderer renderer = particles.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = material;
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.alignment = ParticleSystemRenderSpace.View;
        renderer.sortingLayerID = bodyRenderer.sortingLayerID;
        renderer.sortingOrder = bodyRenderer.sortingOrder + 2;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.lightProbeUsage = LightProbeUsage.Off;
        renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
    }

    public static void Create(string goblinPath, string shamanPath, string salamanderPath,
        string energyOrbPath, string fireballPath, bool goblinFacesRight = true,
        bool shamanFacesRight = true, bool salamanderFacesRight = true)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
            throw new InvalidOperationException("Enemy archetypes require an idle Editor outside Play Mode, compilation and asset updates.");
        Require<GameObject>(EnemySource);
        DefenseProjectile arrow = Require<GameObject>(ArrowSource).GetComponent<DefenseProjectile>();
        if (!arrow) throw new InvalidOperationException("Arrow source requires DefenseProjectile.");
        EnemyStatDefinition sourceStat = Require<EnemyStatDefinition>("Assets/Data/ScriptableObject/Defense/EnemyStat.asset");
        string[] inputPaths = { goblinPath, shamanPath, salamanderPath, energyOrbPath, fireballPath };
        foreach (string path in inputPaths)
        {
            if (string.IsNullOrEmpty(path) || !path.StartsWith(ArtFolder, StringComparison.Ordinal) ||
                path.Contains("..") || !path.EndsWith(".png", StringComparison.OrdinalIgnoreCase) || !File.Exists(path))
                throw new InvalidOperationException("Enemy artwork must be an existing PNG inside " + ArtFolder);
        }
        for (int i = 0; i < inputPaths.Length; i++)
            for (int j = i + 1; j < inputPaths.Length; j++)
                if (inputPaths[i] == inputPaths[j]) throw new InvalidOperationException("Each enemy/effect needs its own art path.");
        foreach (string name in _names)
        {
            RequireUnused(PrefabFolder + "/" + name + ".prefab");
            RequireUnused(DefinitionFolder + "/" + name + "Attack.asset");
            RequireUnused(DefinitionFolder + "/" + name + "Stat.asset");
        }
        RequireUnused(PrefabFolder + "/Projectiles/EnergyOrb.prefab");
        RequireUnused(PrefabFolder + "/Projectiles/Fireball.prefab");

        Sprite goblin = ImportSprite(goblinPath, 1.2f, true);
        Sprite shaman = ImportSprite(shamanPath, 1.2f, true);
        Sprite salamander = ImportSprite(salamanderPath, 0.7f, true);
        Sprite energyOrb = ImportSprite(energyOrbPath, 0.28f, false);
        Sprite fireball = ImportSprite(fireballPath, 0.22f, false);
        EnsureFolder(PrefabFolder + "/Projectiles");
        EnsureFolder(DefinitionFolder);
        DefenseProjectile orbPrefab = CreateProjectile("EnergyOrb", energyOrb);
        DefenseProjectile firePrefab = CreateProjectile("Fireball", fireball);
        CreateEnemy(0, goblin, goblinFacesRight, 1.2f, new Vector3(-0.44f, 0.05f, 0f), arrow, null,
            sourceStat, 4.5f, 0.45f, 0.55f, 8f);
        CreateEnemy(1, shaman, shamanFacesRight, 1.2f, new Vector3(-0.50f, 0.02f, 0f), orbPrefab, energyOrb,
            sourceStat, 4f, 0.8f, 0.6f, 6f);
        CreateEnemy(2, salamander, salamanderFacesRight, 0.7f, new Vector3(-0.53f, -0.1f, 0f), firePrefab, null,
            sourceStat, 3f, 1f, 0.65f, 7f);
        Debug.Log("Created DefenseGoblinArcher, DefenseShaman and DefenseSalamander prefab/stat/attack packages. Scenes and waves were not saved or changed.");
    }

    private static Sprite ImportSprite(string path, float visibleHeight, bool feetPivot)
    {
        Texture2D decoded = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        int minX, minY, maxX, maxY, width, height;
        try
        {
            if (!ImageConversion.LoadImage(decoded, File.ReadAllBytes(path)))
                throw new InvalidOperationException("Cannot decode " + path);
            width = decoded.width; height = decoded.height;
            if (width > 8192 || height > 8192) throw new InvalidOperationException("Enemy sprite exceeds supported texture size: " + path);
            minX = width; minY = height; maxX = -1; maxY = -1;
            Color32[] pixels = decoded.GetPixels32();
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                {
                    if (pixels[y * width + x].a <= 15) continue;
                    minX = Mathf.Min(minX, x); minY = Mathf.Min(minY, y);
                    maxX = Mathf.Max(maxX, x); maxY = Mathf.Max(maxY, y);
                }
            if (maxX < minX) throw new InvalidOperationException("Enemy sprite is fully transparent: " + path);
        }
        finally { Object.DestroyImmediate(decoded); }

        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (!importer) throw new InvalidOperationException("Missing TextureImporter: " + path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = (maxY - minY + 1) / visibleHeight;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.filterMode = FilterMode.Point;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.maxTextureSize = 8192;
        TextureImporterSettings settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteAlignment = (int)SpriteAlignment.Custom;
        settings.spritePivot = new Vector2((minX + maxX + 1) * 0.5f / width,
            feetPivot ? (float)minY / height : (minY + maxY + 1) * 0.5f / height);
        settings.spriteMeshType = SpriteMeshType.FullRect;
        importer.SetTextureSettings(settings);
        importer.SaveAndReimport();
        return Require<Sprite>(path);
    }

    private static DefenseProjectile CreateProjectile(string name, Sprite sprite)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(ArrowSource);
        try
        {
            root.name = name;
            SpriteRenderer renderer = root.GetComponent<SpriteRenderer>();
            if (!renderer) throw new InvalidOperationException("Arrow source requires SpriteRenderer.");
            renderer.sprite = sprite;
            renderer.flipX = false;
            renderer.flipY = false;
            GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, PrefabFolder + "/Projectiles/" + name + ".prefab");
            if (!saved) throw new InvalidOperationException("Could not save " + name + " projectile.");
            return saved.GetComponent<DefenseProjectile>();
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    private static void CreateEnemy(int index, Sprite sprite, bool facesRight, float visibleHeight, Vector3 originPosition,
        DefenseProjectile projectile, Sprite chargeSprite, EnemyStatDefinition sourceStat,
        float range, float windup, float recovery, float projectileSpeed)
    {
        string name = _names[index];
        DefenseEnemyAttackDefinition attack = ScriptableObject.CreateInstance<DefenseEnemyAttackDefinition>();
        attack.name = name + "Attack";
        SerializedObject attackData = new SerializedObject(attack);
        attackData.FindProperty("_style").enumValueIndex = index;
        attackData.FindProperty("_windupSeconds").floatValue = windup;
        attackData.FindProperty("_recoverySeconds").floatValue = recovery;
        attackData.FindProperty("_burstCount").intValue = 5;
        attackData.FindProperty("_burstIntervalSeconds").floatValue = 0.12f;
        attackData.FindProperty("_projectileSpeed").floatValue = projectileSpeed;
        attackData.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.CreateAsset(attack, DefinitionFolder + "/" + name + "Attack.asset");
        AssetDatabase.SaveAssetIfDirty(attack);

        EnemyStatDefinition stat = Object.Instantiate(sourceStat);
        stat.name = name + "Stat";
        SerializedObject statData = new SerializedObject(stat);
        statData.FindProperty("_name").stringValue = name;
        statData.FindProperty("_attackRange").floatValue = range;
        statData.FindProperty("_style").enumValueIndex = (int)AttackStyle.Ranged;
        statData.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.CreateAsset(stat, DefinitionFolder + "/" + name + "Stat.asset");
        AssetDatabase.SaveAssetIfDirty(stat);

        GameObject root = PrefabUtility.LoadPrefabContents(EnemySource);
        try
        {
            root.name = name;
            Transform body = root.transform.Find("BodyPose");
            Transform image = root.transform.Find("BodyPose/Visual/Image");
            DefenseActor actor = root.GetComponent<DefenseActor>();
            DefenseCombatPresentation presentation = root.GetComponent<DefenseCombatPresentation>();
            if (!body || !image || !actor || !presentation || !image.TryGetComponent(out SpriteRenderer renderer))
                throw new InvalidOperationException("DefenseEnemy must retain its BodyPose/Visual/Image and actor/presentation structure.");
            body.localPosition = new Vector3(0f, -visibleHeight * 0.5f, 0f);
            image.localPosition = Vector3.zero;
            renderer.sprite = sprite;
            // NPCComponent's native +X root faces LEFT. Normalize right-facing artwork at the renderer only.
            renderer.flipX = facesRight;
            renderer.flipY = false;
            Transform origin = new GameObject("ProjectileOrigin").transform;
            origin.SetParent(root.transform, false);
            origin.localPosition = originPosition;
            Transform charge = null;
            if (chargeSprite)
            {
                charge = new GameObject("ChargeVisual").transform;
                charge.SetParent(origin, false);
                charge.localScale = Vector3.one * 1.4f;
                SpriteRenderer chargeRenderer = charge.gameObject.AddComponent<SpriteRenderer>();
                chargeRenderer.sprite = chargeSprite;
                chargeRenderer.sharedMaterial = renderer.sharedMaterial;
                chargeRenderer.sortingLayerID = renderer.sortingLayerID;
                chargeRenderer.sortingOrder = renderer.sortingOrder + 1;
                charge.gameObject.SetActive(false);
            }
            SerializedObject actorData = new SerializedObject(actor);
            actorData.FindProperty("_enemyAttackDefinition").objectReferenceValue = attack;
            actorData.FindProperty("_projectilePrefab").objectReferenceValue = projectile;
            actorData.FindProperty("_projectileOrigin").objectReferenceValue = origin;
            actorData.ApplyModifiedPropertiesWithoutUndo();
            SerializedObject presentationData = new SerializedObject(presentation);
            presentationData.FindProperty("_body").objectReferenceValue = body;
            presentationData.FindProperty("_chargeVisual").objectReferenceValue = charge;
            presentationData.ApplyModifiedPropertiesWithoutUndo();
            if (!PrefabUtility.SaveAsPrefabAsset(root, PrefabFolder + "/" + name + ".prefab"))
                throw new InvalidOperationException("Could not save " + name + " enemy.");
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    private static T Require<T>(string path) where T : Object
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (!asset) throw new InvalidOperationException("Required " + typeof(T).Name + " missing at " + path);
        return asset;
    }

    private static void RequireUnused(string path)
    {
        if (File.Exists(path) || File.Exists(path + ".meta") || AssetDatabase.LoadMainAssetAtPath(path))
            throw new InvalidOperationException("Preserving existing output; resolve ownership before recreating " + path);
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }
}
