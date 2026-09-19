using ForgeFlow.Core.Utilities;

namespace ForgeFlow.Core.Entities;

/// <summary>
/// Strongly-typed wrapper for entity IDs. Prevents accidental misuse of raw
/// ulong values as entity identifiers. Backed by <see cref="EntityIdFactory"/>.
/// Zero-cost at runtime (single ulong field, readonly struct).
/// </summary>
public readonly struct EntityId : IEquatable<EntityId>
{
    /// <summary>The raw numeric identifier.</summary>
    public ulong Value { get; }

    public EntityId(ulong value)
    {
        Value = value;
    }

    /// <summary>Sentinel value representing "no entity" (Value == 0).</summary>
    public static readonly EntityId None = new(0);

    /// <summary>Allocates the next globally unique entity ID via <see cref="EntityIdFactory"/>.</summary>
    public static EntityId Next() => new(EntityIdFactory.Next());

    /// <summary>
    /// Resets the underlying ID counter to 0 so the next call to <see cref="Next"/>
    /// returns EntityId(1). Only for use in tests — never call in production code.
    /// </summary>
    [Obsolete("Use EntityIdFactory.ResetForTesting() directly; this pass-through will be removed.")]
    public static void ResetForTesting() => EntityIdFactory.ResetForTesting();

    /// <summary>Implicit conversion to ulong for backward compatibility with code not yet migrated to EntityId.</summary>
    public static implicit operator ulong(EntityId id) => id.Value;

    public bool Equals(EntityId other) => Value == other.Value;
    public override bool Equals(object? obj) => obj is EntityId other && Equals(other);
    public override int GetHashCode() => Value.GetHashCode();
    public static bool operator ==(EntityId left, EntityId right) => left.Value == right.Value;
    public static bool operator !=(EntityId left, EntityId right) => left.Value != right.Value;

    /// <inheritdoc />
    public override string ToString() => $"Entity#{Value}";
}
