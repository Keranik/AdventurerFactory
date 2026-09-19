using ForgeFlow.Core.Data;
using ForgeFlow.Core.Entities;
using ForgeFlow.Core.Events;
using ForgeFlow.Core.Proto.Logic;
using ForgeFlow.Core.Systems;

namespace ForgeFlow.Tests;

/// <summary>Tests for HeroEntity runtime state — display fields, maintenance, and refresh.</summary>
public class HeroEntityTests
{
    [Fact]
    public void HeroEntity_ShowWorker_DisplaysToolDurability()
    {
        var hero = new HeroEntity(EntityId.Next())
        {
            Profession = WorkerProfession.Forester,
            ToolDurability = 75f,
            MaxToolDurability = 100f
        };

        Assert.Equal(75f, hero.ToolDurability);
        Assert.Equal(100f, hero.MaxToolDurability);
    }

    [Fact]
    public void HeroEntity_ShowWorker_DisplaysStamina()
    {
        var hero = new HeroEntity(EntityId.Next())
        {
            Profession = WorkerProfession.Warrior,
            Stamina = 42f,
            MaxStamina = 100f
        };

        Assert.Equal(42f, hero.Stamina);
        Assert.Equal(100f, hero.MaxStamina);
    }

    [Fact]
    public void HeroEntity_ShowWorker_DisplaysCarryLoad()
    {
        var hero = new HeroEntity(EntityId.Next())
        {
            Profession = WorkerProfession.HerbGatherer,
            CarryLoad = 30f,
            MaxCarryCapacity = 50f
        };

        float percentage = hero.CarryLoad / hero.MaxCarryCapacity * 100f;
        Assert.InRange(percentage, 59.9f, 60.1f);
    }

    [Fact]
    public void HeroEntity_ShowWorker_DisplaysTraits()
    {
        var hero = new HeroEntity(EntityId.Next());
        hero.Traits.Add("Brave");
        hero.Traits.Add("Strong");

        Assert.Equal(2, hero.Traits.Count);
        Assert.Contains("Brave", hero.Traits);
        Assert.Contains("Strong", hero.Traits);
    }

    [Fact]
    public void HeroEntity_ShowWorker_DisplaysAbilities()
    {
        var hero = new HeroEntity(EntityId.Next()) { Profession = WorkerProfession.Warrior };
        hero.GainAbility(new WorkerAbility { Id = "slash", DisplayName = "Power Slash", BonusValue = 1.5f });
        hero.GainAbility(new WorkerAbility { Id = "block", DisplayName = "Shield Block", BonusValue = 2.0f });

        Assert.Equal(2, hero.Abilities.Count);
        Assert.Equal("Power Slash", hero.Abilities[0].DisplayName);
        Assert.Equal(1.5f, hero.Abilities[0].BonusValue);
    }

    [Fact]
    public void HeroEntity_ShowWorker_DisplaysWearOutReason()
    {
        var hero = new HeroEntity(EntityId.Next())
        {
            Profession = WorkerProfession.Miner,
            LastWearOutReason = WearOutReason.ToolBroken,
            State = HeroState.WornOut
        };

        Assert.Equal(WearOutReason.ToolBroken, hero.LastWearOutReason);
        Assert.Equal(HeroState.WornOut, hero.State);
    }

    [Fact]
    public void HeroEntity_SendToMaintenance_RefreshesWorker()
    {
        var hero = new HeroEntity(EntityId.Next())
        {
            Profession = WorkerProfession.Forester,
            ToolDurability = 0f,
            Stamina = 10f,
            CarryLoad = 40f,
            State = HeroState.WornOut,
            LastWearOutReason = WearOutReason.ToolBroken
        };

        hero.RefreshAfterMaintenance();

        Assert.Equal(100f, hero.ToolDurability);
        Assert.Equal(100f, hero.Stamina);
        Assert.Equal(0f, hero.CarryLoad);
        Assert.Equal(WearOutReason.None, hero.LastWearOutReason);
        Assert.Equal(HeroState.OnPath, hero.State);
    }

    [Fact]
    public void HeroEntity_ReturningToMaintenance_RefreshesViaButton()
    {
        var hero = new HeroEntity(EntityId.Next())
        {
            State = HeroState.ReturningToMaintenance,
            ToolDurability = 5f,
            Stamina = 20f,
            CarryLoad = 45f,
            LastWearOutReason = WearOutReason.StaminaDepleted
        };

        hero.RefreshAfterMaintenance();

        Assert.Equal(HeroState.OnPath, hero.State);
        Assert.Equal(WearOutReason.None, hero.LastWearOutReason);
    }

    [Fact]
    public void HeroEntity_RefreshAfterMaintenance_RestoresAllStats()
    {
        var hero = new HeroEntity(EntityId.Next())
        {
            Profession = WorkerProfession.Forester,
            ToolDurability = 0f,
            Stamina = 10f,
            CarryLoad = 40f,
            State = HeroState.WornOut,
            LastWearOutReason = WearOutReason.ToolBroken
        };

        hero.RefreshAfterMaintenance();

        Assert.Equal(100f, hero.ToolDurability);
        Assert.Equal(100f, hero.Stamina);
        Assert.Equal(0f, hero.CarryLoad);
        Assert.Equal(WearOutReason.None, hero.LastWearOutReason);
        Assert.Equal(HeroState.OnPath, hero.State);
    }
}
