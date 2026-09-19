using ForgeFlow.Core;
using ForgeFlow.Core.Data;
using ForgeFlow.Core.Entities;
using ForgeFlow.Core.Events;
using ForgeFlow.Core.Systems;
using UnityEngine;
using EntityId = ForgeFlow.Core.Entities.EntityId;

namespace ForgeFlow.Presentation.Unity.Visuals
{

/// <summary>
/// Thin Presentation glue for the Appearance Workshop structure.
/// When a hero is selected at a workshop, exposes palette/part changes
/// that call through to Core's AppearanceApplier API.
/// </summary>
internal class AppearanceWorkshop : MonoBehaviour
{
    private SimulationTicker _simulation = null!;
    private GameBootstrapper _bootstrapper = null!;
    private VisualHeroManager _heroManager = null!;
    private EventBus _eventBus = null!;
    private ulong? _selectedHeroId;

    public void Initialize(SimulationTicker simulation, GameBootstrapper bootstrapper,
                           VisualHeroManager heroManager, EventBus eventBus)
    {
        _simulation = simulation;
        _bootstrapper = bootstrapper;
        _heroManager = heroManager;
        _eventBus = eventBus;
    }

    public void SelectHero(ulong heroId)
    {
        if (!_simulation.EntityManager.HeroIndex.ContainsKey(new EntityId(heroId)))
        {
            Debug.LogWarning($"[AppearanceWorkshop] Hero {heroId} not found");
            return;
        }
        _selectedHeroId = heroId;
        Debug.Log($"[AppearanceWorkshop] Selected hero {heroId}");
    }

    public void DeselectHero()
    {
        _selectedHeroId = null;
    }

    public void ApplyPalette(string paletteId)
    {
        if (_selectedHeroId == null) return;
        if (!_simulation.EntityManager.HeroIndex.TryGetValue(new EntityId(_selectedHeroId.Value), out var hero)) return;

        var registry = _bootstrapper.Services.Get<AppearanceRegistry>();
        if (!registry.TryGetPalette(paletteId, out _))
        {
            Debug.LogWarning($"[AppearanceWorkshop] Palette '{paletteId}' not found");
            return;
        }

        // Core call: apply palette to hero
        var applier = _simulation.AppearanceApplier;
        applier.SetRegistry(registry);
        applier.ApplyPalette(hero, paletteId);

        // Publish event so other systems react
        _eventBus.Publish(new AppearanceChangedEvent(hero.Id, "palette", paletteId));

        // Update the visual hero
        if (_heroManager.VisualHeroes.TryGetValue(hero.Id, out var visualHero))
        {
            visualHero.ApplyAppearanceFromTemplate(hero.CurrentTemplate);
        }

        Debug.Log($"[AppearanceWorkshop] Applied palette '{paletteId}' to hero {hero.Id}");
    }

    public void ApplyPart(string partId)
    {
        if (_selectedHeroId == null) return;
        if (!_simulation.EntityManager.HeroIndex.TryGetValue(new EntityId(_selectedHeroId.Value), out var hero)) return;

        var registry = _bootstrapper.Services.Get<AppearanceRegistry>();
        var applier = _simulation.AppearanceApplier;
        applier.SetRegistry(registry);
        applier.ApplyPart(hero, partId);

        _eventBus.Publish(new AppearanceChangedEvent(hero.Id, "part", partId));

        if (_heroManager.VisualHeroes.TryGetValue(hero.Id, out var visualHero))
        {
            visualHero.ApplyAppearanceFromTemplate(hero.CurrentTemplate);
        }

        Debug.Log($"[AppearanceWorkshop] Applied part '{partId}' to hero {hero.Id}");
    }

    public void ApplyTemplate(string templateId)
    {
        if (_selectedHeroId == null) return;
        if (!_simulation.EntityManager.HeroIndex.TryGetValue(new EntityId(_selectedHeroId.Value), out var hero)) return;

        var registry = _bootstrapper.Services.Get<AppearanceRegistry>();
        var applier = _simulation.AppearanceApplier;
        applier.SetRegistry(registry);
        applier.ApplyTemplate(hero, templateId);

        _eventBus.Publish(new AppearanceChangedEvent(hero.Id, "template", templateId));

        if (_heroManager.VisualHeroes.TryGetValue(hero.Id, out var visualHero))
        {
            visualHero.ApplyAppearanceFromTemplate(hero.CurrentTemplate);
        }

        Debug.Log($"[AppearanceWorkshop] Applied template '{templateId}' to hero {hero.Id}");
    }
}
}
