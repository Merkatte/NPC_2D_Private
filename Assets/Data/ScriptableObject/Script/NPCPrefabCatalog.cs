using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NPCPrefabCatalog", menuName = "Scriptable Objects/NPCPrefabCatalog")]
public class NPCPrefabCatalog : ScriptableObject
{
    [SerializeField] private List<Entry> _entries = new List<Entry>();

    public IReadOnlyList<Entry> Entries => _entries;

    public bool TryGetEntry(NPCPrefabType prefabType, out Entry entry)
    {
        entry = null;

        if (prefabType == NPCPrefabType.None || _entries == null)
        {
            return false;
        }

        for (int i = 0; i < _entries.Count; ++i)
        {
            Entry candidate = _entries[i];
            if (candidate == null || candidate.PrefabType != prefabType)
            {
                continue;
            }

            if (entry != null)
            {
                entry = null;
                return false;
            }

            entry = candidate;
        }

        return entry != null && entry.Prefab;
    }

    [Serializable]
    public sealed class Entry
    {
        [SerializeField] private NPCPrefabType _prefabType;
        [SerializeField] private WorkerNPC _prefab;
        [SerializeField] [Min(1)] private int _defaultCapacity = 10;
        [SerializeField] [Min(1)] private int _maxSize = 100;

        public NPCPrefabType PrefabType => _prefabType;
        public WorkerNPC Prefab => _prefab;
        public int DefaultCapacity => Mathf.Max(1, _defaultCapacity);
        public int MaxSize => Mathf.Max(DefaultCapacity, _maxSize);
    }
}
