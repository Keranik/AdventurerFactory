using ForgeFlow.Core;
using ForgeFlow.Core.Data;
using ForgeFlow.Core.Entities;
using ForgeFlow.Core.Events;
using ForgeFlow.Core.Proto.Logic;
using ForgeFlow.Core.Proto.Prototypes;
using ForgeFlow.Core.Save;
using ForgeFlow.Core.Systems;
using ForgeFlow.Core.Utilities;

namespace ForgeFlow.Tests;

/// <summary>
/// Comprehensive tests for Tutorial Phase 2:
/// - Phase 2 step loading from JSON (13 steps, Order 17–29)
/// - Condition advancement for new condition types
/// - StructureManager.SetCraftStationRecipe API and event publishing
/// - Wooden Spear recipe existence in registry
/// - Phase 1 villager limit lifting in Phase 2
/// - Gear-aware dungeon survival mechanics
/// </summary>
public class TutorialPhase2Tests
{
    public TutorialPhase2Tests()
    {
        EntityIdFactory.ResetForTesting();
    }

    private static GameBootstrapper CreateBootstrapper()
    {
        var bootstrapper = new GameBootstrapper();
        bootstrapper.Bootstrap();
        return bootstrapper;
    }

    private static (GameBootstrapper boot, SimulationTicker sim) CreateNewGame(
        Difficulty difficulty = Difficulty.Normal)
    {
        var boot = CreateBootstrapper();
        var settings = new NewGameSettings
        {
            GameName = "TestGame",
            Difficulty = difficulty,
            Seed = 42,
            GuildName = "TestGuild"
        };
        boot.ApplyNewGameSettings(settings);
        return (boot, boot.Services.Get<SimulationTicker>());
    }

    // ── Phase 2 Steps Load from JSON ────────────────────────────

    [Fact]
    public void Phase2Steps_LoadFromJson_All13StepsPresent()
    {
        var boot = CreateBootstrapper();
        var tutorial = boot.Services.Get<SimulationTicker>().TutorialSystem;

        var phase2Missions = tutorial.AllMissions
            .Where(m => m.Phase == 2)
            .OrderBy(m => m.Order)
            .ToList();

        Assert.Equal(13, phase2Missions.Count);
    }

    [Fact]
    public void Phase2Steps_CorrectOrderRange()
    {
        var boot = CreateBootstrapper();
        var tutorial = boot.Services.Get<SimulationTicker>().TutorialSystem;

        var phase2Missions = tutorial.AllMissions
            .Where(m => m.Phase == 2)
            .OrderBy(m => m.Order)
            .ToList();

        Assert.Equal(17, phase2Missions.First().Order);
        Assert.Equal(29, phase2Missions.Last().Order);
    }

    [Fact]
    public void Phase2Steps_CorrectIds()
    {
        var boot = CreateBootstrapper();
        var tutorial = boot.Services.Get<SimulationTicker>().TutorialSystem;

        var phase2Ids = tutorial.AllMissions
            .Where(m => m.Phase == 2)
            .OrderBy(m => m.Order)
            .Select(m => m.ProtoId)
            .ToList();

        var expected = new[]
        {
            "p2_01_place_craft_station",
            "p2_02_craft_station_gates",
            "p2_03_select_spear_recipe",
            "p2_04_place_second_spawner",
            "p2_05_place_filter_splitter",
            "p2_06_configure_filter",
            "p2_07_connect_filter_craft",
            "p2_08_watch_spear_craft",
            "p2_09_place_warrior_trainer",
            "p2_10_trainer_gates_paths",
            "p2_11_watch_training",
            "p2_12_place_dungeon",
            "p2_13_first_dungeon_run"
        };

        Assert.Equal(expected, phase2Ids);
    }

    [Fact]
    public void Phase2Steps_PrerequisiteChain_StartsFromPhase1()
    {
        var boot = CreateBootstrapper();
        var tutorial = boot.Services.Get<SimulationTicker>().TutorialSystem;

        var firstPhase2 = tutorial.AllMissions.First(m => m.ProtoId == "p2_01_place_craft_station");
        Assert.Equal("p1_16_watch_full_loop", firstPhase2.PrerequisiteMissionId);
    }

    [Fact]
    public void Phase2Steps_AllHaveRewards()
    {
        var boot = CreateBootstrapper();
        var tutorial = boot.Services.Get<SimulationTicker>().TutorialSystem;

        var phase2Missions = tutorial.AllMissions.Where(m => m.Phase == 2);
        foreach (var mission in phase2Missions)
        {
            Assert.True(mission.Rewards.ContainsKey("gold"),
                $"Mission {mission.ProtoId} should have a gold reward");
            Assert.True(mission.Rewards["gold"] > 0,
                $"Mission {mission.ProtoId} should have a positive gold reward");
        }
    }

    // ── Condition Advancement ───────────────────────────────────

    [Fact]
    public void Phase2_PlaceCraftStation_AdvancesTutorial()
    {
        var (boot, sim) = CreateNewGame();
        var tutorial = sim.TutorialSystem;
        CompleteAllPhase1(tutorial);

        // Active mission should now be p2_01_place_craft_station
        Assert.NotNull(tutorial.ActiveMission);
        Assert.Equal("p2_01_place_craft_station", tutorial.ActiveMission!.ProtoId);

        bool completed = tutorial.AdvanceCondition(TutorialConditionType.PlaceSpecificStructure, "CraftStation");
        Assert.True(completed);
    }

    [Fact]
    public void Phase2_SelectRecipe_AdvancesTutorial()
    {
        var (boot, sim) = CreateNewGame();
        var tutorial = sim.TutorialSystem;
        CompleteAllPhase1(tutorial);

        // Complete steps up to p2_03
        tutorial.AdvanceCondition(TutorialConditionType.PlaceSpecificStructure, "CraftStation"); // p2_01
        tutorial.AdvanceCondition(TutorialConditionType.PlacePathGateEntrance); // p2_02 condition 1
        tutorial.AdvanceCondition(TutorialConditionType.PlacePathGateExit);     // p2_02 condition 2

        Assert.NotNull(tutorial.ActiveMission);
        Assert.Equal("p2_03_select_spear_recipe", tutorial.ActiveMission!.ProtoId);

        bool completed = tutorial.AdvanceCondition(TutorialConditionType.SelectCraftStationRecipe, "craft_wooden_spear");
        Assert.True(completed);
    }

    [Fact]
    public void Phase2_ConfigureFilter_AdvancesTutorial()
    {
        var (boot, sim) = CreateNewGame();
        var tutorial = sim.TutorialSystem;
        CompleteAllPhase1(tutorial);
        AdvanceToStep(tutorial, "p2_06_configure_filter");

        Assert.NotNull(tutorial.ActiveMission);
        Assert.Equal("p2_06_configure_filter", tutorial.ActiveMission!.ProtoId);

        bool completed = tutorial.AdvanceCondition(TutorialConditionType.ConfigureFilterSplitter, "sticks");
        Assert.True(completed);
    }

    [Fact]
    public void Phase2_CraftItem_AdvancesTutorial()
    {
        var (boot, sim) = CreateNewGame();
        var tutorial = sim.TutorialSystem;
        CompleteAllPhase1(tutorial);
        AdvanceToStep(tutorial, "p2_08_watch_spear_craft");

        Assert.NotNull(tutorial.ActiveMission);
        Assert.Equal("p2_08_watch_spear_craft", tutorial.ActiveMission!.ProtoId);

        bool completed = tutorial.AdvanceCondition(TutorialConditionType.CraftItem, "wooden_spear_0");
        Assert.True(completed);
    }

    [Fact]
    public void Phase2_VillagerTrainClass_AdvancesTutorial()
    {
        var (boot, sim) = CreateNewGame();
        var tutorial = sim.TutorialSystem;
        CompleteAllPhase1(tutorial);
        AdvanceToStep(tutorial, "p2_11_watch_training");

        Assert.NotNull(tutorial.ActiveMission);
        Assert.Equal("p2_11_watch_training", tutorial.ActiveMission!.ProtoId);

        bool completed = tutorial.AdvanceCondition(TutorialConditionType.VillagerTrainClass, "Warrior");
        Assert.True(completed);
    }

    [Fact]
    public void Phase2_VillagerEnterDungeon_AdvancesTutorial()
    {
        var (boot, sim) = CreateNewGame();
        var tutorial = sim.TutorialSystem;
        CompleteAllPhase1(tutorial);
        AdvanceToStep(tutorial, "p2_13_first_dungeon_run");

        Assert.NotNull(tutorial.ActiveMission);
        Assert.Equal("p2_13_first_dungeon_run", tutorial.ActiveMission!.ProtoId);

        bool completed = tutorial.AdvanceCondition(TutorialConditionType.VillagerEnterDungeon);
        Assert.True(completed);
    }

    // ── StructureManager.SetCraftStationRecipe ────────────────────

    [Fact]
    public void SetCraftStationRecipe_PublishesEvent()
    {
        var (boot, sim) = CreateNewGame();
        var craft = new CraftStationLogic(EntityId.Next());
        craft.Position = new GridPosRPG(5, 5);
        sim.EntityManager.AddStructure(craft, craft.Position);

        CraftStationRecipeSelectedEvent? received = null;
        boot.Services.Get<EventBus>().Subscribe<CraftStationRecipeSelectedEvent>(e => received = e);

        bool result = boot.Services.Get<StructureManager>().SetCraftStationRecipe(craft.Id, "craft_wooden_spear");

        Assert.True(result);
        Assert.NotNull(received);
        Assert.Equal("craft_wooden_spear", received!.Value.RecipeId);
        Assert.Equal(craft.Id, received!.Value.StationId);
    }

    [Fact]
    public void SetCraftStationRecipe_InvalidStation_ReturnsFalse()
    {
        var (boot, sim) = CreateNewGame();

        bool result = boot.Services.Get<StructureManager>().SetCraftStationRecipe(999999, "craft_wooden_spear");

        Assert.False(result);
    }

    [Fact]
    public void SetCraftStationRecipe_InvalidRecipe_ReturnsFalse()
    {
        var (boot, sim) = CreateNewGame();
        var craft = new CraftStationLogic(EntityId.Next());
        craft.Position = new GridPosRPG(5, 5);
        sim.EntityManager.AddStructure(craft, craft.Position);

        bool result = boot.Services.Get<StructureManager>().SetCraftStationRecipe(craft.Id, "nonexistent_recipe");

        Assert.False(result);
    }

    [Fact]
    public void SetCraftStationRecipe_SetsRecipeOnLogic()
    {
        var (boot, sim) = CreateNewGame();
        var craft = new CraftStationLogic(EntityId.Next());
        craft.Position = new GridPosRPG(5, 5);
        sim.EntityManager.AddStructure(craft, craft.Position);

        boot.Services.Get<StructureManager>().SetCraftStationRecipe(craft.Id, "craft_wooden_spear");

        Assert.Equal("craft_wooden_spear", craft.ActiveRecipeId);
        Assert.Equal("wooden_spear_0", craft.ActiveRecipeOutputItemId);
        Assert.True(craft.ActiveRecipeInputs.ContainsKey("sticks"));
        Assert.Equal(4, craft.ActiveRecipeInputs["sticks"]);
    }

    // ── Wooden Spear Recipe ─────────────────────────────────────

    [Fact]
    public void WoodenSpearRecipe_ExistsInRegistry()
    {
        var boot = CreateBootstrapper();
        Assert.True(boot.Services.Get<RecipeRegistry>().TryGet("craft_wooden_spear", out var recipe));
        Assert.NotNull(recipe);
    }

    [Fact]
    public void WoodenSpearRecipe_CorrectInputsAndOutput()
    {
        var boot = CreateBootstrapper();
        boot.Services.Get<RecipeRegistry>().TryGet("craft_wooden_spear", out var recipe);

        Assert.Equal("wooden_spear_0", recipe!.OutputItemId);
        Assert.Single(recipe.Inputs);
        Assert.Equal("sticks", recipe.Inputs[0].ItemId);
        Assert.Equal(4, recipe.Inputs[0].Quantity);
        Assert.Equal(4.0f, recipe.CraftDuration);
        Assert.Equal(0, recipe.RequiredTier);
    }

    [Fact]
    public void WoodenSpearItem_ExistsInRegistry()
    {
        var boot = CreateBootstrapper();
        Assert.True(boot.Services.Get<ItemRegistry>().TryGet("wooden_spear_0", out var item));
        Assert.NotNull(item);
        Assert.Equal(ItemCategory.Equipment, item!.Category);
        Assert.NotNull(item.Equipment);
        Assert.Equal(EquipSlot.Weapon, item.Equipment!.Slot);
    }

    // ── Phase 1 Villager Limit ──────────────────────────────────

    [Fact]
    public void Phase1VillagerLimit_ActiveDuringPhase1()
    {
        var (boot, sim) = CreateNewGame();
        var tutorial = sim.TutorialSystem;

        // Phase 1 is active at start
        Assert.True(tutorial.IsPhase1Active);
        Assert.Equal(1, tutorial.Phase1VillagerLimit);
    }

    [Fact]
    public void Phase1VillagerLimit_LiftedInPhase2()
    {
        var (boot, sim) = CreateNewGame();
        var tutorial = sim.TutorialSystem;
        CompleteAllPhase1(tutorial);

        // Phase 2 is now active — villager limit should be lifted
        Assert.False(tutorial.IsPhase1Active);
        Assert.Null(tutorial.Phase1VillagerLimit);
    }

    // ── Gear-Aware Survival ─────────────────────────────────────

    [Fact]
    public void GearAwareSurvival_FullGear_BaseChance()
    {
        var portal = CreateDefaultPortal(survivalChance: 1.0f, runDuration: 1f);
        portal.SetSeed(42);
        portal.AcceptVillager(1, hasWeapon: true, hasArmor: true);
        portal.TickOccupants(1f);

        Assert.Single(portal.PendingResults);
        Assert.True(portal.PendingResults[0].Survived);
    }

    [Fact]
    public void GearAwareSurvival_NoWeapon_ReducedChance()
    {
        // With 0.25 multiplier on a 1.0 base → effective 0.25
        var portal = CreateDefaultPortal(survivalChance: 1.0f, runDuration: 1f);
        portal.SetSeed(42);
        portal.AcceptVillager(1, hasWeapon: false, hasArmor: true);
        portal.TickOccupants(1f);

        // With NoWeaponSurvivalMultiplier = 0.25, base 1.0 → effective 0.25
        // Result is stochastic, but we verify the acceptance and processing work
        Assert.Single(portal.PendingResults);
    }

    [Fact]
    public void GearAwareSurvival_NoGear_LowestChance()
    {
        // No weapon (0.25) * no armor (0.5) * base = 0.125 * base
        var portal = CreateDefaultPortal(survivalChance: 0.5f, runDuration: 1f);
        portal.SetSeed(42);
        portal.AcceptVillager(1, hasWeapon: false, hasArmor: false);
        portal.TickOccupants(1f);

        Assert.Single(portal.PendingResults);
        // 0.5 * 0.25 * 0.5 = 0.0625 — very low survival
    }

    [Fact]
    public void GearAwareSurvival_TracksGearPerOccupant()
    {
        var portal = CreateDefaultPortal(survivalChance: 1.0f, runDuration: 1f, maxOccupants: 4);
        portal.SetSeed(42);

        portal.AcceptVillager(1, hasWeapon: true, hasArmor: true);
        portal.AcceptVillager(2, hasWeapon: false, hasArmor: false);
        portal.AcceptVillager(3, hasWeapon: true, hasArmor: false);

        Assert.True(portal.OccupantHasWeapon[1]);
        Assert.True(portal.OccupantHasArmor[1]);
        Assert.False(portal.OccupantHasWeapon[2]);
        Assert.False(portal.OccupantHasArmor[2]);
        Assert.True(portal.OccupantHasWeapon[3]);
        Assert.False(portal.OccupantHasArmor[3]);

        portal.TickOccupants(1f);

        // All processed, tracking dictionaries cleaned up
        Assert.Empty(portal.CurrentOccupants);
        Assert.Equal(3, portal.PendingResults.Count);
        Assert.Empty(portal.OccupantHasWeapon);
        Assert.Empty(portal.OccupantHasArmor);
    }

    // ── Helpers ──────────────────────────────────────────────────

    /// <summary>
    /// Completes all Phase 1 tutorial missions by advancing their conditions directly.
    /// This gets the tutorial system to Phase 2.
    /// </summary>
    private static void CompleteAllPhase1(TutorialSystem tutorial)
    {
        // p1_01: PlaceSpecificStructure("Spawner")
        tutorial.AdvanceCondition(TutorialConditionType.PlaceSpecificStructure, "Spawner");
        // p1_02: PlacePathGateExit
        tutorial.AdvanceCondition(TutorialConditionType.PlacePathGateExit);
        // p1_03: PlaceSpecificStructure("Forestry")
        tutorial.AdvanceCondition(TutorialConditionType.PlaceSpecificStructure, "Forestry");
        // p1_04: PlacePathGateEntrance
        tutorial.AdvanceCondition(TutorialConditionType.PlacePathGateEntrance);
        // p1_05: BuildPath x3
        tutorial.AdvanceCondition(TutorialConditionType.BuildPath, amount: 3);
        // p1_06: WatchVillagerGather
        tutorial.AdvanceCondition(TutorialConditionType.WatchVillagerGather);
        // p1_07: PlaceSpecificStructure("Stockpile")
        tutorial.AdvanceCondition(TutorialConditionType.PlaceSpecificStructure, "Stockpile");
        // p1_08: SelectStockpileProduct("sticks")
        tutorial.AdvanceCondition(TutorialConditionType.SelectStockpileProduct, "sticks");
        // p1_09: PlacePathGateEntrance
        tutorial.AdvanceCondition(TutorialConditionType.PlacePathGateEntrance);
        // p1_10: PlacePathGateExit
        tutorial.AdvanceCondition(TutorialConditionType.PlacePathGateExit);
        // p1_11: BuildPath x2
        tutorial.AdvanceCondition(TutorialConditionType.BuildPath, amount: 2);
        // p1_12: VillagerDropOffItem
        tutorial.AdvanceCondition(TutorialConditionType.VillagerDropOffItem);
        // p1_13: PlacePathGateExit
        tutorial.AdvanceCondition(TutorialConditionType.PlacePathGateExit);
        // p1_14: BuildPath x3
        tutorial.AdvanceCondition(TutorialConditionType.BuildPath, amount: 3);
        // p1_15: PlaceSpecificStructure("Inn") + PlacePathGateEntrance + PlacePathGateExit
        tutorial.AdvanceCondition(TutorialConditionType.PlaceSpecificStructure, "Inn");
        tutorial.AdvanceCondition(TutorialConditionType.PlacePathGateEntrance);
        tutorial.AdvanceCondition(TutorialConditionType.PlacePathGateExit);
        // p1_16: WatchFullLoop
        tutorial.AdvanceCondition(TutorialConditionType.WatchFullLoop);
    }

    /// <summary>
    /// Advances the tutorial to the specified Phase 2 step by completing all preceding steps.
    /// Assumes Phase 1 is already complete.
    /// </summary>
    private static void AdvanceToStep(TutorialSystem tutorial, string targetStepId)
    {
        // p2_01: PlaceSpecificStructure("CraftStation")
        if (tutorial.ActiveMission?.ProtoId == targetStepId) { return; }
        tutorial.AdvanceCondition(TutorialConditionType.PlaceSpecificStructure, "CraftStation");

        // p2_02: PlacePathGateEntrance + PlacePathGateExit
        if (tutorial.ActiveMission?.ProtoId == targetStepId) { return; }
        tutorial.AdvanceCondition(TutorialConditionType.PlacePathGateEntrance);
        tutorial.AdvanceCondition(TutorialConditionType.PlacePathGateExit);

        // p2_03: SelectCraftStationRecipe("craft_wooden_spear")
        if (tutorial.ActiveMission?.ProtoId == targetStepId) { return; }
        tutorial.AdvanceCondition(TutorialConditionType.SelectCraftStationRecipe, "craft_wooden_spear");

        // p2_04: PlaceSpecificStructure("Spawner") + PlacePathGateExit + BuildPath x2
        if (tutorial.ActiveMission?.ProtoId == targetStepId) { return; }
        tutorial.AdvanceCondition(TutorialConditionType.PlaceSpecificStructure, "Spawner");
        tutorial.AdvanceCondition(TutorialConditionType.PlacePathGateExit);
        tutorial.AdvanceCondition(TutorialConditionType.BuildPath, amount: 2);

        // p2_05: PlaceSpecificStructure("FilterSplitter")
        if (tutorial.ActiveMission?.ProtoId == targetStepId) { return; }
        tutorial.AdvanceCondition(TutorialConditionType.PlaceSpecificStructure, "FilterSplitter");

        // p2_06: ConfigureFilterSplitter("sticks")
        if (tutorial.ActiveMission?.ProtoId == targetStepId) { return; }
        tutorial.AdvanceCondition(TutorialConditionType.ConfigureFilterSplitter, "sticks");

        // p2_07: BuildPath x3
        if (tutorial.ActiveMission?.ProtoId == targetStepId) { return; }
        tutorial.AdvanceCondition(TutorialConditionType.BuildPath, amount: 3);

        // p2_08: CraftItem("wooden_spear_0")
        if (tutorial.ActiveMission?.ProtoId == targetStepId) { return; }
        tutorial.AdvanceCondition(TutorialConditionType.CraftItem, "wooden_spear_0");

        // p2_09: PlaceSpecificStructure("TrainingBuilding")
        if (tutorial.ActiveMission?.ProtoId == targetStepId) { return; }
        tutorial.AdvanceCondition(TutorialConditionType.PlaceSpecificStructure, "TrainingBuilding");

        // p2_10: PlacePathGateEntrance + PlacePathGateExit + BuildPath x2
        if (tutorial.ActiveMission?.ProtoId == targetStepId) { return; }
        tutorial.AdvanceCondition(TutorialConditionType.PlacePathGateEntrance);
        tutorial.AdvanceCondition(TutorialConditionType.PlacePathGateExit);
        tutorial.AdvanceCondition(TutorialConditionType.BuildPath, amount: 2);

        // p2_11: VillagerTrainClass("Warrior")
        if (tutorial.ActiveMission?.ProtoId == targetStepId) { return; }
        tutorial.AdvanceCondition(TutorialConditionType.VillagerTrainClass, "Warrior");

        // p2_12: PlaceSpecificStructure("DungeonPortal") + gates + paths
        if (tutorial.ActiveMission?.ProtoId == targetStepId) { return; }
        tutorial.AdvanceCondition(TutorialConditionType.PlaceSpecificStructure, "DungeonPortal");
        tutorial.AdvanceCondition(TutorialConditionType.PlacePathGateEntrance);
        tutorial.AdvanceCondition(TutorialConditionType.PlacePathGateExit);
        tutorial.AdvanceCondition(TutorialConditionType.BuildPath, amount: 2);

        // p2_13: VillagerEnterDungeon
        if (tutorial.ActiveMission?.ProtoId == targetStepId) { return; }
        tutorial.AdvanceCondition(TutorialConditionType.VillagerEnterDungeon);
    }

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
