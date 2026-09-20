using System;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
#endif

public static class NavigationTests
{
    public static int RunPureChecks()
    {
        int checks = 0;
        var finder = new AStarPathfinder();
        var output = new List<int>();
        var random = new System.Random(73921);
        for (int sample = 0; sample < 240; ++sample)
        {
            int width = random.Next(2, 15);
            int height = random.Next(2, 15);
            var costs = new int[width * height];
            for (int node = 0; node < costs.Length; ++node)
                costs[node] = random.Next(5) == 0 ? 0 : random.Next(1, 5);
            var grid = new NavigationGrid(width, height, costs);
            for (int query = 0; query < 6; ++query)
            {
                int start = random.Next(costs.Length);
                int goal = random.Next(costs.Length);
                long expected = Dijkstra(width, height, costs, start, goal);
                bool found = finder.TryFindPath(grid, start, goal, output, out long actual);
                Require(found == (expected != long.MaxValue), "A*/Dijkstra reachability mismatch.");
                Require(found ? actual == expected : output.Count == 0 && actual == 0, "Cost or failure output mismatch.");
                if (found)
                    ValidatePath(grid, output, start, goal, actual);
                ++checks;
            }
        }

        var corner = new NavigationGrid(2, 2, new[] { 1, 0, 0, 1 });
        Require(!finder.TryFindPath(corner, 0, 3, output, out _), "A diagonal cut a blocked corner.");
        var weighted = new NavigationGrid(5, 3, new[] { 1, 1, 1, 1, 1, 3, 3, 3, 3, 3, 0, 0, 0, 0, 0 });
        Require(finder.TryFindPath(weighted, 5, 9, output, out long roadCost) && roadCost < 120,
            "A cheaper road detour was not selected.");
        Require(output.Exists(node => node < 5), "The chosen route should use the road.");
        var saved = output.ToArray();
        finder.TryFindPath(weighted, 5, 9, output, out _);
        Require(saved.Length == output.Count, "Repeated query changed the route length.");
        for (int i = 0; i < saved.Length; ++i)
            Require(saved[i] == output[i], "Repeated query was not deterministic.");
        Require(finder.TryFindPath(new NavigationGrid(2, 1, new[] { 1, 3 }), 0, 1, output, out long grassCost) && grassCost == 30,
            "Grass must remain traversable.");
        Require(finder.TryFindPath(weighted, 5, 5, output, out long sameCost) && sameCost == 0 && output.Count == 1,
            "Same-node result is invalid.");
        Require(!finder.TryFindPath(weighted, -1, 9, output, out _) && output.Count == 0, "Invalid start left a stale path.");
        return checks + 6;
    }

    private static long Dijkstra(int width, int height, int[] costs, int start, int goal)
    {
        if (costs[start] == 0 || costs[goal] == 0)
            return long.MaxValue;
        var distances = new long[costs.Length];
        var visited = new bool[costs.Length];
        for (int i = 0; i < distances.Length; ++i)
            distances[i] = long.MaxValue;
        distances[start] = 0;
        for (int iteration = 0; iteration < costs.Length; ++iteration)
        {
            int current = -1;
            for (int node = 0; node < costs.Length; ++node)
                if (!visited[node] && distances[node] != long.MaxValue &&
                    (current < 0 || distances[node] < distances[current]))
                    current = node;
            if (current < 0 || current == goal)
                break;
            visited[current] = true;
            // Enumerate all cells rather than sharing production's neighbor generation.
            for (int next = 0; next < costs.Length; ++next)
            {
                int dx = Math.Abs(current % width - next % width);
                int dy = Math.Abs(current / width - next / width);
                if (costs[next] == 0 || dx > 1 || dy > 1 || dx + dy == 0)
                    continue;
                bool diagonal = dx == 1 && dy == 1;
                if (diagonal && (costs[current / width * width + next % width] == 0 ||
                    costs[next / width * width + current % width] == 0))
                    continue;
                long candidate = distances[current] + (diagonal ? 14L : 10L) * costs[next];
                distances[next] = Math.Min(distances[next], candidate);
            }
        }
        return distances[goal];
    }

    private static void ValidatePath(NavigationGrid grid, List<int> path, int start, int goal, long cost)
    {
        Require(path.Count > 0 && path[0] == start && path[path.Count - 1] == goal, "Path endpoints differ.");
        long actual = 0;
        for (int i = 1; i < path.Count; ++i)
        {
            int previous = path[i - 1];
            int next = path[i];
            int dx = Math.Abs(previous % grid.Width - next % grid.Width);
            int dy = Math.Abs(previous / grid.Width - next / grid.Width);
            Require(grid.IsWalkable(next) && dx <= 1 && dy <= 1 && dx + dy > 0, "Path contains an invalid edge.");
            if (dx == 1 && dy == 1)
                Require(grid.IsWalkable(previous / grid.Width * grid.Width + next % grid.Width) &&
                    grid.IsWalkable(next / grid.Width * grid.Width + previous % grid.Width), "Path cut a corner.");
            actual += (dx == 1 && dy == 1 ? 14L : 10L) * grid.GetCost(next);
        }
        Require(actual == cost, "Reconstructed path cost differs from search cost.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }

#if UNITY_EDITOR
    [MenuItem("Tools/NPC/Navigation/Run Checks")]
    public static void RunInEditor()
    {
        int checks = RunPureChecks();
        var navigation = UnityEngine.Object.FindFirstObjectByType<TilemapNavigation>();
        Require(navigation && navigation.IsReady, "Current scene has no ready TilemapNavigation.");
        var output = new List<Vector3>();
        var serialized = new SerializedObject(navigation);
        SerializedProperty areas = serialized.FindProperty("_localAreas");
        var positions = new List<Vector3>();
        for (int i = 0; i < areas.arraySize; ++i)
        {
            SerializedProperty area = areas.GetArrayElementAtIndex(i);
            var box = (BoxCollider2D)area.FindPropertyRelative("_bounds").objectReferenceValue;
            Vector3 first = box.transform.TransformPoint(box.offset - box.size * 0.2f);
            Vector3 second = box.transform.TransformPoint(box.offset + box.size * 0.2f);
            int searches = navigation.SearchCount;
            Require(navigation.TryBuildPath(first, second, output, out _) && output.Count == 1,
                "Local movement did not produce a direct route.");
            Require(navigation.SearchCount == searches, "Local movement invoked A*.");
            positions.Add(first);
            checks += 2;
        }
        var destinations = UnityEngine.Object.FindFirstObjectByType<DestinationDB>();
        Require(destinations, "DestinationDB is missing.");
        foreach (BuildingType key in destinations.RegisteredKeys)
        {
            Require(destinations.TryGetDestinationPos(key, out Vector3 destination), "Missing destination.");
            positions.Add(destination);
        }
        positions.Add(new Vector3(28.5f, -10.5f)); // Existing grass, outside all local areas.
        for (int start = 0; start < positions.Count; ++start)
            for (int goal = 0; goal < positions.Count; ++goal)
            {
                Require(navigation.TryBuildPath(positions[start], positions[goal], output, out NavigationFailure failure),
                    $"Scene route {start}->{goal} failed: {failure}.");
                ++checks;
            }
        Debug.Log($"Navigation checks passed: {checks}. Scene query checks do not prove Play Mode following.");
    }
#endif
}
