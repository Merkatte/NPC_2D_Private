using System.Collections.Generic;
using UnityEngine;

// Scene-local dispatcher owned by the plot registry. Selectors retain role eligibility
// and queue construction, and each plot remains the authority for its individual slots.
internal sealed class ConstructionPriorityManager
{
    internal sealed class BuilderRegistration
    {
        internal readonly WorkerNPC Worker;
        internal readonly BuilderActionSelector Selector;
        internal readonly NPCStat Stat;
        internal readonly long RegistrationOrder;
        internal readonly uint Lifetime;
        internal bool IsRegistered { get; private set; } = true;
        internal readonly HashSet<long> EligibleProjects = new HashSet<long>();
        internal uint ActionVersion;
        internal int NavigationRevision;

        internal BuilderRegistration(WorkerNPC worker, NPCStat stat, BuilderActionSelector selector, long order)
        {
            Worker = worker;
            Selector = selector;
            Stat = stat;
            RegistrationOrder = order;
            Lifetime = worker.LifetimeVersion;
        }

        internal bool IsLive => IsRegistered && Worker && Selector && Worker.LifetimeVersion == Lifetime;
        internal void Revoke() => IsRegistered = false;

        internal bool Refresh(IReadOnlyList<ConstructionProjectInfo> priorities, bool reset)
        {
            if (reset) EligibleProjects.Clear();
            if (!IsLive) return false;
            bool changed = ActionVersion != Worker.ActionVersion || NavigationRevision != Selector.NavigationRevision;
            ActionVersion = Worker.ActionVersion;
            NavigationRevision = Selector.NavigationRevision;
            for (int i = 0; i < priorities.Count; ++i)
            {
                ConstructionProjectInfo project = priorities[i];
                if (project.IsCurrent && project.Plot.CanReserve && Selector.CanTakePriority(Worker, Stat, project.Plot, i + 1))
                    changed |= EligibleProjects.Add(project.ApplicationOrder);
                else
                    changed |= EligibleProjects.Remove(project.ApplicationOrder);
            }
            return changed;
        }
    }

    private readonly struct ReachableSlot
    {
        internal readonly BuilderRegistration Builder;
        internal readonly ConstructionProjectInfo Project;
        internal readonly int Rank;
        internal readonly int Slot;
        private readonly float _distance;

        internal ReachableSlot(BuilderRegistration builder, ConstructionProjectInfo project, int rank, int slot, float distance)
        {
            Builder = builder;
            Project = project;
            Rank = rank;
            Slot = slot;
            _distance = distance;
        }

        internal static int Compare(ReachableSlot left, ReachableSlot right)
        {
            int order = left.Rank.CompareTo(right.Rank);
            if (order != 0) return order;
            order = left._distance.CompareTo(right._distance);
            if (order != 0) return order;
            order = left.Builder.RegistrationOrder.CompareTo(right.Builder.RegistrationOrder);
            return order != 0 ? order : left.Slot.CompareTo(right.Slot);
        }
    }

    private readonly BuildingPlotRegistry _registry;
    private readonly List<BuilderRegistration> _builders = new List<BuilderRegistration>();
    private readonly List<Vector3> _path = new List<Vector3>();
    private readonly List<ReachableSlot> _routes = new List<ReachableSlot>();
    private long _registrationOrder;
    private bool _dirty = true;

    internal ConstructionPriorityManager(BuildingPlotRegistry registry) => _registry = registry;

    internal void Register(WorkerNPC worker, NPCStat stat, BuilderActionSelector selector)
    {
        for (int i = 0; i < _builders.Count; ++i)
        {
            BuilderRegistration previous = _builders[i];
            if (previous.Worker != worker) continue;
            if (previous.IsLive && previous.Selector == selector && previous.Stat == stat) return;
            previous.Revoke();
            _builders.RemoveAt(i);
            break;
        }
        _builders.Add(new BuilderRegistration(worker, stat, selector, ++_registrationOrder));
        Invalidate();
    }

    internal void Unregister(WorkerNPC worker)
    {
        for (int i = 0; i < _builders.Count; ++i)
        {
            if (_builders[i].Worker != worker) continue;
            _builders[i].Revoke();
            _builders.RemoveAt(i);
            Invalidate();
            return;
        }
    }

    internal void Invalidate() => _dirty = true;

    internal void Dispatch()
    {
        IReadOnlyList<ConstructionProjectInfo> priorities = _registry.Priorities;
        bool changed = _dirty;
        foreach (BuilderRegistration builder in _builders) changed |= builder.Refresh(priorities, _dirty);
        if (!changed) return;
        _dirty = false;
        _routes.Clear();
        for (int i = 0; i < priorities.Count; ++i) CollectRoutes(priorities[i], i + 1);
        _routes.Sort(ReachableSlot.Compare);

        // Try remaining pairs after a slot fills or a builder's handoff fails. A
        // successful handoff changes its action version, invalidating its other offers.
        foreach (ReachableSlot route in _routes)
        {
            BuilderRegistration builder = route.Builder;
            if (builder.IsLive)
                builder.Selector.TryDispatch(builder, route.Project, route.Rank, route.Slot);
        }
        _routes.Clear();
    }

    private void CollectRoutes(ConstructionProjectInfo project, int rank)
    {
        if (!project.IsCurrent || !project.Plot.CanReserve) return;
        for (int slot = 0; slot < project.Definition.MaxWorkers; ++slot)
        {
            if (!project.Plot.TryGetFreeWorkPosition(slot, out Vector3 position)) continue;
            foreach (BuilderRegistration builder in _builders)
            {
                if (!builder.IsLive || !builder.EligibleProjects.Contains(project.ApplicationOrder)) continue;
                if (builder.Selector.TryMeasureRoute(builder.Worker.Component.Position, position, _path, out float distance))
                    _routes.Add(new ReachableSlot(builder, project, rank, slot, distance));
            }
        }
    }
}
