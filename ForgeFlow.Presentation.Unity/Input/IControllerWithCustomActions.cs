namespace ForgeFlow.Presentation.Unity.Input
{

/// <summary>
/// Phase 2 extension — allows a controller to declare additional
/// <see cref="UnityEngine.InputSystem.InputAction"/> names beyond the
/// four fixed verbs (LMB, RMB, R, Esc). The <see cref="InputControllerStack"/>
/// binds runtime callbacks for each declared action and routes them through
/// <see cref="HandleCustomAction"/> when the controller is active.
/// </summary>
internal interface IControllerWithCustomActions : IInputController
{
    /// <summary>
    /// Returns action names (matching entries in <see cref="RuntimeInputFactory"/>)
    /// that this controller wants to receive via <see cref="HandleCustomAction"/>.
    /// Called once during registration.
    /// </summary>
    IReadOnlyList<string> AdditionalActionNames { get; }

    /// <summary>
    /// Called when one of the controller's declared custom actions fires.
    /// Return <c>true</c> to consume the input.
    /// </summary>
    bool HandleCustomAction(string actionName);
}

}
