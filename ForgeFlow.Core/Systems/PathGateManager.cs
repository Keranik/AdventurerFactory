using ForgeFlow.Core.Commands;
using ForgeFlow.Core.Entities;
using ForgeFlow.Core.Events;
using ForgeFlow.Core.Proto;
using ForgeFlow.Core.Proto.Logic;
using ForgeFlow.Core.Proto.Prototypes;

namespace ForgeFlow.Core.Systems;

/// <summary>
/// Single source of truth for all PathGate operations: placement, removal,
/// auto-linking to structures, entrance detection, exit placement, spatial
/// queries, and per-tick updates. Coordinates between EntityManager
/// (low-level entity storage), TileManager (grid occupancy), and the
/// villager movement layer.
/// Self-subscribes to LateTick.
/// </summary>
public sealed class PathGateManager : IGameSystem, IDisposable
{
    private readonly EntityManager _entityManager;
    private readonly TileManager _tileManager;
    private readonly EventBus _eventBus;
    private readonly CommandBus _commandBus;
    private readonly TutorialSystem _tutorialSystem;
    private readonly ItemManager _itemManager;
    private readonly ResearchManager _researchManager;

    public PathGateManager(
        EntityManager entityManager,
        TileManager tileManager,
        EventBus eventBus,
        CommandBus commandBus,
        TutorialSystem tutorialSystem,
        ItemManager itemManager,
        ResearchManager researchManager)
    {
        _entityManager = entityManager;
        _tileManager = tileManager;
        _eventBus = eventBus;
        _commandBus = commandBus;
        _tutorialSystem = tutorialSystem;
        _itemManager = itemManager;
        _researchManager = researchManager;

        _eventBus.Subscribe<SimulationLateTickEvent>(OnLateTick);

        _commandBus.Register<PlacePathGateCommand>(HandlePlacePathGate);
    }

    public void Dispose()
    {
        _eventBus.Unsubscribe<SimulationLateTickEvent>(OnLateTick);
        _commandBus.Unregister<PlacePathGateCommand>();
    }

    private void OnLateTick(SimulationLateTickEvent e) => TickPathGates(e.DeltaTime);

    // ── Command Handlers ────────────────────────────────────────────

    /// <summary>
    /// Handles <see cref="PlacePathGateCommand"/>: validates gold, deducts cost,
    /// places the gate, publishes GoldChangedEvent, and returns success/fail.
    /// This is the single source of truth for PathGate placement transactions.
    /// </summary>
    private CommandResult HandlePlacePathGate(PlacePathGateCommand cmd)
    {
        int cost = EconomyConfig.GetStructureCost("PathGate");
        int oldGold = _itemManager.GetStock("gold");

        if (!_itemManager.TrySpendStock("gold", cost))
        {
            _eventBus.Publish(new GatingBlockedEvent("gold", _itemManager.GetStock("gold"), cost, _researchManager.CurrentTier));
            return CommandResult.Fail("Not enough gold");
        }

        var gate = AddPathGate(cmd.Position, cmd.Facing);
        if (gate == null)
        {
            // Placement failed — refund gold
            _itemManager.AddStock("gold", cost);
            return CommandResult.Fail("Placement blocked");
        }

        _eventBus.Publish(new GoldChangedEvent(oldGold, _itemManager.GetStock("gold"), $"Placed PathGate ({gate.Mode})"));
        return CommandResult.Ok();
    }

    // ── Proxy Properties ─────────────────────────────────────────

    /// <summary>All path gates keyed by ID.</summary>
    public IReadOnlyDictionary<EntityId, PathGateLogic> PathGates => _entityManager.PathGates;

    // ── PathGate CRUD ────────────────────────────────────────────

    /// <summary>
    /// Places a PathGate at the given position with the given facing direction.
    /// Automatically links to an adjacent structure if one exists within range.
    /// </summary>
    public PathGateLogic? AddPathGate(GridPosRPG position, Direction facing)
    {
        var gate = new PathGateLogic(EntityId.Next())
        {
            Position = position,
            Facing = facing
        };

        // Auto-link to nearest adjacent structure within range
        AutoLinkPathGate(gate);

        _entityManager.AddPathGate(gate);

        // Determine tutorial condition type
        var conditionType = gate.IsEntrance
            ? TutorialConditionType.PlacePathGateEntrance
            : TutorialConditionType.PlacePathGateExit;
        _tutorialSystem.AdvanceCondition(conditionType);

        _eventBus.Publish(new PathGatePlacedEvent(gate.Id, position, gate.Mode, gate.LinkedStructureId.HasValue ? new EntityId(gate.LinkedStructureId.Value) : (EntityId?)null));
        return gate;
    }

    /// <summary>Removes a PathGate from the world.</summary>
    public void RemovePathGate(GridPosRPG position)
    {
        var gate = _entityManager.GetPathGateAt(position);
        if (gate == null) { return; }
        _entityManager.RemovePathGate(gate.Id, position);
    }

    /// <summary>Gets the number of PathGates currently placed.</summary>
    public int CountPathGates()
    {
        return _entityManager.PathGateCount;
    }

    // ── Rotation ─────────────────────────────────────────────────

    /// <summary>
    /// Rotates a placed PathGate 90° clockwise. Recomputes mode if linked
    /// to a structure, and publishes an EntityRotatedEvent.
    /// </summary>
    public void RotatePathGate(GridPosRPG position)
    {
        var gate = _entityManager.GetPathGateAt(position);
        if (gate == null) { return; }

        // PathGateLogic.Rotate already handles facing + mode recompute
        GridPosRPG? structurePos = null;
        if (gate.LinkedStructureId.HasValue)
        {
            var structure = _entityManager.GetStructure(new EntityId(gate.LinkedStructureId.Value));
            if (structure != null)
            {
                structurePos = structure.Position;
            }
        }
        gate.Rotate(structurePos);

        _eventBus.Publish(new EntityRotatedEvent(gate.Id, "PathGate", position, gate.Facing));
    }

    // ── Spatial Queries ──────────────────────────────────────────

    /// <summary>
    /// Finds the first Exit PathGate linked to the given structure, or null.
    /// </summary>
    public PathGateLogic? FindExitGateForStructure(ulong structureId)
    {
        var gates = _entityManager.PathGatesOrdered;
        for (int i = 0; i < gates.Count; i++)
        {
            var gate = gates[i];
            if (gate.IsExit && gate.LinkedStructureId == structureId)
            {
                return gate;
            }
        }
        return null;
    }

    /// <summary>
    /// Finds the first Entrance PathGate linked to the given structure, or null.
    /// </summary>
    public PathGateLogic? FindEntranceGateForStructure(ulong structureId)
    {
        var gates = _entityManager.PathGatesOrdered;
        for (int i = 0; i < gates.Count; i++)
        {
            var gate = gates[i];
            if (gate.IsEntrance && gate.LinkedStructureId == structureId)
            {
                return gate;
            }
        }
        return null;
    }

    // ── Entrance Detection ───────────────────────────────────────

    /// <summary>
    /// Checks cardinal neighbors of a path segment for a PathGate set as Entrance.
    /// If found and the villager is eligible, diverts the villager off the path
    /// into the linked building. Returns true if the villager was diverted.
    /// Zero-alloc: uses inline checks instead of allocating arrays.
    /// </summary>
    public bool CheckPathGateEntrance(
        VillagerLogic villager,
        PathSegmentLogic segment)
    {
        if (villager.State != VillagerState.Travelling) { return false; }
        if (_entityManager.PathGateCount == 0) { return false; }

        // Check all four cardinal neighbors for a PathGate
        var pos = segment.Position;
        if (TryDivertViaGate(villager, segment, new GridPosRPG(pos.X + 1, pos.Y))) { return true; }
        if (TryDivertViaGate(villager, segment, new GridPosRPG(pos.X - 1, pos.Y))) { return true; }
        if (TryDivertViaGate(villager, segment, new GridPosRPG(pos.X, pos.Y + 1))) { return true; }
        if (TryDivertViaGate(villager, segment, new GridPosRPG(pos.X, pos.Y - 1))) { return true; }

        return false;
    }

    // ── Exit Logic ───────────────────────────────────────────────

    /// <summary>
    /// Finds a PathGate set as Exit for the given structure and places
    /// the villager back onto the adjacent path. Called when a villager
    /// finishes work inside a building.
    /// </summary>
    public bool ExitVillagerViaGate(
        VillagerLogic villager,
        EntityId structureId)
    {
        // Find an Exit gate linked to this structure
        var gates = _entityManager.PathGatesOrdered;
        for (int gi = 0; gi < gates.Count; gi++)
        {
            var gate = gates[gi];
            if (!gate.IsExit) { continue; }
            if (gate.LinkedStructureId != structureId) { continue; }

            // Place the villager on the tile the gate is facing (the path side)
            var exitPos = gate.FacingPosition;
            var segment = _entityManager.GetPathSegmentAt(exitPos);
            if (segment != null &&
                segment.OccupantIds.Count == 0 &&
                segment.TryAddOccupant(villager.Id))
            {
                villager.PlaceOnPath(segment.Id);
                villager.Position = exitPos;
                villager.State = VillagerState.Travelling;

                // Notify Presentation that the villager is back on the path so the
                // visual (which was hidden on VillagerEnteredBuildingEvent) can be
                // re-shown. Per §3.5 Central Manager + Events Pattern, only managers
                // publish events — VillagerLogic never does.
                var structure = _entityManager.GetStructure(structureId);
                var buildingType = structure?.ProtoId ?? string.Empty;
                _eventBus.Publish(new VillagerLeftBuildingEvent(villager.Id, structureId, buildingType));
                return true;
            }
        }

        return false;
    }

    // ── Per-Tick ──────────────────────────────────────────────────

    /// <summary>
    /// Ticks all PathGate entities. Currently a no-op since PathGates are
    /// passive spatial markers, but provides a clean hook for future behavior.
    /// </summary>
    public void TickPathGates(float deltaTime)
    {
        var gates = _entityManager.PathGatesOrdered;
        for (int i = 0; i < gates.Count; i++)
        {
            gates[i].Tick(deltaTime);
        }
    }

    // ── Private Helpers ──────────────────────────────────────────

    /// <summary>
    /// Auto-links a PathGate to the nearest adjacent structure. Checks the
    /// facing direction first, then behind, then all cardinal neighbors.
    /// </summary>
    private void AutoLinkPathGate(PathGateLogic gate)
    {
        // Check the tile the gate is facing and the tile behind it for a structure.
        var facingPos = gate.FacingPosition;
        var behindPos = gate.Position.Neighbor(gate.Facing.Opposite());

        // Facing direction points at a building → Entrance, direction stays
        var facingStructureId = _tileManager.GetStructureIdAt(facingPos);
        if (facingStructureId.HasValue)
        {
            gate.LinkToStructure(facingStructureId.Value, facingPos);
            return;
        }

        // Behind the gate has a building → Exit, direction stays
        var behindStructureId = _tileManager.GetStructureIdAt(behindPos);
        if (behindStructureId.HasValue)
        {
            gate.LinkToStructure(behindStructureId.Value, behindPos);
            return;
        }

        // Building is on a non-aligned cardinal neighbor.
        // Auto-orient the gate to face away from the building (Exit by default)
        GridPosRPG[] neighbors =
        {
            new GridPosRPG(gate.Position.X + 1, gate.Position.Y),
            new GridPosRPG(gate.Position.X - 1, gate.Position.Y),
            new GridPosRPG(gate.Position.X, gate.Position.Y + 1),
            new GridPosRPG(gate.Position.X, gate.Position.Y - 1)
        };

        foreach (var neighborPos in neighbors)
        {
            var structureId = _tileManager.GetStructureIdAt(neighborPos);
            if (structureId.HasValue)
            {
                var dir = GridPosRPG.DirectionFromTo(neighborPos, gate.Position);
                if (dir.HasValue)
                {
                    gate.Facing = dir.Value;
                }
                gate.LinkToStructure(structureId.Value, neighborPos);
                return;
            }
        }
    }

    private bool TryDivertViaGate(
        VillagerLogic villager,
        PathSegmentLogic segment,
        GridPosRPG neighborPos)
    {
        var gate = _entityManager.GetPathGateAt(neighborPos);
        if (gate == null) { return false; }
        if (!gate.IsEntrance) { return false; }
        if (!gate.IsLinked) { return false; }
        var structure = _entityManager.GetStructure(new EntityId(gate.LinkedStructureId!.Value));
        if (structure == null) { return false; }

        // ── Universal entry eligibility check ──
        var gated = structure.AsEntryGated;
        if (gated != null)
        {
            var result = gated.CheckEntry(villager);
            if (!result.IsAccepted)
            {
                // Publish diagnostic event so Presentation can surface why the
                // villager bounced off the gate (tooltip, bubble, log). Without
                // this, silent rejections look like bugs ("villagers don't like
                // to enter") to the player.
                _eventBus.Publish(new VillagerEntryRejectedEvent(
                    villager.Id, structure.Id, structure.ProtoId, result.Reason));
                return false;
            }
        }

        // ── Pre-entry preparation ──
        var preAction = structure.AsPreEntryAction;
        if (preAction != null)
        {
            preAction.PrepareVillagerForEntry(villager);
        }

        // Capacity check for gathering structures — villagers back up on path if full
        if (structure is GatheringLogicBase gathering)
        {
            if (!gathering.HasFreeSlot)
            {
                // Try to enqueue in the structure's wait queue
                if (!gathering.EnqueueWaiting(villager.Id))
                {
                    return false; // Wait queue also full — stay on path
                }
                // Remove from path and park at structure position
                segment.RemoveOccupant(villager.Id);
                villager.CurrentPathSegmentId = null;
                villager.PathProgress = 0f;
                villager.State = VillagerState.EnteringBuilding;
                villager.Position = structure.Position;
                return true;
            }

            // Has free slot — accept the worker
            segment.RemoveOccupant(villager.Id);
            villager.CurrentPathSegmentId = null;
            villager.PathProgress = 0f;
            villager.State = VillagerState.Working;
            villager.Position = structure.Position;
            villager.TargetNodeId = gathering.Id;
            gathering.AcceptWorker(villager.Id, villager.EquippedToolId);
            villager.CurrentActivity = $"Gathering {gathering.TargetResourceId}";
            _eventBus.Publish(new VillagerEnteredBuildingEvent(
                villager.Id, gathering.Id, gathering.ProtoId));
            return true;
        }

        // Residence (VillageSpawner) — check eligibility before removing from path
        // Unlike the Inn, residences ignore inventory — villagers can rest with items.
        // Only assigned residents are accepted.
        if (structure is VillageSpawnerLogic residence)
        {
            segment.RemoveOccupant(villager.Id);
            villager.CurrentPathSegmentId = null;
            villager.PathProgress = 0f;
            villager.State = VillagerState.Resting;
            villager.Position = structure.Position;
            residence.AcceptVillagerForRest(villager);
            _eventBus.Publish(new VillagerEnteredBuildingEvent(
                villager.Id, residence.Id, residence.ProtoId));
            return true;
        }

        // Inn — check eligibility before removing from path
        if (structure is InnLogic inn)
        {
            segment.RemoveOccupant(villager.Id);
            villager.CurrentPathSegmentId = null;
            villager.PathProgress = 0f;
            villager.State = VillagerState.Resting;
            villager.Position = structure.Position;
            inn.AcceptVillager(villager);
            _eventBus.Publish(new VillagerEnteredBuildingEvent(
                villager.Id, inn.Id, inn.ProtoId));
            return true;
        }

        // Dungeon Portal — check eligibility (fighting class + capacity; gear improves survival)
        if (structure is DungeonPortalLogic dungeon)
        {
            segment.RemoveOccupant(villager.Id);
            villager.CurrentPathSegmentId = null;
            villager.PathProgress = 0f;
            villager.State = VillagerState.InDungeon;
            villager.Position = structure.Position;
            dungeon.AcceptVillager(villager.Id, villager.HasWeapon, villager.HasArmor);
            villager.CurrentActivity = $"Exploring {dungeon.DungeonId}";
            _eventBus.Publish(new VillagerEnteredDungeonEvent(
                villager.Id, dungeon.Id, dungeon.DungeonId));
            return true;
        }

        // Craft Station — villager enters, deposits items, and potentially crafts
        if (structure is CraftStationLogic craft)
        {
            segment.RemoveOccupant(villager.Id);
            villager.CurrentPathSegmentId = null;
            villager.PathProgress = 0f;
            villager.State = VillagerState.EnteringBuilding;
            villager.Position = structure.Position;
            craft.PendingVillagerIds.Add(villager.Id.Value);
            _eventBus.Publish(new VillagerEnteredBuildingEvent(
                villager.Id, craft.Id, craft.ProtoId));
            return true;
        }

        // Non-gathering structures — original behavior
        segment.RemoveOccupant(villager.Id);
        villager.CurrentPathSegmentId = null;
        villager.PathProgress = 0f;
        villager.State = VillagerState.EnteringBuilding;
        villager.Position = structure.Position;

        // Handle specific structure interactions
        if (structure is TrainingBuildingLogic school)
        {
            if (school.AcceptTrainee(villager))
            {
                _eventBus.Publish(new VillagerTrainingStartedEvent(
                    villager.Id, school.ProtoId, school.OutputClass.ToString()));
            }
        }
        else if (structure is StockpileLogic stockpile)
        {
            stockpile.PendingVillagerIds.Add(villager.Id);
        }

        return true;
    }
}
