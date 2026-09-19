using ForgeFlow.Core.Entities;
using ForgeFlow.Core.Proto.Prototypes;
using ForgeFlow.Core.Systems;

namespace ForgeFlow.Core.Events;

public readonly struct HeroSpawnedEvent : IGameEvent
{
    public EntityId HeroId { get; }
    public string ClassId { get; }
    public GridPosRPG SpawnPosition { get; }

    public HeroSpawnedEvent(EntityId heroId, string classId, GridPosRPG spawnPosition)
    {
        HeroId = heroId;
        ClassId = classId;
        SpawnPosition = spawnPosition;
    }
}

public readonly struct GearEquippedEvent : IGameEvent
{
    public EntityId HeroId { get; }
    public string ItemId { get; }
    public EquipSlot Slot { get; }
    public GridPosRPG Position { get; }

    public GearEquippedEvent(EntityId heroId, string itemId, EquipSlot slot, GridPosRPG position)
    {
        HeroId = heroId;
        ItemId = itemId;
        Slot = slot;
        Position = position;
    }
}

public readonly struct DungeonCompletedEvent : IGameEvent
{
    public EntityId HeroId { get; }
    public string DungeonId { get; }
    public bool Success { get; }
    public int NewLevel { get; }
    public GridPosRPG PortalPosition { get; }

    public DungeonCompletedEvent(EntityId heroId, string dungeonId, bool success, int newLevel, GridPosRPG portalPosition)
    {
        HeroId = heroId;
        DungeonId = dungeonId;
        Success = success;
        NewLevel = newLevel;
        PortalPosition = portalPosition;
    }
}

public readonly struct HeroFusedEvent : IGameEvent
{
    public EntityId SourceHeroId1 { get; }
    public EntityId SourceHeroId2 { get; }
    public EntityId ResultHeroId { get; }
    public string ResultClassId { get; }
    public int ResultLevel { get; }

    public HeroFusedEvent(EntityId sourceHeroId1, EntityId sourceHeroId2, EntityId resultHeroId, string resultClassId, int resultLevel)
    {
        SourceHeroId1 = sourceHeroId1;
        SourceHeroId2 = sourceHeroId2;
        ResultHeroId = resultHeroId;
        ResultClassId = resultClassId;
        ResultLevel = resultLevel;
    }
}

public readonly struct ItemCraftedEvent : IGameEvent
{
    public string ItemId { get; }
    public GridPosRPG StructurePosition { get; }

    public ItemCraftedEvent(string itemId, GridPosRPG structurePosition)
    {
        ItemId = itemId;
        StructurePosition = structurePosition;
    }
}

public readonly struct HeroDiedEvent : IGameEvent
{
    public EntityId HeroId { get; }
    public string DungeonId { get; }
    public GridPosRPG PortalPosition { get; }

    public HeroDiedEvent(EntityId heroId, string dungeonId, GridPosRPG portalPosition)
    {
        HeroId = heroId;
        DungeonId = dungeonId;
        PortalPosition = portalPosition;
    }
}

public readonly struct ResourceProducedEvent : IGameEvent
{
    public string ResourceId { get; }
    public int Quantity { get; }
    public GridPosRPG SourcePosition { get; }

    public ResourceProducedEvent(string resourceId, int quantity, GridPosRPG sourcePosition)
    {
        ResourceId = resourceId;
        Quantity = quantity;
        SourcePosition = sourcePosition;
    }
}

public readonly struct ResearchUnlockedEvent : IGameEvent
{
    public int Tier { get; }

    public ResearchUnlockedEvent(int tier)
    {
        Tier = tier;
    }
}

public readonly struct AppearanceChangedEvent : IGameEvent
{
    public EntityId HeroId { get; }
    public string ChangeType { get; }
    public string NewValue { get; }

    public AppearanceChangedEvent(EntityId heroId, string changeType, string newValue)
    {
        HeroId = heroId;
        ChangeType = changeType;
        NewValue = newValue;
    }
}

public readonly struct VillageBuildingBuiltEvent : IGameEvent
{
    public string BuildingId { get; }

    public VillageBuildingBuiltEvent(string buildingId)
    {
        BuildingId = buildingId;
    }
}

public readonly struct ModLoadedEvent : IGameEvent
{
    public string ModName { get; }

    public ModLoadedEvent(string modName)
    {
        ModName = modName;
    }
}

public readonly struct CataclysmEvent : IGameEvent
{
    public bool Started { get; }
    public float Intensity { get; }

    public CataclysmEvent(bool started, float intensity)
    {
        Started = started;
        Intensity = intensity;
    }
}

public readonly struct WorldPortalEvent : IGameEvent
{
    public bool Opened { get; }

    public WorldPortalEvent(bool opened)
    {
        Opened = opened;
    }
}

public readonly struct PrestigeResetEvent : IGameEvent
{
    public int PrestigeCount { get; }
    public int PreviousResearchTier { get; }
    public float NewBonusMultiplier { get; }

    public PrestigeResetEvent(int prestigeCount, int previousResearchTier, float newBonusMultiplier)
    {
        PrestigeCount = prestigeCount;
        PreviousResearchTier = previousResearchTier;
        NewBonusMultiplier = newBonusMultiplier;
    }
}

public readonly struct ConsoleReadyEvent : IGameEvent
{
    public string PlatformName { get; }

    public ConsoleReadyEvent(string platformName)
    {
        PlatformName = platformName;
    }
}

// --- Phase 5 Events ---

public readonly struct VillagerSpawnedEvent : IGameEvent
{
    public EntityId VillagerId { get; }
    public string Name { get; }
    public GridPosRPG SpawnPosition { get; }

    public VillagerSpawnedEvent(EntityId villagerId, string name, GridPosRPG spawnPosition)
    {
        VillagerId = villagerId;
        Name = name;
        SpawnPosition = spawnPosition;
    }
}

public readonly struct VillagerJobAssignedEvent : IGameEvent
{
    public EntityId VillagerId { get; }
    public string Job { get; }

    public VillagerJobAssignedEvent(EntityId villagerId, string job)
    {
        VillagerId = villagerId;
        Job = job;
    }
}

public readonly struct PathBuiltEvent : IGameEvent
{
    public EntityId SegmentId { get; }
    public GridPosRPG Position { get; }

    public PathBuiltEvent(EntityId segmentId, GridPosRPG position)
    {
        SegmentId = segmentId;
        Position = position;
    }
}

public readonly struct HotbarChangedEvent : IGameEvent
{
    public int SlotIndex { get; }
    public HotbarSlot Slot { get; }

    public HotbarChangedEvent(int slotIndex, HotbarSlot slot)
    {
        SlotIndex = slotIndex;
        Slot = slot;
    }
}

public readonly struct TutorialCompletedEvent : IGameEvent
{
    public string MissionId { get; }
    public int Order { get; }

    public TutorialCompletedEvent(string missionId, int order)
    {
        MissionId = missionId;
        Order = order;
    }
}

public readonly struct TerrainGeneratedEvent : IGameEvent
{
    public int Width { get; }
    public int Height { get; }

    public TerrainGeneratedEvent(int width, int height)
    {
        Width = width;
        Height = height;
    }
}

public readonly struct GatheringCompleteEvent : IGameEvent
{
    public string ResourceId { get; }
    public int Amount { get; }
    public GridPosRPG StructurePosition { get; }

    public GatheringCompleteEvent(string resourceId, int amount, GridPosRPG structurePosition)
    {
        ResourceId = resourceId;
        Amount = amount;
        StructurePosition = structurePosition;
    }
}

// --- Phase 5 Final Pass Events ---

public readonly struct DungeonEncounterStepEvent : IGameEvent
{
    public EntityId HeroId { get; }
    public string DungeonId { get; }
    public int RoomIndex { get; }
    public EncounterType EncounterType { get; }
    public bool Survived { get; }
    public int DamageDealt { get; }
    public int DamageTaken { get; }
    public string? LootDropId { get; }

    public DungeonEncounterStepEvent(
        EntityId heroId, string dungeonId, int roomIndex,
        EncounterType encounterType, bool survived,
        int damageDealt, int damageTaken, string? lootDropId)
    {
        HeroId = heroId;
        DungeonId = dungeonId;
        RoomIndex = roomIndex;
        EncounterType = encounterType;
        Survived = survived;
        DamageDealt = damageDealt;
        DamageTaken = damageTaken;
        LootDropId = lootDropId;
    }
}

public readonly struct VillagerTrainingStartedEvent : IGameEvent
{
    public EntityId VillagerId { get; }
    public string BuildingId { get; }
    public string TargetClass { get; }

    public VillagerTrainingStartedEvent(EntityId villagerId, string buildingId, string targetClass)
    {
        VillagerId = villagerId;
        BuildingId = buildingId;
        TargetClass = targetClass;
    }
}

public readonly struct VillagerTrainingCompleteEvent : IGameEvent
{
    public EntityId VillagerId { get; }
    public string BuildingId { get; }
    public string TrainedClass { get; }

    public VillagerTrainingCompleteEvent(EntityId villagerId, string buildingId, string trainedClass)
    {
        VillagerId = villagerId;
        BuildingId = buildingId;
        TrainedClass = trainedClass;
    }
}

public readonly struct WalkByInteractionEvent : IGameEvent
{
    public EntityId EntityId { get; }
    public EntityId StructureId { get; }
    public string InteractionType { get; }

    public WalkByInteractionEvent(EntityId entityId, EntityId structureId, string interactionType)
    {
        EntityId = entityId;
        StructureId = structureId;
        InteractionType = interactionType;
    }
}

public readonly struct GatingBlockedEvent : IGameEvent
{
    public string BlockedAction { get; }
    public int CurrentCount { get; }
    public int MaxAllowed { get; }
    public int RequiredTier { get; }

    public GatingBlockedEvent(string blockedAction, int currentCount, int maxAllowed, int requiredTier)
    {
        BlockedAction = blockedAction;
        CurrentCount = currentCount;
        MaxAllowed = maxAllowed;
        RequiredTier = requiredTier;
    }
}

public readonly struct StructureInspectedEvent : IGameEvent
{
    public EntityId StructureId { get; }
    public string StructureType { get; }

    public StructureInspectedEvent(EntityId structureId, string structureType)
    {
        StructureId = structureId;
        StructureType = structureType;
    }
}

/// <summary>
/// Published by <see cref="Modding.ModLoader"/> after <c>LoadAllMods</c> completes,
/// so the HUD can surface a summary of load errors/warnings (#14).
/// </summary>
public readonly struct ModLoadReportEvent : IGameEvent
{
    public int ModsLoaded { get; }
    public int ErrorCount { get; }
    public int WarningCount { get; }

    public ModLoadReportEvent(int modsLoaded, int errorCount, int warningCount)
    {
        ModsLoaded = modsLoaded;
        ErrorCount = errorCount;
        WarningCount = warningCount;
    }
}

/// <summary>
/// Published by <see cref="Save.SaveManager"/> after validating a loaded save,
/// so the HUD can surface a "N items missing from your save" prompt (#26).
/// </summary>
public readonly struct SaveLoadReportEvent : IGameEvent
{
    public int OrphanedIdCount { get; }
    public bool IsClean { get; }

    public SaveLoadReportEvent(int orphanedIdCount, bool isClean)
    {
        OrphanedIdCount = orphanedIdCount;
        IsClean = isClean;
    }
}

// --- Phase 6 Events ---

public readonly struct GameStateChangedEvent : IGameEvent
{
    public GameState PreviousState { get; }
    public GameState NewState { get; }

    public GameStateChangedEvent(GameState previousState, GameState newState)
    {
        PreviousState = previousState;
        NewState = newState;
    }
}

public readonly struct AchievementUnlockedEvent : IGameEvent
{
    public string AchievementId { get; }
    public string DisplayName { get; }

    public AchievementUnlockedEvent(string achievementId, string displayName)
    {
        AchievementId = achievementId;
        DisplayName = displayName;
    }
}

public readonly struct SettingsChangedEvent : IGameEvent
{
    public float MasterVolume { get; }
    public int ResolutionWidth { get; }
    public int ResolutionHeight { get; }
    public int GraphicsQuality { get; }

    public SettingsChangedEvent(float masterVolume, int resolutionWidth, int resolutionHeight, int graphicsQuality)
    {
        MasterVolume = masterVolume;
        ResolutionWidth = resolutionWidth;
        ResolutionHeight = resolutionHeight;
        GraphicsQuality = graphicsQuality;
    }
}

// --- Phase 8 Events ---

public readonly struct ThemeChangedEvent : IGameEvent
{
    public string PreviousThemeId { get; }
    public string NewThemeId { get; }

    public ThemeChangedEvent(string previousThemeId, string newThemeId)
    {
        PreviousThemeId = previousThemeId;
        NewThemeId = newThemeId;
    }
}

public readonly struct EntitySelectedEvent : IGameEvent
{
    public EntityId EntityId { get; }
    public string EntityType { get; }
    public GridPosRPG Position { get; }

    public EntitySelectedEvent(EntityId entityId, string entityType, GridPosRPG position)
    {
        EntityId = entityId;
        EntityType = entityType;
        Position = position;
    }
}

public readonly struct EntityDeselectedEvent : IGameEvent { }

public readonly struct HudLayoutChangedEvent : IGameEvent
{
    public string PanelId { get; }

    public HudLayoutChangedEvent(string panelId)
    {
        PanelId = panelId;
    }
}

// --- Phase 9 Events ---

public readonly struct NewGameStartedEvent : IGameEvent
{
    public string GameName { get; }
    public Difficulty Difficulty { get; }
    public int Seed { get; }

    public NewGameStartedEvent(string gameName, Difficulty difficulty, int seed)
    {
        GameName = gameName;
        Difficulty = difficulty;
        Seed = seed;
    }
}

public readonly struct TutorialStepActivatedEvent : IGameEvent
{
    public string MissionId { get; }
    public int StepIndex { get; }
    public string HighlightTarget { get; }
    public string HintText { get; }
    /// <summary>Optional area highlight key (e.g. "biome_forest"). Empty = no area highlight.</summary>
    public string HighlightArea { get; }

    public TutorialStepActivatedEvent(string missionId, int stepIndex, string highlightTarget, string hintText, string highlightArea = "")
    {
        MissionId = missionId;
        StepIndex = stepIndex;
        HighlightTarget = highlightTarget;
        HintText = hintText;
        HighlightArea = highlightArea ?? string.Empty;
    }
}

public readonly struct EndConditionMetEvent : IGameEvent
{
    public bool IsVictory { get; }
    public string Reason { get; }

    public EndConditionMetEvent(bool isVictory, string reason)
    {
        IsVictory = isVictory;
        Reason = reason;
    }
}

// --- Phase 10 Events ---

public readonly struct WorkerWornOutEvent : IGameEvent
{
    public EntityId WorkerId { get; }
    public WearOutReason Reason { get; }
    public WorkerProfession Profession { get; }
    public GridPosRPG BuildingPosition { get; }

    public WorkerWornOutEvent(EntityId workerId, WearOutReason reason, WorkerProfession profession, GridPosRPG buildingPosition)
    {
        WorkerId = workerId;
        Reason = reason;
        Profession = profession;
        BuildingPosition = buildingPosition;
    }
}

public readonly struct AbilityGainedEvent : IGameEvent
{
    public EntityId WorkerId { get; }
    public string AbilityId { get; }
    public string AbilityName { get; }

    public AbilityGainedEvent(EntityId workerId, string abilityId, string abilityName)
    {
        WorkerId = workerId;
        AbilityId = abilityId;
        AbilityName = abilityName;
    }
}

public readonly struct AbilityLostEvent : IGameEvent
{
    public EntityId WorkerId { get; }
    public int AbilitiesLost { get; }
    public WorkerProfession OldProfession { get; }

    public AbilityLostEvent(EntityId workerId, int abilitiesLost, WorkerProfession oldProfession)
    {
        WorkerId = workerId;
        AbilitiesLost = abilitiesLost;
        OldProfession = oldProfession;
    }
}

public readonly struct GoldChangedEvent : IGameEvent
{
    public int OldAmount { get; }
    public int NewAmount { get; }
    public string Reason { get; }

    public GoldChangedEvent(int oldAmount, int newAmount, string reason)
    {
        OldAmount = oldAmount;
        NewAmount = newAmount;
        Reason = reason;
    }
}

public readonly struct GuildCreatedEvent : IGameEvent
{
    public string GuildName { get; }
    public string BannerId { get; }

    public GuildCreatedEvent(string guildName, string bannerId)
    {
        GuildName = guildName;
        BannerId = bannerId;
    }
}

public readonly struct WorkerRoutedEvent : IGameEvent
{
    public EntityId WorkerId { get; }
    public Direction RouteDirection { get; }
    public string GateReason { get; }

    public WorkerRoutedEvent(EntityId workerId, Direction routeDirection, string gateReason)
    {
        WorkerId = workerId;
        RouteDirection = routeDirection;
        GateReason = gateReason;
    }
}

public readonly struct WorkerLevelDowngradedEvent : IGameEvent
{
    public EntityId WorkerId { get; }
    public int OldLevel { get; }
    public int NewLevel { get; }

    public WorkerLevelDowngradedEvent(EntityId workerId, int oldLevel, int newLevel)
    {
        WorkerId = workerId;
        OldLevel = oldLevel;
        NewLevel = newLevel;
    }
}

// --- Phase 15 Events: Tutorial & Gameplay Flow ---

public readonly struct VillagerEnteredBuildingEvent : IGameEvent
{
    public EntityId VillagerId { get; }
    public EntityId BuildingId { get; }
    public string BuildingType { get; }

    public VillagerEnteredBuildingEvent(EntityId villagerId, EntityId buildingId, string buildingType)
    {
        VillagerId = villagerId;
        BuildingId = buildingId;
        BuildingType = buildingType;
    }
}

public readonly struct VillagerLeftBuildingEvent : IGameEvent
{
    public EntityId VillagerId { get; }
    public EntityId BuildingId { get; }
    public string BuildingType { get; }

    public VillagerLeftBuildingEvent(EntityId villagerId, EntityId buildingId, string buildingType)
    {
        VillagerId = villagerId;
        BuildingId = buildingId;
        BuildingType = buildingType;
    }
}

public readonly struct VillagerPickedUpItemEvent : IGameEvent
{
    public EntityId VillagerId { get; }
    public string ResourceId { get; }
    public int Quantity { get; }

    public VillagerPickedUpItemEvent(EntityId villagerId, string resourceId, int quantity)
    {
        VillagerId = villagerId;
        ResourceId = resourceId;
        Quantity = quantity;
    }
}

public readonly struct VillagerDroppedOffItemEvent : IGameEvent
{
    public EntityId VillagerId { get; }
    public string ResourceId { get; }
    public EntityId BuildingId { get; }

    public VillagerDroppedOffItemEvent(EntityId villagerId, string resourceId, EntityId buildingId)
    {
        VillagerId = villagerId;
        ResourceId = resourceId;
        BuildingId = buildingId;
    }
}

public readonly struct VillagerEquippedToolEvent : IGameEvent
{
    public EntityId VillagerId { get; }
    public string? ToolId { get; }

    public VillagerEquippedToolEvent(EntityId villagerId, string? toolId)
    {
        VillagerId = villagerId;
        ToolId = toolId;
    }
}

public readonly struct TutorialStepCompletedEvent : IGameEvent
{
    public string MissionId { get; }
    public int StepIndex { get; }
    public string CompletionMessage { get; }

    public TutorialStepCompletedEvent(string missionId, int stepIndex, string completionMessage)
    {
        MissionId = missionId;
        StepIndex = stepIndex;
        CompletionMessage = completionMessage;
    }
}

// --- PathGate Events ---

public readonly struct PathGatePlacedEvent : IGameEvent
{
    public EntityId GateId { get; }
    public GridPosRPG Position { get; }
    public PathGateMode Mode { get; }
    public EntityId? LinkedStructureId { get; }

    public PathGatePlacedEvent(EntityId gateId, GridPosRPG position, PathGateMode mode, EntityId? linkedStructureId)
    {
        GateId = gateId;
        Position = position;
        Mode = mode;
        LinkedStructureId = linkedStructureId;
    }
}

public readonly struct PathRemovedEvent : IGameEvent
{
    public EntityId SegmentId { get; }
    public GridPosRPG Position { get; }

    public PathRemovedEvent(EntityId segmentId, GridPosRPG position)
    {
        SegmentId = segmentId;
        Position = position;
    }
}

public readonly struct VillagerReturnedToPoolEvent : IGameEvent
{
    public EntityId VillagerId { get; }
    public string Reason { get; }

    public VillagerReturnedToPoolEvent(EntityId villagerId, string reason)
    {
        VillagerId = villagerId;
        Reason = reason;
    }
}

// --- Phased Tick Events ---

public readonly struct SimulationEarlyTickEvent : IGameEvent
{
    public float DeltaTime { get; }
    public SimulationEarlyTickEvent(float deltaTime) { DeltaTime = deltaTime; }
}

public readonly struct SimulationTickEvent : IGameEvent
{
    public float DeltaTime { get; }
    public SimulationTickEvent(float deltaTime) { DeltaTime = deltaTime; }
}

public readonly struct SimulationLateTickEvent : IGameEvent
{
    public float DeltaTime { get; }
    public SimulationLateTickEvent(float deltaTime) { DeltaTime = deltaTime; }
}

public readonly struct SimulationLagDroppedEvent : IGameEvent
{
    public int DroppedTicks { get; }
    public SimulationLagDroppedEvent(int droppedTicks) { DroppedTicks = droppedTicks; }
}

public readonly struct EntityRotatedEvent : IGameEvent
{
    public EntityId EntityId { get; }
    public string EntityType { get; }
    public GridPosRPG Position { get; }
    public Direction NewFacing { get; }

    public EntityRotatedEvent(EntityId entityId, string entityType, GridPosRPG position, Direction newFacing)
    {
        EntityId = entityId;
        EntityType = entityType;
        Position = position;
        NewFacing = newFacing;
    }
}

// --- UIManager Events ---

public readonly struct WindowOpenedEvent : IGameEvent
{
    public string WindowId { get; }

    public WindowOpenedEvent(string windowId)
    {
        WindowId = windowId;
    }
}

public readonly struct WindowClosedEvent : IGameEvent
{
    public string WindowId { get; }

    public WindowClosedEvent(string windowId)
    {
        WindowId = windowId;
    }
}

public readonly struct GatheringWorkerOutputEvent : IGameEvent
{
    public EntityId VillagerId { get; }
    public EntityId StructureId { get; }
    public string ResourceId { get; }
    public int Quantity { get; }
    public GridPosRPG StructurePosition { get; }

    public GatheringWorkerOutputEvent(EntityId villagerId, EntityId structureId, string resourceId, int quantity, GridPosRPG structurePosition)
    {
        VillagerId = villagerId;
        StructureId = structureId;
        ResourceId = resourceId;
        Quantity = quantity;
        StructurePosition = structurePosition;
    }
}

// --- Tile Highlight Events ---

public readonly struct TileAreaHighlightEvent : IGameEvent
{
    public string GroupId { get; }
    public GridAreaRPG Area { get; }
    public bool Show { get; }

    public TileAreaHighlightEvent(string groupId, GridAreaRPG area, bool show)
    {
        GroupId = groupId;
        Area = area;
        Show = show;
    }
}

// --- Residence / Home Events ---

public readonly struct VillagerAssignedHomeEvent : IGameEvent
{
    public EntityId VillagerId { get; }
    public EntityId HomeId { get; }

    public VillagerAssignedHomeEvent(EntityId villagerId, EntityId homeId)
    {
        VillagerId = villagerId;
        HomeId = homeId;
    }
}

public readonly struct VillagerRestedAtHomeEvent : IGameEvent
{
    public EntityId VillagerId { get; }
    public EntityId HomeId { get; }

    public VillagerRestedAtHomeEvent(EntityId villagerId, EntityId homeId)
    {
        VillagerId = villagerId;
        HomeId = homeId;
    }
}

// --- Stockpile Events ---

public readonly struct StockpileFilterChangedEvent : IGameEvent
{
    public EntityId StockpileId { get; }
    public GridPosRPG Position { get; }

    public StockpileFilterChangedEvent(EntityId stockpileId, GridPosRPG position)
    {
        StockpileId = stockpileId;
        Position = position;
    }
}

// --- Filter Splitter Events ---

public readonly struct FilterSplitterFilterChangedEvent : IGameEvent
{
    public EntityId SplitterId { get; }
    public GridPosRPG Position { get; }

    public FilterSplitterFilterChangedEvent(EntityId splitterId, GridPosRPG position)
    {
        SplitterId = splitterId;
        Position = position;
    }
}

// --- Entry Gating Events ---

/// <summary>
/// Published by <see cref="ForgeFlow.Core.Systems.PathGateManager"/> when a
/// villager reaches an entrance gate but the structure's <see cref="ForgeFlow.Core.Proto.IEntryGated.CheckEntry"/>
/// rejects them. Allows Presentation to surface a diagnostic (tooltip, bubble,
/// log entry) explaining why the villager did not enter instead of silently
/// walking past.
/// </summary>
public readonly struct VillagerEntryRejectedEvent : IGameEvent
{
    public EntityId VillagerId { get; }
    public EntityId StructureId { get; }
    public string StructureProtoId { get; }
    public ForgeFlow.Core.Proto.EntryRejectionReason Reason { get; }

    public VillagerEntryRejectedEvent(
        EntityId villagerId,
        EntityId structureId,
        string structureProtoId,
        ForgeFlow.Core.Proto.EntryRejectionReason reason)
    {
        VillagerId = villagerId;
        StructureId = structureId;
        StructureProtoId = structureProtoId;
        Reason = reason;
    }
}

// --- Villager Dungeon Events ---

public readonly struct VillagerEnteredDungeonEvent : IGameEvent
{
    public EntityId VillagerId { get; }
    public EntityId PortalId { get; }
    public string DungeonId { get; }

    public VillagerEnteredDungeonEvent(EntityId villagerId, EntityId portalId, string dungeonId)
    {
        VillagerId = villagerId;
        PortalId = portalId;
        DungeonId = dungeonId;
    }
}

public readonly struct VillagerDungeonCompletedEvent : IGameEvent
{
    public EntityId VillagerId { get; }
    public EntityId PortalId { get; }
    public string DungeonId { get; }
    public int GoldReward { get; }
    public string? LootItemId { get; }

    public VillagerDungeonCompletedEvent(EntityId villagerId, EntityId portalId, string dungeonId, int goldReward, string? lootItemId)
    {
        VillagerId = villagerId;
        PortalId = portalId;
        DungeonId = dungeonId;
        GoldReward = goldReward;
        LootItemId = lootItemId;
    }
}

public readonly struct VillagerDiedInDungeonEvent : IGameEvent
{
    public EntityId VillagerId { get; }
    public EntityId PortalId { get; }
    public string DungeonId { get; }

    public VillagerDiedInDungeonEvent(EntityId villagerId, EntityId portalId, string dungeonId)
    {
        VillagerId = villagerId;
        PortalId = portalId;
        DungeonId = dungeonId;
    }
}

// --- Craft Station Recipe Selection Events ---

public readonly struct CraftStationRecipeSelectedEvent : IGameEvent
{
    public EntityId StationId { get; }
    public string RecipeId { get; }
    public GridPosRPG Position { get; }

    public CraftStationRecipeSelectedEvent(EntityId stationId, string recipeId, GridPosRPG position)
    {
        StationId = stationId;
        RecipeId = recipeId;
        Position = position;
    }
}

// --- Demolish Events ---

public readonly struct EntityDemolishedEvent : IGameEvent
{
    public EntityId EntityId { get; }
    public string EntityType { get; }
    public GridPosRPG Position { get; }

    public EntityDemolishedEvent(EntityId entityId, string entityType, GridPosRPG position)
    {
        EntityId = entityId;
        EntityType = entityType;
        Position = position;
    }
}

/// <summary>
/// Published after all registries have been cleared and reloaded from embedded JSON.
/// Presentation layer should rebuild any cached previews (hotbar, recipe pickers, etc.).
/// </summary>
public readonly struct DataReloadedEvent { }

/// <summary>Published when a structure is placed on the grid.</summary>
public readonly struct StructurePlacedEvent : IGameEvent
{
    public EntityId StructureId { get; }
    public string Category { get; }
    public GridPosRPG Position { get; }

    public StructurePlacedEvent(EntityId structureId, string category, GridPosRPG position)
    {
        StructureId = structureId;
        Category = category;
        Position = position;
    }
}

/// <summary>Published when a routing node is placed on the grid.</summary>
public readonly struct RoutingNodePlacedEvent : IGameEvent
{
    public EntityId NodeId { get; }
    public string Category { get; }
    public GridPosRPG Position { get; }

    public RoutingNodePlacedEvent(EntityId nodeId, string category, GridPosRPG position)
    {
        NodeId = nodeId;
        Category = category;
        Position = position;
    }
}
