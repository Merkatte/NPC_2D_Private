using System;
using UnityEditor;
using UnityEngine;

public static class SquareCharacterGateRunner
{
    internal const string Profile = "HarnessTest.SquareCharacter.Structure";
    internal const int ProfileVersion = 1;

    public static void VerifyStructureFromCommandLine()
    {
        string resultPath = string.Empty;
        string runId = HarnessBatchRunner.GetOptionalArgument(
            "-harnessRunId",
            "square-character-gate-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff"));

        try
        {
            resultPath = HarnessBatchRunner.GetRequiredArgument("-harnessResultPath");
            HarnessGateResult result = SquareCharacterValidator.EvaluateStructure(runId);
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
