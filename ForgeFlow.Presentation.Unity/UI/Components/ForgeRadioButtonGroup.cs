using UnityEngine;
using UnityEngine.UIElements;

namespace ForgeFlow.Presentation.Unity.UI.Components
{
    /// <summary>Themed radio button group with styled options and accent selection indicator.</summary>
    internal sealed class ForgeRadioButtonGroup : ForgeStyledVisualElement
    {
        private readonly RadioButtonGroup _group;
        private readonly string _locKey;

        public event Action<int>? SelectionChanged;

        public ForgeRadioButtonGroup(string localizationKey, List<string> choices, int defaultIndex = 0)
        {
            _locKey = localizationKey;

            var localizedChoices = new List<string>();
            foreach (var choice in choices)
            {
                localizedChoices.Add(L(choice));
            }

            _group = new RadioButtonGroup(L(localizationKey), localizedChoices);
            _group.value = defaultIndex;
            _group.RegisterValueChangedCallback(evt => SelectionChanged?.Invoke(evt.newValue));
            _group.RegisterCallback<AttachToPanelEvent>(_ => StyleInternalElements());

            Add(_group);
            ApplyTheme();
        }

        public int Value
        {
            get => _group.value;
            set => _group.value = value;
        }

        public override void ApplyTheme()
        {
            _group.label = L(_locKey);
            _group.style.color = C("text.primary");
            _group.style.fontSize = ThemeFontNormal;

            style.backgroundColor = C("bg.secondary");
            var radius = ThemeIsMinimalist ? 2 : 6;
            SetBorder(C("border.normal"), ThemeBorderWidth, radius);
            SetPadding(ThemePaddingSmall + 2);

            StyleInternalElements();
        }

        private void StyleInternalElements()
        {
            // Style the group label
            var label = _group.Q<Label>(className: "unity-base-field__label");
            if (label != null)
            {
                label.style.color = C("text.accent");
                label.style.unityFontStyleAndWeight = FontStyle.Bold;
                label.style.marginBottom = ThemePaddingSmall;
            }

            // Style individual radio buttons
            var radioButtons = _group.Query<RadioButton>().ToList();
            foreach (var rb in radioButtons)
            {
                var checkmark = rb.Q(className: "unity-toggle__checkmark");
                if (checkmark != null)
                {
                    checkmark.style.backgroundColor = C("input.bg");
                    SetBorderOn(checkmark, C("border.normal"), ThemeBorderWidth + 1, 8);
                    checkmark.style.width = 16;
                    checkmark.style.height = 16;
                }

                rb.style.marginBottom = 2;
            }
        }

        // ── Fluent builder API ──

        public static RadioGroupBuilder Create(string locKey, List<string> choices, int defaultIndex = 0)
            => new(new ForgeRadioButtonGroup(locKey, choices, defaultIndex));

        internal sealed class RadioGroupBuilder : ForgeBuilder<RadioGroupBuilder, ForgeRadioButtonGroup>
        {
            internal RadioGroupBuilder(ForgeRadioButtonGroup el) : base(el) { }
            public RadioGroupBuilder Value(int v) { _el.Value = v; return this; }
            public RadioGroupBuilder OnSelectionChanged(Action<int> cb) { _el.SelectionChanged += cb; return this; }
        }
    }
}
