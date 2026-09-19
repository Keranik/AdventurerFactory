using System.Reflection;
using ForgeFlow.Core.Localization;

namespace ForgeFlow.Tests;

/// <summary>
/// Validates that every const key in LocalizationKeys has a matching
/// entry in the embedded en.json language file, and vice versa.
/// </summary>
public class LocalizationValidationTests
{
    private static HashSet<string> GetAllLocalizationKeyValues()
    {
        var fields = typeof(LocalizationKeys)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.IsLiteral && f.FieldType == typeof(string));

        var keys = new HashSet<string>();
        foreach (var field in fields)
        {
            var value = (string)field.GetRawConstantValue()!;
            keys.Add(value);
        }
        return keys;
    }

    private static HashSet<string> GetAllRegisteredEnglishKeys()
    {
        var svc = new TranslationService();
        svc.LoadFromEmbeddedJson();

        // Use reflection to read the active table keys
        var activeTableField = typeof(TranslationService)
            .GetField("_activeTable", BindingFlags.NonPublic | BindingFlags.Instance);
        var table = (Dictionary<string, string>)activeTableField!.GetValue(svc)!;
        return new HashSet<string>(table.Keys);
    }

    [Fact]
    public void AllFields_AreConst()
    {
        var fields = typeof(LocalizationKeys)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.FieldType == typeof(string));

        foreach (var field in fields)
        {
            Assert.True(field.IsLiteral,
                $"LocalizationKeys.{field.Name} should be const, not static readonly");
        }
    }

    [Fact]
    public void AllLocalizationKeys_HaveTranslation()
    {
        var keys = GetAllLocalizationKeyValues();
        var registered = GetAllRegisteredEnglishKeys();

        var missing = keys.Except(registered).OrderBy(k => k).ToList();

        Assert.True(missing.Count == 0,
            $"LocalizationKeys constants missing from en.json:\n" +
            string.Join("\n", missing));
    }

    [Fact]
    public void AllTranslations_HaveLocalizationKey()
    {
        var keys = GetAllLocalizationKeyValues();
        var registered = GetAllRegisteredEnglishKeys();

        // en.json may contain extra keys (tutorial steps, structure names, etc.)
        // that don't need a LocalizationKeys constant. Only validate the ui.* and
        // core event keys that panels reference directly.
        var coreKeys = registered.Where(k =>
            k.StartsWith("ui.") || k.StartsWith("hero.") || k.StartsWith("dungeon.") ||
            k.StartsWith("research.") || k.StartsWith("prestige.") || k.StartsWith("cataclysm.") ||
            k.StartsWith("villager.") || k.StartsWith("worker.")).ToHashSet();

        var orphaned = coreKeys.Except(keys).OrderBy(k => k).ToList();

        Assert.True(orphaned.Count == 0,
            $"en.json has core entries with no matching LocalizationKeys constant:\n" +
            string.Join("\n", orphaned));
    }

    [Fact]
    public void NoDuplicateKeyValues()
    {
        var fields = typeof(LocalizationKeys)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.IsLiteral && f.FieldType == typeof(string))
            .ToList();

        var seen = new Dictionary<string, string>();
        var duplicates = new List<string>();

        foreach (var field in fields)
        {
            var value = (string)field.GetRawConstantValue()!;
            if (seen.TryGetValue(value, out var existingField))
            {
                duplicates.Add($"{field.Name} and {existingField} both map to \"{value}\"");
            }
            else
            {
                seen[value] = field.Name;
            }
        }

        Assert.True(duplicates.Count == 0,
            $"Duplicate key values found:\n" + string.Join("\n", duplicates));
    }

    [Fact]
    public void LoadFromEmbeddedJson_LoadsAllKeys()
    {
        var svc = new TranslationService();
        svc.LoadFromEmbeddedJson();

        Assert.True(svc.KeyCount > 0, "en.json should load with at least one key");
        Assert.Equal("en", svc.ActiveLanguage);
        Assert.Equal("Adventurer Factory", svc.Get("game.title"));
    }

    [Fact]
    public void PerKeyFallback_WorksForMissingActiveKey()
    {
        var svc = new TranslationService();
        svc.LoadFromEmbeddedJson();

        var fr = new Dictionary<string, string>
        {
            { "game.title", "Usine d'Aventuriers" }
        };
        svc.RegisterLanguage("fr", fr);
        svc.SetLanguage("fr");

        Assert.Equal("Usine d'Aventuriers", svc.Get("game.title"));

        Assert.Equal("New Game", svc.Get("ui.mainmenu.new_game"));
    }
}

/// <summary>Tests for TranslationService core functionality.</summary>
public class TranslationServiceTests
{
    [Fact]
    public void TranslationService_RegisterDefaultEnglish_HasKeys()
    {
        var ts = new TranslationService();
        ts.LoadFromEmbeddedJson();

        Assert.Equal("en", ts.ActiveLanguage);
        Assert.True(ts.KeyCount > 0);
    }

    [Fact]
    public void TranslationService_Get_ReturnsTranslation()
    {
        var ts = new TranslationService();
        ts.LoadFromEmbeddedJson();

        Assert.Equal("Adventurer Factory", ts.Get("game.title"));
    }

    [Fact]
    public void TranslationService_Get_ReturnsKeyIfMissing()
    {
        var ts = new TranslationService();
        ts.LoadFromEmbeddedJson();

        Assert.Equal("nonexistent.key", ts.Get("nonexistent.key"));
    }

    [Fact]
    public void TranslationService_GetFormatted_FormatsArgs()
    {
        var ts = new TranslationService();
        ts.RegisterLanguage("en", new Dictionary<string, string>
        {
            { "greeting", "Hello, {0}! You have {1} gold." }
        });
        ts.SetLanguage("en");

        Assert.Equal("Hello, Player! You have 100 gold.", ts.GetFormatted("greeting", "Player", 100));
    }

    [Fact]
    public void TranslationService_Override_ChangesValue()
    {
        var ts = new TranslationService();
        ts.LoadFromEmbeddedJson();

        ts.Override("game.title", "Custom Title");
        Assert.Equal("Custom Title", ts.Get("game.title"));
    }

    [Fact]
    public void TranslationService_OverrideMany_AppliesAll()
    {
        var ts = new TranslationService();
        ts.LoadFromEmbeddedJson();

        ts.OverrideMany(new Dictionary<string, string>
        {
            { "game.title", "Modded Title" },
            { "custom.key", "Custom Value" }
        });

        Assert.Equal("Modded Title", ts.Get("game.title"));
        Assert.Equal("Custom Value", ts.Get("custom.key"));
    }

    [Fact]
    public void TranslationService_SetLanguage_SwitchesTable()
    {
        var ts = new TranslationService();
        ts.RegisterLanguage("en", new Dictionary<string, string> { { "hello", "Hello" } });
        ts.RegisterLanguage("es", new Dictionary<string, string> { { "hello", "Hola" } });

        ts.SetLanguage("en");
        Assert.Equal("Hello", ts.Get("hello"));

        ts.SetLanguage("es");
        Assert.Equal("Hola", ts.Get("hello"));
    }

    [Fact]
    public void TranslationService_SetLanguage_FallsBackToEnglish()
    {
        var ts = new TranslationService();
        ts.RegisterLanguage("en", new Dictionary<string, string> { { "hello", "Hello" } });

        bool result = ts.SetLanguage("nonexistent");
        Assert.False(result);
        Assert.Equal("en", ts.ActiveLanguage);
    }

    [Fact]
    public void TranslationService_HasKey()
    {
        var ts = new TranslationService();
        ts.LoadFromEmbeddedJson();

        Assert.True(ts.HasKey("game.title"));
        Assert.False(ts.HasKey("nonexistent"));
    }

    [Fact]
    public void TranslationService_AvailableLanguages()
    {
        var ts = new TranslationService();
        ts.RegisterLanguage("en", new Dictionary<string, string>());
        ts.RegisterLanguage("de", new Dictionary<string, string>());
        ts.RegisterLanguage("ja", new Dictionary<string, string>());

        var languages = ts.AvailableLanguages.ToList();
        Assert.Contains("en", languages);
        Assert.Contains("de", languages);
        Assert.Contains("ja", languages);
    }

    [Fact]
    public void TranslationService_HasAllPhase10Keys()
    {
        var ts = new TranslationService();
        ts.LoadFromEmbeddedJson();

        Assert.True(ts.HasKey(LocalizationKeys.WorkerWornOut));
        Assert.True(ts.HasKey(LocalizationKeys.WorkerToolBroken));
        Assert.True(ts.HasKey(LocalizationKeys.GuildName));
        Assert.True(ts.HasKey(LocalizationKeys.GuildGold));
        Assert.True(ts.HasKey(LocalizationKeys.CheckGateTitle));
        Assert.True(ts.HasKey(LocalizationKeys.FilterSplitterTitle));
        Assert.True(ts.HasKey(LocalizationKeys.BalancerTitle));
        Assert.True(ts.HasKey(LocalizationKeys.ToolStationTitle));
        Assert.True(ts.HasKey(LocalizationKeys.ArmoryTitle));
        Assert.True(ts.HasKey(LocalizationKeys.JobChangerTitle));
        Assert.True(ts.HasKey(LocalizationKeys.AcademyTitle));
        Assert.True(ts.HasKey(LocalizationKeys.GoldInsufficient));
        Assert.True(ts.HasKey(LocalizationKeys.GoldReward));
        Assert.True(ts.HasKey(LocalizationKeys.BuildingCost));
    }

    [Fact]
    public void Phase11_LocalizationKeys_AllExist()
    {
        var ts = new TranslationService();
        ts.LoadFromEmbeddedJson();

        Assert.True(ts.HasKey(LocalizationKeys.WorkerInspectorTitle));
        Assert.True(ts.HasKey(LocalizationKeys.WorkerDurability));
        Assert.True(ts.HasKey(LocalizationKeys.WorkerStamina));
        Assert.True(ts.HasKey(LocalizationKeys.WorkerCarryLoad));
        Assert.True(ts.HasKey(LocalizationKeys.WorkerProfession));
        Assert.True(ts.HasKey(LocalizationKeys.WorkerTraits));
        Assert.True(ts.HasKey(LocalizationKeys.WorkerAbilities));
        Assert.True(ts.HasKey(LocalizationKeys.WorkerWearOutStatus));
        Assert.True(ts.HasKey(LocalizationKeys.WorkerSendToMaintenance));
        Assert.True(ts.HasKey(LocalizationKeys.WorkerStatusSummary));
        Assert.True(ts.HasKey(LocalizationKeys.WorkerWornOutCount));
        Assert.True(ts.HasKey(LocalizationKeys.GuildHudTitle));
        Assert.True(ts.HasKey(LocalizationKeys.GoldDisplay));
        Assert.True(ts.HasKey(LocalizationKeys.AutomationConfigTitle));
        Assert.True(ts.HasKey(LocalizationKeys.AutomationAddRule));
        Assert.True(ts.HasKey(LocalizationKeys.AutomationRemoveRule));
        Assert.True(ts.HasKey(LocalizationKeys.AutomationApply));
        Assert.True(ts.HasKey(LocalizationKeys.NotificationWorkerWornOut));
        Assert.True(ts.HasKey(LocalizationKeys.NotificationAbilityGained));
        Assert.True(ts.HasKey(LocalizationKeys.NotificationGoldEarned));
        Assert.True(ts.HasKey(LocalizationKeys.WorkerNone));
        Assert.True(ts.HasKey(LocalizationKeys.BalancerOutputs));
    }

    [Fact]
    public void Phase11_TranslationService_ReturnsEnglishStrings()
    {
        var ts = new TranslationService();
        ts.LoadFromEmbeddedJson();

        Assert.Equal("Worker Inspector", ts.Get(LocalizationKeys.WorkerInspectorTitle));
        Assert.Equal("Tool Durability", ts.Get(LocalizationKeys.WorkerDurability));
        Assert.Equal("Send to Maintenance", ts.Get(LocalizationKeys.WorkerSendToMaintenance));
        Assert.Equal("Gold", ts.Get(LocalizationKeys.GoldDisplay));
        Assert.Equal("Guild", ts.Get(LocalizationKeys.GuildHudTitle));
        Assert.Equal("Automation Config", ts.Get(LocalizationKeys.AutomationConfigTitle));
        Assert.Equal("Add Rule", ts.Get(LocalizationKeys.AutomationAddRule));
    }
}
