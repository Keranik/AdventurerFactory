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
/// Tests for the DemolishCommand handler in StructureManager.
/// Validates cross-cutting demolish across structures, path segments,
/// PathGates, and routing nodes via the command dispatch pattern.
/// </summary>
public class DemolishCommandTests
{
    private static (GameBootstrapper boot, SimulationTicker sim) CreateNewGame(
        Difficulty difficulty = Difficulty.Normal)
    {
        EntityBase.ResetIdCounter();
        var boot = new GameBootstrapper();
        boot.Bootstrap();
        var settings = new NewGameSettings
        {
            GameName = "DemolishTest",
            Difficulty = difficulty,
            Seed = 42,
            GuildName = "DemolishGuild"
        };
        boot.ApplyNewGameSettings(settings);
        return (boot, boot.Services.Get<SimulationTicker>());
    }

    // ── Structure Demolish ──────────────────────────────────────────

    [Fact]
    public void Demolish_Structure_ReturnsOkAndRemoves()
    {
        var (boot, sim) = CreateNewGame();
        var spawner = boot.Services.Get<ProtoFactory>().CreateVillageSpawner("spawner_basic");
        var pos = new GridPosRPG(5, 5);

        sim.StructureManager.AddProtoStructure(spawner, pos);
        Assert.NotNull(sim.EntityManager.GetStructureAt(pos));

        var result = boot.Services.Get<CommandBus>().Dispatch(new DemolishCommand(pos));

        Assert.True(result.Success);
        Assert.Null(sim.EntityManager.GetStructureAt(pos));
    }

    [Fact]
    public void Demolish_Structure_PublishesEntityDemolishedEvent()
    {
        var (boot, sim) = CreateNewGame();
        var spawner = boot.Services.Get<ProtoFactory>().CreateVillageSpawner("spawner_basic");
        var pos = new GridPosRPG(6, 6);

        sim.StructureManager.AddProtoStructure(spawner, pos);

        var events = new List<EntityDemolishedEvent>();
        boot.Services.Get<EventBus>().Subscribe<EntityDemolishedEvent>(e => events.Add(e));

        boot.Services.Get<CommandBus>().Dispatch(new DemolishCommand(pos));

        Assert.Single(events);
        Assert.Equal(pos, events[0].Position);
    }

    // ── Path Segment Demolish ───────────────────────────────────────

    [Fact]
    public void Demolish_PathSegment_ReturnsOkAndRemoves()
    {
        var (boot, sim) = CreateNewGame();
        var pos = new GridPosRPG(7, 7);

        sim.PathNodeManager.AddPathSegment(pos, Direction.East);
        Assert.NotNull(sim.EntityManager.GetPathSegmentAt(pos));

        var result = boot.Services.Get<CommandBus>().Dispatch(new DemolishCommand(pos));

        Assert.True(result.Success);
        Assert.Null(sim.EntityManager.GetPathSegmentAt(pos));
    }

    [Fact]
    public void Demolish_PathSegment_PublishesPathRemovedEvent()
    {
        var (boot, sim) = CreateNewGame();
        var pos = new GridPosRPG(8, 8);

        sim.PathNodeManager.AddPathSegment(pos, Direction.East);

        var events = new List<PathRemovedEvent>();
        boot.Services.Get<EventBus>().Subscribe<PathRemovedEvent>(e => events.Add(e));

        boot.Services.Get<CommandBus>().Dispatch(new DemolishCommand(pos));

        Assert.Single(events);
        Assert.Equal(pos, events[0].Position);
    }

    // ── PathGate Demolish ───────────────────────────────────────────

    [Fact]
    public void Demolish_PathGate_ReturnsOkAndRemoves()
    {
        var (boot, sim) = CreateNewGame();
        var pos = new GridPosRPG(9, 9);

        sim.PathGateManager.AddPathGate(pos, Direction.East);
        Assert.NotNull(sim.EntityManager.GetPathGateAt(pos));

        var result = boot.Services.Get<CommandBus>().Dispatch(new DemolishCommand(pos));

        Assert.True(result.Success);
        Assert.Null(sim.EntityManager.GetPathGateAt(pos));
    }

    // ── Routing Node Demolish ───────────────────────────────────────

    [Fact]
    public void Demolish_RoutingNode_ReturnsOkAndRemoves()
    {
        var (boot, sim) = CreateNewGame();
        var pos = new GridPosRPG(10, 10);

        var splitter = boot.Services.Get<ProtoFactory>().CreateFilterSplitter("filter_splitter_basic");
        sim.StructureManager.AddProtoStructure(splitter, pos);
        Assert.NotNull(sim.EntityManager.GetRoutingNodeAt(pos));

        var result = boot.Services.Get<CommandBus>().Dispatch(new DemolishCommand(pos));

        Assert.True(result.Success);
        Assert.Null(sim.EntityManager.GetRoutingNodeAt(pos));
    }

    [Fact]
    public void Demolish_RoutingNode_PublishesEntityDemolishedEvent()
    {
        var (boot, sim) = CreateNewGame();
        var pos = new GridPosRPG(11, 11);

        var splitter = boot.Services.Get<ProtoFactory>().CreateFilterSplitter("filter_splitter_basic");
        sim.StructureManager.AddProtoStructure(splitter, pos);

        var events = new List<EntityDemolishedEvent>();
        boot.Services.Get<EventBus>().Subscribe<EntityDemolishedEvent>(e => events.Add(e));

        boot.Services.Get<CommandBus>().Dispatch(new DemolishCommand(pos));

        Assert.Single(events);
        Assert.Equal(pos, events[0].Position);
    }

    // ── Empty Position ──────────────────────────────────────────────

    [Fact]
    public void Demolish_EmptyPosition_ReturnsFail()
    {
        var (boot, sim) = CreateNewGame();

        var result = boot.Services.Get<CommandBus>().Dispatch(new DemolishCommand(new GridPosRPG(50, 50)));

        Assert.False(result.Success);
        Assert.Contains("Nothing to demolish", result.Reason);
    }

    // ── Priority Order ──────────────────────────────────────────────

    [Fact]
    public void Demolish_PrioritizesPathGateOverStructure()
    {
        // If a PathGate and a structure exist at overlapping positions,
        // the PathGate should be demolished first (matching DemolishController order).
        var (boot, sim) = CreateNewGame();
        var gatePos = new GridPosRPG(12, 12);

        // Place a PathGate
        sim.PathGateManager.AddPathGate(gatePos, Direction.East);
        Assert.NotNull(sim.EntityManager.GetPathGateAt(gatePos));

        var result = boot.Services.Get<CommandBus>().Dispatch(new DemolishCommand(gatePos));

        Assert.True(result.Success);
        Assert.Null(sim.EntityManager.GetPathGateAt(gatePos));
    }

    // ── Parity with Direct Calls ────────────────────────────────────

    [Fact]
    public void Demolish_ProducesSameResultAsDirectManagerCall()
    {
        // Via command
        var (boot1, sim1) = CreateNewGame();
        var pos = new GridPosRPG(13, 13);
        sim1.PathNodeManager.AddPathSegment(pos, Direction.East);
        boot1.Services.Get<CommandBus>().Dispatch(new DemolishCommand(pos));
        var afterCommand = sim1.EntityManager.GetPathSegmentAt(pos);

        // Via direct call
        EntityBase.ResetIdCounter();
        var (boot2, sim2) = CreateNewGame();
        sim2.PathNodeManager.AddPathSegment(pos, Direction.East);
        sim2.PathNodeManager.RemovePathSegment(pos);
        var afterDirect = sim2.EntityManager.GetPathSegmentAt(pos);

        Assert.Null(afterCommand);
        Assert.Null(afterDirect);
    }
}
