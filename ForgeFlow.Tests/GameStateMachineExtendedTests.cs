using ForgeFlow.Core;
using ForgeFlow.Core.Data;
using ForgeFlow.Core.Entities;
using ForgeFlow.Core.Events;
using ForgeFlow.Core.Localization;
using ForgeFlow.Core.Proto;
using ForgeFlow.Core.Proto.Prototypes;
using ForgeFlow.Core.Save;
using ForgeFlow.Core.Systems;

namespace ForgeFlow.Tests;

/// <summary>
/// Extended GameStateMachine tests: guild flow, tutorial wiring,
/// save/load integration, UIManager/window layout, full-journey flows.
/// </summary>
public class GameStateMachineExtendedTests
{
    // ─── GameStateMachine: Single Source of Truth ────────────────

    [Fact]
    public void GameStateMachine_StartsAtMainMenu()
    {
        var eventBus = new EventBus();
        var bootstrapper = new GameBootstrapper();
        var simulation = bootstrapper.Bootstrap("./TestMods");
        var gsm = new GameStateMachine(eventBus, simulation);

        Assert.Equal(GameState.MainMenu, gsm.Current);
    }

    [Fact]
    public void GameStateMachine_MainMenu_ToNewGameSetup()
    {
        var eventBus = new EventBus();
        var bootstrapper = new GameBootstrapper();
        var simulation = bootstrapper.Bootstrap("./TestMods");
        var gsm = new GameStateMachine(eventBus, simulation);

        bool result = gsm.StartNewGameSetup();
        Assert.True(result);
        Assert.Equal(GameState.NewGameSetup, gsm.Current);
        Assert.Equal(GameState.MainMenu, gsm.Previous);
    }

    [Fact]
    public void GameStateMachine_NewGameSetup_ToPlaying()
    {
        var eventBus = new EventBus();
        var bootstrapper = new GameBootstrapper();
        var simulation = bootstrapper.Bootstrap("./TestMods");
        var gsm = new GameStateMachine(eventBus, simulation);

        gsm.StartNewGameSetup();
        bool result = gsm.StartNewGame();
        Assert.True(result);
        Assert.Equal(GameState.Playing, gsm.Current);
    }

    [Fact]
    public void GameStateMachine_Playing_ToPaused_Toggle()
    {
        var eventBus = new EventBus();
        var bootstrapper = new GameBootstrapper();
        var simulation = bootstrapper.Bootstrap("./TestMods");
        var gsm = new GameStateMachine(eventBus, simulation);

        gsm.StartNewGameSetup();
        gsm.StartNewGame();
        gsm.TogglePause();
        Assert.Equal(GameState.Paused, gsm.Current);

        gsm.TogglePause();
        Assert.Equal(GameState.Playing, gsm.Current);
    }

    [Fact]
    public void GameStateMachine_Paused_ReturnToMainMenu()
    {
        var eventBus = new EventBus();
        var bootstrapper = new GameBootstrapper();
        var simulation = bootstrapper.Bootstrap("./TestMods");
        var gsm = new GameStateMachine(eventBus, simulation);

        gsm.StartNewGameSetup();
        gsm.StartNewGame();
        gsm.TogglePause();
        bool result = gsm.ReturnToMainMenu();
        Assert.True(result);
        Assert.Equal(GameState.MainMenu, gsm.Current);
    }

    [Fact]
    public void GameStateMachine_NewGameSetup_BackToMainMenu()
    {
        var eventBus = new EventBus();
        var bootstrapper = new GameBootstrapper();
        var simulation = bootstrapper.Bootstrap("./TestMods");
        var gsm = new GameStateMachine(eventBus, simulation);

        gsm.StartNewGameSetup();
        bool result = gsm.ReturnToMainMenu();
        Assert.True(result);
        Assert.Equal(GameState.MainMenu, gsm.Current);
    }

    [Fact]
    public void GameStateMachine_Playing_GameOver()
    {
        var eventBus = new EventBus();
        var bootstrapper = new GameBootstrapper();
        var simulation = bootstrapper.Bootstrap("./TestMods");
        var gsm = new GameStateMachine(eventBus, simulation);

        gsm.StartNewGameSetup();
        gsm.StartNewGame();
        bool result = gsm.TriggerGameOver();
        Assert.True(result);
        Assert.Equal(GameState.GameOver, gsm.Current);
    }

    [Fact]
    public void GameStateMachine_GameOver_ReturnToMainMenu()
    {
        var eventBus = new EventBus();
        var bootstrapper = new GameBootstrapper();
        var simulation = bootstrapper.Bootstrap("./TestMods");
        var gsm = new GameStateMachine(eventBus, simulation);

        gsm.StartNewGameSetup();
        gsm.StartNewGame();
        gsm.TriggerGameOver();
        bool result = gsm.ReturnToMainMenu();
        Assert.True(result);
        Assert.Equal(GameState.MainMenu, gsm.Current);
    }

    [Fact]
    public void GameStateMachine_Playing_Victory()
    {
        var eventBus = new EventBus();
        var bootstrapper = new GameBootstrapper();
        var simulation = bootstrapper.Bootstrap("./TestMods");
        var gsm = new GameStateMachine(eventBus, simulation);

        gsm.StartNewGameSetup();
        gsm.StartNewGame();
        bool result = gsm.TriggerVictory();
        Assert.True(result);
        Assert.Equal(GameState.Victory, gsm.Current);
    }

    [Fact]
    public void GameStateMachine_Victory_ContinuePlaying()
    {
        var eventBus = new EventBus();
        var bootstrapper = new GameBootstrapper();
        var simulation = bootstrapper.Bootstrap("./TestMods");
        var gsm = new GameStateMachine(eventBus, simulation);

        gsm.StartNewGameSetup();
        gsm.StartNewGame();
        gsm.TriggerVictory();
        bool result = gsm.TransitionTo(GameState.Playing);
        Assert.True(result);
        Assert.Equal(GameState.Playing, gsm.Current);
    }

    [Fact]
    public void GameStateMachine_Victory_ReturnToMainMenu()
    {
        var eventBus = new EventBus();
        var bootstrapper = new GameBootstrapper();
        var simulation = bootstrapper.Bootstrap("./TestMods");
        var gsm = new GameStateMachine(eventBus, simulation);

        gsm.StartNewGameSetup();
        gsm.StartNewGame();
        gsm.TriggerVictory();
        bool result = gsm.ReturnToMainMenu();
        Assert.True(result);
        Assert.Equal(GameState.MainMenu, gsm.Current);
    }

    [Fact]
    public void GameStateMachine_InvalidTransition_ReturnsFalse()
    {
        var eventBus = new EventBus();
        var bootstrapper = new GameBootstrapper();
        var simulation = bootstrapper.Bootstrap("./TestMods");
        var gsm = new GameStateMachine(eventBus, simulation);

        bool result = gsm.TogglePause();
        Assert.False(result);
        Assert.Equal(GameState.MainMenu, gsm.Current);
    }

    [Fact]
    public void GameStateMachine_PublishesStateChangedEvent()
    {
        var eventBus = new EventBus();
        var bootstrapper = new GameBootstrapper();
        var simulation = bootstrapper.Bootstrap("./TestMods");
        var gsm = new GameStateMachine(eventBus, simulation);

        GameStateChangedEvent? captured = null;
        eventBus.Subscribe<GameStateChangedEvent>(e => captured = e);

        gsm.StartNewGameSetup();
        Assert.NotNull(captured);
        Assert.Equal(GameState.MainMenu, captured.Value.PreviousState);
        Assert.Equal(GameState.NewGameSetup, captured.Value.NewState);
    }

    [Fact]
    public void GameStateMachine_LoseTimerResetsOnHeroPresence()
    {
        var eventBus = new EventBus();
        var bootstrapper = new GameBootstrapper();
        var simulation = bootstrapper.Bootstrap("./TestMods");
        var gsm = new GameStateMachine(eventBus, simulation);

        gsm.StartNewGameSetup();
        gsm.StartNewGame();
        simulation.EntityManager.AddHero(new HeroEntity(EntityId.Next()));
        gsm.EvaluateEndConditions(5.0f);
        Assert.Equal(0f, gsm.LoseTimerSeconds);
        Assert.Equal(GameState.Playing, gsm.Current);
    }

    [Fact]
    public void GameStateMachine_SideEffects_PausesOnMenuStates()
    {
        var eventBus = new EventBus();
        var bootstrapper = new GameBootstrapper();
        var simulation = bootstrapper.Bootstrap("./TestMods");
        var gsm = new GameStateMachine(eventBus, simulation);

        gsm.StartNewGameSetup();
        gsm.StartNewGame();
        Assert.False(simulation.IsPaused);
        gsm.TogglePause();
        Assert.True(simulation.IsPaused);
        gsm.TogglePause();
        Assert.False(simulation.IsPaused);
    }

    // ─── NewGameSettings: Guild Data Flow ────────────────────────

    [Fact]
    public void NewGameSettings_DefaultsIncludeGuildFields()
    {
        var settings = new NewGameSettings();
        Assert.Equal("Unnamed Guild", settings.GuildName);
        Assert.Equal("banner_default", settings.BannerId);
        Assert.Equal("logo_sword", settings.LogoId);
    }

    [Fact]
    public void ApplyNewGameSettings_CreatesGuildData()
    {
        var bootstrapper = new GameBootstrapper();
        bootstrapper.Bootstrap("./TestMods");

        var settings = new NewGameSettings
        {
            GuildName = "TestGuild",
            BannerId = "banner_dragon",
            LogoId = "logo_crown",
            Seed = 42
        };

        bootstrapper.ApplyNewGameSettings(settings);

        Assert.NotNull(bootstrapper.Services.Get<SimulationTicker>().Guild);
        Assert.Equal("TestGuild", bootstrapper.Services.Get<SimulationTicker>().Guild!.GuildName);
        Assert.Equal("banner_dragon", bootstrapper.Services.Get<SimulationTicker>().Guild.BannerId);
        Assert.Equal("logo_crown", bootstrapper.Services.Get<SimulationTicker>().Guild.LogoId);
    }

    [Fact]
    public void ApplyNewGameSettings_PublishesGuildCreatedEvent()
    {
        var bootstrapper = new GameBootstrapper();
        var simulation = bootstrapper.Bootstrap("./TestMods");

        GuildCreatedEvent? captured = null;
        bootstrapper.Services.Get<EventBus>().Subscribe<GuildCreatedEvent>(e => captured = e);

        bootstrapper.ApplyNewGameSettings(new NewGameSettings
        {
            GuildName = "MyGuild",
            BannerId = "banner_eagle",
            Seed = 7
        });

        Assert.NotNull(captured);
        Assert.Equal("MyGuild", captured.Value.GuildName);
        Assert.Equal("banner_eagle", captured.Value.BannerId);
    }

    [Fact]
    public void ApplyNewGameSettings_InitializesTutorialSystem()
    {
        var bootstrapper = new GameBootstrapper();
        bootstrapper.Bootstrap("./TestMods");
        bootstrapper.ApplyNewGameSettings(new NewGameSettings { Seed = 123 });

        var ts = bootstrapper.Services.Get<SimulationTicker>().TutorialSystem;
        Assert.True(ts.AllMissions.Count > 0);
    }

    [Fact]
    public void ApplyNewGameSettings_DifficultyAffectsResources()
    {
        var bootstrapper = new GameBootstrapper();
        bootstrapper.Bootstrap("./TestMods");

        bootstrapper.ApplyNewGameSettings(new NewGameSettings { Difficulty = Difficulty.Casual, Seed = 1 });
        int casualGold = bootstrapper.Services.Get<SimulationTicker>().ItemManager.GetStock("gold");

        bootstrapper.ApplyNewGameSettings(new NewGameSettings { Difficulty = Difficulty.Normal, Seed = 1 });
        int normalGold = bootstrapper.Services.Get<SimulationTicker>().ItemManager.GetStock("gold");

        Assert.True(casualGold > normalGold, "Casual difficulty should give more starting resources");
    }

    // ─── Tutorial ─────────────────────────────────────────────────

    [Fact]
    public void Tutorial_InitializesOnApplyNewGameSettings()
    {
        var bootstrapper = new GameBootstrapper();
        bootstrapper.Bootstrap("./TestMods");
        bootstrapper.ApplyNewGameSettings(new NewGameSettings { Seed = 42 });

        var ts = bootstrapper.Services.Get<SimulationTicker>().TutorialSystem;
        Assert.True(ts.TotalCount > 0, "Tutorial missions should be loaded");
        Assert.NotNull(ts.ActiveMission);
    }

    [Fact]
    public void Tutorial_SkipAllMarksComplete()
    {
        var bootstrapper = new GameBootstrapper();
        bootstrapper.Bootstrap("./TestMods");
        bootstrapper.ApplyNewGameSettings(new NewGameSettings { Seed = 42 });

        var ts = bootstrapper.Services.Get<SimulationTicker>().TutorialSystem;
        foreach (var mission in ts.AllMissions)
        {
            mission.State = TutorialMissionState.Completed;
        }

        Assert.True(ts.AllComplete);
    }

    // ─── Event Wiring ─────────────────────────────────────────────

    [Fact]
    public void WorkerWornOutEvent_Published()
    {
        var eventBus = new EventBus();
        WorkerWornOutEvent? captured = null;
        eventBus.Subscribe<WorkerWornOutEvent>(e => captured = e);

        eventBus.Publish(new WorkerWornOutEvent(new EntityId(1), WearOutReason.ToolBroken, WorkerProfession.Forester, new GridPosRPG(5, 5)));

        Assert.NotNull(captured);
        Assert.Equal(1ul, captured.Value.WorkerId);
        Assert.Equal(WearOutReason.ToolBroken, captured.Value.Reason);
    }

    [Fact]
    public void AbilityGainedEvent_Published()
    {
        var eventBus = new EventBus();
        AbilityGainedEvent? captured = null;
        eventBus.Subscribe<AbilityGainedEvent>(e => captured = e);

        eventBus.Publish(new AbilityGainedEvent(new EntityId(1), "swift_strike", "Swift Strike"));

        Assert.NotNull(captured);
        Assert.Equal("Swift Strike", captured.Value.AbilityName);
    }

    [Fact]
    public void GoldChangedEvent_Published()
    {
        var eventBus = new EventBus();
        GoldChangedEvent? captured = null;
        eventBus.Subscribe<GoldChangedEvent>(e => captured = e);

        eventBus.Publish(new GoldChangedEvent(100, 150, "dungeon_reward"));

        Assert.NotNull(captured);
        Assert.Equal(50, captured.Value.NewAmount - captured.Value.OldAmount);
    }

    [Fact]
    public void EndConditionMetEvent_Published_ForVictory()
    {
        var eventBus = new EventBus();
        var events = new List<EndConditionMetEvent>();
        eventBus.Subscribe<EndConditionMetEvent>(e => events.Add(e));

        var bootstrapper = new GameBootstrapper();
        var simulation = bootstrapper.Bootstrap("./TestMods");
        var gsm = new GameStateMachine(eventBus, simulation);

        gsm.StartNewGameSetup();
        gsm.StartNewGame();
        gsm.TriggerVictory();

        Assert.Single(events);
        Assert.True(events[0].IsVictory);
    }

    // ─── SaveManager ──────────────────────────────────────────────

    [Fact]
    public void SaveManager_CreateSaveData_IncludesResources()
    {
        var bootstrapper = new GameBootstrapper();
        var simulation = bootstrapper.Bootstrap("./TestMods");

        simulation.ItemManager.SetStock("gold", 500);
        simulation.ItemManager.SetStock("ore", 25);

        var data = bootstrapper.Services.Get<SaveManager>().CreateSaveData(simulation);

        Assert.NotNull(data);
        Assert.Equal(500, data.ResourceStocks["gold"]);
        Assert.Equal(25, data.ResourceStocks["ore"]);
    }

    // ─── UIManager / Window Layout ────────────────────────────────

    [Fact]
    public void WindowLayoutData_DefaultValues()
    {
        var layout = new WindowLayoutData();
        Assert.Equal("", layout.WindowId);
        Assert.Equal(300f, layout.Width);
        Assert.Equal(200f, layout.Height);
        Assert.True(layout.Visible);
        Assert.False(layout.Pinned);
    }

    [Fact]
    public void WindowLayoutData_SetAndGet()
    {
        var layout = new WindowLayoutData
        {
            WindowId = "guild_hud",
            X = 10f, Y = 20f, Width = 240f, Height = 160f, Visible = true, Pinned = true
        };

        Assert.Equal("guild_hud", layout.WindowId);
        Assert.Equal(240f, layout.Width);
        Assert.True(layout.Pinned);
    }

    [Fact]
    public void GameSettings_WindowLayouts_DefaultEmpty()
    {
        var settings = new GameSettings();
        Assert.NotNull(settings.WindowLayouts);
        Assert.Empty(settings.WindowLayouts);
    }

    [Fact]
    public void GameSettings_WindowLayouts_AddAndRetrieve()
    {
        var settings = new GameSettings();
        settings.WindowLayouts.Add(new WindowLayoutData { WindowId = "test_window", X = 100f, Width = 400f });
        Assert.Single(settings.WindowLayouts);
        Assert.Equal("test_window", settings.WindowLayouts[0].WindowId);
    }

    [Fact]
    public void WindowOpenedEvent_Published()
    {
        var eventBus = new EventBus();
        WindowOpenedEvent? captured = null;
        eventBus.Subscribe<WindowOpenedEvent>(e => captured = e);

        eventBus.Publish(new WindowOpenedEvent("main_menu"));

        Assert.NotNull(captured);
        Assert.Equal("main_menu", captured.Value.WindowId);
    }

    [Fact]
    public void WindowClosedEvent_Published()
    {
        var eventBus = new EventBus();
        WindowClosedEvent? captured = null;
        eventBus.Subscribe<WindowClosedEvent>(e => captured = e);

        eventBus.Publish(new WindowClosedEvent("settings"));

        Assert.NotNull(captured);
        Assert.Equal("settings", captured.Value.WindowId);
    }

    [Fact]
    public void SettingsManager_SerializesWindowLayouts()
    {
        var eventBus = new EventBus();
        var manager = new SettingsManager(eventBus);
        manager.Settings.WindowLayouts.Add(new WindowLayoutData
        {
            WindowId = "guild_hud", X = 10f, Y = 20f, Width = 240f, Height = 160f, Pinned = true
        });

        var json = manager.SerializeToJson();
        Assert.Contains("guild_hud", json);

        var manager2 = new SettingsManager(eventBus);
        manager2.DeserializeFromJson(json);
        Assert.Single(manager2.Settings.WindowLayouts);
        Assert.Equal("guild_hud", manager2.Settings.WindowLayouts[0].WindowId);
        Assert.True(manager2.Settings.WindowLayouts[0].Pinned);
    }

    // ─── Full Journey Flows ───────────────────────────────────────

    [Fact]
    public void FullFlow_MainMenu_NewGameSetup_Playing_Paused_Playing_Victory_MainMenu()
    {
        var eventBus = new EventBus();
        var bootstrapper = new GameBootstrapper();
        var simulation = bootstrapper.Bootstrap("./TestMods");
        var gsm = new GameStateMachine(eventBus, simulation);

        var stateLog = new List<GameState>();
        eventBus.Subscribe<GameStateChangedEvent>(e => stateLog.Add(e.NewState));

        Assert.True(gsm.StartNewGameSetup());
        Assert.True(gsm.StartNewGame());
        Assert.True(gsm.TogglePause());
        Assert.True(gsm.TogglePause());
        Assert.True(gsm.TriggerVictory());
        Assert.True(gsm.ReturnToMainMenu());
        Assert.Equal(GameState.MainMenu, gsm.Current);

        Assert.Equal(6, stateLog.Count);
        Assert.Equal(new[]
        {
            GameState.NewGameSetup, GameState.Playing, GameState.Paused,
            GameState.Playing, GameState.Victory, GameState.MainMenu
        }, stateLog);
    }

    [Fact]
    public void FullFlow_GameOver_ReturnToMenu_StartAgain()
    {
        var eventBus = new EventBus();
        var bootstrapper = new GameBootstrapper();
        var simulation = bootstrapper.Bootstrap("./TestMods");
        var gsm = new GameStateMachine(eventBus, simulation);

        gsm.StartNewGameSetup();
        gsm.StartNewGame();
        gsm.TriggerGameOver();
        Assert.Equal(GameState.GameOver, gsm.Current);

        gsm.ReturnToMainMenu();
        gsm.StartNewGameSetup();
        Assert.Equal(GameState.NewGameSetup, gsm.Current);
    }

    [Fact]
    public void GameStateMachine_RapidPauseUnpause()
    {
        var eventBus = new EventBus();
        var bootstrapper = new GameBootstrapper();
        var simulation = bootstrapper.Bootstrap("./TestMods");
        var gsm = new GameStateMachine(eventBus, simulation);

        gsm.StartNewGameSetup();
        gsm.StartNewGame();

        for (int i = 0; i < 100; i++)
        {
            gsm.TogglePause();
        }

        Assert.Equal(GameState.Playing, gsm.Current);
    }

    [Fact]
    public void GameStateMachine_AlreadyMainMenu_ReturnToMainMenu_ReturnsFalse()
    {
        var eventBus = new EventBus();
        var bootstrapper = new GameBootstrapper();
        var simulation = bootstrapper.Bootstrap("./TestMods");
        var gsm = new GameStateMachine(eventBus, simulation);

        Assert.False(gsm.ReturnToMainMenu());
    }

    // ─── GuildData Picker Options ─────────────────────────────────

    [Fact]
    public void GuildData_HasBannerOptions()
    {
        Assert.True(GuildData.AvailableBanners.Length >= 8);
        Assert.Contains("banner_default", GuildData.AvailableBanners);
        Assert.Contains("banner_dragon", GuildData.AvailableBanners);
    }

    [Fact]
    public void GuildData_HasLogoOptions()
    {
        Assert.True(GuildData.AvailableLogos.Length >= 8);
        Assert.Contains("logo_sword", GuildData.AvailableLogos);
        Assert.Contains("logo_crown", GuildData.AvailableLogos);
    }

    // ─── Localization ─────────────────────────────────────────────

    [Fact]
    public void Localization_GuildSetupKeysExist()
    {
        var ts = new TranslationService();
        ts.LoadFromEmbeddedJson();
        ts.SetLanguage("en");

        Assert.Equal("Guild Name", ts.Get("ui.newgame.guild_name"));
        Assert.Equal("Guild Banner", ts.Get("ui.newgame.guild_banner"));
        Assert.Equal("Guild Logo", ts.Get("ui.newgame.guild_logo"));
        Assert.Equal("Guild Creation", ts.Get("ui.newgame.guild_section"));
    }

    [Fact]
    public void Localization_PauseSaveKeyExists()
    {
        var ts = new TranslationService();
        ts.LoadFromEmbeddedJson();
        ts.SetLanguage("en");

        Assert.Equal("Save Game", ts.Get("ui.pause.save"));
    }

    [Fact]
    public void LocalizationKeys_GuildSetupConstants()
    {
        Assert.Equal("ui.newgame.guild_name", LocalizationKeys.NewGameGuildName);
        Assert.Equal("ui.newgame.guild_banner", LocalizationKeys.NewGameGuildBanner);
        Assert.Equal("ui.newgame.guild_logo", LocalizationKeys.NewGameGuildLogo);
        Assert.Equal("ui.newgame.guild_section", LocalizationKeys.NewGameGuildSection);
        Assert.Equal("ui.pause.save", LocalizationKeys.PauseSave);
    }

    [Fact]
    public void Localization_AllPhase12KeysAreTranslated()
    {
        var ts = new TranslationService();
        ts.LoadFromEmbeddedJson();
        ts.SetLanguage("en");

        var requiredKeys = new[]
        {
            "ui.newgame.guild_name", "ui.newgame.guild_banner",
            "ui.newgame.guild_logo", "ui.newgame.guild_section",
            "ui.pause.save", "ui.mainmenu.new_game", "ui.mainmenu.continue",
            "ui.mainmenu.settings", "ui.mainmenu.quit",
            "ui.pause.title", "ui.pause.resume", "ui.pause.main_menu",
            "ui.gameover.title", "ui.gameover.try_again",
            "ui.victory.title", "ui.victory.continue", "ui.victory.menu",
            "ui.tutorial.title", "ui.tutorial.skip",
            "ui.newgame.title", "ui.newgame.start", "ui.newgame.back"
        };

        foreach (var key in requiredKeys)
        {
            var value = ts.Get(key);
            Assert.NotEqual(key, value);
        }
    }
}
