using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Bounded authoring commands. Neither command saves scenes or unrelated dirty assets.
public static class DefenseTreeGroveSetup
{
    private const string ScenePath = "Assets/Scenes/DefenseTest.unity";
    private const string TreeFolder = "Assets/Art/Generated/Environment/Trees";
    private const string MaterialFolder = TreeFolder + "/Materials";
    private static readonly string[] _species = { "Green", "Apricot", "Gold", "Rose" };

    [MenuItem("Tools/NPC Harness/Defense Trees/Prepare Assets")]
    public static void PrepareAssets()
    {
        RequireEditableScene();
        Shader shader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/Art/Shaders/InstancedTreeWind.shader");
        if (!shader || ShaderUtil.ShaderHasError(shader))
            throw new InvalidOperationException("InstancedTreeWind shader must import without errors before preparing tree assets.");

        foreach (string species in _species)
        {
            string path = TreeFolder + "/Tree_" + species + ".png";
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (!importer || !Mathf.Approximately(importer.spritePixelsPerUnit, 20f))
                throw new InvalidOperationException(path + " must already exist at 20 PPU.");
        }

        if (!AssetDatabase.IsValidFolder(MaterialFolder)) AssetDatabase.CreateFolder(TreeFolder, "Materials");
        foreach (string species in _species)
        {
            string texturePath = TreeFolder + "/Tree_" + species + ".png";
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(texturePath);
            if (importer.filterMode != FilterMode.Point || importer.mipmapEnabled ||
                importer.textureCompression != TextureImporterCompression.Uncompressed ||
                importer.npotScale != TextureImporterNPOTScale.None || importer.maxTextureSize < 2048)
            {
                importer.filterMode = FilterMode.Point;
                importer.mipmapEnabled = false;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.npotScale = TextureImporterNPOTScale.None;
                importer.maxTextureSize = Mathf.Max(2048, importer.maxTextureSize);
                importer.SaveAndReimport();
            }

            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            if (!texture || texture.width != 1344 || texture.height != 144)
                throw new InvalidOperationException(texturePath + " must import as a 1344 x 144 atlas.");

            string materialPath = MaterialFolder + "/Tree_" + species + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (!material)
            {
                if (AssetDatabase.LoadMainAssetAtPath(materialPath))
                    throw new InvalidOperationException(materialPath + " is occupied by an unrelated asset.");
                material = new Material(shader) { name = "Tree_" + species };
                ConfigureMaterial(material, shader, texture);
                AssetDatabase.CreateAsset(material, materialPath);
            }
            else
            {
                if (material.shader != shader)
                    throw new InvalidOperationException(materialPath + " uses an unrelated shader; preserve it and resolve ownership first.");
                Undo.RecordObject(material, "Configure Instanced Tree Material");
                ConfigureMaterial(material, shader, texture);
                EditorUtility.SetDirty(material);
                AssetDatabase.SaveAssetIfDirty(material);
            }
        }
        Debug.Log("Defense tree atlases and four instanced materials prepared. No scene was saved.");
    }

    private static void ConfigureMaterial(Material material, Shader shader, Texture2D texture)
    {
        material.shader = shader;
        material.enableInstancing = true;
        material.renderQueue = 3001;
        material.SetTexture("_MainTex", texture);
        material.SetFloat("_Cutoff", 0.5f);
    }

    [MenuItem("Tools/NPC Harness/Defense Trees/Place Trees")]
    public static void PlaceTrees()
    {
        Scene scene = RequireEditableScene();
        Transform map = null;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name == "InGameObjects") map = root.transform.Find("Map");
        }
        if (!map) throw new InvalidOperationException("DefenseTest requires /InGameObjects/Map.");
        if (Quaternion.Angle(map.rotation, Quaternion.identity) > 0.01f ||
            (map.lossyScale - Vector3.one).sqrMagnitude > 0.0001f)
            throw new InvalidOperationException("The backdrop setup expects Map to have identity world rotation and scale.");

        Material[] materials = new Material[_species.Length];
        for (int index = 0; index < _species.Length; index++)
        {
            materials[index] = AssetDatabase.LoadAssetAtPath<Material>(
                MaterialFolder + "/Tree_" + _species[index] + ".mat");
            if (!materials[index] || !materials[index].enableInstancing || !materials[index].shader ||
                materials[index].shader.name != "NPC/InstancedTreeWind")
                throw new InvalidOperationException("Run Prepare Assets before placing the tree grove.");
        }

        Transform host = map.Find("InstancedTrees");
        InstancedTreeGrove grove;
        if (host)
        {
            grove = host.GetComponent<InstancedTreeGrove>();
            if (!grove || host.childCount != 0 || host.GetComponents<Component>().Length != 2 ||
                host.localPosition != Vector3.zero || host.localRotation != Quaternion.identity ||
                host.localScale != Vector3.one)
                throw new InvalidOperationException("Existing InstancedTrees contains unexpected state; preserve it and resolve ownership first.");
        }
        else
        {
            GameObject gameObject = new GameObject("InstancedTrees");
            Undo.RegisterCreatedObjectUndo(gameObject, "Create Instanced Tree Grove");
            SceneManager.MoveGameObjectToScene(gameObject, scene);
            host = gameObject.transform;
            host.SetParent(map, false);
            grove = Undo.AddComponent<InstancedTreeGrove>(gameObject);
        }

        SerializedObject serialized = new SerializedObject(grove);
        SerializedProperty speciesMaterials = serialized.FindProperty("_speciesMaterials");
        speciesMaterials.arraySize = materials.Length;
        for (int index = 0; index < materials.Length; index++)
            speciesMaterials.GetArrayElementAtIndex(index).objectReferenceValue = materials[index];

        // The northern backdrop is beyond the enemy spawn line and the wall approach.
        Rect[] worldRegions =
        {
            Rect.MinMaxRect(-14f, 26.8f, 8f, 28f),
            Rect.MinMaxRect(13f, 26.8f, 36f, 28f),
            Rect.MinMaxRect(42f, 26.8f, 61f, 28f)
        };
        SerializedProperty regions = serialized.FindProperty("_placementRegions");
        regions.arraySize = worldRegions.Length;
        for (int index = 0; index < worldRegions.Length; index++)
        {
            Rect worldRegion = worldRegions[index];
            Vector3 minimum = host.InverseTransformPoint(new Vector3(worldRegion.xMin, worldRegion.yMin, 0f));
            Vector3 maximum = host.InverseTransformPoint(new Vector3(worldRegion.xMax, worldRegion.yMax, 0f));
            regions.GetArrayElementAtIndex(index).rectValue = Rect.MinMaxRect(minimum.x, minimum.y, maximum.x, maximum.y);
        }
        serialized.FindProperty("_treeCount").intValue = 50;
        serialized.FindProperty("_seed").intValue = 20261103;
        serialized.FindProperty("_minimumSpacing").floatValue = 1f;
        serialized.FindProperty("_treeScale").floatValue = 1f;
        serialized.FindProperty("_depthOffset").floatValue = -0.1f;
        serialized.FindProperty("_depthPerY").floatValue = 0.001f;
        serialized.ApplyModifiedProperties();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorApplication.QueuePlayerLoopUpdate();
        SceneView.RepaintAll();
        Debug.Log("DefenseTest InstancedTrees configured for 50 decorative trees. Scene remains unsaved.", grove);
    }

    private static Scene RequireEditableScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || PrefabStageUtility.GetCurrentPrefabStage())
            throw new InvalidOperationException("Defense tree setup requires Edit Mode outside Prefab Mode.");
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded || scene.path != ScenePath)
            throw new InvalidOperationException("Open DefenseTest as the active scene before running tree setup.");
        return scene;
    }
}
