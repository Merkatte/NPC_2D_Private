using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;

internal static class HarnessJobRunner
{
    public static HarnessJobResult Validate(HarnessJob job, HarnessExecutionOptions options)
    {
        HarnessToolContext context = new HarnessToolContext(options);
        return ValidateInternal(job, context);
    }

    public static HarnessJobResult RunFromPath(string jobPath, HarnessExecutionOptions options)
    {
        try
        {
            HarnessJob job = HarnessJobStorage.LoadJob(jobPath, out string jobJson);
            return Run(job, Path.GetFullPath(jobPath), jobJson, options);
        }
        catch (Exception exception)
        {
            HarnessJobResult failure = CreateResult(HarnessRunState.Failed, exception.Message, 0);
            HarnessEditorState.SetLastResult(failure);
            return failure;
        }
    }

    private static HarnessJobResult Run(
        HarnessJob job,
        string jobPath,
        string jobJson,
        HarnessExecutionOptions options)
    {
        HarnessToolContext context = new HarnessToolContext(options);
        HarnessJobResult validation = ValidateInternal(job, context);
        if (!validation.success)
        {
            HarnessEditorState.SetLastResult(validation);
            return validation;
        }

        string jobHash = HarnessJobStorage.ComputeHash(jobJson);
        string checkpointPath = HarnessJobStorage.GetCheckpointPath(job.jobId);
        int startStepIndex = ResolveStartStep(
            checkpointPath,
            job,
            jobPath,
            jobHash,
            out bool changedBeforeCheckpoint);
        int undoGroup = -1;
        if (options.Interactive)
        {
            Undo.IncrementCurrentGroup();
            undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName($"Run NPC Harness Job {job.jobId}");
        }

        try
        {
            bool changed = changedBeforeCheckpoint;
            for (int stepIndex = startStepIndex; stepIndex < job.steps.Length; stepIndex++)
            {
                HarnessStep step = job.steps[stepIndex];
                HarnessToolRegistry.TryGet(step.tool, out IHarnessTool tool);
                HarnessToolResult result = tool.Execute(context, step);
                if (result.IsFailure)
                {
                    if (options.Interactive && undoGroup >= 0)
                    {
                        Undo.RevertAllDownToGroup(undoGroup);
                    }

                    HarnessJobResult failure = CreateResult(result.State, $"Step {step.id} failed: {result.Message}", stepIndex);
                    HarnessEditorState.SetLastResult(failure);
                    return failure;
                }

                changed |= result.State == HarnessRunState.Succeeded;
                if (result.State == HarnessRunState.AwaitingCompilation)
                {
                    HarnessCheckpoint checkpoint = new HarnessCheckpoint
                    {
                        jobId = job.jobId,
                        jobPath = jobPath,
                        jobHash = jobHash,
                        nextStepIndex = stepIndex + 1,
                        interactive = options.Interactive,
                        allowOverwrite = options.AllowOverwrite,
                        changedBeforeCheckpoint = true,
                    };
                    HarnessJobStorage.WriteCheckpoint(checkpointPath, checkpoint);
                    HarnessJobResult waiting = CreateResult(
                        HarnessRunState.AwaitingCompilation,
                        result.Message + " Compilation must finish before the Job can continue.",
                        stepIndex + 1);
                    HarnessEditorState.SetLastResult(waiting);
                    if (options.Interactive)
                    {
                        HarnessCompilationResume.Arm(checkpointPath);
                    }

                    return waiting;
                }
            }

            HarnessJobStorage.DeleteCheckpoint(checkpointPath);
            if (options.Interactive && undoGroup >= 0)
            {
                Undo.CollapseUndoOperations(undoGroup);
            }

            HarnessJobResult success = CreateResult(
                changed ? HarnessRunState.Succeeded : HarnessRunState.NoChange,
                changed ? "Harness Job completed successfully." : "Harness Job completed with no changes.",
                job.steps.Length);
            HarnessEditorState.SetLastResult(success);
            return success;
        }
        catch (Exception exception)
        {
            if (options.Interactive && undoGroup >= 0)
            {
                Undo.RevertAllDownToGroup(undoGroup);
            }

            HarnessJobResult failure = CreateResult(HarnessRunState.Failed, exception.Message, startStepIndex);
            HarnessEditorState.SetLastResult(failure);
            return failure;
        }
    }

    private static HarnessJobResult ValidateInternal(HarnessJob job, HarnessToolContext context)
    {
        if (job == null)
        {
            return CreateResult(HarnessRunState.ValidationFailed, "Harness Job is missing.", 0);
        }

        if (job.schemaVersion != 1)
        {
            return CreateResult(
                HarnessRunState.ValidationFailed,
                $"Unsupported Harness Job schema version: {job.schemaVersion}",
                0);
        }

        if (!IsSafeJobId(job.jobId))
        {
            return CreateResult(HarnessRunState.ValidationFailed, "Job ID contains unsupported characters.", 0);
        }

        if (job.steps == null || job.steps.Length == 0)
        {
            return CreateResult(HarnessRunState.ValidationFailed, "Harness Job has no steps.", 0);
        }

        HashSet<string> stepIds = new HashSet<string>(StringComparer.Ordinal);
        for (int stepIndex = 0; stepIndex < job.steps.Length; stepIndex++)
        {
            HarnessStep step = job.steps[stepIndex];
            if (step == null || string.IsNullOrWhiteSpace(step.id) || !stepIds.Add(step.id))
            {
                return CreateResult(
                    HarnessRunState.ValidationFailed,
                    $"Step {stepIndex} has a missing or duplicate ID.",
                    stepIndex);
            }

            if (!HarnessToolRegistry.TryGet(step.tool, out IHarnessTool tool))
            {
                return CreateResult(
                    HarnessRunState.ValidationFailed,
                    $"Step {step.id} uses an unknown Tool: {step.tool}",
                    stepIndex);
            }

            if (step.tool == "WriteCSharpScript" && stepIndex != 0)
            {
                return CreateResult(
                    HarnessRunState.ValidationFailed,
                    "WriteCSharpScript must be the first Job step so compilation cannot interrupt scene changes.",
                    stepIndex);
            }

            HarnessToolResult toolValidation = tool.Validate(context, step);
            if (toolValidation.IsFailure)
            {
                return CreateResult(
                    HarnessRunState.ValidationFailed,
                    $"Step {step.id} is invalid: {toolValidation.Message}",
                    stepIndex);
            }
        }

        return CreateResult(HarnessRunState.Succeeded, "Harness Job is valid.", 0);
    }

    private static int ResolveStartStep(
        string checkpointPath,
        HarnessJob job,
        string jobPath,
        string jobHash,
        out bool changedBeforeCheckpoint)
    {
        if (!File.Exists(checkpointPath))
        {
            changedBeforeCheckpoint = false;
            return 0;
        }

        HarnessCheckpoint checkpoint = HarnessJobStorage.LoadCheckpoint(checkpointPath);
        if (!string.Equals(checkpoint.jobId, job.jobId, StringComparison.Ordinal) ||
            !string.Equals(Path.GetFullPath(checkpoint.jobPath), jobPath, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(checkpoint.jobHash, jobHash, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Harness checkpoint does not match the requested Job.");
        }

        if (checkpoint.nextStepIndex < 0 || checkpoint.nextStepIndex > job.steps.Length)
        {
            throw new InvalidOperationException("Harness checkpoint contains an invalid next step index.");
        }

        changedBeforeCheckpoint = checkpoint.changedBeforeCheckpoint;
        return checkpoint.nextStepIndex;
    }

    private static bool IsSafeJobId(string jobId)
    {
        if (string.IsNullOrWhiteSpace(jobId))
        {
            return false;
        }

        foreach (char character in jobId)
        {
            if (!char.IsLetterOrDigit(character) && character != '-' && character != '_')
            {
                return false;
            }
        }

        return true;
    }

    private static HarnessJobResult CreateResult(HarnessRunState state, string message, int nextStepIndex)
    {
        return new HarnessJobResult
        {
            success = state == HarnessRunState.Succeeded || state == HarnessRunState.NoChange,
            state = state.ToString(),
            message = message,
            nextStepIndex = nextStepIndex,
        };
    }
}
