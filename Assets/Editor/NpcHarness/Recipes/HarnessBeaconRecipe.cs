using System;

internal static class HarnessBeaconRecipe
{
    public const string ScenePath = "Assets/TestOnly/HarnessTest.unity";
    public const string ScriptPath = "Assets/TestOnly/HarnessTest.cs";
    public const string MaterialPath = "Assets/TestOnly/HarnessBeaconLine.mat";
    public const string SuccessMessage = "HarnessSuccess";

    public static HarnessJob Create(string jobId)
    {
        return new HarnessJob
        {
            schemaVersion = 1,
            jobId = jobId,
            steps = new[]
            {
                new HarnessStep
                {
                    id = "write-script",
                    tool = "WriteCSharpScript",
                    assetPath = ScriptPath,
                    source = CreateHarnessTestSource(),
                },
                new HarnessStep
                {
                    id = "ensure-scene",
                    tool = "EnsureScene",
                    scenePath = ScenePath,
                },
                new HarnessStep
                {
                    id = "ensure-camera-object",
                    tool = "EnsureGameObject",
                    scenePath = ScenePath,
                    parentPath = "/",
                    name = "Main Camera",
                    active = true,
                },
                new HarnessStep
                {
                    id = "set-camera-transform",
                    tool = "SetTransform",
                    scenePath = ScenePath,
                    targetPath = "/Main Camera",
                    position = Vector(0f, 0f, -10f),
                    rotation = Vector(0f, 0f, 0f),
                    scale = Vector(1f, 1f, 1f),
                },
                new HarnessStep
                {
                    id = "ensure-camera-component",
                    tool = "EnsureComponent",
                    scenePath = ScenePath,
                    targetPath = "/Main Camera",
                    componentTypeId = "Camera",
                },
                new HarnessStep
                {
                    id = "configure-camera",
                    tool = "ConfigureCamera",
                    scenePath = ScenePath,
                    targetPath = "/Main Camera",
                    tag = "MainCamera",
                    orthographic = true,
                    orthographicSize = 2.5f,
                    backgroundColor = Color(0.06f, 0.07f, 0.11f, 1f),
                },
                new HarnessStep
                {
                    id = "ensure-beacon-object",
                    tool = "EnsureGameObject",
                    scenePath = ScenePath,
                    parentPath = "/",
                    name = "HarnessBeacon",
                    active = true,
                },
                new HarnessStep
                {
                    id = "set-beacon-transform",
                    tool = "SetTransform",
                    scenePath = ScenePath,
                    targetPath = "/HarnessBeacon",
                    position = Vector(0f, 0f, 0f),
                    rotation = Vector(0f, 0f, 0f),
                    scale = Vector(1f, 1f, 1f),
                },
                new HarnessStep
                {
                    id = "ensure-harness-component",
                    tool = "EnsureComponent",
                    scenePath = ScenePath,
                    targetPath = "/HarnessBeacon",
                    componentTypeId = "HarnessTest",
                },
                new HarnessStep
                {
                    id = "ensure-line-renderer",
                    tool = "EnsureComponent",
                    scenePath = ScenePath,
                    targetPath = "/HarnessBeacon",
                    componentTypeId = "LineRenderer",
                },
                new HarnessStep
                {
                    id = "ensure-line-material",
                    tool = "EnsureMaterial",
                    assetPath = MaterialPath,
                    shader = "Sprites/Default",
                    color = Color(1f, 1f, 1f, 1f),
                },
                new HarnessStep
                {
                    id = "configure-arrow",
                    tool = "ConfigureLineRenderer",
                    scenePath = ScenePath,
                    targetPath = "/HarnessBeacon",
                    materialPath = MaterialPath,
                    useWorldSpace = false,
                    loop = false,
                    width = 0.2f,
                    capVertices = 4,
                    cornerVertices = 4,
                    sortingOrder = 10,
                    color = Color(1f, 0.72f, 0.08f, 1f),
                    points = new[]
                    {
                        Vector(0f, -1.25f, 0f),
                        Vector(0f, 1.2f, 0f),
                        Vector(-0.7f, 0.5f, 0f),
                        Vector(0f, 1.2f, 0f),
                        Vector(0.7f, 0.5f, 0f),
                    },
                },
                new HarnessStep
                {
                    id = "save-scene",
                    tool = "SaveScene",
                    scenePath = ScenePath,
                },
            },
        };
    }

    public static string CreateHarnessTestSource()
    {
        return string.Join("\n", new[]
        {
            "using UnityEngine;",
            string.Empty,
            "[DisallowMultipleComponent]",
            "public sealed class HarnessTest : MonoBehaviour",
            "{",
            "    private const string SuccessMessage = \"HarnessSuccess\";",
            string.Empty,
            "    private void Start()",
            "    {",
            "        Debug.Log(SuccessMessage, this);",
            "    }",
            "}",
            string.Empty,
        });
    }

    private static HarnessVector3 Vector(float x, float y, float z)
    {
        return new HarnessVector3 { x = x, y = y, z = z };
    }

    private static HarnessColor Color(float r, float g, float b, float a)
    {
        return new HarnessColor { r = r, g = g, b = b, a = a };
    }
}
