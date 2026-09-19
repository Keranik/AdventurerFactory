namespace ForgeFlow.Core.Entities;

/// <summary>
/// Immutable RPG-style color value. Pure .NET — zero Unity references.
/// Stores normalized RGBA (0–1 range). Provides factories for common
/// RPG color palettes, hex parsing, lerp blending, and brightness utilities.
/// </summary>
[Serializable]
public readonly struct ColorRPG : IEquatable<ColorRPG>
{
    public float R { get; }
    public float G { get; }
    public float B { get; }
    public float A { get; }

    public ColorRPG(float r, float g, float b, float a = 1f)
    {
        R = Clamp01(r);
        G = Clamp01(g);
        B = Clamp01(b);
        A = Clamp01(a);
    }

    // --- Named Colors (RPG palette) ---
    public static ColorRPG White => new(1f, 1f, 1f);
    public static ColorRPG Black => new(0f, 0f, 0f);
    public static ColorRPG Clear => new(0f, 0f, 0f, 0f);
    public static ColorRPG Red => new(1f, 0f, 0f);
    public static ColorRPG Green => new(0f, 1f, 0f);
    public static ColorRPG Blue => new(0f, 0f, 1f);
    public static ColorRPG Yellow => new(1f, 1f, 0f);
    public static ColorRPG Cyan => new(0f, 1f, 1f);
    public static ColorRPG Magenta => new(1f, 0f, 1f);

    // RPG rarity colors
    public static ColorRPG Common => new(0.78f, 0.78f, 0.78f);
    public static ColorRPG Uncommon => new(0.12f, 0.80f, 0.12f);
    public static ColorRPG Rare => new(0.25f, 0.50f, 1.00f);
    public static ColorRPG Epic => new(0.64f, 0.21f, 0.93f);
    public static ColorRPG Legendary => new(1.00f, 0.65f, 0.00f);
    public static ColorRPG Mythic => new(1.00f, 0.20f, 0.20f);
    public static ColorRPG Celestial => new(0.90f, 0.85f, 0.55f);

    // Element colors
    public static ColorRPG Fire => new(1.00f, 0.35f, 0.10f);
    public static ColorRPG Ice => new(0.40f, 0.75f, 1.00f);
    public static ColorRPG Lightning => new(1.00f, 0.95f, 0.30f);
    public static ColorRPG Nature => new(0.20f, 0.70f, 0.20f);
    public static ColorRPG Void => new(0.30f, 0.10f, 0.40f);
    public static ColorRPG Holy => new(1.00f, 0.95f, 0.75f);
    public static ColorRPG Arcane => new(0.50f, 0.30f, 0.90f);

    // --- Factories ---

    /// <summary>Creates a color from 0–255 integer components.</summary>
    public static ColorRPG FromBytes(byte r, byte g, byte b, byte a = 255) =>
        new(r / 255f, g / 255f, b / 255f, a / 255f);

    /// <summary>Creates a color from a hex string (supports #RRGGBB and #RRGGBBAA).</summary>
    public static ColorRPG FromHex(string hex)
    {
        if (string.IsNullOrEmpty(hex)) return White;
        hex = hex.TrimStart('#');
        if (hex.Length < 6) return White;

        byte r = Convert.ToByte(hex.Substring(0, 2), 16);
        byte g = Convert.ToByte(hex.Substring(2, 2), 16);
        byte b = Convert.ToByte(hex.Substring(4, 2), 16);
        byte a = hex.Length >= 8 ? Convert.ToByte(hex.Substring(6, 2), 16) : (byte)255;

        return FromBytes(r, g, b, a);
    }

    /// <summary>Creates a color from HSV values (H: 0–360, S: 0–1, V: 0–1).</summary>
    public static ColorRPG FromHSV(float hue, float saturation, float value)
    {
        float h = hue / 60f;
        float c = value * saturation;
        float x = c * (1f - Math.Abs(h % 2f - 1f));
        float m = value - c;

        float r, g, b;
        if (h < 1f) { r = c; g = x; b = 0f; }
        else if (h < 2f) { r = x; g = c; b = 0f; }
        else if (h < 3f) { r = 0f; g = c; b = x; }
        else if (h < 4f) { r = 0f; g = x; b = c; }
        else if (h < 5f) { r = x; g = 0f; b = c; }
        else { r = c; g = 0f; b = x; }

        return new ColorRPG(r + m, g + m, b + m);
    }

    /// <summary>Returns the rarity color for a given tier (1–7).</summary>
    public static ColorRPG ForTier(int tier) => tier switch
    {
        <= 1 => Common,
        2 => Uncommon,
        3 => Rare,
        4 => Epic,
        5 => Legendary,
        6 => Mythic,
        _ => Celestial
    };

    /// <summary>Returns the element color for a given DamageType.</summary>
    public static ColorRPG ForElement(DamageType type) => type switch
    {
        DamageType.Fire => Fire,
        DamageType.Ice => Ice,
        DamageType.Lightning => Lightning,
        DamageType.Nature => Nature,
        DamageType.Void => Void,
        DamageType.Holy => Holy,
        DamageType.Arcane => Arcane,
        _ => White
    };

    // --- Operations ---

    /// <summary>Linear interpolation between two colors.</summary>
    public static ColorRPG Lerp(ColorRPG a, ColorRPG b, float t)
    {
        t = Clamp01(t);
        return new ColorRPG(
            a.R + (b.R - a.R) * t,
            a.G + (b.G - a.G) * t,
            a.B + (b.B - a.B) * t,
            a.A + (b.A - a.A) * t);
    }

    /// <summary>Perceived brightness (ITU-R BT.709 luminance).</summary>
    public float Brightness => 0.2126f * R + 0.7152f * G + 0.0722f * B;

    /// <summary>Returns a brightened copy.</summary>
    public ColorRPG Brighten(float amount) =>
        new(R + amount, G + amount, B + amount, A);

    /// <summary>Returns a darkened copy.</summary>
    public ColorRPG Darken(float amount) =>
        new(R - amount, G - amount, B - amount, A);

    /// <summary>Returns a copy with a different alpha.</summary>
    public ColorRPG WithAlpha(float alpha) => new(R, G, B, alpha);

    /// <summary>Multiplies RGB by a factor (preserves alpha).</summary>
    public ColorRPG Scale(float factor) => new(R * factor, G * factor, B * factor, A);

    /// <summary>Additive blend.</summary>
    public static ColorRPG operator +(ColorRPG a, ColorRPG b) =>
        new(a.R + b.R, a.G + b.G, a.B + b.B, a.A + b.A);

    /// <summary>Subtractive blend.</summary>
    public static ColorRPG operator -(ColorRPG a, ColorRPG b) =>
        new(a.R - b.R, a.G - b.G, a.B - b.B, a.A - b.A);

    /// <summary>Multiply blend (color × color).</summary>
    public static ColorRPG operator *(ColorRPG a, ColorRPG b) =>
        new(a.R * b.R, a.G * b.G, a.B * b.B, a.A * b.A);

    /// <summary>Scalar multiply.</summary>
    public static ColorRPG operator *(ColorRPG c, float s) =>
        new(c.R * s, c.G * s, c.B * s, c.A * s);

    /// <summary>Converts to hex string (#RRGGBBAA).</summary>
    public string ToHex()
    {
        byte r = (byte)(R * 255f);
        byte g = (byte)(G * 255f);
        byte b = (byte)(B * 255f);
        byte a = (byte)(A * 255f);
        return $"#{r:X2}{g:X2}{b:X2}{a:X2}";
    }

    // --- Equality ---
    public bool Equals(ColorRPG other) =>
        Math.Abs(R - other.R) < 0.001f &&
        Math.Abs(G - other.G) < 0.001f &&
        Math.Abs(B - other.B) < 0.001f &&
        Math.Abs(A - other.A) < 0.001f;

    public override bool Equals(object? obj) => obj is ColorRPG other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(R, G, B, A);

    public static bool operator ==(ColorRPG left, ColorRPG right) => left.Equals(right);
    public static bool operator !=(ColorRPG left, ColorRPG right) => !left.Equals(right);

    public override string ToString() => $"RGBA({R:F2}, {G:F2}, {B:F2}, {A:F2})";

    private static float Clamp01(float v) => v < 0f ? 0f : v > 1f ? 1f : v;
}
