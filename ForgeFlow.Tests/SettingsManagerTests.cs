using ForgeFlow.Core.Events;
using ForgeFlow.Core.Systems;

namespace ForgeFlow.Tests;

/// <summary>Tests for SettingsManager — volumes, resolution, graphics quality, keybindings, and serialization.</summary>
public class SettingsManagerTests
{
    [Fact]
    public void SettingsManager_DefaultSettings()
    {
        var bus = new EventBus();
        var settings = new SettingsManager(bus);

        Assert.Equal(1.0f, settings.Settings.MasterVolume);
        Assert.Equal(1920, settings.Settings.ResolutionWidth);
        Assert.Equal(1080, settings.Settings.ResolutionHeight);
        Assert.True(settings.Settings.Fullscreen);
        Assert.Equal(2, settings.Settings.GraphicsQuality);
    }

    [Fact]
    public void SettingsManager_SetMasterVolume_PublishesEvent()
    {
        var bus = new EventBus();
        var settings = new SettingsManager(bus);

        SettingsChangedEvent? received = null;
        bus.Subscribe<SettingsChangedEvent>(e => received = e);

        settings.SetMasterVolume(0.5f);

        Assert.Equal(0.5f, settings.Settings.MasterVolume);
        Assert.NotNull(received);
        Assert.Equal(0.5f, received.Value.MasterVolume);
    }

    [Fact]
    public void SettingsManager_SetMasterVolume_Clamps()
    {
        var bus = new EventBus();
        var settings = new SettingsManager(bus);

        settings.SetMasterVolume(5.0f);
        Assert.Equal(1.0f, settings.Settings.MasterVolume);

        settings.SetMasterVolume(-1.0f);
        Assert.Equal(0.0f, settings.Settings.MasterVolume);
    }

    [Fact]
    public void SettingsManager_SetResolution()
    {
        var bus = new EventBus();
        var settings = new SettingsManager(bus);

        settings.SetResolution(2560, 1440, false);

        Assert.Equal(2560, settings.Settings.ResolutionWidth);
        Assert.Equal(1440, settings.Settings.ResolutionHeight);
        Assert.False(settings.Settings.Fullscreen);
    }

    [Fact]
    public void SettingsManager_SetGraphicsQuality_Clamps()
    {
        var bus = new EventBus();
        var settings = new SettingsManager(bus);

        settings.SetGraphicsQuality(5);
        Assert.Equal(3, settings.Settings.GraphicsQuality);

        settings.SetGraphicsQuality(-1);
        Assert.Equal(0, settings.Settings.GraphicsQuality);
    }

    [Fact]
    public void SettingsManager_KeyBindings()
    {
        var bus = new EventBus();
        var settings = new SettingsManager(bus);

        Assert.Equal("Escape", settings.GetKeyBinding("pause"));

        settings.SetKeyBinding("pause", "P");
        Assert.Equal("P", settings.GetKeyBinding("pause"));

        Assert.Null(settings.GetKeyBinding("nonexistent"));
    }

    [Fact]
    public void SettingsManager_SerializeAndDeserialize_Roundtrips()
    {
        var bus = new EventBus();
        var settings = new SettingsManager(bus);

        settings.SetMasterVolume(0.7f);
        settings.SetResolution(3840, 2160, false);
        settings.SetGraphicsQuality(3);
        settings.SetKeyBinding("pause", "Tab");

        var json = settings.SerializeToJson();
        Assert.False(string.IsNullOrEmpty(json));

        var settings2 = new SettingsManager(bus);
        settings2.DeserializeFromJson(json);

        Assert.Equal(0.7f, settings2.Settings.MasterVolume, 0.01);
        Assert.Equal(3840, settings2.Settings.ResolutionWidth);
        Assert.Equal(2160, settings2.Settings.ResolutionHeight);
        Assert.False(settings2.Settings.Fullscreen);
        Assert.Equal(3, settings2.Settings.GraphicsQuality);
        Assert.Equal("Tab", settings2.Settings.KeyBindings["pause"]);
    }
}
