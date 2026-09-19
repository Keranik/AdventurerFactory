using ForgeFlow.Core.Data;
using ForgeFlow.Core.Data.Definitions;
using ForgeFlow.Core.Entities;
using ForgeFlow.Core.Events;

namespace ForgeFlow.Core.Systems;

public sealed class AppearanceApplier : IGameSystem, IDisposable
{
    private readonly EventBus _eventBus;
    private readonly EntityManager _entityManager;
    private AppearanceRegistry? _registry;

    public AppearanceApplier(EventBus eventBus, EntityManager entityManager)
    {
        _eventBus = eventBus;
        _entityManager = entityManager;

        _eventBus.Subscribe<SimulationLateTickEvent>(OnLateTick);
    }

    public void Dispose()
    {
        _eventBus.Unsubscribe<SimulationLateTickEvent>(OnLateTick);
    }

    private void OnLateTick(SimulationLateTickEvent e)
    {
        var heroes = _entityManager.Heroes;
        for (int i = 0; i < heroes.Count; i++)
        {
            var hero = heroes[i];
            if (hero.State == HeroState.OnPath || hero.State == HeroState.EquippingGear)
            {
                ApplyGearToAppearance(hero);
            }
        }
    }

    public void SetRegistry(AppearanceRegistry registry)
    {
        _registry = registry;
    }

    public void ApplyGearToAppearance(HeroEntity hero)
    {
        var appearance = hero.Appearance;

        appearance.Helmet = null;
        appearance.ChestArmor = null;
        appearance.Pauldrons = null;
        appearance.Cape = null;
        appearance.Boots = null;
        appearance.WeaponSheath = null;
        appearance.AuraEffect = null;
        appearance.ParticleTrail = null;

        foreach (var item in hero.Equipment)
        {
            switch (item.Slot)
            {
                case EquipSlot.Helmet:
                    appearance.Helmet = item.ProtoId;
                    break;
                case EquipSlot.ChestArmor:
                    appearance.ChestArmor = item.ProtoId;
                    break;
                case EquipSlot.Pauldrons:
                    appearance.Pauldrons = item.ProtoId;
                    break;
                case EquipSlot.Cape:
                    appearance.Cape = item.ProtoId;
                    break;
                case EquipSlot.Boots:
                    appearance.Boots = item.ProtoId;
                    break;
                case EquipSlot.Weapon:
                    appearance.WeaponSheath = item.ProtoId;
                    break;
            }

            if (item.Tier >= 5)
            {
                appearance.AuraEffect = $"tier{item.Tier}_glow";
            }
            if (item.Tier >= 8)
            {
                appearance.ParticleTrail = $"tier{item.Tier}_trail";
            }
        }

        if (hero.Level >= 20)
        {
            appearance.AuraEffect ??= "champion_aura";
        }
        if (hero.Level >= 40)
        {
            appearance.ParticleTrail ??= "legendary_trail";
        }
    }

    public void ApplyTemplate(HeroEntity hero, string templateId)
    {
        if (_registry == null) return;
        if (!_registry.TryGetTemplate(templateId, out var template)) return;

        var ct = hero.CurrentTemplate;
        ct.Name = template.DisplayName;
        ct.BaseBody = template.BaseBody;
        ct.Hair = template.Hair;
        ct.Face = template.Face;

        if (_registry.TryGetPalette(template.PaletteId, out var palette))
        {
            ct.ColorPalette = new Dictionary<string, string>(palette.Colors);
        }

        foreach (var kvp in template.PartOverrides)
        {
            ct.MaterialOverrides[kvp.Key] = kvp.Value;
        }
    }

    public void ApplyPalette(HeroEntity hero, string paletteId)
    {
        if (_registry == null) return;
        if (!_registry.TryGetPalette(paletteId, out var palette)) return;

        hero.CurrentTemplate.ColorPalette = new Dictionary<string, string>(palette.Colors);
    }

    /// <summary>
    /// Opens the appearance editor for a hero identified by seed.
    /// Returns the editor data snapshot that the Presentation layer
    /// can use to populate the editor panel, or null if the hero was not found.
    /// </summary>
    public AppearanceEditorData? OpenAppearanceEditor(ulong heroSeed, IReadOnlyList<HeroEntity> heroes)
    {
        HeroEntity? hero = null;
        foreach (var h in heroes)
        {
            if (h.Seed == heroSeed) { hero = h; break; }
        }
        if (hero == null) { return null; }

        return new AppearanceEditorData
        {
            HeroSeed = heroSeed,
            ActiveTemplateId = hero.CurrentTemplate.Name,
            ActivePaletteId = hero.CurrentTemplate.ColorPalette.Count > 0 ? "custom" : "default",
            SlotAssignments = new Dictionary<string, string>
            {
                { "hair", hero.CurrentTemplate.Hair },
                { "face", hero.CurrentTemplate.Face },
                { "body", hero.CurrentTemplate.BaseBody }
            },
            SlotColorOverrides = new Dictionary<string, string>(hero.CurrentTemplate.ColorPalette)
        };
    }

    public void ApplyPart(HeroEntity hero, string partId)
    {
        if (_registry == null) return;
        if (!_registry.TryGetPart(partId, out var part)) return;

        switch (part.Category)
        {
            case "hair":
                hero.CurrentTemplate.Hair = partId;
                break;
            case "face":
                hero.CurrentTemplate.Face = partId;
                break;
            case "cape":
                hero.Appearance.Cape = partId;
                break;
        }
    }
}
