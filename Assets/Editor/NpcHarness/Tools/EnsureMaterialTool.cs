using UnityEditor;
using UnityEngine;

internal sealed class EnsureMaterialTool : IHarnessTool
{
    public string Id => "EnsureMaterial";

    public HarnessToolResult Validate(HarnessToolContext context, HarnessStep step)
    {
        HarnessToolResult pathResult = HarnessToolPolicy.ValidateAssetPath(step.assetPath, ".mat");
        if (pathResult.IsFailure)
        {
            return pathResult;
        }

        return HarnessToolPolicy.IsShaderAllowed(step.shader)
            ? HarnessToolResult.Success("EnsureMaterial input is valid.")
            : HarnessToolResult.ValidationFailure($"Shader is not allowed: {step.shader}");
    }

    public HarnessToolResult Execute(HarnessToolContext context, HarnessStep step)
    {
        Shader shader = Shader.Find(step.shader);
        if (!shader)
        {
            return HarnessToolResult.Failure($"Shader was not found: {step.shader}");
        }

        Color requestedColor = HarnessValueUtility.ToColor(step.color);
        Material material = AssetDatabase.LoadAssetAtPath<Material>(step.assetPath);
        if (!material)
        {
            material = new Material(shader)
            {
                name = System.IO.Path.GetFileNameWithoutExtension(step.assetPath),
                color = requestedColor,
            };
            AssetDatabase.CreateAsset(material, step.assetPath);
            context.MarkCreated(material);
            if (context.Options.Interactive)
            {
                Undo.RegisterCreatedObjectUndo(material, "Create Harness Material");
            }

            return HarnessToolResult.Success($"Created material: {step.assetPath}");
        }

        bool isDifferent = material.shader != shader ||
                           !HarnessValueUtility.Approximately(material.color, requestedColor);
        HarnessToolResult permission = HarnessValueUtility.RequireOverwrite(
            context,
            material,
            isDifferent,
            $"Material {step.assetPath}");
        if (permission.State == HarnessRunState.NoChange || permission.IsFailure)
        {
            return permission;
        }

        if (context.Options.Interactive)
        {
            Undo.RecordObject(material, "Configure Harness Material");
        }

        material.shader = shader;
        material.color = requestedColor;
        EditorUtility.SetDirty(material);
        return HarnessToolResult.Success($"Updated material: {step.assetPath}");
    }
}
