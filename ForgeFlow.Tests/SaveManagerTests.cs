using ForgeFlow.Core;
using ForgeFlow.Core.Data;
using ForgeFlow.Core.Data.Definitions;
using ForgeFlow.Core.Entities;
using ForgeFlow.Core.Events;
using ForgeFlow.Core.Proto;
using ForgeFlow.Core.Proto.Prototypes;
using ForgeFlow.Core.Save;
using ForgeFlow.Core.Save.Migrations;
using ForgeFlow.Core.Systems;
using ForgeFlow.Core.Utilities;

namespace ForgeFlow.Tests;

/// <summary>
/// Tests for SaveManager — serialization, deserialization, version migration,
/// CreateSaveData, ApplySaveData, and round-trip integrity.
/// </summary>
public class SaveManagerTests
{
    // ── Helpers ──────────────────────────────────────────────────────

    private static SimulationTicker CreateSimulation()
    {
        EntityBase.ResetIdCounter();
        var eventBus = new EventBus();
        var terrain = new TerrainGrid(32, 32);
        terrain.GenerateDefault();
        var tileManager = new TileManager(terrain);
        var itemManager = new ItemManager(eventBus);
        var villagerSystem = new VillagerSystem(eventBus, itemManager);
        var entityManager = new EntityManager(tileManager, villagerSystem);
        var trafficManager = new TrafficManager();
        var pathTraffic = new PathTrafficSystem(trafficManager, entityManager, tileManager, eventBus);
        var protoRegistry = new ProtoRegistry();
        protoRegistry.RegisterDefaults();
        var protoFactory = new ProtoFactory(protoRegistry, eventBus);
        var tutorialSystem = new TutorialSystem(eventBus, protoFactory);
        var classRegistry = new Core.Data.ClassRegistry();
        var itemRegistry = new Core.Data.ItemRegistry();
        var recipeRegistry = new Core.Data.RecipeRegistry();
        var dungeonRegistry = new Core.Data.DungeonRegistry();
        var autoEquip = new AutoEquipSystem(eventBus, classRegistry, entityManager);
        var dungeonResolver = new DungeonResolver(dungeonRegistry, classRegistry, eventBus, entityManager);
        var fusionCalculator = new FusionCalculator(classRegistry, itemRegistry, itemManager, eventBus, entityManager);
        var appearanceApplier = new AppearanceApplier(eventBus, entityManager);
        var gatingLimits = new GatingLimits();
        var researchManager = new ResearchManager(eventBus);
        var commandBus = new Core.Commands.CommandBus();
        var pathManager = new PathNodeManager(
            entityManager, tileManager, villagerSystem, pathTraffic, eventBus,
            commandBus, tutorialSystem, gatingLimits, researchManager, itemManager);
        var pathGateManager = new PathGateManager(entityManager, tileManager, eventBus, commandBus, tutorialSystem, itemManager, researchManager);
        pathTraffic.SetPathGateManager(pathGateManager);
        var structureManager = new StructureManager(
            entityManager, pathGateManager, pathManager, villagerSystem, itemManager,
            itemRegistry, recipeRegistry,
            gatingLimits, eventBus, commandBus, tutorialSystem, researchManager);
        var worldStateManager = new WorldStateManager(researchManager, entityManager, itemManager, eventBus);
        var dungeonManager = new DungeonManager(entityManager, pathGateManager, villagerSystem, itemManager, eventBus);

        var sim = new SimulationTicker(
            eventBus, commandBus, pathTraffic, villagerSystem, tutorialSystem,
            entityManager, tileManager, pathManager, pathGateManager,
            itemManager, gatingLimits, structureManager, researchManager, worldStateManager,
            appearanceApplier, dungeonResolver, dungeonManager);

        sim.Guild = new Core.Data.GuildData
        {
            GuildName = "Test Guild",
            BannerId = "banner_lion",
            LogoId = "logo_shield",
            
        };

        return sim;
    }

    private static HotbarSystem CreateHotbar(EventBus? eventBus = null)
    {
        return new HotbarSystem(eventBus ?? new EventBus());
    }

    private static AchievementSystem CreateAchievements(EventBus? eventBus = null)
    {
        var system = new AchievementSystem(eventBus ?? new EventBus());
        system.RegisterDefaults();
        return system;
    }

    private static GameStatistics CreateStatistics(EventBus? eventBus = null)
    {
        var stats = new GameStatistics();
        stats.Initialize(eventBus ?? new EventBus());
        return stats;
    }

    // ── Serialization Round-Trip ────────────────────────────────────

    [Fact]
    public void SerializeDeserialize_RoundTrip_PreservesAllFields()
    {
        var saveManager = new SaveManager();
        var data = new SaveData
        {
            GameName = "TestFactory",
            Difficulty = Difficulty.Easy,
            WorldSeed = 42,
            TickCount = 12345,
            GuildName = "Heroes Guild",
            GuildBannerId = "banner_dragon",
            GuildLogoId = "logo_crown",
            GoldBalance = 999,
            CurrentResearchTier = 3,
            CataclysmActive = true,
            WorldPortalOpen = false,
            ActiveTutorialId = "tut_05"
        };
        data.UnlockedTiers = new List<int> { 1, 2, 3 };
        data.ResourceStocks["wood"] = 50;
        data.ResourceStocks["ore"] = 30;
        data.CompletedTutorialIds.Add("tut_01_place_spawner");
        data.CompletedTutorialIds.Add("tut_02_place_exit");

        var json = saveManager.SerializeToJson(data);
        var loaded = saveManager.DeserializeFromJson(json);

        Assert.NotNull(loaded);
        Assert.Equal("TestFactory", loaded!.GameName);
        Assert.Equal(Difficulty.Easy, loaded.Difficulty);
        Assert.Equal(42, loaded.WorldSeed);
        Assert.Equal(12345UL, loaded.TickCount);
        Assert.Equal("Heroes Guild", loaded.GuildName);
        Assert.Equal("banner_dragon", loaded.GuildBannerId);
        Assert.Equal("logo_crown", loaded.GuildLogoId);
        Assert.Equal(999, loaded.GoldBalance);
        Assert.Equal(3, loaded.CurrentResearchTier);
        Assert.True(loaded.CataclysmActive);
        Assert.False(loaded.WorldPortalOpen);
        Assert.Equal("tut_05", loaded.ActiveTutorialId);
        Assert.Equal(3, loaded.UnlockedTiers.Count);
        Assert.Equal(50, loaded.ResourceStocks["wood"]);
        Assert.Equal(30, loaded.ResourceStocks["ore"]);
        Assert.Equal(2, loaded.CompletedTutorialIds.Count);
    }

    [Fact]
    public void SerializeDeserialize_PathGates_RoundTrip()
    {
        var saveManager = new SaveManager();
        var data = new SaveData();
        data.PathGates.Add(new PathGateSaveData
        {
            Id = 10,
            PositionX = 5,
            PositionY = 7,
            Facing = Direction.East,
            Mode = PathGateMode.Entrance,
            LinkedStructureId = 42
        });

        var json = saveManager.SerializeToJson(data);
        var loaded = saveManager.DeserializeFromJson(json);

        Assert.NotNull(loaded);
        Assert.Single(loaded!.PathGates);
        var gate = loaded.PathGates[0];
        Assert.Equal(10UL, gate.Id);
        Assert.Equal(5, gate.PositionX);
        Assert.Equal(7, gate.PositionY);
        Assert.Equal(Direction.East, gate.Facing);
        Assert.Equal(PathGateMode.Entrance, gate.Mode);
        Assert.Equal(42UL, gate.LinkedStructureId);
    }

    [Fact]
    public void SerializeDeserialize_Villagers_RoundTrip()
    {
        var saveManager = new SaveManager();
        var data = new SaveData();
        data.Villagers.Add(new VillagerSaveData
        {
            Id = 20,
            Name = "Bob",
            Profession = VillagerJob.Lumberjack,
            State = VillagerState.Working,
            TrainedClass = VillagerClass.Warrior,
            PositionX = 3,
            PositionY = 4,
            WorkRate = 1.5f,
            Stamina = 80,
            MaxStamina = 100,
            EquippedToolId = "axe",
            EquippedToolDurability = 75.5f,
            CurrentActivity = "Gathering wood",
            Inventory = { new CarriedItemSaveData { ProtoId = "wood", Quantity = 5 } }
        });

        var json = saveManager.SerializeToJson(data);
        var loaded = saveManager.DeserializeFromJson(json);

        Assert.NotNull(loaded);
        Assert.Single(loaded!.Villagers);
        var v = loaded.Villagers[0];
        Assert.Equal("Bob", v.Name);
        Assert.Equal(VillagerJob.Lumberjack, v.Profession);
        Assert.Equal(VillagerState.Working, v.State);
        Assert.Equal(VillagerClass.Warrior, v.TrainedClass);
        Assert.Equal(80, v.Stamina);
        Assert.Equal("axe", v.EquippedToolId);
        Assert.Equal("Gathering wood", v.CurrentActivity);
        Assert.Single(v.Inventory);
        Assert.Equal("wood", v.Inventory[0].ProtoId);
        Assert.Equal(5, v.Inventory[0].Quantity);
    }

    [Fact]
    public void SerializeDeserialize_HotbarSlots_RoundTrip()
    {
        var saveManager = new SaveManager();
        var data = new SaveData();
        data.HotbarSlots.Add(new HotbarSlotSaveData
        {
            SlotIndex = 0,
            ActionType = HotbarActionType.PlaceStructure,
            StructureCategory = "Spawner"
        });
        data.HotbarSlots.Add(new HotbarSlotSaveData
        {
            SlotIndex = 1,
            ActionType = HotbarActionType.DrawPath,
            StructureCategory = "Spawner" // default, ignored for DrawPath
        });

        var json = saveManager.SerializeToJson(data);
        var loaded = saveManager.DeserializeFromJson(json);

        Assert.NotNull(loaded);
        Assert.Equal(2, loaded!.HotbarSlots.Count);
        Assert.Equal(HotbarActionType.PlaceStructure, loaded.HotbarSlots[0].ActionType);
        Assert.Equal("Spawner", loaded.HotbarSlots[0].StructureCategory);
        Assert.Equal(HotbarActionType.DrawPath, loaded.HotbarSlots[1].ActionType);
    }

    [Fact]
    public void SerializeDeserialize_Achievements_RoundTrip()
    {
        var saveManager = new SaveManager();
        var data = new SaveData();
        data.AchievementProgress.Add(new AchievementProgressSaveData
        {
            DefinitionId = "first_hero",
            IsUnlocked = true,
            CurrentValue = 1,
            TargetValue = 1
        });
        data.AchievementProgress.Add(new AchievementProgressSaveData
        {
            DefinitionId = "ten_heroes",
            IsUnlocked = false,
            CurrentValue = 5,
            TargetValue = 10
        });

        var json = saveManager.SerializeToJson(data);
        var loaded = saveManager.DeserializeFromJson(json);

        Assert.NotNull(loaded);
        Assert.Equal(2, loaded!.AchievementProgress.Count);
        Assert.True(loaded.AchievementProgress[0].IsUnlocked);
        Assert.Equal(5, loaded.AchievementProgress[1].CurrentValue);
    }

    [Fact]
    public void SerializeDeserialize_Statistics_RoundTrip()
    {
        var saveManager = new SaveManager();
        var data = new SaveData
        {
            Statistics = new StatisticsSaveData
            {
                TotalHeroesSpawned = 100,
                TotalHeroesDied = 42,
                TotalDungeonsCleared = 7,
                TotalWorkersWornOut = 15,
                TotalGoldEarned = 5000,
                PlayTimeSeconds = 3600.5f
            }
        };

        var json = saveManager.SerializeToJson(data);
        var loaded = saveManager.DeserializeFromJson(json);

        Assert.NotNull(loaded);
        Assert.NotNull(loaded!.Statistics);
        Assert.Equal(100, loaded.Statistics!.TotalHeroesSpawned);
        Assert.Equal(42, loaded.Statistics.TotalHeroesDied);
        Assert.Equal(7, loaded.Statistics.TotalDungeonsCleared);
        Assert.Equal(15, loaded.Statistics.TotalWorkersWornOut);
        Assert.Equal(5000, loaded.Statistics.TotalGoldEarned);
        Assert.Equal(3600.5f, loaded.Statistics.PlayTimeSeconds);
    }

    // ── Version Migration ───────────────────────────────────────────

    [Fact]
    public void MigrateVersion_NullVersion_BecomesCurrentVersion()
    {
        var data = new SaveData { Version = null! };
        SaveManager.MigrateVersion(data);
        Assert.Equal(SaveData.CurrentVersion, data.Version);
    }

    [Fact]
    public void MigrateVersion_EmptyVersion_BecomesCurrentVersion()
    {
        var data = new SaveData { Version = "" };
        SaveManager.MigrateVersion(data);
        Assert.Equal(SaveData.CurrentVersion, data.Version);
    }

    [Fact]
    public void MigrateVersion_1_0_0_InitializesNewCollections()
    {
        var data = new SaveData
        {
            Version = "1.0.0",
            PathGates = null!,
            Villagers = null!,
            HotbarSlots = null!,
            CompletedTutorialIds = null!,
            AchievementProgress = null!
        };

        SaveManager.MigrateVersion(data);

        Assert.Equal("1.2.0", data.Version);
        Assert.NotNull(data.PathGates);
        Assert.NotNull(data.Villagers);
        Assert.NotNull(data.HotbarSlots);
        Assert.NotNull(data.CompletedTutorialIds);
        Assert.NotNull(data.AchievementProgress);
    }

    [Fact]
    public void MigrateVersion_CurrentVersion_NoChange()
    {
        var data = new SaveData { Version = SaveData.CurrentVersion };
        SaveManager.MigrateVersion(data);
        Assert.Equal(SaveData.CurrentVersion, data.Version);
    }

    // ── CreateSaveData ──────────────────────────────────────────────

    [Fact]
    public void CreateSaveData_SerializesGuildData()
    {
        var sim = CreateSimulation();
        sim.ItemManager.SetStock("gold", 500);
        var saveManager = new SaveManager();

        var data = saveManager.CreateSaveData(sim);

        Assert.Equal("Test Guild", data.GuildName);
        Assert.Equal("banner_lion", data.GuildBannerId);
        Assert.Equal("logo_shield", data.GuildLogoId);
        Assert.Equal(500, data.GoldBalance);
    }

    [Fact]
    public void CreateSaveData_SerializesResourceStocks()
    {
        var sim = CreateSimulation();
        sim.ItemManager.SetStock("wood", 75);
        sim.ItemManager.SetStock("ore", 40);
        var saveManager = new SaveManager();

        var data = saveManager.CreateSaveData(sim);

        Assert.Equal(75, data.ResourceStocks["wood"]);
        Assert.Equal(40, data.ResourceStocks["ore"]);
    }

    [Fact]
    public void CreateSaveData_SerializesWorldState()
    {
        var sim = CreateSimulation();
        var saveManager = new SaveManager();

        var data = saveManager.CreateSaveData(sim);

        Assert.False(data.CataclysmActive);
        Assert.False(data.WorldPortalOpen);
    }

    [Fact]
    public void CreateSaveData_IncludesHotbarSlots_WhenProvided()
    {
        var sim = CreateSimulation();
        var hotbar = CreateHotbar();
        hotbar.SetSlot(0, new HotbarSlot { ActionType = HotbarActionType.PlaceStructure, StructureCategory = "Forestry" });
        hotbar.SetSlot(1, new HotbarSlot { ActionType = HotbarActionType.DrawPath });
        var saveManager = new SaveManager();

        var data = saveManager.CreateSaveData(sim, hotbar: hotbar);

        Assert.Equal(HotbarSystem.SlotCount, data.HotbarSlots.Count);
        Assert.Equal(HotbarActionType.PlaceStructure, data.HotbarSlots[0].ActionType);
        Assert.Equal("Forestry", data.HotbarSlots[0].StructureCategory);
        Assert.Equal(HotbarActionType.DrawPath, data.HotbarSlots[1].ActionType);
    }

    [Fact]
    public void CreateSaveData_IncludesAchievements_WhenProvided()
    {
        var sim = CreateSimulation();
        var achievements = CreateAchievements();
        achievements.Advance("first_hero", 1);
        var saveManager = new SaveManager();

        var data = saveManager.CreateSaveData(sim, achievements: achievements);

        Assert.NotEmpty(data.AchievementProgress);
        var firstHero = data.AchievementProgress.Find(a => a.DefinitionId == "first_hero");
        Assert.NotNull(firstHero);
        Assert.True(firstHero!.IsUnlocked);
    }

    [Fact]
    public void CreateSaveData_IncludesStatistics_WhenProvided()
    {
        var sim = CreateSimulation();
        var statistics = CreateStatistics();
        var saveManager = new SaveManager();

        var data = saveManager.CreateSaveData(sim, statistics: statistics);

        Assert.NotNull(data.Statistics);
    }

    // ── ApplySaveData ───────────────────────────────────────────────

    [Fact]
    public void ApplySaveData_RestoresGuildData()
    {
        var sim = CreateSimulation();
        var saveManager = new SaveManager();
        var data = new SaveData
        {
            GuildName = "Loaded Guild",
            GuildBannerId = "banner_eagle",
            GuildLogoId = "logo_hammer",
            GoldBalance = 777,
        };

        saveManager.ApplySaveData(data, sim);

        Assert.Equal("Loaded Guild", sim.Guild!.GuildName);
        Assert.Equal("banner_eagle", sim.Guild.BannerId);
        Assert.Equal("logo_hammer", sim.Guild.LogoId);
        Assert.Equal(777, sim.ItemManager.GetStock("gold"));
    }

    [Fact]
    public void ApplySaveData_RestoresResourceStocks()
    {
        var sim = CreateSimulation();
        var saveManager = new SaveManager();
        var data = new SaveData();
        data.ResourceStocks["wood"] = 200;
        data.ResourceStocks["ore"] = 150;

        saveManager.ApplySaveData(data, sim);

        Assert.Equal(200, sim.ItemManager.GetStock("wood"));
        Assert.Equal(150, sim.ItemManager.GetStock("ore"));
    }

    [Fact]
    public void ApplySaveData_RestoresResearchTiers()
    {
        var sim = CreateSimulation();
        var saveManager = new SaveManager();
        var data = new SaveData
        {
            CurrentResearchTier = 4,
            UnlockedTiers = new List<int> { 1, 2, 3, 4 }
        };

        saveManager.ApplySaveData(data, sim);

        Assert.Equal(4, sim.ResearchManager.CurrentTier);
        Assert.True(sim.ResearchManager.IsTierUnlocked(3));
        Assert.True(sim.ResearchManager.IsTierUnlocked(4));
        Assert.False(sim.ResearchManager.IsTierUnlocked(5));
    }

    [Fact]
    public void ApplySaveData_RestoresWorldState()
    {
        var sim = CreateSimulation();
        var saveManager = new SaveManager();
        var data = new SaveData
        {
            CataclysmActive = true,
            WorldPortalOpen = true,
            PrestigeCount = 3
        };

        saveManager.ApplySaveData(data, sim);

        Assert.True(sim.WorldStateManager.CataclysmActive);
        Assert.True(sim.WorldStateManager.WorldPortalOpen);
        Assert.Equal(3, sim.WorldStateManager.PrestigeCount);
    }

    [Fact]
    public void ApplySaveData_RestoresHotbar()
    {
        var sim = CreateSimulation();
        var hotbar = CreateHotbar();
        var saveManager = new SaveManager();
        var data = new SaveData();
        data.HotbarSlots.Add(new HotbarSlotSaveData { SlotIndex = 2, ActionType = HotbarActionType.PlaceStructure, StructureCategory = "DungeonPortal" });

        saveManager.ApplySaveData(data, sim, hotbar: hotbar);

        Assert.Equal(HotbarActionType.PlaceStructure, hotbar.GetSlot(2).ActionType);
        Assert.Equal("DungeonPortal", hotbar.GetSlot(2).StructureCategory);
    }

    [Fact]
    public void ApplySaveData_RestoresAchievements()
    {
        var sim = CreateSimulation();
        var achievements = CreateAchievements();
        var saveManager = new SaveManager();
        var data = new SaveData();
        data.AchievementProgress.Add(new AchievementProgressSaveData
        {
            DefinitionId = "ten_heroes",
            IsUnlocked = false,
            CurrentValue = 7,
            TargetValue = 10
        });

        saveManager.ApplySaveData(data, sim, achievements: achievements);

        var progress = achievements.GetProgress("ten_heroes");
        Assert.NotNull(progress);
        Assert.Equal(7, progress!.CurrentValue);
        Assert.False(progress.IsUnlocked);
    }

    [Fact]
    public void ApplySaveData_RestoresStatistics()
    {
        var sim = CreateSimulation();
        var statistics = CreateStatistics();
        var saveManager = new SaveManager();
        var data = new SaveData
        {
            Statistics = new StatisticsSaveData
            {
                TotalHeroesSpawned = 250,
                TotalDungeonsCleared = 12,
                TotalWorkersWornOut = 33,
                TotalGoldEarned = 8000,
                PlayTimeSeconds = 7200f
            }
        };

        saveManager.ApplySaveData(data, sim, statistics: statistics);

        Assert.Equal(250, statistics.TotalHeroesSpawned);
        Assert.Equal(12, statistics.TotalDungeonsCleared);
        Assert.Equal(33, statistics.TotalWorkersWornOut);
        Assert.Equal(8000, statistics.TotalGoldEarned);
        Assert.Equal(7200f, statistics.PlayTimeSeconds);
    }

    // ── Full Round-Trip ─────────────────────────────────────────────

    [Fact]
    public void CreateThenApply_FullRoundTrip_PreservesState()
    {
        // Setup source simulation with state
        var sim1 = CreateSimulation();
        sim1.Guild!.GuildName = "Round Trip Guild";
        sim1.ItemManager.SetStock("gold", 1234);
        sim1.ItemManager.SetStock("wood", 99);
        sim1.ItemManager.SetStock("gems", 7);
        var hotbar1 = CreateHotbar();
        hotbar1.SetSlot(3, new HotbarSlot { ActionType = HotbarActionType.PlaceStructure, StructureCategory = "Inn" });
        var achievements1 = CreateAchievements();
        achievements1.Advance("first_hero", 1);
        var stats1 = CreateStatistics();

        var saveManager = new SaveManager();

        // Save
        var data = saveManager.CreateSaveData(sim1, hotbar1, achievements1, stats1);
        var json = saveManager.SerializeToJson(data);

        // Load into fresh simulation
        var loaded = saveManager.DeserializeFromJson(json);
        Assert.NotNull(loaded);

        var sim2 = CreateSimulation();
        var hotbar2 = CreateHotbar();
        var achievements2 = CreateAchievements();
        var stats2 = CreateStatistics();

        saveManager.ApplySaveData(loaded!, sim2, hotbar2, achievements2, stats2);

        // Verify
        Assert.Equal("Round Trip Guild", sim2.Guild!.GuildName);
        Assert.Equal(1234, sim2.ItemManager.GetStock("gold"));
        Assert.Equal(99, sim2.ItemManager.GetStock("wood"));
        Assert.Equal(7, sim2.ItemManager.GetStock("gems"));
        Assert.Equal(HotbarActionType.PlaceStructure, hotbar2.GetSlot(3).ActionType);
        Assert.Equal("Inn", hotbar2.GetSlot(3).StructureCategory);
        Assert.True(achievements2.IsUnlocked("first_hero"));
    }

    // ── Byte Serialization (Cloud Save) ─────────────────────────────

    [Fact]
    public void SerializeDeserialize_Bytes_RoundTrip()
    {
        var saveManager = new SaveManager();
        var data = new SaveData
        {
            GameName = "CloudGame",
            GoldBalance = 555,
        };

        var bytes = saveManager.SerializeToBytes(data);
        Assert.NotNull(bytes);
        Assert.True(bytes.Length > 0);

        var loaded = saveManager.DeserializeFromBytes(bytes);
        Assert.NotNull(loaded);
        Assert.Equal("CloudGame", loaded!.GameName);
        Assert.Equal(555, loaded.GoldBalance);
    }

    [Fact]
    public void DeserializeFromBytes_NullOrEmpty_ReturnsNull()
    {
        var saveManager = new SaveManager();
        Assert.Null(saveManager.DeserializeFromBytes(null!));
        Assert.Null(saveManager.DeserializeFromBytes(Array.Empty<byte>()));
    }

    // ── Edge Cases ──────────────────────────────────────────────────

    [Fact]
    public void ApplySaveData_NoGuild_DoesNotThrow()
    {
        var sim = CreateSimulation();
        sim.Guild = null;
        var saveManager = new SaveManager();
        var data = new SaveData { GuildName = "Ghost Guild" };

        // Should not throw even with null guild
        saveManager.ApplySaveData(data, sim);
    }

    [Fact]
    public void ApplySaveData_EmptyHotbar_DoesNotOverwrite()
    {
        var sim = CreateSimulation();
        var hotbar = CreateHotbar();
        hotbar.SetSlot(0, new HotbarSlot { ActionType = HotbarActionType.PlaceStructure, StructureCategory = "Spawner" });
        var saveManager = new SaveManager();
        var data = new SaveData(); // Empty HotbarSlots

        saveManager.ApplySaveData(data, sim, hotbar: hotbar);

        // Hotbar should NOT be overwritten when save data has empty slots
        Assert.Equal(HotbarActionType.PlaceStructure, hotbar.GetSlot(0).ActionType);
    }

    [Fact]
    public void ApplySaveData_InvalidHotbarIndex_Ignored()
    {
        var sim = CreateSimulation();
        var hotbar = CreateHotbar();
        var saveManager = new SaveManager();
        var data = new SaveData();
        data.HotbarSlots.Add(new HotbarSlotSaveData { SlotIndex = 999, ActionType = HotbarActionType.DrawPath });

        // Should not throw
        saveManager.ApplySaveData(data, sim, hotbar: hotbar);
    }

    [Fact]
    public void CreateSaveData_WithoutOptionalSystems_ProducesValidData()
    {
        var sim = CreateSimulation();
        var saveManager = new SaveManager();

        var data = saveManager.CreateSaveData(sim);

        Assert.NotNull(data);
        Assert.Equal(SaveData.CurrentVersion, data.Version);
        Assert.Empty(data.HotbarSlots);
        Assert.Empty(data.AchievementProgress);
        Assert.Null(data.Statistics);
    }

    // ── Save Migration Pipeline ─────────────────────────────────────

    [Fact]
    public void DeserializeFromJson_V1_0_0_MigratesToCurrentVersion()
    {
        var saveManager = new SaveManager();
        var json = """
        {
            "Version": "1.0.0",
            "GameName": "OldSave",
            "GoldBalance": 100
        }
        """;

        var data = saveManager.DeserializeFromJson(json);

        Assert.NotNull(data);
        Assert.Equal(SaveData.CurrentVersion, data!.Version);
        Assert.Equal("OldSave", data.GameName);
        Assert.NotNull(data.PathGates);
        Assert.NotNull(data.Villagers);
        Assert.NotNull(data.HotbarSlots);
        Assert.NotNull(data.CompletedTutorialIds);
        Assert.NotNull(data.AchievementProgress);
    }

    [Fact]
    public void DeserializeFromJson_NoVersion_MigratesToCurrentVersion()
    {
        var saveManager = new SaveManager();
        var json = """
        {
            "GameName": "AncientSave",
            "GoldBalance": 50
        }
        """;

        var data = saveManager.DeserializeFromJson(json);

        Assert.NotNull(data);
        Assert.Equal(SaveData.CurrentVersion, data!.Version);
    }

    [Fact]
    public void DeserializeFromJson_CurrentVersion_NoMigrationNeeded()
    {
        var saveManager = new SaveManager();
        var data = new SaveData { GameName = "Modern" };
        var json = saveManager.SerializeToJson(data);

        var loaded = saveManager.DeserializeFromJson(json);

        Assert.NotNull(loaded);
        Assert.Equal(SaveData.CurrentVersion, loaded!.Version);
        Assert.Equal("Modern", loaded.GameName);
    }

    [Fact]
    public void SaveMigrationRegistry_UnknownVersion_Throws()
    {
        var registry = new SaveMigrationRegistry();
        var root = new System.Text.Json.Nodes.JsonObject
        {
            ["Version"] = "0.0.1"
        };

        Assert.Throws<SaveMigrationException>(() => registry.Migrate(root));
    }

    [Fact]
    public void SaveMigrationRegistry_ChainIsValid()
    {
        // Construction validates the chain — no exception = valid.
        var registry = new SaveMigrationRegistry();
        Assert.True(registry.Count > 0);
    }

    [Fact]
    public void Migration_1_0_0_To_1_2_0_SetsVersionAndCollections()
    {
        var migration = new Migration_1_0_0_To_1_2_0();
        var root = new System.Text.Json.Nodes.JsonObject
        {
            ["Version"] = "1.0.0",
            ["GameName"] = "Test"
        };

        migration.Apply(root);

        Assert.Equal("1.2.0", root["Version"]?.GetValue<string>());
        Assert.NotNull(root["PathGates"]);
        Assert.NotNull(root["Villagers"]);
        Assert.NotNull(root["HotbarSlots"]);
        Assert.NotNull(root["CompletedTutorialIds"]);
        Assert.NotNull(root["AchievementProgress"]);
    }

    // ── Save Load Validation ───────────────────────────────────────

    [Fact]
    public void Load_OrphanedItemId_ReportsWarning()
    {
        var saveManager = new SaveManager();
        var itemRegistry = new Core.Data.ItemRegistry();
        // Register only "wood" — "nonexistent_item" will be orphaned.
        itemRegistry.Register(new Core.Proto.Prototypes.ItemProto { Id = "wood", DisplayName = "Wood" });

        var data = new SaveData();
        data.ResourceStocks["wood"] = 10;
        data.ResourceStocks["nonexistent_item"] = 5;
        data.ResourceStocks["gold"] = 100;

        var report = saveManager.ValidateLoadedData(data, itemRegistry);

        Assert.False(report.IsClean);
        Assert.Single(report.OrphanedItemIds);
        Assert.Contains("nonexistent_item", report.OrphanedItemIds);
        Assert.Same(report, saveManager.LastLoadReport);
    }

    [Fact]
    public void Load_AllValidIds_ReportsClean()
    {
        var saveManager = new SaveManager();
        var itemRegistry = new Core.Data.ItemRegistry();
        itemRegistry.Register(new Core.Proto.Prototypes.ItemProto { Id = "wood", DisplayName = "Wood" });

        var data = new SaveData();
        data.ResourceStocks["wood"] = 10;
        data.ResourceStocks["gold"] = 50;

        var report = saveManager.ValidateLoadedData(data, itemRegistry);

        Assert.True(report.IsClean);
        Assert.Empty(report.OrphanedItemIds);
    }

    // ── Phase10 Save Fields ─────────────────────────────────────────

    [Fact]
    public void SaveData_ContainsGuildFields()
    {
        var saveData = new SaveData
        {
            GuildName = "My Guild",
            GuildBannerId = "banner_eagle",
            GuildLogoId = "logo_hammer",
            GoldBalance = 500
        };

        var saveManager = new SaveManager();
        var json = saveManager.SerializeToJson(saveData);
        var loaded = saveManager.DeserializeFromJson(json);

        Assert.NotNull(loaded);
        Assert.Equal("My Guild", loaded!.GuildName);
        Assert.Equal("banner_eagle", loaded.GuildBannerId);
        Assert.Equal("logo_hammer", loaded.GuildLogoId);
        Assert.Equal(500, loaded.GoldBalance);
    }

    [Fact]
    public void HeroSaveData_ContainsPhase10Fields()
    {
        var saveData = new SaveData();
        saveData.Heroes.Add(new HeroSaveData
        {
            Id = 1,
            Profession = WorkerProfession.Warrior,
            ToolDurability = 80f,
            Stamina = 50f,
            CarryLoad = 10f,
            GoldEarned = 100,
            Abilities = new List<AbilitySaveData>
            {
                new AbilitySaveData
                {
                    Id = "extra_attack",
                    DisplayName = "Extra Attack",
                    GainedAsProfession = WorkerProfession.Warrior,
                    BonusValue = 0.15f
                }
            }
        });

        var saveManager = new SaveManager();
        var json = saveManager.SerializeToJson(saveData);
        var loaded = saveManager.DeserializeFromJson(json);

        Assert.NotNull(loaded);
        var hero = loaded!.Heroes[0];
        Assert.Equal(WorkerProfession.Warrior, hero.Profession);
        Assert.Equal(80f, hero.ToolDurability);
        Assert.Equal(50f, hero.Stamina);
        Assert.Equal(100, hero.GoldEarned);
        Assert.Single(hero.Abilities);
        Assert.Equal("extra_attack", hero.Abilities[0].Id);
    }

    // ── Phase7Integration Save ──────────────────────────────────────

    [Fact]
    public void SaveManager_EmptyState_SerializesCorrectly()
    {
        var bus = new EventBus();
        var classReg = new ClassRegistry();
        var tm = new TrafficManager();
        var protoReg = new ProtoRegistry();
        protoReg.RegisterDefaults();
        var pf = new ProtoFactory(protoReg, bus);
        var rm = new ItemManager(bus);
        var vs = new VillagerSystem(bus, rm);
        var terrain = new TerrainGrid(32, 32);
        terrain.GenerateDefault();
        var tileMgr = new TileManager(terrain);
        var entMgr = new EntityManager(tileMgr, vs);
        var pt = new PathTrafficSystem(tm, entMgr, tileMgr, bus);
        var ts = new TutorialSystem(bus, pf);
        var ae = new AutoEquipSystem(bus, classReg, entMgr);
        var dr = new DungeonResolver(new DungeonRegistry(), classReg, bus, entMgr);
        var fc = new FusionCalculator(classReg, new ItemRegistry(), rm, bus, entMgr);
        var aa = new AppearanceApplier(bus, entMgr);
        var gl = new GatingLimits();
        var rsMgr = new ResearchManager(bus);
        var cmdBus = new Core.Commands.CommandBus();
        var pm = new PathNodeManager(entMgr, tileMgr, vs, pt, bus, cmdBus, ts, gl, rsMgr, rm);
        var pgm = new PathGateManager(entMgr, tileMgr, bus, cmdBus, ts, rm, rsMgr);
        pt.SetPathGateManager(pgm);
        var mm = new StructureManager(entMgr, pgm, pm, vs, rm, new ItemRegistry(), new RecipeRegistry(), gl, bus, cmdBus, ts, rsMgr);
        var wsMgr = new WorldStateManager(rsMgr, entMgr, rm, bus);
        var dm = new DungeonManager(entMgr, pgm, vs, rm, bus);
        var sim = new SimulationTicker(bus, cmdBus, pt, vs, ts, entMgr, tileMgr, pm, pgm, rm, gl, mm, rsMgr, wsMgr, aa, dr, dm);

        var saveManager = new SaveManager();
        var saveData = saveManager.CreateSaveData(sim);
        var json = saveManager.SerializeToJson(saveData);

        Assert.False(string.IsNullOrWhiteSpace(json));
        var loaded = saveManager.DeserializeFromJson(json);
        Assert.NotNull(loaded);
    }
}
