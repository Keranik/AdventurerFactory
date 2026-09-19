using ForgeFlow.Core.Entities;
using ForgeFlow.Presentation.Unity.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ForgeFlow.Presentation.Unity.Input
{

/// <summary>
/// Central input router — the sole subscriber to <see cref="FactoryInputActions"/>.
/// All gameplay input flows through this stack. Controllers are evaluated
/// top-to-bottom by <see cref="IInputController.Priority"/>. The first active
/// controller that returns <c>true</c> from a handler consumes the event.
///
/// Non-routed actions (Pause, Hotkeys) bypass the chain and fire directly.
/// Escape (CancelKey) routes through the chain first; if unclaimed, toggles
/// the game pause state via the <c>onGamePauseToggle</c> callback.
///
/// Controllers implementing <see cref="IControllerWithCustomActions"/> get
/// their declared actions dynamically bound on registration.
/// </summary>
internal sealed class InputControllerStack : MonoBehaviour
{
    private readonly List<IInputController> _controllers = new();
    private readonly List<IInputController> _sorted = new();
    private bool _dirty = true;

    private FactoryInputActions? _inputActions;
    private Camera? _camera;
    private IUIFocusProvider? _uiFocusProvider;
    private Action? _onSimulationPauseToggle;
    private Action? _onGamePauseToggle;

    private Vector2 _currentMousePos;
    private readonly Plane _groundPlane = new(Vector3.up, Vector3.zero);

    // Dynamic action routing (Phase 2)
    private readonly Dictionary<string, Action<InputAction.CallbackContext>> _customActionCallbacks = new();

    // Shortcut aggregation cache
    private readonly List<ShortcutDescriptor> _shortcutCache = new();
    private readonly HashSet<string> _consumedActionIds = new();
    private readonly HashSet<string> _consumedInputActions = new();

    // Tracks the controller that was the active tool-mode winner last frame
    private IInputController? _previousTopController;

    [SerializeField] private float gridCellSize = 1.0f;

    /// <summary>Current cursor grid position (updated every frame).</summary>
    public GridPosRPG CursorGridPos { get; private set; }

    /// <summary>True when the pointer is over a UI element (delegates to <see cref="IUIFocusProvider"/>).</summary>
    public bool IsPointerOverUI => _uiFocusProvider?.IsPointerOverUI ?? false;

    /// <summary>
    /// The <see cref="IInputController.CursorHint"/> of the highest-priority
    /// active controller, or <c>null</c> when no tool mode is active.
    /// </summary>
    public string? ActiveToolMode { get; private set; }

    /// <summary>Fires when <see cref="ActiveToolMode"/> changes, with full shortcut info.</summary>
    public event Action<ToolModeInfo>? ToolModeChanged;

    /// <summary>
    /// Set to <c>true</c> when <see cref="DispatchCancel"/> fires this frame.
    /// Cleared by the owner (FactoryEntryPoint) after its Tick fallback check.
    /// Prevents double-processing of Escape when InputSystem delivers the event.
    /// </summary>
    public bool CancelDispatchedThisFrame { get; set; }

    // ── Initialization ──────────────────────────────────────────

    /// <param name="camera">Main scene camera for screen-to-world raycasts.</param>
    /// <param name="inputActions">Runtime-created input action wrapper.</param>
    /// <param name="uiFocusProvider">UI focus authority for pointer-over-UI gating.</param>
    /// <param name="onSimulationPauseToggle">Called when P is pressed (toggles simulation ticker).</param>
    /// <param name="onGamePauseToggle">Called when Escape falls through all controllers (toggles game state pause).</param>
    public void Initialize(
        Camera camera,
        FactoryInputActions inputActions,
        IUIFocusProvider? uiFocusProvider,
        Action? onSimulationPauseToggle,
        Action? onGamePauseToggle)
    {
        _camera = camera;
        _inputActions = inputActions;
        _uiFocusProvider = uiFocusProvider;
        _onSimulationPauseToggle = onSimulationPauseToggle;
        _onGamePauseToggle = onGamePauseToggle;

        // ── Routed actions (dispatched through the controller chain) ──
        _inputActions.MousePosition.performed += ctx =>
            _currentMousePos = ctx.ReadValue<Vector2>();

        _inputActions.DrawPath.started += _ => DispatchLeftClickDown();
        _inputActions.DrawPath.canceled += _ => DispatchLeftClickUp();
        _inputActions.RightClick.performed += _ => DispatchRightClick();
        _inputActions.RotateKey.performed += _ => DispatchRotate();
        _inputActions.CancelKey.performed += _ => DispatchCancel();

        // ── Non-routed actions (bypass the chain, always fire) ──
        _inputActions.Pause.performed += _ => _onSimulationPauseToggle?.Invoke();
    }

    // ── Registration ────────────────────────────────────────────

    public void Register(IInputController controller)
    {
        _controllers.Add(controller);
        _dirty = true;

        // Phase 2: Bind dynamic custom actions for controllers that declare them
        if (controller is IControllerWithCustomActions customController && _inputActions != null)
        {
            BindCustomActions(customController);
        }
    }

    public void Unregister(IInputController controller)
    {
        _controllers.Remove(controller);
        _dirty = true;

        // Unbind custom actions
        if (controller is IControllerWithCustomActions customController)
        {
            UnbindCustomActions(customController);
        }
    }

    /// <summary>Finds a registered controller by type.</summary>
    public T? Get<T>() where T : class, IInputController
    {
        for (int i = 0; i < _controllers.Count; i++)
        {
            if (_controllers[i] is T t) return t;
        }
        return null;
    }

    // ── Tool Mode API ───────────────────────────────────────────

    /// <summary>Activates the <see cref="PathDrawingController"/>.</summary>
    public void EnterPathDrawMode()
    {
        for (int i = 0; i < _controllers.Count; i++)
        {
            if (_controllers[i] is PathDrawingController pdc)
            {
                pdc.IsActive = true;
                break;
            }
        }
        RefreshToolMode();
    }

    /// <summary>Deactivates the <see cref="PathDrawingController"/>.</summary>
    public void ExitPathDrawMode()
    {
        for (int i = 0; i < _controllers.Count; i++)
        {
            if (_controllers[i] is PathDrawingController pdc)
            {
                if (pdc.IsActive)
                {
                    pdc.IsActive = false;
                    pdc.OnDeactivated();
                }
                break;
            }
        }
        RefreshToolMode();
    }

    /// <summary>Activates the <see cref="DemolishController"/>.</summary>
    public void EnterDemolishMode()
    {
        for (int i = 0; i < _controllers.Count; i++)
        {
            if (_controllers[i] is DemolishController dc)
            {
                dc.IsActive = true;
                break;
            }
        }
        RefreshToolMode();
    }

    /// <summary>Deactivates the <see cref="DemolishController"/>.</summary>
    public void ExitDemolishMode()
    {
        for (int i = 0; i < _controllers.Count; i++)
        {
            if (_controllers[i] is DemolishController dc)
            {
                if (dc.IsActive)
                {
                    dc.IsActive = false;
                    dc.OnDeactivated();
                }
                break;
            }
        }
        RefreshToolMode();
    }

    // ── Per-frame ───────────────────────────────────────────────

    private void Update()
    {
        CursorGridPos = ScreenToGrid();

        EnsureSorted();
        float dt = Time.deltaTime;
        for (int i = 0; i < _sorted.Count; i++)
        {
            if (_sorted[i].IsActive)
            {
                _sorted[i].UpdateController(dt, CursorGridPos);
            }
        }

        RefreshToolMode();
    }

    // ── Dispatchers (called from InputAction callbacks) ─────────

    private bool IsUIBlocking => _uiFocusProvider != null
        && (_uiFocusProvider.IsPointerOverUI || _uiFocusProvider.IsModalOpen);

    private void DispatchLeftClickDown()
    {
        if (IsUIBlocking) return;
        // ScreenToGrid is safe inside callbacks; recompute for up-to-date position
        CursorGridPos = ScreenToGrid();
        var pos = CursorGridPos;
        EnsureSorted();
        for (int i = 0; i < _sorted.Count; i++)
        {
            var c = _sorted[i];
            if (!c.IsActive) continue;
            if (c.HandleLeftClickDown(pos)) return;
        }
    }

    private void DispatchLeftClickUp()
    {
        if (IsUIBlocking) return;
        var pos = CursorGridPos;
        EnsureSorted();
        for (int i = 0; i < _sorted.Count; i++)
        {
            var c = _sorted[i];
            if (!c.IsActive) continue;
            if (c.HandleLeftClickUp(pos)) return;
        }
    }

    private void DispatchRightClick()
    {
        if (IsUIBlocking) return;
        // ScreenToGrid is safe inside callbacks; recompute for up-to-date position
        CursorGridPos = ScreenToGrid();
        var pos = CursorGridPos;
        EnsureSorted();
        for (int i = 0; i < _sorted.Count; i++)
        {
            var c = _sorted[i];
            if (!c.IsActive) continue;
            if (c.HandleRightClick(pos)) return;
        }
    }

    private void DispatchRotate()
    {
        EnsureSorted();
        for (int i = 0; i < _sorted.Count; i++)
        {
            var c = _sorted[i];
            if (!c.IsActive) continue;
            if (c.HandleRotate()) return;
        }
    }

    private void DispatchCancel()
    {
        CancelDispatchedThisFrame = true;
        EnsureSorted();
        for (int i = 0; i < _sorted.Count; i++)
        {
            var c = _sorted[i];
            if (!c.IsActive) continue;
            if (c.HandleCancel())
            {
                RefreshToolMode();
                return;
            }
        }
        // Nothing consumed Escape — toggle game pause
        _onGamePauseToggle?.Invoke();
    }

    // ── Internals ───────────────────────────────────────────────

    private void EnsureSorted()
    {
        if (!_dirty) return;
        _sorted.Clear();
        _sorted.AddRange(_controllers);
        _sorted.Sort((a, b) => b.Priority.CompareTo(a.Priority));
        _dirty = false;
    }

    private void RefreshToolMode()
    {
        EnsureSorted();
        string? newMode = null;
        IInputController? newTopController = null;
        for (int i = 0; i < _sorted.Count; i++)
        {
            var c = _sorted[i];
            if (c.IsActive && c.CursorHint != null)
            {
                newMode = c.CursorHint;
                newTopController = c;
                break;
            }
        }

        // Notify the previous top controller if it was eclipsed or deactivated
        if (_previousTopController != null && _previousTopController != newTopController)
        {
            _previousTopController.OnDeactivated();
        }
        _previousTopController = newTopController;

        if (newMode != ActiveToolMode)
        {
            ActiveToolMode = newMode;
            var info = BuildToolModeInfo(newMode);
            ToolModeChanged?.Invoke(info);
            Debug.Log($"[Input] Tool mode: {newMode ?? "none"}");
        }
    }

    /// <summary>
    /// Aggregates shortcuts from all active controllers (priority order).
    /// Higher-priority controllers' action IDs shadow lower ones to prevent duplicates.
    /// </summary>
    public List<ShortcutDescriptor> GetActiveShortcuts()
    {
        _shortcutCache.Clear();
        _consumedActionIds.Clear();
        _consumedInputActions.Clear();
        EnsureSorted();

        for (int i = 0; i < _sorted.Count; i++)
        {
            var c = _sorted[i];
            if (!c.IsActive) continue;
            if (c is not IControllerWithShortcuts svc) continue;

            var shortcuts = svc.GetActiveShortcuts();
            for (int j = 0; j < shortcuts.Count; j++)
            {
                var s = shortcuts[j];
                if (!s.IsActive) continue;
                if (_consumedActionIds.Contains(s.ActionId)) continue;
                if (_consumedInputActions.Contains(s.InputActionName)) continue;

                _consumedActionIds.Add(s.ActionId);
                _consumedInputActions.Add(s.InputActionName);
                _shortcutCache.Add(s);
            }
        }

        return _shortcutCache;
    }

    private ToolModeInfo BuildToolModeInfo(string? modeName)
    {
        var shortcuts = GetActiveShortcuts();
        var snapshotList = new List<ShortcutDescriptor>(shortcuts.Count);
        for (int i = 0; i < shortcuts.Count; i++)
        {
            snapshotList.Add(shortcuts[i]);
        }

        string? displayName = modeName switch
        {
            "placement" => Get<StructurePlacementController>() is { } mpc && _structurePlacer != null
                ? _structurePlacer.SelectedCategory
                : "Structure",
            "path_draw" => "Draw Path",
            "demolish" => "Demolish",
            "debug" => "Debug Tools",
            _ => null
        };

        return new ToolModeInfo(modeName, displayName, 0, snapshotList);
    }

    // ── Phase 2: Dynamic Custom Action Routing ──────────────────

    private void BindCustomActions(IControllerWithCustomActions controller)
    {
        if (_inputActions == null) return;

        var actionNames = controller.AdditionalActionNames;
        for (int i = 0; i < actionNames.Count; i++)
        {
            string actionName = actionNames[i];
            var action = _inputActions.FindAction(actionName);
            if (action == null)
            {
                Debug.LogWarning($"[Input] Custom action '{actionName}' not found in input asset.");
                continue;
            }

            string capturedName = actionName;
            Action<InputAction.CallbackContext> callback = _ => DispatchCustomAction(capturedName);
            _customActionCallbacks[actionName] = callback;
            action.performed += callback;
        }
    }

    private void UnbindCustomActions(IControllerWithCustomActions controller)
    {
        if (_inputActions == null) return;

        var actionNames = controller.AdditionalActionNames;
        for (int i = 0; i < actionNames.Count; i++)
        {
            string actionName = actionNames[i];
            if (_customActionCallbacks.TryGetValue(actionName, out var callback))
            {
                var action = _inputActions.FindAction(actionName);
                if (action != null)
                {
                    action.performed -= callback;
                }
                _customActionCallbacks.Remove(actionName);
            }
        }
    }

    private void DispatchCustomAction(string actionName)
    {
        EnsureSorted();
        for (int i = 0; i < _sorted.Count; i++)
        {
            var c = _sorted[i];
            if (!c.IsActive) continue;
            if (c is IControllerWithCustomActions custom && custom.HandleCustomAction(actionName))
            {
                return;
            }
        }
    }

    // ── Reference to StructurePlacer for display name lookup ──────

    private StructurePlacer? _structurePlacer;

    public void SetStructurePlacer(StructurePlacer placer)
    {
        _structurePlacer = placer;
    }

    private GridPosRPG ScreenToGrid()
    {
        if (_camera == null)
        {
            return new GridPosRPG(
                (int)(_currentMousePos.x / gridCellSize),
                (int)(_currentMousePos.y / gridCellSize));
        }

        var ray = _camera.ScreenPointToRay(new Vector3(_currentMousePos.x, _currentMousePos.y, 0));
        if (_groundPlane.Raycast(ray, out float enter))
        {
            var wp = ray.GetPoint(enter);
            return new GridPosRPG(
                Mathf.RoundToInt(wp.x / gridCellSize),
                Mathf.RoundToInt(wp.z / gridCellSize));
        }
        return default;
    }

    private void OnDestroy()
    {
        // Input callbacks are lambdas captured in Initialize — clearing the
        // asset's action map on destruction is sufficient.
    }
}

}
