using ForgeFlow.Core.Proto.Logic;

namespace ForgeFlow.Core.Entities;

/// <summary>
/// Convenience accessors for <see cref="StructureBase"/> instances.
/// </summary>
public static class StructureExtensions
{
    /// <summary>
    /// Returns a stable category name string for the structure, used for costs,
    /// hotbar, save data, and tutorial advancement. For runtime dispatch prefer
    /// C# type matching (<c>is</c> / <c>switch</c>).
    /// </summary>
    public static string GetCategoryName(this StructureBase entity) => entity switch
    {
        ForestryRecipeEntity => "Forestry",
        MiningRecipeEntity => "MiningNode",
        VillageSpawnerLogic => "Spawner",
        InnLogic => "Inn",
        CraftStationLogic => "CraftStation",
        StockpileLogic => "Stockpile",
        ForgeLogic => "Forge",
        FusionAltarLogic => "FusionAltar",
        TrainingBuildingLogic => "TrainingBuilding",
        DungeonPortalLogic => "DungeonPortal",
        AppearanceWorkshopLogic => "AppearanceWorkshop",
        ToolStationLogic => "ToolStation",
        ArmoryLogic => "Armory",
        JobChangerLogic => "JobChanger",
        AcademyLogic => "Academy",
        CheckGateLogic => "CheckGate",
        FilterSplitterLogic => "FilterSplitter",
        BalancerLogic => "Balancer",
        PathGateLogic => "PathGate",
        PathSegmentLogic => "PathSegment",
        GatheringLogicBase => "Gathering",
        _ => entity.GetType().Name
    };
}
