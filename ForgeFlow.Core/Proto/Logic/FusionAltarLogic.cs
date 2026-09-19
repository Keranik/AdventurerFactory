using ForgeFlow.Core.Entities;
using ForgeFlow.Core.Proto.Prototypes;

namespace ForgeFlow.Core.Proto.Logic;

/// <summary>
/// Fusion altar logic. Queues heroes/items for fusion;
/// actual processing is handled by FusionCalculator.
/// </summary>
public sealed class FusionAltarLogic : RecipeEntity
{
    public List<ulong> QueuedHeroIds { get; } = new();
    public List<ItemInstance> QueuedItems { get; } = new();
    public int RequiredInputCount { get; set; } = 2;
    public bool IsReadyToFuse => QueuedHeroIds.Count >= RequiredInputCount
                                  || QueuedItems.Count >= RequiredInputCount;

    public FusionAltarLogic(EntityId id) : base(id)
    {
        ProcessingDuration = 5.0f;
    }

    public override void InitializeFromProto(StructureProtoBase proto)
    {
        base.InitializeFromProto(proto);
        if (proto is FusionAltarProto fp)
        {
            RequiredInputCount = fp.RequiredInputCount;
        }
    }

    public void QueueHero(ulong heroId)
    {
        QueuedHeroIds.Add(heroId);
    }

    public void QueueItem(ItemInstance item)
    {
        QueuedItems.Add(item);
    }

    public override void Tick(float deltaTime)
    {
        // Fusion processing is handled by FusionCalculator system.
    }
}
