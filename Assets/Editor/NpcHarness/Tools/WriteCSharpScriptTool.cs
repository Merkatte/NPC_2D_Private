using System;
using System.IO;
using System.Text;

internal sealed class WriteCSharpScriptTool : IHarnessTool
{
    public string Id => "WriteCSharpScript";

    public HarnessToolResult Validate(HarnessToolContext context, HarnessStep step)
    {
        HarnessToolResult pathResult = HarnessToolPolicy.ValidateAssetPath(step.assetPath, ".cs");
        if (pathResult.IsFailure)
        {
            return pathResult;
        }

        return string.IsNullOrWhiteSpace(step.source)
            ? HarnessToolResult.ValidationFailure("C# source is required.")
            : HarnessToolResult.Success("WriteCSharpScript input is valid.");
    }

    public HarnessToolResult Execute(HarnessToolContext context, HarnessStep step)
    {
        string absolutePath = HarnessToolPolicy.GetAbsoluteProjectPath(step.assetPath);
        string normalizedRequested = NormalizeNewlines(step.source);
        if (File.Exists(absolutePath))
        {
            string normalizedExisting = NormalizeNewlines(File.ReadAllText(absolutePath));
            if (string.Equals(normalizedExisting, normalizedRequested, StringComparison.Ordinal))
            {
                return HarnessToolResult.NoChange($"C# script already matches: {step.assetPath}");
            }

            if (!context.Options.AllowOverwrite)
            {
                return HarnessToolResult.Failure(
                    $"C# script differs and overwrite is not approved: {step.assetPath}");
            }
        }

        Directory.CreateDirectory(Path.GetDirectoryName(absolutePath) ??
                                  throw new InvalidOperationException("Script directory could not be resolved."));
        File.WriteAllText(absolutePath, step.source, new UTF8Encoding(false));
        return HarnessToolResult.AwaitingCompilation($"Wrote C# script: {step.assetPath}");
    }

    private static string NormalizeNewlines(string value)
    {
        return value.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');
    }
}
