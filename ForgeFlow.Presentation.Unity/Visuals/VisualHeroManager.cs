using ForgeFlow.Core.Entities;
using ForgeFlow.Core.Events;
using ForgeFlow.Core.Systems;
using UnityEngine;
using EntityId = ForgeFlow.Core.Entities.EntityId;

namespace ForgeFlow.Presentation.Unity.Visuals
{

/// <summary>
/// Manages visual representations of all heroes on screen.
/// Subscribes to Core events and creates / updates / destroys
/// <see cref="VisualHero"/> instances accordingly.
/// </summary>
internal class VisualHeroManager : MonoBehaviour
{
    [Header("Hero Visuals")]
    [SerializeField] private float _gridCellSize = 1.0f;
    [SerializeField] private float _heroMoveSmoothing = 10f;

    private SimulationTicker _simulation = null!;
    private EventBus _eventBus = null!;
    private GameObjectPool _pool = null!;
    private readonly Dictionary<ulong, VisualHero> _visualHeroes = new();

    public IReadOnlyDictionary<ulong, VisualHero> VisualHeroes => _visualHeroes;

    public void Initialize(SimulationTicker simulation, EventBus eventBus)
    {
        _simulation = simulation;
        _eventBus = eventBus;

        var poolRoot = new GameObject("HeroPool");
        poolRoot.transform.SetParent(transform);
        _pool = new GameObjectPool(poolRoot.transform);
    }

    private void Update()
    {
        SyncPositions();
    }

    /// <summary>
    /// Each render frame, smoothly interpolate every visual hero toward
    /// its Core-side grid position. The simulation may tick multiple times
    /// per frame or not at all, so we lerp to decouple visuals from sim.
    /// </summary>
    private void SyncPositions()
    {
        foreach (var hero in _simulation.EntityManager.Heroes)
        {
            if (!_visualHeroes.TryGetValue(hero.Id, out var visual))
            {
                continue;
            }

            var targetWorld = GridToWorld(hero.Position, hero.PathProgress, hero.CurrentPathSegmentId);
            visual.TargetPosition = targetWorld;
            visual.SmoothMove(Time.deltaTime, _heroMoveSmoothing);
            visual.UpdateStateVisuals(hero.State, hero.Level, hero.Morale);
        }
    }

    public void SpawnVisualHero(ulong heroId, string classId, GridPosRPG spawnPos)
    {
        if (_visualHeroes.ContainsKey(heroId))
        {
            return;
        }

        var go = _pool.Get("hero", () =>
        {
            var g = new GameObject();
            g.AddComponent<VisualHero>();
            return g;
        });
        go.name = $"Hero_{heroId}_{classId}";
        go.transform.position = GridToWorld(spawnPos, 0f, null);

        var visual = go.GetComponent<VisualHero>();
        visual.HeroId = heroId;
        visual.ClassId = classId;
        visual.BuildPlaceholderVisuals();

        _visualHeroes[heroId] = visual;

        Debug.Log($"[Visual] Spawned hero {heroId} ({classId}) at {spawnPos}");
    }

    public void DestroyVisualHero(ulong heroId)
    {
        if (_visualHeroes.TryGetValue(heroId, out var visual))
        {
            _pool.Release(visual.gameObject);
            _visualHeroes.Remove(heroId);
        }
    }

    public void UpdateHeroAppearance(ulong heroId, string itemId, EquipSlot slot)
    {
        if (_visualHeroes.TryGetValue(heroId, out var visual))
        {
            visual.OnGearChanged(itemId, slot);
        }
    }

    public void PlaySuccessEffect(ulong heroId)
    {
        if (_visualHeroes.TryGetValue(heroId, out var visual))
        {
            visual.PlayEffect(VisualHero.EffectType.DungeonSuccess);
        }
    }

    public void PlayDeathEffect(ulong heroId)
    {
        if (_visualHeroes.TryGetValue(heroId, out var visual))
        {
            visual.PlayEffect(VisualHero.EffectType.Death);
            // Ghost lingers briefly then gets cleaned up
        }
    }

    private Vector3 GridToWorld(GridPosRPG gridPos, float pathProgress, ulong? currentSegmentId)
    {
        float x = gridPos.X * _gridCellSize;
        float z = gridPos.Y * _gridCellSize;

        // Offset along the path direction for smooth movement between cells
        if (currentSegmentId.HasValue &&
            _simulation.EntityManager.PathSegments.TryGetValue(new EntityId(currentSegmentId.Value), out var segment))
        {
            var dir = DirectionToVector(segment.Facing);
            x += dir.x * pathProgress * _gridCellSize;
            z += dir.z * pathProgress * _gridCellSize;
        }

        return new Vector3(x, 0f, z);
    }

    private static Vector3 DirectionToVector(Direction dir) => dir switch
    {
        Direction.North => new Vector3(0, 0, 1),
        Direction.East => new Vector3(1, 0, 0),
        Direction.South => new Vector3(0, 0, -1),
        Direction.West => new Vector3(-1, 0, 0),
        _ => Vector3.zero
    };
}
}
