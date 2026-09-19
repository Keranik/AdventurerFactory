using UnityEngine;
using UnityEngine.UIElements;

namespace ForgeFlow.Presentation.Unity.UI.Components
{
    /// <summary>Themed slider with localized label, styled track/thumb, and value display.</summary>
    internal sealed class ForgeSlider : ForgeStyledVisualElement
    {
        private readonly Slider _slider;
        private readonly Label _headerLabel;
        private readonly Label _valueLabel;
        private readonly string _locKey;

        public event Action<float>? ValueChanged;

        public ForgeSlider(string localizationKey, float min = 0f, float max = 1f, float initial = 0.5f)
        {
            _locKey = localizationKey;
            style.flexDirection = FlexDirection.Column;

            var header = new VisualElement();
            header.style.flexDirection = FlexDirection.Row;
            header.style.justifyContent = Justify.SpaceBetween;
            header.style.marginBottom = 2;

            _headerLabel = new Label(L(localizationKey));
            _valueLabel = new Label(initial.ToString("F2"));

            header.Add(_headerLabel);
            header.Add(_valueLabel);
            Add(header);

            _slider = new Slider(min, max) { value = initial };
            _slider.RegisterValueChangedCallback(evt =>
            {
                _valueLabel.text = evt.newValue.ToString("F2");
                ValueChanged?.Invoke(evt.newValue);
            });
            _slider.RegisterCallback<AttachToPanelEvent>(_ => StyleInternalElements());

            Add(_slider);
            ApplyTheme();
        }

        public float Value
        {
            get => _slider.value;
            set
            {
                _slider.value = value;
                _valueLabel.text = value.ToString("F2");
            }
        }

        public override void ApplyTheme()
        {
            style.backgroundColor = C("bg.secondary");
            var radius = ThemeIsMinimalist ? 2 : 6;
            SetBorder(C("border.normal"), ThemeBorderWidth, radius);
            SetPadding(ThemePaddingSmall + 2);

            _headerLabel.style.color = C("text.primary");
            _headerLabel.style.fontSize = ThemeFontNormal;

            // Value label: accent colored, bold for standout
            _valueLabel.style.color = C("text.accent");
            _valueLabel.style.fontSize = ThemeFontNormal;
            _valueLabel.style.unityFontStyleAndWeight = FontStyle.Bold;

            StyleInternalElements();
        }

        private void StyleInternalElements()
        {
            // Style the slider track (dark inset)
            var tracker = _slider.Q(className: "unity-base-slider__tracker");
            if (tracker != null)
            {
                tracker.style.backgroundColor = C("bg.primary");
                SetBorderOn(tracker, C("border.normal"), ThemeBorderWidth, 3);
                tracker.style.height = 6;
            }

            // Style the slider dragger (accent colored thumb)
            var dragger = _slider.Q(className: "unity-base-slider__dragger");
            if (dragger != null)
            {
                dragger.style.backgroundColor = C("accent.primary");
                dragger.style.width = 16;
                dragger.style.height = 16;
                dragger.style.borderTopLeftRadius = 8;
                dragger.style.borderTopRightRadius = 8;
                dragger.style.borderBottomLeftRadius = 8;
                dragger.style.borderBottomRightRadius = 8;
                SetBorderOn(dragger, C("border.focused"), ThemeBorderWidth, 8);
            }

            // Style the fill area
            var fill = _slider.Q(className: "unity-base-slider__drag-container");
            if (fill != null)
            {
                fill.style.paddingTop = 0;
                fill.style.paddingBottom = 0;
            }
        }

        // ── Fluent builder API ──

        public static SliderBuilder Create(string locKey, float min = 0f, float max = 1f, float initial = 0.5f)
            => new(new ForgeSlider(locKey, min, max, initial));

        internal sealed class SliderBuilder : ForgeBuilder<SliderBuilder, ForgeSlider>
        {
            internal SliderBuilder(ForgeSlider el) : base(el) { }
            public SliderBuilder Value(float v) { _el.Value = v; return this; }
            public SliderBuilder Min(float m) { _el._slider.lowValue = m; return this; }
            public SliderBuilder Max(float m) { _el._slider.highValue = m; return this; }
            public SliderBuilder OnValueChanged(Action<float> cb) { _el.ValueChanged += cb; return this; }
        }
    }
}
