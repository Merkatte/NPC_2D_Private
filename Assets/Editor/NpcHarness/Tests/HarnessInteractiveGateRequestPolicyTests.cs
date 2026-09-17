using System.IO;
using NUnit.Framework;
using UnityEngine;

internal sealed class HarnessInteractiveGateRequestPolicyTests
{
    [Test]
    public void TryParseAndValidate_ValidRequest_NormalizesResultPath()
    {
        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        string resultPath = Path.Combine(projectRoot, ".harness-runs", "test-run", "result.json");
        string json = JsonUtility.ToJson(new HarnessInteractiveGateRequest
        {
            runId = "safe-ID_1",
            profile = SquareCharacterGateRunner.Profile,
            resultPath = resultPath,
        });

        bool success = HarnessInteractiveGateRequestPolicy.TryParseAndValidate(
            json,
            projectRoot,
            out HarnessInteractiveGateRequest request,
            out string error);

        Assert.That(success, Is.True, error);
        Assert.That(request.resultPath, Is.EqualTo(Path.GetFullPath(resultPath)));
    }

    [Test]
    public void TryParseAndValidate_UnsafeRunId_IsRejected()
    {
        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        string json = JsonUtility.ToJson(new HarnessInteractiveGateRequest
        {
            runId = "../escape",
            profile = SquareCharacterGateRunner.Profile,
            resultPath = Path.Combine(projectRoot, ".harness-runs", "result.json"),
        });

        bool success = HarnessInteractiveGateRequestPolicy.TryParseAndValidate(
            json,
            projectRoot,
            out _,
            out string error);

        Assert.That(success, Is.False);
        StringAssert.Contains("runId", error);
    }

    [Test]
    public void TryParseAndValidate_ResultOutsideHarnessRuns_IsRejected()
    {
        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        string json = JsonUtility.ToJson(new HarnessInteractiveGateRequest
        {
            runId = "safe-run",
            profile = SquareCharacterGateRunner.Profile,
            resultPath = Path.Combine(projectRoot, "Library", "not-allowed.json"),
        });

        bool success = HarnessInteractiveGateRequestPolicy.TryParseAndValidate(
            json,
            projectRoot,
            out _,
            out string error);

        Assert.That(success, Is.False);
        StringAssert.Contains(".harness-runs", error);
    }

    [Test]
    public void TryResolveProfile_SupportsOnlySynchronousStructureProfiles()
    {
        Assert.That(HarnessInteractiveGateRequestPolicy.TryResolveProfile(
            HarnessBeaconGateRunner.Profile,
            out HarnessInteractiveGateKind beaconKind,
            out int beaconVersion), Is.True);
        Assert.That(beaconKind, Is.EqualTo(HarnessInteractiveGateKind.BeaconStructure));
        Assert.That(beaconVersion, Is.EqualTo(1));

        Assert.That(HarnessInteractiveGateRequestPolicy.TryResolveProfile(
            SquareCharacterGateRunner.Profile,
            out HarnessInteractiveGateKind squareKind,
            out int squareVersion), Is.True);
        Assert.That(squareKind, Is.EqualTo(HarnessInteractiveGateKind.SquareCharacterStructure));
        Assert.That(squareVersion, Is.EqualTo(1));

        Assert.That(HarnessInteractiveGateRequestPolicy.TryResolveProfile(
            "HarnessBeacon.PlayMode",
            out _,
            out _), Is.False);
    }
}
