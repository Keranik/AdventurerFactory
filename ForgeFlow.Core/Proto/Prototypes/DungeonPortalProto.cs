namespace ForgeFlow.Core.Proto.Prototypes;

/// <summary>
/// Proto for a dungeon portal. Defines which dungeon villagers enter,
/// the base survival chance, gold reward, and run duration.
/// Actual dungeon definitions (tiers, loot tables, themes) are in DungeonDefinition JSON.
/// </summary>
public sealed class DungeonPortalProto : ActivityProtoBase
{
    public string DefaultDungeonId { get; set; } = "goblin_caves";

    /// <summary>Base survival chance (0.0–1.0). ~10% means ~90% lethality.</summary>
    public float BaseSurvivalChance { get; set; } = 0.1f;

    /// <summary>Gold awarded to the guild when a villager survives.</summary>
    public int BaseGoldReward { get; set; } = 50;

    /// <summary>Duration in seconds for one dungeon run.</summary>
    public float RunDuration { get; set; } = 15.0f;
}
