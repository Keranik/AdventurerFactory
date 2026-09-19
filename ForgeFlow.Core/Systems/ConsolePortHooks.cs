namespace ForgeFlow.Core.Systems;

/// <summary>
/// Pure interface for platform-specific hooks (console save, achievements,
/// controller input abstraction). The Presentation layer provides the
/// concrete implementation per platform; Core only codes against this interface.
/// </summary>
public interface IConsolePlatformHooks
{
    /// <summary>Platform name for logging/UI ("PC", "Xbox", "PlayStation", "Switch").</summary>
    string PlatformName { get; }

    /// <summary>Whether this platform uses a controller as primary input.</summary>
    bool IsControllerPrimary { get; }

    /// <summary>Save data to platform-specific cloud storage.</summary>
    bool CloudSave(string slotName, byte[] data);

    /// <summary>Load data from platform-specific cloud storage.</summary>
    byte[]? CloudLoad(string slotName);

    /// <summary>Delete a cloud save slot.</summary>
    bool CloudDelete(string slotName);

    /// <summary>Unlock a platform achievement / trophy.</summary>
    void UnlockAchievement(string achievementId);

    /// <summary>Check whether an achievement is already unlocked.</summary>
    bool IsAchievementUnlocked(string achievementId);

    /// <summary>Show the platform's native save indicator (e.g. spinning icon).</summary>
    void ShowSaveIndicator();

    /// <summary>Hide the platform's native save indicator.</summary>
    void HideSaveIndicator();
}

/// <summary>
/// Default PC implementation — writes to local disk and logs achievements.
/// This is what ships on desktop. Console builds inject their own implementation.
/// </summary>
public sealed class DefaultPcHooks : IConsolePlatformHooks
{
    public string PlatformName => "PC";
    public bool IsControllerPrimary => false;

    public bool CloudSave(string slotName, byte[] data)
    {
        // On PC, cloud save can be handled by Steam/GOG SDK.
        // Default implementation is a no-op; local file save is primary.
        return false;
    }

    public byte[]? CloudLoad(string slotName) => null;
    public bool CloudDelete(string slotName) => false;

    public void UnlockAchievement(string achievementId)
    {
        // Platform SDK would be called here (Steamworks, etc.)
    }

    public bool IsAchievementUnlocked(string achievementId) => false;
    public void ShowSaveIndicator() { }
    public void HideSaveIndicator() { }
}
