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

/// <summary>
/// Tests for the training building completion → exit pipeline.
/// Verifies that:
/// - VillagerLogic.TickTraining advances progress and transitions to Idle
/// - StructureManager.TickTrainingTrainees detects completion and ejects villagers
/// - TrainingBuildingLogic.CompleteTraining removes from CurrentTrainees
/// - VillagerTrainingCompleteEvent is published
/// - Villagers exit via PathGate when available
/// - Villagers are parked in WaitingToExitIds when no exit gate exists
/// </summary>
public class TrainingBuildingTests
{
    public TrainingBuildingTests()
    {
        EntityIdFactory.ResetForTesting();
        EntityBase.ResetIdCounter();
    }

    // ── Helpers ──────────────────────────────────────────────────────

    private static VillagerLogic CreateVillager(
        string name = "TestVillager",
        int stamina = 100,
        VillagerClass trainedClass = VillagerClass.Untrained)
    {
        return new VillagerLogic(EntityId.Next())
        {
            Name = name,
            Stamina = stamina,
            MaxStamina = 100,
            IsActive = true,
            TrainedClass = trainedClass
        };
    }

    private static TrainingBuildingLogic CreateTrainingBuilding(
        int maxTrainees = 2,
        float trainingDuration = 15f,
        VillagerClass outputClass = VillagerClass.Warrior)
    {
        var proto = new TrainingBuildingProto
        {
            Id = "training_test",
            OutputClass = outputClass,
            TrainingDuration = trainingDuration,
            MaxTrainees = maxTrainees
        };
        var logic = new TrainingBuildingLogic(EntityId.Next());
        logic.InitializeFromProto(proto);
        logic.IsActive = true;
        return logic;
    }

    private static (EventBus bus, VillagerSystem villagers, ItemManager items, StructureManager structures, EntityManager entities, PathGateManager pathGates) CreateManagers()
    {
        var eventBus = new EventBus();
        var itemRegistry = new ItemRegistry();
        var recipeRegistry = new RecipeRegistry();
        var itemManager = new ItemManager(eventBus);
        var villagerSystem = new VillagerSystem(eventBus, itemManager);
        var terrain = new TerrainGrid(32, 32);
        terrain.GenerateDefault();
        var tileManager = new TileManager(terrain);
        var entityManager = new EntityManager(tileManager, villagerSystem);
        var trafficManager = new TrafficManager();
        var pathTraffic = new PathTrafficSystem(trafficManager, entityManager, tileManager, eventBus);
        var protoRegistry = new ProtoRegistry();
        protoRegistry.RegisterDefaults();
        var protoFactory = new ProtoFactory(protoRegistry, eventBus);
        var tutorialSystem = new TutorialSystem(eventBus, protoFactory);
        var gatingLimits = new GatingLimits();
        var researchManager = new ResearchManager(eventBus);
        var commandBus = new Core.Commands.CommandBus();
        var pathManager = new PathNodeManager(entityManager, tileManager, villagerSystem, pathTraffic, eventBus, commandBus, tutorialSystem, gatingLimits, researchManager, itemManager);
        var pathGateManager = new PathGateManager(entityManager, tileManager, eventBus, commandBus, tutorialSystem, itemManager, researchManager);
        pathTraffic.SetPathGateManager(pathGateManager);

        var structureManager = new StructureManager(
            entityManager, pathGateManager, pathManager, villagerSystem, itemManager,
            itemRegistry, recipeRegistry,
            gatingLimits, eventBus, commandBus, tutorialSystem, researchManager);

        return (eventBus, villagerSystem, itemManager, structureManager, entityManager, pathGateManager);
    }

    // ══════════════════════════════════════════════════════════════════
    // ── VillagerLogic.TickTraining ────────────────────────────────────
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public void TickTraining_AdvancesProgress()
    {
        var villager = CreateVillager();
        var training = CreateTrainingBuilding(trainingDuration: 10f);

        training.AcceptTrainee(villager);

        Assert.Equal(VillagerState.Training, villager.State);
        Assert.Equal(0f, villager.TrainingProgress);

        villager.Tick(3f);

        Assert.Equal(VillagerState.Training, villager.State);
        Assert.Equal(3f, villager.TrainingProgress);
    }

    [Fact]
    public void TickTraining_CompletesAfterDuration()
    {
        var villager = CreateVillager();
        var training = CreateTrainingBuilding(trainingDuration: 5f);

        training.AcceptTrainee(villager);

        // Tick past the training duration
        villager.Tick(6f);

        Assert.Equal(VillagerState.Idle, villager.State);
        Assert.Equal(0f, villager.TrainingProgress);
        Assert.Null(villager.TrainingBuildingId);
    }

    [Fact]
    public void TickTraining_AssignsClassImmediatelyOnEntry()
    {
        var villager = CreateVillager();
        var training = CreateTrainingBuilding(outputClass: VillagerClass.Warrior);

        training.AcceptTrainee(villager);

        // Class is assigned immediately on entry (design choice)
        Assert.Equal(VillagerClass.Warrior, villager.TrainedClass);
    }

    [Fact]
    public void TickTraining_DoesNotCompleteBeforeDuration()
    {
        var villager = CreateVillager();
        var training = CreateTrainingBuilding(trainingDuration: 10f);

        training.AcceptTrainee(villager);

        // Tick just short of duration
        villager.Tick(9.9f);

        Assert.Equal(VillagerState.Training, villager.State);
        Assert.NotNull(villager.TrainingBuildingId);
    }

    // ══════════════════════════════════════════════════════════════════
    // ── TrainingBuildingLogic ─────────────────────────────────────────
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public void AcceptTrainee_AddsToCurrentTrainees()
    {
        var villager = CreateVillager();
        var training = CreateTrainingBuilding();

        training.AcceptTrainee(villager);

        Assert.Single(training.CurrentTrainees);
        Assert.Contains(villager.Id, training.CurrentTrainees);
    }

    [Fact]
    public void AcceptTrainee_RejectsWhenFull()
    {
        var training = CreateTrainingBuilding(maxTrainees: 1);
        var v1 = CreateVillager();
        var v2 = CreateVillager();

        Assert.True(training.AcceptTrainee(v1));
        Assert.False(training.AcceptTrainee(v2));
        Assert.Single(training.CurrentTrainees);
    }

    [Fact]
    public void AcceptTrainee_RejectsAlreadyTrainedVillager()
    {
        var training = CreateTrainingBuilding();
        var villager = CreateVillager(trainedClass: VillagerClass.Warrior);

        Assert.False(training.AcceptTrainee(villager));
        Assert.Empty(training.CurrentTrainees);
    }

    [Fact]
    public void CompleteTraining_RemovesFromTraineeList()
    {
        var villager = CreateVillager();
        var training = CreateTrainingBuilding();

        training.AcceptTrainee(villager);
        Assert.Single(training.CurrentTrainees);

        training.CompleteTraining(villager);
        Assert.Empty(training.CurrentTrainees);
    }

    [Fact]
    public void CompleteTraining_OpensSlotForNewTrainee()
    {
        var training = CreateTrainingBuilding(maxTrainees: 1, trainingDuration: 2f);
        var v1 = CreateVillager();
        var v2 = CreateVillager();

        training.AcceptTrainee(v1);
        Assert.False(training.CanAcceptTrainee);

        training.CompleteTraining(v1);
        Assert.True(training.CanAcceptTrainee);
        Assert.True(training.AcceptTrainee(v2));
    }

    [Fact]
    public void TickTraining_VillagerRetainsClassAfterCompletion()
    {
        var villager = CreateVillager();
        var training = CreateTrainingBuilding(
            trainingDuration: 1f,
            outputClass: VillagerClass.Warrior);

        training.AcceptTrainee(villager);

        // Tick to complete
        villager.Tick(2f);

        Assert.Equal(VillagerState.Idle, villager.State);
        Assert.Equal(VillagerClass.Warrior, villager.TrainedClass);
    }

    // ══════════════════════════════════════════════════════════════════
    // ── StructureManager Integration — Training Completion + Exit ─────
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public void StructureManager_TickTraining_PublishesTrainedEventOnCompletion()
    {
        var (bus, villagers, items, structures, entities, _) = CreateManagers();

        var training = CreateTrainingBuilding(trainingDuration: 0.5f);
        var pos = new GridPosRPG(5, 5);
        structures.AddProtoStructure(training, pos);

        var villager = CreateVillager();
        villager.Position = pos;
        villagers.AddVillager(villager);

        training.AcceptTrainee(villager);
        Assert.Equal(VillagerState.Training, villager.State);

        VillagerTrainingCompleteEvent? trainedEvent = null;
        bus.Subscribe<VillagerTrainingCompleteEvent>(e => trainedEvent = e);

        // Tick villager to complete training (VillagerSystem.Tick advances TickTraining)
        villagers.Tick(1f, items);

        // VillagerLogic sets State=Idle, now StructureManager detects and publishes
        structures.Tick(0.1f);

        Assert.NotNull(trainedEvent);
        Assert.Equal(villager.Id, trainedEvent.Value.VillagerId);
        Assert.Equal("Warrior", trainedEvent.Value.TrainedClass);
        Assert.Empty(training.CurrentTrainees);
    }

    [Fact]
    public void StructureManager_TickTraining_EjectsViaExitGate()
    {
        var (bus, villagers, items, structures, entities, _) = CreateManagers();

        var training = CreateTrainingBuilding(trainingDuration: 0.5f);
        var pos = new GridPosRPG(5, 5);
        structures.AddProtoStructure(training, pos);

        // Create exit gate linked to the training building
        var exitGatePos = new GridPosRPG(6, 5);
        var exitPathPos = new GridPosRPG(7, 5);
        var gate = new PathGateLogic(EntityId.Next())
        {
            IsActive = true,
            Position = exitGatePos,
            Facing = Direction.East
        };
        gate.LinkToStructure(training.Id, pos);
        entities.AddPathGate(gate);

        var segment = new PathSegmentLogic(EntityId.Next())
        {
            IsActive = true,
            Position = exitPathPos,
            Facing = Direction.East
        };
        entities.AddPathSegment(segment);

        var villager = CreateVillager();
        villager.Position = pos;
        villagers.AddVillager(villager);

        training.AcceptTrainee(villager);

        VillagerLeftBuildingEvent? leftEvent = null;
        bus.Subscribe<VillagerLeftBuildingEvent>(e => leftEvent = e);

        // Complete training
        villagers.Tick(1f, items);

        // StructureManager detects completion and ejects via exit gate
        structures.Tick(0.1f);

        Assert.Equal(VillagerState.Travelling, villager.State);
        Assert.NotNull(leftEvent);
        Assert.Equal(villager.Id, leftEvent.Value.VillagerId);
        Assert.Empty(training.CurrentTrainees);
    }

    [Fact]
    public void StructureManager_TickTraining_ParksVillagerWhenNoExitGate()
    {
        var (bus, villagers, items, structures, entities, _) = CreateManagers();

        var training = CreateTrainingBuilding(trainingDuration: 0.5f);
        var pos = new GridPosRPG(5, 5);
        structures.AddProtoStructure(training, pos);

        // No exit gate — villager should be parked in WaitingToExitIds

        var villager = CreateVillager();
        villager.Position = pos;
        villagers.AddVillager(villager);

        training.AcceptTrainee(villager);

        VillagerTrainingCompleteEvent? trainedEvent = null;
        bus.Subscribe<VillagerTrainingCompleteEvent>(e => trainedEvent = e);

        // Complete training
        villagers.Tick(1f, items);
        structures.Tick(0.1f);

        Assert.NotNull(trainedEvent);
        Assert.Empty(training.CurrentTrainees);

        // Villager should be parked waiting for exit
        Assert.Equal(VillagerState.EnteringBuilding, villager.State);
        Assert.Contains(villager.Id, training.WaitingToExitIds);
    }

    [Fact]
    public void StructureManager_TickTraining_HandlesMultipleTrainees()
    {
        var (bus, villagers, items, structures, entities, _) = CreateManagers();

        var training = CreateTrainingBuilding(maxTrainees: 3, trainingDuration: 0.5f);
        var pos = new GridPosRPG(5, 5);
        structures.AddProtoStructure(training, pos);

        var v1 = CreateVillager(name: "V1");
        var v2 = CreateVillager(name: "V2");
        var v3 = CreateVillager(name: "V3");
        v1.Position = pos;
        v2.Position = pos;
        v3.Position = pos;
        villagers.AddVillager(v1);
        villagers.AddVillager(v2);
        villagers.AddVillager(v3);

        training.AcceptTrainee(v1);
        training.AcceptTrainee(v2);
        training.AcceptTrainee(v3);
        Assert.Equal(3, training.CurrentTrainees.Count);

        int trainedCount = 0;
        bus.Subscribe<VillagerTrainingCompleteEvent>(_ => trainedCount++);

        // Complete all training
        villagers.Tick(1f, items);
        structures.Tick(0.1f);

        Assert.Equal(3, trainedCount);
        Assert.Empty(training.CurrentTrainees);
    }

    [Fact]
    public void StructureManager_TickTraining_ClearsCurrentActivity()
    {
        var (bus, villagers, items, structures, entities, _) = CreateManagers();

        var training = CreateTrainingBuilding(trainingDuration: 0.5f);
        var pos = new GridPosRPG(5, 5);
        structures.AddProtoStructure(training, pos);

        var villager = CreateVillager();
        villager.Position = pos;
        villager.CurrentActivity = "Training Warrior";
        villagers.AddVillager(villager);

        training.AcceptTrainee(villager);

        // Complete training
        villagers.Tick(1f, items);
        structures.Tick(0.1f);

        Assert.Null(villager.CurrentActivity);
    }

    [Fact]
    public void StructureManager_TickTraining_DoesNotEjectWhileStillTraining()
    {
        var (bus, villagers, items, structures, entities, _) = CreateManagers();

        var training = CreateTrainingBuilding(trainingDuration: 10f);
        var pos = new GridPosRPG(5, 5);
        structures.AddProtoStructure(training, pos);

        var villager = CreateVillager();
        villager.Position = pos;
        villagers.AddVillager(villager);

        training.AcceptTrainee(villager);

        VillagerTrainingCompleteEvent? trainedEvent = null;
        bus.Subscribe<VillagerTrainingCompleteEvent>(e => trainedEvent = e);

        // Tick only 1 second — training needs 10 seconds
        villagers.Tick(1f, items);
        structures.Tick(0.1f);

        Assert.Null(trainedEvent);
        Assert.Single(training.CurrentTrainees);
        Assert.Equal(VillagerState.Training, villager.State);
    }

    [Fact]
    public void StructureManager_TickTraining_RemovedVillagerIsCleanedUp()
    {
        var (bus, villagers, items, structures, entities, _) = CreateManagers();

        var training = CreateTrainingBuilding(trainingDuration: 10f);
        var pos = new GridPosRPG(5, 5);
        structures.AddProtoStructure(training, pos);

        var villager = CreateVillager();
        villager.Position = pos;
        villagers.AddVillager(villager);

        training.AcceptTrainee(villager);
        Assert.Single(training.CurrentTrainees);

        // Remove the villager from the system entirely (simulating death/despawn)
        villagers.RemoveVillager(villager.Id);

        // StructureManager should clean up the dangling trainee reference
        structures.Tick(0.1f);

        Assert.Empty(training.CurrentTrainees);
    }
}
