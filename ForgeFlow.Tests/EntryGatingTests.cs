using ForgeFlow.Core.Entities;
using ForgeFlow.Core.Events;
using ForgeFlow.Core.Proto;
using ForgeFlow.Core.Proto.Logic;
using ForgeFlow.Core.Proto.Prototypes;
using ForgeFlow.Core.Systems;
using ForgeFlow.Core.Utilities;

namespace ForgeFlow.Tests;

/// <summary>
/// Comprehensive tests for the IEntryGated / IPreEntryAction entry gating system.
/// Covers:
/// - Unit tests for each structure's CheckEntry method
/// - IPreEntryAction tests for DungeonPortalLogic
/// - Structure.AsEntryGated / AsPreEntryAction cached property tests
/// - PathGateManager integration tests for entry rejection
/// </summary>
public class EntryGatingTests
{
    public EntryGatingTests()
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

    private static ItemInstance CreateItem(string protoId, int quantity = 1)
    {
        return new ItemInstance
        {
            InstanceId = EntityIdFactory.Next(),
            ProtoId = protoId,
            Quantity = quantity
        };
    }

    private static GatheringLogicBase CreateGathering(int capacity = 3)
    {
        var structure = new ForestryRecipeEntity(EntityId.Next());
        structure.MaxWorkerCapacity = capacity;
        structure.AllocateSlots();
        return structure;
    }

    private static InnLogic CreateInn(int maxOccupants = 4)
    {
        var proto = new InnProto
        {
            Id = "inn_test",
            MaxOccupants = maxOccupants,
            ProcessingDuration = 5.0f
        };
        var inn = new InnLogic(EntityId.Next());
        inn.InitializeFromProto(proto);
        return inn;
    }

    private static DungeonPortalLogic CreateDungeonPortal(int maxOccupants = 4)
    {
        var proto = new DungeonPortalProto
        {
            Id = "dungeon_test",
            DefaultDungeonId = "goblin_caves",
            BaseSurvivalChance = 0.1f,
            BaseGoldReward = 50,
            RunDuration = 15f,
            MaxOccupants = maxOccupants,
            ProcessingDuration = 15f
        };
        var logic = new DungeonPortalLogic(EntityId.Next());
        logic.InitializeFromProto(proto);
        return logic;
    }

    private static VillageSpawnerLogic CreateSpawner(int maxOccupants = 4)
    {
        var proto = new VillageSpawnerProto
        {
            Id = "spawner_test",
            MaxOccupants = maxOccupants,
            SpawnInterval = 10f,
            MaxVillagers = 5
        };
        var logic = new VillageSpawnerLogic(EntityId.Next());
        logic.InitializeFromProto(proto);
        return logic;
    }

    private static TrainingBuildingLogic CreateTrainingBuilding(int maxTrainees = 2)
    {
        var proto = new TrainingBuildingProto
        {
            Id = "training_test",
            OutputClass = VillagerClass.Warrior,
            TrainingDuration = 15f,
            MaxTrainees = maxTrainees
        };
        var logic = new TrainingBuildingLogic(EntityId.Next());
        logic.InitializeFromProto(proto);
        return logic;
    }

    private static CraftStationLogic CreateCraftStationWithRecipe()
    {
        var craft = new CraftStationLogic(EntityId.Next());
        craft.IsActive = true;
        craft.SetActiveRecipe(
            "craft_wooden_spear",
            new Dictionary<string, int> { { "sticks", 4 } },
            4.0f,
            "wooden_spear_0");
        return craft;
    }

    private static CraftStationLogic CreateCraftStationNoRecipe()
    {
        var craft = new CraftStationLogic(EntityId.Next());
        craft.IsActive = true;
        return craft;
    }

    private static StockpileLogic CreateStockpile(int maxCapacity = 100, string? acceptedItemId = null)
    {
        var logic = new StockpileLogic(EntityId.Next());
        logic.MaxCapacity = maxCapacity;
        if (acceptedItemId != null)
        {
            logic.SetAcceptedItem(acceptedItemId);
        }
        return logic;
    }

    // ══════════════════════════════════════════════════════════════════
    // ── GatheringLogicBase.CheckEntry ────────────────────────────────
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public void GatheringLogicBase_CheckEntry_AcceptsVillagerWithFreeInventory()
    {
        var gathering = CreateGathering();
        var villager = CreateVillager();

        var result = gathering.CheckEntry(villager);

        Assert.True(result.IsAccepted);
        Assert.Equal(EntryRejectionReason.None, result.Reason);
    }

    [Fact]
    public void GatheringLogicBase_CheckEntry_RejectsVillagerWithFullInventory()
    {
        var gathering = CreateGathering();
        var villager = CreateVillager();
        for (int i = 0; i < villager.MaxInventorySlots; i++)
        {
            villager.TryPickUpItem(CreateItem("sticks"));
        }

        var result = gathering.CheckEntry(villager);

        Assert.False(result.IsAccepted);
        Assert.Equal(EntryRejectionReason.InventoryFull, result.Reason);
    }

    [Fact]
    public void GatheringLogicBase_CheckEntry_AcceptsVillagerWithPartialInventory()
    {
        var gathering = CreateGathering();
        var villager = CreateVillager();
        villager.TryPickUpItem(CreateItem("sticks"));

        var result = gathering.CheckEntry(villager);

        Assert.True(result.IsAccepted);
    }

    // ══════════════════════════════════════════════════════════════════
    // ── InnLogic.CheckEntry ──────────────────────────────────────────
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public void InnLogic_CheckEntry_AcceptsEmptyHandedVillager()
    {
        var inn = CreateInn();
        var villager = CreateVillager();

        var result = inn.CheckEntry(villager);

        Assert.True(result.IsAccepted);
    }

    [Fact]
    public void InnLogic_CheckEntry_RejectsVillagerCarryingItems()
    {
        var inn = CreateInn();
        var villager = CreateVillager();
        villager.TryPickUpItem(CreateItem("sticks"));

        var result = inn.CheckEntry(villager);

        Assert.False(result.IsAccepted);
        Assert.Equal(EntryRejectionReason.NotCarryingRequiredItems, result.Reason);
    }

    [Fact]
    public void InnLogic_CheckEntry_RejectsWhenFull()
    {
        var inn = CreateInn(maxOccupants: 1);
        var existing = CreateVillager("Existing");
        inn.AcceptVillager(existing);

        var villager = CreateVillager();
        var result = inn.CheckEntry(villager);

        Assert.False(result.IsAccepted);
        Assert.Equal(EntryRejectionReason.StructureFull, result.Reason);
    }

    // ══════════════════════════════════════════════════════════════════
    // ── DungeonPortalLogic.CheckEntry ────────────────────────────────
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public void DungeonPortalLogic_CheckEntry_AcceptsFightingClassVillager()
    {
        var dungeon = CreateDungeonPortal();
        var villager = CreateVillager(trainedClass: VillagerClass.Warrior);

        var result = dungeon.CheckEntry(villager);

        Assert.True(result.IsAccepted);
    }

    [Fact]
    public void DungeonPortalLogic_CheckEntry_RejectsUntrainedVillager()
    {
        var dungeon = CreateDungeonPortal();
        var villager = CreateVillager(trainedClass: VillagerClass.Untrained);

        var result = dungeon.CheckEntry(villager);

        Assert.False(result.IsAccepted);
        Assert.Equal(EntryRejectionReason.WrongClass, result.Reason);
    }

    [Fact]
    public void DungeonPortalLogic_CheckEntry_RejectsArtisanVillager()
    {
        var dungeon = CreateDungeonPortal();
        var villager = CreateVillager(trainedClass: VillagerClass.Artisan);

        var result = dungeon.CheckEntry(villager);

        Assert.False(result.IsAccepted);
        Assert.Equal(EntryRejectionReason.WrongClass, result.Reason);
    }

    [Fact]
    public void DungeonPortalLogic_CheckEntry_RejectsWhenFull()
    {
        var dungeon = CreateDungeonPortal(maxOccupants: 1);
        dungeon.AcceptVillager(999);

        var villager = CreateVillager(trainedClass: VillagerClass.Warrior);
        var result = dungeon.CheckEntry(villager);

        Assert.False(result.IsAccepted);
        Assert.Equal(EntryRejectionReason.StructureFull, result.Reason);
    }

    [Fact]
    public void DungeonPortalLogic_PrepareVillagerForEntry_AutoEquips()
    {
        var dungeon = CreateDungeonPortal();
        var villager = CreateVillager(trainedClass: VillagerClass.Warrior);
        villager.TryPickUpItem(new ItemInstance
        {
            InstanceId = EntityIdFactory.Next(),
            ProtoId = "sword_t1",
            Slot = EquipSlot.Weapon,
            Damage = 10f
        });
        villager.TryPickUpItem(new ItemInstance
        {
            InstanceId = EntityIdFactory.Next(),
            ProtoId = "shield_t1",
            Slot = EquipSlot.Shield,
            Defense = 5f
        });

        Assert.Null(villager.EquippedWeaponId);
        Assert.Null(villager.EquippedArmorId);

        dungeon.PrepareVillagerForEntry(villager);

        Assert.Equal("sword_t1", villager.EquippedWeaponId);
        Assert.Equal("shield_t1", villager.EquippedArmorId);
    }

    // ══════════════════════════════════════════════════════════════════
    // ── VillageSpawnerLogic.CheckEntry ───────────────────────────────
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public void VillageSpawnerLogic_CheckEntry_AcceptsResident()
    {
        var spawner = CreateSpawner();
        var villager = CreateVillager();
        spawner.AssignResident(villager.Id);

        var result = spawner.CheckEntry(villager);

        Assert.True(result.IsAccepted);
    }

    [Fact]
    public void VillageSpawnerLogic_CheckEntry_RejectsNonResident()
    {
        var spawner = CreateSpawner();
        var villager = CreateVillager();
        // Not assigned as resident

        var result = spawner.CheckEntry(villager);

        Assert.False(result.IsAccepted);
        Assert.Equal(EntryRejectionReason.NotResident, result.Reason);
    }

    [Fact]
    public void VillageSpawnerLogic_CheckEntry_RejectsWhenFull()
    {
        var spawner = CreateSpawner(maxOccupants: 1);
        var resident = CreateVillager("Resident");
        spawner.AssignResident(resident.Id);
        spawner.AcceptVillagerForRest(resident);

        var villager = CreateVillager();
        spawner.AssignResident(villager.Id);
        var result = spawner.CheckEntry(villager);

        Assert.False(result.IsAccepted);
        Assert.Equal(EntryRejectionReason.StructureFull, result.Reason);
    }

    // ══════════════════════════════════════════════════════════════════
    // ── TrainingBuildingLogic.CheckEntry ──────────────────────────────
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public void TrainingBuildingLogic_CheckEntry_AcceptsUntrainedVillager()
    {
        var training = CreateTrainingBuilding();
        var villager = CreateVillager(trainedClass: VillagerClass.Untrained);

        var result = training.CheckEntry(villager);

        Assert.True(result.IsAccepted);
    }

    [Fact]
    public void TrainingBuildingLogic_CheckEntry_RejectsAlreadyTrainedVillager()
    {
        var training = CreateTrainingBuilding();
        var villager = CreateVillager(trainedClass: VillagerClass.Warrior);

        var result = training.CheckEntry(villager);

        Assert.False(result.IsAccepted);
        Assert.Equal(EntryRejectionReason.AlreadyTrained, result.Reason);
    }

    [Fact]
    public void TrainingBuildingLogic_CheckEntry_RejectsWhenFull()
    {
        var training = CreateTrainingBuilding(maxTrainees: 1);
        var existing = CreateVillager("Existing");
        training.AcceptTrainee(existing);

        var villager = CreateVillager();
        var result = training.CheckEntry(villager);

        Assert.False(result.IsAccepted);
        Assert.Equal(EntryRejectionReason.StructureFull, result.Reason);
    }

    // ══════════════════════════════════════════════════════════════════
    // ── CraftStationLogic.CheckEntry ─────────────────────────────────
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public void CraftStationLogic_CheckEntry_AcceptsVillagerWithRecipeItems()
    {
        var craft = CreateCraftStationWithRecipe();
        var villager = CreateVillager();
        villager.TryPickUpItem(CreateItem("sticks"));

        var result = craft.CheckEntry(villager);

        Assert.True(result.IsAccepted);
    }

    [Fact]
    public void CraftStationLogic_CheckEntry_RejectsVillagerWithNoRecipeItems()
    {
        var craft = CreateCraftStationWithRecipe();
        var villager = CreateVillager();
        villager.TryPickUpItem(CreateItem("ore"));

        var result = craft.CheckEntry(villager);

        Assert.False(result.IsAccepted);
        Assert.Equal(EntryRejectionReason.NotCarryingRequiredItems, result.Reason);
    }

    [Fact]
    public void CraftStationLogic_CheckEntry_RejectsWhenNoRecipeSet()
    {
        var craft = CreateCraftStationNoRecipe();
        var villager = CreateVillager();
        villager.TryPickUpItem(CreateItem("sticks"));

        var result = craft.CheckEntry(villager);

        Assert.False(result.IsAccepted);
        Assert.Equal(EntryRejectionReason.NotEligible, result.Reason);
    }

    [Fact]
    public void CraftStationLogic_CheckEntry_AcceptsVillagerWithMixedItems()
    {
        var craft = CreateCraftStationWithRecipe();
        var villager = CreateVillager();
        villager.TryPickUpItem(CreateItem("ore"));
        villager.TryPickUpItem(CreateItem("sticks"));

        var result = craft.CheckEntry(villager);

        Assert.True(result.IsAccepted);
    }

    [Fact]
    public void CraftStationLogic_CheckEntry_RejectsEmptyHandedVillager()
    {
        var craft = CreateCraftStationWithRecipe();
        var villager = CreateVillager();

        var result = craft.CheckEntry(villager);

        Assert.False(result.IsAccepted);
        Assert.Equal(EntryRejectionReason.NotCarryingRequiredItems, result.Reason);
    }

    // ══════════════════════════════════════════════════════════════════
    // ── StockpileLogic.CheckEntry ────────────────────────────────────
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public void StockpileLogic_CheckEntry_AcceptsVillagerCarryingItems()
    {
        var stockpile = CreateStockpile();
        var villager = CreateVillager();
        villager.TryPickUpItem(CreateItem("sticks"));

        var result = stockpile.CheckEntry(villager);

        Assert.True(result.IsAccepted);
    }

    [Fact]
    public void StockpileLogic_CheckEntry_RejectsEmptyHandedVillager()
    {
        var stockpile = CreateStockpile();
        var villager = CreateVillager();

        var result = stockpile.CheckEntry(villager);

        Assert.False(result.IsAccepted);
        Assert.Equal(EntryRejectionReason.NotCarryingItems, result.Reason);
    }

    [Fact]
    public void StockpileLogic_CheckEntry_RejectsWhenFull()
    {
        var stockpile = CreateStockpile(maxCapacity: 1);
        stockpile.Deposit("sticks", 1);

        var villager = CreateVillager();
        villager.TryPickUpItem(CreateItem("sticks"));

        var result = stockpile.CheckEntry(villager);

        Assert.False(result.IsAccepted);
        Assert.Equal(EntryRejectionReason.StructureFull, result.Reason);
    }

    [Fact]
    public void StockpileLogic_CheckEntry_RejectsWhenFilterDoesNotMatch()
    {
        var stockpile = CreateStockpile(acceptedItemId: "ore");
        var villager = CreateVillager();
        villager.TryPickUpItem(CreateItem("sticks"));

        var result = stockpile.CheckEntry(villager);

        Assert.False(result.IsAccepted);
        Assert.Equal(EntryRejectionReason.NotCarryingRequiredItems, result.Reason);
    }

    [Fact]
    public void StockpileLogic_CheckEntry_AcceptsWhenFilterMatches()
    {
        var stockpile = CreateStockpile(acceptedItemId: "sticks");
        var villager = CreateVillager();
        villager.TryPickUpItem(CreateItem("sticks"));

        var result = stockpile.CheckEntry(villager);

        Assert.True(result.IsAccepted);
    }

    [Fact]
    public void StockpileLogic_CheckEntry_AcceptsWhenNoFilter_AnyItem()
    {
        var stockpile = CreateStockpile();
        var villager = CreateVillager();
        villager.TryPickUpItem(CreateItem("rare_gem"));

        var result = stockpile.CheckEntry(villager);

        Assert.True(result.IsAccepted);
    }

    // ══════════════════════════════════════════════════════════════════
    // ── Structure.AsEntryGated / AsPreEntryAction Cached Properties ──
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public void Structure_AsEntryGated_ReturnsNonNull_ForGating()
    {
        var gathering = CreateGathering();
        Assert.NotNull(gathering.AsEntryGated);
        Assert.IsAssignableFrom<IEntryGated>(gathering.AsEntryGated);
    }

    [Fact]
    public void Structure_AsEntryGated_ReturnsSameInstanceOnRepeatedAccess()
    {
        var inn = CreateInn();
        var first = inn.AsEntryGated;
        var second = inn.AsEntryGated;
        Assert.Same(first, second);
    }

    [Fact]
    public void Structure_AsPreEntryAction_ReturnsNonNull_ForDungeon()
    {
        var dungeon = CreateDungeonPortal();
        Assert.NotNull(dungeon.AsPreEntryAction);
        Assert.IsAssignableFrom<IPreEntryAction>(dungeon.AsPreEntryAction);
    }

    [Fact]
    public void Structure_AsPreEntryAction_ReturnsNull_ForInn()
    {
        var inn = CreateInn();
        Assert.Null(inn.AsPreEntryAction);
    }

    [Fact]
    public void Structure_AsEntryGated_ReturnsNonNull_ForAllGatedStructures()
    {
        var structures = new Structure[]
        {
            CreateGathering(),
            CreateInn(),
            CreateDungeonPortal(),
            CreateSpawner(),
            CreateTrainingBuilding(),
            CreateCraftStationWithRecipe(),
            CreateStockpile()
        };

        foreach (var structure in structures)
        {
            Assert.NotNull(structure.AsEntryGated);
        }
    }

    // ══════════════════════════════════════════════════════════════════
    // ── EntryCheckResult Value Type Tests ─────────────────────────────
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public void EntryCheckResult_Accepted_HasCorrectValues()
    {
        var result = EntryCheckResult.Accepted;
        Assert.True(result.IsAccepted);
        Assert.Equal(EntryRejectionReason.None, result.Reason);
    }

    [Fact]
    public void EntryCheckResult_Rejected_HasCorrectValues()
    {
        var result = EntryCheckResult.Rejected(EntryRejectionReason.InventoryFull);
        Assert.False(result.IsAccepted);
        Assert.Equal(EntryRejectionReason.InventoryFull, result.Reason);
    }

    [Fact]
    public void EntryCheckResult_AllRejectionReasons_AreDistinct()
    {
        var reasons = Enum.GetValues<EntryRejectionReason>();
        var distinctCount = reasons.Distinct().Count();
        Assert.Equal(reasons.Length, distinctCount);
    }

    // ══════════════════════════════════════════════════════════════════
    // ── PathGateManager Integration Tests ────────────────────────────
    // ══════════════════════════════════════════════════════════════════

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

    /// <summary>
    /// Places a structure at structurePos, adds a PathGate entrance adjacent to it,
    /// places a path segment adjacent to the gate, then calls CheckPathGateEntrance.
    /// Layout: structure at structurePos, gate south of structure (entrance, facing north),
    /// path segment south of gate. CheckPathGateEntrance finds gate as neighbor of segment.
    /// Returns whether the villager was diverted.
    /// </summary>
    private static bool SimulateDivert(
        PathGateManager pgm, EntityManager entMgr, TileManager tileMgr,
        PathTrafficSystem pathTraffic, EventBus bus,
        Structure structure, GridPosRPG structurePos,
        VillagerLogic villager)
    {
        entMgr.AddStructure(structure, structurePos);

        // Gate south of the structure, facing north (entrance into structure)
        var gatePos = structurePos.Neighbor(Direction.South);
        var gate = pgm.AddPathGate(gatePos, Direction.North);
        Assert.NotNull(gate);
        Assert.True(gate!.IsEntrance);
        Assert.True(gate.IsLinked);

        // Path segment south of the gate — gate is a cardinal neighbor
        var segPos = gatePos.Neighbor(Direction.South);
        var seg = new PathSegmentLogic(EntityId.Next())
        {
            Position = segPos,
            IsActive = true,
            Facing = Direction.North
        };
        entMgr.AddPathSegment(seg);

        // Place villager on the segment
        villager.CurrentPathSegmentId = seg.Id;
        villager.PathProgress = 0.99f;
        villager.State = VillagerState.Travelling;
        villager.Position = segPos;
        seg.TryAddOccupant(villager.Id);

        // Call CheckPathGateEntrance directly — tests the universal IEntryGated check
        return pgm.CheckPathGateEntrance(villager, seg);
    }

    [Fact]
    public void PathGate_RejectsVillagerWithFullInventory_AtGathering()
    {
        EntityBase.ResetIdCounter();
        var (pgm, entMgr, tileMgr, _, pathTraffic, bus) = CreatePathGateManager();

        var gathering = CreateGathering();
        gathering.IsActive = true;

        var villager = CreateVillager();
        for (int i = 0; i < villager.MaxInventorySlots; i++)
        {
            villager.TryPickUpItem(CreateItem("sticks"));
        }

        bool diverted = SimulateDivert(pgm, entMgr, tileMgr, pathTraffic, bus,
            gathering, new GridPosRPG(5, 5), villager);

        // Villager should NOT be diverted — inventory full
        Assert.False(diverted);
    }

    [Fact]
    public void PathGate_AcceptsVillagerWithFreeInventory_AtGathering()
    {
        EntityBase.ResetIdCounter();
        var (pgm, entMgr, tileMgr, _, pathTraffic, bus) = CreatePathGateManager();

        var gathering = CreateGathering();
        gathering.IsActive = true;

        var villager = CreateVillager();

        bool diverted = SimulateDivert(pgm, entMgr, tileMgr, pathTraffic, bus,
            gathering, new GridPosRPG(5, 5), villager);

        Assert.True(diverted);
    }

    [Fact]
    public void PathGate_RejectsEmptyVillager_AtStockpile()
    {
        EntityBase.ResetIdCounter();
        var (pgm, entMgr, tileMgr, _, pathTraffic, bus) = CreatePathGateManager();

        var stockpile = CreateStockpile();
        stockpile.IsActive = true;

        var villager = CreateVillager();
        // No items in inventory

        bool diverted = SimulateDivert(pgm, entMgr, tileMgr, pathTraffic, bus,
            stockpile, new GridPosRPG(5, 5), villager);

        Assert.False(diverted);
    }

    [Fact]
    public void PathGate_RejectsVillagerWithNoRecipeItems_AtCraftStation()
    {
        EntityBase.ResetIdCounter();
        var (pgm, entMgr, tileMgr, _, pathTraffic, bus) = CreatePathGateManager();

        var craft = CreateCraftStationWithRecipe();

        var villager = CreateVillager();
        villager.TryPickUpItem(CreateItem("ore")); // Not a recipe input

        bool diverted = SimulateDivert(pgm, entMgr, tileMgr, pathTraffic, bus,
            craft, new GridPosRPG(5, 5), villager);

        Assert.False(diverted);
    }

    [Fact]
    public void PathGate_RejectsTrainedVillager_AtTrainingBuilding()
    {
        EntityBase.ResetIdCounter();
        var (pgm, entMgr, tileMgr, _, pathTraffic, bus) = CreatePathGateManager();

        var training = CreateTrainingBuilding();
        training.IsActive = true;

        var villager = CreateVillager(trainedClass: VillagerClass.Warrior);

        bool diverted = SimulateDivert(pgm, entMgr, tileMgr, pathTraffic, bus,
            training, new GridPosRPG(5, 5), villager);

        Assert.False(diverted);
    }

    [Fact]
    public void PathGate_RejectsNonResident_AtResidence()
    {
        EntityBase.ResetIdCounter();
        var (pgm, entMgr, tileMgr, _, pathTraffic, bus) = CreatePathGateManager();

        var spawner = CreateSpawner();
        spawner.IsActive = true;
        // Do not assign villager as resident

        var villager = CreateVillager();

        bool diverted = SimulateDivert(pgm, entMgr, tileMgr, pathTraffic, bus,
            spawner, new GridPosRPG(5, 5), villager);

        Assert.False(diverted);
    }

    // ── DungeonPortal regression tests (Phase 8) ──────────────────────

    [Fact]
    public void PathGate_RejectsUntrainedVillager_AtDungeonPortal_AndPublishesEvent()
    {
        EntityBase.ResetIdCounter();
        var (pgm, entMgr, tileMgr, _, pathTraffic, bus) = CreatePathGateManager();

        var dungeon = CreateDungeonPortal();
        dungeon.IsActive = true;

        VillagerEntryRejectedEvent? captured = null;
        bus.Subscribe<VillagerEntryRejectedEvent>(e => captured = e);

        // Fresh villager has no fighting class → CheckEntry returns WrongClass
        var villager = CreateVillager(trainedClass: VillagerClass.Untrained);

        bool diverted = SimulateDivert(pgm, entMgr, tileMgr, pathTraffic, bus,
            dungeon, new GridPosRPG(5, 5), villager);

        Assert.False(diverted);
        Assert.Empty(dungeon.CurrentOccupants);
        Assert.NotNull(captured);
        Assert.Equal(villager.Id, captured!.Value.VillagerId);
        Assert.Equal(dungeon.Id, captured.Value.StructureId);
        Assert.Equal(EntryRejectionReason.WrongClass, captured.Value.Reason);
    }

    [Fact]
    public void PathGate_AcceptsFightingClassVillager_AtDungeonPortal()
    {
        EntityBase.ResetIdCounter();
        var (pgm, entMgr, tileMgr, _, pathTraffic, bus) = CreatePathGateManager();

        var dungeon = CreateDungeonPortal();
        dungeon.IsActive = true;

        bool rejected = false;
        bus.Subscribe<VillagerEntryRejectedEvent>(_ => rejected = true);

        var villager = CreateVillager(trainedClass: VillagerClass.Warrior);

        bool diverted = SimulateDivert(pgm, entMgr, tileMgr, pathTraffic, bus,
            dungeon, new GridPosRPG(5, 5), villager);

        Assert.True(diverted);
        Assert.False(rejected);
        Assert.Contains(villager.Id.Value, dungeon.CurrentOccupants);
    }

    [Fact]
    public void PathGate_RejectsFullDungeonPortal_AndPublishesStructureFull()
    {
        EntityBase.ResetIdCounter();
        var (pgm, entMgr, tileMgr, _, pathTraffic, bus) = CreatePathGateManager();

        var dungeon = CreateDungeonPortal(maxOccupants: 1);
        dungeon.IsActive = true;
        dungeon.AcceptVillager(999); // Fill the single slot

        VillagerEntryRejectedEvent? captured = null;
        bus.Subscribe<VillagerEntryRejectedEvent>(e => captured = e);

        var villager = CreateVillager(trainedClass: VillagerClass.Warrior);

        bool diverted = SimulateDivert(pgm, entMgr, tileMgr, pathTraffic, bus,
            dungeon, new GridPosRPG(5, 5), villager);

        Assert.False(diverted);
        Assert.NotNull(captured);
        Assert.Equal(EntryRejectionReason.StructureFull, captured!.Value.Reason);
    }
}
