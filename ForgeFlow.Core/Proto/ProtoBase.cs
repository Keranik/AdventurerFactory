namespace ForgeFlow.Core.Proto;

/// <summary>
/// Base class for all prototype definitions. Protos are pure data/config,
/// loaded from JSON at startup and optionally overwritten by mods.
/// </summary>
public abstract class ProtoBase
{
    /// <summary>Unique identifier used for registry lookup and JSON keying.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Human-readable display name.</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Optional mod-facing description.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Research tier required to unlock this proto.</summary>
    public int UnlockTier { get; set; }

    /// <summary>Tags for filtering and mod queries.</summary>
    public List<string> Tags { get; set; } = new();
}
