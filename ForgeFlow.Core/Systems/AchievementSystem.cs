using ForgeFlow.Core.Events;
using ForgeFlow.Core.Save;

namespace ForgeFlow.Core.Systems;

/// <summary>
/// Defines a single achievement that can be tracked and unlocked.
/// </summary>
public sealed class AchievementDefinition
{
    public string Id { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string IconId { get; set; } = string.Empty;
    public bool IsHidden { get; set; }
}

/// <summary>
/// Runtime state for a tracked achievement.
/// </summary>
public sealed class AchievementProgress
{
    public string DefinitionId { get; set; } = string.Empty;
    public bool IsUnlocked { get; set; }
    public long CurrentValue { get; set; }
    public long TargetValue { get; set; } = 1;
    public float Progress => TargetValue > 0 ? (float)CurrentValue / TargetValue : 0f;
}

/// <summary>
/// Achievement registry and tracking system. Subscribes to game events
/// and unlocks achievements when thresholds are met.
/// Pure Core — no Unity dependency.
/// </summary>
public sealed class AchievementSystem : IGameSystem, IDisposable
{
    private readonly EventBus _eventBus;
    private readonly Dictionary<string, AchievementDefinition> _definitions = new();
    private readonly Dictionary<string, AchievementProgress> _progress = new();
    private bool _initialized;

    public IReadOnlyDictionary<string, AchievementDefinition> Definitions => _definitions;
    public IReadOnlyDictionary<string, AchievementProgress> Progress => _progress;
    public int UnlockedCount
    {
        get
        {
            int count = 0;
            foreach (var p in _progress.Values)
            {
                if (p.IsUnlocked) { count++; }
            }
            return count;
        }
    }
    public int TotalCount => _definitions.Count;

    public AchievementSystem(EventBus eventBus)
    {
        _eventBus = eventBus;
    }

    public void RegisterDefaults()
    {
        Register(new AchievementDefinition { Id = "first_hero", DisplayName = "First Steps", Description = "Spawn your first hero." });
        Register(new AchievementDefinition { Id = "ten_heroes", DisplayName = "Assembly Line", Description = "Spawn 10 heroes." });
        Register(new AchievementDefinition { Id = "hundred_heroes", DisplayName = "Hero Factory", Description = "Spawn 100 heroes." });
        Register(new AchievementDefinition { Id = "first_dungeon", DisplayName = "Into the Depths", Description = "Clear a dungeon." });
        Register(new AchievementDefinition { Id = "ten_dungeons", DisplayName = "Dungeon Crawler", Description = "Clear 10 dungeons." });
        Register(new AchievementDefinition { Id = "first_fusion", DisplayName = "Fusion Novice", Description = "Fuse two heroes." });
        Register(new AchievementDefinition { Id = "first_villager", DisplayName = "Village Founder", Description = "Spawn your first villager." });
        Register(new AchievementDefinition { Id = "train_villager", DisplayName = "Scholar", Description = "Train a villager at a school." });
        Register(new AchievementDefinition { Id = "first_prestige", DisplayName = "New Beginning", Description = "Perform your first prestige reset." });
        Register(new AchievementDefinition { Id = "research_tier5", DisplayName = "Advanced Research", Description = "Reach research tier 5." });
        Register(new AchievementDefinition { Id = "research_tier10", DisplayName = "Pinnacle", Description = "Reach research tier 10." });
        Register(new AchievementDefinition { Id = "survive_cataclysm", DisplayName = "Survivor", Description = "Survive a cataclysm event." });
        Register(new AchievementDefinition { Id = "all_tutorials", DisplayName = "Graduate", Description = "Complete all tutorial missions." });
        Register(new AchievementDefinition { Id = "victory", DisplayName = "Champion", Description = "Achieve victory.", IsHidden = true });

        // Set target values for multi-step achievements
        SetTarget("ten_heroes", 10);
        SetTarget("hundred_heroes", 100);
        SetTarget("ten_dungeons", 10);
    }

    public void Register(AchievementDefinition definition)
    {
        _definitions[definition.Id] = definition;
        if (!_progress.ContainsKey(definition.Id))
        {
            _progress[definition.Id] = new AchievementProgress
            {
                DefinitionId = definition.Id,
                TargetValue = 1
            };
        }
    }

    public void SetTarget(string achievementId, long target)
    {
        if (_progress.TryGetValue(achievementId, out var progress))
        {
            progress.TargetValue = target;
        }
    }

    /// <summary>Wires event subscriptions. Call after all definitions are registered.</summary>
    public void Initialize()
    {
        _initialized = true;
        _eventBus.Subscribe<HeroSpawnedEvent>(OnHeroSpawned);
        _eventBus.Subscribe<DungeonCompletedEvent>(OnDungeonCompleted);
        _eventBus.Subscribe<HeroFusedEvent>(OnHeroFused);
        _eventBus.Subscribe<VillagerSpawnedEvent>(OnVillagerSpawned);
        _eventBus.Subscribe<VillagerTrainingCompleteEvent>(OnVillagerTrained);
        _eventBus.Subscribe<PrestigeResetEvent>(OnPrestigeReset);
        _eventBus.Subscribe<ResearchUnlockedEvent>(OnResearchUnlocked);
        _eventBus.Subscribe<CataclysmEvent>(OnCataclysm);
        _eventBus.Subscribe<TutorialCompletedEvent>(OnTutorialCompleted);
        _eventBus.Subscribe<GameStateChangedEvent>(OnGameStateChanged);
    }

    public void Dispose()
    {
        if (!_initialized) { return; }
        _eventBus.Unsubscribe<HeroSpawnedEvent>(OnHeroSpawned);
        _eventBus.Unsubscribe<DungeonCompletedEvent>(OnDungeonCompleted);
        _eventBus.Unsubscribe<HeroFusedEvent>(OnHeroFused);
        _eventBus.Unsubscribe<VillagerSpawnedEvent>(OnVillagerSpawned);
        _eventBus.Unsubscribe<VillagerTrainingCompleteEvent>(OnVillagerTrained);
        _eventBus.Unsubscribe<PrestigeResetEvent>(OnPrestigeReset);
        _eventBus.Unsubscribe<ResearchUnlockedEvent>(OnResearchUnlocked);
        _eventBus.Unsubscribe<CataclysmEvent>(OnCataclysm);
        _eventBus.Unsubscribe<TutorialCompletedEvent>(OnTutorialCompleted);
        _eventBus.Unsubscribe<GameStateChangedEvent>(OnGameStateChanged);
        _initialized = false;
    }

    private void OnHeroSpawned(HeroSpawnedEvent _)
    {
        Advance("first_hero", 1);
        Advance("ten_heroes", 1);
        Advance("hundred_heroes", 1);
    }
    private void OnDungeonCompleted(DungeonCompletedEvent e)
    {
        if (e.Success)
        {
            Advance("first_dungeon", 1);
            Advance("ten_dungeons", 1);
        }
    }
    private void OnHeroFused(HeroFusedEvent _) => Advance("first_fusion", 1);
    private void OnVillagerSpawned(VillagerSpawnedEvent _) => Advance("first_villager", 1);
    private void OnVillagerTrained(VillagerTrainingCompleteEvent _) => Advance("train_villager", 1);
    private void OnPrestigeReset(PrestigeResetEvent _) => Advance("first_prestige", 1);
    private void OnResearchUnlocked(ResearchUnlockedEvent e)
    {
        if (e.Tier >= 5) { Advance("research_tier5", 1); }
        if (e.Tier >= 10) { Advance("research_tier10", 1); }
    }
    private void OnCataclysm(CataclysmEvent e) { if (!e.Started) { Advance("survive_cataclysm", 1); } }
    private void OnTutorialCompleted(TutorialCompletedEvent _) { } // tracked separately via CheckAllTutorials
    private void OnGameStateChanged(GameStateChangedEvent e)
    {
        if (e.NewState == Entities.GameState.Victory)
        {
            Advance("victory", 1);
        }
    }

    /// <summary>Check if all tutorials are complete and unlock achievement.</summary>
    public void CheckAllTutorials(bool allComplete)
    {
        if (allComplete) Advance("all_tutorials", 1);
    }

    /// <summary>Advances an achievement's progress and unlocks if threshold met.</summary>
    public bool Advance(string achievementId, long amount)
    {
        if (!_progress.TryGetValue(achievementId, out var progress)) return false;
        if (progress.IsUnlocked) return false;

        progress.CurrentValue += amount;
        if (progress.CurrentValue >= progress.TargetValue)
        {
            progress.IsUnlocked = true;
            if (_definitions.TryGetValue(achievementId, out var def))
            {
                _eventBus.Publish(new AchievementUnlockedEvent(achievementId, def.DisplayName));
            }
            return true;
        }
        return false;
    }

    public bool IsUnlocked(string achievementId)
    {
        return _progress.TryGetValue(achievementId, out var p) && p.IsUnlocked;
    }

    public AchievementProgress? GetProgress(string achievementId)
    {
        return _progress.TryGetValue(achievementId, out var p) ? p : null;
    }

    /// <summary>
    /// Restores achievement progress from save data. Call after RegisterDefaults().
    /// </summary>
    public void LoadFromSave(List<AchievementProgressSaveData> savedProgress)
    {
        foreach (var saved in savedProgress)
        {
            if (_progress.TryGetValue(saved.DefinitionId, out var progress))
            {
                progress.IsUnlocked = saved.IsUnlocked;
                progress.CurrentValue = saved.CurrentValue;
                progress.TargetValue = saved.TargetValue;
            }
        }
    }
}
