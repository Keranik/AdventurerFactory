using ForgeFlow.Core.Entities;
using ForgeFlow.Core.Proto.Prototypes;

namespace ForgeFlow.Core.Proto.Logic;

/// <summary>
/// Runtime logic for a training building. Accepts villagers via walk-by
/// interaction (villager walks on adjacent path and the building pulls
/// them in). Trains them into a class.
/// </summary>
public sealed class TrainingBuildingLogic : ActivityEntity, IEntryGated, IStructureTickHandler
{
    public TrainingBuildingLogic(EntityId id) : base(id) { }

    public VillagerClass OutputClass { get; set; } = VillagerClass.Warrior;
    public float TrainingDuration { get; set; } = 15.0f;
    public int MaxTrainees { get; set; } = 2;
    public List<ulong> CurrentTrainees { get; } = new();

    public int TraineeCount => CurrentTrainees.Count;
    public bool CanAcceptTrainee => CurrentTrainees.Count < MaxTrainees;

    public override void InitializeFromProto(StructureProtoBase proto)
    {
        base.InitializeFromProto(proto);
        if (proto is TrainingBuildingProto tp)
        {
            OutputClass = tp.OutputClass;
            TrainingDuration = tp.TrainingDuration;
            MaxTrainees = tp.MaxTrainees;
        }
    }

    /// <summary>
    /// Checks whether a villager is eligible to enter this training building.
    /// Rejects when full or when the villager already has a trained class.
    /// </summary>
    public EntryCheckResult CheckEntry(VillagerLogic villager)
    {
        if (!CanAcceptTrainee)
        {
            return EntryCheckResult.Rejected(EntryRejectionReason.StructureFull);
        }
        if (villager.TrainedClass != VillagerClass.Untrained)
        {
            return EntryCheckResult.Rejected(EntryRejectionReason.AlreadyTrained);
        }
        return EntryCheckResult.Accepted;
    }

    /// <summary>
    /// Accepts a villager for training. Called by PathTrafficSystem when
    /// a villager walks by on an adjacent path segment.
    /// </summary>
    public bool AcceptTrainee(VillagerLogic villager)
    {
        if (!CanAcceptTrainee) return false;
        if (villager.TrainedClass != VillagerClass.Untrained) return false;

        CurrentTrainees.Add(villager.Id);
        villager.BeginTraining(ProtoId, OutputClass, TrainingDuration);
        return true;
    }

    /// <summary>
    /// Completes training for a villager and removes them from the trainee list.
    /// </summary>
    public void CompleteTraining(VillagerLogic villager)
    {
        CurrentTrainees.Remove(villager.Id);
    }

    /// <inheritdoc />
    public void ProcessStructureTick(float dt, VillagerLookup villagerLookup, ref StructureTickOutput output)
    {
        for (int i = CurrentTrainees.Count - 1; i >= 0; i--)
        {
            var villager = villagerLookup(CurrentTrainees[i]);
            if (villager == null)
            {
                CurrentTrainees.RemoveAt(i);
                continue;
            }

            // Training is complete when VillagerLogic.TickTraining transitions State away from Training
            if (villager.State != VillagerState.Training)
            {
                CompleteTraining(villager);
                villager.CurrentActivity = null;
                output.PendingExits[output.ExitCount++] = new PendingExit(
                    villager.Id, StructureExitReason.TrainingComplete, villager.TrainedClass.ToString());
                output.TutorialAdvance = TutorialConditionType.VillagerTrainClass;
            }
        }
    }

    public override void Tick(float deltaTime)
    {
        // Training progress is tracked on the VillagerLogic itself.
        // This tick is intentionally lightweight.
    }
}
