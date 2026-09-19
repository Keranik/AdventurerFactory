using ForgeFlow.Core.Entities;
using ForgeFlow.Core.Events;
using ForgeFlow.Core.Proto;
using ForgeFlow.Core.Proto.Logic;
using ForgeFlow.Core.Proto.Prototypes;
using ForgeFlow.Core.Systems;

namespace ForgeFlow.Tests;

/// <summary>Tests for TutorialSystem initialization, condition advancement, and rewards.</summary>
public class TutorialSystemTests
{
    [Fact]
    public void TutorialSystem_Initialize_ActivatesFirstMission()
    {
        var bus = new EventBus();
        var reg = new ProtoRegistry();
        reg.RegisterDefaults();
        var factory = new ProtoFactory(reg, bus);

        var tutorial = new TutorialSystem(bus, factory);
        tutorial.Initialize(reg);

        Assert.NotNull(tutorial.ActiveMission);
        Assert.Equal(1, tutorial.ActiveMission.Order);
        Assert.True(tutorial.TotalCount > 0);
    }

    [Fact]
    public void TutorialSystem_AdvanceCondition_CompletesAndAdvances()
    {
        var bus = new EventBus();
        var reg = new ProtoRegistry();
        reg.RegisterDefaults();
        var factory = new ProtoFactory(reg, bus);

        var tutorial = new TutorialSystem(bus, factory);
        tutorial.Initialize(reg);

        TutorialCompletedEvent? completed = null;
        bus.Subscribe<TutorialCompletedEvent>(e => completed = e);

        bool result = tutorial.AdvanceCondition(TutorialConditionType.PlaceSpecificStructure, "Spawner");

        Assert.True(result);
        Assert.NotNull(completed);
        Assert.Equal(1, completed.Value.Order);
        Assert.NotNull(tutorial.ActiveMission);
        Assert.True(tutorial.ActiveMission.Order > 1);
    }

    [Fact]
    public void TutorialSystem_CollectRewards()
    {
        var bus = new EventBus();
        var reg = new ProtoRegistry();
        reg.RegisterDefaults();
        var factory = new ProtoFactory(reg, bus);

        var tutorial = new TutorialSystem(bus, factory);
        tutorial.Initialize(reg);

        var mission = tutorial.ActiveMission!;
        var itemManager = new ItemManager(bus);

        tutorial.AdvanceCondition(TutorialConditionType.PlaceSpecificStructure, "Spawner");
        tutorial.CollectRewards(mission, itemManager);

        Assert.True(itemManager.GetStock("gold") > 0);
    }

    [Fact]
    public void TutorialSystem_ForestryExit_AutoAdvancesWhenGateAlreadyExists()
    {
        EntityBase.ResetIdCounter();
        var bus = new EventBus();
        var reg = new ProtoRegistry();
        reg.RegisterDefaults();
        var factory = new ProtoFactory(reg, bus);
        var itemManager = new ItemManager(bus);
        var vs = new VillagerSystem(bus, itemManager);
        var terrain = new TerrainGrid(8, 8);
        terrain.GenerateDefault();
        var tileMgr = new TileManager(terrain);
        var entMgr = new EntityManager(tileMgr, vs);

        var tutorial = new TutorialSystem(bus, factory);
        tutorial.SetEntityManager(entMgr);
        tutorial.Initialize(reg);

        var gate1 = new PathGateLogic(EntityId.Next()) { Position = new GridPosRPG(0, 0), Mode = PathGateMode.Exit };
        var gate2 = new PathGateLogic(EntityId.Next()) { Position = new GridPosRPG(2, 0), Mode = PathGateMode.Exit };
        entMgr.AddPathGate(gate1);
        entMgr.AddPathGate(gate2);

        var entranceGate1 = new PathGateLogic(EntityId.Next()) { Position = new GridPosRPG(0, 1), Mode = PathGateMode.Entrance };
        var entranceGate2 = new PathGateLogic(EntityId.Next()) { Position = new GridPosRPG(2, 1), Mode = PathGateMode.Entrance };
        entMgr.AddPathGate(entranceGate1);
        entMgr.AddPathGate(entranceGate2);

        tutorial.AdvanceCondition(TutorialConditionType.PlaceSpecificStructure, "Spawner");
        tutorial.AdvanceCondition(TutorialConditionType.PlacePathGateExit);
        tutorial.AdvanceCondition(TutorialConditionType.PlaceSpecificStructure, "Forestry");
        if (tutorial.ActiveMission != null && tutorial.ActiveMission.ProtoId == "p1_04_forestry_entrance")
        {
            tutorial.AdvanceCondition(TutorialConditionType.PlacePathGateEntrance);
        }
        tutorial.AdvanceCondition(TutorialConditionType.BuildPath, null, 3);
        tutorial.AdvanceCondition(TutorialConditionType.WatchVillagerGather, "sticks");
        tutorial.AdvanceCondition(TutorialConditionType.PlaceSpecificStructure, "Stockpile");
        tutorial.AdvanceCondition(TutorialConditionType.SelectStockpileProduct, "sticks");
        if (tutorial.ActiveMission != null && tutorial.ActiveMission.ProtoId == "p1_09_stockpile_entrance")
        {
            tutorial.AdvanceCondition(TutorialConditionType.PlacePathGateEntrance);
        }

        Assert.NotNull(tutorial.ActiveMission);
        Assert.NotEqual("p1_10_forestry_exit", tutorial.ActiveMission.ProtoId);
        Assert.True(tutorial.ActiveMission.Order > 10,
            $"Expected mission after step 10, but active is '{tutorial.ActiveMission.ProtoId}' (order {tutorial.ActiveMission.Order})");
    }
}
