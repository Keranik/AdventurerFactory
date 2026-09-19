using ForgeFlow.Core;
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
/// Tests for the PathGate entity â€” the core entry/exit system for
/// directing villagers off paths into buildings and back.
/// </summary>
public class PathGateTests
{
    private static GameBootstrapper CreateBootstrapper()
    {
        var bootstrapper = new GameBootstrapper();
        bootstrapper.Bootstrap();
        return bootstrapper;
    }

    private static (GameBootstrapper boot, SimulationTicker sim) CreateNewGame()
    {
        EntityBase.ResetIdCounter();
        var boot = CreateBootstrapper();
        var settings = new NewGameSettings
        {
            GameName = "PathGateTest",
            Difficulty = Difficulty.Normal,
            Seed = 42,
            GuildName = "TestGuild"
        };
        boot.ApplyNewGameSettings(settings);
        return (boot, boot.Services.Get<SimulationTicker>());
    }

    // â”€â”€â”€ PathGateLogic Basic Tests â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    public void PathGateLogic_DefaultMode_IsEntrance()
    {
        EntityBase.ResetIdCounter();
        var gate = new PathGateLogic(EntityId.Next());
        Assert.Equal(PathGateMode.Entrance, gate.Mode);
        Assert.True(gate.IsEntrance);
        Assert.False(gate.IsExit);
    }

    [Fact]
    public void PathGateLogic_FacingPosition_IsCorrect()
    {
        EntityBase.ResetIdCounter();
        var gate = new PathGateLogic(EntityId.Next())
        {
            Position = new GridPosRPG(5, 5),
            Facing = Direction.East
        };
        Assert.Equal(new GridPosRPG(6, 5), gate.FacingPosition);
    }

    [Fact]
    public void PathGateLogic_LinkToStructure_FacingToward_SetsEntrance()
    {
        EntityBase.ResetIdCounter();
        var gate = new PathGateLogic(EntityId.Next())
        {
            Position = new GridPosRPG(3, 3),
            Facing = Direction.East
        };

        // Structure is at (4, 3) â€” gate faces toward it
        gate.LinkToStructure(100, new GridPosRPG(4, 3));

        Assert.Equal(PathGateMode.Entrance, gate.Mode);
        Assert.True(gate.IsEntrance);
        Assert.Equal((ulong)100, gate.LinkedStructureId);
    }

    [Fact]
    public void PathGateLogic_LinkToStructure_FacingAway_SetsExit()
    {
        EntityBase.ResetIdCounter();
        var gate = new PathGateLogic(EntityId.Next())
        {
            Position = new GridPosRPG(3, 3),
            Facing = Direction.West // Facing away from the building at (4, 3)
        };

        // Structure is at (4, 3) â€” gate faces away from it
        gate.LinkToStructure(100, new GridPosRPG(4, 3));

        Assert.Equal(PathGateMode.Exit, gate.Mode);
        Assert.True(gate.IsExit);
    }

    [Fact]
    public void PathGateLogic_Rotate_ChangesDirection()
    {
        EntityBase.ResetIdCounter();
        var gate = new PathGateLogic(EntityId.Next())
        {
            Position = new GridPosRPG(3, 3),
            Facing = Direction.North
        };

        gate.Rotate();
        Assert.Equal(Direction.East, gate.Facing);

        gate.Rotate();
        Assert.Equal(Direction.South, gate.Facing);

        gate.Rotate();
        Assert.Equal(Direction.West, gate.Facing);

        gate.Rotate();
        Assert.Equal(Direction.North, gate.Facing);
    }

    [Fact]
    public void PathGateLogic_Rotate_RecomputesMode()
    {
        EntityBase.ResetIdCounter();
        var structurePos = new GridPosRPG(4, 3);
        var gate = new PathGateLogic(EntityId.Next())
        {
            Position = new GridPosRPG(3, 3),
            Facing = Direction.East // Facing toward structure
        };
        gate.LinkToStructure(100, structurePos);
        Assert.True(gate.IsEntrance);

        // Rotate 180 degrees â€” now facing away
        gate.Rotate(structurePos);
        gate.Rotate(structurePos);
        Assert.True(gate.IsExit);
    }

    [Fact]
    public void PathGateLogic_IsLinked_FalseByDefault()
    {
        EntityBase.ResetIdCounter();
        var gate = new PathGateLogic(EntityId.Next());
        Assert.False(gate.IsLinked);
    }

    [Fact]
    public void PathGateLogic_IsLinked_TrueAfterLink()
    {
        EntityBase.ResetIdCounter();
        var gate = new PathGateLogic(EntityId.Next())
        {
            Position = new GridPosRPG(3, 3),
            Facing = Direction.East
        };
        gate.LinkToStructure(100, new GridPosRPG(4, 3));
        Assert.True(gate.IsLinked);
    }

    [Fact]
    public void PathGateLogic_Tick_DoesNotThrow()
    {
        EntityBase.ResetIdCounter();
        var gate = new PathGateLogic(EntityId.Next());
        gate.Tick(1.0f); // Should not throw â€” passive entity
    }

    [Fact]
    public void PathGateLogic_InitializeFromProto_SetsFields()
    {
        EntityBase.ResetIdCounter();
        var proto = new PathGateProto
        {
            Id = "pathgate_test",
            DisplayName = "Test Gate",
            GateRange = 2
        };

        var gate = new PathGateLogic(EntityId.Next());
        gate.InitializeFromProto(proto);

        Assert.Equal("pathgate_test", gate.ProtoId);
        Assert.Equal(2, gate.GateRange);
    }

    // â”€â”€â”€ PathGateProto Tests â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    public void PathGateProto_DefaultGateRange_IsOne()
    {
        var proto = new PathGateProto();
        Assert.Equal(1, proto.GateRange);
    }

    // â”€â”€â”€ ProtoRegistry PathGate Tests â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    public void ProtoRegistry_RegisterPathGate_CanRetrieve()
    {
        var registry = new ProtoRegistry();
        var proto = new PathGateProto { Id = "gate_test", GateRange = 2 };
        registry.RegisterPathGate(proto);

        var retrieved = registry.GetPathGate("gate_test");
        Assert.NotNull(retrieved);
        Assert.Equal(2, retrieved!.GateRange);
    }

    [Fact]
    public void ProtoRegistry_RegisterDefaults_CreatesPathGateProto()
    {
        var registry = new ProtoRegistry();
        registry.RegisterDefaults();

        Assert.True(registry.PathGateCount > 0);
        var gate = registry.GetPathGate("pathgate_basic");
        Assert.NotNull(gate);
        Assert.Equal("Path Gate", gate!.DisplayName);
    }

    // â”€â”€â”€ ProtoFactory PathGate Tests â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    public void ProtoFactory_CreatePathGate_ReturnsLogicInstance()
    {
        EntityBase.ResetIdCounter();
        var registry = new ProtoRegistry();
        registry.RegisterDefaults();
        var eventBus = new EventBus();
        var factory = new ProtoFactory(registry, eventBus);

        var gate = factory.CreatePathGate("pathgate_basic");
        Assert.NotNull(gate);
        Assert.Equal("pathgate_basic", gate!.ProtoId);
        Assert.Equal(1, gate.GateRange);
    }

    [Fact]
    public void ProtoFactory_CreatePathGate_UnknownId_ReturnsNull()
    {
        var registry = new ProtoRegistry();
        registry.RegisterDefaults();
        var eventBus = new EventBus();
        var factory = new ProtoFactory(registry, eventBus);

        var gate = factory.CreatePathGate("nonexistent_gate");
        Assert.Null(gate);
    }

    // â”€â”€â”€ SimulationTicker PathGate Tests â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    public void SimulationTicker_AddPathGate_AddsToCollections()
    {
        var (boot, sim) = CreateNewGame();

        // Place a structure first so the gate can link to it
        var spawner = new VillageSpawnerLogic(EntityId.Next());

        sim.EntityManager.AddStructure(spawner, new GridPosRPG(5, 5));

        // Place a PathGate adjacent, facing away = Exit
        var gate = sim.PathGateManager.AddPathGate(new GridPosRPG(6, 5), Direction.East);

        Assert.NotNull(gate);
        Assert.True(sim.PathGateManager.PathGates.ContainsKey(gate!.Id));
        Assert.True(sim.TileManager.GetPathGateIdAt(new GridPosRPG(6, 5)).HasValue);
    }

    [Fact]
    public void SimulationTicker_AddPathGate_AutoLinksToAdjacentStructure()
    {
        var (boot, sim) = CreateNewGame();

        var spawner = new VillageSpawnerLogic(EntityId.Next());

        sim.EntityManager.AddStructure(spawner, new GridPosRPG(5, 5));

        // Gate at (6, 5) facing east â€” structure is to the west (behind), so it's Exit
        var gate = sim.PathGateManager.AddPathGate(new GridPosRPG(6, 5), Direction.East);

        Assert.NotNull(gate);
        Assert.True(gate!.IsLinked);
        Assert.Equal(spawner.Id, gate.LinkedStructureId);
        Assert.True(gate.IsExit);
    }

    [Fact]
    public void SimulationTicker_AddPathGate_FacingTowardStructure_IsEntrance()
    {
        var (boot, sim) = CreateNewGame();

        var spawner = new VillageSpawnerLogic(EntityId.Next());

        sim.EntityManager.AddStructure(spawner, new GridPosRPG(5, 5));

        // Gate at (6, 5) facing west â€” structure is to the west, so it's Entrance
        var gate = sim.PathGateManager.AddPathGate(new GridPosRPG(6, 5), Direction.West);

        Assert.NotNull(gate);
        Assert.True(gate!.IsEntrance);
    }

    [Fact]
    public void SimulationTicker_RemovePathGate_RemovesFromCollections()
    {
        var (boot, sim) = CreateNewGame();

        var spawner = new VillageSpawnerLogic(EntityId.Next());

        sim.EntityManager.AddStructure(spawner, new GridPosRPG(5, 5));

        var gate = sim.PathGateManager.AddPathGate(new GridPosRPG(6, 5), Direction.East);
        Assert.NotNull(gate);

        sim.PathGateManager.RemovePathGate(new GridPosRPG(6, 5));

        Assert.False(sim.PathGateManager.PathGates.ContainsKey(gate!.Id));
        Assert.False(sim.TileManager.GetPathGateIdAt(new GridPosRPG(6, 5)).HasValue);
    }

    [Fact]
    public void SimulationTicker_CountPathGates_ReturnsCorrectCount()
    {
        var (boot, sim) = CreateNewGame();

        Assert.Equal(0, sim.PathGateManager.CountPathGates());

        sim.PathGateManager.AddPathGate(new GridPosRPG(1, 1), Direction.North);
        sim.PathGateManager.AddPathGate(new GridPosRPG(2, 2), Direction.South);

        Assert.Equal(2, sim.PathGateManager.CountPathGates());
    }

    [Fact]
    public void SimulationTicker_AddPathGate_PublishesEvent()
    {
        var (boot, sim) = CreateNewGame();
        PathGatePlacedEvent? captured = null;
        boot.Services.Get<EventBus>().Subscribe<PathGatePlacedEvent>(e => captured = e);

        var spawner = new VillageSpawnerLogic(EntityId.Next());

        sim.EntityManager.AddStructure(spawner, new GridPosRPG(5, 5));

        sim.PathGateManager.AddPathGate(new GridPosRPG(6, 5), Direction.East);

        Assert.NotNull(captured);
        Assert.Equal(new GridPosRPG(6, 5), captured!.Value.Position);
    }

    [Fact]
    public void SimulationTicker_PrestigeReset_ClearsPathGates()
    {
        var (boot, sim) = CreateNewGame();

        sim.PathGateManager.AddPathGate(new GridPosRPG(1, 1), Direction.North);
        Assert.Equal(1, sim.PathGateManager.CountPathGates());

        // Need tier 3 for prestige
        sim.ResearchManager.UnlockTier(2);
        sim.ResearchManager.UnlockTier(3);
        sim.WorldStateManager.PrestigeReset();

        Assert.Equal(0, sim.PathGateManager.CountPathGates());
    }

    // â”€â”€â”€ PathTrafficSystem PathGate Integration â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    public void PathTrafficSystem_VillagerDivertedByPathGateEntrance()
    {
        EntityBase.ResetIdCounter();
        var (boot, sim) = CreateNewGame();

        // Place a gathering structure
        var forestry = new ForestryRecipeEntity(EntityId.Next());
        forestry.InitializeFromProto(new ForestryRecipeEntityProto
        {
            Id = "forestry_basic",
            TargetResourceId = "sticks"
        });
        forestry.Initialize(null);
        sim.EntityManager.AddStructure(forestry, new GridPosRPG(5, 5));

        // Place path segments: (0,5) â†’ (1,5) â†’ (2,5) â†’ (3,5) â†’ (4,5)
        for (int x = 0; x <= 4; x++)
        {
            sim.PathNodeManager.AddPathSegment(new GridPosRPG(x, 5), Direction.East);
        }

        // Place PathGate entrance at (4, 4) facing North toward building at (5,5)
        // Actually let's put it at (5,4) facing north - adjacent to the last path segment (4,5)
        // Wait â€” the gate needs to be adjacent to a path segment for the villager to detect it.
        // Gate at (4,4) is adjacent to path segment at (4,5). Gate faces north toward (4,5) -> that doesn't have the structure.
        // Let me think about this differently:
        // Path segment at (4,5). Structure at (5,5). Gate at (5,4) facing north toward (5,5).
        // But (5,4) is not adjacent to (4,5) â€” it's diagonal. Not found in cardinal check.
        
        // Better layout: structure at (5,5), path at (4,5), gate at (5,5-1=4) no...
        // Path at (4,5), gate needs to be cardinal neighbor of (4,5) and face toward structure at (5,5).
        // Gate at (4,4): faces North toward (4,5) which is the path, not the structure.
        // Gate at (5,5): that's where the structure is.
        // Gate at (4,6): faces South toward (4,5) â€” structure is at (5,5), gate faces south away from structure. That's exit.
        
        // Simpler: structure at (5, 5), path at (4, 5), gate at (5, 4) â€” not cardinal to path.
        // Let me rearrange: structure at (3, 6), path from (0,5) to (4,5), gate at (3,5) facing North.
        // Gate at (3,5) is ON a path segment. That's not ideal â€” gates should be separate.
        
        // Actually looking at the check: the system checks cardinal neighbors of the PATH SEGMENT
        // for PathGates. So if path is at (4,5), gate should be at (3,5), (5,5), (4,4), or (4,6).
        // Gate at (4,6) facing North â€” FacingPosition is (4,7). Structure at (4,7)? No, too far.
        // 
        // Let me use: structure at (4,6), path at (4,5), gate at (4,6)... no, gate would overlap structure.
        //
        // OK, simplest correct layout:
        // Structure at (3,6). Path at (3,5). Gate at (3,6)... overlaps.
        // 
        // The gate is placed ON the EDGE between path and building. But it's its own entity at
        // its own grid position. It cannot overlap with the structure.
        //
        // Correct approach: Structure at (3,7), gate at (3,6) facing North toward (3,7).
        // Path segment at (3,5). Gate at (3,6) is a cardinal neighbor of (3,5)? 
        // (3,6) is adjacent to (3,5) â€” yes, North neighbor. âœ“
        // Gate at (3,6) faces North. FacingPos = (3,7) = structure pos. âœ“ â†’ Entrance.

        // Let me redo the layout cleanly
        sim.EntityManager.Clear();

        // Structure at (3, 7)
        sim.EntityManager.AddStructure(forestry, new GridPosRPG(3, 7));

        // Path: (0,5) â†’ (1,5) â†’ (2,5) â†’ (3,5)
        for (int x = 0; x <= 3; x++)
        {
            sim.PathNodeManager.AddPathSegment(new GridPosRPG(x, 5), Direction.East);
        }

        // Gate at (3,6) facing North toward structure at (3,7)
        var gate = sim.PathGateManager.AddPathGate(new GridPosRPG(3, 6), Direction.North);
        Assert.NotNull(gate);
        Assert.True(gate!.IsEntrance);
        Assert.Equal(forestry.Id, gate.LinkedStructureId);

        // Place a villager on first path segment
        var villager = new VillagerLogic(EntityId.Next())
        {
            Name = "TestVillager",
            MovementSpeed = 100f, // Very fast for testing
            State = VillagerState.Travelling,
            Position = new GridPosRPG(0, 5)
        };
        sim.VillagerSystem.AddVillager(villager);
        sim.PathTraffic.PlaceVillagerOnPath(villager, new GridPosRPG(0, 5));

        // Tick enough to move villager to end of path
        for (int i = 0; i < 300; i++)
        {
            sim.Update(SimulationTicker.FixedTimeStep);
        }

        // The villager should have been diverted into the building
        // (state changed from Travelling to EnteringBuilding or Working)
        Assert.NotEqual(VillagerState.Travelling, villager.State);
    }

    [Fact]
    public void PathTrafficSystem_NoPathGate_VillagerContinuesOnPath()
    {
        EntityBase.ResetIdCounter();
        var (boot, sim) = CreateNewGame();

        // Path: (0,5) â†’ (1,5) â†’ (2,5) â†’ (3,5)
        for (int x = 0; x <= 3; x++)
        {
            sim.PathNodeManager.AddPathSegment(new GridPosRPG(x, 5), Direction.East);
        }

        // No PathGate placed â€” villager should walk to end and stop
        var villager = new VillagerLogic(EntityId.Next())
        {
            Name = "TestVillager",
            MovementSpeed = 100f,
            State = VillagerState.Travelling,
            Position = new GridPosRPG(0, 5)
        };
        sim.VillagerSystem.AddVillager(villager);
        sim.PathTraffic.PlaceVillagerOnPath(villager, new GridPosRPG(0, 5));

        for (int i = 0; i < 300; i++)
        {
            sim.Update(SimulationTicker.FixedTimeStep);
        }

        // Without a PathGate, villager reaches end and goes idle
        Assert.True(villager.State == VillagerState.Idle || villager.State == VillagerState.Working);
        Assert.Null(villager.CurrentPathSegmentId);
    }

    [Fact]
    public void PathTrafficSystem_ExitGate_DoesNotDivertVillager()
    {
        EntityBase.ResetIdCounter();
        var (boot, sim) = CreateNewGame();

        var forestry = new ForestryRecipeEntity(EntityId.Next());
        forestry.InitializeFromProto(new ForestryRecipeEntityProto
        {
            Id = "forestry_basic",
            TargetResourceId = "sticks"
        });
        forestry.Initialize(null);
        sim.EntityManager.AddStructure(forestry, new GridPosRPG(3, 7));

        for (int x = 0; x <= 3; x++)
        {
            sim.PathNodeManager.AddPathSegment(new GridPosRPG(x, 5), Direction.East);
        }

        // Gate at (3,6) facing SOUTH (away from structure at (3,7)) = Exit
        var gate = sim.PathGateManager.AddPathGate(new GridPosRPG(3, 6), Direction.South);
        Assert.NotNull(gate);
        Assert.True(gate!.IsExit);

        var villager = new VillagerLogic(EntityId.Next())
        {
            Name = "TestVillager",
            MovementSpeed = 100f,
            State = VillagerState.Travelling,
            Position = new GridPosRPG(0, 5)
        };
        sim.VillagerSystem.AddVillager(villager);
        sim.PathTraffic.PlaceVillagerOnPath(villager, new GridPosRPG(0, 5));

        for (int i = 0; i < 300; i++)
        {
            sim.Update(SimulationTicker.FixedTimeStep);
        }

        // Exit gate should NOT divert the villager â€” they continue to end of path
        Assert.Null(villager.CurrentPathSegmentId);
    }

    // â”€â”€â”€ ExitVillagerViaGate Tests â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    public void ExitVillagerViaGate_PlacesVillagerOnAdjacentPath()
    {
        EntityBase.ResetIdCounter();
        var tm = new TrafficManager(maxOccupantsPerSegment: 10);
        var bus = new EventBus();
        var ItemManager = new ItemManager(bus);
        var vs = new VillagerSystem(bus, ItemManager);
        var terrain = new TerrainGrid(32, 32);
        terrain.GenerateDefault();
        var tileMgr = new TileManager(terrain);
        var entMgr = new EntityManager(tileMgr, vs);
        var pathTraffic = new PathTrafficSystem(tm, entMgr, tileMgr, bus);
        var protoReg = new ProtoRegistry();
        protoReg.RegisterDefaults();
        var pf = new ProtoFactory(protoReg, bus);
        var tutorialSystem = new TutorialSystem(bus, pf);
        var pathGateManager = new PathGateManager(entMgr, tileMgr, bus, new Core.Commands.CommandBus(), tutorialSystem, ItemManager, new ResearchManager(bus));
        pathTraffic.SetPathGateManager(pathGateManager);

        // Path segment at (4, 5)
        var seg = new PathSegmentLogic(EntityId.Next())
        {
            Position = new GridPosRPG(4, 5),
            Facing = Direction.East
        };
        entMgr.AddPathSegment(seg);

        // Structure at (3, 7)
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

        // But we need a path segment adjacent to the gate
        var exitSeg = new PathSegmentLogic(EntityId.Next())
        {
            Position = new GridPosRPG(3, 5),
            Facing = Direction.East
        };
        entMgr.AddPathSegment(exitSeg);

        // Villager leaving the building
        var villager = new VillagerLogic(EntityId.Next())
        {
            Name = "Exiting",
            State = VillagerState.LeavingBuilding,
            Position = new GridPosRPG(3, 7)
        };

        bool result = pathGateManager.ExitVillagerViaGate(villager, new EntityId(structureId));
        Assert.Equal(VillagerState.Travelling, villager.State);
        Assert.NotNull(villager.CurrentPathSegmentId);
    }

    [Fact]
    public void ExitVillagerViaGate_NoExitGate_ReturnsFalse()
    {
        EntityBase.ResetIdCounter();
        var tm = new TrafficManager();
        var bus = new EventBus();
        var ItemManager = new ItemManager(bus);
        var vs = new VillagerSystem(bus, ItemManager);
        var terrain = new TerrainGrid(32, 32);
        terrain.GenerateDefault();
        var tileMgr = new TileManager(terrain);
        var entMgr = new EntityManager(tileMgr, vs);
        var pathTraffic = new PathTrafficSystem(tm, entMgr, tileMgr, bus);
        var protoReg = new ProtoRegistry();
        protoReg.RegisterDefaults();
        var pf = new ProtoFactory(protoReg, bus);
        var tutorialSystem = new TutorialSystem(bus, pf);
        var pathGateManager = new PathGateManager(entMgr, tileMgr, bus, new Core.Commands.CommandBus(), tutorialSystem, ItemManager, new ResearchManager(bus));
        pathTraffic.SetPathGateManager(pathGateManager);

        var villager = new VillagerLogic(EntityId.Next())
        {
            Name = "Stuck",
            State = VillagerState.LeavingBuilding
        };

        bool result = pathGateManager.ExitVillagerViaGate(villager, new EntityId(999));

        Assert.False(result);
    }

    // â”€â”€â”€ Tutorial Integration Tests â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    public void Tutorial_PlacePathGateExit_AdvancesCondition()
    {
        var (boot, sim) = CreateNewGame();
        var tutorial = sim.TutorialSystem;

        // Advance through first tutorial (place spawner)
        tutorial.AdvanceCondition(TutorialConditionType.PlaceSpecificStructure, "Spawner");

        // Now active mission should be p1_02_exit_gate
        Assert.NotNull(tutorial.ActiveMission);
        Assert.Equal("p1_02_exit_gate", tutorial.ActiveMission!.ProtoId);

        // Place a PathGate exit
        bool completed = tutorial.AdvanceCondition(TutorialConditionType.PlacePathGateExit);
        Assert.True(completed);
    }

    [Fact]
    public void Tutorial_PlacePathGateEntrance_AdvancesCondition()
    {
        var (boot, sim) = CreateNewGame();
        var tutorial = sim.TutorialSystem;

        // Advance through first 2 tutorials (Spawner -> Exit)
        tutorial.AdvanceCondition(TutorialConditionType.PlaceSpecificStructure, "Spawner");
        tutorial.AdvanceCondition(TutorialConditionType.PlacePathGateExit);

        // Now active mission should be p1_03_place_forestry
        Assert.NotNull(tutorial.ActiveMission);
        Assert.Equal("p1_03_place_forestry", tutorial.ActiveMission!.ProtoId);

        // Complete structure placement
        tutorial.AdvanceCondition(TutorialConditionType.PlaceSpecificStructure, "Forestry");

        // Place entrance PathGate (now step 4)
        bool completed = tutorial.AdvanceCondition(TutorialConditionType.PlacePathGateEntrance);
        Assert.True(completed);
    }

    [Fact]
    public void Tutorial_InnStep_RequiresEntranceAndExitPathGates()
    {
        var (boot, sim) = CreateNewGame();
        var tutorial = sim.TutorialSystem;

        // Fast-forward through tutorials 1-13 (new 15-step sequence)
        tutorial.AdvanceCondition(TutorialConditionType.PlaceSpecificStructure, "Spawner");  // p1_01
        tutorial.AdvanceCondition(TutorialConditionType.PlacePathGateExit);                // p1_02
        tutorial.AdvanceCondition(TutorialConditionType.PlaceSpecificStructure, "Forestry"); // p1_03
        tutorial.AdvanceCondition(TutorialConditionType.PlacePathGateEntrance);            // p1_04
        tutorial.AdvanceCondition(TutorialConditionType.BuildPath, amount: 3);             // p1_05
        tutorial.AdvanceCondition(TutorialConditionType.WatchVillagerGather, "sticks");    // p1_06
        tutorial.AdvanceCondition(TutorialConditionType.PlaceSpecificStructure, "Stockpile"); // p1_07
        tutorial.AdvanceCondition(TutorialConditionType.SelectStockpileProduct, "sticks"); // p1_08
        tutorial.AdvanceCondition(TutorialConditionType.PlacePathGateEntrance);            // p1_09
        tutorial.AdvanceCondition(TutorialConditionType.PlacePathGateExit);                // p1_10
        tutorial.AdvanceCondition(TutorialConditionType.BuildPath, amount: 2);             // p1_11
        tutorial.AdvanceCondition(TutorialConditionType.VillagerDropOffItem, "sticks");    // p1_12
        tutorial.AdvanceCondition(TutorialConditionType.PlacePathGateExit);                // p1_13
        tutorial.AdvanceCondition(TutorialConditionType.BuildPath, amount: 3);             // p1_14

        // Now at p1_15_place_inn
        Assert.NotNull(tutorial.ActiveMission);
        Assert.Equal("p1_15_place_inn", tutorial.ActiveMission!.ProtoId);

        // Should require: Inn placement, PathGate entrance, PathGate exit (3 conditions)
        Assert.Equal(3, tutorial.ActiveMission.Conditions.Count);

        tutorial.AdvanceCondition(TutorialConditionType.PlaceSpecificStructure, "Inn");
        Assert.False(tutorial.ActiveMission.AllConditionsMet());

        tutorial.AdvanceCondition(TutorialConditionType.PlacePathGateEntrance);
        Assert.False(tutorial.ActiveMission.AllConditionsMet());

        bool completed = tutorial.AdvanceCondition(TutorialConditionType.PlacePathGateExit);
        Assert.True(completed);
    }

    [Fact]
    public void Tutorial_TotalMissionCount_Is29()
    {
        var (boot, sim) = CreateNewGame();
        Assert.Equal(29, sim.TutorialSystem.TotalCount);
    }

    // â”€â”€â”€ PathSegmentLogic Is Untouched â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    public void PathSegmentLogic_HasNoPathGateReferences()
    {
        // Verify PathSegmentLogic remains clean â€” no PathGate fields or methods
        var seg = new PathSegmentLogic(EntityId.Next());
        Assert.Equal(PathNodeType.Straight, seg.NodeType);
        Assert.Null(seg.NextSegmentId);
        Assert.Null(seg.PrevSegmentId);
        Assert.Null(seg.SplitTargetId);
        // PathGate is completely separate from PathSegmentLogic
    }

    // â”€â”€â”€ StructureType Enum â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    public void PathGateLogic_GetCategoryName_IsPathGate()
    {
        var gate = new PathGateLogic(EntityId.Next());
        Assert.Equal("PathGate", gate.GetCategoryName());
    }

    // â”€â”€â”€ Embedded JSON Tutorial Loading â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    public void ProtoRegistry_EmbeddedTutorials_ContainPathGateSteps()
    {
        var registry = new ProtoRegistry();
        registry.LoadAllFromEmbeddedResources();

        var exitTutorial = registry.GetTutorial("p1_02_exit_gate");
        Assert.NotNull(exitTutorial);
        Assert.Equal("Place an Exit Gate", exitTutorial!.DisplayName);
        Assert.Single(exitTutorial.Conditions);
        Assert.Equal(TutorialConditionType.PlacePathGateExit, exitTutorial.Conditions[0].Type);
    }

    // â”€â”€â”€ Spawner Gate Requirement Tests â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    public void Spawner_DoesNotSpawn_WithoutExitGateAndPath()
    {
        EntityBase.ResetIdCounter();
        var (boot, sim) = CreateNewGame();

        // Place spawner with no exit gate and no path
        var spawner = new VillageSpawnerLogic(EntityId.Next());

        spawner.SpawnInterval = 0.1f;
        sim.EntityManager.AddStructure(spawner, new GridPosRPG(0, 0));

        for (int i = 0; i < 60; i++)
        {
            sim.Update(SimulationTicker.FixedTimeStep);
        }

        Assert.Empty(sim.VillagerSystem.Villagers);
    }

    [Fact]
    public void Spawner_SpawnsWaitingVillager_WithExitGateButNoPath()
    {
        EntityBase.ResetIdCounter();
        var (boot, sim) = CreateNewGame();

        var spawner = new VillageSpawnerLogic(EntityId.Next());

        spawner.SpawnInterval = 0.1f;
        sim.EntityManager.AddStructure(spawner, new GridPosRPG(0, 0));

        // Exit gate facing east, but no path at (2, 0) where gate faces
        sim.PathGateManager.AddPathGate(new GridPosRPG(1, 0), Direction.East);

        for (int i = 0; i < 60; i++)
        {
            sim.Update(SimulationTicker.FixedTimeStep);
        }

        // Villager spawns and waits at the gate's facing position
        Assert.NotEmpty(sim.VillagerSystem.Villagers);
        var villager = sim.VillagerSystem.Villagers[0];
        Assert.Equal(VillagerState.Idle, villager.State);
        Assert.Equal(new GridPosRPG(2, 0), villager.Position);
        Assert.Null(villager.CurrentPathSegmentId);
    }

    [Fact]
    public void Spawner_Spawns_WithExitGateAndPath()
    {
        EntityBase.ResetIdCounter();
        var (boot, sim) = CreateNewGame();

        var spawner = new VillageSpawnerLogic(EntityId.Next());

        spawner.SpawnInterval = 0.1f;
        sim.EntityManager.AddStructure(spawner, new GridPosRPG(0, 0));

        // Path at the exit gate's facing position
        sim.PathNodeManager.AddPathSegment(new GridPosRPG(2, 0), Direction.East);
        sim.PathGateManager.AddPathGate(new GridPosRPG(1, 0), Direction.East);

        for (int i = 0; i < 60; i++)
        {
            sim.Update(SimulationTicker.FixedTimeStep);
        }

        Assert.NotEmpty(sim.VillagerSystem.Villagers);
    }

    // â”€â”€â”€ Exclusive Tile Occupancy Tests â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    public void Villager_CannotShareTile_WaitsOnPreviousTile()
    {
        EntityBase.ResetIdCounter();
        var (boot, sim) = CreateNewGame();

        // Path: (0,0) â†’ (1,0) â†’ (2,0)
        sim.PathNodeManager.AddPathSegment(new GridPosRPG(0, 0), Direction.East);
        sim.PathNodeManager.AddPathSegment(new GridPosRPG(1, 0), Direction.East);
        sim.PathNodeManager.AddPathSegment(new GridPosRPG(2, 0), Direction.East);

        var v1 = new VillagerLogic(EntityId.Next())
        {
            Name = "Front",
            MovementSpeed = 0.5f,
            State = VillagerState.Travelling
        };
        var v2 = new VillagerLogic(EntityId.Next())
        {
            Name = "Back",
            MovementSpeed = 100f, // Very fast
            State = VillagerState.Travelling
        };

        sim.VillagerSystem.AddVillager(v1);
        sim.VillagerSystem.AddVillager(v2);

        // Place front villager on (1,0) and back on (0,0)
        sim.PathTraffic.PlaceVillagerOnPath(v1, new GridPosRPG(1, 0));
        sim.PathTraffic.PlaceVillagerOnPath(v2, new GridPosRPG(0, 0));

        // Tick a few times â€” v2 is fast but v1 blocks (1,0)
        for (int i = 0; i < 5; i++)
        {
            sim.Update(SimulationTicker.FixedTimeStep);
        }

        // They should NOT be on the same tile
        Assert.NotEqual(v1.Position, v2.Position);
    }

    [Fact]
    public void PlaceVillagerOnPath_FailsIfTileOccupied()
    {
        EntityBase.ResetIdCounter();
        var tm = new TrafficManager();
        var bus = new EventBus();
        var ItemManager = new ItemManager(bus);
        var vsys = new VillagerSystem(bus, ItemManager);
        var terrain = new TerrainGrid(32, 32);
        terrain.GenerateDefault();
        var tileMgr = new TileManager(terrain);
        var entMgr = new EntityManager(tileMgr, vsys);
        var pathTraffic = new PathTrafficSystem(tm, entMgr, tileMgr, bus);

        var seg = new PathSegmentLogic(EntityId.Next())
        {
            Position = new GridPosRPG(0, 0),
            Facing = Direction.East
        };
        entMgr.AddPathSegment(seg);

        var v1 = new VillagerLogic(EntityId.Next()) { Name = "First" };
        var v2 = new VillagerLogic(EntityId.Next()) { Name = "Second" };

        bool placed1 = pathTraffic.PlaceVillagerOnPath(v1, new GridPosRPG(0, 0));
        bool placed2 = pathTraffic.PlaceVillagerOnPath(v2, new GridPosRPG(0, 0));

        Assert.True(placed1);
        Assert.False(placed2);
    }

    // â”€â”€â”€ Path Removal Eviction Tests â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    public void RemovePathSegment_EvictsVillagerToPool()
    {
        EntityBase.ResetIdCounter();
        var (boot, sim) = CreateNewGame();

        sim.PathNodeManager.AddPathSegment(new GridPosRPG(0, 0), Direction.East);
        sim.PathNodeManager.AddPathSegment(new GridPosRPG(1, 0), Direction.East);

        var villager = new VillagerLogic(EntityId.Next())
        {
            Name = "OnPath",
            State = VillagerState.Travelling
        };
        sim.VillagerSystem.AddVillager(villager);
        sim.PathTraffic.PlaceVillagerOnPath(villager, new GridPosRPG(1, 0));

        Assert.Equal(1, sim.VillagerSystem.Count);

        sim.PathNodeManager.RemovePathSegment(new GridPosRPG(1, 0));

        Assert.Equal(0, sim.VillagerSystem.Count);
    }

    [Fact]
    public void RemovePathSegment_ReturnsVillagerInventoryToStocks()
    {
        EntityBase.ResetIdCounter();
        var (boot, sim) = CreateNewGame();

        sim.PathNodeManager.AddPathSegment(new GridPosRPG(0, 0), Direction.East);

        // Record baseline resource stocks
        sim.ItemManager.VirtualStocks.TryGetValue("wood", out var baseWood);
        sim.ItemManager.VirtualStocks.TryGetValue("ore", out var baseOre);

        var villager = new VillagerLogic(EntityId.Next())
        {
            Name = "Carrier",
            State = VillagerState.Travelling
        };
        villager.TryPickUpItem(new ItemInstance { ProtoId = "wood", Quantity = 5 });
        villager.TryPickUpItem(new ItemInstance { ProtoId = "ore", Quantity = 3 });

        sim.VillagerSystem.AddVillager(villager);
        sim.PathTraffic.PlaceVillagerOnPath(villager, new GridPosRPG(0, 0));

        sim.PathNodeManager.RemovePathSegment(new GridPosRPG(0, 0));

        sim.ItemManager.VirtualStocks.TryGetValue("wood", out var wood);
        sim.ItemManager.VirtualStocks.TryGetValue("ore", out var ore);
        Assert.Equal(baseWood + 5, wood);
        Assert.Equal(baseOre + 3, ore);
    }

    [Fact]
    public void RemovePathSegment_DecrementsSpawnerCount()
    {
        EntityBase.ResetIdCounter();
        var (boot, sim) = CreateNewGame();

        var spawner = new VillageSpawnerLogic(EntityId.Next());

        spawner.SpawnInterval = 0.1f;
        spawner.MaxVillagers = 1;
        sim.EntityManager.AddStructure(spawner, new GridPosRPG(0, 0));

        // Set up exit gate + path
        sim.PathNodeManager.AddPathSegment(new GridPosRPG(2, 0), Direction.East);
        sim.PathNodeManager.AddPathSegment(new GridPosRPG(3, 0), Direction.East);
        sim.PathGateManager.AddPathGate(new GridPosRPG(1, 0), Direction.East);

        // Tick until villager spawns
        for (int i = 0; i < 60; i++)
        {
            sim.Update(SimulationTicker.FixedTimeStep);
        }
        Assert.Equal(1, sim.VillagerSystem.Count);

        // Remove the path the villager is on â€” evicts them
        var villager = sim.VillagerSystem.Villagers[0];
        sim.PathNodeManager.RemovePathSegment(villager.Position);

        Assert.Equal(0, sim.VillagerSystem.Count);

        // Spawner should be able to spawn again (count was decremented)
        // Re-add path
        sim.PathNodeManager.AddPathSegment(villager.Position, Direction.East);

        for (int i = 0; i < 60; i++)
        {
            sim.Update(SimulationTicker.FixedTimeStep);
        }
        Assert.Equal(1, sim.VillagerSystem.Count);
    }

    [Fact]
    public void RemovePathSegment_PublishesPathRemovedEvent()
    {
        EntityBase.ResetIdCounter();
        var (boot, sim) = CreateNewGame();

        sim.PathNodeManager.AddPathSegment(new GridPosRPG(5, 5), Direction.East);

        PathRemovedEvent? captured = null;
        boot.Services.Get<EventBus>().Subscribe<PathRemovedEvent>(e => captured = e);

        sim.PathNodeManager.RemovePathSegment(new GridPosRPG(5, 5));

        Assert.NotNull(captured);
        Assert.Equal(new GridPosRPG(5, 5), captured!.Value.Position);
    }

    [Fact]
    public void RemovePathSegment_PublishesVillagerReturnedToPoolEvent()
    {
        EntityBase.ResetIdCounter();
        var (boot, sim) = CreateNewGame();

        sim.PathNodeManager.AddPathSegment(new GridPosRPG(0, 0), Direction.East);

        var villager = new VillagerLogic(EntityId.Next())
        {
            Name = "Evicted",
            State = VillagerState.Travelling
        };
        sim.VillagerSystem.AddVillager(villager);
        sim.PathTraffic.PlaceVillagerOnPath(villager, new GridPosRPG(0, 0));

        VillagerReturnedToPoolEvent? captured = null;
        boot.Services.Get<EventBus>().Subscribe<VillagerReturnedToPoolEvent>(e => captured = e);

        sim.PathNodeManager.RemovePathSegment(new GridPosRPG(0, 0));

        Assert.NotNull(captured);
        Assert.Equal(villager.Id, captured!.Value.VillagerId);
        Assert.Equal("path_removed", captured.Value.Reason);
    }

    // â”€â”€â”€ Waiting Villager Path Detection Tests â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    public void WaitingVillager_DetectsNewPath_StartsWalking()
    {
        EntityBase.ResetIdCounter();
        var (boot, sim) = CreateNewGame();

        var spawner = new VillageSpawnerLogic(EntityId.Next());

        spawner.SpawnInterval = 0.1f;
        spawner.MaxVillagers = 1;
        sim.EntityManager.AddStructure(spawner, new GridPosRPG(0, 0));

        // Exit gate facing east â€” no path at facing position (2, 0) yet
        sim.PathGateManager.AddPathGate(new GridPosRPG(1, 0), Direction.East);

        // Tick until the villager spawns and waits
        for (int i = 0; i < 60; i++)
        {
            sim.Update(SimulationTicker.FixedTimeStep);
        }

        Assert.NotEmpty(sim.VillagerSystem.Villagers);
        var villager = sim.VillagerSystem.Villagers[0];
        Assert.Equal(VillagerState.Idle, villager.State);
        Assert.Equal(new GridPosRPG(2, 0), villager.Position);

        // Now build a path under the waiting villager
        sim.PathNodeManager.AddPathSegment(new GridPosRPG(2, 0), Direction.East);

        // Tick â€” villager should detect the path and start walking
        for (int i = 0; i < 5; i++)
        {
            sim.Update(SimulationTicker.FixedTimeStep);
        }

        Assert.Equal(VillagerState.Travelling, villager.State);
        Assert.NotNull(villager.CurrentPathSegmentId);
    }

    [Fact]
    public void WaitingVillager_DoesNotMoveWithoutPath()
    {
        EntityBase.ResetIdCounter();
        var (boot, sim) = CreateNewGame();

        var spawner = new VillageSpawnerLogic(EntityId.Next());

        spawner.SpawnInterval = 0.1f;
        spawner.MaxVillagers = 1;
        sim.EntityManager.AddStructure(spawner, new GridPosRPG(0, 0));

        sim.PathGateManager.AddPathGate(new GridPosRPG(1, 0), Direction.East);

        // Tick until spawned
        for (int i = 0; i < 60; i++)
        {
            sim.Update(SimulationTicker.FixedTimeStep);
        }

        var villager = sim.VillagerSystem.Villagers[0];
        var waitPos = villager.Position;

        // Tick more â€” villager should remain idle at same position
        for (int i = 0; i < 120; i++)
        {
            sim.Update(SimulationTicker.FixedTimeStep);
        }

        Assert.Equal(VillagerState.Idle, villager.State);
        Assert.Equal(waitPos, villager.Position);
        Assert.Null(villager.CurrentPathSegmentId);
    }

    // ——— RotatePathGate via PathGateManager ———————————————————

    [Fact]
    public void RotatePathGate_UpdatesFacingAndMode()
    {
        EntityBase.ResetIdCounter();
        var (boot, sim) = CreateNewGame();

        var spawner = new VillageSpawnerLogic(EntityId.Next());

        sim.EntityManager.AddStructure(spawner, new GridPosRPG(5, 5));

        // Gate at (6,5) facing West toward structure ? Entrance
        var gate = sim.PathGateManager.AddPathGate(new GridPosRPG(6, 5), Direction.West);
        Assert.NotNull(gate);
        Assert.True(gate!.IsEntrance);
        Assert.Equal(Direction.West, gate.Facing);

        // Rotate — West ? North
        sim.PathGateManager.RotatePathGate(new GridPosRPG(6, 5));
        Assert.Equal(Direction.North, gate.Facing);

        // Rotate again — North ? East (now facing away from structure at (5,5))
        sim.PathGateManager.RotatePathGate(new GridPosRPG(6, 5));
        Assert.Equal(Direction.East, gate.Facing);
        Assert.True(gate.IsExit);
    }

    [Fact]
    public void RotatePathGate_PublishesEntityRotatedEvent()
    {
        EntityBase.ResetIdCounter();
        var (boot, sim) = CreateNewGame();

        sim.PathGateManager.AddPathGate(new GridPosRPG(3, 3), Direction.North);

        EntityRotatedEvent? received = null;
        boot.Services.Get<EventBus>().Subscribe<EntityRotatedEvent>(e => received = e);

        sim.PathGateManager.RotatePathGate(new GridPosRPG(3, 3));

        Assert.NotNull(received);
        Assert.Equal("PathGate", received!.Value.EntityType);
        Assert.Equal(new GridPosRPG(3, 3), received.Value.Position);
        Assert.Equal(Direction.East, received.Value.NewFacing);
    }
}
