using System.Text.Json;
using ForgeFlow.Core.Data;

namespace ForgeFlow.Core.Theming;

/// <summary>
/// Registry of all available <see cref="ThemeDefinition"/>s.
/// Loads defaults from embedded resources and supports mod overrides.
/// Pure .NET — zero Unity references.
/// </summary>
public sealed class ThemeRegistry : IRegistry
{
    private readonly Dictionary<string, ThemeDefinition> _themes = new();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip
    };

    public IReadOnlyDictionary<string, ThemeDefinition> Themes => _themes;

    public ThemeDefinition? Get(string id) =>
        _themes.TryGetValue(id, out var theme) ? theme : null;

    public void Register(ThemeDefinition theme)
    {
        _themes[theme.Id] = theme;
    }

    /// <summary>Loads all theme definitions from the embedded Themes.json resource.</summary>
    public void LoadFromEmbeddedResources()
    {
        var assembly = typeof(ThemeRegistry).Assembly;
        using var stream = assembly.GetManifestResourceStream("ForgeFlow.Core.Data.Themes.json");
        if (stream == null) return;

        using var reader = new StreamReader(stream);
        var json = reader.ReadToEnd();
        var themes = JsonSerializer.Deserialize<ThemeDefinition[]>(json, JsonOptions);
        if (themes == null) return;

        foreach (var theme in themes)
        {
            _themes[theme.Id] = theme;
        }
    }

    /// <summary>Registers hard-coded default themes as fallback.</summary>
    public void RegisterDefaults()
    {
        // Dark Factory — the default
        Register(new ThemeDefinition());

        // Cyber
        Register(new ThemeDefinition
        {
            Id = "cyber",
            DisplayName = "Cyber",
            BackgroundPrimary = "#0A0A0F",
            BackgroundSecondary = "#0D1117",
            BackgroundTertiary = "#161B22",
            BackgroundPanel = "#0A0A0FE0",
            TextPrimary = "#00FF88",
            TextSecondary = "#00CC66",
            TextAccent = "#00FFFF",
            TextDisabled = "#334433",
            ButtonNormal = "#0D1117",
            ButtonHover = "#1A2233",
            ButtonActive = "#00FFFF",
            ButtonDisabled = "#111118",
            ButtonText = "#00FF88",
            BorderNormal = "#003322",
            BorderFocused = "#00FFFF",
            BorderHover = "#00FF88",
            InputBackground = "#050508",
            InputText = "#00FF88",
            AccentPrimary = "#00FFFF",
            AccentSecondary = "#00FF88",
            HighlightSelection = "#00FFFF30",
            ProgressBackground = "#0A0A0F",
            ProgressFill = "#00FF88",
            TooltipBackground = "#050508E8",
            TooltipText = "#00FF88",
            TooltipBorder = "#00FFFF",
            ScrollbarTrack = "#0A0A0F",
            ScrollbarThumb = "#003322",
            TabInactive = "#0D1117",
            TabActive = "#161B22"
        });

        // Medieval
        Register(new ThemeDefinition
        {
            Id = "medieval",
            DisplayName = "Medieval",
            BackgroundPrimary = "#2C1810",
            BackgroundSecondary = "#3D2417",
            BackgroundTertiary = "#4E3020",
            BackgroundPanel = "#2C1810E0",
            TextPrimary = "#F5DEB3",
            TextSecondary = "#D2B48C",
            TextAccent = "#DAA520",
            TextDisabled = "#6B4226",
            ButtonNormal = "#4E3020",
            ButtonHover = "#6B4226",
            ButtonActive = "#DAA520",
            ButtonDisabled = "#2C1810",
            ButtonText = "#F5DEB3",
            BorderNormal = "#6B4226",
            BorderFocused = "#DAA520",
            BorderHover = "#8B6914",
            InputBackground = "#1A0E08",
            InputText = "#F5DEB3",
            AccentPrimary = "#DAA520",
            AccentSecondary = "#8B4513",
            HighlightSelection = "#DAA52040",
            ProgressBackground = "#2C1810",
            ProgressFill = "#DAA520",
            TooltipBackground = "#1A0E08E8",
            TooltipText = "#F5DEB3",
            TooltipBorder = "#DAA520",
            ScrollbarTrack = "#2C1810",
            ScrollbarThumb = "#6B4226",
            TabInactive = "#3D2417",
            TabActive = "#4E3020"
        });

        // Neon
        Register(new ThemeDefinition
        {
            Id = "neon",
            DisplayName = "Neon",
            BackgroundPrimary = "#0D0221",
            BackgroundSecondary = "#150530",
            BackgroundTertiary = "#1A0640",
            BackgroundPanel = "#0D0221E0",
            TextPrimary = "#FF00FF",
            TextSecondary = "#CC00CC",
            TextAccent = "#00FFFF",
            TextDisabled = "#440044",
            ButtonNormal = "#1A0640",
            ButtonHover = "#2A0860",
            ButtonActive = "#FF00FF",
            ButtonDisabled = "#110220",
            ButtonText = "#FFFFFF",
            BorderNormal = "#6600AA",
            BorderFocused = "#FF00FF",
            BorderHover = "#AA00FF",
            InputBackground = "#060012",
            InputText = "#FF00FF",
            AccentPrimary = "#FF00FF",
            AccentSecondary = "#00FFFF",
            HighlightSelection = "#FF00FF30",
            ProgressBackground = "#0D0221",
            ProgressFill = "#FF00FF",
            TooltipBackground = "#060012E8",
            TooltipText = "#FF00FF",
            TooltipBorder = "#00FFFF",
            ScrollbarTrack = "#0D0221",
            ScrollbarThumb = "#6600AA",
            TabInactive = "#150530",
            TabActive = "#1A0640"
        });
    }

    public int Count => _themes.Count;
    public void Clear() => _themes.Clear();

    public IEnumerable<string> AvailableThemeIds => _themes.Keys;
}
