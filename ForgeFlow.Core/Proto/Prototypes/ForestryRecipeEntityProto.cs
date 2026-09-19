namespace ForgeFlow.Core.Proto.Prototypes;

/// <summary>
/// Proto for a forestry structure. Gathers wood from Forest biome nodes.
/// </summary>
public sealed class ForestryRecipeEntityProto : GatheringProtoBase
{
    public float WoodQualityMultiplier { get; set; } = 1.0f;
    public bool CanPlantSaplings { get; set; } = true;
    public float SaplingGrowthBonus { get; set; } = 0.2f;
}
