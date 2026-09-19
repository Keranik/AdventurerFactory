using ForgeFlow.Core.Entities;
using ForgeFlow.Core.Events;

namespace ForgeFlow.Core.Systems;

/// <summary>
/// Single source of truth for late-game world-level meta-state:
/// cataclysm events, prestige resets, world portals, and console platform hooks.
/// Extracted from SimulationTicker so each concern has a clear owner.
/// </summary>
public sealed class WorldStateManager : IGameSystem
{
    private readonly ResearchManager _researchManager;
    private readonly EntityManager _entityManager;
    private readonly ItemManager _itemManager;
    private readonly EventBus _eventBus;

    /// <summary>Console platform hooks — injected by Presentation at startup.</summary>
    public IConsolePlatformHooks? PlatformHooks { get; private set; }

    /// <summary>Whether a cataclysm event is currently active.</summary>
    public bool CataclysmActive { get; private set; }

    /// <summary>Current cataclysm intensity (0.0–1.0).</summary>
    public float CataclysmIntensity { get; private set; }

    /// <summary>Total prestige resets completed.</summary>
    public int PrestigeCount { get; private set; }

    /// <summary>Permanent prestige bonus multiplier (stacks across resets).</summary>
    public float PrestigeBonusMultiplier => 1.0f + PrestigeCount * 0.15f;

    /// <summary>Whether a world portal is currently open.</summary>
    public bool WorldPortalOpen { get; private set; }

    public WorldStateManager(
        ResearchManager researchManager,
        EntityManager entityManager,
        ItemManager itemManager,
        EventBus eventBus)
    {
        _researchManager = researchManager;
        _entityManager = entityManager;
        _itemManager = itemManager;
        _eventBus = eventBus;
    }

    // ── Console Platform ─────────────────────────────────────────────

    /// <summary>Injects platform-specific hooks. Call once during bootstrap.</summary>
    public void SetPlatformHooks(IConsolePlatformHooks hooks)
    {
        PlatformHooks = hooks;
    }

    /// <summary>
    /// Prepares the simulation for a console build: validates state,
    /// enables controller-friendly defaults, and verifies cloud save compat.
    /// </summary>
    public void PrepareForConsoleBuild()
    {
        if (PlatformHooks == null)
        {
            PlatformHooks = new DefaultPcHooks();
        }

        // Ensure all achievements are trackable
        _eventBus.Publish(new ConsoleReadyEvent(PlatformHooks.PlatformName));
    }

    // ── Cataclysm ────────────────────────────────────────────────────

    /// <summary>
    /// Triggers a cataclysm event — a late-game challenge that increases
    /// dungeon difficulty and speeds up spawners but grants bonus resources.
    /// </summary>
    public bool TriggerCataclysm(float intensity = 0.5f)
    {
        if (CataclysmActive) { return false; }
        if (_researchManager.CurrentTier < 4) { return false; }

        CataclysmActive = true;
        CataclysmIntensity = Math.Clamp(intensity, 0.1f, 1.0f);
        _eventBus.Publish(new CataclysmEvent(true, CataclysmIntensity));
        return true;
    }

    /// <summary>Ends the active cataclysm, awarding bonus resources.</summary>
    public bool EndCataclysm(List<HeroEntity> heroes)
    {
        if (!CataclysmActive) { return false; }

        // Award bonus resources based on intensity and heroes survived
        int survivingHeroes = heroes.Count;
        int bonus = (int)(survivingHeroes * CataclysmIntensity * 100);
        _itemManager.AddStock("gold", bonus);

        CataclysmActive = false;
        CataclysmIntensity = 0f;
        _eventBus.Publish(new CataclysmEvent(false, 0f));
        return true;
    }

    // ── World Portal ─────────────────────────────────────────────────

    /// <summary>
    /// Opens a world portal — allows heroes to travel to a new world tier
    /// with scaled difficulty and rewards.
    /// </summary>
    public bool OpenWorldPortal()
    {
        if (WorldPortalOpen) { return false; }
        if (_researchManager.CurrentTier < 5) { return false; }

        WorldPortalOpen = true;
        _eventBus.Publish(new WorldPortalEvent(true));
        return true;
    }

    /// <summary>Closes the active world portal.</summary>
    public void CloseWorldPortal()
    {
        WorldPortalOpen = false;
        _eventBus.Publish(new WorldPortalEvent(false));
    }

    // ── Prestige ─────────────────────────────────────────────────────

    /// <summary>
    /// Performs a prestige reset: wipes all heroes, paths, structures, and
    /// research but awards a permanent prestige bonus and increments the counter.
    /// </summary>
    public bool PrestigeReset()
    {
        if (_researchManager.CurrentTier < 3) { return false; }

        int previousTier = _researchManager.CurrentTier;
        PrestigeCount++;

        // Wipe all runtime state
        _entityManager.Clear();
        _itemManager.Clear();
        _researchManager.Reset();

        // Close any portals / cataclysms
        CataclysmActive = false;
        CataclysmIntensity = 0f;
        WorldPortalOpen = false;

        _eventBus.Publish(new PrestigeResetEvent(PrestigeCount, previousTier, PrestigeBonusMultiplier));
        return true;
    }

    /// <summary>
    /// Restores world state from save data. Does not publish events.
    /// </summary>
    public void LoadFromSave(bool cataclysmActive, bool worldPortalOpen, int prestigeCount)
    {
        CataclysmActive = cataclysmActive;
        WorldPortalOpen = worldPortalOpen;
        PrestigeCount = prestigeCount;
        CataclysmIntensity = cataclysmActive ? 0.5f : 0f;
    }
}
