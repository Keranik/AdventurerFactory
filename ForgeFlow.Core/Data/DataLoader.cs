using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using ForgeFlow.Core.Data.Definitions;
using ForgeFlow.Core.Entities;
using ForgeFlow.Core.Proto;
using ForgeFlow.Core.Proto.Prototypes;

namespace ForgeFlow.Core.Data;

public sealed class DataLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private static readonly Assembly CoreAssembly = typeof(DataLoader).Assembly;

    private readonly ItemRegistry _itemRegistry;
    private readonly ClassRegistry _classRegistry;
    private readonly RecipeRegistry _recipeRegistry;
    private readonly DungeonRegistry _dungeonRegistry;

    public DataLoader(
        ItemRegistry itemRegistry,
        ClassRegistry classRegistry,
        RecipeRegistry recipeRegistry,
        DungeonRegistry dungeonRegistry)
    {
        _itemRegistry = itemRegistry;
        _classRegistry = classRegistry;
        _recipeRegistry = recipeRegistry;
        _dungeonRegistry = dungeonRegistry;
    }

    public void LoadAllFromEmbeddedResources()
    {
        LoadEmbeddedItems("ForgeFlow.Core.Data.Items.json");
        LoadEmbedded<ClassDefinition>("ForgeFlow.Core.Data.Classes.json", d => _classRegistry.Register(d));
        LoadEmbedded<RecipeDefinition>("ForgeFlow.Core.Data.Recipes.json", d => _recipeRegistry.Register(d));
        LoadEmbedded<DungeonDefinition>("ForgeFlow.Core.Data.Dungeons.json", d => _dungeonRegistry.Register(d));
    }

    /// <summary>
    /// Clears all registries and reloads from embedded JSON resources.
    /// Does NOT refresh existing runtime entity state — only prototype definitions.
    /// </summary>
    public void ReloadAllFromEmbeddedResources()
    {
        _itemRegistry.Clear();
        _classRegistry.Clear();
        _recipeRegistry.Clear();
        _dungeonRegistry.Clear();

        LoadAllFromEmbeddedResources();
    }

    private void LoadEmbeddedItems(string resourceName)
    {
        using var stream = CoreAssembly.GetManifestResourceStream(resourceName);
        if (stream == null)
        {
            throw new InvalidOperationException($"Embedded resource '{resourceName}' not found in {CoreAssembly.GetName().Name}.");
        }

        var dtos = JsonSerializer.Deserialize<List<ItemJsonDto>>(stream, JsonOptions);
        if (dtos == null) { return; }

        foreach (var dto in dtos)
        {
            _itemRegistry.Register(dto.ToProto());
        }
    }

    private static void LoadEmbedded<T>(string resourceName, Action<T> register)
    {
        using var stream = CoreAssembly.GetManifestResourceStream(resourceName);
        if (stream == null)
        {
            throw new InvalidOperationException($"Embedded resource '{resourceName}' not found in {CoreAssembly.GetName().Name}.");
        }

        var items = JsonSerializer.Deserialize<List<T>>(stream, JsonOptions);
        if (items == null) { return; }

        foreach (var item in items)
        {
            register(item);
        }
    }

    public DataValidationResult MergeItems(string filePath)
    {
        var result = new DataValidationResult();
        if (!File.Exists(filePath)) { return result; }

        List<ItemJsonDto>? dtos;
        try
        {
            var json = File.ReadAllText(filePath);
            dtos = JsonSerializer.Deserialize<List<ItemJsonDto>>(json, JsonOptions);
        }
        catch (JsonException ex)
        {
            result.AddError($"[{filePath}] Invalid JSON: {ex.Message}");
            return result;
        }

        if (dtos == null) { return result; }

        foreach (var dto in dtos)
        {
            if (!DataValidator.ValidateId(dto.Id, "Item", filePath, result))
            {
                continue;
            }
            if (_itemRegistry.Get(dto.Id) != null)
            {
                DataValidator.WarnDuplicate(dto.Id, "Item", filePath, result);
            }
            _itemRegistry.Register(dto.ToProto());
        }
        return result;
    }

    public DataValidationResult MergeClasses(string filePath)
    {
        return MergeFromFileValidated<ClassDefinition>(filePath, "Class",
            d => d.Id, d => _classRegistry.Get(d.Id) != null, d => _classRegistry.Register(d));
    }

    public DataValidationResult MergeRecipes(string filePath)
    {
        return MergeFromFileValidated<RecipeDefinition>(filePath, "Recipe",
            d => d.Id, d => _recipeRegistry.Get(d.Id) != null, d => _recipeRegistry.Register(d));
    }

    public DataValidationResult MergeDungeons(string filePath)
    {
        return MergeFromFileValidated<DungeonDefinition>(filePath, "Dungeon",
            d => d.Id, d => _dungeonRegistry.Get(d.Id) != null, d => _dungeonRegistry.Register(d));
    }

    private static DataValidationResult MergeFromFileValidated<T>(
        string filePath,
        string typeName,
        Func<T, string> getId,
        Func<T, bool> existsCheck,
        Action<T> register)
    {
        var result = new DataValidationResult();
        if (!File.Exists(filePath)) { return result; }

        List<T>? items;
        try
        {
            var json = File.ReadAllText(filePath);
            items = JsonSerializer.Deserialize<List<T>>(json, JsonOptions);
        }
        catch (JsonException ex)
        {
            result.AddError($"[{filePath}] Invalid JSON: {ex.Message}");
            return result;
        }

        if (items == null) { return result; }

        foreach (var item in items)
        {
            var id = getId(item);
            if (!DataValidator.ValidateId(id, typeName, filePath, result))
            {
                continue;
            }
            if (existsCheck(item))
            {
                DataValidator.WarnDuplicate(id, typeName, filePath, result);
            }
            register(item);
        }
        return result;
    }

    /// <summary>
    /// Internal DTO that mirrors the existing Items.json shape.
    /// Converts to the unified ItemProto on load.
    /// </summary>
    private sealed class ItemJsonDto
    {
        public string Id { get; set; } = string.Empty;
        public int Tier { get; set; }
        public string Material { get; set; } = string.Empty;
        public string DamageType { get; set; } = string.Empty;
        public string Slot { get; set; } = string.Empty;
        public float BaseDamage { get; set; }
        public float BaseSpeed { get; set; }
        public float CritChance { get; set; }
        public float BaseDefense { get; set; }
        public string SpecialEffectId { get; set; } = string.Empty;
        public string? SetId { get; set; }
        public ItemVisualDto? Visual { get; set; }

        // Optional unified fields (for future / mod JSON that includes them)
        public string? Category { get; set; }
        public int? StackLimit { get; set; }
        public bool IsVirtual { get; set; }
        public float? MaxDurability { get; set; }
        public string? Name { get; set; }
        public string? DisplayName { get; set; }
        public string? Description { get; set; }

        public ItemProto ToProto()
        {
            var category = ResolveCategory();
            var proto = new ItemProto
            {
                Id = Id,
                DisplayName = DisplayName ?? Name ?? Id,
                Description = Description ?? string.Empty,
                Category = category,
                Tier = Tier,
                StackLimit = StackLimit ?? (category == ItemCategory.Resource || category == ItemCategory.Currency ? 9999 : 1),
                IsVirtual = IsVirtual
            };

            if (category == ItemCategory.Equipment)
            {
                proto.Equipment = new EquipmentData
                {
                    Slot = Enum.TryParse<EquipSlot>(Slot, true, out var slot) ? slot : EquipSlot.Weapon,
                    Material = Enum.TryParse<MaterialType>(Material, true, out var mat) ? mat : MaterialType.Iron,
                    DamageType = Enum.TryParse<DamageType>(DamageType, true, out var dmg) ? dmg : Entities.DamageType.Slashing,
                    BaseDamage = BaseDamage,
                    BaseSpeed = BaseSpeed,
                    CritChance = CritChance,
                    BaseDefense = BaseDefense,
                    SpecialEffectId = SpecialEffectId ?? string.Empty,
                    SetId = SetId,
                    Visual = Visual != null
                        ? new ItemVisualData
                        {
                            BladeShape = Visual.BladeShape ?? string.Empty,
                            GripMaterial = Visual.GripMaterial ?? string.Empty,
                            ParticleEmitters = Visual.ParticleEmitters ?? new List<string>()
                        }
                        : new ItemVisualData()
                };
            }
            else if (category == ItemCategory.Tool)
            {
                proto.Tool = new ToolData
                {
                    MaxDurability = MaxDurability ?? 100f
                };
            }

            return proto;
        }

        private ItemCategory ResolveCategory()
        {
            if (!string.IsNullOrEmpty(Category))
            {
                return Enum.TryParse<ItemCategory>(Category, true, out var cat) ? cat : ItemCategory.Equipment;
            }

            if (IsVirtual) { return ItemCategory.Currency; }
            if (MaxDurability.HasValue) { return ItemCategory.Tool; }
            if (!string.IsNullOrEmpty(Slot)) { return ItemCategory.Equipment; }
            return ItemCategory.Resource;
        }
    }

    private sealed class ItemVisualDto
    {
        public string? BladeShape { get; set; }
        public string? GripMaterial { get; set; }
        public List<string>? ParticleEmitters { get; set; }
    }
}
