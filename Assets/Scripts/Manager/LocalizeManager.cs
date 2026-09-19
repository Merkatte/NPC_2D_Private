using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class LocalizeManager : MonoBehaviour
{
    [SerializeField] private LocalizeData _data;
    private readonly HashSet<LocalizeKey> _reportedMissingKeys = new HashSet<LocalizeKey>();

    public static LocalizeManager Instance { get; private set; }
    public string CurrentLanguage => "ko";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetInstance() => Instance = null;

    private void Awake()
    {
        if (Instance && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        if (transform.parent)
        {
            Debug.LogError("LocalizeManager '" + name + "' must be on a dedicated root GameObject.", this);
            enabled = false;
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        if (!_data)
            Debug.LogError("LocalizeManager '" + name + "': missing _data LocalizeData.", this);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public bool TryGetText(LocalizeKey key, out string text)
    {
        text = string.Empty;
        return _data && _data.TryGetText(key, CurrentLanguage, out text);
    }

    public string GetText(LocalizeKey key)
    {
        if (TryGetText(key, out string text))
            return text;
        if (_reportedMissingKeys.Add(key))
            Debug.LogError("LocalizeManager '" + name + "': missing " + CurrentLanguage + " text for " + key + ".", this);
        return "[Missing:" + key + "]";
    }
}
