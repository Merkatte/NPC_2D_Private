using System;
using System.Globalization;

internal readonly struct HarnessPlayModeObservation
{
    public HarnessPlayModeObservation(
        int successLogCount,
        int errorLogCount,
        string firstError,
        bool timedOut,
        bool endedUnexpectedly)
    {
        SuccessLogCount = successLogCount;
        ErrorLogCount = errorLogCount;
        FirstError = firstError ?? string.Empty;
        TimedOut = timedOut;
        EndedUnexpectedly = endedUnexpectedly;
    }

    public int SuccessLogCount { get; }
    public int ErrorLogCount { get; }
    public string FirstError { get; }
    public bool TimedOut { get; }
    public bool EndedUnexpectedly { get; }
}

internal static class HarnessBeaconPlayModeGate
{
    public const string Profile = "HarnessBeacon.PlayMode";
    public const int ProfileVersion = 1;

    public static HarnessGateResult Evaluate(
        string runId,
        HarnessGateResult structureResult,
        HarnessPlayModeObservation observation)
    {
        if (structureResult == null)
        {
            return CreateInfrastructureError(runId, "Structure gate result is missing.");
        }

        if (structureResult.status == HarnessGateStatus.InfrastructureError.ToString())
        {
            return CreateInfrastructureError(runId, structureResult.message);
        }

        HarnessGateResultBuilder builder = new HarnessGateResultBuilder(runId, Profile, ProfileVersion);
        CopyStructureChecks(structureResult, builder);
        if (!structureResult.success)
        {
            return builder.Build();
        }

        if (observation.TimedOut)
        {
            builder.AddFailure(
                "runtime.timeout",
                "Play Mode verification timed out.",
                "false",
                "true");
        }
        else
        {
            builder.AddPass(
                "runtime.timeout",
                "Play Mode verification completed before the timeout.",
                "false",
                "false");
        }

        if (observation.EndedUnexpectedly)
        {
            builder.AddFailure(
                "runtime.completed-by-gate",
                "Play Mode ended before the gate completed its observation window.",
                "true",
                "false");
        }
        else
        {
            builder.AddPass(
                "runtime.completed-by-gate",
                "The gate completed its Play Mode observation window.",
                "true",
                "true");
        }

        if (observation.SuccessLogCount == 1)
        {
            builder.AddPass(
                "runtime.success-log-count",
                "HarnessSuccess was logged exactly once.",
                "1",
                "1");
        }
        else
        {
            builder.AddFailure(
                "runtime.success-log-count",
                "HarnessSuccess must be logged exactly once.",
                "1",
                observation.SuccessLogCount.ToString(CultureInfo.InvariantCulture));
        }

        if (observation.ErrorLogCount == 0)
        {
            builder.AddPass(
                "runtime.error-log-count",
                "Play Mode emitted no Error, Assert, or Exception logs.",
                "0",
                "0");
        }
        else
        {
            string actual = observation.ErrorLogCount.ToString(CultureInfo.InvariantCulture);
            if (!string.IsNullOrWhiteSpace(observation.FirstError))
            {
                actual += ": " + observation.FirstError;
            }

            builder.AddFailure(
                "runtime.error-log-count",
                "Play Mode emitted Error, Assert, or Exception logs.",
                "0",
                actual);
        }

        return builder.Build();
    }

    public static HarnessGateResult CreateInfrastructureError(string runId, string message)
    {
        return HarnessGateResultBuilder.CreateInfrastructureError(
            runId,
            Profile,
            ProfileVersion,
            message);
    }

    private static void CopyStructureChecks(
        HarnessGateResult structureResult,
        HarnessGateResultBuilder builder)
    {
        foreach (HarnessGateCheckResult check in structureResult.checks)
        {
            if (check.success)
            {
                builder.AddPass(check.id, check.message, check.expected, check.actual);
            }
            else
            {
                builder.AddFailure(check.id, check.message, check.expected, check.actual);
            }
        }
    }
}
