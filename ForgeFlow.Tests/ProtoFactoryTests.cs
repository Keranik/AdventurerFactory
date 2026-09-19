using ForgeFlow.Core.Entities;
using ForgeFlow.Core.Events;
using ForgeFlow.Core.Proto;
using ForgeFlow.Core.Proto.Logic;
using ForgeFlow.Core.Proto.Prototypes;

namespace ForgeFlow.Tests;

/// <summary>Tests for ProtoFactory — creating logic instances from prototypes.</summary>
public class ProtoFactoryTests
{
    private static (ProtoRegistry reg, ProtoFactory factory) Create()
    {
        var reg = new ProtoRegistry();
        reg.RegisterDefaults();
        var bus = new EventBus();
        return (reg, new ProtoFactory(reg, bus));
    }

    [Fact]
    public void ProtoFactory_CreateGatheringStructure_ReturnsForestryStructure()
    {
        var (_, factory) = Create();
        var structure = factory.CreateGatheringRecipeEntity("forestry_basic");

        Assert.NotNull(structure);
        Assert.IsType<ForestryRecipeEntity>(structure);
    }

    [Fact]
    public void ProtoFactory_CreateGatheringStructure_ReturnsMiningStructure()
    {
        var (_, factory) = Create();
        var structure = factory.CreateGatheringRecipeEntity("mining_basic");

        Assert.NotNull(structure);
        Assert.IsType<MiningRecipeEntity>(structure);
    }

    [Fact]
    public void ProtoFactory_CreateResourceNode_ReturnsNode()
    {
        var (_, factory) = Create();
        var node = factory.CreateResourceNode("oak_tree");

        Assert.NotNull(node);
        Assert.Equal("wood", node.ResourceId);
    }

    [Fact]
    public void ProtoFactory_CreatePathSegment_ReturnsSegment()
    {
        var (_, factory) = Create();
        var segment = factory.CreatePathSegment("path_straight");

        Assert.NotNull(segment);
        Assert.Equal(PathNodeType.Straight, segment.NodeType);
    }

    [Fact]
    public void ProtoFactory_CreateVillager_ReturnsVillager()
    {
        var (_, factory) = Create();
        var villager = factory.CreateVillager("villager_basic");

        Assert.NotNull(villager);
        Assert.Equal(1.0f, villager.WorkRate);
        Assert.Equal(2.0f, villager.MovementSpeed);
    }

    [Fact]
    public void ProtoFactory_CreateTrainingBuilding_ReturnsSchool()
    {
        var (_, factory) = Create();
        var school = factory.CreateTrainingBuilding("warrior_school");

        Assert.NotNull(school);
        Assert.Equal(VillagerClass.Warrior, school.OutputClass);
    }

    [Fact]
    public void ProtoFactory_CreateTutorialMission_ReturnsMission()
    {
        var (_, factory) = Create();
        var mission = factory.CreateTutorialMission("p1_01_place_home");

        Assert.NotNull(mission);
        Assert.Equal(1, mission.Order);
    }

    [Fact]
    public void ProtoFactory_InvalidId_ReturnsNull()
    {
        var (_, factory) = Create();

        Assert.Null(factory.CreateGatheringRecipeEntity("nonexistent"));
        Assert.Null(factory.CreateResourceNode("nonexistent"));
        Assert.Null(factory.CreatePathSegment("nonexistent"));
        Assert.Null(factory.CreateVillager("nonexistent"));
        Assert.Null(factory.CreateTrainingBuilding("nonexistent"));
    }
}
