using ForgeFlow.Core.Entities;
using ForgeFlow.Core.Proto.Prototypes;

namespace ForgeFlow.Core.Proto.Logic;

/// <summary>
/// Runtime logic for a path segment. Supports splitter junctions,
/// capacity limits, and user-configurable routing rules
/// (round-robin, priority, filtered, weighted).
/// </summary>
public sealed class PathSegmentLogic : StructureBase, IRotatable
{
    public PathSegmentLogic(EntityId id) : base(id) { }

    /// <inheritdoc />
    public override IReadOnlyList<GridPosRPG> GetFootprint() => new[] { Position };

    public PathNodeType NodeType { get; set; } = PathNodeType.Straight;
    public Direction Facing { get; set; }
    public float SpeedMultiplier { get; set; } = 1.0f;
    public int Capacity { get; set; } = 3;
    public bool AllowsHeroes { get; set; } = true;
    public bool AllowsItems { get; set; } = true;
    public bool IsBlocked { get; set; }

    /// <summary>Routing mode for Splitter nodes.</summary>
    public SplitMode SplitMode { get; set; } = SplitMode.RoundRobin;

    /// <summary>For Filtered mode: entities with this tag go to SplitTargetId.</summary>
    public string? SplitFilterTag { get; set; }

    /// <summary>For Weighted mode: percentage (0-100) that goes to primary output.</summary>
    public int SplitWeight { get; set; } = 50;

    /// <summary>Primary output segment ID (straight/corner/merger).</summary>
    public ulong? NextSegmentId { get; set; }

    /// <summary>Previous segment feeding into this one.</summary>
    public ulong? PrevSegmentId { get; set; }

    /// <summary>
    /// For Splitter nodes: secondary output.
    /// </summary>
    public ulong? SplitTargetId { get; set; }

    /// <summary>Connected structure ID for walk-by interaction.</summary>
    public ulong? ConnectedStructureId { get; set; }

    /// <summary>Entities currently on this segment.</summary>
    public List<ulong> OccupantIds { get; } = new();

    /// <summary>Wait queue: entities blocked trying to enter this segment.</summary>
    public List<ulong> WaitQueue { get; } = new();

    private bool _splitToggle;
    private int _splitCounter;

    public GridPosRPG OutputPosition => Position.Neighbor(Facing);

    public void InitializeFromProto(PathSegmentProto proto)
    {
        ProtoId = proto.Id;
        NodeType = proto.NodeType;
        SpeedMultiplier = proto.SpeedMultiplier;
        Capacity = proto.Capacity;
        AllowsHeroes = proto.AllowsHeroes;
        AllowsItems = proto.AllowsItems;
        SplitMode = proto.SplitMode;
        SplitFilterTag = proto.SplitFilterTag;
        SplitWeight = proto.SplitWeight;
    }

    /// <summary>
    /// For Splitter nodes, returns the next output ID based on the configured SplitMode.
    /// For all other node types, returns NextSegmentId.
    /// </summary>
    public ulong? ResolveNextSegment(string? entityTag = null)
    {
        if (NodeType != PathNodeType.Splitter || SplitTargetId == null)
        {
            return NextSegmentId;
        }

        switch (SplitMode)
        {
            case SplitMode.RoundRobin:
                _splitToggle = !_splitToggle;
                return _splitToggle ? NextSegmentId : SplitTargetId;

            case SplitMode.Priority:
                // Primary preferred; use secondary only when primary full
                return NextSegmentId;

            case SplitMode.Filtered:
                if (entityTag != null && entityTag == SplitFilterTag)
                    return SplitTargetId;
                return NextSegmentId;

            case SplitMode.Weighted:
                _splitCounter++;
                return (_splitCounter % 100) < SplitWeight ? NextSegmentId : SplitTargetId;

            default:
                return NextSegmentId;
        }
    }

    /// <summary>
    /// For Priority mode: checks if primary is full and falls back to secondary.
    /// </summary>
    public ulong? ResolveWithFallback(bool primaryFull)
    {
        if (NodeType != PathNodeType.Splitter || SplitTargetId == null)
            return NextSegmentId;

        if (SplitMode == SplitMode.Priority && primaryFull)
            return SplitTargetId;

        return ResolveNextSegment();
    }

    /// <summary>
    /// Attempts to add an occupant to this segment.
    /// Returns false if at capacity or blocked.
    /// </summary>
    public bool TryAddOccupant(ulong entityId)
    {
        if (IsBlocked || OccupantIds.Count >= Capacity) return false;
        OccupantIds.Add(entityId);
        return true;
    }

    public bool RemoveOccupant(ulong entityId)
    {
        return OccupantIds.Remove(entityId);
    }

    /// <summary>Adds an entity to the wait queue when the segment is full.</summary>
    public void EnqueueWaiting(ulong entityId)
    {
        if (!WaitQueue.Contains(entityId))
            WaitQueue.Add(entityId);
    }

    /// <summary>Dequeues the next waiting entity. Returns 0 if none.</summary>
    public ulong DequeueWaiting()
    {
        if (WaitQueue.Count == 0) return 0;
        var id = WaitQueue[0];
        WaitQueue.RemoveAt(0);
        return id;
    }

    /// <summary>Returns true if this segment has adjacent-structure interaction (walk-by).</summary>
    public bool HasAdjacentStructure => ConnectedStructureId.HasValue;

    /// <summary>
    /// Rotates the segment 90° clockwise. Updates Facing and OutputPosition.
    /// Caller (PathNodeManager) is responsible for unlinking/relinking neighbors.
    /// </summary>
    public void Rotate(GridPosRPG? anchorPosition = null)
    {
        Facing = Facing.RotateClockwise();
    }

    public override void Tick(float deltaTime)
    {
        // Unblock when occupants drop below capacity
        if (IsBlocked && OccupantIds.Count < Capacity)
        {
            IsBlocked = false;
        }
    }
}
