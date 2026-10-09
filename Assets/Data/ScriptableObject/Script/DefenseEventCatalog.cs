using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "NPC/Defense/Event Catalog")]
public sealed class DefenseEventCatalog : ScriptableObject
{
    [SerializeField] private DefenseEventDefinition[] _events;

    public IReadOnlyList<DefenseEventDefinition> Events => _events;

    public bool IsValid
    {
        get
        {
            if (_events == null) return false;
            var ids = new HashSet<string>();
            foreach (DefenseEventDefinition definition in _events)
                if (!definition || !definition.IsValid || !ids.Add(definition.EventId)) return false;
            return true;
        }
    }
}
