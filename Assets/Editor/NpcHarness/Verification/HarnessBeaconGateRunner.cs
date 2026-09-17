using System;
using UnityEditor;
using UnityEngine;

public static class HarnessBeaconGateRunner
{
    internal const string Profile = "HarnessBeacon.Structure";
    internal const int ProfileVersion = 1;

    public static void VerifyStructureFromCommandLine()
    {
        string resultPath = string.Empty;
        string runId = HarnessBatchRunner.GetOptionalArgument(
            "-harnessRunId",
            "beacon-gate-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff"));

        try
        {
            resultPath = HarnessBatchRunner.GetRequiredArgument("-harnessResultPath");
            HarnessGateResult result = HarnessBeaconValidator.EvaluateStructure(runId);
            HarnessResultWriter.Write(resultPath, result);
            Debug.Log($"NPC Harness gate result: {result.status} - {result.message}");
            EditorApplication.Exit(result.success ? 0 : 1);
        }
        catch (Exception exception)
        {
            HarnessGateResult result = HarnessGateResultBuilder.CreateInfrastructureError(
                runId,
                Profile,
                ProfileVersion,
                exception.Message);
            HarnessResultWriter.Write(resultPath, result);
            Debug.LogException(exception);
            EditorApplication.Exit(1);
        }
    }
}
