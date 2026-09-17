using System.Globalization;

internal readonly struct HarnessBeaconStructureObservation
{
    public HarnessBeaconStructureObservation(
        bool hasBeacon,
        string beaconResolutionError,
        int harnessComponentCount,
        bool hasLineRenderer,
        int linePositionCount,
        string lineMaterialPath,
        bool hasManagedMaterial,
        bool hasMainCamera,
        string cameraResolutionError,
        bool hasCameraComponent,
        bool isOrthographic,
        float orthographicSize)
    {
        HasBeacon = hasBeacon;
        BeaconResolutionError = beaconResolutionError ?? string.Empty;
        HarnessComponentCount = harnessComponentCount;
        HasLineRenderer = hasLineRenderer;
        LinePositionCount = linePositionCount;
        LineMaterialPath = lineMaterialPath ?? string.Empty;
        HasManagedMaterial = hasManagedMaterial;
        HasMainCamera = hasMainCamera;
        CameraResolutionError = cameraResolutionError ?? string.Empty;
        HasCameraComponent = hasCameraComponent;
        IsOrthographic = isOrthographic;
        OrthographicSize = orthographicSize;
    }

    public bool HasBeacon { get; }
    public string BeaconResolutionError { get; }
    public int HarnessComponentCount { get; }
    public bool HasLineRenderer { get; }
    public int LinePositionCount { get; }
    public string LineMaterialPath { get; }
    public bool HasManagedMaterial { get; }
    public bool HasMainCamera { get; }
    public string CameraResolutionError { get; }
    public bool HasCameraComponent { get; }
    public bool IsOrthographic { get; }
    public float OrthographicSize { get; }
}

internal static class HarnessBeaconStructureGate
{
    private const string BeaconPath = "/HarnessBeacon";
    private const string CameraPath = "/Main Camera";

    public static HarnessGateResult Evaluate(
        string runId,
        HarnessBeaconStructureObservation observation)
    {
        HarnessGateResultBuilder builder = new HarnessGateResultBuilder(
            runId,
            HarnessBeaconGateRunner.Profile,
            HarnessBeaconGateRunner.ProfileVersion);

        if (!observation.HasBeacon)
        {
            builder.AddFailure(
                "scene.beacon-object",
                "HarnessBeacon object must exist exactly once.",
                BeaconPath,
                DescribeResolutionFailure(observation.BeaconResolutionError));
            return builder.Build();
        }

        builder.AddPass(
            "scene.beacon-object",
            "HarnessBeacon object exists exactly once.",
            BeaconPath,
            BeaconPath);

        if (observation.HarnessComponentCount == 1)
        {
            builder.AddPass(
                "scene.harness-component",
                "HarnessBeacon contains exactly one HarnessTest component.",
                "1",
                "1");
        }
        else
        {
            builder.AddFailure(
                "scene.harness-component",
                "HarnessBeacon must contain exactly one HarnessTest component.",
                "1",
                observation.HarnessComponentCount.ToString(CultureInfo.InvariantCulture));
        }

        if (observation.HasLineRenderer && observation.LinePositionCount == 5)
        {
            builder.AddPass(
                "scene.line-renderer",
                "HarnessBeacon arrow LineRenderer contains five points.",
                "component, 5 points",
                "component, 5 points");
        }
        else
        {
            string actual = observation.HasLineRenderer
                ? $"{observation.LinePositionCount.ToString(CultureInfo.InvariantCulture)} points"
                : "component missing";
            builder.AddFailure(
                "scene.line-renderer",
                "HarnessBeacon arrow LineRenderer is missing or incomplete.",
                "component, 5 points",
                actual);
        }

        if (observation.HasManagedMaterial &&
            observation.HasLineRenderer &&
            observation.LineMaterialPath == HarnessBeaconRecipe.MaterialPath)
        {
            builder.AddPass(
                "scene.line-material",
                "HarnessBeacon uses the managed line material.",
                HarnessBeaconRecipe.MaterialPath,
                HarnessBeaconRecipe.MaterialPath);
        }
        else
        {
            string actual = !observation.HasManagedMaterial
                ? "managed material asset missing"
                : string.IsNullOrWhiteSpace(observation.LineMaterialPath)
                    ? "missing"
                    : observation.LineMaterialPath;
            builder.AddFailure(
                "scene.line-material",
                "HarnessBeacon does not use the managed line material.",
                HarnessBeaconRecipe.MaterialPath,
                actual);
        }

        if (observation.HasMainCamera &&
            observation.HasCameraComponent &&
            observation.IsOrthographic &&
            HarnessValueUtility.Approximately(observation.OrthographicSize, 2.5f))
        {
            builder.AddPass(
                "scene.main-camera",
                "Harness test scene contains the configured orthographic Main Camera.",
                "orthographic, size 2.5",
                "orthographic, size 2.5");
        }
        else
        {
            builder.AddFailure(
                "scene.main-camera",
                "Harness test scene requires the configured orthographic Main Camera.",
                "orthographic, size 2.5",
                DescribeCamera(observation));
        }

        return builder.Build();
    }

    private static string DescribeResolutionFailure(string error)
    {
        return string.IsNullOrWhiteSpace(error) ? "missing" : error;
    }

    private static string DescribeCamera(HarnessBeaconStructureObservation observation)
    {
        if (!observation.HasMainCamera)
        {
            return DescribeResolutionFailure(observation.CameraResolutionError);
        }

        if (!observation.HasCameraComponent)
        {
            return "Camera component missing";
        }

        return $"orthographic={observation.IsOrthographic}, " +
               $"size={observation.OrthographicSize.ToString(CultureInfo.InvariantCulture)}";
    }
}
