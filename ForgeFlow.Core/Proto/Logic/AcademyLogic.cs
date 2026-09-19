using ForgeFlow.Core.Entities;
using ForgeFlow.Core.Proto.Prototypes;

namespace ForgeFlow.Core.Proto.Logic;

/// <summary>
/// Academy — converts a citizen into a researcher or other advanced profession.
/// Workers must meet a minimum level before they can be trained.
/// </summary>
public sealed class AcademyLogic : ActivityEntity
{
    public int RequiredLevel { get; set; } = 2;
    public float TrainingDuration { get; set; } = 8.0f;
    public WorkerProfession OutputProfession { get; set; } = WorkerProfession.Researcher;

    public AcademyLogic(EntityId id) : base(id)
    {
        ProcessingDuration = 8.0f;
    }

    public override void InitializeFromProto(StructureProtoBase proto)
    {
        base.InitializeFromProto(proto);
        if (proto is AcademyProto academyProto)
        {
            RequiredLevel = academyProto.RequiredLevel;
            TrainingDuration = academyProto.TrainingDuration;
            OutputProfession = academyProto.OutputProfession;
        }
    }

    public override void Tick(float deltaTime)
    {
        if (!IsActive) return;
    }

    /// <summary>
    /// Checks if a worker meets the requirements for academy training.
    /// </summary>
    public bool CanTrain(HeroEntity worker)
    {
        return worker.Level >= RequiredLevel;
    }

    /// <summary>
    /// Trains the worker into the output profession.
    /// Returns the number of abilities lost from job swap.
    /// </summary>
    public int TrainWorker(HeroEntity worker)
    {
        return worker.SwapProfession(OutputProfession);
    }
}
