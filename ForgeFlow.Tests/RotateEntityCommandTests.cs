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
/// Tests for the RotateEntityCommand handler in StructureManager.
/// Validates rotation of path segments, PathGates, and routing nodes
/// via the command dispatch pattern.
/// </summary>
public class RotateEntityCommandTests
{
    private static (GameBootstrapper boot, SimulationTicker sim) CreateNewGame(
        Difficulty difficulty = Difficulty.Normal)
    {
        EntityBase.ResetIdCounter();
        var boot = new GameBootstrapper();
        boot.Bootstrap();
        var settings = new NewGameSettings
        {
            GameName = "RotateTest",
            Difficulty = difficulty,
            Seed = 42,
            GuildName = "RotateGuild"
        };
        boot.ApplyNewGameSettings(settings);
        return (boot, boot.Services.Get<SimulationTicker>());
    }

    // ── Path Segment Rotation ───────────────────────────────────────

    [Fact]
    public void Rotate_PathSegment_ReturnsOkAndChangesFacing()
    {
        var (boot, sim) = CreateNewGame();
        var pos = new GridPosRPG(5, 5);

        sim.PathNodeManager.AddPathSegment(pos, Direction.East);
        var seg = sim.EntityManager.GetPathSegmentAt(pos);
        Assert.NotNull(seg);
        Assert.Equal(Direction.East, seg!.Facing);

        var result = boot.Services.Get<CommandBus>().Dispatch(new RotateEntityCommand(pos));

        Assert.True(result.Success);
        Assert.Equal(Direction.South, seg.Facing);
    }

    [Fact]
    public void Rotate_PathSegment_PublishesEntityRotatedEvent()
    {
        var (boot, sim) = CreateNewGame();
        var pos = new GridPosRPG(6, 6);

        sim.PathNodeManager.AddPathSegment(pos, Direction.East);

        var events = new List<EntityRotatedEvent>();
        boot.Services.Get<EventBus>().Subscribe<EntityRotatedEvent>(e => events.Add(e));

        boot.Services.Get<CommandBus>().Dispatch(new RotateEntityCommand(pos));

        Assert.Single(events);
        Assert.Equal("PathSegment", events[0].EntityType);
        Assert.Equal(pos, events[0].Position);
        Assert.Equal(Direction.South, events[0].NewFacing);
    }

    // ── PathGate Rotation ───────────────────────────────────────────

    [Fact]
    public void Rotate_PathGate_ReturnsOkAndChangesFacing()
    {
        var (boot, sim) = CreateNewGame();
        var pos = new GridPosRPG(7, 7);

        sim.PathGateManager.AddPathGate(pos, Direction.East);
        var gate = sim.EntityManager.GetPathGateAt(pos);
        Assert.NotNull(gate);
        Assert.Equal(Direction.East, gate!.Facing);

        var result = boot.Services.Get<CommandBus>().Dispatch(new RotateEntityCommand(pos));

        Assert.True(result.Success);
        Assert.Equal(Direction.South, gate.Facing);
    }

    [Fact]
    public void Rotate_PathGate_PublishesEntityRotatedEvent()
    {
        var (boot, sim) = CreateNewGame();
        var pos = new GridPosRPG(8, 8);

        sim.PathGateManager.AddPathGate(pos, Direction.East);

        var events = new List<EntityRotatedEvent>();
        boot.Services.Get<EventBus>().Subscribe<EntityRotatedEvent>(e => events.Add(e));

        boot.Services.Get<CommandBus>().Dispatch(new RotateEntityCommand(pos));

        Assert.Single(events);
        Assert.Equal("PathGate", events[0].EntityType);
        Assert.Equal(pos, events[0].Position);
    }

    // ── Routing Node Rotation ───────────────────────────────────────

    [Fact]
    public void Rotate_RoutingNode_ReturnsOkAndChangesDirection()
    {
        var (boot, sim) = CreateNewGame();
        var pos = new GridPosRPG(9, 9);

        var splitter = boot.Services.Get<ProtoFactory>().CreateFilterSplitter("filter_splitter_basic")!;
        var originalDir = splitter.OutputDirection;
        sim.StructureManager.AddProtoStructure(splitter, pos);

        var result = boot.Services.Get<CommandBus>().Dispatch(new RotateEntityCommand(pos));

        Assert.True(result.Success);
        Assert.NotEqual(originalDir, splitter.OutputDirection);
    }

    [Fact]
    public void Rotate_RoutingNode_PublishesEntityRotatedEvent()
    {
        var (boot, sim) = CreateNewGame();
        var pos = new GridPosRPG(10, 10);

        var splitter = boot.Services.Get<ProtoFactory>().CreateFilterSplitter("filter_splitter_basic")!;
        sim.StructureManager.AddProtoStructure(splitter, pos);

        var events = new List<EntityRotatedEvent>();
        boot.Services.Get<EventBus>().Subscribe<EntityRotatedEvent>(e => events.Add(e));

        boot.Services.Get<CommandBus>().Dispatch(new RotateEntityCommand(pos));

        Assert.Single(events);
        Assert.Equal(pos, events[0].Position);
    }

    // ── Empty Position ──────────────────────────────────────────────

    [Fact]
    public void Rotate_EmptyPosition_ReturnsFail()
    {
        var (boot, sim) = CreateNewGame();

        var result = boot.Services.Get<CommandBus>().Dispatch(new RotateEntityCommand(new GridPosRPG(50, 50)));

        Assert.False(result.Success);
        Assert.Contains("No rotatable entity", result.Reason);
    }

    // ── Non-Rotatable Entity ────────────────────────────────────────

    [Fact]
    public void Rotate_Structure_ReturnsFail()
    {
        // Structures (like spawners) are not rotatable via RotateEntityCommand.
        // The handler checks PathSegment → PathGate → RoutingNode, skipping structures.
        var (boot, sim) = CreateNewGame();
        var pos = new GridPosRPG(11, 11);

        var spawner = boot.Services.Get<ProtoFactory>().CreateVillageSpawner("spawner_basic")!;
        sim.StructureManager.AddProtoStructure(spawner, pos);

        var result = boot.Services.Get<CommandBus>().Dispatch(new RotateEntityCommand(pos));

        Assert.False(result.Success);
    }

    // ── Parity with Direct Calls ────────────────────────────────────

    [Fact]
    public void Rotate_PathSegment_ProducesSameResultAsDirectCall()
    {
        // Via command
        var (boot1, sim1) = CreateNewGame();
        var pos = new GridPosRPG(12, 12);
        sim1.PathNodeManager.AddPathSegment(pos, Direction.East);
        boot1.Services.Get<CommandBus>().Dispatch(new RotateEntityCommand(pos));
        var facingAfterCommand = sim1.EntityManager.GetPathSegmentAt(pos)!.Facing;

        // Via direct call
        EntityBase.ResetIdCounter();
        var (boot2, sim2) = CreateNewGame();
        sim2.PathNodeManager.AddPathSegment(pos, Direction.East);
        sim2.PathNodeManager.RotatePathSegment(pos);
        var facingAfterDirect = sim2.EntityManager.GetPathSegmentAt(pos)!.Facing;

        Assert.Equal(facingAfterDirect, facingAfterCommand);
    }
}
