using UnityEngine;
using UnityEngine.UIElements;

namespace ForgeFlow.Presentation.Unity.UI.Components
{
    /// <summary>
    /// Compact status badge: colored pill with text label.
    /// Used for entity status tags, structure state indicators, worker conditions.
    /// </summary>
    internal sealed class ForgeStatusBadge : ForgeStyledVisualElement
    {
        private readonly Label _label;
        private string _colorKey = "accent.primary";

        public ForgeStatusBadge(string text = "", string colorKey = "accent.primary")
        {
            _colorKey = colorKey;

            style.flexDirection = FlexDirection.Row;
            style.alignItems = Align.Center;
            style.alignSelf = Align.FlexStart;

            _label = new Label(text);
            Add(_label);

            ApplyTheme();
        }

        public string Text
        {
            get => _label.text;
            set => _label.text = value;
        }

        public string ColorKey
        {
            get => _colorKey;
            set
            {
                _colorKey = value;
                ApplyTheme();
            }
        }

        public override void ApplyTheme()
        {
            // Pill background with accent color
            var bgColor = C(_colorKey);
            // Darken background slightly and use the accent as border for better readability
            style.backgroundColor = new Color(bgColor.r * 0.3f, bgColor.g * 0.3f, bgColor.b * 0.3f, 0.9f);
            SetBorder(bgColor, ThemeBorderWidth, ThemeIsMinimalist ? 4 : 10);

            // Horizontal-heavy padding for compact pill shape
            style.paddingTop = 2;
            style.paddingBottom = 2;
            style.paddingLeft = ThemePaddingNormal;
            style.paddingRight = ThemePaddingNormal;

            // Text: small, bold, accent colored
            _label.style.color = bgColor;
            _label.style.fontSize = ThemeFontSmall;
            _label.style.unityFontStyleAndWeight = FontStyle.Bold;
            _label.style.letterSpacing = 1;
        }

        // ── Fluent builder API ──

        public static StatusBadgeBuilder Create(string text = "", string colorKey = "accent.primary")
            => new(new ForgeStatusBadge(text, colorKey));

        internal sealed class StatusBadgeBuilder : ForgeBuilder<StatusBadgeBuilder, ForgeStatusBadge>
        {
            internal StatusBadgeBuilder(ForgeStatusBadge el) : base(el) { }
            public StatusBadgeBuilder Text(string text) { _el.Text = text; return this; }
            public StatusBadgeBuilder Color(string key) { _el.ColorKey = key; return this; }
        }
    }
}
