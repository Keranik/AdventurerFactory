namespace ForgeFlow.Core.Proto.Prototypes;

/// <summary>
/// Proto for an Inn. Defines rest rate, max occupants, and rest recipe.
/// </summary>
public sealed class InnProto : ActivityProtoBase
{
    public float RestRate { get; set; } = 20f;
    public string RestRecipeId { get; set; } = "rest_basic";
}
