using ForgeFlow.Core;
using ForgeFlow.Core.Data;
using ForgeFlow.Core.Entities;
using ForgeFlow.Core.Events;
using ForgeFlow.Core.Proto;
using ForgeFlow.Core.Proto.Logic;
using ForgeFlow.Core.Systems;

namespace ForgeFlow.Tests;

public readonly struct TestEvent : IGameEvent
{
    public int Value { get; }
    public TestEvent(int value) => Value = value;
}

public class EventBusTests
{
    // ── Basic Publish / Subscribe ────────────────────────────────────

    [Fact]
    public void Publish_InvokesSubscribedHandler()
    {
        var bus = new EventBus();
        int received = -1;
        bus.Subscribe<TestEvent>(e => received = e.Value);

        bus.Publish(new TestEvent(42));

        Assert.Equal(42, received);
    }

    [Fact]
    public void Unsubscribe_PreventsHandlerFromBeingCalled()
    {
        var bus = new EventBus();
        int callCount = 0;
        Action<TestEvent> handler = _ => callCount++;
        bus.Subscribe(handler);

        bus.Publish(new TestEvent(1));
        Assert.Equal(1, callCount);

        bus.Unsubscribe(handler);
        bus.Publish(new TestEvent(2));
        Assert.Equal(1, callCount);
    }

    [Fact]
    public void Clear_RemovesAllSubscriptions()
    {
        var bus = new EventBus();
        int callCount = 0;
        bus.Subscribe<TestEvent>(_ => callCount++);

        bus.Publish(new TestEvent(1));
        Assert.Equal(1, callCount);

        bus.Clear();
        bus.Publish(new TestEvent(2));
        Assert.Equal(1, callCount);
    }

    // ── Error Handling ───────────────────────────────────────────────

    [Fact]
    public void Publish_ThrowingHandler_DoesNotPreventSubsequentHandlers()
    {
        var bus = new EventBus();
        int secondHandlerValue = -1;

        bus.Subscribe<TestEvent>(_ => throw new InvalidOperationException("boom"));
        bus.Subscribe<TestEvent>(e => secondHandlerValue = e.Value);

        bus.Publish(new TestEvent(99));

        Assert.Equal(99, secondHandlerValue);
    }

    [Fact]
    public void Publish_ThrowingHandler_InvokesOnHandlerException()
    {
        var bus = new EventBus();
        Exception? captured = null;
        bus.OnHandlerException = ex => captured = ex;

        bus.Subscribe<TestEvent>(_ => throw new InvalidOperationException("test error"));

        bus.Publish(new TestEvent(1));

        Assert.NotNull(captured);
        Assert.IsType<InvalidOperationException>(captured);
        Assert.Equal("test error", captured!.Message);
    }

    [Fact]
    public void Publish_ThrowingHandler_NullCallback_SwallowsSilently()
    {
        var bus = new EventBus();
        bus.OnHandlerException = null;
        int secondHandlerValue = -1;

        bus.Subscribe<TestEvent>(_ => throw new InvalidOperationException("silent"));
        bus.Subscribe<TestEvent>(e => secondHandlerValue = e.Value);

        // Should not throw, and second handler should still run
        bus.Publish(new TestEvent(77));

        Assert.Equal(77, secondHandlerValue);
    }

    [Fact]
    public void Publish_MultipleThrowingHandlers_AllExceptionsReported()
    {
        var bus = new EventBus();
        int exceptionCount = 0;
        bus.OnHandlerException = _ => exceptionCount++;

        bus.Subscribe<TestEvent>(_ => throw new InvalidOperationException("first"));
        bus.Subscribe<TestEvent>(_ => throw new InvalidOperationException("second"));

        bus.Publish(new TestEvent(1));

        Assert.Equal(2, exceptionCount);
    }

    [Fact]
    public void Publish_MixedHandlers_HealthyHandlersStillRun()
    {
        var bus = new EventBus();
        int sum = 0;
        bus.OnHandlerException = _ => { };

        bus.Subscribe<TestEvent>(e => sum += e.Value);
        bus.Subscribe<TestEvent>(_ => throw new InvalidOperationException("fail"));
        bus.Subscribe<TestEvent>(e => sum += e.Value * 10);

        bus.Publish(new TestEvent(3));

        Assert.Equal(33, sum); // 3 + 30
    }

    // ── Re-entrancy Safety ──────────────────────────────────────────

    [Fact]
    public void EventBus_Unsubscribe_DuringPublish_DoesNotSkipHandlers()
    {
        var bus = new EventBus();
        var callOrder = new List<int>();

        Action<TestEvent> handler1 = null!;
        Action<TestEvent> handler2 = _ => callOrder.Add(2);
        Action<TestEvent> handler3 = _ => callOrder.Add(3);

        handler1 = _ =>
        {
            callOrder.Add(1);
            bus.Unsubscribe(handler1); // unsubscribe self during publish
        };

        bus.Subscribe(handler1);
        bus.Subscribe(handler2);
        bus.Subscribe(handler3);

        bus.Publish(new TestEvent(0));

        // All three should have run because publish snapshots the handler array
        Assert.Equal(new[] { 1, 2, 3 }, callOrder);

        // Next publish should not include handler1
        callOrder.Clear();
        bus.Publish(new TestEvent(0));
        Assert.Equal(new[] { 2, 3 }, callOrder);
    }

    [Fact]
    public void EventBus_Subscribe_DuringPublish_DeferredUntilNextPublish()
    {
        var bus = new EventBus();
        var callOrder = new List<int>();

        Action<TestEvent> lateHandler = _ => callOrder.Add(99);

        bus.Subscribe<TestEvent>(_ =>
        {
            callOrder.Add(1);
            bus.Subscribe(lateHandler); // subscribe during publish
        });

        bus.Publish(new TestEvent(0));

        // Late handler should NOT have run during this publish
        Assert.Equal(new[] { 1 }, callOrder);

        // Next publish should include both
        callOrder.Clear();
        bus.Publish(new TestEvent(0));
        Assert.Equal(new[] { 1, 99 }, callOrder);
    }
}

/// <summary>Phase10/11 event tests migrated to topical file.</summary>
public class GameEventTests
{
    [Fact]
    public void WorkerWornOutEvent_CountTracking()
    {
        var eventBus = new EventBus();
        int wornOutCount = 0;

        eventBus.Subscribe<WorkerWornOutEvent>(_ => wornOutCount++);

        eventBus.Publish(new WorkerWornOutEvent(new EntityId(1), WearOutReason.ToolBroken, WorkerProfession.Forester, new GridPosRPG(5, 5)));
        eventBus.Publish(new WorkerWornOutEvent(new EntityId(2), WearOutReason.StaminaDepleted, WorkerProfession.Warrior, new GridPosRPG(3, 3)));
        eventBus.Publish(new WorkerWornOutEvent(new EntityId(3), WearOutReason.CarryCapacityFull, WorkerProfession.HerbGatherer, new GridPosRPG(7, 7)));

        Assert.Equal(3, wornOutCount);
    }

    [Fact]
    public void WorkerWornOutEvent_CarriesPositionForParticles()
    {
        var evt = new WorkerWornOutEvent(new EntityId(42), WearOutReason.ToolBroken, WorkerProfession.Miner, new GridPosRPG(10, 20));

        Assert.Equal(42ul, evt.WorkerId);
        Assert.Equal(WearOutReason.ToolBroken, evt.Reason);
        Assert.Equal(10, evt.BuildingPosition.X);
        Assert.Equal(20, evt.BuildingPosition.Y);
    }

    [Fact]
    public void GoldChangedEvent_HasCorrectFields()
    {
        var evt = new GoldChangedEvent(50, 75, "dungeon_reward");

        Assert.Equal(50, evt.OldAmount);
        Assert.Equal(75, evt.NewAmount);
        Assert.Equal("dungeon_reward", evt.Reason);
    }

    [Fact]
    public void GuildCreatedEvent_HasCorrectFields()
    {
        var evt = new GuildCreatedEvent("My Guild", "banner_lion");

        Assert.Equal("My Guild", evt.GuildName);
        Assert.Equal("banner_lion", evt.BannerId);
    }

    [Fact]
    public void AbilityGainedEvent_CarriesNameForToast()
    {
        var evt = new AbilityGainedEvent(new EntityId(7), "power_strike", "Power Strike");

        Assert.Equal(7ul, evt.WorkerId);
        Assert.Equal("power_strike", evt.AbilityId);
        Assert.Equal("Power Strike", evt.AbilityName);
    }

    [Fact]
    public void AbilityLostEvent_CarriesCountAndProfession()
    {
        var evt = new AbilityLostEvent(new EntityId(5), 3, WorkerProfession.Warrior);

        Assert.Equal(5ul, evt.WorkerId);
        Assert.Equal(3, evt.AbilitiesLost);
        Assert.Equal(WorkerProfession.Warrior, evt.OldProfession);
    }

    [Fact]
    public void WorkerLevelDowngradedEvent_OldAndNewLevel()
    {
        var evt = new WorkerLevelDowngradedEvent(new EntityId(10), 5, 4);

        Assert.Equal(10ul, evt.WorkerId);
        Assert.Equal(5, evt.OldLevel);
        Assert.Equal(4, evt.NewLevel);
    }

    [Fact]
    public void WorkerRoutedEvent_CarriesRouteInfo()
    {
        var evt = new WorkerRoutedEvent(new EntityId(3), Direction.South, "level_check");

        Assert.Equal(3ul, evt.WorkerId);
        Assert.Equal(Direction.South, evt.RouteDirection);
        Assert.Equal("level_check", evt.GateReason);
    }

    [Fact]
    public void StructureInspectedEvent_TriggersForAutomationTypes()
    {
        var eventBus = new EventBus();
        string capturedType = "";

        eventBus.Subscribe<StructureInspectedEvent>(e => capturedType = e.StructureType);

        eventBus.Publish(new StructureInspectedEvent(new EntityId(1), "CheckGate"));
        Assert.Equal("CheckGate", capturedType);

        eventBus.Publish(new StructureInspectedEvent(new EntityId(2), "FilterSplitter"));
        Assert.Equal("FilterSplitter", capturedType);

        eventBus.Publish(new StructureInspectedEvent(new EntityId(3), "Balancer"));
        Assert.Equal("Balancer", capturedType);
    }

    [Fact]
    public void GoldChangedEvent_PublishedOnSpend()
    {
        var eventBus = new EventBus();
        GoldChangedEvent? captured = null;

        eventBus.Subscribe<GoldChangedEvent>(e => captured = e);

        var itemMgr = new ItemManager(eventBus);
        itemMgr.SetStock("gold", 200);
        int oldBalance = itemMgr.GetStock("gold");
        itemMgr.TrySpendStock("gold", 75);
        eventBus.Publish(new GoldChangedEvent(oldBalance, itemMgr.GetStock("gold"), "building_construction"));

        Assert.NotNull(captured);
        Assert.Equal(200, captured.Value.OldAmount);
        Assert.Equal(125, captured.Value.NewAmount);
        Assert.Equal("building_construction", captured.Value.Reason);
    }

    [Fact]
    public void AllPhase10Events_HaveRequiredFields()
    {
        var wornOut = new WorkerWornOutEvent(new EntityId(1), WearOutReason.ToolBroken, WorkerProfession.Forester, new GridPosRPG(5, 5));
        Assert.Equal(1ul, wornOut.WorkerId);
        Assert.Equal(WearOutReason.ToolBroken, wornOut.Reason);
        Assert.Equal(WorkerProfession.Forester, wornOut.Profession);

        var abilityGained = new AbilityGainedEvent(new EntityId(2), "slash", "Slash");
        Assert.Equal(2ul, abilityGained.WorkerId);

        var abilityLost = new AbilityLostEvent(new EntityId(3), 2, WorkerProfession.Warrior);
        Assert.Equal(3ul, abilityLost.WorkerId);

        var goldChanged = new GoldChangedEvent(100, 150, "reward");
        Assert.Equal(50, goldChanged.NewAmount - goldChanged.OldAmount);

        var guildCreated = new GuildCreatedEvent("Test", "banner_default");
        Assert.Equal("Test", guildCreated.GuildName);

        var routed = new WorkerRoutedEvent(new EntityId(4), Direction.North, "gate");
        Assert.Equal(4ul, routed.WorkerId);

        var downgraded = new WorkerLevelDowngradedEvent(new EntityId(5), 3, 2);
        Assert.Equal(5ul, downgraded.WorkerId);
    }

    [Fact]
    public void EventBus_MultipleSubscribers_AllReceive()
    {
        var bus = new EventBus();
        int count = 0;

        bus.Subscribe<HeroSpawnedEvent>(_ => count++);
        bus.Subscribe<HeroSpawnedEvent>(_ => count++);
        bus.Subscribe<HeroSpawnedEvent>(_ => count++);

        bus.Publish(new HeroSpawnedEvent(new EntityId(1), "warrior", new GridPosRPG(0, 0)));
        Assert.Equal(3, count);
    }
}
