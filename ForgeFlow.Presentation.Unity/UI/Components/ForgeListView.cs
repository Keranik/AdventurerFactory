using UnityEngine;
using UnityEngine.UIElements;

namespace ForgeFlow.Presentation.Unity.UI.Components
{
    /// <summary>Themed list view with alternating row backgrounds, selection highlight, and hover state.</summary>
    internal sealed class ForgeListView : ForgeStyledVisualElement
    {
        private readonly ListView _listView;
        private readonly List<string> _items = new();

        public event Action<int>? SelectionChanged;

        public ForgeListView(int itemHeight = 28)
        {
            _listView = new ListView
            {
                fixedItemHeight = itemHeight,
                selectionType = SelectionType.Single
            };

            _listView.makeItem = () =>
            {
                var row = new VisualElement();
                row.style.flexDirection = FlexDirection.Row;
                row.style.alignItems = Align.Center;
                row.style.paddingLeft = ThemePaddingNormal;
                row.style.paddingRight = ThemePaddingNormal;
                row.style.paddingTop = 2;
                row.style.paddingBottom = 2;

                var label = new Label();
                label.style.color = C("text.primary");
                label.style.fontSize = ThemeFontNormal;
                label.style.unityTextAlign = TextAnchor.MiddleLeft;
                label.style.flexGrow = 1;
                row.Add(label);

                // Hover effect
                row.RegisterCallback<MouseEnterEvent>(_ =>
                {
                    row.style.backgroundColor = C("bg.elevated");
                });
                row.RegisterCallback<MouseLeaveEvent>(_ =>
                {
                    // Reset to alternating color — will be re-applied on rebind
                    row.style.backgroundColor = StyleKeyword.Null;
                });

                return row;
            };

            _listView.bindItem = (element, index) =>
            {
                var label = element.Q<Label>();
                if (label != null && index < _items.Count)
                {
                    label.text = _items[index];
                }

                // Alternating row backgrounds
                element.style.backgroundColor = (index % 2 == 0)
                    ? C("bg.row.even")
                    : C("bg.row.odd");
            };

            _listView.itemsSource = _items;

            _listView.selectionChanged += _ =>
            {
                SelectionChanged?.Invoke(_listView.selectedIndex);
            };

            _listView.style.flexGrow = 1;
            _listView.RegisterCallback<AttachToPanelEvent>(_ => StyleInternalElements());
            Add(_listView);
            ApplyTheme();
        }

        public void SetItems(List<string> items)
        {
            _items.Clear();
            _items.AddRange(items);
            _listView.itemsSource = _items;
            _listView.Rebuild();
        }

        public int SelectedIndex => _listView.selectedIndex;

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
            var track = _listView.Q(className: "unity-base-slider__tracker");
            if (track != null)
            {
                track.style.backgroundColor = C("scrollbar.track");
            }

            var dragger = _listView.Q(className: "unity-base-slider__dragger");
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

        public static ListViewBuilder Create(int itemHeight = 28)
            => new(new ForgeListView(itemHeight));

        internal sealed class ListViewBuilder : ForgeBuilder<ListViewBuilder, ForgeListView>
        {
            internal ListViewBuilder(ForgeListView el) : base(el) { }
            public ListViewBuilder Items(List<string> items) { _el.SetItems(items); return this; }
            public ListViewBuilder OnSelectionChanged(Action<int> cb) { _el.SelectionChanged += cb; return this; }
        }
    }
}
