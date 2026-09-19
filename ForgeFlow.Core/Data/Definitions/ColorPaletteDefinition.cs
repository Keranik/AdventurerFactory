namespace ForgeFlow.Core.Data.Definitions;

public sealed class ColorPaletteDefinition
{
    public string Id { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public Dictionary<string, string> Colors { get; set; } = new(); // "primary" → "#FF0000", etc.
    public int RequiredTier { get; set; }
    public List<string> Tags { get; set; } = new();
}
