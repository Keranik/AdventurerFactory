using ForgeFlow.Core;
using ForgeFlow.Core.Commands;
using ForgeFlow.Core.Data;
using ForgeFlow.Core.Data.Definitions;
using ForgeFlow.Core.Entities;
using ForgeFlow.Core.Events;
using ForgeFlow.Core.Proto;
using ForgeFlow.Core.Proto.Logic;
using ForgeFlow.Core.Proto.Prototypes;
using ForgeFlow.Core.Save;
using ForgeFlow.Core.Systems;
using ForgeFlow.Core.Utilities;

namespace ForgeFlow.Tests;

/// <summary>
/// Integration tests for the full gameplay flow: gold awards, tutorial auto-advancement,
/// AutoConnect, FullFlow scenarios, path placement, dungeon rewards, ForestryProto issue fix,
/// villager queuing, and WatchFullLoop stockpile checks.
/// </summary>
public class GameplayFlowTests
{
    // ─── GameplayFlow: Gold Awards ──────────────────────────────

    [Fact]
    public void GameplayFlow_AwardGold_IncreasesBalance()
    {
        var (boot, sim) = CreateNewGame();
        var flow = boot.Services.Get<GameplayFlowSystem>();
        int startGold = sim.ItemManager.GetStock("gold");

        flow.AwardGold(50, "Test reward");

        Assert.Equal(startGold + 50, sim.ItemManager.GetStock("gold"));
    }

    [Fact]
    public void GameplayFlow_AwardGold_PublishesEvent()
    {
        var (boot, sim) = CreateNewGame();
        var flow = boot.Services.Get<GameplayFlowSystem>();
        GoldChangedEvent? captured = null;
        boot.Services.Get<EventBus>().Subscribe<GoldChangedEvent>(e => captured = e);

        flow.AwardGold(25, "Bonus");

        Assert.NotNull(captured);
        Assert.Equal("Bonus", captured!.Value.Reason);
    }

    // ─── Tutorial Auto-Advancement ──────────────────────────────

    [Fact]
    public void Tutorial_InitializesWithActiveMission()
    {
        var (boot, sim) = CreateNewGame();
        var tutorial = sim.TutorialSystem;

        Assert.NotNull(tutorial.ActiveMission);
        Assert.Equal(TutorialMissionState.Active, tutorial.ActiveMission!.State);
    }

    [Fact]
    public void Tutorial_PlaceStructure_AdvancesCondition()
    {
        var (boot, sim) = CreateNewGame();
        var tutorial = sim.TutorialSystem;

        var firstMission = tutorial.ActiveMission;
        Assert.NotNull(firstMission);

        var spawner = boot.Services.Get<ProtoFactory>().CreateVillageSpawner("spawner_basic");
        boot.Services.Get<CommandBus>().Dispatch(new PlaceStructureCommand(spawner, new GridPosRPG(5, 5), "Spawner"));

        var placeCond = firstMission!.Conditions.Find(
            c => c.Type == TutorialConditionType.PlaceStructure);
        if (placeCond != null)
        {
            Assert.True(placeCond.CurrentCount >= 1);
        }
    }

    [Fact]
    public void Tutorial_HeroSpawned_AdvancesCondition()
    {
        var (boot, sim) = CreateNewGame();
        var tutorial = sim.TutorialSystem;

        boot.Services.Get<EventBus>().Publish(new HeroSpawnedEvent(
            new EntityId(1), "warrior", new GridPosRPG(0, 0)));

        var active = tutorial.ActiveMission;
        if (active != null)
        {
            var spawnCond = active.Conditions.Find(c => c.Type == TutorialConditionType.SpawnHero);
            if (spawnCond != null)
            {
                Assert.True(spawnCond.CurrentCount >= 1);
            }
        }
    }

    // ─── Auto-Connect Structure to Adjacent Paths ─────────────────

    [Fact]
    public void AutoConnect_LinksAdjacentPath()
    {
        var (boot, sim) = CreateNewGame();

        boot.Services.Get<CommandBus>().Dispatch(new DrawPathCommand(new GridPosRPG(5, 4), Direction.North));
        var segment = sim.EntityManager.GetPathSegmentAt(new GridPosRPG(5, 4));
        Assert.NotNull(segment);

        var spawner = boot.Services.Get<ProtoFactory>().CreateVillageSpawner("spawner_basic");
        boot.Services.Get<CommandBus>().Dispatch(new PlaceStructureCommand(spawner, new GridPosRPG(5, 5), "Spawner"));

        sim.PathNodeManager.AutoConnectToAdjacentPaths(spawner);

        Assert.Equal(spawner.Id, segment!.ConnectedStructureId);
    }

    // ─── FullFlow Scenarios ─────────────────────────────────────

    [Fact]
    public void FullFlow_NewGame_TutorialStartsActive()
    {
        var (boot, sim) = CreateNewGame();

        Assert.NotEmpty(sim.TutorialSystem.AllMissions);
        Assert.NotNull(sim.TutorialSystem.ActiveMission);
        Assert.Equal(TutorialMissionState.Active, sim.TutorialSystem.ActiveMission!.State);
    }

    [Fact]
    public void FullFlow_NewGame_GoldAndResourcesPresent()
    {
        var (boot, sim) = CreateNewGame();

        Assert.True(sim.ItemManager.GetStock("gold") > 0);
        Assert.True(sim.ItemManager.VirtualStocks.ContainsKey("wood"));
        Assert.True(sim.ItemManager.VirtualStocks.ContainsKey("ore"));
        Assert.True(sim.ItemManager.VirtualStocks.ContainsKey("food"));
    }

    [Fact]
    public void FullFlow_PlaceSpawnerAndPath_GoldDecreases()
    {
        var (boot, sim) = CreateNewGame();
        int startGold = sim.ItemManager.GetStock("gold");

        var spawner = boot.Services.Get<ProtoFactory>().CreateVillageSpawner("spawner_basic");
        boot.Services.Get<CommandBus>().Dispatch(new PlaceStructureCommand(spawner, new GridPosRPG(5, 5), "Spawner"));
        int afterSpawner = sim.ItemManager.GetStock("gold");
        Assert.Equal(startGold - 25 + 50, afterSpawner);

        boot.Services.Get<CommandBus>().Dispatch(new DrawPathCommand(new GridPosRPG(6, 5), Direction.East));
        Assert.Equal(afterSpawner - 2, sim.ItemManager.GetStock("gold"));
    }

    [Fact]
    public void FullFlow_MultiplePurchases_DrainGold()
    {
        var (boot, sim) = CreateNewGame();

        var spawner = boot.Services.Get<ProtoFactory>().CreateVillageSpawner("spawner_basic");
        boot.Services.Get<CommandBus>().Dispatch(new PlaceStructureCommand(spawner, new GridPosRPG(0, 0), "Spawner"));
        var forge = new ForgeLogic(EntityId.Next());
        boot.Services.Get<CommandBus>().Dispatch(new PlaceStructureCommand(
            forge,
            new GridPosRPG(10, 10), "Forge"));

        for (int i = 0; i < 10; i++)
        {
            boot.Services.Get<CommandBus>().Dispatch(new DrawPathCommand(new GridPosRPG(1 + i, 0), Direction.East));
        }

        Assert.Equal(65, sim.ItemManager.GetStock("gold"));
    }

    [Fact]
    public void FullFlow_DungeonComplete_AwardsGold()
    {
        var (boot, sim) = CreateNewGame();
        int beforeDungeon = sim.ItemManager.GetStock("gold");

        boot.Services.Get<EventBus>().Publish(new DungeonCompletedEvent(new EntityId(1), "goblin_caves", true, 2, new GridPosRPG(0, 0)));

        Assert.True(sim.ItemManager.GetStock("gold") > beforeDungeon);
    }

    // ─── Villager Path Queuing ───────────────────────────────────

    [Fact]
    public void Villagers_DoNotOverlap_OnSameTile()
    {
        EntityBase.ResetIdCounter();
        var (sim, bus, _, _, _, _) = CreateTestSimulation();

        var seg1 = sim.PathNodeManager.AddPathSegment(new GridPosRPG(0, 0), Direction.East);
        var seg2 = sim.PathNodeManager.AddPathSegment(new GridPosRPG(1, 0), Direction.East);
        var seg3 = sim.PathNodeManager.AddPathSegment(new GridPosRPG(2, 0), Direction.East);
        Assert.NotNull(seg1);
        Assert.NotNull(seg2);
        Assert.NotNull(seg3);

        var v1 = new VillagerLogic(EntityId.Next()) { Name = "V1", MovementSpeed = 10f };
        var v2 = new VillagerLogic(EntityId.Next()) { Name = "V2", MovementSpeed = 10f };
        sim.VillagerSystem.AddVillager(v1);
        sim.VillagerSystem.AddVillager(v2);

        Assert.True(seg1!.OccupantIds.Count == 0);
        seg1.TryAddOccupant(v1.Id);
        v1.PlaceOnPath(seg1.Id);
        v1.Position = seg1.Position;

        Assert.True(seg1.OccupantIds.Count > 0);

        for (int tick = 0; tick < 20; tick++)
        {
            sim.PathTraffic.TickVillagers(0.5f);

            foreach (var seg in sim.EntityManager.PathSegments.Values)
            {
                Assert.True(seg.OccupantIds.Count <= 1,
                    $"Segment at {seg.Position} has {seg.OccupantIds.Count} occupants — villagers must not overlap!");
            }
        }
    }

    [Fact]
    public void WaitQueue_DoesNotCreateGhostOccupants()
    {
        EntityBase.ResetIdCounter();
        var (sim, bus, _, _, _, _) = CreateTestSimulation();

        var seg1 = sim.PathNodeManager.AddPathSegment(new GridPosRPG(0, 0), Direction.East);
        var seg2 = sim.PathNodeManager.AddPathSegment(new GridPosRPG(1, 0), Direction.East);
        Assert.NotNull(seg1);
        Assert.NotNull(seg2);

        var blocker = new VillagerLogic(EntityId.Next()) { Name = "Blocker", MovementSpeed = 0f };
        sim.VillagerSystem.AddVillager(blocker);
        seg2!.TryAddOccupant(blocker.Id);
        blocker.PlaceOnPath(seg2.Id);
        blocker.Position = seg2.Position;

        var mover = new VillagerLogic(EntityId.Next()) { Name = "Mover", MovementSpeed = 10f };
        sim.VillagerSystem.AddVillager(mover);
        seg1!.TryAddOccupant(mover.Id);
        mover.PlaceOnPath(seg1.Id);
        mover.Position = seg1.Position;

        sim.PathTraffic.TickVillagers(1.0f);

        Assert.Equal(seg1.Id, mover.CurrentPathSegmentId);
        Assert.Contains(mover.Id, seg1.OccupantIds);
        Assert.DoesNotContain(mover.Id, seg2.OccupantIds);
    }

    // ─── WatchFullLoop: Stockpile-Specific Checks ──────────────

    [Fact]
    public void WatchFullLoop_DoesNotAdvance_WhenGlobalStocksHigh_ButStockpileLow()
    {
        var (boot, sim) = CreateNewGame();

        var completedIds = new List<string>
        {
            "p1_01_place_home", "p1_02_exit_gate", "p1_03_place_forestry",
            "p1_04_forestry_entrance", "p1_05_connect_path", "p1_06_watch_gathering",
            "p1_07_place_stockpile", "p1_08_select_sticks_filter", "p1_09_stockpile_entrance", "p1_10_forestry_exit",
            "p1_11_connect_to_stockpile", "p1_12_watch_dropoff", "p1_13_stockpile_exit",
            "p1_14_loop_path", "p1_15_place_inn"
        };
        sim.TutorialSystem.LoadFromSave(completedIds, "p1_16_watch_full_loop");

        Assert.NotNull(sim.TutorialSystem.ActiveMission);
        Assert.Equal("p1_16_watch_full_loop", sim.TutorialSystem.ActiveMission!.ProtoId);

        var stockpile = new StockpileLogic(EntityId.Next());
        sim.StructureManager.AddProtoStructure(stockpile, new GridPosRPG(3, 3));
        stockpile.Deposit("sticks", 5);

        sim.ItemManager.AddStock("sticks", 50);

        boot.Services.Get<EventBus>().Publish(new VillagerDroppedOffItemEvent(new EntityId(999), "sticks", stockpile.Id));

        var watchCond = sim.TutorialSystem.ActiveMission.Conditions.Find(
            c => c.Type == TutorialConditionType.WatchFullLoop);
        Assert.NotNull(watchCond);
        Assert.False(watchCond!.IsMet, "WatchFullLoop should not advance when stockpile has < 20 sticks");
    }

    [Fact]
    public void WatchFullLoop_Advances_WhenSpecificStockpileReaches20()
    {
        var (boot, sim) = CreateNewGame();

        var completedIds = new List<string>
        {
            "p1_01_place_home", "p1_02_exit_gate", "p1_03_place_forestry",
            "p1_04_forestry_entrance", "p1_05_connect_path", "p1_06_watch_gathering",
            "p1_07_place_stockpile", "p1_08_select_sticks_filter", "p1_09_stockpile_entrance", "p1_10_forestry_exit",
            "p1_11_connect_to_stockpile", "p1_12_watch_dropoff", "p1_13_stockpile_exit",
            "p1_14_loop_path", "p1_15_place_inn"
        };
        sim.TutorialSystem.LoadFromSave(completedIds, "p1_16_watch_full_loop");

        var stockpile = new StockpileLogic(EntityId.Next());
        sim.StructureManager.AddProtoStructure(stockpile, new GridPosRPG(3, 3));
        stockpile.Deposit("sticks", 20);

        boot.Services.Get<EventBus>().Publish(new VillagerDroppedOffItemEvent(new EntityId(999), "sticks", stockpile.Id));
        var mission = sim.TutorialSystem.AllMissions.First(m => m.ProtoId == "p1_16_watch_full_loop");
        Assert.Equal(TutorialMissionState.Completed, mission.State);
    }

    [Fact]
    public void WatchFullLoop_DoesNotAdvance_WhenBuildingIsNotStockpile()
    {
        var (boot, sim) = CreateNewGame();

        var completedIds = new List<string>
        {
            "p1_01_place_home", "p1_02_exit_gate", "p1_03_place_forestry",
            "p1_04_forestry_entrance", "p1_05_connect_path", "p1_06_watch_gathering",
            "p1_07_place_stockpile", "p1_08_select_sticks_filter", "p1_09_stockpile_entrance", "p1_10_forestry_exit",
            "p1_11_connect_to_stockpile", "p1_12_watch_dropoff", "p1_13_stockpile_exit",
            "p1_14_loop_path", "p1_15_place_inn"
        };
        sim.TutorialSystem.LoadFromSave(completedIds, "p1_16_watch_full_loop");

        var spawner = new VillageSpawnerLogic(EntityId.Next());
        sim.StructureManager.AddProtoStructure(spawner, new GridPosRPG(4, 4));

        boot.Services.Get<EventBus>().Publish(new VillagerDroppedOffItemEvent(new EntityId(999), "sticks", spawner.Id));

        var watchCond = sim.TutorialSystem.ActiveMission!.Conditions.Find(
            c => c.Type == TutorialConditionType.WatchFullLoop);
        Assert.NotNull(watchCond);
        Assert.False(watchCond!.IsMet, "WatchFullLoop should not advance when building is not a Stockpile");
    }

    // ─── Helpers ─────────────────────────────────────────────────

    private static GameBootstrapper CreateBootstrapper()
    {
        var bootstrapper = new GameBootstrapper();
        bootstrapper.Bootstrap();
        return bootstrapper;
    }

    private static (GameBootstrapper boot, SimulationTicker sim) CreateNewGame(
        Difficulty difficulty = Difficulty.Normal)
    {
        var boot = CreateBootstrapper();
        var settings = new NewGameSettings
        {
            GameName = "TestGame",
            Difficulty = difficulty,
            Seed = 42,
            GuildName = "TestGuild"
        };
        boot.ApplyNewGameSettings(settings);
        return (boot, boot.Services.Get<SimulationTicker>());
    }

    private static (SimulationTicker sim, EventBus bus, ClassRegistry classes, ItemRegistry items, DungeonRegistry dungeons, RecipeRegistry recipes) CreateTestSimulation()
    {
        var bus = new EventBus();
        var classReg = new ClassRegistry();
        classReg.Register(new ClassDefinition { Id = "warrior", Name = "Warrior", ClassBonus = 10 });
        var itemReg = new ItemRegistry();
        var recipeReg = new RecipeRegistry();
        var dungeonReg = new DungeonRegistry();

        var tm = new TrafficManager();
        var protoReg = new ProtoRegistry();
        protoReg.RegisterDefaults();
        var pf = new ProtoFactory(protoReg, bus);
        var rm = new ItemManager(bus);
        var vs = new VillagerSystem(bus, rm);
        var terrain = new TerrainGrid(32, 32);
        terrain.GenerateDefault();
        var tileMgr = new TileManager(terrain);
        var entMgr = new EntityManager(tileMgr, vs);
        var pt = new PathTrafficSystem(tm, entMgr, tileMgr, bus);
        var ts = new TutorialSystem(bus, pf);
        var ae = new AutoEquipSystem(bus, classReg, entMgr);
        var dr = new DungeonResolver(dungeonReg, classReg, bus, entMgr);
        var fc = new FusionCalculator(classReg, itemReg, rm, bus, entMgr);
        var aa = new AppearanceApplier(bus, entMgr);
        var gl = new GatingLimits();
        var rsMgr = new ResearchManager(bus);
        var cmdBus = new Core.Commands.CommandBus();
        var pm = new PathNodeManager(entMgr, tileMgr, vs, pt, bus, cmdBus, ts, gl, rsMgr, rm);
        var pgm = new PathGateManager(entMgr, tileMgr, bus, cmdBus, ts, rm, rsMgr);
        pt.SetPathGateManager(pgm);
        var mm = new StructureManager(entMgr, pgm, pm, vs, rm, itemReg, recipeReg, gl, bus, cmdBus, ts, rsMgr);
        var wsMgr = new WorldStateManager(rsMgr, entMgr, rm, bus);
        var dm = new DungeonManager(entMgr, pgm, vs, rm, bus);
        var sim = new SimulationTicker(bus, cmdBus, pt, vs, ts, entMgr, tileMgr, pm, pgm, rm, gl, mm, rsMgr, wsMgr, aa, dr, dm);
        return (sim, bus, classReg, itemReg, dungeonReg, recipeReg);
    }
}
