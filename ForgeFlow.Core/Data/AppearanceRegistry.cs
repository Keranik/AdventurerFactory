using System.Diagnostics.CodeAnalysis;
using ForgeFlow.Core.Data.Definitions;

namespace ForgeFlow.Core.Data;

public sealed class AppearanceRegistry : IRegistry
{
    private readonly Dictionary<string, AppearancePartDefinition> _parts = new();
    private readonly Dictionary<string, AppearanceTemplateDefinition> _templates = new();
    private readonly Dictionary<string, ColorPaletteDefinition> _palettes = new();

    // --- Parts ---

    public void RegisterPart(AppearancePartDefinition part)
    {
        _parts[part.Id] = part;
    }

    public AppearancePartDefinition? GetPart(string id)
    {
        return _parts.TryGetValue(id, out var part) ? part : null;
    }

    public bool TryGetPart(string id, [NotNullWhen(true)] out AppearancePartDefinition? part)
    {
        return _parts.TryGetValue(id, out part);
    }

    public IEnumerable<AppearancePartDefinition> GetAllParts() => _parts.Values;

    public IEnumerable<AppearancePartDefinition> GetPartsByCategory(string category)
    {
        foreach (var part in _parts.Values)
        {
            if (part.Category == category) yield return part;
        }
    }

    // --- Templates ---

    public void RegisterTemplate(AppearanceTemplateDefinition template)
    {
        _templates[template.Id] = template;
    }

    public AppearanceTemplateDefinition? GetTemplate(string id)
    {
        return _templates.TryGetValue(id, out var template) ? template : null;
    }

    public bool TryGetTemplate(string id, [NotNullWhen(true)] out AppearanceTemplateDefinition? template)
    {
        return _templates.TryGetValue(id, out template);
    }

    public IEnumerable<AppearanceTemplateDefinition> GetAllTemplates() => _templates.Values;

    // --- Palettes ---

    public void RegisterPalette(ColorPaletteDefinition palette)
    {
        _palettes[palette.Id] = palette;
    }

    public ColorPaletteDefinition? GetPalette(string id)
    {
        return _palettes.TryGetValue(id, out var palette) ? palette : null;
    }

    public bool TryGetPalette(string id, [NotNullWhen(true)] out ColorPaletteDefinition? palette)
    {
        return _palettes.TryGetValue(id, out palette);
    }

    public IEnumerable<ColorPaletteDefinition> GetAllPalettes() => _palettes.Values;

    public IEnumerable<ColorPaletteDefinition> GetPalettesByTier(int tier)
    {
        foreach (var palette in _palettes.Values)
        {
            if (palette.RequiredTier <= tier) yield return palette;
        }
    }

    // --- Utility ---

    public void Clear()
    {
        _parts.Clear();
        _templates.Clear();
        _palettes.Clear();
    }

    public int PartCount => _parts.Count;
    public int TemplateCount => _templates.Count;
    public int PaletteCount => _palettes.Count;
    public int Count => _parts.Count + _templates.Count + _palettes.Count;

    public void RegisterDefaults()
    {
        // --- Parts (expanded for Milestone 3) ---
        RegisterPart(new AppearancePartDefinition { Id = "hair_short_01", Category = "hair", DisplayName = "Short Hair" });
        RegisterPart(new AppearancePartDefinition { Id = "hair_long_01", Category = "hair", DisplayName = "Long Hair" });
        RegisterPart(new AppearancePartDefinition { Id = "hair_mohawk_01", Category = "hair", DisplayName = "Mohawk" });
        RegisterPart(new AppearancePartDefinition { Id = "hair_braids_01", Category = "hair", DisplayName = "Braids" });
        RegisterPart(new AppearancePartDefinition { Id = "hair_bald", Category = "hair", DisplayName = "Bald" });
        RegisterPart(new AppearancePartDefinition { Id = "hair_ponytail_01", Category = "hair", DisplayName = "Ponytail", RequiredTier = 2 });
        RegisterPart(new AppearancePartDefinition { Id = "hair_crown_flame", Category = "hair", DisplayName = "Flame Crown", RequiredTier = 5 });
        RegisterPart(new AppearancePartDefinition { Id = "face_01", Category = "face", DisplayName = "Standard Face" });
        RegisterPart(new AppearancePartDefinition { Id = "face_scarred_01", Category = "face", DisplayName = "Scarred Face" });
        RegisterPart(new AppearancePartDefinition { Id = "face_gentle_01", Category = "face", DisplayName = "Gentle Face" });
        RegisterPart(new AppearancePartDefinition { Id = "face_fierce_01", Category = "face", DisplayName = "Fierce Face", RequiredTier = 2 });
        RegisterPart(new AppearancePartDefinition { Id = "face_ancient_01", Category = "face", DisplayName = "Ancient Face", RequiredTier = 4 });
        RegisterPart(new AppearancePartDefinition { Id = "cape_basic", Category = "cape", DisplayName = "Basic Cape" });
        RegisterPart(new AppearancePartDefinition { Id = "cape_royal", Category = "cape", DisplayName = "Royal Cape", RequiredTier = 3 });
        RegisterPart(new AppearancePartDefinition { Id = "cape_tattered", Category = "cape", DisplayName = "Tattered Cape" });
        RegisterPart(new AppearancePartDefinition { Id = "cape_void", Category = "cape", DisplayName = "Void Cloak", RequiredTier = 5 });
        RegisterPart(new AppearancePartDefinition { Id = "tattoo_tribal", Category = "tattoo", DisplayName = "Tribal Tattoo", RequiredTier = 2 });
        RegisterPart(new AppearancePartDefinition { Id = "tattoo_runic", Category = "tattoo", DisplayName = "Runic Tattoo", RequiredTier = 3 });
        RegisterPart(new AppearancePartDefinition { Id = "accessory_earring", Category = "accessory", DisplayName = "Gold Earring" });
        RegisterPart(new AppearancePartDefinition { Id = "accessory_eyepatch", Category = "accessory", DisplayName = "Eye Patch" });

        // --- Templates (expanded for Milestone 3) ---
        RegisterTemplate(new AppearanceTemplateDefinition
        {
            Id = "default",
            DisplayName = "Default",
            BaseBody = "human_default",
            Hair = "hair_short_01",
            Face = "face_01",
            PaletteId = "default"
        });
        RegisterTemplate(new AppearanceTemplateDefinition
        {
            Id = "warrior_heavy",
            DisplayName = "Heavy Warrior",
            BaseBody = "human_default",
            Hair = "hair_mohawk_01",
            Face = "face_scarred_01",
            PaletteId = "iron",
            RequiredClassId = "warrior"
        });
        RegisterTemplate(new AppearanceTemplateDefinition
        {
            Id = "mage_scholar",
            DisplayName = "Scholar Mage",
            BaseBody = "human_default",
            Hair = "hair_long_01",
            Face = "face_gentle_01",
            PaletteId = "arcane_blue",
            RequiredClassId = "mage"
        });
        RegisterTemplate(new AppearanceTemplateDefinition
        {
            Id = "ranger_wild",
            DisplayName = "Wild Ranger",
            BaseBody = "human_default",
            Hair = "hair_braids_01",
            Face = "face_fierce_01",
            PaletteId = "forest",
            RequiredClassId = "ranger"
        });
        RegisterTemplate(new AppearanceTemplateDefinition
        {
            Id = "void_champion",
            DisplayName = "Void Champion",
            BaseBody = "human_default",
            Hair = "hair_crown_flame",
            Face = "face_ancient_01",
            PaletteId = "void_purple",
            RequiredTier = 5,
            PartOverrides = new() { { "cape", "cape_void" } }
        });

        // --- Palettes (expanded for Milestone 3) ---
        RegisterPalette(new ColorPaletteDefinition
        {
            Id = "default",
            DisplayName = "Default",
            Colors = new() { { "primary", "#8B8B8B" }, { "secondary", "#4A4A4A" }, { "accent", "#FFFFFF" } }
        });
        RegisterPalette(new ColorPaletteDefinition
        {
            Id = "iron",
            DisplayName = "Iron",
            Colors = new() { { "primary", "#6B6B6B" }, { "secondary", "#3D3D3D" }, { "accent", "#A0A0B0" } }
        });
        RegisterPalette(new ColorPaletteDefinition
        {
            Id = "crimson",
            DisplayName = "Crimson",
            Colors = new() { { "primary", "#8B0000" }, { "secondary", "#4A0000" }, { "accent", "#FF4444" } },
            RequiredTier = 2
        });
        RegisterPalette(new ColorPaletteDefinition
        {
            Id = "royal_gold",
            DisplayName = "Royal Gold",
            Colors = new() { { "primary", "#DAA520" }, { "secondary", "#B8860B" }, { "accent", "#FFD700" } },
            RequiredTier = 3
        });
        RegisterPalette(new ColorPaletteDefinition
        {
            Id = "void_purple",
            DisplayName = "Void Purple",
            Colors = new() { { "primary", "#4B0082" }, { "secondary", "#2E0854" }, { "accent", "#9B30FF" } },
            RequiredTier = 5
        });
        RegisterPalette(new ColorPaletteDefinition
        {
            Id = "forest",
            DisplayName = "Forest",
            Colors = new() { { "primary", "#228B22" }, { "secondary", "#006400" }, { "accent", "#90EE90" } }
        });
        RegisterPalette(new ColorPaletteDefinition
        {
            Id = "arcane_blue",
            DisplayName = "Arcane Blue",
            Colors = new() { { "primary", "#1E90FF" }, { "secondary", "#00008B" }, { "accent", "#87CEEB" } },
            RequiredTier = 2
        });
        RegisterPalette(new ColorPaletteDefinition
        {
            Id = "ember",
            DisplayName = "Ember",
            Colors = new() { { "primary", "#FF4500" }, { "secondary", "#8B2500" }, { "accent", "#FFA500" } },
            RequiredTier = 3
        });
        RegisterPalette(new ColorPaletteDefinition
        {
            Id = "frost",
            DisplayName = "Frost",
            Colors = new() { { "primary", "#B0E0E6" }, { "secondary", "#4682B4" }, { "accent", "#F0FFFF" } },
            RequiredTier = 4
        });
    }
}
