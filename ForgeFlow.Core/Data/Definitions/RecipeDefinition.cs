namespace ForgeFlow.Core.Data.Definitions;

public sealed class RecipeInput
{
    public string ItemId { get; set; } = string.Empty;
    public int Quantity { get; set; } = 1;
}

public sealed class RecipeDefinition
{
    public string Id { get; set; } = string.Empty;
    public List<RecipeInput> Inputs { get; set; } = new();
    public int EssenceCost { get; set; }
    public string OutputItemId { get; set; } = string.Empty;
    public int OutputQuantity { get; set; } = 1;
    public int RequiredTier { get; set; }
    public float CraftDuration { get; set; } = 4.0f;
}
