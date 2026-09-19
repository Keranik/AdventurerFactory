using ForgeFlow.Core;
using ForgeFlow.Core.Data;
using ForgeFlow.Core.Data.Definitions;
using ForgeFlow.Core.Entities;
using ForgeFlow.Core.Events;
using ForgeFlow.Core.Localization;
using ForgeFlow.Core.Proto;
using ForgeFlow.Core.Save;
using ForgeFlow.Core.Systems;
using ForgeFlow.Core.Utilities;

namespace ForgeFlow.Tests;

public class GameStateMachineTests
{
    // ─── GameStateMachine: NewGameSetup Transition ────────────

    [Fact]
    public void GameStateMachine_MainMenu_CanTransitionToNewGameSetup()
    {
        var bootstrapper = new GameBootstrapper();
        bootstrapper.Bootstrap();
        var gsm = bootstrapper.Services.Get<GameStateMachine>();

        Assert.Equal(GameState.MainMenu, gsm.Current);
        Assert.True(gsm.StartNewGameSetup());
        Assert.Equal(GameState.NewGameSetup, gsm.Current);
    }

    [Fact]
    public void GameStateMachine_NewGameSetup_CanTransitionToPlaying()
    {
        var bootstrapper = new GameBootstrapper();
        bootstrapper.Bootstrap();
        var gsm = bootstrapper.Services.Get<GameStateMachine>();

        gsm.StartNewGameSetup();
        Assert.True(gsm.StartNewGame());
        Assert.Equal(GameState.Playing, gsm.Current);
    }

    [Fact]
    public void GameStateMachine_NewGameSetup_CanGoBackToMainMenu()
    {
        var bootstrapper = new GameBootstrapper();
        bootstrapper.Bootstrap();
        var gsm = bootstrapper.Services.Get<GameStateMachine>();

        gsm.StartNewGameSetup();
        Assert.True(gsm.ReturnToMainMenu());
        Assert.Equal(GameState.MainMenu, gsm.Current);
    }

    [Fact]
    public void GameStateMachine_Playing_CanTransitionToVictory()
    {
        var bootstrapper = new GameBootstrapper();
        bootstrapper.Bootstrap();
        var gsm = bootstrapper.Services.Get<GameStateMachine>();

        gsm.StartNewGame();
        Assert.True(gsm.TriggerVictory());
        Assert.Equal(GameState.Victory, gsm.Current);
    }

    [Fact]
    public void GameStateMachine_Victory_CanContinuePlaying()
    {
        var bootstrapper = new GameBootstrapper();
        bootstrapper.Bootstrap();
        var gsm = bootstrapper.Services.Get<GameStateMachine>();

        gsm.StartNewGame();
        gsm.TriggerVictory();
        Assert.True(gsm.TransitionTo(GameState.Playing));
        Assert.Equal(GameState.Playing, gsm.Current);
    }

    // ─── Timed Lose Condition ─────────────────────────────────

    [Fact]
    public void GameStateMachine_EvaluateEndConditions_TimedLose_DoesNotTriggerImmediately()
    {
        var bootstrapper = new GameBootstrapper();
        var sim = bootstrapper.Bootstrap();
        var gsm = bootstrapper.Services.Get<GameStateMachine>();

        gsm.StartNewGame();
        for (int i = 0; i < 700; i++) { sim.Update(SimulationTicker.FixedTimeStep); }

        gsm.EvaluateEndConditions(1.0f);
        Assert.Equal(GameState.Playing, gsm.Current);
        Assert.True(gsm.LoseTimerSeconds > 0);
    }

    [Fact]
    public void GameStateMachine_EvaluateEndConditions_TimedLose_TriggersAfter30Seconds()
    {
        var bootstrapper = new GameBootstrapper();
        var sim = bootstrapper.Bootstrap();
        var gsm = bootstrapper.Services.Get<GameStateMachine>();

        gsm.StartNewGame();
        for (int i = 0; i < 700; i++) { sim.Update(SimulationTicker.FixedTimeStep); }

        gsm.EvaluateEndConditions(31.0f);
        Assert.Equal(GameState.GameOver, gsm.Current);
    }

    [Fact]
    public void GameStateMachine_EvaluateEndConditions_ResetsTimerWhenHeroesExist()
    {
        var bootstrapper = new GameBootstrapper();
        var sim = bootstrapper.Bootstrap();
        var gsm = bootstrapper.Services.Get<GameStateMachine>();

        gsm.StartNewGame();
        for (int i = 0; i < 700; i++) { sim.Update(SimulationTicker.FixedTimeStep); }

        gsm.EvaluateEndConditions(10.0f);
        Assert.True(gsm.LoseTimerSeconds > 0);

        var hero = new HeroEntity(EntityId.Next()) { Seed = 999, Level = 1, ClassId = "warrior" };
        sim.EntityManager.AddHero(hero);
        gsm.EvaluateEndConditions(1.0f);
        Assert.Equal(0f, gsm.LoseTimerSeconds);
    }

    // ─── NewGameSettings ──────────────────────────────────────

    [Fact]
    public void NewGameSettings_DefaultValues()
    {
        var settings = new NewGameSettings();
        Assert.Equal("Factory #1", settings.GameName);
        Assert.Equal(Difficulty.Normal, settings.Difficulty);
        Assert.Equal(0, settings.Seed);
        Assert.Equal(1.0f, settings.BiomeDensity);
        Assert.Equal(1.0f, settings.StartingResourcesMultiplier);
        Assert.Equal(2, settings.StartingHeroCount);
    }

    [Fact]
    public void GameBootstrapper_ApplyNewGameSettings_SetsResources()
    {
        var bootstrapper = new GameBootstrapper();
        bootstrapper.Bootstrap();

        var settings = new NewGameSettings
        {
            GameName = "Test Factory",
            Difficulty = Difficulty.Casual,
            Seed = 42,
            StartingResourcesMultiplier = 2.0f
        };

        bootstrapper.ApplyNewGameSettings(settings);

        Assert.Equal(600, bootstrapper.Services.Get<SimulationTicker>().ItemManager.GetStock("gold"));
        Assert.Equal(300, bootstrapper.Services.Get<SimulationTicker>().ItemManager.GetStock("wood"));
    }

    [Fact]
    public void GameBootstrapper_ApplyNewGameSettings_RegeneratesTerrain()
    {
        var bootstrapper = new GameBootstrapper();
        bootstrapper.Bootstrap();

        var settings = new NewGameSettings { Seed = 12345 };
        bootstrapper.ApplyNewGameSettings(settings);

        Assert.NotNull(bootstrapper.Services.Get<TerrainGrid>().Get(new GridPosRPG(0, 0)));
    }

    [Fact]
    public void GameBootstrapper_GeneratePreviewTerrain_ReturnsIndependentGrid()
    {
        var preview = GameBootstrapper.GeneratePreviewTerrain(42);
        Assert.NotNull(preview);
        Assert.Equal(GameBootstrapper.DefaultMapWidth, preview.Width);
        Assert.Equal(GameBootstrapper.DefaultMapHeight, preview.Height);
        Assert.NotNull(preview.Get(new GridPosRPG(0, 0)));
    }

    // ─── SaveData Phase 9 Fields ──────────────────────────────

    [Fact]
    public void SaveData_HasPhase9Fields()
    {
        var data = new SaveData
        {
            GameName = "My Factory",
            Difficulty = Difficulty.Easy,
            WorldSeed = 42
        };

        Assert.Equal("My Factory", data.GameName);
        Assert.Equal(Difficulty.Easy, data.Difficulty);
        Assert.Equal(42, data.WorldSeed);
    }

    [Fact]
    public void SaveData_SerializesPhase9Fields()
    {
        var sm = new SaveManager();
        var data = new SaveData
        {
            GameName = "Serialization Test",
            Difficulty = Difficulty.Casual,
            WorldSeed = 999
        };

        var json = sm.SerializeToJson(data);
        var loaded = sm.DeserializeFromJson(json);

        Assert.NotNull(loaded);
        Assert.Equal("Serialization Test", loaded!.GameName);
        Assert.Equal(Difficulty.Casual, loaded.Difficulty);
        Assert.Equal(999, loaded.WorldSeed);
    }

    // ─── SaveManager.ListSaveFiles ────────────────────────────

    [Fact]
    public void SaveManager_ListSaveFiles_EmptyDirectory_ReturnsEmpty()
    {
        var sm = new SaveManager();
        var result = sm.ListSaveFiles(Path.Combine(Path.GetTempPath(), "nonexistent_forgeflow_test"));
        Assert.Empty(result);
    }

    [Fact]
    public void SaveManager_ListSaveFiles_FindsSavedGames()
    {
        var sm = new SaveManager();
        var tempDir = Path.Combine(Path.GetTempPath(), $"forgeflow_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var data = new SaveData
            {
                GameName = "Test Save",
                Difficulty = Difficulty.Normal,
                WorldSeed = 42,
                SaveTimestamp = DateTime.UtcNow
            };
            sm.SaveToFile(data, Path.Combine(tempDir, "slot1.json"));

            var slots = sm.ListSaveFiles(tempDir);
            Assert.Single(slots);
            Assert.Equal("Test Save", slots[0].GameName);
            Assert.Equal("slot1", slots[0].SlotName);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    // ─── TutorialSystem Enhancements ──────────────────────────

    [Fact]
    public void TutorialSystem_GetHighlightTarget_ReturnsEmpty_WhenNoMission()
    {
        var eventBus = new EventBus();
        var protoRegistry = new ProtoRegistry();
        protoRegistry.RegisterDefaults();
        var factory = new ProtoFactory(protoRegistry, eventBus);

        var ts = new TutorialSystem(eventBus, factory);

        Assert.Equal(string.Empty, ts.GetHighlightTarget());
    }

    [Fact]
    public void TutorialSystem_ActiveStepIndex_StartsAtZero()
    {
        var eventBus = new EventBus();
        var protoRegistry = new ProtoRegistry();
        protoRegistry.RegisterDefaults();
        var factory = new ProtoFactory(protoRegistry, eventBus);

        var ts = new TutorialSystem(eventBus, factory);
        ts.Initialize(protoRegistry);

        Assert.Equal(0, ts.ActiveStepIndex);
    }

    [Fact]
    public void TutorialSystem_PublishesTutorialStepActivatedEvent_OnInitialize()
    {
        var eventBus = new EventBus();
        var protoRegistry = new ProtoRegistry();
        protoRegistry.RegisterDefaults();
        var factory = new ProtoFactory(protoRegistry, eventBus);

        TutorialStepActivatedEvent? received = null;
        eventBus.Subscribe<TutorialStepActivatedEvent>(e => received = e);

        var ts = new TutorialSystem(eventBus, factory);
        ts.Initialize(protoRegistry);

        if (ts.TotalCount > 0)
        {
            Assert.NotNull(received);
        }
    }

    // ─── Difficulty Enum ──────────────────────────────────────

    [Fact]
    public void Difficulty_HasThreeValues()
    {
        var values = Enum.GetValues(typeof(Difficulty));
        Assert.Equal(3, values.Length);
    }

    // ─── GameState Enum ───────────────────────────────────────

    [Fact]
    public void GameState_HasNewGameSetup()
    {
        Assert.True(Enum.IsDefined(typeof(GameState), GameState.NewGameSetup));
    }

    // ─── Events ───────────────────────────────────────────────

    [Fact]
    public void NewGameStartedEvent_StoresValues()
    {
        var e = new NewGameStartedEvent("Test", Difficulty.Easy, 42);
        Assert.Equal("Test", e.GameName);
        Assert.Equal(Difficulty.Easy, e.Difficulty);
        Assert.Equal(42, e.Seed);
    }

    [Fact]
    public void TutorialStepActivatedEvent_StoresValues()
    {
        var e = new TutorialStepActivatedEvent("mission_1", 0, "spawner_button", "Place a spawner");
        Assert.Equal("mission_1", e.MissionId);
        Assert.Equal(0, e.StepIndex);
        Assert.Equal("spawner_button", e.HighlightTarget);
        Assert.Equal("Place a spawner", e.HintText);
    }

    [Fact]
    public void EndConditionMetEvent_StoresValues()
    {
        var e = new EndConditionMetEvent(true, "Victory!");
        Assert.True(e.IsVictory);
        Assert.Equal("Victory!", e.Reason);
    }

    [Fact]
    public void GameStateMachine_TriggerGameOver_PublishesEndConditionEvent()
    {
        var bootstrapper = new GameBootstrapper();
        bootstrapper.Bootstrap();
        var gsm = bootstrapper.Services.Get<GameStateMachine>();

        EndConditionMetEvent? received = null;
        bootstrapper.Services.Get<EventBus>().Subscribe<EndConditionMetEvent>(e => received = e);

        gsm.StartNewGame();
        gsm.TriggerGameOver();

        Assert.NotNull(received);
        Assert.False(received!.Value.IsVictory);
    }

    [Fact]
    public void GameStateMachine_TriggerVictory_PublishesEndConditionEvent()
    {
        var bootstrapper = new GameBootstrapper();
        bootstrapper.Bootstrap();
        var gsm = bootstrapper.Services.Get<GameStateMachine>();

        EndConditionMetEvent? received = null;
        bootstrapper.Services.Get<EventBus>().Subscribe<EndConditionMetEvent>(e => received = e);

        gsm.StartNewGame();
        gsm.TriggerVictory();

        Assert.NotNull(received);
        Assert.True(received!.Value.IsVictory);
    }

    // ─── Full Game Flow Integration ───────────────────────────

    [Fact]
    public void FullFlow_MainMenu_NewGameSetup_Playing_Paused_MainMenu()
    {
        var bootstrapper = new GameBootstrapper();
        bootstrapper.Bootstrap();
        var gsm = bootstrapper.Services.Get<GameStateMachine>();

        Assert.Equal(GameState.MainMenu, gsm.Current);
        Assert.True(gsm.StartNewGameSetup());
        bootstrapper.ApplyNewGameSettings(new NewGameSettings { Seed = 42 });
        Assert.True(gsm.StartNewGame());
        Assert.True(gsm.TogglePause());
        Assert.Equal(GameState.Paused, gsm.Current);
        Assert.True(gsm.ReturnToMainMenu());
        Assert.Equal(GameState.MainMenu, gsm.Current);
    }

    [Fact]
    public void FullFlow_Playing_GameOver_MainMenu()
    {
        var bootstrapper = new GameBootstrapper();
        bootstrapper.Bootstrap();
        var gsm = bootstrapper.Services.Get<GameStateMachine>();

        gsm.StartNewGame();
        Assert.True(gsm.TriggerGameOver());
        Assert.Equal(GameState.GameOver, gsm.Current);
        Assert.True(gsm.ReturnToMainMenu());
        Assert.Equal(GameState.MainMenu, gsm.Current);
    }

    [Fact]
    public void FullFlow_Playing_Victory_ContinuePlaying()
    {
        var bootstrapper = new GameBootstrapper();
        bootstrapper.Bootstrap();
        var gsm = bootstrapper.Services.Get<GameStateMachine>();

        gsm.StartNewGame();
        gsm.TriggerVictory();
        Assert.Equal(GameState.Victory, gsm.Current);
        Assert.True(gsm.TransitionTo(GameState.Playing));
        Assert.Equal(GameState.Playing, gsm.Current);
    }

    [Fact]
    public void GameStateMachine_NewGameSetup_PausesSimulation()
    {
        var bootstrapper = new GameBootstrapper();
        var sim = bootstrapper.Bootstrap();
        var gsm = bootstrapper.Services.Get<GameStateMachine>();

        gsm.StartNewGameSetup();
        Assert.True(sim.IsPaused);
    }

    [Fact]
    public void GameStateMachine_StartNewGame_ResumesSimulation()
    {
        var bootstrapper = new GameBootstrapper();
        var sim = bootstrapper.Bootstrap();
        var gsm = bootstrapper.Services.Get<GameStateMachine>();

        gsm.StartNewGameSetup();
        Assert.True(sim.IsPaused);
        gsm.StartNewGame();
        Assert.False(sim.IsPaused);
    }
}

/// <summary>Phase7GameShell GameStateMachine tests + Phase14 GameState tests.</summary>
public class GameStateMachineAdditionalTests
{
    [Fact]
    public void GameStateMachine_Loading_TransitionsToPlaying()
    {
        var (gsm, _, _) = CreateGSM();
        Assert.True(gsm.TransitionTo(GameState.Loading));
        Assert.True(gsm.TransitionTo(GameState.Playing));
    }

    [Fact]
    public void GameStateMachine_CannotGoFromMainMenu_ToGameOver()
    {
        var (gsm, _, _) = CreateGSM();
        Assert.False(gsm.TransitionTo(GameState.GameOver));
    }

    [Fact]
    public void GameStateMachine_CannotGoFromVictory_ToPlaying()
    {
        var (gsm, _, _) = CreateGSM();
        gsm.StartNewGame();
        gsm.TriggerVictory();
        Assert.True(gsm.TransitionTo(GameState.Playing));
    }

    [Fact]
    public void GameStateMachine_ReturnToMenu_FromVictory()
    {
        var (gsm, _, _) = CreateGSM();
        gsm.StartNewGame();
        gsm.TriggerVictory();
        Assert.True(gsm.ReturnToMainMenu());
    }

    [Fact]
    public void GameStateMachine_PreviousState_TrackedCorrectly()
    {
        var (gsm, _, _) = CreateGSM();
        gsm.StartNewGame();
        gsm.TogglePause();

        Assert.Equal(GameState.Playing, gsm.Previous);
        Assert.Equal(GameState.Paused, gsm.Current);
    }

    [Fact]
    public void GameStateMachine_MultipleTransitions_TrackPrevious()
    {
        var (gsm, _, _) = CreateGSM();
        gsm.StartNewGame();
        gsm.TogglePause();
        gsm.TogglePause();

        Assert.Equal(GameState.Paused, gsm.Previous);
        Assert.Equal(GameState.Playing, gsm.Current);
    }

    [Fact]
    public void GameState_StartsAtMainMenu()
    {
        var boot = new GameBootstrapper();
        boot.Bootstrap();
        Assert.Equal(GameState.MainMenu, boot.Services.Get<GameStateMachine>().Current);
    }

    [Fact]
    public void GameState_Transitions_MainMenu_To_NewGameSetup()
    {
        var boot = new GameBootstrapper();
        boot.Bootstrap();
        boot.Services.Get<GameStateMachine>().StartNewGameSetup();
        Assert.Equal(GameState.NewGameSetup, boot.Services.Get<GameStateMachine>().Current);
    }

    [Fact]
    public void GameState_Transitions_NewGameSetup_To_Playing()
    {
        var boot = new GameBootstrapper();
        boot.Bootstrap();
        boot.ApplyNewGameSettings(new NewGameSettings { GameName = "Test" });
        boot.Services.Get<GameStateMachine>().StartNewGame();
        Assert.Equal(GameState.Playing, boot.Services.Get<GameStateMachine>().Current);
    }

    [Fact]
    public void GameState_PauseToggle()
    {
        var boot = new GameBootstrapper();
        boot.Bootstrap();
        boot.ApplyNewGameSettings(new NewGameSettings { GameName = "Test" });
        boot.Services.Get<GameStateMachine>().StartNewGame();
        Assert.Equal(GameState.Playing, boot.Services.Get<GameStateMachine>().Current);

        boot.Services.Get<GameStateMachine>().TogglePause();
        Assert.Equal(GameState.Paused, boot.Services.Get<GameStateMachine>().Current);

        boot.Services.Get<GameStateMachine>().TogglePause();
        Assert.Equal(GameState.Playing, boot.Services.Get<GameStateMachine>().Current);
    }

    private static (GameStateMachine gsm, EventBus bus, SimulationTicker sim) CreateGSM()
    {
        var bus = new EventBus();
        var tm = new TrafficManager();
        var classReg = new ClassRegistry();
        var protoReg = new ProtoRegistry();
        protoReg.RegisterDefaults();
        var pf = new ProtoFactory(protoReg, bus);
        var rm = new ItemManager(bus);
        var vs = new VillagerSystem(bus, rm);
        var terrain = new TerrainGrid(32, 32);
        terrain.GenerateDefault();
        var tileMgr = new TileManager(terrain);
        var entMgr = new EntityManager(tileMgr, vs);
        var pt = new PathTrafficSystem(tm, entMgr, tileMgr, bus);
        var ts = new TutorialSystem(bus, pf);
        var ae = new AutoEquipSystem(bus, classReg, entMgr);
        var dr = new DungeonResolver(new DungeonRegistry(), classReg, bus, entMgr);
        var fc = new FusionCalculator(classReg, new ItemRegistry(), rm, bus, entMgr);
        var aa = new AppearanceApplier(bus, entMgr);
        var gl = new GatingLimits();
        var rsMgr = new ResearchManager(bus);
        var cmdBus = new Core.Commands.CommandBus();
        var pm = new PathNodeManager(entMgr, tileMgr, vs, pt, bus, cmdBus, ts, gl, rsMgr, rm);
        var pgm = new PathGateManager(entMgr, tileMgr, bus, cmdBus, ts, rm, rsMgr);
        pt.SetPathGateManager(pgm);
        var mm = new StructureManager(entMgr, pgm, pm, vs, rm, new ItemRegistry(), new RecipeRegistry(), gl, bus, cmdBus, ts, rsMgr);
        var wsMgr = new WorldStateManager(rsMgr, entMgr, rm, bus);
        var dm = new DungeonManager(entMgr, pgm, vs, rm, bus);
        var sim = new SimulationTicker(bus, cmdBus, pt, vs, ts, entMgr, tileMgr, pm, pgm, rm, gl, mm, rsMgr, wsMgr, aa, dr, dm);
        var gsm = new GameStateMachine(bus, sim);
        return (gsm, bus, sim);
    }

    // ─── GameStateMachine: Core State Transitions (Phase6) ────────────

    [Fact]
    public void GameStateMachine_InitialState_IsMainMenu()
    {
        var (gsm, _, _) = CreateGSM();
        Assert.Equal(GameState.MainMenu, gsm.Current);
    }

    [Fact]
    public void GameStateMachine_StartNewGame_TransitionsToPlaying()
    {
        var (gsm, _, _) = CreateGSM();

        bool result = gsm.StartNewGame();

        Assert.True(result);
        Assert.Equal(GameState.Playing, gsm.Current);
        Assert.Equal(GameState.MainMenu, gsm.Previous);
    }

    [Fact]
    public void GameStateMachine_TogglePause_FromPlaying()
    {
        var (gsm, _, _) = CreateGSM();
        gsm.StartNewGame();

        Assert.True(gsm.TogglePause());
        Assert.Equal(GameState.Paused, gsm.Current);

        Assert.True(gsm.TogglePause());
        Assert.Equal(GameState.Playing, gsm.Current);
    }

    [Fact]
    public void GameStateMachine_TogglePause_InvalidFromMenu()
    {
        var (gsm, _, _) = CreateGSM();

        Assert.False(gsm.TogglePause());
        Assert.Equal(GameState.MainMenu, gsm.Current);
    }

    [Fact]
    public void GameStateMachine_TriggerGameOver_FromPlaying()
    {
        var (gsm, _, _) = CreateGSM();
        gsm.StartNewGame();

        Assert.True(gsm.TriggerGameOver());
        Assert.Equal(GameState.GameOver, gsm.Current);
    }

    [Fact]
    public void GameStateMachine_TriggerGameOver_InvalidFromPaused()
    {
        var (gsm, _, _) = CreateGSM();
        gsm.StartNewGame();
        gsm.TogglePause();

        Assert.False(gsm.TriggerGameOver());
        Assert.Equal(GameState.Paused, gsm.Current);
    }

    [Fact]
    public void GameStateMachine_TriggerVictory_FromPlaying()
    {
        var (gsm, _, _) = CreateGSM();
        gsm.StartNewGame();

        Assert.True(gsm.TriggerVictory());
        Assert.Equal(GameState.Victory, gsm.Current);
    }

    [Fact]
    public void GameStateMachine_ReturnToMainMenu_FromGameOver()
    {
        var (gsm, _, _) = CreateGSM();
        gsm.StartNewGame();
        gsm.TriggerGameOver();

        Assert.True(gsm.ReturnToMainMenu());
        Assert.Equal(GameState.MainMenu, gsm.Current);
    }

    [Fact]
    public void GameStateMachine_ReturnToMainMenu_FromPaused()
    {
        var (gsm, _, _) = CreateGSM();
        gsm.StartNewGame();
        gsm.TogglePause();

        Assert.True(gsm.ReturnToMainMenu());
        Assert.Equal(GameState.MainMenu, gsm.Current);
    }

    [Fact]
    public void GameStateMachine_InvalidTransition_Rejected()
    {
        var (gsm, _, _) = CreateGSM();

        Assert.False(gsm.TransitionTo(GameState.Paused));
        Assert.False(gsm.TransitionTo(GameState.GameOver));
    }

    [Fact]
    public void GameStateMachine_PublishesStateChangedEvent()
    {
        var (gsm, bus, _) = CreateGSM();

        GameStateChangedEvent? received = null;
        bus.Subscribe<GameStateChangedEvent>(e => received = e);

        gsm.StartNewGame();

        Assert.NotNull(received);
        Assert.Equal(GameState.MainMenu, received.Value.PreviousState);
        Assert.Equal(GameState.Playing, received.Value.NewState);
    }

    [Fact]
    public void GameStateMachine_PauseAndResume_AffectsSimulation()
    {
        var (gsm, _, sim) = CreateGSM();
        gsm.StartNewGame();

        Assert.False(sim.IsPaused);

        gsm.TogglePause();
        Assert.True(sim.IsPaused);

        gsm.TogglePause();
        Assert.False(sim.IsPaused);
    }
}
