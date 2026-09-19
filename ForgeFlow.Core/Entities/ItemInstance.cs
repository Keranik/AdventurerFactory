using ForgeFlow.Core.Proto;
using ForgeFlow.Core.Utilities;

namespace ForgeFlow.Core.Entities;

/// <summary>
/// Runtime item instance used in structure queues, villager inventories,
/// and equipment drops. Pooled via ItemManager for zero-alloc hot paths.
/// Implements IResettable so the pool can clear mutable state on return.
/// </summary>
public sealed class ItemInstance : IResettable
{
    /// <summary>Unique runtime ID. Assigned by ItemManager on creation, not in constructor.</summary>
    public ulong InstanceId { get; set; }

    /// <summary>References the ItemProto.Id this instance was created from.</summary>
    public string ProtoId { get; set; } = string.Empty;

    public int Tier { get; set; }
    public int Quantity { get; set; } = 1;
    public EquipSlot Slot { get; set; }
    public float Damage { get; set; }
    public float Defense { get; set; }
    public float Speed { get; set; }
    public float CritChance { get; set; }
    public string? SpecialEffectId { get; set; }
    public GridPosRPG Position { get; set; }
    public bool IsOnPath { get; set; }
    public bool IsScrap { get; set; }

    public ItemInstance()
    {
    }

    public EquippedItem ToEquipped()
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

    /// <summary>
    /// Resets all mutable state for object pooling. Zero-alloc.
    /// Called by ItemManager when returning to the pool.
    /// </summary>
    public void Reset()
    {
        InstanceId = 0;
        ProtoId = string.Empty;
        Tier = 0;
        Quantity = 1;
        Slot = default;
        Damage = 0f;
        Defense = 0f;
        Speed = 0f;
        CritChance = 0f;
        SpecialEffectId = null;
        Position = default;
        IsOnPath = false;
        IsScrap = false;
    }

    /// <summary>Resets the shared ID counter. Only for use in tests.</summary>
    public static void ResetIdCounter() => EntityIdFactory.ResetForTesting();
}
