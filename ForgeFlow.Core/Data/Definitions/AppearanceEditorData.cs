namespace ForgeFlow.Core.Data.Definitions;

/// <summary>
/// Defines the full set of editable appearance slots for the in-game
/// Appearance Editor. Each slot references a part category plus the
/// currently assigned part ID and any per-slot color overrides.
/// </summary>
public sealed class AppearanceEditorData
{
    public ulong HeroSeed { get; set; }
    public string ActiveTemplateId { get; set; } = "default";
    public string ActivePaletteId { get; set; } = "default";
    public Dictionary<string, string> SlotAssignments { get; set; } = new();
    public Dictionary<string, string> SlotColorOverrides { get; set; } = new();
    public List<string> FavoriteParts { get; set; } = new();
    public List<string> FavoritePalettes { get; set; } = new();
}
