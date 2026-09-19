using ForgeFlow.Presentation.Unity.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace ForgeFlow.Presentation.Unity.UI.Components
{
    /// <summary>
    /// Specifies which side of an anchor element a panel should dock to.
    /// </summary>
    internal enum DockSide { Left, Right, Top, Bottom }

    /// <summary>
    /// Themed draggable, resizable window panel.
    /// Features: title bar with close button, drag handle, resize corners.
    /// </summary>
    internal sealed class ForgePanel : ForgeStyledVisualElement
    {
        private readonly VisualElement _titleBar;
        private readonly Label _titleLabel;
        private readonly Button _pinButton;
        private readonly Button _closeButton;
        private readonly VisualElement _titleSeparator;
        private readonly VisualElement _contentContainer;
        private readonly VisualElement _resizeHandle;
        private readonly Label _resizeGrip;
        private readonly string _locKey;
        private readonly string _panelId;

        private bool _isDragging;
        private bool _isResizing;
        private Vector2 _dragOffset;
        private bool _isDraggable = true;
        private bool _isResizable = true;
        private bool _isPinned;

        // ── Transient dialog / docking state ──
        private VisualElement? _dockAnchor;
        private DockSide _dockSide;
        private float _dockGap;

        public event Action? Closed;
        public event Action<bool>? PinToggled;
        public event Action<float, float, float, float>? LayoutChanged;

        public string PanelId => _panelId;
        public VisualElement ContentContainer => _contentContainer;

        /// <summary>Whether this panel is pinned (keeps content when a new selection is made).</summary>
        public bool IsPinned
        {
            get => _isPinned;
            set
            {
                if (_isPinned == value) { return; }
                _isPinned = value;
                _pinButton.text = _isPinned ? "\ud83d\udccc" : "\u25cb";
                ApplyPinButtonStyle();
                PinToggled?.Invoke(_isPinned);
            }
        }

        public ForgePanel(string panelId, string localizationKey, float width = 300f, float height = 200f)
        {
            _panelId = panelId;
            _locKey = localizationKey;

            style.position = Position.Absolute;
            style.width = width;
            style.height = height;
            style.flexDirection = FlexDirection.Column;

            // Title bar — dark header with centered title and close button
            _titleBar = new VisualElement();
            _titleBar.style.flexDirection = FlexDirection.Row;
            _titleBar.style.alignItems = Align.Center;
            _titleBar.style.height = 32;
            _titleBar.style.flexShrink = 0;

            // Left spacer to balance pin + close button width for visual centering
            var leftSpacer = new VisualElement();
            leftSpacer.style.width = 56;
            leftSpacer.style.flexShrink = 0;
            _titleBar.Add(leftSpacer);

            _titleLabel = new Label(L(localizationKey));
            _titleLabel.style.flexGrow = 1;
            _titleLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            _titleBar.Add(_titleLabel);

            // Pin button — toggles IsPinned state
            _pinButton = new Button(() => { IsPinned = !IsPinned; })
            { text = "\u25cb" };
            _pinButton.style.width = 28;
            _pinButton.style.height = 28;
            _pinButton.style.flexShrink = 0;
            _pinButton.style.unityTextAlign = TextAnchor.MiddleCenter;
            _titleBar.Add(_pinButton);

            _closeButton = new Button(() => { Closed?.Invoke(); style.display = DisplayStyle.None; })
            { text = "✕" };
            _closeButton.style.width = 28;
            _closeButton.style.height = 28;
            _closeButton.style.flexShrink = 0;
            _closeButton.style.unityTextAlign = TextAnchor.MiddleCenter;
            _closeButton.RegisterCallback<MouseEnterEvent>(_ =>
            {
                _closeButton.style.backgroundColor = C("status.error");
                _closeButton.style.color = C("text.primary");
            });
            _closeButton.RegisterCallback<MouseLeaveEvent>(_ =>
            {
                _closeButton.style.backgroundColor = Color.clear;
                _closeButton.style.color = C("text.secondary");
            });
            _titleBar.Add(_closeButton);
            Add(_titleBar);

            // Accent separator line under title bar
            _titleSeparator = new VisualElement();
            _titleSeparator.style.height = 2;
            _titleSeparator.style.flexShrink = 0;
            Add(_titleSeparator);

            // Content area
            _contentContainer = new VisualElement();
            _contentContainer.style.flexGrow = 1;
            _contentContainer.style.overflow = Overflow.Hidden;
            Add(_contentContainer);

            // Resize handle (bottom-right corner) with grip icon
            _resizeHandle = new VisualElement();
            _resizeHandle.style.position = Position.Absolute;
            _resizeHandle.style.right = 0;
            _resizeHandle.style.bottom = 0;
            _resizeHandle.style.width = 18;
            _resizeHandle.style.height = 18;
            _resizeHandle.style.cursor = StyleKeyword.Auto;
            _resizeHandle.style.alignItems = Align.Center;
            _resizeHandle.style.justifyContent = Justify.Center;

            _resizeGrip = new Label("⋱");
            _resizeGrip.pickingMode = PickingMode.Ignore;
            _resizeHandle.Add(_resizeGrip);
            Add(_resizeHandle);

            // Drag support on title bar
            _titleBar.RegisterCallback<PointerDownEvent>(OnTitlePointerDown);
            _titleBar.RegisterCallback<PointerMoveEvent>(OnTitlePointerMove);
            _titleBar.RegisterCallback<PointerUpEvent>(OnTitlePointerUp);

            // Resize support
            _resizeHandle.RegisterCallback<PointerDownEvent>(OnResizePointerDown);
            _resizeHandle.RegisterCallback<PointerMoveEvent>(OnResizePointerMove);
            _resizeHandle.RegisterCallback<PointerUpEvent>(OnResizePointerUp);

            // Consume all pointer events on the panel body so clicks don't pass
            // through to the world (e.g. EntitySelectionController inspecting tiles).
            RegisterCallback<PointerDownEvent>(e => e.StopPropagation());
            RegisterCallback<PointerUpEvent>(e => e.StopPropagation());
            RegisterCallback<PointerMoveEvent>(e => e.StopPropagation());

            ApplyTheme();
        }

        public bool IsDraggable
        {
            get => _isDraggable;
            set => _isDraggable = value;
        }

        public bool IsResizable
        {
            get => _isResizable;
            set
            {
                _isResizable = value;
                _resizeHandle.style.display = value ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        public void SetPosition(float x, float y)
        {
            style.left = x;
            style.top = y;
        }

        public void Show()
        {
            style.display = DisplayStyle.Flex;
        }

        public void Hide()
        {
            style.display = DisplayStyle.None;
        }

        public override void ApplyTheme()
        {
            // Panel body: solid dark background, rounded corners, clean border
            style.backgroundColor = C("bg.primary");
            var radius = ThemeIsMinimalist ? 2 : 8;
            SetBorder(C("border.normal"), ThemeBorderWidth + 1, radius);

            // Title bar: darker header strip with padding
            _titleBar.style.backgroundColor = C("bg.header");
            _titleBar.style.paddingLeft = ThemePaddingNormal;
            _titleBar.style.paddingRight = ThemePaddingSmall;
            // Match top-only radius so corners align with panel border
            _titleBar.style.borderTopLeftRadius = Math.Max(radius - 2, 0);
            _titleBar.style.borderTopRightRadius = Math.Max(radius - 2, 0);

            // Title label: centered, bold, accent colored
            _titleLabel.style.color = C("text.accent");
            _titleLabel.style.fontSize = ThemeFontNormal + 1;
            _titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            _titleLabel.text = L(_locKey);

            // Pin button: transparent bg, accent when pinned
            ApplyPinButtonStyle();
            SetBorderOn(_pinButton, Color.clear, 0, radius);
            SetPaddingOn(_pinButton, 0);

            // Close button: transparent bg, muted text, red on hover (handled by callbacks)
            _closeButton.style.backgroundColor = Color.clear;
            _closeButton.style.color = C("text.secondary");
            _closeButton.style.fontSize = ThemeFontNormal;
            SetBorderOn(_closeButton, Color.clear, 0, radius);
            SetPaddingOn(_closeButton, 0);

            // Accent separator between title and content
            _titleSeparator.style.backgroundColor = C("accent.primary");

            // Resize grip: subtle accent text
            _resizeGrip.style.color = C("text.disabled");
            _resizeGrip.style.fontSize = ThemeFontSmall;
            _resizeHandle.style.backgroundColor = Color.clear;

            // Content area padding
            _contentContainer.style.paddingTop = ThemePaddingNormal;
            _contentContainer.style.paddingBottom = ThemePaddingNormal;
            _contentContainer.style.paddingLeft = ThemePaddingNormal;
            _contentContainer.style.paddingRight = ThemePaddingNormal;
        }

        // --- Drag ---
        private void OnTitlePointerDown(PointerDownEvent evt)
        {
            if (!_isDraggable || evt.button != 0) return;
            _isDragging = true;
            _dragOffset = new Vector2(evt.localPosition.x, evt.localPosition.y);
            _titleBar.CapturePointer(evt.pointerId);
            evt.StopPropagation();
        }

        private void OnTitlePointerMove(PointerMoveEvent evt)
        {
            if (!_isDragging) return;
            var newX = resolvedStyle.left + (evt.localPosition.x - _dragOffset.x);
            var newY = resolvedStyle.top + (evt.localPosition.y - _dragOffset.y);
            style.left = newX;
            style.top = newY;
            evt.StopPropagation();
        }

        private void OnTitlePointerUp(PointerUpEvent evt)
        {
            if (!_isDragging) return;
            _isDragging = false;
            _titleBar.ReleasePointer(evt.pointerId);
            NotifyLayoutChanged();
            evt.StopPropagation();
        }

        // --- Resize ---
        private void OnResizePointerDown(PointerDownEvent evt)
        {
            if (!_isResizable || evt.button != 0) return;
            _isResizing = true;
            _resizeHandle.CapturePointer(evt.pointerId);
            evt.StopPropagation();
        }

        private void OnResizePointerMove(PointerMoveEvent evt)
        {
            if (!_isResizing) return;
            var newWidth = Mathf.Max(100f, evt.localPosition.x + resolvedStyle.width - _resizeHandle.resolvedStyle.width);
            var newHeight = Mathf.Max(60f, evt.localPosition.y + resolvedStyle.height - _resizeHandle.resolvedStyle.height);
            style.width = newWidth;
            style.height = newHeight;
            evt.StopPropagation();
        }

        private void OnResizePointerUp(PointerUpEvent evt)
        {
            if (!_isResizing) return;
            _isResizing = false;
            _resizeHandle.ReleasePointer(evt.pointerId);
            NotifyLayoutChanged();
            evt.StopPropagation();
        }

        private void NotifyLayoutChanged()
        {
            LayoutChanged?.Invoke(resolvedStyle.left, resolvedStyle.top, resolvedStyle.width, resolvedStyle.height);
        }

        private void ApplyPinButtonStyle()
        {
            _pinButton.style.backgroundColor = _isPinned ? C("accent.primary") : Color.clear;
            _pinButton.style.color = _isPinned ? C("text.primary") : C("text.secondary");
            _pinButton.style.fontSize = ThemeFontNormal;
        }

        // ── Transient dialog helpers ──

        /// <summary>
        /// Configures this panel as a transient element (popup/dialog) that
        /// auto-registers/unregisters with the given tracker when added to or
        /// removed from the visual tree. Also disables resize by default.
        /// </summary>
        internal void ConfigureAsTransient(ITransientElementTracker? tracker, string trackingId)
        {
            IsResizable = false;

            if (tracker != null)
            {
                var capturedId = trackingId;
                var capturedTracker = tracker;
                RegisterCallback<AttachToPanelEvent>(_ =>
                    capturedTracker.RegisterTransientElement(capturedId, this));
                RegisterCallback<DetachFromPanelEvent>(_ =>
                    capturedTracker.UnregisterTransientElement(capturedId));
            }
        }

        /// <summary>
        /// Configures docking so the panel positions itself adjacent to
        /// <paramref name="anchor"/> after the first layout pass.
        /// The panel starts hidden and becomes visible once positioned.
        /// </summary>
        internal void ConfigureDocking(VisualElement anchor, DockSide side, float gap)
        {
            _dockAnchor = anchor;
            _dockSide = side;
            _dockGap = gap;

            style.visibility = Visibility.Hidden;
            style.position = Position.Absolute;

            RegisterCallbackOnce<GeometryChangedEvent>(_ => ApplyDocking());
        }

        /// <summary>
        /// Computes and applies the docked position relative to the stored anchor.
        /// Falls back to the opposite side if the panel would overflow.
        /// </summary>
        private void ApplyDocking()
        {
            if (_dockAnchor == null) { return; }

            var anchorBounds = _dockAnchor.worldBound;
            float w = resolvedStyle.width;
            float h = resolvedStyle.height;
            float x, y;

            switch (_dockSide)
            {
                case DockSide.Left:
                    x = anchorBounds.x - w - _dockGap;
                    if (x < 0) { x = anchorBounds.xMax + _dockGap; }
                    y = anchorBounds.y;
                    break;
                case DockSide.Right:
                    x = anchorBounds.xMax + _dockGap;
                    y = anchorBounds.y;
                    break;
                case DockSide.Top:
                    x = anchorBounds.x;
                    y = anchorBounds.y - h - _dockGap;
                    if (y < 0) { y = anchorBounds.yMax + _dockGap; }
                    break;
                case DockSide.Bottom:
                    x = anchorBounds.x;
                    y = anchorBounds.yMax + _dockGap;
                    break;
                default:
                    x = anchorBounds.x;
                    y = anchorBounds.y;
                    break;
            }

            style.left = x;
            style.top = y;
            style.right = StyleKeyword.Auto;
            style.visibility = Visibility.Visible;
        }

        // ── Fluent builder API ──

        public static PanelBuilder Create(string panelId, string locKey, float width = 300f, float height = 200f)
            => new(new ForgePanel(panelId, locKey, width, height));

        internal sealed class PanelBuilder : ForgeBuilder<PanelBuilder, ForgePanel>
        {
            internal PanelBuilder(ForgePanel el) : base(el) { }
            public PanelBuilder Title(string text) { _el._titleLabel.text = text; return this; }
            public PanelBuilder Draggable(bool d) { _el.IsDraggable = d; return this; }
            public PanelBuilder Resizable(bool r) { _el.IsResizable = r; return this; }
            public PanelBuilder OnClosed(Action cb) { _el.Closed += cb; return this; }
            public PanelBuilder At(float x, float y) { _el.SetPosition(x, y); return this; }
            public PanelBuilder Content(VisualElement el) { _el.ContentContainer.Add(el); return this; }

            /// <summary>
            /// Marks this panel as a transient element that auto-tracks pointer-over
            /// state with the given <see cref="ITransientElementTracker"/>.
            /// </summary>
            public PanelBuilder AsTransient(ITransientElementTracker? tracker, string trackingId)
            {
                _el.ConfigureAsTransient(tracker, trackingId);
                return this;
            }

            /// <summary>
            /// Positions this panel adjacent to <paramref name="anchor"/> on the
            /// specified <paramref name="side"/> after the first layout pass.
            /// </summary>
            public PanelBuilder DockTo(VisualElement anchor, DockSide side, float gap = 8f)
            {
                _el.ConfigureDocking(anchor, side, gap);
                return this;
            }
        }
    }
}
