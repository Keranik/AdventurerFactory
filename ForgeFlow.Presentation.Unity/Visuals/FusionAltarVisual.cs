using ForgeFlow.Core.Entities;
using ForgeFlow.Core.Events;
using ForgeFlow.Core.Systems;
using UnityEngine;
using EntityId = ForgeFlow.Core.Entities.EntityId;

namespace ForgeFlow.Presentation.Unity.Visuals
{

/// <summary>
/// Visual effects for the Fusion Altar: scale-up animation, golden particles,
/// combine two hero visuals into one fused hero.
/// </summary>
internal class FusionAltarVisual : MonoBehaviour
{
    private SimulationTicker _simulation = null!;
    private EventBus _eventBus = null!;
    private VisualHeroManager _heroManager = null!;
    private bool _isFusing;
    private float _fuseTimer;
    private const float FuseDuration = 2.0f;
    private ulong _sourceHero1;
    private ulong _sourceHero2;
    private ulong _resultHero;

    public void Initialize(SimulationTicker simulation, EventBus eventBus, VisualHeroManager heroManager)
    {
        _simulation = simulation;
        _eventBus = eventBus;
        _heroManager = heroManager;

        _eventBus.Subscribe<HeroFusedEvent>(OnHeroFused);
    }

    private void OnDestroy()
    {
        _eventBus.Unsubscribe<HeroFusedEvent>(OnHeroFused);
    }

    private void OnHeroFused(HeroFusedEvent e)
    {
        _sourceHero1 = e.SourceHeroId1;
        _sourceHero2 = e.SourceHeroId2;
        _resultHero = e.ResultHeroId;
        _isFusing = true;
        _fuseTimer = 0f;

        Debug.Log($"[FusionVFX] Starting fusion: {e.SourceHeroId1} + {e.SourceHeroId2} → {e.ResultHeroId} ({e.ResultClassId} Lv{e.ResultLevel})");

        // Create golden particle burst at fusion location
        SpawnFusionParticles();
    }

    private void Update()
    {
        if (!_isFusing) return;

        _fuseTimer += Time.deltaTime;

        // Scale-up animation: source heroes grow during fusion
        float t = _fuseTimer / FuseDuration;
        if (t < 1.0f)
        {
            float scale = 1.0f + t * 0.5f;
            ScaleHeroVisual(_sourceHero1, scale);
            ScaleHeroVisual(_sourceHero2, scale);
        }
        else
        {
            // Fusion complete — clean up
            _heroManager.DestroyVisualHero(_sourceHero1);
            _heroManager.DestroyVisualHero(_sourceHero2);

            var resultHero = _simulation.EntityManager.GetHero(new EntityId(_resultHero));
            if (resultHero != null)
            {
                _heroManager.SpawnVisualHero(resultHero.Id, resultHero.ClassId, resultHero.Position);
            }

            _isFusing = false;
            Debug.Log("[FusionVFX] Fusion animation complete");
        }
    }

    private void ScaleHeroVisual(ulong heroId, float scale)
    {
        if (_heroManager.VisualHeroes.TryGetValue(heroId, out var visual))
        {
            visual.transform.localScale = new Vector3(scale, scale, scale);
        }
    }

    private void SpawnFusionParticles()
    {
        var particleObj = new GameObject("FusionParticles");
        particleObj.transform.position = transform.position;
        var ps = particleObj.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.startColor = new Color(1f, 0.85f, 0.2f); // golden
        main.startLifetime = 1.5f;
        main.startSpeed = 3f;
        main.startSize = 0.1f;
        main.maxParticles = 50;
        main.loop = false;
        main.duration = FuseDuration;
        var emissionModule = ps.emission;
        emissionModule.rateOverTime = 30f;
        ps.Play();

        Debug.Log("[FusionVFX] Golden particles spawned");
    }
}
}
