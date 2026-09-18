using System;
using System.IO;
using UnityEditor;
using UnityEngine;

[Serializable]
internal sealed class HarnessInteractiveGateRequest
{
    public string runId = string.Empty;
    public string profile = string.Empty;
    public string resultPath = string.Empty;
}

internal enum HarnessInteractiveGateKind
{
    BeaconStructure,
    SquareCharacterStructure,
    FarmerSceneStructure,
}

internal static class HarnessInteractiveGateRequestPolicy
{
    public static bool TryParseAndValidate(
        string json,
        string projectRoot,
        out HarnessInteractiveGateRequest request,
        out string error)
    {
        request = null;
        error = string.Empty;

        try
        {
            request = JsonUtility.FromJson<HarnessInteractiveGateRequest>(json);
        }
        catch (ArgumentException exception)
        {
            error = $"Interactive gate request is malformed: {exception.Message}";
            return false;
        }

        if (request == null)
        {
            error = "Interactive gate request is empty or malformed.";
            return false;
        }

        if (!IsSafeRunId(request.runId))
        {
            error = "Interactive gate request runId may contain only ASCII letters, digits, '-' and '_'.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(request.profile))
        {
            error = "Interactive gate request profile is required.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(request.resultPath))
        {
            error = "Interactive gate request resultPath is required.";
            return false;
        }

        string fullProjectRoot = Path.GetFullPath(projectRoot);
        string allowedRoot = Path.GetFullPath(Path.Combine(fullProjectRoot, ".harness-runs"));
        string fullResultPath;
        try
        {
            fullResultPath = Path.GetFullPath(request.resultPath, fullProjectRoot);
        }
        catch (Exception exception) when (
            exception is ArgumentException or NotSupportedException or PathTooLongException)
        {
            error = $"Interactive gate request resultPath is invalid: {exception.Message}";
            return false;
        }

        string allowedPrefix = allowedRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) +
                               Path.DirectorySeparatorChar;
        StringComparison comparison = Application.platform == RuntimePlatform.WindowsEditor
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;
        if (!fullResultPath.StartsWith(allowedPrefix, comparison))
        {
            error = "Interactive gate request resultPath must be inside the project .harness-runs directory.";
            return false;
        }

        request.resultPath = fullResultPath;
        return true;
    }

    public static bool TryResolveProfile(
        string profile,
        out HarnessInteractiveGateKind kind,
        out int profileVersion)
    {
        if (profile == HarnessBeaconGateRunner.Profile)
        {
            kind = HarnessInteractiveGateKind.BeaconStructure;
            profileVersion = HarnessBeaconGateRunner.ProfileVersion;
            return true;
        }

        if (profile == SquareCharacterGateRunner.Profile)
        {
            kind = HarnessInteractiveGateKind.SquareCharacterStructure;
            profileVersion = SquareCharacterGateRunner.ProfileVersion;
            return true;
        }

        if (profile == FarmerSceneGateRunner.Profile)
        {
            kind = HarnessInteractiveGateKind.FarmerSceneStructure;
            profileVersion = FarmerSceneGateRunner.ProfileVersion;
            return true;
        }

        kind = default;
        profileVersion = 0;
        return false;
    }

    private static bool IsSafeRunId(string runId)
    {
        if (string.IsNullOrWhiteSpace(runId))
        {
            return false;
        }

        foreach (char character in runId)
        {
            bool isAsciiLetter = character is >= 'A' and <= 'Z' or >= 'a' and <= 'z';
            bool isAsciiDigit = character is >= '0' and <= '9';
            if (!isAsciiLetter && !isAsciiDigit && character != '-' && character != '_')
            {
                return false;
            }
        }

        return true;
    }
}

[InitializeOnLoad]
internal static class HarnessInteractiveGateBridge
{
    private const double PollIntervalSeconds = 1d;
    private const string PendingFileName = "PendingGateRequest.json";
    private const string ClaimedFileName = "ClaimedGateRequest.json";

    private static readonly string ProjectRoot = Directory.GetParent(Application.dataPath).FullName;
    private static readonly string RequestDirectory = Path.Combine(ProjectRoot, "Library", "NpcHarness");
    private static readonly string PendingPath = Path.Combine(RequestDirectory, PendingFileName);
    private static readonly string ClaimedPath = Path.Combine(RequestDirectory, ClaimedFileName);
    private static double _nextPollTime;
    private static bool _isProcessing;

    static HarnessInteractiveGateBridge()
    {
        EditorApplication.update += Poll;
        EditorApplication.delayCall += PollImmediately;
    }

    private static void PollImmediately()
    {
        _nextPollTime = 0d;
        Poll();
    }

    private static void Poll()
    {
        if (_isProcessing || EditorApplication.timeSinceStartup < _nextPollTime)
        {
            return;
        }

        _nextPollTime = EditorApplication.timeSinceStartup + PollIntervalSeconds;
        if (!File.Exists(PendingPath) || File.Exists(ClaimedPath))
        {
            return;
        }

        _isProcessing = true;
        try
        {
            Directory.CreateDirectory(RequestDirectory);
            File.Move(PendingPath, ClaimedPath);
            ProcessClaimedRequest();
        }
        catch (IOException exception)
        {
            Debug.LogError($"NPC Harness interactive gate request could not be claimed: {exception.Message}");
        }
        finally
        {
            if (File.Exists(ClaimedPath))
            {
                File.Delete(ClaimedPath);
            }

            _isProcessing = false;
        }
    }

    private static void ProcessClaimedRequest()
    {
        string json;
        try
        {
            json = File.ReadAllText(ClaimedPath);
        }
        catch (Exception exception)
        {
            Debug.LogError($"NPC Harness interactive gate request could not be read: {exception.Message}");
            return;
        }

        if (!HarnessInteractiveGateRequestPolicy.TryParseAndValidate(
                json,
                ProjectRoot,
                out HarnessInteractiveGateRequest request,
                out string error))
        {
            Debug.LogError($"NPC Harness interactive gate request rejected: {error}");
            return;
        }

        HarnessGateResult result;
        if (!HarnessInteractiveGateRequestPolicy.TryResolveProfile(
                request.profile,
                out HarnessInteractiveGateKind kind,
                out int profileVersion))
        {
            result = HarnessGateResultBuilder.CreateInfrastructureError(
                request.runId,
                request.profile,
                1,
                $"Interactive gate profile is not supported: {request.profile}");
        }
        else
        {
            try
            {
                result = Evaluate(request.runId, kind);
            }
            catch (Exception exception)
            {
                result = HarnessGateResultBuilder.CreateInfrastructureError(
                    request.runId,
                    request.profile,
                    profileVersion,
                    exception.Message);
            }
        }

        HarnessResultWriter.Write(request.resultPath, result);
        Debug.Log($"NPC Harness interactive gate result: {result.status} - {result.message}");
    }

    private static HarnessGateResult Evaluate(string runId, HarnessInteractiveGateKind kind)
    {
        switch (kind)
        {
            case HarnessInteractiveGateKind.BeaconStructure:
                return HarnessBeaconValidator.EvaluateStructure(runId);
            case HarnessInteractiveGateKind.SquareCharacterStructure:
                return SquareCharacterValidator.EvaluateStructure(runId);
            case HarnessInteractiveGateKind.FarmerSceneStructure:
                return FarmerSceneValidator.EvaluateStructure(runId);
            default:
                throw new InvalidOperationException($"Unsupported interactive gate kind: {kind}");
        }
    }
}
