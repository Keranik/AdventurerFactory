using ForgeFlow.Core.Entities;

namespace ForgeFlow.Core.Proto;

/// <summary>
/// Implemented by entities that accept villagers inside and need to eject them
/// via a PathGate exit. When the exit gate doesn't exist yet or the exit path
/// is blocked, villagers are queued in <see cref="WaitingToExitIds"/> and retried
/// every tick by StructureManager.
/// </summary>
public interface IStructureWithExits
{
    /// <summary>
    /// Villager IDs that finished their task inside this entity but couldn't exit
    /// (no exit gate or blocked path). StructureManager retries these each tick.
    /// </summary>
    List<EntityId> WaitingToExitIds { get; }

    /// <summary>Whether there are villagers waiting to exit this entity.</summary>
    bool HasWaitingExits { get; }
}
