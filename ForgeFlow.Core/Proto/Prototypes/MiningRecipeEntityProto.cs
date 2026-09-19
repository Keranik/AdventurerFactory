namespace ForgeFlow.Core.Proto.Prototypes;

/// <summary>
/// Proto for a mining structure. Extracts ore from Mountain/Hills biome nodes.
/// </summary>
public sealed class MiningRecipeEntityProto : GatheringProtoBase
{
    public float OreRichnessMultiplier { get; set; } = 1.0f;
    public bool CanDeepMine { get; set; }
    public int DeepMineBonus { get; set; } = 2;
}
