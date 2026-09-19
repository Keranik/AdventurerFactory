using UnityEngine;
using UnityEngine.UIElements;

namespace ForgeFlow.Presentation.Unity.UI.Components
{
    /// <summary>
    /// Themed tooltip that follows the mouse and displays text.
    /// Shown via <see cref="Show"/> and hidden via <see cref="Hide"/>.
    /// Positioned near the cursor within the UI Toolkit root.
    /// </summary>
    internal sealed class ForgeTooltip : ForgeStyledVisualElement
    {
        private readonly Label _textLabel;
        private bool _isVisible;

        public ForgeTooltip()
        {
            style.position = Position.Absolute;
            style.display = DisplayStyle.None;
            style.maxWidth = 300;

            _textLabel = new Label();
            Add(_textLabel);

            pickingMode = PickingMode.Ignore;
            _textLabel.pickingMode = PickingMode.Ignore;

            ApplyTheme();
        }

        public void Show(string text, Vector2 position)
        {
            _textLabel.text = text;
            style.left = position.x + 12f;
            style.top = position.y + 12f;
            style.display = DisplayStyle.Flex;
            _isVisible = true;
            BringToFront();
        }

        public void Show(string locKey)
        {
            _textLabel.text = L(locKey);
            style.display = DisplayStyle.Flex;
            _isVisible = true;
            BringToFront();
        }

        public void UpdatePosition(Vector2 position)
        {
            if (!_isVisible) return;
            style.left = position.x + 12f;
            style.top = position.y + 12f;
        }

        public void Hide()
        {
            style.display = DisplayStyle.None;
            _isVisible = false;
        }

        public override void ApplyTheme()
        {
            // Solid dark background for maximum readability
            style.backgroundColor = C("tooltip.bg");
            _textLabel.style.color = C("tooltip.text");
            _textLabel.style.fontSize = ThemeFontSmall;
            _textLabel.style.whiteSpace = WhiteSpace.Normal;

            // Accent border, tighter corners
            var radius = ThemeIsMinimalist ? 2 : 6;
            SetBorder(C("tooltip.border"), ThemeBorderWidth + 1, radius);

            // Generous padding for readability
            style.paddingTop = ThemePaddingSmall + 2;
            style.paddingBottom = ThemePaddingSmall + 2;
            style.paddingLeft = ThemePaddingNormal;
            style.paddingRight = ThemePaddingNormal;
        }

        // ── Fluent builder API ──

        public static TooltipBuilder Create()
            => new(new ForgeTooltip());

        internal sealed class TooltipBuilder : ForgeBuilder<TooltipBuilder, ForgeTooltip>
        {
            internal TooltipBuilder(ForgeTooltip el) : base(el) { }
        }
    }
}
