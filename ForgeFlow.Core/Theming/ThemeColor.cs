using System;
using ForgeFlow.Core.Entities;

namespace ForgeFlow.Core.Theming;

/// <summary>
/// Named color entry in a theme. Wraps <see cref="ColorRPG"/> with a semantic key.
/// Pure .NET — zero Unity references.
/// </summary>
[Serializable]
public readonly struct ThemeColor : IEquatable<ThemeColor>
{
    public string Key { get; }
    public ColorRPG Color { get; }

    public ThemeColor(string key, ColorRPG color)
    {
        Key = key ?? throw new ArgumentNullException(nameof(key));
        Color = color;
    }

    public ThemeColor(string key, string hex)
    {
        Key = key ?? throw new ArgumentNullException(nameof(key));
        Color = ColorRPG.FromHex(hex);
    }

    public bool Equals(ThemeColor other) =>
        Key == other.Key && Color.Equals(other.Color);

    public override bool Equals(object? obj) =>
        obj is ThemeColor other && Equals(other);

    public override int GetHashCode() =>
        HashCode.Combine(Key, Color);

    public override string ToString() => $"{Key}: {Color}";

    public static bool operator ==(ThemeColor left, ThemeColor right) => left.Equals(right);
    public static bool operator !=(ThemeColor left, ThemeColor right) => !left.Equals(right);
}
