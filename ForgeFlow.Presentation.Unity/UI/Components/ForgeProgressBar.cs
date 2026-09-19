using UnityEngine;
using UnityEngine.UIElements;

namespace ForgeFlow.Presentation.Unity.UI.Components
{
    /// <summary>Themed progress bar with localized label, styled track, and colored fill.</summary>
    internal sealed class ForgeProgressBar : ForgeStyledVisualElement
    {
        private readonly ProgressBar _progressBar;
        private readonly string _locKey;
        private string? _colorKey;

        public ForgeProgressBar(string localizationKey, float min = 0f, float max = 100f)
        {
            _locKey = localizationKey;
            _progressBar = new ProgressBar
            {
                title = L(localizationKey),
                lowValue = min,
                highValue = max,
                value = 0f
            };

            Add(_progressBar);
            // Defer internal element styling until attached to panel
            _progressBar.RegisterCallback<AttachToPanelEvent>(_ => StyleInternalElements());
            ApplyTheme();
        }

        public float Value
        {
            get => _progressBar.value;
            set => _progressBar.value = value;
        }

        public string? ColorKey
        {
            get => _colorKey;
            set
            {
                _colorKey = value;
                var fill = _progressBar.Q(className: "unity-progress-bar__progress");
                if (fill != null && value != null)
                {
                    fill.style.backgroundColor = C(value);
                }
            }
        }

        public override void ApplyTheme()
        {
            _progressBar.title = L(_locKey);
            _progressBar.style.color = C("text.primary");
            _progressBar.style.fontSize = ThemeFontSmall;

            // Outer container: inset look with dark bg
            style.backgroundColor = Color.clear;
            style.marginTop = 2;
            style.marginBottom = 2;

            StyleInternalElements();
        }

        private void StyleInternalElements()
        {
            // Style the progress bar container (track)
            var container = _progressBar.Q(className: "unity-progress-bar__background");
            if (container != null)
            {
                container.style.backgroundColor = C("bg.primary");
                var radius = ThemeIsMinimalist ? 2 : 4;
                SetBorderOn(container, C("border.normal"), ThemeBorderWidth, radius);
                container.style.height = 18;
                container.style.overflow = Overflow.Hidden;
            }

            // Style the fill bar
            var fill = _progressBar.Q(className: "unity-progress-bar__progress");
            if (fill != null)
            {
                fill.style.backgroundColor = _colorKey != null ? C(_colorKey) : C("progress.fill");
                var fillRadius = ThemeIsMinimalist ? 1 : 3;
                fill.style.borderTopLeftRadius = fillRadius;
                fill.style.borderBottomLeftRadius = fillRadius;
                fill.style.marginTop = 1;
                fill.style.marginBottom = 1;
                fill.style.marginLeft = 1;
            }

            // Style the title text overlay
            var title = _progressBar.Q(className: "unity-progress-bar__title");
            if (title != null)
            {
                title.style.color = C("text.primary");
                title.style.fontSize = ThemeFontSmall;
                title.style.unityTextAlign = TextAnchor.MiddleCenter;
                title.style.unityFontStyleAndWeight = FontStyle.Bold;
            }
        }

        // ── Fluent builder API ──

        public static ProgressBarBuilder Create(string locKey, float min = 0f, float max = 100f)
            => new(new ForgeProgressBar(locKey, min, max));

        internal sealed class ProgressBarBuilder : ForgeBuilder<ProgressBarBuilder, ForgeProgressBar>
        {
            internal ProgressBarBuilder(ForgeProgressBar el) : base(el) { }
            public ProgressBarBuilder Value(float v) { _el.Value = v; return this; }
            public ProgressBarBuilder Color(string key)
            {
                var fill = _el._progressBar.Q(className: "unity-progress-bar__progress");
                if (fill != null) fill.style.backgroundColor = GetThemeColor(key);
                return this;
            }
        }
    }
}
