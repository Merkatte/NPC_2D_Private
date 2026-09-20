using UnityEngine;

/// <summary>Explicit manual fixed-message controls. Does not open a hover or mutate gameplay state.</summary>
public sealed class TestNPCMessageControls : MonoBehaviour
{
    [SerializeField] private NPCMessageSource _source;
    [SerializeField] private LocalizeKey _firstKey = LocalizeKey.Tutorial_Welcome;
    [SerializeField] private LocalizeKey _replacementKey = LocalizeKey.UI_Confirm;

    private uint _currentToken;
    private uint _previousToken;
    private string _result = "Assign a source; messages remain hover-only.";

    private void OnDisable()
    {
        if (_source && _currentToken != 0)
            _source.TryClearFixedMessage(_currentToken);
        _currentToken = 0;
        _previousToken = 0;
    }

    private void OnGUI()
    {
        GUILayout.BeginArea(new Rect(20f, 210f, 420f, 210f), GUI.skin.box);
        GUILayout.Label("NPC fixed message probe (hover to view)");
        if (!_source)
        {
            GUILayout.Label("Assign NPCMessageSource in Inspector.");
            GUILayout.EndArea();
            return;
        }
        if (GUILayout.Button("Set first message"))
            SetMessage(_firstKey);
        if (GUILayout.Button("Replace message"))
            SetMessage(_replacementKey);
        if (GUILayout.Button("Try stale token"))
            _result = _source.TryClearFixedMessage(_previousToken) ? "Old token cleared." : "Old token rejected.";
        if (GUILayout.Button("Clear current token"))
            _result = _source.TryClearFixedMessage(_currentToken) ? "Cleared; next hover uses thoughts." : "Clear rejected.";
        GUILayout.Label(_result);
        GUILayout.EndArea();
    }

    private void SetMessage(LocalizeKey key)
    {
        if (!_source.TrySetFixedMessage(key, out uint token))
        {
            _result = "Set rejected; check initialized worker and valid key.";
            return;
        }
        _previousToken = _currentToken;
        _currentToken = token;
        _result = "Set token " + token + "; hover source to view.";
    }
}
