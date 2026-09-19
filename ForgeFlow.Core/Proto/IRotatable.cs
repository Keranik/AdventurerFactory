using ForgeFlow.Core.Entities;

namespace ForgeFlow.Core.Proto;

/// <summary>
/// Implemented by entities that support player-initiated rotation (R key).
/// Managers call Rotate() and publish EntityRotatedEvent.
/// </summary>
public interface IRotatable
{
    Direction Facing { get; set; }
    void Rotate(GridPosRPG? anchorPosition = null);
}
