using ForgeFlow.Core.Entities;
using ForgeFlow.Core.Events;
using ForgeFlow.Core.Proto;
using ForgeFlow.Core.Proto.Logic;
using ForgeFlow.Core.Proto.Prototypes;
using ForgeFlow.Core.Systems;
using UnityEngine;
using EntityId = ForgeFlow.Core.Entities.EntityId;

namespace ForgeFlow.Presentation.Unity.Visuals
{

/// <summary>
/// Renders all path segments, structures, and routing nodes on the factory floor.
/// Subscribes to Core placement/removal/rotation events and creates or destroys
/// visual representations in response. Only animation runs per-frame.
/// </summary>
internal class PathRendererSystem : MonoBehaviour
{
    [Header("Path Visuals")]
    [SerializeField] private float _gridCellSize = 1.0f;
    [SerializeField] private float _pathAnimSpeed = 2.0f;

    private SimulationTicker _simulation = null!;
    private EventBus _eventBus = null!;
    private GameObjectPool _pool = null!;
    private readonly Dictionary<ulong, GameObject> _pathVisuals = new();
    private readonly Dictionary<ulong, GameObject> _structureVisuals = new();
    private readonly Dictionary<ulong, GameObject> _routingNodeVisuals = new();
    private float _pathAnimOffset;

    public void Initialize(SimulationTicker simulation, EventBus eventBus)
    {
        _simulation = simulation;
        _eventBus = eventBus;

        var poolRoot = new GameObject("PathRendererPool");
        poolRoot.transform.SetParent(transform);
        _pool = new GameObjectPool(poolRoot.transform);

        _eventBus.Subscribe<PathBuiltEvent>(OnPathBuilt);
        _eventBus.Subscribe<PathRemovedEvent>(OnPathRemoved);
        _eventBus.Subscribe<StructurePlacedEvent>(OnStructurePlaced);
        _eventBus.Subscribe<RoutingNodePlacedEvent>(OnRoutingNodePlaced);
        _eventBus.Subscribe<EntityDemolishedEvent>(OnEntityDemolished);
        _eventBus.Subscribe<EntityRotatedEvent>(OnEntityRotated);
    }

    private void OnDestroy()
    {
        if (_eventBus != null)
        {
            _eventBus.Unsubscribe<PathBuiltEvent>(OnPathBuilt);
            _eventBus.Unsubscribe<PathRemovedEvent>(OnPathRemoved);
            _eventBus.Unsubscribe<StructurePlacedEvent>(OnStructurePlaced);
            _eventBus.Unsubscribe<RoutingNodePlacedEvent>(OnRoutingNodePlaced);
            _eventBus.Unsubscribe<EntityDemolishedEvent>(OnEntityDemolished);
            _eventBus.Unsubscribe<EntityRotatedEvent>(OnEntityRotated);
        }
    }

    private void Update()
    {
        _pathAnimOffset += Time.deltaTime * _pathAnimSpeed;
        if (_pathAnimOffset > 1f)
        {
            _pathAnimOffset -= 1f;
        }
    }

    // ── Event Handlers ──────────────────────────────────────────────

    private void OnPathBuilt(PathBuiltEvent evt)
    {
        ulong id = evt.SegmentId;
        if (_pathVisuals.ContainsKey(id))
        {
            return;
        }

        if (_simulation.EntityManager.PathSegments.TryGetValue(new EntityId(id), out var segment))
        {
            _pathVisuals[id] = CreatePathVisual(segment);
        }
    }

    private void OnPathRemoved(PathRemovedEvent evt)
    {
        ulong id = evt.SegmentId;
        if (_pathVisuals.TryGetValue(id, out var go))
        {
            _pool.Release(go);
            _pathVisuals.Remove(id);
        }
    }

    private void OnStructurePlaced(StructurePlacedEvent evt)
    {
        ulong id = evt.StructureId;
        if (_structureVisuals.ContainsKey(id))
        {
            return;
        }

        if (_simulation.EntityManager.Structures.TryGetValue(new EntityId(id), out var structure))
        {
            _structureVisuals[id] = CreateStructureVisual(structure);
        }
    }

    private void OnRoutingNodePlaced(RoutingNodePlacedEvent evt)
    {
        ulong id = evt.NodeId;
        if (_routingNodeVisuals.ContainsKey(id))
        {
            return;
        }

        if (_simulation.EntityManager.RoutingNodes.TryGetValue(new EntityId(id), out var node))
        {
            _routingNodeVisuals[id] = CreateRoutingNodeVisual(node);
        }
    }

    private void OnEntityDemolished(EntityDemolishedEvent evt)
    {
        ulong id = evt.EntityId;
        if (_structureVisuals.TryGetValue(id, out var structGo))
        {
            _pool.Release(structGo);
            _structureVisuals.Remove(id);
        }
        else if (_routingNodeVisuals.TryGetValue(id, out var nodeGo))
        {
            _pool.Release(nodeGo);
            _routingNodeVisuals.Remove(id);
        }
    }

    private void OnEntityRotated(EntityRotatedEvent evt)
    {
        ulong id = evt.EntityId;
        float yRot = evt.NewFacing switch
        {
            Direction.North => 0f,
            Direction.East => 90f,
            Direction.South => 180f,
            Direction.West => 270f,
            _ => 0f
        };

        if (_pathVisuals.TryGetValue(id, out var pathGo))
        {
            pathGo.transform.rotation = Quaternion.Euler(0, yRot, 0);
        }
        else if (_routingNodeVisuals.TryGetValue(id, out var nodeGo))
        {
            nodeGo.transform.rotation = Quaternion.Euler(0, yRot, 0);
        }
    }

    private GameObject CreatePathVisual(PathSegmentLogic segment)
    {
        string poolKey = $"path_{segment.NodeType.ToString().ToLowerInvariant()}";
        var prefab = PrefabRegistry.GetPrefab("PathStraight");
        GameObject go = _pool.Get(poolKey, () =>
        {
            if (prefab != null && segment.NodeType == PathNodeType.Straight)
            {
                return UnityEngine.Object.Instantiate(prefab);
            }
            return RuntimePlaceholderFactory.CreatePathSegmentPlaceholder(segment.NodeType);
        });

        go.name = $"Path_{segment.Id}";
        go.transform.position = new Vector3(
            segment.Position.X * _gridCellSize,
            0.05f,
            segment.Position.Y * _gridCellSize);

        float yRotation = segment.Facing switch
        {
            Direction.North => 0f,
            Direction.East => 90f,
            Direction.South => 180f,
            Direction.West => 270f,
            _ => 0f
        };
        go.transform.rotation = Quaternion.Euler(0, yRotation, 0);

        return go;
    }

    private GameObject CreateStructureVisual(Structure structure)
    {
        string poolKey = $"structure_{structure.GetCategoryName()}";
        var go = _pool.Get(poolKey, () =>
            RuntimePlaceholderFactory.CreateStructureVisual(structure.GetCategoryName(), structure.OutputDirection));
        go.name = $"{structure.GetCategoryName()}_{structure.Id}";
        go.transform.position = new Vector3(
            structure.Position.X * _gridCellSize,
            0.5f,
            structure.Position.Y * _gridCellSize);

        Debug.Log($"[Path] Created visual for {structure.GetCategoryName()} at {structure.Position}");

        return go;
    }

    // ── Visual Factories ────────────────────────────────────────────

    private GameObject CreateRoutingNodeVisual(RoutingNodeBase node)
    {
        string poolKey = $"routing_{node.GetCategoryName()}";
        var go = _pool.Get(poolKey, () => new GameObject());
        go.name = $"RoutingNode_{node.GetCategoryName()}_{node.Id}";
        go.transform.position = new Vector3(
            node.Position.X * _gridCellSize,
            0.05f,
            node.Position.Y * _gridCellSize);

        if (node is FilterSplitterLogic filterSplitter)
        {
            var mb = go.AddComponent<FilterSplitterMb>();
            mb.Initialize(filterSplitter);
        }
        else if (node is BalancerLogic balancer)
        {
            var mb = go.AddComponent<BalancerMb>();
            mb.Initialize(balancer);
        }
        else if (node is CheckGateLogic checkGate)
        {
            var mb = go.AddComponent<CheckGateMb>();
            mb.Initialize(checkGate);
        }

        Debug.Log($"[Path] Created routing node visual for {node.GetCategoryName()} at {node.Position}");

        return go;
    }

    private static Vector3 DirectionToVector(Direction dir) => dir switch
    {
        Direction.North => new Vector3(0, 0, 1),
        Direction.East => new Vector3(1, 0, 0),
        Direction.South => new Vector3(0, 0, -1),
        Direction.West => new Vector3(-1, 0, 0),
        _ => Vector3.zero
    };

    // NOTE: Refresh*Visuals() methods were removed. They were legacy hooks from the
    // pre-event-driven renderer that indiscriminately released every cached visual
    // back to the pool. After Change #3 made this system event-driven, callers that
    // still invoked them wiped out the freshly-created visual for the entity that
    // had just been placed — which is why building placement looked broken even
    // though Core state was correct. Add/remove visuals are now driven exclusively
    // by PathBuiltEvent / PathRemovedEvent / StructurePlacedEvent /
    // RoutingNodePlacedEvent / EntityDemolishedEvent / EntityRotatedEvent.
}
}
