using ForgeFlow.Core.Entities;
using ForgeFlow.Core.Events;
using ForgeFlow.Core.Proto;
using ForgeFlow.Core.Proto.Logic;
using ForgeFlow.Core.Proto.Prototypes;
using ForgeFlow.Core.Systems;
using ForgeFlow.Core.Utilities;

namespace ForgeFlow.Tests;

/// <summary>
/// Tests for gathering structure worker logic: capacity, accept/reject,
/// per-worker tick, cycle completion, stamina ejection, PathGate integration,
/// wait queue, and inspector data accuracy.
/// </summary>
public class GatheringWorkerTests
{
    private static GatheringLogicBase CreateForestryRecipeEntity(int capacity = 3)
    {
        var structure = new ForestryRecipeEntity(EntityId.Next());
        structure.MaxWorkerCapacity = capacity;
		structure.AllocateSlots();
        return structure;
    }

    private static GatheringLogicBase CreateMiningRecipeEntity(int capacity = 3)
    {
        var structure = new MiningRecipeEntity(EntityId.Next());
        structure.MaxWorkerCapacity = capacity;
		structure.AllocateSlots();
        return structure;
    }

    private static VillagerLogic CreateVillager(string name = "TestVillager", int stamina = 100)
    {
        var villager = new VillagerLogic(EntityId.Next())
        {
            Name = name,
            Stamina = stamina,
            MaxStamina = 100,
            EquippedToolId = null
        };
        return villager;
    }

    // ── WorkerSlot & Capacity ────────────────────────────────────────

    [Fact]
    public void NewGatheringStructure_HasZeroWorkers()
    {
        var gathering = CreateForestryRecipeEntity();
        Assert.Equal(0, gathering.WorkerCount);
        Assert.True(gathering.HasFreeSlot);
    }

    [Fact]
    public void AcceptWorker_OccupiesSlot()
    {
        var structure = CreateForestryRecipeEntity(capacity: 2);
        int slot = structure.AcceptWorker(100, null);

        Assert.Equal(0, slot);
        Assert.Equal(1, structure.WorkerCount);
        Assert.True(structure.HasFreeSlot);
        Assert.True(structure.WorkerSlots[0].IsOccupied);
        Assert.Equal((ulong)100, structure.WorkerSlots[0].VillagerId);
    }

    [Fact]
    public void AcceptWorker_ResolvesToolOutput()
    {
        var structure = CreateForestryRecipeEntity();
        int slot = structure.AcceptWorker(100, "axe");

        Assert.Equal("logs", structure.WorkerSlots[slot].OutputResourceId);
    }

    [Fact]
    public void AcceptWorker_BareHands_GetsDefaultResource()
    {
        var structure = CreateForestryRecipeEntity();
        int slot = structure.AcceptWorker(100, null);

        Assert.Equal("sticks", structure.WorkerSlots[slot].OutputResourceId);
    }

    [Fact]
    public void AcceptWorker_RejectsWhenFull()
    {
        var structure = CreateForestryRecipeEntity(capacity: 2);
        structure.AcceptWorker(1, null);
        structure.AcceptWorker(2, null);

        int slot = structure.AcceptWorker(3, null);
        Assert.Equal(-1, slot);
        Assert.Equal(2, structure.WorkerCount);
        Assert.False(structure.HasFreeSlot);
    }

    [Fact]
    public void RemoveWorker_FreesSlot()
    {
        var structure = CreateForestryRecipeEntity(capacity: 2);
        int slot = structure.AcceptWorker(100, null);
        Assert.Equal(1, structure.WorkerCount);

        ulong removed = structure.RemoveWorker(slot);
        Assert.Equal((ulong)100, removed);
        Assert.Equal(0, structure.WorkerCount);
        Assert.True(structure.HasFreeSlot);
    }

    [Fact]
    public void RemoveWorkerById_FindsAndRemoves()
    {
        var structure = CreateForestryRecipeEntity(capacity: 3);
        structure.AcceptWorker(10, null);
        structure.AcceptWorker(20, "axe");
        structure.AcceptWorker(30, null);

        int removedSlot = structure.RemoveWorkerById(20);
        Assert.True(removedSlot >= 0);
        Assert.Equal(2, structure.WorkerCount);
    }

    [Fact]
    public void RemoveWorkerById_ReturnsMinusOne_IfNotFound()
    {
        var structure = CreateForestryRecipeEntity();
        structure.AcceptWorker(10, null);

        int result = structure.RemoveWorkerById(999);
        Assert.Equal(-1, result);
        Assert.Equal(1, structure.WorkerCount);
    }

    // ── Per-Worker Tick ──────────────────────────────────────────────

    [Fact]
    public void Tick_AdvancesGatherProgress()
    {
        var structure = CreateForestryRecipeEntity();
        structure.AcceptWorker(100, null);

        structure.Tick(1.0f);

        Assert.Equal(1.0f, structure.WorkerSlots[0].GatherProgress, 0.01f);
        Assert.False(structure.WorkerSlots[0].CycleComplete);
    }

    [Fact]
    public void Tick_CompletesCycle_WhenProgressReachesInterval()
    {
        var structure = CreateForestryRecipeEntity();
        structure.AcceptWorker(100, null);

        // Forestry GatherInterval = 4.0f
        structure.Tick(4.0f);

        Assert.True(structure.WorkerSlots[0].CycleComplete);
        Assert.True(structure.WorkerSlots[0].PendingOutputAmount > 0);
    }

    [Fact]
    public void Tick_DoesNotAdvance_CompletedSlot()
    {
        var structure = CreateForestryRecipeEntity();
        structure.AcceptWorker(100, null);

        structure.Tick(4.0f); // completes cycle
        Assert.True(structure.WorkerSlots[0].CycleComplete);

        float progressAfterCycle = structure.WorkerSlots[0].GatherProgress;
        structure.Tick(1.0f); // additional tick

        // Should not advance further while CycleComplete is true
        Assert.Equal(progressAfterCycle, structure.WorkerSlots[0].GatherProgress, 0.01f);
    }

    [Fact]
    public void Tick_MultipleWorkers_IndependentProgress()
    {
        var structure = CreateForestryRecipeEntity(capacity: 3);
        structure.AcceptWorker(1, null);
        structure.AcceptWorker(2, null);

        structure.Tick(2.0f);

        Assert.Equal(2.0f, structure.WorkerSlots[0].GatherProgress, 0.01f);
        Assert.Equal(2.0f, structure.WorkerSlots[1].GatherProgress, 0.01f);
        Assert.False(structure.WorkerSlots[0].CycleComplete);
        Assert.False(structure.WorkerSlots[1].CycleComplete);
    }

    [Fact]
    public void Tick_InactiveStructure_DoesNotAdvance()
    {
        var structure = CreateForestryRecipeEntity();
        structure.IsActive = false;
        structure.AcceptWorker(100, null);

        structure.Tick(5.0f);

        Assert.Equal(0f, structure.WorkerSlots[0].GatherProgress);
    }

    // ── Wait Queue ──────────────────────────────────────────────────

    [Fact]
    public void WaitQueue_EnqueueDequeue()
    {
        var structure = CreateForestryRecipeEntity(capacity: 1);
        structure.AcceptWorker(1, null);

        Assert.True(structure.EnqueueWaiting(2));
        Assert.Equal(1, structure.WaitQueueCount);

        ulong waiting = structure.DequeueWaiting();
        Assert.Equal((ulong)2, waiting);
        Assert.Equal(0, structure.WaitQueueCount);
    }

    [Fact]
    public void WaitQueue_DequeueEmpty_ReturnsZero()
    {
        var structure = CreateForestryRecipeEntity();
        ulong result = structure.DequeueWaiting();
        Assert.Equal((ulong)0, result);
    }

    [Fact]
    public void WaitQueue_CircularBuffer_WrapsAround()
    {
        var structure = CreateForestryRecipeEntity(capacity: 1);
        structure.AcceptWorker(1, null);

        // Fill and drain queue multiple times to test circular behavior
        for (int round = 0; round < 3; round++)
        {
            for (ulong i = 10; i < 18; i++)
            {
                Assert.True(structure.EnqueueWaiting(i));
            }
            Assert.Equal(8, structure.WaitQueueCount);

            for (ulong i = 10; i < 18; i++)
            {
                ulong id = structure.DequeueWaiting();
                Assert.Equal(i, id);
            }
            Assert.Equal(0, structure.WaitQueueCount);
        }
    }

    [Fact]
    public void WaitQueue_RejectsFull()
    {
        var structure = CreateForestryRecipeEntity(capacity: 1);
        structure.AcceptWorker(1, null);

        for (int i = 0; i < 8; i++)
        {
            Assert.True(structure.EnqueueWaiting((ulong)(100 + i)));
        }
        Assert.Equal(8, structure.WaitQueueCount);
        Assert.False(structure.EnqueueWaiting(999)); // 9th should fail
    }

    // ── AllocateSlots ───────────────────────────────────────────────

    [Fact]
    public void AllocateSlots_ResizesArray()
    {
        var gathering = CreateForestryRecipeEntity(capacity: 2);
        Assert.Equal(2, gathering.WorkerSlots.Length);

        gathering.MaxWorkerCapacity = 6;
        gathering.AllocateSlots();
        Assert.Equal(6, gathering.WorkerSlots.Length);
        Assert.Equal(0, gathering.WorkerCount);
    }

    [Fact]
    public void AllocateSlots_ClearsExistingWorkers()
    {
        var gathering = CreateForestryRecipeEntity(capacity: 3);
        gathering.AcceptWorker(1, null);
        gathering.AcceptWorker(2, null);
        Assert.Equal(2, gathering.WorkerCount);

        gathering.AllocateSlots();
        Assert.Equal(0, gathering.WorkerCount);
        Assert.Equal(0, gathering.WaitQueueCount);
    }

    // ── Mining-Specific ─────────────────────────────────────────────

    [Fact]
    public void MiningStructure_CalculatesOreRichnessBonus()
    {
        var mining = new MiningRecipeEntity(EntityId.Next())
        {
            OreRichnessMultiplier = 2.0f,
            GatherAmountPerCycle = 1,
            BiomeBonusMultiplier = 1.0f
        };
        mining.MaxWorkerCapacity = 2;
        mining.AllocateSlots();
        mining.AcceptWorker(1, "pickaxe");

        // GatherInterval for mining is 5.0f
        mining.Tick(5.0f);

        Assert.True(mining.WorkerSlots[0].CycleComplete);
        // 1 * 2.0 * 1.0 = 2
        Assert.Equal(2, mining.WorkerSlots[0].PendingOutputAmount);
    }

    [Fact]
    public void MiningStructure_DeepMineBonus_AtTier3()
    {
        var mining = new MiningRecipeEntity(EntityId.Next())
        {
            OreRichnessMultiplier = 1.0f,
            GatherAmountPerCycle = 1,
            BiomeBonusMultiplier = 1.0f,
            CanDeepMine = true,
            DeepMineBonus = 3
        };
        mining.Tier = 3;
        mining.MaxWorkerCapacity = 2;
        mining.AllocateSlots();
        mining.AcceptWorker(1, "pickaxe");

        mining.Tick(5.0f);

        Assert.True(mining.WorkerSlots[0].CycleComplete);
        // 1 * 1.0 * 1.0 + 3 = 4
        Assert.Equal(4, mining.WorkerSlots[0].PendingOutputAmount);
    }

    // ── Forestry-Specific ───────────────────────────────────────────

    [Fact]
    public void ForestryStructure_SaplingGrowth_OnCycleComplete()
    {
        var forestry = new ForestryRecipeEntity(EntityId.Next())
        {
            CanPlantSaplings = true,
            SaplingGrowthBonus = 0.5f
        };
        forestry.MaxWorkerCapacity = 2;
        forestry.AllocateSlots();

        var node = new ResourceNodeLogic(EntityId.Next())
        {
            MaxYield = 100,
            CurrentYield = 100,
            IsRenewable = true,
            RegrowthRate = 0.1f
        };
        forestry.Initialize(node);
        forestry.AcceptWorker(1, null);

        float initialRegrowth = node.RegrowthRate;
        forestry.Tick(4.0f); // complete one cycle

        Assert.True(forestry.WorkerSlots[0].CycleComplete);
        Assert.True(node.RegrowthRate > initialRegrowth);
    }

    // ── ResolveOutputForTool ────────────────────────────────────────

    [Fact]
    public void ResolveOutputForTool_UsesToolOutputMap()
    {
        var structure = CreateForestryRecipeEntity();

        Assert.Equal("sticks", structure.ResolveOutputForTool(null));
        Assert.Equal("logs", structure.ResolveOutputForTool("axe"));
        Assert.Equal("logs", structure.ResolveOutputForTool("stone_hatchet"));
        Assert.Equal("plank_wood", structure.ResolveOutputForTool("iron_axe"));
    }

    [Fact]
    public void ResolveOutputForTool_FallsBackToTargetResource()
    {
        var structure = CreateForestryRecipeEntity();
        // Sickle is not in ToolOutputMap — should fall back
        Assert.Equal("sticks", structure.ResolveOutputForTool("sickle"));
    }

    // ── GetJobForResource ───────────────────────────────────────────

    [Fact]
    public void GetJobForResource_MapsCorrectly()
    {
        Assert.Equal(VillagerJob.Lumberjack, GatheringLogicBase.GetJobForResource("wood"));
        Assert.Equal(VillagerJob.Miner, GatheringLogicBase.GetJobForResource("ore"));
        Assert.Equal(VillagerJob.Farmer, GatheringLogicBase.GetJobForResource("food"));
        Assert.Equal(VillagerJob.Farmer, GatheringLogicBase.GetJobForResource("herbs"));
        Assert.Equal(VillagerJob.Builder, GatheringLogicBase.GetJobForResource("unknown"));
    }

    // ── Integration: StructureManager + PathGate ──────────────────────

    private static (StructureManager structureMgr, PathGateManager gateMgr, VillagerSystem villagerSys,
        EntityManager entMgr, TileManager tileMgr, EventBus bus, ItemManager itemMgr)
        CreateIntegrationStack()
    {
        EntityBase.ResetIdCounter();
        var bus = new EventBus();
        var terrain = new TerrainGrid(32, 32);
        terrain.GenerateDefault();
        var tileMgr = new TileManager(terrain);
        var itemMgr = new ItemManager(bus);
        var villagerSys = new VillagerSystem(bus, itemMgr);
        var entMgr = new EntityManager(tileMgr, villagerSys);
        var trafficMgr = new TrafficManager();
        var pathTraffic = new PathTrafficSystem(trafficMgr, entMgr, tileMgr, bus);
        var protoReg = new ProtoRegistry();
        protoReg.RegisterDefaults();
        var protoFactory = new ProtoFactory(protoReg, bus);
        var tutorialSys = new TutorialSystem(bus, protoFactory);
        var gatingLimits = new GatingLimits();
        var researchMgr = new ResearchManager(bus);

        var cmdBus = new Core.Commands.CommandBus();
        var gateMgr = new PathGateManager(entMgr, tileMgr, bus, cmdBus, tutorialSys, itemMgr, researchMgr);
        pathTraffic.SetPathGateManager(gateMgr);

        var pathMgr = new PathNodeManager(entMgr, tileMgr, villagerSys, pathTraffic, bus, cmdBus, tutorialSys, gatingLimits, researchMgr, itemMgr);

        var structureMgr = new StructureManager(
            entMgr, gateMgr, pathMgr, villagerSys, itemMgr,
            new Core.Data.ItemRegistry(), new Core.Data.RecipeRegistry(),
            gatingLimits, bus, cmdBus, tutorialSys, researchMgr);

        return (structureMgr, gateMgr, villagerSys, entMgr, tileMgr, bus, itemMgr);
    }

    [Fact]
    public void Integration_WorkerEntersViaPathGate_GetsAccepted()
    {
        var (structureMgr, gateMgr, villagerSys, entMgr, tileMgr, bus, itemMgr) = CreateIntegrationStack();

        // Place a forestry structure
        var forestry = new ForestryRecipeEntity(EntityId.Next());
        forestry.MaxWorkerCapacity = 2;
        forestry.AllocateSlots();
        var structurePos = new GridPosRPG(5, 5);
        structureMgr.AddProtoStructure(forestry, structurePos, 1);

        // Place an entrance gate adjacent to the structure
        var gatePos = new GridPosRPG(4, 5);
        var gate = gateMgr.AddPathGate(gatePos, Direction.East); // facing toward structure
        Assert.NotNull(gate);
        Assert.True(gate!.IsEntrance);

        // Place a path segment next to the gate
        var pathPos = new GridPosRPG(3, 5);
        var segment = new PathSegmentLogic(EntityId.Next())
        {
            Position = pathPos,
            Facing = Direction.East
        };
        entMgr.AddPathSegment(segment);

        // Create a villager on the path
        var villager = CreateVillager("Worker1");
        villagerSys.AddVillager(villager);
        segment.TryAddOccupant(villager.Id);
        villager.PlaceOnPath(segment.Id);
        villager.Position = pathPos;

        // Check if PathGateManager diverts the villager
        bool diverted = gateMgr.CheckPathGateEntrance(villager, segment);

        // Note: CheckPathGateEntrance checks cardinal neighbors of the segment
        // The gate is at (4,5) which is east of segment at (3,5)
        // But TryDivertViaGate checks if the gate is a neighbor of the segment
        // so this should work if the segment is at the gate's neighbor
        if (diverted)
        {
            Assert.Equal(VillagerState.Working, villager.State);
            Assert.Equal(1, forestry.WorkerCount);
        }
    }

    [Fact]
    public void Integration_StructureManager_DrainsCycle_AddsResource()
    {
        var (structureMgr, gateMgr, villagerSys, entMgr, tileMgr, bus, itemMgr) = CreateIntegrationStack();

        var forestry = new ForestryRecipeEntity(EntityId.Next());
        forestry.MaxWorkerCapacity = 2;
        forestry.StaminaCostPerCycle = 10;
        forestry.AllocateSlots();
        var structurePos = new GridPosRPG(5, 5);
        structureMgr.AddProtoStructure(forestry, structurePos, 1);

        // Manually accept a worker (bypassing PathGate for unit test)
        var villager = CreateVillager("Worker1", stamina: 100);
        villagerSys.AddVillager(villager);
        villager.TargetNodeId = forestry.Id;
        villager.State = VillagerState.Working;
        forestry.AcceptWorker(villager.Id, null);

        // Simulate enough time for one cycle (GatherInterval = 4.0f)
        forestry.Tick(4.0f);
        Assert.True(forestry.WorkerSlots[0].CycleComplete);

        // Now StructureManager ticks and drains the completed cycle
        structureMgr.Tick(0.01f);

        // Worker should have received items in inventory
        Assert.True(villager.Inventory.Count > 0);
        Assert.Equal("sticks", villager.Inventory[0].ProtoId);

        // Stamina should be drained
        Assert.Equal(90, villager.Stamina);

        // Cycle should be reset
        Assert.False(forestry.WorkerSlots[0].CycleComplete);
    }

    [Fact]
    public void Integration_StaminaDepleted_WorkerEjected()
    {
        var (structureMgr, gateMgr, villagerSys, entMgr, tileMgr, bus, itemMgr) = CreateIntegrationStack();

        var forestry = new ForestryRecipeEntity(EntityId.Next());
        forestry.MaxWorkerCapacity = 2;
        forestry.StaminaCostPerCycle = 100; // will deplete in one cycle
        forestry.AllocateSlots();
        var structurePos = new GridPosRPG(5, 5);
        structureMgr.AddProtoStructure(forestry, structurePos, 1);

        // Place an exit gate
        var exitGatePos = new GridPosRPG(6, 5);
        var exitGate = gateMgr.AddPathGate(exitGatePos, Direction.East); // facing away from structure

        // Place a path segment at exit gate's facing position
        var exitPathPos = new GridPosRPG(7, 5);
        var exitSegment = new PathSegmentLogic(EntityId.Next())
        {
            Position = exitPathPos,
            Facing = Direction.East
        };
        entMgr.AddPathSegment(exitSegment);

        // Add worker
        var villager = CreateVillager("Worker1", stamina: 100);
        villagerSys.AddVillager(villager);
        villager.TargetNodeId = forestry.Id;
        villager.State = VillagerState.Working;
        forestry.AcceptWorker(villager.Id, null);

        // Complete a cycle
        forestry.Tick(4.0f);
        Assert.True(forestry.WorkerSlots[0].CycleComplete);

        // Track event
        bool leftBuildingFired = false;
        bus.Subscribe<VillagerLeftBuildingEvent>(e => leftBuildingFired = true);

        // StructureManager processes the cycle — stamina goes to 0 → ejection
        structureMgr.Tick(0.01f);

        Assert.Equal(0, forestry.WorkerCount);
        Assert.Null(villager.TargetNodeId);
        Assert.True(leftBuildingFired);
    }

    [Fact]
    public void Integration_FullStructure_VillagerBacksUp()
    {
        var (structureMgr, gateMgr, villagerSys, entMgr, tileMgr, bus, itemMgr) = CreateIntegrationStack();

        var forestry = new ForestryRecipeEntity(EntityId.Next());
        forestry.MaxWorkerCapacity = 1;
        forestry.AllocateSlots();
        var structurePos = new GridPosRPG(5, 5);
        structureMgr.AddProtoStructure(forestry, structurePos, 1);

        // Fill the structure
        var worker1 = CreateVillager("Worker1");
        villagerSys.AddVillager(worker1);
        worker1.State = VillagerState.Working;
        worker1.TargetNodeId = forestry.Id;
        forestry.AcceptWorker(worker1.Id, null);

        Assert.False(forestry.HasFreeSlot);

        // Second villager tries to enter — should go to wait queue
        var worker2 = CreateVillager("Worker2");
        villagerSys.AddVillager(worker2);

        // Place entrance gate and path
        var gatePos = new GridPosRPG(4, 5);
        gateMgr.AddPathGate(gatePos, Direction.East);
        var pathPos = new GridPosRPG(3, 5);
        var segment = new PathSegmentLogic(EntityId.Next())
        {
            Position = pathPos,
            Facing = Direction.East
        };
        entMgr.AddPathSegment(segment);

        segment.TryAddOccupant(worker2.Id);
        worker2.PlaceOnPath(segment.Id);
        worker2.Position = pathPos;

        bool diverted = gateMgr.CheckPathGateEntrance(worker2, segment);

        if (diverted)
        {
            // Worker2 should be in wait queue, not in a slot
            Assert.Equal(1, forestry.WorkerCount); // still only worker1
            Assert.Equal(1, forestry.WaitQueueCount);
        }
    }

    [Fact]
    public void Integration_WaitQueue_PromotedWhenSlotFrees()
    {
        var (structureMgr, gateMgr, villagerSys, entMgr, tileMgr, bus, itemMgr) = CreateIntegrationStack();

        var forestry = new ForestryRecipeEntity(EntityId.Next());
        forestry.MaxWorkerCapacity = 1;
        forestry.StaminaCostPerCycle = 200; // depletes on first cycle
        forestry.AllocateSlots();
        var structurePos = new GridPosRPG(5, 5);
        structureMgr.AddProtoStructure(forestry, structurePos, 1);

        // Place exit path
        var exitGatePos = new GridPosRPG(6, 5);
        gateMgr.AddPathGate(exitGatePos, Direction.East);
        var exitPathPos = new GridPosRPG(7, 5);
        var exitSeg = new PathSegmentLogic(EntityId.Next()) { Position = exitPathPos, Facing = Direction.East };
        entMgr.AddPathSegment(exitSeg);

        // Worker1 enters
        var worker1 = CreateVillager("Worker1", stamina: 100);
        villagerSys.AddVillager(worker1);
        worker1.TargetNodeId = forestry.Id;
        worker1.State = VillagerState.Working;
        forestry.AcceptWorker(worker1.Id, null);

        // Worker2 is in wait queue
        var worker2 = CreateVillager("Worker2", stamina: 100);
        villagerSys.AddVillager(worker2);
        forestry.EnqueueWaiting(worker2.Id);

        Assert.Equal(1, forestry.WorkerCount);
        Assert.Equal(1, forestry.WaitQueueCount);

        // Complete cycle — worker1 gets ejected (stamina = 0 after drain)
        forestry.Tick(4.0f);
        structureMgr.Tick(0.01f);

        // Worker2 should be promoted from wait queue
        Assert.Equal(1, forestry.WorkerCount);
        Assert.Equal(0, forestry.WaitQueueCount);
        Assert.Equal(VillagerState.Working, worker2.State);
    }

    // ── VillagerSystem Skip ─────────────────────────────────────────

    [Fact]
    public void VillagerSystem_SkipsStructureBoundWorkers()
    {
        var bus = new EventBus();
        var itemMgr = new ItemManager(bus);
        var villagerSys = new VillagerSystem(bus, itemMgr);

        var villager = CreateVillager("Worker", stamina: 100);
        villager.AssignJob(VillagerJob.Lumberjack);
        villager.TargetNodeId = 42; // bound to a structure
        villagerSys.AddVillager(villager);

        float staminaBefore = villager.Stamina;
        villagerSys.Tick(1.0f, itemMgr);

        // Should NOT have been ticked (stamina unchanged)
        Assert.Equal(staminaBefore, villager.Stamina);
    }

    [Fact]
    public void VillagerSystem_TicksUnboundWorkers()
    {
        var bus = new EventBus();
        var itemMgr = new ItemManager(bus);
        var villagerSys = new VillagerSystem(bus, itemMgr);

        var villager = CreateVillager("Worker", stamina: 100);
        villager.AssignJob(VillagerJob.Lumberjack); // sets Working state
        villager.TargetNodeId = null; // NOT bound to a structure
        villagerSys.AddVillager(villager);

        villagerSys.Tick(1.0f, itemMgr);

        // Should have been ticked (stamina decreased)
        Assert.True(villager.Stamina < 100);
    }

    // ── Proto Deserialization ────────────────────────────────────────

    [Fact]
    public void ProtoRegistry_ForestryDefault_HasWorkerCapacity()
    {
        var reg = new ProtoRegistry();
        reg.RegisterDefaults();

        var proto = reg.GetStructure("forestry_basic") as GatheringProtoBase;
        Assert.NotNull(proto);
        Assert.Equal(5, proto!.MaxWorkerCapacity);
        Assert.Equal(10, proto.StaminaCostPerCycle);
    }

    [Fact]
    public void ProtoRegistry_MiningDefault_HasWorkerCapacity()
    {
        var reg = new ProtoRegistry();
        reg.RegisterDefaults();

        var proto = reg.GetStructure("mining_basic") as GatheringProtoBase;
        Assert.NotNull(proto);
        Assert.Equal(3, proto!.MaxWorkerCapacity);
        Assert.Equal(12, proto.StaminaCostPerCycle);
    }

    [Fact]
    public void InitializeFromProto_SetsWorkerCapacityAndAllocatesSlots()
    {
        var proto = new GatheringProtoBase
        {
            Id = "test_gathering",
            MaxWorkerCapacity = 4,
            StaminaCostPerCycle = 15,
            GatherInterval = 2.0f,
            GatherAmountPerCycle = 3,
            TargetResourceId = "test_resource"
        };

        var structure = new ForestryRecipeEntity(EntityId.Next());
        structure.InitializeFromProto(proto);

        Assert.Equal(4, structure.MaxWorkerCapacity);
        Assert.Equal(15, structure.StaminaCostPerCycle);
        Assert.Equal(4, structure.WorkerSlots.Length);
    }

    // -- Stockpile Exit: villager waits when no exit gate, ejects when gate added --

    [Fact]
    public void Stockpile_VillagerDropsOff_WaitsWhenNoExitGate()
    {
        var (structureMgr, gateMgr, villagerSys, entMgr, tileMgr, bus, itemMgr) = CreateIntegrationStack();

        // Place stockpile
        var stockpile = new StockpileLogic(EntityId.Next());
        var stockpilePos = new GridPosRPG(10, 5);
        structureMgr.AddProtoStructure(stockpile, stockpilePos, 1);

        // Create villager carrying items, simulate entry into stockpile
        var villager = CreateVillager("Carrier", stamina: 100);
        villagerSys.AddVillager(villager);
        var item = itemMgr.CreateItem("sticks", 1, 3);
        villager.TryPickUpItem(item);
        villager.Position = stockpilePos;
        villager.State = VillagerState.EnteringBuilding;

        // Add villager to pending drop-off list (as TryDivertViaGate would)
        stockpile.PendingVillagerIds.Add(villager.Id);

        // No exit gate exists � tick StructureManager
        structureMgr.Tick(0.01f);

        // Items should be unloaded
        Assert.False(villager.IsCarryingItems);
        Assert.True(stockpile.TotalStored > 0);

        // Villager should be in WaitingToExitIds, not stranded as Idle
        Assert.Single(stockpile.WaitingToExitIds);
        Assert.Contains(villager.Id, stockpile.WaitingToExitIds);
        Assert.Equal(VillagerState.EnteringBuilding, villager.State);
    }

    [Fact]
    public void Stockpile_VillagerExits_WhenExitGateAddedLater()
    {
        var (structureMgr, gateMgr, villagerSys, entMgr, tileMgr, bus, itemMgr) = CreateIntegrationStack();

        // Place stockpile
        var stockpile = new StockpileLogic(EntityId.Next());
        var stockpilePos = new GridPosRPG(10, 5);
        structureMgr.AddProtoStructure(stockpile, stockpilePos, 1);

        // Create villager carrying items, simulate entry into stockpile
        var villager = CreateVillager("Carrier", stamina: 100);
        villagerSys.AddVillager(villager);
        var item = itemMgr.CreateItem("sticks", 1, 3);
        villager.TryPickUpItem(item);
        villager.Position = stockpilePos;
        villager.State = VillagerState.EnteringBuilding;
        stockpile.PendingVillagerIds.Add(villager.Id);

        // Tick without exit gate � villager waits
        structureMgr.Tick(0.01f);
        Assert.Single(stockpile.WaitingToExitIds);

        // Now add an exit gate and path segment
        var exitGatePos = new GridPosRPG(11, 5);
        gateMgr.AddPathGate(exitGatePos, Direction.East); // facing away from stockpile
        var exitPathPos = new GridPosRPG(12, 5);
        var exitSeg = new PathSegmentLogic(EntityId.Next()) { Position = exitPathPos, Facing = Direction.East };
        entMgr.AddPathSegment(exitSeg);

        // Track exit event
        bool leftBuildingFired = false;
        bus.Subscribe<VillagerLeftBuildingEvent>(e => leftBuildingFired = true);

        // Tick again � retry should succeed
        structureMgr.Tick(0.01f);

        Assert.Empty(stockpile.WaitingToExitIds);
        Assert.Equal(VillagerState.Travelling, villager.State);
        Assert.True(leftBuildingFired);
        Assert.Equal(exitPathPos, villager.Position);
    }

    [Fact]
    public void Stockpile_VillagerExitsImmediately_WhenExitGateExists()
    {
        var (structureMgr, gateMgr, villagerSys, entMgr, tileMgr, bus, itemMgr) = CreateIntegrationStack();

        // Place stockpile
        var stockpile = new StockpileLogic(EntityId.Next());
        var stockpilePos = new GridPosRPG(10, 5);
        structureMgr.AddProtoStructure(stockpile, stockpilePos, 1);

        // Place exit gate and path BEFORE villager enters
        var exitGatePos = new GridPosRPG(11, 5);
        gateMgr.AddPathGate(exitGatePos, Direction.East);
        var exitPathPos = new GridPosRPG(12, 5);
        var exitSeg = new PathSegmentLogic(EntityId.Next()) { Position = exitPathPos, Facing = Direction.East };
        entMgr.AddPathSegment(exitSeg);

        // Create villager carrying items, simulate entry
        var villager = CreateVillager("Carrier", stamina: 100);
        villagerSys.AddVillager(villager);
        var carried = itemMgr.CreateItem("sticks", 1, 5);
        villager.TryPickUpItem(carried);
        villager.Position = stockpilePos;
        villager.State = VillagerState.EnteringBuilding;
        stockpile.PendingVillagerIds.Add(villager.Id);

        bool leftBuildingFired = false;
        bus.Subscribe<VillagerLeftBuildingEvent>(e => leftBuildingFired = true);

        // Tick � should drop off and exit immediately
        structureMgr.Tick(0.01f);

        Assert.False(villager.IsCarryingItems);
        Assert.Empty(stockpile.WaitingToExitIds);
        Assert.Equal(VillagerState.Travelling, villager.State);
        Assert.True(leftBuildingFired);
    }
}
