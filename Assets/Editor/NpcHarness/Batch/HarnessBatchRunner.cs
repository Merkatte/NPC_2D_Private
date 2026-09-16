using System;
using UnityEditor;
using UnityEngine;

public static class HarnessBatchRunner
{
    private const string JobPathArgument = "-harnessJobPath";
    private const string ResultPathArgument = "-harnessResultPath";
    private const string AllowOverwriteArgument = "-harnessAllowOverwrite";
    public const int RestartForCompilationExitCode = 10;

    public static void RunJobFromCommandLine()
    {
        string resultPath = string.Empty;
        try
        {
            string jobPath = GetRequiredArgument(JobPathArgument);
            resultPath = GetRequiredArgument(ResultPathArgument);
            bool allowOverwrite = HasArgument(AllowOverwriteArgument);
            HarnessJobResult result = HarnessJobRunner.RunFromPath(
                jobPath,
                new HarnessExecutionOptions(allowOverwrite, interactive: false));
            HarnessResultWriter.Write(resultPath, result);
            Debug.Log($"NPC Harness Batch result: {result.state} - {result.message}");

            if (result.state == HarnessRunState.AwaitingCompilation.ToString())
            {
                EditorApplication.Exit(RestartForCompilationExitCode);
                return;
            }

            EditorApplication.Exit(result.success ? 0 : 1);
        }
        catch (Exception exception)
        {
            HarnessResultWriter.Write(resultPath, new HarnessJobResult
            {
                success = false,
                state = HarnessRunState.Failed.ToString(),
                message = exception.Message,
            });
            Debug.LogException(exception);
            EditorApplication.Exit(1);
        }
    }

    internal static string GetRequiredArgument(string argumentName)
    {
        string[] arguments = Environment.GetCommandLineArgs();
        for (int index = 0; index < arguments.Length - 1; index++)
        {
            if (arguments[index] == argumentName && !string.IsNullOrWhiteSpace(arguments[index + 1]))
            {
                return System.IO.Path.GetFullPath(arguments[index + 1]);
            }
        }

        throw new InvalidOperationException($"Required command-line argument is missing: {argumentName}");
    }

    private static bool HasArgument(string argumentName)
    {
        foreach (string argument in Environment.GetCommandLineArgs())
        {
            if (argument == argumentName)
            {
                return true;
            }
        }

        return false;
    }
}
