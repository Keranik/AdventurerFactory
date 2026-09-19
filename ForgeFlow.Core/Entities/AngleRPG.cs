namespace ForgeFlow.Core.Entities;

/// <summary>
/// An immutable, self-documenting wrapper for degree-based angles with automatic wrapping.
/// Perfect for rotations, directions, pathfinding, and procedural generation.
/// Pure .NET — zero Unity references.
/// </summary>
[Serializable]
public readonly struct AngleRPG : IEquatable<AngleRPG>, IComparable<AngleRPG>, IFormattable
{
    #region Fields

    private readonly float _degrees;

    private const float EPSILON = 0.0001f;
    private const float DEG2RAD = (float)(Math.PI / 180.0);
    private const float RAD2DEG = (float)(180.0 / Math.PI);

    #endregion

    #region Constructors

    /// <summary>
    /// Creates a new AngleRPG from degrees. Automatically wraps to [0, 360).
    /// </summary>
    public AngleRPG(float degrees)
    {
        _degrees = Wrap(degrees);
    }

    #endregion

    #region Static Presets — Cardinal Directions

    /// <summary>Zero degrees.</summary>
    public static AngleRPG Zero => new(0f);

    /// <summary>North (90° / Up in 2D).</summary>
    public static AngleRPG North => new(90f);

    /// <summary>East (0° / Right in 2D). Standard 0° in math convention.</summary>
    public static AngleRPG East => new(0f);

    /// <summary>South (270° / Down in 2D).</summary>
    public static AngleRPG South => new(270f);

    /// <summary>West (180° / Left in 2D).</summary>
    public static AngleRPG West => new(180f);

    #endregion

    #region Static Presets — Ordinal Directions

    /// <summary>Northeast (45°).</summary>
    public static AngleRPG NorthEast => new(45f);

    /// <summary>Southeast (315°).</summary>
    public static AngleRPG SouthEast => new(315f);

    /// <summary>Southwest (225°).</summary>
    public static AngleRPG SouthWest => new(225f);

    /// <summary>Northwest (135°).</summary>
    public static AngleRPG NorthWest => new(135f);

    #endregion

    #region Static Presets — Aliases

    /// <summary>Right direction (same as East, 0°).</summary>
    public static AngleRPG Right => East;

    /// <summary>Up direction (same as North, 90°).</summary>
    public static AngleRPG Up => North;

    /// <summary>Left direction (same as West, 180°).</summary>
    public static AngleRPG Left => West;

    /// <summary>Down direction (same as South, 270°).</summary>
    public static AngleRPG Down => South;

    #endregion

    #region Static Factories

    /// <summary>Creates an AngleRPG from degrees.</summary>
    public static AngleRPG FromDegrees(float degrees) => new(degrees);

    /// <summary>Creates an AngleRPG from radians.</summary>
    public static AngleRPG FromRadians(float radians) => new(radians * RAD2DEG);

    /// <summary>Creates an AngleRPG from a 2D direction vector.</summary>
    public static AngleRPG FromDirection(Vector2RPG direction)
    {
        if (direction.SqrMagnitude < EPSILON)
        {
            return Zero;
        }

        return new AngleRPG((float)Math.Atan2(direction.Y, direction.X) * RAD2DEG);
    }

    /// <summary>Creates an AngleRPG from a 3D direction vector (XZ plane, Y-up).</summary>
    public static AngleRPG FromDirection3D(Vector3RPG direction)
    {
        if (direction.SqrMagnitude < EPSILON)
        {
            return Zero;
        }

        return new AngleRPG((float)Math.Atan2(direction.Z, direction.X) * RAD2DEG);
    }

    /// <summary>Calculates the angle from one position to another (2D).</summary>
    public static AngleRPG FromTo(Vector2RPG from, Vector2RPG to)
    {
        return FromDirection(to - from);
    }

    /// <summary>Calculates the angle from one position to another (3D, XZ plane).</summary>
    public static AngleRPG FromTo3D(Vector3RPG from, Vector3RPG to)
    {
        return FromDirection3D(to - from);
    }

    /// <summary>Linearly interpolates between two angles (takes shortest path).</summary>
    public static AngleRPG Lerp(AngleRPG a, AngleRPG b, float t)
    {
        float tClamped = Math.Clamp(t, 0f, 1f);
        return new AngleRPG(LerpAngle(a._degrees, b._degrees, tClamped));
    }

    /// <summary>Linearly interpolates between two angles (unclamped).</summary>
    public static AngleRPG LerpUnclamped(AngleRPG a, AngleRPG b, float t)
    {
        return new AngleRPG(LerpAngle(a._degrees, b._degrees, t));
    }

    #endregion

    #region Properties

    /// <summary>Gets the angle in degrees [0, 360).</summary>
    public float Degrees => _degrees;

    /// <summary>Gets the angle in radians [0, 2π).</summary>
    public float Radians => _degrees * DEG2RAD;

    /// <summary>Gets the angle as signed degrees [-180, 180).</summary>
    public float SignedDegrees
    {
        get
        {
            float d = _degrees;
            if (d >= 180f)
            {
                d -= 360f;
            }

            return d;
        }
    }

    /// <summary>Returns the opposite angle (180° rotated).</summary>
    public AngleRPG Opposite => new(_degrees + 180f);

    /// <summary>Returns true if this angle is close to zero.</summary>
    public bool IsZero => _degrees < EPSILON || _degrees > 360f - EPSILON;

    #endregion

    #region Instance Methods — Rotation

    /// <summary>Rotates this angle by the specified degrees.</summary>
    public AngleRPG Rotate(float degrees) => new(_degrees + degrees);

    /// <summary>Rotates this angle by another AngleRPG.</summary>
    public AngleRPG Rotate(AngleRPG angle) => new(_degrees + angle._degrees);

    /// <summary>Rotates clockwise by the specified degrees.</summary>
    public AngleRPG Clockwise(float degrees) => new(_degrees - degrees);

    /// <summary>Rotates counter-clockwise by the specified degrees.</summary>
    public AngleRPG CounterClockwise(float degrees) => new(_degrees + degrees);

    /// <summary>Returns the shortest turn needed to face the target angle.</summary>
    public float ShortestTurn(AngleRPG target)
    {
        float diff = target._degrees - _degrees;
        while (diff > 180f)
        {
            diff -= 360f;
        }

        while (diff < -180f)
        {
            diff += 360f;
        }

        return diff;
    }

    /// <summary>Rotates towards a target angle by a maximum amount.</summary>
    public AngleRPG Towards(AngleRPG target, float maxDelta)
    {
        float turn = ShortestTurn(target);
        float clampedTurn = Math.Clamp(turn, -maxDelta, maxDelta);
        return new AngleRPG(_degrees + clampedTurn);
    }

    /// <summary>Rotates towards a target angle by a maximum AngleRPG amount.</summary>
    public AngleRPG Towards(AngleRPG target, AngleRPG maxTurn)
    {
        return Towards(target, maxTurn._degrees);
    }

    #endregion

    #region Instance Methods — Conversion

    /// <summary>Converts to a 2D unit direction vector.</summary>
    public Vector2RPG ToDirection2D()
    {
        float rad = Radians;
        return new Vector2RPG((float)Math.Cos(rad), (float)Math.Sin(rad));
    }

    /// <summary>Converts to a 3D unit direction vector (XZ plane, Y = 0).</summary>
    public Vector3RPG ToDirection3D()
    {
        float rad = Radians;
        return new Vector3RPG((float)Math.Cos(rad), 0f, (float)Math.Sin(rad));
    }

    /// <summary>Converts to a QuaternionRPG rotation (around Z axis for 2D).</summary>
    public QuaternionRPG ToQuaternion() => QuaternionRPG.Euler(0f, 0f, _degrees);

    /// <summary>Converts to a QuaternionRPG rotation around the Y axis (for 3D top-down).</summary>
    public QuaternionRPG ToQuaternionY() => QuaternionRPG.Euler(0f, _degrees, 0f);

    /// <summary>Returns the cardinal/ordinal direction name.</summary>
    public string ToDirectionName()
    {
        float d = _degrees;
        if (d >= 337.5f || d < 22.5f)
        {
            return "East";
        }

        if (d >= 22.5f && d < 67.5f)
        {
            return "Northeast";
        }

        if (d >= 67.5f && d < 112.5f)
        {
            return "North";
        }

        if (d >= 112.5f && d < 157.5f)
        {
            return "Northwest";
        }

        if (d >= 157.5f && d < 202.5f)
        {
            return "West";
        }

        if (d >= 202.5f && d < 247.5f)
        {
            return "Southwest";
        }

        if (d >= 247.5f && d < 292.5f)
        {
            return "South";
        }

        return "Southeast";
    }

    #endregion

    #region Private Helpers

    private static float Wrap(float degrees)
    {
        degrees %= 360f;
        if (degrees < 0f)
        {
            degrees += 360f;
        }

        return degrees;
    }

    private static float LerpAngle(float a, float b, float t)
    {
        float diff = ((b - a + 540f) % 360f) - 180f;
        return a + diff * t;
    }

    #endregion

    #region Operators — Implicit Conversions

    /// <summary>Implicitly converts AngleRPG to float (degrees).</summary>
    public static implicit operator float(AngleRPG a) => a._degrees;

    /// <summary>Implicitly converts float to AngleRPG.</summary>
    public static implicit operator AngleRPG(float degrees) => new(degrees);

    /// <summary>Implicitly converts int to AngleRPG.</summary>
    public static implicit operator AngleRPG(int degrees) => new(degrees);

    #endregion

    #region Operators — Arithmetic

    public static AngleRPG operator +(AngleRPG a, AngleRPG b) => new(a._degrees + b._degrees);
    public static AngleRPG operator +(AngleRPG a, float degrees) => new(a._degrees + degrees);
    public static AngleRPG operator +(float degrees, AngleRPG a) => new(degrees + a._degrees);
    public static AngleRPG operator -(AngleRPG a, AngleRPG b) => new(a._degrees - b._degrees);
    public static AngleRPG operator -(AngleRPG a, float degrees) => new(a._degrees - degrees);
    public static AngleRPG operator -(AngleRPG a) => a.Opposite;
    public static AngleRPG operator *(AngleRPG a, float scalar) => new(a._degrees * scalar);
    public static AngleRPG operator *(float scalar, AngleRPG a) => new(scalar * a._degrees);
    public static AngleRPG operator /(AngleRPG a, float scalar) =>
        new(scalar != 0 ? a._degrees / scalar : 0f);

    #endregion

    #region Operators — Comparison

    public static bool operator ==(AngleRPG a, AngleRPG b)
    {
        float diff = Math.Abs(a._degrees - b._degrees);
        return diff < EPSILON || diff > 360f - EPSILON;
    }

    public static bool operator !=(AngleRPG a, AngleRPG b) => !(a == b);
    public static bool operator <(AngleRPG a, AngleRPG b) => a._degrees < b._degrees;
    public static bool operator >(AngleRPG a, AngleRPG b) => a._degrees > b._degrees;
    public static bool operator <=(AngleRPG a, AngleRPG b) => a._degrees <= b._degrees;
    public static bool operator >=(AngleRPG a, AngleRPG b) => a._degrees >= b._degrees;

    #endregion

    #region IEquatable / IComparable

    public bool Equals(AngleRPG other) => this == other;
    public override bool Equals(object? obj) => obj is AngleRPG other && Equals(other);
    public override int GetHashCode() => _degrees.GetHashCode();
    public int CompareTo(AngleRPG other) => _degrees.CompareTo(other._degrees);

    #endregion

    #region IFormattable

    /// <summary>Returns "90°".</summary>
    public override string ToString() => $"{_degrees:0.#}°";

    /// <summary>
    /// Formats the angle.
    /// Formats: null/G = "90°", "dir" = "East", "rad" = radians, numeric = custom.
    /// </summary>
    public string ToString(string? format, IFormatProvider? formatProvider = null)
    {
        if (string.IsNullOrEmpty(format) || format == "G")
        {
            return ToString();
        }

        string lower = format.ToLowerInvariant();
        if (lower == "dir" || lower == "direction")
        {
            return ToDirectionName();
        }

        if (lower == "rad" || lower == "radians")
        {
            return $"{Radians:0.###} rad";
        }

        if (lower == "signed")
        {
            return $"{SignedDegrees:0.#}°";
        }

        if (format.EndsWith("°"))
        {
            return $"{_degrees.ToString(format.Substring(0, format.Length - 1), formatProvider)}°";
        }

        return $"{_degrees.ToString(format, formatProvider)}°";
    }

    #endregion
}

/// <summary>
/// Extension methods for creating AngleRPG from numeric types.
/// </summary>
public static class AngleRPGExtensions
{
    /// <summary>Converts a float to AngleRPG (degrees).</summary>
    public static AngleRPG Degrees(this float value) => AngleRPG.FromDegrees(value);

    /// <summary>Converts an int to AngleRPG (degrees).</summary>
    public static AngleRPG Degrees(this int value) => AngleRPG.FromDegrees(value);

    /// <summary>Converts radians to AngleRPG.</summary>
    public static AngleRPG ToAngleFromRadians(this float value) => AngleRPG.FromRadians(value);

    /// <summary>Gets the angle from a Vector2RPG direction.</summary>
    public static AngleRPG ToAngle(this Vector2RPG direction) => AngleRPG.FromDirection(direction);

    /// <summary>Gets the angle from a Vector3RPG direction (XZ plane).</summary>
    public static AngleRPG ToAngle(this Vector3RPG direction) => AngleRPG.FromDirection3D(direction);
}
