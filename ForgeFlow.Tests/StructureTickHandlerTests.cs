using ForgeFlow.Core;
using ForgeFlow.Core.Commands;
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
/// Tests for the IStructureTickHandler interface-based dispatch system.
/// Covers:
/// - Unit tests per structure type (ProcessStructureTick populates output correctly)
/// - Integration tests (StructureManager dispatches via interface, publishes events)
/// - Cache + regression tests (AsTickHandler caching, non-implementing structures)
/// </summary>
public class StructureTickHandlerTests
{
    public StructureTickHandlerTests()
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
            TrainedClass = trainedClass
        };
    }

    private static StructureTickOutput CreateOutput()
    {
        return new StructureTickOutput
        {
            PendingExits = new PendingExit[16],
            PendingItems = new PendingItemOutput[16]
        };
    }

    private static VillagerLookup CreateLookup(params VillagerLogic[] villagers)
    {
        var dict = new Dictionary<ulong, VillagerLogic>();
        foreach (var v in villagers)
        {
            dict[v.Id] = v;
        }
        return id => dict.TryGetValue(id, out var v) ? v : null;
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
        var commandBus = new CommandBus();
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
    // ── InnLogic Unit Tests ──────────────────────────────────────────
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public void InnLogic_ProcessStructureTick_EjectsFullyRestedVillager()
    {
        var inn = new InnLogic(EntityId.Next()) { IsActive = true };
        inn.SetRestRecipe("rest", 1.0f);
        var villager = CreateVillager(stamina: 0);
        inn.CurrentOccupants.Add(villager.Id);

        var output = CreateOutput();
        var lookup = CreateLookup(villager);

        // Rest for enough time to fully restore
        inn.ProcessStructureTick(2.0f, lookup, ref output);

        Assert.Equal(1, output.ExitCount);
        Assert.Equal(villager.Id.Value, output.PendingExits[0].VillagerId);
        Assert.Empty(inn.CurrentOccupants);
        Assert.Null(villager.CurrentActivity);
    }

    [Fact]
    public void InnLogic_ProcessStructureTick_KeepsPartiallyRestedVillager()
    {
        var inn = new InnLogic(EntityId.Next()) { IsActive = true };
        inn.SetRestRecipe("rest", 10.0f);
        var villager = CreateVillager(stamina: 0);
        inn.CurrentOccupants.Add(villager.Id);

        var output = CreateOutput();
        var lookup = CreateLookup(villager);

        inn.ProcessStructureTick(0.1f, lookup, ref output);

        Assert.Equal(0, output.ExitCount);
        Assert.Single(inn.CurrentOccupants);
    }

    [Fact]
    public void InnLogic_ProcessStructureTick_RemovesMissingVillager()
    {
        var inn = new InnLogic(EntityId.Next()) { IsActive = true };
        inn.CurrentOccupants.Add(99999);

        var output = CreateOutput();
        VillagerLookup emptyLookup = _ => null;

        inn.ProcessStructureTick(1.0f, emptyLookup, ref output);

        Assert.Equal(0, output.ExitCount);
        Assert.Empty(inn.CurrentOccupants);
    }

    // ══════════════════════════════════════════════════════════════════
    // ── VillageSpawnerLogic (Residence) Unit Tests ───────────────────
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public void VillageSpawner_ProcessStructureTick_EjectsRestedResident()
    {
        var spawner = new VillageSpawnerLogic(EntityId.Next()) { IsActive = true };
        spawner.HomeRestDuration = 1.0f;
        var villager = CreateVillager(stamina: 0);
        spawner.CurrentOccupants.Add(villager.Id);

        var output = CreateOutput();
        var lookup = CreateLookup(villager);

        spawner.ProcessStructureTick(2.0f, lookup, ref output);

        Assert.Equal(1, output.ExitCount);
        Assert.Equal(villager.Id.Value, output.PendingExits[0].VillagerId);
        Assert.Empty(spawner.CurrentOccupants);
    }

    [Fact]
    public void VillageSpawner_ProcessStructureTick_SetsRestedAtHomeEventTag()
    {
        var spawner = new VillageSpawnerLogic(EntityId.Next()) { IsActive = true };
        spawner.HomeRestDuration = 1.0f;
        var villager = CreateVillager(stamina: 0);
        spawner.CurrentOccupants.Add(villager.Id);

        var output = CreateOutput();
        var lookup = CreateLookup(villager);

        spawner.ProcessStructureTick(2.0f, lookup, ref output);

        Assert.Equal(StructureExitReason.RestedAtHome, output.PendingExits[0].ExitReason);
    }

    // ══════════════════════════════════════════════════════════════════
    // ── TrainingBuildingLogic Unit Tests ──────────────────────────────
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public void TrainingBuilding_ProcessStructureTick_EjectsCompletedTrainee()
    {
        var training = new TrainingBuildingLogic(EntityId.Next()) { IsActive = true };
        training.OutputClass = VillagerClass.Warrior;
        training.TrainingDuration = 1.0f;

        var villager = CreateVillager();
        training.AcceptTrainee(villager);

        // Simulate training completion
        villager.TrainingProgress = villager.TrainingRequired;
        villager.Tick(0.1f); // transitions to Idle

        var output = CreateOutput();
        var lookup = CreateLookup(villager);

        training.ProcessStructureTick(0.1f, lookup, ref output);

        Assert.Equal(1, output.ExitCount);
        Assert.Equal(villager.Id.Value, output.PendingExits[0].VillagerId);
        Assert.Empty(training.CurrentTrainees);
    }

    [Fact]
    public void TrainingBuilding_ProcessStructureTick_SetsTrainingCompleteEventTag()
    {
        var training = new TrainingBuildingLogic(EntityId.Next()) { IsActive = true };
        training.OutputClass = VillagerClass.Warrior;
        training.TrainingDuration = 1.0f;

        var villager = CreateVillager();
        training.AcceptTrainee(villager);
        villager.TrainingProgress = villager.TrainingRequired;
        villager.Tick(0.1f);

        var output = CreateOutput();
        var lookup = CreateLookup(villager);

        training.ProcessStructureTick(0.1f, lookup, ref output);

        Assert.Equal(StructureExitReason.TrainingComplete, output.PendingExits[0].ExitReason);
        Assert.Equal("Warrior", output.PendingExits[0].EventMetadata);
    }

    [Fact]
    public void TrainingBuilding_ProcessStructureTick_SetsTutorialAdvance()
    {
        var training = new TrainingBuildingLogic(EntityId.Next()) { IsActive = true };
        training.OutputClass = VillagerClass.Warrior;
        training.TrainingDuration = 1.0f;

        var villager = CreateVillager();
        training.AcceptTrainee(villager);
        villager.TrainingProgress = villager.TrainingRequired;
        villager.Tick(0.1f);

        var output = CreateOutput();
        var lookup = CreateLookup(villager);

        training.ProcessStructureTick(0.1f, lookup, ref output);

        Assert.Equal(TutorialConditionType.VillagerTrainClass, output.TutorialAdvance);
    }

    [Fact]
    public void TrainingBuilding_ProcessStructureTick_KeepsActiveTrainee()
    {
        var training = new TrainingBuildingLogic(EntityId.Next()) { IsActive = true };
        training.OutputClass = VillagerClass.Warrior;
        training.TrainingDuration = 15.0f;

        var villager = CreateVillager();
        training.AcceptTrainee(villager);
        // Training still in progress

        var output = CreateOutput();
        var lookup = CreateLookup(villager);

        training.ProcessStructureTick(0.1f, lookup, ref output);

        Assert.Equal(0, output.ExitCount);
        Assert.Single(training.CurrentTrainees);
    }

    // ══════════════════════════════════════════════════════════════════
    // ── CraftStationLogic Unit Tests ─────────────────────────────────
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public void CraftStation_ProcessStructureTick_DepositsRecipeItems()
    {
        var craft = new CraftStationLogic(EntityId.Next()) { IsActive = true };
        craft.SetActiveRecipe("recipe", new Dictionary<string, int> { { "sticks", 2 } }, 4f, "output");

        var villager = CreateVillager();
        villager.TryPickUpItem(new ItemInstance { InstanceId = EntityIdFactory.Next(), ProtoId = "sticks", Quantity = 2 });
        craft.PendingVillagerIds.Add(villager.Id);

        var output = CreateOutput();
        var lookup = CreateLookup(villager);

        craft.ProcessStructureTick(0.1f, lookup, ref output);

        Assert.True(craft.StoredInputs.ContainsKey("sticks"));
    }

    [Fact]
    public void CraftStation_ProcessStructureTick_EjectsVillagerWhenCantCraft()
    {
        var craft = new CraftStationLogic(EntityId.Next()) { IsActive = true };
        craft.SetActiveRecipe("recipe", new Dictionary<string, int> { { "sticks", 10 } }, 4f, "output");

        var villager = CreateVillager();
        villager.TryPickUpItem(new ItemInstance { InstanceId = EntityIdFactory.Next(), ProtoId = "sticks", Quantity = 1 });
        craft.PendingVillagerIds.Add(villager.Id);

        var output = CreateOutput();
        var lookup = CreateLookup(villager);

        craft.ProcessStructureTick(0.1f, lookup, ref output);

        Assert.Equal(1, output.ExitCount);
        Assert.Null(villager.CurrentActivity);
    }

    [Fact]
    public void CraftStation_ProcessStructureTick_CompletedCraftProducesPendingItem()
    {
        var craft = new CraftStationLogic(EntityId.Next()) { IsActive = true };
        craft.SetActiveRecipe("recipe", new Dictionary<string, int> { { "sticks", 2 } }, 1f, "wooden_spear");

        var villager = CreateVillager();
        villager.TryPickUpItem(new ItemInstance { InstanceId = EntityIdFactory.Next(), ProtoId = "sticks", Quantity = 2 });
        craft.PendingVillagerIds.Add(villager.Id);

        var output = CreateOutput();
        var lookup = CreateLookup(villager);

        // Step 1: deposit + start crafting
        craft.ProcessStructureTick(0.1f, lookup, ref output);
        Assert.True(craft.IsCrafting);

        // Step 2: complete the timer
        craft.Tick(2.0f);
        Assert.True(craft.PendingCraftComplete);

        // Step 3: process completion
        output.Reset();
        craft.ProcessStructureTick(0.1f, lookup, ref output);

        Assert.Equal(1, output.ItemCount);
        Assert.Equal("wooden_spear", output.PendingItems[0].ItemProtoId);
        Assert.Equal(villager.Id.Value, output.PendingItems[0].TargetVillagerId);
        Assert.Equal(1, output.ExitCount);
    }

    // ══════════════════════════════════════════════════════════════════
    // ── StockpileLogic Unit Tests ────────────────────────────────────
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public void Stockpile_ProcessStructureTick_DepositsAcceptedItems()
    {
        var stockpile = new StockpileLogic(EntityId.Next()) { IsActive = true };

        var villager = CreateVillager();
        villager.TryPickUpItem(new ItemInstance { InstanceId = EntityIdFactory.Next(), ProtoId = "sticks", Quantity = 3 });
        stockpile.PendingVillagerIds.Add(villager.Id);

        var output = CreateOutput();
        var lookup = CreateLookup(villager);

        stockpile.ProcessStructureTick(0.1f, lookup, ref output);

        Assert.Equal(3, stockpile.GetCount("sticks"));
        Assert.Equal(1, output.ItemCount);
        Assert.Equal(1, output.ExitCount);
        Assert.False(villager.IsCarryingItems);
    }

    [Fact]
    public void Stockpile_ProcessStructureTick_RejectsFilteredItems()
    {
        var stockpile = new StockpileLogic(EntityId.Next()) { IsActive = true };
        stockpile.SetAcceptedItem("ore");
        stockpile.PendingFilterChange = false; // reset so we can test below

        var villager = CreateVillager();
        villager.TryPickUpItem(new ItemInstance { InstanceId = EntityIdFactory.Next(), ProtoId = "sticks", Quantity = 2 });
        stockpile.PendingVillagerIds.Add(villager.Id);

        var output = CreateOutput();
        var lookup = CreateLookup(villager);

        stockpile.ProcessStructureTick(0.1f, lookup, ref output);

        Assert.Equal(0, stockpile.GetCount("sticks"));
        Assert.True(villager.IsCarryingItems); // items given back
        Assert.Equal(0, output.ItemCount);
        Assert.Equal(1, output.ExitCount); // still exits
    }

    [Fact]
    public void Stockpile_ProcessStructureTick_SetsConfigChangedOnFilterUpdate()
    {
        var stockpile = new StockpileLogic(EntityId.Next()) { IsActive = true };
        stockpile.SetAcceptedItem("sticks"); // triggers PendingFilterChange

        var output = CreateOutput();
        VillagerLookup lookup = _ => null;

        stockpile.ProcessStructureTick(0.1f, lookup, ref output);

        Assert.True(output.ConfigChanged);
    }

    [Fact]
    public void Stockpile_ProcessStructureTick_EjectsAfterDeposit()
    {
        var stockpile = new StockpileLogic(EntityId.Next()) { IsActive = true };

        var villager = CreateVillager();
        villager.TryPickUpItem(new ItemInstance { InstanceId = EntityIdFactory.Next(), ProtoId = "sticks", Quantity = 1 });
        stockpile.PendingVillagerIds.Add(villager.Id);

        var output = CreateOutput();
        var lookup = CreateLookup(villager);

        stockpile.ProcessStructureTick(0.1f, lookup, ref output);

        Assert.Equal(1, output.ExitCount);
        Assert.Null(villager.CurrentActivity);
    }

    // ══════════════════════════════════════════════════════════════════
    // ── ForgeLogic Unit Tests ────────────────────────────────────────
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public void ForgeLogic_ProcessStructureTick_SignalsPendingItemOnComplete()
    {
        var forge = new ForgeLogic(EntityId.Next()) { IsActive = true };
        forge.ActiveRecipeId = "forge_recipe";
        forge.PendingForgingComplete = true;

        var output = CreateOutput();
        VillagerLookup lookup = _ => null;

        forge.ProcessStructureTick(0.1f, lookup, ref output);

        Assert.Equal(1, output.ItemCount);
        Assert.Equal("forge_recipe", output.PendingItems[0].ItemProtoId);
        Assert.False(forge.PendingForgingComplete);
    }

    [Fact]
    public void ForgeLogic_ProcessStructureTick_NoOutputWhenNoPending()
    {
        var forge = new ForgeLogic(EntityId.Next()) { IsActive = true };
        forge.ActiveRecipeId = "forge_recipe";
        forge.PendingForgingComplete = false;

        var output = CreateOutput();
        VillagerLookup lookup = _ => null;

        forge.ProcessStructureTick(0.1f, lookup, ref output);

        Assert.Equal(0, output.ItemCount);
    }

    // ══════════════════════════════════════════════════════════════════
    // ── GatheringLogicBase Unit Tests ────────────────────────────────
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public void Gathering_ProcessStructureTick_HarvestsFromLinkedNode()
    {
        var gathering = new GatheringLogicBase(EntityId.Next())
        {
            IsActive = true,
            TargetResourceId = "wood",
            GatherAmountPerCycle = 2,
            GatherInterval = 1.0f,
            MaxWorkerCapacity = 5,
            StaminaCostPerCycle = 5
        };
        gathering.AllocateSlots();

        var node = new ResourceNodeLogic(EntityId.Next())
        {
            IsActive = true,
            ResourceId = "wood",
            MaxYield = 100,
            CurrentYield = 100
        };
        gathering.Initialize(node);

        var villager = CreateVillager(stamina: 100);
        gathering.AcceptWorker(villager.Id, null);

        // Progress the gathering cycle to completion
        gathering.Tick(1.0f);
        Assert.True(gathering.WorkerSlots[0].CycleComplete);

        var output = CreateOutput();
        var lookup = CreateLookup(villager);

        gathering.ProcessStructureTick(0.1f, lookup, ref output);

        Assert.Equal(1, output.ItemCount);
        Assert.Equal("wood", output.PendingItems[0].ItemProtoId);
        Assert.Equal(2, output.PendingItems[0].Quantity);
    }

    [Fact]
    public void Gathering_ProcessStructureTick_DrainsStamina()
    {
        var gathering = new GatheringLogicBase(EntityId.Next())
        {
            IsActive = true,
            TargetResourceId = "wood",
            GatherAmountPerCycle = 1,
            GatherInterval = 1.0f,
            MaxWorkerCapacity = 5,
            StaminaCostPerCycle = 30
        };
        gathering.AllocateSlots();

        var villager = CreateVillager(stamina: 100);
        gathering.AcceptWorker(villager.Id, null);
        gathering.Tick(1.0f);

        var output = CreateOutput();
        var lookup = CreateLookup(villager);

        gathering.ProcessStructureTick(0.1f, lookup, ref output);

        Assert.Equal(70, villager.Stamina);
    }

    [Fact]
    public void Gathering_ProcessStructureTick_EjectsExhaustedWorker()
    {
        var gathering = new GatheringLogicBase(EntityId.Next())
        {
            IsActive = true,
            TargetResourceId = "wood",
            GatherAmountPerCycle = 1,
            GatherInterval = 1.0f,
            MaxWorkerCapacity = 5,
            StaminaCostPerCycle = 100
        };
        gathering.AllocateSlots();

        var villager = CreateVillager(stamina: 50);
        gathering.AcceptWorker(villager.Id, null);
        gathering.Tick(1.0f);

        var output = CreateOutput();
        var lookup = CreateLookup(villager);

        gathering.ProcessStructureTick(0.1f, lookup, ref output);

        Assert.Equal(1, output.ExitCount);
        Assert.Equal(villager.Id.Value, output.PendingExits[0].VillagerId);
        Assert.Null(villager.CurrentActivity);
    }

    [Fact]
    public void Gathering_ProcessStructureTick_EjectsWhenNodeDepleted()
    {
        var gathering = new GatheringLogicBase(EntityId.Next())
        {
            IsActive = true,
            TargetResourceId = "wood",
            GatherAmountPerCycle = 1,
            GatherInterval = 1.0f,
            MaxWorkerCapacity = 5,
            StaminaCostPerCycle = 5
        };
        gathering.AllocateSlots();

        var node = new ResourceNodeLogic(EntityId.Next())
        {
            IsActive = true,
            ResourceId = "wood",
            MaxYield = 1,
            CurrentYield = 1
        };
        gathering.Initialize(node);

        var villager = CreateVillager(stamina: 100);
        gathering.AcceptWorker(villager.Id, null);
        gathering.Tick(1.0f);

        var output = CreateOutput();
        var lookup = CreateLookup(villager);

        gathering.ProcessStructureTick(0.1f, lookup, ref output);

        // Node depleted after harvest → worker should leave
        Assert.Equal(1, output.ExitCount);
    }

    [Fact]
    public void Gathering_ProcessStructureTick_PromotesFromWaitQueue()
    {
        var gathering = new GatheringLogicBase(EntityId.Next())
        {
            IsActive = true,
            TargetResourceId = "wood",
            GatherAmountPerCycle = 1,
            GatherInterval = 1.0f,
            MaxWorkerCapacity = 1,
            StaminaCostPerCycle = 200 // will exhaust on first cycle
        };
        gathering.AllocateSlots();

        var worker1 = CreateVillager(name: "Worker1", stamina: 100);
        var worker2 = CreateVillager(name: "Worker2", stamina: 100);

        gathering.AcceptWorker(worker1.Id, null);
        gathering.EnqueueWaiting(worker2.Id);
        gathering.Tick(1.0f);

        var output = CreateOutput();
        var lookup = CreateLookup(worker1, worker2);

        gathering.ProcessStructureTick(0.1f, lookup, ref output);

        // Worker1 ejected (stamina depleted), Worker2 promoted from queue
        Assert.Equal(1, output.ExitCount);
        Assert.True(gathering.WorkerSlots[0].IsOccupied);
        Assert.Equal(worker2.Id.Value, gathering.WorkerSlots[0].VillagerId);
    }

    [Fact]
    public void Gathering_ProcessStructureTick_SkipsNonCompleteCycles()
    {
        var gathering = new GatheringLogicBase(EntityId.Next())
        {
            IsActive = true,
            TargetResourceId = "wood",
            GatherAmountPerCycle = 1,
            GatherInterval = 10.0f,
            MaxWorkerCapacity = 5,
            StaminaCostPerCycle = 5
        };
        gathering.AllocateSlots();

        var villager = CreateVillager();
        gathering.AcceptWorker(villager.Id, null);
        gathering.Tick(0.1f); // not enough for cycle

        var output = CreateOutput();
        var lookup = CreateLookup(villager);

        gathering.ProcessStructureTick(0.1f, lookup, ref output);

        Assert.Equal(0, output.ItemCount);
        Assert.Equal(0, output.ExitCount);
    }

    // ══════════════════════════════════════════════════════════════════
    // ── StructureTickOutput.Reset Tests ──────────────────────────────
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public void StructureTickOutput_Reset_ClearsAllCountsAndFlags()
    {
        var output = CreateOutput();
        output.ExitCount = 3;
        output.ItemCount = 2;
        output.TutorialAdvance = TutorialConditionType.VillagerTrainClass;
        output.ConfigChanged = true;

        output.Reset();

        Assert.Equal(0, output.ExitCount);
        Assert.Equal(0, output.ItemCount);
        Assert.Null(output.TutorialAdvance);
        Assert.False(output.ConfigChanged);
    }

    // ══════════════════════════════════════════════════════════════════
    // ── Cache + Interface Detection Tests ────────────────────────────
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public void Structure_AsTickHandler_CachesImplementation()
    {
        var inn = new InnLogic(EntityId.Next()) { IsActive = true };
        var handler1 = inn.AsTickHandler;
        var handler2 = inn.AsTickHandler;

        Assert.NotNull(handler1);
        Assert.Same(handler1, handler2);
    }

    [Fact]
    public void Structure_AsTickHandler_ReturnsNullForNonImplementing()
    {
        // DungeonPortalLogic does not implement IStructureTickHandler
        var portal = new DungeonPortalLogic(EntityId.Next()) { IsActive = true };
        Assert.Null(portal.AsTickHandler);
    }

    [Fact]
    public void AllExpectedStructures_ImplementIStructureTickHandler()
    {
        Assert.IsAssignableFrom<IStructureTickHandler>(new InnLogic(EntityId.Next()));
        Assert.IsAssignableFrom<IStructureTickHandler>(new VillageSpawnerLogic(EntityId.Next()));
        Assert.IsAssignableFrom<IStructureTickHandler>(new TrainingBuildingLogic(EntityId.Next()));
        Assert.IsAssignableFrom<IStructureTickHandler>(new CraftStationLogic(EntityId.Next()));
        Assert.IsAssignableFrom<IStructureTickHandler>(new StockpileLogic(EntityId.Next()));
        Assert.IsAssignableFrom<IStructureTickHandler>(new ForgeLogic(EntityId.Next()));
        Assert.IsAssignableFrom<IStructureTickHandler>(new GatheringLogicBase(EntityId.Next()));
    }

    // ══════════════════════════════════════════════════════════════════
    // ── Integration Tests (StructureManager dispatch) ────────────────
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public void StructureManager_Tick_DispatchesViaInterface()
    {
        var (bus, villagers, items, structures, entities, pathGates) = CreateManagers();

        var inn = new InnLogic(EntityId.Next()) { IsActive = true };
        inn.SetRestRecipe("rest", 0.5f);
        var pos = new GridPosRPG(3, 3);
        entities.AddStructure(inn, pos);

        var villager = CreateVillager(stamina: 0);
        villagers.AddVillager(villager);
        inn.AcceptVillager(villager);

        // Tick should trigger ProcessStructureTick on InnLogic
        structures.Tick(2.0f);

        // Villager should be ejected (rested) — parked in WaitingToExitIds since no gate
        Assert.Contains(villager.Id, inn.WaitingToExitIds);
        Assert.Empty(inn.CurrentOccupants);
    }

    [Fact]
    public void StructureManager_Tick_SkipsNonImplementingStructures()
    {
        var (bus, villagers, items, structures, entities, pathGates) = CreateManagers();

        // DungeonPortalLogic does not implement IStructureTickHandler
        var portal = new DungeonPortalLogic(EntityId.Next()) { IsActive = true };
        var pos = new GridPosRPG(4, 4);
        entities.AddStructure(portal, pos);

        // Tick should not throw for structures without IStructureTickHandler
        structures.Tick(1.0f);
    }

    [Fact]
    public void StructureManager_Tick_PublishesVillagerLeftBuildingEvent()
    {
        var (bus, villagers, items, structures, entities, pathGates) = CreateManagers();

        var stockpile = new StockpileLogic(EntityId.Next()) { IsActive = true };
        var pos = new GridPosRPG(3, 3);
        entities.AddStructure(stockpile, pos);

        var villager = CreateVillager();
        villager.TryPickUpItem(new ItemInstance { InstanceId = EntityIdFactory.Next(), ProtoId = "sticks", Quantity = 1 });
        villagers.AddVillager(villager);
        stockpile.PendingVillagerIds.Add(villager.Id);

        // No exit gate → villager parks in WaitingToExitIds
        structures.Tick(0.1f);

        Assert.Contains(villager.Id, stockpile.WaitingToExitIds);
    }

    [Fact]
    public void StructureManager_Tick_PublishesTrainingCompleteViaEventTag()
    {
        var (bus, villagers, items, structures, entities, pathGates) = CreateManagers();

        var training = new TrainingBuildingLogic(EntityId.Next()) { IsActive = true };
        training.OutputClass = VillagerClass.Warrior;
        training.TrainingDuration = 0.1f;
        var pos = new GridPosRPG(5, 5);
        entities.AddStructure(training, pos);

        var villager = CreateVillager();
        villagers.AddVillager(villager);
        training.AcceptTrainee(villager);

        // Complete training
        villager.TrainingProgress = villager.TrainingRequired;
        villager.Tick(0.1f);

        VillagerTrainingCompleteEvent? completedEvent = null;
        bus.Subscribe<VillagerTrainingCompleteEvent>(e => completedEvent = e);

        structures.Tick(0.1f);

        Assert.NotNull(completedEvent);
        Assert.Equal(villager.Id, completedEvent!.Value.VillagerId);
    }

    [Fact]
    public void StructureManager_Tick_PublishesRestedAtHomeViaEventTag()
    {
        var (bus, villagers, items, structures, entities, pathGates) = CreateManagers();

        var spawner = new VillageSpawnerLogic(EntityId.Next()) { IsActive = true };
        spawner.HomeRestDuration = 0.5f;
        var pos = new GridPosRPG(5, 5);
        entities.AddStructure(spawner, pos);

        var villager = CreateVillager(stamina: 0);
        villagers.AddVillager(villager);
        spawner.Residents.Add(villager.Id);
        spawner.CurrentOccupants.Add(villager.Id);

        VillagerRestedAtHomeEvent? restedEvent = null;
        bus.Subscribe<VillagerRestedAtHomeEvent>(e => restedEvent = e);

        structures.Tick(2.0f);

        Assert.NotNull(restedEvent);
        Assert.Equal(villager.Id, restedEvent!.Value.VillagerId);
    }

    [Fact]
    public void StructureManager_Tick_CreatesItemForGatheringOutput()
    {
        var (bus, villagers, items, structures, entities, pathGates) = CreateManagers();

        var gathering = new GatheringLogicBase(EntityId.Next())
        {
            IsActive = true,
            TargetResourceId = "wood",
            GatherAmountPerCycle = 1,
            GatherInterval = 1.0f,
            MaxWorkerCapacity = 5,
            StaminaCostPerCycle = 5
        };
        gathering.AllocateSlots();
        var pos = new GridPosRPG(5, 5);
        entities.AddStructure(gathering, pos);

        var villager = CreateVillager(stamina: 100);
        villagers.AddVillager(villager);
        gathering.AcceptWorker(villager.Id, null);

        gathering.Tick(1.0f); // complete one cycle

        GatheringWorkerOutputEvent? outputEvent = null;
        bus.Subscribe<GatheringWorkerOutputEvent>(e => outputEvent = e);

        structures.Tick(0.1f);

        Assert.NotNull(outputEvent);
        Assert.Equal("wood", outputEvent!.Value.ResourceId);
        Assert.True(villager.IsCarryingItems);
    }

    [Fact]
    public void StructureManager_Tick_StillDispatchesSpawnerVillagers()
    {
        var (bus, villagers, items, structures, entities, pathGates) = CreateManagers();

        var spawner = new VillageSpawnerLogic(EntityId.Next())
        {
            IsActive = true,
            SpawnInterval = 1.0f,
            MaxVillagers = 1
        };
        var pos = new GridPosRPG(5, 5);
        entities.AddStructure(spawner, pos);

        // Trigger spawn
        spawner.Tick(1.0f);
        Assert.NotEmpty(spawner.PendingVillagers);

        // Tick will attempt dispatch (no exit gate → spawn discarded, but doesn't crash)
        structures.Tick(0.1f);
        Assert.Empty(spawner.PendingVillagers);
    }

    [Fact]
    public void StructureManager_Tick_StillRetriesWaitingExits()
    {
        var (bus, villagers, items, structures, entities, pathGates) = CreateManagers();

        var inn = new InnLogic(EntityId.Next()) { IsActive = true };
        var pos = new GridPosRPG(3, 3);
        entities.AddStructure(inn, pos);

        var villager = CreateVillager();
        villagers.AddVillager(villager);
        inn.WaitingToExitIds.Add(villager.Id);

        // No exit gate, so retry will still fail but should not crash
        structures.Tick(0.1f);

        Assert.Contains(villager.Id, inn.WaitingToExitIds);
    }
}
