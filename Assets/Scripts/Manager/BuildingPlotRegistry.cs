using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class BuildingPlotRegistry : MonoBehaviour
{
    [SerializeField] private BuildingPlot[] _plots = Array.Empty<BuildingPlot>();
    private IReadOnlyList<BuildingPlot> _view;
    private long _nextOrder;
    private const int PriorityCapacity = 1;
    private readonly List<ConstructionProjectInfo> _priorities = new List<ConstructionProjectInfo>(PriorityCapacity);
    private IReadOnlyList<ConstructionProjectInfo> _priorityView;
    private readonly Dictionary<BuildingPlot, long> _lastCompletion = new Dictionary<BuildingPlot, long>();
    private ConstructionPriorityManager _dispatcher;
    private bool _hasWorkshop;

    public IReadOnlyList<BuildingPlot> Plots => _view ?? (_view = Array.AsReadOnly(_plots));
    public IReadOnlyList<ConstructionProjectInfo> Priorities => _priorityView ?? (_priorityView = _priorities.AsReadOnly());
    public event Action PrioritiesChanged;
    public event Action<ConstructionProjectInfo> ProjectCompleted;
    internal ConstructionPriorityManager Dispatcher => _dispatcher ?? (_dispatcher = new ConstructionPriorityManager(this));
    internal long NextApplicationOrder() => ++_nextOrder;
    internal event Action ConstructionAvailabilityChanged;

    internal bool CanStartSingleton(BuildingPlot requester, BuildingType type, out string reason)
    {
        if (Array.IndexOf(_plots, requester) < 0)
        {
            reason = "이 부지는 마을 건설 목록에 등록되지 않았습니다.";
            return false;
        }
        foreach (BuildingPlot plot in _plots)
        {
            if (plot && plot.HasSingleton(type))
            {
                reason = type == BuildingType.MilitaryTraining
                    ? "군사훈련소는 마을에 한 채만 지을 수 있습니다. 기존 공사를 취소하거나 파괴된 훈련소의 잔해를 정리하세요."
                    : "건설 공방은 마을에 한 채만 지을 수 있습니다. 기존 공사를 취소하거나 파괴된 공방의 잔해를 정리하세요.";
                return false;
            }
        }
        reason = null;
        return true;
    }

    internal void NotifyConstructionAvailabilityChanged()
        => Publish(ConstructionAvailabilityChanged);

    public bool CanSetPriority(BuildingPlot plot)
        => _hasWorkshop && isActiveAndEnabled && plot && plot.isActiveAndEnabled && plot.Registry == this &&
            plot.State == BuildingPlotState.UnderConstruction && !plot.HasCompletionFailure &&
            Array.IndexOf(_plots, plot) >= 0;

    public int GetPriorityRank(BuildingPlot plot)
    {
        if (plot)
            for (int i = 0; i < _priorities.Count; ++i)
                if (_priorities[i].Plot == plot && _priorities[i].ApplicationOrder == plot.ApplicationOrder)
                    return i + 1;
        return 0;
    }

    public bool TrySetPriority(BuildingPlot plot, long applicationOrder)
    {
        if (Time.timeScale <= 0f || !CanSetPriority(plot) || plot.ApplicationOrder != applicationOrder)
            return false;
        if (GetPriorityRank(plot) > 0) return true;
        var project = new ConstructionProjectInfo(plot);
        if (_priorities.Count < PriorityCapacity) _priorities.Add(project);
        else _priorities[_priorities.Count - 1] = project;
        NotifyPrioritiesChanged();
        return true;
    }

    public bool TryClearPriority(BuildingPlot plot, long applicationOrder)
    {
        int rank = GetPriorityRank(plot);
        if (Time.timeScale <= 0f || rank == 0 || applicationOrder != plot.ApplicationOrder) return false;
        _priorities.RemoveAt(rank - 1);
        NotifyPrioritiesChanged();
        return true;
    }

    private void OnEnable() => RefreshPriorities();
    private void OnDisable()
    {
        _hasWorkshop = false;
        _priorities.Clear();
        NotifyPrioritiesChanged();
    }

    internal void NotifyPlotLifecycleChanged(BuildingPlot plot)
    {
        if (!isActiveAndEnabled || Array.IndexOf(_plots, plot) < 0) return;
        _dispatcher?.Invalidate();
        RefreshPriorities();
    }

    private void RefreshPriorities()
    {
        bool hasWorkshop = Array.Exists(_plots, plot => plot && plot.IsOperationalWorkshop);
        bool changed = hasWorkshop != _hasWorkshop;
        _hasWorkshop = hasWorkshop;
        for (int i = _priorities.Count - 1; i >= 0; --i)
        {
            if (_hasWorkshop && _priorities[i].IsCurrent) continue;
            _priorities.RemoveAt(i);
            changed = true;
        }
        if (changed) NotifyPrioritiesChanged();
    }

    private void NotifyPrioritiesChanged()
    {
        _dispatcher?.Invalidate();
        Publish(PrioritiesChanged);
    }

    internal void NotifyProjectCompleted(BuildingPlot plot)
    {
        if (!plot || plot.Registry != this || plot.State != BuildingPlotState.Completed || plot.Definition == null)
            return;
        var project = new ConstructionProjectInfo(plot);
        if (_lastCompletion.TryGetValue(plot, out long order) && order >= project.ApplicationOrder) return;
        _lastCompletion[plot] = project.ApplicationOrder;
        NotifyPlotLifecycleChanged(plot);
        if (ProjectCompleted == null) return;
        foreach (Action<ConstructionProjectInfo> observer in ProjectCompleted.GetInvocationList())
        {
            try { observer(project); }
            catch (Exception exception) { Debug.LogException(exception, this); }
        }
    }

    private void LateUpdate()
    {
        if (Time.timeScale > 0f && _priorities.Count > 0) _dispatcher?.Dispatch();
    }

    private void Publish(Action observers)
    {
        if (observers == null) return;
        foreach (Action observer in observers.GetInvocationList())
        {
            try { observer(); }
            catch (Exception exception) { Debug.LogException(exception, this); }
        }
    }
}
