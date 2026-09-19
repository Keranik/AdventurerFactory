using ForgeFlow.Core.Entities;
using ForgeFlow.Core.Proto.Prototypes;

namespace ForgeFlow.Core.Proto.Logic;

/// <summary>
/// Forestry structure logic. Harvests wood from forest resource nodes.
/// Optionally replants saplings to accelerate regrowth.
/// </summary>
public sealed class ForestryRecipeEntity : GatheringLogicBase
{
    public float WoodQualityMultiplier { get; set; } = 1.0f;
    public bool CanPlantSaplings { get; set; } = true;
    public float SaplingGrowthBonus { get; set; } = 0.2f;

    public ForestryRecipeEntity(EntityId id) : base(id)
    {
        RequiredBiome = BiomeType.Forest;
        TargetResourceId = "sticks";
        GatherInterval = 4.0f;
        GatherAmountPerCycle = 2;

        // Tool-dependent output: bare hands = sticks, axe = logs
        ToolOutputMap[""] = "sticks";
        ToolOutputMap["axe"] = "logs";
        ToolOutputMap["stone_hatchet"] = "logs";
        ToolOutputMap["iron_axe"] = "plank_wood";
    }

    public override void InitializeFromProto(StructureProtoBase proto)
    {
        base.InitializeFromProto(proto);
        if (proto is ForestryRecipeEntityProto fp)
        {
            WoodQualityMultiplier = fp.WoodQualityMultiplier;
            CanPlantSaplings = fp.CanPlantSaplings;
            SaplingGrowthBonus = fp.SaplingGrowthBonus;
        }
    }

    protected override int CalculateGatherAmount()
    {
        return (int)(GatherAmountPerCycle * WoodQualityMultiplier * BiomeBonusMultiplier);
    }

    protected override void OnWorkerCycleComplete(int slotIndex)
    {
        if (CanPlantSaplings && LinkedNode != null && LinkedNode.IsRenewable)
        {
            LinkedNode.RegrowthRate += SaplingGrowthBonus * 0.01f;
        }
    }
}
