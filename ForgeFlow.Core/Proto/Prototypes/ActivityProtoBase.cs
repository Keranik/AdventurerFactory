namespace ForgeFlow.Core.Proto.Prototypes;

/// <summary>
/// Abstract proto base for structures that accept entities (villagers/workers) inside,
/// perform a time-based activity on them, and release them.
/// Mirrors <see cref="ForgeFlow.Core.Entities.ActivityEntity"/> in the Logic layer.
/// InnProto, VillageSpawnerProto, TrainingBuildingProto, DungeonPortalProto,
/// AppearanceWorkshopProto, JobChangerProto, ToolStationProto, ArmoryProto,
/// and AcademyProto inherit from this.
/// </summary>
public abstract class ActivityProtoBase : StructureProtoBase
{
    /// <summary>Maximum number of entities that can be inside at once.</summary>
    public int MaxOccupants { get; set; } = 4;
}
