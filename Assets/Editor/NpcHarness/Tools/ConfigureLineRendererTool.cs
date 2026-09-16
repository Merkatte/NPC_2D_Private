using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

internal sealed class ConfigureLineRendererTool : IHarnessTool
{
    public string Id => "ConfigureLineRenderer";

    public HarnessToolResult Validate(HarnessToolContext context, HarnessStep step)
    {
        HarnessToolResult sceneResult = HarnessToolPolicy.ValidateAssetPath(step.scenePath, ".unity");
        if (sceneResult.IsFailure)
        {
            return sceneResult;
        }

        HarnessToolResult targetResult = HarnessToolPolicy.ValidateHierarchyPath(step.targetPath);
        if (targetResult.IsFailure)
        {
            return targetResult;
        }

        HarnessToolResult materialResult = HarnessToolPolicy.ValidateAssetPath(step.materialPath, ".mat");
        if (materialResult.IsFailure)
        {
            return materialResult;
        }

        if (step.points == null || step.points.Length < 2)
        {
            return HarnessToolResult.ValidationFailure("LineRenderer requires at least two points.");
        }

        return step.width > 0f
            ? HarnessToolResult.Success("ConfigureLineRenderer input is valid.")
            : HarnessToolResult.ValidationFailure("LineRenderer width must be greater than zero.");
    }

    public HarnessToolResult Execute(HarnessToolContext context, HarnessStep step)
    {
        GameObject target = context.FindSingleGameObject(step.scenePath, step.targetPath);
        LineRenderer lineRenderer = target.GetComponent<LineRenderer>();
        if (!lineRenderer)
        {
            return HarnessToolResult.Failure($"LineRenderer component is missing: {step.targetPath}");
        }

        Material material = AssetDatabase.LoadAssetAtPath<Material>(step.materialPath);
        if (!material)
        {
            return HarnessToolResult.Failure($"Material is missing: {step.materialPath}");
        }

        Vector3[] points = new Vector3[step.points.Length];
        for (int index = 0; index < points.Length; index++)
        {
            points[index] = HarnessValueUtility.ToVector3(step.points[index]);
        }

        Color color = HarnessValueUtility.ToColor(step.color);
        bool isDifferent = lineRenderer.useWorldSpace != step.useWorldSpace ||
                           lineRenderer.loop != step.loop ||
                           lineRenderer.alignment != LineAlignment.View ||
                           lineRenderer.textureMode != LineTextureMode.Stretch ||
                           lineRenderer.positionCount != points.Length ||
                           !PositionsMatch(lineRenderer, points) ||
                           !HarnessValueUtility.Approximately(lineRenderer.startWidth, step.width) ||
                           !HarnessValueUtility.Approximately(lineRenderer.endWidth, step.width) ||
                           lineRenderer.numCapVertices != step.capVertices ||
                           lineRenderer.numCornerVertices != step.cornerVertices ||
                           !HarnessValueUtility.Approximately(lineRenderer.startColor, color) ||
                           !HarnessValueUtility.Approximately(lineRenderer.endColor, color) ||
                           lineRenderer.sortingOrder != step.sortingOrder ||
                           lineRenderer.sharedMaterial != material;
        HarnessToolResult permission = HarnessValueUtility.RequireOverwrite(
            context,
            lineRenderer,
            isDifferent,
            $"LineRenderer on {step.targetPath}");
        if (permission.State == HarnessRunState.NoChange || permission.IsFailure)
        {
            return permission;
        }

        if (context.Options.Interactive)
        {
            Undo.RecordObject(lineRenderer, "Configure Harness LineRenderer");
        }

        lineRenderer.useWorldSpace = step.useWorldSpace;
        lineRenderer.loop = step.loop;
        lineRenderer.alignment = LineAlignment.View;
        lineRenderer.textureMode = LineTextureMode.Stretch;
        lineRenderer.positionCount = points.Length;
        lineRenderer.SetPositions(points);
        lineRenderer.startWidth = step.width;
        lineRenderer.endWidth = step.width;
        lineRenderer.numCapVertices = step.capVertices;
        lineRenderer.numCornerVertices = step.cornerVertices;
        lineRenderer.startColor = color;
        lineRenderer.endColor = color;
        lineRenderer.sortingOrder = step.sortingOrder;
        lineRenderer.sharedMaterial = material;
        EditorSceneManager.MarkSceneDirty(target.scene);
        return HarnessToolResult.Success($"Configured LineRenderer: {step.targetPath}");
    }

    private static bool PositionsMatch(LineRenderer lineRenderer, Vector3[] requested)
    {
        if (lineRenderer.positionCount != requested.Length)
        {
            return false;
        }

        Vector3[] existing = new Vector3[requested.Length];
        lineRenderer.GetPositions(existing);
        for (int index = 0; index < requested.Length; index++)
        {
            if (!HarnessValueUtility.Approximately(existing[index], requested[index]))
            {
                return false;
            }
        }

        return true;
    }
}
