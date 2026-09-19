using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "LocalizeData", menuName = "Scriptable Objects/LocalizeData")]
public sealed class LocalizeData : ScriptableObject
{
    [SerializeField] private TextAsset _csv;

    private static int _playSession;
    [NonSerialized] private int _cachedSession = -1;
    [NonSerialized] private bool _hasAttemptedLoad;
    [NonSerialized] private Dictionary<string, Dictionary<LocalizeKey, string>> _texts;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void BeginPlaySession()
    {
        unchecked { _playSession++; }
    }

    private void OnEnable() => InvalidateCache();
    private void OnValidate() => InvalidateCache();

    public void InvalidateCache()
    {
        // Editor imports during play take effect in the next session, never halfway through a lookup.
        if (Application.isPlaying && _cachedSession == _playSession && _hasAttemptedLoad)
            return;
        _texts = null;
        _hasAttemptedLoad = false;
    }

    public bool TryGetText(LocalizeKey key, string languageCode, out string text)
    {
        text = string.Empty;
        if (_cachedSession != _playSession)
        {
            _texts = null;
            _hasAttemptedLoad = false;
            _cachedSession = _playSession;
        }
        if (!_hasAttemptedLoad)
            Load();
        if (_texts == null || string.IsNullOrEmpty(languageCode) ||
            !_texts.TryGetValue(languageCode, out Dictionary<LocalizeKey, string> language) ||
            !language.TryGetValue(key, out string value) || string.IsNullOrEmpty(value))
            return false;
        text = value;
        return true;
    }

    private void Load()
    {
        _hasAttemptedLoad = true;
        if (!_csv)
        {
            Debug.LogError("LocalizeData '" + name + "': missing _csv TextAsset.", this);
            return;
        }
        if (!LocalizeCsvParser.TryParse(_csv.text, out LocalizeCsvParser.Table table, out string error) ||
            !LocalizeKeyValidation.TryValidate(table, out error))
        {
            Debug.LogError("LocalizeData '" + name + "': " + error, this);
            return;
        }

        var texts = new Dictionary<string, Dictionary<LocalizeKey, string>>(StringComparer.Ordinal);
        for (int i = 0; i < table.Languages.Count; i++)
        {
            var language = new Dictionary<LocalizeKey, string>();
            foreach (LocalizeCsvParser.Row row in table.Rows)
                language.Add((LocalizeKey)row.Id, row.Texts[i]);
            texts.Add(table.Languages[i], language);
        }
        _texts = texts;
    }
}
