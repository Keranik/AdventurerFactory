namespace ForgeFlow.Core.Proto.Prototypes;

/// <summary>
/// Abstract proto base for storage structures that act as resource buffers.
/// Mirrors <see cref="ForgeFlow.Core.Entities.StockpileEntity"/> in the Logic layer.
/// StockpileProto and future storage types inherit from this.
/// </summary>
public abstract class StockpileProtoBase : StructureProtoBase
{
    /// <summary>Maximum total items this storage can hold.</summary>
    public int MaxCapacity { get; set; } = 100;
}
