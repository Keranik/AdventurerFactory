using UnityEngine.InputSystem;

namespace ForgeFlow.Presentation.Unity.Input
{

/// <summary>
/// Thin accessor wrapper around a runtime-created <see cref="InputActionAsset"/>.
/// Built entirely in code by <see cref="RuntimeInputFactory"/> — no manual
/// .inputactions Editor asset is needed.
/// </summary>
internal class FactoryInputActions
{
    private readonly InputActionAsset _asset;

    public InputAction DrawPath { get; }
    public InputAction MousePosition { get; }
    public InputAction Hotkey1 { get; }
    public InputAction Hotkey2 { get; }
    public InputAction Hotkey3 { get; }
    public InputAction Hotkey4 { get; }
    public InputAction Hotkey5 { get; }
    public InputAction Hotkey6 { get; }
    public InputAction Hotkey7 { get; }
    public InputAction Hotkey8 { get; }
    public InputAction Hotkey9 { get; }
    public InputAction Hotkey0 { get; }
    public InputAction Pause { get; }
    public InputAction RightClick { get; }
    public InputAction RotateKey { get; }
    public InputAction CancelKey { get; }

    public FactoryInputActions(InputActionAsset asset)
    {
        _asset = asset;

        DrawPath = _asset.FindAction("DrawPath", throwIfNotFound: true)!;
        MousePosition = _asset.FindAction("MousePosition", throwIfNotFound: true)!;
        Hotkey1 = _asset.FindAction("Hotkey1", throwIfNotFound: true)!;
        Hotkey2 = _asset.FindAction("Hotkey2", throwIfNotFound: true)!;
        Hotkey3 = _asset.FindAction("Hotkey3", throwIfNotFound: true)!;
        Hotkey4 = _asset.FindAction("Hotkey4", throwIfNotFound: true)!;
        Hotkey5 = _asset.FindAction("Hotkey5", throwIfNotFound: true)!;
        Hotkey6 = _asset.FindAction("Hotkey6", throwIfNotFound: true)!;
        Hotkey7 = _asset.FindAction("Hotkey7", throwIfNotFound: true)!;
        Hotkey8 = _asset.FindAction("Hotkey8", throwIfNotFound: true)!;
        Hotkey9 = _asset.FindAction("Hotkey9", throwIfNotFound: true)!;
        Hotkey0 = _asset.FindAction("Hotkey0", throwIfNotFound: true)!;
        Pause = _asset.FindAction("Pause", throwIfNotFound: true)!;
        RightClick = _asset.FindAction("RightClick", throwIfNotFound: true)!;
        RotateKey = _asset.FindAction("RotateKey", throwIfNotFound: true)!;
        CancelKey = _asset.FindAction("CancelKey", throwIfNotFound: true)!;
    }

    /// <summary>True when the action map is currently enabled.</summary>
    public bool IsEnabled { get; private set; }

    /// <summary>Looks up an action by name. Returns null if not found.</summary>
    public InputAction? FindAction(string name) => _asset.FindAction(name, throwIfNotFound: false);

    /// <summary>Returns the underlying asset for binding display queries.</summary>
    public InputActionAsset Asset => _asset;

    public void EnableAll()  { _asset.Enable();  IsEnabled = true;  }
    public void DisableAll() { _asset.Disable(); IsEnabled = false; }
}
}
