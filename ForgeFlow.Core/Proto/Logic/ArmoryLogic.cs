using ForgeFlow.Core.Entities;
using ForgeFlow.Core.Proto.Prototypes;

namespace ForgeFlow.Core.Proto.Logic;

/// <summary>
/// Armory — re-equips workers with gear and restores stamina.
/// Workers with WearOutReason.StaminaDepleted or needing equipment come here.
/// </summary>
public sealed class ArmoryLogic : ActivityEntity
{
    public float StaminaRestoreRate { get; set; } = 20f;

    public ArmoryLogic(EntityId id) : base(id)
    {
        ProcessingDuration = 4.0f;
    }

    public override void InitializeFromProto(StructureProtoBase proto)
    {
        base.InitializeFromProto(proto);
        if (proto is ArmoryProto armoryProto)
        {
            StaminaRestoreRate = armoryProto.StaminaRestoreRate;
        }
    }

    public override void Tick(float deltaTime)
    {
        if (!IsActive) return;
    }

    /// <summary>Restores a worker's stamina and clears carry load.</summary>
    public bool RestoreWorker(HeroEntity worker)
    {
        worker.Stamina = worker.MaxStamina;
        worker.CarryLoad = 0f;
        worker.LastWearOutReason = WearOutReason.None;
        return true;
    }
}
