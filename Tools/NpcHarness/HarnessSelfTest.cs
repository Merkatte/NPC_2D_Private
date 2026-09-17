using System.Text.Json;

namespace NpcHarness;

internal static class HarnessSelfTest
{
    public static int Run(string repositoryRoot)
    {
        List<(string Name, Action Test)> tests = new List<(string, Action)>
        {
            ("parse verify", TestParseVerify),
            ("parse adapter", TestParseAdapter),
            ("reject natural language", TestRejectNaturalLanguage),
            ("resolve gate profiles", TestProfiles),
            ("read Unity project version", () => TestUnityVersion(repositoryRoot)),
            ("validate GateResult outcomes", TestGateResults),
            ("reject missing GateResult", TestMissingGateResult),
            ("reject malformed GateResult", TestMalformedGateResult),
            ("reject GateResult schema mismatch", TestGateResultSchemaMismatch),
            ("preserve GateResult identity and evidence", TestGateResultSummary),
            ("reject empty decided GateResult", TestEmptyDecidedGateResults),
            ("reject inconsistent GateResult status", TestInconsistentGateResultStatus),
            ("reject inconsistent GateResult checks", TestInconsistentGateResultChecks),
            ("validate requested gate identity", TestRequestedGateIdentity),
            ("reject read-only gate mutations", TestReadOnlyGateMutations),
            ("validate gate process exit code", TestGateProcessExitCode),
            ("validate adapter result", TestAdapterResult),
            ("validate Harness Job", TestJob),
            ("validate run IDs", TestRunIds),
            ("validate interactive result path boundary", () => TestInteractiveResultPathBoundary(repositoryRoot)),
        };

        int failures = 0;
        foreach ((string name, Action test) in tests)
        {
            try
            {
                test();
                Console.WriteLine($"PASS  {name}");
            }
            catch (Exception exception)
            {
                failures++;
                Console.Error.WriteLine($"FAIL  {name}: {exception.Message}");
            }
        }

        Console.WriteLine($"Self-test: {tests.Count - failures}/{tests.Count} passed.");
        return failures == 0 ? Program.SuccessExitCode : Program.InfrastructureErrorExitCode;
    }

    private static void TestParseVerify()
    {
        Assert(HarnessCommandLine.TryParse(
            new[] { "verify", "--profile", "beacon-structure", "--timeout", "30" },
            out HarnessCommand? command,
            out _), "verify command should parse");
        VerifyCommand verify = command as VerifyCommand ?? throw new Exception("wrong command type");
        Assert(verify.Profile == "beacon-structure", "profile was not preserved");
        Assert(verify.Timeout == TimeSpan.FromSeconds(30), "timeout was not parsed");
    }

    private static void TestParseAdapter()
    {
        Assert(HarnessCommandLine.TryParse(
            new[] { "run-adapter", "--job", "job.json", "--allow-overwrite" },
            out HarnessCommand? command,
            out _), "adapter command should parse");
        RunAdapterCommand adapter = command as RunAdapterCommand ?? throw new Exception("wrong command type");
        Assert(adapter.JobPath == "job.json", "job path was not preserved");
        Assert(adapter.AllowOverwrite, "overwrite flag was not preserved");
    }

    private static void TestRejectNaturalLanguage()
    {
        Assert(!HarnessCommandLine.TryParse(
            new[] { "create", "a", "scene" },
            out _,
            out _), "natural language must not be classified as a command");
    }

    private static void TestProfiles()
    {
        Assert(GateProfile.Resolve("beacon-structure").Name == "HarnessBeacon.Structure", "structure profile mismatch");
        Assert(GateProfile.Resolve("HarnessBeacon.PlayMode").Name == "HarnessBeacon.PlayMode", "play profile mismatch");
        GateProfile square = GateProfile.Resolve("square-character-structure");
        Assert(square.Name == "HarnessTest.SquareCharacter.Structure", "square character profile mismatch");
        Assert(square.Version == 1, "square character profile version mismatch");
        Assert(square.SupportsInteractiveEditor, "square character profile should support the open Editor bridge");
        AssertThrows<ArgumentException>(() => GateProfile.Resolve("unknown"));
    }

    private static void TestUnityVersion(string repositoryRoot)
    {
        string version = UnityEditorLocator.ReadProjectUnityVersion(repositoryRoot);
        Assert(!string.IsNullOrWhiteSpace(version), "Unity version is empty");
        Assert(UnityEditorLocator.GetDefaultCandidates(version).Count > 0, "no platform default path was produced");
    }

    private static void TestGateResults()
    {
        WithTemporaryFile(CreateGateJson("Pass", true, Check("Pass", true)), path =>
            Assert(HarnessResultContracts.ReadGateResult(path).Outcome == GateOutcome.Pass, "Pass was not read"));
        WithTemporaryFile(CreateGateJson("Fail", false, Check("Fail", false)), path =>
            Assert(HarnessResultContracts.ReadGateResult(path).Outcome == GateOutcome.Fail, "Fail was not read"));
        WithTemporaryFile(CreateGateJson("InfrastructureError", false), path =>
            Assert(
                HarnessResultContracts.ReadGateResult(path).Outcome == GateOutcome.InfrastructureError,
                "InfrastructureError was not read"));
    }

    private static void TestMissingGateResult()
    {
        string path = Path.Combine(
            Path.GetTempPath(),
            "npc-harness-missing-" + Guid.NewGuid().ToString("N") + ".json");
        Assert(!File.Exists(path), "missing-result fixture unexpectedly exists");
        AssertThrows<FileNotFoundException>(() => HarnessResultContracts.ReadGateResult(path));
    }

    private static void TestMalformedGateResult()
    {
        WithTemporaryFile("{not-json", path =>
            AssertThrows<JsonException>(() => HarnessResultContracts.ReadGateResult(path)));
    }

    private static void TestGateResultSchemaMismatch()
    {
        string json = CreateGateJson("Pass", true, Check("Pass", true))
            .Replace("\"schemaVersion\":1", "\"schemaVersion\":2", StringComparison.Ordinal);
        WithTemporaryFile(json, path =>
            AssertThrows<InvalidDataException>(() => HarnessResultContracts.ReadGateResult(path)));
    }

    private static void TestGateResultSummary()
    {
        WithTemporaryFile(
            CreateGateJson(
                "Pass",
                true,
                new[] { "Assets/Unexpected.asset" },
                Check("Pass", true)),
            path =>
            {
                GateResultSummary result = HarnessResultContracts.ReadGateResult(path);
                Assert(result.RunId == "self-test", "runId was not preserved");
                Assert(result.Profile == "HarnessBeacon.Structure", "profile was not preserved");
                Assert(result.ProfileVersion == 1, "profileVersion was not preserved");
                Assert(result.CheckCount == 1, "check count was not preserved");
                Assert(result.FailedCheckCount == 0, "failed check count was not preserved");
                Assert(result.ChangedFiles.SequenceEqual(new[] { "Assets/Unexpected.asset" }),
                    "changed files were not preserved");
            });
    }

    private static void TestEmptyDecidedGateResults()
    {
        WithTemporaryFile(CreateGateJson("Pass", true), path =>
            AssertThrows<InvalidDataException>(() => HarnessResultContracts.ReadGateResult(path)));
        WithTemporaryFile(CreateGateJson("Fail", false), path =>
            AssertThrows<InvalidDataException>(() => HarnessResultContracts.ReadGateResult(path)));
    }

    private static void TestInconsistentGateResultStatus()
    {
        WithTemporaryFile(CreateGateJson("Pass", false, Check("Pass", true)), path =>
            AssertThrows<InvalidDataException>(() => HarnessResultContracts.ReadGateResult(path)));
    }

    private static void TestInconsistentGateResultChecks()
    {
        WithTemporaryFile(CreateGateJson("Pass", true, Check("Fail", false)), path =>
            AssertThrows<InvalidDataException>(() => HarnessResultContracts.ReadGateResult(path)));
        WithTemporaryFile(CreateGateJson("Fail", false, Check("Pass", true)), path =>
            AssertThrows<InvalidDataException>(() => HarnessResultContracts.ReadGateResult(path)));
    }

    private static void TestRequestedGateIdentity()
    {
        GateProfile profile = GateProfile.Resolve("beacon-structure");
        GateResultSummary validResult = ReadTemporaryGateResult(
            CreateGateJson("Pass", true, Check("Pass", true)));
        HarnessRunner.ValidateGateResultForRequest(validResult, "self-test", profile);

        AssertThrows<InvalidDataException>(() => HarnessRunner.ValidateGateResultForRequest(
            validResult with { RunId = "other-run" },
            "self-test",
            profile));
        AssertThrows<InvalidDataException>(() => HarnessRunner.ValidateGateResultForRequest(
            validResult with { Profile = "HarnessBeacon.PlayMode" },
            "self-test",
            profile));
        AssertThrows<InvalidDataException>(() => HarnessRunner.ValidateGateResultForRequest(
            validResult with { ProfileVersion = 2 },
            "self-test",
            profile));
    }

    private static void TestReadOnlyGateMutations()
    {
        GateProfile profile = GateProfile.Resolve("beacon-playmode");
        GateResultSummary changedResult = ReadTemporaryGateResult(
            CreateGateJson(
                "Pass",
                true,
                new[] { "Assets/TestOnly/HarnessTest.unity" },
                Check("Pass", true)),
            profileName: profile.Name);
        AssertThrows<InvalidDataException>(() => HarnessRunner.ValidateGateResultForRequest(
            changedResult,
            "self-test",
            profile));
    }

    private static void TestGateProcessExitCode()
    {
        GateResultSummary passed = ReadTemporaryGateResult(
            CreateGateJson("Pass", true, Check("Pass", true)));
        GateResultSummary failed = ReadTemporaryGateResult(
            CreateGateJson("Fail", false, Check("Fail", false)));
        GateResultSummary infrastructureError = ReadTemporaryGateResult(
            CreateGateJson("InfrastructureError", false));

        HarnessRunner.ValidateGateProcessExitCode(0, passed);
        HarnessRunner.ValidateGateProcessExitCode(1, failed);
        HarnessRunner.ValidateGateProcessExitCode(1, infrastructureError);
        AssertThrows<InvalidDataException>(() => HarnessRunner.ValidateGateProcessExitCode(1, passed));
        AssertThrows<InvalidDataException>(() => HarnessRunner.ValidateGateProcessExitCode(0, failed));
        AssertThrows<InvalidDataException>(() => HarnessRunner.ValidateGateProcessExitCode(0, infrastructureError));
    }

    private static void TestAdapterResult()
    {
        WithTemporaryFile(
            "{\"success\":true,\"state\":\"NoChange\",\"message\":\"no changes\"}",
            path => Assert(HarnessResultContracts.ReadAdapterResult(path).Success, "adapter success was not read"));
    }

    private static void TestJob()
    {
        WithTemporaryFile(
            "{\"schemaVersion\":1,\"jobId\":\"self-test\",\"steps\":[{}]}",
            HarnessResultContracts.ValidateJob);
        WithTemporaryFile(
            "{\"schemaVersion\":1,\"jobId\":\"self-test\",\"steps\":[]}",
            path => AssertThrows<InvalidDataException>(() => HarnessResultContracts.ValidateJob(path)));
    }

    private static void TestRunIds()
    {
        Assert(RunPaths.ValidateOrCreateRunId("safe-ID_1") == "safe-ID_1", "valid run ID changed");
        AssertThrows<ArgumentException>(() => RunPaths.ValidateOrCreateRunId("../escape"));
    }

    private static void TestInteractiveResultPathBoundary(string repositoryRoot)
    {
        string allowed = Path.Combine(repositoryRoot, ".harness-runs", "self-test", "result.json");
        string sibling = Path.Combine(repositoryRoot, ".harness-runs-escape", "result.json");
        string prefix = Path.GetFullPath(Path.Combine(repositoryRoot, ".harness-runs"))
                            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) +
                        Path.DirectorySeparatorChar;
        StringComparison comparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

        Assert(Path.GetFullPath(allowed).StartsWith(prefix, comparison), "allowed interactive result was rejected");
        Assert(!Path.GetFullPath(sibling).StartsWith(prefix, comparison), "sibling path escaped interactive result boundary");
    }

    private static string CreateGateJson(string status, bool success, params object[] checks)
    {
        return CreateGateJson(status, success, Array.Empty<string>(), checks);
    }

    private static string CreateGateJson(
        string status,
        bool success,
        IReadOnlyList<string> changedFiles,
        params object[] checks)
    {
        return JsonSerializer.Serialize(new
        {
            schemaVersion = 1,
            runId = "self-test",
            profile = "HarnessBeacon.Structure",
            profileVersion = 1,
            success,
            status,
            message = "test result",
            checks,
            artifacts = Array.Empty<object>(),
            changedFiles,
            startedAtUtc = "2026-01-01T00:00:00Z",
            finishedAtUtc = "2026-01-01T00:00:01Z",
        });
    }

    private static object Check(string status, bool success)
    {
        return new
        {
            id = "self-test.check",
            success,
            status,
            message = "check result",
            expected = "expected",
            actual = "actual",
        };
    }

    private static GateResultSummary ReadTemporaryGateResult(string json, string? profileName = null)
    {
        if (!string.IsNullOrWhiteSpace(profileName))
        {
            using JsonDocument document = JsonDocument.Parse(json);
            Dictionary<string, object?> values = document.RootElement
                .EnumerateObject()
                .ToDictionary(property => property.Name, property => (object?)property.Value.Clone());
            values["profile"] = profileName;
            json = JsonSerializer.Serialize(values);
        }

        GateResultSummary? result = null;
        WithTemporaryFile(json, path => result = HarnessResultContracts.ReadGateResult(path));
        return result ?? throw new Exception("GateResult was not read.");
    }

    private static void WithTemporaryFile(string content, Action<string> action)
    {
        string path = Path.Combine(Path.GetTempPath(), "npc-harness-" + Guid.NewGuid().ToString("N") + ".json");
        try
        {
            File.WriteAllText(path, content);
            action(path);
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition)
        {
            throw new Exception(message);
        }
    }

    private static void AssertThrows<TException>(Action action)
        where TException : Exception
    {
        try
        {
            action();
        }
        catch (TException)
        {
            return;
        }

        throw new Exception($"Expected {typeof(TException).Name}.");
    }
}
