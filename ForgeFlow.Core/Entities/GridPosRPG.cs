namespace ForgeFlow.Core.Entities;

/// <summary>
/// Immutable RPG-style grid position struct. Extends the GridPosition concept
/// with rich operators, factories, and expressive helper methods.
/// Pure .NET — zero Unity references.
/// </summary>
[Serializable]
public readonly struct GridPosRPG : IEquatable<GridPosRPG>, IComparable<GridPosRPG>
{
    public int X { get; }
    public int Y { get; }

    public GridPosRPG(int x, int y)
    {
        X = x;
        Y = y;
    }

    // --- Named Factories ---
    public static GridPosRPG Origin => default;
    public static GridPosRPG One => new(1, 1);
    public static GridPosRPG North => new(0, 1);
    public static GridPosRPG South => new(0, -1);
    public static GridPosRPG East => new(1, 0);
    public static GridPosRPG West => new(-1, 0);

    /// <summary>Creates from a Direction enum.</summary>
    public static GridPosRPG FromDirection(Direction direction) => direction switch
    {
        Direction.North => North,
        Direction.East => East,
        Direction.South => South,
        Direction.West => West,
        _ => Origin
    };

    // --- Navigation ---

    /// <summary>Returns the neighbor in the given direction.</summary>
    public GridPosRPG Neighbor(Direction direction) => this + FromDirection(direction);

    /// <summary>Returns a position offset by (dx, dy).</summary>
    public GridPosRPG Offset(int dx, int dy) => new(X + dx, Y + dy);

    /// <summary>Returns all 4 cardinal neighbors.</summary>
    public void GetCardinalNeighbors(Span<GridPosRPG> output)
    {
        output[0] = new GridPosRPG(X, Y + 1);
        output[1] = new GridPosRPG(X + 1, Y);
        output[2] = new GridPosRPG(X, Y - 1);
        output[3] = new GridPosRPG(X - 1, Y);
    }

    /// <summary>Returns all 8 neighbors (cardinal + diagonal).</summary>
    public void GetAllNeighbors(Span<GridPosRPG> output)
    {
        output[0] = new GridPosRPG(X, Y + 1);
        output[1] = new GridPosRPG(X + 1, Y + 1);
        output[2] = new GridPosRPG(X + 1, Y);
        output[3] = new GridPosRPG(X + 1, Y - 1);
        output[4] = new GridPosRPG(X, Y - 1);
        output[5] = new GridPosRPG(X - 1, Y - 1);
        output[6] = new GridPosRPG(X - 1, Y);
        output[7] = new GridPosRPG(X - 1, Y + 1);
    }

    // --- Distance ---

    /// <summary>Manhattan distance (grid-optimal for 4-dir movement).</summary>
    public int ManhattanTo(GridPosRPG other) =>
        Math.Abs(X - other.X) + Math.Abs(Y - other.Y);

    /// <summary>Chebyshev distance (grid-optimal for 8-dir movement).</summary>
    public int ChebyshevTo(GridPosRPG other) =>
        Math.Max(Math.Abs(X - other.X), Math.Abs(Y - other.Y));

    /// <summary>Squared Euclidean distance (avoids sqrt, good for comparisons).</summary>
    public int SquaredDistanceTo(GridPosRPG other)
    {
        int dx = X - other.X;
        int dy = Y - other.Y;
        return dx * dx + dy * dy;
    }

    /// <summary>Euclidean distance.</summary>
    public float DistanceTo(GridPosRPG other) =>
        (float)Math.Sqrt(SquaredDistanceTo(other));

    /// <summary>Whether this position is within range of another (Manhattan).</summary>
    public bool IsWithinRange(GridPosRPG other, int range) =>
        ManhattanTo(other) <= range;

    // --- Area / Region ---

    /// <summary>Whether this position is inside a rectangular region (inclusive).</summary>
    public bool IsInRect(GridPosRPG min, GridPosRPG max) =>
        X >= min.X && X <= max.X && Y >= min.Y && Y <= max.Y;

    /// <summary>Clamps this position within a rectangular region.</summary>
    public GridPosRPG Clamp(GridPosRPG min, GridPosRPG max) =>
        new(Math.Max(min.X, Math.Min(max.X, X)), Math.Max(min.Y, Math.Min(max.Y, Y)));

    // --- Direction ---

    /// <summary>
    /// Returns the cardinal direction from <paramref name="from"/> to <paramref name="to"/>.
    /// When the positions differ on both axes, the dominant axis wins.
    /// Returns null when the positions are equal.
    /// </summary>
    public static Direction? DirectionFromTo(GridPosRPG from, GridPosRPG to)
    {
        int dx = to.X - from.X;
        int dy = to.Y - from.Y;
        if (dx == 0 && dy == 0) return null;
        if (Math.Abs(dx) >= Math.Abs(dy))
            return dx > 0 ? Direction.East : Direction.West;
        return dy > 0 ? Direction.North : Direction.South;
    }

    // --- Operators ---

    public static GridPosRPG operator +(GridPosRPG a, GridPosRPG b) => new(a.X + b.X, a.Y + b.Y);
    public static GridPosRPG operator -(GridPosRPG a, GridPosRPG b) => new(a.X - b.X, a.Y - b.Y);
    public static GridPosRPG operator *(GridPosRPG a, int scalar) => new(a.X * scalar, a.Y * scalar);
    public static GridPosRPG operator -(GridPosRPG a) => new(-a.X, -a.Y);

    // --- Equality & Comparison ---

    public bool Equals(GridPosRPG other) => X == other.X && Y == other.Y;
    public override bool Equals(object? obj) => obj is GridPosRPG other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(X, Y);
    public int CompareTo(GridPosRPG other)
    {
        int cmp = Y.CompareTo(other.Y);
        return cmp != 0 ? cmp : X.CompareTo(other.X);
    }

    public static bool operator ==(GridPosRPG left, GridPosRPG right) => left.Equals(right);
    public static bool operator !=(GridPosRPG left, GridPosRPG right) => !left.Equals(right);

    public override string ToString() => $"({X}, {Y})";
}

/// <summary>
/// Immutable relative grid offset. Represents a direction + magnitude
/// for path segment deltas, area-of-effect radii, building footprints, etc.
/// Pure .NET — zero Unity references.
/// </summary>
[Serializable]
public readonly struct GridRelRPG : IEquatable<GridRelRPG>
{
    public int DX { get; }
    public int DY { get; }

    public GridRelRPG(int dx, int dy)
    {
        DX = dx;
        DY = dy;
    }

    // --- Named Offsets ---
    public static GridRelRPG Zero => default;
    public static GridRelRPG Up => new(0, 1);
    public static GridRelRPG Down => new(0, -1);
    public static GridRelRPG Right => new(1, 0);
    public static GridRelRPG Left => new(-1, 0);
    public static GridRelRPG UpRight => new(1, 1);
    public static GridRelRPG UpLeft => new(-1, 1);
    public static GridRelRPG DownRight => new(1, -1);
    public static GridRelRPG DownLeft => new(-1, -1);

    /// <summary>Creates from a Direction enum.</summary>
    public static GridRelRPG FromDirection(Direction direction) => direction switch
    {
        Direction.North => Up,
        Direction.East => Right,
        Direction.South => Down,
        Direction.West => Left,
        _ => Zero
    };

    /// <summary>Manhattan magnitude.</summary>
    public int Manhattan => Math.Abs(DX) + Math.Abs(DY);

    /// <summary>Chebyshev magnitude.</summary>
    public int Chebyshev => Math.Max(Math.Abs(DX), Math.Abs(DY));

    /// <summary>Squared magnitude (avoids sqrt).</summary>
    public int SquaredMagnitude => DX * DX + DY * DY;

    /// <summary>Rotates 90° clockwise.</summary>
    public GridRelRPG RotateCW() => new(DY, -DX);

    /// <summary>Rotates 90° counter-clockwise.</summary>
    public GridRelRPG RotateCCW() => new(-DY, DX);

    /// <summary>Rotates 180°.</summary>
    public GridRelRPG Reverse() => new(-DX, -DY);

    /// <summary>Scales by a factor.</summary>
    public GridRelRPG Scale(int factor) => new(DX * factor, DY * factor);

    /// <summary>Applies this offset to a position.</summary>
    public GridPosRPG ApplyTo(GridPosRPG origin) => new(origin.X + DX, origin.Y + DY);

    // --- Operators ---
    public static GridRelRPG operator +(GridRelRPG a, GridRelRPG b) => new(a.DX + b.DX, a.DY + b.DY);
    public static GridRelRPG operator -(GridRelRPG a, GridRelRPG b) => new(a.DX - b.DX, a.DY - b.DY);
    public static GridRelRPG operator *(GridRelRPG a, int s) => new(a.DX * s, a.DY * s);
    public static GridRelRPG operator -(GridRelRPG a) => new(-a.DX, -a.DY);

    /// <summary>Apply offset to a position via + operator.</summary>
    public static GridPosRPG operator +(GridPosRPG pos, GridRelRPG rel) => new(pos.X + rel.DX, pos.Y + rel.DY);

    // --- Equality ---
    public bool Equals(GridRelRPG other) => DX == other.DX && DY == other.DY;
    public override bool Equals(object? obj) => obj is GridRelRPG other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(DX, DY);

    public static bool operator ==(GridRelRPG left, GridRelRPG right) => left.Equals(right);
    public static bool operator !=(GridRelRPG left, GridRelRPG right) => !left.Equals(right);

    public override string ToString() => $"Δ({DX}, {DY})";
}
