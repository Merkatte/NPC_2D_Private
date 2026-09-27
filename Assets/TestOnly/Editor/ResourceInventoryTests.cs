using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class ResourceInventoryTests
{
    private const int Gold = ResourceManager.GoldItemId;
    private const int Wood = 9;
    private const int Stone = 10;

    [Test]
    public void FailedMultiSpendAndOverflowExchange_PreserveAllBalancesAndNotifications()
    {
        using (var fixture = new Fixture())
        {
            ResourceManager resources = fixture.Resources;
            Assert.That(resources.TryRefund(new Dictionary<int, int> { { Gold, int.MaxValue }, { Wood, 20 }, { Stone, 10 } }), Is.True);
            int events = 0;
            resources.ResourcesChanged += () => ++events;
            Assert.That(resources.TrySpend(new Dictionary<int, int> { { Wood, 5 }, { Stone, 11 } }), Is.False);
            Assert.That(resources.TryExchange(Amount(Wood, 5), Amount(Gold, 1)), Is.False);
            Assert.That(resources.TryDeposit(Gold, 1, out int accepted), Is.False);
            Assert.That(accepted, Is.Zero);
            Assert.That(resources.GetQuantity(Wood), Is.EqualTo(20));
            Assert.That(resources.GetQuantity(Stone), Is.EqualTo(10));
            Assert.That(resources.GetQuantity(Gold), Is.EqualTo(int.MaxValue));
            Assert.That(resources.UsedCapacity, Is.EqualTo(30));
            Assert.That(events, Is.Zero);
        }
    }

    [Test]
    public void Exchange_CommitsDebitsAndCreditsTogether_AndNoOpEmitsNothing()
    {
        using (var fixture = new Fixture())
        {
            ResourceManager resources = fixture.Resources;
            Assert.That(resources.TryRefund(Amount(Wood, 20)), Is.True);
            int events = 0;
            int observedWood = -1;
            int observedGold = -1;
            resources.ResourcesChanged += () => { ++events; observedWood = resources.GetQuantity(Wood); observedGold = resources.GetQuantity(Gold); };
            Assert.That(resources.TryExchange(Amount(Wood, 7), Amount(Gold, 21)), Is.True);
            Assert.That(observedWood, Is.EqualTo(13));
            Assert.That(observedGold, Is.EqualTo(21));
            Assert.That(resources.UsedCapacity, Is.EqualTo(13));
            Assert.That(resources.TrySpend(new Dictionary<int, int>()), Is.True);
            Assert.That(resources.TryRefund(new Dictionary<int, int>()), Is.True);
            Assert.That(resources.TryExchange(Amount(Wood, 3), Amount(Wood, 3)), Is.True);
            Assert.That(resources.TrySpend(Amount(Wood, 0)), Is.False);
            Assert.That(resources.TryRefund(Amount(-1, 2)), Is.False);
            Assert.That(events, Is.EqualTo(1));
        }
    }

    [Test]
    public void CapacityRegistration_IsIdempotent_AndRefundPreservesOverCapacityStock()
    {
        using (var fixture = new Fixture())
        {
            ResourceManager resources = fixture.Resources;
            int events = 0;
            resources.ResourcesChanged += () => ++events;
            Assert.That(resources.Capacity, Is.Zero);
            Assert.That(resources.RegisterCapacity(fixture.Root, 5), Is.True);
            Assert.That(resources.RegisterCapacity(fixture.Root, 5), Is.True);
            Assert.That(events, Is.EqualTo(1));
            Assert.That(resources.RegisterCapacity(fixture.Root, 3), Is.True);
            Assert.That(resources.Capacity, Is.EqualTo(3));
            Assert.That(resources.TryRefund(Amount(Wood, 8)), Is.True);
            Assert.That(resources.TryDeposit(Stone, 1, out int accepted), Is.False);
            Assert.That(accepted, Is.Zero);
            Assert.That(resources.TryDeposit(Gold, 50, out accepted), Is.True);
            Assert.That(accepted, Is.EqualTo(50));
            resources.UnregisterCapacity(fixture.Root);
            int beforeRepeat = events;
            resources.UnregisterCapacity(fixture.Root);
            Assert.That(events, Is.EqualTo(beforeRepeat));
            Assert.That(resources.Capacity, Is.Zero);
            Assert.That(resources.UsedCapacity, Is.EqualTo(8));
            Assert.That(resources.GetQuantity(Wood), Is.EqualTo(8));
        }
    }

    [Test]
    public void PartialDeposit_ConservesCargo_AndProductionRemovalCallbackReleasesCapacity()
    {
        using (var fixture = new Fixture())
        {
            ResourceManager resources = fixture.Resources;
            var deposit = fixture.Root.AddComponent<WarehouseDepositPoint>();
            deposit.Configure(resources, 3);
            Assert.That(deposit.RegisterCapacity(), Is.True);
            var cargo = new WorkerInventory(10, _ => { });
            Assert.That(cargo.TryAdd(Wood, 7, out _), Is.True);
            int events = 0;
            int observedCargo = -1;
            int observedStock = -1;
            resources.ResourcesChanged += () => { ++events; observedCargo = cargo.GetQuantity(Wood); observedStock = resources.GetQuantity(Wood); };
            Assert.That(deposit.TryInteract(new InteractionRequest(ActionType.Deposit, cargo: cargo), out _), Is.True);
            Assert.That(cargo.GetQuantity(Wood), Is.EqualTo(4));
            Assert.That(resources.GetQuantity(Wood), Is.EqualTo(3));
            Assert.That(observedCargo + observedStock, Is.EqualTo(7));
            Assert.That(observedCargo, Is.EqualTo(4));
            Assert.That(events, Is.EqualTo(1));
            Assert.That(deposit.CanInteract(ActionType.Deposit), Is.False);
            Assert.That(deposit.TryInteract(new InteractionRequest(ActionType.Deposit, cargo: cargo), out _), Is.False);
            Assert.That(cargo.GetQuantity(Wood) + resources.GetQuantity(Wood), Is.EqualTo(7));
            deposit.enabled = false;
            Assert.That(resources.Capacity, Is.EqualTo(3), "Temporary disable must retain warehouse capacity.");
            // Plain MonoBehaviour callbacks are not dispatched by this synchronous EditMode fixture.
            // Exercise the production callback contract; engine dispatch remains human Play QA.
            MethodInfo removal = typeof(WarehouseDepositPoint).GetMethod("OnDestroy", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(removal, Is.Not.Null);
            removal.Invoke(deposit, null);
            Assert.That(resources.Capacity, Is.Zero, "The production removal callback must release capacity.");
            removal.Invoke(deposit, null);
            Assert.That(resources.Capacity, Is.Zero, "Repeated removal callback must be idempotent.");
            UnityEngine.Object.DestroyImmediate(deposit);
            Assert.That(!deposit, Is.True, "The temporary deposit component must be destroyed.");
            Assert.That(resources.Capacity, Is.Zero, "Actual warehouse removal must release its capacity.");
            Assert.That(resources.GetQuantity(Wood), Is.EqualTo(3));
        }
    }

    [Test]
    public void NestedNotificationScope_FlushesOnce_AndReentryDoesNotNestCallbacks()
    {
        using (var fixture = new Fixture())
        {
            ResourceManager resources = fixture.Resources;
            int events = 0;
            int depth = 0;
            int maximumDepth = 0;
            bool domainCommitted = false;
            bool observedCommitted = true;
            resources.ResourcesChanged += () =>
            {
                ++depth;
                maximumDepth = Math.Max(maximumDepth, depth);
                ++events;
                observedCommitted &= domainCommitted;
                if (events == 1)
                    resources.TryRefund(Amount(Stone, 1));
                --depth;
            };
            IDisposable outer = resources.DeferNotifications();
            using (resources.DeferNotifications())
            {
                Assert.That(resources.TryRefund(Amount(Wood, 2)), Is.True);
                Assert.That(resources.TryRefund(Amount(Gold, 4)), Is.True);
            }
            Assert.That(events, Is.Zero);
            domainCommitted = true;
            outer.Dispose();
            outer.Dispose();
            Assert.That(events, Is.EqualTo(2));
            Assert.That(maximumDepth, Is.EqualTo(1));
            Assert.That(observedCommitted, Is.True);
            Assert.That(resources.GetQuantity(Stone), Is.EqualTo(1));
        }
    }

    private static Dictionary<int, int> Amount(int id, int quantity) => new Dictionary<int, int> { { id, quantity } };

    [Test]
    public void ThrowingObserver_CannotUndoCommittedTrade_OrSuppressFollowingObserver()
    {
        using (var fixture = new Fixture())
        {
            int followingCalls = 0;
            fixture.Resources.ResourcesChanged += () => throw new InvalidOperationException("Expected resource observer fixture failure");
            fixture.Resources.ResourcesChanged += () => ++followingCalls;
            LogAssert.Expect(LogType.Exception, new Regex("Expected resource observer fixture failure"));
            Assert.That(fixture.Resources.TryRefund(Amount(Gold, 5)), Is.True);
            Assert.That(fixture.Resources.GetQuantity(Gold), Is.EqualTo(5));
            Assert.That(followingCalls, Is.EqualTo(1));
        }
    }

    private sealed class Fixture : IDisposable
    {
        public readonly GameObject Root;
        public readonly ResourceManager Resources;
        private readonly ItemDataContext _items;
        private readonly TextAsset _csv;

        public Fixture()
        {
            Root = new GameObject("ResourceChecks") { hideFlags = HideFlags.HideAndDontSave };
            _items = ScriptableObject.CreateInstance<ItemDataContext>();
            _csv = new TextAsset("id,category,itemName,itemDescription,healthDelta,hungerDelta,thirstDelta,fatigueDelta,moodDelta,sellPrice,usesStorage,showInWarehouse\n"
                + "8,Resource,Gold,gold,0,0,0,0,0,0,false,false\n9,Resource,Wood,wood,0,0,0,0,0,0,true,true\n10,Resource,Stone,stone,0,0,0,0,0,0,true,true\n");
            Set(_items, "_itemTextData", _csv);
            Resources = Root.AddComponent<ResourceManager>();
            Set(Resources, "_itemDataContext", _items);
        }

        public void Dispose()
        {
            UnityEngine.Object.DestroyImmediate(Root);
            UnityEngine.Object.DestroyImmediate(_items);
            UnityEngine.Object.DestroyImmediate(_csv);
        }
    }

    private static void Set(object owner, string fieldName, object value)
    {
        FieldInfo field = owner.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.That(field, Is.Not.Null, fieldName);
        field.SetValue(owner, value);
    }
}
