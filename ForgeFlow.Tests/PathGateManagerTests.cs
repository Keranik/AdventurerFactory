using ForgeFlow.Core.Data;
using ForgeFlow.Core.Entities;
using ForgeFlow.Core.Events;
using ForgeFlow.Core.Proto;
using ForgeFlow.Core.Proto.Logic;
using ForgeFlow.Core.Proto.Prototypes;
using ForgeFlow.Core.Systems;
using ForgeFlow.Core.Utilities;

namespace ForgeFlow.Tests;

/// <summary>
/// Tests for PathGateManager — the single source of truth for all PathGate operations:
/// placement, removal, auto-linking, entrance detection, exit logic, spatial queries, and ticking.
/// </summary>
public class PathGateManagerTests
{
    private static (PathGateManager pgm, EntityManager entMgr, TileManager tileMgr,
        VillagerSystem villagerSystem, PathTrafficSystem pathTraffic, EventBus bus)
        CreatePathGateManager()
    {
        var bus = new EventBus();
        var terrain = new TerrainGrid(32, 32);
        terrain.GenerateDefault();
        var tileMgr = new TileManager(terrain);
        var rm = new ItemManager(bus);
        var villagerSystem = new VillagerSystem(bus, rm);
        var entMgr = new EntityManager(tileMgr, villagerSystem);
        var trafficMgr = new TrafficManager();
        var pathTraffic = new PathTrafficSystem(trafficMgr, entMgr, tileMgr, bus);
        var protoReg = new ProtoRegistry();
        protoReg.RegisterDefaults();
        var protoFactory = new ProtoFactory(protoReg, bus);
        var tutorialSystem = new TutorialSystem(bus, protoFactory);

        var pgm = new PathGateManager(entMgr, tileMgr, bus, new Core.Commands.CommandBus(), tutorialSystem, rm, new ResearchManager(bus));
        pathTraffic.SetPathGateManager(pgm);
        return (pgm, entMgr, tileMgr, villagerSystem, pathTraffic, bus);
    }

    // ── AddPathGate ──────────────────────────────────────────────────

    [Fact]
    public void AddPathGate_CreatesGateAndRegistersInEntityManager()
    {
        EntityBase.ResetIdCounter();
        var (pgm, entMgr, tileMgr, _, _, _) = CreatePathGateManager();
        var pos = new GridPosRPG(5, 5);

        var gate = pgm.AddPathGate(pos, Direction.North);

        Assert.NotNull(gate);
        Assert.Equal(pos, gate!.Position);
        Assert.Equal(1, entMgr.PathGateCount);
        Assert.True(tileMgr.GetPathGateIdAt(pos).HasValue);
    }

    [Fact]
    public void AddPathGate_PublishesPathGatePlacedEvent()
    {
        EntityBase.ResetIdCounter();
        var (pgm, _, _, _, _, bus) = CreatePathGateManager();

        PathGatePlacedEvent? received = null;
        bus.Subscribe<PathGatePlacedEvent>(e => received = e);

        pgm.AddPathGate(new GridPosRPG(5, 5), Direction.North);

        Assert.NotNull(received);
    }

    [Fact]
    public void AddPathGate_AutoLinksToAdjacentStructure()
    {
        EntityBase.ResetIdCounter();
        var (pgm, entMgr, _, _, _, _) = CreatePathGateManager();

        // Place a structure at (5,5)
        var spawner = new VillageSpawnerLogic(EntityId.Next());
        entMgr.AddStructure(spawner, new GridPosRPG(5, 5));

        // Place a PathGate at (5,6) — structure is south, auto-link finds it
        var gate = pgm.AddPathGate(new GridPosRPG(5, 6), Direction.North);

        Assert.NotNull(gate);
        Assert.True(gate!.IsLinked);
        Assert.Equal(spawner.Id, gate.LinkedStructureId);
    }

    // ── RemovePathGate ───────────────────────────────────────────────

    [Fact]
    public void RemovePathGate_RemovesFromEntityManagerAndTileManager()
    {
        EntityBase.ResetIdCounter();
        var (pgm, entMgr, tileMgr, _, _, _) = CreatePathGateManager();
        var pos = new GridPosRPG(5, 5);

        pgm.AddPathGate(pos, Direction.North);
        Assert.Equal(1, entMgr.PathGateCount);

        pgm.RemovePathGate(pos);

        Assert.Equal(0, entMgr.PathGateCount);
        Assert.Null(entMgr.GetPathGateAt(pos));
        Assert.False(tileMgr.GetPathGateIdAt(pos).HasValue);
    }

    [Fact]
    public void RemovePathGate_DoesNothing_WhenPositionEmpty()
    {
        EntityBase.ResetIdCounter();
        var (pgm, entMgr, _, _, _, _) = CreatePathGateManager();

        pgm.RemovePathGate(new GridPosRPG(99, 99));

        Assert.Equal(0, entMgr.PathGateCount);
    }

    // ── CountPathGates ───────────────────────────────────────────────

    [Fact]
    public void CountPathGates_ReturnsCorrectCount()
    {
        EntityBase.ResetIdCounter();
        var (pgm, _, _, _, _, _) = CreatePathGateManager();

        Assert.Equal(0, pgm.CountPathGates());

        pgm.AddPathGate(new GridPosRPG(1, 1), Direction.North);
        pgm.AddPathGate(new GridPosRPG(2, 2), Direction.East);

        Assert.Equal(2, pgm.CountPathGates());
    }

    // ── PathGates Proxy ──────────────────────────────────────────────

    [Fact]
    public void PathGates_ProxyMatchesEntityManager()
    {
        EntityBase.ResetIdCounter();
        var (pgm, entMgr, _, _, _, _) = CreatePathGateManager();

        pgm.AddPathGate(new GridPosRPG(5, 5), Direction.East);

        Assert.Same(entMgr.PathGates, pgm.PathGates);
    }

    // ── FindExitGateForStructure ───────────────────────────────────────

    [Fact]
    public void FindExitGateForStructure_ReturnsGate_WhenLinked()
    {
        EntityBase.ResetIdCounter();
        var (pgm, entMgr, _, _, _, _) = CreatePathGateManager();

        var structureId = 123UL;
        var gate = new PathGateLogic(EntityId.Next())
        {
            Position = new GridPosRPG(4, 4),
            Facing = Direction.South
        };
        gate.LinkToStructure(structureId, new GridPosRPG(4, 5));
        entMgr.AddPathGate(gate);

        var found = pgm.FindExitGateForStructure(structureId);
        Assert.NotNull(found);
        Assert.Equal(gate.Id, found!.Id);
        Assert.True(found.IsExit);
    }

    [Fact]
    public void FindExitGateForStructure_ReturnsNull_WhenNoExitGate()
    {
        EntityBase.ResetIdCounter();
        var (pgm, _, _, _, _, _) = CreatePathGateManager();

        Assert.Null(pgm.FindExitGateForStructure(999));
    }

    // ── FindEntranceGateForStructure ───────────────────────────────────

    [Fact]
    public void FindEntranceGateForStructure_ReturnsGate_WhenLinked()
    {
        EntityBase.ResetIdCounter();
        var (pgm, entMgr, _, _, _, _) = CreatePathGateManager();

        var structureId = 456UL;
        var gate = new PathGateLogic(EntityId.Next())
        {
            Position = new GridPosRPG(4, 4),
            Facing = Direction.North
        };
        // Facing toward building at (4,5) → entrance
        gate.LinkToStructure(structureId, new GridPosRPG(4, 5));
        entMgr.AddPathGate(gate);

        var found = pgm.FindEntranceGateForStructure(structureId);
        Assert.NotNull(found);
        Assert.Equal(gate.Id, found!.Id);
        Assert.True(found.IsEntrance);
    }

    [Fact]
    public void FindEntranceGateForStructure_ReturnsNull_WhenNoEntranceGate()
    {
        EntityBase.ResetIdCounter();
        var (pgm, _, _, _, _, _) = CreatePathGateManager();

        Assert.Null(pgm.FindEntranceGateForStructure(999));
    }

    // ── ExitVillagerViaGate ──────────────────────────────────────────

    [Fact]
    public void ExitVillagerViaGate_PlacesVillagerOnAdjacentPath()
    {
        EntityBase.ResetIdCounter();
        var (pgm, entMgr, _, _, _, _) = CreatePathGateManager();

        ulong structureId = 999;

        // Exit gate at (3, 6) facing South (away from structure at (3,7))
        var gate = new PathGateLogic(EntityId.Next())
        {
            Position = new GridPosRPG(3, 6),
            Facing = Direction.South
        };
        gate.LinkToStructure(structureId, new GridPosRPG(3, 7));
        Assert.True(gate.IsExit);
        entMgr.AddPathGate(gate);

        // Path segment at the gate's facing position (3, 5)
        var exitSeg = new PathSegmentLogic(EntityId.Next())
        {
            Position = new GridPosRPG(3, 5),
            Facing = Direction.East
        };
        entMgr.AddPathSegment(exitSeg);

        var villager = new VillagerLogic(EntityId.Next())
        {
            Name = "Exiting",
            State = VillagerState.LeavingBuilding,
            Position = new GridPosRPG(3, 7)
        };

        bool result = pgm.ExitVillagerViaGate(villager, new EntityId(structureId));
        Assert.Equal(VillagerState.Travelling, villager.State);
        Assert.NotNull(villager.CurrentPathSegmentId);
    }

    [Fact]
    public void ExitVillagerViaGate_ReturnsFalse_WhenNoExitGate()
    {
        EntityBase.ResetIdCounter();
        var (pgm, _, _, _, _, _) = CreatePathGateManager();

        var villager = new VillagerLogic(EntityId.Next())
        {
            Name = "Stuck",
            State = VillagerState.LeavingBuilding
        };

        bool result = pgm.ExitVillagerViaGate(villager, new EntityId(999));

        Assert.False(result);
    }

    // ── CheckPathGateEntrance ────────────────────────────────────────

    [Fact]
    public void CheckPathGateEntrance_ReturnsFalse_WhenNoGatesExist()
    {
        EntityBase.ResetIdCounter();
        var (pgm, entMgr, _, _, _, _) = CreatePathGateManager();

        var seg = new PathSegmentLogic(EntityId.Next())
        {
            Position = new GridPosRPG(5, 5),
            Facing = Direction.East
        };
        entMgr.AddPathSegment(seg);

        var villager = new VillagerLogic(EntityId.Next())
        {
            Name = "Walker",
            State = VillagerState.Travelling
        };

        bool diverted = pgm.CheckPathGateEntrance(villager, seg);

        Assert.False(diverted);
    }

    [Fact]
    public void CheckPathGateEntrance_ReturnsFalse_WhenVillagerNotTravelling()
    {
        EntityBase.ResetIdCounter();
        var (pgm, entMgr, _, _, _, _) = CreatePathGateManager();

        var seg = new PathSegmentLogic(EntityId.Next())
        {
            Position = new GridPosRPG(5, 5),
            Facing = Direction.East
        };
        entMgr.AddPathSegment(seg);

        var villager = new VillagerLogic(EntityId.Next())
        {
            Name = "Idle",
            State = VillagerState.Idle
        };

        bool diverted = pgm.CheckPathGateEntrance(villager, seg);

        Assert.False(diverted);
    }

    // ── TickPathGates ────────────────────────────────────────────────

    [Fact]
    public void TickPathGates_DoesNotThrow()
    {
        EntityBase.ResetIdCounter();
        var (pgm, _, _, _, _, _) = CreatePathGateManager();

        pgm.AddPathGate(new GridPosRPG(1, 1), Direction.North);
        pgm.AddPathGate(new GridPosRPG(2, 2), Direction.East);

        // Should not throw — just verifies the tick loop runs cleanly
        pgm.TickPathGates(0.016f);
    }

    // ── Regression: villager visual stall on building entry (Bibles bug §1.6) ──

    /// <summary>
    /// Regression for the "villager parked at gate tile" bug. When a villager
    /// crosses into a path tile adjacent to an entrance PathGate,
    /// PathTrafficSystem.TickVillagersFromBuffer used to call
    /// state.ApplyToLogic(villager) AFTER the gate had mutated the villager,
    /// stomping State / CurrentPathSegmentId / Position back to their
    /// pre-diversion values. Symptoms: villager.State stays Travelling,
    /// CurrentPathSegmentId stays on the path tile, Position never moves to
    /// the structure — the visual stays "ghosted" at the gate tile and the
    /// player perceives the villager as stopping mid-path.
    ///
    /// Fix: re-snapshot the buffer from VillagerLogic after gate diversion so
    /// the trailing ApplyToLogic becomes a no-op write of correct values.
    /// </summary>
    [Fact]
    public void TickVillagers_DivertedByEntranceGate_DoesNotStompGateState()
    {
        EntityBase.ResetIdCounter();
        var (pgm, entMgr, _, villagerSystem, pathTraffic, _) = CreatePathGateManager();

        // Inn at (5,5). Entrance PathGate at (5,6) facing South → FacingPosition
        // is the inn (5,5), so AutoLinkPathGate marks it as Entrance.
        var inn = new InnLogic(EntityId.Next());
        entMgr.AddStructure(inn, new GridPosRPG(5, 5));
        var gate = pgm.AddPathGate(new GridPosRPG(5, 6), Direction.South);
        Assert.NotNull(gate);
        Assert.True(gate!.IsEntrance);
        Assert.Equal(inn.Id.Value, gate.LinkedStructureId);

        // Two path tiles: source (5,8) → destination (5,7). Destination is
        // the gate's neighbor, so crossing into it triggers diversion.
        var segSource = new PathSegmentLogic(EntityId.Next())
        {
            Position = new GridPosRPG(5, 8),
            Facing = Direction.North
        };
        var segDest = new PathSegmentLogic(EntityId.Next())
        {
            Position = new GridPosRPG(5, 7),
            Facing = Direction.North
        };
        segSource.NextSegmentId = segDest.Id.Value;
        entMgr.AddPathSegment(segSource);
        entMgr.AddPathSegment(segDest);

        // Villager travelling on source segment, near the end of it.
        var villager = new VillagerLogic(EntityId.Next())
        {
            Name = "Tester",
            State = VillagerState.Travelling,
            Position = segSource.Position,
            MovementSpeed = 2.0f,
            PathProgress = 0.99f
        };
        villager.PlaceOnPath(segSource.Id.Value);
        villager.State = VillagerState.Travelling; // PlaceOnPath resets PathProgress; restore
        villager.PathProgress = 0.99f;
        segSource.TryAddOccupant(villager.Id);
        Assert.True(villagerSystem.AddVillager(villager));

        // Single tick: progress (0.99 + 0.2) crosses 1.0 → villager moves into
        // segDest, gate diverts into the inn.
        pathTraffic.TickVillagers(0.1f);

        // Gate's writes must survive the tick (Bug A/B):
        Assert.Equal(VillagerState.Resting, villager.State);
        Assert.Null(villager.CurrentPathSegmentId);
        Assert.Equal(inn.Position.X, villager.Position.X);
        Assert.Equal(inn.Position.Y, villager.Position.Y);
        // And the inn registers the occupant.
        Assert.Contains(villager.Id.Value, inn.CurrentOccupants);
    }

    /// <summary>
    /// Regression: ExitVillagerViaGate must publish VillagerLeftBuildingEvent
    /// so the Presentation layer can re-show the hidden VillagerMb.
    /// </summary>
    [Fact]
    public void ExitVillagerViaGate_PublishesVillagerLeftBuildingEvent()
    {
        EntityBase.ResetIdCounter();
        var (pgm, entMgr, _, _, _, bus) = CreatePathGateManager();

        var inn = new InnLogic(EntityId.Next());
        entMgr.AddStructure(inn, new GridPosRPG(3, 7));

        var gate = new PathGateLogic(EntityId.Next())
        {
            Position = new GridPosRPG(3, 6),
            Facing = Direction.South
        };
        gate.LinkToStructure(inn.Id.Value, new GridPosRPG(3, 7));
        Assert.True(gate.IsExit);
        entMgr.AddPathGate(gate);

        var exitSeg = new PathSegmentLogic(EntityId.Next())
        {
            Position = new GridPosRPG(3, 5),
            Facing = Direction.East
        };
        entMgr.AddPathSegment(exitSeg);

        var villager = new VillagerLogic(EntityId.Next())
        {
            Name = "Leaver",
            State = VillagerState.LeavingBuilding,
            Position = new GridPosRPG(3, 7)
        };

        VillagerLeftBuildingEvent? received = null;
        bus.Subscribe<VillagerLeftBuildingEvent>(e => received = e);

        bool ok = pgm.ExitVillagerViaGate(villager, inn.Id);

        Assert.True(ok);
        Assert.NotNull(received);
        Assert.Equal(villager.Id, received!.Value.VillagerId);
        Assert.Equal(inn.Id, received.Value.BuildingId);
    }

    // ── Integration: SimulationTicker proxy delegates to PathGateManager ──

    [Fact]
    public void SimulationTicker_AddPathGate_DelegatesToPathGateManager()
    {
        EntityBase.ResetIdCounter();
        var bus = new EventBus();
        var terrain = new TerrainGrid(32, 32);
        terrain.GenerateDefault();
        var tileMgr = new TileManager(terrain);
        var rm = new ItemManager(bus);
        var vs = new VillagerSystem(bus, rm);
        var entMgr = new EntityManager(tileMgr, vs);
        var tm = new TrafficManager();
        var pt = new PathTrafficSystem(tm, entMgr, tileMgr, bus);
        var protoReg = new ProtoRegistry();
        protoReg.RegisterDefaults();
        var pf = new ProtoFactory(protoReg, bus);
        var ts = new TutorialSystem(bus, pf);
        var gl = new GatingLimits();
        var rsMgr = new ResearchManager(bus);
        var cmdBus = new Core.Commands.CommandBus();
        var pm = new PathNodeManager(entMgr, tileMgr, vs, pt, bus, cmdBus, ts, gl, rsMgr, rm);
        var pgm = new PathGateManager(entMgr, tileMgr, bus, cmdBus, ts, rm, rsMgr);
        pt.SetPathGateManager(pgm);
        var classReg = new ClassRegistry();
        var ae = new AutoEquipSystem(bus, classReg, entMgr);
        var dr = new DungeonResolver(new DungeonRegistry(), classReg, bus, entMgr);
        var fc = new FusionCalculator(classReg, new ItemRegistry(), rm, bus, entMgr);
        var aa = new AppearanceApplier(bus, entMgr);
        var mm = new StructureManager(entMgr, pgm, pm, vs, rm, new ItemRegistry(), new RecipeRegistry(), gl, bus, cmdBus, ts, rsMgr);
        var wsMgr = new WorldStateManager(rsMgr, entMgr, rm, bus);
        var dm = new DungeonManager(entMgr, pgm, vs, rm, bus);
        var sim = new SimulationTicker(bus, cmdBus, pt, vs, ts, entMgr, tileMgr, pm, pgm, rm, gl, mm, rsMgr, wsMgr, aa, dr, dm);

        sim.PathGateManager.AddPathGate(new GridPosRPG(5, 5), Direction.East);
        Assert.Equal(1, entMgr.PathGateCount);

        sim.PathGateManager.RemovePathGate(new GridPosRPG(5, 5));
        Assert.Equal(0, entMgr.PathGateCount);
    }
}
