using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

internal static class SquareCharacterValidator
{
    internal const string ManifestPath = "Tools/NpcHarness/Profiles/square-character-structure.json";

    public static HarnessGateResult EvaluateStructure(string runId)
    {
        HarnessGateResultBuilder builder = new HarnessGateResultBuilder(
            runId,
            SquareCharacterGateRunner.Profile,
            SquareCharacterGateRunner.ProfileVersion);
        Scene scene = default;
        bool openedForValidation = false;

        try
        {
            DeclarativeSceneGateManifest manifest = DeclarativeSceneGateManifestLoader.Load(ManifestPath);
            if (manifest.profile != SquareCharacterGateRunner.Profile ||
                manifest.profileVersion != SquareCharacterGateRunner.ProfileVersion)
            {
                throw new System.InvalidOperationException(
                    $"Manifest identity {manifest.profile} v{manifest.profileVersion} does not match " +
                    $"registered profile {SquareCharacterGateRunner.Profile} v{SquareCharacterGateRunner.ProfileVersion}.");
            }

            scene = SceneManager.GetSceneByPath(manifest.scenePath);
            if (!scene.IsValid() || !scene.isLoaded)
            {
                SceneAsset sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(manifest.scenePath);
                if (!sceneAsset)
                {
                    throw new System.InvalidOperationException($"Scene does not exist: {manifest.scenePath}");
                }

                scene = EditorSceneManager.OpenScene(manifest.scenePath, OpenSceneMode.Additive);
                openedForValidation = true;
            }

            if (scene.isDirty)
            {
                throw new System.InvalidOperationException(
                    $"Target harness scene has unsaved changes: {manifest.scenePath}");
            }

            return DeclarativeSceneStructureGate.Evaluate(runId, manifest, CollectObservation(scene, manifest));
        }
        catch (System.Exception exception)
        {
            builder.SetInfrastructureError(exception.Message);
            return builder.Build();
        }
        finally
        {
            if (openedForValidation && scene.IsValid() && scene.isLoaded)
            {
                EditorSceneManager.CloseScene(scene, removeScene: true);
            }
        }
    }

    private static DeclarativeSceneObservation CollectObservation(
        Scene scene,
        DeclarativeSceneGateManifest manifest)
    {
        List<DeclarativeSceneObjectObservation> objects = new List<DeclarativeSceneObjectObservation>();
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            CollectObject(root.transform, string.Empty, objects);
        }

        HashSet<string> existingAssetPaths = new HashSet<string>(System.StringComparer.Ordinal);
        foreach (DeclarativeSceneGateCheck check in manifest.checks)
        {
            if (!string.IsNullOrWhiteSpace(check.materialPath) &&
                AssetDatabase.LoadAssetAtPath<Material>(check.materialPath))
            {
                existingAssetPaths.Add(check.materialPath);
            }
        }

        return new DeclarativeSceneObservation(objects, existingAssetPaths);
    }

    private static void CollectObject(
        Transform transform,
        string parentPath,
        ICollection<DeclarativeSceneObjectObservation> objects)
    {
        string path = parentPath + "/" + transform.name;
        string[] childNames = new string[transform.childCount];
        for (int index = 0; index < transform.childCount; index++)
        {
            childNames[index] = transform.GetChild(index).name;
        }

        LineRenderer[] lineRenderers = transform.GetComponents<LineRenderer>();
        LineRenderer lineRenderer = lineRenderers.Length == 1 ? lineRenderers[0] : null;
        DeclarativeVector3[] points = lineRenderer
            ? CollectPoints(lineRenderer)
            : System.Array.Empty<DeclarativeVector3>();
        string materialPath = lineRenderer && lineRenderer.sharedMaterial
            ? AssetDatabase.GetAssetPath(lineRenderer.sharedMaterial)
            : string.Empty;
        objects.Add(new DeclarativeSceneObjectObservation(
            path,
            transform.gameObject.activeSelf,
            transform.gameObject.GetComponents<Component>().Length,
            ToObservation(transform.localPosition),
            ToObservation(transform.localRotation),
            ToObservation(transform.localScale),
            childNames,
            new DeclarativeLineRendererObservation(
                lineRenderers.Length,
                lineRenderer && lineRenderer.enabled,
                lineRenderer && lineRenderer.useWorldSpace,
                lineRenderer && lineRenderer.loop,
                lineRenderer ? lineRenderer.sortingOrder : 0,
                lineRenderer ? lineRenderer.startWidth : 0f,
                lineRenderer ? lineRenderer.endWidth : 0f,
                lineRenderer ? lineRenderer.numCornerVertices : 0,
                lineRenderer ? lineRenderer.numCapVertices : 0,
                materialPath,
                points)));

        for (int index = 0; index < transform.childCount; index++)
        {
            CollectObject(transform.GetChild(index), path, objects);
        }
    }

    private static DeclarativeVector3[] CollectPoints(LineRenderer lineRenderer)
    {
        Vector3[] positions = new Vector3[lineRenderer.positionCount];
        lineRenderer.GetPositions(positions);
        DeclarativeVector3[] points = new DeclarativeVector3[positions.Length];
        for (int index = 0; index < positions.Length; index++)
        {
            points[index] = ToObservation(positions[index]);
        }

        return points;
    }

    private static DeclarativeVector3 ToObservation(Vector3 value)
    {
        return new DeclarativeVector3(value.x, value.y, value.z);
    }

    private static DeclarativeQuaternion ToObservation(Quaternion value)
    {
        return new DeclarativeQuaternion(value.x, value.y, value.z, value.w);
    }
}
