using ForgeFlow.Core;
using ForgeFlow.Core.Data;
using ForgeFlow.Core.Data.Definitions;
using ForgeFlow.Core.Entities;
using ForgeFlow.Core.Events;
using ForgeFlow.Core.Proto;
using ForgeFlow.Core.Proto.Logic;
using ForgeFlow.Core.Proto.Prototypes;
using ForgeFlow.Core.Systems;
using ForgeFlow.Core.Utilities;

namespace ForgeFlow.Tests;

/// <summary>Tests for SimulationTicker path management API, villager job assignment, and structure counting.</summary>
public class SimulationTickerApiTests
{
    private static (SimulationTicker sim, EventBus bus) CreateSim()
    {
        var eventBus = new EventBus();
        var classRegistry = new ClassRegistry();
        classRegistry.Register(new ClassDefinition
        {
            Id = "warrior", Name = "Warrior", ClassBonus = 10f,
            BaseHealth = 120, BaseMana = 20
        });

        var trafficManager = new TrafficManager();
        var protoRegistry = new ProtoRegistry();
        protoRegistry.RegisterDefaults();
        var protoFactory = new ProtoFactory(protoRegistry, eventBus);
        var itemManager = new ItemManager(eventBus);
        var villagerSystem = new VillagerSystem(eventBus, itemManager);
        var terrain = new TerrainGrid(32, 32);
        terrain.GenerateDefault();
        var tileManager = new TileManager(terrain);
        var entityManager = new EntityManager(tileManager, villagerSystem);
        var pathTraffic = new PathTrafficSystem(trafficManager, entityManager, tileManager, eventBus);
        var tutorialSystem = new TutorialSystem(eventBus, protoFactory);
        var autoEquip = new AutoEquipSystem(eventBus, classRegistry, entityManager);
        var dungeonResolver = new DungeonResolver(new DungeonRegistry(), classRegistry, eventBus, entityManager, seed: 42);
        var fusionCalculator = new FusionCalculator(classRegistry, new ItemRegistry(), itemManager, eventBus, entityManager);
        var appearanceApplier = new AppearanceApplier(eventBus, entityManager);

        var gatingLimits = new GatingLimits();
        var researchManager = new ResearchManager(eventBus);
        var commandBus = new Core.Commands.CommandBus();
        var pathManager = new PathNodeManager(entityManager, tileManager, villagerSystem, pathTraffic, eventBus, commandBus, tutorialSystem, gatingLimits, researchManager, itemManager);
        var pathGateManager = new PathGateManager(entityManager, tileManager, eventBus, commandBus, tutorialSystem, itemManager, researchManager);
        pathTraffic.SetPathGateManager(pathGateManager);

        var structureManager = new StructureManager(
            entityManager, pathGateManager, pathManager, villagerSystem, itemManager,
            new ItemRegistry(), new RecipeRegistry(),
            gatingLimits, eventBus, commandBus, tutorialSystem, researchManager);

        var worldStateManager = new WorldStateManager(researchManager, entityManager, itemManager, eventBus);
        var dungeonManager = new DungeonManager(entityManager, pathGateManager, villagerSystem, itemManager, eventBus);

        return (new SimulationTicker(
            eventBus, commandBus,
            pathTraffic, villagerSystem, tutorialSystem,
            entityManager, tileManager, pathManager, pathGateManager, itemManager, gatingLimits,
            structureManager, researchManager, worldStateManager,
            appearanceApplier, dungeonResolver, dungeonManager), eventBus);
    }

    [Fact]
    public void SimulationTicker_CreatePathLine_CreatesLinkedSegments()
    {
        var (sim, _) = CreateSim();

        var segments = sim.PathNodeManager.CreatePathLine(new GridPosRPG(0, 0), new GridPosRPG(4, 0));

        Assert.Equal(5, segments.Count);
        Assert.NotNull(segments[0].NextSegmentId);
        Assert.Equal(segments[1].Id, segments[0].NextSegmentId);
    }

    [Fact]
    public void SimulationTicker_RemovePathSegment_Unlinks()
    {
        var (sim, _) = CreateSim();

        var s1 = sim.PathNodeManager.AddPathSegment(new GridPosRPG(0, 0), Direction.East);
        var s2 = sim.PathNodeManager.AddPathSegment(new GridPosRPG(1, 0), Direction.East);

        Assert.NotNull(s1);
        Assert.NotNull(s2);
        Assert.Equal(s2.Id, s1.NextSegmentId);

        sim.PathNodeManager.RemovePathSegment(new GridPosRPG(1, 0));

        Assert.Null(s1.NextSegmentId);
        Assert.False(sim.PathNodeManager.PathSegments.ContainsKey(s2.Id));
    }

    [Fact]
    public void SimulationTicker_AssignVillagerJob_PublishesEvent()
    {
        var (sim, bus) = CreateSim();

        var villager = new VillagerLogic(EntityId.Next()) { Name = "Test" };
        sim.VillagerSystem.AddVillager(villager);

        VillagerJobAssignedEvent? received = null;
        bus.Subscribe<VillagerJobAssignedEvent>(e => received = e);

        bool result = sim.VillagerSystem.AssignVillagerJob(villager.Id, VillagerJob.Miner);

        Assert.True(result);
        Assert.NotNull(received);
        Assert.Equal("Miner", received.Value.Job);
    }

    [Fact]
    public void SimulationTicker_CountSpawners()
    {
        var (sim, _) = CreateSim();

        Assert.Equal(0, sim.StructureManager.CountSpawners());

        var spawner = new VillageSpawnerLogic(EntityId.Next());
        sim.StructureManager.AddProtoStructure(spawner, new GridPosRPG(0, 0));

        Assert.Equal(1, sim.StructureManager.CountSpawners());
    }

    [Fact]
    public void SimulationTicker_CountTrainingBuildings()
    {
        var (sim, _) = CreateSim();

        Assert.Equal(0, sim.StructureManager.CountTrainingBuildings());

        var school = new TrainingBuildingLogic(EntityId.Next());
        sim.StructureManager.AddProtoStructure(school, new GridPosRPG(0, 0));

        Assert.Equal(1, sim.StructureManager.CountTrainingBuildings());
    }

    [Fact]
    public void SimulationTicker_AddPathSegment_NoLongerBlockedByTierLimits()
    {
        var (sim, _) = CreateSim();

        for (int i = 0; i < 31; i++)
        {
            var seg = sim.PathNodeManager.AddPathSegment(new GridPosRPG(i, 0), Direction.East);
            Assert.NotNull(seg);
        }
    }

    [Fact]
    public void SimulationTicker_AddProtoStructure_NoLongerBlockedByTierLimits()
    {
        var (sim, _) = CreateSim();

        for (int i = 0; i < 4; i++)
        {
            var structure = new GatheringLogicBase(EntityId.Next());
            Assert.True(sim.StructureManager.AddProtoStructure(structure, new GridPosRPG(i, 0)));
        }
    }
}
