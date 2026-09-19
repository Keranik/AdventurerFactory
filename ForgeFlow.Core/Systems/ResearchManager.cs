using ForgeFlow.Core.Events;

namespace ForgeFlow.Core.Systems;

/// <summary>
/// Single source of truth for research tier state: current tier, unlocked tiers,
/// and tier unlock operations. Publishes ResearchUnlockedEvent on advancement.
/// Extracted from SimulationTicker to give research a dedicated owner.
/// </summary>
public sealed class ResearchManager : IGameSystem
{
    private readonly EventBus _eventBus;

    /// <summary>The highest research tier currently unlocked.</summary>
    public int CurrentTier { get; private set; } = 1;

    /// <summary>Set of all unlocked research tiers.</summary>
    public HashSet<int> UnlockedTiers { get; } = new() { 1 };

    public ResearchManager(EventBus eventBus)
    {
        _eventBus = eventBus;
    }

    /// <summary>Returns true if the given tier has been unlocked.</summary>
    public bool IsTierUnlocked(int tier) => UnlockedTiers.Contains(tier);

    /// <summary>Returns true if the next sequential tier can be unlocked.</summary>
    public bool CanUnlockNext() => CurrentTier < 10;

    /// <summary>
    /// Unlocks a research tier if the transition is valid.
    /// Returns true if the tier was successfully unlocked.
    /// </summary>
    public bool UnlockTier(int tier)
    {
        if (tier <= 0 || tier > 10) { return false; }
        if (UnlockedTiers.Contains(tier)) { return false; }
        if (tier > CurrentTier + 1) { return false; }

        UnlockedTiers.Add(tier);
        CurrentTier = Math.Max(CurrentTier, tier);
        _eventBus.Publish(new ResearchUnlockedEvent(tier));
        return true;
    }

    /// <summary>
    /// Resets research state (used during prestige reset).
    /// Wipes all unlocked tiers back to tier 1.
    /// </summary>
    public void Reset()
    {
        UnlockedTiers.Clear();
        UnlockedTiers.Add(1);
        CurrentTier = 1;
    }

    /// <summary>
    /// Restores research state from save data. Does not publish events.
    /// </summary>
    public void LoadFromSave(int currentTier, IEnumerable<int> unlockedTiers)
    {
        UnlockedTiers.Clear();
        foreach (var tier in unlockedTiers)
        {
            UnlockedTiers.Add(tier);
        }
        CurrentTier = currentTier;
    }
}
