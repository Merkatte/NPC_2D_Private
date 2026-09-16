using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

internal sealed class HarnessToolContext
{
    private readonly HashSet<int> _createdInstanceIds = new HashSet<int>();
    private Scene _scene;
    private string _scenePath = string.Empty;

    public HarnessToolContext(HarnessExecutionOptions options)
    {
        Options = options;
    }

    public HarnessExecutionOptions Options { get; }

    public void MarkCreated(UnityEngine.Object createdObject)
    {
        if (createdObject)
        {
            _createdInstanceIds.Add(createdObject.GetInstanceID());
        }
    }

    public bool WasCreated(UnityEngine.Object target)
    {
        return target && _createdInstanceIds.Contains(target.GetInstanceID());
    }

    public Scene OpenOrCreateScene(string scenePath)
    {
        if (_scene.IsValid() && string.Equals(_scenePath, scenePath, StringComparison.Ordinal))
        {
            return _scene;
        }

        RefuseToReplaceDirtyScene(scenePath);
        string absolutePath = HarnessToolPolicy.GetAbsoluteProjectPath(scenePath);
        _scene = File.Exists(absolutePath)
            ? EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single)
            : EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        _scenePath = scenePath;
        return _scene;
    }

    public Scene RequireScene(string scenePath)
    {
        if (_scene.IsValid() && string.Equals(_scenePath, scenePath, StringComparison.Ordinal))
        {
            return _scene;
        }

        string absolutePath = HarnessToolPolicy.GetAbsoluteProjectPath(scenePath);
        if (!File.Exists(absolutePath))
        {
            throw new InvalidOperationException($"Scene does not exist: {scenePath}");
        }

        RefuseToReplaceDirtyScene(scenePath);
        _scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        _scenePath = scenePath;
        return _scene;
    }

    public GameObject FindSingleGameObject(string scenePath, string targetPath)
    {
        Scene scene = RequireScene(scenePath);
        string[] parts = targetPath.Trim('/').Split('/');
        Transform current = null;

        for (int partIndex = 0; partIndex < parts.Length; partIndex++)
        {
            string part = parts[partIndex];
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

    public static Type ResolveAllowedComponentType(string componentTypeId)
    {
        if (!HarnessToolPolicy.IsComponentAllowed(componentTypeId))
        {
            throw new InvalidOperationException($"Component type is not allowed: {componentTypeId}");
        }

        if (componentTypeId == "Camera")
        {
            return typeof(Camera);
        }

        if (componentTypeId == "LineRenderer")
        {
            return typeof(LineRenderer);
        }

        foreach (System.Reflection.Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            Type type = assembly.GetType(componentTypeId, false);
            if (type != null && typeof(Component).IsAssignableFrom(type))
            {
                return type;
            }
        }

        throw new InvalidOperationException($"Compiled Component type was not found: {componentTypeId}");
    }

    private static void RefuseToReplaceDirtyScene(string requestedScenePath)
    {
        Scene activeScene = SceneManager.GetActiveScene();
        if (!activeScene.IsValid() || !activeScene.isDirty || activeScene.path == requestedScenePath)
        {
            return;
        }

        throw new InvalidOperationException(
            $"The active scene has unsaved changes and cannot be replaced: {activeScene.path}");
    }
}
