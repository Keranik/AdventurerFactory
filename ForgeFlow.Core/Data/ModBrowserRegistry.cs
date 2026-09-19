namespace ForgeFlow.Core.Data;

/// <summary>
/// Registry tracking discovered / loaded mods for the in-game mod browser.
/// Stores metadata about each mod so the Presentation layer can display
/// a browsable list without touching the filesystem directly.
/// </summary>
public sealed class ModBrowserRegistry : IRegistry
{
    private readonly Dictionary<string, ModBrowserEntry> _entries = new();

    public void Register(ModBrowserEntry entry)
    {
        _entries[entry.ModName] = entry;
    }

    public void Remove(string modName)
    {
        _entries.Remove(modName);
    }

    public ModBrowserEntry? GetEntry(string modName)
    {
        return _entries.TryGetValue(modName, out var entry) ? entry : null;
    }

    public IEnumerable<ModBrowserEntry> GetAllEntries() => _entries.Values;

    public int Count => _entries.Count;

    public void Clear() => _entries.Clear();
}

/// <summary>
/// Metadata for a single discovered mod.
/// </summary>
public sealed class ModBrowserEntry
{
    public string ModName { get; set; } = string.Empty;
    public string FolderPath { get; set; } = string.Empty;
    public bool IsLoaded { get; set; }
    public bool HasItems { get; set; }
    public bool HasClasses { get; set; }
    public bool HasRecipes { get; set; }
    public bool HasDungeons { get; set; }
    public bool HasDlls { get; set; }
    public List<string> LoadErrors { get; set; } = new();
}
