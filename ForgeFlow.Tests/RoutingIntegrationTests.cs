using ForgeFlow.Core;
using ForgeFlow.Core.Entities;
using ForgeFlow.Core.Events;
using ForgeFlow.Core.Proto;
using ForgeFlow.Core.Proto.Logic;
using ForgeFlow.Core.Proto.Prototypes;
using ForgeFlow.Core.Save;
using ForgeFlow.Core.Systems;

namespace ForgeFlow.Tests;

/// <summary>
/// Integration tests for Phase 2: Routing entity consultation in PathTrafficSystem.
/// Tests that FilterSplitter, CheckGate, and Balancer correctly redirect
/// villagers and heroes during path traversal.
/// Also tests CheckGateLogic.EvaluateVillagerRoute and WorkerRoutedEvent publishing.
/// </summary>
public class RoutingIntegrationTests
{
    // ── Helpers ──────────────────────────────────────────────────────

    private static GameBootstrapper CreateBootstrapper()
    {
        var bootstrapper = new GameBootstrapper();
        bootstrapper.Bootstrap();
        return bootstrapper;
    }

    private static (GameBootstrapper boot, SimulationTicker sim) CreateNewGame(
        Difficulty difficulty = Difficulty.Normal)
    {
        EntityBase.ResetIdCounter();
        var boot = CreateBootstrapper();
        var settings = new NewGameSettings
        {
            GameName = "RoutingTest",
            Difficulty = difficulty,
            Seed = 42,
            GuildName = "TestGuild"
        };
        boot.ApplyNewGameSettings(settings);
        return (boot, boot.Services.Get<SimulationTicker>());
    }

    /// <summary>
    /// Builds a T-junction layout:
    ///   segA (start) → [routingEntity at routingPos] → segEast (East) or segSouth (South)
    /// Returns (segA, segEast, segSouth) with segA.ConnectedStructureId linked to the routing entity.
    /// </summary>
    private static (PathSegmentLogic segA, PathSegmentLogic segEast, PathSegmentLogic segSouth)
        BuildTJunction(
            SimulationTicker sim,
            StructureBase routingEntity,
            GridPosRPG routingPos)
    {
        // Place routing entity
        sim.StructureManager.AddProtoStructure(routingEntity, routingPos);

        // segA is west of the routing entity — the junction segment
        var segAPos = new GridPosRPG(routingPos.X - 1, routingPos.Y);
        var segA = sim.PathNodeManager.AddPathSegment(segAPos, Direction.East);

        // segEast is east of the routing entity
        var segEastPos = new GridPosRPG(routingPos.X + 1, routingPos.Y);
        var segEast = sim.PathNodeManager.AddPathSegment(segEastPos, Direction.East);

        // segSouth is south of the routing entity (Y-1 because South = (0,-1))
        var segSouthPos = new GridPosRPG(routingPos.X, routingPos.Y - 1);
        var segSouth = sim.PathNodeManager.AddPathSegment(segSouthPos, Direction.South);

        // Wire segA to the routing entity and set default next to segEast
        segA!.ConnectedStructureId = routingEntity.Id;
        segA.NextSegmentId = segEast!.Id;

        return (segA, segEast!, segSouth!);
    }

    /// <summary>
    /// Creates a villager, places it on the given segment ready to move on next tick.
    /// </summary>
    private static VillagerLogic PlaceVillagerOnSegment(
        SimulationTicker sim,
        PathSegmentLogic segment)
    {
        var villager = new VillagerLogic(EntityId.Next())
        {
            State = VillagerState.Travelling,
            MovementSpeed = 100f // high speed so PathProgress exceeds 1.0 in one tick
        };
        sim.VillagerSystem.AddVillager(villager);
        segment.TryAddOccupant(villager.Id);
        villager.CurrentPathSegmentId = segment.Id;
        villager.Position = segment.Position;
        villager.PathProgress = 0.99f; // about to transition
        return villager;
    }

    /// <summary>
    /// Creates a hero, places it on the given segment ready to move on next tick.
    /// </summary>
    private static HeroEntity PlaceHeroOnSegment(
        SimulationTicker sim,
        PathSegmentLogic segment)
    {
        var hero = new HeroEntity(EntityId.Next())
        {
            State = HeroState.OnPath
        };
        sim.EntityManager.AddHero(hero);
        segment.TryAddOccupant(hero.Id);
        hero.CurrentPathSegmentId = segment.Id;
        hero.Position = segment.Position;
        hero.PathProgress = 0.99f;
        return hero;
    }

    // ── CheckGateLogic.EvaluateVillagerRoute ────────────────────────

    [Fact]
    public void CheckGateLogic_EvaluateVillagerRoute_MatchingProfession_ReturnsRuleDirection()
    {
        var gate = new CheckGateLogic(EntityId.Next())
        {
            DefaultDirection = Direction.East,
            Rule = new GateRule
            {
                ConditionType = GateConditionType.HasProfession,
                ConditionValue = "Lumberjack",
                OutputDirection = Direction.South
            }
        };

        var villager = new VillagerLogic(EntityId.Next()) { Profession = VillagerJob.Lumberjack };
        Assert.Equal(Direction.South, gate.EvaluateVillagerRoute(villager));
    }

    [Fact]
    public void CheckGateLogic_EvaluateVillagerRoute_NoMatch_ReturnsDefault()
    {
        var gate = new CheckGateLogic(EntityId.Next())
        {
            DefaultDirection = Direction.East,
            Rule = new GateRule
            {
                ConditionType = GateConditionType.HasProfession,
                ConditionValue = "Guard",
                OutputDirection = Direction.South
            }
        };

        var villager = new VillagerLogic(EntityId.Next()) { Profession = VillagerJob.Lumberjack };
        Assert.Equal(Direction.East, gate.EvaluateVillagerRoute(villager));
    }

    [Fact]
    public void CheckGateLogic_EvaluateVillagerRoute_HasJobClass_Matches()
    {
        var gate = new CheckGateLogic(EntityId.Next())
        {
            DefaultDirection = Direction.East,
            Rule = new GateRule
            {
                ConditionType = GateConditionType.HasJobClass,
                ConditionValue = "Warrior",
                OutputDirection = Direction.North
            }
        };

        var villager = new VillagerLogic(EntityId.Next()) { TrainedClass = VillagerClass.Warrior };
        Assert.Equal(Direction.North, gate.EvaluateVillagerRoute(villager));
    }

    [Fact]
    public void CheckGateLogic_EvaluateVillagerRoute_CarryingItem_Matches()
    {
        var gate = new CheckGateLogic(EntityId.Next())
        {
            DefaultDirection = Direction.East,
            Rule = new GateRule
            {
                ConditionType = GateConditionType.CarryingItem,
                ConditionValue = "sticks",
                OutputDirection = Direction.West
            }
        };

        var villager = new VillagerLogic(EntityId.Next());
        villager.TryPickUpItem(new ItemInstance { ProtoId = "sticks" });
        Assert.Equal(Direction.West, gate.EvaluateVillagerRoute(villager));
    }

    [Fact]
    public void CheckGateLogic_EvaluateVillagerRoute_HasToolType_Matches()
    {
        var gate = new CheckGateLogic(EntityId.Next())
        {
            DefaultDirection = Direction.East,
            Rule = new GateRule
            {
                ConditionType = GateConditionType.HasToolType,
                ConditionValue = "iron_axe",
                OutputDirection = Direction.South
            }
        };

        var villager = new VillagerLogic(EntityId.Next());
        villager.EquipTool("iron_axe");
        Assert.Equal(Direction.South, gate.EvaluateVillagerRoute(villager));
    }

    [Fact]
    public void CheckGateLogic_EvaluateVillagerRoute_UnsupportedCondition_ReturnsDefault()
    {
        var gate = new CheckGateLogic(EntityId.Next())
        {
            DefaultDirection = Direction.East,
            Rule = new GateRule
            {
                ConditionType = GateConditionType.MinLevel, // Not applicable to villagers
                ConditionValue = "1",
                OutputDirection = Direction.South
            }
        };

        var villager = new VillagerLogic(EntityId.Next());
        Assert.Equal(Direction.East, gate.EvaluateVillagerRoute(villager));
    }

    // ── FilterSplitter PathTraffic Integration (Villagers) ──────────

    [Fact]
    public void FilterSplitter_VillagerCarryingFilteredItem_RoutedToFilteredDirection()
    {
        var (_, sim) = CreateNewGame();
        var routingPos = new GridPosRPG(10, 10);

        var splitter = new FilterSplitterLogic(EntityId.Next());
        splitter.DefaultDirection = Direction.East;
        splitter.FilteredOutputDirection = Direction.South;
        splitter.SetFilteredItem("sticks");
        splitter.PendingFilterChange = false;
        splitter.IsActive = true;

        var (segA, segEast, segSouth) = BuildTJunction(sim, splitter, routingPos);

        var villager = PlaceVillagerOnSegment(sim, segA);
        villager.TryPickUpItem(new ItemInstance { ProtoId = "sticks" });

        sim.PathTraffic.TickVillagers(1.0f);

        Assert.Equal(segSouth.Id, villager.CurrentPathSegmentId);
    }

    [Fact]
    public void FilterSplitter_VillagerNotCarryingFilteredItem_RoutedToDefault()
    {
        var (_, sim) = CreateNewGame();
        var routingPos = new GridPosRPG(10, 10);

        var splitter = new FilterSplitterLogic(EntityId.Next());
        splitter.DefaultDirection = Direction.East;
        splitter.FilteredOutputDirection = Direction.South;
        splitter.SetFilteredItem("sticks");
        splitter.PendingFilterChange = false;
        splitter.IsActive = true;

        var (segA, segEast, segSouth) = BuildTJunction(sim, splitter, routingPos);

        var villager = PlaceVillagerOnSegment(sim, segA);
        // Villager has no items

        sim.PathTraffic.TickVillagers(1.0f);

        Assert.Equal(segEast.Id, villager.CurrentPathSegmentId);
    }

    [Fact]
    public void FilterSplitter_VillagerCarryingOreNotSticks_RoutedToDefault()
    {
        var (_, sim) = CreateNewGame();
        var routingPos = new GridPosRPG(10, 10);

        var splitter = new FilterSplitterLogic(EntityId.Next());
        splitter.DefaultDirection = Direction.East;
        splitter.FilteredOutputDirection = Direction.South;
        splitter.SetFilteredItem("sticks");
        splitter.PendingFilterChange = false;
        splitter.IsActive = true;

        var (segA, segEast, segSouth) = BuildTJunction(sim, splitter, routingPos);

        var villager = PlaceVillagerOnSegment(sim, segA);
        villager.TryPickUpItem(new ItemInstance { ProtoId = "ore" });

        sim.PathTraffic.TickVillagers(1.0f);

        Assert.Equal(segEast.Id, villager.CurrentPathSegmentId);
    }

    // ── CheckGate PathTraffic Integration (Villagers) ───────────────

    [Fact]
    public void CheckGate_VillagerMatchingRule_RoutedToRuleDirection()
    {
        var (_, sim) = CreateNewGame();
        var routingPos = new GridPosRPG(10, 10);

        var gate = new CheckGateLogic(EntityId.Next())
        {
            DefaultDirection = Direction.East,
            Rule = new GateRule
            {
                ConditionType = GateConditionType.HasProfession,
                ConditionValue = "Lumberjack",
                OutputDirection = Direction.South
            },
            IsActive = true
        };

        var (segA, segEast, segSouth) = BuildTJunction(sim, gate, routingPos);

        var villager = PlaceVillagerOnSegment(sim, segA);
        villager.Profession = VillagerJob.Lumberjack;

        sim.PathTraffic.TickVillagers(1.0f);

        Assert.Equal(segSouth.Id, villager.CurrentPathSegmentId);
    }

    [Fact]
    public void CheckGate_VillagerNotMatchingRule_RoutedToDefault()
    {
        var (_, sim) = CreateNewGame();
        var routingPos = new GridPosRPG(10, 10);

        var gate = new CheckGateLogic(EntityId.Next())
        {
            DefaultDirection = Direction.East,
            Rule = new GateRule
            {
                ConditionType = GateConditionType.HasProfession,
                ConditionValue = "Guard",
                OutputDirection = Direction.South
            },
            IsActive = true
        };

        var (segA, segEast, segSouth) = BuildTJunction(sim, gate, routingPos);

        var villager = PlaceVillagerOnSegment(sim, segA);
        villager.Profession = VillagerJob.Lumberjack;

        sim.PathTraffic.TickVillagers(1.0f);

        Assert.Equal(segEast.Id, villager.CurrentPathSegmentId);
    }

    // ── Balancer PathTraffic Integration (Villagers) ────────────────

    [Fact]
    public void Balancer_DistributesVillagers_RoundRobin()
    {
        var (_, sim) = CreateNewGame();
        var routingPos = new GridPosRPG(10, 10);

        var balancer = new BalancerLogic(EntityId.Next()) { IsActive = true };
        balancer.OutputDirections.Clear();
        balancer.OutputDirections.Add(Direction.East);
        balancer.OutputDirections.Add(Direction.South);

        var (segA, segEast, segSouth) = BuildTJunction(sim, balancer, routingPos);

        // First villager
        var v1 = PlaceVillagerOnSegment(sim, segA);
        sim.PathTraffic.TickVillagers(1.0f);
        var v1Dest = v1.CurrentPathSegmentId;

        // Second villager — put on segA again (need to clear occupant first)
        segA.OccupantIds.Clear();
        var v2 = PlaceVillagerOnSegment(sim, segA);
        sim.PathTraffic.TickVillagers(1.0f);
        var v2Dest = v2.CurrentPathSegmentId;

        // Round-robin: one should go East, one should go South
        Assert.NotEqual(v1Dest, v2Dest);
        Assert.True(
            (v1Dest == segEast.Id && v2Dest == segSouth.Id) ||
            (v1Dest == segSouth.Id && v2Dest == segEast.Id));
    }

    // ── Hero Routing Integration ────────────────────────────────────

    [Fact]
    public void FilterSplitter_HeroMatchingMinLevel_RoutedToRuleDirection()
    {
        var (_, sim) = CreateNewGame();
        var routingPos = new GridPosRPG(10, 10);

        var splitter = new FilterSplitterLogic(EntityId.Next());
        splitter.DefaultDirection = Direction.East;
        splitter.IsActive = true;
        splitter.Rules.Add(new GateRule
        {
            ConditionType = GateConditionType.MinLevel,
            ConditionValue = "5",
            OutputDirection = Direction.South
        });

        var (segA, segEast, segSouth) = BuildTJunction(sim, splitter, routingPos);

        var hero = PlaceHeroOnSegment(sim, segA);
        hero.Level = 10;

        sim.PathTraffic.Tick(1.0f);

        Assert.Equal(segSouth.Id, hero.CurrentPathSegmentId);
    }

    [Fact]
    public void FilterSplitter_HeroBelowMinLevel_RoutedToDefault()
    {
        var (_, sim) = CreateNewGame();
        var routingPos = new GridPosRPG(10, 10);

        var splitter = new FilterSplitterLogic(EntityId.Next());
        splitter.DefaultDirection = Direction.East;
        splitter.IsActive = true;
        splitter.Rules.Add(new GateRule
        {
            ConditionType = GateConditionType.MinLevel,
            ConditionValue = "20",
            OutputDirection = Direction.South
        });

        var (segA, segEast, segSouth) = BuildTJunction(sim, splitter, routingPos);

        var hero = PlaceHeroOnSegment(sim, segA);
        hero.Level = 3;

        sim.PathTraffic.Tick(1.0f);

        Assert.Equal(segEast.Id, hero.CurrentPathSegmentId);
    }

    [Fact]
    public void CheckGate_HeroMatchingHasTrait_RoutedToRuleDirection()
    {
        var (_, sim) = CreateNewGame();
        var routingPos = new GridPosRPG(10, 10);

        var gate = new CheckGateLogic(EntityId.Next())
        {
            DefaultDirection = Direction.East,
            Rule = new GateRule
            {
                ConditionType = GateConditionType.HasTrait,
                ConditionValue = "brave",
                OutputDirection = Direction.South
            },
            IsActive = true
        };

        var (segA, segEast, segSouth) = BuildTJunction(sim, gate, routingPos);

        var hero = PlaceHeroOnSegment(sim, segA);
        hero.Traits.Add("brave");

        sim.PathTraffic.Tick(1.0f);

        Assert.Equal(segSouth.Id, hero.CurrentPathSegmentId);
    }

    // ── WorkerRoutedEvent Publishing ────────────────────────────────

    [Fact]
    public void RoutingEntity_PublishesWorkerRoutedEvent_OnVillagerRedirect()
    {
        var (boot, sim) = CreateNewGame();
        var routingPos = new GridPosRPG(10, 10);

        var splitter = new FilterSplitterLogic(EntityId.Next());
        splitter.DefaultDirection = Direction.East;
        splitter.FilteredOutputDirection = Direction.South;
        splitter.SetFilteredItem("sticks");
        splitter.PendingFilterChange = false;
        splitter.IsActive = true;

        var (segA, _, _) = BuildTJunction(sim, splitter, routingPos);

        var villager = PlaceVillagerOnSegment(sim, segA);
        villager.TryPickUpItem(new ItemInstance { ProtoId = "sticks" });

        WorkerRoutedEvent? received = null;
        boot.Services.Get<EventBus>().Subscribe<WorkerRoutedEvent>(e => received = e);

        sim.PathTraffic.TickVillagers(1.0f);

        Assert.NotNull(received);
        Assert.Equal(villager.Id, received.Value.WorkerId);
        Assert.Equal(Direction.South, received.Value.RouteDirection);
    }

    [Fact]
    public void RoutingEntity_PublishesWorkerRoutedEvent_OnHeroRedirect()
    {
        var (boot, sim) = CreateNewGame();
        var routingPos = new GridPosRPG(10, 10);

        var splitter = new FilterSplitterLogic(EntityId.Next());
        splitter.DefaultDirection = Direction.East;
        splitter.IsActive = true;
        splitter.Rules.Add(new GateRule
        {
            ConditionType = GateConditionType.MinLevel,
            ConditionValue = "1",
            OutputDirection = Direction.South
        });

        var (segA, _, _) = BuildTJunction(sim, splitter, routingPos);

        var hero = PlaceHeroOnSegment(sim, segA);
        hero.Level = 5;

        WorkerRoutedEvent? received = null;
        boot.Services.Get<EventBus>().Subscribe<WorkerRoutedEvent>(e => received = e);

        sim.PathTraffic.Tick(1.0f);

        Assert.NotNull(received);
        Assert.Equal(hero.Id, received.Value.WorkerId);
        Assert.Equal(Direction.South, received.Value.RouteDirection);
    }

    [Fact]
    public void RoutingEntity_NoEvent_WhenNoRoutingEntityConnected()
    {
        var (boot, sim) = CreateNewGame();

        // Create two plain segments linked, no routing entity
        var seg1 = sim.PathNodeManager.AddPathSegment(new GridPosRPG(5, 5), Direction.East);
        var seg2 = sim.PathNodeManager.AddPathSegment(new GridPosRPG(6, 5), Direction.East);
        seg1!.NextSegmentId = seg2!.Id;

        var villager = PlaceVillagerOnSegment(sim, seg1);

        bool eventFired = false;
        boot.Services.Get<EventBus>().Subscribe<WorkerRoutedEvent>(_ => eventFired = true);

        sim.PathTraffic.TickVillagers(1.0f);

        Assert.False(eventFired);
        Assert.Equal(seg2.Id, villager.CurrentPathSegmentId);
    }

    // ── Fallback & Edge Cases ───────────────────────────────────────

    [Fact]
    public void RoutingEntity_FallbackToDefault_WhenNoSegmentInEvaluatedDirection()
    {
        var (_, sim) = CreateNewGame();
        var routingPos = new GridPosRPG(10, 10);

        var splitter = new FilterSplitterLogic(EntityId.Next());
        splitter.DefaultDirection = Direction.North; // No segment placed north
        splitter.FilteredOutputDirection = Direction.North;
        splitter.SetFilteredItem("sticks");
        splitter.PendingFilterChange = false;
        splitter.IsActive = true;

        // Only place East and South segments — no North segment
        sim.StructureManager.AddProtoStructure(splitter, routingPos);

        var segAPos = new GridPosRPG(routingPos.X - 1, routingPos.Y);
        var segA = sim.PathNodeManager.AddPathSegment(segAPos, Direction.East);

        var segEastPos = new GridPosRPG(routingPos.X + 1, routingPos.Y);
        var segEast = sim.PathNodeManager.AddPathSegment(segEastPos, Direction.East);

        segA!.ConnectedStructureId = splitter.Id;
        segA.NextSegmentId = segEast!.Id;

        var villager = PlaceVillagerOnSegment(sim, segA);
        villager.TryPickUpItem(new ItemInstance { ProtoId = "sticks" });

        sim.PathTraffic.TickVillagers(1.0f);

        // Should fall back to default nextId (segEast) since no segment at North
        Assert.Equal(segEast.Id, villager.CurrentPathSegmentId);
    }

    [Fact]
    public void RoutingEntity_NonRoutingStructure_NoOverride()
    {
        var (_, sim) = CreateNewGame();

        // Connect a non-routing structure (Inn) to segment — should NOT trigger routing
        var inn = new InnLogic(EntityId.Next()) { IsActive = true };
        sim.StructureManager.AddProtoStructure(inn, new GridPosRPG(10, 10));

        var seg1 = sim.PathNodeManager.AddPathSegment(new GridPosRPG(9, 10), Direction.East);
        var seg2 = sim.PathNodeManager.AddPathSegment(new GridPosRPG(11, 10), Direction.East);
        seg1!.ConnectedStructureId = inn.Id;
        seg1.NextSegmentId = seg2!.Id;

        var villager = PlaceVillagerOnSegment(sim, seg1);

        sim.PathTraffic.TickVillagers(1.0f);

        // Should follow normal path to seg2, no routing entity override
        Assert.Equal(seg2.Id, villager.CurrentPathSegmentId);
    }

    [Fact]
    public void RoutingEntity_SegmentWithNoConnectedStructure_NoOverride()
    {
        var (_, sim) = CreateNewGame();

        var seg1 = sim.PathNodeManager.AddPathSegment(new GridPosRPG(5, 5), Direction.East);
        var seg2 = sim.PathNodeManager.AddPathSegment(new GridPosRPG(6, 5), Direction.East);
        seg1!.NextSegmentId = seg2!.Id;
        // No ConnectedStructureId set

        var villager = PlaceVillagerOnSegment(sim, seg1);

        sim.PathTraffic.TickVillagers(1.0f);

        Assert.Equal(seg2.Id, villager.CurrentPathSegmentId);
    }

    // ── Multi-Rule FilterSplitter ───────────────────────────────────

    [Fact]
    public void FilterSplitter_MultiRule_VillagerMatchesSecondRule()
    {
        var (_, sim) = CreateNewGame();
        var routingPos = new GridPosRPG(10, 10);

        var splitter = new FilterSplitterLogic(EntityId.Next());
        splitter.DefaultDirection = Direction.East;
        splitter.IsActive = true;
        // No single-item filter, use multi-rule mode
        splitter.Rules.Add(new GateRule
        {
            ConditionType = GateConditionType.HasProfession,
            ConditionValue = "Guard",
            OutputDirection = Direction.North // Won't match
        });
        splitter.Rules.Add(new GateRule
        {
            ConditionType = GateConditionType.HasProfession,
            ConditionValue = "Lumberjack",
            OutputDirection = Direction.South // Will match
        });

        var (segA, segEast, segSouth) = BuildTJunction(sim, splitter, routingPos);

        // Also place a north segment for the first rule (North = Y+1)
        sim.PathNodeManager.AddPathSegment(new GridPosRPG(routingPos.X, routingPos.Y + 1), Direction.North);

        var villager = PlaceVillagerOnSegment(sim, segA);
        villager.Profession = VillagerJob.Lumberjack;

        sim.PathTraffic.TickVillagers(1.0f);

        Assert.Equal(segSouth.Id, villager.CurrentPathSegmentId);
    }

    // ── Balancer Hero Integration ───────────────────────────────────

    [Fact]
    public void Balancer_DistributesHeroes_RoundRobin()
    {
        var (_, sim) = CreateNewGame();
        var routingPos = new GridPosRPG(10, 10);

        var balancer = new BalancerLogic(EntityId.Next()) { IsActive = true };
        balancer.OutputDirections.Clear();
        balancer.OutputDirections.Add(Direction.East);
        balancer.OutputDirections.Add(Direction.South);

        var (segA, segEast, segSouth) = BuildTJunction(sim, balancer, routingPos);

        // First hero
        var h1 = PlaceHeroOnSegment(sim, segA);
        sim.PathTraffic.Tick(1.0f);
        var h1Dest = h1.CurrentPathSegmentId;

        // Second hero — clear segA
        segA.OccupantIds.Clear();
        var h2 = PlaceHeroOnSegment(sim, segA);
        sim.PathTraffic.Tick(1.0f);
        var h2Dest = h2.CurrentPathSegmentId;

        // Round-robin: different destinations
        Assert.NotEqual(h1Dest, h2Dest);
        Assert.True(
            (h1Dest == segEast.Id && h2Dest == segSouth.Id) ||
            (h1Dest == segSouth.Id && h2Dest == segEast.Id));
    }

    // ── Walk-By Guard ───────────────────────────────────────────────

    [Fact]
    public void RoutingEntity_WalkByInteraction_SkippedForRoutingEntities()
    {
        var (boot, sim) = CreateNewGame();
        var routingPos = new GridPosRPG(10, 10);

        // Place a CheckGate connected to the DESTINATION segment
        var gate = new CheckGateLogic(EntityId.Next())
        {
            DefaultDirection = Direction.East,
            Rule = new GateRule
            {
                ConditionType = GateConditionType.HasProfession,
                ConditionValue = "Guard",
                OutputDirection = Direction.South
            },
            IsActive = true
        };
        sim.StructureManager.AddProtoStructure(gate, routingPos);

        // seg1 → seg2 (seg2 is connected to the routing entity)
        var seg1 = sim.PathNodeManager.AddPathSegment(new GridPosRPG(8, 10), Direction.East);
        var seg2 = sim.PathNodeManager.AddPathSegment(new GridPosRPG(9, 10), Direction.East);
        var seg3 = sim.PathNodeManager.AddPathSegment(new GridPosRPG(11, 10), Direction.East);
        seg1!.NextSegmentId = seg2!.Id;
        seg2.NextSegmentId = seg3!.Id;
        seg2.ConnectedStructureId = gate.Id;

        var villager = PlaceVillagerOnSegment(sim, seg1);

        bool trainingStarted = false;
        boot.Services.Get<EventBus>().Subscribe<VillagerTrainingStartedEvent>(_ => trainingStarted = true);

        sim.PathTraffic.TickVillagers(1.0f);

        // Should NOT trigger any walk-by training interaction
        Assert.False(trainingStarted);
    }
}
