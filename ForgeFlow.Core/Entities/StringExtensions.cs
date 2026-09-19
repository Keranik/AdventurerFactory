using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace ForgeFlow.Core.Entities;

/// <summary>
/// Comprehensive string extension methods tailored for RPG/factory-builder development.
/// Covers UI display, formatting, rich text, naming conventions, proto ID cleanup,
/// and RPG-specific utilities like Roman numerals and ordinals.
/// All methods are null-safe with early returns. Pure .NET — zero Unity references.
/// </summary>
public static class StringExtensions
{
    #region Safety & Basics

    /// <summary>Returns true if the string is null or empty.</summary>
    public static bool IsNullOrEmpty(this string? str) => string.IsNullOrEmpty(str);

    /// <summary>Returns true if the string is null, empty, or whitespace only.</summary>
    public static bool IsNullOrWhitespace(this string? str) => string.IsNullOrWhiteSpace(str);

    #endregion

    #region Substring & Truncation

    /// <summary>Returns the first N characters, or the full string if shorter.</summary>
    public static string First(this string? str, int length)
    {
        if (str == null || str.Length == 0 || length <= 0)
        {
            return string.Empty;
        }

        return str.Length <= length ? str : str.Substring(0, length);
    }

    /// <summary>Returns the first 2 characters.</summary>
    public static string First2(this string? str) => str.First(2);

    /// <summary>Returns the first 2 characters in uppercase.</summary>
    public static string First2Upper(this string? str) => str.First(2).ToUpperInvariant();

    /// <summary>Returns the first 3 characters in uppercase.</summary>
    public static string First3Upper(this string? str) => str.First(3).ToUpperInvariant();

    /// <summary>Truncates the string to the specified max length, appending a suffix if truncated.</summary>
    public static string Truncate(this string? str, int maxLength, string suffix = "...")
    {
        if (str == null || str.Length == 0)
        {
            return string.Empty;
        }

        if (str.Length <= maxLength)
        {
            return str;
        }

        int suffixLen = suffix.Length;
        if (maxLength <= suffixLen)
        {
            return suffix.Substring(0, maxLength);
        }

        return str.Substring(0, maxLength - suffixLen) + suffix;
    }

    /// <summary>Truncates the middle of a long string, keeping prefix and suffix visible.</summary>
    public static string TruncateMiddle(this string? str, int maxLength, string middle = "...")
    {
        if (str == null || str.Length == 0 || str.Length <= maxLength)
        {
            return str ?? string.Empty;
        }

        int midLen = middle.Length;
        int side = (maxLength - midLen) / 2;
        if (side <= 0)
        {
            return middle;
        }

        return str.Substring(0, side) + middle + str.Substring(str.Length - side);
    }

    #endregion

    #region Casing & Formatting

    /// <summary>Converts to Title Case.</summary>
    public static string ToTitleCase(this string? str)
    {
        if (str == null || str.Length == 0)
        {
            return string.Empty;
        }

        return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(str.ToLowerInvariant());
    }

    /// <summary>Converts to sentence case (first letter uppercase).</summary>
    public static string ToSentenceCase(this string? str)
    {
        if (str == null || str.Length == 0)
        {
            return string.Empty;
        }

        if (str.Length == 1)
        {
            return str.ToUpperInvariant();
        }

        return char.ToUpperInvariant(str[0]) + str.Substring(1).ToLowerInvariant();
    }

    /// <summary>Capitalizes only the first character.</summary>
    public static string CapitalizeFirst(this string? str)
    {
        if (str == null || str.Length == 0)
        {
            return string.Empty;
        }

        if (str.Length == 1)
        {
            return str.ToUpperInvariant();
        }

        return char.ToUpperInvariant(str[0]) + str.Substring(1);
    }

    /// <summary>Removes all whitespace characters.</summary>
    public static string RemoveWhitespace(this string? str)
    {
        if (str == null || str.Length == 0)
        {
            return string.Empty;
        }

        return str.Replace(" ", "")
                  .Replace("\t", "")
                  .Replace("\n", "")
                  .Replace("\r", "");
    }

    #endregion

    #region Case Conversion (camel, pascal, snake, kebab)

    private static readonly Regex CamelSplit = new("([a-z])([A-Z])", RegexOptions.Compiled);
    private static readonly Regex InvalidIdChars = new("[^a-zA-Z0-9_]", RegexOptions.Compiled);

    /// <summary>Converts to camelCase.</summary>
    public static string ToCamelCase(this string? str)
    {
        if (str == null || str.Length == 0)
        {
            return string.Empty;
        }

        string pascal = str.ToPascalCase();
        return pascal.Length > 1
            ? char.ToLowerInvariant(pascal[0]) + pascal.Substring(1)
            : pascal.ToLowerInvariant();
    }

    /// <summary>Converts to PascalCase.</summary>
    public static string ToPascalCase(this string? str)
    {
        if (str == null || str.Length == 0)
        {
            return string.Empty;
        }

        str = str.Replace("_", " ").Replace("-", " ").ToTitleCase();
        return str.RemoveWhitespace();
    }

    /// <summary>Converts to snake_case.</summary>
    public static string ToSnakeCase(this string? str)
    {
        if (str == null || str.Length == 0)
        {
            return string.Empty;
        }

        str = CamelSplit.Replace(str, "$1_$2");
        return InvalidIdChars.Replace(str, "").ToLowerInvariant();
    }

    /// <summary>Converts to kebab-case.</summary>
    public static string ToKebabCase(this string? str) => str.ToSnakeCase().Replace('_', '-');

    #endregion

    #region Initials & Abbreviations

    /// <summary>
    /// Creates smart initials from a name.
    /// "Deep Forest" → "DF", "Health Potion" → "HP", "Sword" → "SW"
    /// </summary>
    public static string ToInitials(this string? str)
    {
        if (str == null || str.Length == 0)
        {
            return "?";
        }

        var parts = str.Split(new[] { ' ', '_' }, StringSplitOptions.RemoveEmptyEntries);
        var sb = new StringBuilder();

        foreach (var part in parts)
        {
            if (part.Length > 0 && char.IsLetterOrDigit(part[0]))
            {
                sb.Append(char.ToUpperInvariant(part[0]));
                if (sb.Length >= 2)
                {
                    break;
                }
            }
        }

        return sb.Length == 0 ? "?" : sb.ToString();
    }

    #endregion

    #region Pluralization (Simple English)

    private static readonly Dictionary<string, string> PluralOverrides = new(StringComparer.OrdinalIgnoreCase)
    {
        { "sheep", "sheep" },
        { "deer", "deer" },
        { "fish", "fish" },
        { "series", "series" },
        { "species", "species" },
        { "gold", "gold" },
        { "mana", "mana" }
    };

    /// <summary>Simple English pluralization with RPG-specific overrides.</summary>
    public static string Pluralize(this string? str, int count = 2)
    {
        if (str == null || str.Length == 0 || count == 1)
        {
            return str ?? string.Empty;
        }

        if (PluralOverrides.TryGetValue(str, out var plural))
        {
            return plural;
        }

        if (str.EndsWith("s", StringComparison.OrdinalIgnoreCase) ||
            str.EndsWith("x", StringComparison.OrdinalIgnoreCase) ||
            str.EndsWith("ch", StringComparison.OrdinalIgnoreCase) ||
            str.EndsWith("sh", StringComparison.OrdinalIgnoreCase))
        {
            return str + "es";
        }

        if (str.Length > 1 && str.EndsWith("y", StringComparison.OrdinalIgnoreCase) &&
            !"aeiouAEIOU".Contains(str[str.Length - 2]))
        {
            return str.Substring(0, str.Length - 1) + "ies";
        }

        if (str.EndsWith("fe", StringComparison.OrdinalIgnoreCase))
        {
            return str.Substring(0, str.Length - 2) + "ves";
        }

        if (str.EndsWith("f", StringComparison.OrdinalIgnoreCase))
        {
            return str.Substring(0, str.Length - 1) + "ves";
        }

        return str + "s";
    }

    /// <summary>Simple singularization (reverse of Pluralize).</summary>
    public static string Singularize(this string? str)
    {
        if (str == null || str.Length == 0)
        {
            return string.Empty;
        }

        if (PluralOverrides.TryGetValue(str, out var singular))
        {
            return singular;
        }

        if (str.EndsWith("ies", StringComparison.OrdinalIgnoreCase) && str.Length > 3)
        {
            return str.Substring(0, str.Length - 3) + "y";
        }

        if (str.EndsWith("es", StringComparison.OrdinalIgnoreCase))
        {
            return str.Length > 4 ? str.Substring(0, str.Length - 2) : str;
        }

        if (str.EndsWith("s", StringComparison.OrdinalIgnoreCase) &&
            !str.EndsWith("ss", StringComparison.OrdinalIgnoreCase))
        {
            return str.Substring(0, str.Length - 1);
        }

        return str;
    }

    #endregion

    #region Case-Insensitive Operations

    /// <summary>Case-insensitive Contains.</summary>
    public static bool ContainsIgnoreCase(this string? source, string value)
    {
        if (source == null)
        {
            return false;
        }

        return source.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    /// <summary>Case-insensitive equality.</summary>
    public static bool EqualsIgnoreCase(this string? source, string? value)
    {
        return string.Equals(source, value, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Case-insensitive replace.</summary>
    public static string ReplaceIgnoreCase(this string? source, string oldValue, string newValue)
    {
        if (source == null || source.Length == 0)
        {
            return string.Empty;
        }

        return Regex.Replace(source, Regex.Escape(oldValue), newValue.Replace("$", "$$"), RegexOptions.IgnoreCase);
    }

    #endregion

    #region Rich Text (Markup Tags)

    /// <summary>Wraps text in color tag using ColorRPG.</summary>
    public static string WithColor(this string? text, ColorRPG color)
    {
        if (text == null || text.Length == 0)
        {
            return string.Empty;
        }

        string hex = color.ToHex().TrimStart('#');
        return $"<color=#{hex}>{text}</color>";
    }

    /// <summary>Wraps text in size tag.</summary>
    public static string WithSize(this string? text, int size)
    {
        if (text == null || text.Length == 0)
        {
            return string.Empty;
        }

        return $"<size={size}>{text}</size>";
    }

    /// <summary>Applies bold tag.</summary>
    public static string Bold(this string? text) =>
        text == null || text.Length == 0 ? string.Empty : $"<b>{text}</b>";

    /// <summary>Applies italic tag.</summary>
    public static string Italic(this string? text) =>
        text == null || text.Length == 0 ? string.Empty : $"<i>{text}</i>";

    /// <summary>Applies underline tag.</summary>
    public static string Underline(this string? text) =>
        text == null || text.Length == 0 ? string.Empty : $"<u>{text}</u>";

    /// <summary>Applies strikethrough tag.</summary>
    public static string Strikethrough(this string? text) =>
        text == null || text.Length == 0 ? string.Empty : $"<s>{text}</s>";

    /// <summary>Strips all rich text tags.</summary>
    public static string StripRichText(this string? text)
    {
        if (text == null || text.Length == 0)
        {
            return string.Empty;
        }

        return Regex.Replace(text, @"</?(color|size|b|i|u|s|mark|nobr|align|sprite).*?>", string.Empty);
    }

    #endregion

    #region RPG Display & ID Utilities

    /// <summary>
    /// Converts a proto ID to display name.
    /// "Terrain_Forest_DeepForest" → "Deep Forest"
    /// </summary>
    public static string ToDisplayName(this string? str)
    {
        if (str == null || str.Length == 0)
        {
            return string.Empty;
        }

        int lastUnderscore = str.LastIndexOf('_');
        if (lastUnderscore >= 0 && lastUnderscore < str.Length - 1)
        {
            str = str.Substring(lastUnderscore + 1);
        }

        var sb = new StringBuilder();
        for (int i = 0; i < str.Length; i++)
        {
            char c = str[i];
            if (i > 0 && char.IsUpper(c) && !char.IsUpper(str[i - 1]))
            {
                sb.Append(' ');
            }

            sb.Append(c);
        }

        return sb.ToString().ToTitleCase();
    }

    /// <summary>Converts a display name to proto ID format (snake_case).</summary>
    public static string ToIdFormat(this string? str)
    {
        if (str == null || str.Length == 0)
        {
            return string.Empty;
        }

        return str.ToSnakeCase();
    }

    #endregion

    #region RPG Number Formatting (int extensions)

    /// <summary>Converts an integer to Roman numeral (1-3999).</summary>
    public static string ToRomanNumeral(this int number)
    {
        if (number < 1 || number > 3999)
        {
            return number.ToString();
        }

        var roman = new[]
        {
            ("M", 1000), ("CM", 900), ("D", 500), ("CD", 400),
            ("C", 100), ("XC", 90), ("L", 50), ("XL", 40),
            ("X", 10), ("IX", 9), ("V", 5), ("IV", 4), ("I", 1)
        };

        var sb = new StringBuilder();
        foreach (var (symbol, value) in roman)
        {
            while (number >= value)
            {
                sb.Append(symbol);
                number -= value;
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Converts an integer to ordinal string.
    /// 1 → "1st", 2 → "2nd", 3 → "3rd", 11 → "11th"
    /// </summary>
    public static string ToOrdinal(this int number)
    {
        if (number <= 0)
        {
            return number.ToString();
        }

        int mod100 = number % 100;
        if (mod100 == 11 || mod100 == 12 || mod100 == 13)
        {
            return $"{number}th";
        }

        return (number % 10) switch
        {
            1 => $"{number}st",
            2 => $"{number}nd",
            3 => $"{number}rd",
            _ => $"{number}th"
        };
    }

    #endregion

    #region Wrapping

    /// <summary>Wraps text in quotes.</summary>
    public static string WrapInQuotes(this string? text) =>
        text == null || text.Length == 0 ? "\"\"" : $"\"{text}\"";

    /// <summary>Wraps text in square brackets.</summary>
    public static string WrapInBrackets(this string? text) =>
        text == null || text.Length == 0 ? "[]" : $"[{text}]";

    /// <summary>Wraps text in parentheses.</summary>
    public static string WrapInParens(this string? text) =>
        text == null || text.Length == 0 ? "()" : $"({text})";

    /// <summary>Formats as hotkey label for UI. "Q" → "[Q]", "LeftMouse" → "[LMB]".</summary>
    public static string AsHotkeyLabel(this string? text)
    {
        if (text == null || text.Length == 0)
        {
            return "[]";
        }

        text = text.ToUpperInvariant();
        text = text.Replace("LEFTMOUSE", "LMB")
                   .Replace("RIGHTMOUSE", "RMB")
                   .Replace("MIDDLEMOUSE", "MMB")
                   .Replace("MOUSE", "M");
        return $"[{text}]";
    }

    #endregion
}
