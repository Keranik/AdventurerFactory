using ForgeFlow.Core.Entities;

namespace ForgeFlow.Core.Data.Definitions;

public sealed class DungeonDefinition
{
    public string Id { get; set; } = string.Empty;
    public int Tier { get; set; }
    public DungeonTheme Theme { get; set; }
    public float Penalty { get; set; }
    public float RandomEventModifierRange { get; set; }
    public List<string> PossibleDropItemIds { get; set; } = new();
    public int RecommendedHeroLevel { get; set; }

    /// <summary>Base survival chance (0.0–1.0). Overrides the portal proto value when present.</summary>
    public float BaseSurvivalChance { get; set; } = 0.1f;

    /// <summary>Gold awarded to the guild when a villager survives this dungeon.</summary>
    public int BaseGoldReward { get; set; } = 50;

    /// <summary>Duration in seconds for one dungeon run in this dungeon.</summary>
    public float RunDuration { get; set; } = 15.0f;
}
