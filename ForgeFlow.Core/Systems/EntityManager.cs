using ForgeFlow.Core.Entities;
using ForgeFlow.Core.Proto;
using ForgeFlow.Core.Proto.Logic;

namespace ForgeFlow.Core.Systems;

/// <summary>
/// Single source of truth for all entities in the world. Provides centralized
/// lookup by ID, type-filtered queries, and Add/Remove operations that
/// automatically update TileManager occupancy.
/// </summary>
public sealed class EntityManager : IGameSystem
{
    private readonly TileManager _tileManager;
    private readonly VillagerSystem _villagerSystem;

    // Heroes
    private readonly List<HeroEntity> _heroes = new();
    private readonly Dictionary<EntityId, HeroEntity> _heroIndex = new();
    private readonly Dictionary<EntityId, int> _heroIndexInList = new();

    // Structures
    private readonly Dictionary<EntityId, Structure> _structures = new();
    private readonly List<Structure> _orderedStructures = new();
    private readonly Dictionary<EntityId, int> _structureIndexInList = new();

    // Routing Nodes
    private readonly Dictionary<EntityId, RoutingNodeBase> _routingNodes = new();
    private readonly List<RoutingNodeBase> _orderedRoutingNodes = new();
    private readonly Dictionary<EntityId, int> _routingNodeIndexInList = new();

    // Path Segments
    private readonly Dictionary<EntityId, PathSegmentLogic> _pathSegments = new();
    private readonly List<PathSegmentLogic> _orderedPathSegments = new();
    private readonly Dictionary<EntityId, int> _pathSegmentIndexInList = new();

    // Path Gates
    private readonly Dictionary<EntityId, PathGateLogic> _pathGates = new();
    private readonly List<PathGateLogic> _orderedPathGates = new();
    private readonly Dictionary<EntityId, int> _pathGateIndexInList = new();

    // Resource Nodes
    private readonly Dictionary<EntityId, ResourceNodeLogic> _resourceNodes = new();
    private readonly List<ResourceNodeLogic> _orderedResourceNodes = new();
    private readonly Dictionary<EntityId, int> _resourceNodeIndexInList = new();

    public EntityManager(TileManager tileManager, VillagerSystem villagerSystem)
    {
        _tileManager = tileManager;
        _villagerSystem = villagerSystem;
    }

    /// <summary>The TileManager used for spatial occupancy tracking.</summary>
    public TileManager TileManager => _tileManager;

    // ── Heroes ───────────────────────────────────────────────────────

    /// <summary>All heroes currently alive in the world.</summary>
    public IReadOnlyList<HeroEntity> Heroes => _heroes;

    /// <summary>Fast hero lookup by ID.</summary>
    public IReadOnlyDictionary<EntityId, HeroEntity> HeroIndex => _heroIndex;

    /// <summary>Returns the hero with the given ID, or null.</summary>
    public HeroEntity? GetHero(EntityId id) => _heroIndex.TryGetValue(id, out var h) ? h : null;

    public void AddHero(HeroEntity hero)
    {
        _heroIndexInList[hero.Id] = _heroes.Count;
        _heroes.Add(hero);
        _heroIndex[hero.Id] = hero;
    }

    public void RemoveHero(EntityId heroId)
    {
        if (_heroIndex.Remove(heroId))
        {
            SwapRemove(_heroes, _heroIndexInList, heroId);
        }
    }

    /// <summary>
    /// Atomically removes two source heroes and adds a fused result hero.
    /// Used by FusionCalculator to avoid exposing mutable collections.
    /// </summary>
    public void FuseHeroes(EntityId removeId1, EntityId removeId2, HeroEntity fusedHero)
    {
        RemoveHero(removeId1);
        RemoveHero(removeId2);
        AddHero(fusedHero);
    }

    // ── Structures ─────────────────────────────────────────────────────

    /// <summary>All structures keyed by ID.</summary>
    public IReadOnlyDictionary<EntityId, Structure> Structures => _structures;

    /// <summary>All structures in deterministic insertion order.</summary>
    public IReadOnlyList<Structure> StructuresOrdered => _orderedStructures;

    /// <summary>Number of placed structures.</summary>
    public int StructureCount => _structures.Count;

    /// <summary>Returns the structure with the given ID, or null.</summary>
    public Structure? GetStructure(EntityId id) =>
        _structures.TryGetValue(id, out var s) ? s : null;

    /// <summary>Returns the structure at the given grid position, or null.</summary>
    public Structure? GetStructureAt(GridPosRPG pos)
    {
        var id = _tileManager.GetStructureIdAt(pos);
        if (id.HasValue && _structures.TryGetValue(new EntityId(id.Value), out var structure))
        {
            return structure;
        }
        return null;
    }

    /// <summary>Adds a structure at the given position and registers tile occupancy.</summary>
    public void AddStructure(Structure structure, GridPosRPG pos)
    {
        structure.Position = pos;
        _structures[structure.Id] = structure;
        _structureIndexInList[structure.Id] = _orderedStructures.Count;
        _orderedStructures.Add(structure);
        _tileManager.SetOccupant(pos, TileOccupantType.Structure, structure.Id.Value);
    }

    /// <summary>Removes the structure at the given position and clears tile occupancy.</summary>
    public void RemoveStructure(GridPosRPG pos)
    {
        var structureId = _tileManager.GetStructureIdAt(pos);
        if (structureId == null) { return; }
        var entityId = new EntityId(structureId.Value);
        if (_structures.Remove(entityId))
        {
            SwapRemove(_orderedStructures, _structureIndexInList, entityId);
        }
        _tileManager.ClearOccupant(pos);
    }

    // ── Routing Nodes ─────────────────────────────────────────────────

    /// <summary>All routing nodes keyed by ID.</summary>
    public IReadOnlyDictionary<EntityId, RoutingNodeBase> RoutingNodes => _routingNodes;

    /// <summary>All routing nodes in deterministic insertion order.</summary>
    public IReadOnlyList<RoutingNodeBase> RoutingNodesOrdered => _orderedRoutingNodes;

    /// <summary>Number of placed routing nodes.</summary>
    public int RoutingNodeCount => _routingNodes.Count;

    /// <summary>Returns the routing node with the given ID, or null.</summary>
    public RoutingNodeBase? GetRoutingNode(EntityId id) =>
        _routingNodes.TryGetValue(id, out var r) ? r : null;

    /// <summary>Adds a routing node at the given position and registers tile occupancy.</summary>
    public void AddRoutingNode(RoutingNodeBase node, GridPosRPG pos)
    {
        node.Position = pos;
        _routingNodes[node.Id] = node;
        _routingNodeIndexInList[node.Id] = _orderedRoutingNodes.Count;
        _orderedRoutingNodes.Add(node);
        _tileManager.SetOccupant(pos, TileOccupantType.Structure, node.Id.Value);
    }

    /// <summary>Returns the routing node at the given grid position, or null.</summary>
    public RoutingNodeBase? GetRoutingNodeAt(GridPosRPG pos)
    {
        var id = _tileManager.GetStructureIdAt(pos);
        if (id.HasValue && _routingNodes.TryGetValue(new EntityId(id.Value), out var node))
        {
            return node;
        }
        return null;
    }

    /// <summary>Removes the routing node at the given position and clears tile occupancy.</summary>
    public void RemoveRoutingNode(GridPosRPG pos)
    {
        var nodeId = _tileManager.GetStructureIdAt(pos);
        if (nodeId == null) { return; }
        var entityId = new EntityId(nodeId.Value);
        if (_routingNodes.Remove(entityId))
        {
            SwapRemove(_orderedRoutingNodes, _routingNodeIndexInList, entityId);
        }
        _tileManager.ClearOccupant(pos);
    }

    /// <summary>
    /// Unified lookup for entities that occupy "structure" tiles.
    /// Checks structures first, then routing nodes. Used when resolving
    /// ConnectedStructureId which can reference either type.
    /// </summary>
    public StructureBase? GetStructureOrRoutingNode(EntityId id)
    {
        if (_structures.TryGetValue(id, out var structure)) { return structure; }
        if (_routingNodes.TryGetValue(id, out var routing)) { return routing; }
        return null;
    }

    // ── Path Segments ────────────────────────────────────────────────

    /// <summary>All path segments keyed by ID.</summary>
    public IReadOnlyDictionary<EntityId, PathSegmentLogic> PathSegments => _pathSegments;

    /// <summary>All path segments in deterministic insertion order.</summary>
    public IReadOnlyList<PathSegmentLogic> PathSegmentsOrdered => _orderedPathSegments;

    /// <summary>Number of placed path segments.</summary>
    public int PathSegmentCount => _pathSegments.Count;

    /// <summary>Returns the path segment with the given ID, or null.</summary>
    public PathSegmentLogic? GetPathSegment(EntityId id) =>
        _pathSegments.TryGetValue(id, out var s) ? s : null;

    /// <summary>Returns the path segment at the given grid position, or null.</summary>
    public PathSegmentLogic? GetPathSegmentAt(GridPosRPG pos)
    {
        var id = _tileManager.GetPathIdAt(pos);
        if (id.HasValue && _pathSegments.TryGetValue(new EntityId(id.Value), out var seg))
        {
            return seg;
        }
        return null;
    }

    /// <summary>Adds a path segment and registers tile occupancy.</summary>
    public void AddPathSegment(PathSegmentLogic segment)
    {
        _pathSegments[segment.Id] = segment;
        _pathSegmentIndexInList[segment.Id] = _orderedPathSegments.Count;
        _orderedPathSegments.Add(segment);
        _tileManager.SetOccupant(segment.Position, TileOccupantType.Path, segment.Id.Value);
    }

    /// <summary>Low-level removal: deletes a path segment from storage and clears tile occupancy.
    /// Callers that need full coordinated removal (neighbor unlinking, villager eviction,
    /// cache rebuild, event publishing) should use PathNodeManager.RemovePathSegment instead.</summary>
    public void UnregisterPathSegment(EntityId id, GridPosRPG pos)
    {
        if (_pathSegments.Remove(id))
        {
            SwapRemove(_orderedPathSegments, _pathSegmentIndexInList, id);
        }
        _tileManager.ClearOccupant(pos);
    }

    // ── Path Gates ───────────────────────────────────────────────────

    /// <summary>All path gates keyed by ID.</summary>
    public IReadOnlyDictionary<EntityId, PathGateLogic> PathGates => _pathGates;

    /// <summary>All path gates in deterministic insertion order.</summary>
    public IReadOnlyList<PathGateLogic> PathGatesOrdered => _orderedPathGates;

    /// <summary>Number of placed path gates.</summary>
    public int PathGateCount => _pathGates.Count;

    /// <summary>Returns the path gate with the given ID, or null.</summary>
    public PathGateLogic? GetPathGate(EntityId id) =>
        _pathGates.TryGetValue(id, out var g) ? g : null;

    /// <summary>Returns the path gate at the given grid position, or null.</summary>
    public PathGateLogic? GetPathGateAt(GridPosRPG pos)
    {
        var id = _tileManager.GetPathGateIdAt(pos);
        if (id.HasValue && _pathGates.TryGetValue(new EntityId(id.Value), out var gate))
        {
            return gate;
        }
        return null;
    }

    /// <summary>Adds a path gate and registers tile occupancy.</summary>
    public void AddPathGate(PathGateLogic gate)
    {
        _pathGates[gate.Id] = gate;
        _pathGateIndexInList[gate.Id] = _orderedPathGates.Count;
        _orderedPathGates.Add(gate);
        _tileManager.SetOccupant(gate.Position, TileOccupantType.PathGate, gate.Id.Value);
    }

    /// <summary>Removes a path gate by ID and clears tile occupancy.</summary>
    public void RemovePathGate(EntityId id, GridPosRPG pos)
    {
        if (_pathGates.Remove(id))
        {
            SwapRemove(_orderedPathGates, _pathGateIndexInList, id);
        }
        _tileManager.ClearOccupant(pos);
    }

    // ── Resource Nodes ───────────────────────────────────────────────

    /// <summary>All resource nodes keyed by ID.</summary>
    public IReadOnlyDictionary<EntityId, ResourceNodeLogic> ResourceNodes => _resourceNodes;

    /// <summary>All resource nodes in deterministic insertion order.</summary>
    public IReadOnlyList<ResourceNodeLogic> ResourceNodesOrdered => _orderedResourceNodes;

    /// <summary>Returns the resource node with the given ID, or null.</summary>
    public ResourceNodeLogic? GetResourceNode(EntityId id) =>
        _resourceNodes.TryGetValue(id, out var n) ? n : null;

    /// <summary>Number of placed resource nodes.</summary>
    public int ResourceNodeCount => _resourceNodes.Count;

    /// <summary>Returns the resource node at the given grid position, or null.</summary>
    public ResourceNodeLogic? GetResourceNodeAt(GridPosRPG pos)
    {
        var id = _tileManager.GetResourceNodeIdAt(pos);
        if (id.HasValue && _resourceNodes.TryGetValue(new EntityId(id.Value), out var node))
        {
            return node;
        }
        return null;
    }

    /// <summary>Adds a resource node and registers tile occupancy.</summary>
    public void AddResourceNode(ResourceNodeLogic node)
    {
        _resourceNodes[node.Id] = node;
        _resourceNodeIndexInList[node.Id] = _orderedResourceNodes.Count;
        _orderedResourceNodes.Add(node);
        _tileManager.SetOccupant(node.Position, TileOccupantType.ResourceNode, node.Id.Value);
    }

    /// <summary>Removes a resource node by ID and clears tile occupancy.</summary>
    public void RemoveResourceNode(EntityId id, GridPosRPG pos)
    {
        if (_resourceNodes.Remove(id))
        {
            SwapRemove(_orderedResourceNodes, _resourceNodeIndexInList, id);
        }
        _tileManager.ClearOccupant(pos);
    }

    // ── Villagers (delegates to VillagerSystem) ──────────────────────

    /// <summary>All villagers currently alive.</summary>
    public List<VillagerLogic> Villagers => _villagerSystem.Villagers;

    /// <summary>Returns the villager with the given ID, or null.</summary>
    public VillagerLogic? GetVillager(EntityId id) =>
        _villagerSystem.VillagerIndex.TryGetValue(id.Value, out var v) ? v : null;

    // ── Typed Queries ────────────────────────────────────────────────

    /// <summary>Gets the number of VillageSpawner structures currently placed.</summary>
    public int CountSpawners()
    {
        int count = 0;
        foreach (var m in _orderedStructures)
        {
            if (m is VillageSpawnerLogic) { count++; }
        }
        return count;
    }

    /// <summary>Gets the number of training buildings currently placed.</summary>
    public int CountTrainingBuildings()
    {
        int count = 0;
        foreach (var m in _orderedStructures)
        {
            if (m is TrainingBuildingLogic) { count++; }
        }
        return count;
    }

    /// <summary>
    /// Returns true if any villager (other than excludeId) occupies the given position.
    /// Used for tile occupancy checks on tiles that may not have a path segment.
    /// </summary>
    public bool IsPositionOccupiedByVillager(GridPosRPG pos, ulong excludeId)
    {
        var villagers = _villagerSystem.Villagers;
        for (int i = 0; i < villagers.Count; i++)
        {
            if (villagers[i].Id != excludeId && villagers[i].Position == pos)
            {
                return true;
            }
        }
        return false;
    }

    // ── Lifecycle ────────────────────────────────────────────────────

    /// <summary>Clears all entity collections, villagers, and tile occupancy.</summary>
    public void Clear()
    {
        _heroes.Clear();
        _heroIndex.Clear();
        _heroIndexInList.Clear();
        _structures.Clear();
        _orderedStructures.Clear();
        _structureIndexInList.Clear();
        _routingNodes.Clear();
        _orderedRoutingNodes.Clear();
        _routingNodeIndexInList.Clear();
        _pathSegments.Clear();
        _orderedPathSegments.Clear();
        _pathSegmentIndexInList.Clear();
        _pathGates.Clear();
        _orderedPathGates.Clear();
        _pathGateIndexInList.Clear();
        _resourceNodes.Clear();
        _orderedResourceNodes.Clear();
        _resourceNodeIndexInList.Clear();
        _villagerSystem.Clear();
        _tileManager.Clear();
    }

    /// <summary>True O(1) swap-remove: looks up the list slot via <paramref name="indexById"/>,
    /// moves the last element into that slot, updates the moved element's index, and
    /// removes the trailing slot. Keeps dict and list in sync without any O(n) scan.</summary>
    private static void SwapRemove<T>(List<T> list, Dictionary<EntityId, int> indexById, EntityId id)
        where T : EntityBase
    {
        if (!indexById.TryGetValue(id, out int idx)) { return; }
        int last = list.Count - 1;
        if (idx != last)
        {
            var moved = list[last];
            list[idx] = moved;
            indexById[moved.Id] = idx;
        }
        list.RemoveAt(last);
        indexById.Remove(id);
    }
}
