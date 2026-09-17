using System;
using System.Collections.Generic;

internal sealed class HarnessGateResultBuilder
{
    private readonly string _runId;
    private readonly string _profile;
    private readonly int _profileVersion;
    private readonly string _startedAtUtc;
    private readonly HashSet<string> _checkIds = new HashSet<string>(StringComparer.Ordinal);
    private readonly List<HarnessGateCheckResult> _checks = new List<HarnessGateCheckResult>();
    private readonly List<HarnessGateArtifact> _artifacts = new List<HarnessGateArtifact>();
    private string[] _changedFiles = Array.Empty<string>();
    private string _infrastructureError = string.Empty;

    public HarnessGateResultBuilder(string runId, string profile, int profileVersion)
    {
        if (string.IsNullOrWhiteSpace(runId))
        {
            throw new ArgumentException("A gate run ID is required.", nameof(runId));
        }

        if (string.IsNullOrWhiteSpace(profile))
        {
            throw new ArgumentException("A gate profile is required.", nameof(profile));
        }

        if (profileVersion <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(profileVersion), "A gate profile version must be positive.");
        }

        _runId = runId;
        _profile = profile;
        _profileVersion = profileVersion;
        _startedAtUtc = DateTime.UtcNow.ToString("O");
    }

    public void AddPass(string id, string message, string expected = "", string actual = "")
    {
        AddCheck(id, true, message, expected, actual);
    }

    public void AddFailure(string id, string message, string expected = "", string actual = "")
    {
        AddCheck(id, false, message, expected, actual);
    }

    public void AddArtifact(string kind, string path)
    {
        if (string.IsNullOrWhiteSpace(kind) || string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Gate artifacts require a kind and path.");
        }

        _artifacts.Add(new HarnessGateArtifact
        {
            kind = kind,
            path = path,
        });
    }

    public void SetChangedFiles(IEnumerable<string> changedFiles)
    {
        if (changedFiles == null)
        {
            _changedFiles = Array.Empty<string>();
            return;
        }

        List<string> paths = new List<string>();
        foreach (string changedFile in changedFiles)
        {
            if (!string.IsNullOrWhiteSpace(changedFile))
            {
                paths.Add(changedFile.Replace('\\', '/'));
            }
        }

        _changedFiles = paths.ToArray();
    }

    public void SetInfrastructureError(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            throw new ArgumentException("An infrastructure error message is required.", nameof(message));
        }

        _infrastructureError = message;
    }

    public HarnessGateResult Build()
    {
        if (_checks.Count == 0 && _infrastructureError.Length == 0)
        {
            _infrastructureError = $"Gate profile {_profile} produced no checks.";
        }

        HarnessGateStatus status = ResolveStatus();
        int failedCheckCount = 0;
        foreach (HarnessGateCheckResult check in _checks)
        {
            if (!check.success)
            {
                failedCheckCount++;
            }
        }

        return new HarnessGateResult
        {
            runId = _runId,
            profile = _profile,
            profileVersion = _profileVersion,
            success = status == HarnessGateStatus.Pass,
            status = status.ToString(),
            message = CreateSummary(status, failedCheckCount),
            checks = _checks.ToArray(),
            artifacts = _artifacts.ToArray(),
            changedFiles = _changedFiles,
            startedAtUtc = _startedAtUtc,
            finishedAtUtc = DateTime.UtcNow.ToString("O"),
        };
    }

    public static HarnessGateResult CreateInfrastructureError(
        string runId,
        string profile,
        int profileVersion,
        string message)
    {
        HarnessGateResultBuilder builder = new HarnessGateResultBuilder(runId, profile, profileVersion);
        builder.SetInfrastructureError(message);
        return builder.Build();
    }

    private void AddCheck(string id, bool success, string message, string expected, string actual)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("A gate check ID is required.", nameof(id));
        }

        if (!_checkIds.Add(id))
        {
            throw new InvalidOperationException($"Duplicate gate check ID: {id}");
        }

        _checks.Add(new HarnessGateCheckResult
        {
            id = id,
            success = success,
            status = success ? HarnessGateCheckStatus.Pass.ToString() : HarnessGateCheckStatus.Fail.ToString(),
            message = message ?? string.Empty,
            expected = expected ?? string.Empty,
            actual = actual ?? string.Empty,
        });
    }

    private HarnessGateStatus ResolveStatus()
    {
        if (_infrastructureError.Length > 0)
        {
            return HarnessGateStatus.InfrastructureError;
        }

        foreach (HarnessGateCheckResult check in _checks)
        {
            if (!check.success)
            {
                return HarnessGateStatus.Fail;
            }
        }

        return HarnessGateStatus.Pass;
    }

    private string CreateSummary(HarnessGateStatus status, int failedCheckCount)
    {
        switch (status)
        {
            case HarnessGateStatus.Pass:
                return $"Gate profile {_profile} passed {_checks.Count} checks.";
            case HarnessGateStatus.Fail:
                return $"Gate profile {_profile} failed {failedCheckCount} of {_checks.Count} checks.";
            default:
                return _infrastructureError;
        }
    }
}
