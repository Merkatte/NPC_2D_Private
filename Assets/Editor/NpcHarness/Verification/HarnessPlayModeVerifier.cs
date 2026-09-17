using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class HarnessPlayModeVerifier
{
    private const string PendingKey = "NpcHarness.PlayVerification.Pending";
    private const string BatchKey = "NpcHarness.PlayVerification.Batch";
    private const string ResultPathKey = "NpcHarness.PlayVerification.ResultPath";
    private const string RunIdKey = "NpcHarness.PlayVerification.RunId";
    private const string StructureResultKey = "NpcHarness.PlayVerification.StructureResult";
    private const string StartedTicksKey = "NpcHarness.PlayVerification.StartedTicks";
    private const string FirstSuccessTicksKey = "NpcHarness.PlayVerification.FirstSuccessTicks";
    private const string SuccessCountKey = "NpcHarness.PlayVerification.SuccessCount";
    private const string ErrorCountKey = "NpcHarness.PlayVerification.ErrorCount";
    private const string FirstErrorKey = "NpcHarness.PlayVerification.FirstError";
    private const string TimedOutKey = "NpcHarness.PlayVerification.TimedOut";
    private const string ExpectedExitKey = "NpcHarness.PlayVerification.ExpectedExit";
    private const string InfrastructureErrorKey = "NpcHarness.PlayVerification.InfrastructureError";
    private const string PreviousPlayModeStartSceneKey = "NpcHarness.PlayVerification.PreviousStartScene";
    private const string ChangedPlayModeStartSceneKey = "NpcHarness.PlayVerification.ChangedStartScene";
    private const double TimeoutSeconds = 30d;
    private const double ObservationSecondsAfterSuccess = 1d;

    static HarnessPlayModeVerifier()
    {
        if (SessionState.GetBool(PendingKey, false))
        {
            RegisterCallbacks();
        }
    }

    public static void VerifyFromCommandLine()
    {
        string resultPath = string.Empty;
        string runId = HarnessBatchRunner.GetOptionalArgument(
            "-harnessRunId",
            "beacon-play-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff"));

        try
        {
            resultPath = HarnessBatchRunner.GetRequiredArgument("-harnessResultPath");
            Start(runId, resultPath, batch: true);
        }
        catch (Exception exception)
        {
            HarnessGateResult result = HarnessBeaconPlayModeGate.CreateInfrastructureError(runId, exception.Message);
            HarnessResultWriter.Write(resultPath, result);
            Debug.LogException(exception);
            EditorApplication.Exit(1);
        }
    }

    internal static HarnessToolResult StartInteractive()
    {
        string runId = "interactive-beacon-play-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff");
        return Start(runId, string.Empty, batch: false);
    }

    private static HarnessToolResult Start(string runId, string resultPath, bool batch)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            HarnessGateResult result = HarnessBeaconPlayModeGate.CreateInfrastructureError(
                runId,
                "Play Mode verification cannot start while Unity is entering or running Play Mode.");
            FinishImmediately(resultPath, batch, result);
            return HarnessToolResult.Failure(result.message);
        }

        HarnessGateResult structureResult = HarnessBeaconValidator.EvaluateStructure(runId);
        if (!structureResult.success)
        {
            HarnessGateResult result = HarnessBeaconPlayModeGate.Evaluate(
                runId,
                structureResult,
                default);
            FinishImmediately(resultPath, batch, result);
            return HarnessToolResult.ValidationFailure(result.message);
        }

        SessionState.SetBool(PendingKey, true);
        SessionState.SetBool(BatchKey, batch);
        SessionState.SetString(ResultPathKey, resultPath);
        SessionState.SetString(RunIdKey, runId);
        SessionState.SetString(StructureResultKey, JsonUtility.ToJson(structureResult));
        SessionState.SetString(StartedTicksKey, DateTime.UtcNow.Ticks.ToString());
        SessionState.EraseString(FirstSuccessTicksKey);
        SessionState.SetInt(SuccessCountKey, 0);
        SessionState.SetInt(ErrorCountKey, 0);
        SessionState.SetString(FirstErrorKey, string.Empty);
        SessionState.SetBool(TimedOutKey, false);
        SessionState.SetBool(ExpectedExitKey, false);
        SessionState.SetString(InfrastructureErrorKey, string.Empty);
        if (!TryConfigurePlayModeStartScene(out string configurationError))
        {
            HarnessGateResult result = HarnessBeaconPlayModeGate.CreateInfrastructureError(
                runId,
                configurationError);
            ClearState();
            FinishImmediately(resultPath, batch, result);
            return HarnessToolResult.Failure(result.message);
        }

        RegisterCallbacks();
        try
        {
            EditorApplication.EnterPlaymode();
        }
        catch (Exception exception)
        {
            HarnessGateResult result = HarnessBeaconPlayModeGate.CreateInfrastructureError(
                runId,
                "Unity could not enter Play Mode: " + exception.Message);
            ClearState();
            FinishImmediately(resultPath, batch, result);
            return HarnessToolResult.Failure(result.message);
        }

        return HarnessToolResult.Success("Play Mode gate started.");
    }

    private static void HandleLogMessage(string condition, string stackTrace, LogType type)
    {
        if (!SessionState.GetBool(PendingKey, false))
        {
            return;
        }

        if (condition == HarnessBeaconRecipe.SuccessMessage)
        {
            int successCount = SessionState.GetInt(SuccessCountKey, 0) + 1;
            SessionState.SetInt(SuccessCountKey, successCount);
            if (successCount == 1)
            {
                SessionState.SetString(FirstSuccessTicksKey, DateTime.UtcNow.Ticks.ToString());
            }
        }

        if (type != LogType.Error && type != LogType.Assert && type != LogType.Exception)
        {
            return;
        }

        int errorCount = SessionState.GetInt(ErrorCountKey, 0) + 1;
        SessionState.SetInt(ErrorCountKey, errorCount);
        if (errorCount == 1)
        {
            SessionState.SetString(FirstErrorKey, condition ?? string.Empty);
        }
    }

    private static void HandleVerificationUpdate()
    {
        if (!SessionState.GetBool(PendingKey, false))
        {
            UnregisterCallbacks();
            return;
        }

        if (!TryHasElapsed(StartedTicksKey, TimeoutSeconds, out bool timedOut))
        {
            FailInfrastructure("Play Mode gate start time is missing or invalid.");
            return;
        }

        if (timedOut)
        {
            SessionState.SetBool(TimedOutKey, true);
            RequestExpectedPlayModeExit();
            return;
        }

        int successCount = SessionState.GetInt(SuccessCountKey, 0);
        int errorCount = SessionState.GetInt(ErrorCountKey, 0);
        if (successCount > 1 || errorCount > 0)
        {
            RequestExpectedPlayModeExit();
            return;
        }

        if (successCount == 1)
        {
            if (!TryHasElapsed(FirstSuccessTicksKey, ObservationSecondsAfterSuccess, out bool observedLongEnough))
            {
                FailInfrastructure("First success log time is missing or invalid.");
                return;
            }

            if (observedLongEnough)
            {
                RequestExpectedPlayModeExit();
            }
        }
    }

    private static void HandlePlayModeStateChanged(PlayModeStateChange state)
    {
        if (!SessionState.GetBool(PendingKey, false) || state != PlayModeStateChange.EnteredEditMode)
        {
            return;
        }

        string runId = SessionState.GetString(RunIdKey, string.Empty);
        string resultPath = SessionState.GetString(ResultPathKey, string.Empty);
        bool batch = SessionState.GetBool(BatchKey, false);
        HarnessGateResult result;
        try
        {
            result = BuildCompletedResult(runId);
        }
        catch (Exception exception)
        {
            string effectiveRunId = string.IsNullOrWhiteSpace(runId) ? "missing-play-mode-run" : runId;
            result = HarnessBeaconPlayModeGate.CreateInfrastructureError(
                effectiveRunId,
                "Play Mode gate could not build its result: " + exception.Message);
        }

        ClearState();
        StoreEditorResult(result);
        if (!string.IsNullOrWhiteSpace(resultPath))
        {
            HarnessResultWriter.Write(resultPath, result);
        }

        Debug.Log(result.success
            ? "Harness Play Mode gate passed."
            : $"Harness Play Mode gate failed: {result.message}");

        if (batch)
        {
            EditorApplication.Exit(result.success ? 0 : 1);
        }
    }

    private static HarnessGateResult BuildCompletedResult(string runId)
    {
        string infrastructureError = SessionState.GetString(InfrastructureErrorKey, string.Empty);
        if (!string.IsNullOrWhiteSpace(infrastructureError))
        {
            return HarnessBeaconPlayModeGate.CreateInfrastructureError(runId, infrastructureError);
        }

        string structureJson = SessionState.GetString(StructureResultKey, string.Empty);
        HarnessGateResult structureResult = string.IsNullOrWhiteSpace(structureJson)
            ? null
            : JsonUtility.FromJson<HarnessGateResult>(structureJson);
        HarnessPlayModeObservation observation = new HarnessPlayModeObservation(
            SessionState.GetInt(SuccessCountKey, 0),
            SessionState.GetInt(ErrorCountKey, 0),
            SessionState.GetString(FirstErrorKey, string.Empty),
            SessionState.GetBool(TimedOutKey, false),
            !SessionState.GetBool(ExpectedExitKey, false));
        return HarnessBeaconPlayModeGate.Evaluate(runId, structureResult, observation);
    }

    private static void RequestExpectedPlayModeExit()
    {
        if (SessionState.GetBool(ExpectedExitKey, false))
        {
            return;
        }

        SessionState.SetBool(ExpectedExitKey, true);
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorApplication.ExitPlaymode();
            return;
        }

        HandlePlayModeStateChanged(PlayModeStateChange.EnteredEditMode);
    }

    private static void FailInfrastructure(string message)
    {
        SessionState.SetString(InfrastructureErrorKey, message);
        RequestExpectedPlayModeExit();
    }

    private static bool TryHasElapsed(string ticksKey, double seconds, out bool hasElapsed)
    {
        hasElapsed = false;
        string ticksText = SessionState.GetString(ticksKey, string.Empty);
        if (!long.TryParse(ticksText, out long ticks))
        {
            return false;
        }

        hasElapsed = (DateTime.UtcNow - new DateTime(ticks, DateTimeKind.Utc)).TotalSeconds >= seconds;
        return true;
    }

    private static void FinishImmediately(string resultPath, bool batch, HarnessGateResult result)
    {
        StoreEditorResult(result);
        if (!string.IsNullOrWhiteSpace(resultPath))
        {
            HarnessResultWriter.Write(resultPath, result);
        }

        Debug.LogError(result.message);
        if (batch)
        {
            EditorApplication.Exit(1);
        }
    }

    private static void StoreEditorResult(HarnessGateResult result)
    {
        HarnessEditorState.SetLastResult(new HarnessJobResult
        {
            success = result.success,
            state = result.status,
            message = result.message,
        });
    }

    private static bool TryConfigurePlayModeStartScene(out string error)
    {
        SceneAsset targetScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(HarnessBeaconRecipe.ScenePath);
        if (!targetScene)
        {
            error = $"Play Mode start scene does not exist: {HarnessBeaconRecipe.ScenePath}";
            return false;
        }

        SceneAsset previousScene = EditorSceneManager.playModeStartScene;
        SessionState.SetString(
            PreviousPlayModeStartSceneKey,
            previousScene ? AssetDatabase.GetAssetPath(previousScene) : string.Empty);
        SessionState.SetBool(ChangedPlayModeStartSceneKey, true);
        EditorSceneManager.playModeStartScene = targetScene;
        error = string.Empty;
        return true;
    }

    private static void RestorePlayModeStartScene()
    {
        if (!SessionState.GetBool(ChangedPlayModeStartSceneKey, false))
        {
            return;
        }

        string previousPath = SessionState.GetString(PreviousPlayModeStartSceneKey, string.Empty);
        EditorSceneManager.playModeStartScene = string.IsNullOrWhiteSpace(previousPath)
            ? null
            : AssetDatabase.LoadAssetAtPath<SceneAsset>(previousPath);
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
        RestorePlayModeStartScene();
        SessionState.EraseBool(PendingKey);
        SessionState.EraseBool(BatchKey);
        SessionState.EraseString(ResultPathKey);
        SessionState.EraseString(RunIdKey);
        SessionState.EraseString(StructureResultKey);
        SessionState.EraseString(StartedTicksKey);
        SessionState.EraseString(FirstSuccessTicksKey);
        SessionState.EraseInt(SuccessCountKey);
        SessionState.EraseInt(ErrorCountKey);
        SessionState.EraseString(FirstErrorKey);
        SessionState.EraseBool(TimedOutKey);
        SessionState.EraseBool(ExpectedExitKey);
        SessionState.EraseString(InfrastructureErrorKey);
        SessionState.EraseString(PreviousPlayModeStartSceneKey);
        SessionState.EraseBool(ChangedPlayModeStartSceneKey);
        UnregisterCallbacks();
    }
}
