using UnityEngine;
using UnityEngine.InputSystem;

namespace ForgeFlow.Presentation.Unity.Input
{

/// <summary>
/// Creates the entire InputActionAsset at runtime — no manual .inputactions
/// Editor asset required. Called from FactoryEntryPoint.Awake().
/// </summary>
internal static class RuntimeInputFactory
{
    public static InputActionAsset CreateFactoryInputActions()
    {
        var asset = ScriptableObject.CreateInstance<InputActionAsset>();
        var map = asset.AddActionMap("FactoryMap");

        // Path drawing (left mouse)
        var draw = map.AddAction("DrawPath", InputActionType.Button);
        draw.AddBinding("<Mouse>/leftButton");

        // Mouse position (continuous value)
        var mousePos = map.AddAction("MousePosition", InputActionType.Value);
        mousePos.AddBinding("<Pointer>/position");

        // Hotkeys 1-9 and 0 (maps to 10 hotbar slots)
        for (int i = 1; i <= 9; i++)
        {
            var hotkey = map.AddAction($"Hotkey{i}", InputActionType.Button);
            hotkey.AddBinding($"<Keyboard>/{i}");
        }
        var hotkey0 = map.AddAction("Hotkey0", InputActionType.Button);
        hotkey0.AddBinding("<Keyboard>/0");

        // Pause
        var pause = map.AddAction("Pause", InputActionType.Button);
        pause.AddBinding("<Keyboard>/p");

        // Right-click
        var rightClick = map.AddAction("RightClick", InputActionType.Button);
        rightClick.AddBinding("<Mouse>/rightButton");

        // Rotate
        var rotate = map.AddAction("RotateKey", InputActionType.Button);
        rotate.AddBinding("<Keyboard>/r");

        // Cancel / Escape
        var cancel = map.AddAction("CancelKey", InputActionType.Button);
        cancel.AddBinding("<Keyboard>/escape");

        // Debug / Dev tool custom actions
        var debugGold = map.AddAction("DebugGiveGold", InputActionType.Button);
        debugGold.AddBinding("<Keyboard>/g");

        var debugTerrain = map.AddAction("DebugCycleTerrain", InputActionType.Button);
        debugTerrain.AddBinding("<Keyboard>/t");

        var debugKill = map.AddAction("DebugKillAll", InputActionType.Button);
        debugKill.AddBinding("<Keyboard>/k");

        asset.Enable();
        return asset;
    }
}
}
