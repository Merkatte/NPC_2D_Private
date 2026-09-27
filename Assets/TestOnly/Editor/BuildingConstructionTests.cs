using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public sealed class BuildingConstructionTests
{
    [Test]
    public void Definition_CopiesCost_AndExposesNoMutableCostView()
    {
        var cost = new Dictionary<int, int> { { 8, 100 }, { 9, 20 } };
        var definition = new BuildingDefinition(1, BuildingType.Warehouse, "Warehouse", 100f, 2, 500, cost);
        cost[8] = 1;
        cost.Clear();
        Assert.That(definition.Cost[8], Is.EqualTo(100));
        var mutable = definition.Cost as IDictionary<int, int>;
        Assert.That(mutable == null || mutable.IsReadOnly, Is.True);
        if (mutable != null)
            Assert.Throws<NotSupportedException>(() => mutable[8] = 1);
    }

    [TestCase("2,Inn,Bad,NaN,2,0", "2,8,100")]
    [TestCase("2,Inn,Bad,100,0,0", "2,8,100")]
    [TestCase("1,Inn,Duplicate,100,2,0", "2,8,100")]
    [TestCase("2,Inn,Bad,100,2,0", "2,999,100")]
    [TestCase("2,Inn,Bad,100,2,0", "2,8,0")]
    [TestCase("2,Inn,Unwired,100,2,0", "2,8,100")]
    public void InvalidLaterDefinitionOrAssets_PublishNoPartialCatalog(string laterRow, string laterCost)
    {
        using (var fixture = new Fixture(laterRow, laterCost))
        {
            Assert.That(fixture.Buildings.TryInitialize(out string error), Is.False);
            Assert.That(error, Is.Not.Null.And.Not.Empty);
            Assert.That(fixture.Buildings.Definitions, Is.Empty);
            Assert.That(fixture.Buildings.Work, Is.Null);
            Assert.That(fixture.Buildings.TryGetBuildingDefinition(1, out _), Is.False);
            Assert.That(fixture.Buildings.TryInitialize(out _), Is.False);
        }
    }

    [Test]
    public void ReservationBeforeWork_CancelsForFullRefund_AndStaleLeaseCannotTouchNewConstruction()
    {
        using (var fixture = new Fixture())
        {
            BuildingPlot plot = fixture.Plot;
            fixture.Start();
            Assert.That(plot.TryReserve(out ConstructionReservation old), Is.True);
            Assert.That(plot.HasWorkStarted, Is.False);
            Assert.That(plot.TryCancelConstruction(out _), Is.True);
            Assert.That(fixture.Resources.GetQuantity(8), Is.EqualTo(1000));
            Assert.That(fixture.Resources.GetQuantity(9), Is.EqualTo(200));
            Assert.That(old.IsValid, Is.False);
            fixture.Start();
            Assert.That(plot.TryReserve(out ConstructionReservation current), Is.True);
            Assert.That(plot.TryInteract(Work(old, 10f), out _), Is.False);
            old.Dispose();
            old.Dispose();
            Assert.That(current.IsValid, Is.True);
            Assert.That(plot.ReservedWorkers, Is.EqualTo(1));
            Assert.That(plot.Progress, Is.Zero);
            current.Dispose();
            current.Dispose();
            Assert.That(plot.ReservedWorkers, Is.Zero);
            Assert.That(plot.TryReserve(out ConstructionReservation replacement), Is.True);
            Assert.That(replacement.IsValid, Is.True);
        }
    }

    [Test]
    public void TwoWorkers_AccumulateWorkAtCapacity_AndCancellationFloorsEachRefund()
    {
        using (var fixture = new Fixture())
        {
            fixture.Start();
            BuildingPlot plot = fixture.Plot;
            Assert.That(plot.TryReserve(out ConstructionReservation first), Is.True);
            Assert.That(plot.TryReserve(out ConstructionReservation second), Is.True);
            Assert.That(plot.TryReserve(out _), Is.False);
            Assert.That(plot.TryInteract(Work(first, 0f), out _), Is.False);
            Assert.That(plot.HasWorkStarted, Is.False);
            Assert.That(plot.TryInteract(Work(first, 10f), out _), Is.True);
            Assert.That(plot.TryInteract(Work(second, 25f), out _), Is.True);
            Assert.That(plot.Progress, Is.EqualTo(0.35f).Within(0.00001f));
            first.Dispose();
            second.Dispose();
            Assert.That(plot.HasWorkStarted, Is.True);
            IReadOnlyDictionary<int, int> refund = plot.GetExpectedRefund();
            Assert.That(refund[8], Is.EqualTo(65));
            Assert.That(refund[9], Is.EqualTo(13));
            Assert.That(refund[10], Is.EqualTo(6));
            Assert.That(plot.TryCancelConstruction(out _), Is.True);
            Assert.That(fixture.Resources.GetQuantity(8), Is.EqualTo(965));
            Assert.That(fixture.Resources.GetQuantity(9), Is.EqualTo(193));
            Assert.That(fixture.Resources.GetQuantity(10), Is.EqualTo(196));
            Assert.That(plot.TryCancelConstruction(out _), Is.False);
        }
    }

    [Test]
    public void ResourceObservers_SeeCommittedPlot_AndCannotReenterPaymentOrRefund()
    {
        using (var fixture = new Fixture())
        {
            int events = 0;
            bool reentered = false;
            BuildingPlotState observed = BuildingPlotState.Empty;
            fixture.Resources.ResourcesChanged += () =>
            {
                ++events;
                observed = fixture.Plot.State;
                reentered |= fixture.Plot.TryCancelConstruction(out _);
                reentered |= fixture.Plot.TryStartConstruction(1, out _);
            };
            fixture.Start();
            Assert.That(observed, Is.EqualTo(BuildingPlotState.UnderConstruction));
            Assert.That(events, Is.EqualTo(1));
            Assert.That(reentered, Is.False);
            Assert.That(fixture.Plot.TryStartConstruction(1, out _), Is.False);
            Assert.That(fixture.Plot.TryCancelConstruction(out _), Is.True);
            Assert.That(observed, Is.EqualTo(BuildingPlotState.Empty));
            Assert.That(events, Is.EqualTo(2));
            Assert.That(reentered, Is.False);
            Assert.That(fixture.Resources.GetQuantity(8), Is.EqualTo(1000));
        }
    }

    [Test]
    public void RefundOverflow_PreservesConstructionAndLease_ForLaterRetry()
    {
        using (var fixture = new Fixture())
        {
            fixture.Start();
            Assert.That(fixture.Plot.TryReserve(out ConstructionReservation lease), Is.True);
            Assert.That(fixture.Resources.TryRefund(new Dictionary<int, int> { { 8, int.MaxValue - 900 } }), Is.True);
            Assert.That(fixture.Plot.TryCancelConstruction(out _), Is.False);
            Assert.That(fixture.Plot.State, Is.EqualTo(BuildingPlotState.UnderConstruction));
            Assert.That(lease.IsValid, Is.True);
            Assert.That(fixture.Resources.GetQuantity(9), Is.EqualTo(180));
            Assert.That(fixture.Resources.TrySpend(new Dictionary<int, int> { { 8, 100 } }), Is.True);
            Assert.That(fixture.Plot.TryCancelConstruction(out _), Is.True);
            Assert.That(fixture.Resources.GetQuantity(8), Is.EqualTo(int.MaxValue));
            Assert.That(fixture.Resources.GetQuantity(9), Is.EqualTo(200));
        }
    }

    [Test]
    public void FailedCompletion_PreservesFullProgress_AndRetryNeverChargesAgain()
    {
        using (var fixture = new Fixture())
        {
            fixture.Start();
            Assert.That(fixture.Plot.TryReserve(out ConstructionReservation lease), Is.True);
            Assert.That(fixture.Plot.TryInteract(Work(lease, 100f), out _), Is.True);
            Assert.That(fixture.Plot.State, Is.EqualTo(BuildingPlotState.UnderConstruction));
            Assert.That(fixture.Plot.Progress, Is.EqualTo(1f));
            Assert.That(fixture.Plot.HasCompletionFailure, Is.True);
            Assert.That(lease.IsValid, Is.False);
            Assert.That(fixture.Plot.TryReserve(out _), Is.False);
            Assert.That(fixture.Plot.TryInteract(Work(lease, 10f), out _), Is.False);
            Assert.That(fixture.Plot.TryRetryCompletion(out _), Is.False);
            Assert.That(fixture.Plot.Progress, Is.EqualTo(1f));
            Assert.That(fixture.Resources.GetQuantity(8), Is.EqualTo(900));
            Assert.That(fixture.Plot.GetExpectedRefund(), Is.Empty);
            Assert.That(fixture.Plot.TryCancelConstruction(out _), Is.True);
            Assert.That(fixture.Resources.GetQuantity(8), Is.EqualTo(900));
        }
    }

    private static InteractionRequest Work(ConstructionReservation lease, float amount)
        => new InteractionRequest(ActionType.Build, strength: amount, reservation: lease);

    [Test]
    public void FailedCompletion_AfterRepairRegistersOnce_AndProductionRemovalCallbackPreservesOtherFacilities()
    {
        using (var fixture = new Fixture())
        {
            fixture.Start();
            Assert.That(fixture.Plot.TryReserve(out ConstructionReservation lease), Is.True);
            Assert.That(fixture.Plot.TryInteract(Work(lease, 100f), out _), Is.True);
            Assert.That(fixture.Plot.HasCompletionFailure, Is.True);
            Assert.That(fixture.Plot.State, Is.EqualTo(BuildingPlotState.UnderConstruction));
            fixture.RepairFactory(out DestinationDB destinations, out InteractableManager interactions);
            DestinationInfo existing = CreateWarehouse(fixture.Plot.transform, fixture.Resources, interactions,
                new Vector3(20f, 0f, 0f));
            Assert.That(destinations.Register(existing), Is.True);
            int events = 0;
            bool observedCompleted = false;
            fixture.Resources.ResourcesChanged += () =>
            {
                ++events;
                observedCompleted = fixture.Plot.State == BuildingPlotState.Completed
                    && fixture.Plot.CompletedFacility && fixture.Resources.Capacity == 1000;
            };
            Assert.That(fixture.Plot.TryRetryCompletion(out string reason), Is.True, reason);
            CompletedBuildingFacility completed = fixture.Plot.CompletedFacility;
            Assert.That(completed, Is.Not.Null);
            Assert.That(fixture.Plot.State, Is.EqualTo(BuildingPlotState.Completed));
            Assert.That(fixture.Plot.Progress, Is.EqualTo(1f));
            Assert.That(fixture.Plot.HasCompletionFailure, Is.False);
            Assert.That(fixture.Plot.ReservedWorkers, Is.Zero);
            Assert.That(lease.IsValid, Is.False);
            Assert.That(completed.Entrance.position, Is.EqualTo(fixture.Plot.transform.position));
            Assert.That(events, Is.EqualTo(1));
            Assert.That(observedCompleted, Is.True);
            Assert.That(fixture.Resources.Capacity, Is.EqualTo(1000));
            Assert.That(fixture.Resources.GetQuantity(8), Is.EqualTo(900));
            Assert.That(fixture.Resources.GetQuantity(9), Is.EqualTo(180));
            Assert.That(fixture.Resources.GetQuantity(10), Is.EqualTo(190));
            Assert.That(destinations.GetCandidates(BuildingType.Warehouse).Count, Is.EqualTo(2));
            Assert.That(destinations.TrySelectNearest(BuildingType.Warehouse, ActionType.Deposit, fixture.Plot.transform.position,
                out DestinationInfo selected, out IInteractionProvider provider, out _), Is.True);
            Assert.That(selected.DestinationObject, Is.SameAs(completed.Provider.gameObject));
            Assert.That(provider, Is.SameAs(completed.Provider));
            Assert.That(fixture.Plot.TryRetryCompletion(out _), Is.False);
            Assert.That(fixture.Plot.TryStartConstruction(1, out _), Is.False);
            Assert.That(fixture.Plot.TryCancelConstruction(out _), Is.False);
            Assert.That(fixture.Plot.TryInteract(Work(lease, 1f), out _), Is.False);
            lease.Dispose();
            Assert.That(fixture.Plot.CompletedFacility, Is.SameAs(completed));
            Assert.That(destinations.GetCandidates(BuildingType.Warehouse).Count, Is.EqualTo(2));
            Assert.That(fixture.Resources.Capacity, Is.EqualTo(1000));
            Assert.That(fixture.Resources.GetQuantity(8), Is.EqualTo(900));
            Assert.That(events, Is.EqualTo(1));
            GameObject completedOwner = completed.Provider.gameObject;
            // Synchronous EditMode fixtures invoke the actual production callback explicitly.
            // This covers callback cleanup, not Unity's runtime callback dispatch.
            MethodInfo removal = typeof(CompletedBuildingFacility).GetMethod("OnDestroy", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(removal, Is.Not.Null);
            removal.Invoke(completed, null);
            Assert.That(destinations.GetCandidates(BuildingType.Warehouse).Count, Is.EqualTo(1));
            Assert.That(destinations.GetCandidates(BuildingType.Warehouse)[0], Is.SameAs(existing));
            Assert.That(fixture.Resources.Capacity, Is.EqualTo(500));
            Assert.That(interactions.TryGetInteractionProvider(completedOwner, ActionType.Deposit, out _), Is.False);
            Assert.That(interactions.TryGetInteractionProvider(existing.DestinationObject, ActionType.Deposit, out _), Is.True);
            removal.Invoke(completed, null);
            Assert.That(destinations.GetCandidates(BuildingType.Warehouse).Count, Is.EqualTo(1));
            Assert.That(fixture.Resources.Capacity, Is.EqualTo(500), "Repeated callback must preserve the other warehouse's capacity.");
            Assert.That(interactions.TryGetInteractionProvider(existing.DestinationObject, ActionType.Deposit, out _), Is.True);
            UnityEngine.Object.DestroyImmediate(completed.gameObject);
            Assert.That(!completed && !completedOwner, Is.True, "The temporary facility and provider owner must be destroyed.");
            Assert.That(destinations.GetCandidates(BuildingType.Warehouse).Count, Is.EqualTo(1));
            Assert.That(destinations.GetCandidates(BuildingType.Warehouse)[0], Is.SameAs(existing));
            Assert.That(fixture.Resources.Capacity, Is.EqualTo(500));
            Assert.That(interactions.TryGetInteractionProvider(completedOwner, ActionType.Deposit, out _), Is.False);
            Assert.That(interactions.TryGetInteractionProvider(existing.DestinationObject, ActionType.Deposit, out _), Is.True);
        }
    }

    [Test]
    public void BuildAction_RepeatedCleanupAndReuse_ReleaseOnlyItsCurrentLease()
    {
        using (var fixture = new Fixture())
        {
            fixture.Start();
            Assert.That(fixture.Plot.TryReserve(out ConstructionReservation first), Is.True);
            var action = new BuildAction();
            action.Init(new ActionContext(null, null, provider: fixture.Plot, request: Work(first, 1f)));
            action.Stop();
            action.Stop();
            action.Clear();
            action.Clear();
            Assert.That(first.IsValid, Is.False);
            Assert.That(fixture.Plot.ReservedWorkers, Is.Zero);
            Assert.That(fixture.Plot.TryReserve(out ConstructionReservation second), Is.True);
            action.Init(new ActionContext(null, null, provider: fixture.Plot, request: Work(second, 1f)));
            first.Dispose();
            Assert.That(second.IsValid, Is.True);
            action.Clear();
            Assert.That(second.IsValid, Is.False);
            Assert.That(fixture.Plot.ReservedWorkers, Is.Zero);
            Assert.That(fixture.Plot.HasWorkStarted, Is.False);
        }
    }

    [Test]
    public void MultipleFacilities_SelectNearestAvailableIdentity_WithStableDistanceTies()
    {
        using (var fixture = new Fixture())
        {
            GameObject root = fixture.Resources.gameObject;
            InteractableManager interactions = root.AddComponent<InteractableManager>();
            DestinationDB destinations = root.AddComponent<DestinationDB>();
            Set(destinations, "_interactableManager", interactions);
            DestinationInfo first = CreateWarehouse(root.transform, fixture.Resources, interactions, new Vector3(-2f, 0f, 0f));
            DestinationInfo second = CreateWarehouse(root.transform, fixture.Resources, interactions, new Vector3(2f, 0f, 0f));
            Assert.That(destinations.Register(first), Is.True);
            Assert.That(destinations.Register(second), Is.True);
            Assert.That(destinations.Register(first), Is.True);
            Assert.That(destinations.GetCandidates(BuildingType.Warehouse).Count, Is.EqualTo(2));
            Assert.That(destinations.TrySelectNearest(BuildingType.Warehouse, ActionType.Deposit, Vector3.zero,
                out DestinationInfo selected, out IInteractionProvider provider, out Vector3 position), Is.True);
            Assert.That(selected, Is.SameAs(first));
            Assert.That(provider, Is.SameAs(first.DestinationObject.GetComponent<WarehouseDepositPoint>()));
            Assert.That(position, Is.EqualTo(first.DestinationLoc.position));
            Assert.That(destinations.TrySelectNearest(BuildingType.Warehouse, ActionType.Deposit, Vector3.right,
                out selected, out provider, out position), Is.True);
            Assert.That(selected, Is.SameAs(second));
            Assert.That(provider, Is.SameAs(second.DestinationObject.GetComponent<WarehouseDepositPoint>()));
            second.DestinationObject.SetActive(false);
            Assert.That(destinations.TrySelectNearest(BuildingType.Warehouse, ActionType.Deposit, Vector3.right,
                out selected, out _, out _), Is.True);
            Assert.That(selected, Is.SameAs(first));
            destinations.Unregister(first);
            destinations.Unregister(first);
            Assert.That(destinations.GetCandidates(BuildingType.Warehouse).Count, Is.EqualTo(1));
            Assert.That(destinations.TrySelectNearest(BuildingType.Warehouse, ActionType.Deposit, Vector3.zero,
                out _, out _, out _), Is.False);
        }
    }

    private static DestinationInfo CreateWarehouse(Transform parent, ResourceManager resources,
        InteractableManager interactions, Vector3 position)
    {
        var owner = new GameObject("TemporaryWarehouse");
        owner.transform.SetParent(parent);
        owner.transform.position = position;
        var deposit = owner.AddComponent<WarehouseDepositPoint>();
        deposit.Configure(resources, 500);
        Assert.That(deposit.RegisterCapacity(), Is.True);
        Assert.That(interactions.TryRegisterProvider(deposit), Is.True);
        return new DestinationInfo { BuildingType = BuildingType.Warehouse, DestinationLoc = owner.transform, DestinationObject = owner };
    }

    private sealed class Fixture : IDisposable
    {
        public readonly BuildingPlot Plot;
        public readonly ResourceManager Resources;
        public readonly BuildingDataContext Buildings;
        private readonly List<UnityEngine.Object> _owned = new List<UnityEngine.Object>();
        private readonly IDataManager _previousDataManager = DataManager.instance;
        private readonly DataManager _dataManager;
        private readonly ItemDataContext _items;
        private readonly CompletedBuildingFacility _template;
        private readonly BuildingFactory _factory;

        public Fixture(string laterRow = null, string laterCost = null)
        {
            var root = Own(new GameObject("ConstructionChecks") { hideFlags = HideFlags.HideAndDontSave });
            ItemDataContext items = Own(ScriptableObject.CreateInstance<ItemDataContext>());
            _items = items;
            Set(items, "_itemTextData", Own(new TextAsset("id,category,itemName,itemDescription,healthDelta,hungerDelta,thirstDelta,fatigueDelta,moodDelta,sellPrice,usesStorage,showInWarehouse\n"
                + "8,Resource,Gold,gold,0,0,0,0,0,0,false,false\n9,Resource,Wood,wood,0,0,0,0,0,0,true,true\n10,Resource,Stone,stone,0,0,0,0,0,0,true,true\n")));
            Resources = root.AddComponent<ResourceManager>();
            Set(Resources, "_itemDataContext", items);
            Buildings = Own(ScriptableObject.CreateInstance<BuildingDataContext>());
            Set(Buildings, "_items", items);
            Set(Buildings, "_buildingData", Own(new TextAsset("buildingId,buildingType,displayName,requiredWork,maxWorkers,providedCapacity\n1,Warehouse,Warehouse,100,2,500\n" + laterRow)));
            Set(Buildings, "_buildingCosts", Own(new TextAsset("buildingId,itemId,quantity\n1,8,100\n1,9,20\n1,10,10\n" + laterCost)));
            Set(Buildings, "_builderWork", Own(new TextAsset("workPerSecond,hungerPerSecond,thirstPerSecond,fatiguePerSecond\n10,0.3,0.3,0.3\n")));
            var template = Own(new GameObject("TemporaryFacilityTemplate") { hideFlags = HideFlags.HideAndDontSave });
            template.SetActive(false);
            var prefab = template.AddComponent<CompletedBuildingFacility>();
            _template = prefab;
            Texture2D texture = Own(new Texture2D(2, 2));
            Sprite icon = Own(Sprite.Create(texture, new Rect(0, 0, 2, 2), Vector2.zero));
            Type entryType = typeof(BuildingDataContext).GetNestedType("AssetEntry", BindingFlags.NonPublic);
            object entry = Activator.CreateInstance(entryType, true);
            Set(entry, "_buildingId", 1);
            Set(entry, "_prefab", prefab);
            Set(entry, "_icon", icon);
            Array entries = Array.CreateInstance(entryType, 1);
            entries.SetValue(entry, 0);
            Set(Buildings, "_assets", entries);
            DataManager data = root.AddComponent<DataManager>();
            _dataManager = data;
            Set(data, "_costInfos", Array.Empty<CostInfo>());
            Set(data, "_buildingDataContext", Buildings);
            Set(data, "_itemDataContext", items);
            Plot = root.AddComponent<BuildingPlot>();
            Set(Plot, "_dataManager", data);
            Set(Plot, "_resources", Resources);
            // A present but unconfigured factory exercises recoverable completion failure.
            _factory = root.AddComponent<BuildingFactory>();
            Set(Plot, "_factory", _factory);
            Set(Plot, "_registry", root.AddComponent<BuildingPlotRegistry>());
            Set(Plot, "_entrance", root.transform);
            Set(Plot, "_workArea", root.AddComponent<BoxCollider2D>());
            var first = new GameObject("FirstWorkPosition");
            first.transform.SetParent(root.transform);
            var second = new GameObject("SecondWorkPosition");
            second.transform.SetParent(root.transform);
            second.transform.localPosition = Vector3.right;
            Set(Plot, "_workPositions", new[] { first.transform, second.transform });
            Assert.That(Resources.TryRefund(new Dictionary<int, int> { { 8, 1000 }, { 9, 200 }, { 10, 200 } }), Is.True);
        }

        public void Start() => Assert.That(Plot.TryStartConstruction(1, out string reason), Is.True, reason);

        public void RepairFactory(out DestinationDB destinations, out InteractableManager interactions)
        {
            var warehouse = _template.gameObject.AddComponent<WarehouseDepositPoint>();
            Set(_template, "_entrance", _template.transform);
            Set(_template, "_warehouse", warehouse);
            Set(_template, "_provider", warehouse);
            interactions = Plot.gameObject.AddComponent<InteractableManager>();
            destinations = Plot.gameObject.AddComponent<DestinationDB>();
            Set(destinations, "_interactableManager", interactions);
            Set(_factory, "_buildingDataContext", Buildings);
            Set(_factory, "_resourceManager", Resources);
            Set(_factory, "_destinationDB", destinations);
            Set(_factory, "_interactableManager", interactions);
            Set(_factory, "_itemDataContext", _items);
            Set(_factory, "_cropCatalog", Own(ScriptableObject.CreateInstance<CropCatalog>()));
        }

        private T Own<T>(T value) where T : UnityEngine.Object { _owned.Add(value); return value; }

        public void Dispose()
        {
            if (ReferenceEquals(DataManager.instance, _dataManager))
                DataManager.instance = _previousDataManager;
            // Destroy scene objects before the transient data they reference.
            foreach (UnityEngine.Object value in _owned)
                if (value is GameObject) UnityEngine.Object.DestroyImmediate(value);
            for (int i = _owned.Count - 1; i >= 0; --i)
                if (_owned[i]) UnityEngine.Object.DestroyImmediate(_owned[i]);
        }
    }

    private static void Set(object owner, string fieldName, object value)
    {
        FieldInfo field = owner.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.That(field, Is.Not.Null, fieldName);
        field.SetValue(owner, value);
    }
}
