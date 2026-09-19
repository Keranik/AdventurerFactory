using System.Diagnostics.CodeAnalysis;

namespace ForgeFlow.Core.Data;

/// <summary>
/// Defines a sound effect or music track that can be referenced by string key.
/// Pure data — no audio clips or engine types. Presentation maps these
/// entries to actual audio resources at runtime.
/// </summary>
public sealed class SoundEntry
{
    /// <summary>Unique key used by game logic to request a sound (e.g. "sfx_hero_spawn").</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Human-readable display name (for editors/debug).</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Relative asset path or resource name that Presentation resolves to an audio clip.</summary>
    public string AssetPath { get; set; } = string.Empty;

    /// <summary>Default volume (0.0–1.0). Presentation can override per user settings.</summary>
    public float DefaultVolume { get; set; } = 1.0f;

    /// <summary>Whether this entry represents background music (vs. a one-shot SFX).</summary>
    public bool IsMusic { get; set; }

    /// <summary>Category for filtering (e.g. "ui", "combat", "ambient", "music").</summary>
    public string Category { get; set; } = string.Empty;
}

/// <summary>
/// Pure data catalog of all known sound effects and music tracks.
/// Core systems reference sounds by string key; Presentation maps them
/// to engine-specific audio clips. Moddable via JSON.
/// </summary>
public sealed class SoundDb : IRegistry
{
    private readonly Dictionary<string, SoundEntry> _entries = new();

    public void Register(SoundEntry entry)
    {
        _entries[entry.Id] = entry;
    }

    public SoundEntry? Get(string id)
    {
        return _entries.TryGetValue(id, out var entry) ? entry : null;
    }

    public bool TryGet(string id, [NotNullWhen(true)] out SoundEntry? entry)
    {
        return _entries.TryGetValue(id, out entry);
    }

    public IEnumerable<SoundEntry> GetAll() => _entries.Values;

    public IEnumerable<SoundEntry> GetByCategory(string category)
    {
        foreach (var entry in _entries.Values)
        {
            if (entry.Category == category)
            {
                yield return entry;
            }
        }
    }

    public IEnumerable<SoundEntry> GetMusic()
    {
        foreach (var entry in _entries.Values)
        {
            if (entry.IsMusic)
            {
                yield return entry;
            }
        }
    }

    public IEnumerable<SoundEntry> GetSfx()
    {
        foreach (var entry in _entries.Values)
        {
            if (!entry.IsMusic)
            {
                yield return entry;
            }
        }
    }

    public int Count => _entries.Count;
    public void Clear() => _entries.Clear();

    /// <summary>Registers hard-coded default sound entries as fallback.</summary>
    public void RegisterDefaults()
    {
        // UI
        Register(new SoundEntry { Id = "sfx_button_click", DisplayName = "Button Click", Category = "ui", AssetPath = "Audio/SFX/ui_click" });
        Register(new SoundEntry { Id = "sfx_button_hover", DisplayName = "Button Hover", Category = "ui", AssetPath = "Audio/SFX/ui_hover", DefaultVolume = 0.5f });
        Register(new SoundEntry { Id = "sfx_panel_open", DisplayName = "Panel Open", Category = "ui", AssetPath = "Audio/SFX/ui_panel_open" });
        Register(new SoundEntry { Id = "sfx_panel_close", DisplayName = "Panel Close", Category = "ui", AssetPath = "Audio/SFX/ui_panel_close" });
        Register(new SoundEntry { Id = "sfx_notification", DisplayName = "Notification", Category = "ui", AssetPath = "Audio/SFX/ui_notification" });

        // Gameplay
        Register(new SoundEntry { Id = "sfx_hero_spawn", DisplayName = "Hero Spawn", Category = "gameplay", AssetPath = "Audio/SFX/hero_spawn" });
        Register(new SoundEntry { Id = "sfx_hero_death", DisplayName = "Hero Death", Category = "gameplay", AssetPath = "Audio/SFX/hero_death" });
        Register(new SoundEntry { Id = "sfx_hero_levelup", DisplayName = "Hero Level Up", Category = "gameplay", AssetPath = "Audio/SFX/hero_levelup" });
        Register(new SoundEntry { Id = "sfx_villager_spawn", DisplayName = "Villager Spawn", Category = "gameplay", AssetPath = "Audio/SFX/villager_spawn" });

        // Building
        Register(new SoundEntry { Id = "sfx_structure_place", DisplayName = "Structure Place", Category = "building", AssetPath = "Audio/SFX/structure_place" });
        Register(new SoundEntry { Id = "sfx_structure_remove", DisplayName = "Structure Remove", Category = "building", AssetPath = "Audio/SFX/structure_remove" });
        Register(new SoundEntry { Id = "sfx_path_build", DisplayName = "Path Build", Category = "building", AssetPath = "Audio/SFX/path_build" });
        Register(new SoundEntry { Id = "sfx_path_remove", DisplayName = "Path Remove", Category = "building", AssetPath = "Audio/SFX/path_remove" });
        Register(new SoundEntry { Id = "sfx_pathgate_place", DisplayName = "PathGate Place", Category = "building", AssetPath = "Audio/SFX/pathgate_place" });

        // Combat / Dungeon
        Register(new SoundEntry { Id = "sfx_dungeon_enter", DisplayName = "Dungeon Enter", Category = "combat", AssetPath = "Audio/SFX/dungeon_enter" });
        Register(new SoundEntry { Id = "sfx_dungeon_victory", DisplayName = "Dungeon Victory", Category = "combat", AssetPath = "Audio/SFX/dungeon_victory" });
        Register(new SoundEntry { Id = "sfx_dungeon_defeat", DisplayName = "Dungeon Defeat", Category = "combat", AssetPath = "Audio/SFX/dungeon_defeat" });

        // Crafting
        Register(new SoundEntry { Id = "sfx_forge_complete", DisplayName = "Forge Complete", Category = "crafting", AssetPath = "Audio/SFX/forge_complete" });
        Register(new SoundEntry { Id = "sfx_fusion_complete", DisplayName = "Fusion Complete", Category = "crafting", AssetPath = "Audio/SFX/fusion_complete" });
        Register(new SoundEntry { Id = "sfx_research_unlock", DisplayName = "Research Unlock", Category = "crafting", AssetPath = "Audio/SFX/research_unlock" });

        // Gathering
        Register(new SoundEntry { Id = "sfx_chop_wood", DisplayName = "Chop Wood", Category = "gathering", AssetPath = "Audio/SFX/chop_wood" });
        Register(new SoundEntry { Id = "sfx_mine_stone", DisplayName = "Mine Stone", Category = "gathering", AssetPath = "Audio/SFX/mine_stone" });
        Register(new SoundEntry { Id = "sfx_gather_herb", DisplayName = "Gather Herb", Category = "gathering", AssetPath = "Audio/SFX/gather_herb" });

        // Tutorial
        Register(new SoundEntry { Id = "sfx_tutorial_step", DisplayName = "Tutorial Step", Category = "tutorial", AssetPath = "Audio/SFX/tutorial_step" });
        Register(new SoundEntry { Id = "sfx_tutorial_complete", DisplayName = "Tutorial Complete", Category = "tutorial", AssetPath = "Audio/SFX/tutorial_complete" });
        Register(new SoundEntry { Id = "sfx_celebration", DisplayName = "Celebration", Category = "tutorial", AssetPath = "Audio/SFX/celebration" });

        // Ambient / Music
        Register(new SoundEntry { Id = "music_main_menu", DisplayName = "Main Menu Theme", Category = "music", AssetPath = "Audio/Music/main_menu", IsMusic = true });
        Register(new SoundEntry { Id = "music_factory", DisplayName = "Factory Theme", Category = "music", AssetPath = "Audio/Music/factory", IsMusic = true });
        Register(new SoundEntry { Id = "music_dungeon", DisplayName = "Dungeon Theme", Category = "music", AssetPath = "Audio/Music/dungeon", IsMusic = true });
        Register(new SoundEntry { Id = "music_victory", DisplayName = "Victory Theme", Category = "music", AssetPath = "Audio/Music/victory", IsMusic = true });
        Register(new SoundEntry { Id = "sfx_ambient_factory", DisplayName = "Factory Ambient", Category = "ambient", AssetPath = "Audio/SFX/ambient_factory", DefaultVolume = 0.3f });
    }
}
