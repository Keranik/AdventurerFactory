namespace ForgeFlow.Core.Entities;

public sealed class AppearanceTemplate
{
    public string Name { get; set; } = "Default";
    public string BaseBody { get; set; } = "human_default";
    public string Hair { get; set; } = "short_01";
    public string Face { get; set; } = "face_01";
    public string? Helmet { get; set; }
    public string? ChestArmor { get; set; }
    public string? Pauldrons { get; set; }
    public string? Cape { get; set; }
    public string? Boots { get; set; }
    public string? WeaponSheath { get; set; }
    public string? AuraEffect { get; set; }
    public string? ParticleTrail { get; set; }
    public Dictionary<string, string> ColorPalette { get; set; } = new();
    public Dictionary<string, string> MaterialOverrides { get; set; } = new();
}
