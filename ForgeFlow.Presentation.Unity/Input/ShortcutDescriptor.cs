
namespace ForgeFlow.Presentation.Unity.Input
{

/// <summary>
/// Describes a single shortcut action exposed by an <see cref="IControllerWithShortcuts"/>.
/// Pre-allocated as a fixed-size array per controller to avoid per-frame allocations.
/// </summary>
internal struct ShortcutDescriptor
{
    /// <summary>Logical name for this action (e.g. "place", "cancel", "rotate").</summary>
    public string ActionId;

    /// <summary>
    /// The <see cref="UnityEngine.InputSystem.InputAction"/> name registered in
    /// <see cref="RuntimeInputFactory"/> (e.g. "DrawPath", "RotateKey").
    /// Used to look up the real bound key label at runtime.
    /// </summary>
    public string InputActionName;

    /// <summary>Localization key for the hint text shown in the <see cref="ToolModeIndicator"/>.</summary>
    public string HintLocalizationKey;

    /// <summary>Whether this shortcut is currently relevant (may be toggled per-frame).</summary>
    public bool IsActive;

    public ShortcutDescriptor(string actionId, string inputActionName, string hintKey, bool isActive = true)
    {
        ActionId = actionId;
        InputActionName = inputActionName;
        HintLocalizationKey = hintKey;
        IsActive = isActive;
    }
}

}
