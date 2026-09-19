namespace ForgeFlow.Core.Proto.Prototypes;

/// <summary>
/// Proto definition for a resource node (tree, ore vein, crystal deposit, etc.).
/// Loaded from JSON. Placed onto TerrainCells.
/// </summary>
public sealed class ResourceNodeProto : ProtoBase
{
    public string ResourceId { get; set; } = string.Empty;
    public BiomeType PreferredBiome { get; set; } = BiomeType.Plains;
    public int MaxYield { get; set; } = 100;
    public float HarvestRate { get; set; } = 1.0f;
    public float RegrowthRate { get; set; } = 0.1f;
    public bool IsRenewable { get; set; } = true;
    public Dictionary<string, float> BonusByBiome { get; set; } = new();
}
