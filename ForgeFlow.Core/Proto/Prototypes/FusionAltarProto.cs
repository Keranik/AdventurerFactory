namespace ForgeFlow.Core.Proto.Prototypes;

/// <summary>
/// Proto for a fusion altar. Fuses heroes or items into stronger versions.
/// </summary>
public sealed class FusionAltarProto : RecipeProtoBase
{
    public int RequiredInputCount { get; set; } = 2;
}
