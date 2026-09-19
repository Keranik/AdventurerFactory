namespace ForgeFlow.Presentation.Unity.Input
{

/// <summary>
/// Extends <see cref="IInputController"/> with a list of shortcut descriptors
/// that the controller wants to advertise for the <see cref="ToolModeIndicator"/>.
/// Each controller pre-allocates its shortcut array once and updates
/// <see cref="ShortcutDescriptor.IsActive"/> per frame as needed.
/// </summary>
internal interface IControllerWithShortcuts : IInputController
{
    /// <summary>
    /// Returns the pre-allocated shortcut descriptors for this controller.
    /// The list must be stable (same instance every call) and updated in-place.
    /// </summary>
    IReadOnlyList<ShortcutDescriptor> GetActiveShortcuts();
}

}
