using System.Collections.Generic;
using UnityEngine;

public sealed class TestNavigationWindow : MonoBehaviour
{
    [SerializeField] private TilemapNavigation _navigation;
    [SerializeField] private Transform _start;
    [SerializeField] private Transform _destination;
    [SerializeField] private bool _showWindow;

    private readonly List<Vector3> _path = new List<Vector3>();
    private Rect _window = new Rect(20f, 600f, 310f, 180f);
    private string _result = "Assign Start and Destination, then query.";
    private Vector3 _pathStart;

    [ContextMenu("Query Path")]
    public void QueryPath()
    {
        _path.Clear();
        if (!_navigation || !_start || !_destination)
        {
            _result = "Navigation / Start / Destination reference missing.";
            return;
        }
        _pathStart = _start.position;
        int before = _navigation.SearchCount;
        bool found = _navigation.TryBuildPath(_pathStart, _destination.position, _path, out NavigationFailure failure);
        _result = found ? $"{_path.Count} points; A* calls: {_navigation.SearchCount - before}" : failure.ToString();
        Debug.Log($"Navigation probe: {_result}", this);
    }

    private void OnGUI()
    {
        if (_showWindow)
            _window = GUI.Window(GetInstanceID(), _window, DrawWindow, "Navigation probe");
    }

    private void DrawWindow(int id)
    {
        GUILayout.Label(_result);
        if (_navigation)
        {
            GUILayout.Label($"Searches: {_navigation.SearchCount} / Local: {_navigation.LocalRouteCount}");
            if (!_navigation.IsReady)
                GUILayout.Label(_navigation.ConfigurationError);
        }
        if (GUILayout.Button("Query assigned points"))
            QueryPath();
        GUI.DragWindow();
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Vector3 previous = _pathStart;
        foreach (Vector3 point in _path)
        {
            Gizmos.DrawLine(previous, point);
            Gizmos.DrawSphere(point, 0.06f);
            previous = point;
        }
    }
}
