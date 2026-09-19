using ForgeFlow.Core.Data;
using ForgeFlow.Core.Entities;
using ForgeFlow.Core.Events;
using ForgeFlow.Core.Proto;
using ForgeFlow.Core.Proto.Logic;
using ForgeFlow.Core.Proto.Prototypes;

namespace ForgeFlow.Core.Systems;

/// <summary>
/// Tutorial and game-phase coordinator. Subscribes to gameplay events and
/// auto-advances tutorial conditions. Also handles gold rewards for dungeon
/// completion and tutorial milestones.
/// Pure Core — no Unity dependency.
/// Gold transactions for player-initiated actions (placement, demolition) are
/// now handled by command handlers in the owning managers (StructureManager,
/// PathNodeManager, PathGateManager).
/// </summary>
public sealed class GameplayFlowSystem : IGameSystem
{
    private readonly EventBus _eventBus;
    private readonly SimulationTicker _simulation;
    private readonly TutorialSystem _tutorial;

    public GameplayFlowSystem(EventBus eventBus, SimulationTicker simulation, TutorialSystem tutorial)
    {
        _eventBus = eventBus;
        _simulation = simulation;
        _tutorial = tutorial;

        // Subscribe to events to auto-advance tutorial conditions
        _eventBus.Subscribe<HeroSpawnedEvent>(OnHeroSpawned);
        _eventBus.Subscribe<DungeonCompletedEvent>(OnDungeonCompleted);
        _eventBus.Subscribe<VillageBuildingBuiltEvent>(OnVillageBuildingBuilt);
        _eventBus.Subscribe<VillagerSpawnedEvent>(OnVillagerSpawned);
        _eventBus.Subscribe<GatheringCompleteEvent>(OnGatheringComplete);
        _eventBus.Subscribe<GearEquippedEvent>(OnGearEquipped);
        _eventBus.Subscribe<HeroFusedEvent>(OnHeroFused);
        _eventBus.Subscribe<TutorialCompletedEvent>(OnTutorialCompleted);
        _eventBus.Subscribe<VillagerEnteredBuildingEvent>(OnVillagerEnteredBuilding);
        _eventBus.Subscribe<VillagerPickedUpItemEvent>(OnVillagerPickedUpItem);
        _eventBus.Subscribe<VillagerDroppedOffItemEvent>(OnVillagerDroppedOffItem);
        _eventBus.Subscribe<ItemCraftedEvent>(OnItemCrafted);
        _eventBus.Subscribe<GatheringWorkerOutputEvent>(OnGatheringWorkerOutput);
        _eventBus.Subscribe<StockpileFilterChangedEvent>(OnStockpileFilterChanged);
        _eventBus.Subscribe<VillagerTrainingCompleteEvent>(OnVillagerTrainingComplete);
        _eventBus.Subscribe<VillagerEnteredDungeonEvent>(OnVillagerEnteredDungeon);
        _eventBus.Subscribe<FilterSplitterFilterChangedEvent>(OnFilterSplitterFilterChanged);
        _eventBus.Subscribe<CraftStationRecipeSelectedEvent>(OnCraftStationRecipeSelected);
    }

    /// <summary>
    /// Awards gold to the guild and publishes a GoldChangedEvent.
    /// Used internally for dungeon rewards and tutorial completion rewards.
    /// </summary>
    public void AwardGold(int amount, string reason)
    {
        var im = _simulation.ItemManager;

        int oldGold = im.GetStock("gold");
        im.AddStock("gold", amount);
        _eventBus.Publish(new GoldChangedEvent(oldGold, im.GetStock("gold"), reason));
    }

    /// <summary>Unsubscribes from all events. Call on shutdown.</summary>
    public void Dispose()
    {
        _eventBus.Unsubscribe<HeroSpawnedEvent>(OnHeroSpawned);
        _eventBus.Unsubscribe<DungeonCompletedEvent>(OnDungeonCompleted);
        _eventBus.Unsubscribe<VillageBuildingBuiltEvent>(OnVillageBuildingBuilt);
        _eventBus.Unsubscribe<VillagerSpawnedEvent>(OnVillagerSpawned);
        _eventBus.Unsubscribe<GatheringCompleteEvent>(OnGatheringComplete);
        _eventBus.Unsubscribe<GearEquippedEvent>(OnGearEquipped);
        _eventBus.Unsubscribe<HeroFusedEvent>(OnHeroFused);
        _eventBus.Unsubscribe<TutorialCompletedEvent>(OnTutorialCompleted);
        _eventBus.Unsubscribe<VillagerEnteredBuildingEvent>(OnVillagerEnteredBuilding);
        _eventBus.Unsubscribe<VillagerPickedUpItemEvent>(OnVillagerPickedUpItem);
        _eventBus.Unsubscribe<VillagerDroppedOffItemEvent>(OnVillagerDroppedOffItem);
        _eventBus.Unsubscribe<ItemCraftedEvent>(OnItemCrafted);
        _eventBus.Unsubscribe<GatheringWorkerOutputEvent>(OnGatheringWorkerOutput);
        _eventBus.Unsubscribe<StockpileFilterChangedEvent>(OnStockpileFilterChanged);
        _eventBus.Unsubscribe<VillagerTrainingCompleteEvent>(OnVillagerTrainingComplete);
        _eventBus.Unsubscribe<VillagerEnteredDungeonEvent>(OnVillagerEnteredDungeon);
        _eventBus.Unsubscribe<FilterSplitterFilterChangedEvent>(OnFilterSplitterFilterChanged);
        _eventBus.Unsubscribe<CraftStationRecipeSelectedEvent>(OnCraftStationRecipeSelected);
    }

    // --- Event Handlers (auto-advance tutorial) ---

    private void OnHeroSpawned(HeroSpawnedEvent e)
    {
        _tutorial.AdvanceCondition(TutorialConditionType.SpawnHero);
    }

    private void OnDungeonCompleted(DungeonCompletedEvent e)
    {
        if (e.Success)
        {
            _tutorial.AdvanceCondition(TutorialConditionType.CompleteDungeon);

            // Award gold for dungeon completion
            int reward = EconomyConfig.GetDungeonGoldReward(1);
            AwardGold(reward, "Dungeon completed");
        }
    }

    private void OnVillageBuildingBuilt(VillageBuildingBuiltEvent e)
    {
        _tutorial.AdvanceCondition(TutorialConditionType.BuildVillageBuilding, e.BuildingId);
    }

    private void OnVillagerSpawned(VillagerSpawnedEvent e)
    {
        _tutorial.AdvanceCondition(TutorialConditionType.SpawnVillager);
    }

    private void OnGatheringComplete(GatheringCompleteEvent e)
    {
        _tutorial.AdvanceCondition(TutorialConditionType.GatherResource, e.ResourceId, e.Amount);
    }

    private void OnGearEquipped(GearEquippedEvent e)
    {
        _tutorial.AdvanceCondition(TutorialConditionType.EquipHero);
    }

    private void OnHeroFused(HeroFusedEvent e)
    {
        _tutorial.AdvanceCondition(TutorialConditionType.FuseHeroes);
    }

    private void OnTutorialCompleted(TutorialCompletedEvent e)
    {
        // Award tutorial completion rewards
        foreach (var m in _tutorial.AllMissions)
        {
            if (m.ProtoId == e.MissionId && m.State == TutorialMissionState.Completed)
            {
                var im = _simulation.ItemManager;
                int oldGold = im.GetStock("gold");

                // CollectRewards adds all rewards (including gold) via ItemManager.AddStock
                _tutorial.CollectRewards(m, im);

                // Publish GoldChangedEvent for UI feedback if gold was awarded
                int newGold = im.GetStock("gold");
                if (newGold != oldGold)
                {
                    _eventBus.Publish(new GoldChangedEvent(oldGold, newGold, $"Tutorial '{e.MissionId}' completed"));
                }
                break;
            }
        }
    }

    private void OnVillagerEnteredBuilding(VillagerEnteredBuildingEvent e)
    {
        _tutorial.AdvanceCondition(TutorialConditionType.VillagerReachBuilding, e.BuildingType);

        if (e.BuildingType == "Inn")
        {
            _tutorial.AdvanceCondition(TutorialConditionType.VillagerEnterInn);
            _tutorial.AdvanceCondition(TutorialConditionType.WatchVillagerRest);
        }
    }

    private void OnVillagerPickedUpItem(VillagerPickedUpItemEvent e)
    {
        _tutorial.AdvanceCondition(TutorialConditionType.VillagerPickUpItem, e.ResourceId, e.Quantity);
        _tutorial.AdvanceCondition(TutorialConditionType.WatchVillagerGather, e.ResourceId, e.Quantity);
    }

    private void OnVillagerDroppedOffItem(VillagerDroppedOffItemEvent e)
    {
        _tutorial.AdvanceCondition(TutorialConditionType.VillagerDropOffItem, e.ResourceId);

        // WatchFullLoop completes when the specific stockpile reaches 20 sticks
        if (e.ResourceId == "sticks")
        {
            var structure = _simulation.EntityManager.GetStructure(new EntityId(e.BuildingId));
            if (structure is StockpileLogic stockpile && stockpile.GetCount("sticks") >= 20)
            {
                _tutorial.AdvanceCondition(TutorialConditionType.WatchFullLoop);
            }
        }
    }

    private void OnItemCrafted(ItemCraftedEvent e)
    {
        _tutorial.AdvanceCondition(TutorialConditionType.CraftItem, e.ItemId);
    }

    private void OnGatheringWorkerOutput(GatheringWorkerOutputEvent e)
    {
        _tutorial.AdvanceCondition(TutorialConditionType.WatchVillagerGather, e.ResourceId, e.Quantity);
    }

    private void OnStockpileFilterChanged(StockpileFilterChangedEvent e)
    {
        // When a stockpile product assignment is changed, check if it now accepts the target item
        var structure = _simulation.EntityManager.GetStructure(new EntityId(e.StockpileId));
        if (structure is StockpileLogic stockpile && stockpile.AcceptedItemId != null)
        {
            _tutorial.AdvanceCondition(TutorialConditionType.SelectStockpileProduct, stockpile.AcceptedItemId);
        }
    }

    private void OnVillagerTrainingComplete(VillagerTrainingCompleteEvent e)
    {
        _tutorial.AdvanceCondition(TutorialConditionType.VillagerTrainClass, e.TrainedClass);
    }

    private void OnVillagerEnteredDungeon(VillagerEnteredDungeonEvent e)
    {
        _tutorial.AdvanceCondition(TutorialConditionType.VillagerEnterDungeon, e.DungeonId);
    }

    private void OnFilterSplitterFilterChanged(FilterSplitterFilterChangedEvent e)
    {
        var node = _simulation.EntityManager.GetRoutingNode(new EntityId(e.SplitterId));
        if (node is FilterSplitterLogic splitter && splitter.FilteredItemId != null)
        {
            _tutorial.AdvanceCondition(TutorialConditionType.ConfigureFilterSplitter, splitter.FilteredItemId);
        }
    }

    private void OnCraftStationRecipeSelected(CraftStationRecipeSelectedEvent e)
    {
        _tutorial.AdvanceCondition(TutorialConditionType.SelectCraftStationRecipe, e.RecipeId);
    }
}
