using System.Text;

namespace NpcHarness;

internal static class Program
{
    private static async Task<int> Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;

        if (!HarnessOptions.TryParse(args, out HarnessOptions? options, out string error))
        {
            Console.Error.WriteLine(error);
            PrintUsage();
            return 64;
        }

        try
        {
            string repositoryRoot = FindRepositoryRoot(Environment.CurrentDirectory);
            HarnessOrchestrator orchestrator = new HarnessOrchestrator(repositoryRoot, options!);
            return await orchestrator.RunAsync();
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"Harness failed before initialization: {exception.Message}");
            return 1;
        }
    }

    private static string FindRepositoryRoot(string startDirectory)
    {
        DirectoryInfo? directory = new DirectoryInfo(startDirectory);
        while (directory != null)
        {
            string projectVersionPath = Path.Combine(directory.FullName, "ProjectSettings", "ProjectVersion.txt");
            string assetsPath = Path.Combine(directory.FullName, "Assets");
            if (File.Exists(projectVersionPath) && Directory.Exists(assetsPath))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Run the harness from inside the Unity project repository.");
    }

    private static void PrintUsage()
    {
        Console.WriteLine(
            "Usage: run-harness.cmd \"<natural language request>\" [--dry-run] [--approve] [--allow-overwrite]");
    }
}

internal sealed record HarnessOptions(string Request, bool DryRun, bool Approve, bool AllowOverwrite)
{
    public static bool TryParse(string[] args, out HarnessOptions? options, out string error)
    {
        bool dryRun = false;
        bool approve = false;
        bool allowOverwrite = false;
        List<string> requestParts = new List<string>();

        foreach (string argument in args)
        {
            switch (argument)
            {
                case "--dry-run":
                    dryRun = true;
                    break;
                case "--approve":
                    approve = true;
                    break;
                case "--allow-overwrite":
                    allowOverwrite = true;
                    break;
                default:
                    if (argument.StartsWith("--", StringComparison.Ordinal))
                    {
                        options = null;
                        error = $"Unknown option: {argument}";
                        return false;
                    }

                    requestParts.Add(argument);
                    break;
            }
        }

        string request = string.Join(" ", requestParts).Trim();
        if (request.Length == 0)
        {
            options = null;
            error = "A natural-language request is required.";
            return false;
        }

        options = new HarnessOptions(request, dryRun, approve, allowOverwrite);
        error = string.Empty;
        return true;
    }
}
