namespace ForgeFlow.Core.Proto.Prototypes;

/// <summary>Condition type for tutorial mission completion.</summary>
public enum TutorialConditionType
{
    PlaceStructure,
    BuildPath,
    SpawnHero,
    CompleteDungeon,
    ReachResearchTier,
    BuildVillageBuilding,
    SpawnVillager,
    GatherResource,
    EquipHero,
    FuseHeroes,
    PlaceSpecificStructure,
    ConnectPathToStructure,
    VillagerReachBuilding,
    VillagerPickUpItem,
    VillagerDropOffItem,
    VillagerEnterInn,
    VillagerTrainClass,
    VillagerEnterDungeon,
    CraftItem,
    PlaceBuildingInput,
    PlaceBuildingOutput,
    UseBalancer,
    WatchVillagerGather,
    PlacePathGateEntrance,
    PlacePathGateExit,
    WatchVillagerRest,
    WatchFullLoop,
    SelectStockpileProduct,
    SelectCraftStationRecipe,
    ConfigureFilterSplitter
}

/// <summary>
/// A single condition that must be met to complete a tutorial mission.
/// Each condition represents one explicit step in the guided tutorial.
/// </summary>
public sealed class TutorialCondition
{
    public TutorialConditionType Type { get; set; }
    public string? TargetId { get; set; }
    public int RequiredCount { get; set; } = 1;
    public int CurrentCount { get; set; }

    /// <summary>
    /// Which UI element, toolbar icon, or world tile to highlight for this step.
    /// Used by the presentation layer to render glowing borders / pulsing arrows.
    /// </summary>
    public string HighlightTarget { get; set; } = string.Empty;

    /// <summary>
    /// The message shown to the player when this step activates.
    /// E.g. "Place a spawner by clicking on a tile in the map".
    /// </summary>
    public string StepMessage { get; set; } = string.Empty;

    /// <summary>
    /// Celebration message shown when this step is completed.
    /// E.g. "Objective complete!"
    /// </summary>
    public string CompletionMessage { get; set; } = string.Empty;

    /// <summary>
    /// Optional area highlight key for this step (e.g. "biome_forest").
    /// When set, the presentation layer highlights matching tiles in the world.
    /// </summary>
    public string HighlightArea { get; set; } = string.Empty;

    public bool IsMet => CurrentCount >= RequiredCount;
}

/// <summary>
/// Proto for a tutorial mission / progression gate.
/// Defines what the player must do to unlock the next stage.
/// Each mission is a guided sequence with explicit step messages.
/// </summary>
public sealed class TutorialMissionProto : ProtoBase
{
    public int Order { get; set; }
    public string? PrerequisiteMissionId { get; set; }
    public List<TutorialCondition> Conditions { get; set; } = new();
    public Dictionary<string, int> Rewards { get; set; } = new();
    public string HintText { get; set; } = string.Empty;
    /// <summary>Message shown when the entire mission is completed.</summary>
    public string CelebrationMessage { get; set; } = "Objective complete!";
    /// <summary>Tutorial phase this mission belongs to (1 = Phase 1, etc.). 0 = unassigned.</summary>
    public int Phase { get; set; }
}

/// <summary>Tutorial mission state.</summary>
public enum TutorialMissionState
{
    Locked,
    Active,
    Completed
}
