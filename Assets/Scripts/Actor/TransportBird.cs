using System;
using UnityEngine;

public class TransportBird : MonoBehaviour
{
    [SerializeField] private SpriteRenderer _spriteRenderer;
    [SerializeField] private Sprite[] _sprites;

    private enum BirdImg
    {
        Normal,
        Seek,
        Fly
    };
    
    
}
