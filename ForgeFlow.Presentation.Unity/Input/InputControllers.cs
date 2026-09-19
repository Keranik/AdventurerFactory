using EntityId = ForgeFlow.Core.Entities.EntityId;
using ForgeFlow.Core.Commands;
using ForgeFlow.Core.Entities;
using ForgeFlow.Core.Events;
using ForgeFlow.Core.Localization;
using ForgeFlow.Core.Systems;
using ForgeFlow.Presentation.Unity.Cameras;
using ForgeFlow.Presentation.Unity.UI.Components;
using ForgeFlow.Presentation.Unity.Visuals;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ForgeFlow.Presentation.Unity.Input
{

/// <summary>
/// Input controller for structure placement (PathGate, Spawner, Forge, etc.).
/// Priority 200 — highest among gameplay controllers.
/// Active whenever <see cref="StructurePlacer.IsPlacing"/> is true.
/// Handles left-click (place), R (rotate ghost), right-click / Escape (cancel).
/// Delegates all placement logic to <see cref="StructurePlacer"/>.
/// </summary>
internal sealed class StructurePlacementController : IControllerWithShortcuts
{
    private readonly StructurePlacer _placer;

    private readonly ShortcutDescriptor[] _shortcuts;

    public string Name => "StructurePlacement";
    public int Priority => 200;
    public bool IsActive { get => _placer.IsPlacing; set { } }
    public string? CursorHint => _placer.IsPlacing ? "placement" : null;

    public StructurePlacementController(StructurePlacer placer)
    {
        _placer = placer;
        _shortcuts = new[]
        {
            new ShortcutDescriptor("place", "DrawPath", LocalizationKeys.ToolHintPlace),
            new ShortcutDescriptor("rotate", "RotateKey", LocalizationKeys.ToolHintRotate),
            new ShortcutDescriptor("cancel", "RightClick", LocalizationKeys.ToolHintCancel),
            new ShortcutDescriptor("exit_mode", "CancelKey", LocalizationKeys.ToolHintExitMode),
        };
    }

    public IReadOnlyList<ShortcutDescriptor> GetActiveShortcuts()
    {
        var cat = _placer.SelectedCategory;
        _shortcuts[0].IsActive = true;
        _shortcuts[1].IsActive = cat == "PathGate"
            || cat == "FilterSplitter" || cat == "Balancer" || cat == "CheckGate";
        _shortcuts[2].IsActive = true;
        _shortcuts[3].IsActive = true;
        return _shortcuts;
    }

    public bool HandleLeftClickDown(GridPosRPG gridPos)
    {
        if (!_placer.IsPlacing) return false;
        _placer.PlaceAtGrid(gridPos);
        return true;
    }

    public bool HandleLeftClickUp(GridPosRPG gridPos) => false;

    public bool HandleRightClick(GridPosRPG gridPos)
    {
        if (!_placer.IsPlacing) return false;
        _placer.CancelPlacement();
        return true;
    }

    public bool HandleRotate()
    {
        if (!_placer.IsPlacing) return false;
        _placer.RotateGhost();
        return true;
    }

    public bool HandleCancel()
    {
        if (!_placer.IsPlacing) return false;
        _placer.CancelPlacement();
        return true;
    }

    public void UpdateController(float deltaTime, GridPosRPG cursorGridPos)
    {
        if (_placer.IsPlacing)
        {
            _placer.UpdateGhostPosition(cursorGridPos);
        }
    }

    public void OnDeactivated() { }
}

/// <summary>
/// Input controller for path drawing.
/// Priority 150 — active when the player has selected the path tool from the toolbar.
/// Left-click-drag draws connected path segments; right-click cancels/exits.
/// Escape exits the active drag first, then exits path-draw mode entirely.
/// </summary>
internal sealed class PathDrawingController : IControllerWithShortcuts
{
    private readonly SimulationTicker _simulation;
    private readonly PathRendererSystem _pathRenderer;

    private bool _isDrawing;
    private GridPosRPG _lastPlacedPosition;

    // Ghost preview for next path segment
    private GhostInstance? _ghost;
    private GridPosRPG _lastGhostPos;

    private readonly ShortcutDescriptor[] _shortcuts;

    public string Name => "PathDrawing";
    public int Priority => 150;
    public bool IsActive { get; set; }
    public string? CursorHint => "path_draw";

    public PathDrawingController(
        SimulationTicker simulation,
        PathRendererSystem pathRenderer)
    {
        _simulation = simulation;
        _pathRenderer = pathRenderer;
        _shortcuts = new[]
        {
            new ShortcutDescriptor("draw", "DrawPath", LocalizationKeys.ToolHintDraw),
            new ShortcutDescriptor("cancel", "RightClick", LocalizationKeys.ToolHintCancel),
            new ShortcutDescriptor("exit_mode", "CancelKey", LocalizationKeys.ToolHintExitMode),
        };
    }

    public IReadOnlyList<ShortcutDescriptor> GetActiveShortcuts()
    {
        _shortcuts[0].IsActive = true;
        _shortcuts[1].IsActive = true;
        _shortcuts[2].IsActive = true;
        return _shortcuts;
    }

    public bool HandleLeftClickDown(GridPosRPG gridPos)
    {
        // Active tool always consumes LMB — prevents fall-through to EntitySelectionController.
        if (_simulation.TileManager.GetPathIdAt(gridPos).HasValue) { return true; }

        var result = _simulation.CommandBus.Dispatch(new DrawPathCommand(gridPos, Direction.East));
        if (!result.Success) { return true; }

        _isDrawing = true;
        _lastPlacedPosition = gridPos;
        // Visual is created by PathRendererSystem in response to PathBuiltEvent.
        DestroyGhost();
        return true;
    }

    public bool HandleLeftClickUp(GridPosRPG gridPos)
    {
        if (_isDrawing)
        {
            _isDrawing = false;
            return true;
        }
        return false;
    }

    public bool HandleRightClick(GridPosRPG gridPos)
    {
        // RMB consistently cancels: stop drag if active, then exit mode
        if (_isDrawing)
        {
            _isDrawing = false;
            return true;
        }
        // Exit path-draw mode entirely
        IsActive = false;
        DestroyGhost();
        return true;
    }

    public bool HandleRotate() => false;

    public bool HandleCancel()
    {
        if (_isDrawing)
        {
            _isDrawing = false;
            return true;
        }
        // Exit path-draw mode entirely
        IsActive = false;
        DestroyGhost();
        return true;
    }

    public void UpdateController(float deltaTime, GridPosRPG cursorGridPos)
    {
        // Path drawing ghost preview with validity coloring
        if (!_isDrawing)
        {
            UpdateGhostPreview(cursorGridPos);
            return;
        }

        if (cursorGridPos != _lastPlacedPosition &&
            !_simulation.TileManager.GetPathIdAt(cursorGridPos).HasValue)
        {
            var direction = InferDirection(_lastPlacedPosition, cursorGridPos);

            // Update the previous segment's facing through the manager so the renderer
            // receives an EntityRotatedEvent and visually rotates the tile.
            _simulation.PathNodeManager.SetPathSegmentFacing(_lastPlacedPosition, direction);

            var drawResult = _simulation.CommandBus.Dispatch(new DrawPathCommand(cursorGridPos, direction));
            if (!drawResult.Success)
            {
                _isDrawing = false;
                return;
            }

            _lastPlacedPosition = cursorGridPos;
            // Visual for the new segment is created by PathRendererSystem in response to PathBuiltEvent.
        }
    }

    private void UpdateGhostPreview(GridPosRPG cursorGridPos)
    {
        if (cursorGridPos == _lastGhostPos && _ghost != null) return;
        _lastGhostPos = cursorGridPos;

        bool isOccupied = _simulation.TileManager.GetPathIdAt(cursorGridPos).HasValue
            || _simulation.TileManager.GetStructureIdAt(cursorGridPos).HasValue;

        bool canAfford = _simulation.ItemManager.GetStock("gold") >= EconomyConfig.PathSegmentCost;
        bool isValid = !isOccupied && canAfford;

        if (_ghost == null)
        {
            var prefab = PrefabRegistry.GetPrefab("PathStraight");
            _ghost = prefab != null
                ? GhostPreviewFactory.CreateGhostFromPrefab(prefab)
                : GhostPreviewFactory.CreateGhostFromInstance(
                    RuntimePlaceholderFactory.CreatePathGhostPreview());
        }

        _ghost.SetPosition(new Vector3(cursorGridPos.X, 0.05f, cursorGridPos.Y));
        _ghost.SetValidity(isValid);
    }

    public void OnDeactivated()
    {
        _isDrawing = false;
        DestroyGhost();
    }

    private void DestroyGhost()
    {
        if (_ghost != null)
        {
            _ghost.Destroy();
            _ghost = null;
        }
    }

    private static Direction InferDirection(GridPosRPG from, GridPosRPG to)
    {
        int dx = to.X - from.X;
        int dy = to.Y - from.Y;
        if (Math.Abs(dx) >= Math.Abs(dy))
        {
            return dx >= 0 ? Direction.East : Direction.West;
        }
        return dy >= 0 ? Direction.North : Direction.South;
    }
}

/// <summary>
/// Input controller for entity selection / inspection.
/// Priority 50 — fires when no higher-priority tool is active.
/// Left-click raycasts into the scene and publishes <see cref="EntitySelectedEvent"/>.
/// When a rotatable entity (PathSegment or PathGate) is selected, pressing R
/// rotates it 90° clockwise in place via the appropriate Core manager.
/// </summary>
internal sealed class EntitySelectionController : IControllerWithShortcuts
{
    private readonly Camera _camera;
    private readonly EventBus _eventBus;
    private readonly SimulationTicker _simulation;

    // Selection state tracking
    private ulong _selectedEntityId;
    private string? _selectedEntityType;
    private GridPosRPG _selectedPosition;

    private readonly ShortcutDescriptor[] _shortcuts;

    public string Name => "EntitySelection";
    public int Priority => 50;
    public bool IsActive { get; set; } = true;
    public string? CursorHint => null;

    public EntitySelectionController(Camera camera, EventBus eventBus, SimulationTicker simulation)
    {
        _camera = camera;
        _eventBus = eventBus;
        _simulation = simulation;
        _shortcuts = new[]
        {
            new ShortcutDescriptor("select", "DrawPath", LocalizationKeys.ToolHintSelect),
            new ShortcutDescriptor("rotate_entity", "RotateKey", LocalizationKeys.ToolHintRotateEntity),
        };
    }

    public IReadOnlyList<ShortcutDescriptor> GetActiveShortcuts()
    {
        _shortcuts[0].IsActive = true;
        _shortcuts[1].IsActive = IsRotatableSelected();
        return _shortcuts;
    }

    public bool HandleLeftClickDown(GridPosRPG gridPos)
    {
        var ray = _camera.ScreenPointToRay((Vector3)Mouse.current.position.ReadValue());
        if (Physics.Raycast(ray, out var hit, 200f))
        {
            var go = hit.collider.gameObject;
            var entityMb = go.GetComponentInParent<IEntityIdentifier>();
            if (entityMb != null)
            {
                var pos = new GridPosRPG((int)go.transform.position.x, (int)go.transform.position.z);
                StoreSelection(entityMb.EntityId, entityMb.EntityType, pos);
                _eventBus.Publish(new EntitySelectedEvent(
                    new EntityId(entityMb.EntityId),
                    entityMb.EntityType,
                    pos));
                return true;
            }

            var hitPos = new GridPosRPG(
                Mathf.RoundToInt(hit.point.x),
                Mathf.RoundToInt(hit.point.z));

            // Check if we hit a path segment or path gate by position
            var pathSeg = _simulation.EntityManager.GetPathSegmentAt(hitPos);
            if (pathSeg != null)
            {
                StoreSelection(pathSeg.Id, "PathSegment", hitPos);
                _eventBus.Publish(new EntitySelectedEvent(pathSeg.Id, "PathSegment", hitPos));
                return true;
            }

            var pathGate = _simulation.EntityManager.GetPathGateAt(hitPos);
            if (pathGate != null)
            {
                StoreSelection(pathGate.Id, "PathGate", hitPos);
                _eventBus.Publish(new EntitySelectedEvent(pathGate.Id, "PathGate", hitPos));
                return true;
            }

            var routingNode = _simulation.EntityManager.GetRoutingNodeAt(hitPos);
            if (routingNode != null)
            {
                var category = routingNode.GetCategoryName();
                StoreSelection(routingNode.Id, category, hitPos);
                _eventBus.Publish(new EntitySelectedEvent(routingNode.Id, category, hitPos));
                return true;
            }

            // Query TileManager for authoritative terrain/biome type (single source of truth).
            // Never derive terrain type from visual object names or pixel colors.
            var biome = _simulation.TileManager.GetBiome(hitPos);
            var entityType = $"Terrain_{biome}";
            StoreSelection(0, entityType, hitPos);
            _eventBus.Publish(new EntitySelectedEvent(EntityId.None, entityType, hitPos));
            return true;
        }

        return false;
    }

    public bool HandleLeftClickUp(GridPosRPG gridPos) => false;

    public bool HandleRightClick(GridPosRPG gridPos) => false;

    public bool HandleRotate()
    {
        if (!IsRotatableSelected()) { return false; }

        var result = _simulation.CommandBus.Dispatch(new RotateEntityCommand(_selectedPosition));
        return result.Success;
    }

    public bool HandleCancel()
    {
        ClearSelection();
        _eventBus.Publish(new EntityDeselectedEvent());
        return false; // let Escape pass through to pause
    }

    public void UpdateController(float deltaTime, GridPosRPG cursorGridPos) { }

    public void OnDeactivated() { }

    private void StoreSelection(ulong entityId, string entityType, GridPosRPG position)
    {
        _selectedEntityId = entityId;
        _selectedEntityType = entityType;
        _selectedPosition = position;
    }

    private void ClearSelection()
    {
        _selectedEntityId = 0;
        _selectedEntityType = null;
        _selectedPosition = default;
    }

    private bool IsRotatableSelected()
    {
        return _selectedEntityType == "PathSegment"
            || _selectedEntityType == "PathGate"
            || _selectedEntityType == "FilterSplitter"
            || _selectedEntityType == "Balancer"
            || _selectedEntityType == "CheckGate";
    }
}

/// <summary>
/// Input controller for demolishing placed entities.
/// Priority 175 — between StructurePlacement (200) and PathDrawing (150).
/// Active when the player selects the Demolish tool from the hotbar.
/// Left-click removes the entity at the cursor position; right-click / Escape exits.
/// Entity detection order: PathGate → RoutingNode → Structure → PathSegment.
/// </summary>
internal sealed class DemolishController : IControllerWithShortcuts
{
    private readonly SimulationTicker _simulation;

    private bool _isActive;
    private GameObject? _ghostPreview;
    private GridPosRPG _lastGhostPos;

    private readonly ShortcutDescriptor[] _shortcuts;

    public string Name => "Demolish";
    public int Priority => 175;
    public bool IsActive { get => _isActive; set => _isActive = value; }
    public string? CursorHint => _isActive ? "demolish" : null;

    public DemolishController(SimulationTicker simulation)
    {
        _simulation = simulation;
        _shortcuts = new[]
        {
            new ShortcutDescriptor("demolish", "DrawPath", LocalizationKeys.ToolHintDemolish),
            new ShortcutDescriptor("cancel", "RightClick", LocalizationKeys.ToolHintCancel),
            new ShortcutDescriptor("exit_mode", "CancelKey", LocalizationKeys.ToolHintExitMode),
        };
    }

    public IReadOnlyList<ShortcutDescriptor> GetActiveShortcuts()
    {
        _shortcuts[0].IsActive = true;
        _shortcuts[1].IsActive = true;
        _shortcuts[2].IsActive = true;
        return _shortcuts;
    }

    public bool HandleLeftClickDown(GridPosRPG gridPos)
    {
        // Active tool always consumes LMB — prevents fall-through to EntitySelectionController.
        DemolishAt(gridPos);
        return true;
    }

    public bool HandleLeftClickUp(GridPosRPG gridPos) => false;

    public bool HandleRightClick(GridPosRPG gridPos)
    {
        _isActive = false;
        DestroyGhost();
        return true;
    }

    public bool HandleRotate() => false;

    public bool HandleCancel()
    {
        _isActive = false;
        DestroyGhost();
        return true;
    }

    public void UpdateController(float deltaTime, GridPosRPG cursorGridPos)
    {
        UpdateGhostPreview(cursorGridPos);
    }

    public void OnDeactivated()
    {
        DestroyGhost();
    }

    private void DemolishAt(GridPosRPG pos)
    {
        var result = _simulation.CommandBus.Dispatch(new DemolishCommand(pos));
        if (!result.Success)
        {
            Debug.Log($"[Input] Demolish failed at {pos} — {result.Reason}");
        }
    }

    private void UpdateGhostPreview(GridPosRPG cursorGridPos)
    {
        if (cursorGridPos == _lastGhostPos && _ghostPreview != null) { return; }
        _lastGhostPos = cursorGridPos;

        bool hasEntity = _simulation.EntityManager.GetPathGateAt(cursorGridPos) != null
            || _simulation.EntityManager.GetRoutingNodeAt(cursorGridPos) != null
            || _simulation.EntityManager.GetStructureAt(cursorGridPos) != null
            || _simulation.EntityManager.GetPathSegmentAt(cursorGridPos) != null;

        if (_ghostPreview == null)
        {
            _ghostPreview = CreateDemolishGhost();
        }

        _ghostPreview.transform.position = new Vector3(cursorGridPos.X, 0.05f, cursorGridPos.Y);

        // Red when entity present (will demolish), dim when empty (nothing to demolish)
        var renderer = _ghostPreview.GetComponentInChildren<MeshRenderer>();
        if (renderer != null)
        {
            var color = hasEntity
                ? new Color(0.9f, 0.15f, 0.15f, 0.45f)
                : new Color(0.5f, 0.5f, 0.5f, 0.2f);
            renderer.material.color = color;
        }
    }

    private void DestroyGhost()
    {
        if (_ghostPreview != null)
        {
            UnityEngine.Object.Destroy(_ghostPreview);
            _ghostPreview = null;
        }
    }

    private static GameObject CreateDemolishGhost()
    {
        var root = new GameObject("DemolishGhost");

        var tile = GameObject.CreatePrimitive(PrimitiveType.Cube);
        tile.name = "Tile";
        tile.transform.SetParent(root.transform);
        tile.transform.localPosition = Vector3.zero;
        tile.transform.localScale = new Vector3(0.9f, 0.08f, 0.9f);

        var collider = tile.GetComponent<Collider>();
        if (collider != null)
        {
            UnityEngine.Object.Destroy(collider);
        }

        var renderer = tile.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            renderer.material = RuntimePlaceholderFactory.CreateLitMaterial(
                new Color(0.9f, 0.15f, 0.15f, 0.45f));
        }

        // X-mark cross strips
        var stripColor = new Color(1f, 0.2f, 0.2f, 0.6f);
        AddCrossStrip(root, stripColor, 45f);
        AddCrossStrip(root, stripColor, -45f);

        return root;
    }

    private static void AddCrossStrip(GameObject parent, Color color, float angle)
    {
        var strip = GameObject.CreatePrimitive(PrimitiveType.Cube);
        strip.name = "CrossStrip";
        strip.transform.SetParent(parent.transform);
        strip.transform.localPosition = new Vector3(0, 0.06f, 0);
        strip.transform.localScale = new Vector3(0.7f, 0.04f, 0.08f);
        strip.transform.localRotation = Quaternion.Euler(0, angle, 0);

        var collider = strip.GetComponent<Collider>();
        if (collider != null)
        {
            UnityEngine.Object.Destroy(collider);
        }

        var renderer = strip.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            renderer.material = RuntimePlaceholderFactory.CreateLitMaterial(color);
        }
    }
}

/// <summary>
/// Input controller for camera rotation via the R key.
/// Priority 10 — lowest in the chain. Only fires when no other controller
/// consumes the Rotate action (e.g. ghost rotation during placement).
/// </summary>
internal sealed class CameraRotateController : IControllerWithShortcuts
{
    private readonly CameraController _cameraController;
    private readonly Func<bool> _isToolModeActive;

    private readonly ShortcutDescriptor[] _shortcuts;

    public string Name => "CameraRotate";
    public int Priority => 10;
    public bool IsActive { get; set; } = true;
    public string? CursorHint => null;

    public CameraRotateController(CameraController cameraController, Func<bool> isToolModeActive)
    {
        _cameraController = cameraController;
        _isToolModeActive = isToolModeActive;
        _shortcuts = new[]
        {
            new ShortcutDescriptor("rotate_camera", "RotateKey", LocalizationKeys.ToolHintRotateCamera),
        };
    }

    public IReadOnlyList<ShortcutDescriptor> GetActiveShortcuts()
    {
        _shortcuts[0].IsActive = !_isToolModeActive();
        return _shortcuts;
    }

    public bool HandleLeftClickDown(GridPosRPG gridPos) => false;
    public bool HandleLeftClickUp(GridPosRPG gridPos) => false;
    public bool HandleRightClick(GridPosRPG gridPos) => false;

    public bool HandleRotate()
    {
        if (_isToolModeActive()) { return false; }
        _cameraController.RotateCamera90();
        return true;
    }

    public bool HandleCancel() => false;

    public void UpdateController(float deltaTime, GridPosRPG cursorGridPos) { }

    public void OnDeactivated() { }
}

}
