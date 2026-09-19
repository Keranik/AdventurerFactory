namespace ForgeFlow.Core.Proto.Prototypes;

/// <summary>Villager job type for the village loop.</summary>
public enum VillagerJob
{
    Idle,
    Lumberjack,
    Miner,
    Farmer,
    Builder,
    Guard,
    Scholar,
    Merchant
}

/// <summary>Villager state in the simulation.</summary>
public enum VillagerState
{
    Idle,
    Working,
    Travelling,
    Resting,
    Injured,
    Training,
    EnteringBuilding,
    LeavingBuilding,
    InDungeon,
    Dead
}

/// <summary>
/// Villager class earned by attending a training building (school).
/// Provides permanent bonuses to specific job types.
/// </summary>
public enum VillagerClass
{
    Untrained,
    Warrior,
    Cleric,
    Mage,
    Ranger,
    Paladin,
    Artisan,
    Thief,
    Rogue
}

/// <summary>
/// Proto definition for a villager entity. JSON-driven.
/// </summary>
public sealed class VillagerProto : ProtoBase
{
    public VillagerJob DefaultJob { get; set; } = VillagerJob.Idle;
    public float BaseWorkRate { get; set; } = 1.0f;
    public float BaseMovementSpeed { get; set; } = 2.0f;
    public float BaseStamina { get; set; } = 100f;
    public Dictionary<string, float> JobEfficiencies { get; set; } = new();
}
