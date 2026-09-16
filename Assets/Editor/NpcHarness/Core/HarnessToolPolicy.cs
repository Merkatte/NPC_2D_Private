using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

internal static class HarnessToolPolicy
{
    private const string DefaultAllowedRoot = "Assets/TestOnly";

    private static readonly HashSet<string> AllowedComponentTypeIds = new HashSet<string>(StringComparer.Ordinal)
    {
        "Camera",
        "HarnessTest",
        "LineRenderer",
    };

    private static readonly HashSet<string> AllowedShaders = new HashSet<string>(StringComparer.Ordinal)
    {
        "Sprites/Default",
    };

    public static HarnessToolResult ValidateAssetPath(string assetPath, string requiredExtension = "")
    {
        if (string.IsNullOrWhiteSpace(assetPath))
        {
            return HarnessToolResult.ValidationFailure("An asset path is required.");
        }

        string normalized = assetPath.Replace('\\', '/');
        if (normalized.Contains("..", StringComparison.Ordinal) ||
            (!string.Equals(normalized, DefaultAllowedRoot, StringComparison.Ordinal) &&
             !normalized.StartsWith(DefaultAllowedRoot + "/", StringComparison.Ordinal)))
        {
            return HarnessToolResult.ValidationFailure(
                $"Path is outside the allowed root {DefaultAllowedRoot}: {assetPath}");
        }

        if (!string.IsNullOrEmpty(requiredExtension) &&
            !string.Equals(Path.GetExtension(normalized), requiredExtension, StringComparison.OrdinalIgnoreCase))
        {
            return HarnessToolResult.ValidationFailure(
                $"Path must use the {requiredExtension} extension: {assetPath}");
        }

        return HarnessToolResult.Success("Asset path is allowed.");
    }

    public static HarnessToolResult ValidateHierarchyPath(string hierarchyPath, bool allowRoot = false)
    {
        if (allowRoot && hierarchyPath == "/")
        {
            return HarnessToolResult.Success("Root hierarchy path is valid.");
        }

        if (string.IsNullOrWhiteSpace(hierarchyPath) || !hierarchyPath.StartsWith("/", StringComparison.Ordinal))
        {
            return HarnessToolResult.ValidationFailure("Hierarchy paths must start with '/'.");
        }

        if (hierarchyPath.EndsWith("/", StringComparison.Ordinal) ||
            hierarchyPath.Contains("//", StringComparison.Ordinal) ||
            hierarchyPath.Contains("..", StringComparison.Ordinal))
        {
            return HarnessToolResult.ValidationFailure($"Invalid hierarchy path: {hierarchyPath}");
        }

        return HarnessToolResult.Success("Hierarchy path is valid.");
    }

    public static bool IsComponentAllowed(string componentTypeId)
    {
        return AllowedComponentTypeIds.Contains(componentTypeId);
    }

    public static bool IsShaderAllowed(string shaderName)
    {
        return AllowedShaders.Contains(shaderName);
    }

    public static string GetAbsoluteProjectPath(string assetPath)
    {
        string projectRoot = Directory.GetParent(Application.dataPath)?.FullName ??
                             throw new InvalidOperationException("Could not resolve the Unity project root.");
        return Path.GetFullPath(Path.Combine(projectRoot, assetPath.Replace('/', Path.DirectorySeparatorChar)));
    }
}
