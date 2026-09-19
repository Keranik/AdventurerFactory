using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using ForgeFlow.Core.Data;
using ForgeFlow.Core.Entities;
using ForgeFlow.Core.Proto.Prototypes;

namespace ForgeFlow.Core.Proto;

/// <summary>
/// Central registry for all Proto definitions. Loads from JSON at bootstrap.
/// Supports mod overwrite (last registration wins for duplicate IDs).
/// </summary>
public sealed class ProtoRegistry : IRegistry
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        Converters = { new JsonStringEnumConverter(), new StructureProtoConverter() }
    };

    private static readonly Assembly CoreAssembly = typeof(ProtoRegistry).Assembly;

    private readonly Dictionary<string, StructureProtoBase> _structures = new();
    private readonly Dictionary<string, ResourceNodeProto> _resourceNodes = new();
    private readonly Dictionary<string, PathSegmentProto> _pathSegments = new();
    private readonly Dictionary<string, VillagerProto> _villagers = new();
    private readonly Dictionary<string, TutorialMissionProto> _tutorials = new();
    private readonly Dictionary<string, TerrainBiomeProto> _biomes = new();
    private readonly Dictionary<string, PathGateProto> _pathGates = new();

    // --- Structure Protos ---

    public void RegisterStructure(StructureProtoBase proto) => _structures[proto.Id] = proto;
    public StructureProtoBase? GetStructure(string id) => _structures.TryGetValue(id, out var p) ? p : null;
    public IEnumerable<StructureProtoBase> GetAllStructures() => _structures.Values;

    public bool TryGetStructure(string id, [NotNullWhen(true)] out StructureProtoBase? proto)
        => _structures.TryGetValue(id, out proto);

    // --- Resource Node Protos ---

    public void RegisterResourceNode(ResourceNodeProto proto) => _resourceNodes[proto.Id] = proto;
    public ResourceNodeProto? GetResourceNode(string id) => _resourceNodes.TryGetValue(id, out var p) ? p : null;
    public IEnumerable<ResourceNodeProto> GetAllResourceNodes() => _resourceNodes.Values;

    // --- Path Segment Protos ---

    public void RegisterPathSegment(PathSegmentProto proto) => _pathSegments[proto.Id] = proto;
    public PathSegmentProto? GetPathSegment(string id) => _pathSegments.TryGetValue(id, out var p) ? p : null;
    public IEnumerable<PathSegmentProto> GetAllPathSegments() => _pathSegments.Values;

    // --- Villager Protos ---

    public void RegisterVillager(VillagerProto proto) => _villagers[proto.Id] = proto;
    public VillagerProto? GetVillager(string id) => _villagers.TryGetValue(id, out var p) ? p : null;
    public IEnumerable<VillagerProto> GetAllVillagers() => _villagers.Values;

    // --- Tutorial Protos ---

    public void RegisterTutorial(TutorialMissionProto proto) => _tutorials[proto.Id] = proto;
    public TutorialMissionProto? GetTutorial(string id) => _tutorials.TryGetValue(id, out var p) ? p : null;
    public IEnumerable<TutorialMissionProto> GetAllTutorials() => _tutorials.Values;

    // --- Biome Protos ---

    public void RegisterBiome(TerrainBiomeProto proto) => _biomes[proto.Id] = proto;
    public TerrainBiomeProto? GetBiome(string id) => _biomes.TryGetValue(id, out var p) ? p : null;
    public IEnumerable<TerrainBiomeProto> GetAllBiomes() => _biomes.Values;

    // --- PathGate Protos ---

    public void RegisterPathGate(PathGateProto proto) => _pathGates[proto.Id] = proto;
    public PathGateProto? GetPathGate(string id) => _pathGates.TryGetValue(id, out var p) ? p : null;
    public IEnumerable<PathGateProto> GetAllPathGates() => _pathGates.Values;

    // --- Embedded JSON Loading ---

    public void LoadAllFromEmbeddedResources()
    {
        LoadEmbedded<StructureProtoBase>("ForgeFlow.Core.Data.Structures.json",
            p => RegisterStructure(p));
        LoadEmbedded<ResourceNodeProto>("ForgeFlow.Core.Data.ResourceNodes.json",
            p => RegisterResourceNode(p));
        LoadEmbedded<PathSegmentProto>("ForgeFlow.Core.Data.PathSegments.json",
            p => RegisterPathSegment(p));
        LoadEmbedded<VillagerProto>("ForgeFlow.Core.Data.Villagers.json",
            p => RegisterVillager(p));
        LoadEmbedded<TutorialMissionProto>("ForgeFlow.Core.Data.Tutorials.json",
            p => RegisterTutorial(p));
        LoadEmbedded<TerrainBiomeProto>("ForgeFlow.Core.Data.TerrainBiomes.json",
            p => RegisterBiome(p));
        LoadEmbedded<PathGateProto>("ForgeFlow.Core.Data.PathGates.json",
            p => RegisterPathGate(p));
    }

    /// <summary>
    /// Registers minimal safety-net fallbacks. All real game data comes from
    /// embedded JSON files via <see cref="LoadAllFromEmbeddedResources"/>.
    /// If no data has been loaded yet (e.g. in test setups that skip the
    /// explicit JSON load), this method loads from embedded resources first.
    /// </summary>
    public void RegisterDefaults()
    {
        // Safety net: if nothing was loaded yet, pull from embedded JSON.
        if (Count == 0)
        {
            LoadAllFromEmbeddedResources();
        }

        // Absolute minimal fallback: one basic villager proto so the
        // simulation can always boot even if Villagers.json is missing.
        if (_villagers.Count == 0)
        {
            RegisterVillager(new VillagerProto
            {
                Id = "villager_basic",
                DisplayName = "Villager",
                BaseWorkRate = 1.0f,
                BaseMovementSpeed = 2.0f,
                BaseStamina = 100
            });
        }
    }

    // --- JSON Merge (for mods) ---

    public void MergeStructures(string filePath) =>
        MergeFromFile<GatheringProtoBase>(filePath, p => RegisterStructure(p));

    public void MergeResourceNodes(string filePath) =>
        MergeFromFile<ResourceNodeProto>(filePath, p => RegisterResourceNode(p));

    public void MergeTutorials(string filePath) =>
        MergeFromFile<TutorialMissionProto>(filePath, p => RegisterTutorial(p));

    // --- Internal helpers ---

    private static void LoadEmbedded<T>(string resourceName, Action<T> register)
    {
        using var stream = CoreAssembly.GetManifestResourceStream(resourceName);
        if (stream == null) return; // Not all embedded resources are required

        var items = JsonSerializer.Deserialize<List<T>>(stream, JsonOptions);
        if (items == null) return;

        foreach (var item in items)
        {
            register(item);
        }
    }

    private static void MergeFromFile<T>(string filePath, Action<T> register)
    {
        if (!File.Exists(filePath)) return;

        var json = File.ReadAllText(filePath);
        var items = JsonSerializer.Deserialize<List<T>>(json, JsonOptions);
        if (items == null) return;

        foreach (var item in items)
        {
            register(item);
        }
    }

    // --- Counts ---

    public int StructureCount => _structures.Count;
    public int ResourceNodeCount => _resourceNodes.Count;
    public int PathSegmentCount => _pathSegments.Count;
    public int VillagerCount => _villagers.Count;
    public int TutorialCount => _tutorials.Count;
    public int BiomeCount => _biomes.Count;
    public int PathGateCount => _pathGates.Count;
    public int Count => _structures.Count + _resourceNodes.Count + _pathSegments.Count
        + _villagers.Count + _tutorials.Count + _biomes.Count + _pathGates.Count;

    public void Clear()
    {
        _structures.Clear();
        _resourceNodes.Clear();
        _pathSegments.Clear();
        _villagers.Clear();
        _tutorials.Clear();
        _biomes.Clear();
        _pathGates.Clear();
    }
}
