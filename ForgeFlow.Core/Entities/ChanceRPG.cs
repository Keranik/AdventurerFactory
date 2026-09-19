namespace ForgeFlow.Core.Entities;

/// <summary>
/// Immutable RPG-style probability struct. Represents a chance (0–1) with
/// rich operators for combining, chaining, and evaluating random outcomes.
/// Thread-safe roll evaluation. Pure .NET — zero Unity references.
/// </summary>
[Serializable]
public readonly struct ChanceRPG : IEquatable<ChanceRPG>, IComparable<ChanceRPG>
{
    /// <summary>The probability value (0.0 = impossible, 1.0 = guaranteed).</summary>
    public float Value { get; }

    public ChanceRPG(float value)
    {
        Value = value < 0f ? 0f : value > 1f ? 1f : value;
    }

    // --- Named Factories ---
    public static ChanceRPG Impossible => new(0f);
    public static ChanceRPG Certain => new(1f);
    public static ChanceRPG Half => new(0.5f);
    public static ChanceRPG OneInTen => new(0.1f);
    public static ChanceRPG OneInFour => new(0.25f);
    public static ChanceRPG ThreeInFour => new(0.75f);
    public static ChanceRPG OneInHundred => new(0.01f);

    /// <summary>Creates a chance from a percentage (0–100).</summary>
    public static ChanceRPG FromPercent(float percent) => new(percent / 100f);

    /// <summary>Creates a chance from ratio (e.g., 1-in-6 → FromRatio(1, 6)).</summary>
    public static ChanceRPG FromRatio(int numerator, int denominator) =>
        denominator > 0 ? new((float)numerator / denominator) : Impossible;

    /// <summary>Creates a chance for a given RPG tier (higher tier = better odds).</summary>
    public static ChanceRPG ForTier(int tier, float baseChance = 0.1f) =>
        new(baseChance + (tier - 1) * 0.05f);

    /// <summary>Creates a crit chance based on hero stats.</summary>
    public static ChanceRPG CritChance(float baseCrit, int tier) =>
        new(baseCrit + tier * 0.02f);

    // --- Evaluation ---

    /// <summary>Rolls against this chance using the provided random. Thread-safe.</summary>
    public bool Roll(Random random) => random.NextDouble() < Value;

    /// <summary>Rolls against this chance using a seeded random from the given seed.</summary>
    public bool Roll(int seed) => new Random(seed).NextDouble() < Value;

    /// <summary>Whether this chance always succeeds.</summary>
    public bool IsGuaranteed => Value >= 1f;

    /// <summary>Whether this chance always fails.</summary>
    public bool IsImpossible => Value <= 0f;

    /// <summary>Returns this chance as a percentage (0–100).</summary>
    public float AsPercent => Value * 100f;

    // --- Combinators ---

    /// <summary>Independent AND: both must succeed.</summary>
    public ChanceRPG And(ChanceRPG other) => new(Value * other.Value);

    /// <summary>Independent OR: at least one must succeed.</summary>
    public ChanceRPG Or(ChanceRPG other) => new(1f - (1f - Value) * (1f - other.Value));

    /// <summary>The complement (NOT): probability of failure.</summary>
    public ChanceRPG Not() => new(1f - Value);

    /// <summary>Boosts by a multiplier (clamps to 0–1).</summary>
    public ChanceRPG Boost(float multiplier) => new(Value * multiplier);

    /// <summary>Adds a flat bonus (clamps to 0–1).</summary>
    public ChanceRPG AddFlat(float bonus) => new(Value + bonus);

    /// <summary>Lerps between this chance and another.</summary>
    public ChanceRPG LerpTo(ChanceRPG other, float t) =>
        new(Value + (other.Value - Value) * (t < 0f ? 0f : t > 1f ? 1f : t));

    /// <summary>The probability of succeeding at least once in N independent trials.</summary>
    public ChanceRPG AtLeastOnceIn(int trials) =>
        new(1f - (float)Math.Pow(1f - Value, trials));

    /// <summary>The expected number of trials to succeed once.</summary>
    public float ExpectedTrials => Value > 0f ? 1f / Value : float.PositiveInfinity;

    // --- Operators ---
    public static ChanceRPG operator +(ChanceRPG a, ChanceRPG b) => a.Or(b);
    public static ChanceRPG operator *(ChanceRPG a, ChanceRPG b) => a.And(b);
    public static ChanceRPG operator *(ChanceRPG a, float s) => a.Boost(s);
    public static ChanceRPG operator !(ChanceRPG a) => a.Not();

    public static bool operator >(ChanceRPG a, ChanceRPG b) => a.Value > b.Value;
    public static bool operator <(ChanceRPG a, ChanceRPG b) => a.Value < b.Value;
    public static bool operator >=(ChanceRPG a, ChanceRPG b) => a.Value >= b.Value;
    public static bool operator <=(ChanceRPG a, ChanceRPG b) => a.Value <= b.Value;

    // --- Equality ---
    public bool Equals(ChanceRPG other) => Math.Abs(Value - other.Value) < 0.0001f;
    public override bool Equals(object? obj) => obj is ChanceRPG other && Equals(other);
    public override int GetHashCode() => Value.GetHashCode();
    public int CompareTo(ChanceRPG other) => Value.CompareTo(other.Value);

    public static bool operator ==(ChanceRPG left, ChanceRPG right) => left.Equals(right);
    public static bool operator !=(ChanceRPG left, ChanceRPG right) => !left.Equals(right);

    public override string ToString() => $"{Value * 100f:F1}%";
}
