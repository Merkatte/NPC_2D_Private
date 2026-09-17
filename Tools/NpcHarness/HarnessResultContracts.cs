using System.Text.Json;

namespace NpcHarness;

internal enum GateOutcome
{
    Pass,
    Fail,
    InfrastructureError,
}

internal sealed record GateResultSummary(
    GateOutcome Outcome,
    string RunId,
    string Profile,
    int ProfileVersion,
    string Message,
    int CheckCount,
    int FailedCheckCount,
    IReadOnlyList<string> ChangedFiles);

internal sealed record AdapterResultSummary(bool Success, string State, string Message);

internal static class HarnessResultContracts
{
    public static GateResultSummary ReadGateResult(string path)
    {
        using JsonDocument document = JsonDocument.Parse(File.ReadAllText(path));
        JsonElement root = document.RootElement;
        RequireObject(root, "GateResult");
        RequireOnlyProperties(
            root,
            "GateResult",
            "schemaVersion", "runId", "profile", "profileVersion", "success", "status", "message",
            "checks", "artifacts", "changedFiles", "startedAtUtc", "finishedAtUtc");

        RequireNumber(root, "schemaVersion", expected: 1);
        string runId = RequireNonEmptyString(root, "runId");
        string profile = RequireNonEmptyString(root, "profile");
        int profileVersion = RequirePositiveNumber(root, "profileVersion");
        bool success = RequireBoolean(root, "success");
        string status = RequireNonEmptyString(root, "status");
        string message = RequireNonEmptyString(root, "message");
        JsonElement checks = RequireArray(root, "checks");
        JsonElement artifacts = RequireArray(root, "artifacts");
        JsonElement changedFiles = RequireArray(root, "changedFiles");
        RequireNonEmptyString(root, "startedAtUtc");
        RequireNonEmptyString(root, "finishedAtUtc");

        GateOutcome outcome = status switch
        {
            "Pass" => GateOutcome.Pass,
            "Fail" => GateOutcome.Fail,
            "InfrastructureError" => GateOutcome.InfrastructureError,
            _ => throw new InvalidDataException($"GateResult has an unsupported status: {status}"),
        };
        if (success != (outcome == GateOutcome.Pass))
        {
            throw new InvalidDataException("GateResult success does not match status.");
        }

        int failedCheckCount = ValidateChecks(checks);
        if (outcome is GateOutcome.Pass or GateOutcome.Fail && checks.GetArrayLength() == 0)
        {
            throw new InvalidDataException($"GateResult {outcome} must contain at least one check.");
        }

        if (outcome == GateOutcome.Pass && failedCheckCount != 0)
        {
            throw new InvalidDataException("GateResult Pass contains one or more failed checks.");
        }

        if (outcome == GateOutcome.Fail && failedCheckCount == 0)
        {
            throw new InvalidDataException("GateResult Fail must contain at least one failed check.");
        }

        ValidateArtifacts(artifacts);
        IReadOnlyList<string> changedFilePaths = ReadStringArray(changedFiles, "changedFiles");
        return new GateResultSummary(
            outcome,
            runId,
            profile,
            profileVersion,
            message,
            checks.GetArrayLength(),
            failedCheckCount,
            changedFilePaths);
    }

    public static AdapterResultSummary ReadAdapterResult(string path)
    {
        using JsonDocument document = JsonDocument.Parse(File.ReadAllText(path));
        JsonElement root = document.RootElement;
        RequireObject(root, "adapter result");
        bool success = RequireBoolean(root, "success");
        string state = RequireNonEmptyString(root, "state");
        string message = RequireNonEmptyString(root, "message");
        if (success && state != "Succeeded" && state != "NoChange")
        {
            throw new InvalidDataException("Adapter result success does not match state.");
        }

        if (!success && (state == "Succeeded" || state == "NoChange"))
        {
            throw new InvalidDataException("Adapter result failure does not match state.");
        }

        return new AdapterResultSummary(success, state, message);
    }

    public static void ValidateJob(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("Harness Job does not exist.", path);
        }

        using JsonDocument document = JsonDocument.Parse(File.ReadAllText(path));
        JsonElement root = document.RootElement;
        RequireObject(root, "Harness Job");
        RequireNumber(root, "schemaVersion", expected: 1);
        RequireNonEmptyString(root, "jobId");
        JsonElement steps = RequireArray(root, "steps");
        if (steps.GetArrayLength() == 0)
        {
            throw new InvalidDataException("Harness Job steps must not be empty.");
        }
    }

    private static int ValidateChecks(JsonElement checks)
    {
        HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
        int failedCheckCount = 0;
        foreach (JsonElement check in checks.EnumerateArray())
        {
            RequireObject(check, "GateResult check");
            RequireOnlyProperties(
                check,
                "GateResult check",
                "id", "success", "status", "message", "expected", "actual");
            string id = RequireNonEmptyString(check, "id");
            if (!ids.Add(id))
            {
                throw new InvalidDataException($"GateResult contains a duplicate check ID: {id}");
            }

            bool success = RequireBoolean(check, "success");
            string status = RequireNonEmptyString(check, "status");
            if ((status != "Pass" && status != "Fail") || success != (status == "Pass"))
            {
                throw new InvalidDataException($"GateResult check {id} has inconsistent success and status.");
            }

            if (!success)
            {
                failedCheckCount++;
            }

            RequireString(check, "message");
            RequireString(check, "expected");
            RequireString(check, "actual");
        }

        return failedCheckCount;
    }

    private static void ValidateArtifacts(JsonElement artifacts)
    {
        foreach (JsonElement artifact in artifacts.EnumerateArray())
        {
            RequireObject(artifact, "GateResult artifact");
            RequireOnlyProperties(artifact, "GateResult artifact", "kind", "path");
            RequireNonEmptyString(artifact, "kind");
            RequireNonEmptyString(artifact, "path");
        }
    }

    private static IReadOnlyList<string> ReadStringArray(JsonElement array, string name)
    {
        List<string> values = new List<string>();
        foreach (JsonElement value in array.EnumerateArray())
        {
            if (value.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(value.GetString()))
            {
                throw new InvalidDataException($"GateResult {name} must contain non-empty strings.");
            }

            values.Add(value.GetString()!);
        }

        return values;
    }

    private static void RequireObject(JsonElement value, string name)
    {
        if (value.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidDataException($"{name} must be a JSON object.");
        }
    }

    private static void RequireOnlyProperties(JsonElement value, string name, params string[] allowedNames)
    {
        HashSet<string> allowed = new HashSet<string>(allowedNames, StringComparer.Ordinal);
        foreach (JsonProperty property in value.EnumerateObject())
        {
            if (!allowed.Contains(property.Name))
            {
                throw new InvalidDataException($"{name} contains an unsupported property: {property.Name}");
            }
        }
    }

    private static string RequireNonEmptyString(JsonElement parent, string name)
    {
        string value = RequireString(parent, name);
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidDataException($"{name} must not be empty.");
        }

        return value;
    }

    private static string RequireString(JsonElement parent, string name)
    {
        if (!parent.TryGetProperty(name, out JsonElement value) || value.ValueKind != JsonValueKind.String)
        {
            throw new InvalidDataException($"Missing or invalid string property: {name}");
        }

        return value.GetString()!;
    }

    private static bool RequireBoolean(JsonElement parent, string name)
    {
        if (!parent.TryGetProperty(name, out JsonElement value) ||
            (value.ValueKind != JsonValueKind.True && value.ValueKind != JsonValueKind.False))
        {
            throw new InvalidDataException($"Missing or invalid boolean property: {name}");
        }

        return value.GetBoolean();
    }

    private static void RequireNumber(JsonElement parent, string name, int expected)
    {
        if (!parent.TryGetProperty(name, out JsonElement value) ||
            !value.TryGetInt32(out int actual) ||
            actual != expected)
        {
            throw new InvalidDataException($"{name} must be {expected}.");
        }
    }

    private static int RequirePositiveNumber(JsonElement parent, string name)
    {
        if (!parent.TryGetProperty(name, out JsonElement value) ||
            !value.TryGetInt32(out int actual) ||
            actual <= 0)
        {
            throw new InvalidDataException($"{name} must be a positive integer.");
        }

        return actual;
    }

    private static JsonElement RequireArray(JsonElement parent, string name)
    {
        if (!parent.TryGetProperty(name, out JsonElement value) || value.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidDataException($"Missing or invalid array property: {name}");
        }

        return value;
    }
}
