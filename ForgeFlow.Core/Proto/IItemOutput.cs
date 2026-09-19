using ForgeFlow.Core.Entities;

namespace ForgeFlow.Core.Proto;

/// <summary>
/// Interface for structures that produce output items in a queue.
/// Implemented by RecipeEntity and any other structure that holds
/// finished items for hero pickup (e.g. FusionAltarLogic).
/// </summary>
public interface IItemOutput
{
    Queue<ItemInstance> OutputQueue { get; }
}
