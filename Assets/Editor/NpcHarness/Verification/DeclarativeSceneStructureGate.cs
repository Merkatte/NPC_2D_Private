using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

internal static class DeclarativeSceneStructureGate
{
    public static HarnessGateResult Evaluate(
        string runId,
        DeclarativeSceneGateManifest manifest,
        DeclarativeSceneObservation observation)
    {
        DeclarativeSceneGateManifestLoader.Validate(manifest, "in-memory manifest");
        if (observation == null)
        {
            throw new ArgumentNullException(nameof(observation));
        }

        HarnessGateResultBuilder builder = new HarnessGateResultBuilder(
            runId,
            manifest.profile,
            manifest.profileVersion);
        foreach (DeclarativeSceneGateCheck check in manifest.checks)
        {
            EvaluateCheck(builder, manifest.tolerance, check, observation);
        }

        return builder.Build();
    }

    private static void EvaluateCheck(
        HarnessGateResultBuilder builder,
        float tolerance,
        DeclarativeSceneGateCheck check,
        DeclarativeSceneObservation observation)
    {
        switch (check.type)
        {
            case DeclarativeSceneGateCheckType.ObjectLayout:
                EvaluateObjectLayout(builder, tolerance, check, observation.Objects);
                return;
            case DeclarativeSceneGateCheckType.ExactChildren:
                EvaluateExactChildren(builder, check, observation.Objects);
                return;
            case DeclarativeSceneGateCheckType.LineRendererShape:
                EvaluateLineRendererShape(builder, tolerance, check, observation.Objects);
                return;
            case DeclarativeSceneGateCheckType.LineRendererMaterial:
                EvaluateLineRendererMaterial(builder, check, observation);
                return;
            default:
                throw new InvalidOperationException(
                    $"Unsupported declarative scene gate check type: {check.type}");
        }
    }

    private static void EvaluateObjectLayout(
        HarnessGateResultBuilder builder,
        float tolerance,
        DeclarativeSceneGateCheck check,
        IReadOnlyList<DeclarativeSceneObjectObservation> objects)
    {
        Resolution resolution = Resolve(objects, check.path);
        bool matches = resolution.IsUnique &&
                       resolution.Object.Active == check.active &&
                       resolution.Object.ComponentCount == check.componentCount &&
                       Approximately(resolution.Object.LocalPosition, check.localPosition, tolerance) &&
                       Approximately(resolution.Object.LocalRotation, check.localRotation, tolerance) &&
                       Approximately(resolution.Object.LocalScale, check.localScale, tolerance);
        string actual = resolution.IsUnique
            ? $"active={resolution.Object.Active}; components={resolution.Object.ComponentCount}; " +
              $"localPosition={Format(resolution.Object.LocalPosition)}; " +
              $"localRotation={Format(resolution.Object.LocalRotation)}; " +
              $"localScale={Format(resolution.Object.LocalScale)}"
            : resolution.Error;

        AddResult(builder, check, matches, actual);
    }

    private static void EvaluateExactChildren(
        HarnessGateResultBuilder builder,
        DeclarativeSceneGateCheck check,
        IReadOnlyList<DeclarativeSceneObjectObservation> objects)
    {
        Resolution resolution = Resolve(objects, check.path);
        IReadOnlyList<string> actualNames = resolution.IsUnique
            ? resolution.Object.DirectChildNames
            : Array.Empty<string>();
        bool matches = resolution.IsUnique &&
                       actualNames.Count == check.children.Length &&
                       check.children.All(expected => actualNames.Count(actual => actual == expected) == 1);
        string actual = actualNames.Count == 0
            ? "none"
            : string.Join(", ", actualNames.OrderBy(name => name, StringComparer.Ordinal));

        AddResult(builder, check, matches, actual);
    }

    private static void EvaluateLineRendererShape(
        HarnessGateResultBuilder builder,
        float tolerance,
        DeclarativeSceneGateCheck check,
        IReadOnlyList<DeclarativeSceneObjectObservation> objects)
    {
        Resolution resolution = Resolve(objects, check.path);
        DeclarativeLineRendererObservation lineRenderer = resolution.IsUnique
            ? resolution.Object.LineRenderer
            : null;
        bool matches = resolution.IsUnique &&
                       lineRenderer.Count == 1 &&
                       lineRenderer.Enabled == check.enabled &&
                       lineRenderer.UseWorldSpace == check.useWorldSpace &&
                       lineRenderer.Loop == check.loop &&
                       lineRenderer.SortingOrder == check.sortingOrder &&
                       Approximately(lineRenderer.StartWidth, check.startWidth, tolerance) &&
                       Approximately(lineRenderer.EndWidth, check.endWidth, tolerance) &&
                       lineRenderer.CornerVertices == check.cornerVertices &&
                       lineRenderer.CapVertices == check.capVertices &&
                       Approximately(lineRenderer.Points, check.points, tolerance);
        string actual = resolution.IsUnique
            ? $"lineRenderers={lineRenderer.Count.ToString(CultureInfo.InvariantCulture)}; " +
              $"enabled={lineRenderer.Enabled}; useWorldSpace={lineRenderer.UseWorldSpace}; " +
              $"loop={lineRenderer.Loop}; sortingOrder={lineRenderer.SortingOrder}; " +
              $"startWidth={Format(lineRenderer.StartWidth)}; endWidth={Format(lineRenderer.EndWidth)}; " +
              $"cornerVertices={lineRenderer.CornerVertices}; capVertices={lineRenderer.CapVertices}; " +
              $"points={Format(lineRenderer.Points)}"
            : resolution.Error;

        AddResult(builder, check, matches, actual);
    }

    private static void EvaluateLineRendererMaterial(
        HarnessGateResultBuilder builder,
        DeclarativeSceneGateCheck check,
        DeclarativeSceneObservation observation)
    {
        Resolution resolution = Resolve(observation.Objects, check.path);
        DeclarativeLineRendererObservation lineRenderer = resolution.IsUnique
            ? resolution.Object.LineRenderer
            : null;
        bool hasMaterialAsset = observation.HasAsset(check.materialPath);
        bool matches = resolution.IsUnique &&
                       lineRenderer.Count == 1 &&
                       hasMaterialAsset &&
                       lineRenderer.MaterialPath == check.materialPath;
        string actual = !resolution.IsUnique
            ? resolution.Error
            : !hasMaterialAsset
                ? "managed material asset missing"
                : string.IsNullOrWhiteSpace(lineRenderer.MaterialPath) ? "missing" : lineRenderer.MaterialPath;

        AddResult(builder, check, matches, actual);
    }

    private static void AddResult(
        HarnessGateResultBuilder builder,
        DeclarativeSceneGateCheck check,
        bool success,
        string actual)
    {
        if (success)
        {
            builder.AddPass(check.id, check.passMessage, check.expected, actual);
        }
        else
        {
            builder.AddFailure(check.id, check.failMessage, check.expected, actual);
        }
    }

    private static Resolution Resolve(
        IReadOnlyList<DeclarativeSceneObjectObservation> objects,
        string path)
    {
        DeclarativeSceneObjectObservation match = null;
        int count = 0;
        for (int index = 0; index < objects.Count; index++)
        {
            if (objects[index].Path != path)
            {
                continue;
            }

            match = objects[index];
            count++;
        }

        if (count == 1)
        {
            return new Resolution(match, string.Empty);
        }

        string error = count == 0
            ? $"GameObject is missing: {path}"
            : $"Hierarchy path is ambiguous because duplicate objects exist: {path}";
        return new Resolution(null, error);
    }

    private static bool Approximately(DeclarativeVector3 left, DeclarativeVector3 right, float tolerance)
    {
        return Approximately(left.x, right.x, tolerance) &&
               Approximately(left.y, right.y, tolerance) &&
               Approximately(left.z, right.z, tolerance);
    }

    private static bool Approximately(
        DeclarativeQuaternion left,
        DeclarativeQuaternion right,
        float tolerance)
    {
        bool direct = Approximately(left.x, right.x, tolerance) &&
                      Approximately(left.y, right.y, tolerance) &&
                      Approximately(left.z, right.z, tolerance) &&
                      Approximately(left.w, right.w, tolerance);
        bool negated = Approximately(left.x, -right.x, tolerance) &&
                       Approximately(left.y, -right.y, tolerance) &&
                       Approximately(left.z, -right.z, tolerance) &&
                       Approximately(left.w, -right.w, tolerance);
        return direct || negated;
    }

    private static bool Approximately(
        IReadOnlyList<DeclarativeVector3> left,
        IReadOnlyList<DeclarativeVector3> right,
        float tolerance)
    {
        if (left.Count != right.Count)
        {
            return false;
        }

        for (int index = 0; index < left.Count; index++)
        {
            if (!Approximately(left[index], right[index], tolerance))
            {
                return false;
            }
        }

        return true;
    }

    private static bool Approximately(float left, float right, float tolerance)
    {
        return Math.Abs(left - right) <= tolerance;
    }

    private static string Format(DeclarativeVector3 value)
    {
        return $"({Format(value.x)}, {Format(value.y)}, {Format(value.z)})";
    }

    private static string Format(DeclarativeQuaternion value)
    {
        return $"({Format(value.x)}, {Format(value.y)}, {Format(value.z)}, {Format(value.w)})";
    }

    private static string Format(IReadOnlyList<DeclarativeVector3> points)
    {
        return "[" + string.Join(", ", points.Select(Format)) + "]";
    }

    private static string Format(float value)
    {
        return value.ToString("0.####", CultureInfo.InvariantCulture);
    }

    private readonly struct Resolution
    {
        public Resolution(DeclarativeSceneObjectObservation value, string error)
        {
            Object = value;
            Error = error;
        }

        public DeclarativeSceneObjectObservation Object { get; }
        public string Error { get; }
        public bool IsUnique => Object != null;
    }
}
