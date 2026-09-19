using UnityEngine.UIElements;

namespace ForgeFlow.Presentation.Unity.UI.Components
{
    /// <summary>Themed toggle with localized label and styled checkbox.</summary>
    internal sealed class ForgeToggle : ForgeStyledVisualElement
    {
        private readonly Toggle _toggle;
        private readonly string _locKey;

        public event Action<bool>? ValueChanged;

        public ForgeToggle(string localizationKey, bool initialValue = false)
        {
            _locKey = localizationKey;
            _toggle = new Toggle(L(localizationKey)) { value = initialValue };
            _toggle.RegisterValueChangedCallback(evt => ValueChanged?.Invoke(evt.newValue));
            _toggle.RegisterCallback<AttachToPanelEvent>(_ => StyleInternalElements());

            Add(_toggle);
            ApplyTheme();
        }

        public bool Value
        {
            get => _toggle.value;
            set => _toggle.value = value;
        }

        public override void ApplyTheme()
        {
            _toggle.label = L(_locKey);
            _toggle.style.color = C("text.primary");
            _toggle.style.fontSize = ThemeFontNormal;

            style.backgroundColor = C("bg.secondary");
            var radius = ThemeIsMinimalist ? 2 : 6;
            SetBorder(C("border.normal"), ThemeBorderWidth, radius);
            SetPadding(ThemePaddingSmall + 2);

            StyleInternalElements();
        }

        private void StyleInternalElements()
        {
            // Style the checkmark box
            var checkmark = _toggle.Q(className: "unity-toggle__checkmark");
            if (checkmark != null)
            {
                checkmark.style.backgroundColor = C("input.bg");
                var cbRadius = ThemeIsMinimalist ? 1 : 3;
                SetBorderOn(checkmark, C("border.normal"), ThemeBorderWidth + 1, cbRadius);
                checkmark.style.width = 18;
                checkmark.style.height = 18;
            }

            // Style the label portion
            var label = _toggle.Q<Label>();
            if (label != null)
            {
                label.style.color = C("text.primary");
                label.style.marginLeft = ThemePaddingSmall;
            }
        }

        // ── Fluent builder API ──

        public static ToggleBuilder Create(string locKey, bool initialValue = false)
            => new(new ForgeToggle(locKey, initialValue));

        internal sealed class ToggleBuilder : ForgeBuilder<ToggleBuilder, ForgeToggle>
        {
            internal ToggleBuilder(ForgeToggle el) : base(el) { }
            public ToggleBuilder Value(bool v) { _el.Value = v; return this; }
            public ToggleBuilder OnValueChanged(Action<bool> cb) { _el.ValueChanged += cb; return this; }
        }
    }
}
