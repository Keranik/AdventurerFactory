using ForgeFlow.Core.Data;
using ForgeFlow.Core.Entities;
using ForgeFlow.Core.Events;
using ForgeFlow.Core.Proto;
using ForgeFlow.Core.Systems;

namespace ForgeFlow.Tests;

/// <summary>Tests for the WorkerLifecycleSystem — wear-out conditions, ability gain, and difficulty-based job resolution.</summary>
public class WorkerLifecycleTests
{
    private static EntityManager CreateDummyEntityManager(EventBus bus)
    {
        var terrain = new TerrainGrid(4, 4);
        terrain.GenerateDefault();
        var tileMgr = new TileManager(terrain);
        var rm = new ItemManager(bus);
        var vs = new VillagerSystem(bus, rm);
        return new EntityManager(tileMgr, vs);
    }

    // ─── Wear-Out Conditions ──────────────────────────────────────

    [Fact]
    public void ForesterWorker_WearOutWhenToolDurabilityReachesZero()
    {
        var hero = new HeroEntity(EntityId.Next()) { Profession = WorkerProfession.Forester, ToolDurability = 5f, AssignedBuildingId = 1 };
        var eventBus = new EventBus();
        var lifecycle = new WorkerLifecycleSystem(eventBus, CreateDummyEntityManager(eventBus), Difficulty.Normal, 42);

        for (int i = 0; i < 180; i++)
        {
            lifecycle.Tick(1f / 60f, new List<HeroEntity> { hero });
        }

        Assert.Equal(HeroState.WornOut, hero.State);
        Assert.Equal(WearOutReason.ToolBroken, hero.LastWearOutReason);
    }

    [Fact]
    public void MinerWorker_WearOutWhenToolBreaks()
    {
        var hero = new HeroEntity(EntityId.Next()) { Profession = WorkerProfession.Miner, ToolDurability = 3f, AssignedBuildingId = 1 };
        var eventBus = new EventBus();
        var lifecycle = new WorkerLifecycleSystem(eventBus, CreateDummyEntityManager(eventBus), Difficulty.Normal, 42);

        for (int i = 0; i < 120; i++)
        {
            lifecycle.Tick(1f / 60f, new List<HeroEntity> { hero });
        }

        Assert.Equal(HeroState.WornOut, hero.State);
        Assert.Equal(WearOutReason.ToolBroken, hero.LastWearOutReason);
    }

    [Fact]
    public void HerbGatherer_WearOutWhenCarryCapacityFull()
    {
        var hero = new HeroEntity(EntityId.Next())
        {
            Profession = WorkerProfession.HerbGatherer,
            CarryLoad = 45f,
            MaxCarryCapacity = 50f,
            Stamina = 100f,
            AssignedBuildingId = 1
        };
        var eventBus = new EventBus();
        var lifecycle = new WorkerLifecycleSystem(eventBus, CreateDummyEntityManager(eventBus), Difficulty.Normal, 42);

        for (int i = 0; i < 120; i++)
        {
            lifecycle.Tick(1f / 60f, new List<HeroEntity> { hero });
        }

        Assert.Equal(HeroState.WornOut, hero.State);
        Assert.Equal(WearOutReason.CarryCapacityFull, hero.LastWearOutReason);
    }

    [Fact]
    public void Warrior_WearOutWhenStaminaDepleted()
    {
        var hero = new HeroEntity(EntityId.Next())
        {
            Profession = WorkerProfession.Warrior,
            Stamina = 3f,
            MaxStamina = 100f,
            AssignedBuildingId = 1
        };
        var eventBus = new EventBus();
        var lifecycle = new WorkerLifecycleSystem(eventBus, CreateDummyEntityManager(eventBus), Difficulty.Normal, 42);

        for (int i = 0; i < 120; i++)
        {
            lifecycle.Tick(1f / 60f, new List<HeroEntity> { hero });
        }

        Assert.Equal(HeroState.WornOut, hero.State);
        Assert.Equal(WearOutReason.StaminaDepleted, hero.LastWearOutReason);
    }

    [Fact]
    public void Worker_DoesNotWearOutWithoutAssignedBuilding()
    {
        var hero = new HeroEntity(EntityId.Next()) { Profession = WorkerProfession.Forester, ToolDurability = 1f };
        var eventBus = new EventBus();
        var lifecycle = new WorkerLifecycleSystem(eventBus, CreateDummyEntityManager(eventBus), Difficulty.Normal, 42);

        for (int i = 0; i < 120; i++)
        {
            lifecycle.Tick(1f / 60f, new List<HeroEntity> { hero });
        }

        Assert.Equal(HeroState.OnPath, hero.State);
    }

    [Fact]
    public void Worker_DoesNotWearOutWithNoProfession()
    {
        var hero = new HeroEntity(EntityId.Next()) { Profession = WorkerProfession.None, ToolDurability = 1f, AssignedBuildingId = 1 };
        var eventBus = new EventBus();
        var lifecycle = new WorkerLifecycleSystem(eventBus, CreateDummyEntityManager(eventBus), Difficulty.Normal, 42);

        for (int i = 0; i < 120; i++)
        {
            lifecycle.Tick(1f / 60f, new List<HeroEntity> { hero });
        }

        Assert.Equal(HeroState.OnPath, hero.State);
    }

    // ─── Ability System ───────────────────────────────────────────

    [Fact]
    public void HeroEntity_GainAbility_AddsToProfessionList()
    {
        var hero = new HeroEntity(EntityId.Next()) { Profession = WorkerProfession.Warrior };
        hero.GainAbility(new WorkerAbility { Id = "extra_attack", DisplayName = "Extra Attack" });

        Assert.Single(hero.Abilities);
        Assert.Equal(WorkerProfession.Warrior, hero.Abilities[0].GainedAsProfession);
    }

    [Fact]
    public void HeroEntity_SwapProfession_LosesOldAbilities()
    {
        var hero = new HeroEntity(EntityId.Next()) { Profession = WorkerProfession.Warrior };
        hero.GainAbility(new WorkerAbility { Id = "extra_attack", DisplayName = "Extra Attack" });
        hero.GainAbility(new WorkerAbility { Id = "shield_bash", DisplayName = "Shield Bash" });

        int lost = hero.SwapProfession(WorkerProfession.Researcher);

        Assert.Equal(2, lost);
        Assert.Empty(hero.Abilities);
        Assert.Equal(WorkerProfession.Researcher, hero.Profession);
    }

    [Fact]
    public void HeroEntity_SwapProfession_KeepsAbilitiesFromOtherProfessions()
    {
        var hero = new HeroEntity(EntityId.Next()) { Profession = WorkerProfession.Warrior };
        hero.GainAbility(new WorkerAbility { Id = "attack", DisplayName = "Attack" });

        hero.SwapProfession(WorkerProfession.Cleric);
        hero.GainAbility(new WorkerAbility { Id = "heal", DisplayName = "Heal" });

        int lost = hero.SwapProfession(WorkerProfession.Researcher);

        Assert.Equal(1, lost);
        Assert.Empty(hero.Abilities);
    }

    [Fact]
    public void HeroEntity_IsCombatProfession_ReturnsCorrectly()
    {
        var warrior = new HeroEntity(EntityId.Next()) { Profession = WorkerProfession.Warrior };
        var cleric = new HeroEntity(EntityId.Next()) { Profession = WorkerProfession.Cleric };
        var forester = new HeroEntity(EntityId.Next()) { Profession = WorkerProfession.Forester };
        var researcher = new HeroEntity(EntityId.Next()) { Profession = WorkerProfession.Researcher };

        Assert.True(warrior.IsCombatProfession);
        Assert.True(cleric.IsCombatProfession);
        Assert.False(forester.IsCombatProfession);
        Assert.False(researcher.IsCombatProfession);
    }

    // ─── Difficulty-Based Job Completion ─────────────────────────

    [Fact]
    public void Casual_ResolveJobCompletion_NeverDowngrades()
    {
        var eventBus = new EventBus();
        var lifecycle = new WorkerLifecycleSystem(eventBus, CreateDummyEntityManager(eventBus), Difficulty.Casual, 42);

        int downgrades = 0;
        for (int trial = 0; trial < 100; trial++)
        {
            var hero = new HeroEntity(EntityId.Next()) { Profession = WorkerProfession.Forester };
            hero.Level = 3;
            int levelBefore = hero.Level;

            lifecycle.ResolveJobCompletion(hero);

            if (hero.Level < levelBefore) downgrades++;
        }

        Assert.Equal(0, downgrades);
    }

    [Fact]
    public void Casual_ResolveJobCompletion_AlwaysGainsAbility()
    {
        var eventBus = new EventBus();
        var lifecycle = new WorkerLifecycleSystem(eventBus, CreateDummyEntityManager(eventBus), Difficulty.Casual, 42);
        var hero = new HeroEntity(EntityId.Next()) { Profession = WorkerProfession.Forester };
        hero.Level = 3;

        lifecycle.ResolveJobCompletion(hero);

        Assert.NotEmpty(hero.Abilities);
    }

    [Fact]
    public void Normal_ResolveJobCompletion_CanKillWorker()
    {
        var eventBus = new EventBus();
        int deaths = 0;

        for (int seed = 0; seed < 200; seed++)
        {
            var lc = new WorkerLifecycleSystem(eventBus, CreateDummyEntityManager(eventBus), Difficulty.Normal, seed);
            var hero = new HeroEntity(EntityId.Next()) { Profession = WorkerProfession.Forester };
            hero.Level = 3;
            lc.ResolveJobCompletion(hero);
            if (hero.State == HeroState.Ghost) deaths++;
        }

        Assert.True(deaths > 0, "Normal difficulty should occasionally kill workers");
    }

    [Fact]
    public void Normal_ResolveJobCompletion_OftenDowngradesLevel()
    {
        var eventBus = new EventBus();
        int downgrades = 0;

        for (int seed = 0; seed < 200; seed++)
        {
            var lc = new WorkerLifecycleSystem(eventBus, CreateDummyEntityManager(eventBus), Difficulty.Normal, seed);
            var hero = new HeroEntity(EntityId.Next()) { Profession = WorkerProfession.Forester };
            hero.Level = 5;
            int before = hero.Level;
            lc.ResolveJobCompletion(hero);
            if (hero.Level < before && hero.State != HeroState.Ghost) downgrades++;
        }

        Assert.True(downgrades > 0, "Normal difficulty should downgrade workers");
    }

    // ─── Dungeon Ability Gain ─────────────────────────────────────

    [Fact]
    public void DungeonAbilityGain_OnlyCombatProfessions()
    {
        var eventBus = new EventBus();
        var lifecycle = new WorkerLifecycleSystem(eventBus, CreateDummyEntityManager(eventBus), Difficulty.Casual, 42);

        var forester = new HeroEntity(EntityId.Next()) { Profession = WorkerProfession.Forester };
        lifecycle.ResolveDungeonAbilityGain(forester);
        Assert.Empty(forester.Abilities);

        var warrior = new HeroEntity(EntityId.Next()) { Profession = WorkerProfession.Warrior };
        lifecycle.ResolveDungeonAbilityGain(warrior);
        Assert.NotEmpty(warrior.Abilities);
    }

    // ─── WearOutConditions Static ─────────────────────────────────

    [Fact]
    public void WearOutConditions_ToolWearRates_ArePositiveForToolProfessions()
    {
        Assert.True(WearOutConditions.GetToolWearRate(WorkerProfession.Forester) > 0f);
        Assert.True(WearOutConditions.GetToolWearRate(WorkerProfession.Miner) > 0f);
        Assert.Equal(0f, WearOutConditions.GetToolWearRate(WorkerProfession.Researcher));
    }

    [Fact]
    public void WearOutConditions_StaminaDrainRates_ArePositiveForCombat()
    {
        Assert.True(WearOutConditions.GetStaminaDrainRate(WorkerProfession.Warrior) > 0f);
        Assert.True(WearOutConditions.GetStaminaDrainRate(WorkerProfession.Cleric) > 0f);
        Assert.Equal(0f, WearOutConditions.GetStaminaDrainRate(WorkerProfession.Forester));
    }

    [Fact]
    public void WearOutConditions_Check_ReturnsNoneForHealthyWorker()
    {
        var hero = new HeroEntity(EntityId.Next())
        {
            Profession = WorkerProfession.Forester,
            ToolDurability = 50f
        };

        Assert.Equal(WearOutReason.None, WearOutConditions.Check(hero));
    }

    [Fact]
    public void WearOutConditions_Check_DetectsToolBroken()
    {
        var hero = new HeroEntity(EntityId.Next())
        {
            Profession = WorkerProfession.Forester,
            ToolDurability = 0f
        };

        Assert.Equal(WearOutReason.ToolBroken, WearOutConditions.Check(hero));
    }

    [Fact]
    public void WearOutConditions_Check_DetectsStaminaDepleted()
    {
        var hero = new HeroEntity(EntityId.Next())
        {
            Profession = WorkerProfession.Warrior,
            Stamina = 0f
        };

        Assert.Equal(WearOutReason.StaminaDepleted, WearOutConditions.Check(hero));
    }

    [Fact]
    public void WearOutConditions_Check_DetectsCarryCapacityFull()
    {
        var hero = new HeroEntity(EntityId.Next())
        {
            Profession = WorkerProfession.HerbGatherer,
            CarryLoad = 50f,
            MaxCarryCapacity = 50f,
            Stamina = 100f
        };

        Assert.Equal(WearOutReason.CarryCapacityFull, WearOutConditions.Check(hero));
    }

    // ─── Enums ────────────────────────────────────────────────────

    [Fact]
    public void WorkerProfession_HasAllRequiredValues()
    {
        Assert.True(Enum.IsDefined(typeof(WorkerProfession), WorkerProfession.Forester));
        Assert.True(Enum.IsDefined(typeof(WorkerProfession), WorkerProfession.Miner));
        Assert.True(Enum.IsDefined(typeof(WorkerProfession), WorkerProfession.HerbGatherer));
        Assert.True(Enum.IsDefined(typeof(WorkerProfession), WorkerProfession.Researcher));
        Assert.True(Enum.IsDefined(typeof(WorkerProfession), WorkerProfession.Warrior));
        Assert.True(Enum.IsDefined(typeof(WorkerProfession), WorkerProfession.Cleric));
    }

    [Fact]
    public void HeroState_ContainsWornOutAndReturning()
    {
        Assert.True(Enum.IsDefined(typeof(HeroState), HeroState.WornOut));
        Assert.True(Enum.IsDefined(typeof(HeroState), HeroState.ReturningToMaintenance));
    }
}
