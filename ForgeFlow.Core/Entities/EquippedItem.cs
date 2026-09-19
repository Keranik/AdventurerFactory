namespace ForgeFlow.Core.Entities;

public sealed class EquippedItem
{
    public string ProtoId { get; set; } = string.Empty;
    public EquipSlot Slot { get; set; }
    public int Tier { get; set; }
    public float Damage { get; set; }
    public float Defense { get; set; }
    public float Speed { get; set; }
    public float CritChance { get; set; }
    public string? SpecialEffectId { get; set; }

    public EquippedItem Clone()
    {
        return new EquippedItem
        {
            ProtoId = ProtoId,
            Slot = Slot,
            Tier = Tier,
            Damage = Damage,
            Defense = Defense,
            Speed = Speed,
            CritChance = CritChance,
            SpecialEffectId = SpecialEffectId
        };
    }
}
