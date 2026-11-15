using System;
using UnityEngine;

[Serializable]
public struct DefenseWaveEnemyEntry
{
    [SerializeField] private string _id;
    [SerializeField] private WorkerNPC _prefab;
    [SerializeField] private NPCStatDefinition _statDefinition;

    public string Id => _id;
    public WorkerNPC Prefab => _prefab;
    public NPCStatDefinition StatDefinition => _statDefinition;
}
