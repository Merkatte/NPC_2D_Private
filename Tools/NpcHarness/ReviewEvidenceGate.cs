using System.Globalization;
using System.Text.Json;
using static NpcHarness.ReviewEvidenceJson;
using static NpcHarness.ReviewEvidenceInputs;

namespace NpcHarness;

internal static class ReviewEvidenceGate
{
    public const string Profile = "Harness.ReviewEvidence";

    public static string Snapshot(string repositoryRoot, string requestPath, string requestSha256)
    {
        JsonElement request = ReadRequest(repositoryRoot, requestPath, requestSha256);
        string runId = Text(request, "runId");
        SortedDictionary<string, string> files = Capture(repositoryRoot, request);
        string output = Resolve(repositoryRoot, SnapshotPath(runId));
        WriteNew(output, new
        {
            schemaVersion = 1, runId, requestSha256 = requestSha256.ToLowerInvariant(),
            createdAtUtc = DateTimeOffset.UtcNow.ToString("O"), files,
        });
        return Sha256(output);
    }

    public static int Accept(string repositoryRoot, string requestPath, string requestSha256, string snapshotSha256)
    {
        DateTimeOffset started = DateTimeOffset.UtcNow;
        string? runId = null;
        string status = "InfrastructureError";
        string message;
        try
        {
            // Establish only a safe output location before checking the pinned request;
            // a bad hash must not leave an earlier Pass at this run's output path.
            string[] requestParts = requestPath.Split('/');
            Require(requestParts.Length == 3 && requestParts[0] == ".harness-runs" &&
                    requestParts[2] == "review-request.json", "Unexpected request path.");
            string requestedRun = requestParts[1];
            Require(!string.IsNullOrWhiteSpace(requestedRun) &&
                    RunPaths.ValidateOrCreateRunId(requestedRun) == requestedRun, "Invalid run ID.");
            runId = requestedRun;
            JsonElement request = ReadRequest(repositoryRoot, requestPath, requestSha256);
            Hash(snapshotSha256);
            string snapshotPath = Resolve(repositoryRoot, SnapshotPath(runId));
            Require(Same(Sha256(snapshotPath), snapshotSha256), "Snapshot hash mismatch.");
            JsonElement snapshot = Read(snapshotPath);
            Fields(snapshot, "schemaVersion", "runId", "requestSha256", "createdAtUtc", "files");
            Require(Number(snapshot, "schemaVersion") == 1 && Text(snapshot, "runId") == runId &&
                    Same(Text(snapshot, "requestSha256"), requestSha256), "Snapshot identity mismatch.");
            DateTimeOffset snapshotTime = Time(snapshot, "createdAtUtc");
            Require(snapshotTime <= started, "Snapshot is in the future.");
            Require(EqualFiles(snapshot.GetProperty("files"), Capture(repositoryRoot, request)),
                "Candidate inputs changed after snapshot.");

            Dictionary<string, string> gateHashes = new(StringComparer.Ordinal);
            bool gatesPassed = true;
            foreach (JsonElement required in Rows(request, "requiredGates"))
            {
                string relative = Text(required, "path");
                string path = Resolve(repositoryRoot, relative);
                string beforeHash = Sha256(path);
                JsonElement gate = Read(path);
                GateResultSummary result = HarnessResultContracts.ReadGateResult(path);
                Require(result.RunId == runId && result.Profile == Text(required, "profile") &&
                        result.ProfileVersion == Number(required, "version"), "Gate identity mismatch: " + relative);
                DateTimeOffset gateStart = Time(gate, "startedAtUtc");
                DateTimeOffset gateEnd = Time(gate, "finishedAtUtc");
                Require(gateStart >= snapshotTime && gateEnd >= gateStart && gateEnd <= DateTimeOffset.UtcNow,
                    "Gate is stale or has invalid timestamps: " + relative);
                Require(result.Outcome != GateOutcome.InfrastructureError, "Required gate has infrastructure failure.");
                HashSet<string> checkIds = Rows(gate, "checks").Select(c => Text(c, "id")).ToHashSet(StringComparer.Ordinal);
                Require(Strings(required, "requiredCheckIds").All(checkIds.Contains), "Required gate check missing.");
                foreach (JsonElement artifact in Rows(gate, "artifacts", nonempty: false))
                {
                    string artifactPath = Text(artifact, "path");
                    InRun(artifactPath, runId);
                    Require(File.Exists(Resolve(repositoryRoot, artifactPath)), "Missing gate artifact: " + artifactPath);
                }
                Require(Same(beforeHash, Sha256(path)), "Gate changed while being read.");
                gateHashes.Add(relative, beforeHash);
                gatesPassed &= result.Outcome == GateOutcome.Pass;
            }

            string reviewPath = Resolve(repositoryRoot, Text(request, "reviewPath"));
            string reviewHash = Sha256(reviewPath);
            JsonElement review = Read(reviewPath);
            bool reviewApproved = ValidateReview(repositoryRoot, request, review, requestSha256, snapshotSha256, gateHashes);
            Require(Same(reviewHash, Sha256(reviewPath)), "Review changed while being read.");
            Require(Same(requestSha256, Sha256(Resolve(repositoryRoot, requestPath))) &&
                    Same(snapshotSha256, Sha256(snapshotPath)), "Request or snapshot changed during verification.");
            foreach ((string path, string hash) in gateHashes)
                Require(Same(hash, Sha256(Resolve(repositoryRoot, path))), "Gate changed during verification.");
            Require(EqualFiles(snapshot.GetProperty("files"), Capture(repositoryRoot, request)),
                "Candidate changed during verification.");
            status = gatesPassed && reviewApproved ? "Pass" : "Fail";
            message = status == "Pass"
                ? "Required review evidence is complete for the unchanged snapshotted inputs; not a blanket project approval."
                : "Required gate or independent review rejected this candidate.";
        }
        catch (Exception exception) when (exception is InvalidDataException or IOException or JsonException or ArgumentException or
                                           InvalidOperationException or UnauthorizedAccessException)
        {
            message = exception.Message;
        }

        if (runId != null && runId.Length > 0 && runId.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_'))
        {
            string output = Resolve(repositoryRoot, OutputPath(runId));
            Directory.CreateDirectory(Path.GetDirectoryName(output)!);
            File.WriteAllText(output, JsonSerializer.Serialize(new
            {
                schemaVersion = 1, runId, profile = Profile, profileVersion = 1,
                success = status == "Pass", status, message,
                checks = new[] { new {
                    id = "review.evidence-complete", success = status == "Pass",
                    status = status == "Pass" ? "Pass" : "Fail", message,
                    expected = "Current required gate and independent review evidence",
                    actual = status,
                } },
                artifacts = Array.Empty<object>(), changedFiles = Array.Empty<string>(),
                startedAtUtc = started.ToString("O"), finishedAtUtc = DateTimeOffset.UtcNow.ToString("O"),
            }, new JsonSerializerOptions { WriteIndented = true }));
        }
        Console.WriteLine($"{status}: {message}");
        return status == "Pass" ? 0 : status == "Fail" ? 1 : 2;
    }

    private static JsonElement ReadRequest(string root, string path, string expectedHash)
    {
        Hash(expectedHash);
        string full = Resolve(root, path);
        Require(Same(Sha256(full), expectedHash), "Request hash mismatch.");
        JsonElement request = Read(full);
        Fields(request, "schemaVersion", "runId", "inputRoots", "authorIds", "requiredGates", "rules", "reviewPath");
        Require(Number(request, "schemaVersion") == 1, "Unsupported request version.");
        string runId = Text(request, "runId");
        Require(RunPaths.ValidateOrCreateRunId(runId) == runId, "Invalid run ID.");
        Require(path == RequestPath(runId), "Request must use .harness-runs/<runId>/review-request.json.");
        Strings(request, "inputRoots");
        Strings(request, "authorIds");
        List<string> evidencePaths = new() { path, SnapshotPath(runId), OutputPath(runId), Text(request, "reviewPath") };
        foreach (JsonElement gate in Rows(request, "requiredGates"))
        {
            Fields(gate, "path", "profile", "version", "requiredCheckIds");
            evidencePaths.Add(Text(gate, "path"));
            Require(Text(gate, "profile") != Profile, "Review evidence cannot accept itself.");
            Number(gate, "version");
            Strings(gate, "requiredCheckIds");
        }
        Unique(evidencePaths, "evidence paths");
        foreach (string evidence in evidencePaths)
        {
            InRun(evidence, runId);
            Require(evidence.EndsWith(".json", StringComparison.Ordinal), "Evidence must be JSON.");
            Resolve(root, evidence);
        }
        JsonElement[] rules = Rows(request, "rules");
        Unique(rules.Select(r => Text(r, "id")), "rule IDs");
        foreach (JsonElement rule in rules)
        {
            Fields(rule, "id", "documentPath", "section", "allowNotApplicable");
            Resolve(root, Text(rule, "documentPath"));
            Text(rule, "section");
            Boolean(rule, "allowNotApplicable");
        }
        return request;
    }

    private static bool ValidateReview(string root, JsonElement request, JsonElement review,
        string requestHash, string snapshotHash, Dictionary<string, string> gateHashes)
    {
        Fields(review, "schemaVersion", "runId", "requestSha256", "snapshotSha256", "reviewerId", "verdict",
            "gateEvidence", "ruleCoverage", "findings", "summary");
        Require(Number(review, "schemaVersion") == 2 && Text(review, "runId") == Text(request, "runId") &&
                Same(Text(review, "requestSha256"), requestHash) && Same(Text(review, "snapshotSha256"), snapshotHash),
            "Review candidate identity mismatch.");
        Require(!Strings(request, "authorIds").Contains(Text(review, "reviewerId"), StringComparer.OrdinalIgnoreCase),
            "Candidate author cannot be its reviewer.");
        string verdict = Text(review, "verdict");
        Require(new[] { "Approve", "ChangesRequested", "InsufficientEvidence" }.Contains(verdict), "Unknown verdict.");
        JsonElement[] gateEvidence = Rows(review, "gateEvidence");
        Unique(gateEvidence.Select(g => Text(g, "path")), "review gate paths");
        Require(gateEvidence.Length == gateHashes.Count, "Review gate evidence is incomplete.");
        foreach (JsonElement evidence in gateEvidence)
        {
            Fields(evidence, "path", "sha256");
            Require(gateHashes.TryGetValue(Text(evidence, "path"), out string? hash) && Same(hash, Text(evidence, "sha256")),
                "Review gate hash mismatch.");
        }
        Dictionary<string, JsonElement> rules = Rows(request, "rules").ToDictionary(r => Text(r, "id"), StringComparer.Ordinal);
        JsonElement[] coverage = Rows(review, "ruleCoverage");
        Unique(coverage.Select(c => Text(c, "id")), "coverage IDs");
        Require(coverage.Length == rules.Count, "Required review coverage missing.");
        HashSet<string> violated = new(StringComparer.Ordinal);
        bool notCovered = false;
        foreach (JsonElement row in coverage)
        {
            Fields(row, "id", "status", "evidence");
            string id = Text(row, "id");
            Require(rules.TryGetValue(id, out JsonElement rule), "Unknown coverage rule.");
            Text(row, "evidence");
            string status = Text(row, "status");
            Require(new[] { "Satisfied", "Violated", "NotCovered", "NotApplicable" }.Contains(status), "Unknown coverage status.");
            Require(status != "NotApplicable" || Boolean(rule, "allowNotApplicable"), "NotApplicable is not authorized.");
            if (status == "Violated") violated.Add(id);
            notCovered |= status == "NotCovered";
        }
        JsonElement[] findings = Rows(review, "findings", nonempty: false);
        Unique(findings.Select(f => Text(f, "id")), "finding IDs");
        HashSet<string> blockingRules = new(StringComparer.Ordinal);
        foreach (JsonElement finding in findings)
        {
            Fields(finding, "id", "severity", "ruleId", "file", "evidence", "recommendation", "origin");
            string ruleId = Text(finding, "ruleId");
            Require(rules.ContainsKey(ruleId), "Finding references unknown rule.");
            Resolve(root, Text(finding, "file"));
            Text(finding, "evidence");
            Text(finding, "recommendation");
            Require(new[] { "Introduced", "PreExisting" }.Contains(Text(finding, "origin")), "Unknown finding origin.");
            string severity = Text(finding, "severity");
            Require(new[] { "Critical", "Major", "Minor" }.Contains(severity), "Unknown severity.");
            if (severity != "Minor") blockingRules.Add(ruleId);
        }
        Require(violated.All(blockingRules.Contains), "Violated rules require blocking findings.");
        string expectedVerdict = blockingRules.Count > 0 ? "ChangesRequested" : notCovered ? "InsufficientEvidence" : "Approve";
        Require(verdict == expectedVerdict, "Review verdict contradicts coverage/findings.");
        JsonElement summary = review.GetProperty("summary");
        Fields(summary, "responsibilities", "dependencies", "remainingRisks");
        Text(summary, "responsibilities");
        Text(summary, "dependencies");
        Text(summary, "remainingRisks");
        return verdict == "Approve";
    }

    private static DateTimeOffset Time(JsonElement value, string field)
    {
        Require(DateTimeOffset.TryParse(Text(value, field), CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind, out DateTimeOffset result) && result.Offset == TimeSpan.Zero,
            "Expected UTC timestamp: " + field);
        return result;
    }

    private static bool Same(string? left, string right) => string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
    private static void InRun(string path, string runId) => Require(
        path.StartsWith($".harness-runs/{runId}/", StringComparison.Ordinal), "Evidence must belong to this run.");
    private static string RequestPath(string runId) => $".harness-runs/{runId}/review-request.json";
    private static string SnapshotPath(string runId) => $".harness-runs/{runId}/review-snapshot.json";
    private static string OutputPath(string runId) => $".harness-runs/{runId}/gate-results/review-evidence.json";
}
