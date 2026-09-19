using ForgeFlow.Core;
using ForgeFlow.Core.Commands;
using ForgeFlow.Core.Entities;
using ForgeFlow.Core.Events;
using ForgeFlow.Core.Proto;
using ForgeFlow.Core.Proto.Logic;
using ForgeFlow.Core.Save;
using ForgeFlow.Core.Systems;

namespace ForgeFlow.Tests;

/// <summary>
/// Tests for the PlacePathGateCommand handler in PathGateManager.
/// Validates gold deduction, gate placement, event publishing, failure cases,
/// and tutorial advancement through the command dispatch pattern.
/// </summary>
public class PlacePathGateCommandTests
{
    private static (GameBootstrapper boot, SimulationTicker sim) CreateNewGame(
        Difficulty difficulty = Difficulty.Normal)
    {
        EntityBase.ResetIdCounter();
        var boot = new GameBootstrapper();
        boot.Bootstrap();
        var settings = new NewGameSettings
        {
            GameName = "GateCmdTest",
            Difficulty = difficulty,
            Seed = 42,
            GuildName = "GateGuild"
        };
        boot.ApplyNewGameSettings(settings);
        return (boot, boot.Services.Get<SimulationTicker>());
    }

    // ── Success Path ─────────────────────────────────────────────────

    [Fact]
    public void PlacePathGate_Success_ReturnsOk()
    {
        var (boot, sim) = CreateNewGame();

        // Place a structure first so the gate can auto-link
        var spawner = boot.Services.Get<ProtoFactory>().CreateVillageSpawner("spawner_basic");
        boot.Services.Get<CommandBus>().Dispatch(new PlaceStructureCommand(
            spawner, new GridPosRPG(5, 5), "Spawner"));

        var result = boot.Services.Get<CommandBus>().Dispatch(new PlacePathGateCommand(
            new GridPosRPG(5, 6), Direction.South));

        Assert.True(result.Success);
    }

    [Fact]
    public void PlacePathGate_Success_DeductsGold()
    {
        var (boot, sim) = CreateNewGame();
        var spawner = boot.Services.Get<ProtoFactory>().CreateVillageSpawner("spawner_basic");
        boot.Services.Get<CommandBus>().Dispatch(new PlaceStructureCommand(
            spawner, new GridPosRPG(5, 5), "Spawner"));

        int goldBefore = sim.ItemManager.GetStock("gold");
        int cost = EconomyConfig.GetStructureCost("PathGate");

        boot.Services.Get<CommandBus>().Dispatch(new PlacePathGateCommand(
            new GridPosRPG(5, 6), Direction.South));

        Assert.Equal(goldBefore - cost, sim.ItemManager.GetStock("gold"));
    }

    [Fact]
    public void PlacePathGate_Success_GateAppearsInEntityManager()
    {
        var (boot, sim) = CreateNewGame();

        boot.Services.Get<CommandBus>().Dispatch(new PlacePathGateCommand(
            new GridPosRPG(5, 5), Direction.East));

        var gate = sim.EntityManager.GetPathGateAt(new GridPosRPG(5, 5));
        Assert.NotNull(gate);
    }

    [Fact]
    public void PlacePathGate_Success_PublishesGoldChangedEvent()
    {
        var (boot, sim) = CreateNewGame();
        var captured = new List<GoldChangedEvent>();
        boot.Services.Get<EventBus>().Subscribe<GoldChangedEvent>(e => captured.Add(e));

        boot.Services.Get<CommandBus>().Dispatch(new PlacePathGateCommand(
            new GridPosRPG(5, 5), Direction.East));

        Assert.True(captured.Count >= 1);
        Assert.Contains(captured, e => e.Reason.Contains("PathGate"));
    }

    [Fact]
    public void PlacePathGate_Success_PublishesPathGatePlacedEvent()
    {
        var (boot, sim) = CreateNewGame();
        PathGatePlacedEvent? captured = null;
        boot.Services.Get<EventBus>().Subscribe<PathGatePlacedEvent>(e => captured = e);

        boot.Services.Get<CommandBus>().Dispatch(new PlacePathGateCommand(
            new GridPosRPG(5, 5), Direction.East));

        Assert.NotNull(captured);
        Assert.Equal(new GridPosRPG(5, 5), captured!.Value.Position);
    }

    // ── Failure: Insufficient Gold ───────────────────────────────────

    [Fact]
    public void PlacePathGate_InsufficientGold_ReturnsFail()
    {
        var (boot, sim) = CreateNewGame();
        sim.ItemManager.TrySpendStock("gold", sim.ItemManager.GetStock("gold"));

        var result = boot.Services.Get<CommandBus>().Dispatch(new PlacePathGateCommand(
            new GridPosRPG(5, 5), Direction.East));

        Assert.False(result.Success);
        Assert.Equal("Not enough gold", result.Reason);
    }

    [Fact]
    public void PlacePathGate_InsufficientGold_DoesNotPlaceGate()
    {
        var (boot, sim) = CreateNewGame();
        sim.ItemManager.TrySpendStock("gold", sim.ItemManager.GetStock("gold"));

        boot.Services.Get<CommandBus>().Dispatch(new PlacePathGateCommand(
            new GridPosRPG(5, 5), Direction.East));

        Assert.Null(sim.EntityManager.GetPathGateAt(new GridPosRPG(5, 5)));
    }

    [Fact]
    public void PlacePathGate_InsufficientGold_PublishesGatingBlockedEvent()
    {
        var (boot, sim) = CreateNewGame();
        sim.ItemManager.TrySpendStock("gold", sim.ItemManager.GetStock("gold"));

        GatingBlockedEvent? captured = null;
        boot.Services.Get<EventBus>().Subscribe<GatingBlockedEvent>(e => captured = e);

        boot.Services.Get<CommandBus>().Dispatch(new PlacePathGateCommand(
            new GridPosRPG(5, 5), Direction.East));

        Assert.NotNull(captured);
        Assert.Equal("gold", captured!.Value.BlockedAction);
    }

    // ── Multiple Placements ──────────────────────────────────────────

    [Fact]
    public void PlacePathGate_MultiplePlacements_DrainGoldCorrectly()
    {
        var (boot, sim) = CreateNewGame();
        int gold = sim.ItemManager.GetStock("gold");
        int cost = EconomyConfig.GetStructureCost("PathGate");

        boot.Services.Get<CommandBus>().Dispatch(new PlacePathGateCommand(new GridPosRPG(3, 3), Direction.East));
        boot.Services.Get<CommandBus>().Dispatch(new PlacePathGateCommand(new GridPosRPG(5, 5), Direction.West));

        Assert.Equal(gold - (cost * 2), sim.ItemManager.GetStock("gold"));
    }
}
