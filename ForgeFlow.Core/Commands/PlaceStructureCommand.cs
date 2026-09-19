using ForgeFlow.Core.Entities;

namespace ForgeFlow.Core.Commands;

/// <summary>
/// Command to place a proto-based structure at a grid position.
/// Handled by <see cref="Systems.StructureManager"/>: validates gold,
/// deducts cost, places the entity, publishes events, and advances tutorials.
/// </summary>
public readonly struct PlaceStructureCommand : IGameCommand
{
    /// <summary>The pre-created structure entity to place.</summary>
    public StructureBase Entity { get; }

    /// <summary>Grid position to place the structure at.</summary>
    public GridPosRPG Position { get; }

    /// <summary>Category name for cost lookup (e.g. "Spawner", "Forestry", "PathGate").</summary>
    public string Category { get; }

    public PlaceStructureCommand(StructureBase entity, GridPosRPG position, string category)
    {
        Entity = entity;
        Position = position;
        Category = category;
    }

    public override string ToString() => $"Place {Category} at {Position}";
}
