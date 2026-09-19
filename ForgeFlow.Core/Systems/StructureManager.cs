using ForgeFlow.Core.Commands;
using ForgeFlow.Core.Data;
using ForgeFlow.Core.Entities;
using ForgeFlow.Core.Events;
using ForgeFlow.Core.Proto;
using ForgeFlow.Core.Proto.Logic;
using ForgeFlow.Core.Proto.Prototypes;

namespace ForgeFlow.Core.Systems;

/// <summary>
/// Single source of truth for all structure operations: ticking, spawner dispatch,
/// resource node ticking, ghost cleanup, and gating enforcement for structure placement.
/// Self-subscribes to EarlyTick (structure/resource ticking) and LateTick (ghost cleanup).
/// </summary>
public sealed class StructureManager : IGameSystem, IDisposable
{
    private readonly EntityManager _entityManager;
    private readonly PathGateManager _pathGateManager;
    private readonly PathNodeManager _pathNodeManager;
    private readonly VillagerSystem _villagerSystem;
    private readonly ItemManager _itemManager;
    private readonly ItemRegistry _itemRegistry;
    private readonly RecipeRegistry _recipeRegistry;
    private readonly GatingLimits _gatingLimits;
    private readonly EventBus _eventBus;
    private readonly CommandBus _commandBus;
    private readonly TutorialSystem _tutorialSystem;
    private readonly ResearchManager _researchManager;

    // ── IStructureTickHandler dispatch ──
    private StructureTickOutput _tickOutput = new StructureTickOutput
    {
        PendingExits = new PendingExit[16],
        PendingItems = new PendingItemOutput[16]
    };
    private readonly VillagerLookup _villagerLookup;

    // ── IRoutingNodeTickHandler dispatch ──
    private RoutingTickOutput _routingTickOutput;

    // ── IItemOutputStrategy dispatch ──
    private readonly ItemOutputContext _itemOutputContext;

    public StructureManager(
        EntityManager entityManager,
        PathGateManager pathGateManager,
        PathNodeManager pathNodeManager,
        VillagerSystem villagerSystem,
        ItemManager itemManager,
        ItemRegistry itemRegistry,
        RecipeRegistry recipeRegistry,
        GatingLimits gatingLimits,
        EventBus eventBus,
        CommandBus commandBus,
        TutorialSystem tutorialSystem,
        ResearchManager researchManager)
    {
        _entityManager = entityManager;
        _pathGateManager = pathGateManager;
        _pathNodeManager = pathNodeManager;
        _villagerSystem = villagerSystem;
        _itemManager = itemManager;
        _itemRegistry = itemRegistry;
        _recipeRegistry = recipeRegistry;
        _gatingLimits = gatingLimits;
        _eventBus = eventBus;
        _commandBus = commandBus;
        _tutorialSystem = tutorialSystem;
        _researchManager = researchManager;

        _villagerLookup = LookupVillager;

        _itemOutputContext = new ItemOutputContext(
            _itemManager, _recipeRegistry, _itemRegistry,
            _villagerSystem.VillagerIndex);

        _eventBus.Subscribe<SimulationEarlyTickEvent>(OnEarlyTick);
        _eventBus.Subscribe<SimulationLateTickEvent>(OnLateTick);

        _commandBus.Register<PlaceStructureCommand>(HandlePlaceStructure);
        _commandBus.Register<DemolishCommand>(HandleDemolish);
        _commandBus.Register<RotateEntityCommand>(HandleRotateEntity);
        _commandBus.Register<SetFilterCommand>(HandleSetFilter);
        _commandBus.Register<SetRecipeCommand>(HandleSetRecipe);
        _commandBus.Register<SetStockpileFilterCommand>(HandleSetStockpileFilter);
    }

    public void Dispose()
    {
        _eventBus.Unsubscribe<SimulationEarlyTickEvent>(OnEarlyTick);
        _eventBus.Unsubscribe<SimulationLateTickEvent>(OnLateTick);
        _commandBus.Unregister<PlaceStructureCommand>();
        _commandBus.Unregister<DemolishCommand>();
        _commandBus.Unregister<RotateEntityCommand>();
        _commandBus.Unregister<SetFilterCommand>();
        _commandBus.Unregister<SetRecipeCommand>();
        _commandBus.Unregister<SetStockpileFilterCommand>();
    }

    private void OnEarlyTick(SimulationEarlyTickEvent e) => Tick(e.DeltaTime);
    private void OnLateTick(SimulationLateTickEvent e) => CleanupGhosts(_entityManager.Heroes);

    // ── Proxy Properties ─────────────────────────────────────────────

    /// <summary>All structures keyed by ID.</summary>
    public IReadOnlyDictionary<EntityId, Structure> Structures => _entityManager.Structures;

    /// <summary>All routing nodes keyed by ID.</summary>
    public IReadOnlyDictionary<EntityId, RoutingNodeBase> RoutingNodes => _entityManager.RoutingNodes;

    /// <summary>All resource nodes keyed by ID.</summary>
    public IReadOnlyDictionary<EntityId, ResourceNodeLogic> ResourceNodes => _entityManager.ResourceNodes;

    // ── Placement with Gating ───────────────────────────────────────

    /// <summary>
    /// Adds a Proto-based entity using the current research tier from ResearchManager.
    /// Routing entities are automatically routed to the routing nodes collection.
    /// </summary>
    public bool AddProtoStructure(StructureBase entity, GridPosRPG position)
    {
        return AddProtoStructure(entity, position, _researchManager.CurrentTier);
    }

    /// <summary>
    /// Adds a Proto-based entity to the world.
    /// Routing entities go to their own collection; structures go to the structures collection.
    /// Gold/resource costs are enforced by command handlers (HandlePlaceStructure).
    /// </summary>
    public bool AddProtoStructure(StructureBase entity, GridPosRPG position, int currentResearchTier)
    {
        if (entity is RoutingNodeBase routingNode)
        {
            _entityManager.AddRoutingNode(routingNode, position);
            _eventBus.Publish(new RoutingNodePlacedEvent(routingNode.Id, routingNode.GetCategoryName(), position));
        }
        else if (entity is Structure structure)
        {
            _entityManager.AddStructure(structure, position);
            _eventBus.Publish(new StructurePlacedEvent(structure.Id, structure.GetCategoryName(), position));

            // Resolve rest recipe for Inn (Central Manager + Events pattern)
            if (structure is InnLogic inn && !string.IsNullOrEmpty(inn.RestRecipeId))
            {
                if (_recipeRegistry.TryGet(inn.RestRecipeId, out var restRecipe))
                {
                    inn.SetRestRecipe(restRecipe.Id, restRecipe.CraftDuration);
                }
            }
        }

        _tutorialSystem.AdvanceCondition(TutorialConditionType.PlaceStructure);
        return true;
    }

    // ── Command Handlers ────────────────────────────────────────────

    /// <summary>
    /// Handles <see cref="PlaceStructureCommand"/>: validates gold, deducts cost,
    /// places the entity, publishes GoldChangedEvent, and advances tutorial.
    /// This is the single source of truth for structure placement transactions.
    /// </summary>
    private CommandResult HandlePlaceStructure(PlaceStructureCommand cmd)
    {
        int cost = EconomyConfig.GetStructureCost(cmd.Category);
        int oldGold = _itemManager.GetStock("gold");

        if (!_itemManager.TrySpendStock("gold", cost))
        {
            _eventBus.Publish(new GatingBlockedEvent("gold", _itemManager.GetStock("gold"), cost, _researchManager.CurrentTier));
            return CommandResult.Fail("Not enough gold");
        }

        if (!AddProtoStructure(cmd.Entity, cmd.Position))
        {
            // Placement failed — refund gold
            _itemManager.AddStock("gold", cost);
            return CommandResult.Fail("Placement blocked");
        }

        _eventBus.Publish(new GoldChangedEvent(oldGold, _itemManager.GetStock("gold"), $"Placed {cmd.Category}"));

        // Advance tutorial for specific structure placement
        _tutorialSystem.AdvanceCondition(TutorialConditionType.PlaceSpecificStructure, cmd.Category);

        return CommandResult.Ok();
    }

    /// <summary>
    /// Handles <see cref="DemolishCommand"/>: resolves entity type at position
    /// (PathGate → RoutingNode → Structure → PathSegment) and delegates to
    /// the appropriate manager's removal method.
    /// </summary>
    private CommandResult HandleDemolish(DemolishCommand cmd)
    {
        var gate = _entityManager.GetPathGateAt(cmd.Position);
        if (gate != null)
        {
            _pathGateManager.RemovePathGate(cmd.Position);
            return CommandResult.Ok();
        }

        var routingNode = _entityManager.GetRoutingNodeAt(cmd.Position);
        if (routingNode != null)
        {
            _pathNodeManager.RemoveRoutingNode(cmd.Position);
            return CommandResult.Ok();
        }

        var structure = _entityManager.GetStructureAt(cmd.Position);
        if (structure != null)
        {
            DemolishStructure(cmd.Position);
            return CommandResult.Ok();
        }

        var pathSeg = _entityManager.GetPathSegmentAt(cmd.Position);
        if (pathSeg != null)
        {
            _pathNodeManager.RemovePathSegment(cmd.Position);
            return CommandResult.Ok();
        }

        return CommandResult.Fail("Nothing to demolish at position");
    }

    /// <summary>
    /// Handles <see cref="RotateEntityCommand"/>: resolves entity type at position
    /// (PathSegment, PathGate, RoutingNode) and delegates to the appropriate
    /// manager's rotation method.
    /// </summary>
    private CommandResult HandleRotateEntity(RotateEntityCommand cmd)
    {
        var pathSeg = _entityManager.GetPathSegmentAt(cmd.Position);
        if (pathSeg != null)
        {
            _pathNodeManager.RotatePathSegment(cmd.Position);
            return CommandResult.Ok();
        }

        var gate = _entityManager.GetPathGateAt(cmd.Position);
        if (gate != null)
        {
            _pathGateManager.RotatePathGate(cmd.Position);
            return CommandResult.Ok();
        }

        var routingNode = _entityManager.GetRoutingNodeAt(cmd.Position);
        if (routingNode != null)
        {
            _pathNodeManager.RotateRoutingNode(cmd.Position);
            return CommandResult.Ok();
        }

        return CommandResult.Fail("No rotatable entity at position");
    }

    /// <summary>
    /// Handles <see cref="SetFilterCommand"/>: sets or clears the single-item filter
    /// on a filter splitter. Delegates to <see cref="SetFilterSplitterItem"/> or
    /// <see cref="ClearFilterSplitterItem"/>.
    /// </summary>
    private CommandResult HandleSetFilter(SetFilterCommand cmd)
    {
        bool success = cmd.ItemId != null
            ? SetFilterSplitterItem(cmd.SplitterId, cmd.ItemId)
            : ClearFilterSplitterItem(cmd.SplitterId);

        return success
            ? CommandResult.Ok()
            : CommandResult.Fail("Invalid splitter or entity not found");
    }

    /// <summary>
    /// Handles <see cref="SetRecipeCommand"/>: sets or clears the active recipe
    /// on a craft station. Delegates to <see cref="SetCraftStationRecipe"/> or
    /// <see cref="ClearCraftStationRecipe"/>.
    /// </summary>
    private CommandResult HandleSetRecipe(SetRecipeCommand cmd)
    {
        bool success = cmd.RecipeId != null
            ? SetCraftStationRecipe(cmd.StationId, cmd.RecipeId)
            : ClearCraftStationRecipe(cmd.StationId);

        return success
            ? CommandResult.Ok()
            : CommandResult.Fail("Invalid station, recipe, or entity not found");
    }

    /// <summary>
    /// Handles <see cref="SetStockpileFilterCommand"/>: sets or clears the accepted
    /// product on a stockpile. Delegates to <see cref="SetStockpileProduct"/> or
    /// <see cref="ClearStockpileProduct"/>.
    /// </summary>
    private CommandResult HandleSetStockpileFilter(SetStockpileFilterCommand cmd)
    {
        bool success = cmd.ItemId != null
            ? SetStockpileProduct(cmd.StockpileId, cmd.ItemId)
            : ClearStockpileProduct(cmd.StockpileId);

        return success
            ? CommandResult.Ok()
            : CommandResult.Fail("Invalid stockpile or entity not found");
    }

    // ── Stockpile Product API ───────────────────────────────────────

    /// <summary>
    /// Sets the single accepted product on a stockpile. Central Manager + Events pattern:
    /// Manager calls Logic method → publishes event on next tick.
    /// Pass null to accept all items.
    /// </summary>
    public bool SetStockpileProduct(ulong stockpileId, string? itemId)
    {
        if (!_entityManager.Structures.TryGetValue(new EntityId(stockpileId), out var structure))
        {
            return false;
        }
        if (structure is not StockpileLogic stockpile)
        {
            return false;
        }
        stockpile.SetAcceptedItem(itemId);
        return true;
    }

    /// <summary>Clears the product assignment on a stockpile so it accepts all items.</summary>
    public bool ClearStockpileProduct(ulong stockpileId)
    {
        if (!_entityManager.Structures.TryGetValue(new EntityId(stockpileId), out var structure))
        {
            return false;
        }
        if (structure is not StockpileLogic stockpile)
        {
            return false;
        }
        stockpile.ClearAcceptedItem();
        return true;
    }

    // ── Filter Splitter API ─────────────────────────────────────────

    /// <summary>
    /// Sets the active recipe on a craft station. Central Manager + Events pattern:
    /// Manager resolves recipe data, calls Logic method, publishes event.
    /// Returns true if the recipe was successfully set.
    /// </summary>
    public bool SetCraftStationRecipe(ulong stationId, string recipeId)
    {
        if (!_entityManager.Structures.TryGetValue(new EntityId(stationId), out var structure))
        {
            return false;
        }
        if (structure is not CraftStationLogic craft)
        {
            return false;
        }
        if (!_recipeRegistry.TryGet(recipeId, out var recipe))
        {
            return false;
        }

        var inputs = new Dictionary<string, int>();
        foreach (var input in recipe.Inputs)
        {
            inputs[input.ItemId] = input.Quantity;
        }

        craft.SetActiveRecipe(recipe.Id, inputs, recipe.CraftDuration, recipe.OutputItemId);
        _eventBus.Publish(new CraftStationRecipeSelectedEvent(new EntityId(stationId), recipeId, craft.Position));
        return true;
    }

    /// <summary>Clears the active recipe on a craft station.</summary>
    public bool ClearCraftStationRecipe(ulong stationId)
    {
        if (!_entityManager.Structures.TryGetValue(new EntityId(stationId), out var structure))
        {
            return false;
        }
        if (structure is not CraftStationLogic craft)
        {
            return false;
        }
        craft.ClearActiveRecipe();
        return true;
    }

    /// <summary>
    /// Sets the single-item filter on a filter splitter. Central Manager + Events pattern:
    /// Manager calls Logic method → publishes event on next tick.
    /// Pass null to clear the filter.
    /// </summary>
    public bool SetFilterSplitterItem(ulong splitterId, string? itemId)
    {
        if (_entityManager.GetRoutingNode(new EntityId(splitterId)) is not FilterSplitterLogic splitter)
        {
            return false;
        }
        splitter.SetFilteredItem(itemId);
        return true;
    }

    /// <summary>Clears the single-item filter on a filter splitter so it uses rules only.</summary>
    public bool ClearFilterSplitterItem(ulong splitterId)
    {
        if (_entityManager.GetRoutingNode(new EntityId(splitterId)) is not FilterSplitterLogic splitter)
        {
            return false;
        }
        splitter.ClearFilter();
        return true;
    }

    // ── Demolish ────────────────────────────────────────────────────

    /// <summary>
    /// Removes a structure at the given position. Evicts all villagers inside
    /// (returning their inventory to stocks) and publishes an EntityDemolishedEvent.
    /// </summary>
    public void DemolishStructure(GridPosRPG position)
    {
        var structure = _entityManager.GetStructureAt(position);
        if (structure == null) { return; }

        // Evict villagers that are currently inside this structure.
        // A villager is "inside" if it is in Working state, not on a path segment,
        // and positioned at the structure's tile.
        var villagers = _villagerSystem.Villagers;
        for (int i = villagers.Count - 1; i >= 0; i--)
        {
            var v = villagers[i];
            if (v.State == VillagerState.Working
                && !v.CurrentPathSegmentId.HasValue
                && v.Position == structure.Position)
            {
                _itemManager.ReturnVillagerInventory(v.Inventory);

                // Decrement the owning spawner's count so it can respawn
                if (v.OwnerStructureId.HasValue &&
                    _entityManager.GetStructure(new EntityId(v.OwnerStructureId.Value)) is VillageSpawnerLogic spawner)
                {
                    spawner.DecrementSpawnCount();
                }

                _villagerSystem.RemoveVillager(v.Id);
                _eventBus.Publish(new VillagerReturnedToPoolEvent(v.Id, "structure_demolished"));
            }
        }

        string category = structure.GetCategoryName();
        var entityId = structure.Id;
        _entityManager.RemoveStructure(position);
        _eventBus.Publish(new EntityDemolishedEvent(entityId, category, position));
    }

    // ── Tick ─────────────────────────────────────────────────────────

    /// <summary>
    /// Ticks all structures, dispatches pending villagers from spawners
    /// via exit gates, ticks routing nodes, and ticks resource nodes.
    /// </summary>
    public void Tick(float dt)
    {
        TickStructures(dt);
        TickRoutingNodes();
        TickResourceNodes(dt);
    }

    private void TickStructures(float dt)
    {
        var orderedStructures = _entityManager.StructuresOrdered;
        for (int i = 0; i < orderedStructures.Count; i++)
        {
            var structure = orderedStructures[i];
            structure.Tick(dt);

            var handler = structure.AsTickHandler;
            if (handler != null)
            {
                _tickOutput.Reset();

                handler.ProcessStructureTick(dt, _villagerLookup, ref _tickOutput);
                ProcessPendingItems(structure, ref _tickOutput);
                ProcessPendingExits(structure, ref _tickOutput);

                if (_tickOutput.TutorialAdvance.HasValue)
                {
                    _tutorialSystem.AdvanceCondition(_tickOutput.TutorialAdvance.Value);
                }
                if (_tickOutput.ConfigChanged)
                {
                    PublishConfigChangeEvent(structure);
                }
            }

            // Retry exit for villagers waiting for an exit gate/path
            if (structure.HasWaitingExits)
            {
                RetryStructureExits(structure);
            }

            if (structure is VillageSpawnerLogic vs && vs.PendingVillagers.Count > 0)
            {
                DispatchSpawnerVillagers(vs, structure.Id);
            }
        }
    }

    private VillagerLogic? LookupVillager(ulong id)
    {
        return _villagerSystem.VillagerIndex.TryGetValue(id, out var v) ? v : null;
    }

    /// <summary>
    /// Creates items from <see cref="StructureTickOutput.PendingItems"/> and gives them
    /// to the target villager or places them at the structure's output position.
    /// Publishes appropriate events per structure type.
    /// </summary>
    private void ProcessPendingItems(Structure structure, ref StructureTickOutput output)
    {
        var strategy = structure.AsItemOutputStrategy;
        for (int i = 0; i < output.ItemCount; i++)
        {
            ref readonly var pending = ref output.PendingItems[i];

            if (strategy != null)
            {
                var result = strategy.ProcessItem(in pending, structure, _itemOutputContext);
                PublishItemOutputEvent(result, structure);
            }
            else
            {
                // Generic fallback (CraftStation, etc.): create item and give to villager
                var outputItem = CreateItemFromProto(pending.ItemProtoId, structure.OutputPosition);
                _eventBus.Publish(new ItemCraftedEvent(pending.ItemProtoId, structure.Position));
                if (outputItem != null && pending.TargetVillagerId.HasValue &&
                    _villagerSystem.VillagerIndex.TryGetValue(pending.TargetVillagerId.Value, out var craftVillager))
                {
                    craftVillager.TryPickUpItem(outputItem);
                }
            }
        }
    }

    /// <summary>
    /// Publishes the appropriate event based on the item-output result kind.
    /// Central Manager + Events rule: only the manager publishes events.
    /// </summary>
    private void PublishItemOutputEvent(ItemOutputResult result, Structure structure)
    {
        switch (result.Kind)
        {
            case ItemOutputKind.Gathered:
                _eventBus.Publish(new GatheringWorkerOutputEvent(
                    new EntityId(result.VillagerId), structure.Id,
                    result.ItemProtoId, result.Quantity, structure.Position));
                break;
            case ItemOutputKind.DroppedOff:
                _eventBus.Publish(new VillagerDroppedOffItemEvent(
                    new EntityId(result.VillagerId), result.ItemProtoId, structure.Id));
                break;
            case ItemOutputKind.Crafted:
                _eventBus.Publish(new ItemCraftedEvent(result.ItemProtoId, structure.Position));
                break;
        }
    }

    /// <summary>
    /// Ejects villagers listed in <see cref="StructureTickOutput.PendingExits"/> via PathGateManager.
    /// Parks them in <see cref="IStructureWithExits.WaitingToExitIds"/> if blocked.
    /// Publishes structure-specific events based on <see cref="PendingExit.ExitReason"/>.
    /// </summary>
    private void ProcessPendingExits(Structure structure, ref StructureTickOutput output)
    {
        for (int i = 0; i < output.ExitCount; i++)
        {
            ref readonly var exit = ref output.PendingExits[i];
            EntityId villagerId = new EntityId(exit.VillagerId);

            if (!_villagerSystem.VillagerIndex.TryGetValue(villagerId, out var villager))
            {
                continue;
            }

            // Publish structure-specific events before ejection
            PublishStructureSpecificEvent(in exit, structure, villagerId);

            if (_pathGateManager.ExitVillagerViaGate(villager, structure.Id))
            {
                villager.State = VillagerState.Travelling;
                _eventBus.Publish(new VillagerLeftBuildingEvent(
                    villagerId, structure.Id, structure.ProtoId));
            }
            else
            {
                // No exit gate or path blocked — park villager inside and retry next tick
                villager.State = VillagerState.EnteringBuilding;
                structure.WaitingToExitIds.Add(villagerId);
            }
        }
    }

    /// <summary>
    /// Publishes structure-specific lifecycle events based on the <see cref="PendingExit.ExitReason"/>.
    /// </summary>
    private void PublishStructureSpecificEvent(in PendingExit exit, Structure structure, EntityId villagerId)
    {
        switch (exit.ExitReason)
        {
            case StructureExitReason.TrainingComplete:
                _eventBus.Publish(new VillagerTrainingCompleteEvent(
                    villagerId, structure.ProtoId, exit.EventMetadata ?? string.Empty));
                break;
            case StructureExitReason.RestedAtHome:
                _eventBus.Publish(new VillagerRestedAtHomeEvent(villagerId, structure.Id));
                break;
            case StructureExitReason.Generic:
            default:
                break;
        }
    }

    /// <summary>
    /// Publishes configuration change events for structures that support them.
    /// </summary>
    private void PublishConfigChangeEvent(Structure structure)
    {
        if (structure is StockpileLogic)
        {
            _eventBus.Publish(new StockpileFilterChangedEvent(structure.Id, structure.Position));
        }
    }

    private void TickRoutingNodes()
    {
        var orderedRoutingNodes = _entityManager.RoutingNodesOrdered;
        for (int i = 0; i < orderedRoutingNodes.Count; i++)
        {
            var node = orderedRoutingNodes[i];
            var handler = node.AsRoutingTickHandler;
            if (handler != null)
            {
                _routingTickOutput.Reset();
                handler.ProcessRoutingTick(ref _routingTickOutput);

                if (_routingTickOutput.ConfigChanged)
                {
                    PublishRoutingConfigChangeEvent(node);
                }
            }
        }
    }

    /// <summary>
    /// Publishes configuration change events for routing nodes that support them.
    /// </summary>
    private void PublishRoutingConfigChangeEvent(RoutingNodeBase node)
    {
        if (node is FilterSplitterLogic)
        {
            _eventBus.Publish(new FilterSplitterFilterChangedEvent(node.Id, node.Position));
        }
    }

    /// <summary>
    /// Retries ejecting villagers stuck inside a structure (no exit gate or blocked path
    /// on the previous attempt). Removes successfully ejected villagers from the waiting list.
    /// Works for any structure that implements <see cref="IStructureWithExits"/>.
    /// </summary>
    private void RetryStructureExits(Structure structure)
    {
        for (int i = structure.WaitingToExitIds.Count - 1; i >= 0; i--)
        {
            EntityId villagerId = structure.WaitingToExitIds[i];
            if (!_villagerSystem.VillagerIndex.TryGetValue(villagerId, out var villager))
            {
                structure.WaitingToExitIds.RemoveAt(i);
                continue;
            }

            if (_pathGateManager.ExitVillagerViaGate(villager, structure.Id))
            {
                villager.State = VillagerState.Travelling;
                structure.WaitingToExitIds.RemoveAt(i);
                _eventBus.Publish(new VillagerLeftBuildingEvent(
                    villagerId, structure.Id, structure.ProtoId));
            }
        }
    }

    /// <summary>
    /// Creates a physical ItemInstance from an ItemProto ID via the ItemManager pool.
    /// Handles both equipment (with full stats) and plain items.
    /// </summary>
    private ItemInstance? CreateItemFromProto(string protoId, GridPosRPG outputPosition)
    {
        var proto = _itemRegistry.Get(protoId);
        if (proto != null && proto.Equipment != null)
        {
            return _itemManager.CreateEquipment(
                protoId, proto.Tier, proto.Equipment.Slot,
                proto.Equipment.BaseDamage, proto.Equipment.BaseDefense,
                proto.Equipment.BaseSpeed, proto.Equipment.CritChance,
                proto.Equipment.SpecialEffectId, outputPosition);
        }

        int tier = proto?.Tier ?? 1;
        var item = _itemManager.CreateItem(protoId, tier);
        item.Position = outputPosition;
        item.IsOnPath = true;
        return item;
    }

    private void DispatchSpawnerVillagers(VillageSpawnerLogic vs, EntityId structureId)
    {
        // Phase 1: enforce single-villager limit
        int? tutorialLimit = _tutorialSystem.Phase1VillagerLimit;
        if (tutorialLimit.HasValue && _villagerSystem.Count >= tutorialLimit.Value)
        {
            foreach (var villager in vs.PendingVillagers)
            {
                vs.DecrementSpawnCount();
            }
            vs.PendingVillagers.Clear();
            return;
        }

        // Find an Exit gate linked to this spawner
        PathGateLogic? exitGate = _pathGateManager.FindExitGateForStructure(structureId);

        if (exitGate == null)
        {
            // No exit gate — cannot spawn. Discard pending villagers.
            foreach (var villager in vs.PendingVillagers)
            {
                vs.DecrementSpawnCount();
            }
            vs.PendingVillagers.Clear();
            return;
        }

        foreach (var villager in vs.PendingVillagers)
        {
            villager.OwnerStructureId = structureId;
            villager.HomeId = structureId;
            vs.AssignResident(villager.Id);
            _villagerSystem.AddVillager(villager);

            // Try placing on a path via the exit gate
            if (_pathGateManager.ExitVillagerViaGate(villager, structureId))
            {
                // Successfully placed on path — villager is Travelling
            }
            else
            {
                // No path at gate's facing position — place villager
                // there as Idle (waiting for a path to be built).
                var waitPos = exitGate.FacingPosition;

                if (_entityManager.IsPositionOccupiedByVillager(waitPos, villager.Id))
                {
                    // Tile already occupied — clean up
                    _villagerSystem.RemoveVillager(villager.Id);
                    vs.DecrementSpawnCount();
                    continue;
                }
                else
                {
                    villager.Position = waitPos;
                    villager.State = VillagerState.Idle;
                }
            }

            _eventBus.Publish(new VillagerSpawnedEvent(villager.Id, villager.Name, villager.Position));
            _eventBus.Publish(new VillagerAssignedHomeEvent(villager.Id, structureId));
        }
        vs.PendingVillagers.Clear();
    }

    private void TickResourceNodes(float dt)
    {
        var orderedResourceNodes = _entityManager.ResourceNodesOrdered;
        for (int i = 0; i < orderedResourceNodes.Count; i++)
        {
            orderedResourceNodes[i].Tick(dt);
        }
    }

    // ── Ghost Cleanup ────────────────────────────────────────────────

    /// <summary>
    /// Removes dead heroes (ghosts) and drops their gear as scrap
    /// onto connected structures via path segments.
    /// </summary>
    public void CleanupGhosts(IReadOnlyList<HeroEntity> heroes)
    {
        for (int i = heroes.Count - 1; i >= 0; i--)
        {
            var hero = heroes[i];
            if (hero.State == HeroState.Ghost)
            {
                // Drop all gear as scrap onto nearest path
                var droppedGear = hero.UnequipAll();
                foreach (var gear in droppedGear)
                {
                    var scrapItem = _itemManager.CreateScrap(
                        gear.ProtoId, gear.Tier, gear.Slot,
                        gear.Damage, gear.Defense, gear.Speed, gear.CritChance,
                        hero.Position);

                    var seg = _entityManager.GetPathSegmentAt(hero.Position);
                    if (seg != null &&
                        seg.ConnectedStructureId.HasValue &&
                        _entityManager.GetStructure(new EntityId(seg.ConnectedStructureId.Value)) is RecipeEntity connectedRecipe)
                    {
                        connectedRecipe.TryEnqueueInput(scrapItem);
                    }
                }

                _entityManager.RemoveHero(hero.Id);
            }
        }
    }

    // ── Fusion ────────────────────────────────────────────────────────

    /// <summary>
    /// Creates a fusion altar at the given position and publishes a placement event.
    /// </summary>
    public FusionAltarLogic CreateFusionAltar(GridPosRPG pos)
    {
        var altar = new FusionAltarLogic(EntityId.Next());
        _entityManager.AddStructure(altar, pos);
        _eventBus.Publish(new ResourceProducedEvent("fusion_altar_placed", 1, pos));
        return altar;
    }

    /// <summary>
    /// Attempts to merge two heroes via fusion. Finds an available altar,
    /// validates hero levels match, and queues both heroes for fusion.
    /// </summary>
    public bool AttemptHeroMerge(ulong heroASeed, ulong heroBSeed)
    {
        HeroEntity? heroA = null;
        HeroEntity? heroB = null;

        foreach (var hero in _entityManager.Heroes)
        {
            if (hero.Seed == heroASeed) { heroA = hero; }
            else if (hero.Seed == heroBSeed) { heroB = hero; }
            if (heroA != null && heroB != null) { break; }
        }

        if (heroA == null || heroB == null) { return false; }
        if (heroA.Level != heroB.Level) { return false; }

        // Find a fusion altar that is active
        FusionAltarLogic? altar = null;
        var structures = _entityManager.StructuresOrdered;
        for (int si = 0; si < structures.Count; si++)
        {
            var structure = structures[si];
            if (structure is FusionAltarLogic a && a.IsActive && a.QueuedHeroIds.Count == 0)
            {
                altar = a;
                break;
            }
        }

        if (altar == null) { return false; }

        heroA.State = HeroState.AwaitingFusion;
        heroB.State = HeroState.AwaitingFusion;
        altar.QueueHero(heroA.Id);
        altar.QueueHero(heroB.Id);

        return true;
    }

    // ── Queries ──────────────────────────────────────────────────────

    /// <summary>Gets the number of VillageSpawner structures currently placed.</summary>
    public int CountSpawners() => _entityManager.CountSpawners();

    /// <summary>Gets the number of training buildings currently placed.</summary>
    public int CountTrainingBuildings() => _entityManager.CountTrainingBuildings();
}
