
namespace ForgeFlow.Presentation.Unity.UI
{
    /// <summary>
    /// Metadata for a window registration in <see cref="UIManager"/>.
    /// </summary>
    public sealed class WindowDescriptor
    {
        public string WindowId { get; }
        public UILayer Layer { get; }
        public bool IsModal { get; }
        public bool SaveLayout { get; }

        public WindowDescriptor(string windowId, UILayer layer, bool isModal = false, bool saveLayout = false)
        {
            WindowId = windowId;
            Layer = layer;
            IsModal = isModal;
            SaveLayout = saveLayout;
        }
    }

    /// <summary>
    /// Lightweight handle for referencing a specific window instance.
    /// </summary>
    public readonly struct WindowHandle
    {
        public string WindowId { get; }
        public int InstanceIndex { get; }

        public WindowHandle(string windowId, int instanceIndex = 0)
        {
            WindowId = windowId;
            InstanceIndex = instanceIndex;
        }
    }

    /// <summary>
    /// Central registry of all window ID constants.
    /// Every window must have a unique constant here.
    /// </summary>
    public static class WindowIds
    {
        // Menus
        public const string MainMenu = "main_menu";
        public const string NewGameSetup = "new_game_setup";
        public const string PauseMenu = "pause_menu";
        public const string Settings = "settings";
        public const string GameOver = "game_over";
        public const string Victory = "victory";
        public const string Statistics = "statistics";
        public const string Achievements = "achievements";

        // HUD
        public const string ResourcePanel = "resource_panel";
        public const string TutorialOverlay = "tutorial_overlay";
        public const string ToolModeIndicator = "tool_mode_indicator";
        public const string TopBarHUD = "top_bar_hud";

        // Gameplay
        public const string GameplayToolbar = "gameplay_toolbar";
        public const string StructureInspector = "structure_inspector";
        public const string VillagerInspector = "villager_inspector";
        public const string StockpileInspector = "stockpile_inspector";
        public const string DungeonLog = "dungeon_log";
        public const string WorkerInspector = "worker_inspector";

        // Type-specific structure inspectors
        public const string GatheringInspector = "gathering_inspector";
        public const string SpawnerInspector = "spawner_inspector";
        public const string InnInspector = "inn_inspector";
        public const string CraftStationInspector = "craftstation_inspector";
        public const string TrainingInspector = "training_inspector";
        public const string ForgeInspector = "forge_inspector";
        public const string FilterSplitterInspector = "filter_splitter_inspector";
        public const string DungeonPortalInspector = "dungeon_portal_inspector";
        public const string PathInspector = "path_inspector";
        public const string PathGateInspector = "path_gate_inspector";

        // Modals
        public const string AutomationConfig = "automation_config";

        // Legacy (MonoBehaviour-wrapped)
        public const string DebugHUD = "debug_hud";
        public const string InGameUI = "ingame_ui";
        public const string ResearchPanel = "research_panel";
    }
}
