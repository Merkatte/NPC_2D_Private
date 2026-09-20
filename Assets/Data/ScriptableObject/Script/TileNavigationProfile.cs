using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(menuName = "NPC/Tile Navigation Profile")]
public sealed class TileNavigationProfile : ScriptableObject
{
    [Serializable]
    private sealed class TileGroup
    {
        [SerializeField] private string _name;
        [SerializeField] private bool _walkable = true;
        [SerializeField, Min(1)] private int _cost = 1;
        [SerializeField] private TileBase[] _tiles = Array.Empty<TileBase>();

        public string Name => _name;
        public bool Walkable => _walkable;
        public int Cost => _cost;
        public TileBase[] Tiles => _tiles;
        public void Validate() => _cost = Math.Max(1, _cost);
    }

    [SerializeField] private TileGroup[] _groups = Array.Empty<TileGroup>();

    // A scene-owned lookup keeps runtime caches out of the shared definition asset.
    public bool TryCreateLookup(out Dictionary<TileBase, int> costs, out string error)
    {
        costs = null;
        error = null;
        var candidate = new Dictionary<TileBase, int>();
        if (_groups == null || _groups.Length == 0)
        {
            error = "Profile has no tile groups.";
            return false;
        }

        foreach (TileGroup group in _groups)
        {
            if (group == null || group.Tiles == null || group.Cost < 1)
            {
                error = "Profile contains an invalid group or cost.";
                return false;
            }
            foreach (TileBase tile in group.Tiles)
            {
                if (!tile || candidate.ContainsKey(tile))
                {
                    error = $"Profile group '{group.Name}' contains a missing or duplicate tile.";
                    return false;
                }
                candidate.Add(tile, group.Walkable ? group.Cost : 0);
            }
        }

        if (candidate.Count == 0)
        {
            error = "Profile has no registered tiles.";
            return false;
        }
        costs = candidate;
        return true;
    }

    private void OnValidate()
    {
        if (_groups == null)
            return;
        foreach (TileGroup group in _groups)
            group?.Validate();
    }
}
