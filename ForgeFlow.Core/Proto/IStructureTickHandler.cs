using ForgeFlow.Core.Proto.Logic;
using ForgeFlow.Core.Proto.Prototypes;

namespace ForgeFlow.Core.Proto;

/// <summary>
/// Delegate for looking up a <see cref="VillagerLogic"/> by raw ID.
/// Used by <see cref="IStructureTickHandler.ProcessStructureTick"/> so that
/// Logic classes can inspect villager state without holding system references
/// (Central Manager + Events rule).
/// </summary>
public delegate VillagerLogic? VillagerLookup(ulong villagerId);

/// <summary>
/// Identifies the type of exit event the manager should publish
/// when a villager leaves a structure.
/// </summary>
public enum StructureExitReason
{
    /// <summary>Generic exit — no structure-specific event is published.</summary>
    Generic = 0,

    /// <summary>Villager completed training at a training building.</summary>
    TrainingComplete,

    /// <summary>Villager rested at their home spawner.</summary>
    RestedAtHome,
}

/// <summary>
/// A villager that should be ejected from a structure at the end of a tick.
/// Readonly struct for zero-alloc hot-path usage.
/// </summary>
public readonly struct PendingExit
{
    public ulong VillagerId { get; }

    /// <summary>
    /// Identifies the type of exit event the manager should publish.
    /// <see cref="StructureExitReason.Generic"/> = no structure-specific event.
    /// </summary>
    public StructureExitReason ExitReason { get; }

    /// <summary>Optional metadata for the event (e.g. trained class name).</summary>
    public string? EventMetadata { get; }

    public PendingExit(ulong villagerId, StructureExitReason exitReason = StructureExitReason.Generic, string? eventMetadata = null)
    {
        VillagerId = villagerId;
        ExitReason = exitReason;
        EventMetadata = eventMetadata;
    }
}

/// <summary>
/// An item that a structure produced this tick and that the manager should create/give.
/// Readonly struct for zero-alloc hot-path usage.
/// </summary>
public readonly struct PendingItemOutput
{
    public string ItemProtoId { get; }
    public int Quantity { get; }

    /// <summary>
    /// If non-null, the item should be given to this specific villager
    /// (e.g. the crafter picks up their output, or a gatherer receives resources).
    /// If null, the item is placed at the structure's output position (e.g. Forge queue).
    /// </summary>
    public ulong? TargetVillagerId { get; }

    public PendingItemOutput(string itemProtoId, int quantity, ulong? targetVillagerId = null)
    {
        ItemProtoId = itemProtoId;
        Quantity = quantity;
        TargetVillagerId = targetVillagerId;
    }
}

/// <summary>
/// Mutable output struct populated by <see cref="IStructureTickHandler.ProcessStructureTick"/>.
/// Pre-allocated once on the manager and reused every tick. Fixed-size arrays avoid
/// per-tick allocations on the hot path.
/// </summary>
public struct StructureTickOutput
{
    /// <summary>Number of valid entries in <see cref="PendingExits"/>.</summary>
    public int ExitCount;

    /// <summary>Fixed-size, pre-allocated array of villagers to eject. Only indices 0..<see cref="ExitCount"/>-1 are valid.</summary>
    public PendingExit[] PendingExits;

    /// <summary>Number of valid entries in <see cref="PendingItems"/>.</summary>
    public int ItemCount;

    /// <summary>Fixed-size, pre-allocated array of items to create. Only indices 0..<see cref="ItemCount"/>-1 are valid.</summary>
    public PendingItemOutput[] PendingItems;

    /// <summary>If non-null, the manager should advance the tutorial with this condition type.</summary>
    public TutorialConditionType? TutorialAdvance;

    /// <summary>If true, the manager should publish a config-change event (e.g. stockpile filter changed).</summary>
    public bool ConfigChanged;

    /// <summary>Resets all counts and flags for reuse. Does not reallocate arrays.</summary>
    public void Reset()
    {
        ExitCount = 0;
        ItemCount = 0;
        TutorialAdvance = null;
        ConfigChanged = false;
    }
}

/// <summary>
/// Implemented by structure Logic classes that have per-tick occupant or output
/// processing. The <see cref="StructureManager"/> calls this interface each tick,
/// then reads the <see cref="StructureTickOutput"/> to publish events, create items,
/// and eject villagers.
/// <para>
/// This follows the same successful pattern as <see cref="IEntryGated"/>:
/// structures declare behavior via an interface, the manager orchestrates.
/// </para>
/// </summary>
public interface IStructureTickHandler
{
    /// <summary>
    /// Processes this structure's occupants/state for the current tick.
    /// Populates <paramref name="output"/> with pending exits and item outputs
    /// for the manager to handle. Must not publish events or interact with systems directly.
    /// </summary>
    /// <param name="dt">Delta time for this simulation tick.</param>
    /// <param name="villagerLookup">Delegate to resolve a <see cref="VillagerLogic"/> by raw ID.</param>
    /// <param name="output">Pre-allocated output struct. Caller resets before each call.</param>
    void ProcessStructureTick(float dt, VillagerLookup villagerLookup, ref StructureTickOutput output);
}
