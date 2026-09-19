using ForgeFlow.Core.Data;
using ForgeFlow.Core.Data.Definitions;
using ForgeFlow.Core.Entities;
using ForgeFlow.Core.Events;
using ForgeFlow.Core.Proto;
using ForgeFlow.Core.Systems;

namespace ForgeFlow.Tests;

/// <summary>Tests for DungeonResolver step-by-step dungeon resolution.</summary>
public class DungeonResolverTests
{
    private static (DungeonResolver resolver, DungeonRegistry dungeonRegistry) CreateResolver(string dungeonId = "test_dungeon", int seed = 123)
    {
        var dungeonRegistry = new DungeonRegistry();
        dungeonRegistry.Register(new DungeonDefinition
        {
            Id = dungeonId,
            Tier = 1,
            Theme = DungeonTheme.GoblinCaves,
            Penalty = 20f
        });

        var classRegistry = new ClassRegistry();
        classRegistry.Register(new ClassDefinition
        {
            Id = "warrior", Name = "Warrior", ClassBonus = 10f,
            BaseHealth = 120, BaseMana = 20
        });

        var bus = new EventBus();
        var rm = new ItemManager(bus);
        var vs = new VillagerSystem(bus, rm);
        var terrain = new TerrainGrid(4, 4);
        terrain.GenerateDefault();
        var tileMgr = new TileManager(terrain);
        var entMgr = new EntityManager(tileMgr, vs);
        var resolver = new DungeonResolver(dungeonRegistry, classRegistry, bus, entMgr, seed: seed);

        return (resolver, dungeonRegistry);
    }

    [Fact]
    public void DungeonResolver_StepByStep_LogHasSteps()
    {
        var (resolver, dungeonRegistry) = CreateResolver();
        var dungeon = dungeonRegistry.Get("test_dungeon")!;

        var hero = new HeroEntity(EntityId.Next()) { ClassId = "warrior", Level = 3 };
        hero.Equip(new EquippedItem { ProtoId = "sword", Slot = EquipSlot.Weapon, Tier = 3, Damage = 30 });

        resolver.ResolveDungeon(hero, dungeon, new GridPosRPG(0, 0));

        var log = resolver.LastRunLog;
        Assert.True(log.TotalRooms > 0);
        Assert.True(log.Steps.Count > 0);
        Assert.NotNull(log.DungeonId);
    }

    [Fact]
    public void DungeonResolver_StepByStep_PublishesEncounterEvents()
    {
        var dungeonRegistry = new DungeonRegistry();
        dungeonRegistry.Register(new DungeonDefinition
        {
            Id = "goblin_caves", Tier = 1, Theme = DungeonTheme.GoblinCaves, Penalty = 20f
        });
        var classRegistry = new ClassRegistry();
        classRegistry.Register(new ClassDefinition
        {
            Id = "warrior", Name = "Warrior", ClassBonus = 10f,
            BaseHealth = 120, BaseMana = 20
        });

        var bus = new EventBus();
        int stepCount = 0;
        bus.Subscribe<DungeonEncounterStepEvent>(_ => stepCount++);

        var rm = new ItemManager(bus);
        var vs = new VillagerSystem(bus, rm);
        var terrain = new TerrainGrid(4, 4);
        terrain.GenerateDefault();
        var tileMgr = new TileManager(terrain);
        var entMgr = new EntityManager(tileMgr, vs);
        var resolver = new DungeonResolver(dungeonRegistry, classRegistry, bus, entMgr, seed: 42);
        var dungeon = dungeonRegistry.Get("goblin_caves")!;

        var hero = new HeroEntity(EntityId.Next()) { ClassId = "warrior", Level = 5 };
        hero.Equip(new EquippedItem { ProtoId = "sword", Slot = EquipSlot.Weapon, Tier = 5, Damage = 50 });

        resolver.ResolveDungeon(hero, dungeon, new GridPosRPG(0, 0));

        Assert.True(stepCount > 0);
    }
}
