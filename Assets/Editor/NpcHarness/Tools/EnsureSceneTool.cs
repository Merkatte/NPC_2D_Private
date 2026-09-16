using System.IO;

internal sealed class EnsureSceneTool : IHarnessTool
{
    public string Id => "EnsureScene";

    public HarnessToolResult Validate(HarnessToolContext context, HarnessStep step)
    {
        return HarnessToolPolicy.ValidateAssetPath(step.scenePath, ".unity");
    }

    public HarnessToolResult Execute(HarnessToolContext context, HarnessStep step)
    {
        bool existed = File.Exists(HarnessToolPolicy.GetAbsoluteProjectPath(step.scenePath));
        context.OpenOrCreateScene(step.scenePath);
        return existed
            ? HarnessToolResult.NoChange($"Scene already exists: {step.scenePath}")
            : HarnessToolResult.Success($"Created scene: {step.scenePath}");
    }
}
