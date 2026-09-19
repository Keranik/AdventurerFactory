using UnityEngine;
using UnityEngine.UIElements;

namespace ForgeFlow.Presentation.Unity.UI.Components
{
    /// <summary>Themed foldout with styled header, accent toggle arrow, and indented content.</summary>
    internal sealed class ForgeFoldout : ForgeStyledVisualElement
    {
        private readonly Foldout _foldout;
        private readonly string _locKey;

        public ForgeFoldout(string localizationKey, bool initialOpen = true)
        {
            _locKey = localizationKey;
            _foldout = new Foldout
            {
                text = L(localizationKey),
                value = initialOpen
            };
            _foldout.RegisterCallback<AttachToPanelEvent>(_ => StyleInternalElements());

            Add(_foldout);
            ApplyTheme();
        }

        public bool IsOpen
        {
            get => _foldout.value;
            set => _foldout.value = value;
        }

        public void AddContent(VisualElement element) => _foldout.Add(element);
        public void ClearContent() => _foldout.Clear();

        public override void ApplyTheme()
        {
            _foldout.text = L(_locKey);
            _foldout.style.color = C("text.primary");
            _foldout.style.fontSize = ThemeFontNormal;

            style.backgroundColor = C("bg.secondary");
            var radius = ThemeIsMinimalist ? 2 : 6;
            SetBorder(C("border.normal"), ThemeBorderWidth, radius);
            SetPadding(ThemePaddingSmall + 2);

            StyleInternalElements();
        }

        private void StyleInternalElements()
        {
            // Style the toggle header
            var toggle = _foldout.Q<Toggle>(className: "unity-foldout__toggle");
            if (toggle != null)
            {
                toggle.style.backgroundColor = C("bg.header");
                toggle.style.marginBottom = ThemePaddingSmall;
                SetPaddingOn(toggle, ThemePaddingSmall);
                var toggleRadius = ThemeIsMinimalist ? 1 : 4;
                SetBorderOn(toggle, C("border.normal"), ThemeBorderWidth, toggleRadius);
            }

            // Style the checkmark (arrow indicator)
            var checkmark = _foldout.Q(className: "unity-toggle__checkmark");
            if (checkmark != null)
            {
                checkmark.style.unityBackgroundImageTintColor = C("accent.primary");
            }

            // Style the toggle label text
            var label = toggle?.Q<Label>();
            if (label != null)
            {
                label.style.color = C("text.accent");
                label.style.unityFontStyleAndWeight = FontStyle.Bold;
            }

            // Indent content area
            var content = _foldout.Q(className: "unity-foldout__content");
            if (content != null)
            {
                content.style.paddingLeft = ThemePaddingNormal;
            }
        }

        // ── Fluent builder API ──

        public static FoldoutBuilder Create(string locKey, bool initialOpen = true)
            => new(new ForgeFoldout(locKey, initialOpen));

        internal sealed class FoldoutBuilder : ForgeBuilder<FoldoutBuilder, ForgeFoldout>
        {
            internal FoldoutBuilder(ForgeFoldout el) : base(el) { }
            public FoldoutBuilder Title(string text) { _el._foldout.text = text; return this; }
            public FoldoutBuilder Open(bool o) { _el.IsOpen = o; return this; }
            public FoldoutBuilder Content(VisualElement el) { _el.AddContent(el); return this; }
        }
    }
}
