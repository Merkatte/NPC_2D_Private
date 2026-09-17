using System;
using System.Linq;
using NUnit.Framework;

internal sealed class SquareCharacterStructureGateTests
{
    [Test]
    public void Evaluate_NormalFixture_PassesAllTwentyChecks()
    {
        HarnessGateResult result = Evaluate(CreateNormalFixture());

        Assert.That(result.success, Is.True);
        Assert.That(result.status, Is.EqualTo(HarnessGateStatus.Pass.ToString()));
        Assert.That(result.checks, Has.Length.EqualTo(20));
        Assert.That(result.checks.All(check => check.success), Is.True);
    }

    [Test]
    public void Evaluate_WrongRootTransform_FailsStableRootCheck()
    {
        SquareCharacterStructureObservation normal = CreateNormalFixture();
        SquareCharacterStructureObservation fixture = new SquareCharacterStructureObservation(
            true,
            string.Empty,
            Vector(0f, 0f),
            IdentityRotation(),
            One(),
            normal.RootIsActive,
            normal.RootComponentCount,
            normal.DirectChildNames,
            true,
            normal.Parts);

        AssertFailure(Evaluate(fixture), "scene.square-character.root");
    }

    [Test]
    public void Evaluate_ExtraChild_FailsExactPartsCheck()
    {
        SquareCharacterStructureObservation normal = CreateNormalFixture();
        SquareCharacterStructureObservation fixture = new SquareCharacterStructureObservation(
            true,
            string.Empty,
            normal.RootLocalPosition,
            normal.RootLocalRotation,
            normal.RootLocalScale,
            normal.RootIsActive,
            normal.RootComponentCount,
            normal.DirectChildNames.Concat(new[] { "Extra" }).ToArray(),
            true,
            normal.Parts);

        AssertFailure(Evaluate(fixture), "scene.square-character.exact-parts");
    }

    [Test]
    public void Evaluate_MissingHead_FailsExactAndHeadChecks()
    {
        SquareCharacterStructureObservation normal = CreateNormalFixture();
        SquareCharacterStructureObservation fixture = new SquareCharacterStructureObservation(
            true,
            string.Empty,
            normal.RootLocalPosition,
            normal.RootLocalRotation,
            normal.RootLocalScale,
            normal.RootIsActive,
            normal.RootComponentCount,
            normal.DirectChildNames.Where(name => name != "Head").ToArray(),
            true,
            normal.Parts.Where(part => part.Name != "Head").ToArray());

        HarnessGateResult result = Evaluate(fixture);

        AssertFailure(result, "scene.square-character.exact-parts");
        AssertFailure(result, "scene.square-character.head.layout");
        AssertFailure(result, "scene.square-character.head.shape");
        AssertFailure(result, "scene.square-character.head.material");
    }

    [Test]
    public void Evaluate_WrongPartLayout_FailsOnlyPartLayoutCheck()
    {
        SquareCharacterStructureObservation fixture = ReplacePart(
            CreateNormalFixture(),
            CreatePart(Specification("Body"), localPosition: Vector(0.1f, 0.25f)));

        HarnessGateResult result = Evaluate(fixture);

        AssertFailure(result, "scene.square-character.body.layout");
        Assert.That(FindCheck(result, "scene.square-character.body.shape").success, Is.True);
        Assert.That(FindCheck(result, "scene.square-character.body.material").success, Is.True);
    }

    [Test]
    public void Evaluate_WorldSpaceRenderer_FailsPartShapeCheck()
    {
        SquareCharacterPartSpecification head = Specification("Head");
        SquareCharacterStructureObservation fixture = ReplacePart(
            CreateNormalFixture(),
            CreatePart(head, useWorldSpace: true));

        AssertFailure(Evaluate(fixture), "scene.square-character.head.shape");
    }

    [Test]
    public void Evaluate_WrongPointOrder_FailsPartShapeCheck()
    {
        SquareCharacterPartSpecification leg = Specification("LeftLeg");
        SquareCharacterVectorObservation[] points = Square(leg.HalfSize);
        (points[1], points[2]) = (points[2], points[1]);
        SquareCharacterStructureObservation fixture = ReplacePart(
            CreateNormalFixture(),
            CreatePart(leg, points: points));

        AssertFailure(Evaluate(fixture), "scene.square-character.left-leg.shape");
    }

    [Test]
    public void Evaluate_WrongMaterial_FailsPartMaterialCheck()
    {
        SquareCharacterPartSpecification arm = Specification("RightArm");
        SquareCharacterStructureObservation fixture = ReplacePart(
            CreateNormalFixture(),
            CreatePart(arm, materialPath: "Assets/Other.mat"));

        AssertFailure(Evaluate(fixture), "scene.square-character.right-arm.material");
    }

    private static HarnessGateResult Evaluate(SquareCharacterStructureObservation observation)
    {
        return SquareCharacterStructureGate.Evaluate("square-character-test", observation);
    }

    private static SquareCharacterStructureObservation CreateNormalFixture()
    {
        string[] names = SquareCharacterStructureGate.PartSpecifications
            .Select(specification => specification.Name)
            .ToArray();
        SquareCharacterPartObservation[] parts = SquareCharacterStructureGate.PartSpecifications
            .Select(specification => CreatePart(specification))
            .ToArray();
        return new SquareCharacterStructureObservation(
            true,
            string.Empty,
            Vector(1.8f, 0f),
            IdentityRotation(),
            One(),
            true,
            1,
            names,
            true,
            parts);
    }

    private static SquareCharacterStructureObservation ReplacePart(
        SquareCharacterStructureObservation observation,
        SquareCharacterPartObservation replacement)
    {
        SquareCharacterPartObservation[] parts = observation.Parts
            .Select(part => part.Name == replacement.Name ? replacement : part)
            .ToArray();
        return new SquareCharacterStructureObservation(
            observation.HasUniqueRoot,
            observation.RootResolutionError,
            observation.RootLocalPosition,
            observation.RootLocalRotation,
            observation.RootLocalScale,
            observation.RootIsActive,
            observation.RootComponentCount,
            observation.DirectChildNames,
            observation.HasManagedMaterial,
            parts);
    }

    private static SquareCharacterPartObservation CreatePart(
        SquareCharacterPartSpecification specification,
        SquareCharacterVectorObservation? localPosition = null,
        bool useWorldSpace = false,
        string materialPath = HarnessBeaconRecipe.MaterialPath,
        SquareCharacterVectorObservation[] points = null)
    {
        return new SquareCharacterPartObservation(
            specification.Name,
            true,
            string.Empty,
            localPosition ?? specification.LocalPosition,
            IdentityRotation(),
            One(),
            true,
            2,
            1,
            useWorldSpace,
            false,
            true,
            11,
            0.08f,
            0.08f,
            4,
            4,
            materialPath,
            points ?? Square(specification.HalfSize));
    }

    private static SquareCharacterPartSpecification Specification(string name)
    {
        return SquareCharacterStructureGate.PartSpecifications.Single(value => value.Name == name);
    }

    private static SquareCharacterVectorObservation[] Square(float halfSize)
    {
        return new[]
        {
            Vector(-halfSize, -halfSize),
            Vector(-halfSize, halfSize),
            Vector(halfSize, halfSize),
            Vector(halfSize, -halfSize),
            Vector(-halfSize, -halfSize),
        };
    }

    private static SquareCharacterVectorObservation Vector(float x, float y, float z = 0f)
    {
        return new SquareCharacterVectorObservation(x, y, z);
    }

    private static SquareCharacterQuaternionObservation IdentityRotation()
    {
        return new SquareCharacterQuaternionObservation(0f, 0f, 0f, 1f);
    }

    private static SquareCharacterVectorObservation One()
    {
        return new SquareCharacterVectorObservation(1f, 1f, 1f);
    }

    private static void AssertFailure(HarnessGateResult result, string checkId)
    {
        Assert.That(result.success, Is.False);
        Assert.That(result.status, Is.EqualTo(HarnessGateStatus.Fail.ToString()));
        Assert.That(FindCheck(result, checkId).success, Is.False);
    }

    private static HarnessGateCheckResult FindCheck(HarnessGateResult result, string checkId)
    {
        return result.checks.Single(check => check.id == checkId);
    }
}
