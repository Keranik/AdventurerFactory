using ForgeFlow.Core.Commands;
using ForgeFlow.Core.Entities;
using ForgeFlow.Core.Events;
using ForgeFlow.Core.Proto;
using ForgeFlow.Core.Proto.Logic;
using ForgeFlow.Core.Proto.Prototypes;

namespace ForgeFlow.Core.Systems;

/// <summary>
/// Single source of truth for all path graph operations: creation, removal,
/// linking, eviction, and spatial queries. Coordinates between EntityManager
/// (low-level entity storage), TileManager (grid occupancy), and
/// PathTrafficSystem (movement along paths).
/// Self-subscribes to EarlyTick (activate waiting villagers) and LateTick (tick path segments).
/// </summary>
public sealed class PathNodeManager : IGameSystem, IDisposable
{
    private readonly EntityManager _entityManager;
    private readonly TileManager _tileManager;
    private readonly VillagerSystem _villagerSystem;
    private readonly PathTrafficSystem _pathTraffic;
    private readonly EventBus _eventBus;
    private readonly CommandBus _commandBus;
    private readonly TutorialSystem _tutorialSystem;
    private readonly GatingLimits _gatingLimits;
    private readonly ResearchManager _researchManager;
    private readonly ItemManager _itemManager;

    public PathNodeManager(
        EntityManager entityManager,
        TileManager tileManager,
        VillagerSystem villagerSystem,
        PathTrafficSystem pathTraffic,
        EventBus eventBus,
        CommandBus commandBus,
        TutorialSystem tutorialSystem,
        GatingLimits gatingLimits,
        ResearchManager researchManager,
        ItemManager itemManager)
    {
        _entityManager = entityManager;
        _tileManager = tileManager;
        _villagerSystem = villagerSystem;
        _pathTraffic = pathTraffic;
        _eventBus = eventBus;
        _commandBus = commandBus;
        _tutorialSystem = tutorialSystem;
        _gatingLimits = gatingLimits;
        _researchManager = researchManager;
        _itemManager = itemManager;

        _eventBus.Subscribe<SimulationEarlyTickEvent>(OnEarlyTick);
        _eventBus.Subscribe<SimulationLateTickEvent>(OnLateTick);

        _commandBus.Register<DrawPathCommand>(HandleDrawPath);
    }

    public void Dispose()
    {
        _eventBus.Unsubscribe<SimulationEarlyTickEvent>(OnEarlyTick);
        _eventBus.Unsubscribe<SimulationLateTickEvent>(OnLateTick);
        _commandBus.Unregister<DrawPathCommand>();
    }

    private void OnEarlyTick(SimulationEarlyTickEvent e) => ActivateWaitingVillagers();
    private void OnLateTick(SimulationLateTickEvent e) => TickPaths(e.DeltaTime);

    // ── Command Handlers ────────────────────────────────────────────

    /// <summary>
    /// Handles <see cref="DrawPathCommand"/>: validates gold, deducts cost,
    /// places the segment, publishes GoldChangedEvent, and returns success/fail.
    /// This is the single source of truth for path segment placement transactions.
    /// </summary>
    private CommandResult HandleDrawPath(DrawPathCommand cmd)
    {
        int cost = EconomyConfig.PathSegmentCost;
        int oldGold = _itemManager.GetStock("gold");

        if (!_itemManager.TrySpendStock("gold", cost))
        {
            return CommandResult.Fail("Not enough gold");
        }

        var segment = AddPathSegment(cmd.Position, cmd.Facing, cmd.NodeType);
        if (segment == null)
        {
            // Placement failed — refund gold
            _itemManager.AddStock("gold", cost);
            return CommandResult.Fail("Placement blocked");
        }

        _eventBus.Publish(new GoldChangedEvent(oldGold, _itemManager.GetStock("gold"), "Path segment"));
        return CommandResult.Ok();
    }

    // ── Proxy Properties (for convenient access) ─────────────────────

    /// <summary>All path segments keyed by ID.</summary>
    public IReadOnlyDictionary<EntityId, PathSegmentLogic> PathSegments => _entityManager.PathSegments;

    // ── Path Segment CRUD ────────────────────────────────────────────

    /// <summary>
    /// Adds a path segment using the current research tier from ResearchManager.
    /// </summary>
    public PathSegmentLogic? AddPathSegment(
        GridPosRPG position,
        Direction facing,
        PathNodeType nodeType = PathNodeType.Straight)
    {
        return AddPathSegment(position, facing, _researchManager.CurrentTier, nodeType);
    }

    /// <summary>
    /// Adds a path segment to the world and auto-links to neighbors.
    /// Legacy gating limits removed — placement is limited only by gold/resources
    /// (enforced by GameplayFlowSystem.BuyAndPlace*).
    /// Returns the created segment.
    /// </summary>
    public PathSegmentLogic? AddPathSegment(
        GridPosRPG position,
        Direction facing,
        int currentResearchTier,
        PathNodeType nodeType = PathNodeType.Straight)
    {
        var segment = new PathSegmentLogic(EntityId.Next())
        {
            Position = position,
            Facing = facing,
            NodeType = nodeType
        };

        _entityManager.AddPathSegment(segment);

        // Auto-link to adjacent segments
        AutoLinkPath(segment);

        _eventBus.Publish(new PathBuiltEvent(segment.Id, position));
        _tutorialSystem.AdvanceCondition(TutorialConditionType.BuildPath);
        return segment;
    }

    /// <summary>
    /// Removes a path segment using the injected ResourceManager.
    /// </summary>
    public void RemovePathSegment(GridPosRPG position)
    {
        RemovePathSegment(position, _itemManager);
    }

    /// <summary>
    /// Removes a path segment from the world. Evicts any villagers on it
    /// back to the pool and returns their inventory to resource stocks.
    /// </summary>
    public void RemovePathSegment(GridPosRPG position, ItemManager itemManager)
    {
        var segment = _entityManager.GetPathSegmentAt(position);
        if (segment == null) { return; }
        var segmentId = segment.Id;

        // Evict villagers currently on this segment
        EvictVillagersFromSegment(segmentId, segment, itemManager);

        // Unlink from neighbors
        if (segment.PrevSegmentId.HasValue && _entityManager.GetPathSegment(new EntityId(segment.PrevSegmentId.Value)) is { } prev)
        {
            prev.NextSegmentId = null;
        }
        if (segment.NextSegmentId.HasValue && _entityManager.GetPathSegment(new EntityId(segment.NextSegmentId.Value)) is { } next)
        {
            next.PrevSegmentId = null;
        }

        _entityManager.UnregisterPathSegment(segmentId, position);
        _eventBus.Publish(new PathRemovedEvent(segmentId, position));
    }

    /// <summary>
    /// Creates a path line using the current research tier from ResearchManager.
    /// </summary>
    public List<PathSegmentLogic> CreatePathLine(GridPosRPG start, GridPosRPG end)
    {
        return CreatePathLine(start, end, _researchManager.CurrentTier);
    }

    /// <summary>
    /// Creates a path line between two grid positions by placing individual
    /// segments along the X axis first, then the Y axis.
    /// </summary>
    public List<PathSegmentLogic> CreatePathLine(GridPosRPG start, GridPosRPG end, int currentResearchTier)
    {
        var segments = new List<PathSegmentLogic>();
        int dx = end.X - start.X;
        int dy = end.Y - start.Y;

        int stepX = dx != 0 ? (dx > 0 ? 1 : -1) : 0;
        int stepY = dy != 0 ? (dy > 0 ? 1 : -1) : 0;

        int x = start.X;
        int y = start.Y;

        while (x != end.X)
        {
            var pos = new GridPosRPG(x, y);
            if (_tileManager.GetPathIdAt(pos) == null)
            {
                var seg = AddPathSegment(pos, stepX > 0 ? Direction.East : Direction.West, currentResearchTier);
                if (seg != null) { segments.Add(seg); }
            }
            x += stepX;
        }

        while (y != end.Y)
        {
            var pos = new GridPosRPG(x, y);
            if (_tileManager.GetPathIdAt(pos) == null)
            {
                var seg = AddPathSegment(pos, stepY > 0 ? Direction.North : Direction.South, currentResearchTier);
                if (seg != null) { segments.Add(seg); }
            }
            y += stepY;
        }

        var finalPos = new GridPosRPG(x, y);
        if (_tileManager.GetPathIdAt(finalPos) == null)
        {
            Direction finalDir = segments.Count > 0
                ? segments[segments.Count - 1].Facing
                : (stepX != 0 ? (stepX > 0 ? Direction.East : Direction.West) : (stepY > 0 ? Direction.North : Direction.South));
            var finalSeg = AddPathSegment(finalPos, finalDir, currentResearchTier);
            if (finalSeg != null) { segments.Add(finalSeg); }
        }

        return segments;
    }

    // ── Graph Linking ────────────────────────────────────────────────

    /// <summary>
    /// Auto-links a new path segment to its neighbors. Forward link checks
    /// the segment's output position; backward link checks cardinal neighbors
    /// whose output points toward this segment.
    /// </summary>
    private void AutoLinkPath(PathSegmentLogic newSegment)
    {
        // Forward link: if there's a segment at our output position, connect to it
        var outputPos = newSegment.OutputPosition;
        var nextSegment = _entityManager.GetPathSegmentAt(outputPos);
        if (nextSegment != null && newSegment.NextSegmentId == null)
        {
            newSegment.NextSegmentId = nextSegment.Id;
            if (nextSegment.PrevSegmentId == null)
            {
                nextSegment.PrevSegmentId = newSegment.Id;
            }
        }

        // Backward link: check each cardinal neighbor to see if it outputs toward us
        GridPosRPG[] neighbors =
        {
            new GridPosRPG(newSegment.Position.X + 1, newSegment.Position.Y),
            new GridPosRPG(newSegment.Position.X - 1, newSegment.Position.Y),
            new GridPosRPG(newSegment.Position.X, newSegment.Position.Y + 1),
            new GridPosRPG(newSegment.Position.X, newSegment.Position.Y - 1)
        };

        foreach (var neighborPos in neighbors)
        {
            var neighborSeg = _entityManager.GetPathSegmentAt(neighborPos);
            if (neighborSeg == null) { continue; }
            if (neighborSeg.Id == newSegment.Id) { continue; }

            // If the neighbor's output points to our position, link it → us
            if (neighborSeg.OutputPosition == newSegment.Position &&
                neighborSeg.NextSegmentId == null &&
                newSegment.PrevSegmentId == null)
            {
                neighborSeg.NextSegmentId = newSegment.Id;
                newSegment.PrevSegmentId = neighborSeg.Id;
            }
        }
    }

    /// <summary>
    /// Auto-connects an entity to adjacent path segments by linking them.
    /// When a path segment is adjacent to a structure or routing node,
    /// the segment gets a ConnectedStructureId.
    /// </summary>
    public void AutoConnectToAdjacentPaths(StructureBase entity)
    {
        var pos = entity.Position;
        var directions = new[] { Direction.North, Direction.East, Direction.South, Direction.West };

        foreach (var dir in directions)
        {
            var neighborPos = pos.Neighbor(dir);
            var segment = _entityManager.GetPathSegmentAt(neighborPos);
            if (segment != null)
            {
                // If path segment doesn't have a connected entity yet, link it
                if (!segment.ConnectedStructureId.HasValue)
                {
                    segment.ConnectedStructureId = entity.Id;
                }
            }
        }
    }

    /// <summary>
    /// Connects only the adjacent path segments that feed INTO a routing node.
    /// A segment is considered an input if its OutputPosition equals the routing
    /// node's position (i.e. the segment faces toward the routing node).
    /// Output segments are left unlinked so the routing evaluation does not
    /// fire again when the villager moves past the node.
    /// </summary>
    public void ConnectInputPathsToRoutingNode(RoutingNodeBase routingNode)
    {
        var pos = routingNode.Position;
        var directions = new[] { Direction.North, Direction.East, Direction.South, Direction.West };

        foreach (var dir in directions)
        {
            var neighborPos = pos.Neighbor(dir);
            var segment = _entityManager.GetPathSegmentAt(neighborPos);
            if (segment == null) { continue; }

            // Only connect segments whose output points toward the routing node
            if (segment.OutputPosition == pos && !segment.ConnectedStructureId.HasValue)
            {
                segment.ConnectedStructureId = routingNode.Id;
            }
        }
    }

    // ── Rotation ──────────────────────────────────────────────────

    /// <summary>
    /// Rotates a placed path segment 90° clockwise. Evicts villagers,
    /// unlinks old neighbors, rotates facing, re-links to new neighbors,
    /// and publishes an EntityRotatedEvent.
    /// </summary>
    public void RotatePathSegment(GridPosRPG position)
    {
        var segment = _entityManager.GetPathSegmentAt(position);
        if (segment == null) { return; }

        // Evict villagers before relinking
        EvictVillagersFromSegment(segment.Id, segment, _itemManager);

        // Unlink from current neighbors
        if (segment.PrevSegmentId.HasValue &&
            _entityManager.GetPathSegment(new EntityId(segment.PrevSegmentId.Value)) is { } prev)
        {
            prev.NextSegmentId = null;
        }
        if (segment.NextSegmentId.HasValue &&
            _entityManager.GetPathSegment(new EntityId(segment.NextSegmentId.Value)) is { } next)
        {
            next.PrevSegmentId = null;
        }
        segment.PrevSegmentId = null;
        segment.NextSegmentId = null;

        // Rotate facing 90° clockwise
        segment.Rotate();

        // Re-link to neighbors with new facing
        AutoLinkPath(segment);

        // Also check if any neighbor now points at us
        GridPosRPG[] neighbors =
        {
            new GridPosRPG(segment.Position.X + 1, segment.Position.Y),
            new GridPosRPG(segment.Position.X - 1, segment.Position.Y),
            new GridPosRPG(segment.Position.X, segment.Position.Y + 1),
            new GridPosRPG(segment.Position.X, segment.Position.Y - 1)
        };
        foreach (var neighborPos in neighbors)
        {
            var neighborSeg = _entityManager.GetPathSegmentAt(neighborPos);
            if (neighborSeg == null || neighborSeg.Id == segment.Id) { continue; }

            // If neighbor's output now points to us and it has no next, link it
            if (neighborSeg.OutputPosition == segment.Position &&
                neighborSeg.NextSegmentId == null &&
                segment.PrevSegmentId == null)
            {
                neighborSeg.NextSegmentId = segment.Id;
                segment.PrevSegmentId = neighborSeg.Id;
            }
        }

        _eventBus.Publish(new EntityRotatedEvent(segment.Id, "PathSegment", position, segment.Facing));
    }

    /// <summary>
    /// Sets the facing of a placed path segment to an explicit direction and publishes
    /// an <see cref="EntityRotatedEvent"/> so the renderer can sync the visual rotation.
    /// Used by the path-drawing drag loop when the draw direction changes on the previous tile.
    /// No-op if no segment exists at <paramref name="position"/> or the facing is unchanged.
    /// </summary>
    public void SetPathSegmentFacing(GridPosRPG position, Direction direction)
    {
        var segment = _entityManager.GetPathSegmentAt(position);
        if (segment == null) { return; }
        if (segment.Facing == direction) { return; }

        segment.Facing = direction;
        _eventBus.Publish(new EntityRotatedEvent(segment.Id, "PathSegment", position, direction));
    }

    /// <summary>
    /// Rotates a placed routing node (FilterSplitter, Balancer, CheckGate) 90° clockwise.
    /// Disconnects old input path connections, rotates directions, reconnects, and publishes
    /// an EntityRotatedEvent.
    /// </summary>
    public void RotateRoutingNode(GridPosRPG position)
    {
        var node = _entityManager.GetRoutingNodeAt(position);
        if (node == null) { return; }

        // Disconnect path segments that were linked to this routing node
        DisconnectInputPaths(node);

        // Rotate the output direction 90° clockwise
        node.OutputDirection = node.OutputDirection.RotateClockwise();

        // For filter splitters, also rotate the default and filtered output directions
        if (node is Proto.Logic.FilterSplitterLogic splitter)
        {
            splitter.DefaultDirection = splitter.DefaultDirection.RotateClockwise();
            splitter.FilteredOutputDirection = splitter.FilteredOutputDirection.RotateClockwise();
        }

        // Reconnect input paths to the routing node
        ConnectInputPathsToRoutingNode(node);

        _eventBus.Publish(new EntityRotatedEvent(node.Id, node.GetCategoryName(), position, node.OutputDirection));
    }

    /// <summary>
    /// Removes a routing node from the world. Disconnects linked path segments
    /// and publishes an EntityDemolishedEvent.
    /// </summary>
    public void RemoveRoutingNode(GridPosRPG position)
    {
        var node = _entityManager.GetRoutingNodeAt(position);
        if (node == null) { return; }

        // Disconnect path segments that were linked to this routing node
        DisconnectInputPaths(node);

        _entityManager.RemoveRoutingNode(position);
        _eventBus.Publish(new EntityDemolishedEvent(node.Id, node.GetCategoryName(), position));
    }

    /// <summary>
    /// Disconnects all path segments that reference the given entity as their
    /// ConnectedStructureId.
    /// </summary>
    private void DisconnectInputPaths(StructureBase entity)
    {
        var pos = entity.Position;
        var directions = new[] { Direction.North, Direction.East, Direction.South, Direction.West };

        foreach (var dir in directions)
        {
            var neighborPos = pos.Neighbor(dir);
            var segment = _entityManager.GetPathSegmentAt(neighborPos);
            if (segment != null && segment.ConnectedStructureId == entity.Id)
            {
                segment.ConnectedStructureId = null;
            }
        }
    }

    // ── Path Lifecycle / Per-Tick ─────────────────────────────────────

    /// <summary>
    /// Ticks all path segments. Called once per fixed tick.
    /// </summary>
    public void TickPaths(float deltaTime)
    {
        var orderedSegments = _entityManager.PathSegmentsOrdered;
        for (int i = 0; i < orderedSegments.Count; i++)
        {
            orderedSegments[i].Tick(deltaTime);
        }
    }

    /// <summary>
    /// Scans all idle villagers that are not on a path segment. If a path
    /// has been built under their position, places them on it so they start walking.
    /// </summary>
    public void ActivateWaitingVillagers()
    {
        var villagers = _villagerSystem.Villagers;
        for (int i = 0; i < villagers.Count; i++)
        {
            var v = villagers[i];
            if (v.State != VillagerState.Idle) { continue; }
            if (v.CurrentPathSegmentId.HasValue) { continue; }

            _pathTraffic.PlaceVillagerOnPath(v, v.Position);
        }
    }

    /// <summary>
    /// Removes all villagers currently occupying a segment, returns their
    /// inventory to resource stocks, decrements their spawner's count,
    /// and fires a VillagerReturnedToPoolEvent per villager.
    /// </summary>
    public void EvictVillagersFromSegment(ulong segmentId, PathSegmentLogic segment, ItemManager itemManager)
    {
        var villagers = _villagerSystem.Villagers;
        for (int i = villagers.Count - 1; i >= 0; i--)
        {
            var villager = villagers[i];
            if (villager.CurrentPathSegmentId != segmentId) { continue; }

            // Return carried items to resource stocks
            itemManager.ReturnVillagerInventory(villager.Inventory);

            // Remove from path occupancy
            segment.RemoveOccupant(villager.Id);

            // Decrement the owning spawner's count so it can respawn
            if (villager.OwnerStructureId.HasValue &&
                _entityManager.GetStructure(new EntityId(villager.OwnerStructureId.Value)) is VillageSpawnerLogic spawner)
            {
                spawner.DecrementSpawnCount();
            }

            _villagerSystem.RemoveVillager(villager.Id);
            _eventBus.Publish(new VillagerReturnedToPoolEvent(villager.Id, "path_removed"));
        }

        // Also clear the wait queue — those entities are physically on their
        // previous segment and will re-evaluate routing next tick.
        segment.WaitQueue.Clear();
    }
}
