using System;
using UnityEngine;

[CreateAssetMenu(menuName = "NPC/Defense/Wave Catalog")]
public sealed class DefenseWaveCatalog : ScriptableObject
{
    public enum SourceMode { Legacy = 0, Csv = 1 }

    [SerializeField] private SourceMode _source = SourceMode.Legacy;
    [SerializeField] private TextAsset _csv;
    [SerializeField] private DefenseWaveEnemyEntry[] _enemies;
    [SerializeField] private DefenseWaveDefinition[] _waves;
    [SerializeField, Min(0f)] private float _restDurationSeconds = 60f;

    public float RestDurationSeconds => _restDurationSeconds;

    public bool IsValid
    {
        get
        {
            if (!IsRestDurationValid) return false;
            if (_source == SourceMode.Csv)
                return DefenseWaveCsvMapper.TryMap(_csv, _enemies, out _, out _);
            if (_source != SourceMode.Legacy || _waves == null || _waves.Length == 0) return false;
            foreach (DefenseWaveDefinition wave in _waves)
                if (!wave || !wave.IsValid) return false;
            return true;
        }
    }

    public bool TryGetWave(int waveNumber, out DefenseWaveDefinition wave)
    {
        wave = null;
        if (_source != SourceMode.Legacy || waveNumber < 1 || !IsValid) return false;
        wave = _waves[Mathf.Min(waveNumber - 1, _waves.Length - 1)];
        return true;
    }

    // Called once by the scene owner at session start; this asset never stores session state.
    public bool TryCreateSnapshot(WorkerNPC legacyPrefab, NPCStatDefinition legacyStatDefinition,
        out DefenseWavePlan[] waves, out string error)
    {
        waves = Array.Empty<DefenseWavePlan>();
        error = "Wave catalog key '_restDurationSeconds': expected a finite nonnegative duration.";
        if (!IsRestDurationValid) return false;
        if (_source == SourceMode.Csv)
            return DefenseWaveCsvMapper.TryMap(_csv, _enemies, out waves, out error);
        error = $"Wave catalog key '_source': unsupported value {(int)_source}.";
        if (_source != SourceMode.Legacy) return false;
        error = "Legacy wave catalog key '_waves': missing formations.";
        if (_waves == null || _waves.Length == 0) return false;
        var result = new DefenseWavePlan[_waves.Length];
        int total = 0;
        for (int index = 0; index < _waves.Length; ++index)
        {
            DefenseWaveDefinition definition = _waves[index];
            error = $"Legacy wave {index + 1}: missing/invalid definition or enemy count total exceeds Int32 capacity.";
            if (!definition || !definition.IsValid) return false;
            try { total = checked(total + definition.EnemyCount); }
            catch (OverflowException) { return false; }
            var group = new DefenseWaveSpawnGroup(0, index + 1, 1, "basic", definition.EnemyCount,
                definition.SpawnIntervalSeconds, legacyPrefab, legacyStatDefinition);
            result[index] = new DefenseWavePlan(new[] { group }, definition.EnemyCount);
        }
        waves = result;
        error = null;
        return true;
    }

    private bool IsRestDurationValid => _restDurationSeconds >= 0f &&
        !float.IsNaN(_restDurationSeconds) && !float.IsInfinity(_restDurationSeconds);

    private void OnValidate()
    {
        _restDurationSeconds = float.IsNaN(_restDurationSeconds) || float.IsInfinity(_restDurationSeconds)
            ? 60f : Mathf.Max(0f, _restDurationSeconds);
    }
}
