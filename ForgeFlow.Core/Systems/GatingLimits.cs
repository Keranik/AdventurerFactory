namespace ForgeFlow.Core.Systems;

/// <summary>
/// Gating configuration that limits spawner and building counts per research tier.
/// Prevents the player from spamming 1000 village spawners right off the bat.
/// All limits are expandable via research progression.
/// </summary>
public sealed class GatingLimits : IGameSystem
{
    /// <summary>Max village spawners allowed per research tier. Key = tier, Value = max count.</summary>
    public Dictionary<int, int> MaxSpawnersPerTier { get; } = new()
    {
        { 1, 1 },
        { 2, 2 },
        { 3, 3 },
        { 4, 5 },
        { 5, 8 },
        { 6, 12 },
        { 7, 18 },
        { 8, 25 },
        { 9, 35 },
        { 10, 50 }
    };

    /// <summary>Max total buildings (all types) allowed per research tier.</summary>
    public Dictionary<int, int> MaxBuildingsPerTier { get; } = new()
    {
        { 1, 3 },
        { 2, 6 },
        { 3, 10 },
        { 4, 16 },
        { 5, 24 },
        { 6, 35 },
        { 7, 50 },
        { 8, 70 },
        { 9, 100 },
        { 10, 150 }
    };

    /// <summary>Max path segments allowed per research tier.</summary>
    public Dictionary<int, int> MaxPathsPerTier { get; } = new()
    {
        { 1, 30 },
        { 2, 60 },
        { 3, 100 },
        { 4, 160 },
        { 5, 250 },
        { 6, 400 },
        { 7, 600 },
        { 8, 900 },
        { 9, 1200 },
        { 10, 2000 }
    };

    /// <summary>Max villagers allowed per research tier.</summary>
    public Dictionary<int, int> MaxVillagersPerTier { get; } = new()
    {
        { 1, 5 },
        { 2, 12 },
        { 3, 20 },
        { 4, 35 },
        { 5, 55 },
        { 6, 80 },
        { 7, 120 },
        { 8, 180 },
        { 9, 260 },
        { 10, 400 }
    };

    /// <summary>Max heroes allowed per research tier.</summary>
    public Dictionary<int, int> MaxHeroesPerTier { get; } = new()
    {
        { 1, 2 },
        { 2, 5 },
        { 3, 8 },
        { 4, 14 },
        { 5, 22 },
        { 6, 35 },
        { 7, 50 },
        { 8, 75 },
        { 9, 110 },
        { 10, 160 }
    };

    /// <summary>Hard system cap multiplier applied to the tier max as a crash guard.</summary>
    public int HardCapMultiplier { get; set; } = 2;

    public int GetMaxSpawners(int currentTier)
    {
        return GetMaxForTier(MaxSpawnersPerTier, currentTier, 1);
    }

    public int GetMaxBuildings(int currentTier)
    {
        return GetMaxForTier(MaxBuildingsPerTier, currentTier, 3);
    }

    public int GetMaxPaths(int currentTier)
    {
        return GetMaxForTier(MaxPathsPerTier, currentTier, 30);
    }

    public int GetMaxVillagers(int currentTier)
    {
        return GetMaxForTier(MaxVillagersPerTier, currentTier, 5);
    }

    public int GetMaxHeroes(int currentTier)
    {
        return GetMaxForTier(MaxHeroesPerTier, currentTier, 2);
    }

    /// <summary>
    /// Looks up the max value for the given tier using TryGetValue.
    /// Falls back to the highest tier at or below <paramref name="currentTier"/>.
    /// </summary>
    private static int GetMaxForTier(Dictionary<int, int> limits, int currentTier, int defaultValue)
    {
        if (limits.TryGetValue(currentTier, out int exact))
        {
            return exact;
        }

        // Fall back to the highest tier at or below currentTier
        int best = defaultValue;
        foreach (var kvp in limits)
        {
            if (kvp.Key <= currentTier && kvp.Value > best)
            {
                best = kvp.Value;
            }
        }
        return best;
    }

    }
