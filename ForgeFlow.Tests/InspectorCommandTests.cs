using System.Linq;
using ForgeFlow.Core;
using ForgeFlow.Core.Commands;
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
/// Tests for the inspector-driven commands: SetFilterCommand, SetRecipeCommand,
/// and SetStockpileFilterCommand. Validates that CommandBus dispatch produces
/// identical results to the legacy direct StructureManager API calls.
/// </summary>
public class InspectorCommandTests
{
    private static (GameBootstrapper boot, SimulationTicker sim) CreateNewGame(
        Difficulty difficulty = Difficulty.Normal)
    {
        EntityBase.ResetIdCounter();
        var boot = new GameBootstrapper();
        boot.Bootstrap();
        var settings = new NewGameSettings
        {
            GameName = "InspCmdTest",
            Difficulty = difficulty,
            Seed = 42,
            GuildName = "InspCmdGuild"
        };
        boot.ApplyNewGameSettings(settings);
        return (boot, boot.Services.Get<SimulationTicker>());
    }

    // ── SetFilterCommand ─────────────────────────────────────────────

    [Fact]
    public void SetFilter_Success_SetsFilterOnSplitter()
    {
        var (boot, sim) = CreateNewGame();

        var splitter = new FilterSplitterLogic(EntityId.Next());
        splitter.InitializeFromProto(new FilterSplitterProto { Id = "filter_splitter_basic" });
        sim.StructureManager.AddProtoStructure(splitter, new GridPosRPG(5, 5));

        var result = boot.Services.Get<CommandBus>().Dispatch(new SetFilterCommand(splitter.Id, "sticks"));

        Assert.True(result.Success);
        Assert.Equal("sticks", splitter.FilteredItemId);
    }

    [Fact]
    public void SetFilter_ClearsFilter_WhenItemIdNull()
    {
        var (boot, sim) = CreateNewGame();

        var splitter = new FilterSplitterLogic(EntityId.Next());
        splitter.InitializeFromProto(new FilterSplitterProto { Id = "filter_splitter_basic" });
        sim.StructureManager.AddProtoStructure(splitter, new GridPosRPG(5, 5));
        splitter.SetFilteredItem("sticks");

        var result = boot.Services.Get<CommandBus>().Dispatch(new SetFilterCommand(splitter.Id, null));

        Assert.True(result.Success);
        Assert.Null(splitter.FilteredItemId);
    }

    [Fact]
    public void SetFilter_Fails_ForInvalidSplitterId()
    {
        var (boot, _) = CreateNewGame();

        var result = boot.Services.Get<CommandBus>().Dispatch(new SetFilterCommand(999999, "sticks"));

        Assert.False(result.Success);
    }

    [Fact]
    public void SetFilter_Parity_MatchesDirectApiCall()
    {
        var (boot, sim) = CreateNewGame();

        var splitter1 = new FilterSplitterLogic(EntityId.Next());
        splitter1.InitializeFromProto(new FilterSplitterProto { Id = "filter_splitter_basic" });
        sim.StructureManager.AddProtoStructure(splitter1, new GridPosRPG(5, 5));

        var splitter2 = new FilterSplitterLogic(EntityId.Next());
        splitter2.InitializeFromProto(new FilterSplitterProto { Id = "filter_splitter_basic" });
        sim.StructureManager.AddProtoStructure(splitter2, new GridPosRPG(6, 6));

        // Command path
        boot.Services.Get<CommandBus>().Dispatch(new SetFilterCommand(splitter1.Id, "ore"));

        // Direct API path
        sim.StructureManager.SetFilterSplitterItem(splitter2.Id, "ore");

        Assert.Equal(splitter1.FilteredItemId, splitter2.FilteredItemId);
    }

    // ── SetRecipeCommand ─────────────────────────────────────────────

    [Fact]
    public void SetRecipe_Success_SetsRecipeOnCraftStation()
    {
        var (boot, sim) = CreateNewGame();

        var station = new CraftStationLogic(EntityId.Next());
        station.InitializeFromProto(new CraftStationProto { Id = "craft_station_basic" });
        sim.StructureManager.AddProtoStructure(station, new GridPosRPG(5, 5));

        // Use a recipe that is registered in the default data
        var recipes = boot.Services.Get<RecipeRegistry>().GetAll().ToList();
        if (recipes.Count == 0) { return; } // Skip if no recipes registered

        var recipe = recipes[0];
        var result = boot.Services.Get<CommandBus>().Dispatch(new SetRecipeCommand(station.Id, recipe.Id));

        Assert.True(result.Success);
        Assert.Equal(recipe.Id, station.ActiveRecipeId);
    }

    [Fact]
    public void SetRecipe_ClearsRecipe_WhenRecipeIdNull()
    {
        var (boot, sim) = CreateNewGame();

        var station = new CraftStationLogic(EntityId.Next());
        station.InitializeFromProto(new CraftStationProto { Id = "craft_station_basic" });
        sim.StructureManager.AddProtoStructure(station, new GridPosRPG(5, 5));

        var recipes = boot.Services.Get<RecipeRegistry>().GetAll().ToList();
        if (recipes.Count == 0) { return; }

        // Set first, then clear
        boot.Services.Get<CommandBus>().Dispatch(new SetRecipeCommand(station.Id, recipes[0].Id));
        var result = boot.Services.Get<CommandBus>().Dispatch(new SetRecipeCommand(station.Id, null));

        Assert.True(result.Success);
        Assert.Null(station.ActiveRecipeId);
    }

    [Fact]
    public void SetRecipe_Fails_ForInvalidStationId()
    {
        var (boot, _) = CreateNewGame();

        var result = boot.Services.Get<CommandBus>().Dispatch(new SetRecipeCommand(999999, "some_recipe"));

        Assert.False(result.Success);
    }

    [Fact]
    public void SetRecipe_Fails_ForInvalidRecipeId()
    {
        var (boot, sim) = CreateNewGame();

        var station = new CraftStationLogic(EntityId.Next());
        station.InitializeFromProto(new CraftStationProto { Id = "craft_station_basic" });
        sim.StructureManager.AddProtoStructure(station, new GridPosRPG(5, 5));

        var result = boot.Services.Get<CommandBus>().Dispatch(new SetRecipeCommand(station.Id, "nonexistent_recipe"));

        Assert.False(result.Success);
    }

    [Fact]
    public void SetRecipe_PublishesCraftStationRecipeSelectedEvent()
    {
        var (boot, sim) = CreateNewGame();

        var station = new CraftStationLogic(EntityId.Next());
        station.InitializeFromProto(new CraftStationProto { Id = "craft_station_basic" });
        sim.StructureManager.AddProtoStructure(station, new GridPosRPG(5, 5));

        var recipes = boot.Services.Get<RecipeRegistry>().GetAll().ToList();
        if (recipes.Count == 0) { return; }

        CraftStationRecipeSelectedEvent? received = null;
        boot.Services.Get<EventBus>().Subscribe<CraftStationRecipeSelectedEvent>(e => received = e);

        boot.Services.Get<CommandBus>().Dispatch(new SetRecipeCommand(station.Id, recipes[0].Id));

        Assert.NotNull(received);
        Assert.Equal(recipes[0].Id, received.Value.RecipeId);
    }

    // ── SetStockpileFilterCommand ────────────────────────────────────

    [Fact]
    public void SetStockpileFilter_Success_SetsAcceptedItem()
    {
        var (boot, sim) = CreateNewGame();

        var stockpile = new StockpileLogic(EntityId.Next());
        sim.StructureManager.AddProtoStructure(stockpile, new GridPosRPG(5, 5));

        var result = boot.Services.Get<CommandBus>().Dispatch(new SetStockpileFilterCommand(stockpile.Id, "sticks"));

        Assert.True(result.Success);
        Assert.Equal("sticks", stockpile.AcceptedItemId);
    }

    [Fact]
    public void SetStockpileFilter_ClearsFilter_WhenItemIdNull()
    {
        var (boot, sim) = CreateNewGame();

        var stockpile = new StockpileLogic(EntityId.Next());
        sim.StructureManager.AddProtoStructure(stockpile, new GridPosRPG(5, 5));
        stockpile.SetAcceptedItem("sticks");

        var result = boot.Services.Get<CommandBus>().Dispatch(new SetStockpileFilterCommand(stockpile.Id, null));

        Assert.True(result.Success);
        Assert.Null(stockpile.AcceptedItemId);
    }

    [Fact]
    public void SetStockpileFilter_Fails_ForInvalidId()
    {
        var (boot, _) = CreateNewGame();

        var result = boot.Services.Get<CommandBus>().Dispatch(new SetStockpileFilterCommand(999999, "sticks"));

        Assert.False(result.Success);
    }

    [Fact]
    public void SetStockpileFilter_Parity_MatchesDirectApiCall()
    {
        var (boot, sim) = CreateNewGame();

        var stockpile1 = new StockpileLogic(EntityId.Next());
        sim.StructureManager.AddProtoStructure(stockpile1, new GridPosRPG(5, 5));

        var stockpile2 = new StockpileLogic(EntityId.Next());
        sim.StructureManager.AddProtoStructure(stockpile2, new GridPosRPG(6, 6));

        // Command path
        boot.Services.Get<CommandBus>().Dispatch(new SetStockpileFilterCommand(stockpile1.Id, "ore"));

        // Direct API path
        sim.StructureManager.SetStockpileProduct(stockpile2.Id, "ore");

        Assert.Equal(stockpile1.AcceptedItemId, stockpile2.AcceptedItemId);
    }

    [Fact]
    public void SetStockpileFilter_Fails_ForNonStockpileEntity()
    {
        var (boot, sim) = CreateNewGame();

        var splitter = new FilterSplitterLogic(EntityId.Next());
        splitter.InitializeFromProto(new FilterSplitterProto { Id = "filter_splitter_basic" });
        sim.StructureManager.AddProtoStructure(splitter, new GridPosRPG(5, 5));

        var result = boot.Services.Get<CommandBus>().Dispatch(new SetStockpileFilterCommand(splitter.Id, "sticks"));

        Assert.False(result.Success);
    }
}
