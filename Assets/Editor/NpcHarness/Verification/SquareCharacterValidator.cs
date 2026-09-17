using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

internal static class SquareCharacterValidator
{
    private const string RootName = "SquareCharacter";

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
            scene = SceneManager.GetSceneByPath(HarnessBeaconRecipe.ScenePath);
            if (!scene.IsValid() || !scene.isLoaded)
            {
                SceneAsset sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(HarnessBeaconRecipe.ScenePath);
                if (!sceneAsset)
                {
                    throw new InvalidOperationException($"Scene does not exist: {HarnessBeaconRecipe.ScenePath}");
                }

                scene = EditorSceneManager.OpenScene(HarnessBeaconRecipe.ScenePath, OpenSceneMode.Additive);
                openedForValidation = true;
            }

            if (scene.isDirty)
            {
                throw new InvalidOperationException(
                    $"Target harness scene has unsaved changes: {HarnessBeaconRecipe.ScenePath}");
            }

            return SquareCharacterStructureGate.Evaluate(runId, CollectObservation(scene));
        }
        catch (Exception exception)
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

    private static SquareCharacterStructureObservation CollectObservation(Scene scene)
    {
        Material managedMaterial = AssetDatabase.LoadAssetAtPath<Material>(HarnessBeaconRecipe.MaterialPath);
        List<GameObject> roots = new List<GameObject>();
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name == RootName)
            {
                roots.Add(root);
            }
        }

        if (roots.Count != 1)
        {
            string error = roots.Count == 0
                ? "GameObject is missing: /SquareCharacter"
                : "Hierarchy path is ambiguous because duplicate objects exist: /SquareCharacter";
            return new SquareCharacterStructureObservation(
                false,
                error,
                default,
                default,
                default,
                false,
                0,
                Array.Empty<string>(),
                managedMaterial,
                Array.Empty<SquareCharacterPartObservation>());
        }

        Transform rootTransform = roots[0].transform;
        string[] childNames = new string[rootTransform.childCount];
        for (int index = 0; index < rootTransform.childCount; index++)
        {
            childNames[index] = rootTransform.GetChild(index).name;
        }

        SquareCharacterPartObservation[] parts = new SquareCharacterPartObservation[
            SquareCharacterStructureGate.PartSpecifications.Length];
        for (int index = 0; index < SquareCharacterStructureGate.PartSpecifications.Length; index++)
        {
            SquareCharacterPartSpecification specification =
                SquareCharacterStructureGate.PartSpecifications[index];
            parts[index] = CollectPart(rootTransform, specification.Name);
        }

        return new SquareCharacterStructureObservation(
            true,
            string.Empty,
            ToObservation(rootTransform.localPosition),
            ToObservation(rootTransform.localRotation),
            ToObservation(rootTransform.localScale),
            roots[0].activeSelf,
            roots[0].GetComponents<Component>().Length,
            childNames,
            managedMaterial,
            parts);
    }

    private static SquareCharacterPartObservation CollectPart(Transform root, string partName)
    {
        List<Transform> matches = new List<Transform>();
        for (int index = 0; index < root.childCount; index++)
        {
            Transform child = root.GetChild(index);
            if (child.name == partName)
            {
                matches.Add(child);
            }
        }

        if (matches.Count != 1)
        {
            string path = $"/SquareCharacter/{partName}";
            string error = matches.Count == 0
                ? $"GameObject is missing: {path}"
                : $"Hierarchy path is ambiguous because duplicate objects exist: {path}";
            return new SquareCharacterPartObservation(
                partName,
                false,
                error,
                default,
                default,
                default,
                false,
                0,
                0,
                false,
                false,
                false,
                0,
                0f,
                0f,
                0,
                0,
                string.Empty,
                Array.Empty<SquareCharacterVectorObservation>());
        }

        Transform transform = matches[0];
        LineRenderer[] lineRenderers = transform.GetComponents<LineRenderer>();
        LineRenderer lineRenderer = lineRenderers.Length == 1 ? lineRenderers[0] : null;
        SquareCharacterVectorObservation[] points = lineRenderer
            ? CollectPoints(lineRenderer)
            : Array.Empty<SquareCharacterVectorObservation>();
        string materialPath = lineRenderer && lineRenderer.sharedMaterial
            ? AssetDatabase.GetAssetPath(lineRenderer.sharedMaterial)
            : string.Empty;

        return new SquareCharacterPartObservation(
            partName,
            true,
            string.Empty,
            ToObservation(transform.localPosition),
            ToObservation(transform.localRotation),
            ToObservation(transform.localScale),
            transform.gameObject.activeSelf,
            transform.gameObject.GetComponents<Component>().Length,
            lineRenderers.Length,
            lineRenderer && lineRenderer.useWorldSpace,
            lineRenderer && lineRenderer.loop,
            lineRenderer && lineRenderer.enabled,
            lineRenderer ? lineRenderer.sortingOrder : 0,
            lineRenderer ? lineRenderer.startWidth : 0f,
            lineRenderer ? lineRenderer.endWidth : 0f,
            lineRenderer ? lineRenderer.numCornerVertices : 0,
            lineRenderer ? lineRenderer.numCapVertices : 0,
            materialPath,
            points);
    }

    private static SquareCharacterVectorObservation[] CollectPoints(LineRenderer lineRenderer)
    {
        Vector3[] positions = new Vector3[lineRenderer.positionCount];
        lineRenderer.GetPositions(positions);
        SquareCharacterVectorObservation[] points =
            new SquareCharacterVectorObservation[positions.Length];
        for (int index = 0; index < positions.Length; index++)
        {
            points[index] = ToObservation(positions[index]);
        }

        return points;
    }

    private static SquareCharacterVectorObservation ToObservation(Vector3 value)
    {
        return new SquareCharacterVectorObservation(value.x, value.y, value.z);
    }

    private static SquareCharacterQuaternionObservation ToObservation(Quaternion value)
    {
        return new SquareCharacterQuaternionObservation(value.x, value.y, value.z, value.w);
    }
}
