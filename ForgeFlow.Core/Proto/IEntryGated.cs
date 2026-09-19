using ForgeFlow.Core.Proto.Logic;

namespace ForgeFlow.Core.Proto;

/// <summary>
/// Rejection reasons returned by <see cref="IEntryGated.CheckEntry"/> when a
/// villager is not eligible to enter a structure. Zero-alloc enum for hot-path use.
/// </summary>
public enum EntryRejectionReason
{
    None = 0,
    StructureFull,
    InventoryFull,
    NotCarryingItems,
    NotCarryingRequiredItems,
    WrongClass,
    NotResident,
    AlreadyTrained,
    NotEligible
}

/// <summary>
/// Result of an <see cref="IEntryGated.CheckEntry"/> call.
/// Readonly struct for zero-alloc hot-path usage.
/// </summary>
public readonly struct EntryCheckResult
{
    public bool IsAccepted { get; }
    public EntryRejectionReason Reason { get; }

    private EntryCheckResult(bool accepted, EntryRejectionReason reason)
    {
        IsAccepted = accepted;
        Reason = reason;
    }

    public static readonly EntryCheckResult Accepted = new(true, EntryRejectionReason.None);

    public static EntryCheckResult Rejected(EntryRejectionReason reason)
        => new(false, reason);
}

/// <summary>
/// Implemented by structure Logic classes that declare entry requirements.
/// <see cref="PathGateManager"/> calls <see cref="CheckEntry"/> before allowing
/// a villager through the gate. Must be side-effect-free — this is a read-only
/// eligibility check. Structures that don't implement this accept all villagers
/// unconditionally.
/// </summary>
public interface IEntryGated
{
    EntryCheckResult CheckEntry(VillagerLogic villager);
}
