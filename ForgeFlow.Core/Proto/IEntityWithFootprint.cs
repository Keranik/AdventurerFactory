using ForgeFlow.Core.Entities;

namespace ForgeFlow.Core.Proto;

/// <summary>
/// Implemented by entities that occupy more than one tile (multi-tile buildings, large structures).
/// Returns the set of grid positions the entity covers. Single-tile entities return only their Position.
/// Future-proofing for multi-tile buildings and 3D support.
/// </summary>
public interface IEntityWithFootprint
{
    IReadOnlyList<GridPosRPG> GetFootprint();
}
