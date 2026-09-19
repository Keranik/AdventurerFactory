using ForgeFlow.Core;
using ForgeFlow.Core.Commands;
using ForgeFlow.Core.Entities;
using ForgeFlow.Core.Events;
using ForgeFlow.Core.Proto;
using ForgeFlow.Core.Proto.Logic;
using ForgeFlow.Core.Proto.Prototypes;
using ForgeFlow.Core.Save;
using ForgeFlow.Core.Systems;

namespace ForgeFlow.Tests;

/// <summary>
/// Tests for the PlaceStructureCommand handler in StructureManager.
/// Validates gold deduction, placement, event publishing, failure cases,
/// and tutorial advancement through the command dispatch pattern.
/// </summary>
public class PlaceStructureCommandTests
{
    private static (GameBootstrapper boot, SimulationTicker sim) CreateNewGame(
        Difficulty difficulty = Difficulty.Normal)
    {
        EntityBase.ResetIdCounter();
        var boot = new GameBootstrapper();
        boot.Bootstrap();
        var settings = new NewGameSettings
        {
            GameName = "CmdTest",
            Difficulty = difficulty,
            Seed = 42,
            GuildName = "CmdGuild"
        };
        boot.ApplyNewGameSettings(settings);
        return (boot, boot.Services.Get<SimulationTicker>());
    }

    // ── Success Path ─────────────────────────────────────────────────

    [Fact]
    public void PlaceStructure_Success_ReturnsOk()
    {
        var (boot, sim) = CreateNewGame();
        var spawner = boot.Services.Get<ProtoFactory>().CreateVillageSpawner("spawner_basic")!;

        var result = boot.Services.Get<CommandBus>().Dispatch(new PlaceStructureCommand(
            spawner, new GridPosRPG(5, 5), "Spawner"));

        Assert.True(result.Success);
    }

    [Fact]
    public void PlaceStructure_Success_DeductsGold()
    {
        var (boot, sim) = CreateNewGame();
        int startGold = sim.ItemManager.GetStock("gold");
        int cost = EconomyConfig.GetStructureCost("Spawner");
        var spawner = boot.Services.Get<ProtoFactory>().CreateVillageSpawner("spawner_basic")!;

        boot.Services.Get<CommandBus>().Dispatch(new PlaceStructureCommand(
            spawner, new GridPosRPG(5, 5), "Spawner"));

        // Tutorial tut_01_place_spawner completes on first PlaceStructure → awards +50g
        Assert.Equal(startGold - cost + 50, sim.ItemManager.GetStock("gold"));
    }

    [Fact]
    public void PlaceStructure_Success_EntityAppearsOnGrid()
    {
        var (boot, sim) = CreateNewGame();
        var pos = new GridPosRPG(3, 3);
        var spawner = boot.Services.Get<ProtoFactory>().CreateVillageSpawner("spawner_basic")!;

        boot.Services.Get<CommandBus>().Dispatch(new PlaceStructureCommand(spawner, pos, "Spawner"));

        Assert.True(sim.TileManager.GetStructureIdAt(pos).HasValue);
    }

    [Fact]
    public void PlaceStructure_Success_PublishesGoldChangedEvent()
    {
        var (boot, sim) = CreateNewGame();
        var captured = new List<GoldChangedEvent>();
        boot.Services.Get<EventBus>().Subscribe<GoldChangedEvent>(e => captured.Add(e));

        var spawner = boot.Services.Get<ProtoFactory>().CreateVillageSpawner("spawner_basic")!;
        boot.Services.Get<CommandBus>().Dispatch(new PlaceStructureCommand(
            spawner, new GridPosRPG(5, 5), "Spawner"));

        Assert.True(captured.Count >= 1);
        Assert.Contains(captured, e => e.Reason.Contains("Spawner"));
    }

    [Fact]
    public void PlaceStructure_Success_AdvancesTutorial()
    {
        var (boot, sim) = CreateNewGame();
        var tutorial = sim.TutorialSystem;
        var firstMission = tutorial.ActiveMission;
        Assert.NotNull(firstMission);

        var spawner = boot.Services.Get<ProtoFactory>().CreateVillageSpawner("spawner_basic")!;
        boot.Services.Get<CommandBus>().Dispatch(new PlaceStructureCommand(
            spawner, new GridPosRPG(5, 5), "Spawner"));

        // PlaceStructure condition should have been advanced
        var placeCond = firstMission!.Conditions.Find(
            c => c.Type == TutorialConditionType.PlaceStructure);
        if (placeCond != null)
        {
            Assert.True(placeCond.CurrentCount >= 1);
        }
    }

    // ── Failure: Insufficient Gold ───────────────────────────────────

    [Fact]
    public void PlaceStructure_InsufficientGold_ReturnsFail()
    {
        var (boot, sim) = CreateNewGame();

        // Drain all gold
        sim.ItemManager.TrySpendStock("gold", sim.ItemManager.GetStock("gold"));
        Assert.Equal(0, sim.ItemManager.GetStock("gold"));

        var spawner = boot.Services.Get<ProtoFactory>().CreateVillageSpawner("spawner_basic")!;
        var result = boot.Services.Get<CommandBus>().Dispatch(new PlaceStructureCommand(
            spawner, new GridPosRPG(5, 5), "Spawner"));

        Assert.False(result.Success);
        Assert.Equal("Not enough gold", result.Reason);
    }

    [Fact]
    public void PlaceStructure_InsufficientGold_DoesNotDeductGold()
    {
        var (boot, sim) = CreateNewGame();
        sim.ItemManager.TrySpendStock("gold", sim.ItemManager.GetStock("gold"));

        var spawner = boot.Services.Get<ProtoFactory>().CreateVillageSpawner("spawner_basic")!;
        boot.Services.Get<CommandBus>().Dispatch(new PlaceStructureCommand(
            spawner, new GridPosRPG(5, 5), "Spawner"));

        Assert.Equal(0, sim.ItemManager.GetStock("gold"));
    }

    [Fact]
    public void PlaceStructure_InsufficientGold_PublishesGatingBlockedEvent()
    {
        var (boot, sim) = CreateNewGame();
        sim.ItemManager.TrySpendStock("gold", sim.ItemManager.GetStock("gold"));

        GatingBlockedEvent? captured = null;
        boot.Services.Get<EventBus>().Subscribe<GatingBlockedEvent>(e => captured = e);

        var spawner = boot.Services.Get<ProtoFactory>().CreateVillageSpawner("spawner_basic")!;
        boot.Services.Get<CommandBus>().Dispatch(new PlaceStructureCommand(
            spawner, new GridPosRPG(5, 5), "Spawner"));

        Assert.NotNull(captured);
        Assert.Equal("gold", captured!.Value.BlockedAction);
    }

    // ── Routing Nodes ────────────────────────────────────────────────

    [Fact]
    public void PlaceStructure_RoutingNode_Success()
    {
        var (boot, sim) = CreateNewGame();
        var splitter = boot.Services.Get<ProtoFactory>().CreateFilterSplitter("filter_splitter_basic")!;

        var result = boot.Services.Get<CommandBus>().Dispatch(new PlaceStructureCommand(
            splitter, new GridPosRPG(7, 7), "FilterSplitter"));

        Assert.True(result.Success);
        Assert.NotNull(sim.EntityManager.GetRoutingNode(splitter.Id));
    }

    // ── Multiple Placements ──────────────────────────────────────────

    [Fact]
    public void PlaceStructure_MultiplePlacements_DrainGoldCorrectly()
    {
        var (boot, sim) = CreateNewGame(); // 100g on Normal
        int gold = sim.ItemManager.GetStock("gold");

        // Spawner (25g) → tutorial completes (+50g) → net 125g
        var spawner = boot.Services.Get<ProtoFactory>().CreateVillageSpawner("spawner_basic")!;
        boot.Services.Get<CommandBus>().Dispatch(new PlaceStructureCommand(
            spawner, new GridPosRPG(0, 0), "Spawner"));
        gold = gold - 25 + 50; // tutorial reward

        // Inn (20g) → 105g
        var inn = boot.Services.Get<ProtoFactory>().CreateInn("inn_basic")!;
        boot.Services.Get<CommandBus>().Dispatch(new PlaceStructureCommand(
            inn, new GridPosRPG(5, 5), "Inn"));
        gold -= 20;

        Assert.Equal(gold, sim.ItemManager.GetStock("gold"));
    }

    // ── CommandBus Integration ────────────────────────────────────────

    [Fact]
    public void CommandBus_HasPlaceStructureHandler_AfterBootstrap()
    {
        var boot = new GameBootstrapper();
        boot.Bootstrap();

        Assert.True(boot.Services.Get<CommandBus>().HasHandler<PlaceStructureCommand>());
    }
}
