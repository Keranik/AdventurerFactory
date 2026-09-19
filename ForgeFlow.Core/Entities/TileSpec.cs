namespace ForgeFlow.Core.Entities;

/// <summary>
/// Immutable RPG-style tile descriptor. Represents a tile's terrain type,
/// height layer, walkability, and build-suitability at a grid position.
/// Pure .NET — zero Unity references.
/// </summary>
[Serializable]
public readonly struct TileSpec : IEquatable<TileSpec>
{
    /// <summary>Tile terrain category.</summary>
    public TileKind Kind { get; }

    /// <summary>Height layer (0 = sea level, 1 = ground, 2 = hill, 3 = mountain).</summary>
    public byte Height { get; }

    /// <summary>0–1 fertility modifier for gathering yield.</summary>
    public float Fertility { get; }

    /// <summary>Whether entities can path through this tile.</summary>
    public bool IsWalkable { get; }

    /// <summary>Whether structures can be placed on this tile.</summary>
    public bool IsBuildable { get; }

    /// <summary>Optional resource richness (0–1) for mining/gathering overlays.</summary>
    public float ResourceDensity { get; }

    public TileSpec(TileKind kind, byte height, float fertility, bool isWalkable, bool isBuildable, float resourceDensity = 0f)
    {
        Kind = kind;
        Height = height;
        Fertility = Math.Max(0f, Math.Min(1f, fertility));
        IsWalkable = isWalkable;
        IsBuildable = isBuildable;
        ResourceDensity = Math.Max(0f, Math.Min(1f, resourceDensity));
    }

    // --- Named Factories ---
    public static TileSpec Grass => new(TileKind.Grass, 1, 0.8f, true, true);
    public static TileSpec Forest => new(TileKind.Forest, 1, 1.0f, true, false, 0.6f);
    public static TileSpec Stone => new(TileKind.Stone, 2, 0.1f, true, true, 0.8f);
    public static TileSpec Mountain => new(TileKind.Mountain, 3, 0.0f, false, false, 0.9f);
    public static TileSpec Water => new(TileKind.Water, 0, 0.0f, false, false);
    public static TileSpec Sand => new(TileKind.Sand, 1, 0.3f, true, true, 0.2f);
    public static TileSpec Swamp => new(TileKind.Swamp, 0, 0.6f, true, false, 0.4f);
    public static TileSpec Lava => new(TileKind.Lava, 0, 0.0f, false, false);
    public static TileSpec Snow => new(TileKind.Snow, 2, 0.2f, true, true, 0.1f);
    public static TileSpec Void => new(TileKind.Void, 0, 0.0f, false, false);
    public static TileSpec Road => new(TileKind.Road, 1, 0.0f, true, false);

    // --- Modifiers (return new immutable copy) ---

    /// <summary>Returns a copy with different walkability.</summary>
    public TileSpec WithWalkable(bool walkable) =>
        new(Kind, Height, Fertility, walkable, IsBuildable, ResourceDensity);

    /// <summary>Returns a copy with different buildability.</summary>
    public TileSpec WithBuildable(bool buildable) =>
        new(Kind, Height, Fertility, IsWalkable, buildable, ResourceDensity);

    /// <summary>Returns a copy with modified fertility.</summary>
    public TileSpec WithFertility(float fertility) =>
        new(Kind, Height, fertility, IsWalkable, IsBuildable, ResourceDensity);

    /// <summary>Returns a copy with modified resource density.</summary>
    public TileSpec WithResourceDensity(float density) =>
        new(Kind, Height, Fertility, IsWalkable, IsBuildable, density);

    /// <summary>Returns a copy at a different height layer.</summary>
    public TileSpec AtHeight(byte height) =>
        new(Kind, height, Fertility, IsWalkable, IsBuildable, ResourceDensity);

    // --- Queries ---

    /// <summary>Whether this tile provides gathering resources.</summary>
    public bool HasResources => ResourceDensity > 0.01f;

    /// <summary>Whether this tile is passable and buildable (ideal placement).</summary>
    public bool IsIdealForBuilding => IsWalkable && IsBuildable;

    /// <summary>Movement speed multiplier based on tile kind.</summary>
    public float MovementMultiplier => Kind switch
    {
        TileKind.Road => 1.5f,
        TileKind.Grass => 1.0f,
        TileKind.Sand => 0.7f,
        TileKind.Swamp => 0.4f,
        TileKind.Forest => 0.8f,
        TileKind.Stone => 0.9f,
        TileKind.Snow => 0.6f,
        _ => 1.0f
    };

    /// <summary>Gets the color tint for this tile's kind.</summary>
    public ColorRPG TileColor => Kind switch
    {
        TileKind.Grass => new ColorRPG(0.35f, 0.65f, 0.25f),
        TileKind.Forest => new ColorRPG(0.15f, 0.45f, 0.12f),
        TileKind.Stone => new ColorRPG(0.55f, 0.55f, 0.50f),
        TileKind.Mountain => new ColorRPG(0.40f, 0.38f, 0.35f),
        TileKind.Water => new ColorRPG(0.15f, 0.35f, 0.70f),
        TileKind.Sand => new ColorRPG(0.85f, 0.75f, 0.50f),
        TileKind.Swamp => new ColorRPG(0.30f, 0.40f, 0.25f),
        TileKind.Lava => new ColorRPG(0.90f, 0.30f, 0.05f),
        TileKind.Snow => new ColorRPG(0.90f, 0.92f, 0.95f),
        TileKind.Road => new ColorRPG(0.50f, 0.45f, 0.40f),
        TileKind.Void => ColorRPG.Black,
        _ => ColorRPG.White
    };

    // --- Equality ---
    public bool Equals(TileSpec other) =>
        Kind == other.Kind && Height == other.Height &&
        Math.Abs(Fertility - other.Fertility) < 0.001f &&
        IsWalkable == other.IsWalkable && IsBuildable == other.IsBuildable &&
        Math.Abs(ResourceDensity - other.ResourceDensity) < 0.001f;

    public override bool Equals(object? obj) => obj is TileSpec other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(Kind, Height, Fertility, IsWalkable, IsBuildable, ResourceDensity);

    public static bool operator ==(TileSpec left, TileSpec right) => left.Equals(right);
    public static bool operator !=(TileSpec left, TileSpec right) => !left.Equals(right);

    public override string ToString() => $"{Kind}(h={Height}, f={Fertility:F2}, w={IsWalkable}, b={IsBuildable})";
}

/// <summary>Tile terrain category.</summary>
public enum TileKind : byte
{
    Grass,
    Forest,
    Stone,
    Mountain,
    Water,
    Sand,
    Swamp,
    Lava,
    Snow,
    Road,
    Void
}
