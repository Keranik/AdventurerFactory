using ForgeFlow.Core.Proto;
using ForgeFlow.Core.Proto.Prototypes;

namespace ForgeFlow.Tests;

/// <summary>Tests for ProtoRegistry — JSON loading and default registration.</summary>
public class ProtoRegistryTests
{
    [Fact]
    public void ProtoRegistry_RegisterDefaults_PopulatesAllCategories()
    {
        var reg = new ProtoRegistry();
        reg.RegisterDefaults();

        Assert.True(reg.GetAllStructures().Any());
        Assert.True(reg.GetAllResourceNodes().Any());
        Assert.True(reg.GetAllPathSegments().Any());
        Assert.True(reg.GetAllVillagers().Any());
        Assert.True(reg.GetAllTutorials().Any());
    }

    [Fact]
    public void ProtoRegistry_LoadAllFromEmbeddedResources_LoadsStructuresJson()
    {
        var reg = new ProtoRegistry();
        reg.LoadAllFromEmbeddedResources();

        Assert.NotNull(reg.GetStructure("forestry_basic"));
        Assert.NotNull(reg.GetStructure("mining_basic"));
    }

    [Fact]
    public void ProtoRegistry_LoadAllFromEmbeddedResources_LoadsResourceNodes()
    {
        var reg = new ProtoRegistry();
        reg.LoadAllFromEmbeddedResources();

        Assert.True(reg.GetAllResourceNodes().Any());
    }

    [Fact]
    public void ProtoRegistry_LoadAllFromEmbeddedResources_LoadsVillagers()
    {
        var reg = new ProtoRegistry();
        reg.LoadAllFromEmbeddedResources();

        Assert.True(reg.GetAllVillagers().Any());
    }

    [Fact]
    public void ProtoRegistry_LoadAllFromEmbeddedResources_LoadsTutorials()
    {
        var reg = new ProtoRegistry();
        reg.LoadAllFromEmbeddedResources();

        Assert.True(reg.GetAllTutorials().Any());
    }

    [Fact]
    public void ProtoRegistry_LoadAllFromEmbeddedResources_LoadsBiomes()
    {
        var reg = new ProtoRegistry();
        reg.LoadAllFromEmbeddedResources();

        Assert.True(reg.GetAllBiomes().Any());
    }

    [Fact]
    public void ProtoRegistry_TrainingBuildingDefaults_FiveSchools()
    {
        var reg = new ProtoRegistry();
        reg.RegisterDefaults();

        Assert.NotNull(reg.GetStructure("warrior_school"));
        Assert.NotNull(reg.GetStructure("cleric_school"));
        Assert.NotNull(reg.GetStructure("mage_academy"));
        Assert.NotNull(reg.GetStructure("ranger_lodge"));
        Assert.NotNull(reg.GetStructure("artisan_guild"));
    }

    [Fact]
    public void ForestryProto_HasCorrectCategory_FromJSON()
    {
        var reg = new ProtoRegistry();
        reg.LoadAllFromEmbeddedResources();
        reg.RegisterDefaults();

        var proto = reg.GetStructure("forestry_basic");
        Assert.NotNull(proto);
        Assert.IsAssignableFrom<GatheringProtoBase>(proto);
    }
}
