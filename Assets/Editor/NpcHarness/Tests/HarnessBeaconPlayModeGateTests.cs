using NUnit.Framework;

internal sealed class HarnessBeaconPlayModeGateTests
{
    [Test]
    public void Evaluate_ValidObservation_ReturnsPass()
    {
        HarnessGateResult result = HarnessBeaconPlayModeGate.Evaluate(
            "play-pass",
            CreatePassingStructureResult("play-pass"),
            new HarnessPlayModeObservation(1, 0, string.Empty, false, false));

        Assert.That(result.success, Is.True);
        Assert.That(result.status, Is.EqualTo(HarnessGateStatus.Pass.ToString()));
        Assert.That(result.checks, Has.Length.EqualTo(9));
        Assert.That(FindCheck(result, "runtime.timeout").success, Is.True);
        Assert.That(FindCheck(result, "runtime.completed-by-gate").success, Is.True);
        Assert.That(FindCheck(result, "runtime.success-log-count").success, Is.True);
        Assert.That(FindCheck(result, "runtime.error-log-count").success, Is.True);
    }

    [TestCase(0)]
    [TestCase(2)]
    public void Evaluate_InvalidSuccessCount_FailsSuccessLogCheck(int successCount)
    {
        HarnessGateResult result = HarnessBeaconPlayModeGate.Evaluate(
            "play-success-count",
            CreatePassingStructureResult("play-success-count"),
            new HarnessPlayModeObservation(successCount, 0, string.Empty, false, false));

        HarnessGateCheckResult check = FindCheck(result, "runtime.success-log-count");
        Assert.That(result.success, Is.False);
        Assert.That(check.success, Is.False);
        Assert.That(check.actual, Is.EqualTo(successCount.ToString()));
    }

    [Test]
    public void Evaluate_ErrorLog_FailsAndPreservesFirstError()
    {
        HarnessGateResult result = HarnessBeaconPlayModeGate.Evaluate(
            "play-error",
            CreatePassingStructureResult("play-error"),
            new HarnessPlayModeObservation(1, 2, "Example exception", false, false));

        HarnessGateCheckResult check = FindCheck(result, "runtime.error-log-count");
        Assert.That(result.success, Is.False);
        Assert.That(check.success, Is.False);
        Assert.That(check.actual, Does.Contain("Example exception"));
    }

    [Test]
    public void Evaluate_Timeout_FailsTimeoutCheck()
    {
        HarnessGateResult result = HarnessBeaconPlayModeGate.Evaluate(
            "play-timeout",
            CreatePassingStructureResult("play-timeout"),
            new HarnessPlayModeObservation(0, 0, string.Empty, true, false));

        Assert.That(result.success, Is.False);
        Assert.That(FindCheck(result, "runtime.timeout").success, Is.False);
    }

    [Test]
    public void Evaluate_UnexpectedExit_FailsCompletionCheck()
    {
        HarnessGateResult result = HarnessBeaconPlayModeGate.Evaluate(
            "play-unexpected-exit",
            CreatePassingStructureResult("play-unexpected-exit"),
            new HarnessPlayModeObservation(1, 0, string.Empty, false, true));

        Assert.That(result.success, Is.False);
        Assert.That(FindCheck(result, "runtime.completed-by-gate").success, Is.False);
    }

    [Test]
    public void Evaluate_StructureFailure_DoesNotAddRuntimeChecks()
    {
        HarnessGateResultBuilder structureBuilder = new HarnessGateResultBuilder(
            "play-structure-fail",
            "HarnessBeacon.Structure",
            1);
        structureBuilder.AddFailure("scene.beacon-object", "Missing.", "/HarnessBeacon", "not found");

        HarnessGateResult result = HarnessBeaconPlayModeGate.Evaluate(
            "play-structure-fail",
            structureBuilder.Build(),
            default);

        Assert.That(result.success, Is.False);
        Assert.That(result.profile, Is.EqualTo(HarnessBeaconPlayModeGate.Profile));
        Assert.That(result.checks, Has.Length.EqualTo(1));
        Assert.That(result.checks[0].id, Is.EqualTo("scene.beacon-object"));
    }

    [Test]
    public void Evaluate_MissingStructureResult_ReturnsInfrastructureError()
    {
        HarnessGateResult result = HarnessBeaconPlayModeGate.Evaluate(
            "play-missing-structure",
            null,
            default);

        Assert.That(result.success, Is.False);
        Assert.That(result.status, Is.EqualTo(HarnessGateStatus.InfrastructureError.ToString()));
    }

    [Test]
    public void Evaluate_StructureInfrastructureError_RemainsInfrastructureError()
    {
        HarnessGateResult structureResult = HarnessGateResultBuilder.CreateInfrastructureError(
            "play-structure-infrastructure",
            HarnessBeaconGateRunner.Profile,
            HarnessBeaconGateRunner.ProfileVersion,
            "Scene could not be loaded.");

        HarnessGateResult result = HarnessBeaconPlayModeGate.Evaluate(
            "play-structure-infrastructure",
            structureResult,
            default);

        Assert.That(result.success, Is.False);
        Assert.That(result.status, Is.EqualTo(HarnessGateStatus.InfrastructureError.ToString()));
        Assert.That(result.message, Is.EqualTo("Scene could not be loaded."));
    }

    private static HarnessGateResult CreatePassingStructureResult(string runId)
    {
        HarnessBeaconStructureObservation observation = new HarnessBeaconStructureObservation(
            true,
            string.Empty,
            1,
            true,
            5,
            HarnessBeaconRecipe.MaterialPath,
            true,
            true,
            string.Empty,
            true,
            true,
            2.5f);
        return HarnessBeaconStructureGate.Evaluate(runId, observation);
    }

    private static HarnessGateCheckResult FindCheck(HarnessGateResult result, string id)
    {
        foreach (HarnessGateCheckResult check in result.checks)
        {
            if (check.id == id)
            {
                return check;
            }
        }

        Assert.Fail($"Gate check was not found: {id}");
        return null;
    }
}
