using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class FarmCropPresenter : MonoBehaviour
{
    private const float DefaultCascadeInterval = 0.08f;

    [SerializeField] private FarmWorkSite _workSite;
    [SerializeField] private CropVisualAnimator[] _visuals;
    [SerializeField] private int _visualSeed = 1;
    [SerializeField] [Min(0f)] private float _cascadeInterval = DefaultCascadeInterval;

    private readonly Queue<PresentationCommand> _commands = new Queue<PresentationCommand>();

    private CropVisualAnimator[] _validVisuals;
    private System.Random _visualRandom;
    private WaitForSeconds _cascadeWait;
    private Coroutine _commandRunner;
    private FarmProductionDefinition _observedDefinition;
    private int _observedStageIndex = -1;
    private int[] _shuffleOrder;
    private int _runningVisualCount;

    private void Awake()
    {
        if (!_workSite)
        {
            Debug.LogError($"FarmCropPresenter '{name}': missing FarmWorkSite reference.", this);
            enabled = false;
            return;
        }

        BuildVisualCache();
        if (_validVisuals.Length == 0)
        {
            Debug.LogError($"FarmCropPresenter '{name}': no unique CropVisualAnimator references are configured.", this);
            enabled = false;
            return;
        }

        _visualRandom = new System.Random(_visualSeed);
        _shuffleOrder = new int[_validVisuals.Length];
        _cascadeWait = _cascadeInterval > 0f ? new WaitForSeconds(_cascadeInterval) : null;
    }

    private void OnEnable()
    {
        if (!enabled || !_workSite || _validVisuals == null || _validVisuals.Length == 0)
            return;

        _workSite.StateChanged += HandleWorkSiteStateChanged;
        SynchronizeImmediate();
    }

    private void OnDisable()
    {
        if (_workSite)
            _workSite.StateChanged -= HandleWorkSiteStateChanged;

        StopAllCoroutines();
        _commands.Clear();
        _commandRunner = null;
        _runningVisualCount = 0;
    }

    private void OnValidate()
    {
        _cascadeInterval = Mathf.Max(0f, _cascadeInterval);
    }

    private void HandleWorkSiteStateChanged()
    {
        FarmProductionDefinition currentDefinition = _workSite.CurrentDefinition;
        FarmWorkPhase currentPhase = _workSite.Phase;
        int currentStageIndex = ResolveStageIndex(currentDefinition, currentPhase);

        if (!currentDefinition)
        {
            if (_observedDefinition)
                Enqueue(PresentationCommand.HarvestDisappear());

            CaptureState(null, -1);
            return;
        }

        if (!_observedDefinition || _observedDefinition != currentDefinition)
        {
            if (IsBusy)
                Enqueue(PresentationCommand.ImmediateSync(currentDefinition, currentStageIndex));
            else
                ApplyImmediate(currentDefinition, currentStageIndex);

            CaptureState(currentDefinition, currentStageIndex);
            return;
        }

        if (currentStageIndex > _observedStageIndex)
        {
            for (int stageIndex = _observedStageIndex + 1; stageIndex <= currentStageIndex; ++stageIndex)
                Enqueue(PresentationCommand.StageTransition(currentDefinition, stageIndex));
        }

        CaptureState(currentDefinition, currentStageIndex);
    }

    private void SynchronizeImmediate()
    {
        FarmProductionDefinition currentDefinition = _workSite.CurrentDefinition;
        FarmWorkPhase currentPhase = _workSite.Phase;
        int currentStageIndex = ResolveStageIndex(currentDefinition, currentPhase);

        if (currentDefinition)
            ApplyImmediate(currentDefinition, currentStageIndex);
        else
            HideAllImmediate();

        CaptureState(currentDefinition, currentStageIndex);
    }

    private int ResolveStageIndex(FarmProductionDefinition definition, FarmWorkPhase phase)
    {
        if (!definition || definition.VisualStageCount == 0)
            return -1;

        return phase == FarmWorkPhase.Harvesting
            ? definition.VisualStageCount - 1
            : definition.GetVisualStageIndex(_workSite.NormalizedProgress);
    }

    private void ApplyImmediate(FarmProductionDefinition definition, int stageIndex)
    {
        if (!definition || !definition.TryGetVisualStage(stageIndex, out CropVisualStage stage))
        {
            Debug.LogError($"FarmCropPresenter '{name}': cannot resolve visual stage {stageIndex}.", this);
            HideAllImmediate();
            return;
        }

        for (int i = 0; i < _validVisuals.Length; ++i)
        {
            CropVisualAnimator visual = _validVisuals[i];
            if (visual.TryConfigure(definition.VisualController))
                visual.ShowImmediate(stage.Sprite);
            else
                visual.HideImmediate();
        }
    }

    private void HideAllImmediate()
    {
        if (_validVisuals == null)
            return;

        for (int i = 0; i < _validVisuals.Length; ++i)
            _validVisuals[i].HideImmediate();
    }

    private void Enqueue(PresentationCommand command)
    {
        _commands.Enqueue(command);

        if (_commandRunner == null)
            _commandRunner = StartCoroutine(RunCommandQueue());
    }

    private IEnumerator RunCommandQueue()
    {
        while (_commands.Count > 0)
        {
            PresentationCommand command = _commands.Dequeue();
            switch (command.Kind)
            {
                case PresentationCommandKind.StageTransition:
                    yield return RunStageTransition(command.Definition, command.StageIndex);
                    break;

                case PresentationCommandKind.HarvestDisappear:
                    yield return RunHarvestDisappear();
                    break;

                case PresentationCommandKind.ImmediateSync:
                    ApplyImmediate(command.Definition, command.StageIndex);
                    break;
            }
        }

        _commandRunner = null;
    }

    private IEnumerator RunStageTransition(FarmProductionDefinition definition, int stageIndex)
    {
        if (!definition || !definition.TryGetVisualStage(stageIndex, out CropVisualStage stage))
            yield break;

        ShuffleVisualOrder();
        _runningVisualCount = 0;

        for (int i = 0; i < _shuffleOrder.Length; ++i)
        {
            CropVisualAnimator visual = _validVisuals[_shuffleOrder[i]];
            if (!visual.TryConfigure(definition.VisualController))
                continue;

            ++_runningVisualCount;
            StartCoroutine(RunVisualStageTransition(visual, stage.Sprite));

            if (_cascadeWait != null && i < _shuffleOrder.Length - 1)
                yield return _cascadeWait;
        }

        while (_runningVisualCount > 0)
            yield return null;
    }

    private IEnumerator RunHarvestDisappear()
    {
        ShuffleVisualOrder();
        _runningVisualCount = 0;

        for (int i = 0; i < _shuffleOrder.Length; ++i)
        {
            CropVisualAnimator visual = _validVisuals[_shuffleOrder[i]];
            ++_runningVisualCount;
            StartCoroutine(RunVisualHarvestDisappear(visual));

            if (_cascadeWait != null && i < _shuffleOrder.Length - 1)
                yield return _cascadeWait;
        }

        while (_runningVisualCount > 0)
            yield return null;
    }

    private IEnumerator RunVisualStageTransition(CropVisualAnimator visual, Sprite nextSprite)
    {
        yield return visual.PlayStageTransition(nextSprite);
        --_runningVisualCount;
    }

    private IEnumerator RunVisualHarvestDisappear(CropVisualAnimator visual)
    {
        yield return visual.PlayHarvestDisappear();
        --_runningVisualCount;
    }

    private void ShuffleVisualOrder()
    {
        for (int i = 0; i < _shuffleOrder.Length; ++i)
            _shuffleOrder[i] = i;

        for (int i = _shuffleOrder.Length - 1; i > 0; --i)
        {
            int swapIndex = _visualRandom.Next(i + 1);
            int value = _shuffleOrder[i];
            _shuffleOrder[i] = _shuffleOrder[swapIndex];
            _shuffleOrder[swapIndex] = value;
        }
    }

    private void BuildVisualCache()
    {
        if (_visuals == null || _visuals.Length == 0)
        {
            _validVisuals = System.Array.Empty<CropVisualAnimator>();
            return;
        }

        var uniqueVisuals = new HashSet<CropVisualAnimator>();
        var validVisuals = new List<CropVisualAnimator>(_visuals.Length);

        for (int i = 0; i < _visuals.Length; ++i)
        {
            CropVisualAnimator visual = _visuals[i];
            if (!visual)
            {
                Debug.LogWarning($"FarmCropPresenter '{name}': visual entry {i} is null and will be ignored.", this);
                continue;
            }

            if (!uniqueVisuals.Add(visual))
            {
                Debug.LogWarning(
                    $"FarmCropPresenter '{name}': duplicate visual '{visual.name}' at entry {i} will be ignored.",
                    this);
                continue;
            }

            validVisuals.Add(visual);
        }

        _validVisuals = validVisuals.ToArray();
    }

    private void CaptureState(FarmProductionDefinition definition, int stageIndex)
    {
        _observedDefinition = definition;
        _observedStageIndex = stageIndex;
    }

    private bool IsBusy => _commandRunner != null || _commands.Count > 0;

    private enum PresentationCommandKind
    {
        StageTransition,
        HarvestDisappear,
        ImmediateSync,
    }

    private readonly struct PresentationCommand
    {
        public PresentationCommandKind Kind { get; }
        public FarmProductionDefinition Definition { get; }
        public int StageIndex { get; }

        private PresentationCommand(
            PresentationCommandKind kind,
            FarmProductionDefinition definition,
            int stageIndex)
        {
            Kind = kind;
            Definition = definition;
            StageIndex = stageIndex;
        }

        public static PresentationCommand StageTransition(FarmProductionDefinition definition, int stageIndex)
            => new PresentationCommand(PresentationCommandKind.StageTransition, definition, stageIndex);

        public static PresentationCommand HarvestDisappear()
            => new PresentationCommand(PresentationCommandKind.HarvestDisappear, null, -1);

        public static PresentationCommand ImmediateSync(FarmProductionDefinition definition, int stageIndex)
            => new PresentationCommand(PresentationCommandKind.ImmediateSync, definition, stageIndex);
    }
}
