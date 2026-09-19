using ForgeFlow.Core.Entities;
using ForgeFlow.Core.Proto;
using ForgeFlow.Core.Proto.Logic;
using ForgeFlow.Core.Proto.Prototypes;
using ForgeFlow.Core.Systems;

namespace ForgeFlow.Tests;

/// <summary>Tests for TerrainGrid, ResourceNodeLogic, and GatingLimits.</summary>
public class TerrainGridTests
{
    // ─── TerrainGrid ──────────────────────────────────────────────────

    [Fact]
    public void TerrainGrid_GenerateDefault_PopulatesCells()
    {
        var grid = new TerrainGrid(8, 8);
        grid.GenerateDefault(seed: 42);

        Assert.Equal(64, grid.CellCount);
    }

    [Fact]
    public void TerrainGrid_GetAndSet()
    {
        var grid = new TerrainGrid(4, 4);
        var cell = new TerrainCell
        {
            Position = new GridPosRPG(1, 1),
            Biome = BiomeType.Forest,
            Fertility = 1.5f
        };

        grid.Set(new GridPosRPG(1, 1), cell);
        var retrieved = grid.Get(new GridPosRPG(1, 1));

        Assert.NotNull(retrieved);
        Assert.Equal(BiomeType.Forest, retrieved.Biome);
        Assert.Equal(1.5f, retrieved.Fertility);
    }

    [Fact]
    public void TerrainGrid_GetCellsByBiome()
    {
        var grid = new TerrainGrid(16, 16);
        grid.GenerateDefault(seed: 42);

        var allCells = grid.GetAllCells().ToList();
        var mostCommon = allCells.GroupBy(c => c.Biome)
                                 .OrderByDescending(g => g.Count())
                                 .First().Key;

        var biomeCells = grid.GetCellsByBiome(mostCommon).ToList();
        Assert.True(biomeCells.Count > 0);
        Assert.All(biomeCells, c => Assert.Equal(mostCommon, c.Biome));
    }

    [Fact]
    public void TerrainGrid_GetReturnsNull_ForMissingPosition()
    {
        var grid = new TerrainGrid(4, 4);
        Assert.Null(grid.Get(new GridPosRPG(99, 99)));
    }

    [Fact]
    public void TerrainGrid_GetOrCreate_CreatesIfMissing()
    {
        var grid = new TerrainGrid(4, 4);
        var cell = grid.GetOrCreate(new GridPosRPG(2, 3));

        Assert.NotNull(cell);
        Assert.Equal(new GridPosRPG(2, 3), cell.Position);
        Assert.Equal(BiomeType.Plains, cell.Biome);
    }

    [Fact]
    public void TerrainGrid_MountainCells_NotPathable()
    {
        var grid = new TerrainGrid(16, 16);
        grid.GenerateDefault(seed: 42);

        var mountainCells = grid.GetCellsByBiome(BiomeType.Mountain).ToList();
        if (mountainCells.Count > 0)
        {
            Assert.All(mountainCells, c => Assert.False(c.IsPathable));
        }
    }

    // ─── ResourceNodeLogic ────────────────────────────────────────────

    [Fact]
    public void ResourceNodeLogic_HarvestReducesYield()
    {
        var proto = new ResourceNodeProto
        {
            Id = "test_tree",
            ResourceId = "wood",
            MaxYield = 100,
            HarvestRate = 1.0f,
            IsRenewable = false
        };
        var node = new ResourceNodeLogic(EntityId.Next());
        node.InitializeFromProto(proto);

        int harvested = node.Harvest(10);
        Assert.Equal(10, harvested);
        Assert.Equal(90, node.CurrentYield);
    }

    [Fact]
    public void ResourceNodeLogic_HarvestDoesNotExceedRemaining()
    {
        var proto = new ResourceNodeProto
        {
            Id = "test_tree",
            ResourceId = "wood",
            MaxYield = 5,
            HarvestRate = 1.0f
        };
        var node = new ResourceNodeLogic(EntityId.Next());
        node.InitializeFromProto(proto);

        int harvested = node.Harvest(10);
        Assert.Equal(5, harvested);
        Assert.Equal(0, node.CurrentYield);
        Assert.True(node.IsDepleted);
    }

    [Fact]
    public void ResourceNodeLogic_RenewableRegrows()
    {
        var proto = new ResourceNodeProto
        {
            Id = "test_tree",
            ResourceId = "wood",
            MaxYield = 100,
            HarvestRate = 1.0f,
            RegrowthRate = 10.0f,
            IsRenewable = true
        };
        var node = new ResourceNodeLogic(EntityId.Next());
        node.InitializeFromProto(proto);
        node.Harvest(50);
        Assert.Equal(50, node.CurrentYield);

        node.Tick(5.0f);

        Assert.True(node.CurrentYield > 50);
    }

    // ─── GatingLimits ─────────────────────────────────────────────────

    [Fact]
    public void GatingLimits_GetMaxSpawners_ScalesWithTier()
    {
        var gating = new GatingLimits();

        Assert.Equal(1, gating.GetMaxSpawners(1));
        Assert.Equal(2, gating.GetMaxSpawners(2));
        Assert.Equal(50, gating.GetMaxSpawners(10));
    }

    [Fact]
    public void GatingLimits_GetMaxBuildings_ScalesWithTier()
    {
        var gating = new GatingLimits();

        Assert.Equal(3, gating.GetMaxBuildings(1));
        Assert.Equal(6, gating.GetMaxBuildings(2));
        Assert.Equal(150, gating.GetMaxBuildings(10));
    }

    [Fact]
    public void GatingLimits_GetMaxPaths_ScalesWithTier()
    {
        var gating = new GatingLimits();

        Assert.Equal(30, gating.GetMaxPaths(1));
        Assert.Equal(60, gating.GetMaxPaths(2));
        Assert.Equal(2000, gating.GetMaxPaths(10));
    }
}
