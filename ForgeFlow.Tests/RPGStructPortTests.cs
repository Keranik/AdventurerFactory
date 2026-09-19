using ForgeFlow.Core;
using ForgeFlow.Core.Data;
using ForgeFlow.Core.Data.Definitions;
using ForgeFlow.Core.Entities;
using ForgeFlow.Core.Events;
using ForgeFlow.Core.Proto;
using ForgeFlow.Core.Proto.Logic;
using ForgeFlow.Core.Proto.Prototypes;
using ForgeFlow.Core.Systems;
using ForgeFlow.Core.Utilities;

namespace ForgeFlow.Tests;

/// <summary>
/// Tests for Percent, TilesRPG, AngleRPG, and StringExtensions.
/// </summary>
public class PercentTests
{
    [Fact]
    public void Zero_HasZeroFraction()
    {
        Assert.True(Percent.Zero.IsZero);
        Assert.Equal(0f, Percent.Zero.Fraction, 5);
    }

    [Fact]
    public void One_HasUnitFraction()
    {
        Assert.True(Percent.One.IsOne);
        Assert.Equal(1f, Percent.One.Fraction, 5);
        Assert.Equal(100f, Percent.One.Value, 3);
    }

    [Fact]
    public void ImplicitFromInt_TreatsAsPercentage()
    {
        Percent p = 75;
        Assert.Equal(0.75f, p.Fraction, 5);
        Assert.Equal(75f, p.Value, 3);
    }

    [Fact]
    public void ImplicitFromFloat_SmallAsFraction()
    {
        Percent p = 0.5f;
        Assert.Equal(0.5f, p.Fraction, 5);
        Assert.Equal(50f, p.Value, 3);
    }

    [Fact]
    public void ImplicitFromFloat_LargeAsPercentage()
    {
        Percent p = 200f;
        Assert.Equal(2.0f, p.Fraction, 5);
        Assert.Equal(200f, p.Value, 3);
    }

    [Fact]
    public void FactoryMethods_ProduceCorrectValues()
    {
        Assert.Equal(0.75f, Percent.FromPercent(75).Fraction, 5);
        Assert.Equal(0.75f, Percent.FromPercent(75f).Fraction, 5);
        Assert.Equal(0.75f, Percent.FromFraction(0.75f).Fraction, 5);
        Assert.Equal(3.0f, Percent.FromInt(300).Fraction, 5);
        Assert.Equal(0.5f, Percent.FromFloatPercent(50f).Fraction, 5);
    }

    [Fact]
    public void Clamped_ReturnsNormalized()
    {
        Percent over = Percent.FromPercent(150);
        Assert.Equal(1f, over.Clamped().Fraction, 5);

        Percent under = Percent.Zero;
        Assert.Equal(0f, under.Clamped().Fraction, 5);
    }

    [Fact]
    public void NegativeValue_ClampedToZero()
    {
        Percent p = Percent.FromFraction(-0.5f);
        Assert.True(p.IsZero);
    }

    [Fact]
    public void Arithmetic_AddSubMulDiv()
    {
        Percent a = Percent.FromPercent(50);
        Percent b = Percent.FromPercent(25);

        Assert.Equal(0.75f, (a + b).Fraction, 5);
        Assert.Equal(0.25f, (a - b).Fraction, 5);
        Assert.Equal(0.125f, (a * b).Fraction, 4);
        Assert.Equal(2.0f, (a / b).Fraction, 5);
    }

    [Fact]
    public void DivisionByZero_ReturnsZero()
    {
        Percent a = Percent.FromPercent(50);
        Assert.True((a / Percent.Zero).IsZero);
        Assert.True((a / 0f).IsZero);
        Assert.True((a / 0).IsZero);
    }

    [Fact]
    public void Comparison_Works()
    {
        Percent a = 50;
        Percent b = 75;
        Assert.True(a < b);
        Assert.True(b > a);
        Assert.True(a <= b);
        Assert.True(b >= a);
        Assert.True(a == (Percent)50);
        Assert.True(a != b);
    }

    [Fact]
    public void Inverse_CorrectlyComputed()
    {
        Percent half = Percent.Half;
        Assert.Equal(2.0f, half.Inverse.Fraction, 5);
        Assert.True(Percent.Zero.Inverse.IsZero);
    }

    [Fact]
    public void Complement_CorrectlyComputed()
    {
        Percent p = Percent.FromPercent(75);
        Assert.Equal(0.25f, p.Complement.Fraction, 5);
    }

    [Fact]
    public void Of_AppliesMultiplier()
    {
        Percent p = Percent.FromPercent(150);
        Assert.Equal(150f, p.Of(100f), 3);
        Assert.Equal(150f, p.Of(100), 3);
    }

    [Fact]
    public void Reduce_AppliesReduction()
    {
        Percent p = Percent.FromPercent(25);
        Assert.Equal(75f, p.Reduce(100f), 3);
    }

    [Fact]
    public void AddTo_AppliesBonus()
    {
        Percent p = Percent.FromPercent(50);
        Assert.Equal(150f, p.AddTo(100f), 3);
    }

    [Fact]
    public void Lerp_InterpolatesCorrectly()
    {
        var result = Percent.Lerp(Percent.Zero, Percent.One, 0.5f);
        Assert.Equal(0.5f, result.Fraction, 5);
    }

    [Fact]
    public void ToString_FormatsCorrectly()
    {
        Percent p = 75;
        Assert.Equal("75%", p.ToString());
        Assert.Equal("75.00%", p.ToString(2));
    }

    [Fact]
    public void FormatString_P_F_V()
    {
        Percent p = 75;
        Assert.Contains("75", p.ToString("P0", null));
        Assert.Contains("%", p.ToString("P0", null));
        Assert.Equal("0.75", p.ToString("F2", null));
    }

    [Fact]
    public void ExtensionMethods_Work()
    {
        Percent p1 = 50.Percent();
        Assert.Equal(0.5f, p1.Fraction, 5);

        Percent p2 = 0.75f.AsFractionPercent();
        Assert.Equal(0.75f, p2.Fraction, 5);

        Percent p3 = 75f.AsPercent();
        Assert.Equal(0.75f, p3.Fraction, 5);

        Percent p4 = 0.3f.AsFraction();
        Assert.Equal(0.3f, p4.Fraction, 5);
    }
}

public class TilesRpgTests
{
    [Fact]
    public void Zero_IsZero()
    {
        Assert.True(TilesRPG.Zero.IsZero);
        Assert.Equal(0f, TilesRPG.Zero.Value, 5);
    }

    [Fact]
    public void Presets_HaveCorrectValues()
    {
        Assert.Equal(0.5f, TilesRPG.Half.Value, 5);
        Assert.Equal(1f, TilesRPG.One.Value, 5);
        Assert.Equal(2f, TilesRPG.Two.Value, 5);
        Assert.Equal(3f, TilesRPG.Three.Value, 5);
        Assert.Equal(4f, TilesRPG.Four.Value, 5);
        Assert.True(TilesRPG.Diagonal.Value > 1.41f);
        Assert.True(TilesRPG.Diagonal.Value < 1.42f);
    }

    [Fact]
    public void NegativeValue_ClampedToZero()
    {
        var d = new TilesRPG(-5f);
        Assert.True(d.IsZero);
    }

    [Fact]
    public void ImplicitConversions_Work()
    {
        TilesRPG fromFloat = 3.5f;
        Assert.Equal(3.5f, fromFloat.Value, 5);

        TilesRPG fromInt = 4;
        Assert.Equal(4f, fromInt.Value, 5);

        float asFloat = TilesRPG.Two;
        Assert.Equal(2f, asFloat, 5);
    }

    [Fact]
    public void Arithmetic_Works()
    {
        TilesRPG a = TilesRPG.Two;
        TilesRPG b = TilesRPG.Three;

        Assert.Equal(5f, (a + b).Value, 5);
        Assert.Equal(1f, (b - a).Value, 5);
        Assert.Equal(6f, (a * 3f).Value, 5);
        Assert.Equal(1.5f, (b / 2f).Value, 5);
    }

    [Fact]
    public void DivisionByZero_ReturnsZero()
    {
        var d = TilesRPG.One / 0f;
        Assert.True(d.IsZero);
    }

    [Fact]
    public void Comparison_Works()
    {
        Assert.True(TilesRPG.One < TilesRPG.Two);
        Assert.True(TilesRPG.Two > TilesRPG.One);
        Assert.True(TilesRPG.One == TilesRPG.One);
        Assert.True(TilesRPG.One != TilesRPG.Two);
    }

    [Fact]
    public void RoundingMethods_Work()
    {
        var d = new TilesRPG(2.7f);
        Assert.Equal(3f, d.Ceil().Value, 5);
        Assert.Equal(2f, d.Floor().Value, 5);
        Assert.Equal(3f, d.Round().Value, 5);
        Assert.Equal(3, d.ToInt());
    }

    [Fact]
    public void Clamping_Works()
    {
        var d = new TilesRPG(5f);
        Assert.Equal(3f, d.ClampedMax(TilesRPG.Three).Value, 5);
        Assert.Equal(5f, d.ClampedMin(TilesRPG.Three).Value, 5);
        Assert.Equal(3f, d.Clamped(TilesRPG.One, TilesRPG.Three).Value, 5);
    }

    [Fact]
    public void Distance_BetweenGridPositions()
    {
        var from = new GridPosRPG(0, 0);
        var to = new GridPosRPG(3, 4);
        var dist = TilesRPG.Distance(from, to);
        Assert.Equal(5f, dist.Value, 3);
    }

    [Fact]
    public void Lerp_Works()
    {
        var result = TilesRPG.Lerp(TilesRPG.Zero, TilesRPG.Four, 0.5f);
        Assert.Equal(2f, result.Value, 5);
    }

    [Fact]
    public void ToString_FormatsCorrectly()
    {
        var d = new TilesRPG(3.5f);
        Assert.Equal("3.5 tiles", d.ToString());
        Assert.Equal("3.5t", d.ToString("t"));
    }

    [Fact]
    public void ExtensionMethods_Work()
    {
        TilesRPG d1 = 3.Tiles();
        Assert.Equal(3f, d1.Value, 5);

        TilesRPG d2 = 2.5f.Tiles();
        Assert.Equal(2.5f, d2.Value, 5);
    }
}

public class AngleRPGTests
{
    [Fact]
    public void Zero_IsZero()
    {
        Assert.True(AngleRPG.Zero.IsZero);
    }

    [Fact]
    public void WrapsCorrectly()
    {
        var a = new AngleRPG(370f);
        Assert.Equal(10f, a.Degrees, 3);

        var b = new AngleRPG(-10f);
        Assert.Equal(350f, b.Degrees, 3);
    }

    [Fact]
    public void CardinalPresets_CorrectDegrees()
    {
        Assert.Equal(0f, AngleRPG.East.Degrees, 3);
        Assert.Equal(90f, AngleRPG.North.Degrees, 3);
        Assert.Equal(180f, AngleRPG.West.Degrees, 3);
        Assert.Equal(270f, AngleRPG.South.Degrees, 3);
    }

    [Fact]
    public void FromDegrees_Works()
    {
        var a = AngleRPG.FromDegrees(45f);
        Assert.Equal(45f, a.Degrees, 3);
    }

    [Fact]
    public void FromRadians_Works()
    {
        var a = AngleRPG.FromRadians((float)Math.PI);
        Assert.Equal(180f, a.Degrees, 2);
    }

    [Fact]
    public void FromDirection_Works()
    {
        var right = AngleRPG.FromDirection(new Vector2RPG(1f, 0f));
        Assert.Equal(0f, right.Degrees, 2);

        var up = AngleRPG.FromDirection(new Vector2RPG(0f, 1f));
        Assert.Equal(90f, up.Degrees, 2);
    }

    [Fact]
    public void ToDirection2D_ReturnsUnitVector()
    {
        var dir = AngleRPG.East.ToDirection2D();
        Assert.Equal(1f, dir.X, 3);
        Assert.Equal(0f, dir.Y, 3);

        var dir2 = AngleRPG.North.ToDirection2D();
        Assert.Equal(0f, dir2.X, 3);
        Assert.Equal(1f, dir2.Y, 3);
    }

    [Fact]
    public void Rotate_Works()
    {
        var a = AngleRPG.East.Rotate(90f);
        Assert.Equal(90f, a.Degrees, 3);
    }

    [Fact]
    public void Clockwise_CounterClockwise()
    {
        var a = AngleRPG.North;
        Assert.Equal(45f, a.Clockwise(45f).Degrees, 3);
        Assert.Equal(135f, a.CounterClockwise(45f).Degrees, 3);
    }

    [Fact]
    public void Opposite_Adds180()
    {
        Assert.Equal(180f, AngleRPG.East.Opposite.Degrees, 3);
        Assert.Equal(0f, AngleRPG.West.Opposite.Degrees, 3);
    }

    [Fact]
    public void ShortestTurn_Works()
    {
        float turn = AngleRPG.East.ShortestTurn(AngleRPG.North);
        Assert.Equal(90f, turn, 3);

        float turn2 = AngleRPG.North.ShortestTurn(AngleRPG.East);
        Assert.Equal(-90f, turn2, 3);
    }

    [Fact]
    public void Towards_ClampsTurn()
    {
        var result = AngleRPG.East.Towards(AngleRPG.West, 45f);
        Assert.Equal(45f, result.Degrees, 3);
    }

    [Fact]
    public void Lerp_Works()
    {
        var result = AngleRPG.Lerp(AngleRPG.East, AngleRPG.North, 0.5f);
        Assert.Equal(45f, result.Degrees, 2);
    }

    [Fact]
    public void Comparison_Works()
    {
        Assert.True(AngleRPG.East == AngleRPG.East);
        Assert.True(AngleRPG.East != AngleRPG.North);
        Assert.True(AngleRPG.East < AngleRPG.North);
    }

    [Fact]
    public void Arithmetic_Works()
    {
        var sum = AngleRPG.East + AngleRPG.North;
        Assert.Equal(90f, sum.Degrees, 3);

        var diff = AngleRPG.North - AngleRPG.FromDegrees(45f);
        Assert.Equal(45f, diff.Degrees, 3);
    }

    [Fact]
    public void ImplicitConversions_Work()
    {
        AngleRPG fromFloat = 90f;
        Assert.Equal(90f, fromFloat.Degrees, 3);

        AngleRPG fromInt = 180;
        Assert.Equal(180f, fromInt.Degrees, 3);

        float asFloat = AngleRPG.North;
        Assert.Equal(90f, asFloat, 3);
    }

    [Fact]
    public void SignedDegrees_Works()
    {
        Assert.Equal(0f, AngleRPG.East.SignedDegrees, 3);
        Assert.Equal(90f, AngleRPG.North.SignedDegrees, 3);
        Assert.Equal(-90f, AngleRPG.South.SignedDegrees, 3);
        Assert.Equal(-180f, AngleRPG.West.SignedDegrees, 3);
    }

    [Fact]
    public void ToDirectionName_Works()
    {
        Assert.Equal("East", AngleRPG.East.ToDirectionName());
        Assert.Equal("North", AngleRPG.North.ToDirectionName());
        Assert.Equal("West", AngleRPG.West.ToDirectionName());
        Assert.Equal("South", AngleRPG.South.ToDirectionName());
    }

    [Fact]
    public void ToString_FormatsCorrectly()
    {
        Assert.Equal("90°", AngleRPG.North.ToString());
        Assert.Equal("North", AngleRPG.North.ToString("dir"));
    }

    [Fact]
    public void ToQuaternion_DoesNotThrow()
    {
        var q = AngleRPG.North.ToQuaternion();
        var qy = AngleRPG.North.ToQuaternionY();
        Assert.NotEqual(default(QuaternionRPG), q);
        Assert.NotEqual(default(QuaternionRPG), qy);
    }

    [Fact]
    public void ExtensionMethods_Work()
    {
        AngleRPG a1 = 45.Degrees();
        Assert.Equal(45f, a1.Degrees, 3);

        AngleRPG a2 = 90f.Degrees();
        Assert.Equal(90f, a2.Degrees, 3);

        AngleRPG a3 = ((float)Math.PI).ToAngleFromRadians();
        Assert.Equal(180f, a3.Degrees, 2);

        AngleRPG a4 = new Vector2RPG(0f, 1f).ToAngle();
        Assert.Equal(90f, a4.Degrees, 2);
    }
}

public class StringExtensionsTests
{
    [Theory]
    [InlineData(null, true)]
    [InlineData("", true)]
    [InlineData("hello", false)]
    public void IsNullOrEmpty_Works(string? input, bool expected)
    {
        Assert.Equal(expected, input.IsNullOrEmpty());
    }

    [Theory]
    [InlineData("Evocation", 3, "Evo")]
    [InlineData("Hi", 5, "Hi")]
    [InlineData(null, 3, "")]
    public void First_Works(string? input, int len, string expected)
    {
        Assert.Equal(expected, input.First(len));
    }

    [Theory]
    [InlineData("Very Long Name Here", 12, "Very Long...")]
    [InlineData("Short", 10, "Short")]
    [InlineData(null, 10, "")]
    public void Truncate_Works(string? input, int max, string expected)
    {
        Assert.Equal(expected, input.Truncate(max));
    }

    [Theory]
    [InlineData("deep forest", "Deep Forest")]
    [InlineData(null, "")]
    public void ToTitleCase_Works(string? input, string expected)
    {
        Assert.Equal(expected, input.ToTitleCase());
    }

    [Theory]
    [InlineData("HELLO WORLD", "Hello world")]
    [InlineData(null, "")]
    public void ToSentenceCase_Works(string? input, string expected)
    {
        Assert.Equal(expected, input.ToSentenceCase());
    }

    [Theory]
    [InlineData("helloWorld", "hello_world")]
    [InlineData("DeepForest", "deep_forest")]
    public void ToSnakeCase_Works(string input, string expected)
    {
        Assert.Equal(expected, input.ToSnakeCase());
    }

    [Theory]
    [InlineData("hello_world", "hello-world")]
    public void ToKebabCase_Works(string input, string expected)
    {
        Assert.Equal(expected, input.ToKebabCase());
    }

    [Theory]
    [InlineData("Deep Forest", "DF")]
    [InlineData("Health Potion", "HP")]
    [InlineData(null, "?")]
    public void ToInitials_Works(string? input, string expected)
    {
        Assert.Equal(expected, input.ToInitials());
    }

    [Theory]
    [InlineData("sword", "swords")]
    [InlineData("gold", "gold")]
    [InlineData("berry", "berries")]
    public void Pluralize_Works(string input, string expected)
    {
        Assert.Equal(expected, input.Pluralize());
    }

    [Theory]
    [InlineData("Hello World", "world", true)]
    [InlineData("Hello World", "WORLD", true)]
    [InlineData("Hello World", "xyz", false)]
    public void ContainsIgnoreCase_Works(string source, string value, bool expected)
    {
        Assert.Equal(expected, source.ContainsIgnoreCase(value));
    }

    [Fact]
    public void WithColor_WrapsInTag()
    {
        string result = "Critical!".WithColor(new ColorRPG(1f, 0f, 0f));
        Assert.StartsWith("<color=#", result);
        Assert.EndsWith("</color>", result);
        Assert.Contains("Critical!", result);
    }

    [Fact]
    public void Bold_Italic_Underline()
    {
        Assert.Equal("<b>test</b>", "test".Bold());
        Assert.Equal("<i>test</i>", "test".Italic());
        Assert.Equal("<u>test</u>", "test".Underline());
        Assert.Equal("<s>test</s>", "test".Strikethrough());
    }

    [Fact]
    public void StripRichText_Works()
    {
        string input = "<b>Bold</b> <color=#FF0000>Red</color>";
        string result = input.StripRichText();
        Assert.Equal("Bold Red", result);
    }

    [Theory]
    [InlineData("Terrain_Forest_DeepForest", "Deep Forest")]
    [InlineData("Item_Weapon_LegendarySword", "Legendary Sword")]
    public void ToDisplayName_Works(string input, string expected)
    {
        Assert.Equal(expected, input.ToDisplayName());
    }

    [Theory]
    [InlineData(1, "I")]
    [InlineData(4, "IV")]
    [InlineData(9, "IX")]
    [InlineData(42, "XLII")]
    [InlineData(3999, "MMMCMXCIX")]
    public void ToRomanNumeral_Works(int input, string expected)
    {
        Assert.Equal(expected, input.ToRomanNumeral());
    }

    [Theory]
    [InlineData(1, "1st")]
    [InlineData(2, "2nd")]
    [InlineData(3, "3rd")]
    [InlineData(4, "4th")]
    [InlineData(11, "11th")]
    [InlineData(21, "21st")]
    public void ToOrdinal_Works(int input, string expected)
    {
        Assert.Equal(expected, input.ToOrdinal());
    }

    [Fact]
    public void WrapInQuotes_Works()
    {
        Assert.Equal("\"hello\"", "hello".WrapInQuotes());
    }

    [Fact]
    public void WrapInBrackets_Works()
    {
        Assert.Equal("[hello]", "hello".WrapInBrackets());
    }

    [Fact]
    public void AsHotkeyLabel_Works()
    {
        Assert.Equal("[Q]", "Q".AsHotkeyLabel());
        Assert.Equal("[LMB]", "LeftMouse".AsHotkeyLabel());
    }

    [Fact]
    public void ToCamelCase_Works()
    {
        Assert.Equal("deepForest", "deep_forest".ToCamelCase());
    }

    [Fact]
    public void ToPascalCase_Works()
    {
        Assert.Equal("DeepForest", "deep_forest".ToPascalCase());
    }

    [Fact]
    public void EqualsIgnoreCase_Works()
    {
        Assert.True("Hello".EqualsIgnoreCase("hello"));
        Assert.False("Hello".EqualsIgnoreCase("world"));
    }

    [Fact]
    public void ReplaceIgnoreCase_Works()
    {
        Assert.Equal("Hello Universe", "Hello World".ReplaceIgnoreCase("world", "Universe"));
    }

    [Fact]
    public void NullSafety_AllMethodsHandleNull()
    {
        string? s = null;
        Assert.Equal(string.Empty, s.First(5));
        Assert.Equal(string.Empty, s.Truncate(5));
        Assert.Equal(string.Empty, s.TruncateMiddle(5));
        Assert.Equal(string.Empty, s.ToTitleCase());
        Assert.Equal(string.Empty, s.ToSentenceCase());
        Assert.Equal(string.Empty, s.CapitalizeFirst());
        Assert.Equal(string.Empty, s.RemoveWhitespace());
        Assert.Equal(string.Empty, s.ToCamelCase());
        Assert.Equal(string.Empty, s.ToPascalCase());
        Assert.Equal(string.Empty, s.ToSnakeCase());
        Assert.Equal(string.Empty, s.ToKebabCase());
        Assert.Equal(string.Empty, s.ToDisplayName());
        Assert.Equal(string.Empty, s.ToIdFormat());
        Assert.Equal(string.Empty, s.WithColor(ColorRPG.White));
        Assert.Equal(string.Empty, s.Bold());
        Assert.Equal(string.Empty, s.StripRichText());
        Assert.Equal("?", s.ToInitials());
        Assert.Equal(string.Empty, s.Pluralize());
        Assert.Equal(string.Empty, s.Singularize());
    }
}

/// <summary>Tests for the immutable GameId entity ID system.</summary>
public class GameIdTests
{
    public GameIdTests() => GameId.ResetCounter();

    [Fact]
    public void GameId_None_IsInvalid()
    {
        var none = GameId.None;
        Assert.False(none.IsValid);
        Assert.Equal(0ul, none.Value);
    }

    [Fact]
    public void GameId_Next_ReturnsValidSequentialIds()
    {
        GameId.ResetCounter();
        var a = GameId.Next();
        var b = GameId.Next();
        var c = GameId.Next();

        Assert.True(a.IsValid);
        Assert.True(b.IsValid);
        Assert.True(c.IsValid);
        Assert.True(b > a);
        Assert.True(c > b);
    }

    [Fact]
    public void GameId_From_CreatesSpecificId()
    {
        var id = GameId.From(42);
        Assert.Equal(42ul, id.Value);
        Assert.True(id.IsValid);
    }

    [Fact]
    public void GameId_Equality()
    {
        var a = GameId.From(10);
        var b = GameId.From(10);
        var c = GameId.From(20);

        Assert.Equal(a, b);
        Assert.NotEqual(a, c);
        Assert.True(a == b);
        Assert.True(a != c);
    }

    [Fact]
    public void GameId_Comparison()
    {
        var a = GameId.From(5);
        var b = GameId.From(10);

        Assert.True(a < b);
        Assert.True(b > a);
        Assert.True(a <= b);
        Assert.True(b >= a);
        Assert.True(a.CompareTo(b) < 0);
    }

    [Fact]
    public void GameId_ImplicitConversion_ToUlong()
    {
        var id = GameId.From(99);
        ulong value = id;
        Assert.Equal(99ul, value);
    }

    [Fact]
    public void GameId_ImplicitConversion_FromUlong()
    {
        GameId id = 42ul;
        Assert.Equal(42ul, id.Value);
    }

    [Fact]
    public void GameId_ToString_ContainsGID()
    {
        var id = GameId.From(123);
        Assert.Contains("GID:", id.ToString());
    }

    [Fact]
    public void GameId_ToShortHex_FormatsCorrectly()
    {
        var id = GameId.From(255);
        var hex = id.ToShortHex();
        Assert.StartsWith("#", hex);
        Assert.Contains("FF", hex);
    }

    [Fact]
    public void GameId_HashCode_ConsistentForEqualIds()
    {
        var a = GameId.From(77);
        var b = GameId.From(77);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void GameId_NextBatch_ReturnsUniqueIds()
    {
        GameId.ResetCounter();
        var batch = new GameId[10];
        GameId.NextBatch(batch);

        Assert.Equal(10, batch.Length);
        Assert.All(batch, id => Assert.True(id.IsValid));

        var unique = new HashSet<ulong>(batch.Select(b => b.Value));
        Assert.Equal(10, unique.Count);
    }

    [Fact]
    public void GameId_ResetCounter_ResetsSequence()
    {
        GameId.ResetCounter();
        var first = GameId.Next();
        Assert.Equal(1ul, first.Value);

        GameId.ResetCounter();
        var secondFirst = GameId.Next();
        Assert.Equal(1ul, secondFirst.Value);
    }

    [Fact]
    public void GameId_CanBeUsedAsDictionaryKey()
    {
        var dict = new Dictionary<GameId, string>();
        var id = GameId.From(42);
        dict[id] = "test";

        Assert.Equal("test", dict[GameId.From(42)]);
    }

    [Fact]
    public void GameId_SortedInDictionary()
    {
        GameId.ResetCounter();
        var sorted = new SortedDictionary<GameId, string>();
        var c = GameId.Next();
        var a = GameId.Next();
        var b = GameId.Next();

        sorted[b] = "b";
        sorted[a] = "a";
        sorted[c] = "c";

        var keys = sorted.Keys.ToList();
        Assert.True(keys[0] < keys[1]);
        Assert.True(keys[1] < keys[2]);
    }
}

/// <summary>Tests for the immutable ColorRPG struct.</summary>
public class ColorRPGTests
{
    [Fact]
    public void ColorRPG_Constructor_ClampsValues()
    {
        var color = new ColorRPG(2f, -1f, 0.5f, 1.5f);
        Assert.Equal(1f, color.R);
        Assert.Equal(0f, color.G);
        Assert.Equal(0.5f, color.B);
        Assert.Equal(1f, color.A);
    }

    [Fact]
    public void ColorRPG_NamedColors_AreCorrect()
    {
        Assert.Equal(1f, ColorRPG.White.R);
        Assert.Equal(0f, ColorRPG.Black.R);
        Assert.Equal(0f, ColorRPG.Clear.A);
        Assert.Equal(1f, ColorRPG.Red.R);
        Assert.Equal(1f, ColorRPG.Green.G);
        Assert.Equal(1f, ColorRPG.Blue.B);
    }

    [Fact]
    public void ColorRPG_RarityColors_AreDistinct()
    {
        var common = ColorRPG.Common;
        var rare = ColorRPG.Rare;
        var epic = ColorRPG.Epic;
        var legendary = ColorRPG.Legendary;

        Assert.NotEqual(common, rare);
        Assert.NotEqual(rare, epic);
        Assert.NotEqual(epic, legendary);
    }

    [Fact]
    public void ColorRPG_ForTier_ReturnsCorrectRarity()
    {
        Assert.Equal(ColorRPG.Common, ColorRPG.ForTier(1));
        Assert.Equal(ColorRPG.Uncommon, ColorRPG.ForTier(2));
        Assert.Equal(ColorRPG.Rare, ColorRPG.ForTier(3));
        Assert.Equal(ColorRPG.Epic, ColorRPG.ForTier(4));
        Assert.Equal(ColorRPG.Legendary, ColorRPG.ForTier(5));
        Assert.Equal(ColorRPG.Mythic, ColorRPG.ForTier(6));
        Assert.Equal(ColorRPG.Celestial, ColorRPG.ForTier(7));
    }

    [Fact]
    public void ColorRPG_ForElement_MapsAllDamageTypes()
    {
        Assert.Equal(ColorRPG.Fire, ColorRPG.ForElement(DamageType.Fire));
        Assert.Equal(ColorRPG.Ice, ColorRPG.ForElement(DamageType.Ice));
        Assert.Equal(ColorRPG.Lightning, ColorRPG.ForElement(DamageType.Lightning));
        Assert.Equal(ColorRPG.Arcane, ColorRPG.ForElement(DamageType.Arcane));
        Assert.Equal(ColorRPG.Holy, ColorRPG.ForElement(DamageType.Holy));
        Assert.Equal(ColorRPG.Void, ColorRPG.ForElement(DamageType.Void));
        Assert.Equal(ColorRPG.Nature, ColorRPG.ForElement(DamageType.Nature));
    }

    [Fact]
    public void ColorRPG_FromBytes_ConvertsCorrectly()
    {
        var color = ColorRPG.FromBytes(255, 128, 0);
        Assert.Equal(1f, color.R, 0.01);
        Assert.Equal(128f / 255f, color.G, 0.01);
        Assert.Equal(0f, color.B, 0.01);
    }

    [Fact]
    public void ColorRPG_FromHex_ParsesRGB()
    {
        var color = ColorRPG.FromHex("#FF8000");
        Assert.Equal(1f, color.R, 0.01);
        Assert.Equal(128f / 255f, color.G, 0.01);
        Assert.Equal(0f, color.B, 0.01);
    }

    [Fact]
    public void ColorRPG_FromHex_ParsesRGBA()
    {
        var color = ColorRPG.FromHex("#FF800080");
        Assert.Equal(1f, color.R, 0.01);
        Assert.Equal(0.5f, color.A, 0.02);
    }

    [Fact]
    public void ColorRPG_FromHex_HandlesNoHash()
    {
        var color = ColorRPG.FromHex("FF0000");
        Assert.Equal(1f, color.R, 0.01);
        Assert.Equal(0f, color.G, 0.01);
    }

    [Fact]
    public void ColorRPG_ToHex_RoundTrips()
    {
        var original = ColorRPG.FromHex("#FF8040FF");
        var hex = original.ToHex();
        var parsed = ColorRPG.FromHex(hex);

        Assert.Equal(original.R, parsed.R, 0.02);
        Assert.Equal(original.G, parsed.G, 0.02);
        Assert.Equal(original.B, parsed.B, 0.02);
    }

    [Fact]
    public void ColorRPG_Lerp_InterpolatesCorrectly()
    {
        var black = ColorRPG.Black;
        var white = ColorRPG.White;

        var mid = ColorRPG.Lerp(black, white, 0.5f);
        Assert.Equal(0.5f, mid.R, 0.01);
        Assert.Equal(0.5f, mid.G, 0.01);
        Assert.Equal(0.5f, mid.B, 0.01);
    }

    [Fact]
    public void ColorRPG_Lerp_ClampsTParameter()
    {
        var a = ColorRPG.Black;
        var b = ColorRPG.White;

        Assert.Equal(a, ColorRPG.Lerp(a, b, -1f));
        Assert.Equal(b, ColorRPG.Lerp(a, b, 2f));
    }

    [Fact]
    public void ColorRPG_Brightness_CalculatesLuminance()
    {
        Assert.Equal(0f, ColorRPG.Black.Brightness, 0.01);
        Assert.Equal(1f, ColorRPG.White.Brightness, 0.01);
        Assert.True(ColorRPG.Green.Brightness > ColorRPG.Red.Brightness);
    }

    [Fact]
    public void ColorRPG_WithAlpha_ChangesAlphaOnly()
    {
        var opaque = ColorRPG.Red;
        var transparent = opaque.WithAlpha(0.5f);

        Assert.Equal(1f, transparent.R);
        Assert.Equal(0.5f, transparent.A);
    }

    [Fact]
    public void ColorRPG_Brighten_IncreasesRGB()
    {
        var dark = new ColorRPG(0.3f, 0.3f, 0.3f);
        var bright = dark.Brighten(0.2f);
        Assert.Equal(0.5f, bright.R, 0.01);
    }

    [Fact]
    public void ColorRPG_Scale_MultipliesRGB()
    {
        var color = new ColorRPG(0.5f, 0.5f, 0.5f);
        var scaled = color.Scale(0.5f);
        Assert.Equal(0.25f, scaled.R, 0.01);
    }

    [Fact]
    public void ColorRPG_Equality_Works()
    {
        var a = new ColorRPG(0.5f, 0.5f, 0.5f);
        var b = new ColorRPG(0.5f, 0.5f, 0.5f);
        var c = new ColorRPG(0.6f, 0.5f, 0.5f);

        Assert.Equal(a, b);
        Assert.NotEqual(a, c);
        Assert.True(a == b);
        Assert.True(a != c);
    }

    [Fact]
    public void ColorRPG_Operators_Work()
    {
        var a = new ColorRPG(0.3f, 0.3f, 0.3f);
        var b = new ColorRPG(0.2f, 0.2f, 0.2f);

        var sum = a + b;
        Assert.Equal(0.5f, sum.R, 0.01);

        var product = a * b;
        Assert.Equal(0.06f, product.R, 0.01);

        var scalarProduct = a * 2f;
        Assert.Equal(0.6f, scalarProduct.R, 0.01);
    }

    [Fact]
    public void ColorRPG_FromHSV_RedAt0Degrees()
    {
        var red = ColorRPG.FromHSV(0f, 1f, 1f);
        Assert.Equal(1f, red.R, 0.01);
        Assert.Equal(0f, red.G, 0.1);
        Assert.Equal(0f, red.B, 0.1);
    }

    [Fact]
    public void ColorRPG_AllNamedColors_HaveValidRanges()
    {
        var colors = new[]
        {
            ColorRPG.White, ColorRPG.Black, ColorRPG.Red, ColorRPG.Green, ColorRPG.Blue,
            ColorRPG.Common, ColorRPG.Uncommon, ColorRPG.Rare, ColorRPG.Epic,
            ColorRPG.Legendary, ColorRPG.Mythic, ColorRPG.Celestial,
            ColorRPG.Fire, ColorRPG.Ice, ColorRPG.Lightning, ColorRPG.Nature,
            ColorRPG.Void, ColorRPG.Holy, ColorRPG.Arcane
        };

        foreach (var c in colors)
        {
            Assert.InRange(c.R, 0f, 1f);
            Assert.InRange(c.G, 0f, 1f);
            Assert.InRange(c.B, 0f, 1f);
            Assert.InRange(c.A, 0f, 1f);
        }
    }
}

/// <summary>Tests for GridPosRPG and GridRelRPG structs.</summary>
public class GridPosRPGTests
{
    [Fact]
    public void GridPosRPG_Origin_IsZeroZero()
    {
        var origin = GridPosRPG.Origin;
        Assert.Equal(0, origin.X);
        Assert.Equal(0, origin.Y);
    }

    [Fact]
    public void GridPosRPG_Equality()
    {
        var a = new GridPosRPG(3, 4);
        var b = new GridPosRPG(3, 4);
        var c = new GridPosRPG(5, 6);

        Assert.Equal(a, b);
        Assert.NotEqual(a, c);
        Assert.True(a == b);
        Assert.True(a != c);
    }

    [Fact]
    public void GridPosRPG_Neighbor_ReturnsCorrectDirections()
    {
        var pos = new GridPosRPG(5, 5);

        Assert.Equal(new GridPosRPG(5, 6), pos.Neighbor(Direction.North));
        Assert.Equal(new GridPosRPG(6, 5), pos.Neighbor(Direction.East));
        Assert.Equal(new GridPosRPG(5, 4), pos.Neighbor(Direction.South));
        Assert.Equal(new GridPosRPG(4, 5), pos.Neighbor(Direction.West));
    }

    [Fact]
    public void GridPosRPG_ManhattanDistance()
    {
        var a = new GridPosRPG(0, 0);
        var b = new GridPosRPG(3, 4);
        Assert.Equal(7, a.ManhattanTo(b));
    }

    [Fact]
    public void GridPosRPG_ChebyshevDistance()
    {
        var a = new GridPosRPG(0, 0);
        var b = new GridPosRPG(3, 4);
        Assert.Equal(4, a.ChebyshevTo(b));
    }

    [Fact]
    public void GridPosRPG_SquaredDistance()
    {
        var a = new GridPosRPG(0, 0);
        var b = new GridPosRPG(3, 4);
        Assert.Equal(25, a.SquaredDistanceTo(b));
    }

    [Fact]
    public void GridPosRPG_EuclideanDistance()
    {
        var a = new GridPosRPG(0, 0);
        var b = new GridPosRPG(3, 4);
        Assert.Equal(5f, a.DistanceTo(b), 0.01);
    }

    [Fact]
    public void GridPosRPG_IsWithinRange()
    {
        var center = new GridPosRPG(5, 5);
        Assert.True(center.IsWithinRange(new GridPosRPG(6, 5), 2));
        Assert.True(center.IsWithinRange(new GridPosRPG(5, 5), 0));
        Assert.False(center.IsWithinRange(new GridPosRPG(8, 8), 3));
    }

    [Fact]
    public void GridPosRPG_IsInRect()
    {
        var min = new GridPosRPG(0, 0);
        var max = new GridPosRPG(10, 10);
        Assert.True(new GridPosRPG(5, 5).IsInRect(min, max));
        Assert.True(new GridPosRPG(0, 0).IsInRect(min, max));
        Assert.True(new GridPosRPG(10, 10).IsInRect(min, max));
        Assert.False(new GridPosRPG(11, 5).IsInRect(min, max));
    }

    [Fact]
    public void GridPosRPG_Clamp()
    {
        var min = new GridPosRPG(0, 0);
        var max = new GridPosRPG(10, 10);

        Assert.Equal(new GridPosRPG(5, 5), new GridPosRPG(5, 5).Clamp(min, max));
        Assert.Equal(new GridPosRPG(10, 10), new GridPosRPG(15, 15).Clamp(min, max));
        Assert.Equal(new GridPosRPG(0, 0), new GridPosRPG(-5, -5).Clamp(min, max));
    }

    [Fact]
    public void GridPosRPG_Operators()
    {
        var a = new GridPosRPG(3, 4);
        var b = new GridPosRPG(1, 2);

        Assert.Equal(new GridPosRPG(4, 6), a + b);
        Assert.Equal(new GridPosRPG(2, 2), a - b);
        Assert.Equal(new GridPosRPG(6, 8), a * 2);
        Assert.Equal(new GridPosRPG(-3, -4), -a);
    }

    [Fact]
    public void GridPosRPG_GetCardinalNeighbors_ReturnsFour()
    {
        var pos = new GridPosRPG(5, 5);
        Span<GridPosRPG> neighbors = stackalloc GridPosRPG[4];
        pos.GetCardinalNeighbors(neighbors);

        Assert.Equal(new GridPosRPG(5, 6), neighbors[0]);
        Assert.Equal(new GridPosRPG(6, 5), neighbors[1]);
        Assert.Equal(new GridPosRPG(5, 4), neighbors[2]);
        Assert.Equal(new GridPosRPG(4, 5), neighbors[3]);
    }

    [Fact]
    public void GridPosRPG_GetAllNeighbors_ReturnsEight()
    {
        var pos = new GridPosRPG(5, 5);
        Span<GridPosRPG> neighbors = stackalloc GridPosRPG[8];
        pos.GetAllNeighbors(neighbors);

        Assert.Equal(new GridPosRPG(5, 6), neighbors[0]);
        Assert.Equal(new GridPosRPG(6, 6), neighbors[1]);
        Assert.Equal(new GridPosRPG(6, 5), neighbors[2]);
    }

    [Fact]
    public void GridPosRPG_CompareTo_SortsByYThenX()
    {
        var a = new GridPosRPG(5, 3);
        var b = new GridPosRPG(2, 5);
        var c = new GridPosRPG(1, 3);

        Assert.True(a.CompareTo(b) < 0);
        Assert.True(a.CompareTo(c) > 0);
    }

    [Fact]
    public void GridPosRPG_Offset()
    {
        var pos = new GridPosRPG(5, 5);
        Assert.Equal(new GridPosRPG(7, 3), pos.Offset(2, -2));
    }

    [Fact]
    public void GridPosRPG_EqualityRoundtrip()
    {
        var a = new GridPosRPG(7, 13);
        var b = new GridPosRPG(7, 13);

        Assert.Equal(a, b);
        Assert.Equal(a.X, b.X);
        Assert.Equal(a.Y, b.Y);
    }

    [Fact]
    public void GridPosRPG_HashCode_DifferentForDifferentPositions()
    {
        var a = new GridPosRPG(0, 0);
        var b = new GridPosRPG(1, 0);
        var c = new GridPosRPG(0, 1);

        Assert.NotEqual(a.GetHashCode(), b.GetHashCode());
        Assert.NotEqual(a.GetHashCode(), c.GetHashCode());
    }

    // --- GridRelRPG ---

    [Fact]
    public void GridRelRPG_NamedOffsets()
    {
        Assert.Equal(new GridRelRPG(0, 1), GridRelRPG.Up);
        Assert.Equal(new GridRelRPG(0, -1), GridRelRPG.Down);
        Assert.Equal(new GridRelRPG(1, 0), GridRelRPG.Right);
        Assert.Equal(new GridRelRPG(-1, 0), GridRelRPG.Left);
    }

    [Fact]
    public void GridRelRPG_RotateCW()
    {
        var up = GridRelRPG.Up;
        var rotated = up.RotateCW();
        Assert.Equal(new GridRelRPG(1, 0), rotated);
    }

    [Fact]
    public void GridRelRPG_RotateCCW()
    {
        var up = GridRelRPG.Up;
        var rotated = up.RotateCCW();
        Assert.Equal(new GridRelRPG(-1, 0), rotated);
    }

    [Fact]
    public void GridRelRPG_Reverse()
    {
        var up = GridRelRPG.Up;
        Assert.Equal(GridRelRPG.Down, up.Reverse());
    }

    [Fact]
    public void GridRelRPG_ApplyTo_Position()
    {
        var pos = new GridPosRPG(5, 5);
        var offset = new GridRelRPG(3, -2);
        Assert.Equal(new GridPosRPG(8, 3), offset.ApplyTo(pos));
    }

    [Fact]
    public void GridRelRPG_Manhattan()
    {
        var rel = new GridRelRPG(3, -4);
        Assert.Equal(7, rel.Manhattan);
    }

    [Fact]
    public void GridRelRPG_Chebyshev()
    {
        var rel = new GridRelRPG(3, -4);
        Assert.Equal(4, rel.Chebyshev);
    }

    [Fact]
    public void GridRelRPG_OperatorPlus_WithPosition()
    {
        var pos = new GridPosRPG(5, 5);
        var rel = new GridRelRPG(2, 3);
        Assert.Equal(new GridPosRPG(7, 8), pos + rel);
    }

    [Fact]
    public void GridRelRPG_Scale()
    {
        var rel = new GridRelRPG(2, 3);
        Assert.Equal(new GridRelRPG(6, 9), rel.Scale(3));
    }

    [Fact]
    public void GridRelRPG_FromDirection()
    {
        Assert.Equal(GridRelRPG.Up, GridRelRPG.FromDirection(Direction.North));
        Assert.Equal(GridRelRPG.Right, GridRelRPG.FromDirection(Direction.East));
        Assert.Equal(GridRelRPG.Down, GridRelRPG.FromDirection(Direction.South));
        Assert.Equal(GridRelRPG.Left, GridRelRPG.FromDirection(Direction.West));
    }

    [Fact]
    public void GridRelRPG_FullRotation_Returns_ToOriginal()
    {
        var original = new GridRelRPG(3, 5);
        var rotated = original.RotateCW().RotateCW().RotateCW().RotateCW();
        Assert.Equal(original, rotated);
    }

    [Fact]
    public void GridRelRPG_Operators_Work()
    {
        var a = new GridRelRPG(2, 3);
        var b = new GridRelRPG(1, 1);

        Assert.Equal(new GridRelRPG(3, 4), a + b);
        Assert.Equal(new GridRelRPG(1, 2), a - b);
        Assert.Equal(new GridRelRPG(4, 6), a * 2);
        Assert.Equal(new GridRelRPG(-2, -3), -a);
    }
}

/// <summary>Tests for the TileSpec struct.</summary>
public class TileSpecTests
{
    [Fact]
    public void TilesRPG_Grass_IsWalkableAndBuildable()
    {
        var grass = TileSpec.Grass;
        Assert.True(grass.IsWalkable);
        Assert.True(grass.IsBuildable);
        Assert.Equal(TileKind.Grass, grass.Kind);
    }

    [Fact]
    public void TilesRPG_Mountain_IsNotWalkable()
    {
        var mountain = TileSpec.Mountain;
        Assert.False(mountain.IsWalkable);
        Assert.False(mountain.IsBuildable);
        Assert.Equal(3, mountain.Height);
    }

    [Fact]
    public void TilesRPG_Water_IsNotPassable()
    {
        var water = TileSpec.Water;
        Assert.False(water.IsWalkable);
        Assert.False(water.IsBuildable);
        Assert.Equal(0, water.Height);
    }

    [Fact]
    public void TilesRPG_Forest_HasResources()
    {
        var forest = TileSpec.Forest;
        Assert.True(forest.HasResources);
        Assert.Equal(1.0f, forest.Fertility);
    }

    [Fact]
    public void TilesRPG_WithWalkable_ReturnsModifiedCopy()
    {
        var mountain = TileSpec.Mountain;
        var passable = mountain.WithWalkable(true);

        Assert.False(mountain.IsWalkable);
        Assert.True(passable.IsWalkable);
        Assert.Equal(TileKind.Mountain, passable.Kind);
    }

    [Fact]
    public void TilesRPG_WithBuildable_ReturnsModifiedCopy()
    {
        var forest = TileSpec.Forest;
        var buildable = forest.WithBuildable(true);

        Assert.False(forest.IsBuildable);
        Assert.True(buildable.IsBuildable);
    }

    [Fact]
    public void TilesRPG_WithFertility_ReturnsModifiedCopy()
    {
        var grass = TileSpec.Grass;
        var barren = grass.WithFertility(0.1f);

        Assert.Equal(0.1f, barren.Fertility, 0.01);
    }

    [Fact]
    public void TilesRPG_MovementMultiplier_VariesByKind()
    {
        Assert.Equal(1.5f, TileSpec.Road.MovementMultiplier);
        Assert.Equal(1.0f, TileSpec.Grass.MovementMultiplier);
        Assert.Equal(0.4f, TileSpec.Swamp.MovementMultiplier);
    }

    [Fact]
    public void TilesRPG_IsIdealForBuilding()
    {
        Assert.True(TileSpec.Grass.IsIdealForBuilding);
        Assert.False(TileSpec.Forest.IsIdealForBuilding);
        Assert.False(TileSpec.Mountain.IsIdealForBuilding);
    }

    [Fact]
    public void TilesRPG_TileColor_ReturnsColor()
    {
        var grassColor = TileSpec.Grass.TileColor;
        Assert.True(grassColor.G > grassColor.R);
    }

    [Fact]
    public void TilesRPG_WithResourceDensity()
    {
        var stone = TileSpec.Stone;
        var rich = stone.WithResourceDensity(0.95f);
        Assert.Equal(0.95f, rich.ResourceDensity, 0.01);
    }

    [Fact]
    public void TilesRPG_AtHeight()
    {
        var grass = TileSpec.Grass;
        var elevated = grass.AtHeight(5);
        Assert.Equal(5, elevated.Height);
        Assert.Equal(TileKind.Grass, elevated.Kind);
    }

    [Fact]
    public void TilesRPG_Equality()
    {
        var a = TileSpec.Grass;
        var b = TileSpec.Grass;
        var c = TileSpec.Forest;

        Assert.Equal(a, b);
        Assert.NotEqual(a, c);
    }

    [Fact]
    public void TilesRPG_AllFactories_HaveCorrectKind()
    {
        Assert.Equal(TileKind.Sand, TileSpec.Sand.Kind);
        Assert.Equal(TileKind.Swamp, TileSpec.Swamp.Kind);
        Assert.Equal(TileKind.Lava, TileSpec.Lava.Kind);
        Assert.Equal(TileKind.Snow, TileSpec.Snow.Kind);
        Assert.Equal(TileKind.Void, TileSpec.Void.Kind);
        Assert.Equal(TileKind.Road, TileSpec.Road.Kind);
        Assert.Equal(TileKind.Stone, TileSpec.Stone.Kind);
    }

    [Fact]
    public void TilesRPG_Immutability_OriginalUnchanged()
    {
        var original = TileSpec.Grass;
        var modified = original.WithFertility(0.1f).WithWalkable(false);

        Assert.True(original.IsWalkable);
        Assert.Equal(0.8f, original.Fertility, 0.01);
        Assert.False(modified.IsWalkable);
        Assert.Equal(0.1f, modified.Fertility, 0.01);
    }
}

/// <summary>Tests for the ChanceRPG probability struct.</summary>
public class ChanceRPGTests
{
    [Fact]
    public void ChanceRPG_Impossible_NeverSucceeds()
    {
        var chance = ChanceRPG.Impossible;
        Assert.True(chance.IsImpossible);
        Assert.False(chance.IsGuaranteed);
        Assert.Equal(0f, chance.Value);

        var random = new Random(42);
        for (int i = 0; i < 100; i++)
        {
            Assert.False(chance.Roll(random));
        }
    }

    [Fact]
    public void ChanceRPG_Certain_AlwaysSucceeds()
    {
        var chance = ChanceRPG.Certain;
        Assert.True(chance.IsGuaranteed);
        Assert.False(chance.IsImpossible);
        Assert.Equal(1f, chance.Value);

        var random = new Random(42);
        for (int i = 0; i < 100; i++)
        {
            Assert.True(chance.Roll(random));
        }
    }

    [Fact]
    public void ChanceRPG_Constructor_ClampsValues()
    {
        var over = new ChanceRPG(1.5f);
        var under = new ChanceRPG(-0.5f);

        Assert.Equal(1f, over.Value);
        Assert.Equal(0f, under.Value);
    }

    [Fact]
    public void ChanceRPG_FromPercent()
    {
        var chance = ChanceRPG.FromPercent(75f);
        Assert.Equal(0.75f, chance.Value, 0.001);
    }

    [Fact]
    public void ChanceRPG_FromRatio()
    {
        var chance = ChanceRPG.FromRatio(1, 6);
        Assert.Equal(1f / 6f, chance.Value, 0.001);
    }

    [Fact]
    public void ChanceRPG_AsPercent()
    {
        var chance = new ChanceRPG(0.75f);
        Assert.Equal(75f, chance.AsPercent, 0.1);
    }

    [Fact]
    public void ChanceRPG_And_MultipliesProbabilities()
    {
        var a = new ChanceRPG(0.5f);
        var b = new ChanceRPG(0.5f);
        Assert.Equal(0.25f, a.And(b).Value, 0.001);
    }

    [Fact]
    public void ChanceRPG_Or_CombinesProbabilities()
    {
        var a = new ChanceRPG(0.5f);
        var b = new ChanceRPG(0.5f);
        Assert.Equal(0.75f, a.Or(b).Value, 0.001);
    }

    [Fact]
    public void ChanceRPG_Not_InvertsProbability()
    {
        var chance = new ChanceRPG(0.3f);
        Assert.Equal(0.7f, chance.Not().Value, 0.001);
    }

    [Fact]
    public void ChanceRPG_Boost_MultipliesValue()
    {
        var chance = new ChanceRPG(0.5f);
        Assert.Equal(0.75f, chance.Boost(1.5f).Value, 0.001);
    }

    [Fact]
    public void ChanceRPG_AddFlat_AddsToValue()
    {
        var chance = new ChanceRPG(0.5f);
        Assert.Equal(0.7f, chance.AddFlat(0.2f).Value, 0.001);
    }

    [Fact]
    public void ChanceRPG_LerpTo_Interpolates()
    {
        var a = new ChanceRPG(0.2f);
        var b = new ChanceRPG(0.8f);
        Assert.Equal(0.5f, a.LerpTo(b, 0.5f).Value, 0.001);
    }

    [Fact]
    public void ChanceRPG_AtLeastOnceIn_CalculatesCorrectly()
    {
        var chance = new ChanceRPG(0.5f);
        var atLeastOnce = chance.AtLeastOnceIn(3);
        Assert.Equal(0.875f, atLeastOnce.Value, 0.001);
    }

    [Fact]
    public void ChanceRPG_ExpectedTrials()
    {
        var chance = new ChanceRPG(0.25f);
        Assert.Equal(4f, chance.ExpectedTrials, 0.01);

        Assert.Equal(float.PositiveInfinity, ChanceRPG.Impossible.ExpectedTrials);
    }

    [Fact]
    public void ChanceRPG_Roll_ProducesMixedResults()
    {
        var chance = new ChanceRPG(0.5f);
        var random = new Random(42);
        int successes = 0;
        int trials = 1000;

        for (int i = 0; i < trials; i++)
        {
            if (chance.Roll(random))
            {
                successes++;
            }
        }

        Assert.InRange(successes, 400, 600);
    }

    [Fact]
    public void ChanceRPG_ForTier_IncreasesByTier()
    {
        var tier1 = ChanceRPG.ForTier(1);
        var tier5 = ChanceRPG.ForTier(5);
        Assert.True(tier5.Value > tier1.Value);
    }

    [Fact]
    public void ChanceRPG_Operators()
    {
        var a = new ChanceRPG(0.5f);
        var b = new ChanceRPG(0.3f);

        var or = a + b;
        Assert.Equal(a.Or(b).Value, or.Value, 0.001);

        var and = a * b;
        Assert.Equal(a.And(b).Value, and.Value, 0.001);

        var not = !a;
        Assert.Equal(a.Not().Value, not.Value, 0.001);
    }

    [Fact]
    public void ChanceRPG_Comparison()
    {
        Assert.True(ChanceRPG.Certain > ChanceRPG.Half);
        Assert.True(ChanceRPG.Impossible < ChanceRPG.Half);
    }

    [Fact]
    public void ChanceRPG_Equality()
    {
        var a = new ChanceRPG(0.5f);
        var b = new ChanceRPG(0.5f);
        Assert.Equal(a, b);
        Assert.True(a == b);
    }

    [Fact]
    public void ChanceRPG_CritChance_ScalesWithTier()
    {
        var t1 = ChanceRPG.CritChance(0.05f, 1);
        var t5 = ChanceRPG.CritChance(0.05f, 5);
        Assert.True(t5.Value > t1.Value);
    }

    [Fact]
    public void ChanceRPG_OrIdentity_ZeroChanceIsIdentity()
    {
        var chance = new ChanceRPG(0.7f);
        var result = chance.Or(ChanceRPG.Impossible);
        Assert.Equal(chance.Value, result.Value, 0.001);
    }

    [Fact]
    public void ChanceRPG_AndIdentity_CertainIsIdentity()
    {
        var chance = new ChanceRPG(0.7f);
        var result = chance.And(ChanceRPG.Certain);
        Assert.Equal(chance.Value, result.Value, 0.001);
    }

    [Fact]
    public void ChanceRPG_DoubleNot_ReturnsOriginal()
    {
        var chance = new ChanceRPG(0.35f);
        Assert.Equal(chance.Value, chance.Not().Not().Value, 0.001);
    }
}
