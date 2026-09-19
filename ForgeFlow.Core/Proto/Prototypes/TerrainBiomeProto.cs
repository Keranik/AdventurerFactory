namespace ForgeFlow.Core.Proto.Prototypes;

/// <summary>
/// Proto for a terrain biome definition. Defines visual and gameplay properties
/// for a specific biome type. Loaded from JSON.
/// </summary>
public sealed class TerrainBiomeProto : ProtoBase
{
    public BiomeType BiomeType { get; set; } = BiomeType.Plains;
    public float MovementSpeedModifier { get; set; } = 1.0f;
    public float ResourceYieldModifier { get; set; } = 1.0f;
    public float DangerLevel { get; set; }
    public List<string> AvailableResourceNodeIds { get; set; } = new();

    /// <summary>
    /// Color values for visual representation (R, G, B as 0-1 floats).
    /// Used by Presentation layer to tint terrain tiles.
    /// </summary>
    public float ColorR { get; set; } = 0.4f;
    public float ColorG { get; set; } = 0.6f;
    public float ColorB { get; set; } = 0.3f;
}
