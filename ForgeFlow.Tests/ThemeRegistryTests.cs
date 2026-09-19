using ForgeFlow.Core.Entities;
using ForgeFlow.Core.Events;
using ForgeFlow.Core.Localization;
using ForgeFlow.Core.Systems;
using ForgeFlow.Core.Theming;

namespace ForgeFlow.Tests;

public class ThemeRegistryTests
{
    // ─── ThemeRegistry Tests ───────────────────────────────────

    [Fact]
    public void ThemeRegistry_RegisterDefaults_ContainsFourThemes()
    {
        var registry = new ThemeRegistry();
        registry.RegisterDefaults();

        Assert.Contains("dark_factory", registry.AvailableThemeIds);
        Assert.Contains("cyber", registry.AvailableThemeIds);
        Assert.Contains("medieval", registry.AvailableThemeIds);
        Assert.Contains("neon", registry.AvailableThemeIds);
    }

    [Fact]
    public void ThemeRegistry_Get_ReturnsTheme()
    {
        var registry = new ThemeRegistry();
        registry.RegisterDefaults();

        var theme = registry.Get("cyber");
        Assert.NotNull(theme);
        Assert.Equal("Cyber", theme!.DisplayName);
    }

    [Fact]
    public void ThemeRegistry_Get_UnknownReturnsNull()
    {
        var registry = new ThemeRegistry();
        registry.RegisterDefaults();

        Assert.Null(registry.Get("nonexistent_theme"));
    }

    [Fact]
    public void ThemeRegistry_LoadFromEmbeddedResources_LoadsThemes()
    {
        var registry = new ThemeRegistry();
        registry.LoadFromEmbeddedResources();

        Assert.Contains("dark_factory", registry.AvailableThemeIds);
        Assert.Contains("neon", registry.AvailableThemeIds);
    }

    // ─── ThemeDefinition Tests ─────────────────────────────────

    [Fact]
    public void ThemeDefinition_GetColor_ReturnsCorrectColor()
    {
        var theme = new ThemeDefinition();
        var color = theme.GetColor("text.primary");

        // Default dark_factory text.primary = #E0E0E0
        Assert.True(color.R > 0.8f);
        Assert.True(color.G > 0.8f);
        Assert.True(color.B > 0.8f);
    }

    [Fact]
    public void ThemeDefinition_GetColor_UnknownKeyReturnsMagenta()
    {
        var theme = new ThemeDefinition();
        var color = theme.GetColor("unknown.key");

        Assert.Equal(ColorRPG.Magenta, color);
    }

    // ─── ThemeService Tests ────────────────────────────────────

    [Fact]
    public void ThemeService_SetTheme_PublishesEvent()
    {
        var eventBus = new EventBus();
        var registry = new ThemeRegistry();
        registry.RegisterDefaults();
        var service = new ThemeService(registry, eventBus);

        ThemeChangedEvent? received = null;
        eventBus.Subscribe<ThemeChangedEvent>(e => received = e);

        service.SetTheme("cyber");

        Assert.NotNull(received);
        Assert.Equal("dark_factory", received!.Value.PreviousThemeId);
        Assert.Equal("cyber", received.Value.NewThemeId);
    }

    [Fact]
    public void ThemeService_SetTheme_ReturnsFalseForUnknown()
    {
        var eventBus = new EventBus();
        var registry = new ThemeRegistry();
        registry.RegisterDefaults();
        var service = new ThemeService(registry, eventBus);

        Assert.False(service.SetTheme("nonexistent"));
        Assert.Equal("dark_factory", service.ActiveThemeId);
    }

    [Fact]
    public void ThemeService_GetColor_DelegatesToActiveTheme()
    {
        var eventBus = new EventBus();
        var registry = new ThemeRegistry();
        registry.RegisterDefaults();
        var service = new ThemeService(registry, eventBus);

        service.SetTheme("cyber");
        var color = service.GetColor("text.primary");

        // Cyber text.primary = #00FF88
        Assert.True(color.G > 0.9f);
        Assert.True(color.R < 0.1f);
    }

    [Fact]
    public void ThemeService_FontSizes_ReadFromActiveTheme()
    {
        var eventBus = new EventBus();
        var registry = new ThemeRegistry();
        registry.RegisterDefaults();
        var service = new ThemeService(registry, eventBus);

        Assert.Equal(14, service.FontSizeNormal);
        Assert.Equal(11, service.FontSizeSmall);
        Assert.Equal(18, service.FontSizeLarge);
        Assert.Equal(24, service.FontSizeHeader);
    }

    // ─── ThemeColor Tests ──────────────────────────────────────

    [Fact]
    public void ThemeColor_ConstructsWithKeyAndColor()
    {
        var tc = new ThemeColor("bg.primary", ColorRPG.Red);
        Assert.Equal("bg.primary", tc.Key);
        Assert.Equal(ColorRPG.Red, tc.Color);
    }

    [Fact]
    public void ThemeColor_ConstructsFromHex()
    {
        var tc = new ThemeColor("test", "#FF0000");
        Assert.Equal(1f, tc.Color.R, 0.01f);
        Assert.Equal(0f, tc.Color.G, 0.01f);
        Assert.Equal(0f, tc.Color.B, 0.01f);
    }

    [Fact]
    public void ThemeColor_Equality()
    {
        var a = new ThemeColor("key", ColorRPG.Blue);
        var b = new ThemeColor("key", ColorRPG.Blue);
        var c = new ThemeColor("other", ColorRPG.Blue);

        Assert.Equal(a, b);
        Assert.NotEqual(a, c);
    }

    // ─── LocalizationKeys Tests ────────────────────────────────

    [Fact]
    public void LocalizationKeys_AllKeysExistInDefaultEnglish()
    {
        var ts = new TranslationService();
        ts.LoadFromEmbeddedJson();

        // Spot-check a sample of keys
        Assert.True(ts.HasKey(LocalizationKeys.GameTitle));
        Assert.True(ts.HasKey(LocalizationKeys.SettingsTheme));
        Assert.True(ts.HasKey(LocalizationKeys.CameraZoomSpeed));
        Assert.True(ts.HasKey(LocalizationKeys.InspectorTitle));
        Assert.True(ts.HasKey(LocalizationKeys.RecipePickerTitle));
        Assert.True(ts.HasKey(LocalizationKeys.ConfirmOk));
        Assert.True(ts.HasKey(LocalizationKeys.HudCustomizeMode));
        Assert.True(ts.HasKey(LocalizationKeys.ActionSave));
        Assert.True(ts.HasKey(LocalizationKeys.ThemeDarkFactory));
        Assert.True(ts.HasKey(LocalizationKeys.ThemeCyber));
    }

    [Fact]
    public void TranslationService_Phase8Keys_ReturnTranslatedStrings()
    {
        var ts = new TranslationService();
        ts.LoadFromEmbeddedJson();

        Assert.Equal("Theme", ts.Get(LocalizationKeys.SettingsTheme));
        Assert.Equal("Camera", ts.Get(LocalizationKeys.SettingsCamera));
        Assert.Equal("Customize HUD", ts.Get(LocalizationKeys.SettingsCustomizeHud));
        Assert.Equal("Inspector", ts.Get(LocalizationKeys.InspectorTitle));
        Assert.Equal("OK", ts.Get(LocalizationKeys.ConfirmOk));
        Assert.Equal("Dark Factory", ts.Get(LocalizationKeys.ThemeDarkFactory));
    }

    // ─── CameraSettings Tests ──────────────────────────────────

    [Fact]
    public void CameraSettings_Defaults()
    {
        var settings = new CameraSettings();

        Assert.Equal(2f, settings.ZoomSpeed);
        Assert.Equal(15f, settings.PanSpeed);
        Assert.Equal(90f, settings.RotationSpeed);
        Assert.Equal(3f, settings.MinZoom);
        Assert.Equal(30f, settings.MaxZoom);
        Assert.Equal(12f, settings.DefaultZoom);
        Assert.True(settings.SnapRotation);
    }

    // ─── HudPanelLayout Tests ──────────────────────────────────

    [Fact]
    public void HudPanelLayout_Defaults()
    {
        var layout = new HudPanelLayout();

        Assert.Equal("", layout.PanelId);
        Assert.Equal(300f, layout.Width);
        Assert.Equal(200f, layout.Height);
        Assert.True(layout.Visible);
        Assert.False(layout.Pinned);
    }

    // ─── GameSettings Extended Tests ───────────────────────────

    [Fact]
    public void GameSettings_HasThemeId_DefaultsDarkFactory()
    {
        var settings = new GameSettings();
        Assert.Equal("dark_factory", settings.ThemeId);
    }

    [Fact]
    public void GameSettings_HasCameraSettings()
    {
        var settings = new GameSettings();
        Assert.NotNull(settings.Camera);
        Assert.Equal(12f, settings.Camera.DefaultZoom);
    }

    [Fact]
    public void GameSettings_HasHudLayout()
    {
        var settings = new GameSettings();
        Assert.NotNull(settings.HudLayout);
        Assert.Empty(settings.HudLayout);
    }

    [Fact]
    public void GameSettings_SerializesAndDeserializesWithNewFields()
    {
        var eventBus = new EventBus();
        var manager = new SettingsManager(eventBus);
        manager.Settings.ThemeId = "cyber";
        manager.Settings.Camera.ZoomSpeed = 5f;
        manager.Settings.HudLayout.Add(new HudPanelLayout
        {
            PanelId = "inspector",
            X = 100f,
            Y = 200f,
            Width = 350f,
            Height = 400f,
            Pinned = true,
            Visible = true
        });

        var json = manager.SerializeToJson();
        Assert.Contains("cyber", json);
        Assert.Contains("inspector", json);

        var manager2 = new SettingsManager(eventBus);
        manager2.DeserializeFromJson(json);

        Assert.Equal("cyber", manager2.Settings.ThemeId);
        Assert.Equal(5f, manager2.Settings.Camera.ZoomSpeed);
        Assert.Single(manager2.Settings.HudLayout);
        Assert.Equal("inspector", manager2.Settings.HudLayout[0].PanelId);
        Assert.Equal(350f, manager2.Settings.HudLayout[0].Width);
        Assert.True(manager2.Settings.HudLayout[0].Pinned);
    }

    // ─── GameBootstrapper Integration Tests ────────────────────

    [Fact]
    public void GameBootstrapper_RegistersThemeService()
    {
        var bootstrapper = new Core.GameBootstrapper();
        bootstrapper.Bootstrap();

        Assert.NotNull(bootstrapper.Services.Get<ThemeRegistry>());
        Assert.NotNull(bootstrapper.Services.Get<ThemeService>());
        Assert.Equal("dark_factory", bootstrapper.Services.Get<ThemeService>().ActiveThemeId);
    }

    [Fact]
    public void GameBootstrapper_ThemeService_CanSwitchTheme()
    {
        var bootstrapper = new Core.GameBootstrapper();
        bootstrapper.Bootstrap();

        Assert.True(bootstrapper.Services.Get<ThemeService>().SetTheme("cyber"));
        Assert.Equal("cyber", bootstrapper.Services.Get<ThemeService>().ActiveThemeId);
    }

    // ─── Events Tests ──────────────────────────────────────────

    [Fact]
    public void ThemeChangedEvent_CarriesPreviousAndNewId()
    {
        var evt = new ThemeChangedEvent("old", "new");
        Assert.Equal("old", evt.PreviousThemeId);
        Assert.Equal("new", evt.NewThemeId);
    }

    [Fact]
    public void EntitySelectedEvent_CarriesEntityInfo()
    {
        var evt = new EntitySelectedEvent(new EntityId(42), "Structure", new GridPosRPG(5, 10));
        Assert.Equal(42ul, evt.EntityId);
        Assert.Equal("Structure", evt.EntityType);
        Assert.Equal(5, evt.Position.X);
        Assert.Equal(10, evt.Position.Y);
    }

    [Fact]
    public void HudLayoutChangedEvent_CarriesPanelId()
    {
        var evt = new HudLayoutChangedEvent("inspector");
        Assert.Equal("inspector", evt.PanelId);
    }
}
