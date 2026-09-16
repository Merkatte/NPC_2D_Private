using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

internal sealed class ConfigureCameraTool : IHarnessTool
{
    public string Id => "ConfigureCamera";

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

        if (string.IsNullOrWhiteSpace(step.tag))
        {
            return HarnessToolResult.ValidationFailure("Camera tag is required.");
        }

        return step.orthographicSize > 0f
            ? HarnessToolResult.Success("ConfigureCamera input is valid.")
            : HarnessToolResult.ValidationFailure("Camera orthographic size must be greater than zero.");
    }

    public HarnessToolResult Execute(HarnessToolContext context, HarnessStep step)
    {
        GameObject target = context.FindSingleGameObject(step.scenePath, step.targetPath);
        Camera camera = target.GetComponent<Camera>();
        if (!camera)
        {
            return HarnessToolResult.Failure($"Camera component is missing: {step.targetPath}");
        }

        Color backgroundColor = HarnessValueUtility.ToColor(step.backgroundColor);
        bool isDifferent = camera.orthographic != step.orthographic ||
                           !HarnessValueUtility.Approximately(camera.orthographicSize, step.orthographicSize) ||
                           camera.clearFlags != CameraClearFlags.SolidColor ||
                           !HarnessValueUtility.Approximately(camera.backgroundColor, backgroundColor) ||
                           target.tag != step.tag;
        bool mayChange = context.Options.AllowOverwrite || context.WasCreated(camera) || context.WasCreated(target);
        if (!isDifferent)
        {
            return HarnessToolResult.NoChange($"Camera already matches: {step.targetPath}");
        }

        if (!mayChange)
        {
            return HarnessToolResult.Failure(
                $"Camera settings differ and overwrite is not approved: {step.targetPath}");
        }

        if (context.Options.Interactive)
        {
            Undo.RecordObjects(new Object[] { target, camera }, "Configure Harness Camera");
        }

        target.tag = step.tag;
        camera.orthographic = step.orthographic;
        camera.orthographicSize = step.orthographicSize;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = backgroundColor;
        EditorSceneManager.MarkSceneDirty(target.scene);
        return HarnessToolResult.Success($"Configured Camera: {step.targetPath}");
    }
}
