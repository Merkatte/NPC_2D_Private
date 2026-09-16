using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

internal sealed class EnsureGameObjectTool : IHarnessTool
{
    public string Id => "EnsureGameObject";

    public HarnessToolResult Validate(HarnessToolContext context, HarnessStep step)
    {
        HarnessToolResult sceneResult = HarnessToolPolicy.ValidateAssetPath(step.scenePath, ".unity");
        if (sceneResult.IsFailure)
        {
            return sceneResult;
        }

        HarnessToolResult parentResult = HarnessToolPolicy.ValidateHierarchyPath(step.parentPath, allowRoot: true);
        if (parentResult.IsFailure)
        {
            return parentResult;
        }

        if (string.IsNullOrWhiteSpace(step.name) || step.name.Contains("/", StringComparison.Ordinal))
        {
            return HarnessToolResult.ValidationFailure("GameObject name is missing or contains '/'.");
        }

        return HarnessToolResult.Success("EnsureGameObject input is valid.");
    }

    public HarnessToolResult Execute(HarnessToolContext context, HarnessStep step)
    {
        Scene scene = context.RequireScene(step.scenePath);
        Transform parent = step.parentPath == "/"
            ? null
            : context.FindSingleGameObject(step.scenePath, step.parentPath).transform;

        List<GameObject> matches = FindMatches(scene, parent, step.name);
        if (matches.Count > 1)
        {
            return HarnessToolResult.Failure(
                $"More than one GameObject named {step.name} exists under {step.parentPath}.");
        }

        if (matches.Count == 1)
        {
            GameObject existing = matches[0];
            if (existing.activeSelf == step.active)
            {
                return HarnessToolResult.NoChange($"GameObject already exists: {GetPath(step)}");
            }

            if (!context.Options.AllowOverwrite)
            {
                return HarnessToolResult.Failure(
                    $"GameObject active state differs and overwrite is not approved: {GetPath(step)}");
            }

            if (context.Options.Interactive)
            {
                Undo.RecordObject(existing, "Configure Harness GameObject");
            }

            existing.SetActive(step.active);
            EditorSceneManager.MarkSceneDirty(scene);
            return HarnessToolResult.Success($"Updated GameObject: {GetPath(step)}");
        }

        GameObject created = new GameObject(step.name);
        if (parent)
        {
            created.transform.SetParent(parent, false);
        }
        else
        {
            SceneManager.MoveGameObjectToScene(created, scene);
        }

        created.SetActive(step.active);
        context.MarkCreated(created);
        if (context.Options.Interactive)
        {
            Undo.RegisterCreatedObjectUndo(created, "Create Harness GameObject");
        }

        EditorSceneManager.MarkSceneDirty(scene);

        return HarnessToolResult.Success($"Created GameObject: {GetPath(step)}");
    }

    private static List<GameObject> FindMatches(Scene scene, Transform parent, string objectName)
    {
        List<GameObject> matches = new List<GameObject>();
        if (!parent)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == objectName)
                {
                    matches.Add(root);
                }
            }

            return matches;
        }

        for (int childIndex = 0; childIndex < parent.childCount; childIndex++)
        {
            Transform child = parent.GetChild(childIndex);
            if (child.name == objectName)
            {
                matches.Add(child.gameObject);
            }
        }

        return matches;
    }

    private static string GetPath(HarnessStep step)
    {
        return step.parentPath == "/" ? "/" + step.name : step.parentPath + "/" + step.name;
    }
}
