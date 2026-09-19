using ForgeFlow.Core;
using ForgeFlow.Core.Data;
using ForgeFlow.Core.Entities;
using ForgeFlow.Core.Events;
using ForgeFlow.Core.Proto.Logic;
using ForgeFlow.Core.Proto.Prototypes;
using ForgeFlow.Core.Systems;
using ForgeFlow.Core.Save;
using ForgeFlow.Core.Utilities;

namespace ForgeFlow.Tests;

/// <summary>
/// Comprehensive tests for villager-driven crafting at the CraftStation.
/// Covers:
/// - CraftStationLogic unit tests (deposit, ingredient check, timer, clear)
/// - StructureManager integration tests (ProcessCraftStationDeposits, CompleteCraftStationCraft)
/// - Full end-to-end flow (villager enters → deposits → crafts → exits with item)
/// - Edge cases (no recipe, insufficient ingredients, second villager while crafting)
/// </summary>
public class CraftStationTests
{
    public CraftStationTests()
    {
        EntityIdFactory.ResetForTesting();
    }

    // ── Helpers ──────────────────────────────────────────────────────

    private static CraftStationLogic CreateCraftStation()
    {
        var craft = new CraftStationLogic(EntityId.Next());
        craft.IsActive = true;
        craft.Position = new GridPosRPG(5, 5);
        return craft;
    }

    private static CraftStationLogic CreateCraftStationWithSpearRecipe()
    {
        var craft = CreateCraftStation();
        craft.SetActiveRecipe(
            "craft_wooden_spear",
            new Dictionary<string, int> { { "sticks", 4 } },
            4.0f,
            "wooden_spear_0");
        return craft;
    }

    private static VillagerLogic CreateVillager(string name = "TestVillager", int stamina = 100)
    {
        var villager = new VillagerLogic(EntityId.Next())
        {
            Name = name,
            Stamina = stamina,
            MaxStamina = 100
        };
        return villager;
    }

    private static ItemInstance CreateSticks(int quantity = 1)
    {
        return new ItemInstance
        {
            InstanceId = EntityIdFactory.Next(),
            ProtoId = "sticks",
            Quantity = quantity
        };
    }

    private static (GameBootstrapper boot, SimulationTicker sim) CreateNewGame(
        Difficulty difficulty = Difficulty.Normal)
    {
        var boot = new GameBootstrapper();
        boot.Bootstrap();
        var settings = new NewGameSettings
        {
            GameName = "CraftTest",
            Difficulty = difficulty,
            Seed = 42,
            GuildName = "TestGuild"
        };
        boot.ApplyNewGameSettings(settings);
        return (boot, boot.Services.Get<SimulationTicker>());
    }

    // ══════════════════════════════════════════════════════════════════
    // ── CraftStationLogic Unit Tests ─────────────────────────────────
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public void NewCraftStation_HasNoRecipe()
    {
        var craft = CreateCraftStation();
        Assert.Null(craft.ActiveRecipeId);
        Assert.Empty(craft.ActiveRecipeInputs);
        Assert.Null(craft.ActiveRecipeOutputItemId);
        Assert.False(craft.IsCrafting);
        Assert.Empty(craft.StoredInputs);
    }

    [Fact]
    public void SetActiveRecipe_SetsAllFields()
    {
        var craft = CreateCraftStationWithSpearRecipe();

        Assert.Equal("craft_wooden_spear", craft.ActiveRecipeId);
        Assert.Equal("wooden_spear_0", craft.ActiveRecipeOutputItemId);
        Assert.Equal(4.0f, craft.ActiveRecipeDuration);
        Assert.True(craft.ActiveRecipeInputs.ContainsKey("sticks"));
        Assert.Equal(4, craft.ActiveRecipeInputs["sticks"]);
    }

    [Fact]
    public void ClearActiveRecipe_ResetsAllFields()
    {
        var craft = CreateCraftStationWithSpearRecipe();
        craft.CraftingVillagerId = 99;

        craft.ClearActiveRecipe();

        Assert.Null(craft.ActiveRecipeId);
        Assert.Empty(craft.ActiveRecipeInputs);
        Assert.Null(craft.ActiveRecipeOutputItemId);
        Assert.Null(craft.CraftingVillagerId);
        Assert.False(craft.IsCrafting);
        Assert.Equal(0f, craft.CraftTimer);
    }

    [Fact]
    public void DepositItem_AddsToStoredInputs()
    {
        var craft = CreateCraftStation();

        craft.DepositItem("sticks", 2);

        Assert.Equal(2, craft.StoredInputs["sticks"]);
    }

    [Fact]
    public void DepositItem_AccumulatesMultipleDeposits()
    {
        var craft = CreateCraftStation();

        craft.DepositItem("sticks", 2);
        craft.DepositItem("sticks", 3);

        Assert.Equal(5, craft.StoredInputs["sticks"]);
    }

    [Fact]
    public void DepositItem_DifferentResources_TrackedSeparately()
    {
        var craft = CreateCraftStation();

        craft.DepositItem("sticks", 2);
        craft.DepositItem("ore", 1);

        Assert.Equal(2, craft.StoredInputs["sticks"]);
        Assert.Equal(1, craft.StoredInputs["ore"]);
    }

    [Fact]
    public void HasRequiredIngredients_ReturnsFalse_WhenNoRecipe()
    {
        var craft = CreateCraftStation();
        Assert.False(craft.HasRequiredIngredients());
    }

    [Fact]
    public void HasRequiredIngredients_ReturnsFalse_WhenInsufficientInputs()
    {
        var craft = CreateCraftStationWithSpearRecipe();
        craft.DepositItem("sticks", 2);

        Assert.False(craft.HasRequiredIngredients());
    }

    [Fact]
    public void HasRequiredIngredients_ReturnsTrue_WhenExactInputs()
    {
        var craft = CreateCraftStationWithSpearRecipe();
        craft.DepositItem("sticks", 4);

        Assert.True(craft.HasRequiredIngredients());
    }

    [Fact]
    public void HasRequiredIngredients_ReturnsTrue_WhenExcessInputs()
    {
        var craft = CreateCraftStationWithSpearRecipe();
        craft.DepositItem("sticks", 10);

        Assert.True(craft.HasRequiredIngredients());
    }

    [Fact]
    public void TryStartCrafting_ReturnsFalse_WhenInsufficientInputs()
    {
        var craft = CreateCraftStationWithSpearRecipe();
        craft.DepositItem("sticks", 2);

        Assert.False(craft.TryStartCrafting());
        Assert.Equal(2, craft.StoredInputs["sticks"]); // not consumed
    }

    [Fact]
    public void TryStartCrafting_ReturnsTrue_ConsumesInputs()
    {
        var craft = CreateCraftStationWithSpearRecipe();
        craft.DepositItem("sticks", 4);

        Assert.True(craft.TryStartCrafting());
        Assert.Equal(0, craft.StoredInputs["sticks"]);
        Assert.Equal(0f, craft.CraftTimer);
    }

    [Fact]
    public void TryStartCrafting_ConsumesOnlyRequired_LeavesExcess()
    {
        var craft = CreateCraftStationWithSpearRecipe();
        craft.DepositItem("sticks", 7);

        Assert.True(craft.TryStartCrafting());
        Assert.Equal(3, craft.StoredInputs["sticks"]); // 7 - 4 = 3
    }

    [Fact]
    public void TryStartCrafting_ReturnsFalse_WhenNoRecipeSet()
    {
        var craft = CreateCraftStation();
        craft.DepositItem("sticks", 10);

        Assert.False(craft.TryStartCrafting());
    }

    // ── Tick Behavior ────────────────────────────────────────────────

    [Fact]
    public void Tick_DoesNotProgress_WhenNoCraftingVillager()
    {
        var craft = CreateCraftStationWithSpearRecipe();

        craft.Tick(2.0f);

        Assert.Equal(0f, craft.CraftTimer);
        Assert.False(craft.PendingCraftComplete);
    }

    [Fact]
    public void Tick_DoesNotProgress_WhenInactive()
    {
        var craft = CreateCraftStationWithSpearRecipe();
        craft.CraftingVillagerId = 99;
        craft.IsActive = false;

        craft.Tick(5.0f);

        Assert.Equal(0f, craft.CraftTimer);
        Assert.False(craft.PendingCraftComplete);
    }

    [Fact]
    public void Tick_DoesNotProgress_WhenNoRecipe()
    {
        var craft = CreateCraftStation();
        craft.CraftingVillagerId = 99;

        craft.Tick(5.0f);

        Assert.Equal(0f, craft.CraftTimer);
        Assert.False(craft.PendingCraftComplete);
    }

    [Fact]
    public void Tick_ProgressesTimer_WhenCraftingVillagerPresent()
    {
        var craft = CreateCraftStationWithSpearRecipe();
        craft.CraftingVillagerId = 99;

        craft.Tick(2.0f);

        Assert.Equal(2.0f, craft.CraftTimer, 0.01f);
        Assert.False(craft.PendingCraftComplete);
    }

    [Fact]
    public void Tick_SetsPendingCraftComplete_WhenTimerReachesDuration()
    {
        var craft = CreateCraftStationWithSpearRecipe();
        craft.CraftingVillagerId = 99;

        craft.Tick(4.0f); // duration = 4.0

        Assert.True(craft.PendingCraftComplete);
        Assert.Equal(0f, craft.CraftTimer); // reset after completion
    }

    [Fact]
    public void Tick_SetsPendingCraftComplete_WhenTimerExceedsDuration()
    {
        var craft = CreateCraftStationWithSpearRecipe();
        craft.CraftingVillagerId = 99;

        craft.Tick(5.0f); // exceeds 4.0 duration

        Assert.True(craft.PendingCraftComplete);
    }

    [Fact]
    public void Tick_IncrementalProgress_CompletesOverMultipleTicks()
    {
        var craft = CreateCraftStationWithSpearRecipe();
        craft.CraftingVillagerId = 99;

        craft.Tick(1.5f);
        Assert.Equal(1.5f, craft.CraftTimer, 0.01f);
        Assert.False(craft.PendingCraftComplete);

        craft.Tick(1.5f);
        Assert.Equal(3.0f, craft.CraftTimer, 0.01f);
        Assert.False(craft.PendingCraftComplete);

        craft.Tick(1.5f); // 4.5 >= 4.0
        Assert.True(craft.PendingCraftComplete);
    }

    // ── IsCrafting ───────────────────────────────────────────────────

    [Fact]
    public void IsCrafting_ReturnsTrue_WhenCraftingVillagerSet()
    {
        var craft = CreateCraftStation();
        craft.CraftingVillagerId = 42;

        Assert.True(craft.IsCrafting);
    }

    [Fact]
    public void IsCrafting_ReturnsFalse_WhenNoCraftingVillager()
    {
        var craft = CreateCraftStation();
        Assert.False(craft.IsCrafting);
    }

    // ── PendingVillagerIds ───────────────────────────────────────────

    [Fact]
    public void PendingVillagerIds_StartsEmpty()
    {
        var craft = CreateCraftStation();
        Assert.Empty(craft.PendingVillagerIds);
    }

    // ══════════════════════════════════════════════════════════════════
    // ── StructureManager Integration Tests ───────────────────────────
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public void VillagerDepositsItems_WhenEnteringCraftStation()
    {
        var (boot, sim) = CreateNewGame();

        // Create craft station with spear recipe
        var craft = new CraftStationLogic(EntityId.Next());
        craft.IsActive = true;
        craft.Position = new GridPosRPG(5, 5);
        sim.EntityManager.AddStructure(craft, craft.Position);
        boot.Services.Get<StructureManager>().SetCraftStationRecipe(craft.Id, "craft_wooden_spear");

        // Create a villager carrying 2 sticks
        var villager = new VillagerLogic(EntityId.Next())
        {
            Name = "Carrier",
            Stamina = 100,
            MaxStamina = 100
        };
        villager.TryPickUpItem(CreateSticks(2));
        sim.VillagerSystem.AddVillager(villager);

        // Simulate villager entering the craft station
        craft.PendingVillagerIds.Add(villager.Id);

        // Tick triggers deposit processing
        boot.Services.Get<StructureManager>().Tick(0.1f);

        // Villager's items should be deposited into StoredInputs
        Assert.False(villager.IsCarryingItems);
        Assert.Equal(2, craft.StoredInputs["sticks"]);
    }

    [Fact]
    public void VillagerExitsImmediately_WhenIngredientsInsufficient()
    {
        var (boot, sim) = CreateNewGame();

        var craft = new CraftStationLogic(EntityId.Next());
        craft.IsActive = true;
        craft.Position = new GridPosRPG(5, 5);
        sim.EntityManager.AddStructure(craft, craft.Position);
        boot.Services.Get<StructureManager>().SetCraftStationRecipe(craft.Id, "craft_wooden_spear");

        var villager = new VillagerLogic(EntityId.Next())
        {
            Name = "Carrier",
            Stamina = 100,
            MaxStamina = 100
        };
        villager.TryPickUpItem(CreateSticks(2)); // Only 2, need 4
        sim.VillagerSystem.AddVillager(villager);

        craft.PendingVillagerIds.Add(villager.Id);

        boot.Services.Get<StructureManager>().Tick(0.1f);

        // Deposited but not enough — should not be crafting
        Assert.False(craft.IsCrafting);
        Assert.Equal(2, craft.StoredInputs["sticks"]);
        // Villager should be queued to exit (WaitingToExitIds) since no exit gate exists
        Assert.Contains(villager.Id, craft.WaitingToExitIds);
    }

    [Fact]
    public void VillagerStaysToCraft_WhenIngredientsAreMet()
    {
        var (boot, sim) = CreateNewGame();

        var craft = new CraftStationLogic(EntityId.Next());
        craft.IsActive = true;
        craft.Position = new GridPosRPG(5, 5);
        sim.EntityManager.AddStructure(craft, craft.Position);
        boot.Services.Get<StructureManager>().SetCraftStationRecipe(craft.Id, "craft_wooden_spear");

        var villager = new VillagerLogic(EntityId.Next())
        {
            Name = "Crafter",
            Stamina = 100,
            MaxStamina = 100
        };
        villager.TryPickUpItem(CreateSticks(4)); // Exact recipe requirement
        sim.VillagerSystem.AddVillager(villager);

        craft.PendingVillagerIds.Add(villager.Id);

        boot.Services.Get<StructureManager>().Tick(0.1f);

        // Villager should be the active crafter
        Assert.True(craft.IsCrafting);
        Assert.Equal(villager.Id.Value, craft.CraftingVillagerId);
        Assert.Equal(VillagerState.Working, villager.State);
        Assert.Contains("wooden_spear_0", villager.CurrentActivity!);
    }

    [Fact]
    public void VillagerStaysToCraft_WhenPreviousDepositsAndNewDepositMeetRecipe()
    {
        var (boot, sim) = CreateNewGame();

        var craft = new CraftStationLogic(EntityId.Next());
        craft.IsActive = true;
        craft.Position = new GridPosRPG(5, 5);
        sim.EntityManager.AddStructure(craft, craft.Position);
        boot.Services.Get<StructureManager>().SetCraftStationRecipe(craft.Id, "craft_wooden_spear");

        // Pre-deposit 2 sticks (from previous villager)
        craft.DepositItem("sticks", 2);

        // New villager carries 2 more
        var villager = new VillagerLogic(EntityId.Next())
        {
            Name = "Crafter",
            Stamina = 100,
            MaxStamina = 100
        };
        villager.TryPickUpItem(CreateSticks(2));
        sim.VillagerSystem.AddVillager(villager);

        craft.PendingVillagerIds.Add(villager.Id);

        boot.Services.Get<StructureManager>().Tick(0.1f);

        // 2 (pre-deposited) + 2 (new deposit) = 4 → recipe met
        Assert.True(craft.IsCrafting);
        Assert.Equal(villager.Id.Value, craft.CraftingVillagerId);
    }

    [Fact]
    public void SecondVillager_DepositsAndExits_WhileFirstIsCrafting()
    {
        var (boot, sim) = CreateNewGame();

        var craft = new CraftStationLogic(EntityId.Next());
        craft.IsActive = true;
        craft.Position = new GridPosRPG(5, 5);
        sim.EntityManager.AddStructure(craft, craft.Position);
        boot.Services.Get<StructureManager>().SetCraftStationRecipe(craft.Id, "craft_wooden_spear");

        // First villager triggers crafting
        var villager1 = new VillagerLogic(EntityId.Next())
        {
            Name = "Crafter1",
            Stamina = 100,
            MaxStamina = 100
        };
        villager1.TryPickUpItem(CreateSticks(4));
        sim.VillagerSystem.AddVillager(villager1);

        craft.PendingVillagerIds.Add(villager1.Id);
        boot.Services.Get<StructureManager>().Tick(0.1f);

        Assert.True(craft.IsCrafting);
        Assert.Equal(villager1.Id.Value, craft.CraftingVillagerId);

        // Second villager enters while first is still crafting
        var villager2 = new VillagerLogic(EntityId.Next())
        {
            Name = "Carrier2",
            Stamina = 100,
            MaxStamina = 100
        };
        villager2.TryPickUpItem(CreateSticks(3));
        sim.VillagerSystem.AddVillager(villager2);

        craft.PendingVillagerIds.Add(villager2.Id);
        boot.Services.Get<StructureManager>().Tick(0.1f);

        // Villager 2 should have deposited their sticks but not be crafting
        Assert.Equal(villager1.Id.Value, craft.CraftingVillagerId); // first still crafting
        Assert.False(villager2.IsCarryingItems); // deposited
        Assert.Equal(3, craft.StoredInputs["sticks"]); // 3 sticks from second villager
        // Second villager should be waiting to exit
        Assert.Contains(villager2.Id, craft.WaitingToExitIds);
    }

    [Fact]
    public void CraftCompletion_PublishesItemCraftedEvent()
    {
        var (boot, sim) = CreateNewGame();

        var craft = new CraftStationLogic(EntityId.Next());
        craft.IsActive = true;
        craft.Position = new GridPosRPG(5, 5);
        sim.EntityManager.AddStructure(craft, craft.Position);
        boot.Services.Get<StructureManager>().SetCraftStationRecipe(craft.Id, "craft_wooden_spear");

        var villager = new VillagerLogic(EntityId.Next())
        {
            Name = "Crafter",
            Stamina = 100,
            MaxStamina = 100
        };
        villager.TryPickUpItem(CreateSticks(4));
        sim.VillagerSystem.AddVillager(villager);

        craft.PendingVillagerIds.Add(villager.Id);
        boot.Services.Get<StructureManager>().Tick(0.1f); // deposit + start crafting

        Assert.True(craft.IsCrafting);

        ItemCraftedEvent? craftedEvent = null;
        boot.Services.Get<EventBus>().Subscribe<ItemCraftedEvent>(e => craftedEvent = e);

        // Simulate crafting completion
        craft.Tick(4.0f); // duration = 4.0
        Assert.True(craft.PendingCraftComplete);

        boot.Services.Get<StructureManager>().Tick(0.1f); // processes PendingCraftComplete

        Assert.NotNull(craftedEvent);
        Assert.Equal("wooden_spear_0", craftedEvent!.Value.ItemId);
    }

    [Fact]
    public void CraftCompletion_GivesItemToVillager()
    {
        var (boot, sim) = CreateNewGame();

        var craft = new CraftStationLogic(EntityId.Next());
        craft.IsActive = true;
        craft.Position = new GridPosRPG(5, 5);
        sim.EntityManager.AddStructure(craft, craft.Position);
        boot.Services.Get<StructureManager>().SetCraftStationRecipe(craft.Id, "craft_wooden_spear");

        var villager = new VillagerLogic(EntityId.Next())
        {
            Name = "Crafter",
            Stamina = 100,
            MaxStamina = 100
        };
        villager.TryPickUpItem(CreateSticks(4));
        sim.VillagerSystem.AddVillager(villager);

        craft.PendingVillagerIds.Add(villager.Id);
        boot.Services.Get<StructureManager>().Tick(0.1f); // deposit + start crafting

        Assert.False(villager.IsCarryingItems); // deposited all sticks

        // Complete crafting
        craft.Tick(4.0f);
        boot.Services.Get<StructureManager>().Tick(0.1f);

        // Villager should now carry the crafted item
        Assert.True(villager.IsCarryingItems);
        Assert.True(villager.HasItem("wooden_spear_0"));
    }

    [Fact]
    public void CraftCompletion_ClearsCraftingVillagerId()
    {
        var (boot, sim) = CreateNewGame();

        var craft = new CraftStationLogic(EntityId.Next());
        craft.IsActive = true;
        craft.Position = new GridPosRPG(5, 5);
        sim.EntityManager.AddStructure(craft, craft.Position);
        boot.Services.Get<StructureManager>().SetCraftStationRecipe(craft.Id, "craft_wooden_spear");

        var villager = new VillagerLogic(EntityId.Next())
        {
            Name = "Crafter",
            Stamina = 100,
            MaxStamina = 100
        };
        villager.TryPickUpItem(CreateSticks(4));
        sim.VillagerSystem.AddVillager(villager);

        craft.PendingVillagerIds.Add(villager.Id);
        boot.Services.Get<StructureManager>().Tick(0.1f); // deposit + start
        Assert.True(craft.IsCrafting);

        // Complete crafting
        craft.Tick(4.0f);
        boot.Services.Get<StructureManager>().Tick(0.1f);

        // CraftingVillagerId should be cleared
        Assert.False(craft.IsCrafting);
        Assert.Null(craft.CraftingVillagerId);
    }

    [Fact]
    public void CraftCompletion_EjectsVillager_OrQueuesForExit()
    {
        var (boot, sim) = CreateNewGame();

        var craft = new CraftStationLogic(EntityId.Next());
        craft.IsActive = true;
        craft.Position = new GridPosRPG(5, 5);
        sim.EntityManager.AddStructure(craft, craft.Position);
        boot.Services.Get<StructureManager>().SetCraftStationRecipe(craft.Id, "craft_wooden_spear");

        var villager = new VillagerLogic(EntityId.Next())
        {
            Name = "Crafter",
            Stamina = 100,
            MaxStamina = 100
        };
        villager.TryPickUpItem(CreateSticks(4));
        sim.VillagerSystem.AddVillager(villager);

        craft.PendingVillagerIds.Add(villager.Id);
        boot.Services.Get<StructureManager>().Tick(0.1f); // deposit + start
        craft.Tick(4.0f); // complete craft timer

        VillagerLeftBuildingEvent? leftEvent = null;
        boot.Services.Get<EventBus>().Subscribe<VillagerLeftBuildingEvent>(e => leftEvent = e);

        boot.Services.Get<StructureManager>().Tick(0.1f); // process completion

        // No exit gate configured, so villager should be in WaitingToExitIds
        // (ExitVillagerViaGate returns false without a gate)
        Assert.Contains(villager.Id, craft.WaitingToExitIds);
        Assert.Null(villager.CurrentActivity); // cleared on exit
    }

    [Fact]
    public void VillagerWithNoItems_DepositsNothing_ExitsImmediately()
    {
        var (boot, sim) = CreateNewGame();

        var craft = new CraftStationLogic(EntityId.Next());
        craft.IsActive = true;
        craft.Position = new GridPosRPG(5, 5);
        sim.EntityManager.AddStructure(craft, craft.Position);
        boot.Services.Get<StructureManager>().SetCraftStationRecipe(craft.Id, "craft_wooden_spear");

        var villager = new VillagerLogic(EntityId.Next())
        {
            Name = "EmptyHands",
            Stamina = 100,
            MaxStamina = 100
        };
        sim.VillagerSystem.AddVillager(villager);

        craft.PendingVillagerIds.Add(villager.Id);
        boot.Services.Get<StructureManager>().Tick(0.1f);

        // No items deposited, not crafting, should be queued to exit
        Assert.Empty(craft.StoredInputs);
        Assert.False(craft.IsCrafting);
        Assert.Contains(villager.Id, craft.WaitingToExitIds);
    }

    [Fact]
    public void MultipleVillagers_FirstCrafts_OthersDepositAndExit()
    {
        var (boot, sim) = CreateNewGame();

        var craft = new CraftStationLogic(EntityId.Next());
        craft.IsActive = true;
        craft.Position = new GridPosRPG(5, 5);
        sim.EntityManager.AddStructure(craft, craft.Position);
        boot.Services.Get<StructureManager>().SetCraftStationRecipe(craft.Id, "craft_wooden_spear");

        // Three villagers enter simultaneously, each carrying 2 sticks
        var villagers = new List<VillagerLogic>();
        for (int i = 0; i < 3; i++)
        {
            var v = new VillagerLogic(EntityId.Next())
            {
                Name = $"Villager{i}",
                Stamina = 100,
                MaxStamina = 100
            };
            v.TryPickUpItem(CreateSticks(2));
            sim.VillagerSystem.AddVillager(v);
            craft.PendingVillagerIds.Add(v.Id);
            villagers.Add(v);
        }

        boot.Services.Get<StructureManager>().Tick(0.1f);

        // First villager deposits 2. Not enough (need 4). Exits.
        // Second villager deposits 2. Now 4 total. Starts crafting!
        // Third villager deposits 2 (total stored = 2 excess). But someone is crafting. Exits.
        Assert.True(craft.IsCrafting);
        Assert.Equal(villagers[1].Id.Value, craft.CraftingVillagerId);

        // All three deposited
        Assert.False(villagers[0].IsCarryingItems);
        Assert.False(villagers[1].IsCarryingItems);
        Assert.False(villagers[2].IsCarryingItems);

        // Villager 0 and 2 should be queued to exit (no exit gate)
        Assert.Contains(villagers[0].Id, craft.WaitingToExitIds);
        Assert.Contains(villagers[2].Id, craft.WaitingToExitIds);

        // Villager 1 is crafting (not in exit queue)
        Assert.DoesNotContain(villagers[1].Id, craft.WaitingToExitIds);
    }

    [Fact]
    public void PendingVillagerIds_ClearedAfterProcessing()
    {
        var (boot, sim) = CreateNewGame();

        var craft = new CraftStationLogic(EntityId.Next());
        craft.IsActive = true;
        craft.Position = new GridPosRPG(5, 5);
        sim.EntityManager.AddStructure(craft, craft.Position);
        boot.Services.Get<StructureManager>().SetCraftStationRecipe(craft.Id, "craft_wooden_spear");

        var villager = new VillagerLogic(EntityId.Next())
        {
            Name = "Worker",
            Stamina = 100,
            MaxStamina = 100
        };
        villager.TryPickUpItem(CreateSticks(1));
        sim.VillagerSystem.AddVillager(villager);

        craft.PendingVillagerIds.Add(villager.Id);
        Assert.Single(craft.PendingVillagerIds);

        boot.Services.Get<StructureManager>().Tick(0.1f);

        Assert.Empty(craft.PendingVillagerIds);
    }

    [Fact]
    public void CraftStation_NoRecipeSet_VillagerKeepsItemsAndExits()
    {
        var (boot, sim) = CreateNewGame();

        var craft = new CraftStationLogic(EntityId.Next());
        craft.IsActive = true;
        craft.Position = new GridPosRPG(5, 5);
        sim.EntityManager.AddStructure(craft, craft.Position);
        // No recipe set

        var villager = new VillagerLogic(EntityId.Next())
        {
            Name = "Carrier",
            Stamina = 100,
            MaxStamina = 100
        };
        villager.TryPickUpItem(CreateSticks(4));
        sim.VillagerSystem.AddVillager(villager);

        craft.PendingVillagerIds.Add(villager.Id);
        boot.Services.Get<StructureManager>().Tick(0.1f);

        // No recipe set → nothing deposited → villager keeps items → exits
        Assert.True(villager.IsCarryingItems);
        Assert.Empty(craft.StoredInputs);
        Assert.False(craft.IsCrafting);
        Assert.Contains(villager.Id, craft.WaitingToExitIds);
    }

    // ══════════════════════════════════════════════════════════════════
    // ── Full End-to-End Flow ─────────────────────────────────────────
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public void FullFlow_VillagerEnters_Deposits_Crafts_ExitsWithItem()
    {
        var (boot, sim) = CreateNewGame();

        var craft = new CraftStationLogic(EntityId.Next());
        craft.IsActive = true;
        craft.Position = new GridPosRPG(5, 5);
        sim.EntityManager.AddStructure(craft, craft.Position);
        boot.Services.Get<StructureManager>().SetCraftStationRecipe(craft.Id, "craft_wooden_spear");

        var villager = new VillagerLogic(EntityId.Next())
        {
            Name = "FullFlowVillager",
            Stamina = 100,
            MaxStamina = 100
        };
        villager.TryPickUpItem(CreateSticks(4));
        sim.VillagerSystem.AddVillager(villager);

        // Step 1: Villager enters
        craft.PendingVillagerIds.Add(villager.Id);

        // Step 2: Manager processes deposit + starts crafting
        boot.Services.Get<StructureManager>().Tick(0.1f);
        Assert.True(craft.IsCrafting);
        Assert.Equal(villager.Id.Value, craft.CraftingVillagerId);
        Assert.Equal(VillagerState.Working, villager.State);
        Assert.False(villager.IsCarryingItems);

        // Step 3: Tick the craft timer to completion
        craft.Tick(4.0f);
        Assert.True(craft.PendingCraftComplete);

        // Step 4: Manager processes completion, gives item to villager
        boot.Services.Get<StructureManager>().Tick(0.1f);
        Assert.False(craft.IsCrafting);
        Assert.True(villager.IsCarryingItems);
        Assert.True(villager.HasItem("wooden_spear_0"));
        Assert.Null(villager.CurrentActivity);
    }

    [Fact]
    public void FullFlow_PartialDeposits_ThenCraftOnFourthStick()
    {
        var (boot, sim) = CreateNewGame();

        var craft = new CraftStationLogic(EntityId.Next());
        craft.IsActive = true;
        craft.Position = new GridPosRPG(5, 5);
        sim.EntityManager.AddStructure(craft, craft.Position);
        boot.Services.Get<StructureManager>().SetCraftStationRecipe(craft.Id, "craft_wooden_spear");

        // First villager brings 1 stick
        var v1 = new VillagerLogic(EntityId.Next()) { Name = "V1", Stamina = 100, MaxStamina = 100 };
        v1.TryPickUpItem(CreateSticks(1));
        sim.VillagerSystem.AddVillager(v1);
        craft.PendingVillagerIds.Add(v1.Id);
        boot.Services.Get<StructureManager>().Tick(0.1f);
        Assert.Equal(1, craft.StoredInputs["sticks"]);
        Assert.False(craft.IsCrafting);

        // Second villager brings 2 sticks
        var v2 = new VillagerLogic(EntityId.Next()) { Name = "V2", Stamina = 100, MaxStamina = 100 };
        v2.TryPickUpItem(CreateSticks(2));
        sim.VillagerSystem.AddVillager(v2);
        craft.PendingVillagerIds.Add(v2.Id);
        boot.Services.Get<StructureManager>().Tick(0.1f);
        Assert.Equal(3, craft.StoredInputs["sticks"]);
        Assert.False(craft.IsCrafting);

        // Third villager brings 1 stick (total = 4, meets recipe)
        var v3 = new VillagerLogic(EntityId.Next()) { Name = "V3", Stamina = 100, MaxStamina = 100 };
        v3.TryPickUpItem(CreateSticks(1));
        sim.VillagerSystem.AddVillager(v3);
        craft.PendingVillagerIds.Add(v3.Id);
        boot.Services.Get<StructureManager>().Tick(0.1f);

        // Third villager should be the one crafting
        Assert.True(craft.IsCrafting);
        Assert.Equal(v3.Id.Value, craft.CraftingVillagerId);
        Assert.Equal(VillagerState.Working, v3.State);

        // Complete the craft
        craft.Tick(4.0f);
        boot.Services.Get<StructureManager>().Tick(0.1f);

        // V3 exits with the spear
        Assert.True(v3.HasItem("wooden_spear_0"));
        Assert.False(craft.IsCrafting);
    }

    [Fact]
    public void FullFlow_ConsecutiveCrafts_WorkCorrectly()
    {
        var (boot, sim) = CreateNewGame();

        var craft = new CraftStationLogic(EntityId.Next());
        craft.IsActive = true;
        craft.Position = new GridPosRPG(5, 5);
        sim.EntityManager.AddStructure(craft, craft.Position);
        boot.Services.Get<StructureManager>().SetCraftStationRecipe(craft.Id, "craft_wooden_spear");

        // First craft cycle
        var v1 = new VillagerLogic(EntityId.Next()) { Name = "V1", Stamina = 100, MaxStamina = 100 };
        v1.TryPickUpItem(CreateSticks(4));
        sim.VillagerSystem.AddVillager(v1);
        craft.PendingVillagerIds.Add(v1.Id);
        boot.Services.Get<StructureManager>().Tick(0.1f);
        craft.Tick(4.0f);
        boot.Services.Get<StructureManager>().Tick(0.1f);

        Assert.True(v1.HasItem("wooden_spear_0"));
        Assert.False(craft.IsCrafting);

        // Second craft cycle — station should accept a new crafter
        var v2 = new VillagerLogic(EntityId.Next()) { Name = "V2", Stamina = 100, MaxStamina = 100 };
        v2.TryPickUpItem(CreateSticks(4));
        sim.VillagerSystem.AddVillager(v2);
        craft.PendingVillagerIds.Add(v2.Id);
        boot.Services.Get<StructureManager>().Tick(0.1f);

        Assert.True(craft.IsCrafting);
        Assert.Equal(v2.Id.Value, craft.CraftingVillagerId);

        craft.Tick(4.0f);
        boot.Services.Get<StructureManager>().Tick(0.1f);

        Assert.True(v2.HasItem("wooden_spear_0"));
        Assert.False(craft.IsCrafting);
    }

    [Fact]
    public void InvalidVillagerId_InPendingList_IsSkipped()
    {
        var (boot, sim) = CreateNewGame();

        var craft = new CraftStationLogic(EntityId.Next());
        craft.IsActive = true;
        craft.Position = new GridPosRPG(5, 5);
        sim.EntityManager.AddStructure(craft, craft.Position);
        boot.Services.Get<StructureManager>().SetCraftStationRecipe(craft.Id, "craft_wooden_spear");

        // Add a non-existent villager ID
        craft.PendingVillagerIds.Add(999999);

        // Should not throw, should just skip
        boot.Services.Get<StructureManager>().Tick(0.1f);

        Assert.Empty(craft.PendingVillagerIds); // cleared
        Assert.False(craft.IsCrafting);
    }

    // ══════════════════════════════════════════════════════════════════
    // ── Selective Deposit Tests ───────────────────────────────────────
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public void Deposit_OnlyRecipeItems_NonRecipeItemsStayInInventory()
    {
        var (boot, sim) = CreateNewGame();

        var craft = new CraftStationLogic(EntityId.Next());
        craft.IsActive = true;
        craft.Position = new GridPosRPG(5, 5);
        sim.EntityManager.AddStructure(craft, craft.Position);
        boot.Services.Get<StructureManager>().SetCraftStationRecipe(craft.Id, "craft_wooden_spear");

        var villager = new VillagerLogic(EntityId.Next())
        {
            Name = "MixedCarrier",
            Stamina = 100,
            MaxStamina = 100
        };

        // Villager carries a weapon (non-recipe item) AND recipe ingredients
        var weapon = new ItemInstance
        {
            InstanceId = EntityIdFactory.Next(),
            ProtoId = "wooden_spear_0",
            Slot = EquipSlot.Weapon,
            Damage = 5f,
            Quantity = 1
        };
        villager.TryPickUpItem(weapon);
        villager.TryPickUpItem(CreateSticks(4));
        sim.VillagerSystem.AddVillager(villager);

        craft.PendingVillagerIds.Add(villager.Id);
        boot.Services.Get<StructureManager>().Tick(0.1f);

        // Sticks deposited and consumed for crafting; weapon stays in inventory
        Assert.True(craft.IsCrafting);
        Assert.False(craft.StoredInputs.ContainsKey("wooden_spear_0"));
        Assert.True(villager.HasItem("wooden_spear_0"));
        Assert.False(villager.HasItem("sticks"));
    }

    [Fact]
    public void Deposit_VillagerWithOnlyNonRecipeItems_DepositsNothing()
    {
        var (boot, sim) = CreateNewGame();

        var craft = new CraftStationLogic(EntityId.Next());
        craft.IsActive = true;
        craft.Position = new GridPosRPG(5, 5);
        sim.EntityManager.AddStructure(craft, craft.Position);
        boot.Services.Get<StructureManager>().SetCraftStationRecipe(craft.Id, "craft_wooden_spear");

        var villager = new VillagerLogic(EntityId.Next())
        {
            Name = "WeaponOnly",
            Stamina = 100,
            MaxStamina = 100
        };

        var weapon = new ItemInstance
        {
            InstanceId = EntityIdFactory.Next(),
            ProtoId = "wooden_spear_0",
            Slot = EquipSlot.Weapon,
            Damage = 5f,
            Quantity = 1
        };
        villager.TryPickUpItem(weapon);
        sim.VillagerSystem.AddVillager(villager);

        craft.PendingVillagerIds.Add(villager.Id);
        boot.Services.Get<StructureManager>().Tick(0.1f);

        // Nothing deposited, weapon stays, villager exits
        Assert.Empty(craft.StoredInputs);
        Assert.True(villager.HasItem("wooden_spear_0"));
        Assert.False(craft.IsCrafting);
    }

    [Fact]
    public void Deposit_VillagerWithMixedItems_CraftsAndExitsWithBothItems()
    {
        var (boot, sim) = CreateNewGame();

        var craft = new CraftStationLogic(EntityId.Next());
        craft.IsActive = true;
        craft.Position = new GridPosRPG(5, 5);
        sim.EntityManager.AddStructure(craft, craft.Position);
        boot.Services.Get<StructureManager>().SetCraftStationRecipe(craft.Id, "craft_wooden_spear");

        var villager = new VillagerLogic(EntityId.Next())
        {
            Name = "MixedCrafter",
            Stamina = 100,
            MaxStamina = 100
        };

        // Villager carries 1 existing spear + 4 sticks (enough for another spear)
        var existingWeapon = new ItemInstance
        {
            InstanceId = EntityIdFactory.Next(),
            ProtoId = "wooden_spear_0",
            Slot = EquipSlot.Weapon,
            Damage = 5f,
            Quantity = 1
        };
        villager.TryPickUpItem(existingWeapon);
        villager.TryPickUpItem(CreateSticks(4));
        sim.VillagerSystem.AddVillager(villager);

        craft.PendingVillagerIds.Add(villager.Id);
        boot.Services.Get<StructureManager>().Tick(0.1f);

        // Should be crafting — sticks deposited, weapon kept, has 1 slot left for output
        Assert.True(craft.IsCrafting);
        Assert.Equal(villager.Id.Value, craft.CraftingVillagerId);
        Assert.True(villager.HasItem("wooden_spear_0"));
        Assert.Equal(1, villager.Inventory.Count); // just the weapon

        // Complete the craft
        craft.Tick(4.0f);
        boot.Services.Get<StructureManager>().Tick(0.1f);

        // Villager exits with BOTH weapons
        Assert.Equal(2, villager.Inventory.Count);
        Assert.Equal(2, villager.CountItem("wooden_spear_0"));
        Assert.False(craft.IsCrafting);
    }

    [Fact]
    public void CraftDoesNotStart_WhenVillagerInventoryFull()
    {
        var (boot, sim) = CreateNewGame();

        var craft = new CraftStationLogic(EntityId.Next());
        craft.IsActive = true;
        craft.Position = new GridPosRPG(5, 5);
        sim.EntityManager.AddStructure(craft, craft.Position);
        boot.Services.Get<StructureManager>().SetCraftStationRecipe(craft.Id, "craft_wooden_spear");

        // Pre-store enough sticks so recipe is satisfied even without this villager's deposit
        craft.DepositItem("sticks", 4);

        var villager = new VillagerLogic(EntityId.Next())
        {
            Name = "FullInventory",
            Stamina = 100,
            MaxStamina = 100
        };

        // Fill villager inventory to max (4 slots) with non-recipe items
        for (int j = 0; j < 4; j++)
        {
            villager.TryPickUpItem(new ItemInstance
            {
                InstanceId = EntityIdFactory.Next(),
                ProtoId = $"some_item_{j}",
                Quantity = 1
            });
        }
        Assert.True(villager.IsInventoryFull);
        sim.VillagerSystem.AddVillager(villager);

        craft.PendingVillagerIds.Add(villager.Id);
        boot.Services.Get<StructureManager>().Tick(0.1f);

        // Recipe is satisfied but villager can't carry the output → no crafting
        Assert.False(craft.IsCrafting);
        Assert.True(villager.IsInventoryFull);
        // Sticks are still in storage, not consumed
        Assert.Equal(4, craft.StoredInputs["sticks"]);
    }

    // ══════════════════════════════════════════════════════════════════
    // ── VillagerLogic Selective Drop Tests ────────────────────────────
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public void DropItemsMatchingKeys_DropsOnlyMatchingItems()
    {
        var villager = CreateVillager();
        villager.TryPickUpItem(CreateSticks(2));
        villager.TryPickUpItem(new ItemInstance
        {
            InstanceId = EntityIdFactory.Next(),
            ProtoId = "wooden_spear_0",
            Slot = EquipSlot.Weapon,
            Damage = 5f,
            Quantity = 1
        });
        villager.TryPickUpItem(new ItemInstance
        {
            InstanceId = EntityIdFactory.Next(),
            ProtoId = "ore",
            Quantity = 3
        });

        var recipeKeys = new HashSet<string> { "sticks", "ore" };
        var dropped = villager.DropItemsMatchingKeys(recipeKeys);

        Assert.Equal(2, dropped.Count);
        Assert.Contains(dropped, d => d.ProtoId == "sticks");
        Assert.Contains(dropped, d => d.ProtoId == "ore");
        Assert.Single(villager.Inventory);
        Assert.Equal("wooden_spear_0", villager.Inventory[0].ProtoId);
    }

    [Fact]
    public void DropItemsMatchingKeys_EmptyKeys_DropsNothing()
    {
        var villager = CreateVillager();
        villager.TryPickUpItem(CreateSticks(2));

        var dropped = villager.DropItemsMatchingKeys(new HashSet<string>());
        Assert.Empty(dropped);
        Assert.True(villager.IsCarryingItems);
    }

    [Fact]
    public void DropItemsMatchingKeys_NoMatchingItems_DropsNothing()
    {
        var villager = CreateVillager();
        villager.TryPickUpItem(new ItemInstance
        {
            InstanceId = EntityIdFactory.Next(),
            ProtoId = "wooden_spear_0",
            Slot = EquipSlot.Weapon,
            Damage = 5f,
            Quantity = 1
        });

        var recipeKeys = new HashSet<string> { "sticks" };
        var dropped = villager.DropItemsMatchingKeys(recipeKeys);
        Assert.Empty(dropped);
        Assert.Single(villager.Inventory);
    }

    // ══════════════════════════════════════════════════════════════════
    // ── Auto-Equip From Inventory Tests ──────────────────────────────
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public void AutoEquipFromInventory_EquipsBestWeapon()
    {
        var villager = CreateVillager();
        villager.TryPickUpItem(new ItemInstance
        {
            InstanceId = EntityIdFactory.Next(),
            ProtoId = "wooden_spear_0",
            Slot = EquipSlot.Weapon,
            Damage = 5f,
            Quantity = 1
        });
        villager.TryPickUpItem(new ItemInstance
        {
            InstanceId = EntityIdFactory.Next(),
            ProtoId = "iron_sword_1",
            Slot = EquipSlot.Weapon,
            Damage = 15f,
            Quantity = 1
        });

        Assert.Null(villager.EquippedWeaponId);
        villager.AutoEquipFromInventory();
        Assert.Equal("iron_sword_1", villager.EquippedWeaponId);
    }

    [Fact]
    public void AutoEquipFromInventory_EquipsBestArmor()
    {
        var villager = CreateVillager();
        villager.TryPickUpItem(new ItemInstance
        {
            InstanceId = EntityIdFactory.Next(),
            ProtoId = "leather_vest_0",
            Slot = EquipSlot.ChestArmor,
            Defense = 3f,
            Quantity = 1
        });
        villager.TryPickUpItem(new ItemInstance
        {
            InstanceId = EntityIdFactory.Next(),
            ProtoId = "iron_helm_1",
            Slot = EquipSlot.Helmet,
            Defense = 8f,
            Quantity = 1
        });

        Assert.Null(villager.EquippedArmorId);
        villager.AutoEquipFromInventory();
        Assert.Equal("iron_helm_1", villager.EquippedArmorId);
    }

    [Fact]
    public void AutoEquipFromInventory_EquipsBothWeaponAndArmor()
    {
        var villager = CreateVillager();
        villager.TryPickUpItem(new ItemInstance
        {
            InstanceId = EntityIdFactory.Next(),
            ProtoId = "wooden_spear_0",
            Slot = EquipSlot.Weapon,
            Damage = 5f,
            Quantity = 1
        });
        villager.TryPickUpItem(new ItemInstance
        {
            InstanceId = EntityIdFactory.Next(),
            ProtoId = "leather_vest_0",
            Slot = EquipSlot.ChestArmor,
            Defense = 3f,
            Quantity = 1
        });

        villager.AutoEquipFromInventory();
        Assert.True(villager.HasWeapon);
        Assert.True(villager.HasArmor);
        Assert.Equal("wooden_spear_0", villager.EquippedWeaponId);
        Assert.Equal("leather_vest_0", villager.EquippedArmorId);
    }

    [Fact]
    public void AutoEquipFromInventory_NoEquipment_DoesNothing()
    {
        var villager = CreateVillager();
        villager.TryPickUpItem(CreateSticks(4));

        villager.AutoEquipFromInventory();
        Assert.Null(villager.EquippedWeaponId);
        Assert.Null(villager.EquippedArmorId);
    }

    [Fact]
    public void AutoEquipFromInventory_EmptyInventory_DoesNothing()
    {
        var villager = CreateVillager();
        villager.AutoEquipFromInventory();
        Assert.Null(villager.EquippedWeaponId);
        Assert.Null(villager.EquippedArmorId);
    }

    // ══════════════════════════════════════════════════════════════════
    // ── Full Flow: Craft → Dungeon with Auto-Equip ───────────────────
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public void FullFlow_VillagerCraftsWeapon_ThenAutoEquipsAtDungeon()
    {
        var (boot, sim) = CreateNewGame();

        // Set up craft station with spear recipe
        var craft = new CraftStationLogic(EntityId.Next());
        craft.IsActive = true;
        craft.Position = new GridPosRPG(5, 5);
        sim.EntityManager.AddStructure(craft, craft.Position);
        boot.Services.Get<StructureManager>().SetCraftStationRecipe(craft.Id, "craft_wooden_spear");

        // Set up dungeon portal
        var dungeon = new DungeonPortalLogic(EntityId.Next());
        dungeon.IsActive = true;
        dungeon.Position = new GridPosRPG(10, 5);
        dungeon.DungeonId = "dark_forest";
        sim.EntityManager.AddStructure(dungeon, dungeon.Position);

        // Create trained warrior carrying 4 sticks
        var villager = new VillagerLogic(EntityId.Next())
        {
            Name = "WarriorCrafter",
            Stamina = 100,
            MaxStamina = 100,
            TrainedClass = VillagerClass.Warrior
        };
        villager.TryPickUpItem(CreateSticks(4));
        sim.VillagerSystem.AddVillager(villager);

        // Step 1: Craft the weapon
        craft.PendingVillagerIds.Add(villager.Id);
        boot.Services.Get<StructureManager>().Tick(0.1f);
        Assert.True(craft.IsCrafting);

        craft.Tick(4.0f);
        boot.Services.Get<StructureManager>().Tick(0.1f);
        Assert.True(villager.HasItem("wooden_spear_0"));
        Assert.False(villager.HasWeapon); // Not yet equipped — just in inventory

        // Step 2: Simulate entering dungeon by calling AutoEquipFromInventory
        villager.AutoEquipFromInventory();
        Assert.True(villager.HasWeapon);
        Assert.Equal("wooden_spear_0", villager.EquippedWeaponId);
    }

    [Fact]
    public void SecondCraftCycle_VillagerWithExistingWeapon_KeepsWeaponAndCraftsAnother()
    {
        var (boot, sim) = CreateNewGame();

        var craft = new CraftStationLogic(EntityId.Next());
        craft.IsActive = true;
        craft.Position = new GridPosRPG(5, 5);
        sim.EntityManager.AddStructure(craft, craft.Position);
        boot.Services.Get<StructureManager>().SetCraftStationRecipe(craft.Id, "craft_wooden_spear");

        // Simulate a villager that already has a weapon from a previous craft (e.g. dropped sticks first)
        var villager = new VillagerLogic(EntityId.Next())
        {
            Name = "ReturningCrafter",
            Stamina = 100,
            MaxStamina = 100
        };

        // Existing weapon in inventory
        villager.TryPickUpItem(new ItemInstance
        {
            InstanceId = EntityIdFactory.Next(),
            ProtoId = "wooden_spear_0",
            Slot = EquipSlot.Weapon,
            Damage = 5f,
            Quantity = 1
        });
        // New sticks for another craft
        villager.TryPickUpItem(CreateSticks(4));
        Assert.Equal(2, villager.Inventory.Count);
        sim.VillagerSystem.AddVillager(villager);

        craft.PendingVillagerIds.Add(villager.Id);
        boot.Services.Get<StructureManager>().Tick(0.1f);

        // Sticks deposited, weapon kept, crafting started
        Assert.True(craft.IsCrafting);
        Assert.Single(villager.Inventory);
        Assert.Equal("wooden_spear_0", villager.Inventory[0].ProtoId);

        // Complete the craft
        craft.Tick(4.0f);
        boot.Services.Get<StructureManager>().Tick(0.1f);

        // Villager exits with both spears
        Assert.Equal(2, villager.Inventory.Count);
        Assert.Equal(2, villager.CountItem("wooden_spear_0"));
    }
}
