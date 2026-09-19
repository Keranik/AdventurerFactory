using ForgeFlow.Core.Systems;

namespace ForgeFlow.Tests;

/// <summary>
/// Tests for EconomyConfig — the single source of truth for all gold costs.
/// </summary>
public class EconomyConfigTests
{
    // ── Structure Costs ───────────────────────────────────────────────

    [Fact]
    public void GetStructureCost_KnownTypes_ReturnExpectedValues()
    {
        Assert.Equal(25, EconomyConfig.GetStructureCost("Spawner"));
        Assert.Equal(60, EconomyConfig.GetStructureCost("DungeonPortal"));
        Assert.Equal(80, EconomyConfig.GetStructureCost("FusionAltar"));
        Assert.Equal(10, EconomyConfig.GetStructureCost("PathGate"));
        Assert.Equal(20, EconomyConfig.GetStructureCost("Inn"));
        Assert.Equal(15, EconomyConfig.GetStructureCost("Stockpile"));
    }

    [Fact]
    public void GetStructureCost_AllDefinedTypes_HavePositiveCost()
    {
        foreach (var kvp in EconomyConfig.StructureCosts)
        {
            Assert.True(kvp.Value > 0, $"{kvp.Key} should have a positive cost");
        }
    }

    [Fact]
    public void GetStructureCost_UnknownType_ReturnsDefaultCost()
    {
        // ManaExtractor is not in StructureCosts dictionary
        var cost = EconomyConfig.GetStructureCost("ManaExtractor");
        Assert.Equal(EconomyConfig.DefaultStructureCost, cost);
    }

    [Fact]
    public void PathSegmentCost_IsPositive()
    {
        Assert.True(EconomyConfig.PathSegmentCost > 0);
    }

    // ── Tier-Scaled Building Costs ──────────────────────────────────

    [Fact]
    public void GetBuildingGoldCost_Tier1_Returns25()
    {
        Assert.Equal(25, EconomyConfig.GetBuildingGoldCost(1));
    }

    [Fact]
    public void GetBuildingGoldCost_IncreasesWithTier()
    {
        int previousCost = 0;
        for (int tier = 1; tier <= 10; tier++)
        {
            int cost = EconomyConfig.GetBuildingGoldCost(tier);
            Assert.True(cost > previousCost, $"Tier {tier} cost ({cost}) should be greater than tier {tier - 1} cost ({previousCost})");
            previousCost = cost;
        }
    }

    [Fact]
    public void GetBuildingGoldCost_BeyondTier10_ScalesLinearly()
    {
        int tier10 = EconomyConfig.GetBuildingGoldCost(10);
        int tier11 = EconomyConfig.GetBuildingGoldCost(11);
        Assert.True(tier11 > tier10);
    }

    // ── Tier-Scaled Path Costs ──────────────────────────────────────

    [Fact]
    public void GetPathGoldCost_Tier1_Returns2()
    {
        Assert.Equal(2, EconomyConfig.GetPathGoldCost(1));
    }

    [Fact]
    public void GetPathGoldCost_IncreasesWithTier()
    {
        int previousCost = 0;
        for (int tier = 1; tier <= 4; tier++)
        {
            int cost = EconomyConfig.GetPathGoldCost(tier);
            Assert.True(cost > previousCost, $"Tier {tier} cost ({cost}) should be greater than tier {tier - 1} cost ({previousCost})");
            previousCost = cost;
        }
    }

    // ── Dungeon Rewards ─────────────────────────────────────────────

    [Fact]
    public void GetDungeonGoldReward_Tier1_Returns10()
    {
        Assert.Equal(10, EconomyConfig.GetDungeonGoldReward(1));
    }

    [Fact]
    public void GetDungeonGoldReward_IncreasesWithTier()
    {
        int previousReward = 0;
        for (int tier = 1; tier <= 10; tier++)
        {
            int reward = EconomyConfig.GetDungeonGoldReward(tier);
            Assert.True(reward > previousReward, $"Tier {tier} reward ({reward}) should be greater than tier {tier - 1} reward ({previousReward})");
            previousReward = reward;
        }
    }

    [Fact]
    public void GetDungeonGoldReward_BeyondTier10_ScalesLinearly()
    {
        int tier10 = EconomyConfig.GetDungeonGoldReward(10);
        int tier11 = EconomyConfig.GetDungeonGoldReward(11);
        Assert.True(tier11 > tier10);
    }

    // ── Delegation tests removed — GameplayFlowSystem no longer exposes ──
    // ── static cost properties (callers use EconomyConfig directly)     ──

    [Fact]
    public void EconomyConfig_GoldMethods_ReturnConsistentValues()
    {
        Assert.True(EconomyConfig.GetBuildingGoldCost(3) > 0);
        Assert.True(EconomyConfig.GetPathGoldCost(2) > 0);
        Assert.True(EconomyConfig.GetDungeonGoldReward(5) > 0);
    }

    [Fact]
    public void EconomyConfig_GoldScaling_IsConsistent()
    {
        Assert.True(EconomyConfig.GetBuildingGoldCost(4) > 0);
        Assert.True(EconomyConfig.GetPathGoldCost(3) > 0);
        Assert.True(EconomyConfig.GetDungeonGoldReward(7) > 0);
    }

    // ── Phase10 Economy ─────────────────────────────────────────────

    [Fact]
    public void GoldCost_HigherTierCostsMore()
    {
        int tier1 = EconomyConfig.GetBuildingGoldCost(1);
        int tier3 = EconomyConfig.GetBuildingGoldCost(3);
        int tier5 = EconomyConfig.GetBuildingGoldCost(5);

        Assert.True(tier3 > tier1);
        Assert.True(tier5 > tier3);
    }

    [Fact]
    public void GoldReward_HigherTierDungeonsRewardMore()
    {
        int t1 = EconomyConfig.GetDungeonGoldReward(1);
        int t3 = EconomyConfig.GetDungeonGoldReward(3);
        int t5 = EconomyConfig.GetDungeonGoldReward(5);

        Assert.True(t3 > t1);
        Assert.True(t5 > t3);
    }

    [Fact]
    public void PathGoldCost_ScalesWithTier()
    {
        int t1 = EconomyConfig.GetPathGoldCost(1);
        int t4 = EconomyConfig.GetPathGoldCost(4);

        Assert.True(t4 > t1);
    }

    // ── Phase14 Economy ─────────────────────────────────────────────

    [Fact]
    public void EconomyConfig_StructureCosts_AllDefined()
    {
        Assert.True(EconomyConfig.StructureCosts.ContainsKey("Spawner"));
        Assert.True(EconomyConfig.StructureCosts.ContainsKey("Forge"));
        Assert.True(EconomyConfig.StructureCosts.ContainsKey("DungeonPortal"));
        Assert.True(EconomyConfig.StructureCosts.ContainsKey("FusionAltar"));
        Assert.True(EconomyConfig.StructureCosts.ContainsKey("AppearanceWorkshop"));
    }

    [Fact]
    public void EconomyConfig_SpawnerCosts25()
    {
        Assert.Equal(25, EconomyConfig.GetStructureCost("Spawner"));
    }

    [Fact]
    public void EconomyConfig_PathSegmentCost_Is2()
    {
        Assert.Equal(2, EconomyConfig.PathSegmentCost);
    }

    [Fact]
    public void DungeonReward_Tier1_Is10Gold()
    {
        Assert.Equal(10, EconomyConfig.GetDungeonGoldReward(1));
    }

    [Fact]
    public void DungeonReward_Tier5_Is200Gold()
    {
        Assert.Equal(200, EconomyConfig.GetDungeonGoldReward(5));
    }

    [Fact]
    public void DungeonReward_Tier10_Is6000Gold()
    {
        Assert.Equal(6000, EconomyConfig.GetDungeonGoldReward(10));
    }
}
