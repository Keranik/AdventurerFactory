namespace ForgeFlow.Core.Data.Definitions;

/// <summary>
/// Defines a village building that retired heroes can be assigned to.
/// Buildings provide passive bonuses (stat boosts, resource gen, research speed)
/// based on the level and class of the heroes assigned.
/// </summary>
public sealed class VillageBuildingDefinition
{
    public string Id { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Category { get; set; } = "production"; // "production", "training", "research", "decoration"
    public int MaxAssignedHeroes { get; set; } = 3;
    public int RequiredTier { get; set; }
    public Dictionary<string, float> PassiveBonuses { get; set; } = new(); // "spawn_rate" → 0.1, etc.
    public Dictionary<string, int> BuildCost { get; set; } = new(); // "gold" → 100, etc.
    public List<string> Tags { get; set; } = new();
}
