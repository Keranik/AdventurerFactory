namespace ForgeFlow.Presentation.Unity.Input
{

/// <summary>
/// Snapshot of the current tool mode, broadcast via
/// <see cref="InputControllerStack.ToolModeChanged"/>. Contains enough
/// information for the <see cref="ToolModeIndicator"/> to render a full
/// shortcut bar above the hotbar.
/// </summary>
internal struct ToolModeInfo
{
    /// <summary>Internal mode name (e.g. "placement", "path_draw", or null for idle).</summary>
    public string? ModeName;

    /// <summary>Human-readable item/tool label (e.g. "Spawner", "Draw Path").</summary>
    public string? ItemDisplayName;

    /// <summary>Gold cost of the active action, or 0 if not applicable.</summary>
    public int GoldCost;

    /// <summary>
    /// Priority-filtered, de-duplicated shortcut list from all active controllers.
    /// Rebuilt each time the mode changes. The indicator reads this directly.
    /// </summary>
    public List<ShortcutDescriptor> Shortcuts;

    public ToolModeInfo(string? modeName, string? itemDisplayName, int goldCost, List<ShortcutDescriptor> shortcuts)
    {
        ModeName = modeName;
        ItemDisplayName = itemDisplayName;
        GoldCost = goldCost;
        Shortcuts = shortcuts;
    }
}

}
