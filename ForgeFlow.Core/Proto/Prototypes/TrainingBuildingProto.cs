namespace ForgeFlow.Core.Proto.Prototypes;

/// <summary>
/// Proto for a training building (school). JSON-driven.
/// Cleric school, warrior school, mage academy, etc.
/// Villagers visit these buildings to earn a VillagerClass.
/// </summary>
public sealed class TrainingBuildingProto : ActivityProtoBase
{
    public VillagerClass OutputClass { get; set; } = VillagerClass.Warrior;
    public float TrainingDuration { get; set; } = 15.0f;
    public int MaxTrainees { get; set; } = 2;
    public int RequiredTier { get; set; } = 1;
    public Dictionary<string, int> TrainingCost { get; set; } = new();
}
