namespace ForgeFlow.Core.Proto;

/// <summary>
/// Implemented by entities that support being returned to a clean initial state
/// for object pooling or re-use. Reset() clears all mutable runtime state while
/// preserving the entity's identity (Id, ProtoId).
/// </summary>
public interface IResettable
{
    void Reset();
}
