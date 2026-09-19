using ForgeFlow.Core.Localization;
using UnityEngine;
using UnityEngine.UIElements;

namespace ForgeFlow.Presentation.Unity.UI.Components
{
    /// <summary>
    /// Styling variants for the product picker.
    /// <list type="bullet">
    /// <item><description>SmallIcons — compact grid of small icons, no text.</description></item>
    /// <item><description>MediumWithText — medium icons with product name underneath.</description></item>
    /// <item><description>DropdownList — full list rows with [Icon] Product Name.</description></item>
    /// </list>
    /// </summary>
    internal enum ProductPickerStyle
    {
        SmallIcons,
        MediumWithText,
        DropdownList
    }

    /// <summary>
    /// Lightweight product display data for the picker.
    /// </summary>
    internal readonly struct ProductEntry
    {
        public string Id { get; }
        public string DisplayName { get; }
        public Color DisplayColor { get; }
        public bool IsSelected { get; }

        public ProductEntry(string id, string displayName, Color displayColor, bool isSelected = false)
        {
            Id = id;
            DisplayName = displayName;
            DisplayColor = displayColor;
            IsSelected = isSelected;
        }
    }

    /// <summary>
    /// Flexible product/item picker UI component with three styling variants.
    /// Used by Stockpile Inspector, future structure configs, etc.
    /// Supports fluent builder API with <c>.Style()</c> to switch variants.
    /// </summary>
    internal sealed class ForgeProductPicker : ForgeStyledVisualElement
    {
        private readonly VisualElement _container;
        private readonly Label _emptyLabel;
        private ProductPickerStyle _style = ProductPickerStyle.SmallIcons;
        private List<ProductEntry> _products = new();

        public event Action<string>? ItemSelected;
        public event Action<string>? ItemDeselected;

        public ForgeProductPicker()
        {
            style.flexDirection = FlexDirection.Column;

            _container = new VisualElement();
            _container.style.flexGrow = 1;

            _emptyLabel = new Label(L(LocalizationKeys.ProductPickerNoItems));
            _emptyLabel.style.color = C("text.secondary");
            _emptyLabel.style.fontSize = ThemeFontNormal;
            _emptyLabel.style.unityTextAlign = TextAnchor.MiddleCenter;

            Add(_container);
            ApplyTheme();
        }

        public ProductPickerStyle CurrentStyle
        {
            get => _style;
            set
            {
                if (_style == value)
                {
                    return;
                }
                _style = value;
                RebuildCells();
            }
        }

        public void SetProducts(List<ProductEntry> products)
        {
            _products = products;
            RebuildCells();
        }

        private void RebuildCells()
        {
            _container.Clear();

            if (_products.Count == 0)
            {
                _emptyLabel.text = L(LocalizationKeys.ProductPickerNoItems);
                _container.Add(_emptyLabel);
                return;
            }

            switch (_style)
            {
                case ProductPickerStyle.SmallIcons:
                    BuildSmallIconsLayout();
                    break;
                case ProductPickerStyle.MediumWithText:
                    BuildMediumWithTextLayout();
                    break;
                case ProductPickerStyle.DropdownList:
                    BuildDropdownListLayout();
                    break;
            }
        }

        // ── SmallIcons: compact grid of colored squares ──

        private void BuildSmallIconsLayout()
        {
            var grid = new VisualElement();
            grid.style.flexDirection = FlexDirection.Row;
            grid.style.flexWrap = Wrap.Wrap;

            foreach (var product in _products)
            {
                var cell = CreateSmallIconCell(product);
                grid.Add(cell);
            }

            _container.Add(grid);
        }

        private VisualElement CreateSmallIconCell(ProductEntry product)
        {
            int size = 32;
            var cell = new VisualElement();
            cell.style.width = size;
            cell.style.height = size;
            cell.style.marginRight = 3;
            cell.style.marginBottom = 3;
            cell.style.backgroundColor = product.DisplayColor;
            cell.style.alignItems = Align.Center;
            cell.style.justifyContent = Justify.Center;
            cell.tooltip = product.DisplayName;

            var radius = ThemeIsMinimalist ? 2 : 4;
            var borderColor = product.IsSelected ? C("accent.primary") : C("border.normal");
            var borderWidth = product.IsSelected ? ThemeBorderWidth + 2 : ThemeBorderWidth;
            SetBorderOn(cell, borderColor, borderWidth, radius);

            // Short initial (1-2 chars) for visual identification
            var initial = new Label(product.DisplayName.Length >= 2
                ? product.DisplayName[..2].ToUpperInvariant()
                : product.DisplayName.ToUpperInvariant());
            initial.style.color = C("text.primary");
            initial.style.fontSize = ThemeFontSmall - 1;
            initial.style.unityTextAlign = TextAnchor.MiddleCenter;
            initial.style.unityFontStyleAndWeight = FontStyle.Bold;
            initial.pickingMode = PickingMode.Ignore;
            cell.Add(initial);

            var id = product.Id;
            var isSelected = product.IsSelected;
            cell.RegisterCallback<ClickEvent>(_ =>
            {
                if (isSelected)
                {
                    ItemDeselected?.Invoke(id);
                }
                else
                {
                    ItemSelected?.Invoke(id);
                }
            });

            cell.RegisterCallback<MouseEnterEvent>(_ =>
            {
                SetBorderOn(cell, C("accent.primary"), ThemeBorderWidth + 2, radius);
            });
            cell.RegisterCallback<MouseLeaveEvent>(_ =>
            {
                var bc = isSelected ? C("accent.primary") : C("border.normal");
                var bw = isSelected ? ThemeBorderWidth + 2 : ThemeBorderWidth;
                SetBorderOn(cell, bc, bw, radius);
            });

            return cell;
        }

        // ── MediumWithText: medium icons with name underneath ──

        private void BuildMediumWithTextLayout()
        {
            var grid = new VisualElement();
            grid.style.flexDirection = FlexDirection.Row;
            grid.style.flexWrap = Wrap.Wrap;

            foreach (var product in _products)
            {
                var cell = CreateMediumCell(product);
                grid.Add(cell);
            }

            _container.Add(grid);
        }

        private VisualElement CreateMediumCell(ProductEntry product)
        {
            int iconSize = 48;
            var cell = new VisualElement();
            cell.style.width = 64;
            cell.style.marginRight = 4;
            cell.style.marginBottom = 4;
            cell.style.alignItems = Align.Center;

            var iconBox = new VisualElement();
            iconBox.style.width = iconSize;
            iconBox.style.height = iconSize;
            iconBox.style.backgroundColor = product.DisplayColor;
            iconBox.style.alignItems = Align.Center;
            iconBox.style.justifyContent = Justify.Center;

            var radius = ThemeIsMinimalist ? 2 : 6;
            var borderColor = product.IsSelected ? C("accent.primary") : C("border.normal");
            var borderWidth = product.IsSelected ? ThemeBorderWidth + 2 : ThemeBorderWidth;
            SetBorderOn(iconBox, borderColor, borderWidth, radius);

            var initial = new Label(product.DisplayName.Length >= 3
                ? product.DisplayName[..3].ToUpperInvariant()
                : product.DisplayName.ToUpperInvariant());
            initial.style.color = C("text.primary");
            initial.style.fontSize = ThemeFontNormal;
            initial.style.unityTextAlign = TextAnchor.MiddleCenter;
            initial.style.unityFontStyleAndWeight = FontStyle.Bold;
            initial.pickingMode = PickingMode.Ignore;
            iconBox.Add(initial);

            cell.Add(iconBox);

            var nameLabel = new Label(product.DisplayName);
            nameLabel.style.color = C("text.primary");
            nameLabel.style.fontSize = ThemeFontSmall;
            nameLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            nameLabel.style.overflow = Overflow.Hidden;
            nameLabel.style.maxWidth = 64;
            nameLabel.style.marginTop = 2;
            nameLabel.pickingMode = PickingMode.Ignore;
            cell.Add(nameLabel);

            var id = product.Id;
            var isSelected = product.IsSelected;
            cell.RegisterCallback<ClickEvent>(_ =>
            {
                if (isSelected)
                {
                    ItemDeselected?.Invoke(id);
                }
                else
                {
                    ItemSelected?.Invoke(id);
                }
            });

            cell.RegisterCallback<MouseEnterEvent>(_ =>
            {
                SetBorderOn(iconBox, C("accent.primary"), ThemeBorderWidth + 2, radius);
            });
            cell.RegisterCallback<MouseLeaveEvent>(_ =>
            {
                var bc = isSelected ? C("accent.primary") : C("border.normal");
                var bw = isSelected ? ThemeBorderWidth + 2 : ThemeBorderWidth;
                SetBorderOn(iconBox, bc, bw, radius);
            });

            return cell;
        }

        // ── DropdownList: full list rows with [Icon] Product Name ──

        private void BuildDropdownListLayout()
        {
            var list = new VisualElement();
            list.style.flexDirection = FlexDirection.Column;

            foreach (var product in _products)
            {
                var row = CreateDropdownRow(product);
                list.Add(row);
            }

            _container.Add(list);
        }

        private VisualElement CreateDropdownRow(ProductEntry product)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.height = 28;
            row.style.paddingLeft = ThemePaddingSmall;
            row.style.paddingRight = ThemePaddingSmall;
            row.style.marginBottom = 1;

            var radius = ThemeIsMinimalist ? 2 : 4;
            var bgColor = product.IsSelected ? C("accent.primary") : C("bg.secondary");
            row.style.backgroundColor = bgColor;
            SetBorderOn(row, product.IsSelected ? C("accent.primary") : C("border.normal"),
                ThemeBorderWidth, radius);

            // Color swatch icon
            var swatch = new VisualElement();
            swatch.style.width = 18;
            swatch.style.height = 18;
            swatch.style.backgroundColor = product.DisplayColor;
            swatch.style.marginRight = ThemePaddingSmall;
            swatch.style.flexShrink = 0;
            SetBorderOn(swatch, C("border.normal"), 1, 3);
            row.Add(swatch);

            // Name label
            var label = new Label(product.DisplayName);
            label.style.color = product.IsSelected ? C("text.primary") : C("text.primary");
            label.style.fontSize = ThemeFontSmall;
            label.style.flexGrow = 1;
            label.pickingMode = PickingMode.Ignore;
            row.Add(label);

            // Selection indicator
            if (product.IsSelected)
            {
                var check = new Label("✓");
                check.style.color = C("text.primary");
                check.style.fontSize = ThemeFontSmall;
                check.style.unityFontStyleAndWeight = FontStyle.Bold;
                check.style.flexShrink = 0;
                check.pickingMode = PickingMode.Ignore;
                row.Add(check);
            }

            var id = product.Id;
            var isSelected = product.IsSelected;
            row.RegisterCallback<ClickEvent>(_ =>
            {
                if (isSelected)
                {
                    ItemDeselected?.Invoke(id);
                }
                else
                {
                    ItemSelected?.Invoke(id);
                }
            });

            row.RegisterCallback<MouseEnterEvent>(_ =>
            {
                if (!isSelected)
                {
                    row.style.backgroundColor = C("bg.tertiary");
                }
            });
            row.RegisterCallback<MouseLeaveEvent>(_ =>
            {
                row.style.backgroundColor = isSelected ? C("accent.primary") : C("bg.secondary");
            });

            return row;
        }

        public override void ApplyTheme()
        {
            _emptyLabel.style.color = C("text.secondary");
            _emptyLabel.style.fontSize = ThemeFontNormal;
            RebuildCells();
        }

        // ── Fluent builder API ──

        public static ProductPickerBuilder Create(ProductPickerStyle style = ProductPickerStyle.SmallIcons)
            => new(new ForgeProductPicker { CurrentStyle = style });

        internal sealed class ProductPickerBuilder : ForgeBuilder<ProductPickerBuilder, ForgeProductPicker>
        {
            internal ProductPickerBuilder(ForgeProductPicker el) : base(el) { }

            public ProductPickerBuilder Style(ProductPickerStyle s) { _el.CurrentStyle = s; return this; }
            public ProductPickerBuilder Products(List<ProductEntry> p) { _el.SetProducts(p); return this; }
            public ProductPickerBuilder OnItemSelected(Action<string> cb) { _el.ItemSelected += cb; return this; }
            public ProductPickerBuilder OnItemDeselected(Action<string> cb) { _el.ItemDeselected += cb; return this; }
        }
    }
}
