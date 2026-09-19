using ForgeFlow.Core.Entities;
using ForgeFlow.Core.Events;
using ForgeFlow.Core.Proto.Logic;
using ForgeFlow.Core.Proto.Prototypes;
using ForgeFlow.Core.Systems;
using ForgeFlow.Core.Utilities;

namespace ForgeFlow.Tests;

/// <summary>Tests for VillagerSystem lifecycle and tick behaviour.</summary>
public class VillagerSystemTests
{
    [Fact]
    public void VillagerSystem_AddAndRemove()
    {
        var bus = new EventBus();
        var rm = new ItemManager(bus);
        var system = new VillagerSystem(bus, rm);

        var villager = new VillagerLogic(EntityId.Next()) { Name = "TestVillager" };
        system.AddVillager(villager);

        Assert.Equal(1, system.Count);
        Assert.True(system.VillagerIndex.ContainsKey(villager.Id));

        system.RemoveVillager(villager.Id);

        Assert.Equal(0, system.Count);
    }

    [Fact]
    public void VillagerSystem_Tick_ProducesResources()
    {
        var bus = new EventBus();
        var itemManager = new ItemManager(bus);
        var system = new VillagerSystem(bus, itemManager);

        var villager = new VillagerLogic(EntityId.Next()) { IsActive = true, Stamina = 1000, MaxStamina = 1000, WorkRate = 100f };
        villager.AssignJob(VillagerJob.Lumberjack);
        system.AddVillager(villager);

        ResourceProducedEvent? produced = null;
        bus.Subscribe<ResourceProducedEvent>(e => produced = e);

        system.Tick(1.0f, itemManager);

        Assert.NotNull(produced);
        Assert.Equal("wood", produced.Value.ResourceId);
        Assert.True(itemManager.GetStock("wood") > 0);
    }

    [Fact]
    public void VillagerSystem_Clear_RemovesAll()
    {
        var bus = new EventBus();
        var rm = new ItemManager(bus);
        var system = new VillagerSystem(bus, rm);
        system.AddVillager(new VillagerLogic(EntityId.Next()));
        system.AddVillager(new VillagerLogic(EntityId.Next()));

        system.Clear();

        Assert.Equal(0, system.Count);
    }

    [Fact]
    public void VillagerSystem_MovementStateBuffer_AlignedWithVillagersList()
    {
        EntityIdFactory.ResetForTesting();
        var bus = new EventBus();
        var system = new VillagerSystem(bus, new ItemManager(bus));

        var v1 = new VillagerLogic(EntityId.Next()) { State = VillagerState.Travelling };
        var v2 = new VillagerLogic(EntityId.Next()) { State = VillagerState.Working };
        var v3 = new VillagerLogic(EntityId.Next()) { State = VillagerState.Idle };

        system.AddVillager(v1);
        system.AddVillager(v2);
        system.AddVillager(v3);

        Assert.Equal(3, system.MovementStateCount);
        Assert.Equal(v1.Id, system.MovementStateBuffer[0].Id);
        Assert.Equal(v2.Id, system.MovementStateBuffer[1].Id);
        Assert.Equal(v3.Id, system.MovementStateBuffer[2].Id);
        Assert.Equal(VillagerState.Travelling, system.MovementStateBuffer[0].State);
        Assert.Equal(VillagerState.Working, system.MovementStateBuffer[1].State);
        Assert.Equal(VillagerState.Idle, system.MovementStateBuffer[2].State);
    }

    [Fact]
    public void VillagerSystem_MovementStateBuffer_SwapRemoveKeepsAlignment()
    {
        EntityIdFactory.ResetForTesting();
        var bus = new EventBus();
        var system = new VillagerSystem(bus, new ItemManager(bus));

        var v1 = new VillagerLogic(EntityId.Next());
        var v2 = new VillagerLogic(EntityId.Next());
        var v3 = new VillagerLogic(EntityId.Next());

        system.AddVillager(v1);
        system.AddVillager(v2);
        system.AddVillager(v3);

        // Remove v2 (middle) — v3 should swap into index 1
        system.RemoveVillager(v2.Id);

        Assert.Equal(2, system.MovementStateCount);
        Assert.Equal(2, system.Villagers.Count);

        // Verify list and buffer are still aligned
        for (int i = 0; i < system.MovementStateCount; i++)
        {
            Assert.Equal(system.Villagers[i].Id, system.MovementStateBuffer[i].Id);
        }
    }

    [Fact]
    public void VillagerSystem_MovementStateBuffer_GrowsWithCapacity()
    {
        EntityIdFactory.ResetForTesting();
        var bus = new EventBus();
        var system = new VillagerSystem(bus, new ItemManager(bus));

        // Add 70 villagers (default capacity is 64 — should auto-grow)
        for (int i = 0; i < 70; i++)
        {
            system.AddVillager(new VillagerLogic(EntityId.Next()));
        }

        Assert.Equal(70, system.MovementStateCount);
        Assert.True(system.MovementStateBuffer.Length >= 70);

        // Verify alignment still holds after growth
        for (int i = 0; i < system.MovementStateCount; i++)
        {
            Assert.Equal(system.Villagers[i].Id, system.MovementStateBuffer[i].Id);
        }
    }
}
