namespace ForgeFlow.Core.Entities;

/// <summary>
/// Central immutable entity ID system. Every entity, structure, path segment,
/// resource node, villager, and hero uses GameId for unique tracking.
/// Thread-safe, zero-alloc, value-type semantics. Pure .NET — zero Unity references.
/// </summary>
[Serializable]
public readonly struct GameId : IEquatable<GameId>, IComparable<GameId>
{
    private static long _globalCounter;

    /// <summary>The underlying 64-bit identifier.</summary>
    public ulong Value { get; }

    public GameId(ulong value) => Value = value;

    /// <summary>Creates a new globally unique GameId. Thread-safe.</summary>
    public static GameId Next() => new((ulong)Interlocked.Increment(ref _globalCounter));

    /// <summary>Creates a GameId from a known value (deserialization, tests).</summary>
    public static GameId From(ulong value) => new(value);

    /// <summary>Creates a batch of sequential GameIds. Zero-alloc for small batches.</summary>
    public static void NextBatch(Span<GameId> output)
    {
        long start = Interlocked.Add(ref _globalCounter, output.Length) - output.Length;
        for (int i = 0; i < output.Length; i++)
        {
            output[i] = new GameId((ulong)(start + i + 1));
        }
    }

    /// <summary>The invalid/empty sentinel value.</summary>
    public static GameId None => default;

    /// <summary>Whether this ID has been assigned (non-zero).</summary>
    public bool IsValid => Value != 0;

    /// <summary>Resets the global counter. For testing only.</summary>
    public static void ResetCounter() => Interlocked.Exchange(ref _globalCounter, 0);

    // --- Equality & Comparison ---
    public bool Equals(GameId other) => Value == other.Value;
    public override bool Equals(object? obj) => obj is GameId other && Equals(other);
    public override int GetHashCode() => Value.GetHashCode();
    public int CompareTo(GameId other) => Value.CompareTo(other.Value);

    // --- Operators ---
    public static bool operator ==(GameId left, GameId right) => left.Value == right.Value;
    public static bool operator !=(GameId left, GameId right) => left.Value != right.Value;
    public static bool operator <(GameId left, GameId right) => left.Value < right.Value;
    public static bool operator >(GameId left, GameId right) => left.Value > right.Value;
    public static bool operator <=(GameId left, GameId right) => left.Value <= right.Value;
    public static bool operator >=(GameId left, GameId right) => left.Value >= right.Value;

    // --- Implicit conversions for backward compatibility ---
    public static implicit operator ulong(GameId id) => id.Value;
    public static implicit operator GameId(ulong value) => new(value);

    public override string ToString() => $"GID:{Value}";

    /// <summary>Short hex display for debug HUDs.</summary>
    public string ToShortHex() => $"#{Value:X8}";
}
