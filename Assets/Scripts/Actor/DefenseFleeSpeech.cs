using UnityEngine;
using UnityEngine.UI;

// Actor message presentation; selectors/actions only publish the flee transition.
public sealed class DefenseFleeSpeech : MonoBehaviour
{
    [SerializeField] private DefenseActor _actor;
    [SerializeField] private GameObject _panel;
    [SerializeField] private Text _text;
    private float _remaining;
    private Vector3 _panelScale;
    private void Awake() { if (_panel) _panelScale = _panel.transform.localScale; }
    private void OnEnable()
    {
        if (_actor) { _actor.FleeStarted += ShowPanic; _actor.Downed += Hide; }
        Hide();
    }
    private void OnDisable()
    {
        if (_actor) { _actor.FleeStarted -= ShowPanic; _actor.Downed -= Hide; }
        Hide();
    }
    private void ShowPanic()
    {
        if (!_actor || !_actor.Settings || !_panel || !_text) return;
        _text.text = _actor.Settings.FleeMessage;
        _remaining = _actor.Settings.SpeechDuration;
        _panel.SetActive(true);
    }
    private void Update()
    {
        if (_remaining <= 0f) return;
        _remaining -= Time.deltaTime;
        if (_remaining <= 0f) Hide();
    }
    private void LateUpdate()
    {
        if (!_panel || !_panel.activeSelf) return;
        Transform panel = _panel.transform;
        panel.rotation = Quaternion.identity;
        float sign = panel.parent && panel.parent.lossyScale.x < 0f ? -1f : 1f;
        panel.localScale = new Vector3(Mathf.Abs(_panelScale.x) * sign, _panelScale.y, _panelScale.z);
    }
    private void Hide() { _remaining = 0f; if (_panel) _panel.SetActive(false); }
}
