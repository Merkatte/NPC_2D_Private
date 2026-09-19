using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Manual lifecycle controls, confined to LocalizeTest. No gameplay dependency.</summary>
public sealed class TestLocalizeControls : MonoBehaviour
{
    [SerializeField] private LocalizeText _legacy;
    [SerializeField] private LocalizeText _tmp;
    [SerializeField] private RectTransform _runtimeParent;
    private readonly LocalizeKey[] _keys =
        { LocalizeKey.UI_Confirm, LocalizeKey.NPC_Thought_Hungry, LocalizeKey.Tutorial_Welcome };
    private int _keyIndex;
    private LocalizeText _spawned;
    private string _result = "Compare Text and TMP. Built-in fonts may lack Korean glyphs.";

    private void OnGUI()
    {
        GUILayout.BeginArea(new Rect(20, 20, 600, 180), GUI.skin.box);
        GUILayout.Label("LocalizeTest / ko");
        if (GUILayout.Button("Next key (Text + TMP)"))
        {
            _keyIndex = (_keyIndex + 1) % _keys.Length;
            _legacy.SetKey(_keys[_keyIndex]);
            _tmp.SetKey(_keys[_keyIndex]);
        }
        if (GUILayout.Button("Toggle TMP active"))
            _tmp.gameObject.SetActive(!_tmp.gameObject.activeSelf);
        if (GUILayout.Button("Create / replace runtime Text"))
        {
            if (_spawned)
                Destroy(_spawned.gameObject);
            _spawned = Instantiate(_legacy, _runtimeParent);
            ((RectTransform)_spawned.transform).anchoredPosition = new Vector2(0, -140);
            _spawned.SetKey(_keys[_keyIndex]);
        }
        if (GUILayout.Button("Check duplicate + scene transition"))
            StartCoroutine(CheckPersistence());
        GUILayout.Label(_result);
        GUILayout.EndArea();
    }

    private IEnumerator CheckPersistence()
    {
        LocalizeManager original = LocalizeManager.Instance;
        if (!original)
        {
            _result = "FAIL: manager missing";
            yield break;
        }
        GameObject duplicate = Instantiate(original.gameObject);
        yield return null;
        bool duplicateRemoved = !duplicate && LocalizeManager.Instance == original;
        Scene previous = SceneManager.GetActiveScene();
        Scene temporary = SceneManager.CreateScene("LocalizePersistenceProbe");
        SceneManager.SetActiveScene(temporary);
        bool persistent = LocalizeManager.Instance == original && original.gameObject.scene != previous &&
            original.gameObject.scene != temporary;
        SceneManager.SetActiveScene(previous);
        yield return SceneManager.UnloadSceneAsync(temporary);
        _result = duplicateRemoved && persistent && LocalizeManager.Instance == original
            ? "PASS: duplicate removed, same persistent manager after scene transition"
            : "FAIL: singleton persistence";
        Debug.Log("TestLocalizeControls: " + _result, this);
    }
}
