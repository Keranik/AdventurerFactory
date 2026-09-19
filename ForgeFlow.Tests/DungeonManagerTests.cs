using ForgeFlow.Core.Data;
using ForgeFlow.Core.Data.Definitions;
using ForgeFlow.Core.Entities;
using ForgeFlow.Core.Events;
using ForgeFlow.Core.Proto;
using ForgeFlow.Core.Proto.Logic;
using ForgeFlow.Core.Proto.Prototypes;
using ForgeFlow.Core.Systems;
using ForgeFlow.Core.Utilities;

namespace ForgeFlow.Tests;

/// <summary>
/// Integration tests for DungeonManager — the central manager that processes
/// DungeonPortalLogic pending results, handles survival/death/rewards, and
/// publishes villager dungeon events. Follows the Central Manager + Events pattern.
/// </summary>
public class DungeonManagerTests
{
    public DungeonManagerTests()
    {
        EntityIdFactory.ResetForTesting();
        EntityBase.ResetIdCounter();
    }

    // ── Tick Processing ──────────────────────────────────────────

    [Fact]
    public void TickProcessesPendingResults_SurvivorGetsGoldAndLevelUp()
    {
        var (dm, bus, vs, im, entMgr, pgm) = CreateDungeonManager();
        var portal = CreatePortal(survivalChance: 1.0f, runDuration: 1f, goldReward: 100);
        portal.SetSeed(42);
        entMgr.AddStructure(portal, new GridPosRPG(5, 5));

        var villager = CreateDungeonReadyVillager();
        vs.AddVillager(villager);
        portal.AcceptVillager(villager.Id, hasWeapon: true, hasArmor: true);

        int initialLevel = villager.Level;

        // Simulate one tick that completes the dungeon run
        bus.Publish(new SimulationTickEvent(1f));

        Assert.Equal(initialLevel + 1, villager.Level);
        Assert.True(im.VirtualStocks.ContainsKey("gold") && im.VirtualStocks["gold"] >= 100);
    }

    [Fact]
    public void TickProcessesPendingResults_DeadVillagerIsRemoved()
    {
        var (dm, bus, vs, im, entMgr, pgm) = CreateDungeonManager();
        var portal = CreatePortal(survivalChance: 0f, runDuration: 1f);
        portal.SetSeed(42);
        entMgr.AddStructure(portal, new GridPosRPG(5, 5));

        var villager = CreateDungeonReadyVillager();
        vs.AddVillager(villager);
        portal.AcceptVillager(villager.Id);

        bus.Publish(new SimulationTickEvent(1f));

        Assert.Equal(VillagerState.Dead, villager.State);
        Assert.False(villager.IsActive);
        Assert.False(vs.VillagerIndex.ContainsKey(villager.Id));
    }

    [Fact]
    public void TickPublishesCompletedEvent_OnSurvival()
    {
        var (dm, bus, vs, im, entMgr, pgm) = CreateDungeonManager();
        var portal = CreatePortal(survivalChance: 1.0f, runDuration: 1f, goldReward: 50);
        portal.SetSeed(42);
        entMgr.AddStructure(portal, new GridPosRPG(5, 5));

        var villager = CreateDungeonReadyVillager();
        vs.AddVillager(villager);
        portal.AcceptVillager(villager.Id, hasWeapon: true, hasArmor: true);

        VillagerDungeonCompletedEvent? captured = null;
        bus.Subscribe<VillagerDungeonCompletedEvent>(e => captured = e);

        bus.Publish(new SimulationTickEvent(1f));

        Assert.NotNull(captured);
        Assert.Equal(villager.Id, captured.Value.VillagerId);
        Assert.Equal(50, captured.Value.GoldReward);
    }

    [Fact]
    public void TickPublishesDiedEvent_OnDeath()
    {
        var (dm, bus, vs, im, entMgr, pgm) = CreateDungeonManager();
        var portal = CreatePortal(survivalChance: 0f, runDuration: 1f);
        portal.SetSeed(42);
        entMgr.AddStructure(portal, new GridPosRPG(5, 5));

        var villager = CreateDungeonReadyVillager();
        vs.AddVillager(villager);
        portal.AcceptVillager(villager.Id);

        VillagerDiedInDungeonEvent? captured = null;
        bus.Subscribe<VillagerDiedInDungeonEvent>(e => captured = e);

        bus.Publish(new SimulationTickEvent(1f));

        Assert.NotNull(captured);
        Assert.Equal(villager.Id, captured.Value.VillagerId);
    }

    [Fact]
    public void TickDoesNothing_WhenNoPortalsHaveOccupants()
    {
        var (dm, bus, vs, im, entMgr, pgm) = CreateDungeonManager();
        var portal = CreatePortal(runDuration: 10f);
        entMgr.AddStructure(portal, new GridPosRPG(5, 5));

        // No occupants — tick should not throw or produce any events
        VillagerDungeonCompletedEvent? completed = null;
        VillagerDiedInDungeonEvent? died = null;
        bus.Subscribe<VillagerDungeonCompletedEvent>(e => completed = e);
        bus.Subscribe<VillagerDiedInDungeonEvent>(e => died = e);

        bus.Publish(new SimulationTickEvent(1f));

        Assert.Null(completed);
        Assert.Null(died);
    }

    [Fact]
    public void TickProcessesMultiplePortals()
    {
        var (dm, bus, vs, im, entMgr, pgm) = CreateDungeonManager();

        var portal1 = CreatePortal(survivalChance: 1.0f, runDuration: 1f, goldReward: 30);
        portal1.SetSeed(42);
        entMgr.AddStructure(portal1, new GridPosRPG(5, 5));

        var portal2 = CreatePortal(survivalChance: 1.0f, runDuration: 1f, goldReward: 70);
        portal2.SetSeed(42);
        entMgr.AddStructure(portal2, new GridPosRPG(10, 10));

        var v1 = CreateDungeonReadyVillager();
        var v2 = CreateDungeonReadyVillager();
        vs.AddVillager(v1);
        vs.AddVillager(v2);
        portal1.AcceptVillager(v1.Id, hasWeapon: true, hasArmor: true);
        portal2.AcceptVillager(v2.Id, hasWeapon: true, hasArmor: true);

        int completedCount = 0;
        bus.Subscribe<VillagerDungeonCompletedEvent>(_ => completedCount++);

        bus.Publish(new SimulationTickEvent(1f));

        Assert.Equal(2, completedCount);
        Assert.True(im.VirtualStocks["gold"] >= 100);
    }

    [Fact]
    public void TickSkipsPortals_WhereTimerNotComplete()
    {
        var (dm, bus, vs, im, entMgr, pgm) = CreateDungeonManager();
        var portal = CreatePortal(survivalChance: 1.0f, runDuration: 10f);
        portal.SetSeed(42);
        entMgr.AddStructure(portal, new GridPosRPG(5, 5));

        var villager = CreateDungeonReadyVillager();
        vs.AddVillager(villager);
        portal.AcceptVillager(villager.Id);

        VillagerDungeonCompletedEvent? captured = null;
        bus.Subscribe<VillagerDungeonCompletedEvent>(e => captured = e);

        // Only 3 seconds — not enough for 10s run
        bus.Publish(new SimulationTickEvent(3f));

        Assert.Null(captured);
        Assert.Single(portal.CurrentOccupants);
    }

    [Fact]
    public void TickClearsPendingResults_AfterProcessing()
    {
        var (dm, bus, vs, im, entMgr, pgm) = CreateDungeonManager();
        var portal = CreatePortal(survivalChance: 1.0f, runDuration: 1f);
        portal.SetSeed(42);
        entMgr.AddStructure(portal, new GridPosRPG(5, 5));

        var villager = CreateDungeonReadyVillager();
        vs.AddVillager(villager);
        portal.AcceptVillager(villager.Id);

        bus.Publish(new SimulationTickEvent(1f));

        Assert.Empty(portal.PendingResults);
        Assert.Empty(portal.CurrentOccupants);
    }

    [Fact]
    public void SurvivorWithLoot_PicksUpItem()
    {
        var dungeonRegistry = new DungeonRegistry();
        dungeonRegistry.Register(new DungeonDefinition
        {
            Id = "loot_dungeon",
            Tier = 1,
            BaseSurvivalChance = 1.0f,
            BaseGoldReward = 10,
            RunDuration = 1f,
            PossibleDropItemIds = new List<string> { "iron_sword" }
        });

        var (dm, bus, vs, im, entMgr, pgm) = CreateDungeonManager();
        var portal = CreatePortal(dungeonId: "loot_dungeon", runDuration: 1f, survivalChance: 1.0f);
        portal.Initialize(dungeonRegistry);
        portal.SetSeed(42);
        entMgr.AddStructure(portal, new GridPosRPG(5, 5));

        var villager = CreateDungeonReadyVillager();
        vs.AddVillager(villager);
        portal.AcceptVillager(villager.Id, hasWeapon: true, hasArmor: true);

        bus.Publish(new SimulationTickEvent(1f));

        Assert.True(villager.IsCarryingItems);
    }

    [Fact]
    public void DeadVillagerGetsNoGold()
    {
        var (dm, bus, vs, im, entMgr, pgm) = CreateDungeonManager();
        var portal = CreatePortal(survivalChance: 0f, runDuration: 1f, goldReward: 999);
        portal.SetSeed(42);
        entMgr.AddStructure(portal, new GridPosRPG(5, 5));

        var villager = CreateDungeonReadyVillager();
        vs.AddVillager(villager);
        portal.AcceptVillager(villager.Id);

        int goldBefore = im.VirtualStocks.ContainsKey("gold") ? im.VirtualStocks["gold"] : 0;

        bus.Publish(new SimulationTickEvent(1f));

        int goldAfter = im.VirtualStocks.ContainsKey("gold") ? im.VirtualStocks["gold"] : 0;
        Assert.Equal(goldBefore, goldAfter);
    }

    [Fact]
    public void Dispose_UnsubscribesFromEventBus()
    {
        var (dm, bus, vs, im, entMgr, pgm) = CreateDungeonManager();
        var portal = CreatePortal(survivalChance: 1.0f, runDuration: 1f);
        portal.SetSeed(42);
        entMgr.AddStructure(portal, new GridPosRPG(5, 5));

        var villager = CreateDungeonReadyVillager();
        vs.AddVillager(villager);
        portal.AcceptVillager(villager.Id);

        dm.Dispose();

        VillagerDungeonCompletedEvent? captured = null;
        bus.Subscribe<VillagerDungeonCompletedEvent>(e => captured = e);

        bus.Publish(new SimulationTickEvent(1f));

        // After dispose, DungeonManager should not process anything
        Assert.Null(captured);
        // The occupant should still be in the portal (no processing happened)
        Assert.Single(portal.CurrentOccupants);
    }

    // ── Helpers ──────────────────────────────────────────────────

    private static (DungeonManager dm, EventBus bus, VillagerSystem vs, ItemManager im, EntityManager entMgr, PathGateManager pgm) CreateDungeonManager()
    {
        var bus = new EventBus();
        var im = new ItemManager(bus);
        var vs = new VillagerSystem(bus, im);
        var terrain = new TerrainGrid(32, 32);
        terrain.GenerateDefault();
        var tileMgr = new TileManager(terrain);
        var entMgr = new EntityManager(tileMgr, vs);

        var protoReg = new ProtoRegistry();
        protoReg.RegisterDefaults();
        var pf = new ProtoFactory(protoReg, bus);
        var ts = new TutorialSystem(bus, pf);
        var pgm = new PathGateManager(entMgr, tileMgr, bus, new Core.Commands.CommandBus(), ts, im, new ResearchManager(bus));

        var dm = new DungeonManager(entMgr, pgm, vs, im, bus);
        return (dm, bus, vs, im, entMgr, pgm);
    }

    private static DungeonPortalLogic CreatePortal(
        string dungeonId = "goblin_caves",
        float runDuration = 15f,
        float survivalChance = 0.1f,
        int goldReward = 50,
        int maxOccupants = 4)
    {
        var proto = new DungeonPortalProto
        {
            Id = "dungeon_portal_test",
            DefaultDungeonId = dungeonId,
            BaseSurvivalChance = survivalChance,
            BaseGoldReward = goldReward,
            RunDuration = runDuration,
            MaxOccupants = maxOccupants,
            ProcessingDuration = runDuration
        };

        var logic = new DungeonPortalLogic(EntityId.Next());
        logic.InitializeFromProto(proto);
        return logic;
    }

    private static VillagerLogic CreateDungeonReadyVillager()
    {
        return new VillagerLogic(EntityId.Next())
        {
            State = VillagerState.InDungeon,
            TrainedClass = VillagerClass.Warrior,
            EquippedWeaponId = "iron_sword",
            EquippedArmorId = "iron_shield",
            Level = 1,
            IsActive = true
        };
    }
}
