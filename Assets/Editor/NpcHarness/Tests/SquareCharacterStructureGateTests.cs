using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

internal sealed class SquareCharacterStructureGateTests
{
    private const string MaterialPath = "Assets/TestOnly/HarnessBeaconLine.mat";
    private const string RootPath = "/SquareCharacter";

    private static readonly PartFixture[] Parts =
    {
        new PartFixture("Head", 0f, 1.35f, 0.45f),
        new PartFixture("Body", 0f, 0.25f, 0.6f),
        new PartFixture("LeftArm", -0.95f, 0.25f, 0.35f),
        new PartFixture("RightArm", 0.95f, 0.25f, 0.35f),
        new PartFixture("LeftLeg", -0.38f, -0.9f, 0.38f),
        new PartFixture("RightLeg", 0.38f, -0.9f, 0.38f),
    };

    [Test]
    public void Evaluate_NormalFixture_PassesStableTwentyChecksFromManifest()
    {
        HarnessGateResult result = Evaluate(CreateNormalFixture());
        string[] expectedCheckIds =
        {
            "scene.square-character.root",
            "scene.square-character.exact-parts",
            "scene.square-character.head.layout",
            "scene.square-character.head.shape",
            "scene.square-character.head.material",
            "scene.square-character.body.layout",
            "scene.square-character.body.shape",
            "scene.square-character.body.material",
            "scene.square-character.left-arm.layout",
            "scene.square-character.left-arm.shape",
            "scene.square-character.left-arm.material",
            "scene.square-character.right-arm.layout",
            "scene.square-character.right-arm.shape",
            "scene.square-character.right-arm.material",
            "scene.square-character.left-leg.layout",
            "scene.square-character.left-leg.shape",
            "scene.square-character.left-leg.material",
            "scene.square-character.right-leg.layout",
            "scene.square-character.right-leg.shape",
            "scene.square-character.right-leg.material",
        };

        Assert.That(result.success, Is.True);
        Assert.That(result.status, Is.EqualTo(HarnessGateStatus.Pass.ToString()));
        Assert.That(result.profile, Is.EqualTo(SquareCharacterGateRunner.Profile));
        Assert.That(result.profileVersion, Is.EqualTo(SquareCharacterGateRunner.ProfileVersion));
        Assert.That(result.checks.Select(check => check.id), Is.EqualTo(expectedCheckIds));
        Assert.That(result.checks.All(check => check.success), Is.True);
    }

    [Test]
    public void Evaluate_WrongRootTransform_FailsStableRootCheck()
    {
        DeclarativeSceneObservation normal = CreateNormalFixture();
        DeclarativeSceneObjectObservation root = Resolve(normal, RootPath);
        DeclarativeSceneObservation fixture = ReplaceObject(
            normal,
            new DeclarativeSceneObjectObservation(
                RootPath,
                root.Active,
                root.ComponentCount,
                Vector(0f, 0f),
                IdentityRotation(),
                One(),
                root.DirectChildNames,
                root.LineRenderer));

        AssertFailure(Evaluate(fixture), "scene.square-character.root");
    }

    [Test]
    public void Evaluate_ExtraChild_FailsExactPartsCheck()
    {
        DeclarativeSceneObservation normal = CreateNormalFixture();
        DeclarativeSceneObjectObservation root = Resolve(normal, RootPath);
        DeclarativeSceneObservation fixture = ReplaceObject(
            normal,
            new DeclarativeSceneObjectObservation(
                RootPath,
                root.Active,
                root.ComponentCount,
                root.LocalPosition,
                root.LocalRotation,
                root.LocalScale,
                root.DirectChildNames.Concat(new[] { "Extra" }).ToArray(),
                root.LineRenderer));

        AssertFailure(Evaluate(fixture), "scene.square-character.exact-parts");
    }

    [Test]
    public void Evaluate_MissingHead_FailsExactAndHeadChecks()
    {
        DeclarativeSceneObservation normal = CreateNormalFixture();
        DeclarativeSceneObjectObservation root = Resolve(normal, RootPath);
        List<DeclarativeSceneObjectObservation> objects = normal.Objects
            .Where(value => value.Path != RootPath + "/Head" && value.Path != RootPath)
            .ToList();
        objects.Add(new DeclarativeSceneObjectObservation(
            RootPath,
            root.Active,
            root.ComponentCount,
            root.LocalPosition,
            root.LocalRotation,
            root.LocalScale,
            root.DirectChildNames.Where(name => name != "Head").ToArray(),
            root.LineRenderer));
        DeclarativeSceneObservation fixture = Observation(objects);

        HarnessGateResult result = Evaluate(fixture);

        AssertFailure(result, "scene.square-character.exact-parts");
        AssertFailure(result, "scene.square-character.head.layout");
        AssertFailure(result, "scene.square-character.head.shape");
        AssertFailure(result, "scene.square-character.head.material");
    }

    [Test]
    public void Evaluate_WrongPartLayout_FailsOnlyPartLayoutCheck()
    {
        PartFixture body = Part("Body");
        DeclarativeSceneObservation fixture = ReplaceObject(
            CreateNormalFixture(),
            CreatePart(body, localPosition: Vector(0.1f, 0.25f)));

        HarnessGateResult result = Evaluate(fixture);

        AssertFailure(result, "scene.square-character.body.layout");
        Assert.That(FindCheck(result, "scene.square-character.body.shape").success, Is.True);
        Assert.That(FindCheck(result, "scene.square-character.body.material").success, Is.True);
    }

    [Test]
    public void Evaluate_WorldSpaceRenderer_FailsPartShapeCheck()
    {
        PartFixture head = Part("Head");
        DeclarativeSceneObservation fixture = ReplaceObject(
            CreateNormalFixture(),
            CreatePart(head, useWorldSpace: true));

        AssertFailure(Evaluate(fixture), "scene.square-character.head.shape");
    }

    [Test]
    public void Evaluate_WrongPointOrder_FailsPartShapeCheck()
    {
        PartFixture leg = Part("LeftLeg");
        DeclarativeVector3[] points = Square(leg.HalfSize);
        (points[1], points[2]) = (points[2], points[1]);
        DeclarativeSceneObservation fixture = ReplaceObject(
            CreateNormalFixture(),
            CreatePart(leg, points: points));

        AssertFailure(Evaluate(fixture), "scene.square-character.left-leg.shape");
    }

    [Test]
    public void Evaluate_WrongMaterial_FailsPartMaterialCheck()
    {
        PartFixture arm = Part("RightArm");
        DeclarativeSceneObservation fixture = ReplaceObject(
            CreateNormalFixture(),
            CreatePart(arm, materialPath: "Assets/Other.mat"));

        AssertFailure(Evaluate(fixture), "scene.square-character.right-arm.material");
    }

    private static HarnessGateResult Evaluate(DeclarativeSceneObservation observation)
    {
        DeclarativeSceneGateManifest manifest =
            DeclarativeSceneGateManifestLoader.Load(SquareCharacterValidator.ManifestPath);
        return DeclarativeSceneStructureGate.Evaluate("square-character-test", manifest, observation);
    }

    private static DeclarativeSceneObservation CreateNormalFixture()
    {
        string[] childNames = Parts.Select(part => part.Name).ToArray();
        List<DeclarativeSceneObjectObservation> objects = new List<DeclarativeSceneObjectObservation>
        {
            new DeclarativeSceneObjectObservation(
                RootPath,
                true,
                1,
                Vector(1.8f, 0f),
                IdentityRotation(),
                One(),
                childNames,
                EmptyLineRenderer()),
        };
        objects.AddRange(Parts.Select(part => CreatePart(part)));
        return Observation(objects);
    }

    private static DeclarativeSceneObservation ReplaceObject(
        DeclarativeSceneObservation observation,
        DeclarativeSceneObjectObservation replacement)
    {
        DeclarativeSceneObjectObservation[] objects = observation.Objects
            .Select(value => value.Path == replacement.Path ? replacement : value)
            .ToArray();
        return Observation(objects);
    }

    private static DeclarativeSceneObjectObservation CreatePart(
        PartFixture part,
        DeclarativeVector3 localPosition = null,
        bool useWorldSpace = false,
        string materialPath = MaterialPath,
        DeclarativeVector3[] points = null)
    {
        return new DeclarativeSceneObjectObservation(
            RootPath + "/" + part.Name,
            true,
            2,
            localPosition ?? Vector(part.X, part.Y),
            IdentityRotation(),
            One(),
            Array.Empty<string>(),
            new DeclarativeLineRendererObservation(
                1,
                true,
                useWorldSpace,
                false,
                11,
                0.08f,
                0.08f,
                4,
                4,
                materialPath,
                points ?? Square(part.HalfSize)));
    }

    private static DeclarativeSceneObservation Observation(
        IEnumerable<DeclarativeSceneObjectObservation> objects)
    {
        return new DeclarativeSceneObservation(objects, new[] { MaterialPath });
    }

    private static DeclarativeSceneObjectObservation Resolve(
        DeclarativeSceneObservation observation,
        string path)
    {
        return observation.Objects.Single(value => value.Path == path);
    }

    private static PartFixture Part(string name)
    {
        return Parts.Single(value => value.Name == name);
    }

    private static DeclarativeLineRendererObservation EmptyLineRenderer()
    {
        return new DeclarativeLineRendererObservation(
            0,
            false,
            false,
            false,
            0,
            0f,
            0f,
            0,
            0,
            string.Empty,
            Array.Empty<DeclarativeVector3>());
    }

    private static DeclarativeVector3[] Square(float halfSize)
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

    private static DeclarativeVector3 Vector(float x, float y, float z = 0f)
    {
        return new DeclarativeVector3(x, y, z);
    }

    private static DeclarativeQuaternion IdentityRotation()
    {
        return new DeclarativeQuaternion(0f, 0f, 0f, 1f);
    }

    private static DeclarativeVector3 One()
    {
        return new DeclarativeVector3(1f, 1f, 1f);
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

    private readonly struct PartFixture
    {
        public PartFixture(string name, float x, float y, float halfSize)
        {
            Name = name;
            X = x;
            Y = y;
            HalfSize = halfSize;
        }

        public string Name { get; }
        public float X { get; }
        public float Y { get; }
        public float HalfSize { get; }
    }
}
