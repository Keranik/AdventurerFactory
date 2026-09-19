using ForgeFlow.Core.Entities;
using ForgeFlow.Core.Events;
using ForgeFlow.Core.Systems;

namespace ForgeFlow.Core.Theming;

/// <summary>
/// Manages the active theme. UI components call <see cref="GetColor"/> to read
/// semantic colors from the current <see cref="ThemeDefinition"/>.
/// Publishes <see cref="ThemeChangedEvent"/> when the theme changes.
/// Pure .NET — zero Unity references.
/// </summary>
public sealed class ThemeService
{
    private readonly ThemeRegistry _registry;
    private readonly EventBus _eventBus;
    private ThemeDefinition _active;
    private UIStyleMode _styleMode = UIStyleMode.FunWhimsical;

    public ThemeService(ThemeRegistry registry, EventBus eventBus)
    {
        _registry = registry;
        _eventBus = eventBus;
        _active = new ThemeDefinition(); // default "dark_factory"
    }

    public ThemeDefinition ActiveTheme => _active;
    public string ActiveThemeId => _active.Id;
    public UIStyleMode StyleMode => _styleMode;

    /// <summary>Sets the UI style mode (Fun/Whimsical vs Minimalist/Expert).</summary>
    public void SetStyleMode(UIStyleMode mode) => _styleMode = mode;

    /// <summary>Switches to the theme with the given ID. Returns false if not found.</summary>
    public bool SetTheme(string themeId)
    {
        var theme = _registry.Get(themeId);
        if (theme == null) return false;

        var previousId = _active.Id;
        _active = theme;
        _eventBus.Publish(new ThemeChangedEvent(previousId, themeId));
        return true;
    }

    /// <summary>Resolves a semantic color key (e.g., "btn.hover") to a <see cref="ColorRPG"/>.</summary>
    public ColorRPG GetColor(string key) => _active.GetColor(key);

    public int FontSizeSmall => _styleMode == UIStyleMode.MinimalistExpert ? Math.Max(_active.FontSizeSmall - 1, 9) : _active.FontSizeSmall;
    public int FontSizeNormal => _styleMode == UIStyleMode.MinimalistExpert ? Math.Max(_active.FontSizeNormal - 1, 11) : _active.FontSizeNormal;
    public int FontSizeLarge => _active.FontSizeLarge;
    public int FontSizeHeader => _active.FontSizeHeader;
    public int BorderWidth => _active.BorderWidth;
    public int BorderRadius => _styleMode == UIStyleMode.MinimalistExpert ? 2 : _active.BorderRadius;
    public int PaddingSmall => _styleMode == UIStyleMode.MinimalistExpert ? Math.Max(_active.PaddingSmall - 1, 2) : _active.PaddingSmall;
    public int PaddingNormal => _styleMode == UIStyleMode.MinimalistExpert ? Math.Max(_active.PaddingNormal - 2, 4) : _active.PaddingNormal;
    public int PaddingLarge => _styleMode == UIStyleMode.MinimalistExpert ? Math.Max(_active.PaddingLarge - 4, 8) : _active.PaddingLarge;
    public bool IsMinimalist => _styleMode == UIStyleMode.MinimalistExpert;
}
