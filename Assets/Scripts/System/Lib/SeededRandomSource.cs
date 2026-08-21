using UnityEngine;

public class SeededRandomSource : MonoBehaviour, IRandomSource
{
    [SerializeField] private int _seed;

    private System.Random _random;

    public int NextInclusive(int minimum, int maximum)
    {
        EnsureInitialized();

        if (maximum < minimum)
            return minimum;

        return _random.Next(minimum, maximum + 1);
    }

    private void EnsureInitialized()
    {
        if (_random != null)
            return;

        _random = new System.Random(_seed);
    }
}
