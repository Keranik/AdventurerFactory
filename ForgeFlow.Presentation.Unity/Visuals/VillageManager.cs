using ForgeFlow.Core;
using ForgeFlow.Core.Data;
using ForgeFlow.Core.Events;
using ForgeFlow.Core.Systems;
using UnityEngine;

namespace ForgeFlow.Presentation.Unity.Visuals
{

/// <summary>
/// Manages village buildings at runtime: spawns primitive visual placeholders
/// for each built building, shows assigned heroes, and forwards build/assign
/// commands to Core's VillageRegistry.
/// </summary>
internal class VillageManager : MonoBehaviour
{
    private SimulationTicker _simulation = null!;
    private GameBootstrapper _bootstrapper = null!;
    private EventBus _eventBus = null!;

    private readonly Dictionary<string, GameObject> _buildingObjects = new();
    private int _nextBuildingSlotX;

    public void Initialize(SimulationTicker simulation, GameBootstrapper bootstrapper, EventBus eventBus)
    {
        _simulation = simulation;
        _bootstrapper = bootstrapper;
        _eventBus = eventBus;

        _eventBus.Subscribe<VillageBuildingBuiltEvent>(OnBuildingBuilt);
    }

    private void OnDestroy()
    {
        _eventBus.Unsubscribe<VillageBuildingBuiltEvent>(OnBuildingBuilt);
    }

    public void BuildBuilding(string buildingId)
    {
        var villageReg = _bootstrapper.Services.Get<VillageRegistry>();
        if (!villageReg.TryGetDefinition(buildingId, out var def)) return;
        if (def.RequiredTier > _simulation.ResearchManager.CurrentTier)
        {
            Debug.Log($"[Village] {buildingId} requires Research Tier {def.RequiredTier}");
            return;
        }

        if (!_simulation.ItemManager.TrySpendResources(def.BuildCost))
        {
            Debug.Log($"[Village] Cannot build {buildingId} (missing resources)");
            return;
        }

        var built = villageReg.BuildBuilding(buildingId);
        if (built)
        {
            _eventBus.Publish(new VillageBuildingBuiltEvent(buildingId));
            Debug.Log($"[Village] Built {buildingId}");
        }
        else
        {
            // Refund if already built
            foreach (var kvp in def.BuildCost)
            {
                _simulation.ItemManager.AddStock(kvp.Key, kvp.Value);
            }
            Debug.Log($"[Village] Cannot build {buildingId} (already built)");
        }
    }

    private void OnBuildingBuilt(VillageBuildingBuiltEvent e)
    {
        SpawnBuildingVisual(e.BuildingId);
    }

    private void SpawnBuildingVisual(string buildingId)
    {
        if (_buildingObjects.ContainsKey(buildingId)) return;

        var def = _bootstrapper.Services.Get<VillageRegistry>().GetDefinition(buildingId);
        if (def == null) return;

        // Spawn a primitive at a village area offset from the factory
        var obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obj.name = $"Village_{buildingId}";
        obj.transform.position = new Vector3(-8f + _nextBuildingSlotX * 3f, 0.75f, -8f);
        obj.transform.localScale = new Vector3(2f, 1.5f, 2f);

        var renderer = obj.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            renderer.material = RuntimePlaceholderFactory.CreateLitMaterial(def.Category switch
            {
                "production" => new Color(0.6f, 0.5f, 0.2f),
                "training" => new Color(0.5f, 0.2f, 0.2f),
                "research" => new Color(0.2f, 0.3f, 0.6f),
                "decoration" => new Color(0.7f, 0.7f, 0.3f),
                _ => new Color(0.5f, 0.5f, 0.5f)
            });
        }

        // Floating name label placeholder
        var labelObj = new GameObject("Label");
        labelObj.transform.SetParent(obj.transform);
        labelObj.transform.localPosition = new Vector3(0f, 1.5f, 0f);

        _buildingObjects[buildingId] = obj;
        _nextBuildingSlotX++;
    }

    }
}
