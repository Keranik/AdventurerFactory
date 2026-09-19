using ForgeFlow.Core.Entities;
using ForgeFlow.Core.Proto;

namespace ForgeFlow.Core.Systems;

/// <summary>Type of entity occupying a grid tile.</summary>
public enum TileOccupantType
{
    None,
    Structure,
    Path,
    PathGate,
    ResourceNode
}

/// <summary>Identifies what occupies a specific grid tile.</summary>
public readonly struct TileOccupant
{
    public static readonly TileOccupant Empty = new(TileOccupantType.None, 0);

    public TileOccupantType Type { get; }
    public ulong EntityId { get; }
    public bool IsEmpty => Type == TileOccupantType.None;

    public TileOccupant(TileOccupantType type, ulong entityId)
    {
        Type = type;
        EntityId = entityId;
    }
}

/// <summary>
/// Single source of truth for the entire grid. Owns terrain data (via TerrainGrid)
/// and tile occupancy (structures, paths, path gates). Provides fast spatial queries.
/// Zero-alloc: all lookups are single dictionary hits with no allocation.
/// </summary>
public sealed class TileManager : IGameSystem
{
    private readonly Dictionary<GridPosRPG, TileOccupant> _occupants = new();

    /// <summary>The underlying terrain grid with biome and cell data.</summary>
    public TerrainGrid Terrain { get; }

    public int Width => Terrain.Width;
    public int Height => Terrain.Height;

    public TileManager(TerrainGrid terrain)
    {
        Terrain = terrain;
    }

    // ── Biome Queries ────────────────────────────────────────────────

    /// <summary>Returns the biome at the given position.</summary>
    public BiomeType GetBiome(GridPosRPG pos)
    {
        var cell = Terrain.Get(pos);
        return cell?.Biome ?? BiomeType.Plains;
    }

    /// <summary>Returns the full terrain cell at the given position, or null.</summary>
    public TerrainCell? GetCell(GridPosRPG pos) => Terrain.Get(pos);

    /// <summary>Returns true if the tile is pathable (walkable terrain).</summary>
    public bool IsPathable(GridPosRPG pos)
    {
        var cell = Terrain.Get(pos);
        return cell?.IsPathable ?? false;
    }

    // ── Occupancy Reads ──────────────────────────────────────────────

    /// <summary>Returns true if any entity (structure, path, gate, or resource node) occupies this tile.</summary>
    public bool IsOccupied(GridPosRPG pos) => _occupants.ContainsKey(pos);

    /// <summary>Tries to get the occupant at the given position.</summary>
    public bool TryGetOccupant(GridPosRPG pos, out TileOccupant occupant)
    {
        return _occupants.TryGetValue(pos, out occupant);
    }

    /// <summary>Returns the type of occupant at the given position, or None.</summary>
    public TileOccupantType GetOccupantType(GridPosRPG pos)
    {
        return _occupants.TryGetValue(pos, out var occupant) ? occupant.Type : TileOccupantType.None;
    }

    /// <summary>
    /// Returns true if the tile is free for placement (not occupied and pathable).
    /// </summary>
    public bool CanPlaceAt(GridPosRPG pos)
    {
        if (_occupants.ContainsKey(pos)) { return false; }
        var cell = Terrain.Get(pos);
        return cell?.IsPathable ?? false;
    }

    // ── Occupancy Writes ─────────────────────────────────────────────

    /// <summary>Registers an entity as occupying the given tile.</summary>
    public void SetOccupant(GridPosRPG pos, TileOccupantType type, ulong entityId)
    {
        _occupants[pos] = new TileOccupant(type, entityId);
    }

    /// <summary>Removes any occupant from the given tile.</summary>
    public void ClearOccupant(GridPosRPG pos)
    {
        _occupants.Remove(pos);
    }

    // ── Type-Filtered Lookups ────────────────────────────────────────

    /// <summary>Returns the structure ID at the given position, or null if no structure is there.</summary>
    public ulong? GetStructureIdAt(GridPosRPG pos)
    {
        if (_occupants.TryGetValue(pos, out var occ) && occ.Type == TileOccupantType.Structure)
        {
            return occ.EntityId;
        }
        return null;
    }

    /// <summary>Returns the path segment ID at the given position, or null.</summary>
    public ulong? GetPathIdAt(GridPosRPG pos)
    {
        if (_occupants.TryGetValue(pos, out var occ) && occ.Type == TileOccupantType.Path)
        {
            return occ.EntityId;
        }
        return null;
    }

    /// <summary>Returns the path gate ID at the given position, or null.</summary>
    public ulong? GetPathGateIdAt(GridPosRPG pos)
    {
        if (_occupants.TryGetValue(pos, out var occ) && occ.Type == TileOccupantType.PathGate)
        {
            return occ.EntityId;
        }
        return null;
    }

    /// <summary>Returns the resource node ID at the given position, or null.</summary>
    public ulong? GetResourceNodeIdAt(GridPosRPG pos)
    {
        if (_occupants.TryGetValue(pos, out var occ) && occ.Type == TileOccupantType.ResourceNode)
        {
            return occ.EntityId;
        }
        return null;
    }

    // ── Neighbor Queries

    /// <summary>
    /// Checks all four cardinal neighbors for a structure. Returns true if found.
    /// Zero-alloc: inline checks without array allocation.
    /// </summary>
    public bool HasStructureNeighbor(GridPosRPG pos, out ulong structureId, out GridPosRPG structurePos)
    {
        GridPosRPG n;

        n = new GridPosRPG(pos.X + 1, pos.Y);
        if (_occupants.TryGetValue(n, out var occ) && occ.Type == TileOccupantType.Structure)
        {
            structureId = occ.EntityId;
            structurePos = n;
            return true;
        }

        n = new GridPosRPG(pos.X - 1, pos.Y);
        if (_occupants.TryGetValue(n, out occ) && occ.Type == TileOccupantType.Structure)
        {
            structureId = occ.EntityId;
            structurePos = n;
            return true;
        }

        n = new GridPosRPG(pos.X, pos.Y + 1);
        if (_occupants.TryGetValue(n, out occ) && occ.Type == TileOccupantType.Structure)
        {
            structureId = occ.EntityId;
            structurePos = n;
            return true;
        }

        n = new GridPosRPG(pos.X, pos.Y - 1);
        if (_occupants.TryGetValue(n, out occ) && occ.Type == TileOccupantType.Structure)
        {
            structureId = occ.EntityId;
            structurePos = n;
            return true;
        }

        structureId = 0;
        structurePos = default;
        return false;
    }

    // ── Lifecycle ────────────────────────────────────────────────────

    /// <summary>Clears all tile occupancy data (does not affect terrain).</summary>
    public void Clear()
    {
        _occupants.Clear();
    }

    /// <summary>Clears occupancy and regenerates terrain with the given seed.</summary>
    public void RegenerateTerrain(int seed = 0)
    {
        _occupants.Clear();
        Terrain.GenerateDefault(seed);
    }
}
