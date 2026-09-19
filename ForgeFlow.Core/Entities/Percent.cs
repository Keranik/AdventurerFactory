namespace ForgeFlow.Core.Entities;

/// <summary>
/// An immutable percentage/multiplier value stored internally as a fraction (≥0.0).
/// Perfect for RPG damage multipliers, buff stacks, stat bonuses, and economy scaling.
/// Pure .NET — zero Unity references.
/// </summary>
/// <remarks>
/// <para>Values are clamped to ≥0.0 (no negative percentages). No upper limit — 300% is valid!</para>
/// <para>Internal storage is a multiplier: 75% = 0.75f, 300% = 3.0f</para>
/// <para>Use <see cref="Clamped"/> when you need normalized 0-100% (health bars, progress, etc.)</para>
/// </remarks>
[Serializable]
public readonly struct Percent : IEquatable<Percent>, IComparable<Percent>, IFormattable
{
    #region Fields

    /// <summary>
    /// Internal storage: multiplier/fraction, always ≥0.0f.
    /// 75% = 0.75f, 300% = 3.0f
    /// </summary>
    private readonly float _value;

    private const float EPSILON = 1e-6f;

    #endregion

    #region Constants

    /// <summary>0% (0.0 multiplier)</summary>
    public static readonly Percent Zero = new(0f);

    /// <summary>100% (1.0 multiplier)</summary>
    public static readonly Percent One = new(1f);

    /// <summary>100% (1.0 multiplier) — alias for One.</summary>
    public static readonly Percent Hundred = new(1f);

    /// <summary>100% (1.0 multiplier) — alias for One.</summary>
    public static readonly Percent Full = new(1f);

    /// <summary>50% (0.5 multiplier)</summary>
    public static readonly Percent Half = new(0.5f);

    /// <summary>200% (2.0 multiplier)</summary>
    public static readonly Percent Double = new(2f);

    /// <summary>300% (3.0 multiplier)</summary>
    public static readonly Percent Triple = new(3f);

    /// <summary>10% (0.1 multiplier)</summary>
    public static readonly Percent Ten = new(0.1f);

    /// <summary>25% (0.25 multiplier)</summary>
    public static readonly Percent TwentyFive = new(0.25f);

    /// <summary>50% (0.5 multiplier) — alias for Half.</summary>
    public static readonly Percent Fifty = new(0.5f);

    /// <summary>75% (0.75 multiplier)</summary>
    public static readonly Percent SeventyFive = new(0.75f);

    /// <summary>90% (0.9 multiplier)</summary>
    public static readonly Percent Ninety = new(0.9f);

    #endregion

    #region Constructors

    /// <summary>
    /// Creates a Percent from a multiplier/fraction value (≥0.0).
    /// Values below 0 are clamped to 0.
    /// </summary>
    private Percent(float fraction)
    {
        _value = ClampMin(fraction);
    }

    #endregion

    #region Properties

    /// <summary>
    /// Gets the percentage as a fraction/multiplier (≥0.0).
    /// </summary>
    public float Fraction => _value;

    /// <summary>
    /// Gets the percentage as a display value (0 to ∞).
    /// </summary>
    public float Value => _value * 100f;

    /// <summary>
    /// Gets whether this represents 0% (no effect).
    /// </summary>
    public bool IsZero => _value <= EPSILON;

    /// <summary>
    /// Gets whether this represents exactly 100% (1.0 multiplier).
    /// </summary>
    public bool IsOne => Math.Abs(_value - 1f) <= EPSILON;

    /// <summary>
    /// Gets whether this represents exactly 100% (1.0 multiplier).
    /// Alias for IsOne.
    /// </summary>
    public bool IsFull => IsOne;

    /// <summary>
    /// Gets the inverse of this percentage (1.0 / fraction).
    /// Returns Zero if this is Zero to avoid division by zero.
    /// </summary>
    public Percent Inverse => IsZero ? Zero : new(1f / _value);

    /// <summary>
    /// Gets the complement (1.0 - fraction), clamped to ≥0.
    /// Useful for "remaining" calculations.
    /// </summary>
    public Percent Complement => new(1f - _value);

    #endregion

    #region Factory Methods

    /// <summary>
    /// Creates a Percent from an integer percentage value (0-∞).
    /// </summary>
    public static Percent FromPercent(int value) => new(value / 100f);

    /// <summary>
    /// Creates a Percent from a float percentage value (0-∞).
    /// </summary>
    public static Percent FromPercent(float value) => new(value / 100f);

    /// <summary>
    /// Creates a Percent from a fraction/multiplier value (0.0-∞).
    /// </summary>
    public static Percent FromFraction(float fraction) => new(fraction);

    /// <summary>
    /// Creates a Percent from an integer percentage value (0-∞).
    /// </summary>
    public static Percent FromInt(int percent) => new(percent / 100f);

    /// <summary>
    /// Creates a Percent from a float value treated as a percentage (0-∞).
    /// </summary>
    public static Percent FromFloatPercent(float percent) => new(percent / 100f);

    #endregion

    #region Clamping

    /// <summary>
    /// Returns this percentage clamped to [0.0, 1.0] (0% to 100%).
    /// </summary>
    public Percent Clamped() => new(Math.Clamp(_value, 0f, 1f));

    /// <summary>Returns this percentage clamped to a maximum value.</summary>
    public Percent ClampedMax(Percent max) => _value > max._value ? max : this;

    /// <summary>Returns this percentage clamped to a minimum value.</summary>
    public Percent ClampedMin(Percent min) => _value < min._value ? min : this;

    /// <summary>Returns this percentage clamped between min and max.</summary>
    public Percent ClampedRange(Percent min, Percent max) => new(Math.Clamp(_value, min._value, max._value));

    #endregion

    #region Implicit Conversions

    /// <summary>
    /// Implicitly converts an integer to a Percent (treated as 0-∞ percentage).
    /// </summary>
    public static implicit operator Percent(int value) => FromPercent(value);

    /// <summary>
    /// Implicitly converts a float to a Percent.
    /// Values ≤ 1.5 are treated as fractions; values > 1.5 are treated as percentages.
    /// </summary>
    public static implicit operator Percent(float value)
    {
        return value > 1.5f ? FromPercent(value) : FromFraction(value);
    }

    /// <summary>
    /// Implicitly converts a Percent to a float (returns the fraction/multiplier).
    /// </summary>
    public static implicit operator float(Percent p) => p._value;

    #endregion

    #region Arithmetic Operators

    public static Percent operator +(Percent a, Percent b) => new(a._value + b._value);
    public static Percent operator +(Percent a, float b) => new(a._value + b);
    public static Percent operator +(float a, Percent b) => new(a + b._value);
    public static Percent operator +(Percent a, int b) => new(a._value + b / 100f);
    public static Percent operator +(int a, Percent b) => new(a / 100f + b._value);

    public static Percent operator -(Percent a, Percent b) => new(a._value - b._value);
    public static Percent operator -(Percent a, float b) => new(a._value - b);
    public static Percent operator -(float a, Percent b) => new(a - b._value);
    public static Percent operator -(Percent a, int b) => new(a._value - b / 100f);
    public static Percent operator -(int a, Percent b) => new(a / 100f - b._value);

    public static Percent operator *(Percent a, Percent b) => new(a._value * b._value);
    public static Percent operator *(Percent a, float b) => new(a._value * b);
    public static Percent operator *(float a, Percent b) => new(a * b._value);
    public static Percent operator *(Percent a, int b) => new(a._value * b);
    public static Percent operator *(int a, Percent b) => new(a * b._value);

    public static Percent operator /(Percent a, Percent b) =>
        b.IsZero ? Zero : new(a._value / b._value);

    public static Percent operator /(Percent a, float b) =>
        Math.Abs(b) < EPSILON ? Zero : new(a._value / b);

    public static Percent operator /(float a, Percent b) =>
        b.IsZero ? Zero : new(a / b._value);

    public static Percent operator /(Percent a, int b) =>
        b == 0 ? Zero : new(a._value / b);

    public static Percent operator /(int a, Percent b) =>
        b.IsZero ? Zero : new(a / b._value);

    public static Percent operator +(Percent p) => p;

    public static Percent operator -(Percent p) => Zero;

    #endregion

    #region Comparison Operators

    public static bool operator ==(Percent a, Percent b) => Math.Abs(a._value - b._value) < EPSILON;
    public static bool operator !=(Percent a, Percent b) => !(a == b);
    public static bool operator <(Percent a, Percent b) => a._value < b._value - EPSILON;
    public static bool operator >(Percent a, Percent b) => a._value > b._value + EPSILON;
    public static bool operator <=(Percent a, Percent b) => a._value <= b._value + EPSILON;
    public static bool operator >=(Percent a, Percent b) => a._value >= b._value - EPSILON;

    #endregion

    #region Utility Methods

    /// <summary>Linearly interpolates between two percentages.</summary>
    public static Percent Lerp(Percent a, Percent b, Percent t)
    {
        float tClamped = Math.Clamp(t._value, 0f, 1f);
        return new Percent(a._value + (b._value - a._value) * tClamped);
    }

    /// <summary>Linearly interpolates between two percentages.</summary>
    public static Percent Lerp(Percent a, Percent b, float t)
    {
        float tClamped = Math.Clamp(t, 0f, 1f);
        return new Percent(a._value + (b._value - a._value) * tClamped);
    }

    /// <summary>Returns the larger of two percentages.</summary>
    public static Percent Max(Percent a, Percent b) => a._value >= b._value ? a : b;

    /// <summary>Returns the smaller of two percentages.</summary>
    public static Percent Min(Percent a, Percent b) => a._value <= b._value ? a : b;

    /// <summary>Applies this percentage as a multiplier to a value.</summary>
    public float Of(float value) => value * _value;

    /// <summary>Applies this percentage as a multiplier to an integer value.</summary>
    public float Of(int value) => value * _value;

    /// <summary>
    /// Returns the value after applying this as a reduction (value × (1 - fraction)).
    /// Reduction is clamped so result is never negative.
    /// </summary>
    public float Reduce(float value) => value * Math.Max(0f, 1f - _value);

    /// <summary>Returns the value after applying this as a reduction.</summary>
    public float Reduce(int value) => value * Math.Max(0f, 1f - _value);

    /// <summary>
    /// Returns the bonus amount when applying this percentage.
    /// Same as Of(), but semantically clearer for additive bonuses.
    /// </summary>
    public float Bonus(float value) => value * _value;

    /// <summary>Returns the bonus amount for an integer value.</summary>
    public float Bonus(int value) => value * _value;

    /// <summary>
    /// Returns the total after applying this as an additive bonus (value × (1 + fraction)).
    /// </summary>
    public float AddTo(float value) => value * (1f + _value);

    /// <summary>Returns the total after applying this as an additive bonus.</summary>
    public float AddTo(int value) => value * (1f + _value);

    #endregion

    #region Equality & Comparison

    public bool Equals(Percent other) => this == other;
    public override bool Equals(object? obj) => obj is Percent other && Equals(other);
    public override int GetHashCode() => _value.GetHashCode();
    public int CompareTo(Percent other) => _value.CompareTo(other._value);

    #endregion

    #region Formatting

    public override string ToString() => $"{Value:0.#}%";

    /// <summary>
    /// Returns a string representation with the specified number of decimal places.
    /// </summary>
    public string ToString(int decimals)
    {
        if (decimals <= 0)
        {
            return $"{Value:0}%";
        }

        string format = "0." + new string('0', decimals);
        return $"{Value.ToString(format)}%";
    }

    /// <summary>
    /// Formats the percentage according to the specified format string.
    /// P = percent display (with % sign). F = fraction/multiplier. V = value (0-∞).
    /// </summary>
    public string ToString(string? format, IFormatProvider? formatProvider)
    {
        if (string.IsNullOrEmpty(format))
        {
            return ToString();
        }

        char specifier = char.ToUpperInvariant(format[0]);
        string precision = format.Length > 1 ? format.Substring(1) : "";

        return specifier switch
        {
            'P' => Value.ToString($"F{precision}", formatProvider) + "%",
            'F' => _value.ToString($"F{precision}", formatProvider),
            'V' => Value.ToString($"F{precision}", formatProvider),
            _ => Value.ToString(format, formatProvider) + "%"
        };
    }

    #endregion

    #region Private Helpers

    private static float ClampMin(float value) => Math.Max(0f, value);

    #endregion
}

/// <summary>
/// Extension methods for creating Percent values from numeric types.
/// </summary>
public static class PercentExtensions
{
    /// <summary>Converts an integer to a Percent (treated as 0-∞ percentage).</summary>
    public static Percent Percent(this int value) => Entities.Percent.FromPercent(value);

    /// <summary>Converts a float to a Percent (treated as percentage value).</summary>
    public static Percent Percent(this float value) => Entities.Percent.FromPercent(value);

    /// <summary>Converts a float fraction/multiplier to a Percent.</summary>
    public static Percent AsFractionPercent(this float value) => Entities.Percent.FromFraction(value);

    /// <summary>Converts an integer to a Percent, ensuring non-negative.</summary>
    public static Percent ClampPercent(this int value) => Entities.Percent.FromPercent(Math.Max(0, value));

    /// <summary>Converts a float to a Percent, ensuring non-negative.</summary>
    public static Percent ClampPercent(this float value) => Entities.Percent.FromPercent(Math.Max(0f, value));

    /// <summary>Converts a float treated explicitly as a percentage (0-∞) to a Percent.</summary>
    public static Percent AsPercent(this float value) => Entities.Percent.FromFloatPercent(value);

    /// <summary>Converts a float treated explicitly as a fraction/multiplier to a Percent.</summary>
    public static Percent AsFraction(this float value) => Entities.Percent.FromFraction(value);
}
