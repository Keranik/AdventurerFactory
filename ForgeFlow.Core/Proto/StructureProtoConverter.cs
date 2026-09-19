using System.Text.Json;
using System.Text.Json.Serialization;
using ForgeFlow.Core.Proto.Prototypes;

namespace ForgeFlow.Core.Proto;

/// <summary>
/// Polymorphic JSON converter for <see cref="StructureProtoBase"/>.
/// Uses the <c>StructureType</c> JSON field as a string discriminator to instantiate
/// the correct concrete Proto type during deserialization.
/// </summary>
internal sealed class StructureProtoConverter : JsonConverter<StructureProtoBase>
{
    public override StructureProtoBase? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;
        var rawText = root.GetRawText();

        if (!root.TryGetProperty("StructureType", out var mtElement))
        {
            return JsonSerializer.Deserialize<StructureProtoBase>(rawText, FallbackOptions);
        }

        var mt = mtElement.GetString();

        return mt switch
        {
            "Spawner" or "VillageSpawner"
                => JsonSerializer.Deserialize<VillageSpawnerProto>(rawText, FallbackOptions),
            "Forestry"
                => JsonSerializer.Deserialize<ForestryRecipeEntityProto>(rawText, FallbackOptions),
            "MiningNode"
                => JsonSerializer.Deserialize<MiningRecipeEntityProto>(rawText, FallbackOptions),
            "Inn"
                => JsonSerializer.Deserialize<InnProto>(rawText, FallbackOptions),
            "CraftStation"
                => JsonSerializer.Deserialize<CraftStationProto>(rawText, FallbackOptions),
            "Stockpile"
                => JsonSerializer.Deserialize<StockpileProto>(rawText, FallbackOptions),
            "TrainingBuilding"
                => JsonSerializer.Deserialize<TrainingBuildingProto>(rawText, FallbackOptions),
            "Forge"
                => JsonSerializer.Deserialize<ForgeProto>(rawText, FallbackOptions),
            "DungeonPortal"
                => JsonSerializer.Deserialize<DungeonPortalProto>(rawText, FallbackOptions),
            "FusionAltar"
                => JsonSerializer.Deserialize<FusionAltarProto>(rawText, FallbackOptions),
            "AppearanceWorkshop"
                => JsonSerializer.Deserialize<AppearanceWorkshopProto>(rawText, FallbackOptions),
            "ToolStation"
                => JsonSerializer.Deserialize<ToolStationProto>(rawText, FallbackOptions),
            "Armory"
                => JsonSerializer.Deserialize<ArmoryProto>(rawText, FallbackOptions),
            "JobChanger"
                => JsonSerializer.Deserialize<JobChangerProto>(rawText, FallbackOptions),
            "Academy"
                => JsonSerializer.Deserialize<AcademyProto>(rawText, FallbackOptions),
            "CheckGate"
                => JsonSerializer.Deserialize<CheckGateProto>(rawText, FallbackOptions),
            "FilterSplitter"
                => JsonSerializer.Deserialize<FilterSplitterProto>(rawText, FallbackOptions),
            "Balancer"
                => JsonSerializer.Deserialize<BalancerProto>(rawText, FallbackOptions),
            _ => JsonSerializer.Deserialize<StructureProtoBase>(rawText, FallbackOptions)
        };
    }

    public override void Write(Utf8JsonWriter writer, StructureProtoBase value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value, value.GetType(), FallbackOptions);
    }

    /// <summary>
    /// Options without this converter to avoid infinite recursion during
    /// sub-deserialization of concrete types.
    /// </summary>
    private static readonly JsonSerializerOptions FallbackOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        Converters = { new JsonStringEnumConverter() }
    };
}
