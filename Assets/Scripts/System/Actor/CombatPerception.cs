using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Combat-domain adapter over ProximitySensor2D. Resolves sensor colliders into live
/// ICombatTarget candidates, de-duplicating a target's multiple colliders into one entry.
/// Does not select or store a chosen target - that stays in the owning selector/CombatRuntimeState.
/// Shared by any actor that needs to perceive combat targets (Guard, Enemy, ...).
/// </summary>
public class CombatPerception : MonoBehaviour
{
    [SerializeField] private ProximitySensor2D _sensor;

    private class TargetEntry
    {
        public ICombatTarget Target;
        public Component Owner;
        public readonly HashSet<Collider2D> Colliders = new HashSet<Collider2D>();
    }

    private readonly Dictionary<Collider2D, TargetEntry> _colliderToEntry = new Dictionary<Collider2D, TargetEntry>();
    private readonly List<TargetEntry> _entries = new List<TargetEntry>();

    public bool HasCandidate => _entries.Count > 0;

    private void OnEnable()
    {
        if (!_sensor)
        {
            return;
        }

        _sensor.OnCandidateEntered += HandleCandidateEntered;
        _sensor.OnCandidateExited += HandleCandidateExited;

        IReadOnlyList<Collider2D> snapshot = _sensor.Candidates;
        for (int i = 0; i < snapshot.Count; ++i)
        {
            HandleCandidateEntered(snapshot[i]);
        }
    }

    private void OnDisable()
    {
        if (_sensor)
        {
            _sensor.OnCandidateEntered -= HandleCandidateEntered;
            _sensor.OnCandidateExited -= HandleCandidateExited;
        }

        _colliderToEntry.Clear();
        _entries.Clear();
    }

    private void Update()
    {
        if (_sensor)
        {
            _sensor.Prune();
        }

        Prune();
    }

    private void HandleCandidateEntered(Collider2D candidate)
    {
        if (_colliderToEntry.ContainsKey(candidate))
        {
            return;
        }

        ICombatTarget target = candidate.GetComponentInParent<ICombatTarget>();
        if (target == null)
        {
            return;
        }

        Component owner = target as Component;
        if (!CombatTargetHandle.IsValidPair(target, owner))
        {
            return;
        }

        TargetEntry entry = FindEntryByOwner(owner);
        if (entry == null)
        {
            entry = new TargetEntry { Target = target, Owner = owner };
            _entries.Add(entry);
        }

        entry.Colliders.Add(candidate);
        _colliderToEntry[candidate] = entry;
    }

    private void HandleCandidateExited(Collider2D candidate)
    {
        if (!_colliderToEntry.TryGetValue(candidate, out TargetEntry entry))
        {
            return;
        }

        _colliderToEntry.Remove(candidate);
        entry.Colliders.Remove(candidate);

        if (entry.Colliders.Count == 0)
        {
            _entries.Remove(entry);
        }
    }

    private TargetEntry FindEntryByOwner(Component owner)
    {
        for (int i = 0; i < _entries.Count; ++i)
        {
            if (_entries[i].Owner == owner)
            {
                return _entries[i];
            }
        }

        return null;
    }

    /// <summary>
    /// Drops destroyed/dead candidates. Only called from Update(), never from a read-only getter.
    /// </summary>
    public void Prune()
    {
        for (int i = _entries.Count - 1; i >= 0; --i)
        {
            TargetEntry entry = _entries[i];
            if (CombatTargetHandle.IsValidPair(entry.Target, entry.Owner))
            {
                continue;
            }

            _entries.RemoveAt(i);
            foreach (Collider2D collider in entry.Colliders)
            {
                _colliderToEntry.Remove(collider);
            }
        }
    }

    /// <summary>
    /// Copies the current live candidates into the caller's buffer. Intended for the owning
    /// selector to call only at replan time, not every Tick.
    /// </summary>
    public void CopyCandidatesTo(List<(ICombatTarget Target, Component Owner)> buffer)
    {
        buffer.Clear();
        for (int i = 0; i < _entries.Count; ++i)
        {
            buffer.Add((_entries[i].Target, _entries[i].Owner));
        }
    }
}
