using NUnit.Framework;

internal sealed class HarnessBeaconStructureGateTests
{
    [Test]
    public void Evaluate_NormalFixture_PassesAllStructureChecks()
    {
        HarnessGateResult result = Evaluate(CreateNormalFixture());

        Assert.That(result.success, Is.True);
        Assert.That(result.status, Is.EqualTo(HarnessGateStatus.Pass.ToString()));
        Assert.That(result.checks, Has.Length.EqualTo(5));
        Assert.That(FindCheck(result, "scene.beacon-object").success, Is.True);
        Assert.That(FindCheck(result, "scene.harness-component").success, Is.True);
        Assert.That(FindCheck(result, "scene.line-renderer").success, Is.True);
        Assert.That(FindCheck(result, "scene.line-material").success, Is.True);
        Assert.That(FindCheck(result, "scene.main-camera").success, Is.True);
    }

    [Test]
    public void Evaluate_MissingHarnessComponent_FailsHarnessComponentCheck()
    {
        HarnessBeaconStructureObservation fixture = CreateFixture(harnessComponentCount: 0);

        HarnessGateResult result = Evaluate(fixture);

        AssertExpectedFailure(result, "scene.harness-component", "0");
    }

    [Test]
    public void Evaluate_MissingBeacon_FailsBeforeDependentChecks()
    {
        HarnessBeaconStructureObservation fixture = new HarnessBeaconStructureObservation(
            false,
            "not found",
            0,
            false,
            0,
            string.Empty,
            false,
            true,
            string.Empty,
            true,
            true,
            2.5f);

        HarnessGateResult result = Evaluate(fixture);

        AssertExpectedFailure(result, "scene.beacon-object", "not found");
        Assert.That(result.checks, Has.Length.EqualTo(1));
    }

    [Test]
    public void Evaluate_MissingLineRenderer_FailsLineRendererCheck()
    {
        HarnessBeaconStructureObservation fixture = CreateFixture(
            hasLineRenderer: false,
            linePositionCount: 0,
            lineMaterialPath: string.Empty);

        HarnessGateResult result = Evaluate(fixture);

        AssertExpectedFailure(result, "scene.line-renderer", "component missing");
    }

    [Test]
    public void Evaluate_MissingLineMaterial_FailsLineMaterialCheck()
    {
        HarnessBeaconStructureObservation fixture = CreateFixture(lineMaterialPath: string.Empty);

        HarnessGateResult result = Evaluate(fixture);

        AssertExpectedFailure(result, "scene.line-material", "missing");
    }

    [Test]
    public void Evaluate_CameraMismatch_FailsMainCameraCheck()
    {
        HarnessBeaconStructureObservation fixture = CreateFixture(
            isOrthographic: false,
            orthographicSize: 4f);

        HarnessGateResult result = Evaluate(fixture);

        AssertExpectedFailure(result, "scene.main-camera", "orthographic=False, size=4");
    }

    [Test]
    public void Evaluate_MissingMainCamera_FailsMainCameraCheck()
    {
        HarnessBeaconStructureObservation fixture = new HarnessBeaconStructureObservation(
            true,
            string.Empty,
            1,
            true,
            5,
            HarnessBeaconRecipe.MaterialPath,
            true,
            false,
            "not found",
            false,
            false,
            0f);

        HarnessGateResult result = Evaluate(fixture);

        AssertExpectedFailure(result, "scene.main-camera", "not found");
    }

    private static HarnessGateResult Evaluate(HarnessBeaconStructureObservation observation)
    {
        return HarnessBeaconStructureGate.Evaluate("structure-fixture", observation);
    }

    private static HarnessBeaconStructureObservation CreateNormalFixture()
    {
        return CreateFixture();
    }

    private static HarnessBeaconStructureObservation CreateFixture(
        int harnessComponentCount = 1,
        bool hasLineRenderer = true,
        int linePositionCount = 5,
        string lineMaterialPath = HarnessBeaconRecipe.MaterialPath,
        bool isOrthographic = true,
        float orthographicSize = 2.5f)
    {
        return new HarnessBeaconStructureObservation(
            true,
            string.Empty,
            harnessComponentCount,
            hasLineRenderer,
            linePositionCount,
            lineMaterialPath,
            true,
            true,
            string.Empty,
            true,
            isOrthographic,
            orthographicSize);
    }

    private static void AssertExpectedFailure(
        HarnessGateResult result,
        string checkId,
        string expectedActual)
    {
        HarnessGateCheckResult check = FindCheck(result, checkId);
        Assert.That(result.success, Is.False);
        Assert.That(result.status, Is.EqualTo(HarnessGateStatus.Fail.ToString()));
        Assert.That(check.success, Is.False);
        Assert.That(check.status, Is.EqualTo(HarnessGateCheckStatus.Fail.ToString()));
        Assert.That(check.actual, Is.EqualTo(expectedActual));
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
