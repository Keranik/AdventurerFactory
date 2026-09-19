using ForgeFlow.Core.Entities;
using ForgeFlow.Core.Events;
using ForgeFlow.Core.Systems;

namespace ForgeFlow.Tests;

/// <summary>Tests for the GameStatistics event-tracking system.</summary>
public class StatisticsTests
{
    [Fact]
    public void GameStatistics_TracksVillagers()
    {
        var bus = new EventBus();
        var stats = new GameStatistics();
        stats.Initialize(bus);

        bus.Publish(new VillagerSpawnedEvent(new EntityId(1), "TestVillager", new GridPosRPG(0, 0)));
        Assert.Equal(1, stats.TotalVillagersSpawned);
    }

    [Fact]
    public void GameStatistics_TracksFusions()
    {
        var bus = new EventBus();
        var stats = new GameStatistics();
        stats.Initialize(bus);

        bus.Publish(new HeroFusedEvent(new EntityId(1), new EntityId(2), new EntityId(3), "spellsword", 6));
        Assert.Equal(1, stats.TotalFusions);
    }

    [Fact]
    public void GameStatistics_TracksPrestige()
    {
        var bus = new EventBus();
        var stats = new GameStatistics();
        stats.Initialize(bus);

        bus.Publish(new PrestigeResetEvent(1, 3, 1.1f));
        Assert.Equal(1, stats.TotalPrestigeResets);
    }

    [Fact]
    public void GameStatistics_TracksWorkersWornOut()
    {
        var eventBus = new EventBus();
        var stats = new GameStatistics();
        stats.Initialize(eventBus);

        eventBus.Publish(new WorkerWornOutEvent(new EntityId(1), WearOutReason.ToolBroken, WorkerProfession.Forester, default));
        eventBus.Publish(new WorkerWornOutEvent(new EntityId(2), WearOutReason.StaminaDepleted, WorkerProfession.Warrior, default));

        Assert.Equal(2, stats.TotalWorkersWornOut);
    }

    [Fact]
    public void GameStatistics_TracksAbilitiesGained()
    {
        var eventBus = new EventBus();
        var stats = new GameStatistics();
        stats.Initialize(eventBus);

        eventBus.Publish(new AbilityGainedEvent(new EntityId(1), "extra_attack", "Extra Attack"));

        Assert.Equal(1, stats.TotalAbilitiesGained);
    }

    [Fact]
    public void GameStatistics_TracksGoldEarned()
    {
        var eventBus = new EventBus();
        var stats = new GameStatistics();
        stats.Initialize(eventBus);

        eventBus.Publish(new GoldChangedEvent(0, 50, "dungeon"));

        Assert.Equal(50, stats.TotalGoldEarned);
    }

    [Fact]
    public void GameStatistics_TracksPhase10Events()
    {
        var eventBus = new EventBus();
        var stats = new GameStatistics();
        stats.Initialize(eventBus);

        eventBus.Publish(new WorkerWornOutEvent(new EntityId(1), WearOutReason.ToolBroken, WorkerProfession.Forester, new GridPosRPG(0, 0)));
        eventBus.Publish(new AbilityGainedEvent(new EntityId(1), "slash", "Slash"));
        eventBus.Publish(new AbilityLostEvent(new EntityId(1), 2, WorkerProfession.Warrior));

        Assert.Equal(1, stats.TotalWorkersWornOut);
        Assert.Equal(1, stats.TotalAbilitiesGained);
        Assert.Equal(2, stats.TotalAbilitiesLost);
    }

    // ─── GameStatistics: Core Tracking (Phase6) ───────────────────────

    [Fact]
    public void GameStatistics_TracksHeroSpawns()
    {
        var bus = new EventBus();
        var stats = new GameStatistics();
        stats.Initialize(bus);

        bus.Publish(new HeroSpawnedEvent(new EntityId(1), "warrior", new GridPosRPG(0, 0)));
        bus.Publish(new HeroSpawnedEvent(new EntityId(2), "mage", new GridPosRPG(0, 0)));

        Assert.Equal(2, stats.TotalHeroesSpawned);
    }

    [Fact]
    public void GameStatistics_TracksDungeons()
    {
        var bus = new EventBus();
        var stats = new GameStatistics();
        stats.Initialize(bus);

        bus.Publish(new DungeonCompletedEvent(new EntityId(1), "goblin_caves", true, 2, new GridPosRPG(0, 0)));
        bus.Publish(new DungeonCompletedEvent(new EntityId(2), "goblin_caves", false, 1, new GridPosRPG(0, 0)));

        Assert.Equal(2, stats.TotalDungeonsAttempted);
        Assert.Equal(1, stats.TotalDungeonsCleared);
    }

    [Fact]
    public void GameStatistics_TracksResources()
    {
        var bus = new EventBus();
        var stats = new GameStatistics();
        stats.Initialize(bus);

        bus.Publish(new ResourceProducedEvent("wood", 10, new GridPosRPG(0, 0)));
        bus.Publish(new ResourceProducedEvent("ore", 5, new GridPosRPG(0, 0)));

        Assert.Equal(15, stats.TotalResourcesProduced);
        Assert.Equal(10, stats.ResourcesProducedByType["wood"]);
        Assert.Equal(5, stats.ResourcesProducedByType["ore"]);
    }

    [Fact]
    public void GameStatistics_TickPlayTime()
    {
        var bus = new EventBus();
        var stats = new GameStatistics();
        stats.Initialize(bus);

        stats.TickPlayTime(1.5f);
        stats.TickPlayTime(2.5f);

        Assert.Equal(4.0f, stats.PlayTimeSeconds, 0.01);
    }

    [Fact]
    public void GameStatistics_Reset_ClearsAll()
    {
        var bus = new EventBus();
        var stats = new GameStatistics();
        stats.Initialize(bus);

        bus.Publish(new HeroSpawnedEvent(new EntityId(1), "warrior", new GridPosRPG(0, 0)));
        stats.TickPlayTime(10f);

        stats.Reset();

        Assert.Equal(0, stats.TotalHeroesSpawned);
        Assert.Equal(0f, stats.PlayTimeSeconds);
    }
}
