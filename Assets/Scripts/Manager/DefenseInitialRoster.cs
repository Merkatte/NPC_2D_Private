using UnityEngine;

public sealed class DefenseInitialRoster : MonoBehaviour
{
    [SerializeField] private NPCManager _npcManager;
    [SerializeField] private Transform[] _guardSpawns;
    [SerializeField] private Transform[] _archerSpawns;

    private void Start()
    {
        if (!_npcManager || !HasValidSpawns(_guardSpawns) || !HasValidSpawns(_archerSpawns))
        {
            Debug.LogError($"DefenseInitialRoster '{name}': missing NPC manager or initial spawn anchors.", this);
            enabled = false;
            return;
        }
        Spawn(NPCType.Guard, _guardSpawns);
        Spawn(NPCType.Archer, _archerSpawns);
    }

    private static bool HasValidSpawns(Transform[] spawns)
    {
        if (spawns == null || spawns.Length == 0) return false;
        foreach (Transform point in spawns) if (!point) return false;
        return true;
    }

    private void Spawn(NPCType role, Transform[] spawns)
    {
        foreach (Transform point in spawns)
        {
            if (!_npcManager.TryReserveWorker(role, point.position, out WorkerReservation reservation))
            {
                Debug.LogError($"DefenseInitialRoster '{name}': could not reserve {role} at '{point.name}'.", this);
                return;
            }
            if (_npcManager.CommitReservation(reservation)) continue;
            _npcManager.CancelReservation(reservation);
            Debug.LogError($"DefenseInitialRoster '{name}': could not commit {role} at '{point.name}'.", this);
            return;
        }
    }
}
