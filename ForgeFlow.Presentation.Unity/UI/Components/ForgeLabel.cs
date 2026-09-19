using UnityEngine.UIElements;

namespace ForgeFlow.Presentation.Unity.UI.Components
{
    /// <summary>Themed label with localized text and size variants. Supports fluent builder API.</summary>
    internal sealed class ForgeLabel : ForgeStyledVisualElement
    {
        private readonly Label _label;
        private readonly string _locKey;
        private readonly ForgeLabelSize _size;
        private readonly bool _isRaw;
        private string? _colorKeyOverride;
        private int _fontSizeOverride;

        public ForgeLabel(string localizationKey, ForgeLabelSize size = ForgeLabelSize.Normal)
            : this(localizationKey, size, false) { }

        private ForgeLabel(string text, ForgeLabelSize size, bool isRaw)
        {
            _locKey = isRaw ? "" : text;
            _size = size;
            _isRaw = isRaw;
            _label = new Label(isRaw ? text : L(text));

            Add(_label);
            ApplyTheme();
        }

        /// <summary>Creates a label with literal (non-localized) text.</summary>
        public static ForgeLabel Raw(string text, ForgeLabelSize size = ForgeLabelSize.Normal)
            => new(text, size, true);

        public string Text
        {
            get => _label.text;
            set => _label.text = value;
        }

        public string? ColorKey
        {
            get => _colorKeyOverride;
            set
            {
                _colorKeyOverride = value;
                _label.style.color = C(value ?? (_size == ForgeLabelSize.Header ? "text.accent" : "text.primary"));
            }
        }

        public void SetRawText(string text) => _label.text = text;

        // ── Fluent builder API ──

        public static LabelBuilder Create(string locKey, ForgeLabelSize size = ForgeLabelSize.Normal)
            => new(new ForgeLabel(locKey, size));

        public static LabelBuilder CreateRaw(string text, ForgeLabelSize size = ForgeLabelSize.Normal)
            => new(new ForgeLabel(text, size, true));

        internal sealed class LabelBuilder : ForgeBuilder<LabelBuilder, ForgeLabel>
        {
            internal LabelBuilder(ForgeLabel el) : base(el) { }
            public LabelBuilder Text(string text) { _el.Text = text; return this; }
            public LabelBuilder FontSize(int s) { _el._fontSizeOverride = s; _el._label.style.fontSize = s; return this; }
            public LabelBuilder Color(string key) { _el._colorKeyOverride = key; _el._label.style.color = GetThemeColor(key); return this; }
            public LabelBuilder TextAlign(UnityEngine.TextAnchor a) { _el._label.style.unityTextAlign = a; return this; }
            public LabelBuilder WhiteSpace(WhiteSpace ws) { _el._label.style.whiteSpace = ws; return this; }
            public LabelBuilder Bold() { _el._label.style.unityFontStyleAndWeight = UnityEngine.FontStyle.Bold; return this; }
        }

        public override void ApplyTheme()
        {
            if (!_isRaw) _label.text = L(_locKey);

            var fontSize = _fontSizeOverride > 0 ? _fontSizeOverride : _size switch
            {
                ForgeLabelSize.Small => ThemeFontSmall,
                ForgeLabelSize.Normal => ThemeFontNormal,
                ForgeLabelSize.Large => ThemeFontLarge,
                ForgeLabelSize.Header => ThemeFontHeader,
                _ => ThemeFontNormal
            };

            var textColor = _colorKeyOverride ?? (_size == ForgeLabelSize.Header ? "text.accent" : "text.primary");

            _label.style.fontSize = fontSize;
            _label.style.color = C(textColor);

            // Headers get bold text and extra bottom margin
            if (_size == ForgeLabelSize.Header)
            {
                _label.style.unityFontStyleAndWeight = UnityEngine.FontStyle.Bold;
                _label.style.marginBottom = ThemePaddingSmall;
            }
            else if (_size == ForgeLabelSize.Large)
            {
                _label.style.unityFontStyleAndWeight = UnityEngine.FontStyle.Bold;
            }

            // Small labels get slightly tighter line height for compactness
            if (_size == ForgeLabelSize.Small)
            {
                _label.style.marginTop = 1;
                _label.style.marginBottom = 1;
            }
        }
    }

    internal enum ForgeLabelSize
    {
        Small,
        Normal,
        Large,
        Header
    }
}
