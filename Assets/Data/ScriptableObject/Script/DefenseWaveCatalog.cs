using UnityEngine;

[CreateAssetMenu(menuName = "NPC/Defense/Wave Catalog")]
public sealed class DefenseWaveCatalog : ScriptableObject
{
    [SerializeField] private DefenseWaveDefinition[] _waves;
    [SerializeField, Min(0f)] private float _restDurationSeconds = 60f;

    public float RestDurationSeconds => _restDurationSeconds;

    public bool IsValid
    {
        get
        {
            if (_waves == null || _waves.Length == 0 || _restDurationSeconds < 0f ||
                float.IsNaN(_restDurationSeconds) || float.IsInfinity(_restDurationSeconds)) return false;
            foreach (DefenseWaveDefinition wave in _waves)
                if (!wave || !wave.IsValid) return false;
            return true;
        }
    }

    public bool TryGetWave(int waveNumber, out DefenseWaveDefinition wave)
    {
        wave = null;
        if (waveNumber < 1 || !IsValid) return false;
        wave = _waves[Mathf.Min(waveNumber - 1, _waves.Length - 1)];
        return true;
    }

    private void OnValidate()
    {
        _restDurationSeconds = float.IsNaN(_restDurationSeconds) || float.IsInfinity(_restDurationSeconds)
            ? 60f : Mathf.Max(0f, _restDurationSeconds);
    }
}
