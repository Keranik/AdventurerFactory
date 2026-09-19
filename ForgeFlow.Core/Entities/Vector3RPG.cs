namespace ForgeFlow.Core.Entities;

/// <summary>
/// Immutable RPG-style 3D vector. Pure .NET — zero Unity references.
/// Use <c>ToUnityVector3()</c> in the Presentation layer to convert at the last moment.
/// </summary>
[Serializable]
public readonly struct Vector3RPG : IEquatable<Vector3RPG>
{
    public float X { get; }
    public float Y { get; }
    public float Z { get; }

    public Vector3RPG(float x, float y, float z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    // --- Named Factories ---
    public static Vector3RPG Zero => default;
    public static Vector3RPG One => new(1f, 1f, 1f);
    public static Vector3RPG Up => new(0f, 1f, 0f);
    public static Vector3RPG Down => new(0f, -1f, 0f);
    public static Vector3RPG Left => new(-1f, 0f, 0f);
    public static Vector3RPG Right => new(1f, 0f, 0f);
    public static Vector3RPG Forward => new(0f, 0f, 1f);
    public static Vector3RPG Back => new(0f, 0f, -1f);

    // --- Math ---

    public float Magnitude => (float)Math.Sqrt(X * X + Y * Y + Z * Z);
    public float SqrMagnitude => X * X + Y * Y + Z * Z;

    public Vector3RPG Normalized
    {
        get
        {
            float mag = Magnitude;
            return mag > 1e-6f ? new(X / mag, Y / mag, Z / mag) : Zero;
        }
    }

    public static float Dot(Vector3RPG a, Vector3RPG b) =>
        a.X * b.X + a.Y * b.Y + a.Z * b.Z;

    public static Vector3RPG Cross(Vector3RPG a, Vector3RPG b) =>
        new(a.Y * b.Z - a.Z * b.Y,
            a.Z * b.X - a.X * b.Z,
            a.X * b.Y - a.Y * b.X);

    public static float Distance(Vector3RPG a, Vector3RPG b) =>
        (a - b).Magnitude;

    public static Vector3RPG Lerp(Vector3RPG a, Vector3RPG b, float t)
    {
        t = t < 0f ? 0f : t > 1f ? 1f : t;
        return new(a.X + (b.X - a.X) * t,
                   a.Y + (b.Y - a.Y) * t,
                   a.Z + (b.Z - a.Z) * t);
    }

    // --- Operators ---

    public static Vector3RPG operator +(Vector3RPG a, Vector3RPG b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
    public static Vector3RPG operator -(Vector3RPG a, Vector3RPG b) => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
    public static Vector3RPG operator *(Vector3RPG a, float d) => new(a.X * d, a.Y * d, a.Z * d);
    public static Vector3RPG operator *(float d, Vector3RPG a) => new(a.X * d, a.Y * d, a.Z * d);
    public static Vector3RPG operator /(Vector3RPG a, float d) => new(a.X / d, a.Y / d, a.Z / d);
    public static Vector3RPG operator -(Vector3RPG a) => new(-a.X, -a.Y, -a.Z);

    public static bool operator ==(Vector3RPG a, Vector3RPG b) => a.Equals(b);
    public static bool operator !=(Vector3RPG a, Vector3RPG b) => !a.Equals(b);

    // --- Conversions ---

    /// <summary>Drops the Z component, returning a 2D vector.</summary>
    public Vector2RPG ToVector2XY() => new(X, Y);

    /// <summary>Returns (X, Z) as a 2D vector (common for top-down grid mapping).</summary>
    public Vector2RPG ToVector2XZ() => new(X, Z);

    // --- Equality ---

    public bool Equals(Vector3RPG other) =>
        Math.Abs(X - other.X) < 1e-6f &&
        Math.Abs(Y - other.Y) < 1e-6f &&
        Math.Abs(Z - other.Z) < 1e-6f;

    public override bool Equals(object? obj) => obj is Vector3RPG other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(X, Y, Z);
    public override string ToString() => $"({X:F2}, {Y:F2}, {Z:F2})";
}
