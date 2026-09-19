namespace ForgeFlow.Core.Entities;

/// <summary>
/// Abstract base for NPC entities — non-player characters that move along paths,
/// perform work, and interact with buildings.
/// Villagers and future enemy entities inherit from this.
/// Distinguished from <see cref="PlayerEntity"/> which covers player-controlled entities.
/// </summary>
public abstract class NPCEntity : LivingEntity
{
    protected NPCEntity(EntityId id) : base(id) { }
}
