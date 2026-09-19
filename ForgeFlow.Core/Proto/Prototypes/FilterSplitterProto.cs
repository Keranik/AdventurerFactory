using ForgeFlow.Core.Entities;

namespace ForgeFlow.Core.Proto.Prototypes;

/// <summary>
/// Proto for a filter splitter. Multi-rule routing structure.
/// Supports an optional single-item filter and multi-rule routing.
/// </summary>
public sealed class FilterSplitterProto : RoutingProtoBase
{
    /// <summary>Default single-item filter. Null = no item filter active.</summary>
    public string? DefaultFilteredItemId { get; set; }

    /// <summary>Output direction for villagers matching the single-item filter.</summary>
    public Direction DefaultFilteredOutputDirection { get; set; } = Direction.West;
}
