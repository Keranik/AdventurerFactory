using ForgeFlow.Core.Entities;

namespace ForgeFlow.Core.Theming;

/// <summary>
/// Complete UI theme definition. Holds all semantic colors, font sizes, and border widths
/// needed by the Forge UI component suite. Pure .NET — zero Unity references.
/// </summary>
[Serializable]
public sealed class ThemeDefinition
{
    public string Id { get; set; } = "dark_factory";
    public string DisplayName { get; set; } = "Dark Factory";

    // --- Background colors ---
    public string BackgroundPrimary { get; set; } = "#1A1A2E";
    public string BackgroundSecondary { get; set; } = "#16213E";
    public string BackgroundTertiary { get; set; } = "#0F3460";
    public string BackgroundPanel { get; set; } = "#1A1A2EE0";

    // --- Text colors ---
    public string TextPrimary { get; set; } = "#E0E0E0";
    public string TextSecondary { get; set; } = "#A0A0B0";
    public string TextAccent { get; set; } = "#E94560";
    public string TextDisabled { get; set; } = "#606070";

    // --- Button colors ---
    public string ButtonNormal { get; set; } = "#0F3460";
    public string ButtonHover { get; set; } = "#1A4F80";
    public string ButtonActive { get; set; } = "#E94560";
    public string ButtonDisabled { get; set; } = "#2A2A3E";
    public string ButtonText { get; set; } = "#E0E0E0";

    // --- Borders ---
    public string BorderNormal { get; set; } = "#333355";
    public string BorderFocused { get; set; } = "#E94560";
    public string BorderHover { get; set; } = "#4444AA";

    // --- Input fields ---
    public string InputBackground { get; set; } = "#0D0D1A";
    public string InputText { get; set; } = "#E0E0E0";

    // --- Accent / highlight ---
    public string AccentPrimary { get; set; } = "#E94560";
    public string AccentSecondary { get; set; } = "#0F3460";
    public string HighlightSelection { get; set; } = "#E9456040";

    // --- Progress bar ---
    public string ProgressBackground { get; set; } = "#1A1A2E";
    public string ProgressFill { get; set; } = "#E94560";

    // --- Tooltip ---
    public string TooltipBackground { get; set; } = "#0D0D1AE8";
    public string TooltipText { get; set; } = "#E0E0E0";
    public string TooltipBorder { get; set; } = "#E94560";

    // --- Status colors ---
    public string StatusSuccess { get; set; } = "#4CAF50";
    public string StatusWarning { get; set; } = "#E5C845";
    public string StatusError { get; set; } = "#E94560";

    // --- Header & row backgrounds ---
    public string BackgroundHeader { get; set; } = "#122448";
    public string BackgroundRowEven { get; set; } = "#16213E";
    public string BackgroundRowOdd { get; set; } = "#1A1A2E";
    public string BackgroundElevated { get; set; } = "#1E2A4A";
    public string ButtonPressed { get; set; } = "#C23050";

    // --- Scrollbar ---
    public string ScrollbarTrack { get; set; } = "#1A1A2E";
    public string ScrollbarThumb { get; set; } = "#333355";

    // --- Tab ---
    public string TabInactive { get; set; } = "#16213E";
    public string TabActive { get; set; } = "#0F3460";

    // --- Font sizes ---
    public int FontSizeSmall { get; set; } = 11;
    public int FontSizeNormal { get; set; } = 14;
    public int FontSizeLarge { get; set; } = 18;
    public int FontSizeHeader { get; set; } = 24;

    // --- Border widths ---
    public int BorderWidth { get; set; } = 1;
    public int BorderRadius { get; set; } = 4;

    // --- Spacing ---
    public int PaddingSmall { get; set; } = 4;
    public int PaddingNormal { get; set; } = 8;
    public int PaddingLarge { get; set; } = 16;

    /// <summary>Resolves a semantic color key to a <see cref="ColorRPG"/>.</summary>
    public ColorRPG GetColor(string key) => key switch
    {
        "bg.primary" => ColorRPG.FromHex(BackgroundPrimary),
        "bg.secondary" => ColorRPG.FromHex(BackgroundSecondary),
        "bg.tertiary" => ColorRPG.FromHex(BackgroundTertiary),
        "bg.panel" => ColorRPG.FromHex(BackgroundPanel),
        "text.primary" => ColorRPG.FromHex(TextPrimary),
        "text.secondary" => ColorRPG.FromHex(TextSecondary),
        "text.accent" => ColorRPG.FromHex(TextAccent),
        "text.disabled" => ColorRPG.FromHex(TextDisabled),
        "btn.normal" => ColorRPG.FromHex(ButtonNormal),
        "btn.hover" => ColorRPG.FromHex(ButtonHover),
        "btn.active" => ColorRPG.FromHex(ButtonActive),
        "btn.disabled" => ColorRPG.FromHex(ButtonDisabled),
        "btn.text" => ColorRPG.FromHex(ButtonText),
        "border.normal" => ColorRPG.FromHex(BorderNormal),
        "border.focused" => ColorRPG.FromHex(BorderFocused),
        "border.hover" => ColorRPG.FromHex(BorderHover),
        "input.bg" => ColorRPG.FromHex(InputBackground),
        "input.text" => ColorRPG.FromHex(InputText),
        "accent.primary" => ColorRPG.FromHex(AccentPrimary),
        "accent.secondary" => ColorRPG.FromHex(AccentSecondary),
        "highlight.selection" => ColorRPG.FromHex(HighlightSelection),
        "progress.bg" => ColorRPG.FromHex(ProgressBackground),
        "progress.fill" => ColorRPG.FromHex(ProgressFill),
        "tooltip.bg" => ColorRPG.FromHex(TooltipBackground),
        "tooltip.text" => ColorRPG.FromHex(TooltipText),
        "tooltip.border" => ColorRPG.FromHex(TooltipBorder),
        "bg.header" => ColorRPG.FromHex(BackgroundHeader),
        "bg.row.even" => ColorRPG.FromHex(BackgroundRowEven),
        "bg.row.odd" => ColorRPG.FromHex(BackgroundRowOdd),
        "bg.elevated" => ColorRPG.FromHex(BackgroundElevated),
        "btn.pressed" => ColorRPG.FromHex(ButtonPressed),
        "scrollbar.track" => ColorRPG.FromHex(ScrollbarTrack),
        "scrollbar.thumb" => ColorRPG.FromHex(ScrollbarThumb),
        "tab.inactive" => ColorRPG.FromHex(TabInactive),
        "tab.active" => ColorRPG.FromHex(TabActive),
        "status.success" => ColorRPG.FromHex(StatusSuccess),
        "status.warning" => ColorRPG.FromHex(StatusWarning),
        "status.error" => ColorRPG.FromHex(StatusError),
        _ => ColorRPG.Magenta // missing key = magenta so it's obvious
    };
}
