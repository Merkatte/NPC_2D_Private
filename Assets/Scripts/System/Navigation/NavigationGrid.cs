using System;

public sealed class NavigationGrid
{
    private readonly int[] _traversalCosts;

    public int Width { get; }
    public int Height { get; }
    public int NodeCount => _traversalCosts.Length;
    public int MinimumCost { get; }

    public NavigationGrid(int width, int height, int[] traversalCosts)
    {
        if (width <= 0)
            throw new ArgumentOutOfRangeException(nameof(width));
        if (height <= 0 || (long)width * height > int.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(height));
        if (traversalCosts == null)
            throw new ArgumentNullException(nameof(traversalCosts));
        if (traversalCosts.Length != width * height)
            throw new ArgumentException("Traversal costs must cover every grid node.", nameof(traversalCosts));

        int minimumCost = int.MaxValue;
        int maximumCost = 0;
        for (int i = 0; i < traversalCosts.Length; i++)
        {
            int cost = traversalCosts[i];
            if (cost < 0)
                throw new ArgumentOutOfRangeException(nameof(traversalCosts), "Zero blocks a node; traversable costs must be positive.");

            if (cost > 0)
                minimumCost = Math.Min(minimumCost, cost);
            maximumCost = Math.Max(maximumCost, cost);
        }

        // A simple path and its heuristic each have at most NodeCount - 1 steps.
        // Reserve both bounds so all A* f scores fit in a signed 64-bit integer.
        long maximumSteps = traversalCosts.Length - 1L;
        if (maximumSteps > 0 && maximumCost > long.MaxValue / 28 / maximumSteps)
            throw new ArgumentOutOfRangeException(nameof(traversalCosts), "Grid size and traversal costs exceed the supported score range.");

        Width = width;
        Height = height;
        MinimumCost = maximumCost == 0 ? 0 : minimumCost;
        _traversalCosts = (int[])traversalCosts.Clone();
    }

    public int GetCost(int node) => _traversalCosts[node];

    public bool IsWalkable(int node)
    {
        return node >= 0 && node < NodeCount && _traversalCosts[node] > 0;
    }

    public bool TryGetIndex(int x, int y, out int node)
    {
        node = -1;
        if (x < 0 || x >= Width || y < 0 || y >= Height)
            return false;

        node = y * Width + x;
        return true;
    }
}
