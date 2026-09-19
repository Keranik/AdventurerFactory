using ForgeFlow.Core;
using ForgeFlow.Core.Data;
using ForgeFlow.Core.Entities;
using ForgeFlow.Core.Events;
using ForgeFlow.Core.Proto;
using ForgeFlow.Core.Proto.Logic;
using ForgeFlow.Core.Proto.Prototypes;
using ForgeFlow.Core.Save;
using ForgeFlow.Core.Systems;

namespace ForgeFlow.Tests;

/// <summary>
/// Comprehensive tests for the Stockpile system:
/// - Single-product assignment (AcceptedItemId)
/// - Deposit/Withdraw with product assignment
/// - InitializeFromProto with AcceptedItemId
/// - StructureManager stockpile product API
/// - StockpileFilterChangedEvent publishing
/// - ProductPicker data entries
/// - BaseInspectorPanel shared infrastructure
/// </summary>
public class StockpileTests
{
    // ── AcceptedItemId Single-Product Assignment ──────────────────

    [Fact]
    public void StockpileLogic_AcceptedItemId_DefaultsToNull()
    {
        var stockpile = new StockpileLogic(EntityId.Next());
        Assert.Null(stockpile.AcceptedItemId);
    }

    [Fact]
    public void StockpileLogic_AcceptsItem_ReturnsTrue_WhenNoProductAssigned()
    {
        var stockpile = new StockpileLogic(EntityId.Next());
        Assert.True(stockpile.AcceptsItem("sticks"));
        Assert.True(stockpile.AcceptsItem("ore"));
        Assert.True(stockpile.AcceptsItem("anything"));
    }

    [Fact]
    public void StockpileLogic_AcceptsItem_ReturnsTrue_WhenItemMatchesProduct()
    {
        var stockpile = new StockpileLogic(EntityId.Next());
        stockpile.SetAcceptedItem("sticks");
        Assert.True(stockpile.AcceptsItem("sticks"));
    }

    [Fact]
    public void StockpileLogic_AcceptsItem_ReturnsFalse_WhenItemDoesNotMatchProduct()
    {
        var stockpile = new StockpileLogic(EntityId.Next());
        stockpile.SetAcceptedItem("sticks");
        Assert.False(stockpile.AcceptsItem("ore"));
        Assert.False(stockpile.AcceptsItem("logs"));
    }

    [Fact]
    public void StockpileLogic_SetAcceptedItem_SetsPendingFilterChange()
    {
        var stockpile = new StockpileLogic(EntityId.Next());
        Assert.False(stockpile.PendingFilterChange);
        stockpile.SetAcceptedItem("sticks");
        Assert.True(stockpile.PendingFilterChange);
    }

    [Fact]
    public void StockpileLogic_SetAcceptedItem_SameItem_NoPendingChange()
    {
        var stockpile = new StockpileLogic(EntityId.Next());
        stockpile.SetAcceptedItem("sticks");
        stockpile.PendingFilterChange = false;
        stockpile.SetAcceptedItem("sticks"); // same item
        Assert.False(stockpile.PendingFilterChange);
    }

    [Fact]
    public void StockpileLogic_ClearAcceptedItem_SetsPendingFilterChange()
    {
        var stockpile = new StockpileLogic(EntityId.Next());
        stockpile.SetAcceptedItem("sticks");
        stockpile.PendingFilterChange = false;
        stockpile.ClearAcceptedItem();
        Assert.True(stockpile.PendingFilterChange);
        Assert.Null(stockpile.AcceptedItemId);
    }

    [Fact]
    public void StockpileLogic_ClearAcceptedItem_WhenNull_NoPendingChange()
    {
        var stockpile = new StockpileLogic(EntityId.Next());
        stockpile.ClearAcceptedItem();
        Assert.False(stockpile.PendingFilterChange);
    }

    [Fact]
    public void StockpileLogic_SetAcceptedItem_ReplacesExistingProduct()
    {
        var stockpile = new StockpileLogic(EntityId.Next());
        stockpile.SetAcceptedItem("old_item");
        stockpile.PendingFilterChange = false;

        stockpile.SetAcceptedItem("sticks");
        Assert.Equal("sticks", stockpile.AcceptedItemId);
        Assert.True(stockpile.PendingFilterChange);
    }

    [Fact]
    public void StockpileLogic_ClearAcceptedItem_RemovesProductAndSetsPending()
    {
        var stockpile = new StockpileLogic(EntityId.Next());
        stockpile.SetAcceptedItem("sticks");
        stockpile.PendingFilterChange = false;

        stockpile.ClearAcceptedItem();
        Assert.Null(stockpile.AcceptedItemId);
        Assert.True(stockpile.PendingFilterChange);
    }

    [Fact]
    public void StockpileLogic_ClearAcceptedItem_EmptyAssignment_NoPendingChange()
    {
        var stockpile = new StockpileLogic(EntityId.Next());
        stockpile.ClearAcceptedItem();
        Assert.False(stockpile.PendingFilterChange);
    }

    // ── Deposit with Product Assignment ──────────────────────────

    [Fact]
    public void StockpileLogic_Deposit_AcceptsAll_WhenNoProductAssigned()
    {
        var stockpile = new StockpileLogic(EntityId.Next());
        Assert.True(stockpile.Deposit("sticks", 5));
        Assert.True(stockpile.Deposit("ore", 3));
        Assert.Equal(8, stockpile.TotalStored);
    }

    [Fact]
    public void StockpileLogic_Deposit_RejectsItem_WhenNotMatchingProduct()
    {
        var stockpile = new StockpileLogic(EntityId.Next());
        stockpile.SetAcceptedItem("sticks");
        Assert.False(stockpile.Deposit("ore", 1));
        Assert.Equal(0, stockpile.TotalStored);
    }

    [Fact]
    public void StockpileLogic_Deposit_AcceptsItem_WhenMatchingProduct()
    {
        var stockpile = new StockpileLogic(EntityId.Next());
        stockpile.SetAcceptedItem("sticks");
        Assert.True(stockpile.Deposit("sticks", 5));
        Assert.Equal(5, stockpile.TotalStored);
    }

    [Fact]
    public void StockpileLogic_Deposit_RejectsWhenFull_EvenIfMatchingProduct()
    {
        var stockpile = new StockpileLogic(EntityId.Next()) { MaxCapacity = 10 };
        stockpile.SetAcceptedItem("sticks");
        Assert.True(stockpile.Deposit("sticks", 10));
        Assert.False(stockpile.Deposit("sticks", 1));
    }

    [Fact]
    public void StockpileLogic_Deposit_RejectsNonMatchingEvenWithRoom()
    {
        var stockpile = new StockpileLogic(EntityId.Next()) { MaxCapacity = 100 };
        stockpile.SetAcceptedItem("sticks");
        Assert.True(stockpile.HasRoom);
        Assert.False(stockpile.Deposit("ore", 1));
    }

    // ── Withdraw (unchanged behavior) ────────────────────────────

    [Fact]
    public void StockpileLogic_Withdraw_ReturnsAmount_WhenAvailable()
    {
        var stockpile = new StockpileLogic(EntityId.Next());
        stockpile.Deposit("sticks", 10);
        Assert.Equal(5, stockpile.Withdraw("sticks", 5));
        Assert.Equal(5, stockpile.GetCount("sticks"));
    }

    [Fact]
    public void StockpileLogic_Withdraw_ReturnsZero_WhenNotStored()
    {
        var stockpile = new StockpileLogic(EntityId.Next());
        Assert.Equal(0, stockpile.Withdraw("ore", 5));
    }

    [Fact]
    public void StockpileLogic_Withdraw_ClampsToAvailable()
    {
        var stockpile = new StockpileLogic(EntityId.Next());
        stockpile.Deposit("sticks", 3);
        Assert.Equal(3, stockpile.Withdraw("sticks", 10));
        Assert.Equal(0, stockpile.GetCount("sticks"));
    }

    [Fact]
    public void StockpileLogic_Withdraw_RemovesKeyWhenZero()
    {
        var stockpile = new StockpileLogic(EntityId.Next());
        stockpile.Deposit("sticks", 5);
        stockpile.Withdraw("sticks", 5);
        Assert.DoesNotContain("sticks", stockpile.StoredResources.Keys);
    }

    // ── InitializeFromProto ──────────────────────────────────────

    [Fact]
    public void StockpileLogic_InitializeFromProto_SetsMaxCapacity()
    {
        var proto = new StockpileProto { MaxCapacity = 200 };
        var logic = new StockpileLogic(EntityId.Next());
        logic.InitializeFromProto(proto);
        Assert.Equal(200, logic.MaxCapacity);
    }

    [Fact]
    public void StockpileLogic_InitializeFromProto_SetsAcceptedItemId()
    {
        var proto = new StockpileProto
        {
            MaxCapacity = 50,
            AcceptedItemId = "sticks"
        };
        var logic = new StockpileLogic(EntityId.Next());
        logic.InitializeFromProto(proto);
        Assert.Equal("sticks", logic.AcceptedItemId);
    }

    [Fact]
    public void StockpileLogic_InitializeFromProto_NullAcceptedItemId_AcceptsAll()
    {
        var proto = new StockpileProto { AcceptedItemId = null };
        var logic = new StockpileLogic(EntityId.Next());
        logic.InitializeFromProto(proto);
        Assert.Null(logic.AcceptedItemId);
        Assert.True(logic.AcceptsItem("anything"));
    }

    [Fact]
    public void StockpileLogic_InitializeFromProto_ClearsPreviousProduct()
    {
        var logic = new StockpileLogic(EntityId.Next());
        logic.SetAcceptedItem("old_item");

        var proto = new StockpileProto { AcceptedItemId = "new_item" };
        logic.InitializeFromProto(proto);
        Assert.Equal("new_item", logic.AcceptedItemId);
    }

    // ── StockpileProto ───────────────────────────────────────────

    [Fact]
    public void StockpileProto_MaxCapacity_DefaultIs100()
    {
        var proto = new StockpileProto();
        Assert.Equal(100, proto.MaxCapacity);
    }

    [Fact]
    public void StockpileProto_AcceptedItemId_DefaultsToNull()
    {
        var proto = new StockpileProto();
        Assert.Null(proto.AcceptedItemId);
    }

    [Fact]
    public void StockpileProto_AcceptedItemId_CanBeSet()
    {
        var proto = new StockpileProto
        {
            AcceptedItemId = "sticks"
        };
        Assert.Equal("sticks", proto.AcceptedItemId);
    }

    // ── TotalStored / HasRoom ────────────────────────────────────

    [Fact]
    public void StockpileLogic_TotalStored_AggregatesAllResources()
    {
        var stockpile = new StockpileLogic(EntityId.Next());
        stockpile.Deposit("sticks", 5);
        stockpile.Deposit("ore", 3);
        stockpile.Deposit("logs", 2);
        Assert.Equal(10, stockpile.TotalStored);
    }

    [Fact]
    public void StockpileLogic_HasRoom_TrueWhenUnderCapacity()
    {
        var stockpile = new StockpileLogic(EntityId.Next()) { MaxCapacity = 10 };
        stockpile.Deposit("sticks", 5);
        Assert.True(stockpile.HasRoom);
    }

    [Fact]
    public void StockpileLogic_HasRoom_FalseWhenAtCapacity()
    {
        var stockpile = new StockpileLogic(EntityId.Next()) { MaxCapacity = 5 };
        stockpile.Deposit("sticks", 5);
        Assert.False(stockpile.HasRoom);
    }

    // ── StructureManager Stockpile Product API ────────────────────

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

    [Fact]
    public void StructureManager_SetStockpileProduct_ReturnsFalse_WhenStructureNotFound()
    {
        var (_, sim) = CreateNewGame();
        Assert.False(sim.StructureManager.SetStockpileProduct(999999, "sticks"));
    }

    [Fact]
    public void StructureManager_ClearStockpileProduct_ReturnsFalse_WhenStructureNotFound()
    {
        var (_, sim) = CreateNewGame();
        Assert.False(sim.StructureManager.ClearStockpileProduct(999999));
    }

    [Fact]
    public void StructureManager_SetStockpileProduct_ReturnsFalse_WhenNotStockpile()
    {
        var (_, sim) = CreateNewGame();
        var spawner = new VillageSpawnerLogic(EntityId.Next());
        sim.StructureManager.AddProtoStructure(spawner, new GridPosRPG(5, 5));

        Assert.False(sim.StructureManager.SetStockpileProduct(spawner.Id, "sticks"));
    }

    [Fact]
    public void StructureManager_SetStockpileProduct_SetsProduct_OnStockpile()
    {
        var (_, sim) = CreateNewGame();
        var stockpile = new StockpileLogic(EntityId.Next());
        sim.StructureManager.AddProtoStructure(stockpile, new GridPosRPG(5, 5));

        Assert.True(sim.StructureManager.SetStockpileProduct(stockpile.Id, "sticks"));
        Assert.Equal("sticks", stockpile.AcceptedItemId);
    }

    [Fact]
    public void StructureManager_SetStockpileProduct_ReplacesExistingProduct()
    {
        var (_, sim) = CreateNewGame();
        var stockpile = new StockpileLogic(EntityId.Next());
        sim.StructureManager.AddProtoStructure(stockpile, new GridPosRPG(5, 5));

        Assert.True(sim.StructureManager.SetStockpileProduct(stockpile.Id, "sticks"));
        Assert.True(sim.StructureManager.SetStockpileProduct(stockpile.Id, "ore"));
        Assert.Equal("ore", stockpile.AcceptedItemId);
    }

    [Fact]
    public void StructureManager_ClearStockpileProduct_ClearsProduct()
    {
        var (_, sim) = CreateNewGame();
        var stockpile = new StockpileLogic(EntityId.Next());
        sim.StructureManager.AddProtoStructure(stockpile, new GridPosRPG(5, 5));

        sim.StructureManager.SetStockpileProduct(stockpile.Id, "sticks");
        Assert.True(sim.StructureManager.ClearStockpileProduct(stockpile.Id));
        Assert.Null(stockpile.AcceptedItemId);
    }

    [Fact]
    public void StructureManager_SetStockpileProduct_NullClearsProduct()
    {
        var (_, sim) = CreateNewGame();
        var stockpile = new StockpileLogic(EntityId.Next());
        sim.StructureManager.AddProtoStructure(stockpile, new GridPosRPG(5, 5));

        sim.StructureManager.SetStockpileProduct(stockpile.Id, "sticks");
        Assert.True(sim.StructureManager.SetStockpileProduct(stockpile.Id, null));
        Assert.Null(stockpile.AcceptedItemId);
    }

    // ── StockpileFilterChangedEvent ─────────────────────────────

    [Fact]
    public void StructureManager_PublishesFilterChangedEvent_OnTick()
    {
        var (boot, sim) = CreateNewGame();
        var stockpile = new StockpileLogic(EntityId.Next());
        sim.StructureManager.AddProtoStructure(stockpile, new GridPosRPG(5, 5));

        StockpileFilterChangedEvent? received = null;
        boot.Services.Get<EventBus>().Subscribe<StockpileFilterChangedEvent>(e => received = e);

        sim.StructureManager.SetStockpileProduct(stockpile.Id, "sticks");

        // Tick so the event gets published
        sim.StructureManager.Tick(0.1f);

        Assert.NotNull(received);
        Assert.Equal(stockpile.Id, received.Value.StockpileId);
    }

    // ── Stockpile Category ────────────────────────────────────

    [Fact]
    public void StockpileLogic_GetCategoryName_IsStockpile()
    {
        var stockpile = new StockpileLogic(EntityId.Next());
        Assert.Equal("Stockpile", stockpile.GetCategoryName());
    }

    [Fact]
    public void StockpileLogic_ProcessingDuration_IsZero()
    {
        var stockpile = new StockpileLogic(EntityId.Next());
        Assert.Equal(0f, stockpile.ProcessingDuration);
    }

    // ── GetCount ─────────────────────────────────────────────────

    [Fact]
    public void StockpileLogic_GetCount_ReturnsZero_WhenEmpty()
    {
        var stockpile = new StockpileLogic(EntityId.Next());
        Assert.Equal(0, stockpile.GetCount("sticks"));
    }

    [Fact]
    public void StockpileLogic_GetCount_ReturnsCorrectAmount()
    {
        var stockpile = new StockpileLogic(EntityId.Next());
        stockpile.Deposit("sticks", 7);
        Assert.Equal(7, stockpile.GetCount("sticks"));
    }

    // ── Product assignment lifecycle ─────────────────────────────

    [Fact]
    public void StockpileLogic_ProductLifecycle_Sequence()
    {
        var stockpile = new StockpileLogic(EntityId.Next());

        // Start: accepts all
        Assert.True(stockpile.AcceptsItem("sticks"));
        Assert.True(stockpile.AcceptsItem("ore"));

        // Assign product: only sticks
        stockpile.SetAcceptedItem("sticks");
        Assert.True(stockpile.AcceptsItem("sticks"));
        Assert.False(stockpile.AcceptsItem("ore"));

        // Change product: only ore
        stockpile.SetAcceptedItem("ore");
        Assert.False(stockpile.AcceptsItem("sticks"));
        Assert.True(stockpile.AcceptsItem("ore"));

        // Clear: accepts all again
        stockpile.ClearAcceptedItem();
        Assert.True(stockpile.AcceptsItem("sticks"));
        Assert.True(stockpile.AcceptsItem("ore"));
        Assert.True(stockpile.AcceptsItem("logs"));
    }

    // ── PendingVillagerIds ───────────────────────────────────────

    [Fact]
    public void StockpileLogic_PendingVillagerIds_DefaultsToEmpty()
    {
        var stockpile = new StockpileLogic(EntityId.Next());
        Assert.Empty(stockpile.PendingVillagerIds);
    }

    // ── Tick is passive ──────────────────────────────────────────

    [Fact]
    public void StockpileLogic_Tick_DoesNothing()
    {
        var stockpile = new StockpileLogic(EntityId.Next());
        stockpile.Deposit("sticks", 5);
        stockpile.Tick(1.0f);
        Assert.Equal(5, stockpile.GetCount("sticks"));
    }

    // ── Deposit product + capacity combined edge cases ───────────

    [Fact]
    public void StockpileLogic_Deposit_OnlyAcceptsAssignedProduct()
    {
        var stockpile = new StockpileLogic(EntityId.Next()) { MaxCapacity = 50 };
        stockpile.SetAcceptedItem("sticks");

        Assert.True(stockpile.Deposit("sticks", 10));
        Assert.False(stockpile.Deposit("logs", 5));
        Assert.False(stockpile.Deposit("ore", 1));
        Assert.False(stockpile.Deposit("gold", 1));
        Assert.Equal(10, stockpile.TotalStored);
    }

    [Fact]
    public void StockpileLogic_Deposit_ZeroQuantity_Succeeds()
    {
        var stockpile = new StockpileLogic(EntityId.Next());
        Assert.True(stockpile.Deposit("sticks", 0));
        Assert.Equal(0, stockpile.TotalStored);
    }

    // ── SelectStockpileProduct Tutorial Condition ─────────────────

    [Fact]
    public void SelectStockpileProduct_AdvancesTutorial_WhenSticksProductAssigned()
    {
        var (boot, sim) = CreateNewGame();

        // Fast-forward tutorial to the SelectStockpileProduct mission
        var completedIds = new List<string>
        {
            "p1_01_place_home", "p1_02_exit_gate", "p1_03_place_forestry",
            "p1_04_forestry_entrance", "p1_05_connect_path", "p1_06_watch_gathering",
            "p1_07_place_stockpile"
        };
        sim.TutorialSystem.LoadFromSave(completedIds, "p1_08_select_sticks_filter");

        Assert.NotNull(sim.TutorialSystem.ActiveMission);
        Assert.Equal("p1_08_select_sticks_filter", sim.TutorialSystem.ActiveMission!.ProtoId);

        // Place a stockpile and assign "sticks" product
        var stockpile = new StockpileLogic(EntityId.Next());
        sim.StructureManager.AddProtoStructure(stockpile, new GridPosRPG(5, 5));
        sim.StructureManager.SetStockpileProduct(stockpile.Id, "sticks");

        // Tick so StockpileFilterChangedEvent is published
        sim.StructureManager.Tick(0.1f);

        // Tutorial mission should be completed
        var mission = sim.TutorialSystem.AllMissions.First(m => m.ProtoId == "p1_08_select_sticks_filter");
        Assert.Equal(TutorialMissionState.Completed, mission.State);
    }

    [Fact]
    public void SelectStockpileProduct_DoesNotAdvance_WhenWrongItemSelected()
    {
        var (boot, sim) = CreateNewGame();

        // Fast-forward tutorial to the SelectStockpileProduct mission
        var completedIds = new List<string>
        {
            "p1_01_place_home", "p1_02_exit_gate", "p1_03_place_forestry",
            "p1_04_forestry_entrance", "p1_05_connect_path", "p1_06_watch_gathering",
            "p1_07_place_stockpile"
        };
        sim.TutorialSystem.LoadFromSave(completedIds, "p1_08_select_sticks_filter");

        // Place a stockpile and assign "ore" product (not "sticks")
        var stockpile = new StockpileLogic(EntityId.Next());
        sim.StructureManager.AddProtoStructure(stockpile, new GridPosRPG(5, 5));
        sim.StructureManager.SetStockpileProduct(stockpile.Id, "ore");

        // Tick so StockpileFilterChangedEvent is published
        sim.StructureManager.Tick(0.1f);

        // Tutorial should NOT be completed (needs "sticks", not "ore")
        Assert.Equal("p1_08_select_sticks_filter", sim.TutorialSystem.ActiveMission!.ProtoId);
        var cond = sim.TutorialSystem.ActiveMission.Conditions.Find(
            c => c.Type == TutorialConditionType.SelectStockpileProduct);
        Assert.NotNull(cond);
        Assert.False(cond!.IsMet);
    }

    [Fact]
    public void Tutorial_TotalMissionCount_Is29_AfterSelectSticksAdded()
    {
        var (_, sim) = CreateNewGame();
        Assert.Equal(29, sim.TutorialSystem.TotalCount);
    }

    // ─── StockpileDropOff ──────────────────────────────────────────────

    [Fact]
    public void StockpileDropOff_ManagerClearsActivity()
    {
        EntityBase.ResetIdCounter();
        var villager = new VillagerLogic(EntityId.Next()) { IsActive = true };
        villager.AssignJob(VillagerJob.Builder);
        villager.CurrentActivity = "Gathering sticks";
        Assert.Equal(VillagerJob.Builder, villager.Profession);

        var item = new ItemInstance { ProtoId = "sticks", Quantity = 5 };
        villager.TryPickUpItem(item);
        Assert.True(villager.IsCarryingItems);

        var stockpile = new StockpileLogic(EntityId.Next()) { IsActive = true, MaxCapacity = 100 };

        var droppedItems = villager.DropAllItems();
        foreach (var dropped in droppedItems)
        {
            stockpile.Deposit(dropped.ProtoId, dropped.Quantity);
        }

        villager.CurrentActivity = null;

        Assert.Null(villager.CurrentActivity);
        Assert.Equal(VillagerJob.Builder, villager.Profession);
        Assert.False(villager.IsCarryingItems);
        Assert.Equal(5, stockpile.GetCount("sticks"));
    }

    [Fact]
    public void StockpileDropOff_StateBecomeTravellingAfterExit()
    {
        EntityBase.ResetIdCounter();
        var villager = new VillagerLogic(EntityId.Next()) { IsActive = true };
        villager.AssignJob(VillagerJob.Lumberjack);

        var item = new ItemInstance { ProtoId = "logs", Quantity = 3 };
        villager.TryPickUpItem(item);

        var stockpile = new StockpileLogic(EntityId.Next()) { IsActive = true, MaxCapacity = 100 };

        var droppedItems = villager.DropAllItems();
        foreach (var dropped in droppedItems)
        {
            stockpile.Deposit(dropped.ProtoId, dropped.Quantity);
        }

        villager.CurrentActivity = null;
        villager.PlaceOnPath(99);

        Assert.Equal(VillagerJob.Lumberjack, villager.Profession);
        Assert.Equal(VillagerState.Travelling, villager.State);
    }
}
