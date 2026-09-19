using ForgeFlow.Core.Entities;
using ForgeFlow.Core.Events;
using ForgeFlow.Core.Systems;
using ForgeFlow.Presentation.Unity.UI.Components;
using UnityEngine;
using UnityEngine.UIElements;

namespace ForgeFlow.Presentation.Unity.UI
{
    /// <summary>
    /// Central window manager — the single source of truth for all UI windows,
    /// open/close state, z-order layering, layout persistence, and theme application.
    /// Plain class (not MonoBehaviour). Created and owned by <see cref="FactoryEntryPoint"/>.
    /// Does NOT subscribe to events directly; the entry point coordinates state transitions.
    /// </summary>
    internal sealed class UIManager : IUIFocusProvider, ITransientElementTracker, IDisposable
    {
        private readonly Dictionary<string, WindowEntry> _windows = new();
        private readonly List<WindowEntry> _pinnedInstances = new();
        private readonly Dictionary<UILayer, VisualElement> _layerContainers = new();
        private readonly EventBus _eventBus;
        private readonly SettingsManager _settings;
        private VisualElement _uiRoot = null!;

        // ── IUIFocusProvider state (event-driven, zero-alloc queries) ──
        private readonly HashSet<string> _pointerOverWindows = new();

        /// <inheritdoc />
        public bool IsPointerOverUI => _pointerOverWindows.Count > 0;

        /// <inheritdoc />
        public bool HasKeyboardFocus => false; // TODO: track FocusIn/FocusOut on text fields

        /// <inheritdoc />
        public bool IsModalOpen
        {
            get
            {
                foreach (var kvp in _windows)
                {
                    if (kvp.Value.Descriptor.IsModal && kvp.Value.Window.IsVisible)
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        /// <summary>All registered window entries (for testing/debug).</summary>
        public IReadOnlyDictionary<string, WindowEntry> Windows => _windows;

        /// <summary>Number of currently pinned (detached) window instances.</summary>
        public int PinnedInstanceCount => _pinnedInstances.Count;

        public UIManager(EventBus eventBus, SettingsManager settings)
        {
            _eventBus = eventBus;
            _settings = settings;
        }

        /// <summary>
        /// Initializes the UIManager with the root VisualElement from the UIDocument.
        /// Creates layer containers for z-ordering.
        /// </summary>
        public void Initialize(VisualElement uiRoot)
        {
            _uiRoot = uiRoot;

            // Create layer containers in z-order (lower layers added first)
            foreach (UILayer layer in Enum.GetValues(typeof(UILayer)))
            {
                var container = ForgeContainer.Create()
                    .Name($"Layer_{layer}")
                    .Position(Position.Absolute)
                    .WidthPercent(100).HeightPercent(100)
                    .PickingMode(PickingMode.Ignore)
                    .Build();
                _layerContainers[layer] = container;
                _uiRoot.Add(container);
            }
        }

        /// <summary>
        /// Registers a window with its descriptor. Attaches it to the correct layer container.
        /// </summary>
        public void Register(WindowDescriptor descriptor, IUIWindow window)
        {
            if (_windows.ContainsKey(descriptor.WindowId))
            {
                Debug.LogWarning($"[UIManager] Window '{descriptor.WindowId}' already registered. Skipping.");
                return;
            }

            var entry = new WindowEntry(descriptor, window);
            _windows[descriptor.WindowId] = entry;

            // Attach to the correct layer container
            if (_layerContainers.TryGetValue(descriptor.Layer, out var container))
            {
                container.Add(window.Root);
            }

            // Start hidden
            window.Hide();

            // Wire pointer enter/leave for IUIFocusProvider tracking
            AttachPointerTracking(descriptor.WindowId, window.Root);

            // Wire close button to go through UIManager's Close flow
            if (window.Root is ForgePanel panel)
            {
                var capturedId = descriptor.WindowId;
                panel.Closed += () => Close(capturedId);
            }

            // Restore saved layout if applicable
            if (descriptor.SaveLayout)
            {
                RestoreLayout(descriptor.WindowId, window);
            }
        }

        /// <summary>Opens a window by ID. Publishes WindowOpenedEvent.</summary>
        public void Open(string windowId, object? context = null)
        {
            if (!_windows.TryGetValue(windowId, out var entry))
            {
                return;
            }

            entry.Window.Show(context);
            _eventBus.Publish(new WindowOpenedEvent(windowId));
        }

        /// <summary>Closes a window by ID. Publishes WindowClosedEvent.</summary>
        public void Close(string windowId)
        {
            if (!_windows.TryGetValue(windowId, out var entry))
            {
                return;
            }

            entry.Window.Hide();
            // Safety: clear pointer-over state for this window
            _pointerOverWindows.Remove(windowId);
            _eventBus.Publish(new WindowClosedEvent(windowId));
        }

        /// <summary>Toggles a window open/closed.</summary>
        public void Toggle(string windowId, object? context = null)
        {
            if (!_windows.TryGetValue(windowId, out var entry))
            {
                return;
            }

            if (entry.Window.IsVisible)
            {
                Close(windowId);
            }
            else
            {
                Open(windowId, context);
            }
        }

        /// <summary>Closes all windows, optionally filtered by layer.</summary>
        public void CloseAll(UILayer? layer = null)
        {
            foreach (var kvp in _windows)
            {
                if (layer.HasValue && kvp.Value.Descriptor.Layer != layer.Value)
                {
                    continue;
                }

                if (kvp.Value.Window.IsVisible)
                {
                    kvp.Value.Window.Hide();
                    _pointerOverWindows.Remove(kvp.Key);
                }
            }
        }

        /// <summary>Gets a typed window by ID, or null if not found.</summary>
        public T? GetWindow<T>(string windowId) where T : class, IUIWindow
        {
            if (_windows.TryGetValue(windowId, out var entry))
            {
                return entry.Window as T;
            }
            return null;
        }

        /// <summary>Gets the IUIWindow by ID, or null if not found.</summary>
        public IUIWindow? GetWindow(string windowId)
        {
            if (_windows.TryGetValue(windowId, out var entry))
            {
                return entry.Window;
            }
            return null;
        }

        /// <summary>
        /// Shows the correct windows for a given game state.
        /// Returns true if gameplay is active (3D scene, input should be enabled).
        /// </summary>
        public bool ShowWindowsForState(GameState state)
        {
            // Hide all flow panels first
            CloseAll();

            switch (state)
            {
                case GameState.MainMenu:
                    Open(WindowIds.MainMenu);
                    return false;

                case GameState.NewGameSetup:
                    Open(WindowIds.NewGameSetup);
                    return false;

                case GameState.Playing:
                    Open(WindowIds.TopBarHUD);
                    Open(WindowIds.TutorialOverlay);
                    Open(WindowIds.GameplayToolbar);
                    Open(WindowIds.ToolModeIndicator);
                    // Refresh gameplay windows
                    RefreshWindow(WindowIds.TutorialOverlay);
                    RefreshWindow(WindowIds.GameplayToolbar);
                    return true;

                case GameState.Paused:
                    Open(WindowIds.PauseMenu);
                    return false;

                case GameState.GameOver:
                    Open(WindowIds.GameOver);
                    return false;

                case GameState.Victory:
                    Open(WindowIds.Victory);
                    return false;
            }

            return false;
        }

        /// <summary>
        /// Pins the current window for the given windowId (moves it to the pinned list)
        /// and registers the replacement window as the new primary for that ID.
        /// The old window keeps its visual state and can be independently closed.
        /// </summary>
        public void PinAndReplaceWindow(string windowId, IUIWindow replacement)
        {
            if (!_windows.TryGetValue(windowId, out var existing))
            {
                return;
            }

            // Move old entry to pinned list
            _pinnedInstances.Add(existing);

            // Wire the old window's panel Closed event to auto-remove from pinned
            if (existing.Window.Root is ForgePanel pinnedPanel)
            {
                pinnedPanel.Closed += () => ClosePinnedInstance(existing.Window);
            }

            // Register replacement as the new primary
            var newEntry = new WindowEntry(existing.Descriptor, replacement);
            _windows[windowId] = newEntry;

            if (_layerContainers.TryGetValue(existing.Descriptor.Layer, out var container))
            {
                container.Add(replacement.Root);
            }

            replacement.Hide();
        }

        /// <summary>
        /// Removes and disposes a pinned window instance.
        /// </summary>
        public void ClosePinnedInstance(IUIWindow window)
        {
            for (int i = _pinnedInstances.Count - 1; i >= 0; i--)
            {
                if (_pinnedInstances[i].Window == window)
                {
                    var entry = _pinnedInstances[i];
                    entry.Window.Hide();
                    entry.Window.Dispose();

                    if (_layerContainers.TryGetValue(entry.Descriptor.Layer, out var container))
                    {
                        container.Remove(entry.Window.Root);
                    }

                    _pinnedInstances.RemoveAt(i);
                    _eventBus.Publish(new WindowClosedEvent(entry.Descriptor.WindowId + "_pinned"));
                    return;
                }
            }
        }

        /// <summary>Ticks all visible ITickableWindow instances (primary + pinned).</summary>
        public void Tick(float deltaTime)
        {
            foreach (var kvp in _windows)
            {
                if (kvp.Value.Window.IsVisible && kvp.Value.Window is ITickableWindow tickable)
                {
                    tickable.Tick(deltaTime);
                }
            }

            for (int i = 0; i < _pinnedInstances.Count; i++)
            {
                var entry = _pinnedInstances[i];
                if (entry.Window.IsVisible && entry.Window is ITickableWindow pinnedTickable)
                {
                    pinnedTickable.Tick(deltaTime);
                }
            }
        }

        /// <summary>Refreshes a specific window by ID.</summary>
        public void RefreshWindow(string windowId)
        {
            if (_windows.TryGetValue(windowId, out var entry))
            {
                entry.Window.Refresh();
            }
        }

        /// <summary>Re-applies theme colors to all registered windows.</summary>
        public void ApplyThemeToAll()
        {
            foreach (var kvp in _windows)
            {
                kvp.Value.Window.ApplyTheme();
            }
        }

        /// <summary>Saves current layout for windows with SaveLayout=true.</summary>
        public void SaveAllLayouts()
        {
            foreach (var kvp in _windows)
            {
                if (kvp.Value.Descriptor.SaveLayout)
                {
                    SaveLayout(kvp.Key, kvp.Value.Window);
                }
            }
        }

        /// <summary>
        /// Toggles HUD customization mode — makes saveable panels draggable/resizable.
        /// </summary>
        public void ToggleCustomizeMode()
        {
            foreach (var kvp in _windows)
            {
                if (kvp.Value.Descriptor.SaveLayout && kvp.Value.Window.Root is ForgePanel forgePanel)
                {
                    forgePanel.IsDraggable = !forgePanel.IsDraggable;
                    forgePanel.IsResizable = !forgePanel.IsResizable;
                }
            }
        }

        /// <summary>Resets all saveable window layouts to defaults.</summary>
        public void ResetLayout()
        {
            _settings.Settings.WindowLayouts.Clear();
        }

        public void Dispose()
        {
            foreach (var kvp in _windows)
            {
                kvp.Value.Window.Dispose();
            }
            _windows.Clear();
            _pointerOverWindows.Clear();

            for (int i = _pinnedInstances.Count - 1; i >= 0; i--)
            {
                _pinnedInstances[i].Window.Dispose();
            }
            _pinnedInstances.Clear();
        }

        // ── Layout Persistence ─────────────────────────────────

        private void RestoreLayout(string windowId, IUIWindow window)
        {
            var layout = FindLayout(windowId);
            if (layout == null)
            {
                return;
            }

            if (window.Root is ForgePanel forgePanel)
            {
                forgePanel.SetPosition(layout.X, layout.Y);
                forgePanel.style.width = layout.Width;
                forgePanel.style.height = layout.Height;

                // Wire layout change callback
                forgePanel.LayoutChanged += (x, y, w, h) =>
                    SaveLayout(windowId, window);
            }
        }

        private void SaveLayout(string windowId, IUIWindow window)
        {
            var root = window.Root;
            float x = root.resolvedStyle.left;
            float y = root.resolvedStyle.top;
            float w = root.resolvedStyle.width;
            float h = root.resolvedStyle.height;

            var existing = FindLayout(windowId);
            if (existing != null)
            {
                existing.X = x;
                existing.Y = y;
                existing.Width = w;
                existing.Height = h;
                existing.Visible = window.IsVisible;
            }
            else
            {
                _settings.Settings.WindowLayouts.Add(new WindowLayoutData
                {
                    WindowId = windowId,
                    X = x,
                    Y = y,
                    Width = w,
                    Height = h,
                    Visible = window.IsVisible
                });
            }
        }

        private WindowLayoutData? FindLayout(string windowId)
        {
            foreach (var layout in _settings.Settings.WindowLayouts)
            {
                if (layout.WindowId == windowId)
                {
                    return layout;
                }
            }
            return null;
        }

        // ── Transient Element Tracking (ITransientElementTracker) ───

        /// <inheritdoc />
        public void RegisterTransientElement(string trackingId, VisualElement element)
        {
            AttachPointerTracking(trackingId, element);
        }

        /// <inheritdoc />
        public void UnregisterTransientElement(string trackingId)
        {
            _pointerOverWindows.Remove(trackingId);
        }

        // ── Pointer Tracking Internals ───────────────────────────

        private void AttachPointerTracking(string windowId, VisualElement root)
        {
            // MonoBehaviour adapters use invisible placeholder roots (PickingMode.Ignore)
            // that never receive pointer events — skip them to avoid pointless registrations.
            if (root.pickingMode == PickingMode.Ignore)
            {
                return;
            }

            var capturedId = windowId;
            root.RegisterCallback<PointerEnterEvent>(_ => _pointerOverWindows.Add(capturedId));
            root.RegisterCallback<PointerLeaveEvent>(_ => _pointerOverWindows.Remove(capturedId));
        }

        /// <summary>
        /// Stored entry for a registered window.
        /// </summary>
        internal sealed class WindowEntry
        {
            public WindowDescriptor Descriptor { get; }
            public IUIWindow Window { get; }

            public WindowEntry(WindowDescriptor descriptor, IUIWindow window)
            {
                Descriptor = descriptor;
                Window = window;
            }
        }
    }

    /// <summary>
    /// Adapter that wraps a legacy MonoBehaviour-based panel as an <see cref="IUIWindow"/>.
    /// Used for DebugHUD and ResearchPanel which use UGUI/IMGUI.
    /// </summary>
    internal sealed class MonoBehaviourWindowAdapter : IUIWindow
    {
        private readonly string _windowId;
        private readonly MonoBehaviour _component;
        private readonly ForgeContainer _root;
        private readonly Action<bool>? _setVisible;
        private bool _isVisible;

        public string WindowId => _windowId;
        public VisualElement Root => _root;
        public bool IsVisible => _isVisible;

        /// <summary>
        /// Wraps a MonoBehaviour component. The optional setVisible callback
        /// controls the component's actual visibility (e.g. SetActive, enabled).
        /// </summary>
        public MonoBehaviourWindowAdapter(string windowId, MonoBehaviour component, Action<bool>? setVisible = null)
        {
            _windowId = windowId;
            _component = component;
            _setVisible = setVisible;
            // Invisible placeholder root — MonoBehaviour manages its own rendering
            _root = ForgeContainer.Create()
                .Name(windowId)
                .Display(DisplayStyle.None)
                .PickingMode(PickingMode.Ignore)
                .Build();
        }

        public void Show(object? context = null)
        {
            _isVisible = true;
            _component.enabled = true;
            _setVisible?.Invoke(true);
        }

        public void Hide()
        {
            _isVisible = false;
            _component.enabled = false;
            _setVisible?.Invoke(false);
        }

        public void Refresh() { }
        public void ApplyTheme() { }
        public void Dispose() { }
    }
}
