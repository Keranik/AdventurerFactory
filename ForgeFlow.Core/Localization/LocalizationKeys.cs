namespace ForgeFlow.Core.Localization;

/// <summary>
/// Central registry of all localization key constants used by the Forge UI suite.
/// Every text string displayed in the UI should reference a key from this class.
/// Pure .NET — zero Unity references.
/// </summary>
public static class LocalizationKeys
{
    // --- General ---
    public const string GameTitle = "game.title";
    public const string GameSubtitle = "game.subtitle";

    // --- Main Menu ---
    public const string MainMenuNewGame = "ui.mainmenu.new_game";
    public const string MainMenuContinue = "ui.mainmenu.continue";
    public const string MainMenuSettings = "ui.mainmenu.settings";
    public const string MainMenuAchievements = "ui.mainmenu.achievements";
    public const string MainMenuStatistics = "ui.mainmenu.statistics";
    public const string MainMenuQuit = "ui.mainmenu.quit";

    // --- Pause ---
    public const string PauseTitle = "ui.pause.title";
    public const string PauseResume = "ui.pause.resume";
    public const string PauseSave = "ui.pause.save";
    public const string PauseMainMenu = "ui.pause.main_menu";

    // --- Settings ---
    public const string SettingsTitle = "ui.settings.title";
    public const string SettingsAudio = "ui.settings.audio";
    public const string SettingsGraphics = "ui.settings.graphics";
    public const string SettingsKeybinds = "ui.settings.keybinds";
    public const string SettingsLanguage = "ui.settings.language";
    public const string SettingsApply = "ui.settings.apply";
    public const string SettingsBack = "ui.settings.back";
    public const string SettingsTheme = "ui.settings.theme";
    public const string SettingsCamera = "ui.settings.camera";
    public const string SettingsCustomizeHud = "ui.settings.customize_hud";

    // --- Camera ---
    public const string CameraZoomSpeed = "ui.camera.zoom_speed";
    public const string CameraPanSpeed = "ui.camera.pan_speed";
    public const string CameraRotationSpeed = "ui.camera.rotation_speed";
    public const string CameraSnapRotation = "ui.camera.snap_rotation";
    public const string CameraFreeRotation = "ui.camera.free_rotation";

    // --- Theme ---
    public const string ThemeDarkFactory = "ui.theme.dark_factory";
    public const string ThemeCyber = "ui.theme.cyber";
    public const string ThemeMedieval = "ui.theme.medieval";
    public const string ThemeNeon = "ui.theme.neon";
    public const string ThemeSelect = "ui.theme.select";

    // --- Game Over / Victory ---
    public const string GameOverTitle = "ui.gameover.title";
    public const string GameOverMessage = "ui.gameover.message";
    public const string VictoryTitle = "ui.victory.title";
    public const string VictoryMessage = "ui.victory.message";

    // --- Stats / Achievements ---
    public const string StatsTitle = "ui.stats.title";
    public const string AchievementsTitle = "ui.achievements.title";

    // --- Tutorial ---
    public const string TutorialTitle = "ui.tutorial.title";
    public const string TutorialComplete = "ui.tutorial.complete";

    // --- HUD ---
    public const string HudCustomizeMode = "ui.hud.customize_mode";
    public const string HudResetLayout = "ui.hud.reset_layout";
    public const string HudLockPanels = "ui.hud.lock_panels";
    public const string HudUnlockPanels = "ui.hud.unlock_panels";
    public const string HudPinPanel = "ui.hud.pin_panel";
    public const string HudUnpinPanel = "ui.hud.unpin_panel";

    // --- Inspector ---
    public const string InspectorTitle = "ui.inspector.title";
    public const string InspectorNoSelection = "ui.inspector.no_selection";
    public const string InspectorType = "ui.inspector.type";
    public const string InspectorPosition = "ui.inspector.position";
    public const string InspectorLevel = "ui.inspector.level";
    public const string InspectorHealth = "ui.inspector.health";
    public const string InspectorRecipe = "ui.inspector.recipe";

    // --- Recipe Picker ---
    public const string RecipePickerTitle = "ui.recipe_picker.title";
    public const string RecipePickerNoRecipes = "ui.recipe_picker.no_recipes";
    public const string RecipePickerSelect = "ui.recipe_picker.select";

    // --- Confirmation / Modal ---
    public const string ConfirmOk = "ui.confirm.ok";
    public const string ConfirmCancel = "ui.confirm.cancel";
    public const string ConfirmYes = "ui.confirm.yes";
    public const string ConfirmNo = "ui.confirm.no";
    public const string ConfirmTitle = "ui.confirm.title";

    // --- Tooltip ---
    public const string TooltipClose = "ui.tooltip.close";

    // --- Common actions ---
    public const string ActionSave = "ui.action.save";
    public const string ActionLoad = "ui.action.load";
    public const string ActionDelete = "ui.action.delete";
    public const string ActionRefresh = "ui.action.refresh";
    public const string ActionClose = "ui.action.close";

    // --- Entities ---
    public const string HeroSpawned = "hero.spawned";
    public const string HeroDied = "hero.died";
    public const string DungeonCleared = "dungeon.cleared";
    public const string DungeonFailed = "dungeon.failed";
    public const string ResearchUnlocked = "research.unlocked";
    public const string PrestigeReset = "prestige.reset";
    public const string VillagerSpawned = "villager.spawned";
    public const string VillagerTrained = "villager.trained";

    // --- Phase 9: New Game Setup ---
    public const string NewGameTitle = "ui.newgame.title";
    public const string NewGameName = "ui.newgame.name";
    public const string NewGameGuildName = "ui.newgame.guild_name";
    public const string NewGameGuildBanner = "ui.newgame.guild_banner";
    public const string NewGameGuildLogo = "ui.newgame.guild_logo";
    public const string NewGameGuildSection = "ui.newgame.guild_section";
    public const string NewGameDifficulty = "ui.newgame.difficulty";
    public const string NewGameCasual = "ui.newgame.casual";
    public const string NewGameEasy = "ui.newgame.easy";
    public const string NewGameNormal = "ui.newgame.normal";
    public const string NewGameSeed = "ui.newgame.seed";
    public const string NewGameRandomSeed = "ui.newgame.random_seed";
    public const string NewGameBiomeDensity = "ui.newgame.biome_density";
    public const string NewGameStartingResources = "ui.newgame.starting_resources";
    public const string NewGameStartingHeroes = "ui.newgame.starting_heroes";
    public const string NewGameWorldGen = "ui.newgame.world_gen";
    public const string NewGameMapPreview = "ui.newgame.map_preview";
    public const string NewGameStart = "ui.newgame.start";
    public const string NewGameBack = "ui.newgame.back";

    // --- Phase 9: Tutorial Overlay ---
    public const string TutorialOverlayHint = "ui.tutorial.overlay_hint";
    public const string TutorialStep = "ui.tutorial.step";
    public const string TutorialSkip = "ui.tutorial.skip";
    public const string TutorialNext = "ui.tutorial.next";

    // --- Phase 9: End Conditions ---
    public const string GameOverTryAgain = "ui.gameover.try_again";
    public const string GameOverReason = "ui.gameover.reason";
    public const string VictoryCongrats = "ui.victory.congrats";
    public const string VictoryStats = "ui.victory.stats";
    public const string VictoryContinue = "ui.victory.continue";
    public const string VictoryMenu = "ui.victory.menu";

    // --- Phase 9: Load Game ---
    public const string LoadGameTitle = "ui.loadgame.title";
    public const string LoadGameEmpty = "ui.loadgame.empty";
    public const string LoadGameSlot = "ui.loadgame.slot";

    // --- Phase 10: Worker Lifecycle ---
    public const string WorkerWornOut = "worker.worn_out";
    public const string WorkerToolBroken = "worker.tool_broken";
    public const string WorkerStaminaDepleted = "worker.stamina_depleted";
    public const string WorkerCarryFull = "worker.carry_full";
    public const string WorkerResearchDone = "worker.research_done";
    public const string WorkerLevelDowngrade = "worker.level_downgrade";
    public const string AbilityGained = "worker.ability_gained";
    public const string AbilityLost = "worker.ability_lost";

    // --- Phase 10: Guild ---
    public const string GuildName = "ui.guild.name";
    public const string GuildBanner = "ui.guild.banner";
    public const string GuildLogo = "ui.guild.logo";
    public const string GuildCreated = "ui.guild.created";
    public const string GuildGold = "ui.guild.gold";
    public const string GuildLeaderboard = "ui.guild.leaderboard";

    // --- Phase 10: Automation ---
    public const string CheckGateTitle = "ui.automation.check_gate";
    public const string FilterSplitterTitle = "ui.automation.filter_splitter";
    public const string BalancerTitle = "ui.automation.balancer";
    public const string GateCondition = "ui.automation.gate_condition";
    public const string GateOutput = "ui.automation.gate_output";
    public const string GateDefault = "ui.automation.gate_default";

    // --- Phase 10: Service Structures ---
    public const string ToolStationTitle = "ui.structure.tool_station";
    public const string ArmoryTitle = "ui.structure.armory";
    public const string JobChangerTitle = "ui.structure.job_changer";
    public const string AcademyTitle = "ui.structure.academy";

    // --- Phase 10: Gold Economy ---
    public const string GoldInsufficient = "ui.gold.insufficient";
    public const string GoldReward = "ui.gold.reward";
    public const string BuildingCost = "ui.gold.building_cost";

    // --- Phase 11: UI & Visual Polish ---
    public const string WorkerInspectorTitle = "ui.worker_inspector.title";
    public const string WorkerDurability = "ui.worker_inspector.durability";
    public const string WorkerStamina = "ui.worker_inspector.stamina";
    public const string WorkerCarryLoad = "ui.worker_inspector.carry_load";
    public const string WorkerProfession = "ui.worker_inspector.profession";
    public const string WorkerTraits = "ui.worker_inspector.traits";
    public const string WorkerAbilities = "ui.worker_inspector.abilities";
    public const string WorkerWearOutStatus = "ui.worker_inspector.wear_out_status";
    public const string WorkerSendToMaintenance = "ui.worker_inspector.send_to_maintenance";
    public const string WorkerStatusSummary = "ui.hud.worker_status_summary";
    public const string WorkerWornOutCount = "ui.hud.workers_worn_out";
    public const string GuildHudTitle = "ui.hud.guild_title";
    public const string GoldDisplay = "ui.hud.gold_display";
    public const string AutomationConfigTitle = "ui.automation.config_title";
    public const string AutomationAddRule = "ui.automation.add_rule";
    public const string AutomationRemoveRule = "ui.automation.remove_rule";
    public const string AutomationApply = "ui.automation.apply";
    public const string NotificationWorkerWornOut = "ui.notification.worker_worn_out";
    public const string NotificationAbilityGained = "ui.notification.ability_gained";
    public const string NotificationGoldEarned = "ui.notification.gold_earned";
    public const string WorkerNone = "ui.worker_inspector.none";
    public const string BalancerOutputs = "ui.automation.balancer_outputs";

    // --- Phase 13: UI/UX Polish ---
    public const string SettingsUIStyle = "ui.settings.ui_style";
    public const string UIStyleFunWhimsical = "ui.style.fun_whimsical";
    public const string UIStyleMinimalist = "ui.style.minimalist";
    public const string ThemeEditorTitle = "ui.theme_editor.title";
    public const string ThemeEditorReset = "ui.theme_editor.reset";
    public const string ThemeEditorPreview = "ui.theme_editor.preview";
    public const string ItemOverlayEmpty = "ui.item_overlay.empty";
    public const string StatusBadgeDefault = "ui.status_badge.default";
    public const string MainMenuVersion = "ui.mainmenu.version";
    public const string MainMenuModBrowser = "ui.mainmenu.mod_browser";
    public const string PauseQuickSave = "ui.pause.quick_save";
    public const string PauseQuickLoad = "ui.pause.quick_load";
    public const string HudResourceBar = "ui.hud.resource_bar";
    public const string HudMinimap = "ui.hud.minimap";
    public const string HudToolbar = "ui.hud.toolbar";
    public const string HotbarSlot = "ui.hotbar.slot";
    public const string HotbarEmpty = "ui.hotbar.empty";
    public const string HotbarPath = "ui.hotbar.path";
    public const string SettingsResetDefaults = "ui.settings.reset_defaults";
    public const string SettingsRebindKey = "ui.settings.rebind_key";
    public const string SettingsPressKey = "ui.settings.press_key";

    // --- Phase 15: Tutorial & Gameplay Flow ---
    public const string TutorialObjectiveComplete = "ui.tutorial.objective_complete";
    public const string TutorialPlaceSpawner = "ui.tutorial.place_spawner";
    public const string TutorialDrawPath = "ui.tutorial.draw_path";
    public const string TutorialFirstVillager = "ui.tutorial.first_villager";
    public const string TutorialPlaceForestry = "ui.tutorial.place_forestry";
    public const string TutorialWatchGathering = "ui.tutorial.watch_gathering";
    public const string TutorialPlaceInn = "ui.tutorial.place_inn";
    public const string TutorialStoneGathering = "ui.tutorial.stone_gathering";
    public const string TutorialCrafting = "ui.tutorial.crafting";
    public const string TutorialClassTraining = "ui.tutorial.class_training";
    public const string TutorialDungeon = "ui.tutorial.dungeon";
    public const string TutorialStockpile = "ui.tutorial.stockpile";
    public const string InnTitle = "ui.structure.inn";
    public const string CraftStationTitle = "ui.structure.craft_station";
    public const string StockpileTitle = "ui.structure.stockpile";
    public const string VillagerInventory = "ui.villager.inventory";
    public const string VillagerTool = "ui.villager.tool";
    public const string VillagerBareHands = "ui.villager.bare_hands";
    public const string GatheringToolDependent = "ui.gathering.tool_dependent";
    public const string BuildingInput = "ui.building.input";
    public const string BuildingOutput = "ui.building.output";

    // --- Cataclysm ---
    public const string CataclysmStarted = "cataclysm.started";
    public const string CataclysmSurvived = "cataclysm.survived";

    // --- Achievements (extended) ---
    public const string AchievementsHidden = "ui.achievements.hidden";
    public const string AchievementsLocked = "ui.achievements.locked";
    public const string AchievementsUnlocked = "ui.achievements.unlocked";

    // --- Dungeon ---
    public const string DungeonTitle = "ui.dungeon.title";
    public const string DungeonDefeated = "ui.dungeon.defeated";
    public const string DungeonLoot = "ui.dungeon.loot";
    public const string DungeonStep = "ui.dungeon.step";
    public const string DungeonSurvived = "ui.dungeon.survived";
    public const string DungeonSuccess = "ui.dungeon.success";
    public const string DungeonFailed2 = "ui.dungeon.failed";
    public const string DungeonHeader = "ui.dungeon.header";
    public const string DungeonRoomsCleared = "ui.dungeon.rooms_cleared";
    public const string DungeonDamageSummary = "ui.dungeon.damage_summary";

    // --- Notification ---
    public const string NotificationAbilitiesLost = "ui.notification.abilities_lost";
    public const string NotificationLevelDowngrade = "ui.notification.level_downgrade";

    // --- Settings Quality ---
    public const string SettingsQualityLow = "ui.settings.quality_low";
    public const string SettingsQualityMedium = "ui.settings.quality_medium";
    public const string SettingsQualityHigh = "ui.settings.quality_high";
    public const string SettingsQualityUltra = "ui.settings.quality_ultra";

    // --- Game Over (extended) ---
    public const string GameOverRestart = "ui.gameover.restart";

    // --- Gating ---
    public const string GatingBlocked = "ui.gating.blocked";
    public const string GatingBuildingLimit = "ui.gating.building_limit";
    public const string GatingSpawnerLimit = "ui.gating.spawner_limit";

    // --- Inspector (extended) ---
    public const string InspectorActive = "ui.inspector.active";
    public const string InspectorAssignedEntities = "ui.inspector.assigned_entities";
    public const string InspectorProfession = "ui.inspector.profession";
    public const string InspectorGatherRate = "ui.inspector.gather_rate";
    public const string InspectorInputQueue = "ui.inspector.input_queue";
    public const string InspectorOutputClass = "ui.inspector.output_class";
    public const string InspectorOutputQueue = "ui.inspector.output_queue";
    public const string InspectorRequiredBiome = "ui.inspector.required_biome";
    public const string InspectorTargetResource = "ui.inspector.target_resource";
    public const string InspectorTrainees = "ui.inspector.trainees";
    public const string InspectorTrainingDuration = "ui.inspector.training_duration";

    // --- Inspector Structure (v1.3) ---
    public const string InspectorTier = "ui.inspector.tier";
    public const string InspectorOutputDirection = "ui.inspector.output_dir";
    public const string InspectorSectionProduction = "ui.inspector.section_production";
    public const string InspectorSectionStatus = "ui.inspector.section_status";
    public const string InspectorSectionStorage = "ui.inspector.section_storage";
    public const string InspectorSectionTraining = "ui.inspector.section_training";
    public const string InspectorGatherInterval = "ui.inspector.gather_interval";
    public const string InspectorNodeHealth = "ui.inspector.node_health";
    public const string InspectorToolOutputs = "ui.inspector.tool_outputs";
    public const string InspectorSpawnInterval = "ui.inspector.spawn_interval";
    public const string InspectorSpawnMax = "ui.inspector.spawn_max";
    public const string InspectorOccupants = "ui.inspector.occupants";
    public const string InspectorRestRate = "ui.inspector.rest_rate";
    public const string InspectorCraftRecipe = "ui.inspector.craft_recipe";
    public const string InspectorCraftProgress = "ui.inspector.craft_progress";
    public const string InspectorStoredInputs = "ui.inspector.stored_inputs";
    public const string InspectorNoRecipe = "ui.inspector.no_recipe";
    public const string InspectorCapacity = "ui.inspector.capacity";
    public const string InspectorStored = "ui.inspector.stored";

    // --- Inspector Workers (Phase 14) ---
    public const string InspectorSectionWorkers = "ui.inspector.section_workers";
    public const string InspectorWorkers = "ui.inspector.workers";
    public const string InspectorWorkerQueue = "ui.inspector.worker_queue";
    public const string InspectorGatherProgress = "ui.inspector.gather_progress";
    public const string InspectorSectionEquipment = "ui.inspector.section_equipment";
    public const string InspectorTool = "ui.inspector.tool";

    // --- Main Menu (extended) ---
    public const string MainMenuTitle = "ui.mainmenu.title";

    // --- Pause (extended) ---
    public const string PauseSettings = "ui.pause.settings";

    // --- Resources ---
    public const string ResourcesFood = "ui.resources.food";
    public const string ResourcesGold = "ui.resources.gold";
    public const string ResourcesHerbs = "ui.resources.herbs";
    public const string ResourcesKnowledge = "ui.resources.knowledge";
    public const string ResourcesMaterials = "ui.resources.materials";
    public const string ResourcesOre = "ui.resources.ore";
    public const string ResourcesTitle = "ui.resources.title";
    public const string ResourcesWood = "ui.resources.wood";

    // --- Settings (extended) ---
    public const string SettingsFullscreen = "ui.settings.fullscreen";
    public const string SettingsMasterVolume = "ui.settings.master_volume";
    public const string SettingsMusicVolume = "ui.settings.music_volume";
    public const string SettingsQuality = "ui.settings.quality";
    public const string SettingsResolution = "ui.settings.resolution";
    public const string SettingsSfxVolume = "ui.settings.sfx_volume";
    public const string SettingsVsync = "ui.settings.vsync";

    // --- Stats (extended) ---
    public const string StatsDungeonsCleared = "ui.stats.dungeons_cleared";
    public const string StatsFusions = "ui.stats.fusions";
    public const string StatsHeroesDied = "ui.stats.heroes_died";
    public const string StatsHeroesSpawned = "ui.stats.heroes_spawned";
    public const string StatsPlayTime = "ui.stats.play_time";
    public const string StatsPrestigeResets = "ui.stats.prestige_resets";
    public const string StatsResourcesProduced = "ui.stats.resources_produced";
    public const string StatsVillagersSpawned = "ui.stats.villagers_spawned";
    public const string StatsVillagersTrained = "ui.stats.villagers_trained";

    // --- Tutorial (extended) ---
    public const string TutorialProgress = "ui.tutorial.progress";

    // --- Villager Inspector ---
    public const string VillagerClass = "ui.villager.class";
    public const string VillagerCurrentTask = "ui.villager.current_task";
    public const string VillagerEquippedArmor = "ui.villager.equipped_armor";
    public const string VillagerEquippedWeapon = "ui.villager.equipped_weapon";
    public const string VillagerHome = "ui.villager.home";
    public const string VillagerInventorySection = "ui.villager.inventory_section";
    public const string VillagerProfession = "ui.villager.profession";
    public const string VillagerLevel = "ui.villager.level";
    public const string VillagerName = "ui.villager.name";
    public const string VillagerStamina = "ui.villager.stamina";
    public const string VillagerState = "ui.villager.state";
    public const string VillagerTitle = "ui.villager.title";
    public const string VillagerToolDurability = "ui.villager.tool_durability";
    public const string VillagerTraits = "ui.villager.traits";
    public const string VillagerWorkRate = "ui.villager.work_rate";

    // --- HUD Top Bar ---
    public const string HudTopBarTitle = "ui.hud.top_bar";
    public const string HudGlobalResources = "ui.hud.global_resources";
    public const string HudEntityStatus = "ui.hud.entity_status";
    public const string HudGuildIdentity = "ui.hud.guild_identity";
    public const string HudVillagerCount = "ui.hud.villager_count";
    public const string HudHeroCount = "ui.hud.hero_count";
    public const string HudActiveDungeons = "ui.hud.active_dungeons";
    public const string HudBuildingCount = "ui.hud.building_count";

    // --- Tool Mode Indicator ---
    public const string ToolModeNone = "ui.tool.mode_none";
    public const string ToolModePlace = "ui.tool.mode_place";
    public const string ToolModePathDraw = "ui.tool.mode_path_draw";
    public const string ToolModeDebug = "ui.tool.mode_debug";
    public const string ToolHintPlace = "ui.tool.hint_place";
    public const string ToolHintDraw = "ui.tool.hint_draw";
    public const string ToolHintCancel = "ui.tool.hint_cancel";
    public const string ToolHintRotate = "ui.tool.hint_rotate";
    public const string ToolHintRotateCamera = "ui.tool.hint_rotate_camera";
    public const string ToolHintRotateEntity = "ui.tool.hint_rotate_entity";
    public const string ToolHintSelect = "ui.tool.hint_select";
    public const string ToolHintExitMode = "ui.tool.hint_exit_mode";
    public const string ToolHintDebugGold = "ui.tool.hint_debug_gold";
    public const string ToolHintDebugTerrain = "ui.tool.hint_debug_terrain";
    public const string ToolHintDebugKillAll = "ui.tool.hint_debug_kill_all";

    // --- Phase 1 Tutorial ---
    public const string TutorialP1PlaceHome = "ui.tutorial.p1.place_home";
    public const string TutorialP1ExitGate = "ui.tutorial.p1.exit_gate";
    public const string TutorialP1InitialPath = "ui.tutorial.p1.initial_path";
    public const string TutorialP1PlaceForestry = "ui.tutorial.p1.place_forestry";
    public const string TutorialP1ForestryEntrance = "ui.tutorial.p1.forestry_entrance";
    public const string TutorialP1ConnectToForestry = "ui.tutorial.p1.connect_to_forestry";
    public const string TutorialP1WatchGathering = "ui.tutorial.p1.watch_gathering";
    public const string TutorialP1PlaceStockpile = "ui.tutorial.p1.place_stockpile";
    public const string TutorialP1SelectSticksFilter = "ui.tutorial.p1.select_sticks_filter";
    public const string TutorialP1StockpileEntrance = "ui.tutorial.p1.stockpile_entrance";
    public const string TutorialP1ForestryExit = "ui.tutorial.p1.forestry_exit";
    public const string TutorialP1ConnectToStockpile = "ui.tutorial.p1.connect_to_stockpile";
    public const string TutorialP1WatchDropoff = "ui.tutorial.p1.watch_dropoff";
    public const string TutorialP1StockpileExit = "ui.tutorial.p1.stockpile_exit";
    public const string TutorialP1LoopPath = "ui.tutorial.p1.loop_path";
    public const string TutorialP1PlaceInn = "ui.tutorial.p1.place_inn";
    public const string TutorialP1WatchFullLoop = "ui.tutorial.p1.watch_full_loop";
    public const string TutorialP1Complete = "ui.tutorial.p1.complete";

    // --- Phase 2 Tutorial ---
    public const string TutorialP2PlaceCraftStation = "ui.tutorial.p2.place_craft_station";
    public const string TutorialP2CraftStationGates = "ui.tutorial.p2.craft_station_gates";
    public const string TutorialP2SelectSpearRecipe = "ui.tutorial.p2.select_spear_recipe";
    public const string TutorialP2PlaceSecondSpawner = "ui.tutorial.p2.place_second_spawner";
    public const string TutorialP2PlaceFilterSplitter = "ui.tutorial.p2.place_filter_splitter";
    public const string TutorialP2ConfigureFilter = "ui.tutorial.p2.configure_filter";
    public const string TutorialP2ConnectFilterCraft = "ui.tutorial.p2.connect_filter_craft";
    public const string TutorialP2WatchSpearCraft = "ui.tutorial.p2.watch_spear_craft";
    public const string TutorialP2PlaceWarriorTrainer = "ui.tutorial.p2.place_warrior_trainer";
    public const string TutorialP2TrainerGatesPaths = "ui.tutorial.p2.trainer_gates_paths";
    public const string TutorialP2WatchTraining = "ui.tutorial.p2.watch_training";
    public const string TutorialP2PlaceDungeon = "ui.tutorial.p2.place_dungeon";
    public const string TutorialP2FirstDungeonRun = "ui.tutorial.p2.first_dungeon_run";
    public const string TutorialP2Complete = "ui.tutorial.p2.complete";

    // --- Stockpile Inspector ---
    public const string StockpileInspectorTitle = "ui.stockpile.title";
    public const string StockpileCapacity = "ui.stockpile.capacity";
    public const string StockpileStored = "ui.stockpile.stored";
    public const string StockpileProduct = "ui.stockpile.product";
    public const string StockpileAcceptsAll = "ui.stockpile.accepts_all";
    public const string StockpileAssignProduct = "ui.stockpile.assign_product";
    public const string StockpileClearProduct = "ui.stockpile.clear_product";
    public const string StockpileChooseItem = "ui.stockpile.choose_item";
    public const string StockpileEmpty = "ui.stockpile.empty";

    // --- Product Picker ---
    public const string ProductPickerTitle = "ui.product_picker.title";
    public const string ProductPickerNoItems = "ui.product_picker.no_items";
    public const string ProductPickerSearch = "ui.product_picker.search";

    // --- Dungeon Portal Inspector ---
    public const string DungeonPortalInspectorTitle = "ui.dungeon_portal.title";
    public const string DungeonPortalSectionDungeon = "ui.dungeon_portal.section_dungeon";
    public const string DungeonPortalDungeonId = "ui.dungeon_portal.dungeon_id";
    public const string DungeonPortalSurvivalChance = "ui.dungeon_portal.survival_chance";
    public const string DungeonPortalGoldReward = "ui.dungeon_portal.gold_reward";
    public const string DungeonPortalRunDuration = "ui.dungeon_portal.run_duration";
    public const string DungeonPortalRunProgress = "ui.dungeon_portal.run_progress";

    // --- Filter Splitter Inspector ---
    public const string FilterSplitterInspectorTitle = "ui.filter_splitter.title";
    public const string FilterSplitterSectionFilter = "ui.filter_splitter.section_filter";
    public const string FilterSplitterSectionRouting = "ui.filter_splitter.section_routing";
    public const string FilterSplitterSectionRules = "ui.filter_splitter.section_rules";
    public const string FilterSplitterFilteredItem = "ui.filter_splitter.filtered_item";
    public const string FilterSplitterFilterDirection = "ui.filter_splitter.filter_direction";
    public const string FilterSplitterNoFilter = "ui.filter_splitter.no_filter";
    public const string FilterSplitterDefaultDirection = "ui.filter_splitter.default_direction";
    public const string FilterSplitterAssignFilter = "ui.filter_splitter.assign_filter";
    public const string FilterSplitterClearFilter = "ui.filter_splitter.clear_filter";
    public const string FilterSplitterNoRules = "ui.filter_splitter.no_rules";
    public const string FilterSplitterRuleCount = "ui.filter_splitter.rule_count";

    // --- Path Inspector ---
    public const string PathInspectorTitle = "ui.path_inspector.title";
    public const string PathInspectorFacing = "ui.path_inspector.facing";
    public const string PathInspectorDescription = "ui.path_inspector.description";
    public const string PathInspectorNodeType = "ui.path_inspector.node_type";
    public const string PathDescRegular = "ui.path_inspector.desc_regular";
    public const string PathDescFilterSplitter = "ui.path_inspector.desc_filter_splitter";
    public const string PathDescBalancer = "ui.path_inspector.desc_balancer";
    public const string PathDescCheckGate = "ui.path_inspector.desc_check_gate";

    // --- Demolish Tool ---
    public const string ToolModeDemolish = "ui.tool.mode_demolish";
    public const string ToolHintDemolish = "ui.tool.hint_demolish";

    // --- Path Gate Inspector ---
    public const string PathGateInspectorTitle = "ui.path_gate_inspector.title";
    public const string PathGateInspectorFacing = "ui.path_gate_inspector.facing";
    public const string PathGateInspectorMode = "ui.path_gate_inspector.mode";
    public const string PathGateInspectorLinkedBuilding = "ui.path_gate_inspector.linked_building";
    public const string PathGateInspectorNotLinked = "ui.path_gate_inspector.not_linked";
    public const string PathGateInspectorDescription = "ui.path_gate_inspector.description";
    public const string PathGateDescEntrance = "ui.path_gate_inspector.desc_entrance";
    public const string PathGateDescExit = "ui.path_gate_inspector.desc_exit";
}
