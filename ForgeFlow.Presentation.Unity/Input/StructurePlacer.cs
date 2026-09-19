using ForgeFlow.Core;
using ForgeFlow.Core.Commands;
using ForgeFlow.Core.Data;
using ForgeFlow.Core.Entities;
using ForgeFlow.Core.Proto;
using EntityId = ForgeFlow.Core.Entities.EntityId;
using ForgeFlow.Core.Proto.Logic;
using ForgeFlow.Core.Systems;
using ForgeFlow.Presentation.Unity.Visuals;
using UnityEngine;

namespace ForgeFlow.Presentation.Unity.Input
{

/// <summary>
/// Handles structure placement via the GameplayToolbar.
/// Dispatches placement commands via <see cref="CommandBus"/> (gold enforcement
/// is handled by the command handlers in Core managers).
/// All input routing flows through <see cref="InputControllerStack"/> and
/// <see cref="StructurePlacementController"/> — this class exposes the
/// placement API but does not subscribe to any input actions directly.
/// </summary>
internal class StructurePlacer : MonoBehaviour
{
    [Header("Grid")]
    [SerializeField] private float _gridCellSize = 1.0f;

    private SimulationTicker _simulation = null!;
    private GameBootstrapper _bootstrapper = null!;
    private PathRendererSystem _pathRenderer = null!;
    private ProtoFactory? _protoFactory;

    private bool _isPlacing;
    private string _selectedCategory = string.Empty;
    private GhostInstance? _ghost;
    private Direction _currentFacing = Direction.North;

    public bool IsPlacing => _isPlacing;
    public string SelectedCategory => _selectedCategory;

    public void Initialize(SimulationTicker simulation, GameBootstrapper bootstrapper, PathRendererSystem pathRenderer)
    {
        _simulation = simulation;
        _bootstrapper = bootstrapper;
        _pathRenderer = pathRenderer;
        _protoFactory = bootstrapper.Services.Get<ProtoFactory>();
    }

    /// <summary>Enters placement mode from the toolbar.</summary>
    public void EnterPlacementModeFromToolbar(string category)
    {
        EnterPlacementMode(category);
    }

    /// <summary>
    /// Places the currently selected structure at the given grid position.
    /// Called by <see cref="StructurePlacementController"/>.
    /// </summary>
    public void PlaceAtGrid(GridPosRPG gridPos)
    {
        if (!_isPlacing) return;

        if (_simulation.TileManager.GetStructureIdAt(gridPos).HasValue)
        {
            // Routing nodes can replace existing path segments — skip the block check
            if (!IsRoutingNodeCategory(_selectedCategory) || _simulation.EntityManager.GetPathSegmentAt(gridPos) == null)
            {
                Debug.Log($"[Input] Position {gridPos} already occupied by a structure");
                return;
            }
        }

        if (_simulation.ItemManager.GetStock("gold") < EconomyConfig.GetStructureCost(_selectedCategory))
        {
            Debug.Log($"[Input] Not enough gold for {_selectedCategory} (cost: {EconomyConfig.GetStructureCost(_selectedCategory)})");
            return;
        }

        // Routing nodes (FilterSplitter, Balancer, CheckGate) can replace existing path segments
        if (IsRoutingNodeCategory(_selectedCategory))
        {
            var existingPath = _simulation.EntityManager.GetPathSegmentAt(gridPos);
            if (existingPath != null)
            {
                // RemovePathSegment publishes PathRemovedEvent; PathRendererSystem handles visual cleanup.
                _simulation.PathNodeManager.RemovePathSegment(gridPos);
                Debug.Log($"[Input] Removed path segment at {gridPos} to place {_selectedCategory}");
            }
        }

        if (_selectedCategory == "PathGate")
        {
            if (TryPlacePathGate(gridPos))
            {
                Debug.Log($"[Input] Placed PathGate at {gridPos} facing {_currentFacing}");
            }
            else
            {
                Debug.Log($"[Input] Failed to place PathGate at {gridPos}");
            }
            CancelPlacement();
            return;
        }

        if (TryPlaceProtoStructure(_selectedCategory, gridPos))
        {
            // Visual is created by PathRendererSystem in response to StructurePlacedEvent /
            // RoutingNodePlacedEvent published by StructureManager.AddProtoStructure.
            Debug.Log($"[Input] Placed {_selectedCategory} at {gridPos}");
            CancelPlacement();
            return;
        }

        Structure? structure = _selectedCategory switch
        {
            "Forge" => CreateForge(),
            "DungeonPortal" => CreateDungeonPortal(),
            "FusionAltar" => CreateFusionAltar(),
            "AppearanceWorkshop" => CreateAppearanceWorkshopStructure(),
            _ => null
        };

        if (structure != null)
        {
            var result = _simulation.CommandBus.Dispatch(new PlaceStructureCommand(
                structure, gridPos, _selectedCategory));
            if (!result.Success)
            {
                Debug.Log($"[Input] Failed to place {_selectedCategory} — {result.Reason}");
                CancelPlacement();
                return;
            }
            _simulation.PathNodeManager.AutoConnectToAdjacentPaths(structure);

            // Visual is created by PathRendererSystem in response to StructurePlacedEvent.
            Debug.Log($"[Input] Placed {_selectedCategory} at {gridPos}");
        }

        CancelPlacement();
    }

    /// <summary>
    /// Rotates the ghost preview when the Rotate key is pressed during placement.
    /// Called by <see cref="StructurePlacementController"/>.
    /// </summary>
    public void RotateGhost()
    {
        if (!_isPlacing) return;
        if (_selectedCategory != "PathGate" && !IsRoutingNodeCategory(_selectedCategory)) return;

        _currentFacing = _currentFacing.RotateClockwise();
        Debug.Log($"[Input] {_selectedCategory} facing rotated to {_currentFacing}");

        if (_ghost != null)
        {
            float yaw = RuntimePlaceholderFactory.DirectionToYaw(_currentFacing);
            _ghost.SetRotation(Quaternion.Euler(0, yaw, 0));
        }
    }

    /// <summary>
    /// Updates the ghost preview position to follow the cursor.
    /// Called every frame by <see cref="StructurePlacementController.UpdateController"/>.
    /// Applies validity coloring (green = can place, red = blocked/unaffordable).
    /// </summary>
    public void UpdateGhostPosition(GridPosRPG gridPos)
    {
        if (_ghost == null) return;

        _ghost.SetPosition(new Vector3(
            gridPos.X * _gridCellSize,
            IsRoutingNodeCategory(_selectedCategory) ? 0.05f : 0.5f,
            gridPos.Y * _gridCellSize));

        if (_selectedCategory == "PathGate" || IsRoutingNodeCategory(_selectedCategory))
        {
            float yaw = RuntimePlaceholderFactory.DirectionToYaw(_currentFacing);
            _ghost.SetRotation(Quaternion.Euler(0, yaw, 0));
        }

        // Validity coloring — routing nodes are allowed on path tiles
        bool isOccupied = _simulation.TileManager.GetStructureIdAt(gridPos).HasValue;
        if (IsRoutingNodeCategory(_selectedCategory))
        {
            // Routing nodes can be placed on path tiles (they replace the path)
            isOccupied = isOccupied && _simulation.EntityManager.GetPathSegmentAt(gridPos) == null;
        }
        bool canAfford = _simulation.ItemManager.GetStock("gold") >= EconomyConfig.GetStructureCost(_selectedCategory);
        bool isValid = !isOccupied && canAfford;

        _ghost.SetValidity(isValid);
    }

    public void CancelPlacement()
    {
        _isPlacing = false;
        if (_ghost != null)
        {
            _ghost.Destroy();
            _ghost = null;
        }
    }

    private static bool IsRoutingNodeCategory(string category) =>
        category == "FilterSplitter" || category == "Balancer" || category == "CheckGate";

    private void EnterPlacementMode(string category)
    {
        CancelPlacement();
        _isPlacing = true;
        _selectedCategory = category;
        _currentFacing = Direction.North;

        // Map routing node categories to their actual prefab keys
        var prefabKey = category == "FilterSplitter" ? "PathT" : category;
        var prefab = PrefabRegistry.GetPrefab(prefabKey);
        if (prefab != null)
        {
            _ghost = GhostPreviewFactory.CreateGhostFromPrefab(prefab);
        }
        else
        {
            _ghost = GhostPreviewFactory.CreateGhostFromInstance(
                RuntimePlaceholderFactory.CreateStructurePreviewPlaceholder(category));
        }

        int cost = EconomyConfig.GetStructureCost(category);
        Debug.Log($"[Input] Placement mode: {category} (cost: {cost}g, left-click to place, right-click/Esc to cancel)");
    }

    private bool TryPlacePathGate(GridPosRPG gridPos)
    {
        if (_simulation.TileManager.GetPathGateIdAt(gridPos).HasValue)
        {
            Debug.Log($"[Input] Position {gridPos} already has a PathGate");
            return false;
        }

        var result = _simulation.CommandBus.Dispatch(new PlacePathGateCommand(gridPos, _currentFacing));
        if (!result.Success) return false;

        var gate = _simulation.EntityManager.GetPathGateAt(gridPos);
        if (gate == null) return false;

        var go = new GameObject($"PathGate_{gate.Id}");
        var mb = go.AddComponent<PathGateMb>();
        mb.Initialize(gate);

        return true;
    }

    private bool TryPlaceProtoStructure(string category, GridPosRPG gridPos)
    {
        if (_protoFactory == null) return false;

        StructureBase? protoEntity = category switch
        {
            "Spawner" => _protoFactory.CreateVillageSpawner("spawner_basic"),
            "Forestry" => _protoFactory.CreateGatheringRecipeEntity("forestry_basic"),
            "MiningNode" => _protoFactory.CreateGatheringRecipeEntity("mining_basic"),
            "Inn" => _protoFactory.CreateInn("inn_basic"),
            "CraftStation" => _protoFactory.CreateCraftStation("craft_station_basic"),
            "Stockpile" => _protoFactory.CreateStockpile("stockpile_basic"),
            "TrainingBuilding" => _protoFactory.CreateTrainingBuilding("training_basic"),
            "FilterSplitter" => _protoFactory.CreateFilterSplitter("filter_splitter_basic"),
            _ => null
        };

        if (protoEntity == null) return false;

        // Apply placement facing to routing nodes
        if (protoEntity is RoutingNodeBase routingNode)
        {
            routingNode.OutputDirection = _currentFacing;

            // For filter splitters, compute proper T-junction arm directions
            // relative to the facing (right arm = default, left arm = filtered)
            if (protoEntity is FilterSplitterLogic splitter)
            {
                splitter.DefaultDirection = _currentFacing.RotateClockwise();
                splitter.FilteredOutputDirection = _currentFacing.RotateCounterClockwise();
            }
        }

        var result = _simulation.CommandBus.Dispatch(new PlaceStructureCommand(
            protoEntity, gridPos, category));
        if (!result.Success)
        {
            Debug.Log($"[Input] Failed to place {category} — {result.Reason}");
            return false;
        }

        // Connect adjacent input path segments to routing nodes so villagers
        // are redirected by the routing evaluation in PathTrafficSystem.
        if (protoEntity is RoutingNodeBase placedRoutingNode)
        {
            _simulation.PathNodeManager.ConnectInputPathsToRoutingNode(placedRoutingNode);
        }

        return true;
    }

    private ForgeLogic CreateForge()
    {
        var forge = new ForgeLogic(EntityId.Next());
        forge.ActiveRecipeId = "iron_sword";
        forge.ProcessingDuration = 2.0f;

        var inputItem = _bootstrapper.Services.Get<ItemManager>().CreateItem("iron_ore", 0);
        inputItem.Slot = EquipSlot.Weapon;
        forge.TryEnqueueInput(inputItem);

        return forge;
    }

    private DungeonPortalLogic CreateDungeonPortal()
    {
        var portal = new DungeonPortalLogic(EntityId.Next());
        portal.Initialize(_bootstrapper.Services.Get<DungeonRegistry>());
        portal.DungeonId = "goblin_caves";
        return portal;
    }

    private FusionAltarLogic CreateFusionAltar()
    {
        return new FusionAltarLogic(EntityId.Next());
    }

    private AppearanceWorkshopLogic CreateAppearanceWorkshopStructure()
    {
        return new AppearanceWorkshopLogic(EntityId.Next());
    }
}
}
