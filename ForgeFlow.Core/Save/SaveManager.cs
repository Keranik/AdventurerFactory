using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using ForgeFlow.Core.Entities;
using ForgeFlow.Core.Proto;
using ForgeFlow.Core.Proto.Prototypes;
using ForgeFlow.Core.Save.Migrations;
using ForgeFlow.Core.Systems;
using ForgeFlow.Core.Utilities;

namespace ForgeFlow.Core.Save;

public sealed class SaveData
{
    public const string CurrentVersion = "1.2.0";

    public string GameName { get; set; } = "Factory #1";
    public Difficulty Difficulty { get; set; } = Difficulty.Normal;
    public int WorldSeed { get; set; }
    public ulong TickCount { get; set; }
    public List<HeroSaveData> Heroes { get; set; } = new();
    public List<PathSaveData> Paths { get; set; } = new();
    public List<StructureSaveData> Structures { get; set; } = new();
    public List<PathGateSaveData> PathGates { get; set; } = new();
    public List<VillagerSaveData> Villagers { get; set; } = new();
    public Dictionary<string, int> ResourceStocks { get; set; } = new();
    public int CurrentResearchTier { get; set; } = 1;
    public List<int> UnlockedTiers { get; set; } = new() { 1 };
    public List<VillageBuildingSaveData> VillageBuildings { get; set; } = new();
    public List<string> LoadedMods { get; set; } = new();
    public int PrestigeCount { get; set; }
    public DateTime SaveTimestamp { get; set; }
    public string Version { get; set; } = CurrentVersion;

    // Guild & Gold
    public string GuildName { get; set; } = "Unnamed Guild";
    public string GuildBannerId { get; set; } = "banner_default";
    public string GuildLogoId { get; set; } = "logo_sword";
    public int GoldBalance { get; set; } = 100;

    // Hotbar
    public List<HotbarSlotSaveData> HotbarSlots { get; set; } = new();

    // Tutorial progress
    public List<string> CompletedTutorialIds { get; set; } = new();
    public string? ActiveTutorialId { get; set; }

    // Achievements
    public List<AchievementProgressSaveData> AchievementProgress { get; set; } = new();

    // Statistics
    public StatisticsSaveData? Statistics { get; set; }

    // World state
    public bool CataclysmActive { get; set; }
    public bool WorldPortalOpen { get; set; }

    // Entity ID counter (determinism / save-load correctness)
    public ulong NextEntityId { get; set; }
}

public sealed class HeroSaveData
{
    public ulong Id { get; set; }
    public ulong Seed { get; set; }
    public int Level { get; set; }
    public string ClassId { get; set; } = string.Empty;
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public HeroState State { get; set; }
    public int PositionX { get; set; }
    public int PositionY { get; set; }
    public float PathProgress { get; set; }
    public float Morale { get; set; }
    public List<EquippedItemSaveData> Equipment { get; set; } = new();
    public List<string> Traits { get; set; } = new();
    public AppearanceTemplateSaveData? CurrentTemplate { get; set; }

    // Phase 10
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public WorkerProfession Profession { get; set; }
    public List<AbilitySaveData> Abilities { get; set; } = new();
    public float ToolDurability { get; set; } = 100f;
    public float Stamina { get; set; } = 100f;
    public float CarryLoad { get; set; }
    public int GoldEarned { get; set; }
}

public sealed class AbilitySaveData
{
    public string Id { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public WorkerProfession GainedAsProfession { get; set; }
    public float BonusValue { get; set; }
}

public sealed class AppearanceTemplateSaveData
{
    public string Name { get; set; } = "Default";
    public string BaseBody { get; set; } = "human_default";
    public string Hair { get; set; } = "short_01";
    public string Face { get; set; } = "face_01";
    public Dictionary<string, string> ColorPalette { get; set; } = new();
    public Dictionary<string, string> MaterialOverrides { get; set; } = new();
}

public sealed class EquippedItemSaveData
{
    public string ProtoId { get; set; } = string.Empty;
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public EquipSlot Slot { get; set; }
    public int Tier { get; set; }
    public float Damage { get; set; }
    public float Defense { get; set; }
    public float Speed { get; set; }
    public float CritChance { get; set; }
}

public sealed class PathSaveData
{
    public ulong Id { get; set; }
    public int PositionX { get; set; }
    public int PositionY { get; set; }
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public Direction Facing { get; set; }
    public float SpeedMultiplier { get; set; }
}

public sealed class StructureSaveData
{
    public ulong Id { get; set; }
    public string Category { get; set; } = string.Empty;
    public int PositionX { get; set; }
    public int PositionY { get; set; }
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public Direction OutputDirection { get; set; }
    public int Tier { get; set; }
    public bool IsActive { get; set; }
    public Dictionary<string, string> CustomProperties { get; set; } = new();
}

public sealed class VillageBuildingSaveData
{
    public string DefinitionId { get; set; } = string.Empty;
    public List<ulong> AssignedHeroSeeds { get; set; } = new();
    public bool IsActive { get; set; } = true;
}

public sealed class PathGateSaveData
{
    public ulong Id { get; set; }
    public int PositionX { get; set; }
    public int PositionY { get; set; }
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public Direction Facing { get; set; }
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public PathGateMode Mode { get; set; }
    public ulong? LinkedStructureId { get; set; }
}

public sealed class VillagerSaveData
{
    public ulong Id { get; set; }
    public string Name { get; set; } = string.Empty;
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public VillagerJob Profession { get; set; }
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public VillagerState State { get; set; }
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public VillagerClass TrainedClass { get; set; }
    public int PositionX { get; set; }
    public int PositionY { get; set; }
    public float WorkRate { get; set; }
    public float Stamina { get; set; }
    public float MaxStamina { get; set; }
    public string? EquippedToolId { get; set; }
    public float EquippedToolDurability { get; set; }
    public ulong? CurrentPathSegmentId { get; set; }
    public float PathProgress { get; set; }
    public ulong? OwnerStructureId { get; set; }
    public string? CurrentActivity { get; set; }
    public List<CarriedItemSaveData> Inventory { get; set; } = new();
}

public sealed class CarriedItemSaveData
{
    public string ProtoId { get; set; } = string.Empty;
    public int Quantity { get; set; }
}

public sealed class HotbarSlotSaveData
{
    public int SlotIndex { get; set; }
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public HotbarActionType ActionType { get; set; }
    public string StructureCategory { get; set; } = string.Empty;
}

public sealed class AchievementProgressSaveData
{
    public string DefinitionId { get; set; } = string.Empty;
    public bool IsUnlocked { get; set; }
    public long CurrentValue { get; set; }
    public long TargetValue { get; set; }
}

public sealed class StatisticsSaveData
{
    public long TotalHeroesSpawned { get; set; }
    public long TotalHeroesDied { get; set; }
    public long TotalDungeonsAttempted { get; set; }
    public long TotalDungeonsCleared { get; set; }
    public long TotalFusions { get; set; }
    public long TotalItemsCrafted { get; set; }
    public long TotalVillagersSpawned { get; set; }
    public long TotalVillagersTrained { get; set; }
    public long TotalPathsBuilt { get; set; }
    public long TotalResourcesProduced { get; set; }
    public long TotalResearchUnlocked { get; set; }
    public long TotalPrestigeResets { get; set; }
    public long TotalCataclysmsSurvived { get; set; }
    public long TotalTutorialsCompleted { get; set; }
    public long TotalWorkersWornOut { get; set; }
    public long TotalAbilitiesGained { get; set; }
    public long TotalAbilitiesLost { get; set; }
    public long TotalGoldEarned { get; set; }
    public long TotalJobChanges { get; set; }
    public float PlayTimeSeconds { get; set; }
}

public sealed class SaveManager
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    private readonly SaveMigrationRegistry _migrationRegistry;

    /// <summary>
    /// Report from the most recent load operation. Contains orphaned IDs
    /// that were referenced in the save but not found in any registry.
    /// </summary>
    public SaveLoadReport? LastLoadReport { get; private set; }

    public SaveManager()
    {
        _migrationRegistry = new SaveMigrationRegistry();
    }

    public SaveManager(SaveMigrationRegistry migrationRegistry)
    {
        _migrationRegistry = migrationRegistry;
    }

    public SaveData CreateSaveData(
        SimulationTicker simulation,
        HotbarSystem? hotbar = null,
        AchievementSystem? achievements = null,
        GameStatistics? statistics = null)
    {
        var data = new SaveData
        {
            TickCount = simulation.TickCount,
            SaveTimestamp = DateTime.UtcNow,
            Version = SaveData.CurrentVersion,
            CurrentResearchTier = simulation.ResearchManager.CurrentTier,
            UnlockedTiers = new List<int>(simulation.ResearchManager.UnlockedTiers),
            ResourceStocks = new Dictionary<string, int>(simulation.ItemManager.VirtualStocks),
            CataclysmActive = simulation.WorldStateManager.CataclysmActive,
            WorldPortalOpen = simulation.WorldStateManager.WorldPortalOpen
        };

        // Guild
        if (simulation.Guild != null)
        {
            data.GuildName = simulation.Guild.GuildName;
            data.GuildBannerId = simulation.Guild.BannerId;
            data.GuildLogoId = simulation.Guild.LogoId;
            data.GoldBalance = simulation.ItemManager.GetStock("gold");
        }

        // Heroes
        foreach (var hero in simulation.EntityManager.Heroes)
        {
            var heroData = new HeroSaveData
            {
                Id = hero.Id,
                Seed = hero.Seed,
                Level = hero.Level,
                ClassId = hero.ClassId,
                State = hero.State,
                PositionX = hero.Position.X,
                PositionY = hero.Position.Y,
                PathProgress = hero.PathProgress,
                Morale = hero.Morale,
                Profession = hero.Profession,
                ToolDurability = hero.ToolDurability,
                Stamina = hero.Stamina,
                CarryLoad = hero.CarryLoad,
                GoldEarned = hero.GoldEarned,
                CurrentTemplate = new AppearanceTemplateSaveData
                {
                    Name = hero.CurrentTemplate.Name,
                    BaseBody = hero.CurrentTemplate.BaseBody,
                    Hair = hero.CurrentTemplate.Hair,
                    Face = hero.CurrentTemplate.Face,
                    ColorPalette = new Dictionary<string, string>(hero.CurrentTemplate.ColorPalette),
                    MaterialOverrides = new Dictionary<string, string>(hero.CurrentTemplate.MaterialOverrides)
                }
            };

            foreach (var item in hero.Equipment)
            {
                heroData.Equipment.Add(new EquippedItemSaveData
                {
                    ProtoId = item.ProtoId,
                    Slot = item.Slot,
                    Tier = item.Tier,
                    Damage = item.Damage,
                    Defense = item.Defense,
                    Speed = item.Speed,
                    CritChance = item.CritChance
                });
            }

            heroData.Traits.AddRange(hero.Traits);

            foreach (var ability in hero.Abilities)
            {
                heroData.Abilities.Add(new AbilitySaveData
                {
                    Id = ability.Id,
                    DisplayName = ability.DisplayName,
                    GainedAsProfession = ability.GainedAsProfession,
                    BonusValue = ability.BonusValue
                });
            }

            data.Heroes.Add(heroData);
        }

        // Path segments
        foreach (var kvp in simulation.EntityManager.PathSegments)
        {
            var segment = kvp.Value;
            data.Paths.Add(new PathSaveData
            {
                Id = segment.Id,
                PositionX = segment.Position.X,
                PositionY = segment.Position.Y,
                Facing = segment.Facing,
                SpeedMultiplier = segment.SpeedMultiplier
            });
        }

        // Structures
        foreach (var kvp in simulation.EntityManager.Structures)
        {
            var structure = kvp.Value;
            data.Structures.Add(new StructureSaveData
            {
                Id = structure.Id,
                Category = structure.GetCategoryName(),
                PositionX = structure.Position.X,
                PositionY = structure.Position.Y,
                OutputDirection = structure.OutputDirection,
                Tier = structure.Tier,
                IsActive = structure.IsActive
            });
        }

        // Routing Nodes (saved alongside structures in Structures list for backward compatibility)
        foreach (var kvp in simulation.EntityManager.RoutingNodes)
        {
            var node = kvp.Value;
            data.Structures.Add(new StructureSaveData
            {
                Id = node.Id,
                Category = node.GetCategoryName(),
                PositionX = node.Position.X,
                PositionY = node.Position.Y,
                OutputDirection = node.OutputDirection,
                Tier = 0,
                IsActive = node.IsActive
            });
        }

        // PathGates
        foreach (var kvp in simulation.EntityManager.PathGates)
        {
            var gate = kvp.Value;
            data.PathGates.Add(new PathGateSaveData
            {
                Id = gate.Id,
                PositionX = gate.Position.X,
                PositionY = gate.Position.Y,
                Facing = gate.Facing,
                Mode = gate.Mode,
                LinkedStructureId = gate.LinkedStructureId
            });
        }

        // Villagers
        foreach (var villager in simulation.VillagerSystem.Villagers)
        {
            var vData = new VillagerSaveData
            {
                Id = villager.Id,
                Name = villager.Name,
                Profession = villager.Profession,
                State = villager.State,
                TrainedClass = villager.TrainedClass,
                PositionX = villager.Position.X,
                PositionY = villager.Position.Y,
                WorkRate = villager.WorkRate,
                Stamina = villager.Stamina,
                MaxStamina = villager.MaxStamina,
                EquippedToolId = villager.EquippedToolId,
                EquippedToolDurability = villager.EquippedToolDurability,
                CurrentPathSegmentId = villager.CurrentPathSegmentId,
                PathProgress = villager.PathProgress,
                OwnerStructureId = villager.OwnerStructureId,
                CurrentActivity = villager.CurrentActivity
            };

            foreach (var item in villager.Inventory)
            {
                vData.Inventory.Add(new CarriedItemSaveData
                {
                    ProtoId = item.ProtoId,
                    Quantity = item.Quantity
                });
            }

            data.Villagers.Add(vData);
        }

        // Hotbar
        if (hotbar != null)
        {
            var slots = hotbar.GetSlotsForSave();
            for (int i = 0; i < slots.Length; i++)
            {
                data.HotbarSlots.Add(new HotbarSlotSaveData
                {
                    SlotIndex = i,
                    ActionType = slots[i].ActionType,
                    StructureCategory = slots[i].StructureCategory
                });
            }
        }

        // Tutorial progress
        foreach (var mission in simulation.TutorialSystem.AllMissions)
        {
            if (mission.State == TutorialMissionState.Completed)
            {
                data.CompletedTutorialIds.Add(mission.ProtoId);
            }
        }
        data.ActiveTutorialId = simulation.TutorialSystem.ActiveMission?.ProtoId;

        // Achievements
        if (achievements != null)
        {
            foreach (var kvp in achievements.Progress)
            {
                data.AchievementProgress.Add(new AchievementProgressSaveData
                {
                    DefinitionId = kvp.Key,
                    IsUnlocked = kvp.Value.IsUnlocked,
                    CurrentValue = kvp.Value.CurrentValue,
                    TargetValue = kvp.Value.TargetValue
                });
            }
        }

        // Statistics
        if (statistics != null)
        {
            data.Statistics = new StatisticsSaveData
            {
                TotalHeroesSpawned = statistics.TotalHeroesSpawned,
                TotalHeroesDied = statistics.TotalHeroesDied,
                TotalDungeonsAttempted = statistics.TotalDungeonsAttempted,
                TotalDungeonsCleared = statistics.TotalDungeonsCleared,
                TotalFusions = statistics.TotalFusions,
                TotalItemsCrafted = statistics.TotalItemsCrafted,
                TotalVillagersSpawned = statistics.TotalVillagersSpawned,
                TotalVillagersTrained = statistics.TotalVillagersTrained,
                TotalPathsBuilt = statistics.TotalPathsBuilt,
                TotalResourcesProduced = statistics.TotalResourcesProduced,
                TotalResearchUnlocked = statistics.TotalResearchUnlocked,
                TotalPrestigeResets = statistics.TotalPrestigeResets,
                TotalCataclysmsSurvived = statistics.TotalCataclysmsSurvived,
                TotalTutorialsCompleted = statistics.TotalTutorialsCompleted,
                TotalWorkersWornOut = statistics.TotalWorkersWornOut,
                TotalAbilitiesGained = statistics.TotalAbilitiesGained,
                TotalAbilitiesLost = statistics.TotalAbilitiesLost,
                TotalGoldEarned = statistics.TotalGoldEarned,
                TotalJobChanges = statistics.TotalJobChanges,
                PlayTimeSeconds = statistics.PlayTimeSeconds
            };
        }

        // Entity ID counter
        data.NextEntityId = EntityIdFactory.CurrentCounter;

        return data;
    }

    /// <summary>
    /// Restores game state from loaded save data into the simulation and subsystems.
    /// Call after the simulation has been bootstrapped but before unpausing.
    /// Entity reconstruction (heroes, paths, Structures) is handled separately by
    /// the entity creation pipeline — this method restores non-entity state.
    /// </summary>
    public void ApplySaveData(
        SaveData data,
        SimulationTicker simulation,
        HotbarSystem? hotbar = null,
        AchievementSystem? achievements = null,
        GameStatistics? statistics = null)
    {
        // Restore entity ID counter before any entity reconstruction
        if (data.NextEntityId > 0)
        {
            EntityIdFactory.SetCounter(data.NextEntityId);
        }

        // Guild
        if (simulation.Guild != null)
        {
            simulation.Guild.GuildName = data.GuildName;
            simulation.Guild.BannerId = data.GuildBannerId;
            simulation.Guild.LogoId = data.GuildLogoId;
        }

        // Resources
        simulation.ItemManager.Clear();
        foreach (var kvp in data.ResourceStocks)
        {
            simulation.ItemManager.SetStock(kvp.Key, kvp.Value);
        }
        // Gold from save data (backwards compat: standalone field merged into virtual stocks)
        if (!data.ResourceStocks.ContainsKey("gold") && data.GoldBalance > 0)
        {
            simulation.ItemManager.SetStock("gold", data.GoldBalance);
        }

        // Research
        simulation.ResearchManager.LoadFromSave(data.CurrentResearchTier, data.UnlockedTiers);

        // World state
        simulation.WorldStateManager.LoadFromSave(
            data.CataclysmActive,
            data.WorldPortalOpen,
            data.PrestigeCount);

        // Hotbar
        if (hotbar != null && data.HotbarSlots.Count > 0)
        {
            var slots = new HotbarSlot[HotbarSystem.SlotCount];
            for (int i = 0; i < slots.Length; i++)
            {
                slots[i] = new HotbarSlot();
            }
            foreach (var slotData in data.HotbarSlots)
            {
                if (slotData.SlotIndex >= 0 && slotData.SlotIndex < slots.Length)
                {
                    slots[slotData.SlotIndex] = new HotbarSlot
                    {
                        ActionType = slotData.ActionType,
                        StructureCategory = slotData.StructureCategory
                    };
                }
            }
            hotbar.LoadSlots(slots);
        }

        // Tutorial progress
        simulation.TutorialSystem.LoadFromSave(
            data.CompletedTutorialIds,
            data.ActiveTutorialId);

        // Achievements
        if (achievements != null && data.AchievementProgress.Count > 0)
        {
            achievements.LoadFromSave(data.AchievementProgress);
        }

        // Statistics
        if (statistics != null && data.Statistics != null)
        {
            statistics.LoadFromSave(data.Statistics);
        }
    }

    public string SerializeToJson(SaveData data)
    {
        return JsonSerializer.Serialize(data, JsonOptions);
    }

    public SaveData? DeserializeFromJson(string json)
    {
        // Parse into a mutable JSON tree so migrations can transform the schema
        // before we attempt to deserialize into the strongly-typed SaveData.
        var node = JsonNode.Parse(json);
        if (node is not JsonObject root)
        {
            return null;
        }

        _migrationRegistry.Migrate(root);

        var migrated = root.ToJsonString(JsonOptions);
        return JsonSerializer.Deserialize<SaveData>(migrated, JsonOptions);
    }

    /// <summary>
    /// Legacy in-place migration on an already-deserialized <see cref="SaveData"/>.
    /// Prefer the raw-JSON pipeline in <see cref="DeserializeFromJson"/> which runs
    /// migrations before deserialization. This method is kept for backward compatibility
    /// with tests that construct <see cref="SaveData"/> directly.
    /// </summary>
    public static void MigrateVersion(SaveData data)
    {
        if (string.IsNullOrEmpty(data.Version))
        {
            data.Version = "1.0.0";
        }

        // 1.0.0 → 1.2.0: Added PathGates, Villagers, Hotbar, Achievements, Statistics, WorldState
        if (data.Version == "1.0.0")
        {
            data.PathGates ??= new();
            data.Villagers ??= new();
            data.HotbarSlots ??= new();
            data.CompletedTutorialIds ??= new();
            data.AchievementProgress ??= new();
            data.Version = "1.2.0";
        }

        // Future migrations should be added as ISaveMigration classes
        // in ForgeFlow.Core/Save/Migrations/ and registered in SaveMigrationRegistry.
    }

    public void SaveToFile(SaveData data, string filePath)
    {
        var json = SerializeToJson(data);
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
        File.WriteAllText(filePath, json);
    }

    public SaveData? LoadFromFile(string filePath)
    {
        if (!File.Exists(filePath)) return null;
        var json = File.ReadAllText(filePath);
        return DeserializeFromJson(json);
    }

    // --- Console cloud save compatibility ---

    /// <summary>
    /// Serializes save data to a byte array suitable for console cloud storage.
    /// </summary>
    public byte[] SerializeToBytes(SaveData data)
    {
        var json = SerializeToJson(data);
        return System.Text.Encoding.UTF8.GetBytes(json);
    }

    /// <summary>
    /// Deserializes save data from a byte array (console cloud storage).
    /// </summary>
    public SaveData? DeserializeFromBytes(byte[] data)
    {
        if (data == null || data.Length == 0) return null;
        var json = System.Text.Encoding.UTF8.GetString(data);
        return DeserializeFromJson(json);
    }

    /// <summary>
    /// Saves to both local file and cloud (if platform hooks are available).
    /// </summary>
    public void SaveWithCloud(SaveData data, string filePath, IConsolePlatformHooks? platformHooks)
    {
        // Always save locally
        SaveToFile(data, filePath);

        // Attempt cloud save if platform supports it
        if (platformHooks != null)
        {
            platformHooks.ShowSaveIndicator();
            var bytes = SerializeToBytes(data);
            var slotName = Path.GetFileNameWithoutExtension(filePath);
            platformHooks.CloudSave(slotName, bytes);
            platformHooks.HideSaveIndicator();
        }
    }

    /// <summary>
    /// Loads from cloud first (if available), falling back to local file.
    /// </summary>
    public SaveData? LoadWithCloud(string filePath, IConsolePlatformHooks? platformHooks)
    {
        // Try cloud load first
        if (platformHooks != null)
        {
            var slotName = Path.GetFileNameWithoutExtension(filePath);
            var cloudData = platformHooks.CloudLoad(slotName);
            if (cloudData != null)
            {
                var cloudSave = DeserializeFromBytes(cloudData);
                if (cloudSave != null) return cloudSave;
            }
        }

        // Fall back to local file
        return LoadFromFile(filePath);
    }

    /// <summary>
    /// Lists all save files in a directory, returning slot name and save timestamp.
    /// </summary>
    public List<SaveSlotInfo> ListSaveFiles(string saveDirectory)
    {
        var result = new List<SaveSlotInfo>();
        if (!Directory.Exists(saveDirectory)) return result;

        foreach (var file in Directory.GetFiles(saveDirectory, "*.json"))
        {
            try
            {
                var json = File.ReadAllText(file);
                var data = DeserializeFromJson(json);
                if (data != null)
                {
                    result.Add(new SaveSlotInfo
                    {
                        SlotName = Path.GetFileNameWithoutExtension(file),
                        FilePath = file,
                        GameName = data.GameName,
                        Difficulty = data.Difficulty,
                        SaveTimestamp = data.SaveTimestamp,
                        ResearchTier = data.CurrentResearchTier,
                        PrestigeCount = data.PrestigeCount,
                        HeroCount = data.Heroes.Count
                    });
                }
            }
            catch { /* skip corrupt files */ }
        }

        result.Sort((a, b) => b.SaveTimestamp.CompareTo(a.SaveTimestamp));
        return result;
    }

    /// <summary>
    /// Validates all string IDs in a loaded <see cref="SaveData"/> against the provided registries.
    /// Populates <see cref="LastLoadReport"/> with any orphaned IDs.
    /// When <paramref name="eventBus"/> is supplied, publishes a <see cref="Events.SaveLoadReportEvent"/>
    /// summarising the outcome for the HUD (#26).
    /// </summary>
    public SaveLoadReport ValidateLoadedData(
        SaveData data,
        Data.ItemRegistry itemRegistry,
        Data.ClassRegistry? classRegistry = null,
        Events.EventBus? eventBus = null)
    {
        var report = new SaveLoadReport();

        // ResourceStocks → ItemRegistry
        foreach (var kvp in data.ResourceStocks)
        {
            if (itemRegistry.Get(kvp.Key) == null && kvp.Key != "gold")
            {
                report.OrphanedItemIds.Add(kvp.Key);
            }
        }

        // Hero equipment → ItemRegistry; Hero ClassId → ClassRegistry
        foreach (var hero in data.Heroes)
        {
            if (classRegistry != null &&
                !string.IsNullOrEmpty(hero.ClassId) &&
                classRegistry.Get(hero.ClassId) == null)
            {
                report.OrphanedClassIds.Add(hero.ClassId);
            }
            foreach (var eq in hero.Equipment)
            {
                if (!string.IsNullOrEmpty(eq.ProtoId) && itemRegistry.Get(eq.ProtoId) == null)
                {
                    report.OrphanedItemIds.Add(eq.ProtoId);
                }
            }
        }

        // Villager tool IDs + inventory → ItemRegistry
        foreach (var villager in data.Villagers)
        {
            if (!string.IsNullOrEmpty(villager.EquippedToolId) && itemRegistry.Get(villager.EquippedToolId!) == null)
            {
                report.OrphanedItemIds.Add(villager.EquippedToolId!);
            }
            foreach (var carried in villager.Inventory)
            {
                if (!string.IsNullOrEmpty(carried.ProtoId) && itemRegistry.Get(carried.ProtoId) == null)
                {
                    report.OrphanedItemIds.Add(carried.ProtoId);
                }
            }
        }

        LastLoadReport = report;
        eventBus?.Publish(new Events.SaveLoadReportEvent(
            report.OrphanedItemIds.Count + report.OrphanedClassIds.Count,
            report.IsClean));
        return report;
    }
}

/// <summary>
/// Report from a save-load validation pass. Contains IDs that were referenced
/// in the save file but not found in any registry.
/// </summary>
public sealed class SaveLoadReport
{
    /// <summary>Item IDs (stocks, equipment, inventory, tools) that don't exist in ItemRegistry.</summary>
    public List<string> OrphanedItemIds { get; } = new();

    /// <summary>Hero class IDs that don't exist in ClassRegistry.</summary>
    public List<string> OrphanedClassIds { get; } = new();

    /// <summary>True if no orphaned IDs were found.</summary>
    public bool IsClean => OrphanedItemIds.Count == 0 && OrphanedClassIds.Count == 0;
}

public sealed class SaveSlotInfo
{
    public string SlotName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string GameName { get; set; } = string.Empty;
    public Difficulty Difficulty { get; set; }
    public DateTime SaveTimestamp { get; set; }
    public int ResearchTier { get; set; }
    public int PrestigeCount { get; set; }
    public int HeroCount { get; set; }
}

/// <summary>
/// Settings for creating a new game. Pure data — no Unity dependency.
/// </summary>
public sealed class NewGameSettings
{
    public string GameName { get; set; } = "Factory #1";
    public Difficulty Difficulty { get; set; } = Difficulty.Normal;
    public int Seed { get; set; }
    public float BiomeDensity { get; set; } = 1.0f;
    public float StartingResourcesMultiplier { get; set; } = 1.0f;
    public int StartingHeroCount { get; set; } = 2;

    // Phase 10: Guild
    public string GuildName { get; set; } = "Unnamed Guild";
    public string BannerId { get; set; } = "banner_default";
    public string LogoId { get; set; } = "logo_sword";

    /// <summary>Map width in tiles. Defaults to <see cref="GameBootstrapper.DefaultMapWidth"/>.</summary>
    public int MapWidth { get; set; } = GameBootstrapper.DefaultMapWidth;
    /// <summary>Map height in tiles. Defaults to <see cref="GameBootstrapper.DefaultMapHeight"/>.</summary>
    public int MapHeight { get; set; } = GameBootstrapper.DefaultMapHeight;
}
