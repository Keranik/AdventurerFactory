using ForgeFlow.Core;
using ForgeFlow.Core.Entities;
using ForgeFlow.Core.Events;
using ForgeFlow.Core.Localization;
using ForgeFlow.Core.Proto;
using ForgeFlow.Core.Proto.Logic;
using ForgeFlow.Core.Systems;
using ForgeFlow.Presentation.Unity.UI;
using ForgeFlow.Presentation.Unity.UI.Components;
using UnityEngine;

namespace ForgeFlow.Presentation.Unity.Visuals
{

/// <summary>
/// Manages terrain tile visuals. Creates TerrainCellMb for each cell
/// from the Core TerrainGrid. Runtime-created, no prefabs.
/// </summary>
internal class TerrainVisualManager : MonoBehaviour
{
    private TerrainGrid? _terrain;
    private ProtoRegistry? _protoRegistry;
    private readonly Dictionary<GridPosRPG, TerrainCellMb> _cellVisuals = new();
    private GameObject? _terrainRoot;

    public void Initialize(TerrainGrid terrain, ProtoRegistry protoRegistry)
    {
        _terrain = terrain;
        _protoRegistry = protoRegistry;
        CreateTerrainVisuals();
    }

    /// <summary>
    /// Refreshes the visual of a single cell to match the current Core biome data.
    /// Called after runtime biome changes (e.g. debug terrain cycling).
    /// </summary>
    public void RefreshCell(GridPosRPG pos)
    {
        if (_terrain == null || _protoRegistry == null) { return; }
        if (!_cellVisuals.TryGetValue(pos, out var mb)) { return; }

        var cell = _terrain.Get(pos);
        if (cell == null) { return; }

        var biomeId = $"biome_{cell.Biome.ToString().ToLowerInvariant()}";
        var biomeProto = _protoRegistry.GetBiome(biomeId);
        mb.RefreshVisual(biomeProto);
    }

    private void CreateTerrainVisuals()
    {
        if (_terrain == null) return;

        // Destroy previous terrain visuals to prevent stale quads
        if (_terrainRoot != null)
        {
            Destroy(_terrainRoot);
            _cellVisuals.Clear();
        }

        _terrainRoot = new GameObject("TerrainRoot");
        _terrainRoot.transform.SetParent(transform);

        foreach (var cell in _terrain.GetAllCells())
        {
            var go = new GameObject($"Cell_{cell.Position.X}_{cell.Position.Y}");
            go.transform.SetParent(_terrainRoot.transform);

            var mb = go.AddComponent<TerrainCellMb>();
            var biomeId = $"biome_{cell.Biome.ToString().ToLowerInvariant()}";
            var biomeProto = _protoRegistry?.GetBiome(biomeId);
            mb.Initialize(cell, biomeProto);
            _cellVisuals[cell.Position] = mb;
        }

        Debug.Log($"[ForgeFlow] Terrain created: {_cellVisuals.Count} cells");
    }
}

/// <summary>
/// Manages Proto-based structure visuals. Creates appropriate Mb wrappers
/// for gathering structures, village spawners, training buildings, etc.
/// Listens to events. Integrates UI Toolkit inspector panels.
/// </summary>
internal class ProtoStructureRenderer : MonoBehaviour
{
    private SimulationTicker? _simulation;
    private GameBootstrapper? _bootstrapper;
    private EventBus? _eventBus;
    private readonly Dictionary<ulong, MonoBehaviour> _structureVisuals = new();
    private readonly Dictionary<ulong, ResourceNodeMb> _nodeVisuals = new();
    private readonly Dictionary<ulong, VillagerMb> _villagerVisuals = new();
    private readonly Dictionary<ulong, PathSegmentMb> _pathVisuals = new();
    private readonly List<(ulong id, GridPosRPG pos)> _pendingVillagerVisuals = new();
    private GameObject? _structureRoot;
    private GameObject? _nodeRoot;
    private GameObject? _villagerRoot;
    private GameObject? _pathRoot;

    // UI Toolkit panels
    private ResourcePanel? _resourcePanel;
    private TutorialOverlayPanel? _tutorialOverlay;
    private DungeonLogPanel? _dungeonLogPanel;

    public ResourcePanel? ResourcePanel => _resourcePanel;
    public TutorialOverlayPanel? TutorialOverlay => _tutorialOverlay;
    public DungeonLogPanel? DungeonLogPanel => _dungeonLogPanel;

    public void Initialize(SimulationTicker simulation, GameBootstrapper bootstrapper, EventBus eventBus)
    {
        _simulation = simulation;
        _bootstrapper = bootstrapper;
        _eventBus = eventBus;
        CreateRoots();
        CreateUIPanels();
        SubscribeEvents();
    }

    private void CreateRoots()
    {
        _structureRoot = new GameObject("StructuresRoot");
        _structureRoot.transform.SetParent(transform);

        _nodeRoot = new GameObject("ResourceNodesRoot");
        _nodeRoot.transform.SetParent(transform);

        _villagerRoot = new GameObject("VillagersRoot");
        _villagerRoot.transform.SetParent(transform);

        _pathRoot = new GameObject("PathsRoot");
        _pathRoot.transform.SetParent(transform);
    }

    private void CreateUIPanels()
    {
        _resourcePanel = new ResourcePanel(_simulation, _eventBus);
        _tutorialOverlay = new TutorialOverlayPanel(_simulation, _eventBus);
        _dungeonLogPanel = new DungeonLogPanel(_simulation);
    }

    private void SubscribeEvents()
    {
        if (_eventBus == null) return;
        _eventBus.Subscribe<VillagerSpawnedEvent>(OnVillagerSpawned);
        _eventBus.Subscribe<PathBuiltEvent>(OnPathBuilt);
        _eventBus.Subscribe<GatheringCompleteEvent>(OnGatheringComplete);
        _eventBus.Subscribe<DungeonCompletedEvent>(OnDungeonCompleted);
        _eventBus.Subscribe<GatingBlockedEvent>(OnGatingBlocked);
        _eventBus.Subscribe<VillagerTrainingStartedEvent>(OnTrainingStarted);
        _eventBus.Subscribe<VillagerTrainingCompleteEvent>(OnTrainingComplete);
        _eventBus.Subscribe<DungeonEncounterStepEvent>(OnDungeonStep);
        // Hide/show the villager visual when they enter or leave a building.
        // Per §3.5 we subscribe centrally rather than per-Mb (one handler call
        // per event vs N), and per the user's spec the building's inspector UI
        // is the sole "are villagers inside?" indicator — no floating badges.
        _eventBus.Subscribe<VillagerEnteredBuildingEvent>(OnVillagerEnteredBuilding);
        _eventBus.Subscribe<VillagerEnteredDungeonEvent>(OnVillagerEnteredDungeon);
        _eventBus.Subscribe<VillagerLeftBuildingEvent>(OnVillagerLeftBuilding);
        // Destroy the VillagerMb when Core evicts a villager (e.g. from a removed/rotated
        // path segment). Without this, the visual lingers as a ghost while the spawner
        // respawns a fresh villager from the residence — producing duplicate visuals
        // (§3.5 Central Manager + Events: presentation reacts via subscription).
        _eventBus.Subscribe<VillagerReturnedToPoolEvent>(OnVillagerReturnedToPool);
    }

    private void OnDestroy()
    {
        if (_eventBus == null) return;
        _eventBus.Unsubscribe<VillagerSpawnedEvent>(OnVillagerSpawned);
        _eventBus.Unsubscribe<PathBuiltEvent>(OnPathBuilt);
        _eventBus.Unsubscribe<GatheringCompleteEvent>(OnGatheringComplete);
        _eventBus.Unsubscribe<DungeonCompletedEvent>(OnDungeonCompleted);
        _eventBus.Unsubscribe<GatingBlockedEvent>(OnGatingBlocked);
        _eventBus.Unsubscribe<VillagerTrainingStartedEvent>(OnTrainingStarted);
        _eventBus.Unsubscribe<VillagerTrainingCompleteEvent>(OnTrainingComplete);
        _eventBus.Unsubscribe<DungeonEncounterStepEvent>(OnDungeonStep);
        _eventBus.Unsubscribe<VillagerEnteredBuildingEvent>(OnVillagerEnteredBuilding);
        _eventBus.Unsubscribe<VillagerEnteredDungeonEvent>(OnVillagerEnteredDungeon);
        _eventBus.Unsubscribe<VillagerLeftBuildingEvent>(OnVillagerLeftBuilding);
        _eventBus.Unsubscribe<VillagerReturnedToPoolEvent>(OnVillagerReturnedToPool);
    }

    private void Update()
    {
        // Create visuals for villagers that were pending (event fired before VillagerSystem registration)
        if (_pendingVillagerVisuals.Count > 0 && _simulation != null)
        {
            for (int i = _pendingVillagerVisuals.Count - 1; i >= 0; i--)
            {
                var (id, pos) = _pendingVillagerVisuals[i];
                if (_simulation.VillagerSystem.VillagerIndex.TryGetValue(id, out var villager))
                {
                    SpawnVillagerVisual(villager);
                    _pendingVillagerVisuals.RemoveAt(i);
                }
            }
        }
    }

    private void OnVillagerSpawned(VillagerSpawnedEvent e)
    {
        if (_simulation == null) return;
        if (_simulation.VillagerSystem.VillagerIndex.TryGetValue(e.VillagerId, out var villager))
        {
            SpawnVillagerVisual(villager);
        }
        else
        {
            // Villager not yet in VillagerIndex (event fires before SimulationTicker registers it).
            // Defer visual creation to next Update.
            _pendingVillagerVisuals.Add((e.VillagerId, e.SpawnPosition));
        }
    }

    private void OnPathBuilt(PathBuiltEvent e)
    {
        // Path rendering is handled by PathRendererSystem.
        // PathSegmentMb was creating duplicate visuals at y=0 that covered terrain biome tiles.
    }

    private void OnGatheringComplete(GatheringCompleteEvent e)
    {
        Debug.Log($"[ForgeFlow] Gathered {e.Amount}x {e.ResourceId} at ({e.StructurePosition.X}, {e.StructurePosition.Y})");
    }

    private void OnDungeonCompleted(DungeonCompletedEvent e)
    {
        _dungeonLogPanel?.Refresh();
    }

    private void OnGatingBlocked(GatingBlockedEvent e)
    {
        Debug.LogWarning($"[ForgeFlow] GATING: Cannot place {e.BlockedAction} — " +
            $"{e.CurrentCount}/{e.MaxAllowed} at tier {e.RequiredTier}. " +
            ForgeStyledVisualElement.GetLocalizedText(LocalizationKeys.GatingBlocked));
    }

    private void OnTrainingStarted(VillagerTrainingStartedEvent e)
    {
        Debug.Log($"[ForgeFlow] Villager {e.VillagerId} started training at {e.BuildingId} → {e.TargetClass}");
    }

    private void OnTrainingComplete(VillagerTrainingCompleteEvent e)
    {
        Debug.Log($"[ForgeFlow] Villager {e.VillagerId} completed training → {e.TrainedClass}");
    }

    private void OnDungeonStep(DungeonEncounterStepEvent e)
    {
        string result = e.Survived ? "SURVIVED" : "DEFEATED";
        Debug.Log($"[ForgeFlow] Dungeon {e.DungeonId} Room {e.RoomIndex + 1} [{e.EncounterType}]: {result} " +
            $"(DMG dealt: {e.DamageDealt}, taken: {e.DamageTaken})" +
            (e.LootDropId != null ? $" Loot: {e.LootDropId}" : ""));
    }

    // ── Villager visibility (hide on building/dungeon entry, show on exit) ──
    // The user's spec: villagers should "disappear into" the building when inside.
    // The building's existing inspector UI is the sole indicator of occupancy —
    // do NOT add floating population badges (explicit out-of-scope per Bible §1.6).

    private void OnVillagerEnteredBuilding(VillagerEnteredBuildingEvent e)
    {
        SetVillagerVisible(e.VillagerId.Value, false);
    }

    private void OnVillagerEnteredDungeon(VillagerEnteredDungeonEvent e)
    {
        SetVillagerVisible(e.VillagerId.Value, false);
    }

    private void OnVillagerLeftBuilding(VillagerLeftBuildingEvent e)
    {
        SetVillagerVisible(e.VillagerId.Value, true);
    }

    private void SetVillagerVisible(ulong villagerId, bool visible)
    {
        if (_villagerVisuals.TryGetValue(villagerId, out var mb) && mb != null)
        {
            mb.gameObject.SetActive(visible);
        }
    }

    /// <summary>
    /// Destroys the VillagerMb when Core retires the villager (path rotated/removed,
    /// dungeon casualty, etc.). Required so a subsequent respawn from the same
    /// VillageSpawner does not leave a stale visual parked on the old path tile.
    /// </summary>
    private void OnVillagerReturnedToPool(VillagerReturnedToPoolEvent e)
    {
        ulong id = e.VillagerId.Value;
        if (_villagerVisuals.TryGetValue(id, out var mb) && mb != null)
        {
            UnityEngine.Object.Destroy(mb.gameObject);
        }
        _villagerVisuals.Remove(id);
        // Drop any pending visual creation request for this id so a stale
        // SpawnVillagerVisual call cannot resurrect the destroyed Mb.
        for (int i = _pendingVillagerVisuals.Count - 1; i >= 0; i--)
        {
            if (_pendingVillagerVisuals[i].id == id)
            {
                _pendingVillagerVisuals.RemoveAt(i);
            }
        }
    }

    /// <summary>Creates a visual for a gathering structure logic instance.</summary>
    public void SpawnGatheringStructureVisual(GatheringLogicBase logic)
    {
        var go = new GameObject($"GatheringRecipeEntity_{logic.Id}");
        go.transform.SetParent(_structureRoot?.transform);
        go.transform.position = new Vector3(logic.Position.X, 0f, logic.Position.Y);

        if (logic is ForestryRecipeEntity forestry)
        {
            var mb = go.AddComponent<ForestryMb>();
            mb.Initialize(forestry);
            _structureVisuals[logic.Id] = mb;
        }
        else if (logic is MiningRecipeEntity mining)
        {
            var mb = go.AddComponent<MiningMb>();
            mb.Initialize(mining);
            _structureVisuals[logic.Id] = mb;
        }
        else
        {
            var mb = go.AddComponent<GatheringLogicEntityBaseMb>();
            mb.Initialize(logic);
            _structureVisuals[logic.Id] = mb;
        }
    }

    /// <summary>Creates a visual for a training building (school).</summary>
    public void SpawnTrainingBuildingVisual(TrainingBuildingLogic logic)
    {
        var go = new GameObject($"School_{logic.Id}");
        go.transform.SetParent(_structureRoot?.transform);
        go.transform.position = new Vector3(logic.Position.X, 0f, logic.Position.Y);

        var mb = go.AddComponent<TrainingBuildingMb>();
        mb.Initialize(logic);
        _structureVisuals[logic.Id] = mb;
    }

    /// <summary>Creates a visual for a resource node logic instance.</summary>
    public void SpawnResourceNodeVisual(ResourceNodeLogic logic)
    {
        var go = new GameObject($"ResourceNode_{logic.Id}");
        go.transform.SetParent(_nodeRoot?.transform);

        var mb = go.AddComponent<ResourceNodeMb>();
        mb.Initialize(logic);
        _nodeVisuals[logic.Id] = mb;
    }

    /// <summary>Creates a visual for a villager entity.</summary>
    public void SpawnVillagerVisual(VillagerLogic logic)
    {
        if (_villagerVisuals.ContainsKey(logic.Id)) return;

        var go = new GameObject($"Villager_{logic.Id}");
        go.transform.SetParent(_villagerRoot?.transform);

        var mb = go.AddComponent<VillagerMb>();
        mb.Initialize(logic);
        _villagerVisuals[logic.Id] = mb;
    }

    /// <summary>Creates a visual for a path segment.</summary>
    public void SpawnPathVisual(PathSegmentLogic logic)
    {
        if (_pathVisuals.ContainsKey(logic.Id)) return;

        var go = new GameObject($"Path_{logic.Id}");
        go.transform.SetParent(_pathRoot?.transform);

        var mb = go.AddComponent<PathSegmentMb>();
        mb.Initialize(logic);
        _pathVisuals[logic.Id] = mb;
    }

    /// <summary>Creates a visual for a village spawner.</summary>
    public void SpawnVillageSpawnerVisual(VillageSpawnerLogic logic)
    {
        var go = new GameObject($"VillageSpawner_{logic.Id}");
        go.transform.SetParent(_structureRoot?.transform);

        var mb = go.AddComponent<VillageSpawnerMb>();
        mb.Initialize(logic);
        _structureVisuals[logic.Id] = mb;
    }
}

/// <summary>
/// Tutorial HUD panel. Shows active mission hint and progress.
/// Now backed by UI Toolkit TutorialOverlayPanel.
/// </summary>
internal class TutorialPanel : MonoBehaviour
{
    private SimulationTicker? _simulation;
    private EventBus? _eventBus;

    public void Initialize(SimulationTicker simulation, EventBus eventBus)
    {
        _simulation = simulation;
        _eventBus = eventBus;
        _eventBus.Subscribe<TutorialCompletedEvent>(OnTutorialCompleted);
    }

    private void OnDestroy()
    {
        _eventBus?.Unsubscribe<TutorialCompletedEvent>(OnTutorialCompleted);
    }

    private void OnTutorialCompleted(TutorialCompletedEvent e)
    {
        Debug.Log($"[ForgeFlow] Tutorial '{e.MissionId}' completed! (Mission #{e.Order})");
    }

    public string GetActiveHint()
    {
        var active = _simulation?.TutorialSystem.ActiveMission;
        return active?.HintText ?? ForgeStyledVisualElement.GetLocalizedText(LocalizationKeys.TutorialComplete);
    }

    public float GetProgress()
    {
        if (_simulation == null) return 1f;
        var ts = _simulation.TutorialSystem;
        return ts.TotalCount > 0 ? (float)ts.CompletedCount / ts.TotalCount : 1f;
    }
}
}
