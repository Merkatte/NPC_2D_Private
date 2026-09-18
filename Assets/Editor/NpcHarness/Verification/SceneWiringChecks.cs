using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

// Editor-only inspection. Never initializes a gameplay registry or applies serialized changes.
internal sealed class SceneWiringChecks
{
    private readonly Scene _scene;
    private readonly HarnessGateResultBuilder _builder;

    public SceneWiringChecks(Scene scene, HarnessGateResultBuilder builder)
    {
        _scene = scene;
        _builder = builder;
    }

    public T[] FindAll<T>() where T : Component
    {
        return _scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<T>(true))
            .OrderBy(component => Describe(component), StringComparer.Ordinal).ToArray();
    }

    public T RequireSingle<T>(string id) where T : Behaviour
    {
        T[] matches = FindAll<T>();
        bool valid = matches.Length == 1 && matches[0].enabled && matches[0].gameObject.activeInHierarchy;
        Check(id, valid, "one active enabled " + typeof(T).Name,
            matches.Length + ": " + string.Join(", ", matches.Select(component =>
                Describe(component) + " active=" + component.gameObject.activeInHierarchy + " enabled=" + component.enabled)));
        return matches.Length == 1 ? matches[0] : null;
    }

    public T Reference<T>(UnityEngine.Object owner, string propertyPath, string id,
        UnityEngine.Object expected = null, bool requireSameScene = false) where T : UnityEngine.Object
    {
        T value = ReadReference(owner, propertyPath) as T;
        bool valid = value && (!expected || value == expected);
        if (requireSameScene)
        {
            valid &= value is Component component && component.gameObject.scene == _scene;
        }
        Check(id, valid, typeof(T).Name + (expected ? " = " + Describe(expected) : " assigned") +
            (requireSameScene ? " in inspected scene" : ""), Describe(ReadReference(owner, propertyPath)));
        return value;
    }

    public static UnityEngine.Object ReadReference(UnityEngine.Object owner, string path)
    {
        if (!owner)
        {
            return null;
        }
        using (SerializedObject serialized = new SerializedObject(owner))
        {
            SerializedProperty property = serialized.FindProperty(path);
            return property != null && property.propertyType == SerializedPropertyType.ObjectReference
                ? property.objectReferenceValue : null;
        }
    }

    public void Check(string id, bool success, string expected, string actual)
    {
        string message = success ? "Scene wiring satisfies " + id : "Scene wiring violates " + id;
        if (success)
        {
            _builder.AddPass(id, message, expected, actual);
        }
        else
        {
            _builder.AddFailure(id, message, expected, actual);
        }
    }

    public void CheckMissingScripts()
    {
        string[] missing = FindAll<Transform>()
            .Where(transform => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject) > 0)
            .Select(transform => Describe(transform.gameObject)).ToArray();
        Check("scene.missing-scripts", missing.Length == 0, "no missing scripts, including inactive objects",
            missing.Length == 0 ? "none" : string.Join(", ", missing));
    }

    public static string Describe(UnityEngine.Object target)
    {
        if (!target)
        {
            return "missing";
        }
        string assetPath = AssetDatabase.GetAssetPath(target);
        if (!string.IsNullOrEmpty(assetPath))
        {
            return assetPath + " (" + target.GetType().Name + ")";
        }
        Transform transform = target is Component component ? component.transform : (target as GameObject)?.transform;
        string path = string.Empty;
        while (transform)
        {
            path = "/" + transform.name + path;
            transform = transform.parent;
        }
        return (path.Length == 0 ? target.name : path) + " (" + target.GetType().Name + ")";
    }
}
