using DG.Tweening;
using UnityEngine;

public sealed class ConstructionVisual : MonoBehaviour
{
    [SerializeField] private BuildingPlot _plot;
    [SerializeField] private GameObject _emptyVisual;
    [SerializeField] private SpriteRenderer _materials;
    [SerializeField] private SpriteRenderer _lowerFrame;
    [SerializeField] private SpriteRenderer _upperFrame;
    [SerializeField] private SpriteRenderer _cover;
    [SerializeField] private Vector4 _stageThresholds = new Vector4(0f, 0.25f, 0.5f, 0.75f);
    [SerializeField, Min(0f)] private float _enterDuration = 0.4f;
    [SerializeField, Min(0f)] private float _exitDuration = 0.2f;
    [SerializeField, Min(0f)] private float _dropHeight = 0.35f;
    [SerializeField, Min(0f)] private float _riseHeight = 0.2f;
    [SerializeField, Range(0f, 1f)] private float _unfoldScale = 0.8f;
    [SerializeField, Range(0f, 1f)] private float _exitScale = 0.92f;

    private Layer[] _layers;
    private BuildingPlotState _displayedState;
    private int _displayedStage;
    private bool _hasSnapshot;

    private void Awake()
    {
        OnValidate();
        SpriteRenderer[] renderers = { _materials, _lowerFrame, _upperFrame, _cover };
        bool isConfigured = _plot && _plot.gameObject == gameObject && _emptyVisual
            && _emptyVisual.transform.parent == transform;
        for (int i = 0; i < renderers.Length; ++i)
        {
            SpriteRenderer renderer = renderers[i];
            isConfigured &= renderer && renderer.sprite && renderer.transform.parent == transform
                && renderer.gameObject != _emptyVisual && !renderer.GetComponent<Collider2D>();
            for (int j = 0; j < i; ++j)
                isConfigured &= renderer != renderers[j];
        }
        if (!isConfigured)
        {
            Debug.LogError($"ConstructionVisual '{name}': assign _plot on this root, _emptyVisual and four distinct sibling sprite layers without colliders.", this);
            enabled = false;
            return;
        }
        _layers = new Layer[renderers.Length];
        for (int i = 0; i < renderers.Length; ++i)
            _layers[i] = new Layer(renderers[i]);
    }

    private void OnEnable()
    {
        if (_layers == null || !_plot) return;
        _plot.StateChanged += HandleStateChanged;
        ApplySnapshot(false);
    }

    private void OnDisable()
    {
        if (_plot) _plot.StateChanged -= HandleStateChanged;
        if (_layers == null) return;
        ApplySnapshot(false);
        _hasSnapshot = false;
    }

    private void HandleStateChanged()
    {
        if (isActiveAndEnabled) ApplySnapshot(true);
    }

    private void ApplySnapshot(bool animate)
    {
        BuildingPlotState state = _plot ? _plot.State : BuildingPlotState.Empty;
        int stage = -1;
        if (state == BuildingPlotState.UnderConstruction)
            for (int i = 0; i < _layers.Length; ++i)
                if (_plot.Progress >= _stageThresholds[i]) stage = i;

        if (animate && _hasSnapshot && state == _displayedState && stage == _displayedStage) return;
        bool isNewConstruction = _hasSnapshot && state == BuildingPlotState.UnderConstruction
            && _displayedState != BuildingPlotState.UnderConstruction;
        _displayedState = state;
        _displayedStage = stage;
        _hasSnapshot = true;
        if (_emptyVisual) _emptyVisual.SetActive(state == BuildingPlotState.Empty);

        for (int i = 0; i < _layers.Length; ++i)
        {
            // A restart cannot inherit a pending cancellation's hide callback or pose.
            if (isNewConstruction) _layers[i].Snap(false);
            bool visible = i <= stage;
            if (!animate) _layers[i].Snap(visible);
            else if (visible) _layers[i].Show(_enterDuration, i == 0 ? _dropHeight : -_riseHeight,
                i == 0 ? 1f : _unfoldScale, i == 0 ? Ease.OutBack : Ease.OutCubic);
            else _layers[i].Hide(_exitDuration, _exitScale);
        }
    }

    private void OnValidate()
    {
        _stageThresholds.x = Mathf.Clamp01(_stageThresholds.x);
        _stageThresholds.y = Mathf.Clamp(_stageThresholds.y, _stageThresholds.x, 1f);
        _stageThresholds.z = Mathf.Clamp(_stageThresholds.z, _stageThresholds.y, 1f);
        _stageThresholds.w = Mathf.Clamp(_stageThresholds.w, _stageThresholds.z, 1f);
        _enterDuration = Mathf.Max(0f, _enterDuration);
        _exitDuration = Mathf.Max(0f, _exitDuration);
        _dropHeight = Mathf.Max(0f, _dropHeight);
        _riseHeight = Mathf.Max(0f, _riseHeight);
        _unfoldScale = Mathf.Clamp01(_unfoldScale);
        _exitScale = Mathf.Clamp01(_exitScale);
    }

    private sealed class Layer
    {
        private readonly SpriteRenderer _renderer;
        private readonly Vector3 _restingPosition;
        private readonly Vector3 _restingScale;
        private readonly Color _restingColor;
        private Tween _tween;
        private bool _isVisible;

        public Layer(SpriteRenderer renderer)
        {
            _renderer = renderer;
            _restingPosition = renderer.transform.localPosition;
            _restingScale = renderer.transform.localScale;
            _restingColor = renderer.color;
        }

        public void Snap(bool visible)
        {
            StopMotion();
            _isVisible = visible;
            if (!_renderer) return;
            _renderer.transform.localPosition = _restingPosition;
            _renderer.transform.localScale = _restingScale;
            _renderer.color = _restingColor;
            _renderer.gameObject.SetActive(visible);
        }

        public void Show(float duration, float verticalOffset, float unfoldScale, Ease ease)
        {
            if (_isVisible || !_renderer) return;
            Snap(true);
            if (duration <= 0f) return;
            Vector3 startPosition = _restingPosition + Vector3.up * verticalOffset;
            Vector3 startScale = Vector3.Scale(_restingScale, new Vector3(1f, unfoldScale, 1f));
            ApplyPose(startPosition, startScale, 0f);
            float progress = 0f;
            _tween = DOTween.To(() => progress, value =>
            {
                progress = value;
                ApplyPose(Vector3.LerpUnclamped(startPosition, _restingPosition, value),
                    Vector3.LerpUnclamped(startScale, _restingScale, value), Mathf.Clamp01(value));
            }, 1f, duration).SetEase(ease).SetUpdate(false).OnComplete(() => Snap(true));
        }

        public void Hide(float duration, float exitScale)
        {
            if (!_isVisible || !_renderer) return;
            StopMotion();
            _isVisible = false;
            if (duration <= 0f) { Snap(false); return; }
            Vector3 startPosition = _renderer.transform.localPosition;
            Vector3 startScale = _renderer.transform.localScale;
            float startAlpha = _renderer.color.a;
            float progress = 0f;
            _tween = DOTween.To(() => progress, value =>
            {
                progress = value;
                if (!_renderer) return;
                _renderer.transform.localPosition = Vector3.Lerp(startPosition, _restingPosition, value);
                _renderer.transform.localScale = Vector3.Lerp(startScale, _restingScale * exitScale, value);
                Color color = _restingColor;
                color.a = Mathf.Lerp(startAlpha, 0f, value);
                _renderer.color = color;
            }, 1f, duration).SetEase(Ease.InCubic).SetUpdate(false).OnComplete(() => Snap(false));
        }

        private void ApplyPose(Vector3 position, Vector3 scale, float alpha)
        {
            if (!_renderer) return;
            _renderer.transform.localPosition = position;
            _renderer.transform.localScale = scale;
            Color color = _restingColor;
            color.a *= alpha;
            _renderer.color = color;
        }

        private void StopMotion()
        {
            _tween?.Kill();
            _tween = null;
        }
    }
}
