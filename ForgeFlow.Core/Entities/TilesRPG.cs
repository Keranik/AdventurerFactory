namespace ForgeFlow.Core.Entities;

/// <summary>
/// An immutable, self-documenting wrapper for tile-based distances.
/// Provides zero-cost abstraction for measuring distances in tile units.
/// Pure .NET — zero Unity references.
/// </summary>
[Serializable]
public readonly struct TilesRPG : IEquatable<TilesRPG>, IComparable<TilesRPG>, IFormattable
{
    #region Fields

    private readonly float _tiles;

    private const float EPSILON = 0.0001f;

    #endregion

    #region Constructors

    /// <summary>
    /// Creates a new TilesRPG from a tile count (clamped to ≥ 0).
    /// </summary>
    public TilesRPG(float tiles)
    {
        _tiles = Math.Max(0f, tiles);
    }

    #endregion

    #region Static Presets

    /// <summary>Zero tiles.</summary>
    public static TilesRPG Zero => new(0f);

    /// <summary>Half a tile (0.5).</summary>
    public static TilesRPG Half => new(0.5f);

    /// <summary>One tile.</summary>
    public static TilesRPG One => new(1f);

    /// <summary>Two tiles.</summary>
    public static TilesRPG Two => new(2f);

    /// <summary>Three tiles.</summary>
    public static TilesRPG Three => new(3f);

    /// <summary>Four tiles.</summary>
    public static TilesRPG Four => new(4f);

    /// <summary>Diagonal distance across one tile (√2 ≈ 1.414).</summary>
    public static TilesRPG Diagonal => new((float)Math.Sqrt(2.0));

    #endregion

    #region Static Factories

    /// <summary>Creates a TilesRPG from a tile count.</summary>
    public static TilesRPG FromTiles(float tiles) => new(tiles);

    /// <summary>
    /// Creates a TilesRPG from world units, converting based on tile size.
    /// </summary>
    public static TilesRPG FromUnits(float worldUnits, float tileSize = 1f)
    {
        return new TilesRPG(tileSize > 0 ? worldUnits / tileSize : 0f);
    }

    /// <summary>
    /// Calculates the tile distance between two grid positions.
    /// </summary>
    public static TilesRPG Distance(GridPosRPG from, GridPosRPG to)
    {
        float dx = to.X - from.X;
        float dy = to.Y - from.Y;
        return new TilesRPG((float)Math.Sqrt(dx * dx + dy * dy));
    }

    /// <summary>Returns the minimum of two tile distances.</summary>
    public static TilesRPG Min(TilesRPG a, TilesRPG b) =>
        new(Math.Min(a._tiles, b._tiles));

    /// <summary>Returns the maximum of two tile distances.</summary>
    public static TilesRPG Max(TilesRPG a, TilesRPG b) =>
        new(Math.Max(a._tiles, b._tiles));

    /// <summary>Linearly interpolates between two tile distances.</summary>
    public static TilesRPG Lerp(TilesRPG a, TilesRPG b, float t)
    {
        float tClamped = Math.Clamp(t, 0f, 1f);
        return new TilesRPG(a._tiles + (b._tiles - a._tiles) * tClamped);
    }

    #endregion

    #region Properties

    /// <summary>Gets the raw tile count as a float.</summary>
    public float Value => _tiles;

    /// <summary>Returns true if this represents zero distance.</summary>
    public bool IsZero => _tiles < EPSILON;

    /// <summary>Returns true if this represents a positive distance.</summary>
    public bool IsPositive => _tiles > EPSILON;

    #endregion

    #region Instance Methods

    /// <summary>Returns the absolute value (always positive for TilesRPG).</summary>
    public TilesRPG Abs() => new(Math.Abs(_tiles));

    /// <summary>Returns the ceiling (rounded up to nearest whole tile).</summary>
    public TilesRPG Ceil() => new((float)Math.Ceiling(_tiles));

    /// <summary>Returns the floor (rounded down to nearest whole tile).</summary>
    public TilesRPG Floor() => new((float)Math.Floor(_tiles));

    /// <summary>Returns the rounded value (to nearest whole tile).</summary>
    public TilesRPG Round() => new((float)Math.Round(_tiles));

    /// <summary>Converts to an integer tile count (rounded).</summary>
    public int ToInt() => (int)Math.Round(_tiles);

    /// <summary>Returns the raw float value.</summary>
    public float ToFloat() => _tiles;

    /// <summary>Returns a new TilesRPG clamped to a minimum value.</summary>
    public TilesRPG ClampedMin(TilesRPG min) => new(Math.Max(_tiles, min._tiles));

    /// <summary>Returns a new TilesRPG clamped to a maximum value.</summary>
    public TilesRPG ClampedMax(TilesRPG max) => new(Math.Min(_tiles, max._tiles));

    /// <summary>Returns a new TilesRPG clamped between min and max.</summary>
    public TilesRPG Clamped(TilesRPG min, TilesRPG max)
    {
        return new(Math.Clamp(_tiles, min._tiles, max._tiles));
    }

    /// <summary>Converts this tile distance to world units.</summary>
    public float ToWorldUnits(float tileSize = 1f) => _tiles * tileSize;

    #endregion

    #region Operators — Implicit Conversions

    /// <summary>Implicitly converts TilesRPG to float.</summary>
    public static implicit operator float(TilesRPG t) => t._tiles;

    /// <summary>Implicitly converts float to TilesRPG.</summary>
    public static implicit operator TilesRPG(float f) => new(f);

    /// <summary>Implicitly converts int to TilesRPG.</summary>
    public static implicit operator TilesRPG(int i) => new(i);

    #endregion

    #region Operators — Arithmetic

    public static TilesRPG operator +(TilesRPG a, TilesRPG b) => new(a._tiles + b._tiles);
    public static TilesRPG operator +(TilesRPG a, float b) => new(a._tiles + b);
    public static TilesRPG operator +(float a, TilesRPG b) => new(a + b._tiles);
    public static TilesRPG operator -(TilesRPG a, TilesRPG b) => new(a._tiles - b._tiles);
    public static TilesRPG operator -(TilesRPG a, float b) => new(a._tiles - b);
    public static TilesRPG operator *(TilesRPG a, float b) => new(a._tiles * b);
    public static TilesRPG operator *(float a, TilesRPG b) => new(a * b._tiles);
    public static TilesRPG operator *(TilesRPG a, int b) => new(a._tiles * b);
    public static TilesRPG operator *(int a, TilesRPG b) => new(a * b._tiles);
    public static TilesRPG operator /(TilesRPG a, float b) => new(b != 0 ? a._tiles / b : 0f);
    public static TilesRPG operator /(TilesRPG a, int b) => new(b != 0 ? a._tiles / b : 0f);

    #endregion

    #region Operators — Comparison

    public static bool operator ==(TilesRPG a, TilesRPG b) => Math.Abs(a._tiles - b._tiles) < EPSILON;
    public static bool operator !=(TilesRPG a, TilesRPG b) => !(a == b);
    public static bool operator <(TilesRPG a, TilesRPG b) => a._tiles < b._tiles - EPSILON;
    public static bool operator >(TilesRPG a, TilesRPG b) => a._tiles > b._tiles + EPSILON;
    public static bool operator <=(TilesRPG a, TilesRPG b) => a._tiles <= b._tiles + EPSILON;
    public static bool operator >=(TilesRPG a, TilesRPG b) => a._tiles >= b._tiles - EPSILON;

    #endregion

    #region IEquatable / IComparable

    public bool Equals(TilesRPG other) => this == other;
    public override bool Equals(object? obj) => obj is TilesRPG other && Equals(other);
    public override int GetHashCode() => _tiles.GetHashCode();
    public int CompareTo(TilesRPG other) => _tiles.CompareTo(other._tiles);

    #endregion

    #region IFormattable

    /// <summary>Returns "3.5 tiles".</summary>
    public override string ToString() => $"{_tiles:0.##} tiles";

    /// <summary>
    /// Formats the tile distance. End with "t" for abbreviated format.
    /// </summary>
    public string ToString(string? format, IFormatProvider? formatProvider = null)
    {
        if (string.IsNullOrEmpty(format) || format == "G")
        {
            return ToString();
        }

        if (format.EndsWith("t", StringComparison.OrdinalIgnoreCase))
        {
            string numFormat = format.Substring(0, format.Length - 1);
            if (string.IsNullOrEmpty(numFormat))
            {
                numFormat = "0.##";
            }

            return $"{_tiles.ToString(numFormat, formatProvider)}t";
        }

        return _tiles.ToString(format, formatProvider);
    }

    #endregion
}

/// <summary>
/// Extension methods for creating TilesRPG from numeric types.
/// </summary>
public static class TileDistanceRPGExtensions
{
    /// <summary>Converts a float to TilesRPG.</summary>
    public static TilesRPG Tiles(this float value) => TilesRPG.FromTiles(value);

    /// <summary>Converts an int to TilesRPG.</summary>
    public static TilesRPG Tiles(this int value) => TilesRPG.FromTiles(value);

    /// <summary>Converts world units to TilesRPG.</summary>
    public static TilesRPG ToTileDistance(this float worldUnits, float tileSize = 1f)
    {
        return TilesRPG.FromUnits(worldUnits, tileSize);
    }
}
