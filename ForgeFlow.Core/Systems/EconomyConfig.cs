namespace ForgeFlow.Core.Systems;

/// <summary>
/// Single source of truth for all gold costs and economic values in the game.
/// Structure placement costs, path segment costs, tier-scaled building costs,
/// and dungeon gold rewards are all defined here.
/// Referenced by GameplayFlowSystem, GatingLimits, HotbarSystem, and Presentation.
/// </summary>
public static class EconomyConfig
{
    // ── Structure Placement Costs ─────────────────────────────────────

    /// <summary>Gold cost for placing each structure category (keyed by category name).</summary>
    public static readonly Dictionary<string, int> StructureCosts = new()
    {
        { "Spawner", 25 },
        { "Forge", 40 },
        { "DungeonPortal", 60 },
        { "FusionAltar", 80 },
        { "AppearanceWorkshop", 50 },
        { "Smelter", 35 },
        { "MiningNode", 30 },
        { "Forestry", 30 },
        { "VillageSpawner", 40 },
        { "TrainingBuilding", 45 },
        { "Inn", 20 },
        { "CraftStation", 35 },
        { "Stockpile", 15 },
        { "PathGate", 10 },
        { "FilterSplitter", 20 },
    };

    /// <summary>Gold cost per path segment (flat).</summary>
    public const int PathSegmentCost = 2;

    /// <summary>Default cost for unknown structure types.</summary>
    public const int DefaultStructureCost = 30;

    /// <summary>Gets the gold cost for an entity. Returns DefaultStructureCost if unknown.</summary>
    public static int GetStructureCost(string category)
    {
        return StructureCosts.TryGetValue(category, out var cost) ? cost : DefaultStructureCost;
    }

    // ── Tier-Scaled Building Costs ──────────────────────────────────

    /// <summary>
    /// Gets the gold cost for placing a building at a given research tier.
    /// Higher tiers cost exponentially more to prevent speedrunning.
    /// </summary>
    public static int GetBuildingGoldCost(int tier) => tier switch
    {
        1 => 25,
        2 => 75,
        3 => 200,
        4 => 500,
        5 => 1200,
        6 => 2500,
        7 => 5000,
        8 => 10000,
        9 => 20000,
        10 => 50000,
        _ => tier > 10 ? 50000 + (tier - 10) * 25000 : 25
    };

    // ── Tier-Scaled Path Costs ──────────────────────────────────────

    /// <summary>
    /// Gets the gold cost for placing a path segment at a given research tier.
    /// </summary>
    public static int GetPathGoldCost(int tier) => tier switch
    {
        1 => 2,
        2 => 3,
        3 => 5,
        4 => 8,
        _ => 5 + tier * 2
    };

    // ── Dungeon Rewards ─────────────────────────────────────────────

    /// <summary>
    /// Gets the gold reward for completing a dungeon at a given tier.
    /// </summary>
    public static int GetDungeonGoldReward(int dungeonTier) => dungeonTier switch
    {
        1 => 10,
        2 => 25,
        3 => 50,
        4 => 100,
        5 => 200,
        6 => 400,
        7 => 800,
        8 => 1500,
        9 => 3000,
        10 => 6000,
        _ => dungeonTier > 10 ? 6000 + (dungeonTier - 10) * 3000 : 10
    };
}
