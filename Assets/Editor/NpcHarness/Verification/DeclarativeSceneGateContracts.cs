using System;
using System.Collections.Generic;

[Serializable]
internal sealed class DeclarativeSceneGateManifest
{
    public int schemaVersion = 1;
    public string profile = string.Empty;
    public int profileVersion = 1;
    public string scenePath = string.Empty;
    public float tolerance = 0.0001f;
    public DeclarativeSceneGateCheck[] checks = Array.Empty<DeclarativeSceneGateCheck>();
}

[Serializable]
internal sealed class DeclarativeSceneGateCheck
{
    public string id = string.Empty;
    public string type = string.Empty;
    public string path = string.Empty;
    public string passMessage = string.Empty;
    public string failMessage = string.Empty;
    public string expected = string.Empty;
    public bool active;
    public int componentCount;
    public DeclarativeVector3 localPosition;
    public DeclarativeQuaternion localRotation;
    public DeclarativeVector3 localScale;
    public string[] children = Array.Empty<string>();
    public bool enabled;
    public bool useWorldSpace;
    public bool loop;
    public int sortingOrder;
    public float startWidth;
    public float endWidth;
    public int cornerVertices;
    public int capVertices;
    public DeclarativeVector3[] points = Array.Empty<DeclarativeVector3>();
    public string materialPath = string.Empty;
}

[Serializable]
internal sealed class DeclarativeVector3
{
    public float x;
    public float y;
    public float z;

    public DeclarativeVector3()
    {
    }

    public DeclarativeVector3(float x, float y, float z)
    {
        this.x = x;
        this.y = y;
        this.z = z;
    }
}

[Serializable]
internal sealed class DeclarativeQuaternion
{
    public float x;
    public float y;
    public float z;
    public float w = 1f;

    public DeclarativeQuaternion()
    {
    }

    public DeclarativeQuaternion(float x, float y, float z, float w)
    {
        this.x = x;
        this.y = y;
        this.z = z;
        this.w = w;
    }
}

internal sealed class DeclarativeLineRendererObservation
{
    public DeclarativeLineRendererObservation(
        int count,
        bool enabled,
        bool useWorldSpace,
        bool loop,
        int sortingOrder,
        float startWidth,
        float endWidth,
        int cornerVertices,
        int capVertices,
        string materialPath,
        DeclarativeVector3[] points)
    {
        Count = count;
        Enabled = enabled;
        UseWorldSpace = useWorldSpace;
        Loop = loop;
        SortingOrder = sortingOrder;
        StartWidth = startWidth;
        EndWidth = endWidth;
        CornerVertices = cornerVertices;
        CapVertices = capVertices;
        MaterialPath = materialPath ?? string.Empty;
        Points = points ?? Array.Empty<DeclarativeVector3>();
    }

    public int Count { get; }
    public bool Enabled { get; }
    public bool UseWorldSpace { get; }
    public bool Loop { get; }
    public int SortingOrder { get; }
    public float StartWidth { get; }
    public float EndWidth { get; }
    public int CornerVertices { get; }
    public int CapVertices { get; }
    public string MaterialPath { get; }
    public DeclarativeVector3[] Points { get; }
}

internal sealed class DeclarativeSceneObjectObservation
{
    public DeclarativeSceneObjectObservation(
        string path,
        bool active,
        int componentCount,
        DeclarativeVector3 localPosition,
        DeclarativeQuaternion localRotation,
        DeclarativeVector3 localScale,
        string[] directChildNames,
        DeclarativeLineRendererObservation lineRenderer)
    {
        Path = path ?? string.Empty;
        Active = active;
        ComponentCount = componentCount;
        LocalPosition = localPosition ?? new DeclarativeVector3();
        LocalRotation = localRotation ?? new DeclarativeQuaternion();
        LocalScale = localScale ?? new DeclarativeVector3();
        DirectChildNames = directChildNames ?? Array.Empty<string>();
        LineRenderer = lineRenderer ?? new DeclarativeLineRendererObservation(
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

    public string Path { get; }
    public bool Active { get; }
    public int ComponentCount { get; }
    public DeclarativeVector3 LocalPosition { get; }
    public DeclarativeQuaternion LocalRotation { get; }
    public DeclarativeVector3 LocalScale { get; }
    public string[] DirectChildNames { get; }
    public DeclarativeLineRendererObservation LineRenderer { get; }
}

internal sealed class DeclarativeSceneObservation
{
    private readonly List<DeclarativeSceneObjectObservation> _objects;
    private readonly HashSet<string> _existingAssetPaths;

    public DeclarativeSceneObservation(
        IEnumerable<DeclarativeSceneObjectObservation> objects,
        IEnumerable<string> existingAssetPaths)
    {
        _objects = objects == null
            ? new List<DeclarativeSceneObjectObservation>()
            : new List<DeclarativeSceneObjectObservation>(objects);
        _existingAssetPaths = existingAssetPaths == null
            ? new HashSet<string>(StringComparer.Ordinal)
            : new HashSet<string>(existingAssetPaths, StringComparer.Ordinal);
    }

    public IReadOnlyList<DeclarativeSceneObjectObservation> Objects => _objects;

    public bool HasAsset(string assetPath)
    {
        return _existingAssetPaths.Contains(assetPath);
    }
}

internal static class DeclarativeSceneGateCheckType
{
    public const string ObjectLayout = "object-layout";
    public const string ExactChildren = "exact-children";
    public const string LineRendererShape = "line-renderer-shape";
    public const string LineRendererMaterial = "line-renderer-material";
}
