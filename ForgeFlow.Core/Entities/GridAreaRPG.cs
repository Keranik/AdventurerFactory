namespace ForgeFlow.Core.Entities;

/// <summary>
/// Immutable RPG-style multi-tile area. Represents any set of grid tiles —
/// single tiles, solid rectangles, or arbitrary sparse collections — for
/// tutorial highlights, building footprints, selection boxes, tool previews,
/// and pathfinding regions.
/// Pure .NET — zero Unity references.
/// </summary>
/// <remarks>
/// <para><b>Rectangular areas</b> are stored compactly (origin + width + height)
/// with no array allocation. <see cref="Contains(GridPosRPG)"/> is O(1).</para>
/// <para><b>Sparse areas</b> store a sorted, deduplicated copy of tiles for
/// deterministic equality and O(log n) binary-search <see cref="Contains(GridPosRPG)"/>.</para>
/// <para>
/// Typical usage:
/// <code>
/// var single    = GridAreaRPG.FromSingleTile(new GridPosRPG(5, 3));
/// var footprint = GridAreaRPG.FromRect(new GridPosRPG(0, 0), 3, 2);
/// var scattered = GridAreaRPG.FromTiles(tile1, tile2, tile3);
/// bool hit      = footprint.Contains(new GridPosRPG(1, 1)); // true
/// </code>
/// </para>
/// </remarks>
[Serializable]
public readonly struct GridAreaRPG : IEquatable<GridAreaRPG>
{
    // Internal storage — bounding box is valid for both modes.
    //   Rectangular (_sparseTiles == null): all tiles inside origin + width × height.
    //   Sparse      (_sparseTiles != null): only the explicit sorted tile array.
    private readonly GridPosRPG _origin;       // bounding-box min corner
    private readonly int _width;                // bounding-box width  (tiles)
    private readonly int _height;               // bounding-box height (tiles)
    private readonly GridPosRPG[]? _sparseTiles;

    // --- Private Constructors ---

    /// <summary>Rectangular area.</summary>
    private GridAreaRPG(GridPosRPG origin, int width, int height)
    {
        _origin = origin;
        _width = Math.Max(0, width);
        _height = Math.Max(0, height);
        _sparseTiles = null;
    }

    /// <summary>Sparse area. Takes ownership of a pre-sorted, deduplicated array.</summary>
    private GridAreaRPG(GridPosRPG[] sortedTiles, GridPosRPG boundingMin, int boundingWidth, int boundingHeight)
    {
        _sparseTiles = sortedTiles;
        _origin = boundingMin;
        _width = boundingWidth;
        _height = boundingHeight;
    }

    // --- Named Factories ---

    /// <summary>An empty area containing zero tiles.</summary>
    public static GridAreaRPG Empty => default;

    /// <summary>Creates a 1×1 area containing a single tile.</summary>
    public static GridAreaRPG FromSingleTile(GridPosRPG pos) => new(pos, 1, 1);

    /// <summary>Creates a solid rectangular area from an origin (bottom-left) and size in tiles.</summary>
    public static GridAreaRPG FromRect(GridPosRPG origin, int width, int height) =>
        (width <= 0 || height <= 0) ? Empty : new(origin, width, height);

    /// <summary>Creates a solid rectangular area from explicit coordinates and size.</summary>
    public static GridAreaRPG FromRect(int x, int y, int width, int height) =>
        FromRect(new GridPosRPG(x, y), width, height);

    /// <summary>
    /// Creates an area from an arbitrary set of tiles. Tiles are copied, sorted,
    /// and deduplicated. Pass any number of positions; duplicates are removed.
    /// </summary>
    public static GridAreaRPG FromTiles(params GridPosRPG[] tiles)
    {
        if (tiles == null || tiles.Length == 0) { return Empty; }
        if (tiles.Length == 1) { return FromSingleTile(tiles[0]); }

        // Copy so we don't mutate the caller's array
        var sorted = new GridPosRPG[tiles.Length];
        Array.Copy(tiles, sorted, tiles.Length);
        Array.Sort(sorted);

        // Deduplicate in-place
        int write = 1;
        for (int read = 1; read < sorted.Length; read++)
        {
            if (sorted[read] != sorted[write - 1])
            {
                sorted[write++] = sorted[read];
            }
        }

        if (write != sorted.Length)
        {
            Array.Resize(ref sorted, write);
        }

        if (sorted.Length == 1) { return FromSingleTile(sorted[0]); }

        return BuildSparse(sorted);
    }

    /// <summary>
    /// Creates a diamond-shaped area of all tiles within Manhattan distance
    /// <paramref name="radius"/> of <paramref name="center"/>.
    /// Radius 0 → 1 tile. Radius 1 → 5 tiles. Radius 2 → 13 tiles.
    /// </summary>
    public static GridAreaRPG FromManhattanRadius(GridPosRPG center, int radius)
    {
        if (radius < 0) { return Empty; }
        if (radius == 0) { return FromSingleTile(center); }

        int count = 2 * radius * radius + 2 * radius + 1;
        var tiles = new GridPosRPG[count];
        int idx = 0;
        for (int dy = -radius; dy <= radius; dy++)
        {
            int xRange = radius - Math.Abs(dy);
            for (int dx = -xRange; dx <= xRange; dx++)
            {
                tiles[idx++] = new GridPosRPG(center.X + dx, center.Y + dy);
            }
        }

        // Tiles are already in GridPosRPG sort order (Y-major, X-minor)
        int size = 2 * radius + 1;
        var min = new GridPosRPG(center.X - radius, center.Y - radius);
        return new GridAreaRPG(tiles, min, size, size);
    }

    // --- Properties ---

    /// <summary>True if this area contains zero tiles.</summary>
    public bool IsEmpty => TileCount == 0;

    /// <summary>True if this area is stored as a solid rectangle (no sparse array).</summary>
    public bool IsRectangular => _sparseTiles == null;

    /// <summary>Total number of tiles in this area.</summary>
    public int TileCount => _sparseTiles != null ? _sparseTiles.Length : _width * _height;

    /// <summary>Bottom-left corner of the bounding box (inclusive).</summary>
    public GridPosRPG BoundingMin => _origin;

    /// <summary>Top-right corner of the bounding box (inclusive).
    /// Returns <see cref="GridPosRPG.Origin"/> when empty.</summary>
    public GridPosRPG BoundingMax =>
        IsEmpty ? GridPosRPG.Origin : new GridPosRPG(_origin.X + _width - 1, _origin.Y + _height - 1);

    /// <summary>Width of the bounding box in tiles.</summary>
    public int BoundingWidth => _width;

    /// <summary>Height of the bounding box in tiles.</summary>
    public int BoundingHeight => _height;

    /// <summary>Center of the bounding box as a continuous 2D position.</summary>
    public Vector2RPG Center =>
        IsEmpty ? Vector2RPG.Zero : new Vector2RPG(_origin.X + _width * 0.5f, _origin.Y + _height * 0.5f);

    // --- Queries ---

    /// <summary>Whether <paramref name="pos"/> is contained in this area.</summary>
    public bool Contains(GridPosRPG pos)
    {
        if (IsEmpty) { return false; }

        if (_sparseTiles != null)
        {
            return Array.BinarySearch(_sparseTiles, pos) >= 0;
        }

        return pos.X >= _origin.X && pos.X < _origin.X + _width &&
               pos.Y >= _origin.Y && pos.Y < _origin.Y + _height;
    }

    /// <summary>Whether this area shares at least one tile with <paramref name="other"/>.</summary>
    public bool Overlaps(GridAreaRPG other)
    {
        if (IsEmpty || other.IsEmpty) { return false; }

        // Quick bounding-box rejection
        if (_origin.X >= other._origin.X + other._width ||
            other._origin.X >= _origin.X + _width ||
            _origin.Y >= other._origin.Y + other._height ||
            other._origin.Y >= _origin.Y + _height)
        {
            return false;
        }

        // Both solid rectangles — bounding overlap is the full answer
        if (_sparseTiles == null && other._sparseTiles == null) { return true; }

        // At least one sparse — iterate the smaller set, check against the larger
        if (TileCount <= other.TileCount)
        {
            return HasAnyTileIn(other);
        }
        return other.HasAnyTileIn(this);
    }

    // --- Manipulation ---

    /// <summary>Returns a copy translated by (<paramref name="dx"/>, <paramref name="dy"/>).</summary>
    public GridAreaRPG Translate(int dx, int dy)
    {
        if (IsEmpty) { return this; }

        var newOrigin = new GridPosRPG(_origin.X + dx, _origin.Y + dy);

        if (_sparseTiles == null)
        {
            return new GridAreaRPG(newOrigin, _width, _height);
        }

        var moved = new GridPosRPG[_sparseTiles.Length];
        for (int i = 0; i < _sparseTiles.Length; i++)
        {
            moved[i] = _sparseTiles[i].Offset(dx, dy);
        }
        // Sort order is preserved — all tiles shift by the same offset
        return new GridAreaRPG(moved, newOrigin, _width, _height);
    }

    /// <summary>Returns a copy translated by <paramref name="offset"/>.</summary>
    public GridAreaRPG Translate(GridRelRPG offset) => Translate(offset.DX, offset.DY);

    // --- Enumeration ---

    /// <summary>Returns a new array containing all tiles in this area.</summary>
    public GridPosRPG[] GetAllTiles()
    {
        int count = TileCount;
        if (count == 0) { return Array.Empty<GridPosRPG>(); }

        var result = new GridPosRPG[count];
        WriteTiles(result);
        return result;
    }

    /// <summary>
    /// Writes all tiles into <paramref name="output"/>. The span must have at
    /// least <see cref="TileCount"/> elements. Returns the number of tiles written.
    /// Zero-alloc when used with a pre-allocated or stack-allocated buffer.
    /// </summary>
    public int WriteTiles(Span<GridPosRPG> output)
    {
        if (_sparseTiles != null)
        {
            _sparseTiles.AsSpan().CopyTo(output);
            return _sparseTiles.Length;
        }

        int idx = 0;
        for (int y = 0; y < _height; y++)
        {
            for (int x = 0; x < _width; x++)
            {
                output[idx++] = new GridPosRPG(_origin.X + x, _origin.Y + y);
            }
        }
        return idx;
    }

    // --- Conversions ---

    /// <summary>Implicitly converts a single tile position to a 1×1 area.</summary>
    public static implicit operator GridAreaRPG(GridPosRPG pos) => FromSingleTile(pos);

    /// <summary>
    /// Explicitly converts a float-based <see cref="RectRPG"/> to a grid area.
    /// Origin is floored; extent is ceiled so all overlapped tiles are included.
    /// </summary>
    public static explicit operator GridAreaRPG(RectRPG rect)
    {
        int x = (int)Math.Floor(rect.X);
        int y = (int)Math.Floor(rect.Y);
        int w = Math.Max(0, (int)Math.Ceiling(rect.XMax) - x);
        int h = Math.Max(0, (int)Math.Ceiling(rect.YMax) - y);
        return FromRect(x, y, w, h);
    }

    // --- Equality ---

    public bool Equals(GridAreaRPG other)
    {
        if (TileCount != other.TileCount) { return false; }
        if (IsEmpty && other.IsEmpty) { return true; }

        // Both rectangular — same origin and size is sufficient
        if (_sparseTiles == null && other._sparseTiles == null)
        {
            return _origin == other._origin && _width == other._width && _height == other._height;
        }

        // Both sparse — element-by-element (arrays are sorted)
        if (_sparseTiles != null && other._sparseTiles != null)
        {
            for (int i = 0; i < _sparseTiles.Length; i++)
            {
                if (_sparseTiles[i] != other._sparseTiles[i]) { return false; }
            }
            return true;
        }

        // Mixed: one rect, one sparse — sparse must exactly fill the rect
        GridAreaRPG rect = _sparseTiles == null ? this : other;
        GridAreaRPG sparse = _sparseTiles != null ? this : other;

        for (int i = 0; i < sparse._sparseTiles!.Length; i++)
        {
            if (!rect.Contains(sparse._sparseTiles[i])) { return false; }
        }
        return true;
    }

    public override bool Equals(object? obj) => obj is GridAreaRPG other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(_origin, _width, _height, TileCount);

    public static bool operator ==(GridAreaRPG left, GridAreaRPG right) => left.Equals(right);
    public static bool operator !=(GridAreaRPG left, GridAreaRPG right) => !left.Equals(right);

    // --- Formatting ---

    public override string ToString()
    {
        if (IsEmpty) { return "Area(empty)"; }
        if (_sparseTiles == null)
        {
            return $"Area({_origin.X},{_origin.Y} {_width}×{_height} {TileCount} tiles)";
        }
        return $"Area({TileCount} tiles, bounds {_origin.X},{_origin.Y} {_width}×{_height})";
    }

    // --- Private Helpers ---

    private bool HasAnyTileIn(GridAreaRPG target)
    {
        if (_sparseTiles != null)
        {
            for (int i = 0; i < _sparseTiles.Length; i++)
            {
                if (target.Contains(_sparseTiles[i])) { return true; }
            }
            return false;
        }

        for (int y = 0; y < _height; y++)
        {
            for (int x = 0; x < _width; x++)
            {
                if (target.Contains(new GridPosRPG(_origin.X + x, _origin.Y + y))) { return true; }
            }
        }
        return false;
    }

    private static GridAreaRPG BuildSparse(GridPosRPG[] sortedDedupedTiles)
    {
        int minX = sortedDedupedTiles[0].X, maxX = minX;
        int minY = sortedDedupedTiles[0].Y, maxY = minY;

        for (int i = 1; i < sortedDedupedTiles.Length; i++)
        {
            var t = sortedDedupedTiles[i];
            if (t.X < minX) { minX = t.X; }
            if (t.Y < minY) { minY = t.Y; }
            if (t.X > maxX) { maxX = t.X; }
            if (t.Y > maxY) { maxY = t.Y; }
        }

        return new GridAreaRPG(
            sortedDedupedTiles,
            new GridPosRPG(minX, minY),
            maxX - minX + 1,
            maxY - minY + 1);
    }
}

// ============================================================================
// Examples
// ============================================================================
//
// Single tile highlight (tutorial step):
//   var highlight = GridAreaRPG.FromSingleTile(new GridPosRPG(5, 3));
//
// Building footprint (2×3 structure):
//   var footprint = GridAreaRPG.FromRect(new GridPosRPG(10, 10), 2, 3);
//   bool occupied = footprint.Contains(new GridPosRPG(11, 12)); // true
//
// Scattered tiles (biome-filtered selection):
//   var forest = GridAreaRPG.FromTiles(tile1, tile2, tile3);
//   bool overlaps = footprint.Overlaps(forest);
//
// Diamond AoE preview (Manhattan radius 2 → 13 tiles):
//   var aoe = GridAreaRPG.FromManhattanRadius(new GridPosRPG(5, 5), 2);
//
// Implicit conversion from single tile:
//   GridAreaRPG area = new GridPosRPG(3, 4);
//
// Zero-alloc tile enumeration with stack buffer:
//   Span<GridPosRPG> buffer = stackalloc GridPosRPG[footprint.TileCount];
//   int written = footprint.WriteTiles(buffer);
//
// Translation:
//   var moved = footprint.Translate(2, -1);
//
// Explicit conversion from RectRPG:
//   var gridArea = (GridAreaRPG)new RectRPG(1.5f, 2.5f, 3.0f, 2.0f);
