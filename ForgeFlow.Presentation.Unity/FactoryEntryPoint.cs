using ForgeFlow.Core;
using ForgeFlow.Core.Data;
using ForgeFlow.Core.Entities;
using ForgeFlow.Core.Events;
using EntityId = ForgeFlow.Core.Entities.EntityId;
using ForgeFlow.Core.Localization;
using ForgeFlow.Core.Modding;
using ForgeFlow.Core.Proto;
using ForgeFlow.Core.Proto.Logic;
using ForgeFlow.Core.Proto.Prototypes;
using ForgeFlow.Core.Save;
using ForgeFlow.Core.Systems;
using ForgeFlow.Core.Theming;
using ForgeFlow.Presentation.Unity.Audio;
using ForgeFlow.Presentation.Unity.Cameras;
using ForgeFlow.Presentation.Unity.Input;
using ForgeFlow.Presentation.Unity.UI;
using ForgeFlow.Presentation.Unity.UI.Components;
using ForgeFlow.Presentation.Unity.UI.Inspectors;
using ForgeFlow.Presentation.Unity.Visuals;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace ForgeFlow.Presentation.Unity
{

/// <summary>
/// Public entry-point for the Presentation layer. NOT a MonoBehaviour — the
/// thin <c>FactoryBootstrap.cs</c> in the Unity project owns the MonoBehaviour
/// lifecycle and delegates here.
/// All window management is delegated to <see cref="UIManager"/>.
/// </summary>
public sealed class FactoryEntryPoint
{
    private readonly GameObject _rootObject = new GameObject("ForgeFlowRoot");
    private string _modsPath = "Mods";

    private GameBootstrapper _bootstrapper = null!;
    private SimulationTicker _simulation = null!;
    private EventBus _eventBus = null!;
    private InputActionAsset _inputActionAsset = null!;
    private FactoryInputActions _inputActions = null!;

    // Visual managers (non-UI)
    private VisualHeroManager? _visualHeroManager;
    private PathRendererSystem? _pathRenderer;
    private StructurePlacer? _structurePlacer;
    private AudioEventHandler? _audioHandler;
    private DebugHUD? _debugHUD;
    private AppearanceWorkshop? _appearanceWorkshop;
    private FusionAltarVisual? _fusionAltarVisual;
    private ResearchPanel? _researchPanel;
    private FullAppearanceEditor? _appearanceEditor;
    private VillageManager? _villageManager;
    private ModBrowserUI? _modBrowserUI;
    private TerrainVisualManager? _terrainManager;
    private ProtoStructureRenderer? _protoStructureManager;
    private TutorialPanel? _tutorialPanel;
    private WorkerEffectSystem? _workerEffectSystem;
    private TileAreaHighlightRenderer? _tileAreaHighlightRenderer;

    // Camera & input
    private CameraController? _cameraController;
    private EntitySelector? _entitySelector;
    private Camera? _mainCamera;
    private InputControllerStack? _inputStack;

    // 3D scene objects
    private GameObject? _directionalLight;

    // Central UI manager — single source of truth for all windows
    private UIManager? _uiManager;
    private UIDocument? _uiDocument;
    private InspectorCoordinator? _inspectorCoordinator;

    public GameBootstrapper Bootstrapper => _bootstrapper;
    public SimulationTicker Simulation => _simulation;
    public EventBus EventBus => _eventBus;

    /// <summary>
    /// Called by FactoryBootstrap.cs to configure the mods path before Initialize.
    /// </summary>
    public void SetModsPath(string path)
    {
        _modsPath = path;
    }

    /// <summary>
    /// Call once from FactoryBootstrap.Start() to bootstrap Core and build the scene.
    /// </summary>
    public void Initialize()
    {
        UnityEngine.Object.DontDestroyOnLoad(_rootObject);

        // ── 1. Bootstrap Core (100% pure .NET, zero Unity knowledge) ──
        _bootstrapper = new GameBootstrapper();
        _simulation = _bootstrapper.Bootstrap(
            Path.Combine(Application.persistentDataPath, _modsPath));
        _eventBus = _bootstrapper.Services.Get<EventBus>();

        // ── 1b. Inject platform hooks ──
        _simulation.WorldStateManager.SetPlatformHooks(new DefaultPcHooks());
        _simulation.WorldStateManager.PrepareForConsoleBuild();

        // ── 1c. Load prefabs from Resources/ ──
        PrefabRegistry.Initialize();

        // ── 2. Build scene infrastructure programmatically ──
        CreateIsometricCamera();
        CreateDirectionalLight();

        // ── 3. Create and enable Input System actions (100% runtime, no Editor asset) ──
        _inputActionAsset = RuntimeInputFactory.CreateFactoryInputActions();
        _inputActions = new FactoryInputActions(_inputActionAsset);

        // ── 4. Attach all presentation managers to the root GameObject ──
        _visualHeroManager = _rootObject.AddComponent<VisualHeroManager>();
        _pathRenderer = _rootObject.AddComponent<PathRendererSystem>();
        _structurePlacer = _rootObject.AddComponent<StructurePlacer>();
        _audioHandler = _rootObject.AddComponent<AudioEventHandler>();
        _debugHUD = _rootObject.AddComponent<DebugHUD>();
        _appearanceWorkshop = _rootObject.AddComponent<AppearanceWorkshop>();
        _fusionAltarVisual = _rootObject.AddComponent<FusionAltarVisual>();
        _researchPanel = _rootObject.AddComponent<ResearchPanel>();
        _appearanceEditor = _rootObject.AddComponent<FullAppearanceEditor>();
        _villageManager = _rootObject.AddComponent<VillageManager>();
        _modBrowserUI = _rootObject.AddComponent<ModBrowserUI>();
        _terrainManager = _rootObject.AddComponent<TerrainVisualManager>();
        _protoStructureManager = _rootObject.AddComponent<ProtoStructureRenderer>();
        _tutorialPanel = _rootObject.AddComponent<TutorialPanel>();
        _workerEffectSystem = _rootObject.AddComponent<WorkerEffectSystem>();
        _tileAreaHighlightRenderer = _rootObject.AddComponent<TileAreaHighlightRenderer>();

        // ── 5. Initialize with Core references ──
        _visualHeroManager.Initialize(_simulation, _eventBus);
        _pathRenderer.Initialize(_simulation, _eventBus);
        _structurePlacer.Initialize(_simulation, _bootstrapper, _pathRenderer);
        _audioHandler.Initialize(_eventBus);
        _debugHUD.Initialize(_simulation);
        _appearanceWorkshop.Initialize(_simulation, _bootstrapper, _visualHeroManager, _eventBus);
        _fusionAltarVisual.Initialize(_simulation, _eventBus, _visualHeroManager);
        _researchPanel.Initialize(_simulation, _eventBus);
        _appearanceEditor.Initialize(_simulation, _bootstrapper, _visualHeroManager, _eventBus);
        _villageManager.Initialize(_simulation, _bootstrapper, _eventBus);
        _modBrowserUI.Initialize(_simulation, _bootstrapper, _eventBus,
            Path.Combine(Application.persistentDataPath, _modsPath));
        _terrainManager.Initialize(_bootstrapper.Services.Get<TerrainGrid>(), _bootstrapper.Services.Get<ProtoRegistry>());
        _protoStructureManager.Initialize(_simulation, _bootstrapper, _eventBus);
        _tutorialPanel.Initialize(_simulation, _eventBus);
        _workerEffectSystem.Initialize(_eventBus, _simulation, _audioHandler);
        _tileAreaHighlightRenderer!
            .WithBorderThickness(3)
            .WithBorderColor("status.success")
            .WithPulsating(true)
            .WithOverlayColor("status.success")
            .WithOverlayAlpha(0.15f)
            .Initialize(_eventBus);

        // ── 6. Camera controller, UI Toolkit theming, entity selector ──
        ForgeStyledVisualElement.SetServices(_bootstrapper.Services.Get<ThemeService>(), _bootstrapper.Services.Get<TranslationService>());

        _cameraController = _rootObject.AddComponent<CameraController>();
        _cameraController.Initialize(
            _mainCamera!,
            _bootstrapper.Services.Get<SettingsManager>().Settings.Camera,
            _bootstrapper.Services.Get<TerrainGrid>().Width,
            _bootstrapper.Services.Get<TerrainGrid>().Height);

        _entitySelector = _rootObject.AddComponent<EntitySelector>();
        _entitySelector.Initialize(_mainCamera!, _eventBus, _simulation);

        _eventBus.Subscribe<ThemeChangedEvent>(OnThemeChanged);

        // ── 7. Create UIManager and register all windows ──
        RegisterAllWindows();

        // ── 8. Input Controller Stack — sole input router ──
        _inputStack = _rootObject.AddComponent<InputControllerStack>();
        _inputStack.Initialize(
            _mainCamera!,
            _inputActions,
            _uiManager,
            () => _simulation.TogglePause(),
            () =>
            {
                var current = _bootstrapper.Services.Get<GameStateMachine>().Current;
                if (current == GameState.Playing || current == GameState.Paused)
                {
                    _bootstrapper.Services.Get<GameStateMachine>().TogglePause();
                }
            });
        _inputStack.SetStructurePlacer(_structurePlacer);
        _inputStack.Register(new StructurePlacementController(_structurePlacer));
        _inputStack.Register(new DemolishController(_simulation));
        _inputStack.Register(new PathDrawingController(
            _simulation, _pathRenderer));
        _inputStack.Register(new EntitySelectionController(_mainCamera!, _eventBus, _simulation));
        _inputStack.Register(new CameraRotateController(_cameraController!, () => _inputStack.ActiveToolMode != null));

        var debugController = new DebugDevToolController(_simulation);
        debugController.OnTerrainCellChanged = pos => _terrainManager?.RefreshCell(pos);
        _inputStack.Register(debugController);

        // ── Tool Mode Indicator (wired to UIManager window) ──
        var toolModeIndicator = _uiManager!.GetWindow<ToolModeIndicator>(WindowIds.ToolModeIndicator);
        if (toolModeIndicator != null)
        {
            toolModeIndicator.SetInputActions(_inputActions);
            _inputStack.ToolModeChanged += info =>
            {
                toolModeIndicator.OnToolModeChanged(info);
                if (info.ModeName == null)
                {
                    _uiManager.GetWindow<GameplayToolbar>(WindowIds.GameplayToolbar)?.OnToolModeExited();
                }
            };
        }

        // Bind hotkeys 1-0 to the gameplay toolbar
        BindHotkeysToToolbar();

        // ── 9. Subscribe to Core events for visual / audio feedback ──
        SubscribeToEvents();

        // ── 10. Start at Main Menu — simulation paused ──
        _simulation.Pause();
        var gameplayActive = _uiManager!.ShowWindowsForState(GameState.MainMenu);
        SetGameplayActive(gameplayActive);

        Debug.Log("[ForgeFlow] Factory bootstrapped — UIManager is the single source of truth for all windows.");
    }

    /// <summary>
    /// Call every frame from FactoryBootstrap.Update().
    /// </summary>
    public void Tick(float deltaTime)
    {
        _simulation.Update(deltaTime);

        // Clear the per-frame flag after reading it
        bool inputSystemHandledCancel = _inputStack?.CancelDispatchedThisFrame ?? false;
        if (_inputStack != null)
        {
            _inputStack.CancelDispatchedThisFrame = false;
        }

        // Fallback Escape handling — covers cases where InputSystem did not
        // deliver the event (UI Toolkit consumed it, actions disabled, etc.)
        var keyboard = Keyboard.current;
        if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame && !inputSystemHandledCancel)
        {
            var current = _bootstrapper.Services.Get<GameStateMachine>().Current;
            if (current == GameState.Playing || current == GameState.Paused)
            {
                _bootstrapper.Services.Get<GameStateMachine>().TogglePause();
            }
        }

        // Evaluate end conditions every frame while playing
        if (_bootstrapper.Services.Get<GameStateMachine>().Current == GameState.Playing)
        {
            _bootstrapper.Services.Get<GameStateMachine>().EvaluateEndConditions(deltaTime);
        }

        // Tick all visible ITickableWindow instances
        _uiManager?.Tick(deltaTime);
    }

    /// <summary>
    /// Call from FactoryBootstrap.OnDestroy() to clean up.
    /// </summary>
    public void Shutdown()
    {
        UnsubscribeFromEvents();
        _inspectorCoordinator?.Dispose();
        _uiManager?.Dispose();
        _inputActions?.DisableAll();
        if (_inputActionAsset != null)
        {
            UnityEngine.Object.Destroy(_inputActionAsset);
        }
        if (_bootstrapper != null)
        {
            _bootstrapper.Services.Get<GameplayFlowSystem>().Dispose();
            _bootstrapper.Services.Get<ModLoader>().UnloadAllMods();
        }
    }

    // ── UIManager Window Registration ───────────────────────────

    private void RegisterAllWindows()
    {
        // Create UIDocument + PanelSettings
        _uiDocument = _rootObject.AddComponent<UIDocument>();
        var panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
        panelSettings.themeStyleSheet = ScriptableObject.CreateInstance<ThemeStyleSheet>();
        panelSettings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
        panelSettings.referenceResolution = new Vector2Int(1920, 1080);
        panelSettings.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
        panelSettings.match = 0.5f;
        _uiDocument.panelSettings = panelSettings;

        var uiRoot = _uiDocument.rootVisualElement;
        uiRoot.style.width = new Length(100, LengthUnit.Percent);
        uiRoot.style.height = new Length(100, LengthUnit.Percent);

        var runtimeFont = Font.CreateDynamicFontFromOSFont("Arial", 14);
        if (runtimeFont != null)
        {
            uiRoot.style.unityFont = new StyleFont(runtimeFont);
        }

        // Create UIManager
        _uiManager = new UIManager(_eventBus, _bootstrapper.Services.Get<SettingsManager>());
        _uiManager.Initialize(uiRoot);

        // ── Menu Windows ──
        var mainMenu = new MainMenuPanel();
        mainMenu.OnNewGame += () => _bootstrapper.Services.Get<GameStateMachine>().StartNewGameSetup();
        mainMenu.OnContinue += HandleLoadGame;
        mainMenu.OnOpenSettings += () => ShowOverlayWindow(WindowIds.Settings);
        mainMenu.OnOpenAchievements += () => ShowOverlayWindow(WindowIds.Achievements);
        mainMenu.OnOpenStatistics += () => ShowOverlayWindow(WindowIds.Statistics);
        mainMenu.OnOpenModBrowser += () => Debug.Log("[ForgeFlow] Mod Browser opened.");
        mainMenu.OnQuit += () => Application.Quit();
        _uiManager.Register(new WindowDescriptor(WindowIds.MainMenu, UILayer.Menu), mainMenu);

        var newGameSetup = new NewGameSetupPanel();
        newGameSetup.OnStartGame += HandleStartNewGame;
        newGameSetup.OnBack += () => _bootstrapper.Services.Get<GameStateMachine>().ReturnToMainMenu();
        _uiManager.Register(new WindowDescriptor(WindowIds.NewGameSetup, UILayer.Menu), newGameSetup);

        var settingsPanel = new SettingsPanel(_bootstrapper.Services.Get<SettingsManager>());
        settingsPanel.OnBack += () => HideOverlayAndRestore();
        _uiManager.Register(new WindowDescriptor(WindowIds.Settings, UILayer.Modal), settingsPanel);

        var pauseMenu = new PauseMenuPanel();
        pauseMenu.OnResume += () => _bootstrapper.Services.Get<GameStateMachine>().TogglePause();
        pauseMenu.OnSaveGame += HandleSaveFromPause;
        pauseMenu.OnLoadGame += HandleLoadGame;
        pauseMenu.OnOpenSettings += () => ShowOverlayWindow(WindowIds.Settings);
        pauseMenu.OnMainMenu += () => _bootstrapper.Services.Get<GameStateMachine>().ReturnToMainMenu();
        _uiManager.Register(new WindowDescriptor(WindowIds.PauseMenu, UILayer.Menu), pauseMenu);

        var gameOver = new GameOverPanel();
        gameOver.OnReturnToMenu += () => _bootstrapper.Services.Get<GameStateMachine>().ReturnToMainMenu();
        gameOver.OnTryAgain += HandleTryAgain;
        _uiManager.Register(new WindowDescriptor(WindowIds.GameOver, UILayer.Menu), gameOver);

        var victory = new VictoryPanel();
        victory.OnContinuePlaying += () => _bootstrapper.Services.Get<GameStateMachine>().TransitionTo(GameState.Playing);
        victory.OnReturnToMenu += () => _bootstrapper.Services.Get<GameStateMachine>().ReturnToMainMenu();
        _uiManager.Register(new WindowDescriptor(WindowIds.Victory, UILayer.Menu), victory);

        var stats = new GameStatisticsPanel(_bootstrapper.Services.Get<GameStatistics>());
        stats.OnBack += () => HideOverlayAndRestore();
        _uiManager.Register(new WindowDescriptor(WindowIds.Statistics, UILayer.Modal), stats);

        var achievements = new AchievementsPanel(_bootstrapper.Services.Get<AchievementSystem>());
        achievements.OnBack += () => HideOverlayAndRestore();
        _uiManager.Register(new WindowDescriptor(WindowIds.Achievements, UILayer.Modal), achievements);

        // ── HUD Windows ──
        var tutorialOverlay = new TutorialOverlayPanel(_simulation, _eventBus);
        tutorialOverlay.OnSkipTutorial += HandleSkipTutorial;
        _uiManager.Register(new WindowDescriptor(WindowIds.TutorialOverlay, UILayer.HUD), tutorialOverlay);

        var topBarHud = new TopBarHUD(_simulation, _eventBus);
        _uiManager.Register(new WindowDescriptor(WindowIds.TopBarHUD, UILayer.HUD), topBarHud);

        // ── Gameplay Windows ──
        // Type-specific structure + entity inspectors.
        // All inspectors implementing IAutoRegisteredInspector are discovered
        // via reflection and constructor-injected by AutoRegistrar — see
        // Action-Plan #31 Session 3 for the rationale. Adding a new inspector
        // only requires the marker interface; no wiring changes here.
        var inspectors = AutoRegistrar.RegisterInspectors(_bootstrapper.Services, _uiManager);

        // Post-registration wiring for panel events that reach out to this
        // MonoBehaviour (the registrar intentionally does not know about
        // cross-layer event hooks).
        if (inspectors.TryGetValue(WindowIds.WorkerInspector, out var workerInspector)
            && workerInspector is RichWorkerInspectorPanel richWorker)
        {
            richWorker.OnSendToMaintenance += HandleSendToMaintenance;
        }

        var automationConfig = new AutomationConfigWindow(_simulation, _eventBus);
        _uiManager.Register(new WindowDescriptor(WindowIds.AutomationConfig, UILayer.Modal), automationConfig);

        // Inspector coordinator — single EntitySelectedEvent subscriber, routes to correct inspector
        _inspectorCoordinator = new InspectorCoordinator(_simulation, _eventBus, _uiManager);

        // ── Toolbar ──
        var toolbar = new GameplayToolbar(_simulation, _bootstrapper.Services.Get<HotbarSystem>());
        toolbar.OnStructureSelected += type =>
        {
            _inputStack?.ExitPathDrawMode();
            _inputStack?.ExitDemolishMode();
            _structurePlacer?.EnterPlacementModeFromToolbar(type);
        };
        toolbar.OnDrawPathMode += () =>
        {
            _structurePlacer?.CancelPlacement();
            _inputStack?.ExitDemolishMode();
            _inputStack?.EnterPathDrawMode();
            Debug.Log("[ForgeFlow] Path draw mode — click and drag on terrain.");
        };
        toolbar.OnDemolishMode += () =>
        {
            _structurePlacer?.CancelPlacement();
            _inputStack?.ExitPathDrawMode();
            _inputStack?.EnterDemolishMode();
            Debug.Log("[ForgeFlow] Demolish mode — click on entities to remove them.");
        };
        _uiManager.Register(new WindowDescriptor(WindowIds.GameplayToolbar, UILayer.Toolbar), toolbar);

        var toolModeIndicator = new ToolModeIndicator();
        _uiManager.Register(new WindowDescriptor(WindowIds.ToolModeIndicator, UILayer.Toolbar), toolModeIndicator);

        // ── Legacy MonoBehaviour Windows (wrapped) ──
        _uiManager.Register(
            new WindowDescriptor(WindowIds.DebugHUD, UILayer.Debug),
            new MonoBehaviourWindowAdapter(WindowIds.DebugHUD, _debugHUD!));
        _uiManager.Register(
            new WindowDescriptor(WindowIds.ResearchPanel, UILayer.Gameplay),
            new MonoBehaviourWindowAdapter(WindowIds.ResearchPanel, _researchPanel!));

        // Subscribe to state changes
        _eventBus.Subscribe<GameStateChangedEvent>(OnGameStateChanged);
        _eventBus.Subscribe<TutorialStepActivatedEvent>(OnTutorialStepActivated);
        _eventBus.Subscribe<EndConditionMetEvent>(OnEndConditionMet);

        Debug.Log("[ForgeFlow] UIManager initialized with all windows registered.");
    }

    // ── Game State Handling ─────────────────────────────────────

    private void OnGameStateChanged(GameStateChangedEvent e)
    {
        // Pre-state actions
        if (e.NewState == GameState.Victory)
        {
            _uiManager?.GetWindow<VictoryPanel>(WindowIds.Victory)?.SetStats(_bootstrapper.Services.Get<GameStatistics>());
        }

        var gameplayActive = _uiManager!.ShowWindowsForState(e.NewState);
        SetGameplayActive(gameplayActive);
        Debug.Log($"[ForgeFlow] Game state: {e.PreviousState} → {e.NewState}");
    }

    private void SetGameplayActive(bool active)
    {
        // Toggle gameplay-related managers
        if (_inputStack != null) _inputStack.enabled = active;
        if (_structurePlacer != null) _structurePlacer.enabled = active;
        if (_entitySelector != null) _entitySelector.enabled = active;
        if (_cameraController != null) _cameraController.enabled = active;
        if (_researchPanel != null) _researchPanel.enabled = active;

        // Toggle input actions
        if (active)
        {
            _inputActions?.EnableAll();
        }
        else
        {
            _inputActions?.DisableAll();
        }

        // Toggle 3D scene objects
        _directionalLight?.SetActive(active);
    }

    private void ShowOverlayWindow(string windowId)
    {
        _uiManager?.CloseAll();
        var window = _uiManager?.GetWindow(windowId);
        if (window != null)
        {
            window.Refresh();
            _uiManager?.Open(windowId);
        }
    }

    private void HideOverlayAndRestore()
    {
        // Re-show the panel for the current state
        var gameplayActive = _uiManager!.ShowWindowsForState(_bootstrapper.Services.Get<GameStateMachine>().Current);
        SetGameplayActive(gameplayActive);
    }

    // ── Game Flow Handlers ──────────────────────────────────────

    private void HandleStartNewGame(NewGameSettings settings)
    {
        _bootstrapper.ApplyNewGameSettings(settings);
        _terrainManager?.Initialize(_bootstrapper.Services.Get<TerrainGrid>(), _bootstrapper.Services.Get<ProtoRegistry>());
        _bootstrapper.Services.Get<GameStateMachine>().StartNewGame();
    }

    private void HandleLoadGame()
    {
        var saveDir = Path.Combine(Application.persistentDataPath, "Saves");
        var slots = _bootstrapper.Services.Get<SaveManager>().ListSaveFiles(saveDir);

        if (slots.Count > 0)
        {
            var latest = slots[0];
            var data = _bootstrapper.Services.Get<SaveManager>().LoadFromFile(latest.FilePath);
            if (data != null)
            {
                ApplyLoadedSaveData(data);
                _bootstrapper.Services.Get<GameStateMachine>().StartNewGame();
                Debug.Log($"[ForgeFlow] Loaded save: {latest.GameName} from {latest.FilePath}");
            }
            else
            {
                Debug.LogWarning("[ForgeFlow] Failed to load save file.");
            }
        }
        else
        {
            Debug.Log("[ForgeFlow] No save files found — starting new game setup instead.");
            _bootstrapper.Services.Get<GameStateMachine>().StartNewGameSetup();
        }
    }

    private void ApplyLoadedSaveData(SaveData data)
    {
        _bootstrapper.Services.Get<TerrainGrid>().GenerateDefault(data.WorldSeed);
        _terrainManager?.Initialize(_bootstrapper.Services.Get<TerrainGrid>(), _bootstrapper.Services.Get<ProtoRegistry>());
        _eventBus.Publish(new TerrainGeneratedEvent(_bootstrapper.Services.Get<TerrainGrid>().Width, _bootstrapper.Services.Get<TerrainGrid>().Height));

        _simulation.ItemManager.Clear();
        foreach (var kvp in data.ResourceStocks)
        {
            _simulation.ItemManager.SetStock(kvp.Key, kvp.Value);
        }
    }

    private void HandleTryAgain()
    {
        var lastSettings = _bootstrapper.LastNewGameSettings ?? new NewGameSettings();
        _bootstrapper.ApplyNewGameSettings(lastSettings);
        _terrainManager?.Initialize(_bootstrapper.Services.Get<TerrainGrid>(), _bootstrapper.Services.Get<ProtoRegistry>());
        _bootstrapper.Services.Get<GameStateMachine>().ReturnToMainMenu();
        _bootstrapper.Services.Get<GameStateMachine>().StartNewGameSetup();
    }

    private void HandleSkipTutorial()
    {
        var ts = _simulation.TutorialSystem;
        foreach (var mission in ts.AllMissions)
        {
            mission.State = TutorialMissionState.Completed;
        }
        _uiManager?.RefreshWindow(WindowIds.TutorialOverlay);
        Debug.Log("[ForgeFlow] Tutorial skipped.");
    }

    private void HandleSaveFromPause()
    {
        var gameName = _bootstrapper.LastNewGameSettings?.GameName ?? "QuickSave";
        SaveGame(gameName);
    }

    private void OnTutorialStepActivated(TutorialStepActivatedEvent e)
    {
        _uiManager?.GetWindow<TutorialOverlayPanel>(WindowIds.TutorialOverlay)
            ?.SetHighlightTarget(e.HighlightTarget, e.HintText);
        _uiManager?.RefreshWindow(WindowIds.TutorialOverlay);

        // Handle tile area highlights for tutorial steps
        HandleTutorialAreaHighlight(e.HighlightArea);
    }

    private void HandleTutorialAreaHighlight(string highlightArea)
    {
        const string tutorialGroupId = "tutorial_area";

        if (string.IsNullOrEmpty(highlightArea))
        {
            // No area highlight for this step — clear any previous
            _eventBus.Publish(new TileAreaHighlightEvent(tutorialGroupId, default, show: false));
            return;
        }

        // Build GridAreaRPG from the highlight area key
        GridAreaRPG area = default;

        if (highlightArea.StartsWith("biome_"))
        {
            string biomeName = highlightArea.Substring(6); // "biome_forest" → "forest"
            if (Enum.TryParse<BiomeType>(biomeName, ignoreCase: true, out var biomeType))
            {
                area = BuildBiomeArea(biomeType);
            }
        }

        if (!area.IsEmpty)
        {
            _eventBus.Publish(new TileAreaHighlightEvent(tutorialGroupId, area, show: true));
        }
        else
        {
            _eventBus.Publish(new TileAreaHighlightEvent(tutorialGroupId, default, show: false));
        }
    }

    private GridAreaRPG BuildBiomeArea(BiomeType biomeType)
    {
        var terrain = _bootstrapper.Services.Get<TerrainGrid>();
        var tiles = new List<GridPosRPG>();

        for (int y = 0; y < terrain.Height; y++)
        {
            for (int x = 0; x < terrain.Width; x++)
            {
                var pos = new GridPosRPG(x, y);
                var cell = terrain.Get(pos);
                if (cell != null && cell.Biome == biomeType)
                {
                    tiles.Add(pos);
                }
            }
        }

        if (tiles.Count == 0)
        {
            return default;
        }

        return GridAreaRPG.FromTiles(tiles.ToArray());
    }

    private void OnEndConditionMet(EndConditionMetEvent e)
    {
        if (!e.IsVictory)
        {
            _uiManager?.GetWindow<GameOverPanel>(WindowIds.GameOver)?.SetReason(e.Reason);
        }
    }

    // ── Scene Creation ──────────────────────────────────────────

    private void CreateIsometricCamera()
    {
        var camObj = new GameObject("MainCamera");
        camObj.tag = "MainCamera";
        camObj.transform.position = new Vector3(20f, 12f, -20f);
        camObj.transform.rotation = Quaternion.Euler(30f, 45f, 0f);

        var cam = camObj.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 12f;
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = 100f;
        cam.backgroundColor = new Color(0.15f, 0.15f, 0.2f);
        cam.clearFlags = CameraClearFlags.SolidColor;

        _mainCamera = cam;
    }

    private void CreateDirectionalLight()
    {
        var lightObj = new GameObject("DirectionalLight");
        lightObj.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        _directionalLight = lightObj;

        var light = lightObj.AddComponent<Light>();
        light.type = LightType.Directional;
        light.color = new Color(1f, 0.96f, 0.9f);
        light.intensity = 1.2f;
        light.shadows = LightShadows.Soft;
    }

    // ── Event Wiring ────────────────────────────────────────────

    private void SubscribeToEvents()
    {
        _eventBus.Subscribe<HeroSpawnedEvent>(OnHeroSpawned);
        _eventBus.Subscribe<GearEquippedEvent>(OnGearEquipped);
        _eventBus.Subscribe<DungeonCompletedEvent>(OnDungeonCompleted);
        _eventBus.Subscribe<HeroDiedEvent>(OnHeroDied);
        _eventBus.Subscribe<HeroFusedEvent>(OnHeroFused);
        _eventBus.Subscribe<ResearchUnlockedEvent>(OnResearchUnlocked);
        _eventBus.Subscribe<VillageBuildingBuiltEvent>(OnVillageBuilt);
        _eventBus.Subscribe<ModLoadedEvent>(OnModLoaded);
        _eventBus.Subscribe<CataclysmEvent>(OnCataclysm);
        _eventBus.Subscribe<WorldPortalEvent>(OnWorldPortal);
        _eventBus.Subscribe<PrestigeResetEvent>(OnPrestigeReset);
        _eventBus.Subscribe<ConsoleReadyEvent>(OnConsoleReady);
        _eventBus.Subscribe<VillagerSpawnedEvent>(OnVillagerSpawned);
        _eventBus.Subscribe<TutorialCompletedEvent>(OnTutorialCompleted);
        _eventBus.Subscribe<TerrainGeneratedEvent>(OnTerrainGenerated);
        _eventBus.Subscribe<WorkerWornOutEvent>(OnWorkerWornOut);
        _eventBus.Subscribe<GoldChangedEvent>(OnGoldChangedEntry);
        _eventBus.Subscribe<HotbarChangedEvent>(OnHotbarChanged);
    }

    private void UnsubscribeFromEvents()
    {
        _eventBus.Unsubscribe<HeroSpawnedEvent>(OnHeroSpawned);
        _eventBus.Unsubscribe<GearEquippedEvent>(OnGearEquipped);
        _eventBus.Unsubscribe<DungeonCompletedEvent>(OnDungeonCompleted);
        _eventBus.Unsubscribe<HeroDiedEvent>(OnHeroDied);
        _eventBus.Unsubscribe<HeroFusedEvent>(OnHeroFused);
        _eventBus.Unsubscribe<ResearchUnlockedEvent>(OnResearchUnlocked);
        _eventBus.Unsubscribe<VillageBuildingBuiltEvent>(OnVillageBuilt);
        _eventBus.Unsubscribe<ModLoadedEvent>(OnModLoaded);
        _eventBus.Unsubscribe<CataclysmEvent>(OnCataclysm);
        _eventBus.Unsubscribe<WorldPortalEvent>(OnWorldPortal);
        _eventBus.Unsubscribe<PrestigeResetEvent>(OnPrestigeReset);
        _eventBus.Unsubscribe<ConsoleReadyEvent>(OnConsoleReady);
        _eventBus.Unsubscribe<VillagerSpawnedEvent>(OnVillagerSpawned);
        _eventBus.Unsubscribe<TutorialCompletedEvent>(OnTutorialCompleted);
        _eventBus.Unsubscribe<TerrainGeneratedEvent>(OnTerrainGenerated);
        _eventBus.Unsubscribe<ThemeChangedEvent>(OnThemeChanged);
        _eventBus.Unsubscribe<GameStateChangedEvent>(OnGameStateChanged);
        _eventBus.Unsubscribe<TutorialStepActivatedEvent>(OnTutorialStepActivated);
        _eventBus.Unsubscribe<EndConditionMetEvent>(OnEndConditionMet);
        _eventBus.Unsubscribe<WorkerWornOutEvent>(OnWorkerWornOut);
        _eventBus.Unsubscribe<GoldChangedEvent>(OnGoldChangedEntry);
        _eventBus.Unsubscribe<HotbarChangedEvent>(OnHotbarChanged);
    }

    private void OnHeroSpawned(HeroSpawnedEvent e)
    {
        _visualHeroManager?.SpawnVisualHero(e.HeroId, e.ClassId, e.SpawnPosition);
    }

    private void OnGearEquipped(GearEquippedEvent e)
    {
        _visualHeroManager?.UpdateHeroAppearance(e.HeroId, e.ItemId, e.Slot);
    }

    private void OnDungeonCompleted(DungeonCompletedEvent e)
    {
        _debugHUD?.RecordDungeonResult(e.Success);
        if (e.Success)
        {
            _visualHeroManager?.PlaySuccessEffect(e.HeroId);
            _audioHandler?.PlayDungeonSuccess();
        }
        else
        {
            _audioHandler?.PlayDungeonFailure();
        }
    }

    private void OnHeroDied(HeroDiedEvent e)
    {
        _visualHeroManager?.PlayDeathEffect(e.HeroId);
        _audioHandler?.PlayHeroDeath();
    }

    private void OnHeroFused(HeroFusedEvent e)
    {
        _audioHandler?.PlayFusionComplete();
    }

    private void OnResearchUnlocked(ResearchUnlockedEvent e)
    {
        Debug.Log($"[ForgeFlow] Research Tier {e.Tier} unlocked — new recipes available!");
    }

    private void OnVillageBuilt(VillageBuildingBuiltEvent e)
    {
        Debug.Log($"[ForgeFlow] Village building constructed: {e.BuildingId}");
    }

    private void OnModLoaded(ModLoadedEvent e)
    {
        Debug.Log($"[ForgeFlow] Mod loaded: {e.ModName}");
    }

    private void OnCataclysm(CataclysmEvent e)
    {
        if (e.Started)
        {
            Debug.Log($"[ForgeFlow] CATACLYSM STARTED — intensity {e.Intensity:F1}! Prepare your heroes!");
        }
        else
        {
            Debug.Log("[ForgeFlow] Cataclysm ended — bonus resources awarded.");
        }
    }

    private void OnWorldPortal(WorldPortalEvent e)
    {
        Debug.Log(e.Opened
            ? "[ForgeFlow] World Portal OPENED — new world tier accessible!"
            : "[ForgeFlow] World Portal closed.");
    }

    private void OnPrestigeReset(PrestigeResetEvent e)
    {
        Debug.Log($"[ForgeFlow] PRESTIGE RESET #{e.PrestigeCount}! Previous tier: {e.PreviousResearchTier}. New bonus multiplier: {e.NewBonusMultiplier:F2}x");
    }

    private void OnConsoleReady(ConsoleReadyEvent e)
    {
        Debug.Log($"[ForgeFlow] Console-ready on platform: {e.PlatformName}");
    }

    private void OnHotbarChanged(HotbarChangedEvent e)
    {
        _uiManager?.RefreshWindow(WindowIds.GameplayToolbar);
    }

    private void OnVillagerSpawned(VillagerSpawnedEvent e)
    {
        Debug.Log($"[ForgeFlow] Villager spawned: {e.Name} at ({e.SpawnPosition.X}, {e.SpawnPosition.Y})");
    }

    private void OnTutorialCompleted(TutorialCompletedEvent e)
    {
        Debug.Log($"[ForgeFlow] Tutorial mission '{e.MissionId}' completed!");
        _uiManager?.RefreshWindow(WindowIds.TutorialOverlay);
    }

    private void OnTerrainGenerated(TerrainGeneratedEvent e)
    {
        Debug.Log($"[ForgeFlow] Terrain generated: {e.Width}x{e.Height}");
    }

    private void OnThemeChanged(ThemeChangedEvent e)
    {
        Debug.Log($"[ForgeFlow] Theme changed: {e.PreviousThemeId} → {e.NewThemeId}");
        _uiManager?.ApplyThemeToAll();
    }

    private void OnWorkerWornOut(WorkerWornOutEvent e)
    {
        Debug.Log($"[ForgeFlow] Worker #{e.WorkerId} worn out — {e.Reason} (profession: {e.Profession})");
    }

    private void OnGoldChangedEntry(GoldChangedEvent e)
    {
        int diff = e.NewAmount - e.OldAmount;
        if (diff > 0)
        {
            Debug.Log($"[ForgeFlow] Gold earned: +{diff} ({e.Reason}). Balance: {e.NewAmount}");
        }
        else
        {
            Debug.Log($"[ForgeFlow] Gold spent: {diff} ({e.Reason}). Balance: {e.NewAmount}");
        }

        _uiManager?.RefreshWindow(WindowIds.GameplayToolbar);
    }

    // ── Worker Maintenance (from RichWorkerInspectorPanel) ─────────

    private void HandleSendToMaintenance(ulong workerId)
    {
        if (_simulation.EntityManager.HeroIndex.TryGetValue(new EntityId(workerId), out var worker))
        {
            var maintenanceStructures = _simulation.EntityManager.Structures.Values
                .Where(m => m.GetCategoryName() == "ToolStation"
                         || m.GetCategoryName() == "Armory")
                .ToList();

            if (maintenanceStructures.Count > 0)
            {
                var target = maintenanceStructures[0];
                worker.MaintenanceTargetId = target.Id;
                worker.State = HeroState.ReturningToMaintenance;
                Debug.Log($"[ForgeFlow] Worker #{workerId} routed to maintenance at structure #{target.Id}");
            }
            else
            {
                worker.RefreshAfterMaintenance();
                Debug.Log($"[ForgeFlow] Worker #{workerId} refreshed (no maintenance building found)");
            }
        }
    }

    /// <summary>
    /// Binds keyboard hotkeys 1-9 and 0 to activate the corresponding hotbar slot.
    /// </summary>
    private void BindHotkeysToToolbar()
    {
        var toolbar = _uiManager?.GetWindow<GameplayToolbar>(WindowIds.GameplayToolbar);
        if (_inputActions == null || toolbar == null) return;

        _inputActions.Hotkey1.performed += _ => toolbar.ActivateSlotByIndex(0);
        _inputActions.Hotkey2.performed += _ => toolbar.ActivateSlotByIndex(1);
        _inputActions.Hotkey3.performed += _ => toolbar.ActivateSlotByIndex(2);
        _inputActions.Hotkey4.performed += _ => toolbar.ActivateSlotByIndex(3);
        _inputActions.Hotkey5.performed += _ => toolbar.ActivateSlotByIndex(4);
        _inputActions.Hotkey6.performed += _ => toolbar.ActivateSlotByIndex(5);
        _inputActions.Hotkey7.performed += _ => toolbar.ActivateSlotByIndex(6);
        _inputActions.Hotkey8.performed += _ => toolbar.ActivateSlotByIndex(7);
        _inputActions.Hotkey9.performed += _ => toolbar.ActivateSlotByIndex(8);
        _inputActions.Hotkey0.performed += _ => toolbar.ActivateSlotByIndex(9);
    }

    // ── Public API ──────────────────────────────────────────────

    public void SaveGame(string slotName)
    {
        var saveManager = _bootstrapper.Services.Get<SaveManager>();
        var data = saveManager.CreateSaveData(_simulation);
        if (_bootstrapper.LastNewGameSettings != null)
        {
            data.GameName = _bootstrapper.LastNewGameSettings.GameName;
            data.Difficulty = _bootstrapper.LastNewGameSettings.Difficulty;
            data.WorldSeed = _bootstrapper.LastNewGameSettings.Seed;
        }
        var saveDir = Path.Combine(Application.persistentDataPath, "Saves");
        var filePath = Path.Combine(saveDir, $"{slotName}.json");
        saveManager.SaveWithCloud(data, filePath, _simulation.WorldStateManager.PlatformHooks);
        Debug.Log($"[ForgeFlow] Game saved to {filePath}");
    }

    public void PauseSimulation() => _simulation.Pause();
    public void ResumeSimulation() => _simulation.Resume();
    public void TogglePause() => _bootstrapper.Services.Get<GameStateMachine>().TogglePause();

    public void SwitchTheme(string themeId) => _bootstrapper.Services.Get<ThemeService>().SetTheme(themeId);
    public void ToggleHudCustomization() => _uiManager?.ToggleCustomizeMode();
    public void ResetHudLayout() => _uiManager?.ResetLayout();
}
}