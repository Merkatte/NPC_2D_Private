using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

internal static class HarnessBeaconValidator
{
    public static HarnessToolResult Validate()
    {
        HarnessGateResult result = EvaluateStructure("interactive-beacon-structure");
        return result.success
            ? HarnessToolResult.Success(result.message)
            : HarnessToolResult.Failure(result.message);
    }

    public static HarnessGateResult EvaluateStructure(string runId)
    {
        HarnessGateResultBuilder builder = new HarnessGateResultBuilder(
            runId,
            HarnessBeaconGateRunner.Profile,
            HarnessBeaconGateRunner.ProfileVersion);
        Scene scene = default;
        bool openedForValidation = false;

        try
        {
            scene = SceneManager.GetSceneByPath(HarnessBeaconRecipe.ScenePath);
            if (!scene.IsValid() || !scene.isLoaded)
            {
                SceneAsset sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(HarnessBeaconRecipe.ScenePath);
                if (!sceneAsset)
                {
                    throw new InvalidOperationException(
                        $"Scene does not exist: {HarnessBeaconRecipe.ScenePath}");
                }

                scene = EditorSceneManager.OpenScene(HarnessBeaconRecipe.ScenePath, OpenSceneMode.Additive);
                openedForValidation = true;
            }

            if (scene.isDirty)
            {
                throw new InvalidOperationException(
                    $"Target harness scene has unsaved changes: {HarnessBeaconRecipe.ScenePath}");
            }

            HarnessBeaconStructureObservation observation = CollectObservation(scene);
            return HarnessBeaconStructureGate.Evaluate(runId, observation);
        }
        catch (Exception exception)
        {
            builder.AddFailure(
                "scene.load-and-resolve",
                "HarnessBeacon scene could not be loaded or resolved.",
                "loadable scene with unique required objects",
                exception.Message);
        }
        finally
        {
            if (openedForValidation && scene.IsValid() && scene.isLoaded)
            {
                EditorSceneManager.CloseScene(scene, removeScene: true);
            }
        }

        return builder.Build();
    }

    private static HarnessBeaconStructureObservation CollectObservation(Scene scene)
    {
        if (!TryFindSingleGameObject(scene, "/HarnessBeacon", out GameObject beacon, out string beaconError))
        {
            return new HarnessBeaconStructureObservation(
                false,
                beaconError,
                0,
                false,
                0,
                string.Empty,
                AssetDatabase.LoadAssetAtPath<Material>(HarnessBeaconRecipe.MaterialPath),
                false,
                string.Empty,
                false,
                false,
                0f);
        }

        Type harnessType = HarnessToolContext.ResolveAllowedComponentType("HarnessTest");
        LineRenderer lineRenderer = beacon.GetComponent<LineRenderer>();
        Material managedMaterial = AssetDatabase.LoadAssetAtPath<Material>(HarnessBeaconRecipe.MaterialPath);
        bool hasCamera = TryFindSingleGameObject(
            scene,
            "/Main Camera",
            out GameObject cameraObject,
            out string cameraError);
        Camera camera = hasCamera ? cameraObject.GetComponent<Camera>() : null;

        return new HarnessBeaconStructureObservation(
            true,
            string.Empty,
            beacon.GetComponents(harnessType).Length,
            lineRenderer,
            lineRenderer ? lineRenderer.positionCount : 0,
            lineRenderer && lineRenderer.sharedMaterial
                ? AssetDatabase.GetAssetPath(lineRenderer.sharedMaterial)
                : string.Empty,
            managedMaterial,
            hasCamera,
            cameraError,
            camera,
            camera && camera.orthographic,
            camera ? camera.orthographicSize : 0f);
    }

    private static bool TryFindSingleGameObject(
        Scene scene,
        string targetPath,
        out GameObject gameObject,
        out string error)
    {
        try
        {
            gameObject = FindSingleGameObject(scene, targetPath);
            error = string.Empty;
            return true;
        }
        catch (InvalidOperationException exception)
        {
            gameObject = null;
            error = exception.Message;
            return false;
        }
    }

    private static GameObject FindSingleGameObject(Scene scene, string targetPath)
    {
        string[] parts = targetPath.Trim('/').Split('/');
        Transform current = null;

        foreach (string part in parts)
        {
            List<Transform> matches = new List<Transform>();
            if (current == null)
            {
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    if (root.name == part)
                    {
                        matches.Add(root.transform);
                    }
                }
            }
            else
            {
                for (int childIndex = 0; childIndex < current.childCount; childIndex++)
                {
                    Transform child = current.GetChild(childIndex);
                    if (child.name == part)
                    {
                        matches.Add(child);
                    }
                }
            }

            if (matches.Count != 1)
            {
                throw new InvalidOperationException(
                    matches.Count == 0
                        ? $"GameObject is missing: {targetPath}"
                        : $"Hierarchy path is ambiguous because duplicate objects exist: {targetPath}");
            }

            current = matches[0];
        }

        return current.gameObject;
    }
}
