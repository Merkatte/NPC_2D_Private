using System;

internal static class HarnessToolTemplate
{
    public static HarnessStep Create(string toolId)
    {
        HarnessJob recipe = HarnessBeaconRecipe.Create("template");
        foreach (HarnessStep step in recipe.steps)
        {
            if (step.tool == toolId)
            {
                step.id = "manual-step";
                return step;
            }
        }

        throw new InvalidOperationException($"No template is registered for Tool: {toolId}");
    }
}
