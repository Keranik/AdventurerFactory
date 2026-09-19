using ForgeFlow.Core;
using ForgeFlow.Core.Commands;
using ForgeFlow.Core.Entities;
using ForgeFlow.Core.Events;
using ForgeFlow.Core.Proto.Prototypes;
using ForgeFlow.Core.Save;
using ForgeFlow.Core.Systems;

namespace ForgeFlow.Tests;

/// <summary>
/// Tests for the DrawPathCommand handler in PathNodeManager.
/// Validates gold deduction, segment placement, event publishing, failure cases,
/// and tutorial advancement through the command dispatch pattern.
/// </summary>
public class DrawPathCommandTests
{
    private static (GameBootstrapper boot, SimulationTicker sim) CreateNewGame(
        Difficulty difficulty = Difficulty.Normal)
    {
        EntityBase.ResetIdCounter();
        var boot = new GameBootstrapper();
        boot.Bootstrap();
        var settings = new NewGameSettings
        {
            GameName = "PathCmdTest",
            Difficulty = difficulty,
            Seed = 42,
            GuildName = "PathGuild"
        };
        boot.ApplyNewGameSettings(settings);
        return (boot, boot.Services.Get<SimulationTicker>());
    }

    // ── Success Path ─────────────────────────────────────────────────

    [Fact]
    public void DrawPath_Success_ReturnsOk()
    {
        var (boot, sim) = CreateNewGame();

        var result = boot.Services.Get<CommandBus>().Dispatch(new DrawPathCommand(
            new GridPosRPG(5, 5), Direction.East));

        Assert.True(result.Success);
    }

    [Fact]
    public void DrawPath_Success_DeductsGold()
    {
        var (boot, sim) = CreateNewGame();
        int startGold = sim.ItemManager.GetStock("gold");
        int cost = EconomyConfig.PathSegmentCost;

        boot.Services.Get<CommandBus>().Dispatch(new DrawPathCommand(
            new GridPosRPG(5, 5), Direction.East));

        Assert.Equal(startGold - cost, sim.ItemManager.GetStock("gold"));
    }

    [Fact]
    public void DrawPath_Success_SegmentAppearsOnGrid()
    {
        var (boot, sim) = CreateNewGame();
        var pos = new GridPosRPG(5, 5);

        boot.Services.Get<CommandBus>().Dispatch(new DrawPathCommand(pos, Direction.East));

        Assert.True(sim.TileManager.GetPathIdAt(pos).HasValue);
    }

    [Fact]
    public void DrawPath_Success_PublishesGoldChangedEvent()
    {
        var (boot, sim) = CreateNewGame();
        var captured = new List<GoldChangedEvent>();
        boot.Services.Get<EventBus>().Subscribe<GoldChangedEvent>(e => captured.Add(e));

        boot.Services.Get<CommandBus>().Dispatch(new DrawPathCommand(
            new GridPosRPG(5, 5), Direction.East));

        Assert.True(captured.Count >= 1);
        Assert.Contains(captured, e => e.Reason.Contains("Path segment"));
    }

    [Fact]
    public void DrawPath_Success_PublishesPathBuiltEvent()
    {
        var (boot, sim) = CreateNewGame();
        PathBuiltEvent? captured = null;
        boot.Services.Get<EventBus>().Subscribe<PathBuiltEvent>(e => captured = e);

        boot.Services.Get<CommandBus>().Dispatch(new DrawPathCommand(
            new GridPosRPG(5, 5), Direction.East));

        Assert.NotNull(captured);
        Assert.Equal(new GridPosRPG(5, 5), captured!.Value.Position);
    }

    [Fact]
    public void DrawPath_Success_AdvancesBuildPathTutorialCondition()
    {
        var (boot, sim) = CreateNewGame();
        var tutorial = sim.TutorialSystem;
        var mission = tutorial.ActiveMission;

        boot.Services.Get<CommandBus>().Dispatch(new DrawPathCommand(
            new GridPosRPG(5, 5), Direction.East));

        var buildPathCond = mission?.Conditions.Find(
            c => c.Type == TutorialConditionType.BuildPath);
        if (buildPathCond != null)
        {
            Assert.True(buildPathCond.CurrentCount >= 1);
        }
    }

    // ── Failure: Insufficient Gold ───────────────────────────────────

    [Fact]
    public void DrawPath_InsufficientGold_ReturnsFail()
    {
        var (boot, sim) = CreateNewGame();
        sim.ItemManager.TrySpendStock("gold", sim.ItemManager.GetStock("gold"));
        Assert.Equal(0, sim.ItemManager.GetStock("gold"));

        var result = boot.Services.Get<CommandBus>().Dispatch(new DrawPathCommand(
            new GridPosRPG(5, 5), Direction.East));

        Assert.False(result.Success);
        Assert.Equal("Not enough gold", result.Reason);
    }

    [Fact]
    public void DrawPath_InsufficientGold_DoesNotPlaceSegment()
    {
        var (boot, sim) = CreateNewGame();
        sim.ItemManager.TrySpendStock("gold", sim.ItemManager.GetStock("gold"));

        boot.Services.Get<CommandBus>().Dispatch(new DrawPathCommand(
            new GridPosRPG(5, 5), Direction.East));

        Assert.False(sim.TileManager.GetPathIdAt(new GridPosRPG(5, 5)).HasValue);
    }

    // ── Multiple Placements ──────────────────────────────────────────

    [Fact]
    public void DrawPath_MultiplePlacements_DrainGoldCorrectly()
    {
        var (boot, sim) = CreateNewGame();
        int gold = sim.ItemManager.GetStock("gold");
        int cost = EconomyConfig.PathSegmentCost;

        boot.Services.Get<CommandBus>().Dispatch(new DrawPathCommand(new GridPosRPG(5, 5), Direction.East));
        boot.Services.Get<CommandBus>().Dispatch(new DrawPathCommand(new GridPosRPG(6, 5), Direction.East));
        boot.Services.Get<CommandBus>().Dispatch(new DrawPathCommand(new GridPosRPG(7, 5), Direction.East));

        Assert.Equal(gold - (cost * 3), sim.ItemManager.GetStock("gold"));
    }

    // ── NodeType Propagation ─────────────────────────────────────────

    [Fact]
    public void DrawPath_WithNodeType_CreatesCorrectType()
    {
        var (boot, sim) = CreateNewGame();
        var pos = new GridPosRPG(5, 5);

        boot.Services.Get<CommandBus>().Dispatch(new DrawPathCommand(pos, Direction.East, PathNodeType.Splitter));

        var segment = sim.EntityManager.GetPathSegmentAt(pos);
        Assert.NotNull(segment);
        Assert.Equal(PathNodeType.Splitter, segment!.NodeType);
    }
}
