namespace ForgeFlow.Core.Proto.Prototypes;

/// <summary>
/// Proto for a tool station. Defines repair rate.
/// </summary>
public sealed class ToolStationProto : ActivityProtoBase
{
    public float RepairRate { get; set; } = 25f;
}
