using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

internal sealed class SaveSceneTool : IHarnessTool
{
    public string Id => "SaveScene";

    public HarnessToolResult Validate(HarnessToolContext context, HarnessStep step)
    {
        return HarnessToolPolicy.ValidateAssetPath(step.scenePath, ".unity");
    }

    public HarnessToolResult Execute(HarnessToolContext context, HarnessStep step)
    {
        Scene scene = context.RequireScene(step.scenePath);
        bool wasDirty = scene.isDirty || string.IsNullOrEmpty(scene.path);
        if (!EditorSceneManager.SaveScene(scene, step.scenePath))
        {
            return HarnessToolResult.Failure($"Could not save scene: {step.scenePath}");
        }

        AssetDatabase.SaveAssets();
        return wasDirty
            ? HarnessToolResult.Success($"Saved scene: {step.scenePath}")
            : HarnessToolResult.NoChange($"Scene was already saved: {step.scenePath}");
    }
}
