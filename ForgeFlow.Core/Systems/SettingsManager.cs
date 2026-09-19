using System.Text.Json;
using ForgeFlow.Core.Events;

namespace ForgeFlow.Core.Systems;

/// <summary>
/// UI style mode: Fun/Whimsical (default) vs Minimalist/Expert.
/// Affects border radius, padding, glows, and animations.
/// </summary>
public enum UIStyleMode
{
    /// <summary>Vibrant, rounded corners (8px), glows, hover scale, particles.</summary>
    FunWhimsical = 0,
    /// <summary>Sharp corners (2px), compact, reduced animations, information-dense.</summary>
    MinimalistExpert = 1
}

/// <summary>
/// Serializable game settings. Pure data — no Unity dependency.
/// Presentation reads these to apply volume, resolution, quality, keybinds.
/// </summary>
public sealed class GameSettings
{
    public float MasterVolume { get; set; } = 1.0f;
    public float MusicVolume { get; set; } = 0.8f;
    public float SfxVolume { get; set; } = 1.0f;
    public int ResolutionWidth { get; set; } = 1920;
    public int ResolutionHeight { get; set; } = 1080;
    public bool Fullscreen { get; set; } = true;
    public int GraphicsQuality { get; set; } = 2; // 0=Low, 1=Medium, 2=High, 3=Ultra
    public bool VSync { get; set; } = true;
    public string Language { get; set; } = "en";
    public string ThemeId { get; set; } = "dark_factory";
    public UIStyleMode UIStyle { get; set; } = UIStyleMode.FunWhimsical;
    public CameraSettings Camera { get; set; } = new();
    public List<HudPanelLayout> HudLayout { get; set; } = new();
    public List<WindowLayoutData> WindowLayouts { get; set; } = new();
    public Dictionary<string, string> KeyBindings { get; set; } = new()
    {
        { "pause", "Escape" },
        { "place_structure", "Mouse0" },
        { "remove_structure", "Mouse1" },
        { "rotate", "R" },
        { "speed_up", "Plus" },
        { "speed_down", "Minus" },
        { "camera_up", "W" },
        { "camera_down", "S" },
        { "camera_left", "A" },
        { "camera_right", "D" },
        { "zoom_in", "ScrollUp" },
        { "zoom_out", "ScrollDown" }
    };
}

/// <summary>
/// Serializable camera settings. Pure data — no Unity dependency.
/// </summary>
public sealed class CameraSettings
{
    public float ZoomSpeed { get; set; } = 2f;
    public float PanSpeed { get; set; } = 15f;
    public float RotationSpeed { get; set; } = 90f;
    public float MinZoom { get; set; } = 3f;
    public float MaxZoom { get; set; } = 30f;
    public float DefaultZoom { get; set; } = 12f;
    public bool SnapRotation { get; set; } = true;
}

/// <summary>
/// Serializable HUD panel layout entry. Stores position, size, visibility per panel.
/// </summary>
public sealed class HudPanelLayout
{
    public string PanelId { get; set; } = "";
    public float X { get; set; }
    public float Y { get; set; }
    public float Width { get; set; } = 300f;
    public float Height { get; set; } = 200f;
    public bool Pinned { get; set; }
    public bool Visible { get; set; } = true;
}

/// <summary>
/// Serializable window layout entry for the centralized UIManager.
/// Stores position, size, visibility, and pin state per window.
/// </summary>
public sealed class WindowLayoutData
{
    public string WindowId { get; set; } = "";
    public float X { get; set; }
    public float Y { get; set; }
    public float Width { get; set; } = 300f;
    public float Height { get; set; } = 200f;
    public bool Visible { get; set; } = true;
    public bool Pinned { get; set; }
}

/// <summary>
/// Manages game settings: load, save, apply, and notify via events.
/// Pure Core — headless-capable.
/// </summary>
public sealed class SettingsManager : IGameSystem
{
    private readonly EventBus _eventBus;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public GameSettings Settings { get; private set; } = new();

    public SettingsManager(EventBus eventBus)
    {
        _eventBus = eventBus;
    }

    public void Apply(GameSettings newSettings)
    {
        Settings = newSettings;
        _eventBus.Publish(new SettingsChangedEvent(
            newSettings.MasterVolume,
            newSettings.ResolutionWidth,
            newSettings.ResolutionHeight,
            newSettings.GraphicsQuality));
    }

    public void SetMasterVolume(float volume)
    {
        Settings.MasterVolume = Math.Clamp(volume, 0f, 1f);
        _eventBus.Publish(new SettingsChangedEvent(
            Settings.MasterVolume,
            Settings.ResolutionWidth,
            Settings.ResolutionHeight,
            Settings.GraphicsQuality));
    }

    public void SetResolution(int width, int height, bool fullscreen)
    {
        Settings.ResolutionWidth = width;
        Settings.ResolutionHeight = height;
        Settings.Fullscreen = fullscreen;
        _eventBus.Publish(new SettingsChangedEvent(
            Settings.MasterVolume, width, height, Settings.GraphicsQuality));
    }

    public void SetGraphicsQuality(int quality)
    {
        Settings.GraphicsQuality = Math.Clamp(quality, 0, 3);
        _eventBus.Publish(new SettingsChangedEvent(
            Settings.MasterVolume,
            Settings.ResolutionWidth,
            Settings.ResolutionHeight,
            Settings.GraphicsQuality));
    }

    public void SetKeyBinding(string action, string key)
    {
        Settings.KeyBindings[action] = key;
    }

    public string? GetKeyBinding(string action)
    {
        return Settings.KeyBindings.TryGetValue(action, out var key) ? key : null;
    }

    public string SerializeToJson()
    {
        return JsonSerializer.Serialize(Settings, JsonOptions);
    }

    public void DeserializeFromJson(string json)
    {
        var loaded = JsonSerializer.Deserialize<GameSettings>(json, JsonOptions);
        if (loaded != null)
        {
            Apply(loaded);
        }
    }
}
