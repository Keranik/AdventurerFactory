using ForgeFlow.Core;
using ForgeFlow.Core.Entities;
using ForgeFlow.Core.Events;
using ForgeFlow.Core.Systems;
using ForgeFlow.Core.Utilities;

namespace ForgeFlow.Tests;

/// <summary>Tests for the AchievementSystem.</summary>
public class AchievementsTests
{
    [Fact]
    public void AchievementSystem_VictoryAchievement_UnlocksOnVictory()
    {
        var bus = new EventBus();
        var achievements = new AchievementSystem(bus);
        achievements.RegisterDefaults();
        achievements.Initialize();

        bus.Publish(new GameStateChangedEvent(GameState.Playing, GameState.Victory));

        Assert.True(achievements.IsUnlocked("victory"));
    }

    [Fact]
    public void AchievementSystem_ResearchTier5_UnlocksAtTier5()
    {
        var bus = new EventBus();
        var achievements = new AchievementSystem(bus);
        achievements.RegisterDefaults();
        achievements.Initialize();

        bus.Publish(new ResearchUnlockedEvent(5));
        Assert.True(achievements.IsUnlocked("research_tier5"));
    }

    [Fact]
    public void AchievementSystem_PrestigeAchievement()
    {
        var bus = new EventBus();
        var achievements = new AchievementSystem(bus);
        achievements.RegisterDefaults();
        achievements.Initialize();

        bus.Publish(new PrestigeResetEvent(1, 5, 1.1f));
        Assert.True(achievements.IsUnlocked("first_prestige"));
    }

    [Fact]
    public void GameBootstrapper_FullFlow_SpawnHeroCheckAchievements()
    {
        ServiceLocator.Clear();
        var bootstrapper = new GameBootstrapper();
        bootstrapper.Bootstrap();

        bootstrapper.Services.Get<GameStateMachine>().StartNewGame();

        bootstrapper.Services.Get<EventBus>().Publish(new HeroSpawnedEvent(new EntityId(1), "warrior", new GridPosRPG(0, 0)));

        Assert.True(bootstrapper.Services.Get<AchievementSystem>().IsUnlocked("first_hero"));
        Assert.Equal(1, bootstrapper.Services.Get<GameStatistics>().TotalHeroesSpawned);
    }

    // ─── AchievementSystem: Core Mechanics (Phase6) ───────────────────

    [Fact]
    public void AchievementSystem_RegisterDefaults_HasAchievements()
    {
        var bus = new EventBus();
        var achievements = new AchievementSystem(bus);
        achievements.RegisterDefaults();

        Assert.True(achievements.TotalCount >= 14);
    }

    [Fact]
    public void AchievementSystem_Advance_UnlocksAtThreshold()
    {
        var bus = new EventBus();
        var achievements = new AchievementSystem(bus);
        achievements.RegisterDefaults();
        achievements.Initialize();

        AchievementUnlockedEvent? received = null;
        bus.Subscribe<AchievementUnlockedEvent>(e => received = e);

        achievements.Advance("first_hero", 1);

        Assert.True(achievements.IsUnlocked("first_hero"));
        Assert.NotNull(received);
        Assert.Equal("first_hero", received.Value.AchievementId);
    }

    [Fact]
    public void AchievementSystem_MultiStep_RequiresThreshold()
    {
        var bus = new EventBus();
        var achievements = new AchievementSystem(bus);
        achievements.RegisterDefaults();

        Assert.False(achievements.IsUnlocked("ten_heroes"));

        for (int i = 0; i < 9; i++)
        {
            achievements.Advance("ten_heroes", 1);
        }
        Assert.False(achievements.IsUnlocked("ten_heroes"));

        achievements.Advance("ten_heroes", 1);
        Assert.True(achievements.IsUnlocked("ten_heroes"));
    }

    [Fact]
    public void AchievementSystem_DoesNotDoubleUnlock()
    {
        var bus = new EventBus();
        var achievements = new AchievementSystem(bus);
        achievements.RegisterDefaults();

        achievements.Advance("first_hero", 1);
        Assert.True(achievements.IsUnlocked("first_hero"));

        int unlockCount = 0;
        bus.Subscribe<AchievementUnlockedEvent>(_ => unlockCount++);

        achievements.Advance("first_hero", 1);
        Assert.Equal(0, unlockCount);
    }

    [Fact]
    public void AchievementSystem_GetProgress_ReturnsCorrectValues()
    {
        var bus = new EventBus();
        var achievements = new AchievementSystem(bus);
        achievements.RegisterDefaults();

        achievements.Advance("ten_heroes", 3);

        var progress = achievements.GetProgress("ten_heroes");
        Assert.NotNull(progress);
        Assert.Equal(3, progress.CurrentValue);
        Assert.Equal(10, progress.TargetValue);
        Assert.Equal(0.3f, progress.Progress, 0.01);
    }

    [Fact]
    public void AchievementSystem_EventDriven_HeroSpawnUnlocks()
    {
        var bus = new EventBus();
        var achievements = new AchievementSystem(bus);
        achievements.RegisterDefaults();
        achievements.Initialize();

        bus.Publish(new HeroSpawnedEvent(new EntityId(1), "warrior", new GridPosRPG(0, 0)));
    }

    [Fact]
    public void AchievementSystem_CheckAllTutorials()
    {
        var bus = new EventBus();
        var achievements = new AchievementSystem(bus);
        achievements.RegisterDefaults();
        achievements.Initialize();

        achievements.CheckAllTutorials(true);

        Assert.True(achievements.IsUnlocked("all_tutorials"));
    }
}
