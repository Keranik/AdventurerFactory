using ForgeFlow.Core.Entities;

namespace ForgeFlow.Core.Proto.Prototypes;

/// <summary>
/// Proto definition for a structure. JSON-driven, moddable, readonly where possible.
/// </summary>
public class StructureProtoBase : ProtoBase
{
    public float ProcessingDuration { get; set; } = 2.0f;
    public int MaxOutputQueueSize { get; set; } = 5;
    public int Tier { get; set; } = 1;
    public Dictionary<string, float> CustomProperties { get; set; } = new();
}
