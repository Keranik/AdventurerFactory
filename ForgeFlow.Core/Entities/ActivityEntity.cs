namespace ForgeFlow.Core.Entities;

/// <summary>
/// Abstract base for structures that accept entities (villagers/workers) inside,
/// perform a time-based activity on them, and release them.
/// Inn, TrainingBuilding, Academy, ToolStation, Armory, JobChanger,
/// VillageSpawner (residence), and DungeonPortal inherit from this.
/// </summary>
public abstract class ActivityEntity : Structure
{
    protected ActivityEntity(EntityId id) : base(id) { }

    // ── Activity state ──

    /// <summary>Maximum number of entities that can be inside at once.</summary>
    public int MaxOccupants { get; set; } = 4;

    /// <summary>IDs of entities currently inside this building.</summary>
    public List<EntityId> CurrentOccupantIds { get; } = new();

    /// <summary>Number of entities currently inside.</summary>
    public int OccupantCount => CurrentOccupantIds.Count;

    /// <summary>Whether the structure has room for another entity.</summary>
    public bool CanAcceptOccupant => OccupantCount < MaxOccupants;
}
