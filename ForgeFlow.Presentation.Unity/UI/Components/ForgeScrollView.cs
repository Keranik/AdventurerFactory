using UnityEngine.UIElements;

namespace ForgeFlow.Presentation.Unity.UI.Components
{
    /// <summary>Themed scroll view with styled scrollbar track and thumb.</summary>
    internal sealed class ForgeScrollView : ForgeStyledVisualElement
    {
        private readonly ScrollView _scrollView;

        public ForgeScrollView(ScrollViewMode mode = ScrollViewMode.Vertical)
        {
            _scrollView = new ScrollView(mode);
            _scrollView.style.flexGrow = 1;
            _scrollView.RegisterCallback<AttachToPanelEvent>(_ => StyleInternalElements());
            Add(_scrollView);
            ApplyTheme();
        }

        public void AddContent(VisualElement element) => _scrollView.Add(element);
        public void ClearContent() => _scrollView.Clear();

        public ScrollView Inner => _scrollView;

        public override void ApplyTheme()
        {
            style.backgroundColor = C("bg.primary");
            var radius = ThemeIsMinimalist ? 2 : 6;
            SetBorder(C("border.normal"), ThemeBorderWidth, radius);

            StyleInternalElements();
        }

        private void StyleInternalElements()
        {
            // Style vertical scrollbar
            var vScroller = _scrollView.Q(className: "unity-scroller--vertical");
            if (vScroller != null)
            {
                vScroller.style.width = ThemeIsMinimalist ? 8 : 10;
            }

            // Style scrollbar track
            var track = _scrollView.Q(className: "unity-base-slider__tracker");
            if (track != null)
            {
                track.style.backgroundColor = C("scrollbar.track");
                SetBorderOn(track, C("border.normal"), 0, 3);
            }

            // Style scrollbar dragger (thumb)
            var dragger = _scrollView.Q(className: "unity-base-slider__dragger");
            if (dragger != null)
            {
                dragger.style.backgroundColor = C("scrollbar.thumb");
                var thumbRadius = ThemeIsMinimalist ? 2 : 4;
                dragger.style.borderTopLeftRadius = thumbRadius;
                dragger.style.borderTopRightRadius = thumbRadius;
                dragger.style.borderBottomLeftRadius = thumbRadius;
                dragger.style.borderBottomRightRadius = thumbRadius;
            }
        }

        // ── Fluent builder API ──

        public static ScrollViewBuilder Create(ScrollViewMode mode = ScrollViewMode.Vertical)
            => new(new ForgeScrollView(mode));

        internal sealed class ScrollViewBuilder : ForgeBuilder<ScrollViewBuilder, ForgeScrollView>
        {
            internal ScrollViewBuilder(ForgeScrollView el) : base(el) { }
            public ScrollViewBuilder Content(VisualElement el) { _el.AddContent(el); return this; }
        }
    }
}
