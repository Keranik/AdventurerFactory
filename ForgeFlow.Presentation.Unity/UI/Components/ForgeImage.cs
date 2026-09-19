using UnityEngine;
using UnityEngine.UIElements;

namespace ForgeFlow.Presentation.Unity.UI.Components
{
    /// <summary>Themed image element with background texture and tint support.</summary>
    internal sealed class ForgeImage : ForgeStyledVisualElement
    {
        private readonly VisualElement _imageElement;
        private Color _tint = Color.white;

        public ForgeImage(Texture2D? texture = null, Color? tint = null)
        {
            // Container: center content, tinted background
            style.alignItems = Align.Center;
            style.justifyContent = Justify.Center;
            style.backgroundColor = C("bg.tertiary");

            _imageElement = new VisualElement();
            _imageElement.style.flexGrow = 1;
            _imageElement.style.alignSelf = Align.Center;

            if (texture != null)
            {
                _imageElement.style.backgroundImage = new StyleBackground(texture);
            }

            if (tint.HasValue)
            {
                _tint = tint.Value;
                _imageElement.style.unityBackgroundImageTintColor = _tint;
            }

            Add(_imageElement);
            ApplyTheme();
        }

        public void SetTexture(Texture2D? texture)
        {
            _imageElement.style.backgroundImage = texture != null
                ? new StyleBackground(texture)
                : StyleKeyword.None;
        }

        public void SetTint(Color color)
        {
            _tint = color;
            _imageElement.style.unityBackgroundImageTintColor = _tint;
        }

        public override void ApplyTheme()
        {
            style.backgroundColor = C("bg.tertiary");
            var radius = ThemeIsMinimalist ? 2 : 6;
            SetBorder(C("border.normal"), ThemeBorderWidth, radius);
            SetPadding(ThemePaddingSmall);
        }

        // ── Fluent builder API ──

        public static ImageBuilder Create(Texture2D? texture = null, Color? tint = null)
            => new(new ForgeImage(texture, tint));

        internal sealed class ImageBuilder : ForgeBuilder<ImageBuilder, ForgeImage>
        {
            internal ImageBuilder(ForgeImage el) : base(el) { }
            public ImageBuilder Texture(Texture2D? tex) { _el.SetTexture(tex); return this; }
            public ImageBuilder Tint(Color c) { _el.SetTint(c); return this; }
        }
    }
}
