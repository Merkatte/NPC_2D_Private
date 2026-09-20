using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public sealed class TilemapNavigation : MonoBehaviour, INavigationService
{
    private const float GeometryTolerance = 0.001f;

    [Serializable]
    private sealed class LocalArea
    {
        [SerializeField] private BoxCollider2D _bounds;
        [SerializeField] private Transform _entrance;
        public BoxCollider2D Bounds => _bounds;
        public Transform Entrance => _entrance;
    }

    [SerializeField] private Tilemap _tilemap;
    [SerializeField] private TileNavigationProfile _profile;
    [SerializeField] private LocalArea[] _localAreas = Array.Empty<LocalArea>();
    [SerializeField] private BoxCollider2D[] _blockedAreas = Array.Empty<BoxCollider2D>();
    [SerializeField] private bool _drawNodes;

    private NavigationGrid _grid;
    private readonly AStarPathfinder _pathfinder = new AStarPathfinder();
    private readonly List<int> _nodePath = new List<int>();
    private BoundsInt _cellBounds;
    private Rect[] _areaRects;
    private Vector3[] _entrances;
    private int[] _entranceNodes;
    private int[] _groundCosts;
    private Vector3 _firstCenter;
    private float _cellSize;
    private bool _initializationAttempted;
    private bool _hasLoggedConfigurationError;

    public bool IsReady => this && isActiveAndEnabled && EnsureInitialized();
    public int SearchCount { get; private set; }
    public int LocalRouteCount { get; private set; }
    public string ConfigurationError { get; private set; }

    private void Awake() => EnsureInitialized();

    private void OnDisable()
    {
        _grid = null;
        _groundCosts = null;
        _areaRects = null;
        _entrances = null;
        _entranceNodes = null;
        _nodePath.Clear();
        _initializationAttempted = false;
    }

    public bool TryBuildPath(Vector3 start, Vector3 destination, List<Vector3> output,
        out NavigationFailure failure)
    {
        failure = NavigationFailure.InvalidConfiguration;
        if (output == null)
            return false;
        output.Clear();
        if (!IsReady)
            return false;
        if (!TryGetNode(start, out int startNode) || _groundCosts[startNode] == 0)
        {
            failure = NavigationFailure.InvalidStart;
            return false;
        }
        if (!TryGetNode(destination, out int goalNode) || _groundCosts[goalNode] == 0)
        {
            failure = NavigationFailure.InvalidDestination;
            return false;
        }

        int startArea = FindArea(start);
        int goalArea = FindArea(destination);
        if (startNode == goalNode || (startArea >= 0 && startArea == goalArea))
        {
            output.Add(destination);
            ++LocalRouteCount;
            failure = NavigationFailure.None;
            return true;
        }

        int routeStart = startArea >= 0 ? _entranceNodes[startArea] : startNode;
        int routeGoal = goalArea >= 0 ? _entranceNodes[goalArea] : goalNode;
        ++SearchCount;
        if (!_pathfinder.TryFindPath(_grid, routeStart, routeGoal, _nodePath, out _))
        {
            failure = NavigationFailure.NoPath;
            return false;
        }

        // Cell centers keep every network segment inside its permitted cells. No
        // line-of-sight smoothing may cut a blocked corner or bypass terrain costs.
        if (startArea >= 0)
            AddPoint(output, _entrances[startArea], start.z);
        foreach (int node in _nodePath)
            AddPoint(output, GetCenter(node), start.z);
        if (goalArea >= 0)
            AddPoint(output, _entrances[goalArea], start.z);
        AddPoint(output, destination, start.z);
        failure = NavigationFailure.None;
        return true;
    }

    private bool EnsureInitialized()
    {
        if (_initializationAttempted)
            return _grid != null;
        _initializationAttempted = true;
        if (TryInitialize(out string error))
        {
            ConfigurationError = null;
            return true;
        }
        _grid = null;
        ConfigurationError = error;
        if (!_hasLoggedConfigurationError)
        {
            Debug.LogError($"TilemapNavigation '{name}': {error}", this);
            _hasLoggedConfigurationError = true;
        }
        return false;
    }

    private bool TryInitialize(out string error)
    {
        error = "Assign Tilemap and TileNavigationProfile.";
        if (!_tilemap || !_profile)
            return false;
        if (!_profile.TryCreateLookup(out Dictionary<TileBase, int> tileCosts, out error))
            return false;

        _cellBounds = _tilemap.cellBounds;
        _firstCenter = _tilemap.GetCellCenterWorld(_cellBounds.min);
        Vector3 horizontal = _tilemap.GetCellCenterWorld(_cellBounds.min + Vector3Int.right) - _firstCenter;
        Vector3 vertical = _tilemap.GetCellCenterWorld(_cellBounds.min + Vector3Int.up) - _firstCenter;
        _cellSize = horizontal.x;
        error = "Navigation requires a nonempty axis-aligned XY square grid with no cell gap.";
        if (_cellBounds.size.x <= 0 || _cellBounds.size.y <= 0 || _cellBounds.size.z != 1 ||
            _cellSize <= 0f || Mathf.Abs(horizontal.y) > GeometryTolerance ||
            Mathf.Abs(vertical.x) > GeometryTolerance || Mathf.Abs(vertical.y - _cellSize) > GeometryTolerance ||
            !_tilemap.layoutGrid || _tilemap.layoutGrid.cellLayout != GridLayout.CellLayout.Rectangle ||
            _tilemap.layoutGrid.cellGap.sqrMagnitude > GeometryTolerance * GeometryTolerance)
            return false;

        long count = (long)_cellBounds.size.x * _cellBounds.size.y;
        if (count > int.MaxValue)
        {
            error = "Tilemap bounds exceed supported node count.";
            return false;
        }
        _groundCosts = new int[(int)count];
        for (int node = 0; node < _groundCosts.Length; ++node)
        {
            Vector3Int cell = _cellBounds.min + new Vector3Int(node % _cellBounds.size.x, node / _cellBounds.size.x, 0);
            TileBase tile = _tilemap.GetTile(cell);
            _groundCosts[node] = tile && tileCosts.TryGetValue(tile, out int cost) ? cost : 0;
        }

        if (_blockedAreas == null || _localAreas == null)
        {
            error = "Area lists must be initialized.";
            return false;
        }
        foreach (BoxCollider2D obstacle in _blockedAreas)
        {
            if (!TryGetRectangle(obstacle, out Rect rect))
            {
                error = "Blocked areas require live, axis-aligned BoxCollider2D references.";
                return false;
            }
            for (int node = 0; node < _groundCosts.Length; ++node)
                if (GetCellRectangle(node).Overlaps(rect))
                    _groundCosts[node] = 0;
        }

        var networkCosts = (int[])_groundCosts.Clone();
        _areaRects = new Rect[_localAreas.Length];
        _entrances = new Vector3[_localAreas.Length];
        _entranceNodes = new int[_localAreas.Length];
        for (int area = 0; area < _localAreas.Length; ++area)
        {
            LocalArea source = _localAreas[area];
            if (source == null || !source.Entrance || !TryGetRectangle(source.Bounds, out Rect rect))
            {
                error = $"Local area {area} requires an axis-aligned box and entrance.";
                return false;
            }
            // Whole cells are the navigation unit. Cover boundary cells too, so a
            // grass landing in a partial work-area cell can still reach its portal.
            rect = SnapToCells(rect);
            for (int previous = 0; previous < area; ++previous)
            {
                if (_areaRects[previous].Overlaps(rect))
                {
                    error = $"Local areas {previous} and {area} overlap.";
                    return false;
                }
            }
            _areaRects[area] = rect;
            if (!TryGetNode(source.Entrance.position, out int entranceNode) ||
                !Contains(rect, source.Entrance.position) || !Contains(rect, GetCenter(entranceNode)))
            {
                error = $"Local area {area} entrance and its node center must be inside the area.";
                return false;
            }
            _entranceNodes[area] = entranceNode;
            // The serialized entrance selects a node, whose center is the actual portal.
            _entrances[area] = GetCenter(entranceNode);
            if (!TryGetNode(new Vector3(rect.xMin + GeometryTolerance, rect.yMin + GeometryTolerance), out _) ||
                !TryGetNode(new Vector3(rect.xMax - GeometryTolerance, rect.yMax - GeometryTolerance), out _))
            {
                error = $"Local area {area} extends outside the Tilemap.";
                return false;
            }
            for (int node = 0; node < networkCosts.Length; ++node)
            {
                if (!GetCellRectangle(node).Overlaps(rect))
                    continue;
                if (_groundCosts[node] == 0)
                {
                    error = $"Local area {area} intersects blocked node {node}. Direct movement would be unsafe.";
                    return false;
                }
                if (node != entranceNode)
                    networkCosts[node] = 0;
            }
        }

        try
        {
            var candidate = new NavigationGrid(_cellBounds.size.x, _cellBounds.size.y, networkCosts);
            foreach (int entrance in _entranceNodes)
            {
                bool connected = false;
                int x = entrance % candidate.Width;
                int y = entrance / candidate.Width;
                for (int direction = 0; direction < 4; ++direction)
                {
                    int dx = direction == 0 ? 1 : direction == 1 ? -1 : 0;
                    int dy = direction == 2 ? 1 : direction == 3 ? -1 : 0;
                    if (candidate.TryGetIndex(x + dx, y + dy, out int next) && candidate.IsWalkable(next))
                        connected = true;
                }
                if (!connected)
                {
                    error = "An entrance node has no walkable network neighbor; place it on the area edge.";
                    return false;
                }
            }
            _grid = candidate;
        }
        catch (ArgumentException exception)
        {
            error = exception.Message;
            return false;
        }
        error = null;
        return true;
    }

    private bool TryGetNode(Vector3 position, out int node)
    {
        node = -1;
        if (float.IsNaN(position.x) || float.IsInfinity(position.x) ||
            float.IsNaN(position.y) || float.IsInfinity(position.y))
            return false;
        Vector3Int cell = _tilemap.WorldToCell(position);
        int x = cell.x - _cellBounds.xMin;
        int y = cell.y - _cellBounds.yMin;
        if (x < 0 || y < 0 || x >= _cellBounds.size.x || y >= _cellBounds.size.y)
            return false;
        node = y * _cellBounds.size.x + x;
        return true;
    }

    private int FindArea(Vector3 point)
    {
        for (int area = 0; area < _areaRects.Length; ++area)
            if (Contains(_areaRects[area], point))
                return area;
        return -1;
    }

    private Vector3 GetCenter(int node) => _firstCenter + new Vector3(
        node % _cellBounds.size.x * _cellSize, node / _cellBounds.size.x * _cellSize, 0f);

    private Rect GetCellRectangle(int node)
    {
        Vector3 center = GetCenter(node);
        return new Rect(center.x - _cellSize * 0.5f, center.y - _cellSize * 0.5f, _cellSize, _cellSize);
    }

    private Rect SnapToCells(Rect rectangle)
    {
        Vector3 origin = _firstCenter - new Vector3(_cellSize, _cellSize) * 0.5f;
        return Rect.MinMaxRect(
            origin.x + Mathf.Floor((rectangle.xMin - origin.x + GeometryTolerance) / _cellSize) * _cellSize,
            origin.y + Mathf.Floor((rectangle.yMin - origin.y + GeometryTolerance) / _cellSize) * _cellSize,
            origin.x + Mathf.Ceil((rectangle.xMax - origin.x - GeometryTolerance) / _cellSize) * _cellSize,
            origin.y + Mathf.Ceil((rectangle.yMax - origin.y - GeometryTolerance) / _cellSize) * _cellSize);
    }

    private static bool TryGetRectangle(BoxCollider2D box, out Rect rectangle)
    {
        rectangle = default;
        if (!box || box.size.x <= 0f || box.size.y <= 0f)
            return false;
        Vector3 xAxis = box.transform.TransformVector(Vector3.right);
        Vector3 yAxis = box.transform.TransformVector(Vector3.up);
        if (Mathf.Abs(xAxis.y) > GeometryTolerance || Mathf.Abs(yAxis.x) > GeometryTolerance ||
            Mathf.Abs(xAxis.x) < GeometryTolerance || Mathf.Abs(yAxis.y) < GeometryTolerance)
            return false;
        Vector3 left = box.transform.TransformPoint(box.offset - box.size * 0.5f);
        Vector3 right = box.transform.TransformPoint(box.offset + box.size * 0.5f);
        rectangle = Rect.MinMaxRect(Mathf.Min(left.x, right.x), Mathf.Min(left.y, right.y),
            Mathf.Max(left.x, right.x), Mathf.Max(left.y, right.y));
        return true;
    }

    private static bool Contains(Rect rectangle, Vector3 point) =>
        point.x >= rectangle.xMin && point.x <= rectangle.xMax &&
        point.y >= rectangle.yMin && point.y <= rectangle.yMax;

    private static void AddPoint(List<Vector3> output, Vector3 point, float z)
    {
        point.z = z;
        if (output.Count == 0 || (output[output.Count - 1] - point).sqrMagnitude > GeometryTolerance * GeometryTolerance)
            output.Add(point);
    }

    private void OnDrawGizmosSelected()
    {
        if (!_drawNodes || _grid == null)
            return;
        for (int node = 0; node < _grid.NodeCount; ++node)
        {
            int cost = _grid.GetCost(node);
            Gizmos.color = cost == 0 ? Color.red : cost == 1 ? Color.yellow : Color.green;
            Gizmos.DrawWireCube(GetCenter(node), Vector3.one * (_cellSize * 0.9f));
        }
        foreach (Vector3 entrance in _entrances)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(entrance, _cellSize * 0.15f);
        }
    }
}
