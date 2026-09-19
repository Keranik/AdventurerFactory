using UnityEngine.UIElements;

namespace ForgeFlow.Presentation.Unity.UI.Components
{
    /// <summary>Themed dropdown with localized label, styled input, and hover highlight.</summary>
    internal sealed class ForgeDropdown : ForgeStyledVisualElement
    {
        private readonly DropdownField _dropdown;
        private readonly string _locKey;

        public event Action<string>? SelectionChanged;

        public ForgeDropdown(string localizationKey, List<string> choices, int defaultIndex = 0)
        {
            _locKey = localizationKey;
            _dropdown = new DropdownField(L(localizationKey), choices, defaultIndex);
            _dropdown.RegisterValueChangedCallback(evt => SelectionChanged?.Invoke(evt.newValue));
            _dropdown.RegisterCallback<AttachToPanelEvent>(_ => StyleInternalElements());

            Add(_dropdown);
            ApplyTheme();
        }

        public string Value
        {
            get => _dropdown.value;
            set => _dropdown.value = value;
        }

        public int Index
        {
            get => _dropdown.index;
            set => _dropdown.index = value;
        }

        public void SetChoices(List<string> choices)
        {
            _dropdown.choices = choices;
        }

        public override void ApplyTheme()
        {
            _dropdown.label = L(_locKey);
            _dropdown.style.color = C("text.primary");
            _dropdown.style.fontSize = ThemeFontNormal;

            style.backgroundColor = C("bg.secondary");
            var radius = ThemeIsMinimalist ? 2 : 6;
            SetBorder(C("border.normal"), ThemeBorderWidth, radius);
            SetPadding(ThemePaddingSmall + 2);

            StyleInternalElements();
        }

        private void StyleInternalElements()
        {
            // Style the dropdown input area
            var input = _dropdown.Q(className: "unity-base-popup-field__input");
            if (input != null)
            {
                input.style.backgroundColor = C("input.bg");
                var inputRadius = ThemeIsMinimalist ? 2 : 4;
                SetBorderOn(input, C("border.normal"), ThemeBorderWidth, inputRadius);
                SetPaddingOn(input, ThemePaddingSmall);
            }

            // Style the dropdown arrow
            var arrow = _dropdown.Q(className: "unity-base-popup-field__arrow");
            if (arrow != null)
            {
                arrow.style.unityBackgroundImageTintColor = C("text.accent");
            }

            // Style the label
            var label = _dropdown.Q<Label>(className: "unity-base-field__label");
            if (label != null)
            {
                label.style.color = C("text.secondary");
                label.style.minWidth = 60;
            }
        }

        // ── Fluent builder API ──

        public static DropdownBuilder Create(string locKey, List<string> choices, int defaultIndex = 0)
            => new(new ForgeDropdown(locKey, choices, defaultIndex));

        internal sealed class DropdownBuilder : ForgeBuilder<DropdownBuilder, ForgeDropdown>
        {
            internal DropdownBuilder(ForgeDropdown el) : base(el) { }
            public DropdownBuilder Choices(List<string> c) { _el.SetChoices(c); return this; }
            public DropdownBuilder OnSelectionChanged(Action<string> cb) { _el.SelectionChanged += cb; return this; }
        }
    }
}
