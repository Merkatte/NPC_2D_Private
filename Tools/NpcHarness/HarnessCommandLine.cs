namespace NpcHarness;

internal abstract record HarnessCommand;

internal sealed record VerifyCommand(
    string Profile,
    string? UnityPath,
    string? RunId,
    string? OutputPath,
    TimeSpan Timeout) : HarnessCommand;

internal sealed record RunAdapterCommand(
    string JobPath,
    string? UnityPath,
    string? RunId,
    string? OutputPath,
    TimeSpan Timeout,
    bool AllowOverwrite) : HarnessCommand;

internal sealed record SelfTestCommand : HarnessCommand;

internal static class HarnessCommandLine
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromMinutes(5);

    public static bool TryParse(
        IReadOnlyList<string> args,
        out HarnessCommand? command,
        out string error)
    {
        command = null;
        error = string.Empty;
        if (args.Count == 0)
        {
            error = "A command is required.";
            return false;
        }

        switch (args[0])
        {
            case "verify":
                return TryParseVerify(args, out command, out error);
            case "run-adapter":
                return TryParseAdapter(args, out command, out error);
            case "self-test":
                if (args.Count != 1)
                {
                    error = "self-test does not accept options.";
                    return false;
                }

                command = new SelfTestCommand();
                return true;
            case "--help":
            case "-h":
            case "help":
                error = "Help requested.";
                return false;
            default:
                error = $"Unknown command: {args[0]}";
                return false;
        }
    }

    public static void PrintUsage(TextWriter writer)
    {
        writer.WriteLine("NPC Harness deterministic runner");
        writer.WriteLine();
        writer.WriteLine("Commands:");
        writer.WriteLine("  verify --profile <beacon-structure|beacon-playmode> [options]");
        writer.WriteLine("  run-adapter --job <path> [options]");
        writer.WriteLine("  self-test");
        writer.WriteLine();
        writer.WriteLine("Shared options:");
        writer.WriteLine("  --unity <path>       Unity executable override");
        writer.WriteLine("  --run-id <id>        Stable run identifier");
        writer.WriteLine("  --output <path>      Result JSON path");
        writer.WriteLine("  --timeout <seconds>  Process timeout (default: 300)");
        writer.WriteLine();
        writer.WriteLine("run-adapter options:");
        writer.WriteLine("  --allow-overwrite    Permit adapter overwrites allowed by Unity policy");
        writer.WriteLine();
        writer.WriteLine("Unity can also be set with NPC_HARNESS_UNITY_PATH.");
    }

    private static bool TryParseVerify(
        IReadOnlyList<string> args,
        out HarnessCommand? command,
        out string error)
    {
        if (!TryReadOptions(args, 1, allowJob: false, allowOverwrite: false, out ParsedOptions options, out error))
        {
            command = null;
            return false;
        }

        if (string.IsNullOrWhiteSpace(options.Profile))
        {
            command = null;
            error = "verify requires --profile.";
            return false;
        }

        command = new VerifyCommand(
            options.Profile,
            options.UnityPath,
            options.RunId,
            options.OutputPath,
            options.Timeout);
        return true;
    }

    private static bool TryParseAdapter(
        IReadOnlyList<string> args,
        out HarnessCommand? command,
        out string error)
    {
        if (!TryReadOptions(args, 1, allowJob: true, allowOverwrite: true, out ParsedOptions options, out error))
        {
            command = null;
            return false;
        }

        if (string.IsNullOrWhiteSpace(options.JobPath))
        {
            command = null;
            error = "run-adapter requires --job.";
            return false;
        }

        if (!string.IsNullOrWhiteSpace(options.Profile))
        {
            command = null;
            error = "--profile is only valid for verify.";
            return false;
        }

        command = new RunAdapterCommand(
            options.JobPath,
            options.UnityPath,
            options.RunId,
            options.OutputPath,
            options.Timeout,
            options.AllowOverwrite);
        return true;
    }

    private static bool TryReadOptions(
        IReadOnlyList<string> args,
        int startIndex,
        bool allowJob,
        bool allowOverwrite,
        out ParsedOptions options,
        out string error)
    {
        options = new ParsedOptions { Timeout = DefaultTimeout };
        error = string.Empty;
        HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);

        for (int index = startIndex; index < args.Count; index++)
        {
            string option = args[index];
            if (!seen.Add(option))
            {
                error = $"Option was provided more than once: {option}";
                return false;
            }

            if (option == "--allow-overwrite")
            {
                if (!allowOverwrite)
                {
                    error = $"Option is not valid for this command: {option}";
                    return false;
                }

                options.AllowOverwrite = true;
                continue;
            }

            if (index + 1 >= args.Count)
            {
                error = $"Option requires a value: {option}";
                return false;
            }

            string value = args[++index];
            switch (option)
            {
                case "--profile":
                    options.Profile = value;
                    break;
                case "--job" when allowJob:
                    options.JobPath = value;
                    break;
                case "--unity":
                    options.UnityPath = value;
                    break;
                case "--run-id":
                    options.RunId = value;
                    break;
                case "--output":
                    options.OutputPath = value;
                    break;
                case "--timeout":
                    if (!int.TryParse(value, out int seconds) || seconds <= 0)
                    {
                        error = "--timeout must be a positive integer number of seconds.";
                        return false;
                    }

                    options.Timeout = TimeSpan.FromSeconds(seconds);
                    break;
                default:
                    error = $"Unknown option: {option}";
                    return false;
            }
        }

        return true;
    }

    private sealed class ParsedOptions
    {
        public string? Profile { get; set; }
        public string? JobPath { get; set; }
        public string? UnityPath { get; set; }
        public string? RunId { get; set; }
        public string? OutputPath { get; set; }
        public TimeSpan Timeout { get; set; }
        public bool AllowOverwrite { get; set; }
    }
}
