using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

internal sealed class EnsureComponentTool : IHarnessTool
{
    public string Id => "EnsureComponent";

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

        return HarnessToolPolicy.IsComponentAllowed(step.componentTypeId)
            ? HarnessToolResult.Success("EnsureComponent input is valid.")
            : HarnessToolResult.ValidationFailure($"Component type is not allowed: {step.componentTypeId}");
    }

    public HarnessToolResult Execute(HarnessToolContext context, HarnessStep step)
    {
        GameObject target = context.FindSingleGameObject(step.scenePath, step.targetPath);
        Type componentType = HarnessToolContext.ResolveAllowedComponentType(step.componentTypeId);
        Component[] matches = target.GetComponents(componentType);
        if (matches.Length > 1)
        {
            return HarnessToolResult.Failure(
                $"Expected at most one {step.componentTypeId} on {step.targetPath}, found {matches.Length}.");
        }

        if (matches.Length == 1)
        {
            return HarnessToolResult.NoChange(
                $"Component already exists: {step.targetPath} / {step.componentTypeId}");
        }

        Component created = context.Options.Interactive
            ? Undo.AddComponent(target, componentType)
            : target.AddComponent(componentType);
        context.MarkCreated(created);
        EditorSceneManager.MarkSceneDirty(target.scene);
        return HarnessToolResult.Success($"Added component: {step.targetPath} / {step.componentTypeId}");
    }
}
