namespace ForgeFlow.Core.Proto.Prototypes;

/// <summary>
/// Proto for a forge structure. Crafts items from recipes.
/// </summary>
public sealed class ForgeProto : RecipeProtoBase
{
    public string DefaultRecipeId { get; set; } = "iron_sword";
}
