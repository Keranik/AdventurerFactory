namespace ForgeFlow.Core.Proto.Prototypes;

/// <summary>
/// Proto for a village spawner that produces villagers on a timer.
/// </summary>
public sealed class VillageSpawnerProto : ActivityProtoBase
{
    public float SpawnInterval { get; set; } = 10.0f;
    public int MaxVillagers { get; set; } = 5;
    public string DefaultVillagerProtoId { get; set; } = "villager_basic";
    public List<string> SpawnableProtoIds { get; set; } = new();

    /// <summary>Duration in seconds for a full rest at home. Lower = faster than Inn.</summary>
    public float HomeRestDuration { get; set; } = 3.0f;
}
