using ForgeFlow.Core.Entities;
using ForgeFlow.Core.Proto.Prototypes;

namespace ForgeFlow.Core.Proto.Logic;

/// <summary>
/// Mining structure logic. Extracts ore, stone, and minerals from mountain resource nodes.
/// Deep mining unlocks bonus yields at higher tiers.
/// </summary>
public sealed class MiningRecipeEntity : GatheringLogicBase
{
    public float OreRichnessMultiplier { get; set; } = 1.0f;
    public bool CanDeepMine { get; set; }
    public int DeepMineBonus { get; set; } = 2;

    public MiningRecipeEntity(EntityId id) : base(id)
    {
        RequiredBiome = BiomeType.Mountain;
        TargetResourceId = "stones";
        GatherInterval = 5.0f;
        GatherAmountPerCycle = 1;

        // Tool-dependent output: bare hands = stones, pickaxe = ore
        ToolOutputMap[""] = "stones";
        ToolOutputMap["pickaxe"] = "ore";
        ToolOutputMap["steel_pickaxe"] = "ore";
    }

    public override void InitializeFromProto(StructureProtoBase proto)
    {
        base.InitializeFromProto(proto);
        if (proto is MiningRecipeEntityProto mp)
        {
            OreRichnessMultiplier = mp.OreRichnessMultiplier;
            CanDeepMine = mp.CanDeepMine;
            DeepMineBonus = mp.DeepMineBonus;
        }
    }

    protected override int CalculateGatherAmount()
    {
        int amount = (int)(GatherAmountPerCycle * OreRichnessMultiplier * BiomeBonusMultiplier);
        if (CanDeepMine && Tier >= 3)
        {
            amount += DeepMineBonus;
        }
        return amount;
    }
}
