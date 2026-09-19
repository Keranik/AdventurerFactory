using ForgeFlow.Core.Data;
using ForgeFlow.Core.Entities;
using ForgeFlow.Core.Events;
using ForgeFlow.Core.Proto.Logic;
using ForgeFlow.Core.Proto.Prototypes;
using ForgeFlow.Core.Systems;

namespace ForgeFlow.Tests;

/// <summary>Tests for service structures: ToolStation, Armory, JobChanger, Academy.</summary>
public class ServiceStructureTests
{
    [Fact]
    public void ToolStation_RepairsWorkerToolDurability()
    {
        var station = new ToolStationLogic(EntityId.Next());
        var hero = new HeroEntity(EntityId.Next())
        {
            Profession = WorkerProfession.Forester,
            ToolDurability = 0f,
            LastWearOutReason = WearOutReason.ToolBroken
        };

        bool result = station.RepairWorker(hero);

        Assert.True(result);
        Assert.Equal(100f, hero.ToolDurability);
        Assert.Equal(WearOutReason.None, hero.LastWearOutReason);
    }

    [Fact]
    public void Armory_RestoresWorkerStamina()
    {
        var armory = new ArmoryLogic(EntityId.Next());
        var hero = new HeroEntity(EntityId.Next())
        {
            Profession = WorkerProfession.Warrior,
            Stamina = 0f,
            CarryLoad = 30f,
            LastWearOutReason = WearOutReason.StaminaDepleted
        };

        bool result = armory.RestoreWorker(hero);

        Assert.True(result);
        Assert.Equal(100f, hero.Stamina);
        Assert.Equal(0f, hero.CarryLoad);
        Assert.Equal(WearOutReason.None, hero.LastWearOutReason);
    }

    [Fact]
    public void JobChanger_StripsAbilitiesAndChangesProfession()
    {
        var jobChanger = new JobChangerLogic(EntityId.Next());
        var hero = new HeroEntity(EntityId.Next()) { Profession = WorkerProfession.Warrior };
        hero.GainAbility(new WorkerAbility { Id = "attack", DisplayName = "Attack" });

        int lost = jobChanger.ChangeJob(hero, WorkerProfession.Researcher);

        Assert.Equal(1, lost);
        Assert.Equal(WorkerProfession.Researcher, hero.Profession);
        Assert.Empty(hero.Abilities);
        Assert.Equal(100f, hero.ToolDurability);
    }

    [Fact]
    public void Academy_RequiresMinimumLevel()
    {
        var academy = new AcademyLogic(EntityId.Next()) { RequiredLevel = 2 };
        var lowLevel = new HeroEntity(EntityId.Next()) { Level = 1 };
        var highLevel = new HeroEntity(EntityId.Next()) { Level = 3 };

        Assert.False(academy.CanTrain(lowLevel));
        Assert.True(academy.CanTrain(highLevel));
    }

    [Fact]
    public void Academy_TrainsWorkerToResearcher()
    {
        var academy = new AcademyLogic(EntityId.Next()) { OutputProfession = WorkerProfession.Researcher };
        var hero = new HeroEntity(EntityId.Next()) { Profession = WorkerProfession.None, Level = 3 };

        int lost = academy.TrainWorker(hero);

        Assert.Equal(0, lost);
        Assert.Equal(WorkerProfession.Researcher, hero.Profession);
    }

    [Fact]
    public void Phase10Structures_GetCategoryName_ReturnsCorrectValues()
    {
        Assert.Equal("ToolStation", new ToolStationLogic(EntityId.Next()).GetCategoryName());
        Assert.Equal("Armory", new ArmoryLogic(EntityId.Next()).GetCategoryName());
        Assert.Equal("JobChanger", new JobChangerLogic(EntityId.Next()).GetCategoryName());
        Assert.Equal("Academy", new AcademyLogic(EntityId.Next()).GetCategoryName());
        Assert.Equal("CheckGate", new CheckGateLogic(EntityId.Next()).GetCategoryName());
        Assert.Equal("FilterSplitter", new FilterSplitterLogic(EntityId.Next()).GetCategoryName());
        Assert.Equal("Balancer", new BalancerLogic(EntityId.Next()).GetCategoryName());
    }

    // ─── InnLogic ─────────────────────────────────────────────────────

    [Fact]
    public void InnLogic_RestVillager_ManagerClearsActivity()
    {
        EntityBase.ResetIdCounter();
        var villager = new VillagerLogic(EntityId.Next()) { IsActive = true, Stamina = 0, MaxStamina = 100 };
        villager.AssignJob(VillagerJob.Builder);
        villager.CurrentActivity = "Gathering sticks";
        Assert.Equal(VillagerJob.Builder, villager.Profession);

        var inn = new InnLogic(EntityId.Next()) { IsActive = true };
        inn.SetRestRecipe("rest_basic", 0.01f);
        Assert.True(inn.AcceptVillager(villager));

        bool rested = false;
        for (int i = 0; i < 1000 && !rested; i++)
        {
            rested = inn.RestVillager(villager, 0.1f);
        }

        Assert.True(rested, "Villager should have finished resting");

        villager.CurrentActivity = null;

        Assert.Null(villager.CurrentActivity);
        Assert.Equal(VillagerJob.Builder, villager.Profession);
        Assert.Equal(villager.MaxStamina, villager.Stamina);
    }

    [Fact]
    public void InnLogic_RestVillager_StateBecomeTravellingAfterExit()
    {
        EntityBase.ResetIdCounter();
        var villager = new VillagerLogic(EntityId.Next()) { IsActive = true, Stamina = 0, MaxStamina = 100 };
        villager.AssignJob(VillagerJob.Lumberjack);

        var inn = new InnLogic(EntityId.Next()) { IsActive = true };
        inn.SetRestRecipe("rest_basic", 0.01f);
        inn.AcceptVillager(villager);

        bool rested = false;
        for (int i = 0; i < 1000 && !rested; i++)
        {
            rested = inn.RestVillager(villager, 0.1f);
        }

        villager.CurrentActivity = null;
        villager.PlaceOnPath(99);

        Assert.Equal(VillagerState.Travelling, villager.State);
        Assert.Equal(VillagerJob.Lumberjack, villager.Profession);
        Assert.Equal(99ul, villager.CurrentPathSegmentId);
    }

    [Fact]
    public void InnLogic_RejectsVillagerCarryingItems()
    {
        EntityBase.ResetIdCounter();
        var villager = new VillagerLogic(EntityId.Next()) { IsActive = true };
        villager.TryPickUpItem(new ItemInstance { ProtoId = "stick", Quantity = 1 });

        var inn = new InnLogic(EntityId.Next()) { IsActive = true, MaxOccupants = 4 };

        Assert.False(inn.AcceptVillager(villager));
        Assert.Empty(inn.CurrentOccupants);
    }

    [Fact]
    public void InnLogic_RejectsWhenFull()
    {
        EntityBase.ResetIdCounter();
        var inn = new InnLogic(EntityId.Next()) { IsActive = true, MaxOccupants = 1 };

        var v1 = new VillagerLogic(EntityId.Next()) { IsActive = true };
        var v2 = new VillagerLogic(EntityId.Next()) { IsActive = true };

        Assert.True(inn.AcceptVillager(v1));
        Assert.False(inn.AcceptVillager(v2));
        Assert.Single(inn.CurrentOccupants);
    }
}
