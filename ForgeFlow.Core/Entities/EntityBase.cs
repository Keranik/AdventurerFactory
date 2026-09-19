using ForgeFlow.Core.Utilities;

namespace ForgeFlow.Core.Entities;

/// <summary>
/// Root base class for all headless entity logic instances.
/// Contains no Unity references — runs on servers, in tests, and in console builds.
/// </summary>
public abstract class EntityBase
{
    public EntityId Id { get; }
    public string ProtoId { get; set; } = string.Empty;
    public GridPosRPG Position { get; set; }
    public bool IsActive { get; set; } = true;

    protected EntityBase(EntityId id)
    {
        Id = id;
    }

    /// <summary>Per-frame fixed-step tick. Override in subclasses.</summary>
    public abstract void Tick(float deltaTime);

    /// <summary>Resets the shared ID counter. Only for use in tests.</summary>
    public static void ResetIdCounter() => EntityIdFactory.ResetForTesting();
}
