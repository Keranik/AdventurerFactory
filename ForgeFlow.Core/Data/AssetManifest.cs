using System.Diagnostics.CodeAnalysis;

namespace ForgeFlow.Core.Data;

/// <summary>
/// Describes a single visual/audio asset that Presentation must load.
/// Core references assets by string key; Presentation resolves the
/// actual resource path at runtime. Moddable via JSON.
/// </summary>
public sealed class AssetEntry
{
    /// <summary>Unique key (e.g. "sprite_warrior_idle", "prefab_spawner").</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Human-readable name (for editors/debug).</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Asset type discriminator.</summary>
    public AssetType Type { get; set; }

    /// <summary>Relative resource path that Presentation resolves.</summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>Optional tags for filtering (e.g. "tier1", "warrior").</summary>
    public List<string> Tags { get; set; } = new();
}

/// <summary>
/// Discriminator for asset types in the manifest.
/// </summary>
public enum AssetType
{
    Sprite,
    Prefab,
    Material,
    Animation,
    Particle,
    Texture,
    AudioClip,
    Font,
    UIDocument,
    Other
}

/// <summary>
/// Central manifest of all assets the game expects to exist.
/// Core systems reference assets by string key; Presentation resolves
/// them to engine-specific resources. Moddable — mods can register
/// additional entries or override existing ones.
/// </summary>
public sealed class AssetManifest : IRegistry
{
    private readonly Dictionary<string, AssetEntry> _entries = new();

    public void Register(AssetEntry entry)
    {
        _entries[entry.Id] = entry;
    }

    public AssetEntry? Get(string id)
    {
        return _entries.TryGetValue(id, out var entry) ? entry : null;
    }

    public bool TryGet(string id, [NotNullWhen(true)] out AssetEntry? entry)
    {
        return _entries.TryGetValue(id, out entry);
    }

    public IEnumerable<AssetEntry> GetAll() => _entries.Values;

    public IEnumerable<AssetEntry> GetByType(AssetType type)
    {
        foreach (var entry in _entries.Values)
        {
            if (entry.Type == type)
            {
                yield return entry;
            }
        }
    }

    public IEnumerable<AssetEntry> GetByTag(string tag)
    {
        foreach (var entry in _entries.Values)
        {
            if (entry.Tags.Contains(tag))
            {
                yield return entry;
            }
        }
    }

    public int Count => _entries.Count;
    public void Clear() => _entries.Clear();

    /// <summary>Registers hard-coded default asset manifest entries as fallback.</summary>
    public void RegisterDefaults()
    {
        // Structure sprites
        Register(new AssetEntry { Id = "sprite_spawner", DisplayName = "Spawner Sprite", Type = AssetType.Sprite, Path = "Sprites/Structures/spawner", Tags = { "structure", "spawner" } });
        Register(new AssetEntry { Id = "sprite_forge", DisplayName = "Forge Sprite", Type = AssetType.Sprite, Path = "Sprites/Structures/forge", Tags = { "structure", "forge" } });
        Register(new AssetEntry { Id = "sprite_forestry", DisplayName = "Forestry Sprite", Type = AssetType.Sprite, Path = "Sprites/Structures/forestry", Tags = { "structure", "gathering" } });
        Register(new AssetEntry { Id = "sprite_mining", DisplayName = "Mining Sprite", Type = AssetType.Sprite, Path = "Sprites/Structures/mining", Tags = { "structure", "gathering" } });
        Register(new AssetEntry { Id = "sprite_inn", DisplayName = "Inn Sprite", Type = AssetType.Sprite, Path = "Sprites/Structures/inn", Tags = { "structure", "service" } });
        Register(new AssetEntry { Id = "sprite_craft_station", DisplayName = "Craft Station Sprite", Type = AssetType.Sprite, Path = "Sprites/Structures/craft_station", Tags = { "structure", "production" } });
        Register(new AssetEntry { Id = "sprite_stockpile", DisplayName = "Stockpile Sprite", Type = AssetType.Sprite, Path = "Sprites/Structures/stockpile", Tags = { "structure", "storage" } });
        Register(new AssetEntry { Id = "sprite_dungeon_portal", DisplayName = "Dungeon Portal Sprite", Type = AssetType.Sprite, Path = "Sprites/Structures/dungeon_portal", Tags = { "structure", "dungeon" } });
        Register(new AssetEntry { Id = "sprite_fusion_altar", DisplayName = "Fusion Altar Sprite", Type = AssetType.Sprite, Path = "Sprites/Structures/fusion_altar", Tags = { "structure", "crafting" } });
        Register(new AssetEntry { Id = "sprite_training_building", DisplayName = "Training Building Sprite", Type = AssetType.Sprite, Path = "Sprites/Structures/training_building", Tags = { "structure", "training" } });
        Register(new AssetEntry { Id = "sprite_village_spawner", DisplayName = "Village Spawner Sprite", Type = AssetType.Sprite, Path = "Sprites/Structures/village_spawner", Tags = { "structure", "spawner" } });

        // Path / PathGate
        Register(new AssetEntry { Id = "sprite_path_segment", DisplayName = "Path Segment", Type = AssetType.Sprite, Path = "Sprites/Paths/path_segment", Tags = { "path" } });
        Register(new AssetEntry { Id = "sprite_pathgate", DisplayName = "PathGate", Type = AssetType.Sprite, Path = "Sprites/Paths/pathgate", Tags = { "path", "gate" } });

        // Villager / Hero
        Register(new AssetEntry { Id = "prefab_villager", DisplayName = "Villager Prefab", Type = AssetType.Prefab, Path = "Prefabs/Villager", Tags = { "character", "villager" } });
        Register(new AssetEntry { Id = "prefab_hero", DisplayName = "Hero Prefab", Type = AssetType.Prefab, Path = "Prefabs/Hero", Tags = { "character", "hero" } });

        // Particles
        Register(new AssetEntry { Id = "particle_spawn", DisplayName = "Spawn Particle", Type = AssetType.Particle, Path = "Particles/spawn_burst", Tags = { "fx", "spawn" } });
        Register(new AssetEntry { Id = "particle_levelup", DisplayName = "Level Up Particle", Type = AssetType.Particle, Path = "Particles/levelup_burst", Tags = { "fx", "levelup" } });
        Register(new AssetEntry { Id = "particle_death", DisplayName = "Death Particle", Type = AssetType.Particle, Path = "Particles/death_poof", Tags = { "fx", "death" } });

        // UI
        Register(new AssetEntry { Id = "sprite_gold_icon", DisplayName = "Gold Icon", Type = AssetType.Sprite, Path = "Sprites/UI/gold_icon", Tags = { "ui", "icon" } });
        Register(new AssetEntry { Id = "sprite_heart_icon", DisplayName = "Heart Icon", Type = AssetType.Sprite, Path = "Sprites/UI/heart_icon", Tags = { "ui", "icon" } });
        Register(new AssetEntry { Id = "sprite_star_icon", DisplayName = "Star Icon", Type = AssetType.Sprite, Path = "Sprites/UI/star_icon", Tags = { "ui", "icon" } });
    }
}
