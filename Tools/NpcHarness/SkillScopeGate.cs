using System.Diagnostics;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace NpcHarness;

internal sealed record ScopeGateCheck(
    string Id,
    bool Success,
    string Message,
    string Expected,
    string Actual);

internal sealed record ScopeGateEvaluation(
    IReadOnlyList<ScopeGateCheck> Checks,
    IReadOnlyList<string> ChangedFiles)
{
    public bool Success => Checks.All(check => check.Success);
}

internal sealed record PngInfo(int Width, int Height, bool HasAlpha);

internal static class ScopePathPolicy
{
    public static void ValidateRepositoryPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path) ||
            Path.IsPathRooted(path) ||
            path.Contains('\\') ||
            path.Contains(':') ||
            path.Contains('*') ||
            path.Contains('?') ||
            path.Split('/').Any(segment => segment.Length == 0 || segment == "." || segment == ".."))
        {
            throw new InvalidDataException($"Repository path is invalid: {path}");
        }
    }

    public static void ValidatePattern(string pattern)
    {
        string path = pattern.EndsWith("/**", StringComparison.Ordinal)
            ? pattern[..^3]
            : pattern;
        ValidateRepositoryPath(path);
        if (!string.Equals(path, pattern, StringComparison.Ordinal) && !pattern.EndsWith("/**", StringComparison.Ordinal))
        {
            throw new InvalidDataException($"Only exact paths and trailing '/**' patterns are supported: {pattern}");
        }
    }

    public static bool Matches(string pattern, string repositoryPath)
    {
        string normalizedPath = repositoryPath.Replace('\\', '/');
        if (pattern.EndsWith("/**", StringComparison.Ordinal))
        {
            string prefix = pattern[..^3];
            return normalizedPath.StartsWith(prefix + "/", StringComparison.Ordinal);
        }
        return string.Equals(pattern, normalizedPath, StringComparison.Ordinal);
    }

    public static bool IsCoveredBy(string narrowerPattern, string broaderPattern)
    {
        if (broaderPattern.EndsWith("/**", StringComparison.Ordinal))
        {
            string prefix = broaderPattern[..^3];
            string narrowerRoot = narrowerPattern.EndsWith("/**", StringComparison.Ordinal)
                ? narrowerPattern[..^3]
                : narrowerPattern;
            return narrowerRoot.StartsWith(prefix + "/", StringComparison.Ordinal) ||
                   string.Equals(narrowerRoot, prefix, StringComparison.Ordinal);
        }
        return string.Equals(narrowerPattern, broaderPattern, StringComparison.Ordinal);
    }

    public static string ResolveInsideRepository(string repositoryRoot, string repositoryPath)
    {
        ValidateRepositoryPath(repositoryPath);
        string fullRoot = Path.GetFullPath(repositoryRoot);
        string fullPath = Path.GetFullPath(repositoryPath, fullRoot);
        string prefix = fullRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) +
                        Path.DirectorySeparatorChar;
        StringComparison comparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;
        if (!fullPath.StartsWith(prefix, comparison))
        {
            throw new InvalidDataException($"Path escapes the repository: {repositoryPath}");
        }
        return fullPath;
    }
}

internal static class SkillScopeGate
{
    public static ScopeGateEvaluation Evaluate(
        string repositoryRoot,
        string policyPath,
        SkillPolicyDefinition policy,
        WorkerAssignmentDefinition assignment,
        string expectedAssignmentSha256,
        string actualAssignmentSha256)
    {
        List<ScopeGateCheck> checks = new List<ScopeGateCheck>();
        bool assignmentIntegrity = string.Equals(
            expectedAssignmentSha256,
            actualAssignmentSha256,
            StringComparison.OrdinalIgnoreCase);
        AddCheck(
            checks,
            "scope.assignment-integrity",
            assignmentIntegrity,
            "WorkerAssignment matches the root-recorded pre-delegation SHA-256.",
            expectedAssignmentSha256,
            actualAssignmentSha256);
        bool policyUnmodified = IsTrackedAndUnmodified(repositoryRoot, policyPath);
        AddCheck(
            checks,
            "scope.policy-integrity",
            policyUnmodified,
            "Selected SkillPolicy is tracked and unchanged from HEAD.",
            "tracked and unmodified",
            policyUnmodified ? "tracked and unmodified" : "untracked or modified");
        AddCheck(
            checks,
            "scope.assignment-identity",
            assignment.RoleSkill == policy.PolicyId &&
            assignment.PolicyVersion == policy.PolicyVersion &&
            assignment.Execution.Kind == policy.ExecutionKind,
            "WorkerAssignment identity and execution kind match the selected SkillPolicy.",
            $"roleSkill={policy.PolicyId}; policyVersion={policy.PolicyVersion}; execution={policy.ExecutionKind}",
            $"roleSkill={assignment.RoleSkill}; policyVersion={assignment.PolicyVersion}; " +
            $"execution={assignment.Execution.Kind}");

        bool pathsNarrowPolicy = assignment.WritablePaths.All(path =>
            policy.CandidateRules.Any(rule => ScopePathPolicy.IsCoveredBy(path, rule.Path)) &&
            !policy.ForbiddenCandidatePaths.Any(forbidden => PatternsOverlap(path, forbidden)) &&
            !assignment.ForbiddenPaths.Any(forbidden => PatternsOverlap(path, forbidden)));
        bool extensionsAllowed = assignment.WritablePaths.All(path =>
            path.EndsWith("/**", StringComparison.Ordinal) || IsPathAndExtensionAllowed(path, policy));
        AddCheck(
            checks,
            "scope.assignment-paths",
            pathsNarrowPolicy && extensionsAllowed,
            "Assigned writable paths narrow the policy allowlist and avoid forbidden paths and extensions.",
            string.Join(", ", policy.CandidateRules.Select(rule => rule.Path)),
            string.Join(", ", assignment.WritablePaths));

        bool toolsNarrowPolicy = assignment.AllowedTools.All(tool => policy.AllowedTools.Contains(tool, StringComparer.Ordinal));
        AddCheck(
            checks,
            "scope.assignment-tools",
            toolsNarrowPolicy,
            "Assigned tools are a subset of the role policy.",
            string.Join(", ", policy.AllowedTools),
            string.Join(", ", assignment.AllowedTools));

        string runArtifactPrefix = $".harness-runs/{assignment.RunId}/";
        bool artifactPathsValid = assignment.ArtifactPaths.WorkerResultPath.StartsWith(
                                      runArtifactPrefix + "worker-results/",
                                      StringComparison.Ordinal) &&
                                  assignment.ArtifactPaths.LogPath.StartsWith(
                                      runArtifactPrefix + "logs/",
                                      StringComparison.Ordinal) &&
                                  ExecutionArtifactsStayInRun(assignment.Execution, runArtifactPrefix);
        bool acceptanceRootValid = policy.AcceptanceManifestRoots.Any(root =>
            assignment.AcceptanceGate.ManifestPath.StartsWith(root.TrimEnd('/') + "/", StringComparison.Ordinal));
        AddCheck(
            checks,
            "scope.assignment-artifacts",
            artifactPathsValid && acceptanceRootValid,
            "Worker, execution, log, and acceptance artifacts stay inside their declared roots.",
            $"{runArtifactPrefix} and [{string.Join(", ", policy.AcceptanceManifestRoots)}]",
            $"workerResult={assignment.ArtifactPaths.WorkerResultPath}; " +
            $"log={assignment.ArtifactPaths.LogPath}; " +
            $"acceptance={assignment.AcceptanceGate.ManifestPath}");

        if (assignment.Execution is HarnessJobExecutionDefinition harness)
        {
            ValidateJob(repositoryRoot, policy, assignment, harness, checks);
            ValidateAdapterReceipt(repositoryRoot, harness, checks);
        }
        else if (assignment.Execution is RasterArtExecutionDefinition raster)
        {
            ValidateRasterOutput(repositoryRoot, assignment, raster, checks);
        }
        ValidateAcceptance(repositoryRoot, assignment, checks);

        string head = RunGit(repositoryRoot, "rev-parse", "HEAD").Trim();
        AddCheck(
            checks,
            "scope.baseline-commit",
            string.Equals(head, assignment.BaselineCommit, StringComparison.OrdinalIgnoreCase),
            "Repository HEAD matches the assignment baseline commit.",
            assignment.BaselineCommit,
            head);

        IReadOnlyList<string> changedFiles = ReadChangedFiles(repositoryRoot);
        HashSet<string> baselinePaths = ValidateBaselineDirtyFiles(
            repositoryRoot,
            assignment,
            assignmentIntegrity,
            checks);
        string[] candidateChangedFiles = changedFiles
            .Where(path => !baselinePaths.Contains(path))
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();
        string[] unauthorized = candidateChangedFiles
            .Where(path => !IsCandidatePathAllowed(path, assignment, policy))
            .ToArray();

        if (assignment.Execution is DirectCodeExecutionDefinition code)
        {
            ValidateCodeCandidate(repositoryRoot, assignment, policy, code, candidateChangedFiles, checks);
        }
        AddCheck(
            checks,
            "scope.changed-files",
            unauthorized.Length == 0,
            unauthorized.Length == 0
                ? "All candidate changes are inside assigned writable paths."
                : "Candidate contains changes outside assigned writable paths.",
            string.Join(", ", assignment.WritablePaths),
            candidateChangedFiles.Length == 0 ? "none" : string.Join(", ", candidateChangedFiles));

        return new ScopeGateEvaluation(checks, candidateChangedFiles);
    }

    private static void ValidateJob(
        string repositoryRoot,
        SkillPolicyDefinition policy,
        WorkerAssignmentDefinition assignment,
        HarnessJobExecutionDefinition harness,
        ICollection<ScopeGateCheck> checks)
    {
        try
        {
            string jobPath = ScopePathPolicy.ResolveInsideRepository(repositoryRoot, harness.JobPath);
            HarnessResultContracts.ValidateJob(jobPath);
            using JsonDocument document = JsonDocument.Parse(File.ReadAllText(jobPath));
            JsonElement root = document.RootElement;
            string jobId = root.GetProperty("jobId").GetString() ?? string.Empty;
            JsonElement steps = root.GetProperty("steps");
            List<string> tools = new List<string>();
            List<string> mutationPaths = new List<string>();
            List<string> componentTypes = new List<string>();
            foreach (JsonElement step in steps.EnumerateArray())
            {
                string tool = step.TryGetProperty("tool", out JsonElement toolValue)
                    ? toolValue.GetString() ?? string.Empty
                    : string.Empty;
                tools.Add(tool);
                string? mutationPath = ReadMutationPath(step, tool);
                if (!string.IsNullOrWhiteSpace(mutationPath))
                {
                    mutationPaths.Add(mutationPath);
                }
                if (step.TryGetProperty("componentTypeId", out JsonElement componentTypeValue) &&
                    componentTypeValue.ValueKind == JsonValueKind.String &&
                    !string.IsNullOrWhiteSpace(componentTypeValue.GetString()))
                {
                    componentTypes.Add(componentTypeValue.GetString()!);
                }
            }

            foreach (string mutationPath in mutationPaths)
            {
                ScopePathPolicy.ValidateRepositoryPath(mutationPath);
            }

            bool valid = string.Equals(jobId, harness.JobId, StringComparison.Ordinal) &&
                         tools.All(tool => assignment.AllowedTools.Contains(tool, StringComparer.Ordinal)) &&
                         componentTypes.All(componentType =>
                             harness.AllowedComponentTypes.Contains(componentType, StringComparer.Ordinal)) &&
                         mutationPaths.All(path => IsCandidatePathAllowed(path, assignment, policy));
            AddCheck(
                checks,
                "scope.job",
                valid,
                "Harness Job identity, tools, and mutation paths match the assignment.",
                $"jobId={harness.JobId}; tools=[{string.Join(", ", assignment.AllowedTools)}]; " +
                $"components=[{string.Join(", ", harness.AllowedComponentTypes)}]",
                $"jobId={jobId}; tools=[{string.Join(", ", tools)}]; " +
                $"components=[{string.Join(", ", componentTypes)}]; paths=[{string.Join(", ", mutationPaths)}]");
        }
        catch (Exception exception) when (exception is IOException or InvalidDataException or JsonException)
        {
            AddCheck(checks, "scope.job", false, "Harness Job is missing or invalid.",
                harness.JobPath, exception.Message);
        }
    }

    private static void ValidateAdapterReceipt(
        string repositoryRoot,
        HarnessJobExecutionDefinition harness,
        ICollection<ScopeGateCheck> checks)
    {
        try
        {
            string resultPath = ScopePathPolicy.ResolveInsideRepository(
                repositoryRoot,
                harness.AdapterResultPath);
            AdapterResultSummary result = HarnessResultContracts.ReadAdapterResult(resultPath);
            AddCheck(
                checks,
                "scope.adapter-receipt",
                result.Success,
                "Adapter receipt reports a successful candidate execution.",
                "Succeeded or NoChange",
                result.State);
        }
        catch (Exception exception) when (exception is IOException or InvalidDataException or JsonException)
        {
            AddCheck(checks, "scope.adapter-receipt", false, "Adapter receipt is missing or invalid.",
                harness.AdapterResultPath, exception.Message);
        }
    }

    private static bool ExecutionArtifactsStayInRun(
        WorkerExecutionDefinition execution,
        string runArtifactPrefix)
    {
        return execution switch
        {
            HarnessJobExecutionDefinition harness =>
                harness.JobPath.StartsWith(runArtifactPrefix + "assignments/", StringComparison.Ordinal) &&
                harness.AdapterResultPath.StartsWith(
                    runArtifactPrefix + "adapter-results/",
                    StringComparison.Ordinal),
            RasterArtExecutionDefinition raster =>
                raster.ImportSpecPath.StartsWith(runArtifactPrefix + "artifacts/", StringComparison.Ordinal),
            DirectCodeExecutionDefinition => true,
            _ => false,
        };
    }

    private static void ValidateCodeCandidate(
        string repositoryRoot,
        WorkerAssignmentDefinition assignment,
        SkillPolicyDefinition policy,
        DirectCodeExecutionDefinition code,
        IReadOnlyCollection<string> candidateChangedFiles,
        ICollection<ScopeGateCheck> checks)
    {
        List<string> issues = new List<string>();
        if (!code.CompileRequired)
        {
            issues.Add("compileRequired=false");
        }

        foreach (string sourcePath in code.SourcePaths)
        {
            if (!string.Equals(Path.GetExtension(sourcePath), ".cs", StringComparison.OrdinalIgnoreCase) ||
                !IsCandidatePathAllowed(sourcePath, assignment, policy))
            {
                issues.Add($"invalid source path: {sourcePath}");
                continue;
            }
            string fullPath = ScopePathPolicy.ResolveInsideRepository(repositoryRoot, sourcePath);
            if (!File.Exists(fullPath) || new FileInfo(fullPath).Length == 0)
            {
                issues.Add($"missing or empty source: {sourcePath}");
            }
        }

        foreach (string documentPath in code.OwningDocumentPaths)
        {
            if (!string.Equals(Path.GetExtension(documentPath), ".md", StringComparison.OrdinalIgnoreCase) ||
                !IsCandidatePathAllowed(documentPath, assignment, policy))
            {
                issues.Add($"invalid owning document path: {documentPath}");
                continue;
            }
            string fullPath = ScopePathPolicy.ResolveInsideRepository(repositoryRoot, documentPath);
            if (!File.Exists(fullPath))
            {
                issues.Add($"missing owning document: {documentPath}");
            }
        }

        HashSet<string> declaredPaths = new HashSet<string>(
            code.SourcePaths.Concat(code.OwningDocumentPaths),
            StringComparer.Ordinal);
        HashSet<string> newSources = new HashSet<string>(
            code.SourcePaths.Where(path => !IsTrackedAtHead(repositoryRoot, path)),
            StringComparer.Ordinal);
        foreach (string newSource in newSources)
        {
            string metaPath = newSource + ".meta";
            if (!candidateChangedFiles.Contains(metaPath, StringComparer.Ordinal) ||
                !File.Exists(ScopePathPolicy.ResolveInsideRepository(repositoryRoot, metaPath)))
            {
                issues.Add($"new source is missing companion meta: {newSource}");
            }
            else
            {
                declaredPaths.Add(metaPath);
            }
        }

        foreach (string changedPath in candidateChangedFiles)
        {
            if (!declaredPaths.Contains(changedPath))
            {
                issues.Add($"undeclared code candidate path: {changedPath}");
            }
        }

        AddCheck(
            checks,
            "scope.code-assets",
            issues.Count == 0,
            issues.Count == 0
                ? "Code sources, owning documents, and new companion meta files match the assignment."
                : "Code execution contract is incomplete or contains undeclared candidate files.",
            "declared non-empty C#; owning docs; compileRequired=true; new C# companion meta",
            issues.Count == 0 ? "valid" : string.Join(", ", issues));
    }

    private static void ValidateRasterOutput(
        string repositoryRoot,
        WorkerAssignmentDefinition assignment,
        RasterArtExecutionDefinition raster,
        ICollection<ScopeGateCheck> checks)
    {
        List<string> issues = new List<string>();
        if (!HasSingleRasterOutput(assignment, raster))
        {
            issues.Add("writablePaths must contain exactly outputPath");
        }
        if (!string.Equals(Path.GetExtension(raster.OutputPath), ".png", StringComparison.OrdinalIgnoreCase))
        {
            issues.Add("outputPath is not PNG");
        }
        foreach (string referencePath in raster.ReferenceAssets)
        {
            string fullReferencePath = ScopePathPolicy.ResolveInsideRepository(repositoryRoot, referencePath);
            if (!File.Exists(fullReferencePath))
            {
                issues.Add($"missing reference asset: {referencePath}");
            }
        }
        string importMetaPath = ScopePathPolicy.ResolveInsideRepository(
            repositoryRoot,
            raster.ImportReferenceMetaPath);
        if (!File.Exists(importMetaPath) ||
            !raster.ReferenceAssets.Any(reference =>
                string.Equals(reference + ".meta", raster.ImportReferenceMetaPath, StringComparison.Ordinal)))
        {
            issues.Add("import reference meta is missing or not paired with an approved reference");
        }
        else
        {
            ValidateImportSpec(
                repositoryRoot,
                raster.ImportSpecPath,
                importMetaPath,
                issues);
        }

        try
        {
            string outputPath = ScopePathPolicy.ResolveInsideRepository(repositoryRoot, raster.OutputPath);
            PngInfo png = ReadPngInfo(outputPath);
            if (png.Width != raster.Width || png.Height != raster.Height)
            {
                issues.Add($"dimensions={png.Width}x{png.Height}");
            }
            if (raster.RequireAlpha && !png.HasAlpha)
            {
                issues.Add("alpha channel or transparency chunk missing");
            }
            if (raster.Width % raster.FrameColumns != 0 || raster.Height % raster.FrameRows != 0)
            {
                issues.Add("frame grid does not divide output dimensions");
            }
        }
        catch (Exception exception) when (exception is IOException or InvalidDataException)
        {
            issues.Add(exception.Message);
        }

        AddCheck(
            checks,
            "scope.raster-output",
            issues.Count == 0,
            issues.Count == 0
                ? "Raster output matches its assigned PNG dimensions, alpha, frame grid, and references."
                : "Raster execution contract or output is invalid.",
            $"{raster.Width}x{raster.Height}; alpha={raster.RequireAlpha}; " +
            $"frames={raster.FrameColumns}x{raster.FrameRows}",
            issues.Count == 0 ? raster.OutputPath : string.Join(", ", issues));
    }

    private static void ValidateImportSpec(
        string repositoryRoot,
        string importSpecPath,
        string referenceMetaPath,
        ICollection<string> issues)
    {
        string[] requiredProperties =
        {
            "schemaVersion", "textureType", "spriteMode", "pixelsPerUnit", "pivot", "filterMode",
            "compression", "alphaIsTransparency", "wrapMode", "mipmaps", "sRGB", "meshType",
        };
        try
        {
            string fullSpecPath = ScopePathPolicy.ResolveInsideRepository(repositoryRoot, importSpecPath);
            using JsonDocument document = JsonDocument.Parse(File.ReadAllText(fullSpecPath));
            JsonElement root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                throw new InvalidDataException("importSpec must be an object.");
            }
            HashSet<string> required = new HashSet<string>(requiredProperties, StringComparer.Ordinal);
            HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (JsonProperty property in root.EnumerateObject())
            {
                if (!seen.Add(property.Name) || !required.Contains(property.Name))
                {
                    throw new InvalidDataException($"importSpec property is duplicate or unsupported: {property.Name}");
                }
            }
            if (!required.SetEquals(seen))
            {
                throw new InvalidDataException("importSpec is missing one or more required properties.");
            }
            if (!root.TryGetProperty("schemaVersion", out JsonElement schemaVersion) ||
                !schemaVersion.TryGetInt32(out int schemaVersionValue) ||
                schemaVersionValue != 1)
            {
                throw new InvalidDataException("importSpec schemaVersion must be 1.");
            }

            string meta = File.ReadAllText(referenceMetaPath);
            CompareImportString(root, "textureType", MapMetaValue(meta, "textureType", "8", "Sprite"), issues);
            CompareImportString(root, "spriteMode", MapMetaValue(meta, "spriteMode", "1", "Single", "2", "Multiple"), issues);
            CompareImportNumber(root, "pixelsPerUnit", ReadMetaScalar(meta, "spritePixelsToUnits"), issues);
            CompareImportString(root, "filterMode", MapMetaValue(meta, "filterMode", "0", "Point", "1", "Bilinear", "2", "Trilinear"), issues);
            CompareImportString(
                root,
                "compression",
                MapMetaValue(meta, "textureCompression", "0", "Uncompressed", "1", "Compressed", "2", "CompressedHQ", "3", "CompressedLQ"),
                issues);
            CompareImportBoolean(root, "alphaIsTransparency", ReadMetaScalar(meta, "alphaIsTransparency") == "1", issues);
            CompareImportString(root, "wrapMode", MapMetaValue(meta, "wrapU", "0", "Repeat", "1", "Clamp", "2", "Mirror", "3", "MirrorOnce"), issues);
            CompareImportBoolean(root, "mipmaps", ReadMetaScalar(meta, "enableMipMap") == "1", issues);
            CompareImportBoolean(root, "sRGB", ReadMetaScalar(meta, "sRGBTexture") == "1", issues);
            CompareImportString(root, "meshType", MapMetaValue(meta, "spriteMeshType", "0", "FullRect", "1", "Tight"), issues);
            CompareImportPivot(root, ReadMetaScalar(meta, "spritePivot"), issues);
        }
        catch (Exception exception) when (exception is IOException or JsonException or InvalidDataException)
        {
            issues.Add($"invalid importSpec: {exception.Message}");
        }
    }

    private static string ReadMetaScalar(string meta, string key)
    {
        string prefix = key + ":";
        foreach (string line in meta.Split('\n'))
        {
            string trimmed = line.Trim();
            if (trimmed.StartsWith(prefix, StringComparison.Ordinal))
            {
                return trimmed[prefix.Length..].Trim();
            }
        }
        throw new InvalidDataException($"Reference meta is missing {key}.");
    }

    private static string MapMetaValue(string meta, string key, params string[] mappings)
    {
        string value = ReadMetaScalar(meta, key);
        for (int index = 0; index + 1 < mappings.Length; index += 2)
        {
            if (string.Equals(value, mappings[index], StringComparison.Ordinal))
            {
                return mappings[index + 1];
            }
        }
        throw new InvalidDataException($"Reference meta {key} value is unsupported: {value}");
    }

    private static void CompareImportString(
        JsonElement root,
        string propertyName,
        string expected,
        ICollection<string> issues)
    {
        if (!root.TryGetProperty(propertyName, out JsonElement value) ||
            value.ValueKind != JsonValueKind.String ||
            !string.Equals(value.GetString(), expected, StringComparison.Ordinal))
        {
            issues.Add($"importSpec {propertyName} does not match reference ({expected})");
        }
    }

    private static void CompareImportNumber(
        JsonElement root,
        string propertyName,
        string expectedText,
        ICollection<string> issues)
    {
        if (!double.TryParse(
                expectedText,
                System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture,
                out double expected) ||
            !root.TryGetProperty(propertyName, out JsonElement value) ||
            !value.TryGetDouble(out double actual) ||
            Math.Abs(actual - expected) > 0.0001)
        {
            issues.Add($"importSpec {propertyName} does not match reference ({expectedText})");
        }
    }

    private static void CompareImportBoolean(
        JsonElement root,
        string propertyName,
        bool expected,
        ICollection<string> issues)
    {
        if (!root.TryGetProperty(propertyName, out JsonElement value) ||
            (value.ValueKind != JsonValueKind.True && value.ValueKind != JsonValueKind.False) ||
            value.GetBoolean() != expected)
        {
            issues.Add($"importSpec {propertyName} does not match reference ({expected})");
        }
    }

    internal static bool HasSingleRasterOutput(
        WorkerAssignmentDefinition assignment,
        RasterArtExecutionDefinition raster)
    {
        return assignment.WritablePaths.Count == 1 &&
               string.Equals(assignment.WritablePaths[0], raster.OutputPath, StringComparison.Ordinal);
    }

    internal static void CompareImportPivot(
        JsonElement root,
        string expectedText,
        ICollection<string> issues)
    {
        if (!root.TryGetProperty("pivot", out JsonElement pivot) ||
            pivot.ValueKind != JsonValueKind.Object)
        {
            issues.Add("importSpec pivot is invalid");
            return;
        }

        HashSet<string> expectedProperties = new HashSet<string>(new[] { "x", "y" }, StringComparer.Ordinal);
        HashSet<string> seenProperties = new HashSet<string>(StringComparer.Ordinal);
        foreach (JsonProperty property in pivot.EnumerateObject())
        {
            if (!seenProperties.Add(property.Name) || !expectedProperties.Contains(property.Name))
            {
                issues.Add($"importSpec pivot property is duplicate or unsupported: {property.Name}");
                return;
            }
        }

        if (!expectedProperties.SetEquals(seenProperties) ||
            !pivot.TryGetProperty("x", out JsonElement xValue) ||
            !pivot.TryGetProperty("y", out JsonElement yValue) ||
            !xValue.TryGetDouble(out double x) ||
            !yValue.TryGetDouble(out double y) ||
            !double.IsFinite(x) ||
            !double.IsFinite(y) ||
            x < 0 || x > 1 || y < 0 || y > 1)
        {
            issues.Add("importSpec pivot is invalid");
            return;
        }

        string normalized = expectedText.Replace(" ", string.Empty, StringComparison.Ordinal);
        string expectedPrefix = "{x:";
        string separator = ",y:";
        int separatorIndex = normalized.IndexOf(separator, StringComparison.Ordinal);
        if (!normalized.StartsWith(expectedPrefix, StringComparison.Ordinal) ||
            !normalized.EndsWith('}') ||
            separatorIndex < 0 ||
            !double.TryParse(
                normalized[expectedPrefix.Length..separatorIndex],
                System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture,
                out double expectedX) ||
            !double.TryParse(
                normalized[(separatorIndex + separator.Length)..^1],
                System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture,
                out double expectedY) ||
            Math.Abs(x - expectedX) > 0.0001 ||
            Math.Abs(y - expectedY) > 0.0001)
        {
            issues.Add($"importSpec pivot does not match reference ({expectedText})");
        }
    }

    internal static PngInfo ReadPngInfo(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("Raster output does not exist.", path);
        }
        byte[] bytes = File.ReadAllBytes(path);
        byte[] signature = { 137, 80, 78, 71, 13, 10, 26, 10 };
        if (bytes.Length < 33 || !bytes.AsSpan(0, 8).SequenceEqual(signature))
        {
            throw new InvalidDataException("Raster output is not a valid PNG stream.");
        }

        int width = 0;
        int height = 0;
        int bitDepth = 0;
        int colorType = -1;
        bool hasTransparencyChunk = false;
        bool sawHeader = false;
        bool sawPalette = false;
        bool sawTransparency = false;
        bool sawImageData = false;
        bool imageDataEnded = false;
        bool sawEnd = false;
        int paletteEntries = 0;
        using MemoryStream compressedImageData = new MemoryStream();
        int offset = 8;
        while (offset + 12 <= bytes.Length)
        {
            int length = System.Buffers.Binary.BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(offset, 4));
            if (length < 0 || offset + 12L + length > bytes.Length)
            {
                throw new InvalidDataException("PNG chunk length is invalid.");
            }
            string type = Encoding.ASCII.GetString(bytes, offset + 4, 4);
            if (type.Any(character => character is < 'A' or > 'z' || character is > 'Z' and < 'a') ||
                type[2] is >= 'a' and <= 'z')
            {
                throw new InvalidDataException($"PNG chunk type is invalid: {type}.");
            }
            ReadOnlySpan<byte> chunkData = bytes.AsSpan(offset + 8, length);
            uint expectedCrc = System.Buffers.Binary.BinaryPrimitives.ReadUInt32BigEndian(
                bytes.AsSpan(offset + 8 + length, 4));
            uint actualCrc = ComputePngCrc(bytes.AsSpan(offset + 4, length + 4));
            if (actualCrc != expectedCrc)
            {
                throw new InvalidDataException($"PNG {type} chunk CRC is invalid.");
            }

            if (!sawHeader && type != "IHDR")
            {
                throw new InvalidDataException("PNG IHDR must be the first chunk.");
            }
            if (type == "IHDR")
            {
                if (sawHeader || length != 13)
                {
                    throw new InvalidDataException("PNG must contain exactly one 13-byte IHDR chunk.");
                }
                width = System.Buffers.Binary.BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(offset + 8, 4));
                height = System.Buffers.Binary.BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(offset + 12, 4));
                bitDepth = bytes[offset + 16];
                colorType = bytes[offset + 17];
                if (!IsSupportedPngFormat(bitDepth, colorType) ||
                    bytes[offset + 18] != 0 ||
                    bytes[offset + 19] != 0 ||
                    bytes[offset + 20] != 0)
                {
                    throw new InvalidDataException("PNG IHDR uses an unsupported format.");
                }
                sawHeader = true;
            }
            else if (type == "PLTE")
            {
                if (sawPalette || sawImageData || colorType is 0 or 4 ||
                    length < 3 || length > 768 || length % 3 != 0)
                {
                    throw new InvalidDataException("PNG PLTE chunk is invalid or out of order.");
                }
                paletteEntries = length / 3;
                if (colorType == 3 && paletteEntries > 1 << bitDepth)
                {
                    throw new InvalidDataException("PNG PLTE has too many entries for its bit depth.");
                }
                sawPalette = true;
            }
            else if (type == "tRNS")
            {
                bool validTransparency = colorType switch
                {
                    0 => length == 2,
                    2 => length == 6,
                    3 => sawPalette && length > 0 && length <= paletteEntries,
                    _ => false,
                };
                if (sawTransparency || sawImageData || !validTransparency)
                {
                    throw new InvalidDataException("PNG tRNS chunk is invalid or out of order.");
                }
                sawTransparency = true;
                hasTransparencyChunk = true;
            }
            else if (type == "IDAT")
            {
                if (imageDataEnded || colorType == 3 && !sawPalette)
                {
                    throw new InvalidDataException(
                        colorType == 3 && !sawPalette
                            ? "Indexed PNG requires PLTE before IDAT."
                            : "PNG IDAT chunks must be consecutive.");
                }
                sawImageData = true;
                compressedImageData.Write(chunkData);
            }
            else if (type == "IEND")
            {
                if (!sawImageData || length != 0)
                {
                    throw new InvalidDataException("PNG IEND is invalid or precedes image data.");
                }
                sawEnd = true;
                offset += length + 12;
                break;
            }
            else if (type[0] is >= 'A' and <= 'Z')
            {
                throw new InvalidDataException($"PNG contains unsupported critical chunk: {type}.");
            }
            else if (sawImageData)
            {
                imageDataEnded = true;
            }
            offset += length + 12;
        }
        if (!sawHeader || width <= 0 || height <= 0 || colorType < 0)
        {
            throw new InvalidDataException("PNG is missing a valid IHDR chunk.");
        }
        if (!sawEnd || offset != bytes.Length)
        {
            throw new InvalidDataException("PNG must end with one terminal IEND chunk.");
        }

        ValidatePngImageData(compressedImageData.ToArray(), width, height, bitDepth, colorType);
        return new PngInfo(width, height, colorType is 4 or 6 || hasTransparencyChunk);
    }

    private static bool IsSupportedPngFormat(int bitDepth, int colorType)
    {
        return colorType switch
        {
            0 => bitDepth is 1 or 2 or 4 or 8 or 16,
            2 => bitDepth is 8 or 16,
            3 => bitDepth is 1 or 2 or 4 or 8,
            4 => bitDepth is 8 or 16,
            6 => bitDepth is 8 or 16,
            _ => false,
        };
    }

    private static void ValidatePngImageData(
        byte[] compressed,
        int width,
        int height,
        int bitDepth,
        int colorType)
    {
        int channels = colorType switch
        {
            0 or 3 => 1,
            2 => 3,
            4 => 2,
            6 => 4,
            _ => throw new InvalidDataException("PNG color type is unsupported."),
        };
        long rowBytes = ((long)width * channels * bitDepth + 7) / 8;
        long rowLength = rowBytes + 1;
        long expectedLength = rowLength * height;
        if (expectedLength > int.MaxValue)
        {
            throw new InvalidDataException("PNG decoded image data is too large.");
        }

        try
        {
            using MemoryStream input = new MemoryStream(compressed, writable: false);
            using ZLibStream inflater = new ZLibStream(input, CompressionMode.Decompress);
            byte[] buffer = new byte[8192];
            long decodedLength = 0;
            int read;
            while ((read = inflater.Read(buffer, 0, buffer.Length)) > 0)
            {
                for (int index = 0; index < read; index++)
                {
                    if ((decodedLength + index) % rowLength == 0 && buffer[index] > 4)
                    {
                        throw new InvalidDataException(
                            $"PNG scanline filter is invalid: {buffer[index]}.");
                    }
                }
                decodedLength += read;
                if (decodedLength > expectedLength)
                {
                    throw new InvalidDataException("PNG decoded image data exceeds its IHDR dimensions.");
                }
            }
            if (decodedLength != expectedLength)
            {
                throw new InvalidDataException(
                    $"PNG decoded image data length is invalid: {decodedLength}, expected {expectedLength}.");
            }
            if (input.Position != input.Length)
            {
                throw new InvalidDataException("PNG IDAT contains trailing compressed data.");
            }
        }
        catch (InvalidDataException)
        {
            throw;
        }
        catch (Exception exception) when (exception is IOException)
        {
            throw new InvalidDataException("PNG IDAT stream cannot be decoded.", exception);
        }
    }

    internal static uint ComputePngCrc(ReadOnlySpan<byte> bytes)
    {
        uint crc = uint.MaxValue;
        foreach (byte value in bytes)
        {
            crc ^= value;
            for (int bit = 0; bit < 8; bit++)
            {
                uint mask = (uint)-(int)(crc & 1);
                crc = (crc >> 1) ^ (0xedb88320u & mask);
            }
        }
        return ~crc;
    }

    private static void ValidateAcceptance(
        string repositoryRoot,
        WorkerAssignmentDefinition assignment,
        ICollection<ScopeGateCheck> checks)
    {
        try
        {
            SkillPolicyContracts.ValidateAcceptanceIdentity(repositoryRoot, assignment.AcceptanceGate);
            AddCheck(
                checks,
                "scope.acceptance-contract",
                true,
                "Acceptance manifest exists and matches the assigned gate identity.",
                $"{assignment.AcceptanceGate.Profile} v{assignment.AcceptanceGate.ProfileVersion}",
                assignment.AcceptanceGate.ManifestPath);
        }
        catch (Exception exception) when (exception is IOException or InvalidDataException or JsonException)
        {
            AddCheck(checks, "scope.acceptance-contract", false,
                "Acceptance manifest is missing, invalid, or has the wrong identity.",
                $"{assignment.AcceptanceGate.Profile} v{assignment.AcceptanceGate.ProfileVersion}",
                exception.Message);
        }
    }

    private static HashSet<string> ValidateBaselineDirtyFiles(
        string repositoryRoot,
        WorkerAssignmentDefinition assignment,
        bool assignmentIntegrity,
        ICollection<ScopeGateCheck> checks)
    {
        HashSet<string> paths = new HashSet<string>(StringComparer.Ordinal);
        List<string> mismatches = new List<string>();
        foreach (BaselineDirtyFileDefinition dirtyFile in assignment.BaselineDirtyFiles)
        {
            paths.Add(dirtyFile.Path);
            string fullPath = ScopePathPolicy.ResolveInsideRepository(repositoryRoot, dirtyFile.Path);
            string actual = File.Exists(fullPath) ? ComputeSha256(fullPath) : "missing";
            if (!string.Equals(actual, dirtyFile.Sha256, StringComparison.OrdinalIgnoreCase))
            {
                mismatches.Add($"{dirtyFile.Path}={actual}");
            }
        }

        bool baselineValid = assignmentIntegrity && mismatches.Count == 0;
        AddCheck(
            checks,
            "scope.baseline-dirty-files",
            baselineValid,
            "Pre-existing dirty files are bound to the trusted assignment and remain byte-identical.",
            assignment.BaselineDirtyFiles.Count == 0
                ? "trusted assignment; no baseline dirty files"
                : "trusted assignment and recorded SHA-256 values",
            !assignmentIntegrity
                ? "assignment digest mismatch; no baseline exemptions applied"
                : mismatches.Count == 0 ? "unchanged" : string.Join(", ", mismatches));
        return baselineValid ? paths : new HashSet<string>(StringComparer.Ordinal);
    }

    internal static bool IsCandidatePathAllowed(
        string path,
        WorkerAssignmentDefinition assignment,
        SkillPolicyDefinition policy)
    {
        return assignment.WritablePaths.Any(pattern => ScopePathPolicy.Matches(pattern, path)) &&
               !assignment.ForbiddenPaths.Any(pattern => ScopePathPolicy.Matches(pattern, path)) &&
               IsPathAndExtensionAllowed(path, policy) &&
               !policy.ForbiddenCandidatePaths.Any(pattern => ScopePathPolicy.Matches(pattern, path)) &&
               !policy.ForbiddenAssetExtensions.Contains(
                   Path.GetExtension(path),
                   StringComparer.OrdinalIgnoreCase);
    }

    private static bool IsPathAndExtensionAllowed(string path, SkillPolicyDefinition policy)
    {
        string extension = Path.GetExtension(path);
        return policy.CandidateRules.Any(rule =>
                   ScopePathPolicy.Matches(rule.Path, path) &&
                   rule.Extensions.Contains(extension, StringComparer.OrdinalIgnoreCase)) &&
               !policy.ForbiddenAssetExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase);
    }

    private static bool PatternsOverlap(string left, string right)
    {
        string leftRoot = left.EndsWith("/**", StringComparison.Ordinal) ? left[..^3] : left;
        string rightRoot = right.EndsWith("/**", StringComparison.Ordinal) ? right[..^3] : right;
        return leftRoot == rightRoot ||
               leftRoot.StartsWith(rightRoot + "/", StringComparison.Ordinal) ||
               rightRoot.StartsWith(leftRoot + "/", StringComparison.Ordinal);
    }

    private static string? ReadMutationPath(JsonElement step, string tool)
    {
        string propertyName = tool == "EnsureMaterial" || tool == "WriteCSharpScript"
            ? "assetPath"
            : "scenePath";
        return step.TryGetProperty(propertyName, out JsonElement value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
    }

    private static IReadOnlyList<string> ReadChangedFiles(string repositoryRoot)
    {
        HashSet<string> paths = new HashSet<string>(StringComparer.Ordinal);
        AddNullSeparated(paths, RunGit(
            repositoryRoot,
            "diff",
            "--name-only",
            "--no-renames",
            "-z",
            "HEAD"));
        AddNullSeparated(paths, RunGit(repositoryRoot, "ls-files", "--others", "--exclude-standard", "-z"));
        return paths.OrderBy(path => path, StringComparer.Ordinal).ToArray();
    }

    private static void AddNullSeparated(ISet<string> paths, string output)
    {
        foreach (string path in output.Split('\0', StringSplitOptions.RemoveEmptyEntries))
        {
            paths.Add(path.Replace('\\', '/'));
        }
    }

    private static string RunGit(string repositoryRoot, params string[] arguments)
    {
        ProcessStartInfo startInfo = new ProcessStartInfo("git")
        {
            WorkingDirectory = repositoryRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        foreach (string argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using Process process = Process.Start(startInfo) ?? throw new IOException("Could not start git.");
        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();
        process.WaitForExit();
        if (process.ExitCode != 0)
        {
            throw new IOException($"git {string.Join(' ', arguments)} failed: {error.Trim()}");
        }
        return output;
    }

    private static bool IsTrackedAtHead(string repositoryRoot, string path)
    {
        try
        {
            RunGit(repositoryRoot, "cat-file", "-e", $"HEAD:{path}");
            return true;
        }
        catch (IOException)
        {
            return false;
        }
    }

    private static bool IsTrackedAndUnmodified(string repositoryRoot, string policyPath)
    {
        try
        {
            RunGit(repositoryRoot, "ls-files", "--error-unmatch", "--", policyPath);
            return string.IsNullOrWhiteSpace(RunGit(
                repositoryRoot,
                "status",
                "--porcelain=v1",
                "--",
                policyPath));
        }
        catch (IOException)
        {
            return false;
        }
    }

    internal static string ComputeSha256(string path)
    {
        using FileStream stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
    }

    private static void AddCheck(
        ICollection<ScopeGateCheck> checks,
        string id,
        bool success,
        string message,
        string expected,
        string actual)
    {
        checks.Add(new ScopeGateCheck(id, success, message, expected, actual));
    }
}

internal static class SkillScopeGateResultWriter
{
    public static void Write(
        string path,
        string runId,
        SkillPolicyDefinition policy,
        ScopeGateEvaluation evaluation,
        DateTime startedAtUtc,
        DateTime finishedAtUtc,
        string policyPath,
        string assignmentPath)
    {
        object result = new
        {
            schemaVersion = 1,
            runId,
            profile = policy.GateProfile,
            profileVersion = policy.GateProfileVersion,
            success = evaluation.Success,
            status = evaluation.Success ? "Pass" : "Fail",
            message = evaluation.Success
                ? $"Skill scope policy {policy.PolicyId} passed {evaluation.Checks.Count} checks."
                : $"Skill scope policy {policy.PolicyId} failed one or more checks.",
            checks = evaluation.Checks.Select(check => new
            {
                id = check.Id,
                success = check.Success,
                status = check.Success ? "Pass" : "Fail",
                message = check.Message,
                expected = check.Expected,
                actual = check.Actual,
            }),
            artifacts = new[]
            {
                new { kind = "SkillPolicy", path = policyPath },
                new { kind = "WorkerAssignment", path = assignmentPath },
            },
            changedFiles = evaluation.ChangedFiles,
            startedAtUtc = startedAtUtc.ToString("O"),
            finishedAtUtc = finishedAtUtc.ToString("O"),
        };
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(
            path,
            JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }),
            new UTF8Encoding(false));
    }
}
