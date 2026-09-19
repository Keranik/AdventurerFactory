using ForgeFlow.Core.Entities;
using ForgeFlow.Core.Proto.Logic;
using ForgeFlow.Core.Proto.Prototypes;

namespace ForgeFlow.Tests;

/// <summary>Tests for VillagerLogic and TrainingBuildingLogic — state, jobs, training, and stamina.</summary>
public class VillagerLogicTests
{
    // ─── VillagerLogic ────────────────────────────────────────────────

    [Fact]
    public void VillagerLogic_AssignJob_TransitionsToWorking()
    {
        var villager = new VillagerLogic(EntityId.Next());
        villager.AssignJob(VillagerJob.Lumberjack);

        Assert.Equal(VillagerJob.Lumberjack, villager.Profession);
        Assert.Equal(VillagerState.Working, villager.State);
    }

    [Fact]
    public void VillagerLogic_AssignIdleJob_StaysIdle()
    {
        var villager = new VillagerLogic(EntityId.Next());
        villager.AssignJob(VillagerJob.Idle);

        Assert.Equal(VillagerState.Idle, villager.State);
    }

    [Fact]
    public void VillagerLogic_EffectiveWorkRate_ClassBonuses()
    {
        var villager = new VillagerLogic(EntityId.Next()) { WorkRate = 1.0f };

        villager.TrainedClass = VillagerClass.Untrained;
        Assert.Equal(1.0f, villager.EffectiveWorkRate);

        villager.TrainedClass = VillagerClass.Warrior;
        villager.Profession = VillagerJob.Guard;
        Assert.Equal(1.5f, villager.EffectiveWorkRate);

        villager.TrainedClass = VillagerClass.Artisan;
        villager.Profession = VillagerJob.Builder;
        Assert.Equal(1.6f, villager.EffectiveWorkRate);
    }

    [Fact]
    public void VillagerLogic_BeginTraining_SetsState()
    {
        var villager = new VillagerLogic(EntityId.Next());
        villager.BeginTraining("warrior_school", VillagerClass.Warrior, 10f);

        Assert.Equal(VillagerState.Training, villager.State);
        Assert.Equal(VillagerClass.Warrior, villager.TrainedClass);
        Assert.Equal("warrior_school", villager.TrainingBuildingId);
        Assert.Equal(10f, villager.TrainingRequired);
    }

    [Fact]
    public void VillagerLogic_TrainingCompletes_AfterDuration()
    {
        var villager = new VillagerLogic(EntityId.Next()) { IsActive = true };
        villager.BeginTraining("school", VillagerClass.Mage, 2.0f);

        for (int i = 0; i < 130; i++)
        {
            villager.Tick(1f / 60f);
        }

        Assert.Equal(VillagerState.Idle, villager.State);
        Assert.Null(villager.TrainingBuildingId);
    }

    [Fact]
    public void VillagerLogic_Working_DrainStamina_TransitionsToResting()
    {
        var villager = new VillagerLogic(EntityId.Next()) { IsActive = true, Stamina = 0, MaxStamina = 100 };
        villager.AssignJob(VillagerJob.Miner);

        villager.Tick(1f / 60f);

        Assert.Equal(VillagerState.Resting, villager.State);
    }

    [Fact]
    public void VillagerLogic_PlaceOnPath()
    {
        var villager = new VillagerLogic(EntityId.Next());
        villager.PlaceOnPath(42);

        Assert.Equal(42ul, villager.CurrentPathSegmentId);
        Assert.Equal(VillagerState.Travelling, villager.State);
    }

    [Fact]
    public void VillagerLogic_InitializeFromProto()
    {
        var proto = new VillagerProto
        {
            Id = "test_villager",
            BaseWorkRate = 2.5f,
            BaseMovementSpeed = 3.0f,
            BaseStamina = 200
        };

        var villager = new VillagerLogic(EntityId.Next());
        villager.InitializeFromProto(proto);

        Assert.Equal("test_villager", villager.ProtoId);
        Assert.Equal(2.5f, villager.WorkRate);
        Assert.Equal(3.0f, villager.MovementSpeed);
        Assert.Equal(200, villager.Stamina);
    }

    // ─── TrainingBuildingLogic ────────────────────────────────────────

    [Fact]
    public void TrainingBuildingLogic_AcceptTrainee_StartsTraining()
    {
        var school = new TrainingBuildingLogic(EntityId.Next())
        {
            OutputClass = VillagerClass.Warrior,
            TrainingDuration = 5f,
            MaxTrainees = 2
        };
        var villager = new VillagerLogic(EntityId.Next());

        bool accepted = school.AcceptTrainee(villager);

        Assert.True(accepted);
        Assert.Equal(VillagerState.Training, villager.State);
        Assert.Equal(1, school.TraineeCount);
    }

    [Fact]
    public void TrainingBuildingLogic_RejectsAlreadyTrained()
    {
        var school = new TrainingBuildingLogic(EntityId.Next()) { MaxTrainees = 2 };
        var villager = new VillagerLogic(EntityId.Next()) { TrainedClass = VillagerClass.Warrior };

        Assert.False(school.AcceptTrainee(villager));
    }

    [Fact]
    public void TrainingBuildingLogic_RejectsWhenFull()
    {
        var school = new TrainingBuildingLogic(EntityId.Next()) { MaxTrainees = 1 };
        var v1 = new VillagerLogic(EntityId.Next());
        var v2 = new VillagerLogic(EntityId.Next());

        Assert.True(school.AcceptTrainee(v1));
        Assert.False(school.AcceptTrainee(v2));
    }

    [Fact]
    public void TrainingBuildingLogic_CompleteTraining_RemovesTrainee()
    {
        var school = new TrainingBuildingLogic(EntityId.Next())
        {
            OutputClass = VillagerClass.Cleric,
            MaxTrainees = 2
        };
        var villager = new VillagerLogic(EntityId.Next());
        school.AcceptTrainee(villager);

        school.CompleteTraining(villager);

        Assert.Equal(0, school.TraineeCount);
    }
}
