using ForgeFlow.Core.Data;
using ForgeFlow.Core.Data.Definitions;
using ForgeFlow.Core.Entities;
using ForgeFlow.Core.Events;
using ForgeFlow.Core.Proto.Logic;
using ForgeFlow.Core.Proto.Prototypes;
using ForgeFlow.Core.Systems;
using ForgeFlow.Core.Utilities;

namespace ForgeFlow.Tests;

/// <summary>
/// Comprehensive tests for the Dungeon Portal system:
/// - DungeonPortalProto enriched fields
/// - DungeonPortalLogic acceptance, timer, resolution
/// - Central Manager + Events pattern (DungeonManager tick handling)
/// - PathGateManager entrance detection for dungeon portals
/// - Villager eligibility checks (CanEnterDungeon)
/// </summary>
public class DungeonPortalTests
{
    public DungeonPortalTests()
    {
        EntityIdFactory.ResetForTesting();
    }

    // ── Proto & Initialization ───────────────────────────────────

    [Fact]
    public void DungeonPortalProto_HasEnrichedDefaults()
    {
        var proto = new DungeonPortalProto();
        Assert.Equal("goblin_caves", proto.DefaultDungeonId);
        Assert.Equal(0.1f, proto.BaseSurvivalChance);
        Assert.Equal(50, proto.BaseGoldReward);
        Assert.Equal(15.0f, proto.RunDuration);
    }

    [Fact]
    public void DungeonPortalLogic_InitializeFromProto_CopiesFields()
    {
        var proto = new DungeonPortalProto
        {
            Id = "dungeon_portal_tier1",
            DefaultDungeonId = "crypts",
            BaseSurvivalChance = 0.2f,
            BaseGoldReward = 100,
            RunDuration = 20f,
            MaxOccupants = 6,
            Tier = 2,
            ProcessingDuration = 20f
        };

        var logic = new DungeonPortalLogic(EntityId.Next());
        logic.InitializeFromProto(proto);

        Assert.Equal("dungeon_portal_tier1", logic.ProtoId);
        Assert.Equal("crypts", logic.DungeonId);
        Assert.Equal(0.2f, logic.BaseSurvivalChance);
        Assert.Equal(100, logic.BaseGoldReward);
        Assert.Equal(20f, logic.RunDuration);
        Assert.Equal(6, logic.MaxOccupants);
        Assert.Equal(2, logic.Tier);
    }

    // ── Acceptance ────────────────────────────────────────────────

    [Fact]
    public void AcceptVillager_ReturnsTrue_WhenCapacityAvailable()
    {
        var dungeon = CreateDefaultPortal();
        Assert.True(dungeon.AcceptVillager(1));
        Assert.Single(dungeon.CurrentOccupants);
        Assert.True(dungeon.OccupantTimers.ContainsKey(1));
    }

    [Fact]
    public void AcceptVillager_ReturnsFalse_WhenFull()
    {
        var dungeon = CreateDefaultPortal(maxOccupants: 2);
        Assert.True(dungeon.AcceptVillager(1));
        Assert.True(dungeon.AcceptVillager(2));
        Assert.False(dungeon.AcceptVillager(3));
        Assert.Equal(2, dungeon.CurrentOccupants.Count);
    }

    [Fact]
    public void CanAcceptVillager_ReturnsFalse_WhenFull()
    {
        var dungeon = CreateDefaultPortal(maxOccupants: 1);
        dungeon.AcceptVillager(1);
        Assert.False(dungeon.CanAcceptVillager);
    }

    // ── Timer & Resolution ───────────────────────────────────────

    [Fact]
    public void TickOccupants_AdvancesTimers()
    {
        var dungeon = CreateDefaultPortal(runDuration: 10f);
        dungeon.AcceptVillager(1);

        dungeon.TickOccupants(3f);

        Assert.Single(dungeon.CurrentOccupants);
        Assert.Empty(dungeon.PendingResults);
        Assert.True(dungeon.OccupantTimers[1] >= 3f);
    }

    [Fact]
    public void TickOccupants_ProducesResult_WhenTimerCompletes()
    {
        var dungeon = CreateDefaultPortal(runDuration: 5f);
        dungeon.AcceptVillager(1);

        dungeon.TickOccupants(5f);

        Assert.Empty(dungeon.CurrentOccupants);
        Assert.Single(dungeon.PendingResults);
        Assert.Equal((ulong)1, dungeon.PendingResults[0].VillagerId);
    }

    [Fact]
    public void TickOccupants_SurvivalChance1_AlwaysSurvives()
    {
        var dungeon = CreateDefaultPortal(runDuration: 1f, survivalChance: 1.0f);
        dungeon.SetSeed(42);

        dungeon.AcceptVillager(1, hasWeapon: true, hasArmor: true);
        dungeon.TickOccupants(1f);

        Assert.Single(dungeon.PendingResults);
        Assert.True(dungeon.PendingResults[0].Survived);
    }

    [Fact]
    public void TickOccupants_SurvivalChance0_AlwaysDies()
    {
        var dungeon = CreateDefaultPortal(runDuration: 1f, survivalChance: 0f);
        dungeon.SetSeed(42);

        dungeon.AcceptVillager(1);
        dungeon.TickOccupants(1f);

        Assert.Single(dungeon.PendingResults);
        Assert.False(dungeon.PendingResults[0].Survived);
    }

    [Fact]
    public void TickOccupants_GoldRewardOnSurvival()
    {
        var dungeon = CreateDefaultPortal(runDuration: 1f, survivalChance: 1.0f, goldReward: 75);
        dungeon.SetSeed(42);

        dungeon.AcceptVillager(1, hasWeapon: true, hasArmor: true);
        dungeon.TickOccupants(1f);

        Assert.Equal(75, dungeon.PendingResults[0].GoldReward);
    }

    [Fact]
    public void TickOccupants_NoGoldRewardOnDeath()
    {
        var dungeon = CreateDefaultPortal(runDuration: 1f, survivalChance: 0f, goldReward: 75);
        dungeon.SetSeed(42);

        dungeon.AcceptVillager(1);
        dungeon.TickOccupants(1f);

        Assert.Equal(0, dungeon.PendingResults[0].GoldReward);
    }

    // ── Run Progress ─────────────────────────────────────────────

    [Fact]
    public void GetRunProgress_ReturnsCorrectFraction()
    {
        var dungeon = CreateDefaultPortal(runDuration: 10f);
        dungeon.AcceptVillager(1);

        dungeon.TickOccupants(5f);

        Assert.Equal(0.5f, dungeon.GetRunProgress(1), 0.01f);
    }

    [Fact]
    public void GetRunProgress_ReturnsZero_ForUnknownVillager()
    {
        var dungeon = CreateDefaultPortal();
        Assert.Equal(0f, dungeon.GetRunProgress(999));
    }

    // ── Multiple Occupants ───────────────────────────────────────

    [Fact]
    public void TickOccupants_HandlesMultipleOccupants()
    {
        var dungeon = CreateDefaultPortal(runDuration: 5f, maxOccupants: 4);
        dungeon.AcceptVillager(1);
        dungeon.AcceptVillager(2);
        dungeon.AcceptVillager(3);

        dungeon.TickOccupants(5f);

        Assert.Empty(dungeon.CurrentOccupants);
        Assert.Equal(3, dungeon.PendingResults.Count);
    }

    // ── DungeonDefinition Override ───────────────────────────────

    [Fact]
    public void TickOccupants_UsesDungeonDefinition_WhenAvailable()
    {
        var registry = new DungeonRegistry();
        registry.Register(new DungeonDefinition
        {
            Id = "test_dungeon",
            Tier = 1,
            BaseSurvivalChance = 1.0f,
            BaseGoldReward = 200,
            RunDuration = 15f,
            PossibleDropItemIds = new List<string> { "sword_t1" }
        });

        var dungeon = CreateDefaultPortal(
            dungeonId: "test_dungeon", runDuration: 2f, survivalChance: 0f, goldReward: 10);
        dungeon.Initialize(registry);
        dungeon.SetSeed(42);

        dungeon.AcceptVillager(1, hasWeapon: true, hasArmor: true);
        dungeon.TickOccupants(2f);

        var result = dungeon.PendingResults[0];
        // Uses definition's 1.0 survival chance, not the proto's 0.0
        Assert.True(result.Survived);
        Assert.Equal(200, result.GoldReward);
    }

    // ── DungeonDefinition Enriched Fields ────────────────────────

    [Fact]
    public void DungeonDefinition_HasEnrichedDefaults()
    {
        var def = new DungeonDefinition();
        Assert.Equal(0.1f, def.BaseSurvivalChance);
        Assert.Equal(50, def.BaseGoldReward);
        Assert.Equal(15.0f, def.RunDuration);
    }

    // ── VillagerLogic.CanEnterDungeon ────────────────────────────

    [Fact]
    public void VillagerLogic_CanEnterDungeon_False_WhenUntrained()
    {
        var villager = new VillagerLogic(EntityId.Next())
        {
            TrainedClass = VillagerClass.Untrained,
            EquippedWeaponId = "sword",
            EquippedArmorId = "shield"
        };
        Assert.False(villager.CanEnterDungeon);
    }

    [Fact]
    public void VillagerLogic_CanEnterDungeon_True_WhenTrainedWarrior_NoWeapon()
    {
        var villager = new VillagerLogic(EntityId.Next())
        {
            TrainedClass = VillagerClass.Warrior,
            EquippedWeaponId = null,
            EquippedArmorId = "shield"
        };
        Assert.True(villager.CanEnterDungeon);
    }

    [Fact]
    public void VillagerLogic_CanEnterDungeon_True_WhenTrainedWarrior_NoArmor()
    {
        var villager = new VillagerLogic(EntityId.Next())
        {
            TrainedClass = VillagerClass.Warrior,
            EquippedWeaponId = "sword",
            EquippedArmorId = null
        };
        Assert.True(villager.CanEnterDungeon);
    }

    [Fact]
    public void VillagerLogic_CanEnterDungeon_True_WhenTrainedAndEquipped()
    {
        var villager = new VillagerLogic(EntityId.Next())
        {
            TrainedClass = VillagerClass.Warrior,
            EquippedWeaponId = "sword",
            EquippedArmorId = "shield"
        };
        Assert.True(villager.CanEnterDungeon);
    }

    [Fact]
    public void VillagerLogic_CanEnterDungeon_True_WhenTrainedWarrior_NoGear()
    {
        var villager = new VillagerLogic(EntityId.Next())
        {
            TrainedClass = VillagerClass.Warrior,
            EquippedWeaponId = null,
            EquippedArmorId = null
        };
        Assert.True(villager.CanEnterDungeon);
    }

    [Fact]
    public void VillagerLogic_CanEnterDungeon_False_WhenArtisan()
    {
        var villager = new VillagerLogic(EntityId.Next())
        {
            TrainedClass = VillagerClass.Artisan,
            EquippedWeaponId = "sword",
            EquippedArmorId = "shield"
        };
        Assert.False(villager.CanEnterDungeon);
    }

    [Fact]
    public void VillagerLogic_IsFightingClass_TrueForCombatClasses()
    {
        var warrior = new VillagerLogic(EntityId.Next()) { TrainedClass = VillagerClass.Warrior };
        var cleric = new VillagerLogic(EntityId.Next()) { TrainedClass = VillagerClass.Cleric };
        var mage = new VillagerLogic(EntityId.Next()) { TrainedClass = VillagerClass.Mage };
        var ranger = new VillagerLogic(EntityId.Next()) { TrainedClass = VillagerClass.Ranger };
        var paladin = new VillagerLogic(EntityId.Next()) { TrainedClass = VillagerClass.Paladin };

        Assert.True(warrior.IsFightingClass);
        Assert.True(cleric.IsFightingClass);
        Assert.True(mage.IsFightingClass);
        Assert.True(ranger.IsFightingClass);
        Assert.True(paladin.IsFightingClass);
    }

    [Fact]
    public void VillagerLogic_IsFightingClass_FalseForNonCombatClasses()
    {
        var untrained = new VillagerLogic(EntityId.Next()) { TrainedClass = VillagerClass.Untrained };
        var artisan = new VillagerLogic(EntityId.Next()) { TrainedClass = VillagerClass.Artisan };

        Assert.False(untrained.IsFightingClass);
        Assert.False(artisan.IsFightingClass);
    }

    // ── Helpers ──────────────────────────────────────────────────

    private static DungeonPortalLogic CreateDefaultPortal(
        string dungeonId = "goblin_caves",
        float runDuration = 15f,
        float survivalChance = 0.1f,
        int goldReward = 50,
        int maxOccupants = 4)
    {
        var proto = new DungeonPortalProto
        {
            Id = "dungeon_portal_test",
            DefaultDungeonId = dungeonId,
            BaseSurvivalChance = survivalChance,
            BaseGoldReward = goldReward,
            RunDuration = runDuration,
            MaxOccupants = maxOccupants,
            ProcessingDuration = runDuration
        };

        var logic = new DungeonPortalLogic(EntityId.Next());
        logic.InitializeFromProto(proto);
        return logic;
    }
}
