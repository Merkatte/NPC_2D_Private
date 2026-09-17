using System;
using NUnit.Framework;

internal sealed class HarnessGateResultBuilderTests
{
    [Test]
    public void Build_AllChecksPass_ReturnsPass()
    {
        HarnessGateResultBuilder builder = new HarnessGateResultBuilder("run-pass", "Test.Profile", 1);
        builder.AddPass("compile", "Compilation succeeded.", "0 errors", "0 errors");

        HarnessGateResult result = builder.Build();

        Assert.That(result.success, Is.True);
        Assert.That(result.status, Is.EqualTo(HarnessGateStatus.Pass.ToString()));
        Assert.That(result.checks, Has.Length.EqualTo(1));
        Assert.That(result.checks[0].status, Is.EqualTo(HarnessGateCheckStatus.Pass.ToString()));
    }

    [Test]
    public void Build_AnyCheckFails_ReturnsFailWithEvidence()
    {
        HarnessGateResultBuilder builder = new HarnessGateResultBuilder("run-fail", "Test.Profile", 1);
        builder.AddPass("compile", "Compilation succeeded.");
        builder.AddFailure("scene", "Required object is missing.", "/HarnessBeacon", "not found");

        HarnessGateResult result = builder.Build();

        Assert.That(result.success, Is.False);
        Assert.That(result.status, Is.EqualTo(HarnessGateStatus.Fail.ToString()));
        Assert.That(result.checks[1].expected, Is.EqualTo("/HarnessBeacon"));
        Assert.That(result.checks[1].actual, Is.EqualTo("not found"));
    }

    [Test]
    public void Build_InfrastructureError_DoesNotMasqueradeAsCandidateFailure()
    {
        HarnessGateResult result = HarnessGateResultBuilder.CreateInfrastructureError(
            "run-infrastructure",
            "Test.Profile",
            1,
            "Unity executable was not found.");

        Assert.That(result.success, Is.False);
        Assert.That(result.status, Is.EqualTo(HarnessGateStatus.InfrastructureError.ToString()));
        Assert.That(result.message, Is.EqualTo("Unity executable was not found."));
    }

    [Test]
    public void Build_NoChecks_ReturnsInfrastructureError()
    {
        HarnessGateResultBuilder builder = new HarnessGateResultBuilder("run-empty", "Test.Profile", 1);

        HarnessGateResult result = builder.Build();

        Assert.That(result.success, Is.False);
        Assert.That(result.status, Is.EqualTo(HarnessGateStatus.InfrastructureError.ToString()));
        Assert.That(result.message, Does.Contain("produced no checks"));
    }

    [Test]
    public void AddCheck_DuplicateId_Throws()
    {
        HarnessGateResultBuilder builder = new HarnessGateResultBuilder("run-duplicate", "Test.Profile", 1);
        builder.AddPass("compile", "Compilation succeeded.");

        Assert.Throws<InvalidOperationException>(() => builder.AddPass("compile", "Duplicate."));
    }

    [Test]
    public void Build_ArtifactsAndChangedFiles_PreserveEvidence()
    {
        HarnessGateResultBuilder builder = new HarnessGateResultBuilder("run-evidence", "Test.Profile", 2);
        builder.AddPass("compile", "Compilation succeeded.");
        builder.AddArtifact("unity-log", ".harness-runs/run-evidence/unity.log");
        builder.SetChangedFiles(new[] { "Assets\\TestOnly\\HarnessTest.unity" });

        HarnessGateResult result = builder.Build();

        Assert.That(result.profileVersion, Is.EqualTo(2));
        Assert.That(result.artifacts[0].kind, Is.EqualTo("unity-log"));
        Assert.That(result.changedFiles[0], Is.EqualTo("Assets/TestOnly/HarnessTest.unity"));
        Assert.That(result.startedAtUtc, Is.Not.Empty);
        Assert.That(result.finishedAtUtc, Is.Not.Empty);
    }

}
