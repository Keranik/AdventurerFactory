namespace ForgeFlow.Core.Proto.Prototypes;

/// <summary>
/// Proto for an armory. Defines stamina restore rate.
/// </summary>
public sealed class ArmoryProto : ActivityProtoBase
{
    public float StaminaRestoreRate { get; set; } = 20f;
}
