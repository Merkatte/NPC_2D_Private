using System;
using System.Collections.Generic;

public sealed class AStarPathfinder
{
    private const int StraightCost = 10;
    private const int DiagonalCost = 14;

    private long[] _costs = Array.Empty<long>();
    private int[] _parents = Array.Empty<int>();
    private bool[] _closed = Array.Empty<bool>();
    private AStarOpenSet _openSet;

    // One scene owner reuses this workspace sequentially; searches are not reentrant.
    // Failure clears output and returns totalCost = 0. Successful paths include both endpoints.
    public bool TryFindPath(NavigationGrid grid, int start, int goal, List<int> output, out long totalCost)
    {
        if (output == null)
            throw new ArgumentNullException(nameof(output));

        output.Clear();
        totalCost = 0;
        if (grid == null || !grid.IsWalkable(start) || !grid.IsWalkable(goal))
            return false;
        if (start == goal)
        {
            output.Add(start);
            return true;
        }

        PrepareWorkspace(grid.NodeCount);
        int goalX = goal % grid.Width;
        int goalY = goal / grid.Width;
        _costs[start] = 0;
        long initialHeuristic = GetHeuristic(start % grid.Width, start / grid.Width, goalX, goalY, grid.MinimumCost);
        _openSet.InsertOrDecrease(start, initialHeuristic, initialHeuristic);

        while (_openSet.TryPop(out int current))
        {
            if (current == goal)
            {
                totalCost = _costs[current];
                BuildPath(current, output);
                return true;
            }

            _closed[current] = true;
            int currentX = current % grid.Width;
            int currentY = current / grid.Width;
            for (int dy = -1; dy <= 1; dy++)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    if (dx == 0 && dy == 0)
                        continue;
                    if (!grid.TryGetIndex(currentX + dx, currentY + dy, out int next) || !grid.IsWalkable(next) || _closed[next])
                        continue;

                    bool isDiagonal = dx != 0 && dy != 0;
                    if (isDiagonal && (!grid.IsWalkable(current + dx) || !grid.IsWalkable(current + dy * grid.Width)))
                        continue;

                    long nextCost = _costs[current] + (long)(isDiagonal ? DiagonalCost : StraightCost) * grid.GetCost(next);
                    if (nextCost >= _costs[next])
                        continue;

                    _costs[next] = nextCost;
                    _parents[next] = current;
                    long heuristic = GetHeuristic(currentX + dx, currentY + dy, goalX, goalY, grid.MinimumCost);
                    _openSet.InsertOrDecrease(next, nextCost + heuristic, heuristic);
                }
            }
        }
        return false;
    }

    private void PrepareWorkspace(int nodeCount)
    {
        if (_costs.Length < nodeCount)
        {
            _costs = new long[nodeCount];
            _parents = new int[nodeCount];
            _closed = new bool[nodeCount];
            _openSet = new AStarOpenSet(nodeCount);
        }
        _openSet.Clear();
        for (int i = 0; i < nodeCount; i++)
        {
            _costs[i] = long.MaxValue;
            _parents[i] = -1;
            _closed[i] = false;
        }
    }

    private static long GetHeuristic(int x, int y, int goalX, int goalY, int minimumCost)
    {
        int dx = Math.Abs(goalX - x);
        int dy = Math.Abs(goalY - y);
        int diagonalSteps = Math.Min(dx, dy);
        int straightSteps = Math.Max(dx, dy) - diagonalSteps;
        // Minimum terrain cost keeps octile distance a consistent lower bound,
        // even with destination-weighted edges. Closed nodes never need reopening.
        return ((long)diagonalSteps * DiagonalCost + (long)straightSteps * StraightCost) * minimumCost;
    }

    private void BuildPath(int goal, List<int> output)
    {
        for (int node = goal; node >= 0; node = _parents[node])
            output.Add(node);
        output.Reverse();
    }
}
