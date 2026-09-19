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
/// Tests for PathNodeManager â€” the single source of truth for all path graph operations:
/// creation, removal, linking, eviction, activation, and per-tick updates.
/// </summary>
public class PathNodeManagerTests
{
    private static (PathNodeManager pathMgr, EntityManager entMgr, TileManager tileMgr,
        VillagerSystem villagerSystem, PathTrafficSystem pathTraffic, EventBus bus)
        CreatePathManager()
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
        var gatingLimits = new GatingLimits();
        var rsMgr = new ResearchManager(bus);

        var pathMgr = new PathNodeManager(entMgr, tileMgr, villagerSystem, pathTraffic, bus, new Core.Commands.CommandBus(), tutorialSystem, gatingLimits, rsMgr, rm);
        return (pathMgr, entMgr, tileMgr, villagerSystem, pathTraffic, bus);
    }

    // â”€â”€ AddPathSegment â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    public void AddPathSegment_CreatesSegmentAndRegistersInEntityManager()
    {
        EntityBase.ResetIdCounter();
        var (pathMgr, entMgr, tileMgr, _, _, _) = CreatePathManager();
        var pos = new GridPosRPG(5, 5);

        var seg = pathMgr.AddPathSegment(pos, Direction.East, 1);

        Assert.NotNull(seg);
        Assert.Equal(pos, seg!.Position);
        Assert.Equal(Direction.East, seg.Facing);
        Assert.Equal(1, entMgr.PathSegmentCount);
        Assert.Equal(seg, entMgr.GetPathSegmentAt(pos));
        Assert.True(tileMgr.GetPathIdAt(pos).HasValue);
    }

    [Fact]
    public void AddPathSegment_PublishesPathBuiltEvent()
    {
        EntityBase.ResetIdCounter();
        var (pathMgr, _, _, _, _, bus) = CreatePathManager();
        PathBuiltEvent? received = null;
        bus.Subscribe<PathBuiltEvent>(e => received = e);

        pathMgr.AddPathSegment(new GridPosRPG(5, 5), Direction.East, 1);

        Assert.NotNull(received);
    }

    [Fact]
    public void AddPathSegment_NoLongerBlockedByTierLimits()
    {
        EntityBase.ResetIdCounter();
        var (pathMgr, _, _, _, _, bus) = CreatePathManager();

        // Place 31 paths at tier 1 - should all succeed (no tier gating)
        for (int i = 0; i < 31; i++)
        {
            var result = pathMgr.AddPathSegment(new GridPosRPG(i, 0), Direction.East, 1);
            Assert.NotNull(result);
        }
    }

    // â”€â”€ AutoLinkPath â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    public void AddPathSegment_AutoLinksForward()
    {
        EntityBase.ResetIdCounter();
        var (pathMgr, _, _, _, _, _) = CreatePathManager();

        // Place first segment facing East at (5,5) â€” its output is (6,5)
        var seg1 = pathMgr.AddPathSegment(new GridPosRPG(5, 5), Direction.East, 1);
        // Place second segment at (6,5) â€” should auto-link as seg1's next
        var seg2 = pathMgr.AddPathSegment(new GridPosRPG(6, 5), Direction.East, 1);

        Assert.NotNull(seg1);
        Assert.NotNull(seg2);
        Assert.Equal(seg2!.Id, seg1!.NextSegmentId);
        Assert.Equal(seg1.Id, seg2.PrevSegmentId);
    }

    [Fact]
    public void AddPathSegment_AutoLinksBackward()
    {
        EntityBase.ResetIdCounter();
        var (pathMgr, _, _, _, _, _) = CreatePathManager();

        // Place second segment first at (6,5)
        var seg2 = pathMgr.AddPathSegment(new GridPosRPG(6, 5), Direction.East, 1);
        // Place first segment at (5,5) facing East (output â†’ 6,5)
        var seg1 = pathMgr.AddPathSegment(new GridPosRPG(5, 5), Direction.East, 1);

        Assert.NotNull(seg1);
        Assert.NotNull(seg2);
        Assert.Equal(seg2!.Id, seg1!.NextSegmentId);
        Assert.Equal(seg1.Id, seg2.PrevSegmentId);
    }

    // â”€â”€ RemovePathSegment â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    public void RemovePathSegment_RemovesFromEntityManagerAndTileManager()
    {
        EntityBase.ResetIdCounter();
        var (pathMgr, entMgr, tileMgr, _, _, bus) = CreatePathManager();
        var pos = new GridPosRPG(5, 5);
        var ItemManager = new ItemManager(bus);

        pathMgr.AddPathSegment(pos, Direction.East, 1);
        Assert.Equal(1, entMgr.PathSegmentCount);

        pathMgr.RemovePathSegment(pos, ItemManager);

        Assert.Equal(0, entMgr.PathSegmentCount);
        Assert.Null(entMgr.GetPathSegmentAt(pos));
        Assert.False(tileMgr.GetPathIdAt(pos).HasValue);
    }

    [Fact]
    public void RemovePathSegment_PublishesPathRemovedEvent()
    {
        EntityBase.ResetIdCounter();
        var (pathMgr, _, _, _, _, bus) = CreatePathManager();
        var pos = new GridPosRPG(5, 5);
        var ItemManager = new ItemManager(bus);

        pathMgr.AddPathSegment(pos, Direction.East, 1);

        PathRemovedEvent? received = null;
        bus.Subscribe<PathRemovedEvent>(e => received = e);

        pathMgr.RemovePathSegment(pos, ItemManager);

        Assert.NotNull(received);
    }

    [Fact]
    public void RemovePathSegment_UnlinksNeighbors()
    {
        EntityBase.ResetIdCounter();
        var (pathMgr, _, _, _, _, bus) = CreatePathManager();
        var ItemManager = new ItemManager(bus);

        var seg1 = pathMgr.AddPathSegment(new GridPosRPG(5, 5), Direction.East, 1);
        var seg2 = pathMgr.AddPathSegment(new GridPosRPG(6, 5), Direction.East, 1);
        var seg3 = pathMgr.AddPathSegment(new GridPosRPG(7, 5), Direction.East, 1);

        Assert.NotNull(seg1);
        Assert.NotNull(seg2);
        Assert.NotNull(seg3);

        // Remove middle segment
        pathMgr.RemovePathSegment(new GridPosRPG(6, 5), ItemManager);

        Assert.Null(seg1!.NextSegmentId);
        Assert.Null(seg3!.PrevSegmentId);
    }

    [Fact]
    public void RemovePathSegment_DoesNothing_WhenPositionEmpty()
    {
        EntityBase.ResetIdCounter();
        var (pathMgr, entMgr, _, _, _, bus) = CreatePathManager();
        var ItemManager = new ItemManager(bus);

        pathMgr.RemovePathSegment(new GridPosRPG(99, 99), ItemManager);

        Assert.Equal(0, entMgr.PathSegmentCount);
    }

    // â”€â”€ EvictVillagersFromSegment â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    public void RemovePathSegment_EvictsVillagersAndReturnsInventory()
    {
        EntityBase.ResetIdCounter();
        var (pathMgr, entMgr, _, villagerSystem, pathTraffic, bus) = CreatePathManager();
        var pos = new GridPosRPG(5, 5);
        var ItemManager = new ItemManager(bus);

        var seg = pathMgr.AddPathSegment(pos, Direction.East, 1);
        Assert.NotNull(seg);

        // Create and place a villager on the path
        var villager = new VillagerLogic(EntityId.Next());
        villager.Inventory.Add(new ItemInstance { ProtoId = "wood", Quantity = 3 });
        villagerSystem.AddVillager(villager);
        pathTraffic.PlaceVillagerOnPath(villager, pos);

        Assert.Equal(VillagerState.Travelling, villager.State);

        VillagerReturnedToPoolEvent? evictEvent = null;
        bus.Subscribe<VillagerReturnedToPoolEvent>(e => evictEvent = e);

        pathMgr.RemovePathSegment(pos, ItemManager);

        Assert.NotNull(evictEvent);
        Assert.Equal(3, ItemManager.GetStock("wood"));
        Assert.Empty(villagerSystem.Villagers);
    }

    // â”€â”€ CreatePathLine â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    public void CreatePathLine_CreatesMultipleLinkedSegments()
    {
        EntityBase.ResetIdCounter();
        var (pathMgr, entMgr, _, _, _, _) = CreatePathManager();

        var segments = pathMgr.CreatePathLine(new GridPosRPG(2, 2), new GridPosRPG(5, 2), 1);

        Assert.Equal(4, segments.Count);
        Assert.Equal(4, entMgr.PathSegmentCount);

        // Verify linking: each segment should link to the next
        for (int i = 0; i < segments.Count - 1; i++)
        {
            Assert.Equal(segments[i + 1].Id, segments[i].NextSegmentId);
        }
    }

    [Fact]
    public void CreatePathLine_SkipsOccupiedPositions()
    {
        EntityBase.ResetIdCounter();
        var (pathMgr, entMgr, _, _, _, _) = CreatePathManager();

        // Place a segment at (3,2) first
        pathMgr.AddPathSegment(new GridPosRPG(3, 2), Direction.East, 1);

        var segments = pathMgr.CreatePathLine(new GridPosRPG(2, 2), new GridPosRPG(5, 2), 1);

        // Should skip (3,2) since it's already occupied
        Assert.Equal(3, segments.Count);
    }

    // â”€â”€ TickPaths â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    public void TickPaths_TicksAllSegments()
    {
        EntityBase.ResetIdCounter();
        var (pathMgr, _, _, _, _, _) = CreatePathManager();

        pathMgr.AddPathSegment(new GridPosRPG(5, 5), Direction.East, 1);
        pathMgr.AddPathSegment(new GridPosRPG(6, 5), Direction.East, 1);

        // Should not throw â€” just verifies the tick loop runs cleanly
        pathMgr.TickPaths(0.016f);
    }

    // â”€â”€ Proxy Properties â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    public void PathSegments_ProxyMatchesEntityManager()
    {
        EntityBase.ResetIdCounter();
        var (pathMgr, entMgr, _, _, _, _) = CreatePathManager();

        pathMgr.AddPathSegment(new GridPosRPG(5, 5), Direction.East, 1);

        Assert.Same(entMgr.PathSegments, pathMgr.PathSegments);
    }

    // â”€â”€ Integration: SimulationTicker proxy delegates to PathNodeManager â”€â”€

    [Fact]
    public void SimulationTicker_AddPathSegment_DelegatesToPathManager()
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
        var classReg = new ClassRegistry();
        var ae = new AutoEquipSystem(bus, classReg, entMgr);
        var dr = new DungeonResolver(new DungeonRegistry(), classReg, bus, entMgr);
        var fc = new FusionCalculator(classReg, new ItemRegistry(), rm, bus, entMgr);
        var aa = new AppearanceApplier(bus, entMgr);
        var pgm = new PathGateManager(entMgr, tileMgr, bus, cmdBus, ts, rm, rsMgr);
        pt.SetPathGateManager(pgm);
        var mm = new StructureManager(entMgr, pgm, pm, vs, rm, new ItemRegistry(), new RecipeRegistry(), gl, bus, cmdBus, ts, rsMgr);
        var wsMgr = new WorldStateManager(rsMgr, entMgr, rm, bus);
        var dm = new DungeonManager(entMgr, pgm, vs, rm, bus);
        var sim = new SimulationTicker(bus, cmdBus, pt, vs, ts, entMgr, tileMgr, pm, pgm, rm, gl, mm, rsMgr, wsMgr, aa, dr, dm);

        var seg = sim.PathNodeManager.AddPathSegment(new GridPosRPG(5, 5), Direction.East);

        Assert.NotNull(seg);
        Assert.Equal(1, entMgr.PathSegmentCount);
        Assert.Same(pm, sim.PathNodeManager);
    }

    [Fact]
    public void SimulationTicker_RemovePathSegment_DelegatesToPathManager()
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
        var classReg = new ClassRegistry();
        var ae = new AutoEquipSystem(bus, classReg, entMgr);
        var dr = new DungeonResolver(new DungeonRegistry(), classReg, bus, entMgr);
        var fc = new FusionCalculator(classReg, new ItemRegistry(), rm, bus, entMgr);
        var aa = new AppearanceApplier(bus, entMgr);
        var pgm = new PathGateManager(entMgr, tileMgr, bus, cmdBus, ts, rm, rsMgr);
        pt.SetPathGateManager(pgm);
        var mm = new StructureManager(entMgr, pgm, pm, vs, rm, new ItemRegistry(), new RecipeRegistry(), gl, bus, cmdBus, ts, rsMgr);
        var wsMgr = new WorldStateManager(rsMgr, entMgr, rm, bus);
        var dm = new DungeonManager(entMgr, pgm, vs, rm, bus);
        var sim = new SimulationTicker(bus, cmdBus, pt, vs, ts, entMgr, tileMgr, pm, pgm, rm, gl, mm, rsMgr, wsMgr, aa, dr, dm);

        sim.PathNodeManager.AddPathSegment(new GridPosRPG(5, 5), Direction.East);
        Assert.Equal(1, entMgr.PathSegmentCount);

        sim.PathNodeManager.RemovePathSegment(new GridPosRPG(5, 5));
        Assert.Equal(0, entMgr.PathSegmentCount);
    }

    // â”€â”€ RotatePathSegment â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    public void RotatePathSegment_UpdatesFacingAndRelinks()
    {
        EntityBase.ResetIdCounter();
        var (pathMgr, entMgr, _, _, _, _) = CreatePathManager();

        // Place three segments in a line facing East: (5,5) â†’ (6,5) â†’ (7,5)
        var seg1 = pathMgr.AddPathSegment(new GridPosRPG(5, 5), Direction.East, 1);
        var seg2 = pathMgr.AddPathSegment(new GridPosRPG(6, 5), Direction.East, 1);
        var seg3 = pathMgr.AddPathSegment(new GridPosRPG(7, 5), Direction.East, 1);

        Assert.NotNull(seg1);
        Assert.NotNull(seg2);
        Assert.NotNull(seg3);

        // Verify initial linking: seg1 â†’ seg2 â†’ seg3
        Assert.Equal(seg2!.Id, seg1!.NextSegmentId);
        Assert.Equal(seg3!.Id, seg2.NextSegmentId);

        // Rotate the middle segment (6,5) â€” East â†’ South
        pathMgr.RotatePathSegment(new GridPosRPG(6, 5));

        Assert.Equal(Direction.South, seg2.Facing);
        // seg2 now outputs to (6,4) instead of (7,5), so it should unlink from seg3
        Assert.Null(seg2.NextSegmentId);
        // seg1 still outputs to (6,5) where seg2 is, so seg1 â†’ seg2 link should remain
        Assert.Equal(seg2.Id, seg1.NextSegmentId);
        // seg3 should no longer have seg2 as prev
        Assert.Null(seg3.PrevSegmentId);
    }

    [Fact]
    public void RotatePathSegment_PublishesEntityRotatedEvent()
    {
        EntityBase.ResetIdCounter();
        var (pathMgr, _, _, _, _, bus) = CreatePathManager();

        pathMgr.AddPathSegment(new GridPosRPG(5, 5), Direction.East, 1);

        EntityRotatedEvent? received = null;
        bus.Subscribe<EntityRotatedEvent>(e => received = e);

        pathMgr.RotatePathSegment(new GridPosRPG(5, 5));

        Assert.NotNull(received);
        Assert.Equal("PathSegment", received!.Value.EntityType);
        Assert.Equal(new GridPosRPG(5, 5), received.Value.Position);
        Assert.Equal(Direction.South, received.Value.NewFacing);
    }

    [Fact]
    public void RotatePathSegment_EvictsVillagersAndPublishesPoolEvent()
    {
        // Bug D regression: when a path segment is rotated with a villager on it,
        // Core must (a) remove the villager from the system, (b) return inventory,
        // and (c) publish VillagerReturnedToPoolEvent so presentation can destroy
        // the orphaned VillagerMb. Without this contract, the visual lingers as a
        // ghost while the spawner respawns a fresh visual = duplicate villagers.
        EntityBase.ResetIdCounter();
        var (pathMgr, _, _, villagerSystem, pathTraffic, bus) = CreatePathManager();
        var pos = new GridPosRPG(5, 5);

        var seg = pathMgr.AddPathSegment(pos, Direction.East, 1);
        Assert.NotNull(seg);

        var villager = new VillagerLogic(EntityId.Next());
        villager.Inventory.Add(new ItemInstance { ProtoId = "wood", Quantity = 2 });
        villagerSystem.AddVillager(villager);
        pathTraffic.PlaceVillagerOnPath(villager, pos);
        Assert.Equal(VillagerState.Travelling, villager.State);

        VillagerReturnedToPoolEvent? evictEvent = null;
        bus.Subscribe<VillagerReturnedToPoolEvent>(e => evictEvent = e);

        pathMgr.RotatePathSegment(pos);

        Assert.NotNull(evictEvent);
        Assert.Equal(villager.Id, evictEvent!.Value.VillagerId);
        Assert.Empty(villagerSystem.Villagers);
        Assert.Equal(Direction.South, seg!.Facing);
    }

    [Fact]
    public void RotatePathSegment_NoSegment_DoesNothing()
    {
        EntityBase.ResetIdCounter();
        var (pathMgr, _, _, _, _, bus) = CreatePathManager();

        EntityRotatedEvent? received = null;
        bus.Subscribe<EntityRotatedEvent>(e => received = e);

        // Rotate at empty position â€” should not throw or publish
        pathMgr.RotatePathSegment(new GridPosRPG(99, 99));

        Assert.Null(received);
    }
}
