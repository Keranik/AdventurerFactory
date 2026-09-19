namespace ForgeFlow.Core.Proto.Prototypes;

/// <summary>Path segment type for routing logic.</summary>
public enum PathNodeType
{
    Straight,
    Corner,
    Splitter,
    Merger,
    Endpoint
}

/// <summary>User-configurable routing mode for Splitter nodes.</summary>
public enum SplitMode
{
    /// <summary>Alternates between primary and secondary outputs.</summary>
    RoundRobin,
    /// <summary>Primary output preferred; secondary used only when primary is full.</summary>
    Priority,
    /// <summary>Only entities matching SplitFilterTag go to secondary; rest go primary.</summary>
    Filtered,
    /// <summary>Weighted random: SplitWeight% go to primary, rest to secondary.</summary>
    Weighted
}

/// <summary>
/// Proto for a path segment. Defines speed, capacity, node type, and routing.
/// </summary>
public sealed class PathSegmentProto : ProtoBase
{
    public PathNodeType NodeType { get; set; } = PathNodeType.Straight;
    public float SpeedMultiplier { get; set; } = 1.0f;
    public int Capacity { get; set; } = 3;
    public bool AllowsHeroes { get; set; } = true;
    public bool AllowsItems { get; set; } = true;
    public SplitMode SplitMode { get; set; } = SplitMode.RoundRobin;
    public string? SplitFilterTag { get; set; }
    public int SplitWeight { get; set; } = 50;
}
