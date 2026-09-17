using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

internal readonly struct SquareCharacterVectorObservation
{
    public SquareCharacterVectorObservation(float x, float y, float z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    public float X { get; }
    public float Y { get; }
    public float Z { get; }
}

internal readonly struct SquareCharacterQuaternionObservation
{
    public SquareCharacterQuaternionObservation(float x, float y, float z, float w)
    {
        X = x;
        Y = y;
        Z = z;
        W = w;
    }

    public float X { get; }
    public float Y { get; }
    public float Z { get; }
    public float W { get; }
}

internal readonly struct SquareCharacterPartObservation
{
    public SquareCharacterPartObservation(
        string name,
        bool isUnique,
        string resolutionError,
        SquareCharacterVectorObservation localPosition,
        SquareCharacterQuaternionObservation localRotation,
        SquareCharacterVectorObservation localScale,
        bool isActive,
        int componentCount,
        int lineRendererCount,
        bool useWorldSpace,
        bool loop,
        bool lineRendererEnabled,
        int sortingOrder,
        float startWidth,
        float endWidth,
        int cornerVertices,
        int capVertices,
        string materialPath,
        SquareCharacterVectorObservation[] points)
    {
        Name = name ?? string.Empty;
        IsUnique = isUnique;
        ResolutionError = resolutionError ?? string.Empty;
        LocalPosition = localPosition;
        LocalRotation = localRotation;
        LocalScale = localScale;
        IsActive = isActive;
        ComponentCount = componentCount;
        LineRendererCount = lineRendererCount;
        UseWorldSpace = useWorldSpace;
        Loop = loop;
        LineRendererEnabled = lineRendererEnabled;
        SortingOrder = sortingOrder;
        StartWidth = startWidth;
        EndWidth = endWidth;
        CornerVertices = cornerVertices;
        CapVertices = capVertices;
        MaterialPath = materialPath ?? string.Empty;
        Points = points ?? Array.Empty<SquareCharacterVectorObservation>();
    }

    public string Name { get; }
    public bool IsUnique { get; }
    public string ResolutionError { get; }
    public SquareCharacterVectorObservation LocalPosition { get; }
    public SquareCharacterQuaternionObservation LocalRotation { get; }
    public SquareCharacterVectorObservation LocalScale { get; }
    public bool IsActive { get; }
    public int ComponentCount { get; }
    public int LineRendererCount { get; }
    public bool UseWorldSpace { get; }
    public bool Loop { get; }
    public bool LineRendererEnabled { get; }
    public int SortingOrder { get; }
    public float StartWidth { get; }
    public float EndWidth { get; }
    public int CornerVertices { get; }
    public int CapVertices { get; }
    public string MaterialPath { get; }
    public SquareCharacterVectorObservation[] Points { get; }
}

internal readonly struct SquareCharacterStructureObservation
{
    public SquareCharacterStructureObservation(
        bool hasUniqueRoot,
        string rootResolutionError,
        SquareCharacterVectorObservation rootLocalPosition,
        SquareCharacterQuaternionObservation rootLocalRotation,
        SquareCharacterVectorObservation rootLocalScale,
        bool rootIsActive,
        int rootComponentCount,
        string[] directChildNames,
        bool hasManagedMaterial,
        SquareCharacterPartObservation[] parts)
    {
        HasUniqueRoot = hasUniqueRoot;
        RootResolutionError = rootResolutionError ?? string.Empty;
        RootLocalPosition = rootLocalPosition;
        RootLocalRotation = rootLocalRotation;
        RootLocalScale = rootLocalScale;
        RootIsActive = rootIsActive;
        RootComponentCount = rootComponentCount;
        DirectChildNames = directChildNames ?? Array.Empty<string>();
        HasManagedMaterial = hasManagedMaterial;
        Parts = parts ?? Array.Empty<SquareCharacterPartObservation>();
    }

    public bool HasUniqueRoot { get; }
    public string RootResolutionError { get; }
    public SquareCharacterVectorObservation RootLocalPosition { get; }
    public SquareCharacterQuaternionObservation RootLocalRotation { get; }
    public SquareCharacterVectorObservation RootLocalScale { get; }
    public bool RootIsActive { get; }
    public int RootComponentCount { get; }
    public string[] DirectChildNames { get; }
    public bool HasManagedMaterial { get; }
    public SquareCharacterPartObservation[] Parts { get; }
}

internal readonly struct SquareCharacterPartSpecification
{
    public SquareCharacterPartSpecification(string name, string checkName, float x, float y, float halfSize)
    {
        Name = name;
        CheckName = checkName;
        LocalPosition = new SquareCharacterVectorObservation(x, y, 0f);
        HalfSize = halfSize;
    }

    public string Name { get; }
    public string CheckName { get; }
    public SquareCharacterVectorObservation LocalPosition { get; }
    public float HalfSize { get; }
}

internal static class SquareCharacterStructureGate
{
    private const float Tolerance = 0.0001f;
    private const string RootPath = "/SquareCharacter";

    internal static readonly SquareCharacterPartSpecification[] PartSpecifications =
    {
        new SquareCharacterPartSpecification("Head", "head", 0f, 1.35f, 0.45f),
        new SquareCharacterPartSpecification("Body", "body", 0f, 0.25f, 0.6f),
        new SquareCharacterPartSpecification("LeftArm", "left-arm", -0.95f, 0.25f, 0.35f),
        new SquareCharacterPartSpecification("RightArm", "right-arm", 0.95f, 0.25f, 0.35f),
        new SquareCharacterPartSpecification("LeftLeg", "left-leg", -0.38f, -0.9f, 0.38f),
        new SquareCharacterPartSpecification("RightLeg", "right-leg", 0.38f, -0.9f, 0.38f),
    };

    public static HarnessGateResult Evaluate(string runId, SquareCharacterStructureObservation observation)
    {
        HarnessGateResultBuilder builder = new HarnessGateResultBuilder(
            runId,
            SquareCharacterGateRunner.Profile,
            SquareCharacterGateRunner.ProfileVersion);

        AddRootCheck(builder, observation);
        AddExactPartsCheck(builder, observation.DirectChildNames);

        foreach (SquareCharacterPartSpecification specification in PartSpecifications)
        {
            SquareCharacterPartObservation part = ResolvePart(observation.Parts, specification.Name);
            AddLayoutCheck(builder, specification, part);
            AddShapeCheck(builder, specification, part);
            AddMaterialCheck(builder, specification, part, observation.HasManagedMaterial);
        }

        return builder.Build();
    }

    private static void AddRootCheck(
        HarnessGateResultBuilder builder,
        SquareCharacterStructureObservation observation)
    {
        SquareCharacterVectorObservation expectedPosition = new SquareCharacterVectorObservation(1.8f, 0f, 0f);
        bool matches = observation.HasUniqueRoot &&
                       observation.RootIsActive &&
                       observation.RootComponentCount == 1 &&
                       Approximately(observation.RootLocalPosition, expectedPosition) &&
                       IsIdentity(observation.RootLocalRotation) &&
                       IsOne(observation.RootLocalScale);
        const string expected = "unique active /SquareCharacter; Transform only; localPosition=(1.8, 0, 0); identity rotation/scale";
        string actual = observation.HasUniqueRoot
            ? $"active={observation.RootIsActive}; components={observation.RootComponentCount}; " +
              $"localPosition={Format(observation.RootLocalPosition)}; " +
              $"localRotation={Format(observation.RootLocalRotation)}; " +
              $"localScale={Format(observation.RootLocalScale)}"
            : DescribeMissing(observation.RootResolutionError);

        if (matches)
        {
            builder.AddPass("scene.square-character.root", "SquareCharacter root matches the required transform.", expected, actual);
        }
        else
        {
            builder.AddFailure("scene.square-character.root", "SquareCharacter root is missing, ambiguous, or has the wrong transform.", expected, actual);
        }
    }

    private static void AddExactPartsCheck(HarnessGateResultBuilder builder, IReadOnlyList<string> actualNames)
    {
        string[] expectedNames = PartSpecifications.Select(specification => specification.Name).ToArray();
        bool matches = actualNames.Count == expectedNames.Length &&
                       expectedNames.All(expected => actualNames.Count(actual => actual == expected) == 1);
        string expected = string.Join(", ", expectedNames);
        string actual = actualNames.Count == 0
            ? "none"
            : string.Join(", ", actualNames.OrderBy(name => name, StringComparer.Ordinal));

        if (matches)
        {
            builder.AddPass("scene.square-character.exact-parts", "SquareCharacter has exactly the six required direct children.", expected, actual);
        }
        else
        {
            builder.AddFailure("scene.square-character.exact-parts", "SquareCharacter must have exactly the six required direct children and no extras.", expected, actual);
        }
    }

    private static void AddLayoutCheck(
        HarnessGateResultBuilder builder,
        SquareCharacterPartSpecification specification,
        SquareCharacterPartObservation part)
    {
        string checkId = $"scene.square-character.{specification.CheckName}.layout";
        bool matches = part.IsUnique &&
                       part.IsActive &&
                       part.ComponentCount == 2 &&
                       Approximately(part.LocalPosition, specification.LocalPosition) &&
                       IsIdentity(part.LocalRotation) &&
                       IsOne(part.LocalScale);
        string expected = $"active; Transform + LineRenderer only; localPosition={Format(specification.LocalPosition)}; identity rotation/scale";
        string actual = part.IsUnique
            ? $"active={part.IsActive}; components={part.ComponentCount}; " +
              $"localPosition={Format(part.LocalPosition)}; localRotation={Format(part.LocalRotation)}; " +
              $"localScale={Format(part.LocalScale)}"
            : DescribeMissing(part.ResolutionError);

        if (matches)
        {
            builder.AddPass(checkId, $"{specification.Name} has the required local layout.", expected, actual);
        }
        else
        {
            builder.AddFailure(checkId, $"{specification.Name} is missing, ambiguous, or has the wrong local layout.", expected, actual);
        }
    }

    private static void AddShapeCheck(
        HarnessGateResultBuilder builder,
        SquareCharacterPartSpecification specification,
        SquareCharacterPartObservation part)
    {
        string checkId = $"scene.square-character.{specification.CheckName}.shape";
        SquareCharacterVectorObservation[] expectedPoints = CreateSquare(specification.HalfSize);
        bool pointsMatch = part.Points.Length == expectedPoints.Length;
        if (pointsMatch)
        {
            for (int index = 0; index < expectedPoints.Length; index++)
            {
                if (!Approximately(part.Points[index], expectedPoints[index]))
                {
                    pointsMatch = false;
                    break;
                }
            }
        }

        bool matches = part.IsUnique &&
                       part.LineRendererCount == 1 &&
                       part.LineRendererEnabled &&
                       !part.UseWorldSpace &&
                       !part.Loop &&
                       part.SortingOrder == 11 &&
                       Approximately(part.StartWidth, 0.08f) &&
                       Approximately(part.EndWidth, 0.08f) &&
                       part.CornerVertices == 4 &&
                       part.CapVertices == 4 &&
                       pointsMatch;
        string expected = $"one enabled local-space non-looping LineRenderer; sortingOrder=11; " +
                          $"start/end width=0.08; corner/cap vertices=4; points={Format(expectedPoints)}";
        string actual = part.IsUnique
            ? $"lineRenderers={part.LineRendererCount.ToString(CultureInfo.InvariantCulture)}; " +
              $"enabled={part.LineRendererEnabled}; useWorldSpace={part.UseWorldSpace}; loop={part.Loop}; " +
              $"sortingOrder={part.SortingOrder}; startWidth={Format(part.StartWidth)}; " +
              $"endWidth={Format(part.EndWidth)}; cornerVertices={part.CornerVertices}; " +
              $"capVertices={part.CapVertices}; points={Format(part.Points)}"
            : DescribeMissing(part.ResolutionError);

        if (matches)
        {
            builder.AddPass(checkId, $"{specification.Name} is the required closed axis-aligned square.", expected, actual);
        }
        else
        {
            builder.AddFailure(checkId, $"{specification.Name} does not have the required square LineRenderer geometry.", expected, actual);
        }
    }

    private static void AddMaterialCheck(
        HarnessGateResultBuilder builder,
        SquareCharacterPartSpecification specification,
        SquareCharacterPartObservation part,
        bool hasManagedMaterial)
    {
        string checkId = $"scene.square-character.{specification.CheckName}.material";
        bool matches = part.IsUnique &&
                       part.LineRendererCount == 1 &&
                       hasManagedMaterial &&
                       part.MaterialPath == HarnessBeaconRecipe.MaterialPath;
        string actual = !part.IsUnique
            ? DescribeMissing(part.ResolutionError)
            : !hasManagedMaterial
                ? "managed material asset missing"
                : string.IsNullOrWhiteSpace(part.MaterialPath) ? "missing" : part.MaterialPath;

        if (matches)
        {
            builder.AddPass(checkId, $"{specification.Name} uses the managed material.", HarnessBeaconRecipe.MaterialPath, actual);
        }
        else
        {
            builder.AddFailure(checkId, $"{specification.Name} does not use the managed material.", HarnessBeaconRecipe.MaterialPath, actual);
        }
    }

    private static SquareCharacterPartObservation ResolvePart(
        IReadOnlyList<SquareCharacterPartObservation> parts,
        string name)
    {
        for (int index = 0; index < parts.Count; index++)
        {
            if (parts[index].Name == name)
            {
                return parts[index];
            }
        }

        return new SquareCharacterPartObservation(
            name,
            false,
            $"GameObject is missing: {RootPath}/{name}",
            default,
            default,
            default,
            false,
            0,
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
            Array.Empty<SquareCharacterVectorObservation>());
    }

    private static SquareCharacterVectorObservation[] CreateSquare(float halfSize)
    {
        return new[]
        {
            new SquareCharacterVectorObservation(-halfSize, -halfSize, 0f),
            new SquareCharacterVectorObservation(-halfSize, halfSize, 0f),
            new SquareCharacterVectorObservation(halfSize, halfSize, 0f),
            new SquareCharacterVectorObservation(halfSize, -halfSize, 0f),
            new SquareCharacterVectorObservation(-halfSize, -halfSize, 0f),
        };
    }

    private static bool Approximately(
        SquareCharacterVectorObservation left,
        SquareCharacterVectorObservation right)
    {
        return Math.Abs(left.X - right.X) <= Tolerance &&
               Math.Abs(left.Y - right.Y) <= Tolerance &&
               Math.Abs(left.Z - right.Z) <= Tolerance;
    }

    private static bool IsIdentity(SquareCharacterQuaternionObservation value)
    {
        bool direct = Math.Abs(value.X) <= Tolerance &&
                      Math.Abs(value.Y) <= Tolerance &&
                      Math.Abs(value.Z) <= Tolerance &&
                      Math.Abs(value.W - 1f) <= Tolerance;
        bool negated = Math.Abs(value.X) <= Tolerance &&
                       Math.Abs(value.Y) <= Tolerance &&
                       Math.Abs(value.Z) <= Tolerance &&
                       Math.Abs(value.W + 1f) <= Tolerance;
        return direct || negated;
    }

    private static bool IsOne(SquareCharacterVectorObservation value)
    {
        return Approximately(value, new SquareCharacterVectorObservation(1f, 1f, 1f));
    }

    private static bool Approximately(float left, float right)
    {
        return Math.Abs(left - right) <= Tolerance;
    }

    private static string Format(SquareCharacterVectorObservation value)
    {
        return $"({Format(value.X)}, {Format(value.Y)}, {Format(value.Z)})";
    }

    private static string Format(SquareCharacterQuaternionObservation value)
    {
        return $"({Format(value.X)}, {Format(value.Y)}, {Format(value.Z)}, {Format(value.W)})";
    }

    private static string Format(IReadOnlyList<SquareCharacterVectorObservation> points)
    {
        return "[" + string.Join(", ", points.Select(Format)) + "]";
    }

    private static string Format(float value)
    {
        return value.ToString("0.####", CultureInfo.InvariantCulture);
    }

    private static string DescribeMissing(string error)
    {
        return string.IsNullOrWhiteSpace(error) ? "missing or ambiguous" : error;
    }
}
