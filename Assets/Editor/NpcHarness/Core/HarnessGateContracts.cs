using System;

[Serializable]
internal sealed class HarnessGateResult
{
    public int schemaVersion = 1;
    public string runId = string.Empty;
    public string profile = string.Empty;
    public int profileVersion = 1;
    public bool success;
    public string status = HarnessGateStatus.InfrastructureError.ToString();
    public string message = string.Empty;
    public HarnessGateCheckResult[] checks = Array.Empty<HarnessGateCheckResult>();
    public HarnessGateArtifact[] artifacts = Array.Empty<HarnessGateArtifact>();
    public string[] changedFiles = Array.Empty<string>();
    public string startedAtUtc = string.Empty;
    public string finishedAtUtc = string.Empty;
}

[Serializable]
internal sealed class HarnessGateCheckResult
{
    public string id = string.Empty;
    public bool success;
    public string status = HarnessGateCheckStatus.Fail.ToString();
    public string message = string.Empty;
    public string expected = string.Empty;
    public string actual = string.Empty;
}

[Serializable]
internal sealed class HarnessGateArtifact
{
    public string kind = string.Empty;
    public string path = string.Empty;
}

internal enum HarnessGateStatus
{
    Pass,
    Fail,
    InfrastructureError,
}

internal enum HarnessGateCheckStatus
{
    Pass,
    Fail,
}
