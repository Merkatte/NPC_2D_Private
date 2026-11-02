using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// Decorative presentation only: these instances are not obstacles or destinations.
[ExecuteAlways]
[DisallowMultipleComponent]
public sealed class InstancedTreeGrove : MonoBehaviour
{
    private const int MaximumTreeCount = 5000;
    private const int MaximumBatchSize = 256;
    private const int AttemptsPerTree = 128;
    private const string ShaderName = "NPC/InstancedTreeWind";

    [SerializeField] private Material[] _speciesMaterials = Array.Empty<Material>();
    [SerializeField] private Rect[] _placementRegions = Array.Empty<Rect>();
    [SerializeField, Range(0, MaximumTreeCount)] private int _treeCount = 50;
    [SerializeField] private int _seed = 20261103;
    [SerializeField, Min(0.1f)] private float _minimumSpacing = 1f;
    [SerializeField, Min(0.01f)] private float _treeScale = 1f;
    [SerializeField] private float _depthOffset = -0.1f;
    [SerializeField] private float _depthPerY = 0.001f;

    private Mesh _quad;
    private TreeBatch[] _batches = Array.Empty<TreeBatch>();
    private Matrix4x4 _cachedRootMatrix;
    private bool _requiresRebuild = true;
    private bool _wasPlaying;
    private int _placedTreeCount;

    public int TreeCount => _treeCount;
    public int PlacedTreeCount => _placedTreeCount;
    public int DrawBatchCount => _batches.Length;

    private sealed class TreeBatch
    {
        public Matrix4x4[] LocalMatrices;
        public Matrix4x4[] WorldMatrices;
        public RenderParams Parameters;
    }

    private void OnEnable()
    {
        _requiresRebuild = true;
        _wasPlaying = Application.isPlaying;
        RenderPipelineManager.beginCameraRendering += RenderCamera;
    }

    private void OnDisable()
    {
        RenderPipelineManager.beginCameraRendering -= RenderCamera;
        ReleaseCache();
    }

    private void OnValidate()
    {
        _treeCount = Mathf.Clamp(_treeCount, 0, MaximumTreeCount);
        _minimumSpacing = Mathf.Max(0.1f, _minimumSpacing);
        _treeScale = Mathf.Max(0.01f, _treeScale);
        // OnValidate may run during deserialization; create/destroy Unity objects in Update.
        _requiresRebuild = true;
    }

    private void Update()
    {
        if (_wasPlaying != Application.isPlaying)
        {
            _wasPlaying = Application.isPlaying;
            _requiresRebuild = true;
        }

        if (!_requiresRebuild) return;
        _requiresRebuild = false;
        RebuildCache();
    }

    private void RebuildCache()
    {
        ReleaseCache();
        if (_treeCount == 0) return;
        if (!HasValidConfiguration()) return;

        int requestedCount = Mathf.Clamp(_treeCount, 0, MaximumTreeCount);
        Vector2[] positions = new Vector2[requestedCount];
        System.Random random = new System.Random(_seed);
        float minimumSpacing = Mathf.Max(0.1f, _minimumSpacing);
        float spacingSquared = minimumSpacing * minimumSpacing;
        float cellSize = minimumSpacing / Mathf.Sqrt(2f);
        Dictionary<Vector2Int, Vector2> occupiedCells = new Dictionary<Vector2Int, Vector2>(requestedCount);
        int maximumAttempts = requestedCount * AttemptsPerTree;

        for (int attempt = 0; attempt < maximumAttempts && _placedTreeCount < requestedCount; attempt++)
        {
            Rect region = _placementRegions[random.Next(_placementRegions.Length)];
            Vector2 position = new Vector2(
                region.xMin + (float)random.NextDouble() * region.width,
                region.yMin + (float)random.NextDouble() * region.height);
            Vector2Int cell = Vector2Int.FloorToInt(position / cellSize);
            if (!HasMinimumSpacing(position, cell, occupiedCells, spacingSquared)) continue;
            occupiedCells.Add(cell, position);
            positions[_placedTreeCount++] = position;
        }

        if (_placedTreeCount < requestedCount)
        {
            Debug.LogWarning($"InstancedTreeGrove '{name}' placed {_placedTreeCount}/{requestedCount} trees. " +
                "Enlarge _placementRegions or reduce _minimumSpacing.", this);
        }

        if (_placedTreeCount == 0) return;
        _quad = CreateQuad();
        int speciesCount = Mathf.Min(_speciesMaterials.Length, _placedTreeCount);
        int batchCount = 0;
        for (int speciesIndex = 0; speciesIndex < speciesCount; speciesIndex++)
        {
            int count = (_placedTreeCount - 1 - speciesIndex) / speciesCount + 1;
            batchCount += (count + MaximumBatchSize - 1) / MaximumBatchSize;
        }
        _batches = new TreeBatch[batchCount];
        float scale = Mathf.Max(0.01f, _treeScale);
        int batchIndex = 0;
        for (int speciesIndex = 0; speciesIndex < speciesCount; speciesIndex++)
        {
            int speciesTreeCount = (_placedTreeCount - 1 - speciesIndex) / speciesCount + 1;
            for (int offset = 0; offset < speciesTreeCount; offset += MaximumBatchSize)
            {
                int count = Mathf.Min(MaximumBatchSize, speciesTreeCount - offset);
                TreeBatch batch = new TreeBatch
                {
                    LocalMatrices = new Matrix4x4[count],
                    WorldMatrices = new Matrix4x4[count],
                    Parameters = new RenderParams(_speciesMaterials[speciesIndex])
                    {
                        shadowCastingMode = ShadowCastingMode.Off,
                        receiveShadows = false,
                        lightProbeUsage = LightProbeUsage.Off,
                        reflectionProbeUsage = ReflectionProbeUsage.Off,
                        motionVectorMode = MotionVectorGenerationMode.ForceNoMotion
                    }
                };
                for (int index = 0; index < count; index++)
                {
                    Vector2 position = positions[speciesIndex + (offset + index) * speciesCount];
                    batch.LocalMatrices[index] = Matrix4x4.TRS(
                        new Vector3(position.x, position.y, 0f), Quaternion.identity, Vector3.one * scale);
                }
                _batches[batchIndex++] = batch;
            }
        }
        RefreshWorldMatrices(transform.localToWorldMatrix);
    }

    private bool HasValidConfiguration()
    {
        if (!float.IsFinite(_minimumSpacing) || !float.IsFinite(_treeScale) ||
            !float.IsFinite(_depthOffset) || !float.IsFinite(_depthPerY))
        {
            Debug.LogError($"InstancedTreeGrove '{name}' requires finite spacing, scale and depth settings.", this);
            return false;
        }
        if (!SystemInfo.supportsInstancing)
        {
            Debug.LogError($"InstancedTreeGrove '{name}' requires a graphics device with GPU instancing support.", this);
            return false;
        }
        if (_speciesMaterials == null || _speciesMaterials.Length == 0 ||
            _placementRegions == null || _placementRegions.Length == 0)
        {
            Debug.LogError($"InstancedTreeGrove '{name}' requires _speciesMaterials and _placementRegions.", this);
            return false;
        }
        for (int index = 0; index < _speciesMaterials.Length; index++)
        {
            Material material = _speciesMaterials[index];
            if (!material || !material.enableInstancing || !material.shader ||
                material.shader.name != ShaderName || !material.shader.isSupported)
            {
                Debug.LogError($"InstancedTreeGrove '{name}' _speciesMaterials[{index}] must use " +
                    $"supported shader '{ShaderName}' with Enable GPU Instancing checked.", this);
                return false;
            }
            Texture texture = material.GetTexture("_MainTex");
            if (!texture || texture.width != 1344 || texture.height != 144)
            {
                Debug.LogError($"InstancedTreeGrove '{name}' _speciesMaterials[{index}] requires " +
                    "a 1344 x 144 twelve-frame _MainTex atlas.", this);
                return false;
            }
        }
        for (int index = 0; index < _placementRegions.Length; index++)
        {
            Rect region = _placementRegions[index];
            if (region.width <= 0f || region.height <= 0f ||
                !float.IsFinite(region.xMin) || !float.IsFinite(region.yMin) ||
                !float.IsFinite(region.xMax) || !float.IsFinite(region.yMax))
            {
                Debug.LogError($"InstancedTreeGrove '{name}' _placementRegions[{index}] requires finite positive dimensions.", this);
                return false;
            }
        }
        return true;
    }

    private static bool HasMinimumSpacing(Vector2 position, Vector2Int cell,
        Dictionary<Vector2Int, Vector2> occupiedCells, float spacingSquared)
    {
        // Cell diagonal equals minimum spacing, so each cell can contain at most one tree.
        if (occupiedCells.ContainsKey(cell)) return false;
        for (int y = -2; y <= 2; y++)
        {
            for (int x = -2; x <= 2; x++)
            {
                if (occupiedCells.TryGetValue(cell + new Vector2Int(x, y), out Vector2 neighbor) &&
                    (neighbor - position).sqrMagnitude < spacingSquared) return false;
            }
        }
        return true;
    }

    private static Mesh CreateQuad()
    {
        // 112 x 144 pixels, 20 PPU, ground pivot (56, 130) measured from the top.
        Mesh quad = new Mesh
        {
            name = "Instanced Tree Grove Quad",
            hideFlags = HideFlags.HideAndDontSave,
            vertices = new[]
            {
                new Vector3(-2.8f, -0.7f, 0f), new Vector3(2.8f, -0.7f, 0f),
                new Vector3(-2.8f, 6.5f, 0f), new Vector3(2.8f, 6.5f, 0f)
            },
            uv = new[] { Vector2.zero, Vector2.right, Vector2.up, Vector2.one },
            triangles = new[] { 0, 2, 1, 2, 3, 1 },
            bounds = new Bounds(new Vector3(0f, 2.9f, 0f), new Vector3(5.6f, 7.2f, 0.1f))
        };
        quad.UploadMeshData(true);
        return quad;
    }

    private void RefreshWorldMatrices(Matrix4x4 rootMatrix)
    {
        _cachedRootMatrix = rootMatrix;
        Bounds localBounds = _quad.bounds;
        foreach (TreeBatch batch in _batches)
        {
            Bounds worldBounds = default;
            for (int index = 0; index < batch.LocalMatrices.Length; index++)
            {
                Matrix4x4 matrix = rootMatrix * batch.LocalMatrices[index];
                // Whole-quad depth resolves tree/tree overlap for the XY backdrop camera.
                matrix.m23 += _depthOffset + matrix.m13 * _depthPerY;
                batch.WorldMatrices[index] = matrix;
                Vector3 extents = localBounds.extents;
                Vector3 axisX = matrix.MultiplyVector(new Vector3(extents.x, 0f, 0f));
                Vector3 axisY = matrix.MultiplyVector(new Vector3(0f, extents.y, 0f));
                Vector3 axisZ = matrix.MultiplyVector(new Vector3(0f, 0f, extents.z));
                Vector3 worldExtents = new Vector3(
                    Mathf.Abs(axisX.x) + Mathf.Abs(axisY.x) + Mathf.Abs(axisZ.x),
                    Mathf.Abs(axisX.y) + Mathf.Abs(axisY.y) + Mathf.Abs(axisZ.y),
                    Mathf.Abs(axisX.z) + Mathf.Abs(axisY.z) + Mathf.Abs(axisZ.z));
                Bounds instanceBounds = new Bounds(matrix.MultiplyPoint3x4(localBounds.center), worldExtents * 2f);
                if (index == 0) worldBounds = instanceBounds;
                else worldBounds.Encapsulate(instanceBounds);
            }
            batch.Parameters.worldBounds = worldBounds;
        }
    }

    private void RenderCamera(ScriptableRenderContext context, Camera camera)
    {
        if (!_quad || _requiresRebuild || !camera ||
            (camera.cameraType != CameraType.Game && camera.cameraType != CameraType.SceneView)) return;

        Matrix4x4 rootMatrix = transform.localToWorldMatrix;
        if (rootMatrix != _cachedRootMatrix) RefreshWorldMatrices(rootMatrix);
        foreach (TreeBatch batch in _batches)
        {
            if (!batch.Parameters.material) continue;
            batch.Parameters.camera = camera;
            batch.Parameters.layer = gameObject.layer;
            Graphics.RenderMeshInstanced(batch.Parameters, _quad, 0,
                batch.WorldMatrices, batch.WorldMatrices.Length);
        }
    }

    private void ReleaseCache()
    {
        if (_quad)
        {
            if (Application.isPlaying) Destroy(_quad);
            else DestroyImmediate(_quad);
        }
        _quad = null;
        _batches = Array.Empty<TreeBatch>();
        _placedTreeCount = 0;
    }
}
