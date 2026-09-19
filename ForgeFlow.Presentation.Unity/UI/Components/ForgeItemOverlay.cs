using UnityEngine;
using UnityEngine.UIElements;

namespace ForgeFlow.Presentation.Unity.UI.Components
{
    /// <summary>
    /// Compact item overlay: icon placeholder + quantity badge.
    /// Used for item slots, inventory cells, recipe input/output display.
    /// </summary>
    internal sealed class ForgeItemOverlay : ForgeStyledVisualElement
    {
        private readonly VisualElement _iconElement;
        private readonly Label _quantityLabel;
        private int _quantity;

        public ForgeItemOverlay(int size = 40)
        {
            style.width = size;
            style.height = size;
            style.position = Position.Relative;
            style.alignItems = Align.Center;
            style.justifyContent = Justify.Center;

            _iconElement = new VisualElement();
            _iconElement.style.width = size - 8;
            _iconElement.style.height = size - 8;
            _iconElement.style.backgroundColor = C("bg.tertiary");
            _iconElement.style.alignSelf = Align.Center;
            _iconElement.style.borderTopLeftRadius = ThemeBorderRadius;
            _iconElement.style.borderTopRightRadius = ThemeBorderRadius;
            _iconElement.style.borderBottomLeftRadius = ThemeBorderRadius;
            _iconElement.style.borderBottomRightRadius = ThemeBorderRadius;
            Add(_iconElement);

            // Quantity badge — bottom-right corner
            _quantityLabel = new Label();
            _quantityLabel.style.position = Position.Absolute;
            _quantityLabel.style.right = 1;
            _quantityLabel.style.bottom = 1;
            _quantityLabel.style.fontSize = ThemeFontSmall;
            _quantityLabel.style.color = C("text.primary");
            _quantityLabel.style.backgroundColor = new Color(0, 0, 0, 0.7f);
            _quantityLabel.style.paddingLeft = 2;
            _quantityLabel.style.paddingRight = 2;
            _quantityLabel.style.display = DisplayStyle.None;
            Add(_quantityLabel);

            ApplyTheme();
        }

        public int Quantity
        {
            get => _quantity;
            set
            {
                _quantity = value;
                if (value > 0)
                {
                    _quantityLabel.text = value.ToString();
                    _quantityLabel.style.display = DisplayStyle.Flex;
                }
                else
                {
                    _quantityLabel.style.display = DisplayStyle.None;
                }
            }
        }

        public void SetIcon(Texture2D? texture)
        {
            _iconElement.style.backgroundImage = texture != null
                ? new StyleBackground(texture)
                : StyleKeyword.None;
        }

        public void SetIconColor(string colorKey)
        {
            _iconElement.style.backgroundColor = C(colorKey);
        }

        public override void ApplyTheme()
        {
            // Slot container: dark background with inset border
            style.backgroundColor = C("bg.secondary");
            var radius = ThemeIsMinimalist ? 2 : 6;
            SetInsetBorderOn(this, C("border.normal"), ThemeBorderWidth + 1, radius);

            // Icon area: slightly lighter, rounded
            _iconElement.style.backgroundColor = C("bg.tertiary");
            var iconRadius = ThemeIsMinimalist ? 2 : 4;
            _iconElement.style.borderTopLeftRadius = iconRadius;
            _iconElement.style.borderTopRightRadius = iconRadius;
            _iconElement.style.borderBottomLeftRadius = iconRadius;
            _iconElement.style.borderBottomRightRadius = iconRadius;

            // Quantity badge: accent background, bold text, rounded
            _quantityLabel.style.color = C("text.primary");
            _quantityLabel.style.backgroundColor = C("accent.primary");
            _quantityLabel.style.fontSize = ThemeFontSmall;
            _quantityLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            _quantityLabel.style.paddingLeft = 3;
            _quantityLabel.style.paddingRight = 3;
            _quantityLabel.style.paddingTop = 1;
            _quantityLabel.style.paddingBottom = 1;
            _quantityLabel.style.borderTopLeftRadius = 4;
            _quantityLabel.style.borderTopRightRadius = 4;
            _quantityLabel.style.borderBottomLeftRadius = 4;
            _quantityLabel.style.borderBottomRightRadius = 4;
        }

        // ── Fluent builder API ──

        public static ItemOverlayBuilder Create(int size = 40)
            => new(new ForgeItemOverlay(size));

        internal sealed class ItemOverlayBuilder : ForgeBuilder<ItemOverlayBuilder, ForgeItemOverlay>
        {
            internal ItemOverlayBuilder(ForgeItemOverlay el) : base(el) { }
            public ItemOverlayBuilder Icon(Texture2D? tex) { _el.SetIcon(tex); return this; }
            public ItemOverlayBuilder IconColor(string key) { _el.SetIconColor(key); return this; }
            public ItemOverlayBuilder Value(int qty) { _el.Quantity = qty; return this; }
        }
    }
}
