using System;

internal sealed class AStarOpenSet
{
    private readonly int[] _nodes;
    private readonly int[] _positions;
    private readonly long[] _fScores;
    private readonly long[] _hScores;

    public int Count { get; private set; }

    public AStarOpenSet(int capacity)
    {
        if (capacity <= 0)
            throw new ArgumentOutOfRangeException(nameof(capacity));

        _nodes = new int[capacity];
        _positions = new int[capacity];
        _fScores = new long[capacity];
        _hScores = new long[capacity];
        for (int i = 0; i < capacity; i++)
            _positions[i] = -1;
    }

    public void Clear()
    {
        for (int i = 0; i < Count; i++)
            _positions[_nodes[i]] = -1;
        Count = 0;
    }

    public void InsertOrDecrease(int node, long f, long h)
    {
        if (node < 0 || node >= _positions.Length)
            throw new ArgumentOutOfRangeException(nameof(node));

        int position = _positions[node];
        if (position >= 0 && (f > _fScores[node] || (f == _fScores[node] && h >= _hScores[node])))
            return;

        _fScores[node] = f;
        _hScores[node] = h;
        if (position < 0)
        {
            position = Count++;
            _nodes[position] = node;
            _positions[node] = position;
        }
        SiftUp(position);
    }

    public bool TryPop(out int node)
    {
        node = -1;
        if (Count == 0)
            return false;

        node = _nodes[0];
        _positions[node] = -1;
        Count--;
        if (Count > 0)
        {
            _nodes[0] = _nodes[Count];
            _positions[_nodes[0]] = 0;
            SiftDown(0);
        }
        return true;
    }

    private void SiftUp(int position)
    {
        while (position > 0)
        {
            int parent = (position - 1) / 2;
            if (!HasPriority(_nodes[position], _nodes[parent]))
                break;

            Swap(position, parent);
            position = parent;
        }
    }

    private void SiftDown(int position)
    {
        while (position < Count / 2)
        {
            int child = position * 2 + 1;
            int right = child + 1;
            if (right < Count && HasPriority(_nodes[right], _nodes[child]))
                child = right;
            if (!HasPriority(_nodes[child], _nodes[position]))
                break;

            Swap(position, child);
            position = child;
        }
    }

    private bool HasPriority(int first, int second)
    {
        if (_fScores[first] != _fScores[second])
            return _fScores[first] < _fScores[second];
        if (_hScores[first] != _hScores[second])
            return _hScores[first] < _hScores[second];
        return first < second;
    }

    private void Swap(int first, int second)
    {
        // Every live node occurs once, and its reverse index follows every swap.
        int node = _nodes[first];
        _nodes[first] = _nodes[second];
        _nodes[second] = node;
        _positions[_nodes[first]] = first;
        _positions[_nodes[second]] = second;
    }
}
