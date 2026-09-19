using ForgeFlow.Core.Entities;
using ForgeFlow.Core.Events;
using ForgeFlow.Core.Proto;
using ForgeFlow.Core.Proto.Logic;
using ForgeFlow.Core.Systems;
using UnityEngine;
using EntityId = ForgeFlow.Core.Entities.EntityId;

namespace ForgeFlow.Presentation.Unity.UI.Inspectors
{
    /// <summary>
    /// Single coordinator for all entity inspectors. Subscribes to <see cref="EntitySelectedEvent"/>
    /// and <see cref="EntityDeselectedEvent"/>, determines the entity type, and opens the
    /// correct inspector panel via <see cref="UIManager"/>. Ensures mutual exclusion — only
    /// one inspector is visible at a time (unless pinned).
    /// Replaces the dual-subscription pattern from WorkerInspectorWindow + FactoryEntryPoint.
    /// </summary>
    internal sealed class InspectorCoordinator : System.IDisposable
    {
        private readonly SimulationTicker _simulation;
        private readonly EventBus _eventBus;
        private readonly UIManager _uiManager;

        /// <summary>Tracks which inspector window ID is currently open (for mutual exclusion).</summary>
        private string? _activeInspectorId;

        public InspectorCoordinator(SimulationTicker simulation, EventBus eventBus, UIManager uiManager)
        {
            _simulation = simulation;
            _eventBus = eventBus;
            _uiManager = uiManager;

            _eventBus.Subscribe<EntitySelectedEvent>(OnEntitySelected);
            _eventBus.Subscribe<EntityDeselectedEvent>(OnEntityDeselected);
        }

        public void Dispose()
        {
            _eventBus.Unsubscribe<EntitySelectedEvent>(OnEntitySelected);
            _eventBus.Unsubscribe<EntityDeselectedEvent>(OnEntityDeselected);
        }

        private void OnEntitySelected(EntitySelectedEvent e)
        {
            // Determine entity type and open the correct inspector
            string targetWindowId = ResolveInspectorForEntity(e);

            // Close previous inspector if it's a different type
            if (_activeInspectorId != null && _activeInspectorId != targetWindowId)
            {
                _uiManager.Close(_activeInspectorId);
            }

            // Set context on the target inspector and open it
            switch (targetWindowId)
            {
                case WindowIds.StockpileInspector:
                {
                    var panel = _uiManager.GetWindow<StockpileInspectorPanel>(WindowIds.StockpileInspector);
                    var entity = ResolveEntity(e);
                    if (panel != null && entity != null)
                    {
                        panel.InspectStructure(entity.Id);
                        if (!panel.IsVisible) { _uiManager.Open(WindowIds.StockpileInspector); }
                    }
                    break;
                }
                case WindowIds.GatheringInspector:
                {
                    var panel = _uiManager.GetWindow<GatheringInspectorPanel>(WindowIds.GatheringInspector);
                    var entity = ResolveEntity(e);
                    if (panel != null && entity != null)
                    {
                        panel.InspectStructure(entity.Id);
                        if (!panel.IsVisible) { _uiManager.Open(WindowIds.GatheringInspector); }
                    }
                    break;
                }
                case WindowIds.SpawnerInspector:
                {
                    var panel = _uiManager.GetWindow<SpawnerInspectorPanel>(WindowIds.SpawnerInspector);
                    var entity = ResolveEntity(e);
                    if (panel != null && entity != null)
                    {
                        panel.InspectStructure(entity.Id);
                        if (!panel.IsVisible) { _uiManager.Open(WindowIds.SpawnerInspector); }
                    }
                    break;
                }
                case WindowIds.InnInspector:
                {
                    var panel = _uiManager.GetWindow<InnInspectorPanel>(WindowIds.InnInspector);
                    var entity = ResolveEntity(e);
                    if (panel != null && entity != null)
                    {
                        panel.InspectStructure(entity.Id);
                        if (!panel.IsVisible) { _uiManager.Open(WindowIds.InnInspector); }
                    }
                    break;
                }
                case WindowIds.CraftStationInspector:
                {
                    var panel = _uiManager.GetWindow<CraftStationInspectorPanel>(WindowIds.CraftStationInspector);
                    var entity = ResolveEntity(e);
                    if (panel != null && entity != null)
                    {
                        panel.InspectStructure(entity.Id);
                        if (!panel.IsVisible) { _uiManager.Open(WindowIds.CraftStationInspector); }
                    }
                    break;
                }
                case WindowIds.TrainingInspector:
                {
                    var panel = _uiManager.GetWindow<TrainingInspectorPanel>(WindowIds.TrainingInspector);
                    var entity = ResolveEntity(e);
                    if (panel != null && entity != null)
                    {
                        panel.InspectStructure(entity.Id);
                        if (!panel.IsVisible) { _uiManager.Open(WindowIds.TrainingInspector); }
                    }
                    break;
                }
                case WindowIds.ForgeInspector:
                {
                    var panel = _uiManager.GetWindow<ForgeInspectorPanel>(WindowIds.ForgeInspector);
                    var entity = ResolveEntity(e);
                    if (panel != null && entity != null)
                    {
                        panel.InspectStructure(entity.Id);
                        if (!panel.IsVisible) { _uiManager.Open(WindowIds.ForgeInspector); }
                    }
                    break;
                }
                case WindowIds.FilterSplitterInspector:
                {
                    var panel = _uiManager.GetWindow<FilterSplitterInspectorPanel>(WindowIds.FilterSplitterInspector);
                    var entity = ResolveEntity(e);
                    if (panel != null && entity != null)
                    {
                        panel.InspectStructure(entity.Id);
                        if (!panel.IsVisible) { _uiManager.Open(WindowIds.FilterSplitterInspector); }
                    }
                    break;
                }
                case WindowIds.DungeonPortalInspector:
                {
                    var panel = _uiManager.GetWindow<DungeonPortalInspectorPanel>(WindowIds.DungeonPortalInspector);
                    var entity = ResolveEntity(e);
                    if (panel != null && entity != null)
                    {
                        panel.InspectStructure(entity.Id);
                        if (!panel.IsVisible) { _uiManager.Open(WindowIds.DungeonPortalInspector); }
                    }
                    break;
                }
                case WindowIds.PathInspector:
                {
                    var panel = _uiManager.GetWindow<PathInspectorPanel>(WindowIds.PathInspector);
                    var entity = ResolveEntity(e);
                    if (panel != null && entity != null)
                    {
                        panel.InspectStructure(entity.Id);
                        if (!panel.IsVisible) { _uiManager.Open(WindowIds.PathInspector); }
                    }
                    break;
                }
                case WindowIds.PathGateInspector:
                {
                    var panel = _uiManager.GetWindow<PathGateInspectorPanel>(WindowIds.PathGateInspector);
                    var gate = _simulation.EntityManager.GetPathGate(new EntityId(e.EntityId));
                    if (panel != null && gate != null)
                    {
                        panel.InspectGate(gate.Id);
                        if (!panel.IsVisible) { _uiManager.Open(WindowIds.PathGateInspector); }
                    }
                    break;
                }
                case WindowIds.StructureInspector:
                {
                    var panel = _uiManager.GetWindow<GenericStructureInspectorPanel>(WindowIds.StructureInspector);
                    var entity = ResolveEntity(e);
                    if (panel != null && entity != null)
                    {
                        panel.InspectStructure(entity.Id);
                        if (!panel.IsVisible) { _uiManager.Open(WindowIds.StructureInspector); }
                    }
                    break;
                }
                case WindowIds.VillagerInspector:
                {
                    var panel = _uiManager.GetWindow<RichVillagerInspectorPanel>(WindowIds.VillagerInspector);
                    if (panel != null)
                    {
                        panel.InspectVillager(e.EntityId);
                        if (!panel.IsVisible) { _uiManager.Open(WindowIds.VillagerInspector); }
                    }
                    break;
                }
                case WindowIds.WorkerInspector:
                {
                    var panel = _uiManager.GetWindow<RichWorkerInspectorPanel>(WindowIds.WorkerInspector);
                    if (panel != null)
                    {
                        panel.InspectWorker(e.EntityId);
                        if (!panel.IsVisible) { _uiManager.Open(WindowIds.WorkerInspector); }
                    }
                    break;
                }
            }

            _activeInspectorId = targetWindowId;
        }

        private void OnEntityDeselected(EntityDeselectedEvent e)
        {
            if (_activeInspectorId != null)
            {
                _uiManager.Close(_activeInspectorId);
                _activeInspectorId = null;
            }
        }

        /// <summary>
        /// Determines which inspector window ID should handle the selected entity.
        /// </summary>
        private string ResolveInspectorForEntity(EntitySelectedEvent e)
        {
            // Check for hero/worker first
            if (_simulation.EntityManager.HeroIndex.TryGetValue(new EntityId(e.EntityId), out _))
            {
                return WindowIds.WorkerInspector;
            }

            // Check for villager
            var villager = _simulation.EntityManager.GetVillager(new EntityId(e.EntityId));
            if (villager != null)
            {
                return WindowIds.VillagerInspector;
            }

            // Check for structure by ID or position
            var structure = _simulation.EntityManager.GetStructure(new EntityId(e.EntityId))
                           ?? _simulation.EntityManager.GetStructureAt(e.Position);
            if (structure != null)
            {
                return ResolveEntityInspectorId(structure);
            }

            // Check for routing node
            var routingNode = _simulation.EntityManager.GetRoutingNode(new EntityId(e.EntityId));
            if (routingNode != null)
            {
                return ResolveEntityInspectorId(routingNode);
            }

            // Check for path segment
            var pathSeg = _simulation.EntityManager.GetPathSegment(new EntityId(e.EntityId));
            if (pathSeg != null)
            {
                return ResolveEntityInspectorId(pathSeg);
            }

            // Check for path gate
            var pathGate = _simulation.EntityManager.GetPathGate(new EntityId(e.EntityId));
            if (pathGate != null)
            {
                return WindowIds.PathGateInspector;
            }

            // Fallback to generic inspector
            return WindowIds.StructureInspector;
        }

        /// <summary>Maps an entity logic instance to the correct inspector window ID.</summary>
        private static string ResolveEntityInspectorId(StructureBase entity) => entity switch
        {
            StockpileLogic => WindowIds.StockpileInspector,
            GatheringLogicBase => WindowIds.GatheringInspector,
            VillageSpawnerLogic => WindowIds.SpawnerInspector,
            InnLogic => WindowIds.InnInspector,
            CraftStationLogic => WindowIds.CraftStationInspector,
            TrainingBuildingLogic => WindowIds.TrainingInspector,
            ForgeLogic => WindowIds.ForgeInspector,
            FilterSplitterLogic => WindowIds.FilterSplitterInspector,
            DungeonPortalLogic => WindowIds.DungeonPortalInspector,
            PathSegmentLogic => WindowIds.PathInspector,
            BalancerLogic => WindowIds.PathInspector,
            CheckGateLogic => WindowIds.PathInspector,
            _ => WindowIds.StructureInspector
        };

        private StructureBase? ResolveEntity(EntitySelectedEvent e)
        {
            StructureBase? entity = _simulation.EntityManager.GetStructure(new EntityId(e.EntityId));
            if (entity != null) { return entity; }
            entity = _simulation.EntityManager.GetRoutingNode(new EntityId(e.EntityId));
            if (entity != null) { return entity; }
            entity = _simulation.EntityManager.GetPathSegment(new EntityId(e.EntityId));
            if (entity != null) { return entity; }
            return _simulation.EntityManager.GetStructureAt(e.Position);
        }
    }
}
