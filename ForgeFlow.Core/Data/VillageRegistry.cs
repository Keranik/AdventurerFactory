using System.Diagnostics.CodeAnalysis;
using ForgeFlow.Core.Data.Definitions;

namespace ForgeFlow.Core.Data;

/// <summary>
/// Registry for village buildings. Stores all building definitions
/// and the runtime state of built buildings (position, assigned heroes).
/// </summary>
public sealed class VillageRegistry : IRegistry
{
    private readonly Dictionary<string, VillageBuildingDefinition> _definitions = new();
    private readonly Dictionary<string, VillageBuildingState> _builtBuildings = new();

    // --- Definitions ---

    public void RegisterBuilding(VillageBuildingDefinition definition)
    {
        _definitions[definition.Id] = definition;
    }

    public VillageBuildingDefinition? GetDefinition(string id)
    {
        return _definitions.TryGetValue(id, out var def) ? def : null;
    }

    public bool TryGetDefinition(string id, [NotNullWhen(true)] out VillageBuildingDefinition? definition)
    {
        return _definitions.TryGetValue(id, out definition);
    }

    public IEnumerable<VillageBuildingDefinition> GetAllDefinitions() => _definitions.Values;

    public IEnumerable<VillageBuildingDefinition> GetBuildingsByCategory(string category)
    {
        foreach (var def in _definitions.Values)
        {
            if (def.Category == category) yield return def;
        }
    }

    // --- Built buildings (runtime state) ---

    public bool BuildBuilding(string definitionId)
    {
        if (!_definitions.TryGetValue(definitionId, out var def)) return false;
        if (_builtBuildings.ContainsKey(definitionId)) return false;

        _builtBuildings[definitionId] = new VillageBuildingState
        {
            DefinitionId = definitionId,
            AssignedHeroSeeds = new List<ulong>(),
            IsActive = true
        };
        return true;
    }

    public bool AssignHero(string buildingId, ulong heroSeed)
    {
        if (!_builtBuildings.TryGetValue(buildingId, out var state)) return false;
        if (!_definitions.TryGetValue(buildingId, out var def)) return false;
        if (state.AssignedHeroSeeds.Count >= def.MaxAssignedHeroes) return false;
        if (state.AssignedHeroSeeds.Contains(heroSeed)) return false;

        state.AssignedHeroSeeds.Add(heroSeed);
        return true;
    }

    public bool RemoveHero(string buildingId, ulong heroSeed)
    {
        if (!_builtBuildings.TryGetValue(buildingId, out var state)) return false;
        return state.AssignedHeroSeeds.Remove(heroSeed);
    }

    public VillageBuildingState? GetBuildingState(string buildingId)
    {
        return _builtBuildings.TryGetValue(buildingId, out var state) ? state : null;
    }

    public IEnumerable<VillageBuildingState> GetAllBuiltBuildings() => _builtBuildings.Values;

    public int BuiltCount => _builtBuildings.Count;
    public int DefinitionCount => _definitions.Count;
    public int Count => _definitions.Count;

    /// <summary>
    /// Calculates the aggregate passive bonus value across all built buildings
    /// for a given bonus key (e.g. "spawn_rate", "research_speed").
    /// </summary>
    public float GetTotalBonus(string bonusKey)
    {
        float total = 0f;
        foreach (var state in _builtBuildings.Values)
        {
            if (!state.IsActive) continue;
            if (_definitions.TryGetValue(state.DefinitionId, out var def) &&
                def.PassiveBonuses.TryGetValue(bonusKey, out var bonus))
            {
                // Bonus scales with number of assigned heroes
                total += bonus * (1f + state.AssignedHeroSeeds.Count * 0.25f);
            }
        }
        return total;
    }

    public void RegisterDefaults()
    {
        RegisterBuilding(new VillageBuildingDefinition
        {
            Id = "tavern",
            DisplayName = "Tavern",
            Category = "production",
            MaxAssignedHeroes = 3,
            PassiveBonuses = new() { { "morale_regen", 0.05f }, { "spawn_rate", 0.1f } },
            BuildCost = new() { { "gold", 100 } }
        });
        RegisterBuilding(new VillageBuildingDefinition
        {
            Id = "training_grounds",
            DisplayName = "Training Grounds",
            Category = "training",
            MaxAssignedHeroes = 5,
            RequiredTier = 2,
            PassiveBonuses = new() { { "hero_xp", 0.15f }, { "stat_boost", 0.05f } },
            BuildCost = new() { { "gold", 200 }, { "iron", 50 } }
        });
        RegisterBuilding(new VillageBuildingDefinition
        {
            Id = "library",
            DisplayName = "Library",
            Category = "research",
            MaxAssignedHeroes = 2,
            RequiredTier = 2,
            PassiveBonuses = new() { { "research_speed", 0.2f } },
            BuildCost = new() { { "gold", 300 } }
        });
        RegisterBuilding(new VillageBuildingDefinition
        {
            Id = "smithy",
            DisplayName = "Village Smithy",
            Category = "production",
            MaxAssignedHeroes = 4,
            RequiredTier = 3,
            PassiveBonuses = new() { { "forge_speed", 0.15f }, { "gear_quality", 0.1f } },
            BuildCost = new() { { "gold", 250 }, { "iron", 100 }, { "steel", 25 } }
        });
        RegisterBuilding(new VillageBuildingDefinition
        {
            Id = "monument",
            DisplayName = "Hero Monument",
            Category = "decoration",
            MaxAssignedHeroes = 1,
            RequiredTier = 4,
            PassiveBonuses = new() { { "morale_regen", 0.2f } },
            BuildCost = new() { { "gold", 500 }, { "steel", 50 } }
        });
    }

    public void Clear()
    {
        _definitions.Clear();
        _builtBuildings.Clear();
    }
}

/// <summary>
/// Runtime state of a single built village building.
/// </summary>
public sealed class VillageBuildingState
{
    public string DefinitionId { get; set; } = string.Empty;
    public List<ulong> AssignedHeroSeeds { get; set; } = new();
    public bool IsActive { get; set; } = true;
}
