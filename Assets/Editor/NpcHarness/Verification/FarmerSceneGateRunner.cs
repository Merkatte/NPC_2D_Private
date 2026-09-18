using System;
using UnityEditor;
using UnityEngine;

public static class FarmerSceneGateRunner
{
    internal const string Profile = "FarmerScene.Structure";
    internal const int ProfileVersion = 1;

    public static void VerifyStructureFromCommandLine()
    {
        string resultPath = string.Empty;
        string runId = HarnessBatchRunner.GetOptionalArgument("-harnessRunId",
            "farmer-scene-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff"));
        HarnessGateResult result;
        try
        {
            resultPath = HarnessBatchRunner.GetRequiredArgument("-harnessResultPath");
            result = FarmerSceneValidator.EvaluateStructure(runId);
        }
        catch (Exception exception)
        {
            result = HarnessGateResultBuilder.CreateInfrastructureError(runId, Profile, ProfileVersion, exception.Message);
        }
        HarnessResultWriter.Write(resultPath, result);
        Debug.Log("NPC Harness gate result: " + result.status + " - " + result.message);
        if (Application.isBatchMode)
        {
            EditorApplication.Exit(result.success ? 0 : 1);
        }
    }

    [MenuItem("Tools/NPC Harness/Verify Farmer Scene Structure")]
    private static void VerifyFromMenu()
    {
        string runId = "farmer-scene-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff");
        HarnessGateResult result = FarmerSceneValidator.EvaluateStructure(runId);
        string resultPath = ".harness-runs/" + runId + "/gate-results/farmer-scene-structure.json";
        HarnessResultWriter.Write(resultPath, result);
        Debug.Log(result.status + ": " + result.message + " Result: " + resultPath);
    }
}
