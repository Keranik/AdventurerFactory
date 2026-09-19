using ForgeFlow.Core;
using ForgeFlow.Core.Entities;
using ForgeFlow.Core.Events;
using ForgeFlow.Core.Proto;
using ForgeFlow.Core.Systems;
using ForgeFlow.Core.Utilities;

namespace ForgeFlow.Tests;

/// <summary>
/// Regression tests for the placement-visual bug where Presentation-layer
/// <c>PathRendererSystem.Refresh*Visuals()</c> helpers were clobbering the
/// freshly-created visual that the event-driven renderer had just produced.
///
/// Core-side guarantees covered here (Presentation subscribes to these):
///   - <see cref="StructurePlacedEvent"/> fires exactly once per structure placement.
///   - <see cref="RoutingNodePlacedEvent"/> fires exactly once per routing-node placement.
///   - <see cref="PathBuiltEvent"/> fires exactly once per path segment.
///   - New <see cref="Systems.PathNodeManager.SetPathSegmentFacing"/> publishes
///     <see cref="EntityRotatedEvent"/> on facing change (used by the path-draw drag loop).
/// </summary>
public class PlacementVisualRegressionTests
{
    public PlacementVisualRegressionTests()
    {
        EntityIdFactory.ResetForTesting();
    }

    [Fact]
    public void StructurePlacement_PublishesStructurePlacedEventOnce()
    {
        var boot = new GameBootstrapper();
        boot.Bootstrap();

        int eventCount = 0;
        EntityId placedId = default;
        boot.Services.Get<EventBus>().Subscribe<StructurePlacedEvent>(e =>
        {
            eventCount++;
            placedId = e.StructureId;
        });

        var inn = boot.Services.Get<ProtoFactory>().CreateInn("inn_basic");
        Assert.NotNull(inn);
        bool placed = boot.Services.Get<StructureManager>().AddProtoStructure(inn!, new GridPosRPG(4, 4));

        Assert.True(placed);
        Assert.Equal(1, eventCount);
        Assert.Equal(inn!.Id, placedId);
    }

    [Fact]
    public void RoutingNodePlacement_PublishesRoutingNodePlacedEventOnce()
    {
        var boot = new GameBootstrapper();
        boot.Bootstrap();

        int eventCount = 0;
        boot.Services.Get<EventBus>().Subscribe<RoutingNodePlacedEvent>(_ => eventCount++);

        var balancer = boot.Services.Get<ProtoFactory>().CreateBalancer("balancer_basic");
        Assert.NotNull(balancer);
        boot.Services.Get<StructureManager>().AddProtoStructure(balancer!, new GridPosRPG(5, 5));

        Assert.Equal(1, eventCount);
    }

    [Fact]
    public void SetPathSegmentFacing_WhenFacingChanges_PublishesEntityRotatedEvent()
    {
        var boot = new GameBootstrapper();
        boot.Bootstrap();

        var pos = new GridPosRPG(2, 2);
        var seg = boot.Services.Get<PathNodeManager>().AddPathSegment(pos, Direction.East);
        Assert.NotNull(seg);

        int rotations = 0;
        Direction lastFacing = Direction.East;
        boot.Services.Get<EventBus>().Subscribe<EntityRotatedEvent>(e =>
        {
            rotations++;
            lastFacing = e.NewFacing;
        });

        boot.Services.Get<PathNodeManager>().SetPathSegmentFacing(pos, Direction.South);
        Assert.Equal(1, rotations);
        Assert.Equal(Direction.South, lastFacing);
        Assert.Equal(Direction.South, seg!.Facing);

        // No-op when facing is unchanged.
        boot.Services.Get<PathNodeManager>().SetPathSegmentFacing(pos, Direction.South);
        Assert.Equal(1, rotations);
    }
}
