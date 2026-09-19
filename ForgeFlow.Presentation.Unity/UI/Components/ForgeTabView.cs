using UnityEngine;
using UnityEngine.UIElements;

namespace ForgeFlow.Presentation.Unity.UI.Components
{
    /// <summary>
    /// Themed tab view built from a row of styled tab buttons and content panels.
    /// Active tab gets accent underline and raised appearance.
    /// </summary>
    internal sealed class ForgeTabView : ForgeStyledVisualElement
    {
        private readonly VisualElement _tabBar;
        private readonly VisualElement _tabBarSeparator;
        private readonly VisualElement _contentContainer;
        private readonly List<ForgeTab> _tabs = new();
        private int _activeIndex = -1;

        public event Action<int>? TabChanged;

        public ForgeTabView()
        {
            style.flexDirection = FlexDirection.Column;
            style.flexGrow = 1;

            _tabBar = new VisualElement();
            _tabBar.style.flexDirection = FlexDirection.Row;
            _tabBar.style.flexShrink = 0;
            _tabBar.style.backgroundColor = C("bg.header");
            Add(_tabBar);

            // Separator line under tab bar
            _tabBarSeparator = new VisualElement();
            _tabBarSeparator.style.height = 2;
            _tabBarSeparator.style.flexShrink = 0;
            Add(_tabBarSeparator);

            _contentContainer = new VisualElement();
            _contentContainer.style.flexGrow = 1;
            Add(_contentContainer);

            ApplyTheme();
        }

        public void AddTab(ForgeTab tab)
        {
            var index = _tabs.Count;
            _tabs.Add(tab);

            var tabButton = new Button(() => SetActiveTab(index))
            {
                text = tab.Title
            };
            tabButton.style.flexGrow = 1;
            tabButton.style.fontSize = ThemeFontNormal;
            tabButton.style.unityTextAlign = TextAnchor.MiddleCenter;
            tabButton.style.paddingTop = ThemePaddingSmall + 2;
            tabButton.style.paddingBottom = ThemePaddingSmall + 2;
            tabButton.style.paddingLeft = ThemePaddingNormal;
            tabButton.style.paddingRight = ThemePaddingNormal;
            tabButton.style.borderBottomWidth = 3;
            tabButton.style.borderTopWidth = 0;
            tabButton.style.borderLeftWidth = 0;
            tabButton.style.borderRightWidth = 0;

            // Hover effect
            tabButton.RegisterCallback<MouseEnterEvent>(_ =>
            {
                if (_tabs.IndexOf(tab) != _activeIndex)
                {
                    tabButton.style.backgroundColor = C("bg.elevated");
                }
            });
            tabButton.RegisterCallback<MouseLeaveEvent>(_ =>
            {
                if (_tabs.IndexOf(tab) != _activeIndex)
                {
                    tabButton.style.backgroundColor = C("tab.inactive");
                }
            });

            tab.TabButton = tabButton;
            _tabBar.Add(tabButton);

            if (_tabs.Count == 1)
            {
                SetActiveTab(0);
            }
        }

        public void SetActiveTab(int index)
        {
            if (index < 0 || index >= _tabs.Count) return;

            _activeIndex = index;
            _contentContainer.Clear();
            _contentContainer.Add(_tabs[index].Content);

            for (int i = 0; i < _tabs.Count; i++)
            {
                var btn = _tabs[i].TabButton;
                if (btn == null) continue;

                if (i == index)
                {
                    btn.style.backgroundColor = C("tab.active");
                    btn.style.color = C("text.accent");
                    btn.style.borderBottomColor = C("accent.primary");
                    btn.style.unityFontStyleAndWeight = FontStyle.Bold;
                }
                else
                {
                    btn.style.backgroundColor = C("tab.inactive");
                    btn.style.color = C("text.secondary");
                    btn.style.borderBottomColor = C("border.normal");
                    btn.style.unityFontStyleAndWeight = FontStyle.Normal;
                }
            }

            TabChanged?.Invoke(index);
        }

        public int ActiveIndex => _activeIndex;

        public override void ApplyTheme()
        {
            style.backgroundColor = C("bg.primary");
            var radius = ThemeIsMinimalist ? 2 : 6;
            SetBorder(C("border.normal"), ThemeBorderWidth, radius);

            _tabBar.style.backgroundColor = C("bg.header");
            _tabBarSeparator.style.backgroundColor = C("accent.primary");

            // Re-apply active state
            if (_activeIndex >= 0)
            {
                SetActiveTab(_activeIndex);
            }
        }

        // ── Fluent builder API ──

        public static TabViewBuilder Create()
            => new(new ForgeTabView());

        internal sealed class TabViewBuilder : ForgeBuilder<TabViewBuilder, ForgeTabView>
        {
            internal TabViewBuilder(ForgeTabView el) : base(el) { }
            public TabViewBuilder Tab(ForgeTab tab) { _el.AddTab(tab); return this; }
            public TabViewBuilder OnTabChanged(Action<int> cb) { _el.TabChanged += cb; return this; }
        }
    }

    /// <summary>A single tab's data: title and content panel.</summary>
    internal sealed class ForgeTab
    {
        public string Title { get; }
        public VisualElement Content { get; }
        internal Button? TabButton { get; set; }

        public ForgeTab(string localizationKey, VisualElement content)
        {
            Title = ForgeStyledVisualElement.GetLocalizedText(localizationKey);
            Content = content;
        }

        public static ForgeTab Create(string locKey, VisualElement content)
            => new(locKey, content);
    }
}
