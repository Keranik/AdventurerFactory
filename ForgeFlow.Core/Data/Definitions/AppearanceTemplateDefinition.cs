namespace ForgeFlow.Core.Data.Definitions;

public sealed class AppearanceTemplateDefinition
{
    public string Id { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string BaseBody { get; set; } = "human_default";
    public string Hair { get; set; } = "short_01";
    public string Face { get; set; } = "face_01";
    public string PaletteId { get; set; } = "default";
    public Dictionary<string, string> PartOverrides { get; set; } = new();
    public int RequiredTier { get; set; }
    public string? RequiredClassId { get; set; }
    public List<string> Tags { get; set; } = new();
}
