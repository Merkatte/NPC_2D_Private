using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace NpcHarness;

// Independent protocol fixtures: do not derive expected records from the implementation.
internal static class ReviewEvidenceSelfTest
{
    public static IEnumerable<(string Name, Action Test)> Tests()
    {
        foreach ((string name, Action test) in CommandLineTests())
        {
            yield return (name, test);
        }
        yield return ("review evidence accepts complete independent review", () => WithFixture(f => f.AssertAccept(0)));
        yield return ("review evidence accepts allowed N/A with explanation", () => WithFixture(f =>
        {
            f.Coverage[0]!["status"] = "NotApplicable";
            f.Coverage[0]!["evidence"] = "No naming declarations changed in the selected candidate.";
            f.SaveReview();
            f.AssertAccept(0);
        }, allowNotApplicable: true));
        yield return ("review evidence accepts nonblocking minor finding", () => WithFixture(f =>
        {
            f.AddFinding("Minor");
            f.SaveReview();
            f.AssertAccept(0);
        }));

        (string Name, Action<Fixture> Mutate)[] invalidCases =
        {
            ("missing review", f => File.Delete(f.Resolve(Fixture.ReviewPath))),
            ("missing rule coverage", f => { f.Coverage.Clear(); f.SaveReview(); }),
            ("unknown rule coverage", f => { f.Coverage[0]!["id"] = "unknown"; f.SaveReview(); }),
            ("duplicate rule coverage", f => { f.Coverage.Add(f.Coverage[0]!.DeepClone()); f.SaveReview(); }),
            ("missing gate", f => File.Delete(f.Resolve(Fixture.GatePath))),
            ("missing required gate check", f => { f.Gate["checks"]![0]!["id"] = "unrelated"; f.SaveGateAndReview(); }),
            ("missing gate evidence", f => { f.Review["gateEvidence"]!.AsArray().Clear(); f.SaveReview(); }),
            ("unknown gate evidence", f => { f.Review["gateEvidence"]![0]!["path"] = ".harness-runs/evidence-test/extra.json"; f.SaveReview(); }),
            ("duplicate gate evidence", f => { JsonArray rows = f.Review["gateEvidence"]!.AsArray(); rows.Add(rows[0]!.DeepClone()); f.SaveReview(); }),
            ("candidate modified", f => f.Write("Candidate/Work.cs", "changed candidate")),
            ("candidate added", f => f.Write("Candidate/Added.cs", "new candidate")),
            ("candidate deleted", f => File.Delete(f.Resolve("Candidate/Work.cs"))),
            ("document changed", f => f.Write("Docs/Rules.md", "weakened rules")),
            ("review run mismatch", f => { f.Review["runId"] = "another-run"; f.SaveReview(); }),
            ("gate run mismatch", f => { f.Gate["runId"] = "another-run"; f.SaveGateAndReview(); }),
            ("gate profile mismatch", f => { f.Gate["profile"] = "Other.Profile"; f.SaveGateAndReview(); }),
            ("gate version mismatch", f => { f.Gate["profileVersion"] = 2; f.SaveGateAndReview(); }),
            ("review request hash mismatch", f => { f.Review["requestSha256"] = new string('0', 64); f.SaveReview(); }),
            ("review snapshot hash mismatch", f => { f.Review["snapshotSha256"] = new string('0', 64); f.SaveReview(); }),
            ("review gate hash mismatch", f => { f.Review["gateEvidence"]![0]!["sha256"] = new string('0', 64); f.SaveReview(); }),
            ("gate changed after review", f => { f.Gate["message"] = "edited after review"; f.SaveGate(); }),
            ("gate predates snapshot", f => { f.Gate["startedAtUtc"] = "2020-01-01T00:00:00Z"; f.Gate["finishedAtUtc"] = "2020-01-01T00:00:01Z"; f.SaveGateAndReview(); }),
            ("self review", f => { f.Review["reviewerId"] = "implementer"; f.SaveReview(); }),
            ("empty review evidence", f => { f.Coverage[0]!["evidence"] = "  "; f.SaveReview(); }),
            ("disallowed N/A", f => { f.Coverage[0]!["status"] = "NotApplicable"; f.SaveReview(); }),
            ("unknown coverage status", f => { f.Coverage[0]!["status"] = "LooksGood"; f.SaveReview(); }),
            ("approve with blocking finding", f => { f.AddFinding("Major"); f.SaveReview(); }),
            ("approve with uncovered rule", f => { f.Coverage[0]!["status"] = "NotCovered"; f.SaveReview(); }),
            ("violation without finding", f => { f.Coverage[0]!["status"] = "Violated"; f.Review["verdict"] = "ChangesRequested"; f.SaveReview(); }),
            ("changes requested without finding", f => { f.Review["verdict"] = "ChangesRequested"; f.SaveReview(); }),
            ("insufficient evidence without uncovered rule", f => { f.Review["verdict"] = "InsufficientEvidence"; f.SaveReview(); }),
            ("missing change summary", f => { f.Review.Remove("summary"); f.SaveReview(); }),
            ("blank change summary", f => { f.Review["summary"]!["responsibilities"] = " "; f.SaveReview(); }),
            ("unknown review field", f => { f.Review["unexpected"] = true; f.SaveReview(); }),
            ("unknown nested review field", f => { f.Coverage[0]!["unexpected"] = true; f.SaveReview(); }),
            ("malformed review JSON", f => f.Write(Fixture.ReviewPath, "{invalid")),
            ("duplicate review property", f => f.Write(Fixture.ReviewPath, f.Review.ToJsonString().Replace("\"schemaVersion\":2", "\"schemaVersion\":2,\"schemaVersion\":2", StringComparison.Ordinal))),
            ("snapshot bytes changed", f => File.AppendAllText(f.Resolve(Fixture.SnapshotPath), " ")),
        };
        foreach ((string name, Action<Fixture> mutate) in invalidCases)
        {
            yield return ("review evidence rejects " + name, () => WithFixture(f =>
            {
                mutate(f);
                f.AssertAccept(2);
            }));
        }

        yield return ("review evidence rejects coherent blocking review", () => WithFixture(f =>
        {
            f.Coverage[0]!["status"] = "Violated";
            f.Review["verdict"] = "ChangesRequested";
            f.AddFinding("Major");
            f.SaveReview();
            f.AssertAccept(1);
        }));
        yield return ("review evidence rejects coherent insufficient evidence", () => WithFixture(f =>
        {
            f.Coverage[0]!["status"] = "NotCovered";
            f.Review["verdict"] = "InsufficientEvidence";
            f.SaveReview();
            f.AssertAccept(1);
        }));
        yield return ("review evidence rejects pre-existing blocking acceptance issue", () => WithFixture(f =>
        {
            f.Coverage[0]!["status"] = "Violated";
            f.Review["verdict"] = "ChangesRequested";
            f.AddFinding("Critical");
            f.Review["findings"]![0]!["origin"] = "PreExisting";
            f.SaveReview();
            f.AssertAccept(1);
        }));
        yield return ("review evidence rejects functional gate failure", () => WithFixture(f =>
        {
            f.Gate["status"] = "Fail";
            f.Gate["success"] = false;
            f.Gate["checks"]![0]!["status"] = "Fail";
            f.Gate["checks"]![0]!["success"] = false;
            f.SaveGateAndReview();
            f.AssertAccept(1);
        }));
        yield return ("review evidence rejects invalid per-check infrastructure status", () => WithFixture(f =>
        {
            f.Gate["status"] = "InfrastructureError";
            f.Gate["success"] = false;
            f.Gate["checks"]![0]!["status"] = "InfrastructureError";
            f.Gate["checks"]![0]!["success"] = false;
            f.SaveGateAndReview();
            f.AssertAccept(2);
        }));
        yield return ("review evidence preserves valid gate infrastructure failure", () => WithFixture(f =>
        {
            f.Gate["status"] = "InfrastructureError";
            f.Gate["success"] = false;
            f.Gate["checks"]![0]!["status"] = "Fail";
            f.Gate["checks"]![0]!["success"] = false;
            f.SaveGateAndReview();
            Assert(HarnessResultContracts.ReadGateResult(f.Resolve(Fixture.GatePath)).Outcome == GateOutcome.InfrastructureError,
                "infrastructure fixture itself must satisfy GateResult v1");
            f.AssertAccept(2);
        }));
        yield return ("review evidence rejects missing declared gate artifact", () => WithFixture(f =>
        {
            f.Gate["artifacts"]!.AsArray().Add(JsonSerializer.SerializeToNode(new
            {
                kind = "log", path = ".harness-runs/evidence-test/artifacts/missing.log",
            }));
            f.SaveGateAndReview();
            Assert(HarnessResultContracts.ReadGateResult(f.Resolve(Fixture.GatePath)).Outcome == GateOutcome.Pass,
                "artifact fixture must be schema-valid before missing-file validation");
            f.AssertAccept(2);
        }));
        yield return ("review evidence accepts existing declared gate artifact", () => WithFixture(f =>
        {
            const string artifactPath = ".harness-runs/evidence-test/artifacts/function.log";
            f.Write(artifactPath, "Fixture gate executed.");
            f.Gate["artifacts"]!.AsArray().Add(JsonSerializer.SerializeToNode(new { kind = "log", path = artifactPath }));
            f.SaveGateAndReview();
            f.AssertAccept(0);
        }));
        yield return ("review evidence replaces previous Pass after review disappears", () => WithFixture(f =>
        {
            f.AssertAccept(0);
            File.Delete(f.Resolve(Fixture.ReviewPath));
            f.AssertAccept(2);
        }));
        yield return ("review evidence replaces previous Pass after request is malformed", () => WithFixture(f =>
        {
            f.AssertAccept(0);
            f.Write(Fixture.RequestPath, "{invalid");
            f.AssertAccept(2);
        }));
        yield return ("review snapshot refuses overwrite", () => WithFixture(f =>
        {
            string before = HashFile(f.Resolve(Fixture.SnapshotPath));
            AssertThrows(() => ReviewEvidenceGate.Snapshot(f.Root, Fixture.RequestPath, f.RequestHash));
            Assert(HashFile(f.Resolve(Fixture.SnapshotPath)) == before, "existing snapshot was overwritten");
        }));
        yield return ("review evidence rejects incorrect retained request hash", () => WithFixture(f =>
            Assert(ReviewEvidenceGate.Accept(f.Root, Fixture.RequestPath, new string('0', 64), f.SnapshotHash) == 2,
                "incorrect external request hash must not pass")));
        yield return ("review evidence rejects request changed after pinning", () => WithFixture(f =>
        {
            f.Request["authorIds"]![0] = "changed-author";
            f.SaveRequest();
            Assert(ReviewEvidenceGate.Accept(f.Root, Fixture.RequestPath, f.RequestHash, f.SnapshotHash) == 2,
                "changed request must not pass using the retained hash");
        }));
        yield return ("review evidence rejects incorrect retained snapshot hash", () => WithFixture(f =>
            Assert(ReviewEvidenceGate.Accept(f.Root, Fixture.RequestPath, f.RequestHash, new string('0', 64)) == 2,
                "incorrect external snapshot hash must not pass")));
        yield return ("review snapshot excludes generated bin and obj", () => WithFixture(f =>
        {
            f.Write("Candidate/bin/generated.dll", "generated binary");
            f.Write("Candidate/obj/build.json", "generated build data");
            f.AssertAccept(0);
        }));

        (string Name, Action<Fixture> Mutate)[] invalidRequests =
        {
            ("empty input roots", f => f.Request["inputRoots"]!.AsArray().Clear()),
            ("repository root input", f => f.Request["inputRoots"]![0] = "."),
            ("parent traversal input", f => f.Request["inputRoots"]![0] = "../outside"),
            ("absolute input", f => f.Request["inputRoots"]![0] = f.Root),
            ("missing input", f => f.Request["inputRoots"]![0] = "Missing"),
            ("uncovered document", f => f.Request["inputRoots"]!.AsArray().RemoveAt(1)),
            ("duplicate input path", f => f.Request["inputRoots"]!.AsArray().Add("candidate")),
            ("empty author list", f => f.Request["authorIds"]!.AsArray().Clear()),
            ("duplicate author", f => f.Request["authorIds"]!.AsArray().Add("implementer")),
            ("empty gate list", f => f.Request["requiredGates"]!.AsArray().Clear()),
            ("empty rule list", f => f.Request["rules"]!.AsArray().Clear()),
            ("duplicate rule id", f => { JsonArray rules = f.Request["rules"]!.AsArray(); rules.Add(rules[0]!.DeepClone()); }),
            ("duplicate required check id", f => f.Request["requiredGates"]![0]!["requiredCheckIds"]!.AsArray().Add("function.behavior")),
            ("gate outside run", f => f.Request["requiredGates"]![0]!["path"] = ".harness-runs/another/gate.json"),
            ("review outside run", f => f.Request["reviewPath"] = ".harness-runs/another/review.json"),
            ("gate and review collision", f => f.Request["reviewPath"] = Fixture.GatePath),
            ("snapshot output collision", f => f.Request["reviewPath"] = Fixture.SnapshotPath),
            ("accept output collision", f => f.Request["reviewPath"] = Fixture.OutputPath),
            ("request output collision", f => f.Request["reviewPath"] = Fixture.RequestPath),
            ("unknown request property", f => f.Request["unexpected"] = true),
            ("unknown nested request property", f => f.Request["rules"]![0]!["unexpected"] = true),
        };
        foreach ((string name, Action<Fixture> mutate) in invalidRequests)
        {
            yield return ("review snapshot rejects " + name, () => WithFixture(f =>
            {
                mutate(f);
                f.SaveRequest();
                AssertThrows(() => ReviewEvidenceGate.Snapshot(f.Root, Fixture.RequestPath, HashFile(f.Resolve(Fixture.RequestPath))));
                Assert(!File.Exists(f.Resolve(Fixture.SnapshotPath)), "invalid request created a snapshot");
            }, createEvidence: false));
        }
        yield return ("review snapshot rejects duplicate request property", () => WithFixture(f =>
        {
            f.Write(Fixture.RequestPath, f.Request.ToJsonString().Replace("\"schemaVersion\":1", "\"schemaVersion\":1,\"schemaVersion\":1", StringComparison.Ordinal));
            AssertThrows(() => ReviewEvidenceGate.Snapshot(f.Root, Fixture.RequestPath, HashFile(f.Resolve(Fixture.RequestPath))));
        }, createEvidence: false));
        yield return ("review snapshot rejects malformed request", () => WithFixture(f =>
        {
            f.Write(Fixture.RequestPath, "{invalid");
            AssertThrows(() => ReviewEvidenceGate.Snapshot(f.Root, Fixture.RequestPath, HashFile(f.Resolve(Fixture.RequestPath))));
        }, createEvidence: false));
    }

    private static IEnumerable<(string Name, Action Test)> CommandLineTests()
    {
        string requestHash = new string('a', 64);
        string snapshotHash = new string('B', 64);
        string[] snapshotArgs = { "review-snapshot", "--request", Fixture.RequestPath, "--request-sha256", requestHash };
        string[] acceptArgs = { "accept-review", "--request", Fixture.RequestPath, "--request-sha256", requestHash, "--snapshot-sha256", snapshotHash };
        yield return ("parse review snapshot command", () =>
        {
            Assert(HarnessCommandLine.TryParse(snapshotArgs, out HarnessCommand? parsed, out string error), error);
            ReviewEvidenceCommand command = parsed as ReviewEvidenceCommand ?? throw new InvalidOperationException("wrong command type");
            Assert(command.CreateSnapshot && command.RequestPath == Fixture.RequestPath &&
                command.RequestSha256 == requestHash && command.SnapshotSha256 == null, "snapshot command fields changed");
        });
        yield return ("parse review acceptance command", () =>
        {
            Assert(HarnessCommandLine.TryParse(acceptArgs, out HarnessCommand? parsed, out string error), error);
            ReviewEvidenceCommand command = parsed as ReviewEvidenceCommand ?? throw new InvalidOperationException("wrong command type");
            Assert(!command.CreateSnapshot && command.RequestPath == Fixture.RequestPath &&
                command.RequestSha256 == requestHash && command.SnapshotSha256 == snapshotHash, "accept command fields changed");
        });
        (string Name, string[] Args)[] invalidCases =
        {
            ("missing request", new[] { "review-snapshot", "--request-sha256", requestHash }),
            ("missing request hash", new[] { "review-snapshot", "--request", Fixture.RequestPath }),
            ("missing snapshot hash", new[] { "accept-review", "--request", Fixture.RequestPath, "--request-sha256", requestHash }),
            ("duplicate request", snapshotArgs.Concat(new[] { "--request", Fixture.RequestPath }).ToArray()),
            ("duplicate request hash", snapshotArgs.Concat(new[] { "--request-sha256", requestHash }).ToArray()),
            ("duplicate snapshot hash", acceptArgs.Concat(new[] { "--snapshot-sha256", snapshotHash }).ToArray()),
            ("snapshot hash on snapshot command", snapshotArgs.Concat(new[] { "--snapshot-sha256", snapshotHash }).ToArray()),
            ("unknown option", acceptArgs.Concat(new[] { "--force", "true" }).ToArray()),
            ("incomplete option", acceptArgs.Concat(new[] { "--output" }).ToArray()),
            ("short request hash", new[] { "review-snapshot", "--request", Fixture.RequestPath, "--request-sha256", "abc" }),
            ("nonhex snapshot hash", new[] { "accept-review", "--request", Fixture.RequestPath, "--request-sha256", requestHash, "--snapshot-sha256", new string('z', 64) }),
            ("blank request", new[] { "review-snapshot", "--request", " ", "--request-sha256", requestHash }),
        };
        foreach ((string name, string[] args) in invalidCases)
        {
            yield return ("review command rejects " + name, () =>
            {
                Assert(!HarnessCommandLine.TryParse(args, out HarnessCommand? command, out string error), "invalid command parsed");
                Assert(command == null && !string.IsNullOrWhiteSpace(error), "parse failure must have no command and a diagnostic");
            });
        }
    }

    private static void WithFixture(Action<Fixture> action, bool allowNotApplicable = false, bool createEvidence = true)
    {
        using Fixture fixture = new Fixture(allowNotApplicable);
        if (createEvidence)
        {
            fixture.CreateEvidence();
        }
        action(fixture);
    }

    private static string HashFile(string path)
    {
        return Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))).ToLowerInvariant();
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    private static void AssertThrows(Action action)
    {
        try
        {
            action();
        }
        catch (Exception exception) when (exception is InvalidDataException or IOException or ArgumentException or JsonException)
        {
            return;
        }
        throw new InvalidOperationException("Invalid review snapshot input was not rejected.");
    }

    private sealed class Fixture : IDisposable
    {
        public const string RequestPath = ".harness-runs/evidence-test/review-request.json";
        public const string SnapshotPath = ".harness-runs/evidence-test/review-snapshot.json";
        public const string GatePath = ".harness-runs/evidence-test/gate-results/function.json";
        public const string ReviewPath = ".harness-runs/evidence-test/review.json";
        public const string OutputPath = ".harness-runs/evidence-test/gate-results/review-evidence.json";
        public string Root { get; } = Path.Combine(Path.GetTempPath(), "npc-review-evidence-" + Guid.NewGuid().ToString("N"));
        public JsonObject Request { get; }
        public JsonObject Gate { get; private set; } = new JsonObject();
        public JsonObject Review { get; private set; } = new JsonObject();
        public JsonArray Coverage => Review["ruleCoverage"]!.AsArray();
        public string RequestHash { get; private set; } = "";
        public string SnapshotHash { get; private set; } = "";

        public Fixture(bool allowNotApplicable)
        {
            Write("Candidate/Work.cs", "internal class Work { private int _value; }");
            Write("Docs/Rules.md", "# Rules\n## Naming\nPrivate fields use _camelCase.\n");
            Request = JsonSerializer.SerializeToNode(new
            {
                schemaVersion = 1,
                runId = "evidence-test",
                inputRoots = new[] { "Candidate", "Docs" },
                authorIds = new[] { "implementer" },
                requiredGates = new[] { new { path = GatePath, profile = "Example.Function", version = 1, requiredCheckIds = new[] { "function.behavior" } } },
                rules = new[] { new { id = "naming", documentPath = "Docs/Rules.md", section = "Naming", allowNotApplicable } },
                reviewPath = ReviewPath,
            })!.AsObject();
            SaveRequest();
        }

        public void CreateEvidence()
        {
            RequestHash = HashFile(Resolve(RequestPath));
            SnapshotHash = ReviewEvidenceGate.Snapshot(Root, RequestPath, RequestHash);
            Assert(SnapshotHash == HashFile(Resolve(SnapshotPath)), "snapshot returned hash differs from written bytes");
            DateTime started = DateTime.UtcNow;
            Gate = JsonSerializer.SerializeToNode(new
            {
                schemaVersion = 1,
                runId = "evidence-test",
                profile = "Example.Function",
                profileVersion = 1,
                success = true,
                status = "Pass",
                message = "Fixture function passed.",
                checks = new[] { new { id = "function.behavior", success = true, status = "Pass", message = "Observed expected behavior.", expected = "one", actual = "one" } },
                artifacts = Array.Empty<object>(),
                changedFiles = Array.Empty<string>(),
                startedAtUtc = started.ToString("O"),
                finishedAtUtc = DateTime.UtcNow.ToString("O"),
            })!.AsObject();
            SaveGate();
            Review = JsonSerializer.SerializeToNode(new
            {
                schemaVersion = 2,
                runId = "evidence-test",
                requestSha256 = RequestHash,
                snapshotSha256 = SnapshotHash,
                reviewerId = "independent-reviewer",
                verdict = "Approve",
                gateEvidence = new[] { new { path = GatePath, sha256 = HashFile(Resolve(GatePath)) } },
                ruleCoverage = new[] { new { id = "naming", status = "Satisfied", evidence = "Candidate/Work.cs:1; private field follows _camelCase." } },
                findings = Array.Empty<object>(),
                summary = new { responsibilities = "Existing responsibility preserved.", dependencies = "No dependency direction changed.", remainingRisks = "Fixture only; not gameplay validation." },
            })!.AsObject();
            SaveReview();
        }

        public void AddFinding(string severity)
        {
            Review["findings"]!.AsArray().Add(JsonSerializer.SerializeToNode(new
            {
                id = "finding-1", severity, ruleId = "naming", file = "Candidate/Work.cs",
                evidence = "Candidate/Work.cs:1; fixture diagnostic.", recommendation = "Correct the declaration.", origin = "Introduced",
            }));
        }

        public void AssertAccept(int expectedExit)
        {
            int actualExit = ReviewEvidenceGate.Accept(Root, RequestPath, RequestHash, SnapshotHash);
            Assert(actualExit == expectedExit, $"Expected exit {expectedExit}, actual {actualExit}.");
            GateResultSummary result = HarnessResultContracts.ReadGateResult(Resolve(OutputPath));
            GateOutcome expectedOutcome = expectedExit switch
            {
                0 => GateOutcome.Pass,
                1 => GateOutcome.Fail,
                _ => GateOutcome.InfrastructureError,
            };
            Assert(result.Outcome == expectedOutcome, $"Output must report {expectedOutcome}, actual {result.Outcome}.");
            Assert(result.RunId == "evidence-test", "output run mismatch");
        }

        public void SaveRequest() => Write(RequestPath, Request.ToJsonString());
        public void SaveReview() => Write(ReviewPath, Review.ToJsonString());
        public void SaveGate() => Write(GatePath, Gate.ToJsonString());

        public void SaveGateAndReview()
        {
            SaveGate();
            Review["gateEvidence"]![0]!["sha256"] = HashFile(Resolve(GatePath));
            SaveReview();
        }

        public string Resolve(string relativePath) => Path.Combine(Root, relativePath.Replace('/', Path.DirectorySeparatorChar));

        public void Write(string relativePath, string contents)
        {
            string path = Resolve(relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, contents);
        }

        public void Dispose()
        {
            // Root is a freshly allocated fixture directory, never a caller-supplied path.
            string fullPath = Path.GetFullPath(Root);
            string temporaryRoot = Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            if (!fullPath.StartsWith(temporaryRoot, StringComparison.OrdinalIgnoreCase) ||
                !Path.GetFileName(fullPath).StartsWith("npc-review-evidence-", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Refusing to remove an unexpected fixture path.");
            }
            Directory.Delete(fullPath, recursive: true);
        }
    }
}
