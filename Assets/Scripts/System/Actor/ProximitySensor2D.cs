using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Job/domain-neutral Trigger2D candidate detector. Knows nothing about Guard, Enemy, or
/// ICombatTarget; it only tracks which colliders on the configured LayerMask are currently
/// inside its trigger and notifies enter/exit.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class ProximitySensor2D : MonoBehaviour
{
    [SerializeField] private LayerMask _detectionMask;

    private readonly List<Collider2D> _candidates = new List<Collider2D>();

    public event Action<Collider2D> OnCandidateEntered;
    public event Action<Collider2D> OnCandidateExited;

    public IReadOnlyList<Collider2D> Candidates => _candidates;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsInMask(other) || _candidates.Contains(other))
        {
            return;
        }

        _candidates.Add(other);
        OnCandidateEntered?.Invoke(other);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!_candidates.Remove(other))
        {
            return;
        }

        OnCandidateExited?.Invoke(other);
    }

    private void OnDisable()
    {
        for (int i = _candidates.Count - 1; i >= 0; --i)
        {
            Collider2D candidate = _candidates[i];
            _candidates.RemoveAt(i);
            OnCandidateExited?.Invoke(candidate);
        }
    }

    /// <summary>
    /// Removes destroyed/deactivated colliders that never sent an exit callback and
    /// notifies subscribers. Must only be called from an explicit lifecycle point
    /// (e.g. a periodic Update), never from a read-only property getter.
    /// </summary>
    public void Prune()
    {
        for (int i = _candidates.Count - 1; i >= 0; --i)
        {
            Collider2D candidate = _candidates[i];
            if (candidate && candidate.gameObject.activeInHierarchy)
            {
                continue;
            }

            _candidates.RemoveAt(i);
            OnCandidateExited?.Invoke(candidate);
        }
    }

    private bool IsInMask(Collider2D other)
    {
        return (_detectionMask.value & (1 << other.gameObject.layer)) != 0;
    }
}
