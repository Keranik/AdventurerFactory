using ForgeFlow.Core.Events;
using ForgeFlow.Core.Save;

namespace ForgeFlow.Core.Systems;

/// <summary>
/// Tracks aggregate game statistics by subscribing to events.
/// Pure Core — headless. Read by Presentation for the stats screen.
/// </summary>
public sealed class GameStatistics : IGameSystem, IDisposable
{
    private EventBus? _eventBus;

    public long TotalHeroesSpawned { get; private set; }
    public long TotalHeroesDied { get; private set; }
    public long TotalDungeonsAttempted { get; private set; }
    public long TotalDungeonsCleared { get; private set; }
    public long TotalFusions { get; private set; }
    public long TotalItemsCrafted { get; private set; }
    public long TotalVillagersSpawned { get; private set; }
    public long TotalVillagersTrained { get; private set; }
    public long TotalPathsBuilt { get; private set; }
    public long TotalResourcesProduced { get; private set; }
    public long TotalResearchUnlocked { get; private set; }
    public long TotalPrestigeResets { get; private set; }
    public long TotalCataclysmsSurvived { get; private set; }
    public long TotalTutorialsCompleted { get; private set; }
    public long TotalWorkersWornOut { get; private set; }
    public long TotalAbilitiesGained { get; private set; }
    public long TotalAbilitiesLost { get; private set; }
    public long TotalGoldEarned { get; private set; }
    public long TotalJobChanges { get; private set; }
    public float PlayTimeSeconds { get; private set; }

    public Dictionary<string, long> ResourcesProducedByType { get; } = new();

    public void Initialize(EventBus eventBus)
    {
        _eventBus = eventBus;
        eventBus.Subscribe<HeroSpawnedEvent>(OnHeroSpawned);
        eventBus.Subscribe<HeroDiedEvent>(OnHeroDied);
        eventBus.Subscribe<DungeonCompletedEvent>(OnDungeonCompleted);
        eventBus.Subscribe<HeroFusedEvent>(OnHeroFused);
        eventBus.Subscribe<ItemCraftedEvent>(OnItemCrafted);
        eventBus.Subscribe<VillagerSpawnedEvent>(OnVillagerSpawned);
        eventBus.Subscribe<VillagerTrainingCompleteEvent>(OnVillagerTrained);
        eventBus.Subscribe<PathBuiltEvent>(OnPathBuilt);
        eventBus.Subscribe<ResourceProducedEvent>(OnResourceProduced);
        eventBus.Subscribe<ResearchUnlockedEvent>(OnResearchUnlocked);
        eventBus.Subscribe<PrestigeResetEvent>(OnPrestigeReset);
        eventBus.Subscribe<CataclysmEvent>(OnCataclysm);
        eventBus.Subscribe<TutorialCompletedEvent>(OnTutorialCompleted);
        eventBus.Subscribe<WorkerWornOutEvent>(OnWorkerWornOut);
        eventBus.Subscribe<AbilityGainedEvent>(OnAbilityGained);
        eventBus.Subscribe<AbilityLostEvent>(OnAbilityLost);
        eventBus.Subscribe<GoldChangedEvent>(OnGoldChanged);
    }

    public void Dispose()
    {
        if (_eventBus == null) { return; }
        _eventBus.Unsubscribe<HeroSpawnedEvent>(OnHeroSpawned);
        _eventBus.Unsubscribe<HeroDiedEvent>(OnHeroDied);
        _eventBus.Unsubscribe<DungeonCompletedEvent>(OnDungeonCompleted);
        _eventBus.Unsubscribe<HeroFusedEvent>(OnHeroFused);
        _eventBus.Unsubscribe<ItemCraftedEvent>(OnItemCrafted);
        _eventBus.Unsubscribe<VillagerSpawnedEvent>(OnVillagerSpawned);
        _eventBus.Unsubscribe<VillagerTrainingCompleteEvent>(OnVillagerTrained);
        _eventBus.Unsubscribe<PathBuiltEvent>(OnPathBuilt);
        _eventBus.Unsubscribe<ResourceProducedEvent>(OnResourceProduced);
        _eventBus.Unsubscribe<ResearchUnlockedEvent>(OnResearchUnlocked);
        _eventBus.Unsubscribe<PrestigeResetEvent>(OnPrestigeReset);
        _eventBus.Unsubscribe<CataclysmEvent>(OnCataclysm);
        _eventBus.Unsubscribe<TutorialCompletedEvent>(OnTutorialCompleted);
        _eventBus.Unsubscribe<WorkerWornOutEvent>(OnWorkerWornOut);
        _eventBus.Unsubscribe<AbilityGainedEvent>(OnAbilityGained);
        _eventBus.Unsubscribe<AbilityLostEvent>(OnAbilityLost);
        _eventBus.Unsubscribe<GoldChangedEvent>(OnGoldChanged);
        _eventBus = null;
    }

    private void OnHeroSpawned(HeroSpawnedEvent _) => TotalHeroesSpawned++;
    private void OnHeroDied(HeroDiedEvent _) => TotalHeroesDied++;
    private void OnDungeonCompleted(DungeonCompletedEvent e)
    {
        TotalDungeonsAttempted++;
        if (e.Success) { TotalDungeonsCleared++; }
    }
    private void OnHeroFused(HeroFusedEvent _) => TotalFusions++;
    private void OnItemCrafted(ItemCraftedEvent _) => TotalItemsCrafted++;
    private void OnVillagerSpawned(VillagerSpawnedEvent _) => TotalVillagersSpawned++;
    private void OnVillagerTrained(VillagerTrainingCompleteEvent _) => TotalVillagersTrained++;
    private void OnPathBuilt(PathBuiltEvent _) => TotalPathsBuilt++;
    private void OnResourceProduced(ResourceProducedEvent e)
    {
        TotalResourcesProduced += e.Quantity;
        ResourcesProducedByType.TryGetValue(e.ResourceId, out var current);
        ResourcesProducedByType[e.ResourceId] = current + e.Quantity;
    }
    private void OnResearchUnlocked(ResearchUnlockedEvent _) => TotalResearchUnlocked++;
    private void OnPrestigeReset(PrestigeResetEvent _) => TotalPrestigeResets++;
    private void OnCataclysm(CataclysmEvent e) { if (!e.Started) { TotalCataclysmsSurvived++; } }
    private void OnTutorialCompleted(TutorialCompletedEvent _) => TotalTutorialsCompleted++;
    private void OnWorkerWornOut(WorkerWornOutEvent _) => TotalWorkersWornOut++;
    private void OnAbilityGained(AbilityGainedEvent _) => TotalAbilitiesGained++;
    private void OnAbilityLost(AbilityLostEvent e) => TotalAbilitiesLost += e.AbilitiesLost;
    private void OnGoldChanged(GoldChangedEvent e)
    {
        if (e.NewAmount > e.OldAmount) { TotalGoldEarned += e.NewAmount - e.OldAmount; }
    }

    public void TickPlayTime(float deltaTime)
    {
        PlayTimeSeconds += deltaTime;
    }

    public void Reset()
    {
        TotalHeroesSpawned = 0;
        TotalHeroesDied = 0;
        TotalDungeonsAttempted = 0;
        TotalDungeonsCleared = 0;
        TotalFusions = 0;
        TotalItemsCrafted = 0;
        TotalVillagersSpawned = 0;
        TotalVillagersTrained = 0;
        TotalPathsBuilt = 0;
        TotalResourcesProduced = 0;
        TotalResearchUnlocked = 0;
        TotalPrestigeResets = 0;
        TotalCataclysmsSurvived = 0;
        TotalTutorialsCompleted = 0;
        TotalWorkersWornOut = 0;
        TotalAbilitiesGained = 0;
        TotalAbilitiesLost = 0;
        TotalGoldEarned = 0;
        TotalJobChanges = 0;
        PlayTimeSeconds = 0;
        ResourcesProducedByType.Clear();
    }

    /// <summary>
    /// Restores statistics from save data. Called during game load.
    /// </summary>
    public void LoadFromSave(StatisticsSaveData data)
    {
        TotalHeroesSpawned = data.TotalHeroesSpawned;
        TotalHeroesDied = data.TotalHeroesDied;
        TotalDungeonsAttempted = data.TotalDungeonsAttempted;
        TotalDungeonsCleared = data.TotalDungeonsCleared;
        TotalFusions = data.TotalFusions;
        TotalItemsCrafted = data.TotalItemsCrafted;
        TotalVillagersSpawned = data.TotalVillagersSpawned;
        TotalVillagersTrained = data.TotalVillagersTrained;
        TotalPathsBuilt = data.TotalPathsBuilt;
        TotalResourcesProduced = data.TotalResourcesProduced;
        TotalResearchUnlocked = data.TotalResearchUnlocked;
        TotalPrestigeResets = data.TotalPrestigeResets;
        TotalCataclysmsSurvived = data.TotalCataclysmsSurvived;
        TotalTutorialsCompleted = data.TotalTutorialsCompleted;
        TotalWorkersWornOut = data.TotalWorkersWornOut;
        TotalAbilitiesGained = data.TotalAbilitiesGained;
        TotalAbilitiesLost = data.TotalAbilitiesLost;
        TotalGoldEarned = data.TotalGoldEarned;
        TotalJobChanges = data.TotalJobChanges;
        PlayTimeSeconds = data.PlayTimeSeconds;
    }
}
