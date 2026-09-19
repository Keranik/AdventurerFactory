using ForgeFlow.Core.Entities;

namespace ForgeFlow.Core.Proto.Prototypes;

/// <summary>
/// Proto for an academy. Defines required level, training duration, and output profession.
/// </summary>
public sealed class AcademyProto : ActivityProtoBase
{
    public int RequiredLevel { get; set; } = 2;
    public float TrainingDuration { get; set; } = 8.0f;
    public WorkerProfession OutputProfession { get; set; } = WorkerProfession.Researcher;
}
