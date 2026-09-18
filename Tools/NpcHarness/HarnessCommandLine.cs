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

internal sealed record VerifyScopeCommand(
    string PolicyPath,
    string AssignmentPath,
    string AssignmentSha256,
    string? RunId,
    string? OutputPath) : HarnessCommand;

internal sealed record SelfTestCommand : HarnessCommand;

internal sealed record ReviewEvidenceCommand(
    bool CreateSnapshot, string RequestPath, string RequestSha256, string? SnapshotSha256) : HarnessCommand;

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
            case "review-snapshot":
            case "accept-review":
                return TryParseReviewEvidence(args, out command, out error);
            case "verify":
                return TryParseVerify(args, out command, out error);
            case "run-adapter":
                return TryParseAdapter(args, out command, out error);
            case "verify-scope":
                return TryParseScope(args, out command, out error);
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
        writer.WriteLine("  verify --profile <beacon-structure|beacon-playmode|square-character-structure|farmer-scene-structure> [options]");
        writer.WriteLine("  verify-scope --policy <path> --assignment <path> --assignment-sha256 <hash> [options]");
        writer.WriteLine("  run-adapter --job <path> [options]");
        writer.WriteLine("  self-test");
        writer.WriteLine("  review-snapshot --request <path> --request-sha256 <hash>");
        writer.WriteLine("  accept-review --request <path> --request-sha256 <hash> --snapshot-sha256 <hash>");
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
        writer.WriteLine("verify-scope options:");
        writer.WriteLine("  --policy <path>      Tracked SkillPolicy JSON under Tools/NpcHarness/SkillPolicies");
        writer.WriteLine("  --assignment <path>  WorkerAssignment JSON under the selected run directory");
        writer.WriteLine("  --assignment-sha256  Root-recorded pre-delegation WorkerAssignment SHA-256");
        writer.WriteLine();
        writer.WriteLine("Unity can also be set with NPC_HARNESS_UNITY_PATH.");
    }

    private static bool TryParseReviewEvidence(
        IReadOnlyList<string> args, out HarnessCommand? command, out string error)
    {
        command = null;
        error = string.Empty;
        bool snapshot = args[0] == "review-snapshot";
        Dictionary<string, string> options = new(StringComparer.Ordinal);
        for (int index = 1; index < args.Count; index += 2)
        {
            string option = args[index];
            if ((option != "--request" && option != "--request-sha256" &&
                 (snapshot || option != "--snapshot-sha256")) || index + 1 >= args.Count ||
                string.IsNullOrWhiteSpace(args[index + 1]) || !options.TryAdd(option, args[index + 1]))
            {
                error = "Unknown, duplicate, or incomplete review evidence option: " + option;
                return false;
            }
        }
        if (!options.TryGetValue("--request", out string? request) ||
            !options.TryGetValue("--request-sha256", out string? requestHash) ||
            (!snapshot && !options.ContainsKey("--snapshot-sha256")))
        {
            error = "Review evidence commands require --request, --request-sha256, and accept-review requires --snapshot-sha256.";
            return false;
        }
        string? snapshotHash = options.GetValueOrDefault("--snapshot-sha256");
        if (new[] { requestHash, snapshotHash }.Where(h => h != null).Any(h => h!.Length != 64 || !h.All(Uri.IsHexDigit)))
        {
            error = "Review evidence digests must be 64 hexadecimal characters.";
            return false;
        }
        command = new ReviewEvidenceCommand(snapshot, request, requestHash, snapshotHash);
        return true;
    }

    private static bool TryParseVerify(
        IReadOnlyList<string> args,
        out HarnessCommand? command,
        out string error)
    {
        if (!TryReadOptions(
                args,
                1,
                allowJob: false,
                allowOverwrite: false,
                allowScope: false,
                out ParsedOptions options,
                out error))
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
        if (!TryReadOptions(
                args,
                1,
                allowJob: true,
                allowOverwrite: true,
                allowScope: false,
                out ParsedOptions options,
                out error))
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

    private static bool TryParseScope(
        IReadOnlyList<string> args,
        out HarnessCommand? command,
        out string error)
    {
        if (!TryReadOptions(
                args,
                1,
                allowJob: false,
                allowOverwrite: false,
                allowScope: true,
                out ParsedOptions options,
                out error))
        {
            command = null;
            return false;
        }

        if (string.IsNullOrWhiteSpace(options.PolicyPath) ||
            string.IsNullOrWhiteSpace(options.AssignmentPath) ||
            string.IsNullOrWhiteSpace(options.AssignmentSha256))
        {
            command = null;
            error = "verify-scope requires --policy, --assignment, and --assignment-sha256.";
            return false;
        }

        if (options.AssignmentSha256.Length != 64 ||
            options.AssignmentSha256.Any(character => !Uri.IsHexDigit(character)))
        {
            command = null;
            error = "--assignment-sha256 must be a 64-character SHA-256 value.";
            return false;
        }

        if (!string.IsNullOrWhiteSpace(options.Profile) ||
            !string.IsNullOrWhiteSpace(options.UnityPath))
        {
            command = null;
            error = "--profile and --unity are not valid for verify-scope.";
            return false;
        }

        command = new VerifyScopeCommand(
            options.PolicyPath,
            options.AssignmentPath,
            options.AssignmentSha256.ToLowerInvariant(),
            options.RunId,
            options.OutputPath);
        return true;
    }

    private static bool TryReadOptions(
        IReadOnlyList<string> args,
        int startIndex,
        bool allowJob,
        bool allowOverwrite,
        bool allowScope,
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
                case "--policy" when allowScope:
                    options.PolicyPath = value;
                    break;
                case "--assignment" when allowScope:
                    options.AssignmentPath = value;
                    break;
                case "--assignment-sha256" when allowScope:
                    options.AssignmentSha256 = value;
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
        public string? PolicyPath { get; set; }
        public string? AssignmentPath { get; set; }
        public string? AssignmentSha256 { get; set; }
        public string? UnityPath { get; set; }
        public string? RunId { get; set; }
        public string? OutputPath { get; set; }
        public TimeSpan Timeout { get; set; }
        public bool AllowOverwrite { get; set; }
    }
}
