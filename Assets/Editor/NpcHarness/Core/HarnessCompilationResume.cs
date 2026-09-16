using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.Compilation;
using UnityEngine;

[InitializeOnLoad]
internal static class HarnessCompilationResume
{
    private const string PendingCheckpointKey = "NpcHarness.PendingCompilationCheckpoint";

    static HarnessCompilationResume()
    {
        CompilationPipeline.compilationFinished -= HandleCompilationFinished;
        CompilationPipeline.compilationFinished += HandleCompilationFinished;
    }

    public static void Arm(string checkpointPath)
    {
        SessionState.SetString(PendingCheckpointKey, checkpointPath);
        EditorApplication.delayCall += () => AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
    }

    [DidReloadScripts]
    private static void ResumeAfterReload()
    {
        string checkpointPath = SessionState.GetString(PendingCheckpointKey, string.Empty);
        if (string.IsNullOrWhiteSpace(checkpointPath) || !File.Exists(checkpointPath))
        {
            return;
        }

        SessionState.EraseString(PendingCheckpointKey);
        EditorApplication.delayCall += () => Resume(checkpointPath);
    }

    private static void HandleCompilationFinished(object context)
    {
        if (!EditorUtility.scriptCompilationFailed)
        {
            return;
        }

        string checkpointPath = SessionState.GetString(PendingCheckpointKey, string.Empty);
        if (string.IsNullOrWhiteSpace(checkpointPath))
        {
            return;
        }

        HarnessEditorState.SetLastResult(new HarnessJobResult
        {
            success = false,
            state = HarnessRunState.Failed.ToString(),
            message = "Unity script compilation failed. Fix the compile errors; the checkpoint is retained.",
        });
        Debug.LogError("NPC Harness paused because Unity script compilation failed.");
    }

    private static void Resume(string checkpointPath)
    {
        HarnessCheckpoint checkpoint = HarnessJobStorage.LoadCheckpoint(checkpointPath);
        if (!checkpoint.interactive)
        {
            return;
        }

        HarnessJobResult result = HarnessJobRunner.RunFromPath(
            checkpoint.jobPath,
            new HarnessExecutionOptions(checkpoint.allowOverwrite, interactive: true));
        Debug.Log($"NPC Harness resume result: {result.state} - {result.message}");
    }
}
