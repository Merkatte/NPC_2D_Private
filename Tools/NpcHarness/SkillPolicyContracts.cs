using System.Text.Json;

namespace NpcHarness;

internal static class WorkerExecutionKinds
{
    public const string HarnessJob = "harness-job";
    public const string DirectCode = "direct-code";
    public const string RasterArt = "raster-art";

    public static bool IsSupported(string value)
    {
        return value is HarnessJob or DirectCode or RasterArt;
    }
}

internal sealed record CandidateRuleDefinition(
    string Path,
    IReadOnlyList<string> Extensions);

internal sealed record SkillPolicyDefinition(
    string PolicyId,
    int PolicyVersion,
    string ExecutionKind,
    string GateProfile,
    int GateProfileVersion,
    IReadOnlyList<CandidateRuleDefinition> CandidateRules,
    IReadOnlyList<string> ForbiddenCandidatePaths,
    IReadOnlyList<string> ForbiddenAssetExtensions,
    IReadOnlyList<string> AllowedTools,
    IReadOnlyList<string> AcceptanceManifestRoots);

internal sealed record BaselineDirtyFileDefinition(string Path, string Sha256);

internal sealed record AcceptanceGateDefinition(
    string ManifestPath,
    string Profile,
    int ProfileVersion);

internal sealed record AssignmentArtifactPathsDefinition(
    string WorkerResultPath,
    string LogPath);

internal sealed record OverwriteAuthorityDefinition(
    bool AllowOverwrite,
    IReadOnlyList<string> ApprovedValues);

internal sealed record SceneTaskSpecificationDefinition(
    IReadOnlyList<string> Hierarchy,
    IReadOnlyList<string> Components,
    IReadOnlyList<string> Transforms,
    IReadOnlyList<string> SerializedValues);

internal abstract record WorkerExecutionDefinition(string Kind);

internal sealed record HarnessJobExecutionDefinition(
    string JobId,
    string JobPath,
    string AdapterResultPath,
    IReadOnlyList<string> AllowedComponentTypes,
    OverwriteAuthorityDefinition OverwriteAuthority,
    SceneTaskSpecificationDefinition TaskSpecification)
    : WorkerExecutionDefinition(WorkerExecutionKinds.HarnessJob);

internal sealed record DirectCodeExecutionDefinition(
    IReadOnlyList<string> SourcePaths,
    IReadOnlyList<string> OwningDocumentPaths,
    bool CompileRequired)
    : WorkerExecutionDefinition(WorkerExecutionKinds.DirectCode);

internal sealed record RasterArtExecutionDefinition(
    string AssetFamily,
    string IntendedUse,
    string OutputPath,
    IReadOnlyList<string> ReferenceAssets,
    string ImportReferenceMetaPath,
    string ImportSpecPath,
    int Width,
    int Height,
    bool RequireAlpha,
    int FrameColumns,
    int FrameRows)
    : WorkerExecutionDefinition(WorkerExecutionKinds.RasterArt);

internal sealed record WorkerAssignmentDefinition(
    string AssignmentId,
    string RunId,
    string RoleSkill,
    int PolicyVersion,
    string Objective,
    string BaselineCommit,
    IReadOnlyList<BaselineDirtyFileDefinition> BaselineDirtyFiles,
    IReadOnlyList<string> RequiredContext,
    IReadOnlyList<string> WritablePaths,
    IReadOnlyList<string> ForbiddenPaths,
    IReadOnlyList<string> AllowedTools,
    IReadOnlyList<string> ForbiddenOperations,
    AssignmentArtifactPathsDefinition ArtifactPaths,
    WorkerExecutionDefinition Execution,
    AcceptanceGateDefinition AcceptanceGate,
    IReadOnlyList<string> AcceptanceConditions,
    IReadOnlyList<string> PreliminaryChecks,
    IReadOnlyList<string> ReportRequirements);

internal static class SkillPolicyContracts
{
    public static SkillPolicyDefinition ReadPolicy(string path)
    {
        using JsonDocument document = ReadDocument(path, "SkillPolicy");
        JsonElement root = document.RootElement;
        RequireObject(root, "SkillPolicy");
        RequireExactProperties(
            root,
            "SkillPolicy",
            "schemaVersion", "policyId", "policyVersion", "executionKind",
            "gateProfile", "gateProfileVersion", "candidateRules",
            "forbiddenCandidatePaths",
            "forbiddenAssetExtensions", "allowedTools", "acceptanceManifestRoots");
        RequireInteger(root, "schemaVersion", 1);

        string executionKind = RequireNonEmptyString(root, "executionKind");
        if (!WorkerExecutionKinds.IsSupported(executionKind))
        {
            throw new InvalidDataException($"SkillPolicy executionKind is unsupported: {executionKind}");
        }

        SkillPolicyDefinition policy = new SkillPolicyDefinition(
            RequireIdentifier(root, "policyId"),
            RequirePositiveInteger(root, "policyVersion"),
            executionKind,
            RequireNonEmptyString(root, "gateProfile"),
            RequirePositiveInteger(root, "gateProfileVersion"),
            ReadCandidateRules(root),
            RequireUniqueStringArray(root, "forbiddenCandidatePaths", allowEmpty: true),
            RequireUniqueStringArray(root, "forbiddenAssetExtensions", allowEmpty: true),
            RequireUniqueStringArray(root, "allowedTools", allowEmpty: false),
            RequireUniqueStringArray(root, "acceptanceManifestRoots", allowEmpty: false));

        foreach (CandidateRuleDefinition rule in policy.CandidateRules)
        {
            ScopePathPolicy.ValidatePattern(rule.Path);
            ValidateExtensions(rule.Extensions, $"candidateRules[{rule.Path}].extensions");
        }
        foreach (string pattern in policy.ForbiddenCandidatePaths)
        {
            ScopePathPolicy.ValidatePattern(pattern);
        }
        foreach (string rootPath in policy.AcceptanceManifestRoots)
        {
            ScopePathPolicy.ValidateRepositoryPath(rootPath);
        }
        ValidateExtensions(policy.ForbiddenAssetExtensions, "forbiddenAssetExtensions");
        return policy;
    }

    private static IReadOnlyList<CandidateRuleDefinition> ReadCandidateRules(JsonElement root)
    {
        JsonElement rulesObject = RequireObjectProperty(root, "candidateRules");
        if (!rulesObject.EnumerateObject().Any())
        {
            throw new InvalidDataException("candidateRules must not be empty.");
        }

        List<CandidateRuleDefinition> rules = new List<CandidateRuleDefinition>();
        HashSet<string> paths = new HashSet<string>(StringComparer.Ordinal);
        foreach (JsonProperty item in rulesObject.EnumerateObject())
        {
            string path = item.Name;
            if (!paths.Add(path))
            {
                throw new InvalidDataException($"SkillPolicy contains duplicate candidate rule path: {path}");
            }
            if (item.Value.ValueKind != JsonValueKind.Array)
            {
                throw new InvalidDataException($"Candidate rule extensions must be an array: {path}");
            }
            rules.Add(new CandidateRuleDefinition(
                path,
                RequireUniqueStringArray(item.Value, path, allowEmpty: false, valueIsArray: true)));
        }
        return rules;
    }

    public static WorkerAssignmentDefinition ReadAssignment(string path)
    {
        using JsonDocument document = ReadDocument(path, "WorkerAssignment");
        JsonElement root = document.RootElement;
        RequireObject(root, "WorkerAssignment");
        RequireExactProperties(
            root,
            "WorkerAssignment",
            "schemaVersion", "assignmentId", "runId", "roleSkill", "policyVersion",
            "objective", "baselineCommit", "baselineDirtyFiles", "requiredContext",
            "writablePaths", "forbiddenPaths", "allowedTools", "forbiddenOperations",
            "artifactPaths", "execution", "acceptanceGate", "acceptanceConditions",
            "preliminaryChecks", "reportRequirements");
        RequireInteger(root, "schemaVersion", 1);

        IReadOnlyList<BaselineDirtyFileDefinition> dirtyFiles = ReadBaselineDirtyFiles(root);

        JsonElement artifactPaths = RequireObjectProperty(root, "artifactPaths");
        RequireExactProperties(artifactPaths, "artifactPaths", "workerResultPath", "logPath");
        AssignmentArtifactPathsDefinition artifacts = new AssignmentArtifactPathsDefinition(
            RequireNonEmptyString(artifactPaths, "workerResultPath"),
            RequireNonEmptyString(artifactPaths, "logPath"));

        WorkerExecutionDefinition execution = ReadExecution(RequireObjectProperty(root, "execution"));

        JsonElement acceptance = RequireObjectProperty(root, "acceptanceGate");
        RequireExactProperties(acceptance, "acceptanceGate", "manifestPath", "profile", "profileVersion");
        AcceptanceGateDefinition acceptanceGate = new AcceptanceGateDefinition(
            RequireNonEmptyString(acceptance, "manifestPath"),
            RequireNonEmptyString(acceptance, "profile"),
            RequirePositiveInteger(acceptance, "profileVersion"));

        WorkerAssignmentDefinition assignment = new WorkerAssignmentDefinition(
            RequireIdentifier(root, "assignmentId"),
            RequireIdentifier(root, "runId"),
            RequireIdentifier(root, "roleSkill"),
            RequirePositiveInteger(root, "policyVersion"),
            RequireNonEmptyString(root, "objective"),
            RequireCommit(root, "baselineCommit"),
            dirtyFiles,
            RequireUniqueStringArray(root, "requiredContext", allowEmpty: false),
            RequireUniqueStringArray(root, "writablePaths", allowEmpty: false),
            RequireUniqueStringArray(root, "forbiddenPaths", allowEmpty: false),
            RequireUniqueStringArray(root, "allowedTools", allowEmpty: false),
            RequireUniqueStringArray(root, "forbiddenOperations", allowEmpty: false),
            artifacts,
            execution,
            acceptanceGate,
            RequireUniqueStringArray(root, "acceptanceConditions", allowEmpty: false),
            RequireUniqueStringArray(root, "preliminaryChecks", allowEmpty: false),
            RequireUniqueStringArray(root, "reportRequirements", allowEmpty: false));

        ValidateAssignmentPaths(assignment);
        return assignment;
    }

    public static void ValidateAcceptanceIdentity(
        string repositoryRoot,
        AcceptanceGateDefinition acceptanceGate)
    {
        string fullPath = ScopePathPolicy.ResolveInsideRepository(repositoryRoot, acceptanceGate.ManifestPath);
        using JsonDocument document = ReadDocument(fullPath, "acceptance manifest");
        JsonElement root = document.RootElement;
        RequireObject(root, "acceptance manifest");
        string profile = RequireNonEmptyString(root, "profile");
        int profileVersion = RequirePositiveInteger(root, "profileVersion");
        if (!string.Equals(profile, acceptanceGate.Profile, StringComparison.Ordinal) ||
            profileVersion != acceptanceGate.ProfileVersion)
        {
            throw new InvalidDataException(
                $"Acceptance manifest identity {profile} v{profileVersion} does not match assignment " +
                $"{acceptanceGate.Profile} v{acceptanceGate.ProfileVersion}.");
        }
    }

    private static IReadOnlyList<BaselineDirtyFileDefinition> ReadBaselineDirtyFiles(JsonElement root)
    {
        List<BaselineDirtyFileDefinition> dirtyFiles = new List<BaselineDirtyFileDefinition>();
        JsonElement dirtyObject = RequireObjectProperty(root, "baselineDirtyFiles");
        HashSet<string> dirtyPaths = new HashSet<string>(StringComparer.Ordinal);
        foreach (JsonProperty item in dirtyObject.EnumerateObject())
        {
            string dirtyPath = item.Name;
            ScopePathPolicy.ValidateRepositoryPath(dirtyPath);
            if (!dirtyPaths.Add(dirtyPath))
            {
                throw new InvalidDataException($"WorkerAssignment contains duplicate baseline dirty path: {dirtyPath}");
            }
            if (item.Value.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(item.Value.GetString()))
            {
                throw new InvalidDataException($"WorkerAssignment baseline hash is invalid: {dirtyPath}");
            }
            string sha256 = item.Value.GetString()!;
            if (!string.Equals(sha256, "missing", StringComparison.Ordinal) &&
                (sha256.Length != 64 || sha256.Any(character => !Uri.IsHexDigit(character))))
            {
                throw new InvalidDataException($"WorkerAssignment baseline hash is invalid: {dirtyPath}");
            }
            dirtyFiles.Add(new BaselineDirtyFileDefinition(dirtyPath, sha256.ToLowerInvariant()));
        }
        return dirtyFiles;
    }

    private static WorkerExecutionDefinition ReadExecution(JsonElement execution)
    {
        string kind = RequireNonEmptyString(execution, "kind");
        return kind switch
        {
            WorkerExecutionKinds.HarnessJob => ReadHarnessJobExecution(execution),
            WorkerExecutionKinds.DirectCode => ReadDirectCodeExecution(execution),
            WorkerExecutionKinds.RasterArt => ReadRasterArtExecution(execution),
            _ => throw new InvalidDataException($"WorkerAssignment execution kind is unsupported: {kind}"),
        };
    }

    private static HarnessJobExecutionDefinition ReadHarnessJobExecution(JsonElement execution)
    {
        RequireExactProperties(
            execution,
            "harness-job execution",
            "kind", "jobId", "jobPath", "adapterResultPath", "allowedComponentTypes",
            "overwriteAuthority", "taskSpecification");

        JsonElement overwriteAuthority = RequireObjectProperty(execution, "overwriteAuthority");
        RequireExactProperties(overwriteAuthority, "overwriteAuthority", "allowOverwrite", "approvedValues");
        bool allowOverwrite = RequireBoolean(overwriteAuthority, "allowOverwrite");
        IReadOnlyList<string> approvedValues = RequireUniqueStringArray(
            overwriteAuthority,
            "approvedValues",
            allowEmpty: true);
        if (allowOverwrite != (approvedValues.Count > 0))
        {
            throw new InvalidDataException(
                "overwriteAuthority requires approvedValues exactly when allowOverwrite is true.");
        }

        JsonElement taskSpecification = RequireObjectProperty(execution, "taskSpecification");
        RequireExactProperties(
            taskSpecification,
            "taskSpecification",
            "hierarchy", "components", "transforms", "serializedValues");

        return new HarnessJobExecutionDefinition(
            RequireIdentifier(execution, "jobId"),
            RequireNonEmptyString(execution, "jobPath"),
            RequireNonEmptyString(execution, "adapterResultPath"),
            RequireUniqueStringArray(execution, "allowedComponentTypes", allowEmpty: false),
            new OverwriteAuthorityDefinition(allowOverwrite, approvedValues),
            new SceneTaskSpecificationDefinition(
                RequireUniqueStringArray(taskSpecification, "hierarchy", allowEmpty: false),
                RequireUniqueStringArray(taskSpecification, "components", allowEmpty: true),
                RequireUniqueStringArray(taskSpecification, "transforms", allowEmpty: true),
                RequireUniqueStringArray(taskSpecification, "serializedValues", allowEmpty: true)));
    }

    private static DirectCodeExecutionDefinition ReadDirectCodeExecution(JsonElement execution)
    {
        RequireExactProperties(
            execution,
            "direct-code execution",
            "kind", "sourcePaths", "owningDocumentPaths", "compileRequired");
        return new DirectCodeExecutionDefinition(
            RequireUniqueStringArray(execution, "sourcePaths", allowEmpty: false),
            RequireUniqueStringArray(execution, "owningDocumentPaths", allowEmpty: true),
            RequireBoolean(execution, "compileRequired"));
    }

    private static RasterArtExecutionDefinition ReadRasterArtExecution(JsonElement execution)
    {
        RequireExactProperties(
            execution,
            "raster-art execution",
            "kind", "assetFamily", "intendedUse", "outputPath", "referenceAssets",
            "importReferenceMetaPath", "importSpecPath", "width", "height", "requireAlpha",
            "frameColumns", "frameRows");
        return new RasterArtExecutionDefinition(
            RequireIdentifier(execution, "assetFamily"),
            RequireNonEmptyString(execution, "intendedUse"),
            RequireNonEmptyString(execution, "outputPath"),
            RequireUniqueStringArray(execution, "referenceAssets", allowEmpty: false),
            RequireNonEmptyString(execution, "importReferenceMetaPath"),
            RequireNonEmptyString(execution, "importSpecPath"),
            RequirePositiveInteger(execution, "width"),
            RequirePositiveInteger(execution, "height"),
            RequireBoolean(execution, "requireAlpha"),
            RequirePositiveInteger(execution, "frameColumns"),
            RequirePositiveInteger(execution, "frameRows"));
    }

    private static void ValidateAssignmentPaths(WorkerAssignmentDefinition assignment)
    {
        foreach (string writablePath in assignment.WritablePaths)
        {
            ScopePathPolicy.ValidatePattern(writablePath);
        }
        foreach (string forbiddenPath in assignment.ForbiddenPaths)
        {
            ScopePathPolicy.ValidatePattern(forbiddenPath);
        }
        foreach (string contextPath in assignment.RequiredContext)
        {
            ScopePathPolicy.ValidateRepositoryPath(contextPath);
        }
        ScopePathPolicy.ValidateRepositoryPath(assignment.ArtifactPaths.WorkerResultPath);
        ScopePathPolicy.ValidateRepositoryPath(assignment.ArtifactPaths.LogPath);
        ScopePathPolicy.ValidateRepositoryPath(assignment.AcceptanceGate.ManifestPath);

        switch (assignment.Execution)
        {
            case HarnessJobExecutionDefinition harness:
                ScopePathPolicy.ValidateRepositoryPath(harness.JobPath);
                ScopePathPolicy.ValidateRepositoryPath(harness.AdapterResultPath);
                break;
            case DirectCodeExecutionDefinition code:
                foreach (string path in code.SourcePaths.Concat(code.OwningDocumentPaths))
                {
                    ScopePathPolicy.ValidateRepositoryPath(path);
                }
                break;
            case RasterArtExecutionDefinition raster:
                ScopePathPolicy.ValidateRepositoryPath(raster.OutputPath);
                foreach (string path in raster.ReferenceAssets)
                {
                    ScopePathPolicy.ValidateRepositoryPath(path);
                }
                ScopePathPolicy.ValidateRepositoryPath(raster.ImportReferenceMetaPath);
                ScopePathPolicy.ValidateRepositoryPath(raster.ImportSpecPath);
                break;
        }
    }

    private static JsonDocument ReadDocument(string path, string name)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"{name} does not exist.", path);
        }
        return JsonDocument.Parse(File.ReadAllText(path));
    }

    private static void ValidateExtensions(IEnumerable<string> extensions, string name)
    {
        foreach (string extension in extensions)
        {
            if (extension.Length < 2 || extension[0] != '.' || extension.Contains('/') || extension.Contains('\\'))
            {
                throw new InvalidDataException($"{name} contains an invalid extension: {extension}");
            }
        }
    }

    private static void RequireExactProperties(JsonElement value, string name, params string[] expectedNames)
    {
        HashSet<string> expected = new HashSet<string>(expectedNames, StringComparer.Ordinal);
        HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (JsonProperty property in value.EnumerateObject())
        {
            if (!seen.Add(property.Name))
            {
                throw new InvalidDataException($"{name} contains duplicate property: {property.Name}");
            }
            if (!expected.Contains(property.Name))
            {
                throw new InvalidDataException($"{name} contains unsupported property: {property.Name}");
            }
        }
        foreach (string expectedName in expected)
        {
            if (!seen.Contains(expectedName))
            {
                throw new InvalidDataException($"{name} is missing required property: {expectedName}");
            }
        }
    }

    private static void RequireObject(JsonElement value, string name)
    {
        if (value.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidDataException($"{name} must be an object.");
        }
    }

    private static JsonElement RequireObjectProperty(JsonElement parent, string name)
    {
        if (!parent.TryGetProperty(name, out JsonElement value) || value.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidDataException($"Missing or invalid object property: {name}");
        }
        return value;
    }

    private static JsonElement RequireArray(JsonElement parent, string name)
    {
        if (!parent.TryGetProperty(name, out JsonElement value) || value.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidDataException($"Missing or invalid array property: {name}");
        }
        return value;
    }

    private static IReadOnlyList<string> RequireUniqueStringArray(
        JsonElement parent,
        string name,
        bool allowEmpty)
    {
        JsonElement array = RequireArray(parent, name);
        return RequireUniqueStringArray(array, name, allowEmpty, valueIsArray: true);
    }

    private static IReadOnlyList<string> RequireUniqueStringArray(
        JsonElement array,
        string name,
        bool allowEmpty,
        bool valueIsArray)
    {
        if (!valueIsArray || array.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidDataException($"{name} must be an array.");
        }
        if (!allowEmpty && array.GetArrayLength() == 0)
        {
            throw new InvalidDataException($"{name} must not be empty.");
        }
        List<string> values = new List<string>();
        HashSet<string> unique = new HashSet<string>(StringComparer.Ordinal);
        foreach (JsonElement item in array.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(item.GetString()))
            {
                throw new InvalidDataException($"{name} must contain non-empty strings.");
            }
            string value = item.GetString()!;
            if (!unique.Add(value))
            {
                throw new InvalidDataException($"{name} contains duplicate value: {value}");
            }
            values.Add(value);
        }
        return values;
    }

    private static string RequireNonEmptyString(JsonElement parent, string name)
    {
        if (!parent.TryGetProperty(name, out JsonElement value) ||
            value.ValueKind != JsonValueKind.String ||
            string.IsNullOrWhiteSpace(value.GetString()))
        {
            throw new InvalidDataException($"Missing or invalid string property: {name}");
        }
        return value.GetString()!;
    }

    private static string RequireIdentifier(JsonElement parent, string name)
    {
        string value = RequireNonEmptyString(parent, name);
        if (value.Any(character => !char.IsAsciiLetterOrDigit(character) && character != '-' && character != '_'))
        {
            throw new InvalidDataException($"{name} contains unsupported characters.");
        }
        return value;
    }

    private static bool RequireBoolean(JsonElement parent, string name)
    {
        if (!parent.TryGetProperty(name, out JsonElement value) ||
            (value.ValueKind != JsonValueKind.True && value.ValueKind != JsonValueKind.False))
        {
            throw new InvalidDataException($"{name} must be a boolean.");
        }
        return value.GetBoolean();
    }

    private static string RequireCommit(JsonElement parent, string name)
    {
        string value = RequireNonEmptyString(parent, name);
        if (value.Length != 40 || value.Any(character => !Uri.IsHexDigit(character)))
        {
            throw new InvalidDataException($"{name} must be a full 40-character Git commit hash.");
        }
        return value.ToLowerInvariant();
    }

    private static int RequirePositiveInteger(JsonElement parent, string name)
    {
        if (!parent.TryGetProperty(name, out JsonElement value) ||
            !value.TryGetInt32(out int result) ||
            result <= 0)
        {
            throw new InvalidDataException($"{name} must be a positive integer.");
        }
        return result;
    }

    private static void RequireInteger(JsonElement parent, string name, int expected)
    {
        if (!parent.TryGetProperty(name, out JsonElement value) ||
            !value.TryGetInt32(out int result) ||
            result != expected)
        {
            throw new InvalidDataException($"{name} must be {expected}.");
        }
    }
}
