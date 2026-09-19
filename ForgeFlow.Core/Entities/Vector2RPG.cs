namespace ForgeFlow.Core.Entities;

/// <summary>
/// Immutable RPG-style 2D vector. Pure .NET — zero Unity references.
/// Use <c>ToUnityVector2()</c> in the Presentation layer to convert at the last moment.
/// </summary>
[Serializable]
public readonly struct Vector2RPG : IEquatable<Vector2RPG>
{
    public float X { get; }
    public float Y { get; }

    public Vector2RPG(float x, float y)
    {
        X = x;
        Y = y;
    }

    // --- Named Factories ---
    public static Vector2RPG Zero => default;
    public static Vector2RPG One => new(1f, 1f);
    public static Vector2RPG Up => new(0f, 1f);
    public static Vector2RPG Down => new(0f, -1f);
    public static Vector2RPG Left => new(-1f, 0f);
    public static Vector2RPG Right => new(1f, 0f);

    // --- Math ---

    public float Magnitude => (float)Math.Sqrt(X * X + Y * Y);
    public float SqrMagnitude => X * X + Y * Y;

    public Vector2RPG Normalized
    {
        get
        {
            float mag = Magnitude;
            return mag > 1e-6f ? new(X / mag, Y / mag) : Zero;
        }
    }

    public static float Dot(Vector2RPG a, Vector2RPG b) =>
        a.X * b.X + a.Y * b.Y;

    public static float Distance(Vector2RPG a, Vector2RPG b) =>
        (a - b).Magnitude;

    public static Vector2RPG Lerp(Vector2RPG a, Vector2RPG b, float t)
    {
        t = t < 0f ? 0f : t > 1f ? 1f : t;
        return new(a.X + (b.X - a.X) * t, a.Y + (b.Y - a.Y) * t);
    }

    // --- Operators ---

    public static Vector2RPG operator +(Vector2RPG a, Vector2RPG b) => new(a.X + b.X, a.Y + b.Y);
    public static Vector2RPG operator -(Vector2RPG a, Vector2RPG b) => new(a.X - b.X, a.Y - b.Y);
    public static Vector2RPG operator *(Vector2RPG a, float d) => new(a.X * d, a.Y * d);
    public static Vector2RPG operator *(float d, Vector2RPG a) => new(a.X * d, a.Y * d);
    public static Vector2RPG operator /(Vector2RPG a, float d) => new(a.X / d, a.Y / d);
    public static Vector2RPG operator -(Vector2RPG a) => new(-a.X, -a.Y);

    public static bool operator ==(Vector2RPG a, Vector2RPG b) => a.Equals(b);
    public static bool operator !=(Vector2RPG a, Vector2RPG b) => !a.Equals(b);

    // --- Equality ---

    public bool Equals(Vector2RPG other) =>
        Math.Abs(X - other.X) < 1e-6f && Math.Abs(Y - other.Y) < 1e-6f;

    public override bool Equals(object? obj) => obj is Vector2RPG other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(X, Y);
    public override string ToString() => $"({X:F2}, {Y:F2})";
}
