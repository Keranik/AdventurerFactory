using UnityEngine;
using UnityEngine.UIElements;

namespace ForgeFlow.Presentation.Unity.UI.Components
{
    /// <summary>Themed button with hover, active, and disabled states. Supports Primary/Secondary/Danger variants.</summary>
    internal sealed class ForgeButton : ForgeStyledVisualElement
    {
        private readonly Button _button;
        private readonly string _locKey;
        private bool _isDisabled;
        private bool _hasCustomText;
        private ButtonVariant _variant = ButtonVariant.Default;

        public event Action? Clicked;

        public ForgeButton(string localizationKey, Action? onClick = null)
        {
            _locKey = localizationKey;
            _button = new Button(() =>
            {
                if (!_isDisabled) Clicked?.Invoke();
            })
            {
                text = L(localizationKey)
            };

            // Ensure button text is always centered
            _button.style.unityTextAlign = TextAnchor.MiddleCenter;
            _button.style.justifyContent = Justify.Center;
            _button.style.alignItems = Align.Center;
            _button.style.flexGrow = 1;

            if (onClick != null) Clicked += onClick;

            _button.RegisterCallback<MouseEnterEvent>(_ => ApplyHover());
            _button.RegisterCallback<MouseLeaveEvent>(_ => ApplyTheme());
            _button.RegisterCallback<PointerDownEvent>(_ => ApplyPressed());
            _button.RegisterCallback<PointerUpEvent>(_ => ApplyHover());

            Add(_button);
            ApplyTheme();
        }

        public string Text
        {
            get => _button.text;
            set
            {
                _button.text = value;
                _hasCustomText = true;
            }
        }

        public bool Disabled
        {
            get => _isDisabled;
            set
            {
                _isDisabled = value;
                _button.SetEnabled(!value);
                ApplyTheme();
            }
        }

        public ButtonVariant Variant
        {
            get => _variant;
            set
            {
                _variant = value;
                ApplyTheme();
            }
        }

        public override void ApplyTheme()
        {
            var bgColor = _isDisabled ? C("btn.disabled") : _variant switch
            {
                ButtonVariant.Primary => C("accent.primary"),
                ButtonVariant.Danger => C("status.error"),
                ButtonVariant.Secondary => C("bg.tertiary"),
                _ => C("btn.normal")
            };
            var textColor = _isDisabled ? C("text.disabled") : _variant switch
            {
                ButtonVariant.Primary or ButtonVariant.Danger => C("text.primary"),
                _ => C("btn.text")
            };

            _button.style.backgroundColor = bgColor;
            _button.style.color = textColor;
            _button.style.fontSize = ThemeFontNormal;
            _button.style.unityTextAlign = TextAnchor.MiddleCenter;
            if (_variant == ButtonVariant.Primary || _variant == ButtonVariant.Danger)
            {
                _button.style.unityFontStyleAndWeight = FontStyle.Bold;
            }
            else
            {
                _button.style.unityFontStyleAndWeight = FontStyle.Normal;
            }
            if (!_hasCustomText)
            {
                _button.text = L(_locKey);
            }

            // Raised 3D border for tactile feel (lighter top/left, darker bottom/right)
            var borderBase = _isDisabled ? C("btn.disabled") : bgColor;
            var radius = ThemeIsMinimalist ? 2 : 6;
            if (_isDisabled)
            {
                SetBorderOn(_button, C("border.normal"), ThemeBorderWidth, radius);
            }
            else
            {
                SetRaisedBorderOn(_button, borderBase, ThemeBorderWidth + 1, radius);
            }

            _button.style.paddingTop = ThemePaddingSmall + 1;
            _button.style.paddingBottom = ThemePaddingSmall + 1;
            _button.style.paddingLeft = ThemePaddingNormal + 2;
            _button.style.paddingRight = ThemePaddingNormal + 2;

            // Minimum height for consistent sizing
            _button.style.minHeight = 28;

            // Outer element should be transparent / borderless
            style.backgroundColor = Color.clear;
        }

        private void ApplyHover()
        {
            if (_isDisabled) return;
            var hoverColor = _variant switch
            {
                ButtonVariant.Primary => C("btn.active"),
                ButtonVariant.Danger => C("status.warning"),
                _ => C("btn.hover")
            };
            _button.style.backgroundColor = hoverColor;
            var radius = ThemeIsMinimalist ? 2 : 6;
            SetBorderOn(_button, C("border.hover"), ThemeBorderWidth + 1, radius);
        }

        private void ApplyPressed()
        {
            if (_isDisabled) return;
            var pressedColor = _variant switch
            {
                ButtonVariant.Primary => C("btn.pressed"),
                ButtonVariant.Danger => C("btn.pressed"),
                _ => C("bg.secondary")
            };
            _button.style.backgroundColor = pressedColor;
            var radius = ThemeIsMinimalist ? 2 : 6;
            SetInsetBorderOn(_button, pressedColor, ThemeBorderWidth + 1, radius);
        }

        // ── Fluent builder API ──

        public static ButtonBuilder Create(string locKey, Action? onClick = null)
            => new(new ForgeButton(locKey, onClick));

        internal sealed class ButtonBuilder : ForgeBuilder<ButtonBuilder, ForgeButton>
        {
            internal ButtonBuilder(ForgeButton el) : base(el) { }
            public ButtonBuilder Text(string text) { _el.Text = text; return this; }
            public ButtonBuilder FontSize(int s) { _el._button.style.fontSize = s; return this; }
            public ButtonBuilder Disabled(bool d) { _el.Disabled = d; return this; }
            public ButtonBuilder OnClick(Action cb) { _el.Clicked += cb; return this; }
            public ButtonBuilder Primary() { _el.Variant = ButtonVariant.Primary; return this; }
            public ButtonBuilder Secondary() { _el.Variant = ButtonVariant.Secondary; return this; }
            public ButtonBuilder Danger() { _el.Variant = ButtonVariant.Danger; return this; }
        }
    }

    internal enum ButtonVariant
    {
        Default,
        Primary,
        Secondary,
        Danger
    }
}
