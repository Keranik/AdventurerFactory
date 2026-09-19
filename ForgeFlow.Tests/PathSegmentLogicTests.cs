using ForgeFlow.Core.Entities;
using ForgeFlow.Core.Proto.Logic;
using ForgeFlow.Core.Proto.Prototypes;

namespace ForgeFlow.Tests;

/// <summary>Tests for PathSegmentLogic — occupancy, wait queue, splitting, and output position.</summary>
public class PathSegmentLogicTests
{
    [Fact]
    public void PathSegmentLogic_OutputPosition_MatchesFacing()
    {
        var seg = new PathSegmentLogic(EntityId.Next())
        {
            Position = new GridPosRPG(5, 5),
            Facing = Direction.East
        };
        Assert.Equal(new GridPosRPG(6, 5), seg.OutputPosition);

        seg.Facing = Direction.North;
        Assert.Equal(new GridPosRPG(5, 6), seg.OutputPosition);
    }

    [Fact]
    public void PathSegmentLogic_TryAddOccupant_RespectsCapacity()
    {
        var seg = new PathSegmentLogic(EntityId.Next()) { Capacity = 2 };

        Assert.True(seg.TryAddOccupant(1));
        Assert.True(seg.TryAddOccupant(2));
        Assert.False(seg.TryAddOccupant(3));
    }

    [Fact]
    public void PathSegmentLogic_TryAddOccupant_RejectsWhenBlocked()
    {
        var seg = new PathSegmentLogic(EntityId.Next()) { Capacity = 3, IsBlocked = true };

        Assert.False(seg.TryAddOccupant(1));
    }

    [Fact]
    public void PathSegmentLogic_RemoveOccupant_Works()
    {
        var seg = new PathSegmentLogic(EntityId.Next()) { Capacity = 3 };
        seg.TryAddOccupant(1);
        seg.TryAddOccupant(2);

        Assert.True(seg.RemoveOccupant(1));
        Assert.False(seg.RemoveOccupant(99));
    }

    [Fact]
    public void PathSegmentLogic_WaitQueue_EnqueueDequeue()
    {
        var seg = new PathSegmentLogic(EntityId.Next());

        seg.EnqueueWaiting(10);
        seg.EnqueueWaiting(20);
        seg.EnqueueWaiting(10); // Duplicate — should not add

        Assert.Equal(2, seg.WaitQueue.Count);
        Assert.Equal(10ul, seg.DequeueWaiting());
        Assert.Equal(20ul, seg.DequeueWaiting());
        Assert.Equal(0ul, seg.DequeueWaiting()); // Empty
    }

    [Fact]
    public void PathSegmentLogic_Splitter_RoundRobin()
    {
        var seg = new PathSegmentLogic(EntityId.Next())
        {
            NodeType = PathNodeType.Splitter,
            SplitMode = SplitMode.RoundRobin,
            NextSegmentId = 100,
            SplitTargetId = 200
        };

        var first = seg.ResolveNextSegment();
        var second = seg.ResolveNextSegment();

        Assert.NotEqual(first, second);
        Assert.True(first == 100 || first == 200);
        Assert.True(second == 100 || second == 200);
    }

    [Fact]
    public void PathSegmentLogic_Splitter_FilteredMode()
    {
        var seg = new PathSegmentLogic(EntityId.Next())
        {
            NodeType = PathNodeType.Splitter,
            SplitMode = SplitMode.Filtered,
            SplitFilterTag = "warrior",
            NextSegmentId = 100,
            SplitTargetId = 200
        };

        Assert.Equal(200ul, seg.ResolveNextSegment("warrior"));
        Assert.Equal(100ul, seg.ResolveNextSegment("mage"));
        Assert.Equal(100ul, seg.ResolveNextSegment(null));
    }

    [Fact]
    public void PathSegmentLogic_Splitter_PriorityMode_WithFallback()
    {
        var seg = new PathSegmentLogic(EntityId.Next())
        {
            NodeType = PathNodeType.Splitter,
            SplitMode = SplitMode.Priority,
            NextSegmentId = 100,
            SplitTargetId = 200
        };

        Assert.Equal(100ul, seg.ResolveWithFallback(primaryFull: false));
        Assert.Equal(200ul, seg.ResolveWithFallback(primaryFull: true));
    }

    [Fact]
    public void PathSegmentLogic_NonSplitter_IgnoresSplitLogic()
    {
        var seg = new PathSegmentLogic(EntityId.Next())
        {
            NodeType = PathNodeType.Straight,
            NextSegmentId = 100,
            SplitTargetId = 200
        };

        Assert.Equal(100ul, seg.ResolveNextSegment());
    }

    [Fact]
    public void PathSegmentLogic_Tick_UnblocksWhenCapacityFrees()
    {
        var seg = new PathSegmentLogic(EntityId.Next()) { Capacity = 2, IsBlocked = true };
        seg.OccupantIds.Add(1);

        seg.Tick(0.1f);
        Assert.False(seg.IsBlocked);
    }

    [Fact]
    public void PathSegmentLogic_HasAdjacentStructure()
    {
        var seg = new PathSegmentLogic(EntityId.Next())
        {
            NodeType = PathNodeType.Straight,
            ConnectedStructureId = 42
        };
        Assert.True(seg.HasAdjacentStructure);

        seg.ConnectedStructureId = null;
        Assert.False(seg.HasAdjacentStructure);
    }
}
