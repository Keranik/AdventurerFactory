namespace ForgeFlow.Core.Data.Definitions;

public sealed class AppearancePartDefinition
{
    public string Id { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty; // "hair", "face", "helmet", "cape", etc.
    public string DisplayName { get; set; } = string.Empty;
    public int RequiredTier { get; set; }
    public string? RequiredClassId { get; set; }
    public Dictionary<string, string> DefaultColors { get; set; } = new();
    public List<string> Tags { get; set; } = new();
}
