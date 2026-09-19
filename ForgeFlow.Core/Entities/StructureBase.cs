using ForgeFlow.Core.Proto;

namespace ForgeFlow.Core.Entities;

/// <summary>
/// Abstract base for static entities that occupy one or more tiles on the grid.
/// Provides the <see cref="IEntityWithFootprint"/> contract.
/// Path segments and building-like entities both derive from this.
/// </summary>
public abstract class StructureBase : EntityStatic, IEntityWithFootprint
{
    protected StructureBase(EntityId id) : base(id) { }

    /// <inheritdoc />
    public abstract IReadOnlyList<GridPosRPG> GetFootprint();
}
