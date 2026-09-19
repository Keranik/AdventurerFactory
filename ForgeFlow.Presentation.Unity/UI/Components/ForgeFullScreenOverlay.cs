using UnityEngine;
using UnityEngine.UIElements;

namespace ForgeFlow.Presentation.Unity.UI.Components
{
    /// <summary>
    /// Full-screen overlay component for menus, modals, and dialogs.
    /// Automatically configures position-absolute 100%×100% layout with
    /// centered content alignment and theme-aware background color.
    /// Survives theme refreshes by re-applying all overlay styles in <see cref="ApplyTheme"/>.
    /// </summary>
    internal class ForgeFullScreenOverlay : ForgeStyledVisualElement
    {
        private readonly VisualElement _contentArea;
        private readonly string? _bgColorKey;
        private readonly Color? _bgColorOverride;

        /// <summary>Centered container where panel content is added.</summary>
        public VisualElement ContentArea => _contentArea;

        /// <summary>
        /// Creates a full-screen overlay with a theme-resolved background color.
        /// </summary>
        /// <param name="backgroundColorKey">Theme color key (e.g. "bg.primary").</param>
        public ForgeFullScreenOverlay(string backgroundColorKey = "bg.primary")
        {
            _bgColorKey = backgroundColorKey;
            _contentArea = new VisualElement();
            _contentArea.style.alignItems = Align.Center;
            Add(_contentArea);
            ApplyOverlayStructure();
            RegisterPointerBlockers();
            ApplyTheme();
        }

        /// <summary>
        /// Creates a full-screen overlay with a fixed background color (e.g. semi-transparent black).
        /// </summary>
        /// <param name="backgroundColor">Fixed RGBA color.</param>
        public ForgeFullScreenOverlay(Color backgroundColor)
        {
            _bgColorOverride = backgroundColor;
            _contentArea = new VisualElement();
            _contentArea.style.alignItems = Align.Center;
            Add(_contentArea);
            ApplyOverlayStructure();
            RegisterPointerBlockers();
            ApplyTheme();
        }

        public void Show() { style.display = DisplayStyle.Flex; }
        public void Hide() { style.display = DisplayStyle.None; }

        /// <summary>
        /// Consume pointer events so clicks on the overlay do not propagate
        /// through to the world input layer (defense-in-depth).
        /// </summary>
        private void RegisterPointerBlockers()
        {
            RegisterCallback<PointerDownEvent>(e => e.StopPropagation());
            RegisterCallback<PointerUpEvent>(e => e.StopPropagation());
            RegisterCallback<PointerMoveEvent>(e => e.StopPropagation());
        }

        private void ApplyOverlayStructure()
        {
            style.position = Position.Absolute;
            style.width = new Length(100, LengthUnit.Percent);
            style.height = new Length(100, LengthUnit.Percent);
            style.justifyContent = Justify.Center;
            style.alignItems = Align.Center;
            SetPadding(0);
            SetBorder(Color.clear, 0, 0);
        }

        public override void ApplyTheme()
        {
            style.backgroundColor = _bgColorOverride ?? C(_bgColorKey ?? "bg.primary");
            ApplyOverlayStructure();
        }
    }
}
