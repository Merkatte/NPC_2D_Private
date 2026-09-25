using System.Globalization;
using System.Text.Json;

namespace NpcHarness;

internal static class UnityCliTransport
{
    public static string CreateEvalCode(string repositoryRoot, GateProfile profile, string runId, string resultPath)
    {
        string request = JsonSerializer.Serialize(new { runId, profile = profile.Name, resultPath = Path.GetFullPath(resultPath) });
        // JSON string escaping is also valid for these C# string literals. The code is
        // written to a file and passed via ArgumentList; it never becomes shell source.
        return "return HarnessInteractiveGateBridge.RunRequest(" + JsonSerializer.Serialize(request) + ", " +
            JsonSerializer.Serialize(Path.GetFullPath(repositoryRoot)) + ");";
    }

    public static string[] CreateArguments(string repositoryRoot, string scriptPath, TimeSpan timeout)
    {
        int milliseconds = (int)Math.Clamp(timeout.TotalMilliseconds, 1, 86_400_000);
        return new[] { "command", "--project-path", Path.GetFullPath(repositoryRoot), "--timeout",
            Math.Ceiling(timeout.TotalSeconds).ToString(CultureInfo.InvariantCulture),
            "eval_file", Path.GetFullPath(scriptPath), milliseconds.ToString(CultureInfo.InvariantCulture), "--json" };
    }

    public static string ReadAcknowledgement(string json, string repositoryRoot)
    {
        using JsonDocument document = JsonDocument.Parse(json);
        JsonElement root = document.RootElement;
        RequireSuccess(root, "Unity CLI");
        JsonElement data = root.GetProperty("data");
        string? project = data.GetProperty("target").GetProperty("projectPath").GetString();
        StringComparison comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        if (string.IsNullOrWhiteSpace(project) || !string.Equals(Path.TrimEndingDirectorySeparator(Path.GetFullPath(project)),
                Path.TrimEndingDirectorySeparator(Path.GetFullPath(repositoryRoot)), comparison))
            throw new InvalidDataException("Unity CLI responded from a different project.");
        JsonElement eval = data.GetProperty("result");
        RequireSuccess(eval, "Unity eval");
        string? status = eval.GetProperty("result").GetString();
        if (status != "Pass" && status != "Fail" && status != "InfrastructureError")
            throw new InvalidDataException("Unity eval did not acknowledge a gate outcome.");
        return status;
    }

    private static void RequireSuccess(JsonElement element, string owner)
    {
        if (!element.TryGetProperty("success", out JsonElement success) || success.ValueKind != JsonValueKind.True)
            throw new InvalidDataException(owner + " reported execution failure. See CLI logs.");
    }

    public static async Task RunAsync(string repositoryRoot, string? cliOverride, GateProfile profile,
        string runId, string resultPath, string runDirectory, TimeSpan timeout, ProcessRunner processRunner)
    {
        string executable = UnityCliLocator.Find(cliOverride);
        // A timed-out eval might finish in the Editor later. Its private output can
        // never satisfy a retry, even when that retry reuses the requested run ID.
        string invocationId = Guid.NewGuid().ToString("N");
        string privateResult = Path.Combine(runDirectory, "gate-results", profile.FileName + "." + invocationId + ".cli.json");
        string artifacts = Path.Combine(runDirectory, "artifacts");
        Directory.CreateDirectory(artifacts);
        string scriptPath = Path.Combine(artifacts, profile.FileName + "." + invocationId + ".eval.cs");
        await File.WriteAllTextAsync(scriptPath, CreateEvalCode(repositoryRoot, profile, runId, privateResult));
        string outputPath = Path.Combine(runDirectory, "logs", profile.FileName + "." + invocationId + ".cli.stdout.json");
        string errorPath = Path.Combine(runDirectory, "logs", profile.FileName + "." + invocationId + ".cli.stderr.log");
        string[] arguments = CreateArguments(repositoryRoot, scriptPath, timeout);
        await File.WriteAllTextAsync(Path.Combine(artifacts, profile.FileName + ".cli-invocation.json"),
            JsonSerializer.Serialize(new { transport = "unity-cli", executable, arguments, invocationId, privateResult,
                outputPath, errorPath, startedAtUtc = DateTime.UtcNow }, new JsonSerializerOptions { WriteIndented = true }));
        ProcessResult process = await processRunner.RunAsync(executable, arguments, null, outputPath, errorPath, timeout);
        if (process.TimedOut || process.ExitCode != 0)
            throw new InvalidDataException($"Unity CLI execution failed (exit {process.ExitCode}, timeout {process.TimedOut}). See {errorPath}. No fallback was attempted.");
        string acknowledgement;
        try
        {
            acknowledgement = ReadAcknowledgement(await File.ReadAllTextAsync(outputPath), repositoryRoot);
        }
        catch (Exception exception) when (exception is JsonException or KeyNotFoundException or InvalidOperationException or ArgumentException)
        {
            throw new InvalidDataException("Unity CLI response is malformed. See " + outputPath, exception);
        }
        GateResultSummary result = HarnessResultContracts.ReadGateResult(privateResult);
        HarnessRunner.ValidateGateResultForRequest(result, runId, profile);
        if (acknowledgement != result.Outcome.ToString())
            throw new InvalidDataException("Unity CLI acknowledgement disagrees with the GateResult.");
        File.Move(privateResult, resultPath, overwrite: true);
    }
}
