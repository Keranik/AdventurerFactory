using ForgeFlow.Core.Localization;
using ForgeFlow.Core.Theming;
using UnityEngine;
using UnityEngine.UIElements;

namespace ForgeFlow.Presentation.Unity.UI.Components
{
    /// <summary>
    /// Base class for all themed Forge UI components.
    /// Provides centralized access to <see cref="ThemeService"/> and <see cref="TranslationService"/>,
    /// with a virtual <see cref="ApplyTheme"/> hook for styling.
    /// </summary>
    internal abstract class ForgeStyledVisualElement : VisualElement
    {
        private static ThemeService? _themeService;
        private static TranslationService? _translationService;

        /// <summary>Sets the shared services once at application startup.</summary>
        public static void SetServices(ThemeService themeService, TranslationService translationService)
        {
            _themeService = themeService;
            _translationService = translationService;
        }

        protected static ThemeService? Theme => _themeService;
        protected static TranslationService? Loc => _translationService;

        /// <summary>Resolves a localization key to a translated string.</summary>
        protected static string L(string key) => _translationService?.Get(key) ?? key;

        /// <summary>Resolves a theme color key to a Unity Color.</summary>
        protected static Color C(string key)
        {
            if (_themeService == null) return Color.magenta;
            var c = _themeService.GetColor(key);
            return new Color(c.R, c.G, c.B, c.A);
        }

        /// <summary>Public accessor for theme color — used by non-derived classes (e.g. MonoBehaviours).</summary>
        public static Color GetThemeColor(string key) => C(key);

        /// <summary>Public accessor for localized text — used by non-derived classes.</summary>
        public static string GetLocalizedText(string key) => L(key);

        /// <summary>Public static accessor for minimalist mode — used by non-derived classes.</summary>
        public static bool IsMinimalistMode => _themeService?.IsMinimalist ?? false;

        // Public static theme-size accessors for non-VisualElement classes (e.g. BaseEntityInspector).
        public static int ThemePaddingSmallStatic => _themeService?.PaddingSmall ?? 4;
        public static int ThemePaddingNormalStatic => _themeService?.PaddingNormal ?? 8;
        public static int ThemeFontSmallStatic => _themeService?.FontSizeSmall ?? 11;
        public static int ThemeFontNormalStatic => _themeService?.FontSizeNormal ?? 14;
        public static int ThemeFontLargeStatic => _themeService?.FontSizeLarge ?? 18;
        public static int ThemeFontHeaderStatic => _themeService?.FontSizeHeader ?? 24;
        public static int ThemeBorderWidthStatic => _themeService?.BorderWidth ?? 1;

        /// <summary>Called after theme change to re-apply all styles. Override in derived classes.</summary>
        public virtual void ApplyTheme()
        {
            // Base does nothing — subclasses override.
        }

        internal void SetBorder(Color color, int width, int radius)
        {
            style.borderTopColor = color;
            style.borderBottomColor = color;
            style.borderLeftColor = color;
            style.borderRightColor = color;
            style.borderTopWidth = width;
            style.borderBottomWidth = width;
            style.borderLeftWidth = width;
            style.borderRightWidth = width;
            style.borderTopLeftRadius = radius;
            style.borderTopRightRadius = radius;
            style.borderBottomLeftRadius = radius;
            style.borderBottomRightRadius = radius;
        }

        internal void SetPadding(int value)
        {
            style.paddingTop = value;
            style.paddingBottom = value;
            style.paddingLeft = value;
            style.paddingRight = value;
        }

        internal void SetMargin(int value)
        {
            style.marginTop = value;
            style.marginBottom = value;
            style.marginLeft = value;
            style.marginRight = value;
        }

        internal int ThemeBorderWidth => _themeService?.BorderWidth ?? 1;
        internal int ThemeBorderRadius => _themeService?.BorderRadius ?? 4;
        internal int ThemePaddingSmall => _themeService?.PaddingSmall ?? 4;
        internal int ThemePaddingNormal => _themeService?.PaddingNormal ?? 8;
        internal int ThemePaddingLarge => _themeService?.PaddingLarge ?? 16;
        internal int ThemeFontSmall => _themeService?.FontSizeSmall ?? 11;
        internal int ThemeFontNormal => _themeService?.FontSizeNormal ?? 14;
        internal int ThemeFontLarge => _themeService?.FontSizeLarge ?? 18;
        internal int ThemeFontHeader => _themeService?.FontSizeHeader ?? 24;
        internal bool ThemeIsMinimalist => _themeService?.IsMinimalist ?? false;

        /// <summary>Applies border styling to an arbitrary child VisualElement.</summary>
        internal static void SetBorderOn(VisualElement el, Color color, int width, int radius)
        {
            el.style.borderTopColor = color;
            el.style.borderBottomColor = color;
            el.style.borderLeftColor = color;
            el.style.borderRightColor = color;
            el.style.borderTopWidth = width;
            el.style.borderBottomWidth = width;
            el.style.borderLeftWidth = width;
            el.style.borderRightWidth = width;
            el.style.borderTopLeftRadius = radius;
            el.style.borderTopRightRadius = radius;
            el.style.borderBottomLeftRadius = radius;
            el.style.borderBottomRightRadius = radius;
        }

        /// <summary>Applies padding to an arbitrary child VisualElement.</summary>
        internal static void SetPaddingOn(VisualElement el, int value)
        {
            el.style.paddingTop = value;
            el.style.paddingBottom = value;
            el.style.paddingLeft = value;
            el.style.paddingRight = value;
        }

        /// <summary>
        /// Applies a raised 3D border effect: lighter top/left, darker bottom/right.
        /// Creates a tactile button/panel look common in factory-game UIs.
        /// </summary>
        internal static void SetRaisedBorderOn(VisualElement el, Color baseColor, int width, int radius)
        {
            var lighter = new Color(
                Mathf.Min(baseColor.r + 0.12f, 1f),
                Mathf.Min(baseColor.g + 0.12f, 1f),
                Mathf.Min(baseColor.b + 0.12f, 1f),
                baseColor.a);
            var darker = new Color(
                Mathf.Max(baseColor.r - 0.08f, 0f),
                Mathf.Max(baseColor.g - 0.08f, 0f),
                Mathf.Max(baseColor.b - 0.08f, 0f),
                baseColor.a);

            el.style.borderTopColor = lighter;
            el.style.borderLeftColor = lighter;
            el.style.borderBottomColor = darker;
            el.style.borderRightColor = darker;
            el.style.borderTopWidth = width;
            el.style.borderBottomWidth = width;
            el.style.borderLeftWidth = width;
            el.style.borderRightWidth = width;
            el.style.borderTopLeftRadius = radius;
            el.style.borderTopRightRadius = radius;
            el.style.borderBottomLeftRadius = radius;
            el.style.borderBottomRightRadius = radius;
        }

        /// <summary>
        /// Applies an inset/pressed border effect: darker top/left, lighter bottom/right.
        /// Used for pressed button states and recessed areas.
        /// </summary>
        internal static void SetInsetBorderOn(VisualElement el, Color baseColor, int width, int radius)
        {
            var lighter = new Color(
                Mathf.Min(baseColor.r + 0.08f, 1f),
                Mathf.Min(baseColor.g + 0.08f, 1f),
                Mathf.Min(baseColor.b + 0.08f, 1f),
                baseColor.a);
            var darker = new Color(
                Mathf.Max(baseColor.r - 0.12f, 0f),
                Mathf.Max(baseColor.g - 0.12f, 0f),
                Mathf.Max(baseColor.b - 0.12f, 0f),
                baseColor.a);

            el.style.borderTopColor = darker;
            el.style.borderLeftColor = darker;
            el.style.borderBottomColor = lighter;
            el.style.borderRightColor = lighter;
            el.style.borderTopWidth = width;
            el.style.borderBottomWidth = width;
            el.style.borderLeftWidth = width;
            el.style.borderRightWidth = width;
            el.style.borderTopLeftRadius = radius;
            el.style.borderTopRightRadius = radius;
            el.style.borderBottomLeftRadius = radius;
            el.style.borderBottomRightRadius = radius;
        }
    }

    /// <summary>
    /// Generic fluent builder base for all Forge UI components.
    /// Provides common short styling methods shared across all builders.
    /// </summary>
    internal abstract class ForgeBuilder<TBuilder, TElement>
        where TBuilder : ForgeBuilder<TBuilder, TElement>
        where TElement : ForgeStyledVisualElement
    {
        protected readonly TElement _el;
        protected ForgeBuilder(TElement element) => _el = element;

        public TBuilder Name(string n) { _el.name = n; return (TBuilder)this; }
        public TBuilder Width(float w) { _el.style.width = w; return (TBuilder)this; }
        public TBuilder Height(float h) { _el.style.height = h; return (TBuilder)this; }
        public TBuilder FlexGrow(float f) { _el.style.flexGrow = f; return (TBuilder)this; }
        public TBuilder FlexShrink(float f) { _el.style.flexShrink = f; return (TBuilder)this; }
        public TBuilder FlexDirection(FlexDirection d) { _el.style.flexDirection = d; return (TBuilder)this; }
        public TBuilder Margin(int m) { _el.SetMargin(m); return (TBuilder)this; }
        public TBuilder MarginTop(float m) { _el.style.marginTop = m; return (TBuilder)this; }
        public TBuilder MarginBottom(float m) { _el.style.marginBottom = m; return (TBuilder)this; }
        public TBuilder MarginLeft(float m) { _el.style.marginLeft = m; return (TBuilder)this; }
        public TBuilder MarginRight(float m) { _el.style.marginRight = m; return (TBuilder)this; }
        public TBuilder Padding(int p) { _el.SetPadding(p); return (TBuilder)this; }
        public TBuilder BackgroundColor(string key) { _el.style.backgroundColor = ForgeStyledVisualElement.GetThemeColor(key); return (TBuilder)this; }
        public TBuilder BorderWidth(int w) { _el.SetBorder(ForgeStyledVisualElement.GetThemeColor("border.normal"), w, _el.ThemeBorderRadius); return (TBuilder)this; }
        public TBuilder BorderColor(string key) { _el.SetBorder(ForgeStyledVisualElement.GetThemeColor(key), _el.ThemeBorderWidth, _el.ThemeBorderRadius); return (TBuilder)this; }
        public TBuilder BorderRadius(int r) { _el.style.borderTopLeftRadius = r; _el.style.borderTopRightRadius = r; _el.style.borderBottomLeftRadius = r; _el.style.borderBottomRightRadius = r; return (TBuilder)this; }
        public TBuilder Tooltip(string text) { _el.tooltip = text; return (TBuilder)this; }
        public TBuilder Display(DisplayStyle d) { _el.style.display = d; return (TBuilder)this; }
        public TBuilder AlignItems(Align a) { _el.style.alignItems = a; return (TBuilder)this; }
        public TBuilder JustifyContent(Justify j) { _el.style.justifyContent = j; return (TBuilder)this; }
        public TBuilder Overflow(Overflow o) { _el.style.overflow = o; return (TBuilder)this; }
        public TBuilder Position(Position p) { _el.style.position = p; return (TBuilder)this; }
        public TBuilder WidthPercent(float p) { _el.style.width = new Length(p, LengthUnit.Percent); return (TBuilder)this; }
        public TBuilder HeightPercent(float p) { _el.style.height = new Length(p, LengthUnit.Percent); return (TBuilder)this; }
        public TBuilder MaxWidth(float w) { _el.style.maxWidth = w; return (TBuilder)this; }
        public TBuilder MaxHeight(float h) { _el.style.maxHeight = h; return (TBuilder)this; }
        public TBuilder MaxHeightPercent(float p) { _el.style.maxHeight = new Length(p, LengthUnit.Percent); return (TBuilder)this; }
        public TBuilder MinWidth(float w) { _el.style.minWidth = w; return (TBuilder)this; }
        public TBuilder MinHeight(float h) { _el.style.minHeight = h; return (TBuilder)this; }
        public TBuilder FlexWrap(Wrap w) { _el.style.flexWrap = w; return (TBuilder)this; }
        public TBuilder Left(float v) { _el.style.left = v; return (TBuilder)this; }
        public TBuilder LeftPercent(float p) { _el.style.left = new Length(p, LengthUnit.Percent); return (TBuilder)this; }
        public TBuilder Right(float v) { _el.style.right = v; return (TBuilder)this; }
        public TBuilder Top(float v) { _el.style.top = v; return (TBuilder)this; }
        public TBuilder Bottom(float v) { _el.style.bottom = v; return (TBuilder)this; }
        public TBuilder PaddingTop(float p) { _el.style.paddingTop = p; return (TBuilder)this; }
        public TBuilder PaddingBottom(float p) { _el.style.paddingBottom = p; return (TBuilder)this; }
        public TBuilder PaddingLeft(float p) { _el.style.paddingLeft = p; return (TBuilder)this; }
        public TBuilder PaddingRight(float p) { _el.style.paddingRight = p; return (TBuilder)this; }

        public TElement Build() => _el;
    }
}
