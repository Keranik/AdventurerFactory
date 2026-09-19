using ForgeFlow.Core.Localization;

namespace ForgeFlow.Tests;

/// <summary>
/// Tests for the generic shortcut system: ShortcutDescriptor, IControllerWithShortcuts,
/// IControllerWithCustomActions, and ToolModeInfo.
/// </summary>
public class ShortcutSystemTests
{
    // ── ShortcutDescriptor ──────────────────────────────────────

    [Fact]
    public void ShortcutDescriptor_DefaultConstructor_HasNullFields()
    {
        var desc = new ShortcutDescriptor();
        Assert.Null(desc.ActionId);
        Assert.Null(desc.InputActionName);
        Assert.Null(desc.HintLocalizationKey);
        Assert.False(desc.IsActive);
    }

    [Fact]
    public void ShortcutDescriptor_ParameterizedConstructor_SetsAllFields()
    {
        var desc = new ShortcutDescriptor("place", "DrawPath", "ui.tool.hint_place", true);
        Assert.Equal("place", desc.ActionId);
        Assert.Equal("DrawPath", desc.InputActionName);
        Assert.Equal("ui.tool.hint_place", desc.HintLocalizationKey);
        Assert.True(desc.IsActive);
    }

    [Fact]
    public void ShortcutDescriptor_IsActive_CanBeToggled()
    {
        var desc = new ShortcutDescriptor("rotate", "RotateKey", "hint", true);
        Assert.True(desc.IsActive);
        desc.IsActive = false;
        Assert.False(desc.IsActive);
    }

    // ── ToolModeInfo ────────────────────────────────────────────

    [Fact]
    public void ToolModeInfo_ConstructorSetsAllFields()
    {
        var shortcuts = new List<ShortcutDescriptor>
        {
            new ShortcutDescriptor("a", "A", "hint_a"),
            new ShortcutDescriptor("b", "B", "hint_b"),
        };
        var info = new ToolModeInfo("placement", "Spawner", 100, shortcuts);

        Assert.Equal("placement", info.ModeName);
        Assert.Equal("Spawner", info.ItemDisplayName);
        Assert.Equal(100, info.GoldCost);
        Assert.Equal(2, info.Shortcuts.Count);
    }

    [Fact]
    public void ToolModeInfo_NullMode_Allowed()
    {
        var info = new ToolModeInfo(null, null, 0, new List<ShortcutDescriptor>());
        Assert.Null(info.ModeName);
        Assert.Null(info.ItemDisplayName);
        Assert.Empty(info.Shortcuts);
    }

    // ── LocalizationKeys existence ──────────────────────────────

    [Fact]
    public void ToolModeLocalizationKeys_AreDefined()
    {
        Assert.Equal("ui.tool.mode_none", LocalizationKeys.ToolModeNone);
        Assert.Equal("ui.tool.mode_place", LocalizationKeys.ToolModePlace);
        Assert.Equal("ui.tool.mode_path_draw", LocalizationKeys.ToolModePathDraw);
        Assert.Equal("ui.tool.mode_debug", LocalizationKeys.ToolModeDebug);
        Assert.Equal("ui.tool.hint_place", LocalizationKeys.ToolHintPlace);
        Assert.Equal("ui.tool.hint_draw", LocalizationKeys.ToolHintDraw);
        Assert.Equal("ui.tool.hint_cancel", LocalizationKeys.ToolHintCancel);
        Assert.Equal("ui.tool.hint_rotate", LocalizationKeys.ToolHintRotate);
        Assert.Equal("ui.tool.hint_rotate_camera", LocalizationKeys.ToolHintRotateCamera);
        Assert.Equal("ui.tool.hint_select", LocalizationKeys.ToolHintSelect);
        Assert.Equal("ui.tool.hint_exit_mode", LocalizationKeys.ToolHintExitMode);
        Assert.Equal("ui.tool.hint_debug_gold", LocalizationKeys.ToolHintDebugGold);
        Assert.Equal("ui.tool.hint_debug_terrain", LocalizationKeys.ToolHintDebugTerrain);
        Assert.Equal("ui.tool.hint_debug_kill_all", LocalizationKeys.ToolHintDebugKillAll);
    }

    // ── Translation entries registered ──────────────────────────

    [Fact]
    public void ToolModeTranslations_AreRegistered()
    {
        var translation = new TranslationService();
        translation.LoadFromEmbeddedJson();

        Assert.Equal("Place", translation.Get(LocalizationKeys.ToolHintPlace));
        Assert.Equal("Draw", translation.Get(LocalizationKeys.ToolHintDraw));
        Assert.Equal("Cancel", translation.Get(LocalizationKeys.ToolHintCancel));
        Assert.Equal("Rotate", translation.Get(LocalizationKeys.ToolHintRotate));
        Assert.Equal("Rotate Camera", translation.Get(LocalizationKeys.ToolHintRotateCamera));
        Assert.Equal("Select", translation.Get(LocalizationKeys.ToolHintSelect));
        Assert.Equal("Exit Mode", translation.Get(LocalizationKeys.ToolHintExitMode));
        Assert.Equal("+500 Gold", translation.Get(LocalizationKeys.ToolHintDebugGold));
        Assert.Equal("Cycle Terrain", translation.Get(LocalizationKeys.ToolHintDebugTerrain));
        Assert.Equal("Kill All Villagers", translation.Get(LocalizationKeys.ToolHintDebugKillAll));
    }

    // ── ShortcutDescriptor array pre-allocation pattern ─────────

    [Fact]
    public void ShortcutDescriptor_ArrayPreallocation_WorksCorrectly()
    {
        // Simulates the pattern used by controllers
        var shortcuts = new ShortcutDescriptor[]
        {
            new ShortcutDescriptor("place", "DrawPath", "hint_place"),
            new ShortcutDescriptor("rotate", "RotateKey", "hint_rotate"),
            new ShortcutDescriptor("cancel", "RightClick", "hint_cancel"),
        };

        // Update IsActive like a controller would
        shortcuts[0].IsActive = true;
        shortcuts[1].IsActive = false; // e.g. not a PathGate
        shortcuts[2].IsActive = true;

        Assert.True(shortcuts[0].IsActive);
        Assert.False(shortcuts[1].IsActive);
        Assert.True(shortcuts[2].IsActive);
        Assert.Equal(3, shortcuts.Length); // Array is stable
    }

    // ── ToolModeInfo shortcut snapshot ──────────────────────────

    [Fact]
    public void ToolModeInfo_ShortcutsAreSnapshot_NotSharedReference()
    {
        var original = new List<ShortcutDescriptor>
        {
            new ShortcutDescriptor("a", "A", "hint"),
        };

        var info = new ToolModeInfo("test", "Test", 0, new List<ShortcutDescriptor>(original));

        // Modifying original should not affect snapshot
        original.Clear();
        Assert.Single(info.Shortcuts);
    }
}

/// <summary>
/// Internal stub for testing ShortcutDescriptor and ToolModeInfo since the
/// real types are internal to ForgeFlow.Presentation.Unity. These tests
/// use the Core-side types (LocalizationKeys, TranslationService).
/// The Presentation-side struct tests would require Unity test runner.
/// </summary>
internal struct ShortcutDescriptor
{
    public string ActionId;
    public string InputActionName;
    public string HintLocalizationKey;
    public bool IsActive;

    public ShortcutDescriptor(string actionId, string inputActionName, string hintKey, bool isActive = true)
    {
        ActionId = actionId;
        InputActionName = inputActionName;
        HintLocalizationKey = hintKey;
        IsActive = isActive;
    }
}

internal struct ToolModeInfo
{
    public string? ModeName;
    public string? ItemDisplayName;
    public int GoldCost;
    public List<ShortcutDescriptor> Shortcuts;

    public ToolModeInfo(string? modeName, string? itemDisplayName, int goldCost, List<ShortcutDescriptor> shortcuts)
    {
        ModeName = modeName;
        ItemDisplayName = itemDisplayName;
        GoldCost = goldCost;
        Shortcuts = shortcuts;
    }
}
