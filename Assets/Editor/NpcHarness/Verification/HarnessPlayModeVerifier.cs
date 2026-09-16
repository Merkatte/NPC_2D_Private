using System;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class HarnessPlayModeVerifier
{
    private const string PendingKey = "NpcHarness.PlayVerification.Pending";
    private const string BatchKey = "NpcHarness.PlayVerification.Batch";
    private const string ResultPathKey = "NpcHarness.PlayVerification.ResultPath";
    private const string StartedTicksKey = "NpcHarness.PlayVerification.StartedTicks";
    private const string SuccessCountKey = "NpcHarness.PlayVerification.SuccessCount";
    private const string StopRequestedKey = "NpcHarness.PlayVerification.StopRequested";
    private const string FailureKey = "NpcHarness.PlayVerification.Failure";
    private const double TimeoutSeconds = 30d;

    static HarnessPlayModeVerifier()
    {
        if (SessionState.GetBool(PendingKey, false))
        {
            RegisterCallbacks();
        }
    }

    public static void VerifyFromCommandLine()
    {
        string resultPath = HarnessBatchRunner.GetRequiredArgument("-harnessResultPath");
        Start(resultPath, batch: true);
    }

    internal static HarnessToolResult StartInteractive()
    {
        return Start(string.Empty, batch: false);
    }

    private static HarnessToolResult Start(string resultPath, bool batch)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            HarnessToolResult alreadyPlaying = HarnessToolResult.Failure(
                "Play Mode verification cannot start while Unity is entering or running Play Mode.");
            FinishImmediately(resultPath, batch, alreadyPlaying);
            return alreadyPlaying;
        }

        HarnessToolResult validation = HarnessBeaconValidator.Validate();
        if (validation.IsFailure)
        {
            FinishImmediately(resultPath, batch, validation);
            return validation;
        }

        SessionState.SetBool(PendingKey, true);
        SessionState.SetBool(BatchKey, batch);
        SessionState.SetString(ResultPathKey, resultPath);
        SessionState.SetString(StartedTicksKey, DateTime.UtcNow.Ticks.ToString());
        SessionState.SetInt(SuccessCountKey, 0);
        SessionState.SetBool(StopRequestedKey, false);
        SessionState.SetString(FailureKey, string.Empty);
        RegisterCallbacks();
        EditorApplication.EnterPlaymode();
        return HarnessToolResult.Success("Play Mode verification started.");
    }

    private static void HandleLogMessage(string condition, string stackTrace, LogType type)
    {
        if (!SessionState.GetBool(PendingKey, false) || condition != HarnessBeaconRecipe.SuccessMessage)
        {
            return;
        }

        SessionState.SetInt(SuccessCountKey, SessionState.GetInt(SuccessCountKey, 0) + 1);
    }

    private static void HandleVerificationUpdate()
    {
        if (!SessionState.GetBool(PendingKey, false))
        {
            UnregisterCallbacks();
            return;
        }

        if (HasTimedOut())
        {
            SessionState.SetString(FailureKey, "Timed out waiting for HarnessSuccess in Play Mode.");
            RequestPlayModeExit();
            return;
        }

        if (SessionState.GetInt(SuccessCountKey, 0) > 0)
        {
            RequestPlayModeExit();
        }
    }

    private static void HandlePlayModeStateChanged(PlayModeStateChange state)
    {
        if (!SessionState.GetBool(PendingKey, false) || state != PlayModeStateChange.EnteredEditMode)
        {
            return;
        }

        int successCount = SessionState.GetInt(SuccessCountKey, 0);
        string failure = SessionState.GetString(FailureKey, string.Empty);
        if (failure.Length == 0 && successCount != 1)
        {
            failure = $"Expected exactly one HarnessSuccess log, observed {successCount}.";
        }

        bool success = failure.Length == 0;
        HarnessJobResult result = new HarnessJobResult
        {
            success = success,
            state = success ? HarnessRunState.Succeeded.ToString() : HarnessRunState.Failed.ToString(),
            message = success
                ? "Observed exactly one HarnessSuccess log in Play Mode."
                : failure,
        };
        string resultPath = SessionState.GetString(ResultPathKey, string.Empty);
        bool batch = SessionState.GetBool(BatchKey, false);
        ClearState();
        HarnessEditorState.SetLastResult(result);
        if (!string.IsNullOrWhiteSpace(resultPath))
        {
            HarnessResultWriter.Write(resultPath, result);
        }
        Debug.Log(success
            ? "Harness Play Mode verification passed."
            : $"Harness Play Mode verification failed: {failure}");

        if (batch)
        {
            EditorApplication.Exit(success ? 0 : 1);
        }
    }

    private static void RequestPlayModeExit()
    {
        if (!EditorApplication.isPlaying)
        {
            if (SessionState.GetString(FailureKey, string.Empty).Length == 0)
            {
                SessionState.SetString(FailureKey, "Play Mode ended before verification completed.");
            }

            HandlePlayModeStateChanged(PlayModeStateChange.EnteredEditMode);
            return;
        }

        if (SessionState.GetBool(StopRequestedKey, false))
        {
            return;
        }

        SessionState.SetBool(StopRequestedKey, true);
        EditorApplication.ExitPlaymode();
    }

    private static bool HasTimedOut()
    {
        string ticksText = SessionState.GetString(StartedTicksKey, string.Empty);
        if (!long.TryParse(ticksText, out long ticks))
        {
            return true;
        }

        return (DateTime.UtcNow - new DateTime(ticks, DateTimeKind.Utc)).TotalSeconds > TimeoutSeconds;
    }

    private static void FinishImmediately(string resultPath, bool batch, HarnessToolResult toolResult)
    {
        HarnessJobResult result = new HarnessJobResult
        {
            success = false,
            state = HarnessRunState.Failed.ToString(),
            message = toolResult.Message,
        };
        HarnessEditorState.SetLastResult(result);
        if (!string.IsNullOrWhiteSpace(resultPath))
        {
            HarnessResultWriter.Write(resultPath, result);
        }
        Debug.LogError(toolResult.Message);
        if (batch)
        {
            EditorApplication.Exit(1);
        }
    }

    private static void RegisterCallbacks()
    {
        Application.logMessageReceived -= HandleLogMessage;
        Application.logMessageReceived += HandleLogMessage;
        EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
        EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
        EditorApplication.update -= HandleVerificationUpdate;
        EditorApplication.update += HandleVerificationUpdate;
    }

    private static void UnregisterCallbacks()
    {
        Application.logMessageReceived -= HandleLogMessage;
        EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
        EditorApplication.update -= HandleVerificationUpdate;
    }

    private static void ClearState()
    {
        SessionState.EraseBool(PendingKey);
        SessionState.EraseBool(BatchKey);
        SessionState.EraseString(ResultPathKey);
        SessionState.EraseString(StartedTicksKey);
        SessionState.EraseInt(SuccessCountKey);
        SessionState.EraseBool(StopRequestedKey);
        SessionState.EraseString(FailureKey);
        UnregisterCallbacks();
    }
}
