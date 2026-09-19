using ForgeFlow.Core.Commands;
using ForgeFlow.Core.Data;
using ForgeFlow.Core.Entities;
using ForgeFlow.Core.Events;
using ForgeFlow.Core.Localization;
using ForgeFlow.Core.Modding;
using ForgeFlow.Core.Proto;
using ForgeFlow.Core.Save;
using ForgeFlow.Core.Systems;
using ForgeFlow.Core.Theming;
using ForgeFlow.Core.Utilities;

namespace ForgeFlow.Core;

/// <summary>
/// Composition root. Owns the <see cref="ServiceContainer"/> that resolves every
/// service in the game. Callers access services through <see cref="Services"/>
/// (an <see cref="IServiceResolver"/>) — e.g. <c>bootstrapper.Services.Get&lt;EventBus&gt;()</c>.
/// </summary>
public sealed class GameBootstrapper
{
    /// <summary>Default map width (tiles). Doubled from 32 in v1.5 for expanded gameplay.</summary>
    public const int DefaultMapWidth = 128;
    /// <summary>Default map height (tiles). Doubled from 32 in v1.5 for expanded gameplay.</summary>
    public const int DefaultMapHeight = 128;

    private readonly ServiceContainer _services = new();

    /// <summary>Canonical resolver for every service wired into the game.</summary>
    public IServiceResolver Services => _services;

    /// <summary>Stores the last new-game settings used, so the presentation layer can reference them.</summary>
    public NewGameSettings? LastNewGameSettings { get; private set; }

    public SimulationTicker Bootstrap(string? modsPath = null)
    {
        RegisterAllServices();

        // Eager-resolve in the legacy order so registration side effects
        // (embedded-JSON loads, RegisterDefaults calls) happen in the same
        // sequence the old phase methods produced.
        _ = _services.Get<ProtoRegistry>();     // LoadEmbedded + RegisterDefaults
        _ = _services.Get<DataLoader>();        // LoadAllFromEmbeddedResources
        _ = _services.Get<AppearanceRegistry>();// RegisterDefaults (post DataLoader)
        _ = _services.Get<VillageRegistry>();   // RegisterDefaults
        var modLoader = _services.Get<ModLoader>();
        if (modsPath != null)
        {
            modLoader.LoadAllMods(modsPath);
        }

        // Full transitive resolve — brings up every core system + ticker.
        var sim = _services.Get<SimulationTicker>();

        // Eagerly construct the "always-on" game-flow / subscription services.
        // They register event handlers in their constructors (tutorial rewards,
        // statistics counters, achievement tracking, etc.), so they must exist
        // before any gameplay event fires — matching the legacy bootstrap order.
        _ = _services.Get<SaveManager>();
        _ = _services.Get<GameStateMachine>();
        _ = _services.Get<GameStatistics>();
        _ = _services.Get<AchievementSystem>();
        _ = _services.Get<SettingsManager>();
        _ = _services.Get<TranslationService>();
        _ = _services.Get<ThemeRegistry>();
        _ = _services.Get<ThemeService>();
        _ = _services.Get<GameplayFlowSystem>();
        _ = _services.Get<HotbarSystem>();

        return sim;
    }

    private void RegisterAllServices()
    {
        // ── Logger (sanctioned ServiceLocator fallback, Bible §3.6) ──
#pragma warning disable CS0618 // ServiceLocator.Register/Get/IsRegistered are [Obsolete] for new code; logger fallback is the sanctioned exception.
        if (!ServiceLocator.IsRegistered<IForgeLogger>())
        {
            ServiceLocator.Register<IForgeLogger>(NullLogger.Instance);
        }
        var logger = ServiceLocator.Get<IForgeLogger>();
#pragma warning restore CS0618
        _services.RegisterInstance<IForgeLogger>(logger);

        // ── Buses ──────────────────────────────────────────────────
        _services.RegisterSingleton<EventBus>(_ =>
        {
            var bus = new EventBus();
            bus.OnHandlerException = ex => logger.Error($"[EventBus] Handler threw: {ex}");
            return bus;
        });
        _services.RegisterSingleton<CommandBus>(_ =>
        {
            var bus = new CommandBus();
            bus.OnHandlerException = ex => logger.Error($"[CommandBus] Handler threw: {ex}");
            return bus;
        });

        // ── Pure registries ────────────────────────────────────────
        _services.RegisterSingleton<ItemRegistry>(_ => new ItemRegistry());
        _services.RegisterSingleton<ClassRegistry>(_ => new ClassRegistry());
        _services.RegisterSingleton<RecipeRegistry>(_ => new RecipeRegistry());
        _services.RegisterSingleton<DungeonRegistry>(_ => new DungeonRegistry());
        _services.RegisterSingleton<ModBrowserRegistry>(_ => new ModBrowserRegistry());

        _services.RegisterSingleton<AppearanceRegistry>(_ =>
        {
            // RegisterDefaults() is sequenced by the eager-resolve in Bootstrap()
            // so it always runs AFTER DataLoader has populated embedded content.
            var r = new AppearanceRegistry();
            r.RegisterDefaults();
            return r;
        });
        _services.RegisterSingleton<VillageRegistry>(_ =>
        {
            var r = new VillageRegistry();
            r.RegisterDefaults();
            return r;
        });

        _services.RegisterSingleton<SoundDb>(_ =>
        {
            var s = new SoundDb();
            s.RegisterDefaults();
            return s;
        });
        _services.RegisterSingleton<AssetManifest>(_ =>
        {
            var a = new AssetManifest();
            a.RegisterDefaults();
            return a;
        });

        _services.RegisterSingleton<ProtoRegistry>(_ =>
        {
            var r = new ProtoRegistry();
            r.LoadAllFromEmbeddedResources();
            r.RegisterDefaults();
            return r;
        });
        _services.RegisterSingleton<ProtoFactory>(s => new ProtoFactory(s.Get<ProtoRegistry>(), s.Get<EventBus>()));

        // ── Data loading ───────────────────────────────────────────
        _services.RegisterSingleton<DataLoader>(s =>
        {
            var l = new DataLoader(s.Get<ItemRegistry>(), s.Get<ClassRegistry>(), s.Get<RecipeRegistry>(), s.Get<DungeonRegistry>());
            l.LoadAllFromEmbeddedResources();
            return l;
        });
        _services.RegisterSingleton<ModLoader>(s => new ModLoader(s.Get<DataLoader>(), s.Get<EventBus>()));

        // ── Core simulation systems ────────────────────────────────
        _services.RegisterSingleton<TrafficManager>(_ => new TrafficManager());
        _services.RegisterSingleton<Pathfinding>(_ => new Pathfinding());

        _services.RegisterSingleton<TerrainGrid>(s =>
        {
            var t = new TerrainGrid(DefaultMapWidth, DefaultMapHeight);
            t.GenerateDefault();
            s.Get<EventBus>().Publish(new TerrainGeneratedEvent(t.Width, t.Height));
            return t;
        });

        _services.RegisterSingleton<ItemManager>(s => new ItemManager(s.Get<EventBus>()));
        _services.RegisterSingleton<GatingLimits>(_ => new GatingLimits());
        _services.RegisterSingleton<ResearchManager>(s => new ResearchManager(s.Get<EventBus>()));
        _services.RegisterSingleton<VillagerSystem>(s => new VillagerSystem(
            s.Get<EventBus>(), s.Get<ItemManager>(), s.Get<GatingLimits>(), s.Get<ResearchManager>()));

        _services.RegisterSingleton<TileManager>(s => new TileManager(s.Get<TerrainGrid>()));
        _services.RegisterSingleton<EntityManager>(s => new EntityManager(s.Get<TileManager>(), s.Get<VillagerSystem>()));

        _services.RegisterSingleton<PathTrafficSystem>(s =>
        {
            var pt = new PathTrafficSystem(s.Get<TrafficManager>(), s.Get<EntityManager>(), s.Get<TileManager>(), s.Get<EventBus>());
            // PathGateManager / VillagerSystem are not upstream of PathTrafficSystem —
            // verified by cycle analysis — so resolving them here is safe.
            pt.SetPathGateManager(s.Get<PathGateManager>());
            pt.SetVillagerSystem(s.Get<VillagerSystem>());
            return pt;
        });

        _services.RegisterSingleton<TutorialSystem>(s =>
        {
            var t = new TutorialSystem(s.Get<EventBus>(), s.Get<ProtoFactory>());
            t.SetEntityManager(s.Get<EntityManager>());
            return t;
        });

        _services.RegisterSingleton<AutoEquipSystem>(s => new AutoEquipSystem(
            s.Get<EventBus>(), s.Get<ClassRegistry>(), s.Get<EntityManager>()));
        _services.RegisterSingleton<DungeonResolver>(s => new DungeonResolver(
            s.Get<DungeonRegistry>(), s.Get<ClassRegistry>(), s.Get<EventBus>(), s.Get<EntityManager>()));
        _services.RegisterSingleton<FusionCalculator>(s => new FusionCalculator(
            s.Get<ClassRegistry>(), s.Get<ItemRegistry>(), s.Get<ItemManager>(), s.Get<EventBus>(), s.Get<EntityManager>()));

        _services.RegisterSingleton<AppearanceApplier>(s =>
        {
            var a = new AppearanceApplier(s.Get<EventBus>(), s.Get<EntityManager>());
            a.SetRegistry(s.Get<AppearanceRegistry>());
            return a;
        });

        _services.RegisterSingleton<PathNodeManager>(s => new PathNodeManager(
            s.Get<EntityManager>(),
            s.Get<TileManager>(),
            s.Get<VillagerSystem>(),
            s.Get<PathTrafficSystem>(),
            s.Get<EventBus>(),
            s.Get<CommandBus>(),
            s.Get<TutorialSystem>(),
            s.Get<GatingLimits>(),
            s.Get<ResearchManager>(),
            s.Get<ItemManager>()));

        _services.RegisterSingleton<PathGateManager>(s => new PathGateManager(
            s.Get<EntityManager>(),
            s.Get<TileManager>(),
            s.Get<EventBus>(),
            s.Get<CommandBus>(),
            s.Get<TutorialSystem>(),
            s.Get<ItemManager>(),
            s.Get<ResearchManager>()));

        _services.RegisterSingleton<StructureManager>(s => new StructureManager(
            s.Get<EntityManager>(),
            s.Get<PathGateManager>(),
            s.Get<PathNodeManager>(),
            s.Get<VillagerSystem>(),
            s.Get<ItemManager>(),
            s.Get<ItemRegistry>(),
            s.Get<RecipeRegistry>(),
            s.Get<GatingLimits>(),
            s.Get<EventBus>(),
            s.Get<CommandBus>(),
            s.Get<TutorialSystem>(),
            s.Get<ResearchManager>()));

        _services.RegisterSingleton<DungeonManager>(s => new DungeonManager(
            s.Get<EntityManager>(),
            s.Get<PathGateManager>(),
            s.Get<VillagerSystem>(),
            s.Get<ItemManager>(),
            s.Get<EventBus>()));

        _services.RegisterSingleton<WorldStateManager>(s => new WorldStateManager(
            s.Get<ResearchManager>(),
            s.Get<EntityManager>(),
            s.Get<ItemManager>(),
            s.Get<EventBus>()));

        // ── Simulation ticker ──────────────────────────────────────
        _services.RegisterSingleton<SimulationTicker>(s =>
        {
            var sim = new SimulationTicker(
                s.Get<EventBus>(),
                s.Get<CommandBus>(),
                s.Get<PathTrafficSystem>(),
                s.Get<VillagerSystem>(),
                s.Get<TutorialSystem>(),
                s.Get<EntityManager>(),
                s.Get<TileManager>(),
                s.Get<PathNodeManager>(),
                s.Get<PathGateManager>(),
                s.Get<ItemManager>(),
                s.Get<GatingLimits>(),
                s.Get<StructureManager>(),
                s.Get<ResearchManager>(),
                s.Get<WorldStateManager>(),
                s.Get<AppearanceApplier>(),
                s.Get<DungeonResolver>(),
                s.Get<DungeonManager>());
            s.Get<TutorialSystem>().Initialize(s.Get<ProtoRegistry>());
            return sim;
        });

        // ── Game-flow / UI-adjacent services ───────────────────────
        _services.RegisterSingleton<SaveManager>(_ => new SaveManager());
        _services.RegisterSingleton<GameStateMachine>(s => new GameStateMachine(s.Get<EventBus>(), s.Get<SimulationTicker>()));
        _services.RegisterSingleton<GameStatistics>(s =>
        {
            var stats = new GameStatistics();
            stats.Initialize(s.Get<EventBus>());
            return stats;
        });
        _services.RegisterSingleton<AchievementSystem>(s =>
        {
            var a = new AchievementSystem(s.Get<EventBus>());
            a.RegisterDefaults();
            a.Initialize();
            return a;
        });
        _services.RegisterSingleton<SettingsManager>(s => new SettingsManager(s.Get<EventBus>()));
        _services.RegisterSingleton<TranslationService>(_ =>
        {
            var t = new TranslationService();
            t.LoadFromEmbeddedJson();
            return t;
        });
        _services.RegisterSingleton<ThemeRegistry>(_ =>
        {
            var r = new ThemeRegistry();
            r.RegisterDefaults();
            r.LoadFromEmbeddedResources();
            return r;
        });
        _services.RegisterSingleton<ThemeService>(s =>
        {
            var t = new ThemeService(s.Get<ThemeRegistry>(), s.Get<EventBus>());
            t.SetTheme(s.Get<SettingsManager>().Settings.ThemeId);
            return t;
        });
        _services.RegisterSingleton<GameplayFlowSystem>(s => new GameplayFlowSystem(
            s.Get<EventBus>(), s.Get<SimulationTicker>(), s.Get<TutorialSystem>()));
        _services.RegisterSingleton<HotbarSystem>(s => new HotbarSystem(s.Get<EventBus>()));
    }

    /// <summary>
    /// Regenerates terrain and resets simulation state for a new game with the given settings.
    /// </summary>
    public void ApplyNewGameSettings(NewGameSettings settings)
    {
        LastNewGameSettings = settings;

        // Reset entity ID counter for fresh game (determinism)
        EntityIdFactory.ResetForNewGame();

        var tile = _services.Get<TileManager>();
        var bus = _services.Get<EventBus>();
        var entities = _services.Get<EntityManager>();
        var items = _services.Get<ItemManager>();
        var sim = _services.Get<SimulationTicker>();

        // Regenerate terrain with new seed and reset all entity state
        tile.RegenerateTerrain(settings.Seed);
        bus.Publish(new TerrainGeneratedEvent(tile.Width, tile.Height));

        // Reset simulation state
        entities.Clear();

        // Compute starting gold using the same difficulty curve as resources
        float difficultyMultiplier = settings.Difficulty switch
        {
            Difficulty.Casual => 3.0f,
            Difficulty.Easy => 2.0f,
            _ => 1.0f
        };
        int startingGold = (int)(100 * settings.StartingResourcesMultiplier * difficultyMultiplier);

        // Initialize all virtual stocks (wood, ore, food, gold) via ItemManager
        items.InitializeStartingStocks(settings.Difficulty, settings.StartingResourcesMultiplier, startingGold);

        // Phase 10: Set up guild data
        var guild = new GuildData
        {
            GuildName = settings.GuildName,
            BannerId = settings.BannerId,
            LogoId = settings.LogoId
        };
        sim.Guild = guild;

        // Phase 10: Create worker lifecycle system with difficulty
        var lifecycle = new WorkerLifecycleSystem(bus, entities, settings.Difficulty, settings.Seed);
        sim.SetWorkerLifecycle(lifecycle);

        bus.Publish(new GuildCreatedEvent(guild.GuildName, guild.BannerId));

        // Re-initialize tutorials
        sim.TutorialSystem.Initialize(_services.Get<ProtoRegistry>());

        bus.Publish(new NewGameStartedEvent(settings.GameName, settings.Difficulty, settings.Seed));
    }

    /// <summary>
    /// Generates a preview-only terrain grid for the new game setup map preview.
    /// Does NOT modify the active terrain. Pure function.
    /// </summary>
    public static TerrainGrid GeneratePreviewTerrain(int seed, int width = DefaultMapWidth, int height = DefaultMapHeight)
    {
        var preview = new TerrainGrid(width, height);
        preview.GenerateDefault(seed);
        return preview;
    }
}
