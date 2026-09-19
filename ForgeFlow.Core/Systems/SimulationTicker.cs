using ForgeFlow.Core.Commands;
using ForgeFlow.Core.Events;
using ForgeFlow.Core.Utilities;

namespace ForgeFlow.Core.Systems;

public sealed class SimulationTicker : IGameSystem
{
    public const float FixedTimeStep = 1f / 60f;

    private readonly EventBus _eventBus;
    private readonly CommandBus _commandBus;
    private readonly PathTrafficSystem _pathTraffic;
    private readonly VillagerSystem _villagerSystem;
    private readonly TutorialSystem _tutorialSystem;
    private readonly EntityManager _entityManager;
    private readonly TileManager _tileManager;
    private readonly PathNodeManager _pathNodeManager;
    private readonly PathGateManager _pathGateManager;
    private readonly ItemManager _itemManager;
    private readonly StructureManager _structureManager;
    private readonly ResearchManager _researchManager;
    private readonly WorldStateManager _worldStateManager;
    private readonly AppearanceApplier _appearanceApplier;
    private readonly DungeonResolver _dungeonResolver;
    private readonly DungeonManager _dungeonManager;

    private float _accumulator;
    private ulong _tickCount;
    private bool _isPaused;

    /// <summary>Maximum number of fixed-step ticks per frame to prevent death spirals.</summary>
    public int MaxCatchUpTicksPerFrame { get; set; } = 5;

    /// <summary>Optional profiler for performance markers. Defaults to NullProfiler (zero overhead).</summary>
    public IProfiler Profiler { get; set; } = NullProfiler.Instance;

    private WorkerLifecycleSystem? _workerLifecycle;

    // ── Manager Accessors (the only public API for reaching subsystems) ──
    public ulong TickCount => _tickCount;
    public bool IsPaused => _isPaused;
    public GatingLimits Gating { get; }
    public VillagerSystem VillagerSystem => _villagerSystem;
    public TutorialSystem TutorialSystem => _tutorialSystem;
    public DungeonResolver DungeonResolver => _dungeonResolver;
    public DungeonManager DungeonManager => _dungeonManager;
    public PathTrafficSystem PathTraffic => _pathTraffic;
    public WorkerLifecycleSystem? WorkerLifecycle => _workerLifecycle;
    public EntityManager EntityManager => _entityManager;
    public TileManager TileManager => _tileManager;
    public PathNodeManager PathNodeManager => _pathNodeManager;
    public PathGateManager PathGateManager => _pathGateManager;
    public ItemManager ItemManager => _itemManager;
    public StructureManager StructureManager => _structureManager;
    public ResearchManager ResearchManager => _researchManager;
    public WorldStateManager WorldStateManager => _worldStateManager;
    public AppearanceApplier AppearanceApplier => _appearanceApplier;
    public CommandBus CommandBus => _commandBus;

    /// <summary>The player's guild data. Set during new game setup.</summary>
    public Data.GuildData? Guild { get; set; }

    /// <summary>Sets the worker lifecycle system (created after difficulty is known).</summary>
    public void SetWorkerLifecycle(WorkerLifecycleSystem lifecycle)
    {
        _workerLifecycle?.Dispose();
        _workerLifecycle = lifecycle;
        _dungeonResolver.LifecycleSystem = lifecycle;
    }

    public SimulationTicker(
        EventBus eventBus,
        CommandBus commandBus,
        PathTrafficSystem pathTraffic,
        VillagerSystem villagerSystem,
        TutorialSystem tutorialSystem,
        EntityManager entityManager,
        TileManager tileManager,
        PathNodeManager pathNodeManager,
        PathGateManager pathGateManager,
        ItemManager itemManager,
        GatingLimits gatingLimits,
        StructureManager structureManager,
        ResearchManager researchManager,
        WorldStateManager worldStateManager,
        AppearanceApplier appearanceApplier,
        DungeonResolver dungeonResolver,
        DungeonManager dungeonManager)
    {
        _eventBus = eventBus;
        _commandBus = commandBus;
        _pathTraffic = pathTraffic;
        _villagerSystem = villagerSystem;
        _tutorialSystem = tutorialSystem;
        _entityManager = entityManager;
        _tileManager = tileManager;
        _pathNodeManager = pathNodeManager;
        _pathGateManager = pathGateManager;
        _itemManager = itemManager;
        Gating = gatingLimits;
        _structureManager = structureManager;
        _researchManager = researchManager;
        _worldStateManager = worldStateManager;
        _appearanceApplier = appearanceApplier;
        _dungeonResolver = dungeonResolver;
        _dungeonManager = dungeonManager;
    }

    public void Update(float deltaTime)
    {
        if (_isPaused) { return; }

        _accumulator += deltaTime;

        for (int i = 0; i < MaxCatchUpTicksPerFrame && _accumulator >= FixedTimeStep; i++)
        {
            _accumulator -= FixedTimeStep;
            FixedTick(FixedTimeStep);
            _tickCount++;
        }

        if (_accumulator >= FixedTimeStep)
        {
            int droppedTicks = (int)(_accumulator / FixedTimeStep);
            _accumulator %= FixedTimeStep;
            _eventBus.Publish(new SimulationLagDroppedEvent(droppedTicks));
        }
    }

    private void FixedTick(float dt)
    {
        using var _ = new ProfilerScope(Profiler, "SimulationTicker.FixedTick");
        // Tag any commands dispatched during this tick with the current tick count
        // so CommandLog replay is deterministic (#17).
        _commandBus.Log?.SetTick(_tickCount);
        _eventBus.Publish(new SimulationEarlyTickEvent(dt));
        _eventBus.Publish(new SimulationTickEvent(dt));
        _eventBus.Publish(new SimulationLateTickEvent(dt));
    }

    public void Pause() => _isPaused = true;
    public void Resume() => _isPaused = false;
    public void TogglePause() => _isPaused = !_isPaused;
}
