using ForgeFlow.Core;
using ForgeFlow.Core.Data;
using ForgeFlow.Core.Entities;
using ForgeFlow.Core.Events;
using ForgeFlow.Core.Localization;
using ForgeFlow.Core.Proto;
using ForgeFlow.Core.Proto.Logic;
using ForgeFlow.Core.Save;
using ForgeFlow.Core.Systems;
using ForgeFlow.Core.Theming;

namespace ForgeFlow.Tests;

/// <summary>
/// Tests for UIStyleMode, ThemeService style mode, settings serialization,
/// entity selection events, and inspector window event coordination.
/// </summary>
public class SettingsAndInspectorSystemTests
{
    // ─── UIStyleMode: Enum Defaults & Values ─────────────────────

    [Fact]
    public void UIStyleMode_HasFunWhimsicalAndMinimalist()
    {
        Assert.Equal(0, (int)UIStyleMode.FunWhimsical);
        Assert.Equal(1, (int)UIStyleMode.MinimalistExpert);
    }

    [Fact]
    public void GameSettings_DefaultsToFunWhimsical()
    {
        var settings = new GameSettings();
        Assert.Equal(UIStyleMode.FunWhimsical, settings.UIStyle);
    }

    [Fact]
    public void GameSettings_UIStyleCanBeSetToMinimalist()
    {
        var settings = new GameSettings();
        settings.UIStyle = UIStyleMode.MinimalistExpert;
        Assert.Equal(UIStyleMode.MinimalistExpert, settings.UIStyle);
    }

    // ─── ThemeService: Style Mode Support ────────────────────────

    [Fact]
    public void ThemeService_DefaultStyleModeIsFunWhimsical()
    {
        var registry = new ThemeRegistry();
        var eventBus = new EventBus();
        var themeService = new ThemeService(registry, eventBus);

        Assert.Equal(UIStyleMode.FunWhimsical, themeService.StyleMode);
        Assert.False(themeService.IsMinimalist);
    }

    [Fact]
    public void ThemeService_SetStyleMode_ChangesToMinimalist()
    {
        var registry = new ThemeRegistry();
        var eventBus = new EventBus();
        var themeService = new ThemeService(registry, eventBus);

        themeService.SetStyleMode(UIStyleMode.MinimalistExpert);

        Assert.Equal(UIStyleMode.MinimalistExpert, themeService.StyleMode);
        Assert.True(themeService.IsMinimalist);
    }

    [Fact]
    public void ThemeService_MinimalistMode_BorderRadiusIs2()
    {
        var registry = new ThemeRegistry();
        var eventBus = new EventBus();
        var themeService = new ThemeService(registry, eventBus);

        int funRadius = themeService.BorderRadius;
        Assert.Equal(4, funRadius);

        themeService.SetStyleMode(UIStyleMode.MinimalistExpert);
        Assert.Equal(2, themeService.BorderRadius);
    }

    [Fact]
    public void ThemeService_MinimalistMode_ReducesPadding()
    {
        var registry = new ThemeRegistry();
        var eventBus = new EventBus();
        var themeService = new ThemeService(registry, eventBus);

        int funPaddingSmall = themeService.PaddingSmall;
        int funPaddingNormal = themeService.PaddingNormal;
        int funPaddingLarge = themeService.PaddingLarge;

        themeService.SetStyleMode(UIStyleMode.MinimalistExpert);

        Assert.True(themeService.PaddingSmall <= funPaddingSmall);
        Assert.True(themeService.PaddingNormal <= funPaddingNormal);
        Assert.True(themeService.PaddingLarge <= funPaddingLarge);
    }

    [Fact]
    public void ThemeService_SetStyleMode_BackToFunWhimsical()
    {
        var registry = new ThemeRegistry();
        var eventBus = new EventBus();
        var themeService = new ThemeService(registry, eventBus);

        themeService.SetStyleMode(UIStyleMode.MinimalistExpert);
        Assert.True(themeService.IsMinimalist);

        themeService.SetStyleMode(UIStyleMode.FunWhimsical);
        Assert.False(themeService.IsMinimalist);
        Assert.Equal(4, themeService.BorderRadius);
    }

    [Fact]
    public void ThemeService_StyleMode_PreservedAcrossThemeSwitch()
    {
        var registry = new ThemeRegistry();
        registry.Register(new ThemeDefinition { Id = "cyber", DisplayName = "Cyber" });
        var eventBus = new EventBus();
        var themeService = new ThemeService(registry, eventBus);

        themeService.SetStyleMode(UIStyleMode.MinimalistExpert);
        themeService.SetTheme("cyber");

        Assert.Equal(UIStyleMode.MinimalistExpert, themeService.StyleMode);
        Assert.True(themeService.IsMinimalist);
        Assert.Equal(2, themeService.BorderRadius);
    }

    [Fact]
    public void ThemeService_StyleMode_DoesNotAffectColors()
    {
        var registry = new ThemeRegistry();
        var eventBus = new EventBus();
        var themeService = new ThemeService(registry, eventBus);

        var colorFun = themeService.GetColor("accent.primary");
        themeService.SetStyleMode(UIStyleMode.MinimalistExpert);
        var colorMin = themeService.GetColor("accent.primary");

        Assert.Equal(colorFun, colorMin);
    }

    // ─── ThemeService: Color Resolution ──────────────────────────

    [Fact]
    public void ThemeService_GetColor_ReturnsValidColors()
    {
        var registry = new ThemeRegistry();
        var eventBus = new EventBus();
        var themeService = new ThemeService(registry, eventBus);

        Assert.NotEqual(ColorRPG.Magenta, themeService.GetColor("accent.primary"));
        Assert.NotEqual(ColorRPG.Magenta, themeService.GetColor("status.success"));
        Assert.NotEqual(ColorRPG.Magenta, themeService.GetColor("status.error"));
        Assert.NotEqual(ColorRPG.Magenta, themeService.GetColor("bg.panel"));
    }

    [Fact]
    public void ThemeService_MissingKey_ReturnsMagenta()
    {
        var registry = new ThemeRegistry();
        var eventBus = new EventBus();
        var themeService = new ThemeService(registry, eventBus);

        Assert.Equal(ColorRPG.Magenta, themeService.GetColor("nonexistent.key"));
    }

    [Fact]
    public void ThemeService_InputBgColor_IsNotMagenta()
    {
        var registry = new ThemeRegistry();
        var eventBus = new EventBus();
        var themeService = new ThemeService(registry, eventBus);

        Assert.NotEqual(ColorRPG.Magenta, themeService.GetColor("input.bg"));
        Assert.NotEqual(ColorRPG.Magenta, themeService.GetColor("input.text"));
    }

    [Fact]
    public void ThemeService_ButtonColors_AreValid()
    {
        var registry = new ThemeRegistry();
        var eventBus = new EventBus();
        var themeService = new ThemeService(registry, eventBus);

        Assert.NotEqual(ColorRPG.Magenta, themeService.GetColor("btn.normal"));
        Assert.NotEqual(ColorRPG.Magenta, themeService.GetColor("btn.hover"));
        Assert.NotEqual(ColorRPG.Magenta, themeService.GetColor("btn.text"));
        Assert.NotEqual(ColorRPG.Magenta, themeService.GetColor("btn.disabled"));
    }

    [Fact]
    public void ThemeService_BorderColors_AreValid()
    {
        var registry = new ThemeRegistry();
        var eventBus = new EventBus();
        var themeService = new ThemeService(registry, eventBus);

        Assert.NotEqual(ColorRPG.Magenta, themeService.GetColor("border.normal"));
        Assert.NotEqual(ColorRPG.Magenta, themeService.GetColor("border.focused"));
        Assert.NotEqual(ColorRPG.Magenta, themeService.GetColor("border.hover"));
    }

    [Fact]
    public void ThemeService_ProgressColors_AreValid()
    {
        var registry = new ThemeRegistry();
        var eventBus = new EventBus();
        var themeService = new ThemeService(registry, eventBus);

        Assert.NotEqual(ColorRPG.Magenta, themeService.GetColor("progress.bg"));
        Assert.NotEqual(ColorRPG.Magenta, themeService.GetColor("progress.fill"));
    }

    // ─── ThemeDefinition: Default Values ─────────────────────────

    [Fact]
    public void ThemeDefinition_DefaultBorderRadiusIs4()
    {
        var theme = new ThemeDefinition();
        Assert.Equal(4, theme.BorderRadius);
    }

    [Fact]
    public void ThemeDefinition_DefaultFontSizes()
    {
        var theme = new ThemeDefinition();
        Assert.Equal(11, theme.FontSizeSmall);
        Assert.Equal(14, theme.FontSizeNormal);
        Assert.Equal(18, theme.FontSizeLarge);
        Assert.Equal(24, theme.FontSizeHeader);
    }

    [Fact]
    public void ThemeDefinition_HasBorderWidth()
    {
        var theme = new ThemeDefinition();
        Assert.True(theme.BorderWidth >= 1);
    }

    [Fact]
    public void ThemeDefinition_HasPaddingValues()
    {
        var theme = new ThemeDefinition();
        Assert.True(theme.PaddingSmall >= 2);
        Assert.True(theme.PaddingNormal >= 4);
        Assert.True(theme.PaddingLarge >= 8);
    }

    [Fact]
    public void ThemeDefinition_HasInputColors()
    {
        var theme = new ThemeDefinition();
        Assert.False(string.IsNullOrEmpty(theme.InputBackground));
        Assert.False(string.IsNullOrEmpty(theme.InputText));
    }

    // ─── Settings Serialization ───────────────────────────────────

    [Fact]
    public void SettingsManager_SerializesUIStyle()
    {
        var eventBus = new EventBus();
        var mgr = new SettingsManager(eventBus);
        mgr.Settings.UIStyle = UIStyleMode.MinimalistExpert;

        var json = mgr.SerializeToJson();
        Assert.Contains("UIStyle", json);
        Assert.Contains("1", json);
    }

    [Fact]
    public void SettingsManager_DeserializesUIStyle()
    {
        var eventBus = new EventBus();
        var mgr = new SettingsManager(eventBus);
        mgr.Settings.UIStyle = UIStyleMode.MinimalistExpert;

        var json = mgr.SerializeToJson();

        var mgr2 = new SettingsManager(eventBus);
        mgr2.DeserializeFromJson(json);

        Assert.Equal(UIStyleMode.MinimalistExpert, mgr2.Settings.UIStyle);
    }

    [Fact]
    public void GameSettings_UIStyle_DefaultSerializesToFunWhimsical()
    {
        var eventBus = new EventBus();
        var mgr = new SettingsManager(eventBus);

        var json = mgr.SerializeToJson();
        Assert.Contains("UIStyle", json);
        Assert.Contains("\"UIStyle\": 0", json);
    }

    [Fact]
    public void GameSettings_FullRoundTrip_PreservesAllFields()
    {
        var eventBus = new EventBus();
        var mgr = new SettingsManager(eventBus);

        mgr.Settings.UIStyle = UIStyleMode.MinimalistExpert;
        mgr.Settings.ThemeId = "cyber";
        mgr.Settings.Language = "de";
        mgr.Settings.MasterVolume = 0.5f;

        var json = mgr.SerializeToJson();
        var mgr2 = new SettingsManager(eventBus);
        mgr2.DeserializeFromJson(json);

        Assert.Equal(UIStyleMode.MinimalistExpert, mgr2.Settings.UIStyle);
        Assert.Equal("cyber", mgr2.Settings.ThemeId);
        Assert.Equal("de", mgr2.Settings.Language);
        Assert.Equal(0.5f, mgr2.Settings.MasterVolume, 3);
    }

    [Fact]
    public void SettingsManager_Apply_PublishesEvent()
    {
        var eventBus = new EventBus();
        var mgr = new SettingsManager(eventBus);

        SettingsChangedEvent? captured = null;
        eventBus.Subscribe<SettingsChangedEvent>(e => captured = e);

        mgr.Apply(new GameSettings { UIStyle = UIStyleMode.MinimalistExpert, MasterVolume = 0.7f });

        Assert.NotNull(captured);
        Assert.Equal(0.7f, captured.Value.MasterVolume, 3);
    }

    // ─── Settings + ThemeService Integration ─────────────────────

    [Fact]
    public void Settings_And_ThemeService_StyleModeIntegration()
    {
        var registry = new ThemeRegistry();
        var eventBus = new EventBus();
        var settingsManager = new SettingsManager(eventBus);
        var themeService = new ThemeService(registry, eventBus);

        settingsManager.Settings.UIStyle = UIStyleMode.MinimalistExpert;
        themeService.SetStyleMode(settingsManager.Settings.UIStyle);
        Assert.True(themeService.IsMinimalist);
        Assert.Equal(2, themeService.BorderRadius);

        settingsManager.Settings.UIStyle = UIStyleMode.FunWhimsical;
        themeService.SetStyleMode(settingsManager.Settings.UIStyle);
        Assert.False(themeService.IsMinimalist);
        Assert.Equal(4, themeService.BorderRadius);
    }

    // ─── Localization: Phase 13 Keys ─────────────────────────────

    [Fact]
    public void LocalizationKeys_Phase13ConstantsExist()
    {
        Assert.Equal("ui.settings.ui_style", LocalizationKeys.SettingsUIStyle);
        Assert.Equal("ui.style.fun_whimsical", LocalizationKeys.UIStyleFunWhimsical);
        Assert.Equal("ui.style.minimalist", LocalizationKeys.UIStyleMinimalist);
        Assert.Equal("ui.theme_editor.title", LocalizationKeys.ThemeEditorTitle);
        Assert.Equal("ui.theme_editor.reset", LocalizationKeys.ThemeEditorReset);
        Assert.Equal("ui.theme_editor.preview", LocalizationKeys.ThemeEditorPreview);
        Assert.Equal("ui.item_overlay.empty", LocalizationKeys.ItemOverlayEmpty);
        Assert.Equal("ui.status_badge.default", LocalizationKeys.StatusBadgeDefault);
        Assert.Equal("ui.mainmenu.version", LocalizationKeys.MainMenuVersion);
        Assert.Equal("ui.mainmenu.mod_browser", LocalizationKeys.MainMenuModBrowser);
        Assert.Equal("ui.pause.quick_save", LocalizationKeys.PauseQuickSave);
        Assert.Equal("ui.pause.quick_load", LocalizationKeys.PauseQuickLoad);
        Assert.Equal("ui.hud.resource_bar", LocalizationKeys.HudResourceBar);
        Assert.Equal("ui.hud.minimap", LocalizationKeys.HudMinimap);
        Assert.Equal("ui.hud.toolbar", LocalizationKeys.HudToolbar);
        Assert.Equal("ui.settings.reset_defaults", LocalizationKeys.SettingsResetDefaults);
        Assert.Equal("ui.settings.rebind_key", LocalizationKeys.SettingsRebindKey);
        Assert.Equal("ui.settings.press_key", LocalizationKeys.SettingsPressKey);
    }

    [Fact]
    public void Localization_AllPhase13KeysAreTranslated()
    {
        var ts = new TranslationService();
        ts.LoadFromEmbeddedJson();
        ts.SetLanguage("en");

        var requiredKeys = new[]
        {
            "ui.settings.ui_style", "ui.style.fun_whimsical", "ui.style.minimalist",
            "ui.theme_editor.title", "ui.theme_editor.reset", "ui.theme_editor.preview",
            "ui.item_overlay.empty", "ui.status_badge.default", "ui.mainmenu.version",
            "ui.mainmenu.mod_browser", "ui.pause.quick_save", "ui.pause.quick_load",
            "ui.hud.resource_bar", "ui.hud.minimap", "ui.hud.toolbar",
            "ui.settings.reset_defaults", "ui.settings.rebind_key", "ui.settings.press_key",
        };

        foreach (var key in requiredKeys)
        {
            Assert.NotEqual(key, ts.Get(key));
        }
    }

    [Fact]
    public void Localization_Phase13_IncreasesKeyCount()
    {
        var ts = new TranslationService();
        ts.LoadFromEmbeddedJson();
        ts.SetLanguage("en");

        Assert.True(ts.KeyCount >= 150, $"Expected 150+ localization keys, got {ts.KeyCount}");
    }

    [Fact]
    public void LocalizationKeys_Phase13_NoDuplicateValues()
    {
        var seen = new HashSet<string>();
        var keys = new[]
        {
            LocalizationKeys.SettingsUIStyle, LocalizationKeys.UIStyleFunWhimsical,
            LocalizationKeys.UIStyleMinimalist, LocalizationKeys.ThemeEditorTitle,
            LocalizationKeys.ThemeEditorReset, LocalizationKeys.ThemeEditorPreview,
            LocalizationKeys.ItemOverlayEmpty, LocalizationKeys.StatusBadgeDefault,
            LocalizationKeys.MainMenuVersion, LocalizationKeys.MainMenuModBrowser,
            LocalizationKeys.PauseQuickSave, LocalizationKeys.PauseQuickLoad,
            LocalizationKeys.HudResourceBar, LocalizationKeys.HudMinimap,
            LocalizationKeys.HudToolbar, LocalizationKeys.SettingsResetDefaults,
            LocalizationKeys.SettingsRebindKey, LocalizationKeys.SettingsPressKey,
        };

        foreach (var key in keys)
        {
            Assert.True(seen.Add(key), $"Duplicate localization key constant: {key}");
        }
    }

    // ─── Entity Selection Events ──────────────────────────────────

    [Fact]
    public void EntitySelectedEvent_PublishesCorrectData()
    {
        var eventBus = new EventBus();
        EntitySelectedEvent? received = null;
        eventBus.Subscribe<EntitySelectedEvent>(e => received = e);

        eventBus.Publish(new EntitySelectedEvent(new EntityId(42), "ForestryStructure", new GridPosRPG(5, 10)));

        Assert.NotNull(received);
        Assert.Equal(42ul, received.Value.EntityId);
        Assert.Equal("ForestryStructure", received.Value.EntityType);
        Assert.Equal(5, received.Value.Position.X);
        Assert.Equal(10, received.Value.Position.Y);
    }

    [Fact]
    public void EntityDeselectedEvent_PublishesCorrectly()
    {
        var eventBus = new EventBus();
        bool received = false;
        eventBus.Subscribe<EntityDeselectedEvent>(_ => received = true);

        eventBus.Publish(new EntityDeselectedEvent());

        Assert.True(received);
    }

    [Fact]
    public void EntitySelectedEvent_MultipleSubscribers_AllReceive()
    {
        var eventBus = new EventBus();
        int count = 0;
        eventBus.Subscribe<EntitySelectedEvent>(_ => count++);
        eventBus.Subscribe<EntitySelectedEvent>(_ => count++);
        eventBus.Subscribe<EntitySelectedEvent>(_ => count++);

        eventBus.Publish(new EntitySelectedEvent(new EntityId(1), "PathGate", new GridPosRPG(0, 0)));

        Assert.Equal(3, count);
    }

    [Fact]
    public void EntitySelectedEvent_UnsubscribedHandler_DoesNotReceive()
    {
        var eventBus = new EventBus();
        int count = 0;
        Action<EntitySelectedEvent> handler = _ => count++;
        eventBus.Subscribe(handler);

        eventBus.Publish(new EntitySelectedEvent(new EntityId(1), "PathSegment", new GridPosRPG(0, 0)));
        Assert.Equal(1, count);

        eventBus.Unsubscribe(handler);
        eventBus.Publish(new EntitySelectedEvent(new EntityId(2), "PathSegment", new GridPosRPG(1, 1)));
        Assert.Equal(1, count);
    }

    [Theory]
    [InlineData("PathSegment")]
    [InlineData("PathGate")]
    [InlineData("ForestryStructure")]
    [InlineData("MiningStructure")]
    [InlineData("VillageSpawner")]
    [InlineData("Inn")]
    [InlineData("CraftStation")]
    [InlineData("DungeonPortal")]
    public void EntitySelectedEvent_SupportsAllEntityTypes(string entityType)
    {
        var eventBus = new EventBus();
        string? receivedType = null;
        eventBus.Subscribe<EntitySelectedEvent>(e => receivedType = e.EntityType);

        eventBus.Publish(new EntitySelectedEvent(new EntityId(1), entityType, new GridPosRPG(0, 0)));

        Assert.Equal(entityType, receivedType);
    }

    [Fact]
    public void RapidEntitySelections_AllProcessedInOrder()
    {
        var eventBus = new EventBus();
        var receivedIds = new List<ulong>();
        eventBus.Subscribe<EntitySelectedEvent>(e => receivedIds.Add(e.EntityId));

        for (ulong i = 1; i <= 20; i++)
        {
            eventBus.Publish(new EntitySelectedEvent(new EntityId(i), "Structure", new GridPosRPG((int)i, 0)));
        }

        Assert.Equal(20, receivedIds.Count);
        for (int i = 0; i < 20; i++)
        {
            Assert.Equal((ulong)(i + 1), receivedIds[i]);
        }
    }

    [Fact]
    public void SelectionFollowedByDeselection_ProperEventOrder()
    {
        var eventBus = new EventBus();
        var events = new List<string>();
        eventBus.Subscribe<EntitySelectedEvent>(_ => events.Add("selected"));
        eventBus.Subscribe<EntityDeselectedEvent>(_ => events.Add("deselected"));

        eventBus.Publish(new EntitySelectedEvent(new EntityId(1), "Forestry", new GridPosRPG(0, 0)));
        eventBus.Publish(new EntityDeselectedEvent());
        eventBus.Publish(new EntitySelectedEvent(new EntityId(2), "Mining", new GridPosRPG(1, 1)));

        Assert.Equal(3, events.Count);
        Assert.Equal("selected", events[0]);
        Assert.Equal("deselected", events[1]);
        Assert.Equal("selected", events[2]);
    }

    // ─── Window Events ────────────────────────────────────────────

    [Fact]
    public void WindowOpenedEvent_PublishesWindowId()
    {
        var eventBus = new EventBus();
        string? receivedId = null;
        eventBus.Subscribe<WindowOpenedEvent>(e => receivedId = e.WindowId);

        eventBus.Publish(new WindowOpenedEvent("worker_inspector"));

        Assert.Equal("worker_inspector", receivedId);
    }

    [Fact]
    public void WindowClosedEvent_PublishesWindowId()
    {
        var eventBus = new EventBus();
        string? receivedId = null;
        eventBus.Subscribe<WindowClosedEvent>(e => receivedId = e.WindowId);

        eventBus.Publish(new WindowClosedEvent("worker_inspector_pinned"));

        Assert.Equal("worker_inspector_pinned", receivedId);
    }

    [Fact]
    public void WindowOpenedEvent_RoundTrip_PreservesId()
    {
        var eventBus = new EventBus();
        string? captured = null;
        eventBus.Subscribe<WindowOpenedEvent>(e => captured = e.WindowId);

        eventBus.Publish(new WindowOpenedEvent("worker_inspector"));
        Assert.Equal("worker_inspector", captured);

        eventBus.Publish(new WindowOpenedEvent("structure_inspector"));
        Assert.Equal("structure_inspector", captured);
    }

    [Fact]
    public void WindowClosedEvent_PinnedSuffix_Tracked()
    {
        var eventBus = new EventBus();
        var closedIds = new List<string>();
        eventBus.Subscribe<WindowClosedEvent>(e => closedIds.Add(e.WindowId));

        eventBus.Publish(new WindowClosedEvent("worker_inspector_pinned"));
        eventBus.Publish(new WindowClosedEvent("worker_inspector"));

        Assert.Equal(2, closedIds.Count);
        Assert.Equal("worker_inspector_pinned", closedIds[0]);
        Assert.Equal("worker_inspector", closedIds[1]);
    }

    // ─── Inspector Pinning Logic ──────────────────────────────────

    [Fact]
    public void PinnedInspector_DoesNotReceiveNewSelections()
    {
        var eventBus = new EventBus();
        int selectedCount = 0;
        bool isPinned = false;

        eventBus.Subscribe<EntitySelectedEvent>(e =>
        {
            if (isPinned) { return; }
            selectedCount++;
        });

        eventBus.Publish(new EntitySelectedEvent(new EntityId(1), "Forestry", new GridPosRPG(0, 0)));
        Assert.Equal(1, selectedCount);

        isPinned = true;
        eventBus.Publish(new EntitySelectedEvent(new EntityId(2), "Mining", new GridPosRPG(1, 1)));
        Assert.Equal(1, selectedCount);
    }

    [Fact]
    public void PinnedInspector_DoesNotClearOnDeselect()
    {
        var eventBus = new EventBus();
        bool wasCleared = false;
        bool isPinned = false;

        eventBus.Subscribe<EntityDeselectedEvent>(_ =>
        {
            if (isPinned) { return; }
            wasCleared = true;
        });

        eventBus.Publish(new EntityDeselectedEvent());
        Assert.True(wasCleared);

        isPinned = true;
        wasCleared = false;
        eventBus.Publish(new EntityDeselectedEvent());
        Assert.False(wasCleared);
    }

    [Fact]
    public void MultiInstance_DisposedSubscriberStopsReceiving()
    {
        var eventBus = new EventBus();
        int handler1Count = 0;
        int handler2Count = 0;

        Action<EntitySelectedEvent> handler1 = _ => handler1Count++;
        Action<EntitySelectedEvent> handler2 = _ => handler2Count++;

        eventBus.Subscribe(handler1);
        eventBus.Subscribe(handler2);

        eventBus.Publish(new EntitySelectedEvent(new EntityId(1), "Spawner", new GridPosRPG(0, 0)));
        Assert.Equal(1, handler1Count);
        Assert.Equal(1, handler2Count);

        eventBus.Unsubscribe(handler1);
        eventBus.Publish(new EntitySelectedEvent(new EntityId(2), "Inn", new GridPosRPG(1, 1)));
        Assert.Equal(1, handler1Count);
        Assert.Equal(2, handler2Count);
    }

    [Fact]
    public void WindowClose_PublishesClosedEvent_ThenReopensOnNextSelect()
    {
        var eventBus = new EventBus();
        var events = new List<string>();

        eventBus.Subscribe<WindowOpenedEvent>(e => events.Add($"opened:{e.WindowId}"));
        eventBus.Subscribe<WindowClosedEvent>(e => events.Add($"closed:{e.WindowId}"));
        eventBus.Subscribe<EntitySelectedEvent>(_ => events.Add("entity_selected"));

        eventBus.Subscribe<EntitySelectedEvent>(_ =>
            eventBus.Publish(new WindowOpenedEvent("worker_inspector")));

        eventBus.Publish(new WindowClosedEvent("worker_inspector"));
        eventBus.Publish(new EntitySelectedEvent(new EntityId(1), "Forestry", new GridPosRPG(0, 0)));

        Assert.Equal(3, events.Count);
        Assert.Equal("closed:worker_inspector", events[0]);
        Assert.Equal("entity_selected", events[1]);
        Assert.Equal("opened:worker_inspector", events[2]);
    }

    // ─── Entity Selection via EntityManager ───────────────────────

    [Fact]
    public void EntitySelection_StructureById_ResolvedByEntityManager()
    {
        var bootstrapper = new GameBootstrapper();
        var simulation = bootstrapper.Bootstrap("./TestMods");
        EntityBase.ResetIdCounter();

        var pos = new GridPosRPG(3, 3);
        var structure = new ForestryRecipeEntity(EntityId.Next()) { Position = pos };
        simulation.EntityManager.AddStructure(structure, pos);

        var found = simulation.EntityManager.GetStructure(structure.Id);
        Assert.NotNull(found);
        Assert.Equal(structure.Id, found!.Id);
        Assert.Equal("Forestry", found.GetCategoryName());
    }

    [Fact]
    public void EntitySelection_StructureByPosition_ResolvedByEntityManager()
    {
        var bootstrapper = new GameBootstrapper();
        var simulation = bootstrapper.Bootstrap("./TestMods");
        EntityBase.ResetIdCounter();

        var pos = new GridPosRPG(7, 4);
        var structure = new MiningRecipeEntity(EntityId.Next()) { Position = pos };
        simulation.EntityManager.AddStructure(structure, pos);

        var found = simulation.EntityManager.GetStructureAt(pos);
        Assert.NotNull(found);
        Assert.Equal(structure.Id, found!.Id);
    }

    [Fact]
    public void EntitySelection_HeroById_ResolvedByEntityManager()
    {
        var bootstrapper = new GameBootstrapper();
        var simulation = bootstrapper.Bootstrap("./TestMods");
        EntityBase.ResetIdCounter();

        var hero = new HeroEntity(EntityId.Next()) { ClassId = "warrior" };
        simulation.EntityManager.AddHero(hero);

        Assert.True(simulation.EntityManager.HeroIndex.ContainsKey(hero.Id));
        var found = simulation.EntityManager.HeroIndex[hero.Id];
        Assert.Equal("warrior", found.ClassId);
    }

    [Fact]
    public void EntitySelection_PathSegment_ResolvedByPosition()
    {
        var bootstrapper = new GameBootstrapper();
        var simulation = bootstrapper.Bootstrap("./TestMods");
        EntityBase.ResetIdCounter();

        var pos = new GridPosRPG(2, 2);
        simulation.PathNodeManager.AddPathSegment(pos, Direction.East);

        var seg = simulation.EntityManager.GetPathSegmentAt(pos);
        Assert.NotNull(seg);
        Assert.Equal(pos, seg!.Position);
    }

    [Fact]
    public void EntitySelection_PathGate_ResolvedByPosition()
    {
        var bootstrapper = new GameBootstrapper();
        var simulation = bootstrapper.Bootstrap("./TestMods");
        EntityBase.ResetIdCounter();

        var pos = new GridPosRPG(4, 4);
        simulation.PathGateManager.AddPathGate(pos, Direction.North);

        var gate = simulation.EntityManager.GetPathGateAt(pos);
        Assert.NotNull(gate);
        Assert.Equal(pos, gate!.Position);
    }

    // ─── GameStateMachine Integration with Settings ───────────────

    [Fact]
    public void GameStateMachine_FullFlow_WithUIStyleSetting()
    {
        var eventBus = new EventBus();
        var bootstrapper = new GameBootstrapper();
        var simulation = bootstrapper.Bootstrap("./TestMods");
        var gsm = new GameStateMachine(eventBus, simulation);

        bootstrapper.Services.Get<SettingsManager>().Settings.UIStyle = UIStyleMode.MinimalistExpert;

        gsm.StartNewGameSetup();
        Assert.Equal(GameState.NewGameSetup, gsm.Current);

        gsm.StartNewGame();
        Assert.Equal(GameState.Playing, gsm.Current);

        gsm.TogglePause();
        Assert.Equal(GameState.Paused, gsm.Current);

        gsm.TogglePause();
        Assert.Equal(GameState.Playing, gsm.Current);
    }

    // ─── Phase7GameShell: Settings Manager ────────────────────────

    [Fact]
    public void SettingsManager_Apply_ReplacesAllSettings()
    {
        var bus = new EventBus();
        var settings = new SettingsManager(bus);

        var newSettings = new GameSettings
        {
            MasterVolume = 0.5f,
            MusicVolume = 0.3f,
            ResolutionWidth = 2560,
            ResolutionHeight = 1440,
            Fullscreen = false,
            GraphicsQuality = 3,
            Language = "de"
        };

        settings.Apply(newSettings);

        Assert.Equal(0.5f, settings.Settings.MasterVolume);
        Assert.Equal(2560, settings.Settings.ResolutionWidth);
        Assert.Equal("de", settings.Settings.Language);
    }

    [Fact]
    public void SettingsManager_Serialization_PreservesLanguage()
    {
        var bus = new EventBus();
        var settings = new SettingsManager(bus);
        settings.Settings.Language = "ja";

        var json = settings.SerializeToJson();
        var settings2 = new SettingsManager(bus);
        settings2.DeserializeFromJson(json);

        Assert.Equal("ja", settings2.Settings.Language);
    }
}
