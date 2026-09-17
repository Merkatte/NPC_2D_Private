using System;
using NUnit.Framework;

internal sealed class DeclarativeSceneGateManifestTests
{
    private const string ObjectLayoutCheckJson =
        "{\"id\":\"scene.root\",\"type\":\"object-layout\",\"path\":\"/Root\"," +
        "\"passMessage\":\"Root matches.\",\"failMessage\":\"Root does not match.\"," +
        "\"expected\":\"active root\",\"active\":true,\"componentCount\":1," +
        "\"localPosition\":{\"x\":0,\"y\":0,\"z\":0}," +
        "\"localRotation\":{\"x\":0,\"y\":0,\"z\":0,\"w\":1}," +
        "\"localScale\":{\"x\":1,\"y\":1,\"z\":1}}";

    [Test]
    public void Validate_MinimalObjectLayoutManifest_Succeeds()
    {
        Assert.DoesNotThrow(() => DeclarativeSceneGateManifestLoader.Validate(
            CreateManifest(CreateObjectLayoutCheck("scene.root")),
            "test manifest"));
    }

    [Test]
    public void Validate_DuplicateCheckIds_Throws()
    {
        DeclarativeSceneGateCheck first = CreateObjectLayoutCheck("scene.duplicate");
        DeclarativeSceneGateCheck second = CreateObjectLayoutCheck("scene.duplicate");

        Assert.Throws<InvalidOperationException>(() => DeclarativeSceneGateManifestLoader.Validate(
            CreateManifest(first, second),
            "test manifest"));
    }

    [Test]
    public void Validate_UnsupportedCheckType_Throws()
    {
        DeclarativeSceneGateCheck check = CreateObjectLayoutCheck("scene.unsupported");
        check.type = "arbitrary-reflection";

        Assert.Throws<InvalidOperationException>(() => DeclarativeSceneGateManifestLoader.Validate(
            CreateManifest(check),
            "test manifest"));
    }

    [Test]
    public void Validate_PathTraversal_Throws()
    {
        DeclarativeSceneGateCheck check = CreateObjectLayoutCheck("scene.escape");
        check.path = "/SquareCharacter/../Other";

        Assert.Throws<InvalidOperationException>(() => DeclarativeSceneGateManifestLoader.Validate(
            CreateManifest(check),
            "test manifest"));
    }

    [Test]
    public void ParseAndValidateJson_ValidManifest_Succeeds()
    {
        Assert.DoesNotThrow(() => DeclarativeSceneGateManifestLoader.ParseAndValidateJson(
            CreateJson(ObjectLayoutCheckJson),
            "test manifest"));
    }

    [Test]
    public void ParseAndValidateJson_MissingScalarField_Throws()
    {
        string check = ObjectLayoutCheckJson.Replace("\"active\":true,", string.Empty);

        Assert.Throws<InvalidOperationException>(() =>
            DeclarativeSceneGateManifestLoader.ParseAndValidateJson(CreateJson(check), "test manifest"));
    }

    [Test]
    public void ParseAndValidateJson_UnknownField_Throws()
    {
        string check = ObjectLayoutCheckJson.Replace(
            "\"componentCount\":1",
            "\"componentCount\":1,\"unexpected\":true");

        Assert.Throws<InvalidOperationException>(() =>
            DeclarativeSceneGateManifestLoader.ParseAndValidateJson(CreateJson(check), "test manifest"));
    }

    [Test]
    public void ParseAndValidateJson_MalformedJson_Throws()
    {
        Assert.Throws<InvalidOperationException>(() =>
            DeclarativeSceneGateManifestLoader.ParseAndValidateJson("{", "test manifest"));
    }

    [Test]
    public void ParseAndValidateJson_DuplicateCheckIds_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => DeclarativeSceneGateManifestLoader.ParseAndValidateJson(
            CreateJson(ObjectLayoutCheckJson + "," + ObjectLayoutCheckJson),
            "test manifest"));
    }

    [Test]
    public void ParseAndValidateJson_UnsupportedCheckType_Throws()
    {
        string check = ObjectLayoutCheckJson.Replace("object-layout", "arbitrary-reflection");

        Assert.Throws<InvalidOperationException>(() =>
            DeclarativeSceneGateManifestLoader.ParseAndValidateJson(CreateJson(check), "test manifest"));
    }

    [Test]
    public void ParseAndValidateJson_PathTraversal_Throws()
    {
        string check = ObjectLayoutCheckJson.Replace("/Root", "/Root/../Other");

        Assert.Throws<InvalidOperationException>(() =>
            DeclarativeSceneGateManifestLoader.ParseAndValidateJson(CreateJson(check), "test manifest"));
    }

    [Test]
    public void ParseAndValidateJson_DuplicateExpectedChildren_Throws()
    {
        const string check =
            "{\"id\":\"scene.children\",\"type\":\"exact-children\",\"path\":\"/Root\"," +
            "\"passMessage\":\"Children match.\",\"failMessage\":\"Children do not match.\"," +
            "\"expected\":\"A, A\",\"children\":[\"A\",\"A\"]}";

        Assert.Throws<InvalidOperationException>(() =>
            DeclarativeSceneGateManifestLoader.ParseAndValidateJson(CreateJson(check), "test manifest"));
    }

    private static DeclarativeSceneGateManifest CreateManifest(params DeclarativeSceneGateCheck[] checks)
    {
        return new DeclarativeSceneGateManifest
        {
            profile = "Test.Declarative.Structure",
            profileVersion = 1,
            scenePath = "Assets/TestOnly/Test.unity",
            tolerance = 0.0001f,
            checks = checks,
        };
    }

    private static string CreateJson(string checks)
    {
        return "{\"schemaVersion\":1,\"profile\":\"Test.Declarative.Structure\"," +
               "\"profileVersion\":1,\"scenePath\":\"Assets/TestOnly/Test.unity\"," +
               "\"tolerance\":0.0001,\"checks\":[" + checks + "]}";
    }

    private static DeclarativeSceneGateCheck CreateObjectLayoutCheck(string id)
    {
        return new DeclarativeSceneGateCheck
        {
            id = id,
            type = DeclarativeSceneGateCheckType.ObjectLayout,
            path = "/Root",
            passMessage = "Root matches.",
            failMessage = "Root does not match.",
            expected = "active root",
            active = true,
            componentCount = 1,
            localPosition = new DeclarativeVector3(0f, 0f, 0f),
            localRotation = new DeclarativeQuaternion(0f, 0f, 0f, 1f),
            localScale = new DeclarativeVector3(1f, 1f, 1f),
        };
    }
}
