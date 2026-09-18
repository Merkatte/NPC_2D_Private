using System.IO.Compression;
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
            ("parse scope verification", TestParseScope),
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
            ("validate assemble SkillPolicy", () => TestSkillPolicy(repositoryRoot)),
            ("validate WorkerAssignment contract", TestWorkerAssignment),
            ("enforce scope path narrowing", () => TestScopePathNarrowing(repositoryRoot)),
            ("enforce single raster output", TestSingleRasterOutput),
            ("validate strict import pivot", TestStrictImportPivot),
            ("validate strict PNG stream", () => TestStrictPng(repositoryRoot)),
            ("serialize scope GateResult", TestScopeGateResult),
        };

        tests.AddRange(ReviewEvidenceSelfTest.Tests());

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

    private static void TestParseScope()
    {
        Assert(HarnessCommandLine.TryParse(
            new[]
            {
                "verify-scope",
                "--policy", "Tools/NpcHarness/SkillPolicies/assemble-unity-objects.json",
                "--assignment", ".harness-runs/test/assignments/assemble.json",
                "--assignment-sha256", new string('a', 64),
            },
            out HarnessCommand? command,
            out _), "verify-scope command should parse");
        VerifyScopeCommand scope = command as VerifyScopeCommand ?? throw new Exception("wrong command type");
        Assert(scope.PolicyPath.EndsWith("assemble-unity-objects.json", StringComparison.Ordinal),
            "policy path was not preserved");
        Assert(scope.AssignmentPath.Contains("assignments", StringComparison.Ordinal),
            "assignment path was not preserved");
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
        GateProfile farmer = GateProfile.Resolve("farmer-scene-structure");
        Assert(farmer.Name == "FarmerScene.Structure" && farmer.Version == 1, "farmer scene profile mismatch");
        Assert(farmer.SupportsInteractiveEditor && farmer.RequiresNoChangedFiles,
            "farmer scene must use read-only checks and support the open Editor bridge");
        Assert(GateProfile.Resolve("FarmerScene.Structure") == farmer, "farmer scene alias mismatch");
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

    private static void TestSkillPolicy(string repositoryRoot)
    {
        SkillPolicyDefinition policy = SkillPolicyContracts.ReadPolicy(Path.Combine(
            repositoryRoot,
            "Tools/NpcHarness/SkillPolicies/assemble-unity-objects.json"));
        Assert(policy.PolicyId == "assemble-unity-objects", "policy ID mismatch");
        Assert(!policy.AllowedTools.Contains("WriteCSharpScript", StringComparer.Ordinal),
            "object assembly policy must reject code authoring");
        Assert(policy.CandidateRules.Select(rule => rule.Path).SequenceEqual(new[] { "Assets/TestOnly/**" }),
            "assemble policy root mismatch");

        SkillPolicyDefinition codePolicy = SkillPolicyContracts.ReadPolicy(Path.Combine(
            repositoryRoot,
            "Tools/NpcHarness/SkillPolicies/author-unity-code.json"));
        Assert(codePolicy.ExecutionKind == WorkerExecutionKinds.DirectCode,
            "code policy execution kind mismatch");
        Assert(codePolicy.CandidateRules.Any(rule =>
                rule.Path == "Assets/Scripts/**" && rule.Extensions.Contains(".cs", StringComparer.Ordinal)),
            "code policy source rule mismatch");

        SkillPolicyDefinition spritePolicy = SkillPolicyContracts.ReadPolicy(Path.Combine(
            repositoryRoot,
            "Tools/NpcHarness/SkillPolicies/create-project-sprites.json"));
        Assert(spritePolicy.ExecutionKind == WorkerExecutionKinds.RasterArt,
            "sprite policy execution kind mismatch");
        Assert(spritePolicy.CandidateRules.Single().Extensions.SequenceEqual(new[] { ".png" }),
            "sprite policy must allow only PNG candidates");
        Assert(spritePolicy.ForbiddenAssetExtensions.Contains(".meta", StringComparer.Ordinal),
            "sprite policy must reject direct importer meta edits");
    }

    private static void TestWorkerAssignment()
    {
        const string assignmentJson = """
        {
          "schemaVersion": 1,
          "assignmentId": "assemble-1",
          "runId": "scope-self-test",
          "roleSkill": "assemble-unity-objects",
          "policyVersion": 1,
          "objective": "Assemble one bounded test scene object.",
          "baselineCommit": "0123456789abcdef0123456789abcdef01234567",
          "baselineDirtyFiles": {},
          "requiredContext": [".codex/skills/assemble-unity-objects/references/current-tool-contract.md"],
          "writablePaths": ["Assets/TestOnly/HarnessTest.unity"],
          "forbiddenPaths": ["Assets/Scripts/**"],
          "allowedTools": ["EnsureScene", "SaveScene"],
          "forbiddenOperations": ["Direct Unity YAML edits"],
          "artifactPaths": {
            "workerResultPath": ".harness-runs/scope-self-test/worker-results/assemble.json",
            "logPath": ".harness-runs/scope-self-test/logs/adapter.log"
          },
          "execution": {
            "kind": "harness-job",
            "jobId": "scope-self-test-job",
            "jobPath": ".harness-runs/scope-self-test/assignments/job.json",
            "adapterResultPath": ".harness-runs/scope-self-test/adapter-results/result.json",
            "allowedComponentTypes": ["Transform"],
            "overwriteAuthority": {
              "allowOverwrite": false,
              "approvedValues": []
            },
            "taskSpecification": {
              "hierarchy": ["Root/TestObject"],
              "components": [],
              "transforms": ["Root/TestObject position=(0,0,0)"],
              "serializedValues": []
            }
          },
          "acceptanceGate": {
            "manifestPath": "Tools/NpcHarness/Profiles/square-character-structure.json",
            "profile": "HarnessTest.SquareCharacter.Structure",
            "profileVersion": 1
          },
          "acceptanceConditions": ["Assigned hierarchy exists."],
          "preliminaryChecks": ["Harness Job validates."],
          "reportRequirements": ["Return changed files and receipts."]
        }
        """;
        WithTemporaryFile(assignmentJson, path =>
        {
            WorkerAssignmentDefinition assignment = SkillPolicyContracts.ReadAssignment(path);
            Assert(assignment.RunId == "scope-self-test", "assignment run ID mismatch");
            Assert(assignment.AllowedTools.Count == 2, "assignment tools mismatch");
        });

        string invalid = assignmentJson.Replace(
            "\"policyVersion\": 1,",
            "\"policyVersion\": 1, \"unexpected\": true,",
            StringComparison.Ordinal);
        WithTemporaryFile(invalid, path =>
            AssertThrows<InvalidDataException>(() => SkillPolicyContracts.ReadAssignment(path)));
    }

    private static void TestScopePathNarrowing(string repositoryRoot)
    {
        SkillPolicyDefinition policy = SkillPolicyContracts.ReadPolicy(Path.Combine(
            repositoryRoot,
            "Tools/NpcHarness/SkillPolicies/assemble-unity-objects.json"));
        WorkerAssignmentDefinition assignment = new WorkerAssignmentDefinition(
            "test", "test", policy.PolicyId, policy.PolicyVersion,
            "Assemble a bounded test scene object.",
            "0123456789abcdef0123456789abcdef01234567",
            Array.Empty<BaselineDirtyFileDefinition>(),
            new[] { ".codex/skills/assemble-unity-objects/references/current-tool-contract.md" },
            new[] { "Assets/TestOnly/HarnessTest.unity", "Assets/TestOnly/HarnessTest.unity.meta" },
            new[] { "Assets/Scripts/**" },
            new[] { "EnsureScene", "SaveScene" },
            new[] { "Direct Unity YAML edits" },
            new AssignmentArtifactPathsDefinition(
                ".harness-runs/test/worker-results/assemble.json",
                ".harness-runs/test/logs/adapter.log"),
            new HarnessJobExecutionDefinition(
                "job",
                ".harness-runs/test/assignments/job.json",
                ".harness-runs/test/adapter-results/result.json",
                new[] { "Transform" },
                new OverwriteAuthorityDefinition(false, Array.Empty<string>()),
                new SceneTaskSpecificationDefinition(
                    new[] { "Root/TestObject" },
                    Array.Empty<string>(),
                    new[] { "Root/TestObject position=(0,0,0)" },
                    Array.Empty<string>())),
            new AcceptanceGateDefinition(
                "Tools/NpcHarness/Profiles/square-character-structure.json",
                "HarnessTest.SquareCharacter.Structure",
                1),
            new[] { "Assigned hierarchy exists." },
            new[] { "Harness Job validates." },
            new[] { "Return changed files and receipts." });

        Assert(SkillScopeGate.IsCandidatePathAllowed(
            "Assets/TestOnly/HarnessTest.unity", assignment, policy), "assigned scene should be allowed");
        Assert(!SkillScopeGate.IsCandidatePathAllowed(
            "Assets/TestOnly/Generated.cs", assignment, policy), "C# must be rejected");
        Assert(!SkillScopeGate.IsCandidatePathAllowed(
            "Assets/Scenes/GuardTest.unity", assignment, policy), "production scene must be rejected");
        Assert(ScopePathPolicy.IsCoveredBy(
            "Assets/TestOnly/HarnessTest.unity", "Assets/TestOnly/**"), "exact path should narrow root");
        AssertThrows<InvalidDataException>(() => ScopePathPolicy.ValidatePattern("Assets/**/Scenes"));
    }

    private static void TestScopeGateResult()
    {
        SkillPolicyDefinition policy = new SkillPolicyDefinition(
            "test-policy", 1, WorkerExecutionKinds.HarnessJob, "SkillPolicy.Test.Scope", 1,
            new[] { new CandidateRuleDefinition("Assets/TestOnly/**", new[] { ".unity" }) },
            Array.Empty<string>(), Array.Empty<string>(),
            new[] { "EnsureScene" }, new[] { "Tools/NpcHarness/Profiles" });
        ScopeGateEvaluation evaluation = new ScopeGateEvaluation(
            new[] { new ScopeGateCheck("scope.test", true, "passed", "expected", "actual") },
            new[] { "Assets/TestOnly/Test.unity" });
        string path = Path.Combine(Path.GetTempPath(), "npc-scope-result-" + Guid.NewGuid().ToString("N") + ".json");
        try
        {
            SkillScopeGateResultWriter.Write(
                path, "scope-self-test", policy, evaluation,
                DateTime.UtcNow, DateTime.UtcNow,
                "Tools/NpcHarness/SkillPolicies/test.json",
                ".harness-runs/scope-self-test/assignments/test.json");
            GateResultSummary result = HarnessResultContracts.ReadGateResult(path);
            Assert(result.Outcome == GateOutcome.Pass, "scope GateResult should pass");
            Assert(result.ChangedFiles.SequenceEqual(new[] { "Assets/TestOnly/Test.unity" }),
                "scope GateResult should preserve changed files");
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static void TestSingleRasterOutput()
    {
        RasterArtExecutionDefinition raster = new RasterArtExecutionDefinition(
            "ui", "test", "Assets/Art/Generated/test.png",
            new[] { "Assets/Art/Generated/reference.png" },
            "Assets/Art/Generated/reference.png.meta",
            ".harness-runs/test/artifacts/import.json",
            32, 32, true, 1, 1);
        WorkerAssignmentDefinition assignment = CreateRasterAssignment(raster.OutputPath);
        Assert(SkillScopeGate.HasSingleRasterOutput(assignment, raster),
            "one exact raster output should be accepted");

        WorkerAssignmentDefinition extraOutput = assignment with
        {
            WritablePaths = new[]
            {
                raster.OutputPath,
                "Assets/Art/Generated/unvalidated-extra.png",
            },
        };
        Assert(!SkillScopeGate.HasSingleRasterOutput(extraOutput, raster),
            "extra raster output must be rejected");
    }

    private static void TestStrictImportPivot()
    {
        using JsonDocument validDocument = JsonDocument.Parse("{\"pivot\":{\"x\":0.5,\"y\":0.5}}");
        List<string> validIssues = new List<string>();
        SkillScopeGate.CompareImportPivot(validDocument.RootElement, "{x: 0.5, y: 0.5}", validIssues);
        Assert(validIssues.Count == 0, "valid import pivot should pass");

        using JsonDocument extraDocument = JsonDocument.Parse(
            "{\"pivot\":{\"x\":0.5,\"y\":0.5,\"unexpected\":true}}");
        List<string> extraIssues = new List<string>();
        SkillScopeGate.CompareImportPivot(extraDocument.RootElement, "{x: 0.5, y: 0.5}", extraIssues);
        Assert(extraIssues.Count == 1, "extra pivot properties must be rejected");

        using JsonDocument rangeDocument = JsonDocument.Parse("{\"pivot\":{\"x\":2,\"y\":0.5}}");
        List<string> rangeIssues = new List<string>();
        SkillScopeGate.CompareImportPivot(rangeDocument.RootElement, "{x: 2, y: 0.5}", rangeIssues);
        Assert(rangeIssues.Count == 1, "out-of-range pivot values must be rejected");
    }

    private static void TestStrictPng(string repositoryRoot)
    {
        string validPath = Path.Combine(
            repositoryRoot,
            "Assets/Art/Generated/merchant-npc-female.png");
        PngInfo valid = SkillScopeGate.ReadPngInfo(validPath);
        Assert(valid.Width > 0 && valid.Height > 0, "project PNG should decode");

        byte[] headerOnly =
        {
            137, 80, 78, 71, 13, 10, 26, 10,
            0, 0, 0, 13,
            73, 72, 68, 82,
            0, 0, 0, 1,
            0, 0, 0, 1,
            8, 6, 0, 0, 0,
            31, 21, 196, 137,
        };
        WithTemporaryBinaryFile(headerOnly, path =>
            AssertThrows<InvalidDataException>(() => SkillScopeGate.ReadPngInfo(path)));

        WithTemporaryBinaryFile(CreateTestPng(filter: 5, colorType: 6), path =>
            AssertThrows<InvalidDataException>(() => SkillScopeGate.ReadPngInfo(path)));
        WithTemporaryBinaryFile(CreateTestPng(filter: 0, colorType: 3), path =>
            AssertThrows<InvalidDataException>(() => SkillScopeGate.ReadPngInfo(path)));
        WithTemporaryBinaryFile(CreateTestPng(filter: 0, colorType: 6, unknownCriticalChunk: true), path =>
            AssertThrows<InvalidDataException>(() => SkillScopeGate.ReadPngInfo(path)));
    }

    private static WorkerAssignmentDefinition CreateRasterAssignment(string outputPath)
    {
        return new WorkerAssignmentDefinition(
            "raster-test", "raster-test", "create-project-sprites", 1, "test",
            "0123456789abcdef0123456789abcdef01234567",
            Array.Empty<BaselineDirtyFileDefinition>(),
            new[] { ".codex/skills/create-project-sprites/references/project-art-routing.md" },
            new[] { outputPath },
            new[] { "Assets/Scripts/**" },
            new[] { "ImageGen" },
            new[] { "Direct meta edits" },
            new AssignmentArtifactPathsDefinition(
                ".harness-runs/raster-test/worker-results/sprite.json",
                ".harness-runs/raster-test/logs/generation.log"),
            new RasterArtExecutionDefinition(
                "ui", "test", outputPath,
                new[] { "Assets/Art/Generated/reference.png" },
                "Assets/Art/Generated/reference.png.meta",
                ".harness-runs/raster-test/artifacts/import.json",
                32, 32, true, 1, 1),
            new AcceptanceGateDefinition(
                "Tools/NpcHarness/Profiles/square-character-structure.json",
                "HarnessTest.SquareCharacter.Structure", 1),
            new[] { "output exists" },
            new[] { "inspect output" },
            new[] { "return output" });
    }

    private static byte[] CreateTestPng(byte filter, byte colorType, bool unknownCriticalChunk = false)
    {
        using MemoryStream png = new MemoryStream();
        png.Write(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 });

        byte[] header = new byte[13];
        System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(0, 4), 1);
        System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(4, 4), 1);
        header[8] = 8;
        header[9] = colorType;
        WritePngChunk(png, "IHDR", header);
        if (unknownCriticalChunk)
        {
            WritePngChunk(png, "ABCD", Array.Empty<byte>());
        }

        int channels = colorType == 3 ? 1 : 4;
        byte[] scanline = new byte[channels + 1];
        scanline[0] = filter;
        using MemoryStream compressed = new MemoryStream();
        using (ZLibStream deflater = new ZLibStream(compressed, CompressionLevel.SmallestSize, leaveOpen: true))
        {
            deflater.Write(scanline);
        }
        WritePngChunk(png, "IDAT", compressed.ToArray());
        WritePngChunk(png, "IEND", Array.Empty<byte>());
        return png.ToArray();
    }

    private static void WritePngChunk(Stream output, string type, byte[] data)
    {
        byte[] typeBytes = System.Text.Encoding.ASCII.GetBytes(type);
        byte[] crcInput = new byte[typeBytes.Length + data.Length];
        typeBytes.CopyTo(crcInput, 0);
        data.CopyTo(crcInput, typeBytes.Length);

        Span<byte> integer = stackalloc byte[4];
        System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(integer, data.Length);
        output.Write(integer);
        output.Write(typeBytes);
        output.Write(data);
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32BigEndian(
            integer,
            SkillScopeGate.ComputePngCrc(crcInput));
        output.Write(integer);
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

    private static void WithTemporaryBinaryFile(byte[] content, Action<string> action)
    {
        string path = Path.Combine(Path.GetTempPath(), "npc-harness-" + Guid.NewGuid().ToString("N") + ".png");
        try
        {
            File.WriteAllBytes(path, content);
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
