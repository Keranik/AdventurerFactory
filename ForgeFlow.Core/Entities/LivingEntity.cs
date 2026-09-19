namespace ForgeFlow.Core.Entities;

/// <summary>
/// Abstract base for all living, moving entities (heroes, villagers, NPCs, player character).
/// Extracts shared movement and vitality properties common to
/// <see cref="HeroEntity"/> and <see cref="Proto.Logic.VillagerLogic"/>.
/// </summary>
public abstract class LivingEntity : EntityBase
{
    protected LivingEntity(EntityId id) : base(id) { }

    /// <summary>Base movement speed along paths (tiles per second).</summary>
    public float MovementSpeed { get; set; } = 2.0f;

    /// <summary>Current stamina. Drains during work/combat; restored at rest stations.</summary>
    public float Stamina { get; set; } = 100f;

    /// <summary>Maximum stamina capacity.</summary>
    public float MaxStamina { get; set; } = 100f;

    /// <summary>ID of the path segment this entity is currently traversing, or null if not on a path.</summary>
    public ulong? CurrentPathSegmentId { get; set; }

    /// <summary>Progress along the current path segment (0.0 → 1.0).</summary>
    public float PathProgress { get; set; }

    /// <summary>Current level of the entity. Increases through training, dungeons, or experience.</summary>
    public int Level { get; set; } = 1;

    /// <summary>Permanent traits earned through gameplay (buffs, quirks, etc.).</summary>
    public List<string> Traits { get; } = new();
}
