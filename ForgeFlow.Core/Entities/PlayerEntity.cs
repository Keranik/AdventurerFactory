namespace ForgeFlow.Core.Entities;

/// <summary>
/// Abstract base for player-controlled living entities.
/// Heroes and future controllable characters inherit from this.
/// Distinguished from <see cref="NPCEntity"/> which covers autonomous NPCs (villagers).
/// </summary>
public abstract class PlayerEntity : LivingEntity
{
    protected PlayerEntity(EntityId id) : base(id) { }
}
