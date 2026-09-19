using ForgeFlow.Core.Entities;

namespace ForgeFlow.Core.Proto.Prototypes;

/// <summary>Category of an item in the unified item system.</summary>
public enum ItemCategory
{
    Equipment,
    Tool,
    Resource,
    Currency,
    Consumable
}

/// <summary>
/// Equipment-specific stats for items with Category == Equipment.
/// </summary>
public sealed class EquipmentData
{
    public EquipSlot Slot { get; set; }
    public MaterialType Material { get; set; }
    public DamageType DamageType { get; set; }
    public float BaseDamage { get; set; }
    public float BaseSpeed { get; set; }
    public float CritChance { get; set; }
    public float BaseDefense { get; set; }
    public string SpecialEffectId { get; set; } = string.Empty;
    public string? SetId { get; set; }
    public ItemVisualData Visual { get; set; } = new();
}

/// <summary>
/// Tool-specific stats for items with Category == Tool.
/// </summary>
public sealed class ToolData
{
    public float MaxDurability { get; set; } = 100f;
}

/// <summary>
/// Visual data for items (equipment, tools, etc.).
/// </summary>
public sealed class ItemVisualData
{
    public string BladeShape { get; set; } = string.Empty;
    public string GripMaterial { get; set; } = string.Empty;
    public List<string> ParticleEmitters { get; set; } = new();
}

/// <summary>
/// Unified prototype for every item in the game: equipment, tools, resources,
/// currencies, and consumables. JSON-driven, moddable. Extends ProtoBase.
/// Virtual items (gold, mana) are flagged with IsVirtual and exist only as
/// stock counts — never as physical ItemInstance objects on the map.
/// </summary>
public sealed class ItemProto : ProtoBase, IVirtualItem
{
    public ItemCategory Category { get; set; }
    public int Tier { get; set; }
    public int StackLimit { get; set; } = 99;

    /// <summary>
    /// Virtual items (gold, mana, etc.) are tracked as stock counts only.
    /// They never spawn as physical instances on the map.
    /// </summary>
    public bool IsVirtual { get; set; }

    /// <summary>Equipment stats. Non-null only for Category == Equipment.</summary>
    public EquipmentData? Equipment { get; set; }

    /// <summary>Tool stats. Non-null only for Category == Tool.</summary>
    public ToolData? Tool { get; set; }

    // --- IVirtualItem ---
    bool IVirtualItem.IsVirtual => IsVirtual;
}
