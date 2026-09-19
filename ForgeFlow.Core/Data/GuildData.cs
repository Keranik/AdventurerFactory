namespace ForgeFlow.Core.Data;

/// <summary>
/// Represents the player's guild — name, banner, logo.
/// Created during new game setup. Branding appears on heroes, buildings, UI, leaderboards.
/// Pure data — no Unity dependency.
/// Gold is managed by ItemManager as a virtual stock ("gold") — not stored here.
/// </summary>
public sealed class GuildData
{
    public string GuildName { get; set; } = "Unnamed Guild";
    public string BannerId { get; set; } = "banner_default";
    public string LogoId { get; set; } = "logo_sword";

    /// <summary>Available banner IDs for the guild creation picker.</summary>
    public static readonly string[] AvailableBanners =
    {
        "banner_default", "banner_lion", "banner_eagle", "banner_dragon",
        "banner_wolf", "banner_phoenix", "banner_serpent", "banner_stag"
    };

    /// <summary>Available logo/icon IDs for the guild creation picker.</summary>
    public static readonly string[] AvailableLogos =
    {
        "logo_sword", "logo_shield", "logo_crown", "logo_hammer",
        "logo_star", "logo_anvil", "logo_tome", "logo_chalice"
    };
}
