namespace ForgeFlow.Core.Entities;

/// <summary>
/// Immutable RPG-style rectangle. Pure .NET — zero Unity references.
/// Use <c>ToUnityRect()</c> in the Presentation layer to convert at the last moment.
/// </summary>
[Serializable]
public readonly struct RectRPG : IEquatable<RectRPG>
{
    public float X { get; }
    public float Y { get; }
    public float Width { get; }
    public float Height { get; }

    public RectRPG(float x, float y, float width, float height)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }

    // --- Named Factories ---
    public static RectRPG Zero => default;
    public static RectRPG Unit => new(0f, 0f, 1f, 1f);

    /// <summary>Creates a rect centered at (cx, cy) with given size.</summary>
    public static RectRPG Centered(float cx, float cy, float width, float height) =>
        new(cx - width * 0.5f, cy - height * 0.5f, width, height);

    // --- Properties ---

    public float XMin => X;
    public float YMin => Y;
    public float XMax => X + Width;
    public float YMax => Y + Height;
    public Vector2RPG Center => new(X + Width * 0.5f, Y + Height * 0.5f);
    public Vector2RPG Size => new(Width, Height);
    public Vector2RPG Min => new(X, Y);
    public Vector2RPG Max => new(X + Width, Y + Height);
    public float Area => Width * Height;

    // --- Queries ---

    public bool Contains(float px, float py) =>
        px >= X && px <= X + Width && py >= Y && py <= Y + Height;

    public bool Contains(Vector2RPG point) =>
        Contains(point.X, point.Y);

    public bool Overlaps(RectRPG other) =>
        XMin < other.XMax && XMax > other.XMin &&
        YMin < other.YMax && YMax > other.YMin;

    // --- Manipulation ---

    public RectRPG Expand(float amount) =>
        new(X - amount, Y - amount, Width + amount * 2f, Height + amount * 2f);

    public RectRPG Offset(float dx, float dy) =>
        new(X + dx, Y + dy, Width, Height);

    // --- Equality ---

    public bool Equals(RectRPG other) =>
        Math.Abs(X - other.X) < 1e-6f &&
        Math.Abs(Y - other.Y) < 1e-6f &&
        Math.Abs(Width - other.Width) < 1e-6f &&
        Math.Abs(Height - other.Height) < 1e-6f;

    public static bool operator ==(RectRPG a, RectRPG b) => a.Equals(b);
    public static bool operator !=(RectRPG a, RectRPG b) => !a.Equals(b);

    public override bool Equals(object? obj) => obj is RectRPG other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(X, Y, Width, Height);
    public override string ToString() => $"(x:{X:F2}, y:{Y:F2}, w:{Width:F2}, h:{Height:F2})";
}
