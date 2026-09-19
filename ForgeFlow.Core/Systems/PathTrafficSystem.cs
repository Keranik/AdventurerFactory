using ForgeFlow.Core.Entities;
using ForgeFlow.Core.Events;
using ForgeFlow.Core.Proto;
using ForgeFlow.Core.Proto.Logic;
using ForgeFlow.Core.Proto.Prototypes;
using ForgeFlow.Core.Utilities;

namespace ForgeFlow.Core.Systems;

/// <summary>
/// Moves heroes and villagers along PathSegmentLogic chains.
/// Handles splitter routing (round-robin, priority, filtered, weighted),
/// walk-by building interaction, collision resolution
/// with waiting queues, and capacity blocking.
/// Zero-alloc hot path: pre-allocated lists, no LINQ in tick.
/// Performance: cached segment lookups, pooled scratch lists, batch wait-queue processing.
/// Self-subscribes to SimulationTickEvent.
/// </summary>
public sealed class PathTrafficSystem : IGameSystem, IDisposable
{
    private readonly TrafficManager _trafficManager;
    private readonly EntityManager _entityManager;
    private readonly TileManager _tileManager;
    private readonly EventBus _eventBus;
    private PathGateManager? _pathGateManager;
    private VillagerSystem? _villagerSystem;

    // Cached segment arrays for batch iteration
    private PathSegmentLogic[] _segmentCache = new PathSegmentLogic[64];
    private int _segmentCacheCount;

    // Re-usable list for wait-queue batch promotion
    private readonly List<ulong> _promotionBatch = new(32);

    // Performance counters (read by DebugHUD)
    public int LastTickHeroMoves { get; private set; }
    public int LastTickVillagerMoves { get; private set; }
    public int LastTickCollisions { get; private set; }

    public PathTrafficSystem(TrafficManager trafficManager, EntityManager entityManager, TileManager tileManager, EventBus eventBus)
    {
        _trafficManager = trafficManager;
        _entityManager = entityManager;
        _tileManager = tileManager;
        _eventBus = eventBus;

        _eventBus.Subscribe<SimulationTickEvent>(OnTick);
        _eventBus.Subscribe<PathBuiltEvent>(OnPathBuilt);
        _eventBus.Subscribe<PathRemovedEvent>(OnPathRemoved);
    }

    public void Dispose()
    {
        _eventBus.Unsubscribe<SimulationTickEvent>(OnTick);
        _eventBus.Unsubscribe<PathBuiltEvent>(OnPathBuilt);
        _eventBus.Unsubscribe<PathRemovedEvent>(OnPathRemoved);
    }

    private void OnTick(SimulationTickEvent e)
    {
        Tick(e.DeltaTime);
        TickVillagers(e.DeltaTime);
    }

    /// <summary>Injects the PathGateManager. Called after both systems are created.</summary>
    public void SetPathGateManager(PathGateManager pathGateManager)
    {
        _pathGateManager = pathGateManager;
    }

    /// <summary>
    /// Injects the VillagerSystem so <see cref="TickVillagers"/> can iterate the
    /// compact <see cref="VillagerMovementState"/> buffer instead of individual
    /// <see cref="VillagerLogic"/> objects. Called after both systems are created.
    /// </summary>
    public void SetVillagerSystem(VillagerSystem villagerSystem)
    {
        _villagerSystem = villagerSystem;
    }

    private void OnPathBuilt(PathBuiltEvent e)
    {
        var segment = _entityManager.GetPathSegment(e.SegmentId);
        if (segment == null) { return; }
        if (_segmentCacheCount >= _segmentCache.Length)
        {
            Array.Resize(ref _segmentCache, _segmentCache.Length * 2);
        }
        _segmentCache[_segmentCacheCount++] = segment;
    }

    private void OnPathRemoved(PathRemovedEvent e)
    {
        // Linear scan acceptable: structural removal is a low-frequency event and
        // the cache is contiguous so this is cache-friendly.
        for (int i = 0; i < _segmentCacheCount; i++)
        {
            if (_segmentCache[i].Id == e.SegmentId)
            {
                int last = --_segmentCacheCount;
                _segmentCache[i] = _segmentCache[last];
                _segmentCache[last] = null!;
                return;
            }
        }
    }

    public void Tick(float deltaTime)
    {
        var heroes = _entityManager.Heroes;
        var pathSegments = _entityManager.PathSegments;

        // 1. Move heroes along path segments with collision resolution
        MoveHeroesAlongPaths(deltaTime, heroes, pathSegments);

        // 2. Process walk-by building interactions for heroes
        ProcessWalkByInteractions(heroes, pathSegments);

        // 3. Process wait queues — promote waiting entities when space opens
        ProcessWaitQueues(pathSegments);
    }

    /// <summary>
    /// Moves villagers along path segments. Called separately to keep
    /// villager/hero movement decoupled.
    /// <para>
    /// When a <see cref="VillagerSystem"/> is injected (via
    /// <see cref="SetVillagerSystem"/>), the hot loop iterates the compact
    /// <see cref="VillagerMovementState"/> buffer for cache-friendly field access.
    /// Movement results are written back to the matching <see cref="VillagerLogic"/>
    /// objects after each step. Groundwork for a future Burst/Jobs conversion.
    /// </para>
    /// </summary>
    public void TickVillagers(float deltaTime)
    {
        var pathSegments = _entityManager.PathSegments;

        if (_villagerSystem != null)
        {
            TickVillagersFromBuffer(deltaTime, pathSegments, _villagerSystem);
        }
        else
        {
            TickVillagersLegacy(deltaTime, pathSegments);
        }
    }

    /// <summary>
    /// Cache-friendly villager movement loop: syncs from VillagerLogic → struct buffer,
    /// iterates the compact struct array, writes changed state back.
    /// </summary>
    private void TickVillagersFromBuffer(
        float deltaTime,
        IReadOnlyDictionary<EntityId, PathSegmentLogic> pathSegments,
        VillagerSystem villagerSystem)
    {
        int count = villagerSystem.MovementStateCount;
        var buffer = villagerSystem.MovementStateBuffer;
        var villagerList = villagerSystem.Villagers;

        // Sync-in: snapshot current VillagerLogic movement state into the struct buffer
        // (sequential write → cache-efficient for the destination array)
        for (int i = 0; i < count; i++)
        {
            buffer[i] = VillagerMovementState.FromLogic(villagerList[i]);
        }

        // Hot loop: all reads come from the compact struct array (cache-friendly)
        int moveCount = 0;
        for (int i = 0; i < count; i++)
        {
            ref var state = ref buffer[i];

            if (state.State != VillagerState.Travelling) { continue; }
            if (!state.CurrentPathSegmentId.HasValue) { continue; }
            if (!pathSegments.TryGetValue(new EntityId(state.CurrentPathSegmentId.Value), out var segment)) { continue; }

            float speed = state.MovementSpeed * segment.SpeedMultiplier;
            state.PathProgress += speed * deltaTime;

            // Default sync-back point. For sub-threshold ticks (most ticks), only
            // PathProgress advances; without this, the next tick's FromLogic would
            // overwrite the buffer with stale logic.PathProgress (=0) and villagers
            // would never accumulate progress. The crossing branch below performs
            // its own ApplyToLogic after it finishes mutating state, so this early
            // write is harmless in that case (it just writes the pre-crossing value
            // which the later ApplyToLogic supersedes).
            state.ApplyToLogic(villagerList[i]);

            if (state.PathProgress >= 1.0f)
            {
                state.PathProgress -= 1.0f;

                // Resolve next segment using routing rules + villager routing tag.
                // TryResolveVillagerRouting and PathGate checks still need the full
                // VillagerLogic; retrieve it by aligned index.
                var villager = villagerList[i];
                var nextId = segment.ResolveNextSegment(state.RoutingTag);
                nextId = TryResolveVillagerRouting(segment, villager, nextId);

                if (nextId.HasValue && segment.SplitMode == SplitMode.Priority)
                {
                    if (pathSegments.TryGetValue(new EntityId(nextId.Value), out var nextSeg) &&
                        nextSeg.OccupantIds.Count > 0)
                    {
                        nextId = segment.ResolveWithFallback(primaryFull: true);
                    }
                }

                if (nextId.HasValue && pathSegments.TryGetValue(new EntityId(nextId.Value), out var next))
                {
                    if (next.OccupantIds.Count == 0 && next.TryAddOccupant(villager.Id))
                    {
                        segment.RemoveOccupant(villager.Id);
                        state.CurrentPathSegmentId = nextId;
                        state.Position = next.Position;
                        moveCount++;

                        if (_pathGateManager != null && _pathGateManager.CheckPathGateEntrance(villager, next))
                        {
                            // Villager was diverted into a building. The gate already mutated
                            // VillagerLogic directly (cleared CurrentPathSegmentId, set State to
                            // Working/Resting/EnteringBuilding/InDungeon, moved Position to the
                            // structure). Re-snapshot the buffer from the now-authoritative
                            // VillagerLogic so the trailing ApplyToLogic below becomes a no-op
                            // write of the correct values instead of stomping the gate's
                            // changes with the stale path-tile state. Without this, the villager
                            // is left with State=Travelling, CurrentPathSegmentId=next,
                            // Position=next.Position — visible as a "ghost" parked at the gate
                            // tile, never actually moving into the building visually.
                            state = VillagerMovementState.FromLogic(villager);
                        }
                        else if (next.HasAdjacentStructure && next.ConnectedStructureId.HasValue)
                        {
                            HandleVillagerBuildingInteraction(villager, next);
                        }
                    }
                    else
                    {
                        state.PathProgress = 0.99f;
                        next.EnqueueWaiting(villager.Id);
                    }
                }
                else
                {
                    // End of path
                    segment.RemoveOccupant(villager.Id);
                    state.CurrentPathSegmentId = null;
                    state.PathProgress = 0f;
                    moveCount++;
                    var villagerForState = villagerList[i];
                    state.State = villagerForState.Profession != VillagerJob.Idle || villagerForState.CurrentActivity != null
                        ? VillagerState.Working
                        : VillagerState.Idle;
                }

                // Sync-back: write movement result to VillagerLogic
                state.ApplyToLogic(villagerList[i]);
            }
        }

        LastTickVillagerMoves = moveCount;
    }

    /// <summary>
    /// Fallback movement loop used when no <see cref="VillagerSystem"/> is injected.
    /// Iterates <see cref="VillagerLogic"/> objects directly (original behaviour).
    /// </summary>
    private void TickVillagersLegacy(float deltaTime, IReadOnlyDictionary<EntityId, PathSegmentLogic> pathSegments)
    {
        var villagers = _entityManager.Villagers;

        int moveCount = 0;
        for (int i = 0; i < villagers.Count; i++)
        {
            var villager = villagers[i];
            if (villager.State != VillagerState.Travelling) continue;
            if (!villager.CurrentPathSegmentId.HasValue) continue;
            if (!pathSegments.TryGetValue(new EntityId(villager.CurrentPathSegmentId.Value), out var segment)) continue;

            float speed = villager.MovementSpeed * segment.SpeedMultiplier;
            villager.PathProgress += speed * deltaTime;

            if (villager.PathProgress >= 1.0f)
            {
                villager.PathProgress -= 1.0f;

                var nextId = segment.ResolveNextSegment(villager.RoutingTag);
                nextId = TryResolveVillagerRouting(segment, villager, nextId);

                if (nextId.HasValue && segment.SplitMode == SplitMode.Priority)
                {
                    if (pathSegments.TryGetValue(new EntityId(nextId.Value), out var nextSeg) &&
                        nextSeg.OccupantIds.Count > 0)
                    {
                        nextId = segment.ResolveWithFallback(primaryFull: true);
                    }
                }

                if (nextId.HasValue && pathSegments.TryGetValue(new EntityId(nextId.Value), out var next))
                {
                    if (next.OccupantIds.Count == 0 && next.TryAddOccupant(villager.Id))
                    {
                        segment.RemoveOccupant(villager.Id);
                        villager.CurrentPathSegmentId = nextId;
                        villager.Position = next.Position;
                        moveCount++;

                        if (_pathGateManager != null && _pathGateManager.CheckPathGateEntrance(villager, next))
                        {
                            // Villager was diverted into a building — skip walk-by
                        }
                        else if (next.HasAdjacentStructure && next.ConnectedStructureId.HasValue)
                        {
                            HandleVillagerBuildingInteraction(villager, next);
                        }
                    }
                    else
                    {
                        villager.PathProgress = 0.99f;
                        next.EnqueueWaiting(villager.Id);
                    }
                }
                else
                {
                    segment.RemoveOccupant(villager.Id);
                    villager.CurrentPathSegmentId = null;
                    villager.PathProgress = 0f;
                    moveCount++;
                    if (villager.Profession != VillagerJob.Idle || villager.CurrentActivity != null)
                    {
                        villager.State = VillagerState.Working;
                    }
                    else
                    {
                        villager.State = VillagerState.Idle;
                    }
                }
            }
        }
        LastTickVillagerMoves = moveCount;
    }

    private void MoveHeroesAlongPaths(
        float deltaTime,
        IReadOnlyList<HeroEntity> heroes,
        IReadOnlyDictionary<EntityId, PathSegmentLogic> pathSegments)
    {
        int moveCount = 0;
        int collisionCount = 0;

        for (int i = 0; i < heroes.Count; i++)
        {
            var hero = heroes[i];
            if (hero.State != HeroState.OnPath) continue;
            if (!hero.CurrentPathSegmentId.HasValue) continue;
            if (!pathSegments.TryGetValue(new EntityId(hero.CurrentPathSegmentId.Value), out var segment)) continue;

            float speed = 2.0f * segment.SpeedMultiplier;
            hero.PathProgress += speed * deltaTime;

            if (hero.PathProgress >= 1.0f)
            {
                hero.PathProgress -= 1.0f;

                // Resolve next segment (handles splitter routing rules)
                var nextId = segment.ResolveNextSegment();

                // Routing entity override: evaluate connected routing structures.
                nextId = TryResolveHeroRouting(segment, hero, nextId);

                // Priority mode fallback — check capacity inline
                if (nextId.HasValue && segment.SplitMode == SplitMode.Priority)
                {
                    if (pathSegments.TryGetValue(new EntityId(nextId.Value), out var peekSeg) &&
                        peekSeg.OccupantIds.Count >= peekSeg.Capacity)
                    {
                        nextId = segment.ResolveWithFallback(primaryFull: true);
                    }
                }

                if (nextId.HasValue && pathSegments.TryGetValue(new EntityId(nextId.Value), out var nextSegment))
                {
                    if (nextSegment.TryAddOccupant(hero.Id))
                    {
                        segment.RemoveOccupant(hero.Id);
                        hero.CurrentPathSegmentId = nextId;
                        hero.Position = nextSegment.Position;
                        moveCount++;
                    }
                    else
                    {
                        // Collision: enqueue in wait queue for orderly resolution
                        hero.PathProgress = 0.99f;
                        nextSegment.EnqueueWaiting(hero.Id);
                        segment.IsBlocked = true;
                        collisionCount++;
                    }
                }
                else
                {
                    // End of path — hero steps off
                    segment.RemoveOccupant(hero.Id);
                    hero.CurrentPathSegmentId = null;
                    hero.PathProgress = 0f;
                    moveCount++;
                }
            }
        }

        LastTickHeroMoves = moveCount;
        LastTickCollisions = collisionCount;
    }

    /// <summary>
    /// Walk-by interaction: when a hero is on a path segment
    /// adjacent to a building with a ConnectedStructureId, auto-interact.
    /// </summary>
    private void ProcessWalkByInteractions(
        IReadOnlyList<HeroEntity> heroes,
        IReadOnlyDictionary<EntityId, PathSegmentLogic> pathSegments)
    {
        var structures = _entityManager.Structures;
        for (int i = 0; i < heroes.Count; i++)
        {
            var hero = heroes[i];
            if (hero.State != HeroState.OnPath) continue;
            if (!hero.CurrentPathSegmentId.HasValue) continue;
            if (!pathSegments.TryGetValue(new EntityId(hero.CurrentPathSegmentId.Value), out var segment)) continue;

            if (!segment.HasAdjacentStructure) continue;
            if (!segment.ConnectedStructureId.HasValue) continue;
            if (!structures.TryGetValue(new EntityId(segment.ConnectedStructureId.Value), out var structure)) continue;

            // Walk-by: structure pushes items TO the hero (only recipe structures have output queues)
            if (structure is RecipeEntity recipe && recipe.TryDequeueOutput(out var item) && item != null)
            {
                hero.Equip(item.ToEquipped());
            }
        }
    }

    /// <summary>
    /// Walk-by structure interaction for villagers.
    /// When a villager walks by a school/training building, they auto-interact.
    /// </summary>
    private void HandleVillagerBuildingInteraction(
        VillagerLogic villager,
        PathSegmentLogic segment)
    {
        if (!segment.ConnectedStructureId.HasValue) return;
        var entity = _entityManager.GetStructureOrRoutingNode(new EntityId(segment.ConnectedStructureId.Value));
        if (entity == null) return;

        // Routing entities are evaluated during the routing phase — not walk-by
        if (entity is RoutingNodeBase) return;

        // Training building interaction: untrained villager walks by → gets enrolled
        if (entity is TrainingBuildingLogic school)
        {
            if (school.AcceptTrainee(villager))
            {
                _eventBus.Publish(new VillagerTrainingStartedEvent(
                    villager.Id, school.ProtoId, school.OutputClass.ToString()));
            }
        }
        // Gathering structures require entry via PathGate — no walk-by auto-assign
    }

    /// <summary>
    /// Promotes entities from wait queues when segment capacity frees up.
    /// Ensures orderly first-come-first-served collision resolution.
    /// Uses cached segment array for zero-alloc iteration.
    /// </summary>
    private void ProcessWaitQueues(IReadOnlyDictionary<EntityId, PathSegmentLogic> pathSegments)
    {
        // Use cached array when available for zero-alloc iteration
        int count = _segmentCacheCount > 0 ? _segmentCacheCount : 0;
        if (count == 0)
        {
            var orderedSegments = _entityManager.PathSegmentsOrdered;
            for (int j = 0; j < orderedSegments.Count; j++)
            {
                PromoteWaitQueue(orderedSegments[j]);
            }
            return;
        }

        for (int i = 0; i < count; i++)
        {
            PromoteWaitQueue(_segmentCache[i]);
        }
    }

    private static void PromoteWaitQueue(PathSegmentLogic segment)
    {
        // Only drain wait-queue entries — do NOT add occupants here.
        // Adding occupants without completing the full move (removing
        // from previous segment, updating Position/CurrentPathSegmentId)
        // creates ghost occupants that cause tile overlap and permanent
        // blockages. The entity already has PathProgress ≈ 0.99f and will
        // retry its move normally on the next tick.
        while (segment.WaitQueue.Count > 0 && segment.OccupantIds.Count < segment.Capacity)
        {
            segment.DequeueWaiting();
        }
    }

    /// <summary>
    /// Places a hero onto the path segment at the given position.
    /// </summary>
    public bool PlaceHeroOnPath(
        HeroEntity hero,
        GridPosRPG position)
    {
        var seg = _entityManager.GetPathSegmentAt(position);
        if (seg == null) { return false; }
        if (!seg.TryAddOccupant(hero.Id)) { return false; }

        hero.CurrentPathSegmentId = seg.Id;
        hero.Position = position;
        hero.PathProgress = 0f;
        return true;
    }

    /// <summary>
    /// Places a villager onto the path segment at the given position.
    /// </summary>
    public bool PlaceVillagerOnPath(
        VillagerLogic villager,
        GridPosRPG position)
    {
        var seg = _entityManager.GetPathSegmentAt(position);
        if (seg == null) { return false; }
        // Exclusive tile occupancy: villagers never share a tile
        if (seg.OccupantIds.Count > 0) { return false; }
        if (!seg.TryAddOccupant(villager.Id)) { return false; }

        villager.PlaceOnPath(seg.Id);
        villager.Position = position;
        return true;
    }

    // ── Routing Entity Helpers ────────────────────────────────────

    /// <summary>
    /// Checks if the current segment is connected to a RoutingNodeBase.
    /// If so, evaluates the routing entity for the villager and returns
    /// an overridden next segment ID. Otherwise returns defaultNext.
    /// </summary>
    private ulong? TryResolveVillagerRouting(
        PathSegmentLogic segment,
        VillagerLogic villager,
        ulong? defaultNext)
    {
        if (!segment.ConnectedStructureId.HasValue) { return defaultNext; }
        var entity = _entityManager.GetStructureOrRoutingNode(new EntityId(segment.ConnectedStructureId.Value));
        if (entity is not RoutingNodeBase routing) { return defaultNext; }

        var direction = EvaluateVillagerDirection(routing, villager);
        var targetPos = routing.Position.Neighbor(direction);
        var targetSegment = _entityManager.GetPathSegmentAt(targetPos);
        if (targetSegment != null)
        {
            _eventBus.Publish(new WorkerRoutedEvent(villager.Id, direction, routing.ProtoId));
            return targetSegment.Id;
        }
        return defaultNext;
    }

    /// <summary>
    /// Checks if the current segment is connected to a RoutingNodeBase.
    /// If so, evaluates the routing entity for the hero and returns
    /// an overridden next segment ID. Otherwise returns defaultNext.
    /// </summary>
    private ulong? TryResolveHeroRouting(
        PathSegmentLogic segment,
        HeroEntity hero,
        ulong? defaultNext)
    {
        if (!segment.ConnectedStructureId.HasValue) { return defaultNext; }
        var entity = _entityManager.GetStructureOrRoutingNode(new EntityId(segment.ConnectedStructureId.Value));
        if (entity is not RoutingNodeBase routing) { return defaultNext; }

        var direction = EvaluateHeroDirection(routing, hero);
        var targetPos = routing.Position.Neighbor(direction);
        var targetSegment = _entityManager.GetPathSegmentAt(targetPos);
        if (targetSegment != null)
        {
            _eventBus.Publish(new WorkerRoutedEvent(hero.Id, direction, routing.ProtoId));
            return targetSegment.Id;
        }
        return defaultNext;
    }

    /// <summary>Dispatches villager evaluation to the correct routing entity type.</summary>
    private static Direction EvaluateVillagerDirection(RoutingNodeBase routing, VillagerLogic villager)
    {
        return routing switch
        {
            FilterSplitterLogic splitter => splitter.EvaluateVillagerRoute(villager),
            CheckGateLogic gate => gate.EvaluateVillagerRoute(villager),
            BalancerLogic balancer => balancer.GetNextOutput(),
            _ => routing.OutputDirection
        };
    }

    /// <summary>Dispatches hero evaluation to the correct routing entity type.</summary>
    private static Direction EvaluateHeroDirection(RoutingNodeBase routing, HeroEntity hero)
    {
        return routing switch
        {
            FilterSplitterLogic splitter => splitter.EvaluateRoute(hero),
            CheckGateLogic gate => gate.EvaluateRoute(hero),
            BalancerLogic balancer => balancer.GetNextOutput(),
            _ => routing.OutputDirection
        };
    }

    }
