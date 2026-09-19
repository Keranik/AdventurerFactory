using ForgeFlow.Core.Entities;
using ForgeFlow.Core.Proto.Prototypes;

namespace ForgeFlow.Core.Proto.Logic;

/// <summary>
/// Tool station — restores tool durability for workers.
/// Workers with WearOutReason.ToolBroken are routed here.
/// </summary>
public sealed class ToolStationLogic : ActivityEntity
{
    public float RepairRate { get; set; } = 25f;
    public float RepairTimer { get; set; }

    public ToolStationLogic(EntityId id) : base(id)
    {
        ProcessingDuration = 3.0f;
    }

    public override void InitializeFromProto(StructureProtoBase proto)
    {
        base.InitializeFromProto(proto);
        if (proto is ToolStationProto toolProto)
        {
            RepairRate = toolProto.RepairRate;
        }
    }

    public override void Tick(float deltaTime)
    {
        if (!IsActive) return;
        RepairTimer += deltaTime;
    }

    /// <summary>Repairs a worker's tools to full. Returns true if repair is complete.</summary>
    public bool RepairWorker(HeroEntity worker)
    {
        worker.ToolDurability = worker.MaxToolDurability;
        worker.LastWearOutReason = WearOutReason.None;
        return true;
    }
}
