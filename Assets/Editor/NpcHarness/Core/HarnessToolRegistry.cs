using System;
using System.Collections.Generic;

internal static class HarnessToolRegistry
{
    private static readonly Dictionary<string, IHarnessTool> Tools = CreateTools();

    public static IReadOnlyCollection<string> ToolIds => Tools.Keys;

    public static bool TryGet(string toolId, out IHarnessTool tool)
    {
        return Tools.TryGetValue(toolId, out tool);
    }

    private static Dictionary<string, IHarnessTool> CreateTools()
    {
        IHarnessTool[] tools =
        {
            new WriteCSharpScriptTool(),
            new EnsureSceneTool(),
            new EnsureGameObjectTool(),
            new SetTransformTool(),
            new EnsureComponentTool(),
            new EnsureMaterialTool(),
            new ConfigureCameraTool(),
            new ConfigureLineRendererTool(),
            new SaveSceneTool(),
        };

        Dictionary<string, IHarnessTool> registry = new Dictionary<string, IHarnessTool>(StringComparer.Ordinal);
        foreach (IHarnessTool tool in tools)
        {
            registry.Add(tool.Id, tool);
        }

        return registry;
    }
}
