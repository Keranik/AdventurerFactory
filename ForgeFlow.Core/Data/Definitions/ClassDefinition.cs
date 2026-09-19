using ForgeFlow.Core.Entities;

namespace ForgeFlow.Core.Data.Definitions;

public sealed class ClassDefinition
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public List<EquipSlot> PreferredGearSlots { get; set; } = new();
    public List<DamageType> PreferredDamageTypes { get; set; } = new();
    public float ClassBonus { get; set; }
    public float BaseHealth { get; set; }
    public float BaseMana { get; set; }
    public int UnlockTier { get; set; }
    public bool IsHybrid { get; set; }
    public List<string> FusionSourceClassIds { get; set; } = new();
}
