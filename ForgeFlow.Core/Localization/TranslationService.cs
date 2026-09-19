using System.Globalization;
using System.Reflection;
using System.Text.Json;

namespace ForgeFlow.Core.Localization;

/// <summary>
/// Core-side localization/translation service.
/// Loads language tables from embedded JSON resources.
/// Supports per-key fallback to "en", culture-aware formatting,
/// typed overloads to avoid params allocation, and basic plural forms.
/// Pure .NET — no Unity dependency.
/// </summary>
public sealed class TranslationService
{
    private static readonly Assembly CoreAssembly = typeof(TranslationService).Assembly;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    private readonly Dictionary<string, Dictionary<string, string>> _languages = new();
    private Dictionary<string, string> _activeTable = new();
    private Dictionary<string, string> _fallbackTable = new();
    private string _activeLanguage = "en";
    private CultureInfo _activeCulture = CultureInfo.InvariantCulture;

    public string ActiveLanguage => _activeLanguage;
    public CultureInfo ActiveCulture => _activeCulture;
    public int KeyCount => _activeTable.Count;

    /// <summary>Registers a full language table.</summary>
    public void RegisterLanguage(string languageCode, Dictionary<string, string> table)
    {
        _languages[languageCode] = new Dictionary<string, string>(table);
    }

    /// <summary>Sets the active language. Falls back to "en" if missing.</summary>
    public bool SetLanguage(string languageCode)
    {
        if (_languages.TryGetValue(languageCode, out var table))
        {
            _activeTable = table;
            _activeLanguage = languageCode;
            try { _activeCulture = new CultureInfo(languageCode); }
            catch { _activeCulture = CultureInfo.InvariantCulture; }

            if (languageCode != "en" && _languages.TryGetValue("en", out var enTable))
            {
                _fallbackTable = enTable;
            }
            else
            {
                _fallbackTable = _activeTable;
            }
            return true;
        }

        if (_languages.TryGetValue("en", out var fallback))
        {
            _activeTable = fallback;
            _fallbackTable = fallback;
            _activeLanguage = "en";
            _activeCulture = CultureInfo.InvariantCulture;
        }
        return false;
    }

    /// <summary>Gets a translated string by key. Falls back to "en", then returns the key itself.</summary>
    public string Get(string key)
    {
        if (_activeTable.TryGetValue(key, out var value))
        {
            return value;
        }
        if (_fallbackTable.TryGetValue(key, out var fallbackValue))
        {
            return fallbackValue;
        }
        return key;
    }

    /// <summary>Gets a translated string with formatting arguments (allocates params array).</summary>
    public string GetFormatted(string key, params object[] args)
    {
        var template = Get(key);
        try
        {
            return string.Format(_activeCulture, template, args);
        }
        catch (FormatException)
        {
            return template;
        }
    }

    /// <summary>Gets a translated string with one format argument (zero-alloc overload).</summary>
    public string GetFormatted(string key, object arg0)
    {
        var template = Get(key);
        try
        {
            return string.Format(_activeCulture, template, arg0);
        }
        catch (FormatException)
        {
            return template;
        }
    }

    /// <summary>Gets a translated string with two format arguments (zero-alloc overload).</summary>
    public string GetFormatted(string key, object arg0, object arg1)
    {
        var template = Get(key);
        try
        {
            return string.Format(_activeCulture, template, arg0, arg1);
        }
        catch (FormatException)
        {
            return template;
        }
    }

    /// <summary>
    /// Basic plural support. Checks for "{key}_zero", "{key}_one", "{key}_other"
    /// suffixed keys and returns the appropriate form. Falls back to Get(key) with count.
    /// </summary>
    public string GetPlural(string key, int count)
    {
        string suffixedKey;
        if (count == 0)
        {
            suffixedKey = key + "_zero";
            if (HasKey(suffixedKey)) { return Get(suffixedKey); }
        }
        else if (count == 1)
        {
            suffixedKey = key + "_one";
            if (HasKey(suffixedKey)) { return Get(suffixedKey); }
        }

        suffixedKey = key + "_other";
        if (HasKey(suffixedKey))
        {
            return string.Format(_activeCulture, Get(suffixedKey), count);
        }

        return string.Format(_activeCulture, Get(key), count);
    }

    /// <summary>Overrides a single key in the active language.</summary>
    public void Override(string key, string value)
    {
        _activeTable[key] = value;
    }

    /// <summary>Overrides multiple keys in the active language (used by mods).</summary>
    public void OverrideMany(Dictionary<string, string> overrides)
    {
        foreach (var kvp in overrides)
        {
            _activeTable[kvp.Key] = kvp.Value;
        }
    }

    /// <summary>Whether a key exists in the active table or fallback.</summary>
    public bool HasKey(string key) =>
        _activeTable.ContainsKey(key) || _fallbackTable.ContainsKey(key);

    /// <summary>Returns all available language codes.</summary>
    public IEnumerable<string> AvailableLanguages => _languages.Keys;

    /// <summary>
    /// Loads the default English language from the embedded en.json resource,
    /// registers it, and sets it as the active language.
    /// </summary>
    public void LoadFromEmbeddedJson()
    {
        var table = LoadEmbeddedLanguage("ForgeFlow.Core.Data.Localization.en.json");
        if (table != null)
        {
            RegisterLanguage("en", table);
        }
        SetLanguage("en");
    }

    /// <summary>
    /// Loads an additional language from an embedded JSON resource.
    /// Resource name follows: ForgeFlow.Core.Data.Localization.{languageCode}.json
    /// </summary>
    public bool LoadLanguageFromEmbeddedJson(string languageCode)
    {
        var resourceName = $"ForgeFlow.Core.Data.Localization.{languageCode}.json";
        var table = LoadEmbeddedLanguage(resourceName);
        if (table == null) { return false; }
        RegisterLanguage(languageCode, table);
        return true;
    }

    /// <summary>
    /// Loads a language table from a JSON stream (for mod support).
    /// </summary>
    public bool LoadLanguageFromStream(string languageCode, Stream jsonStream)
    {
        try
        {
            using var reader = new StreamReader(jsonStream);
            var json = reader.ReadToEnd();
            var table = JsonSerializer.Deserialize<Dictionary<string, string>>(json, JsonOptions);
            if (table == null) { return false; }
            RegisterLanguage(languageCode, table);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static Dictionary<string, string>? LoadEmbeddedLanguage(string resourceName)
    {
        using var stream = CoreAssembly.GetManifestResourceStream(resourceName);
        if (stream == null) { return null; }

        using var reader = new StreamReader(stream);
        var json = reader.ReadToEnd();
        return JsonSerializer.Deserialize<Dictionary<string, string>>(json, JsonOptions);
    }
}
