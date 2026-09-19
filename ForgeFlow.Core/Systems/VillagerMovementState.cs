using ForgeFlow.Core.Entities;
using ForgeFlow.Core.Proto.Logic;
using ForgeFlow.Core.Proto.Prototypes;

namespace ForgeFlow.Core.Systems;

/// <summary>
/// Compact, cache-friendly mirror of the movement-critical fields used by
/// <see cref="PathTrafficSystem.TickVillagers"/> each fixed step.
/// <para>
/// <see cref="VillagerSystem"/> maintains a parallel <c>VillagerMovementState[]</c>
/// whose index always aligns with <c>VillagerSystem.Villagers</c>. The array is
/// kept tightly packed via swap-remove so it can be iterated without pointer
/// chasing into the larger <see cref="VillagerLogic"/> objects.
/// </para>
/// <para>
/// <b>Groundwork note:</b> this struct is preparation for a future Burst/Jobs
/// conversion. The hot tick loop in <see cref="PathTrafficSystem"/> reads these
/// fields from the compact array and writes results back to the matching
/// <see cref="VillagerLogic"/> at the end of each tick. A full facade conversion
/// (where <see cref="VillagerLogic"/> delegates its properties into this array)
/// is deferred to that future milestone.
/// </para>
/// </summary>
public struct VillagerMovementState
{
    /// <summary>Entity ID — used to locate the owning <see cref="VillagerLogic"/> for sync-back.</summary>
    public ulong Id;

    public VillagerState State;
    public ulong? CurrentPathSegmentId;
    public float PathProgress;
    public float MovementSpeed;
    public GridPosRPG Position;
    public string? RoutingTag;

    /// <summary>Creates a movement state snapshot from the current <see cref="VillagerLogic"/> fields.</summary>
    public static VillagerMovementState FromLogic(VillagerLogic v) => new()
    {
        Id = v.Id,
        State = v.State,
        CurrentPathSegmentId = v.CurrentPathSegmentId,
        PathProgress = v.PathProgress,
        MovementSpeed = v.MovementSpeed,
        Position = v.Position,
        RoutingTag = v.RoutingTag,
    };

    /// <summary>
    /// Writes movement-result fields back to a <see cref="VillagerLogic"/>.
    /// Only the fields that <see cref="PathTrafficSystem"/> may mutate are applied.
    /// </summary>
    public readonly void ApplyToLogic(VillagerLogic v)
    {
        v.State = State;
        v.CurrentPathSegmentId = CurrentPathSegmentId;
        v.PathProgress = PathProgress;
        v.Position = Position;
    }
}
