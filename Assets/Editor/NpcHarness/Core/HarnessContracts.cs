using System;

[Serializable]
internal sealed class HarnessJob
{
    public int schemaVersion = 1;
    public string jobId = string.Empty;
    public HarnessStep[] steps = Array.Empty<HarnessStep>();
}

[Serializable]
internal sealed class HarnessStep
{
    public string id = string.Empty;
    public string tool = string.Empty;
    public string scenePath = string.Empty;
    public string parentPath = string.Empty;
    public string name = string.Empty;
    public string targetPath = string.Empty;
    public string componentTypeId = string.Empty;
    public string assetPath = string.Empty;
    public string shader = string.Empty;
    public string source = string.Empty;
    public string tag = string.Empty;
    public string materialPath = string.Empty;
    public bool active = true;
    public bool orthographic;
    public bool useWorldSpace;
    public bool loop;
    public float orthographicSize;
    public float width;
    public int sortingOrder;
    public int capVertices;
    public int cornerVertices;
    public HarnessVector3 position = new HarnessVector3();
    public HarnessVector3 rotation = new HarnessVector3();
    public HarnessVector3 scale = HarnessVector3.One;
    public HarnessColor color = HarnessColor.White;
    public HarnessColor backgroundColor = HarnessColor.Black;
    public HarnessVector3[] points = Array.Empty<HarnessVector3>();
}

[Serializable]
internal sealed class HarnessVector3
{
    public float x;
    public float y;
    public float z;

    public static HarnessVector3 One => new HarnessVector3 { x = 1f, y = 1f, z = 1f };
}

[Serializable]
internal sealed class HarnessColor
{
    public float r;
    public float g;
    public float b;
    public float a = 1f;

    public static HarnessColor Black => new HarnessColor { a = 1f };
    public static HarnessColor White => new HarnessColor { r = 1f, g = 1f, b = 1f, a = 1f };
}

[Serializable]
internal sealed class HarnessJobResult
{
    public bool success;
    public string state = HarnessRunState.Failed.ToString();
    public string message = string.Empty;
    public int nextStepIndex;
}

[Serializable]
internal sealed class HarnessCheckpoint
{
    public string jobId = string.Empty;
    public string jobPath = string.Empty;
    public string jobHash = string.Empty;
    public int nextStepIndex;
    public bool interactive;
    public bool allowOverwrite;
    public bool changedBeforeCheckpoint;
}

internal enum HarnessRunState
{
    Succeeded,
    NoChange,
    AwaitingCompilation,
    ValidationFailed,
    Failed,
}

internal readonly struct HarnessExecutionOptions
{
    public HarnessExecutionOptions(bool allowOverwrite, bool interactive)
    {
        AllowOverwrite = allowOverwrite;
        Interactive = interactive;
    }

    public bool AllowOverwrite { get; }
    public bool Interactive { get; }
}
