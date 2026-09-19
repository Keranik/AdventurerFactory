using ForgeFlow.Core.Entities;

namespace ForgeFlow.Presentation.Unity.Input
{

/// <summary>
/// A prioritized input handler in the <see cref="InputControllerStack"/>.
/// Controllers are evaluated in descending <see cref="Priority"/> order.
/// A handler that returns <c>true</c> from any <c>Handle*</c> method
/// consumes the input — lower-priority controllers will not receive it.
/// Returning <c>false</c> passes the input through to the next controller.
/// </summary>
internal interface IInputController
{
    /// <summary>Display name for debugging and tool-mode identification.</summary>
    string Name { get; }

    /// <summary>Higher values are checked first.</summary>
    int Priority { get; }

    /// <summary>Only active controllers receive input.</summary>
    bool IsActive { get; set; }

    /// <summary>
    /// Optional cursor/mode hint for visual feedback.
    /// Return a non-null string (e.g. "placement", "path_draw") when this controller
    /// is active and wants to advertise its tool mode. The <see cref="InputControllerStack"/>
    /// uses the highest-priority active hint as <see cref="InputControllerStack.ActiveToolMode"/>.
    /// </summary>
    string? CursorHint { get; }

    /// <summary>Handle left mouse button pressed. Return true to consume.</summary>
    bool HandleLeftClickDown(GridPosRPG gridPos);

    /// <summary>Handle left mouse button released. Return true to consume.</summary>
    bool HandleLeftClickUp(GridPosRPG gridPos);

    /// <summary>Handle right mouse button clicked. Return true to consume.</summary>
    bool HandleRightClick(GridPosRPG gridPos);

    /// <summary>Handle the Rotate key (R). Return true to consume.</summary>
    bool HandleRotate();

    /// <summary>Handle the Cancel key (Escape). Return true to consume.</summary>
    bool HandleCancel();

    /// <summary>Called every frame for continuous behavior (ghost preview, drag, etc.).</summary>
    void UpdateController(float deltaTime, GridPosRPG cursorGridPos);

    /// <summary>
    /// Called by <see cref="InputControllerStack"/> when this controller is
    /// deactivated externally (e.g. tool switch via hotkey or toolbar).
    /// Implementations should clean up any transient visuals (ghost previews,
    /// selection highlights) without allocating. The default behavior is a no-op.
    /// </summary>
    void OnDeactivated();
}

}
