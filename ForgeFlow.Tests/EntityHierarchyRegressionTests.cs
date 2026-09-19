using ForgeFlow.Core.Entities;
using ForgeFlow.Core.Proto;
using ForgeFlow.Core.Proto.Logic;
using ForgeFlow.Core.Proto.Prototypes;

namespace ForgeFlow.Tests;

/// <summary>
/// Regression tests for the entity hierarchy.
/// These tests lock down behavior of EntityBase, Structure,
/// StructureProtoBase, and all concrete structure/logic types.
/// </summary>
public class EntityHierarchyRegressionTests
{
    public EntityHierarchyRegressionTests()
    {
        EntityBase.ResetIdCounter();
    }

    // ── EntityBase (via concrete structures) ───────────────────────

    [Fact]
    public void EntityBase_AssignsUniqueIds_ViaConcreteStructures()
    {
        var a = new ForestryRecipeEntity(EntityId.Next());
        var b = new MiningRecipeEntity(EntityId.Next());
        Assert.NotEqual(a.Id, b.Id);
    }

    [Fact]
    public void EntityBase_DefaultsToActive_ViaConcreteStructure()
    {
        var logic = new ForestryRecipeEntity(EntityId.Next());
        Assert.True(logic.IsActive);
    }

    [Fact]
    public void EntityBase_ProtoIdDefaultsToEmpty_ViaConcreteStructure()
    {
        var logic = new StockpileLogic(EntityId.Next());
        Assert.Equal(string.Empty, logic.ProtoId);
    }

    [Fact]
    public void EntityBase_PositionDefaultsToOrigin_ViaConcreteStructure()
    {
        var logic = new InnLogic(EntityId.Next());
        Assert.Equal(new GridPosRPG(0, 0), logic.Position);
    }

    // ── StructureProtoBase ─────────────────────────────────────────

    [Fact]
    public void StructureProtoBase_DefaultProcessingDuration()
    {
        var proto = new GatheringProtoBase();
        Assert.Equal(2.0f, proto.ProcessingDuration);
    }

    [Fact]
    public void StructureProtoBase_DefaultMaxOutputQueueSize()
    {
        var proto = new GatheringProtoBase();
        Assert.Equal(5, proto.MaxOutputQueueSize);
    }

    [Fact]
    public void StructureProtoBase_DefaultTier()
    {
        var proto = new GatheringProtoBase();
        Assert.Equal(1, proto.Tier);
    }

    [Fact]
    public void StructureProtoBase_CustomPropertiesInitializedEmpty()
    {
        var proto = new GatheringProtoBase();
        Assert.NotNull(proto.CustomProperties);
        Assert.Empty(proto.CustomProperties);
    }

    [Fact]
    public void StructureProtoBase_InheritsProtoBaseFields()
    {
        var proto = new InnProto
        {
            Id = "inn_test",
            DisplayName = "Test Inn",
            Description = "A test inn",
            UnlockTier = 2
        };
        Assert.Equal("inn_test", proto.Id);
        Assert.Equal("Test Inn", proto.DisplayName);
        Assert.Equal("A test inn", proto.Description);
        Assert.Equal(2, proto.UnlockTier);
    }

    // ── Structure & Domain Bases (structure properties) ────

    [Fact]
    public void EntityFootprint_DefaultOutputDirection()
    {
        var logic = new StockpileLogic(EntityId.Next());
        Assert.Equal(Direction.East, logic.OutputDirection);
    }

    [Fact]
    public void RecipeEntity_DefaultQueueEmpty()
    {
        var logic = new CraftStationLogic(EntityId.Next());
        Assert.Empty(logic.InputQueue);
        Assert.Empty(logic.OutputQueue);
    }

    [Fact]
    public void RecipeEntity_TryDequeueOutput_ReturnsFalse_WhenEmpty()
    {
        var logic = new CraftStationLogic(EntityId.Next());
        bool result = logic.TryDequeueOutput(out var item);
        Assert.False(result);
        Assert.Null(item);
    }

    [Fact]
    public void RecipeEntity_TryDequeueOutput_ReturnsItem_WhenAvailable()
    {
        var logic = new CraftStationLogic(EntityId.Next());
        var testItem = new ItemInstance { ProtoId = "item_test", Quantity = 1 };
        logic.OutputQueue.Enqueue(testItem);

        bool result = logic.TryDequeueOutput(out var item);
        Assert.True(result);
        Assert.Equal("item_test", item!.ProtoId);
    }

    [Fact]
    public void RecipeEntity_TryEnqueueInput_AlwaysReturnsTrue()
    {
        var logic = new CraftStationLogic(EntityId.Next());
        var testItem = new ItemInstance { ProtoId = "ore", Quantity = 2 };
        bool result = logic.TryEnqueueInput(testItem);
        Assert.True(result);
        Assert.Single(logic.InputQueue);
    }

    [Fact]
    public void RecipeEntity_OutputQueueFull_RespectsMaxSize()
    {
        var logic = new CraftStationLogic(EntityId.Next());
        logic.MaxOutputQueueSize = 2;
        Assert.False(logic.OutputQueueFull);

        logic.OutputQueue.Enqueue(new ItemInstance { ProtoId = "a" });
        Assert.False(logic.OutputQueueFull);

        logic.OutputQueue.Enqueue(new ItemInstance { ProtoId = "b" });
        Assert.True(logic.OutputQueueFull);
    }

    [Fact]
    public void StructureEntity_WaitingToExitIds_DefaultsEmpty()
    {
        var logic = new InnLogic(EntityId.Next());
        Assert.Empty(logic.WaitingToExitIds);
        Assert.False(logic.HasWaitingExits);
    }

    [Fact]
    public void StructureEntity_HasWaitingExits_TrueWhenPopulated()
    {
        var logic = new InnLogic(EntityId.Next());
        logic.WaitingToExitIds.Add(new EntityId(42));
        Assert.True(logic.HasWaitingExits);
    }

    // ── InitializeFromProto ──────────────────────────────────────

    [Fact]
    public void InitializeFromProto_SetsBaseFields()
    {
        var proto = new InnProto
        {
            Id = "inn_basic",
            ProcessingDuration = 5.0f,
            Tier = 2
        };
        var logic = new InnLogic(EntityId.Next());
        logic.InitializeFromProto(proto);

        Assert.Equal("inn_basic", logic.ProtoId);
        Assert.Equal("Inn", logic.GetCategoryName());
        Assert.Equal(5.0f, logic.ProcessingDuration);
        Assert.Equal(2, logic.Tier);
    }

    [Fact]
    public void InitializeFromProto_RecipeEntity_SetsMaxOutputQueueSize()
    {
        var proto = new CraftStationProto
        {
            Id = "craft_queue",
            MaxOutputQueueSize = 3,
            Tier = 2
        };
        var logic = new CraftStationLogic(EntityId.Next());
        logic.InitializeFromProto(proto);

        Assert.Equal(3, logic.MaxOutputQueueSize);
    }

    [Fact]
    public void InitializeFromProto_InnLogic_SetsInnSpecificFields()
    {
        var proto = new InnProto
        {
            Id = "inn_test",
            MaxOccupants = 6,
            RestRecipeId = "rest_advanced"
        };
        var logic = new InnLogic(EntityId.Next());
        logic.InitializeFromProto(proto);

        Assert.Equal(6, logic.MaxOccupants);
        Assert.Equal("rest_advanced", logic.RestRecipeId);
    }

    [Fact]
    public void InitializeFromProto_StockpileLogic_SetsCapacityAndFilter()
    {
        var proto = new StockpileProto
        {
            Id = "stockpile_1",
            MaxCapacity = 200,
            AcceptedItemId = "logs"
        };
        var logic = new StockpileLogic(EntityId.Next());
        logic.InitializeFromProto(proto);

        Assert.Equal(200, logic.MaxCapacity);
        Assert.Equal("logs", logic.AcceptedItemId);
    }

    [Fact]
    public void InitializeFromProto_GatheringLogic_SetsGatheringFields()
    {
        var proto = new GatheringProtoBase
        {
            Id = "forestry_1",
            TargetResourceId = "wood",
            RequiredBiome = BiomeType.Forest,
            GatherAmountPerCycle = 3,
            GatherInterval = 2.5f
        };
        var logic = new ForestryRecipeEntity(EntityId.Next());
        logic.MaxWorkerCapacity = 3;
        logic.AllocateSlots();
        logic.InitializeFromProto(proto);

        Assert.Equal("forestry_1", logic.ProtoId);
        Assert.Equal("wood", logic.TargetResourceId);
        Assert.Equal(BiomeType.Forest, logic.RequiredBiome);
        Assert.Equal(3, logic.GatherAmountPerCycle);
        Assert.Equal(2.5f, logic.GatherInterval);
    }

    [Fact]
    public void InitializeFromProto_VillageSpawnerLogic_SetsSpawnerFields()
    {
        var proto = new VillageSpawnerProto
        {
            Id = "spawner_1",
            SpawnInterval = 10.0f,
            MaxVillagers = 5,
            DefaultVillagerProtoId = "villager_basic"
        };
        var logic = new VillageSpawnerLogic(EntityId.Next());
        logic.InitializeFromProto(proto);

        Assert.Equal("spawner_1", logic.ProtoId);
        Assert.Equal(10.0f, logic.SpawnInterval);
        Assert.Equal(5, logic.MaxVillagers);
        Assert.Equal("villager_basic", logic.DefaultVillagerProtoId);
    }

    [Fact]
    public void InitializeFromProto_CraftStationLogic_CallsBase()
    {
        var proto = new CraftStationProto
        {
            Id = "craft_1",
            ProcessingDuration = 8.0f,
            Tier = 3
        };
        var logic = new CraftStationLogic(EntityId.Next());
        logic.InitializeFromProto(proto);

        Assert.Equal("craft_1", logic.ProtoId);
        Assert.Equal("CraftStation", logic.GetCategoryName());
        Assert.Equal(8.0f, logic.ProcessingDuration);
        Assert.Equal(3, logic.Tier);
    }

    // ── Concrete Structure Type Defaults ───────────────────────────

    [Fact]
    public void InnLogic_Constructor_SetsCategoryName()
    {
        var logic = new InnLogic(EntityId.Next());
        Assert.Equal("Inn", logic.GetCategoryName());
    }

    [Fact]
    public void StockpileLogic_Constructor_SetsCategoryName()
    {
        var logic = new StockpileLogic(EntityId.Next());
        Assert.Equal("Stockpile", logic.GetCategoryName());
    }

    [Fact]
    public void CraftStationLogic_Constructor_SetsCategoryName()
    {
        var logic = new CraftStationLogic(EntityId.Next());
        Assert.Equal("CraftStation", logic.GetCategoryName());
    }

    [Fact]
    public void BalancerLogic_Constructor_SetsCategoryName()
    {
        var logic = new BalancerLogic(EntityId.Next());
        Assert.Equal("Balancer", logic.GetCategoryName());
    }

    [Fact]
    public void VillageSpawnerLogic_Constructor_SetsCategoryName()
    {
        var logic = new VillageSpawnerLogic(EntityId.Next());
        Assert.Equal("Spawner", logic.GetCategoryName());
    }

    // ── VillagerLogic (living entity contract) ───────────────────

    [Fact]
    public void VillagerLogic_IsEntityBase()
    {
        var villager = new VillagerLogic(EntityId.Next());
        Assert.IsAssignableFrom<EntityBase>(villager);
    }

    [Fact]
    public void VillagerLogic_DefaultState()
    {
        var villager = new VillagerLogic(EntityId.Next());
        Assert.Equal(VillagerState.Idle, villager.State);
        Assert.Equal(VillagerJob.Idle, villager.Profession);
        Assert.Equal(VillagerClass.Untrained, villager.TrainedClass);
        Assert.Equal(100f, villager.Stamina);
        Assert.Equal(100f, villager.MaxStamina);
    }

    [Fact]
    public void VillagerLogic_InitializeFromProto_SetsFields()
    {
        var proto = new VillagerProto
        {
            Id = "villager_basic",
            BaseWorkRate = 1.5f,
            BaseMovementSpeed = 3.0f,
            BaseStamina = 80f
        };
        var villager = new VillagerLogic(EntityId.Next());
        villager.InitializeFromProto(proto);

        Assert.Equal("villager_basic", villager.ProtoId);
        Assert.Equal(1.5f, villager.WorkRate);
        Assert.Equal(3.0f, villager.MovementSpeed);
        Assert.Equal(80f, villager.Stamina);
        Assert.Equal(80f, villager.MaxStamina);
    }

    [Fact]
    public void VillagerLogic_Inventory_DefaultsEmpty()
    {
        var villager = new VillagerLogic(EntityId.Next());
        Assert.Empty(villager.Inventory);
        Assert.Equal(4, villager.MaxInventorySlots);
    }

    // ── HeroEntity (living entity contract) ──────────────────────

    [Fact]
    public void HeroEntity_IsEntityBase()
    {
        var hero = new HeroEntity(EntityId.Next());
        Assert.IsAssignableFrom<EntityBase>(hero);
    }

    [Fact]
    public void HeroEntity_DefaultState()
    {
        var hero = new HeroEntity(EntityId.Next());
        Assert.Equal(HeroState.OnPath, hero.State);
        Assert.Equal(WorkerProfession.None, hero.Profession);
        Assert.Equal(100f, hero.Stamina);
        Assert.Equal(100f, hero.MaxStamina);
        Assert.Equal(100f, hero.ToolDurability);
        Assert.Equal(100f, hero.MaxToolDurability);
    }

    [Fact]
    public void HeroEntity_ImplementsIResettable()
    {
        var hero = new HeroEntity(EntityId.Next());
        Assert.IsAssignableFrom<IResettable>(hero);
    }

    // ── PathSegmentLogic (non-structure entity contract) ───────────

    [Fact]
    public void PathSegmentLogic_IsEntityBase()
    {
        var seg = new PathSegmentLogic(EntityId.Next());
        Assert.IsAssignableFrom<EntityBase>(seg);
    }

    [Fact]
    public void PathSegmentLogic_IsNotEntityFootprint()
    {
        var seg = new PathSegmentLogic(EntityId.Next());
        Assert.False(typeof(Structure).IsAssignableFrom(seg.GetType()));
    }

    [Fact]
    public void PathSegmentLogic_ImplementsIRotatable()
    {
        var seg = new PathSegmentLogic(EntityId.Next());
        Assert.IsAssignableFrom<IRotatable>(seg);
    }

    // ── PathGateLogic (non-structure entity contract) ──────────────

    [Fact]
    public void PathGateLogic_IsEntityBase()
    {
        var gate = new PathGateLogic(EntityId.Next());
        Assert.IsAssignableFrom<EntityBase>(gate);
    }

    [Fact]
    public void PathGateLogic_IsEntityFootprintBase()
    {
        var gate = new PathGateLogic(EntityId.Next());
        Assert.True(gate is StructureBase);
    }

    [Fact]
    public void PathGateLogic_ImplementsIRotatable()
    {
        var gate = new PathGateLogic(EntityId.Next());
        Assert.IsAssignableFrom<IRotatable>(gate);
    }

    [Fact]
    public void PathGateLogic_DefaultMode_IsEntrance()
    {
        var gate = new PathGateLogic(EntityId.Next());
        Assert.Equal(PathGateMode.Entrance, gate.Mode);
    }

    // ── Type Hierarchy Verification ──────────────────────────────

    [Fact]
    public void StructureProtoBase_InheritsProtoBase()
    {
        Assert.True(typeof(ProtoBase).IsAssignableFrom(typeof(StructureProtoBase)));
    }

    [Fact]
    public void EntityFootprint_ImplementsIEntityWithExits()
    {
        Assert.True(typeof(IStructureWithExits).IsAssignableFrom(typeof(Structure)));
    }

    [Fact]
    public void GatheringLogicBase_InheritsEntityFootprint()
    {
        Assert.True(typeof(Structure).IsAssignableFrom(typeof(GatheringLogicBase)));
    }

    [Fact]
    public void GatheringLogicBase_InheritsRecipeEntity()
    {
        Assert.True(typeof(RecipeEntity).IsAssignableFrom(typeof(GatheringLogicBase)));
    }

    [Fact]
    public void GatheringLogicBase_DoesNotInheritActivityEntity()
    {
        Assert.False(typeof(ActivityEntity).IsAssignableFrom(typeof(GatheringLogicBase)));
    }

    [Fact]
    public void FusionAltarLogic_InheritsRecipeEntity()
    {
        Assert.True(typeof(RecipeEntity).IsAssignableFrom(typeof(FusionAltarLogic)));
    }

    [Fact]
    public void FusionAltarLogic_DoesNotInheritActivityEntity()
    {
        Assert.False(typeof(ActivityEntity).IsAssignableFrom(typeof(FusionAltarLogic)));
    }

    [Fact]
    public void ForestryStructureLogic_InheritsGatheringLogicBase()
    {
        Assert.True(typeof(GatheringLogicBase).IsAssignableFrom(typeof(ForestryRecipeEntity)));
    }

    [Fact]
    public void VillagerLogic_DoesNotInheritEntityFootprint()
    {
        Assert.False(typeof(Structure).IsAssignableFrom(typeof(VillagerLogic)));
    }

    [Fact]
    public void HeroEntity_DoesNotInheritEntityFootprint()
    {
        Assert.False(typeof(Structure).IsAssignableFrom(typeof(HeroEntity)));
    }

    // ══════════════════════════════════════════════════════════════
    // Phase 1 — New Base Class Hierarchy Tests
    // ══════════════════════════════════════════════════════════════

    // ── Test stubs for abstract classes ───────────────────────────

    private sealed class TestStructure : Structure
    {
        private readonly GridPosRPG[] _footprint;

        public TestStructure(params GridPosRPG[] tiles) : base(EntityId.Next())
        {
            _footprint = tiles.Length > 0 ? tiles : new[] { Position };
        }

        public override IReadOnlyList<GridPosRPG> GetFootprint() => _footprint;
        public override void Tick(float deltaTime) { }
    }

    private sealed class TestLivingEntity : LivingEntity
    {
        public TestLivingEntity() : base(EntityId.Next()) { }
        public bool Ticked { get; private set; }
        public override void Tick(float deltaTime) { Ticked = true; }
    }

    // ── EntityBase ───────────────────────────────────────────────

    [Fact]
    public void EntityBase_AssignsUniqueIds()
    {
        var a = new TestLivingEntity();
        var b = new TestLivingEntity();
        Assert.NotEqual(a.Id, b.Id);
        Assert.True(a.Id > 0);
        Assert.True(b.Id > 0);
    }

    [Fact]
    public void EntityBase_DefaultsToActive()
    {
        var entity = new TestLivingEntity();
        Assert.True(entity.IsActive);
    }

    [Fact]
    public void EntityBase_ProtoIdDefaultsToEmpty()
    {
        var entity = new TestLivingEntity();
        Assert.Equal(string.Empty, entity.ProtoId);
    }

    [Fact]
    public void EntityBase_PositionDefaultsToOrigin()
    {
        var entity = new TestStructure();
        Assert.Equal(new GridPosRPG(0, 0), entity.Position);
    }

    [Fact]
    public void EntityBase_PositionIsSettable()
    {
        var entity = new TestLivingEntity { Position = new GridPosRPG(5, 10) };
        Assert.Equal(new GridPosRPG(5, 10), entity.Position);
    }

    [Fact]
    public void EntityBase_ResetIdCounter_ResetsSequence()
    {
        var first = new TestLivingEntity();
        EntityBase.ResetIdCounter();
        var afterReset = new TestLivingEntity();
        Assert.Equal(1ul, afterReset.Id);
    }

    [Fact]
    public void EntityBase_TickIsCallable()
    {
        var entity = new TestLivingEntity();
        entity.Tick(0.016f);
        Assert.True(entity.Ticked);
    }

    // ── EntityStatic / StructureBase / Structure ─────

    [Fact]
    public void EntityFootprint_GetFootprint_ReturnsTiles()
    {
        var tiles = new[] { new GridPosRPG(1, 2), new GridPosRPG(1, 3) };
        var building = new TestStructure(tiles);
        var footprint = building.GetFootprint();
        Assert.Equal(2, footprint.Count);
        Assert.Equal(new GridPosRPG(1, 2), footprint[0]);
        Assert.Equal(new GridPosRPG(1, 3), footprint[1]);
    }

    [Fact]
    public void EntityFootprint_InheritsEntityFootprintBase()
    {
        Assert.True(typeof(StructureBase).IsAssignableFrom(typeof(Structure)));
    }

    [Fact]
    public void EntityFootprintBase_InheritsEntityStatic()
    {
        Assert.True(typeof(EntityStatic).IsAssignableFrom(typeof(StructureBase)));
    }

    [Fact]
    public void EntityStatic_InheritsEntityBase()
    {
        Assert.True(typeof(EntityBase).IsAssignableFrom(typeof(EntityStatic)));
    }

    [Fact]
    public void EntityFootprint_ImplementsIEntityWithFootprint()
    {
        Assert.True(typeof(IEntityWithFootprint).IsAssignableFrom(typeof(Structure)));
    }

    [Fact]
    public void EntityFootprint_IsAbstract()
    {
        Assert.True(typeof(Structure).IsAbstract);
    }

    [Fact]
    public void EntityFootprintBase_IsAbstract()
    {
        Assert.True(typeof(StructureBase).IsAbstract);
    }

    [Fact]
    public void EntityStatic_IsAbstract()
    {
        Assert.True(typeof(EntityStatic).IsAbstract);
    }

    // ── LivingEntity ─────────────────────────────────────────────

    [Fact]
    public void LivingEntity_InheritsEntityBase()
    {
        Assert.True(typeof(EntityBase).IsAssignableFrom(typeof(LivingEntity)));
    }

    [Fact]
    public void LivingEntity_IsAbstract()
    {
        Assert.True(typeof(LivingEntity).IsAbstract);
    }

    [Fact]
    public void LivingEntity_DefaultMovementSpeed()
    {
        var entity = new TestLivingEntity();
        Assert.Equal(2.0f, entity.MovementSpeed);
    }

    [Fact]
    public void LivingEntity_DefaultStamina()
    {
        var entity = new TestLivingEntity();
        Assert.Equal(100f, entity.Stamina);
        Assert.Equal(100f, entity.MaxStamina);
    }

    [Fact]
    public void LivingEntity_PathSegmentIdDefaultsToNull()
    {
        var entity = new TestLivingEntity();
        Assert.Null(entity.CurrentPathSegmentId);
    }

    [Fact]
    public void LivingEntity_PathProgressDefaultsToZero()
    {
        var entity = new TestLivingEntity();
        Assert.Equal(0f, entity.PathProgress);
    }

    [Fact]
    public void LivingEntity_StaminaIsSettable()
    {
        var entity = new TestLivingEntity { Stamina = 50f, MaxStamina = 200f };
        Assert.Equal(50f, entity.Stamina);
        Assert.Equal(200f, entity.MaxStamina);
    }

    [Fact]
    public void LivingEntity_PathFieldsAreSettable()
    {
        var entity = new TestLivingEntity
        {
            CurrentPathSegmentId = 42,
            PathProgress = 0.75f
        };
        Assert.Equal(42ul, entity.CurrentPathSegmentId);
        Assert.Equal(0.75f, entity.PathProgress);
    }

    // ── Hierarchy isolation ──────────────────────────────────────

    [Fact]
    public void LivingEntity_DoesNotInheritEntityStatic()
    {
        Assert.False(typeof(EntityStatic).IsAssignableFrom(typeof(LivingEntity)));
    }

    [Fact]
    public void EntityFootprint_DoesNotInheritLivingEntity()
    {
        Assert.False(typeof(LivingEntity).IsAssignableFrom(typeof(Structure)));
    }

    [Fact]
    public void EntityBase_IsAbstract()
    {
        Assert.True(typeof(EntityBase).IsAbstract);
    }

    // ══════════════════════════════════════════════════════════════
    // Phase 2 — Domain Base Class Tests
    // ══════════════════════════════════════════════════════════════

    // ── Test stubs for domain base classes ────────────────────────

    private sealed class TestRecipeEntity : RecipeEntity
    {
        public TestRecipeEntity() : base(EntityId.Next()) { }
        public override IReadOnlyList<GridPosRPG> GetFootprint() => new[] { Position };
        public override void Tick(float deltaTime) { }
    }

    private sealed class TestActivityEntity : ActivityEntity
    {
        public TestActivityEntity() : base(EntityId.Next()) { }
        public override IReadOnlyList<GridPosRPG> GetFootprint() => new[] { Position };
        public override void Tick(float deltaTime) { }
    }

    private sealed class TestStockpileEntity : StockpileEntity
    {
        public TestStockpileEntity() : base(EntityId.Next()) { }
        public override IReadOnlyList<GridPosRPG> GetFootprint() => new[] { Position };
        public override void Tick(float deltaTime) { }
    }

    private sealed class TestNPCEntity : NPCEntity
    {
        public TestNPCEntity() : base(EntityId.Next()) { }
        public override void Tick(float deltaTime) { }
    }

    // ── RecipeEntity ─────────────────────────────────────────────

    [Fact]
    public void RecipeEntity_InheritsEntityFootprint()
    {
        Assert.True(typeof(Structure).IsAssignableFrom(typeof(RecipeEntity)));
    }

    [Fact]
    public void RecipeEntity_IsAbstract()
    {
        Assert.True(typeof(RecipeEntity).IsAbstract);
    }

    [Fact]
    public void RecipeEntity_DefaultActiveRecipeId_IsNull()
    {
        var entity = new TestRecipeEntity();
        Assert.Null(entity.ActiveRecipeId);
    }

    [Fact]
    public void RecipeEntity_DefaultProcessingDuration()
    {
        var entity = new TestRecipeEntity();
        Assert.Equal(2.0f, entity.ProcessingDuration);
    }

    [Fact]
    public void RecipeEntity_DefaultProcessingTimer_IsZero()
    {
        var entity = new TestRecipeEntity();
        Assert.Equal(0f, entity.ProcessingTimer);
    }

    [Fact]
    public void RecipeEntity_HasActiveRecipe_FalseWhenNull()
    {
        var entity = new TestRecipeEntity();
        Assert.False(entity.HasActiveRecipe);
    }

    [Fact]
    public void RecipeEntity_HasActiveRecipe_TrueWhenSet()
    {
        var entity = new TestRecipeEntity { ActiveRecipeId = "recipe_sword" };
        Assert.True(entity.HasActiveRecipe);
    }

    [Fact]
    public void RecipeEntity_ImplementsIEntityWithFootprint()
    {
        Assert.True(typeof(IEntityWithFootprint).IsAssignableFrom(typeof(RecipeEntity)));
    }

    // ── ActivityEntity ───────────────────────────────────────────

    [Fact]
    public void ActivityEntity_InheritsEntityFootprint()
    {
        Assert.True(typeof(Structure).IsAssignableFrom(typeof(ActivityEntity)));
    }

    [Fact]
    public void ActivityEntity_IsAbstract()
    {
        Assert.True(typeof(ActivityEntity).IsAbstract);
    }

    [Fact]
    public void ActivityEntity_DefaultMaxOccupants()
    {
        var entity = new TestActivityEntity();
        Assert.Equal(4, entity.MaxOccupants);
    }

    [Fact]
    public void ActivityEntity_CurrentOccupantIds_DefaultsEmpty()
    {
        var entity = new TestActivityEntity();
        Assert.NotNull(entity.CurrentOccupantIds);
        Assert.Empty(entity.CurrentOccupantIds);
    }

    [Fact]
    public void ActivityEntity_OccupantCount_Zero_WhenEmpty()
    {
        var entity = new TestActivityEntity();
        Assert.Equal(0, entity.OccupantCount);
    }

    [Fact]
    public void ActivityEntity_CanAcceptOccupant_TrueWhenEmpty()
    {
        var entity = new TestActivityEntity();
        Assert.True(entity.CanAcceptOccupant);
    }

    [Fact]
    public void ActivityEntity_CanAcceptOccupant_FalseWhenFull()
    {
        var entity = new TestActivityEntity { MaxOccupants = 2 };
        entity.CurrentOccupantIds.Add(new EntityId(1));
        entity.CurrentOccupantIds.Add(new EntityId(2));
        Assert.False(entity.CanAcceptOccupant);
    }

    [Fact]
    public void ActivityEntity_OccupantCount_TracksAdditions()
    {
        var entity = new TestActivityEntity();
        entity.CurrentOccupantIds.Add(new EntityId(10));
        entity.CurrentOccupantIds.Add(new EntityId(20));
        Assert.Equal(2, entity.OccupantCount);
    }

    // ── StockpileEntity ──────────────────────────────────────────

    [Fact]
    public void StockpileEntity_InheritsEntityFootprint()
    {
        Assert.True(typeof(Structure).IsAssignableFrom(typeof(StockpileEntity)));
    }

    [Fact]
    public void StockpileEntity_IsAbstract()
    {
        Assert.True(typeof(StockpileEntity).IsAbstract);
    }

    [Fact]
    public void StockpileEntity_DefaultMaxCapacity()
    {
        var entity = new TestStockpileEntity();
        Assert.Equal(100, entity.MaxCapacity);
    }

    [Fact]
    public void StockpileEntity_StoredResources_DefaultsEmpty()
    {
        var entity = new TestStockpileEntity();
        Assert.NotNull(entity.StoredResources);
        Assert.Empty(entity.StoredResources);
    }

    [Fact]
    public void StockpileEntity_TotalStored_ZeroWhenEmpty()
    {
        var entity = new TestStockpileEntity();
        Assert.Equal(0, entity.TotalStored);
    }

    [Fact]
    public void StockpileEntity_TotalStored_SumsAllResources()
    {
        var entity = new TestStockpileEntity();
        entity.StoredResources["wood"] = 10;
        entity.StoredResources["ore"] = 5;
        Assert.Equal(15, entity.TotalStored);
    }

    [Fact]
    public void StockpileEntity_HasRoom_TrueWhenBelowCapacity()
    {
        var entity = new TestStockpileEntity { MaxCapacity = 20 };
        entity.StoredResources["wood"] = 10;
        Assert.True(entity.HasRoom);
    }

    [Fact]
    public void StockpileEntity_HasRoom_FalseWhenAtCapacity()
    {
        var entity = new TestStockpileEntity { MaxCapacity = 10 };
        entity.StoredResources["wood"] = 10;
        Assert.False(entity.HasRoom);
    }

    // ── NPCEntity ────────────────────────────────────────────────

    [Fact]
    public void NPCEntity_InheritsLivingEntity()
    {
        Assert.True(typeof(LivingEntity).IsAssignableFrom(typeof(NPCEntity)));
    }

    [Fact]
    public void NPCEntity_IsAbstract()
    {
        Assert.True(typeof(NPCEntity).IsAbstract);
    }

    [Fact]
    public void NPCEntity_DefaultLevel()
    {
        var entity = new TestNPCEntity();
        Assert.Equal(1, entity.Level);
    }

    [Fact]
    public void NPCEntity_Traits_DefaultsEmpty()
    {
        var entity = new TestNPCEntity();
        Assert.NotNull(entity.Traits);
        Assert.Empty(entity.Traits);
    }

    [Fact]
    public void NPCEntity_InheritsLivingEntityProperties()
    {
        var entity = new TestNPCEntity
        {
            MovementSpeed = 3.5f,
            Stamina = 75f,
            MaxStamina = 150f,
            CurrentPathSegmentId = 99,
            PathProgress = 0.5f
        };
        Assert.Equal(3.5f, entity.MovementSpeed);
        Assert.Equal(75f, entity.Stamina);
        Assert.Equal(150f, entity.MaxStamina);
        Assert.Equal(99ul, entity.CurrentPathSegmentId);
        Assert.Equal(0.5f, entity.PathProgress);
    }

    // ── Domain hierarchy isolation ───────────────────────────────

    [Fact]
    public void RecipeEntity_DoesNotInheritActivityEntity()
    {
        Assert.False(typeof(ActivityEntity).IsAssignableFrom(typeof(RecipeEntity)));
    }

    [Fact]
    public void ActivityEntity_DoesNotInheritRecipeEntity()
    {
        Assert.False(typeof(RecipeEntity).IsAssignableFrom(typeof(ActivityEntity)));
    }

    [Fact]
    public void StockpileEntity_DoesNotInheritRecipeEntity()
    {
        Assert.False(typeof(RecipeEntity).IsAssignableFrom(typeof(StockpileEntity)));
    }

    [Fact]
    public void StockpileEntity_DoesNotInheritActivityEntity()
    {
        Assert.False(typeof(ActivityEntity).IsAssignableFrom(typeof(StockpileEntity)));
    }

    [Fact]
    public void NPCEntity_DoesNotInheritEntityFootprint()
    {
        Assert.False(typeof(Structure).IsAssignableFrom(typeof(NPCEntity)));
    }

    [Fact]
    public void NPCEntity_DoesNotInheritEntityStatic()
    {
        Assert.False(typeof(EntityStatic).IsAssignableFrom(typeof(NPCEntity)));
    }

    [Fact]
    public void RecipeEntity_DoesNotInheritLivingEntity()
    {
        Assert.False(typeof(LivingEntity).IsAssignableFrom(typeof(RecipeEntity)));
    }

    // ══════════════════════════════════════════════════════════════
    // Phase 3 — Concrete Type Migration Tests
    // ══════════════════════════════════════════════════════════════

    // ── PathGateLogic hierarchy ──────────────────────────────────

    [Fact]
    public void PathGateLogic_InheritsEntityFootprintBase()
    {
        Assert.True(typeof(StructureBase).IsAssignableFrom(typeof(PathGateLogic)));
    }

    [Fact]
    public void PathGateLogic_InheritsEntityBase()
    {
        Assert.True(typeof(EntityBase).IsAssignableFrom(typeof(PathGateLogic)));
    }

    [Fact]
    public void PathGateLogic_ImplementsIEntityWithFootprint()
    {
        Assert.True(typeof(IEntityWithFootprint).IsAssignableFrom(typeof(PathGateLogic)));
    }

    [Fact]
    public void PathGateLogic_GetFootprint_ReturnsPosition()
    {
        var gate = new PathGateLogic(EntityId.Next()) { Position = new GridPosRPG(3, 4) };
        var footprint = gate.GetFootprint();
        Assert.Single(footprint);
        Assert.Equal(new GridPosRPG(3, 4), footprint[0]);
    }

    // ── PathSegmentLogic hierarchy

    [Fact]
    public void PathSegmentLogic_InheritsEntityFootprintBase()
    {
        Assert.True(typeof(StructureBase).IsAssignableFrom(typeof(PathSegmentLogic)));
    }

    [Fact]
    public void PathSegmentLogic_InheritsEntityBase()
    {
        Assert.True(typeof(EntityBase).IsAssignableFrom(typeof(PathSegmentLogic)));
    }

    [Fact]
    public void PathSegmentLogic_ImplementsIEntityWithFootprint()
    {
        Assert.True(typeof(IEntityWithFootprint).IsAssignableFrom(typeof(PathSegmentLogic)));
    }

    [Fact]
    public void PathSegmentLogic_DoesNotInheritEntityFootprint()
    {
        // PathSegments are footprint entities but NOT buildings.
        Assert.False(typeof(Structure).IsAssignableFrom(typeof(PathSegmentLogic)));
    }

    [Fact]
    public void PathSegmentLogic_GetFootprint_ReturnsPosition()
    {
        var seg = new PathSegmentLogic(EntityId.Next()) { Position = new GridPosRPG(1, 2) };
        var footprint = seg.GetFootprint();
        Assert.Single(footprint);
        Assert.Equal(new GridPosRPG(1, 2), footprint[0]);
    }

    // ── ResourceNodeLogic hierarchy

    [Fact]
    public void ResourceNodeLogic_InheritsEntityFootprintBase()
    {
        Assert.True(typeof(StructureBase).IsAssignableFrom(typeof(ResourceNodeLogic)));
    }

    [Fact]
    public void ResourceNodeLogic_InheritsEntityBase()
    {
        Assert.True(typeof(EntityBase).IsAssignableFrom(typeof(ResourceNodeLogic)));
    }

    [Fact]
    public void ResourceNodeLogic_ImplementsIEntityWithFootprint()
    {
        Assert.True(typeof(IEntityWithFootprint).IsAssignableFrom(typeof(ResourceNodeLogic)));
    }

    [Fact]
    public void ResourceNodeLogic_GetFootprint_ReturnsPosition()
    {
        var node = new ResourceNodeLogic(EntityId.Next()) { Position = new GridPosRPG(7, 8) };
        var footprint = node.GetFootprint();
        Assert.Single(footprint);
        Assert.Equal(new GridPosRPG(7, 8), footprint[0]);
    }

    // ── HeroEntity hierarchy

    [Fact]
    public void HeroEntity_InheritsPlayerEntity()
    {
        Assert.True(typeof(PlayerEntity).IsAssignableFrom(typeof(HeroEntity)));
    }

    [Fact]
    public void HeroEntity_DoesNotInheritNPCEntity()
    {
        Assert.False(typeof(NPCEntity).IsAssignableFrom(typeof(HeroEntity)));
    }

    [Fact]
    public void PlayerEntity_InheritsLivingEntity()
    {
        Assert.True(typeof(LivingEntity).IsAssignableFrom(typeof(PlayerEntity)));
    }

    [Fact]
    public void HeroEntity_InheritsLivingEntity()
    {
        Assert.True(typeof(LivingEntity).IsAssignableFrom(typeof(HeroEntity)));
    }

    [Fact]
    public void HeroEntity_InheritsEntityBase()
    {
        Assert.True(typeof(EntityBase).IsAssignableFrom(typeof(HeroEntity)));
    }

    [Fact]
    public void HeroEntity_InheritsLivingEntityProperties()
    {
        // Properties that were previously on HeroEntity are now inherited from LivingEntity/NPCEntity.
        var hero = new HeroEntity(EntityId.Next());
        Assert.Equal(100f, hero.Stamina);       // from LivingEntity
        Assert.Equal(100f, hero.MaxStamina);     // from LivingEntity
        Assert.Equal(2.0f, hero.MovementSpeed);  // from LivingEntity (new — was not on HeroEntity before)
        Assert.Null(hero.CurrentPathSegmentId);  // from LivingEntity
        Assert.Equal(0f, hero.PathProgress);     // from LivingEntity
        Assert.Equal(1, hero.Level);             // from LivingEntity
        Assert.NotNull(hero.Traits);             // from LivingEntity
        Assert.Empty(hero.Traits);
    }

    // ── VillagerLogic hierarchy ──────────────────────────────────

    [Fact]
    public void VillagerLogic_InheritsNPCEntity()
    {
        Assert.True(typeof(NPCEntity).IsAssignableFrom(typeof(VillagerLogic)));
    }

    [Fact]
    public void VillagerLogic_InheritsLivingEntity()
    {
        Assert.True(typeof(LivingEntity).IsAssignableFrom(typeof(VillagerLogic)));
    }

    [Fact]
    public void VillagerLogic_InheritsEntityBase()
    {
        Assert.True(typeof(EntityBase).IsAssignableFrom(typeof(VillagerLogic)));
    }

    [Fact]
    public void VillagerLogic_InheritsLivingEntityProperties()
    {
        // Properties that were previously on VillagerLogic are now inherited from LivingEntity/NPCEntity.
        var villager = new VillagerLogic(EntityId.Next());
        Assert.Equal(100f, villager.Stamina);         // from LivingEntity
        Assert.Equal(100f, villager.MaxStamina);       // from LivingEntity
        Assert.Equal(2.0f, villager.MovementSpeed);    // from LivingEntity
        Assert.Null(villager.CurrentPathSegmentId);    // from LivingEntity
        Assert.Equal(0f, villager.PathProgress);       // from LivingEntity
        Assert.Equal(1, villager.Level);               // from LivingEntity
        Assert.NotNull(villager.Traits);               // from LivingEntity
        Assert.Empty(villager.Traits);
    }

    // ── Structure hierarchy ───────────────────────────────

    [Fact]
    public void AllDomainBases_InheritEntityFootprint_ViaDomainBase()
    {
        Assert.True(typeof(Structure).IsAssignableFrom(typeof(RecipeEntity)));
        Assert.True(typeof(Structure).IsAssignableFrom(typeof(ActivityEntity)));
        Assert.True(typeof(Structure).IsAssignableFrom(typeof(StockpileEntity)));
        // RoutingNodeBase now inherits StructureBase directly (not Structure)
        Assert.True(typeof(StructureBase).IsAssignableFrom(typeof(RoutingNodeBase)));
    }

    [Fact]
    public void AllDomainBases_InheritEntityFootprint()
    {
        Assert.True(typeof(Structure).IsAssignableFrom(typeof(RecipeEntity)));
        Assert.True(typeof(Structure).IsAssignableFrom(typeof(ActivityEntity)));
        Assert.True(typeof(Structure).IsAssignableFrom(typeof(StockpileEntity)));
        // RoutingNodeBase now inherits StructureBase directly (not Structure)
        Assert.True(typeof(StructureBase).IsAssignableFrom(typeof(RoutingNodeBase)));
    }

    [Fact]
    public void EntityFootprint_GetFootprint_ViaConcreteType()
    {
        var inn = new InnLogic(EntityId.Next());
        inn.Position = new GridPosRPG(5, 5);
        var footprint = ((IEntityWithFootprint)inn).GetFootprint();
        Assert.Single(footprint);
        Assert.Equal(new GridPosRPG(5, 5), footprint[0]);
    }

    // ── All concrete structure types still work through Structure ──

    [Fact]
    public void AllStructureTypes_InheritEntityBase()
    {
        Assert.True(typeof(EntityBase).IsAssignableFrom(typeof(ForestryRecipeEntity)));
        Assert.True(typeof(EntityBase).IsAssignableFrom(typeof(MiningRecipeEntity)));
        Assert.True(typeof(EntityBase).IsAssignableFrom(typeof(InnLogic)));
        Assert.True(typeof(EntityBase).IsAssignableFrom(typeof(StockpileLogic)));
        Assert.True(typeof(EntityBase).IsAssignableFrom(typeof(CraftStationLogic)));
        Assert.True(typeof(EntityBase).IsAssignableFrom(typeof(VillageSpawnerLogic)));
        Assert.True(typeof(EntityBase).IsAssignableFrom(typeof(TrainingBuildingLogic)));
        Assert.True(typeof(EntityBase).IsAssignableFrom(typeof(BalancerLogic)));
        Assert.True(typeof(EntityBase).IsAssignableFrom(typeof(CheckGateLogic)));
        Assert.True(typeof(EntityBase).IsAssignableFrom(typeof(FilterSplitterLogic)));
        Assert.True(typeof(EntityBase).IsAssignableFrom(typeof(DungeonPortalLogic)));
        Assert.True(typeof(EntityBase).IsAssignableFrom(typeof(ToolStationLogic)));
        Assert.True(typeof(EntityBase).IsAssignableFrom(typeof(ArmoryLogic)));
        Assert.True(typeof(EntityBase).IsAssignableFrom(typeof(AcademyLogic)));
        Assert.True(typeof(EntityBase).IsAssignableFrom(typeof(JobChangerLogic)));
    }

    [Fact]
    public void AllStructureTypes_ImplementIEntityWithFootprint()
    {
        Assert.True(typeof(IEntityWithFootprint).IsAssignableFrom(typeof(ForestryRecipeEntity)));
        Assert.True(typeof(IEntityWithFootprint).IsAssignableFrom(typeof(MiningRecipeEntity)));
        Assert.True(typeof(IEntityWithFootprint).IsAssignableFrom(typeof(InnLogic)));
        Assert.True(typeof(IEntityWithFootprint).IsAssignableFrom(typeof(StockpileLogic)));
        Assert.True(typeof(IEntityWithFootprint).IsAssignableFrom(typeof(CraftStationLogic)));
        Assert.True(typeof(IEntityWithFootprint).IsAssignableFrom(typeof(VillageSpawnerLogic)));
    }

    }
