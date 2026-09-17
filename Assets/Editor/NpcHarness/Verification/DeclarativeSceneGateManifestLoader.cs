using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

internal static class DeclarativeSceneGateManifestLoader
{
    private const string AllowedManifestRoot = "Tools/NpcHarness/Profiles";

    private static readonly HashSet<string> RootFields = new HashSet<string>(StringComparer.Ordinal)
    {
        "schemaVersion", "profile", "profileVersion", "scenePath", "tolerance", "checks",
    };

    private static readonly HashSet<string> CheckFields = new HashSet<string>(StringComparer.Ordinal)
    {
        "id", "type", "path", "passMessage", "failMessage", "expected",
        "active", "componentCount", "localPosition", "localRotation", "localScale", "children",
        "enabled", "useWorldSpace", "loop", "sortingOrder", "startWidth", "endWidth",
        "cornerVertices", "capVertices", "points", "materialPath",
    };

    private static readonly HashSet<string> VectorFields = new HashSet<string>(StringComparer.Ordinal)
    {
        "x", "y", "z",
    };

    private static readonly HashSet<string> QuaternionFields = new HashSet<string>(StringComparer.Ordinal)
    {
        "x", "y", "z", "w",
    };

    public static DeclarativeSceneGateManifest Load(string projectRelativePath)
    {
        string normalizedPath = ValidateManifestPath(projectRelativePath);
        string projectRoot = Directory.GetParent(Application.dataPath)?.FullName ??
                             throw new InvalidOperationException("Could not resolve the Unity project root.");
        string absolutePath = Path.GetFullPath(Path.Combine(projectRoot, normalizedPath));
        if (!File.Exists(absolutePath))
        {
            throw new FileNotFoundException($"Declarative gate manifest does not exist: {normalizedPath}");
        }

        try
        {
            return ParseAndValidateJson(File.ReadAllText(absolutePath), normalizedPath);
        }
        catch (IOException exception)
        {
            throw new InvalidOperationException(
                $"Declarative gate manifest could not be read: {normalizedPath}. {exception.Message}",
                exception);
        }
    }

    internal static DeclarativeSceneGateManifest ParseAndValidateJson(string json, string sourceName)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new InvalidOperationException($"Declarative gate manifest is empty: {sourceName}");
        }

        ValidateJsonContract(HarnessStrictJsonReader.Parse(json), sourceName);

        DeclarativeSceneGateManifest manifest;
        try
        {
            manifest = JsonUtility.FromJson<DeclarativeSceneGateManifest>(json);
        }
        catch (ArgumentException exception)
        {
            throw new InvalidOperationException(
                $"Declarative gate manifest contains invalid JSON: {sourceName}. {exception.Message}",
                exception);
        }

        Validate(manifest, sourceName);
        return manifest;
    }

    internal static void Validate(DeclarativeSceneGateManifest manifest, string sourceName)
    {
        if (manifest == null)
        {
            throw new InvalidOperationException($"Declarative gate manifest is empty: {sourceName}");
        }

        if (manifest.schemaVersion != 1)
        {
            throw new InvalidOperationException(
                $"Unsupported declarative gate manifest schemaVersion {manifest.schemaVersion}: {sourceName}");
        }

        if (string.IsNullOrWhiteSpace(manifest.profile) || manifest.profileVersion <= 0)
        {
            throw new InvalidOperationException($"Declarative gate profile identity is invalid: {sourceName}");
        }

        if (!IsProjectScenePath(manifest.scenePath))
        {
            throw new InvalidOperationException(
                $"Declarative gate scenePath must be an Assets .unity path: {manifest.scenePath}");
        }

        if (manifest.tolerance <= 0f)
        {
            throw new InvalidOperationException("Declarative gate tolerance must be greater than zero.");
        }

        if (manifest.checks == null || manifest.checks.Length == 0)
        {
            throw new InvalidOperationException("Declarative gate manifest must contain at least one check.");
        }

        HashSet<string> checkIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (DeclarativeSceneGateCheck check in manifest.checks)
        {
            ValidateCheck(check, checkIds);
        }
    }

    private static void ValidateJsonContract(object parsedJson, string sourceName)
    {
        IDictionary<string, object> root = RequireObject(parsedJson, "manifest", sourceName);
        RequireExactFields(root, RootFields, RootFields, "manifest", sourceName);
        RequireInteger(root["schemaVersion"], "manifest schemaVersion", sourceName);
        RequireString(root["profile"], "manifest profile", sourceName);
        RequireInteger(root["profileVersion"], "manifest profileVersion", sourceName);
        RequireString(root["scenePath"], "manifest scenePath", sourceName);
        RequireNumber(root["tolerance"], "manifest tolerance", sourceName);

        IList<object> checks = RequireArray(root["checks"], "manifest checks", sourceName);
        for (int index = 0; index < checks.Count; index++)
        {
            ValidateJsonCheck(RequireObject(checks[index], $"check {index}", sourceName), sourceName);
        }
    }

    private static void ValidateJsonCheck(IDictionary<string, object> check, string sourceName)
    {
        string[] requiredBaseFields = { "id", "type", "path", "passMessage", "failMessage", "expected" };
        RequireExactFields(check, CheckFields, requiredBaseFields, "check", sourceName);
        foreach (string fieldName in requiredBaseFields)
        {
            RequireString(check[fieldName], $"check {fieldName}", sourceName);
        }

        string checkId = (string)check["id"];
        string checkType = (string)check["type"];
        switch (checkType)
        {
            case DeclarativeSceneGateCheckType.ObjectLayout:
                RequireFields(check, new[]
                {
                    "active", "componentCount", "localPosition", "localRotation", "localScale",
                }, checkId, sourceName);
                RequireBoolean(check["active"], $"check {checkId} active", sourceName);
                RequireNonNegativeInteger(check["componentCount"], $"check {checkId} componentCount", sourceName);
                ValidateJsonVector(check["localPosition"], checkId, "localPosition", sourceName);
                ValidateJsonQuaternion(check["localRotation"], checkId, sourceName);
                ValidateJsonVector(check["localScale"], checkId, "localScale", sourceName);
                break;
            case DeclarativeSceneGateCheckType.ExactChildren:
                RequireFields(check, new[] { "children" }, checkId, sourceName);
                IList<object> children = RequireArray(check["children"], $"check {checkId} children", sourceName);
                foreach (object child in children)
                {
                    RequireString(child, $"check {checkId} child", sourceName);
                }
                break;
            case DeclarativeSceneGateCheckType.LineRendererShape:
                RequireFields(check, new[]
                {
                    "enabled", "useWorldSpace", "loop", "sortingOrder", "startWidth", "endWidth",
                    "cornerVertices", "capVertices", "points",
                }, checkId, sourceName);
                RequireBoolean(check["enabled"], $"check {checkId} enabled", sourceName);
                RequireBoolean(check["useWorldSpace"], $"check {checkId} useWorldSpace", sourceName);
                RequireBoolean(check["loop"], $"check {checkId} loop", sourceName);
                RequireInteger(check["sortingOrder"], $"check {checkId} sortingOrder", sourceName);
                RequireNumber(check["startWidth"], $"check {checkId} startWidth", sourceName);
                RequireNumber(check["endWidth"], $"check {checkId} endWidth", sourceName);
                RequireNonNegativeInteger(check["cornerVertices"], $"check {checkId} cornerVertices", sourceName);
                RequireNonNegativeInteger(check["capVertices"], $"check {checkId} capVertices", sourceName);
                IList<object> points = RequireArray(check["points"], $"check {checkId} points", sourceName);
                foreach (object point in points)
                {
                    ValidateJsonVector(point, checkId, "point", sourceName);
                }
                break;
            case DeclarativeSceneGateCheckType.LineRendererMaterial:
                RequireFields(check, new[] { "materialPath" }, checkId, sourceName);
                RequireString(check["materialPath"], $"check {checkId} materialPath", sourceName);
                break;
        }
    }

    private static void ValidateJsonVector(object value, string checkId, string fieldName, string sourceName)
    {
        IDictionary<string, object> vector = RequireObject(value, $"check {checkId} {fieldName}", sourceName);
        RequireExactFields(vector, VectorFields, VectorFields, $"check {checkId} {fieldName}", sourceName);
        foreach (string coordinate in VectorFields)
        {
            RequireNumber(vector[coordinate], $"check {checkId} {fieldName}.{coordinate}", sourceName);
        }
    }

    private static void ValidateJsonQuaternion(object value, string checkId, string sourceName)
    {
        IDictionary<string, object> quaternion =
            RequireObject(value, $"check {checkId} localRotation", sourceName);
        RequireExactFields(
            quaternion,
            QuaternionFields,
            QuaternionFields,
            $"check {checkId} localRotation",
            sourceName);
        foreach (string coordinate in QuaternionFields)
        {
            RequireNumber(quaternion[coordinate], $"check {checkId} localRotation.{coordinate}", sourceName);
        }
    }

    private static void RequireExactFields(
        IDictionary<string, object> value,
        IEnumerable<string> allowedFields,
        IEnumerable<string> requiredFields,
        string location,
        string sourceName)
    {
        HashSet<string> allowed = new HashSet<string>(allowedFields, StringComparer.Ordinal);
        foreach (string fieldName in value.Keys)
        {
            if (!allowed.Contains(fieldName))
            {
                throw new InvalidOperationException(
                    $"Declarative gate {location} contains unknown field {fieldName}: {sourceName}");
            }
        }

        RequireFields(value, requiredFields, location, sourceName);
    }

    private static void RequireFields(
        IDictionary<string, object> value,
        IEnumerable<string> requiredFields,
        string location,
        string sourceName)
    {
        foreach (string fieldName in requiredFields)
        {
            if (!value.ContainsKey(fieldName))
            {
                throw new InvalidOperationException(
                    $"Declarative gate {location} requires {fieldName}: {sourceName}");
            }
        }
    }

    private static IDictionary<string, object> RequireObject(object value, string location, string sourceName)
    {
        return value as IDictionary<string, object> ?? throw new InvalidOperationException(
            $"Declarative gate {location} must be an object: {sourceName}");
    }

    private static IList<object> RequireArray(object value, string location, string sourceName)
    {
        return value as IList<object> ?? throw new InvalidOperationException(
            $"Declarative gate {location} must be an array: {sourceName}");
    }

    private static void RequireString(object value, string location, string sourceName)
    {
        if (!(value is string))
        {
            throw new InvalidOperationException($"Declarative gate {location} must be a string: {sourceName}");
        }
    }

    private static void RequireBoolean(object value, string location, string sourceName)
    {
        if (!(value is bool))
        {
            throw new InvalidOperationException($"Declarative gate {location} must be a boolean: {sourceName}");
        }
    }

    private static void RequireNumber(object value, string location, string sourceName)
    {
        if (!(value is double))
        {
            throw new InvalidOperationException($"Declarative gate {location} must be a number: {sourceName}");
        }
    }

    private static void RequireInteger(object value, string location, string sourceName)
    {
        RequireNumber(value, location, sourceName);
        double number = (double)value;
        if (number != Math.Truncate(number))
        {
            throw new InvalidOperationException($"Declarative gate {location} must be an integer: {sourceName}");
        }
    }

    private static void RequireNonNegativeInteger(object value, string location, string sourceName)
    {
        RequireInteger(value, location, sourceName);
        if ((double)value < 0d)
        {
            throw new InvalidOperationException(
                $"Declarative gate {location} must be a non-negative integer: {sourceName}");
        }
    }

    private static void ValidateCheck(DeclarativeSceneGateCheck check, ISet<string> checkIds)
    {
        if (check == null || string.IsNullOrWhiteSpace(check.id) || !checkIds.Add(check.id))
        {
            throw new InvalidOperationException("Declarative gate check IDs must be non-empty and unique.");
        }

        if (HarnessToolPolicy.ValidateHierarchyPath(check.path).IsFailure)
        {
            throw new InvalidOperationException($"Declarative gate check {check.id} has an invalid path: {check.path}");
        }

        if (string.IsNullOrWhiteSpace(check.passMessage) ||
            string.IsNullOrWhiteSpace(check.failMessage) ||
            string.IsNullOrWhiteSpace(check.expected))
        {
            throw new InvalidOperationException(
                $"Declarative gate check {check.id} requires passMessage, failMessage, and expected.");
        }

        switch (check.type)
        {
            case DeclarativeSceneGateCheckType.ObjectLayout:
                RequireVector(check.id, "localPosition", check.localPosition);
                RequireQuaternion(check.id, check.localRotation);
                RequireVector(check.id, "localScale", check.localScale);
                break;
            case DeclarativeSceneGateCheckType.ExactChildren:
                if (check.children == null)
                {
                    throw new InvalidOperationException(
                        $"Declarative gate check {check.id} requires a children array.");
                }

                HashSet<string> childNames = new HashSet<string>(StringComparer.Ordinal);
                foreach (string childName in check.children)
                {
                    if (childName == null || !childNames.Add(childName))
                    {
                        throw new InvalidOperationException(
                            $"Declarative gate check {check.id} children must be non-null and unique.");
                    }
                }
                break;
            case DeclarativeSceneGateCheckType.LineRendererShape:
                if (check.points == null || check.points.Length < 2 ||
                    check.startWidth <= 0f || check.endWidth <= 0f)
                {
                    throw new InvalidOperationException(
                        $"Declarative gate check {check.id} requires points and positive widths.");
                }

                foreach (DeclarativeVector3 point in check.points)
                {
                    RequireVector(check.id, "point", point);
                }
                break;
            case DeclarativeSceneGateCheckType.LineRendererMaterial:
                if (string.IsNullOrWhiteSpace(check.materialPath) ||
                    !check.materialPath.StartsWith("Assets/", StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        $"Declarative gate check {check.id} requires an Assets materialPath.");
                }
                break;
            default:
                throw new InvalidOperationException(
                    $"Declarative gate check {check.id} uses unsupported type: {check.type}");
        }
    }

    private static string ValidateManifestPath(string projectRelativePath)
    {
        if (string.IsNullOrWhiteSpace(projectRelativePath))
        {
            throw new ArgumentException("A declarative gate manifest path is required.", nameof(projectRelativePath));
        }

        string normalized = projectRelativePath.Replace('\\', '/');
        if (normalized.Contains("..", StringComparison.Ordinal) ||
            !normalized.StartsWith(AllowedManifestRoot + "/", StringComparison.Ordinal) ||
            !string.Equals(Path.GetExtension(normalized), ".json", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Declarative gate manifest must be a JSON file under {AllowedManifestRoot}: {projectRelativePath}");
        }

        return normalized;
    }

    private static bool IsProjectScenePath(string scenePath)
    {
        return !string.IsNullOrWhiteSpace(scenePath) &&
               scenePath.StartsWith("Assets/", StringComparison.Ordinal) &&
               !scenePath.Contains("..", StringComparison.Ordinal) &&
               string.Equals(Path.GetExtension(scenePath), ".unity", StringComparison.OrdinalIgnoreCase);
    }

    private static void RequireVector(string checkId, string fieldName, DeclarativeVector3 value)
    {
        if (value == null)
        {
            throw new InvalidOperationException(
                $"Declarative gate check {checkId} requires {fieldName}.");
        }
    }

    private static void RequireQuaternion(string checkId, DeclarativeQuaternion value)
    {
        if (value == null)
        {
            throw new InvalidOperationException(
                $"Declarative gate check {checkId} requires localRotation.");
        }
    }
}
