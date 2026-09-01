using System;
using UnityEngine;

[Serializable]
public struct CropVisualStage
{
    [SerializeField] [Range(0f, 1f)] private float _normalizedThreshold;
    [SerializeField] private Sprite _sprite;

    public float NormalizedThreshold => _normalizedThreshold;
    public Sprite Sprite => _sprite;
    public bool IsValid => _normalizedThreshold >= 0f && _normalizedThreshold <= 1f && _sprite;
}
