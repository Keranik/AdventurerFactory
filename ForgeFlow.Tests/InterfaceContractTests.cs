using ForgeFlow.Core.Data;
using ForgeFlow.Core.Entities;
using ForgeFlow.Core.Proto;
using ForgeFlow.Core.Proto.Logic;
using ForgeFlow.Core.Proto.Prototypes;
using ForgeFlow.Core.Systems;
using ForgeFlow.Core.Theming;

namespace ForgeFlow.Tests;

/// <summary>
/// Verifies that all registries implement IRegistry and all systems/managers implement IGameSystem.
/// </summary>
public class InterfaceContractTests
{
    // ── IRegistry ────────────────────────────────────────────────────

    [Fact]
    public void ItemRegistry_ImplementsIRegistry()
    {
        IRegistry registry = new ItemRegistry();
        Assert.Equal(0, registry.Count);
        registry.Clear();
    }

    [Fact]
    public void ClassRegistry_ImplementsIRegistry()
    {
        IRegistry registry = new ClassRegistry();
        Assert.Equal(0, registry.Count);
        registry.Clear();
    }

    [Fact]
    public void RecipeRegistry_ImplementsIRegistry()
    {
        IRegistry registry = new RecipeRegistry();
        Assert.Equal(0, registry.Count);
        registry.Clear();
    }

    [Fact]
    public void DungeonRegistry_ImplementsIRegistry()
    {
        IRegistry registry = new DungeonRegistry();
        Assert.Equal(0, registry.Count);
        registry.Clear();
    }

    [Fact]
    public void AppearanceRegistry_ImplementsIRegistry()
    {
        IRegistry registry = new AppearanceRegistry();
        Assert.Equal(0, registry.Count);
        registry.Clear();
    }

    [Fact]
    public void VillageRegistry_ImplementsIRegistry()
    {
        IRegistry registry = new VillageRegistry();
        Assert.Equal(0, registry.Count);
        registry.Clear();
    }

    [Fact]
    public void ModBrowserRegistry_ImplementsIRegistry()
    {
        IRegistry registry = new ModBrowserRegistry();
        Assert.Equal(0, registry.Count);
        registry.Clear();
    }

    [Fact]
    public void ThemeRegistry_ImplementsIRegistry()
    {
        IRegistry registry = new ThemeRegistry();
        Assert.Equal(0, registry.Count);
        registry.Clear();
    }

    [Fact]
    public void ProtoRegistry_ImplementsIRegistry()
    {
        IRegistry registry = new ProtoRegistry();
        Assert.Equal(0, registry.Count);
        registry.Clear();
    }

    [Fact]
    public void ProtoRegistry_Clear_RemovesAllEntries()
    {
        var reg = new ProtoRegistry();
        reg.RegisterDefaults();
        Assert.True(reg.Count > 0);

        reg.Clear();
        Assert.Equal(0, reg.Count);
        Assert.Equal(0, reg.StructureCount);
        Assert.Equal(0, reg.TutorialCount);
    }

    // ── IGameSystem ──────────────────────────────────────────────────

    [Fact]
    public void SimulationTicker_ImplementsIGameSystem()
    {
        Assert.True(typeof(IGameSystem).IsAssignableFrom(typeof(SimulationTicker)));
    }

    [Fact]
    public void PathTrafficSystem_ImplementsIGameSystem()
    {
        Assert.True(typeof(IGameSystem).IsAssignableFrom(typeof(PathTrafficSystem)));
    }

    [Fact]
    public void VillagerSystem_ImplementsIGameSystem()
    {
        Assert.True(typeof(IGameSystem).IsAssignableFrom(typeof(VillagerSystem)));
    }

    [Fact]
    public void TileManager_ImplementsIGameSystem()
    {
        Assert.True(typeof(IGameSystem).IsAssignableFrom(typeof(TileManager)));
    }

    [Fact]
    public void EntityManager_ImplementsIGameSystem()
    {
        Assert.True(typeof(IGameSystem).IsAssignableFrom(typeof(EntityManager)));
    }

    [Fact]
    public void PathManager_ImplementsIGameSystem()
    {
        Assert.True(typeof(IGameSystem).IsAssignableFrom(typeof(PathNodeManager)));
    }

    [Fact]
    public void PathGateManager_ImplementsIGameSystem()
    {
        Assert.True(typeof(IGameSystem).IsAssignableFrom(typeof(PathGateManager)));
    }

    [Fact]
    public void ResourceManager_ImplementsIGameSystem()
    {
        Assert.True(typeof(IGameSystem).IsAssignableFrom(typeof(ItemManager)));
    }

    [Fact]
    public void StructureManager_ImplementsIGameSystem()
    {
        Assert.True(typeof(IGameSystem).IsAssignableFrom(typeof(StructureManager)));
    }

    [Fact]
    public void ResearchManager_ImplementsIGameSystem()
    {
        Assert.True(typeof(IGameSystem).IsAssignableFrom(typeof(ResearchManager)));
    }

    [Fact]
    public void WorldStateManager_ImplementsIGameSystem()
    {
        Assert.True(typeof(IGameSystem).IsAssignableFrom(typeof(WorldStateManager)));
    }

    [Fact]
    public void WorkerLifecycleSystem_ImplementsIGameSystem()
    {
        Assert.True(typeof(IGameSystem).IsAssignableFrom(typeof(WorkerLifecycleSystem)));
    }

    [Fact]
    public void AchievementSystem_ImplementsIGameSystem()
    {
        Assert.True(typeof(IGameSystem).IsAssignableFrom(typeof(AchievementSystem)));
    }

    [Fact]
    public void GameStatistics_ImplementsIGameSystem()
    {
        Assert.True(typeof(IGameSystem).IsAssignableFrom(typeof(GameStatistics)));
    }

    [Fact]
    public void GameStateMachine_ImplementsIGameSystem()
    {
        Assert.True(typeof(IGameSystem).IsAssignableFrom(typeof(GameStateMachine)));
    }

    [Fact]
    public void SettingsManager_ImplementsIGameSystem()
    {
        Assert.True(typeof(IGameSystem).IsAssignableFrom(typeof(SettingsManager)));
    }

    [Fact]
    public void GameplayFlowSystem_ImplementsIGameSystem()
    {
        Assert.True(typeof(IGameSystem).IsAssignableFrom(typeof(GameplayFlowSystem)));
    }

    [Fact]
    public void HotbarSystem_ImplementsIGameSystem()
    {
        Assert.True(typeof(IGameSystem).IsAssignableFrom(typeof(HotbarSystem)));
    }

    [Fact]
    public void AutoEquipSystem_ImplementsIGameSystem()
    {
        Assert.True(typeof(IGameSystem).IsAssignableFrom(typeof(AutoEquipSystem)));
    }

    [Fact]
    public void DungeonResolver_ImplementsIGameSystem()
    {
        Assert.True(typeof(IGameSystem).IsAssignableFrom(typeof(DungeonResolver)));
    }

    [Fact]
    public void FusionCalculator_ImplementsIGameSystem()
    {
        Assert.True(typeof(IGameSystem).IsAssignableFrom(typeof(FusionCalculator)));
    }

    [Fact]
    public void AppearanceApplier_ImplementsIGameSystem()
    {
        Assert.True(typeof(IGameSystem).IsAssignableFrom(typeof(AppearanceApplier)));
    }

    [Fact]
    public void TutorialSystem_ImplementsIGameSystem()
    {
        Assert.True(typeof(IGameSystem).IsAssignableFrom(typeof(TutorialSystem)));
    }

    [Fact]
    public void GatingLimits_ImplementsIGameSystem()
    {
        Assert.True(typeof(IGameSystem).IsAssignableFrom(typeof(GatingLimits)));
    }

    // ── IRotatable ───────────────────────────────────────────────────

    [Fact]
    public void PathGateLogic_ImplementsIRotatable()
    {
        EntityBase.ResetIdCounter();
        IRotatable rotatable = new PathGateLogic(EntityId.Next()) { Facing = Direction.North };
        Assert.Equal(Direction.North, rotatable.Facing);
        rotatable.Rotate();
        Assert.Equal(Direction.East, rotatable.Facing);
    }

    [Fact]
    public void PathSegmentLogic_ImplementsIRotatable()
    {
        EntityBase.ResetIdCounter();
        IRotatable rotatable = new PathSegmentLogic(EntityId.Next()) { Facing = Direction.North };
        Assert.Equal(Direction.North, rotatable.Facing);
        rotatable.Rotate();
        Assert.Equal(Direction.East, rotatable.Facing);
    }

    // ── IResettable ──────────────────────────────────────────────────

    [Fact]
    public void HeroEntity_ImplementsIResettable()
    {
        EntityBase.ResetIdCounter();
        var hero = new HeroEntity(EntityId.Next())
        {
            ClassId = "warrior",
            Level = 5,
            State = HeroState.InDungeon,
            Stamina = 10f,
            ToolDurability = 5f,
            CarryLoad = 30f,
            GoldEarned = 100,
            GuildId = "test_guild"
        };
        hero.Traits.Add("brave");
        hero.Equipment.Add(new EquippedItem { Slot = EquipSlot.Weapon, Tier = 3 });

        IResettable resettable = hero;
        resettable.Reset();

        Assert.Equal(1, hero.Level);
        Assert.Equal(string.Empty, hero.ClassId);
        Assert.Equal(HeroState.OnPath, hero.State);
        Assert.Equal(100f, hero.Stamina);
        Assert.Equal(100f, hero.ToolDurability);
        Assert.Equal(0f, hero.CarryLoad);
        Assert.Equal(0, hero.GoldEarned);
        Assert.Null(hero.GuildId);
        Assert.Empty(hero.Traits);
        Assert.Empty(hero.Equipment);
        Assert.True(hero.IsActive);
    }

    [Fact]
    public void VillagerLogic_ImplementsIResettable()
    {
        EntityBase.ResetIdCounter();
        var villager = new VillagerLogic(EntityId.Next())
        {
            Name = "TestVillager",
            Level = 3,
            Profession = VillagerJob.Lumberjack,
            State = VillagerState.Working,
            TrainedClass = VillagerClass.Warrior,
            Stamina = 20f,
            RoutingTag = "wood_carrier",
            CurrentActivity = "Gathering sticks"
        };
        villager.Traits.Add("strong");
        villager.EquipTool("axe", 80f);

        IResettable resettable = villager;
        resettable.Reset();

        Assert.Equal(string.Empty, villager.Name);
        Assert.Equal(1, villager.Level);
        Assert.Equal(VillagerJob.Idle, villager.Profession);
        Assert.Equal(VillagerState.Idle, villager.State);
        Assert.Equal(VillagerClass.Untrained, villager.TrainedClass);
        Assert.Equal(100f, villager.Stamina);
        Assert.Null(villager.RoutingTag);
        Assert.Null(villager.CurrentActivity);
        Assert.Empty(villager.Traits);
        Assert.Null(villager.EquippedToolId);
        Assert.True(villager.IsActive);
    }
}
