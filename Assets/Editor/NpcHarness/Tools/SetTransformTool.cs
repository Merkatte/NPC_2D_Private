using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

internal sealed class SetTransformTool : IHarnessTool
{
    public string Id => "SetTransform";

    public HarnessToolResult Validate(HarnessToolContext context, HarnessStep step)
    {
        HarnessToolResult sceneResult = HarnessToolPolicy.ValidateAssetPath(step.scenePath, ".unity");
        return sceneResult.IsFailure
            ? sceneResult
            : HarnessToolPolicy.ValidateHierarchyPath(step.targetPath);
    }

    public HarnessToolResult Execute(HarnessToolContext context, HarnessStep step)
    {
        GameObject target = context.FindSingleGameObject(step.scenePath, step.targetPath);
        Vector3 position = HarnessValueUtility.ToVector3(step.position);
        Quaternion rotation = Quaternion.Euler(HarnessValueUtility.ToVector3(step.rotation));
        Vector3 scale = HarnessValueUtility.ToVector3(step.scale);
        bool isDifferent = !HarnessValueUtility.Approximately(target.transform.localPosition, position) ||
                           Quaternion.Angle(target.transform.localRotation, rotation) > 0.01f ||
                           !HarnessValueUtility.Approximately(target.transform.localScale, scale);

        HarnessToolResult permission = HarnessValueUtility.RequireOverwrite(
            context,
            target,
            isDifferent,
            $"Transform on {step.targetPath}");
        if (permission.State == HarnessRunState.NoChange || permission.IsFailure)
        {
            return permission;
        }

        if (context.Options.Interactive)
        {
            Undo.RecordObject(target.transform, "Set Harness Transform");
        }

        target.transform.localPosition = position;
        target.transform.localRotation = rotation;
        target.transform.localScale = scale;
        EditorSceneManager.MarkSceneDirty(target.scene);
        return HarnessToolResult.Success($"Configured Transform: {step.targetPath}");
    }
}
