namespace ForgeFlow.Core.Entities;

/// <summary>
/// Abstract base for non-moving entities that occupy fixed positions on the grid.
/// Subtypes include footprint-based buildings and path segments.
/// </summary>
public abstract class EntityStatic : EntityBase
{
    protected EntityStatic(EntityId id) : base(id) { }
}
