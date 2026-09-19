using System.Reflection;
using ForgeFlow.Core.Data;
using ForgeFlow.Core.Events;

namespace ForgeFlow.Core.Modding;

public sealed class ModLoader
{
    private readonly DataLoader _dataLoader;
    private readonly EventBus? _eventBus;
    private readonly List<IModExtension> _loadedMods = new();
    private readonly List<string> _loadErrors = new();
    private readonly Data.DataValidationResult _loadReport = new();

    public IReadOnlyList<IModExtension> LoadedMods => _loadedMods;
    public IReadOnlyList<string> LoadErrors => _loadErrors;

    /// <summary>
    /// Accumulated validation report from all loaded mods.
    /// Contains errors (skipped data) and warnings (duplicates, missing fields).
    /// </summary>
    public Data.DataValidationResult LoadReport => _loadReport;

    public ModLoader(DataLoader dataLoader, EventBus? eventBus = null)
    {
        _dataLoader = dataLoader;
        _eventBus = eventBus;
    }

    public void LoadAllMods(string modsDirectory)
    {
        if (!Directory.Exists(modsDirectory)) return;

        var modFolders = Directory.GetDirectories(modsDirectory);
        foreach (var folder in modFolders)
        {
            LoadMod(folder);
        }

        // Publish a summary event so the HUD / debug panel can display issues (#14).
        _eventBus?.Publish(new ModLoadReportEvent(
            _loadedMods.Count,
            _loadErrors.Count + _loadReport.Errors.Count,
            _loadReport.Warnings.Count));
    }

    public void LoadMod(string modPath)
    {
        try
        {
            // Merge JSON data files (last mod wins for duplicate IDs)
            MergeJsonFiles(modPath);

            // Load DLL extensions if present
            LoadExtensionDlls(modPath);
        }
        catch (Exception ex)
        {
            _loadErrors.Add($"Failed to load mod at '{modPath}': {ex.Message}");
        }
    }

    private void MergeJsonFiles(string modPath)
    {
        var itemsPath = Path.Combine(modPath, "Items.json");
        if (File.Exists(itemsPath))
        {
            _loadReport.Merge(_dataLoader.MergeItems(itemsPath));
        }

        var classesPath = Path.Combine(modPath, "Classes.json");
        if (File.Exists(classesPath))
        {
            _loadReport.Merge(_dataLoader.MergeClasses(classesPath));
        }

        var recipesPath = Path.Combine(modPath, "Recipes.json");
        if (File.Exists(recipesPath))
        {
            _loadReport.Merge(_dataLoader.MergeRecipes(recipesPath));
        }

        var dungeonsPath = Path.Combine(modPath, "Dungeons.json");
        if (File.Exists(dungeonsPath))
        {
            _loadReport.Merge(_dataLoader.MergeDungeons(dungeonsPath));
        }
    }

    private void LoadExtensionDlls(string modPath)
    {
        var dllFiles = Directory.GetFiles(modPath, "*.dll");
        foreach (var dllFile in dllFiles)
        {
            try
            {
                var assembly = Assembly.LoadFrom(dllFile);
                var modTypes = assembly.GetTypes();

                foreach (var type in modTypes)
                {
                    if (typeof(IModExtension).IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract)
                    {
                        if (Activator.CreateInstance(type) is IModExtension modExtension)
                        {
                            modExtension.OnLoad();
                            _loadedMods.Add(modExtension);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _loadErrors.Add($"Failed to load DLL '{dllFile}': {ex.Message}");
            }
        }
    }

    public void UnloadAllMods()
    {
        foreach (var mod in _loadedMods)
        {
            try
            {
                mod.OnUnload();
            }
            catch (Exception ex)
            {
                _loadErrors.Add($"Error unloading mod '{mod.ModId}': {ex.Message}");
            }
        }
        _loadedMods.Clear();
    }
}
