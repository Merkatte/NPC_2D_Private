using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

// Shared by observation bubbles and interactive dialogue; owns only its text tween.
public sealed class MessageTyping
{
    private Text _text;
    private string _fullText;
    private Tween _tween;
    public bool IsTyping => _tween != null;
    public void Begin(Text text, string value, float interval)
    {
        Reset();
        _text = text; _fullText = value;
        if (!_text) return;
        _text.text = value;
        if (interval <= 0f || string.IsNullOrEmpty(value)) return;
        _text.text = string.Empty;
        _tween = _text.DOText(value, value.Length * interval, _text.supportRichText)
            .SetEase(Ease.Linear).SetUpdate(true).OnComplete(() => _tween = null);
    }
    public void Reveal() { _tween?.Kill(); _tween = null; if (_text) _text.text = _fullText; }
    public void Pause() => _tween?.Pause();
    public void Resume() => _tween?.Play();
    public void Reset() { Reveal(); _text = null; _fullText = null; }
}
