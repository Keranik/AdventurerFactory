namespace ForgeFlow.Core.Proto.Prototypes;

/// <summary>
/// Proto for any gathering structure (forestry, mining, etc.).
/// Specifies what resource it gathers, from which biome, and at what rate.
/// </summary>
public class GatheringProtoBase : RecipeProtoBase
{
    public string TargetResourceId { get; set; } = string.Empty;
    public BiomeType RequiredBiome { get; set; } = BiomeType.Plains;
    public int GatherAmountPerCycle { get; set; } = 1;
    public float GatherInterval { get; set; } = 3.0f;
    public float BiomeBonusMultiplier { get; set; } = 1.0f;
    public int MaxWorkerCapacity { get; set; } = 5;
    public int StaminaCostPerCycle { get; set; } = 10;

    /// <summary>
    /// Tool-dependent output table. Key = tool proto ID (empty string = bare hands), Value = resource ID produced.
    /// If not set or tool not found, falls back to TargetResourceId.
    /// </summary>
    public Dictionary<string, string> ToolOutputMap { get; set; } = new();
}
