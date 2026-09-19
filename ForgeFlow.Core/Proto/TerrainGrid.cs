using ForgeFlow.Core.Entities;

namespace ForgeFlow.Core.Proto;

/// <summary>Biome types for terrain cells.</summary>
public enum BiomeType
{
    Plains,
    Forest,
    Mountain,
    Hills,
    Swamp,
    Desert,
    Tundra,
    Volcanic
}

/// <summary>A single cell in the terrain grid.</summary>
public sealed class TerrainCell
{
    public GridPosRPG Position { get; set; }
    public BiomeType Biome { get; set; } = BiomeType.Plains;
    public float Fertility { get; set; } = 1.0f;
    public float Hardness { get; set; }
    public string? ResourceNodeId { get; set; }
    public bool IsOccupied { get; set; }
    public bool IsPathable { get; set; } = true;
    public float Elevation { get; set; }
    public float Moisture { get; set; }
}

/// <summary>
/// Grid-based terrain map. Each cell holds a biome and optional resource.
/// Created once at world-gen; read by gathering structures to determine yield.
/// Uses region-based generation inspired by RPGGame's biome system:
/// seed points are scattered, then flood-filled to create coherent biome regions
/// (forests, mountain ranges, ore veins) instead of random per-tile noise.
/// </summary>
public sealed class TerrainGrid
{
    private readonly TerrainCell[] _cells;

    public int Width { get; }
    public int Height { get; }

    public TerrainGrid(int width, int height)
    {
        Width = width;
        Height = height;
        _cells = new TerrainCell[width * height];
        for (int i = 0; i < _cells.Length; i++)
        {
            int x = i % width;
            int y = i / width;
            _cells[i] = new TerrainCell { Position = new GridPosRPG(x, y) };
        }
    }

    /// <summary>Returns true if the position is within grid bounds.</summary>
    public bool InBounds(GridPosRPG pos)
    {
        return (uint)pos.X < (uint)Width && (uint)pos.Y < (uint)Height;
    }

    public TerrainCell GetOrCreate(GridPosRPG pos)
    {
        if (!InBounds(pos))
        {
            return new TerrainCell { Position = pos };
        }
        return _cells[pos.X + pos.Y * Width];
    }

    public TerrainCell? Get(GridPosRPG pos)
    {
        if (!InBounds(pos)) { return null; }
        return _cells[pos.X + pos.Y * Width];
    }

    public void Set(GridPosRPG pos, TerrainCell cell)
    {
        if (!InBounds(pos)) { return; }
        _cells[pos.X + pos.Y * Width] = cell;
    }

    public bool TryGet(GridPosRPG pos, out TerrainCell? cell)
    {
        if (!InBounds(pos)) { cell = null; return false; }
        cell = _cells[pos.X + pos.Y * Width];
        return true;
    }

    public IEnumerable<TerrainCell> GetAllCells() => _cells;

    // ── Feature Generator ───────────────────────────────────────────

    /// <summary>
    /// Generates terrain using region-based biome placement.
    /// 1. Fill everything as Plains
    /// 2. Scatter biome seed points based on region density
    /// 3. Flood-fill outward from seeds to create coherent regions
    /// 4. Carve a central clearing for the factory
    /// 5. Scatter resource nodes appropriate to each biome
    /// </summary>
    public void GenerateDefault(int seed = 0)
    {
        var rng = new Random(seed);

        // Step 1: Reset all cells to default Plains (in-place, zero-alloc)
        for (int i = 0; i < _cells.Length; i++)
        {
            var cell = _cells[i];
            cell.Biome = BiomeType.Plains;
            cell.Fertility = 1.0f;
            cell.Hardness = 0.3f;
            cell.ResourceNodeId = null;
            cell.IsOccupied = false;
            cell.IsPathable = true;
            cell.Elevation = 0f;
            cell.Moisture = 0.5f;
        }

        // Step 2: Generate elevation and moisture fields (value noise)
        var elevation = GenerateNoiseField(Width, Height, rng, octaves: 3, scale: 0.12f);
        var moisture = GenerateNoiseField(Width, Height, rng, octaves: 2, scale: 0.09f);

        // Step 3: Assign biomes based on elevation + moisture (Whittaker-style)
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                var cell = _cells[x + y * Width];
                float e = elevation[x, y];
                float m = moisture[x, y];
                cell.Elevation = e;
                cell.Moisture = m;
                cell.Biome = ClassifyBiome(e, m);
            }
        }

        // Step 4: Smooth biomes — isolated cells adopt neighbor majority
        SmoothBiomes(rng, passes: 2);

        // Step 5: Carve central factory clearing (radius ~3 around center)
        int cx = Width / 2, cy = Height / 2;
        int clearingRadius = Math.Max(2, Math.Min(Width, Height) / 5);
        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                int dx = x - cx, dy = y - cy;
                if (dx * dx + dy * dy <= clearingRadius * clearingRadius)
                {
                    var cell = _cells[x + y * Width];
                    cell.Biome = BiomeType.Plains;
                    cell.Fertility = 1.2f;
                    cell.Hardness = 0.2f;
                    cell.IsPathable = true;
                    cell.Elevation = 0f;
                }
            }
        }

        // Step 6: Apply biome-specific properties
        for (int i = 0; i < _cells.Length; i++)
        {
            ApplyBiomeProperties(_cells[i]);
        }

        // Step 7: Scatter resource nodes
        ScatterResourceNodes(rng);
    }

    /// <summary>Classifies biome from elevation/moisture (Whittaker diagram).</summary>
    private static BiomeType ClassifyBiome(float elevation, float moisture)
    {
        // High elevation → Mountain or Tundra
        if (elevation > 0.75f) return BiomeType.Mountain;
        if (elevation > 0.6f) return moisture > 0.5f ? BiomeType.Hills : BiomeType.Tundra;

        // Mid elevation
        if (elevation > 0.4f)
        {
            if (moisture > 0.7f) return BiomeType.Forest;
            if (moisture > 0.4f) return BiomeType.Hills;
            return BiomeType.Plains;
        }

        // Low elevation
        if (moisture > 0.75f) return BiomeType.Swamp;
        if (moisture > 0.55f) return BiomeType.Forest;
        if (moisture > 0.35f) return BiomeType.Plains;
        if (moisture > 0.15f) return BiomeType.Desert;
        return BiomeType.Volcanic;
    }

    /// <summary>Generates a 2D noise field using layered value noise.</summary>
    private static float[,] GenerateNoiseField(int w, int h, Random rng, int octaves, float scale)
    {
        var field = new float[w, h];
        float amplitude = 1f;
        float frequency = scale;
        float maxVal = 0f;

        // Create random gradient offsets per octave
        var offsetsX = new float[octaves];
        var offsetsY = new float[octaves];
        for (int o = 0; o < octaves; o++)
        {
            offsetsX[o] = (float)(rng.NextDouble() * 1000);
            offsetsY[o] = (float)(rng.NextDouble() * 1000);
        }

        amplitude = 1f;
        for (int o = 0; o < octaves; o++)
        {
            maxVal += amplitude;
            amplitude *= 0.5f;
        }

        for (int x = 0; x < w; x++)
        {
            for (int y = 0; y < h; y++)
            {
                float val = 0f;
                amplitude = 1f;
                frequency = scale;

                for (int o = 0; o < octaves; o++)
                {
                    float sx = (x + offsetsX[o]) * frequency;
                    float sy = (y + offsetsY[o]) * frequency;
                    val += ValueNoise2D(sx, sy) * amplitude;
                    amplitude *= 0.5f;
                    frequency *= 2f;
                }

                field[x, y] = (val / maxVal + 1f) * 0.5f; // normalize to 0..1
            }
        }

        return field;
    }

    /// <summary>Simple value noise using integer lattice + smooth interpolation.</summary>
    private static float ValueNoise2D(float x, float y)
    {
        int ix = (int)Math.Floor(x);
        int iy = (int)Math.Floor(y);
        float fx = x - ix;
        float fy = y - iy;

        // Smoothstep
        fx = fx * fx * (3f - 2f * fx);
        fy = fy * fy * (3f - 2f * fy);

        float v00 = HashFloat(ix, iy);
        float v10 = HashFloat(ix + 1, iy);
        float v01 = HashFloat(ix, iy + 1);
        float v11 = HashFloat(ix + 1, iy + 1);

        float top = v00 + (v10 - v00) * fx;
        float bot = v01 + (v11 - v01) * fx;
        return top + (bot - top) * fy;
    }

    /// <summary>Deterministic hash → float in -1..1 for noise lattice.</summary>
    private static float HashFloat(int x, int y)
    {
        int h = x * 374761393 + y * 668265263;
        h = (h ^ (h >> 13)) * 1274126177;
        h ^= h >> 16;
        return (h & 0x7FFFFFFF) / (float)0x7FFFFFFF * 2f - 1f;
    }

    /// <summary>Smooths biomes so isolated single-cell patches merge with neighbors.</summary>
    private void SmoothBiomes(Random rng, int passes)
    {
        for (int p = 0; p < passes; p++)
        {
            var changes = new Dictionary<GridPosRPG, BiomeType>();

            for (int i = 0; i < _cells.Length; i++)
            {
                var cell = _cells[i];
                var counts = new Dictionary<BiomeType, int>();
                foreach (var npos in GetCardinalNeighbors(cell.Position))
                {
                    if (InBounds(npos))
                    {
                        var neighbor = _cells[npos.X + npos.Y * Width];
                        counts.TryGetValue(neighbor.Biome, out int c);
                        counts[neighbor.Biome] = c + 1;
                    }
                }

                if (counts.Count == 0) { continue; }

                // If this cell's biome is a minority among neighbors, switch
                counts.TryGetValue(cell.Biome, out int selfCount);
                var best = counts.OrderByDescending(kv => kv.Value).First();
                if (best.Value >= 3 && best.Key != cell.Biome)
                {
                    changes[cell.Position] = best.Key;
                }
            }

            foreach (var (pos, biome) in changes)
            {
                _cells[pos.X + pos.Y * Width].Biome = biome;
            }
        }
    }

    private static IEnumerable<GridPosRPG> GetCardinalNeighbors(GridPosRPG pos)
    {
        yield return new GridPosRPG(pos.X - 1, pos.Y);
        yield return new GridPosRPG(pos.X + 1, pos.Y);
        yield return new GridPosRPG(pos.X, pos.Y - 1);
        yield return new GridPosRPG(pos.X, pos.Y + 1);
    }

    /// <summary>Sets cell properties based on biome type.</summary>
    private static void ApplyBiomeProperties(TerrainCell cell)
    {
        switch (cell.Biome)
        {
            case BiomeType.Plains:
                cell.Fertility = 1.0f + cell.Moisture * 0.5f;
                cell.Hardness = 0.3f;
                cell.IsPathable = true;
                break;
            case BiomeType.Forest:
                cell.Fertility = 1.5f + cell.Moisture * 0.3f;
                cell.Hardness = 0.5f;
                cell.IsPathable = true;
                break;
            case BiomeType.Mountain:
                cell.Fertility = 0.1f;
                cell.Hardness = 2.0f + cell.Elevation;
                cell.IsPathable = false;
                break;
            case BiomeType.Hills:
                cell.Fertility = 0.8f;
                cell.Hardness = 1.5f;
                cell.IsPathable = true;
                break;
            case BiomeType.Swamp:
                cell.Fertility = 1.8f;
                cell.Hardness = 0.2f;
                cell.IsPathable = true;
                break;
            case BiomeType.Desert:
                cell.Fertility = 0.2f;
                cell.Hardness = 0.8f;
                cell.IsPathable = true;
                break;
            case BiomeType.Tundra:
                cell.Fertility = 0.3f;
                cell.Hardness = 1.2f;
                cell.IsPathable = true;
                break;
            case BiomeType.Volcanic:
                cell.Fertility = 0.0f;
                cell.Hardness = 2.5f;
                cell.IsPathable = false;
                break;
        }
    }

    /// <summary>Scatters resource nodes into biome-appropriate cells.</summary>
    private void ScatterResourceNodes(Random rng)
    {
        var resourcesByBiome = new Dictionary<BiomeType, (string[] nodeIds, float chance)>
        {
            { BiomeType.Forest, (new[] { "oak_tree", "pine_tree" }, 0.25f) },
            { BiomeType.Hills, (new[] { "iron_vein", "herb_patch" }, 0.15f) },
            { BiomeType.Mountain, (new[] { "iron_vein", "crystal_deposit" }, 0.20f) },
            { BiomeType.Plains, (new[] { "herb_patch" }, 0.08f) },
            { BiomeType.Swamp, (new[] { "herb_patch" }, 0.18f) },
            { BiomeType.Desert, (new[] { "crystal_deposit" }, 0.10f) },
            { BiomeType.Tundra, (new[] { "iron_vein" }, 0.12f) },
            { BiomeType.Volcanic, (new[] { "crystal_deposit" }, 0.15f) },
        };

        for (int i = 0; i < _cells.Length; i++)
        {
            var cell = _cells[i];
            if (cell.IsOccupied || cell.ResourceNodeId != null) { continue; }
            if (!resourcesByBiome.TryGetValue(cell.Biome, out var mapping)) { continue; }

            if (rng.NextDouble() < mapping.chance)
            {
                cell.ResourceNodeId = mapping.nodeIds[rng.Next(mapping.nodeIds.Length)];
            }
        }
    }

    public IEnumerable<TerrainCell> GetCellsByBiome(BiomeType biome)
    {
        for (int i = 0; i < _cells.Length; i++)
        {
            if (_cells[i].Biome == biome) { yield return _cells[i]; }
        }
    }

    public int CellCount => _cells.Length;
}
