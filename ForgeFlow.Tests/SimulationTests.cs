using ForgeFlow.Core;
using ForgeFlow.Core.Data;
using ForgeFlow.Core.Data.Definitions;
using ForgeFlow.Core.Proto;
using ForgeFlow.Core.Entities;
using ForgeFlow.Core.Events;
using ForgeFlow.Core.Proto.Logic;
using ForgeFlow.Core.Proto.Prototypes;
using ForgeFlow.Core.Save;
using ForgeFlow.Core.Systems;
using ForgeFlow.Core.Utilities;

namespace ForgeFlow.Tests;

public class SimulationTests
{
    private static (SimulationTicker sim, EventBus bus, ClassRegistry classes, ItemRegistry items, DungeonRegistry dungeons, RecipeRegistry recipes) CreateTestSimulation()
    {
        var eventBus = new EventBus();
        var classRegistry = new ClassRegistry();
        var itemRegistry = new ItemRegistry();
        var recipeRegistry = new RecipeRegistry();
        var dungeonRegistry = new DungeonRegistry();

        // Register test classes
        classRegistry.Register(new ClassDefinition
        {
            Id = "warrior",
            Name = "Warrior",
            PreferredGearSlots = [EquipSlot.Weapon, EquipSlot.ChestArmor, EquipSlot.Helmet],
            ClassBonus = 10f,
            BaseHealth = 120,
            BaseMana = 20,
            UnlockTier = 0
        });
        classRegistry.Register(new ClassDefinition
        {
            Id = "mage",
            Name = "Mage",
            PreferredGearSlots = new() { EquipSlot.Weapon, EquipSlot.Cape, EquipSlot.Accessory },
            PreferredDamageTypes = new() { DamageType.Arcane, DamageType.Fire },
            ClassBonus = 6f,
            BaseHealth = 60,
            BaseMana = 100,
            UnlockTier = 0
        });
        classRegistry.Register(new ClassDefinition
        {
            Id = "spellsword",
            Name = "Spellsword",
            ClassBonus = 11f,
            BaseHealth = 100,
            BaseMana = 80,
            UnlockTier = 5,
            IsHybrid = true,
            FusionSourceClassIds = new() { "warrior", "mage" }
        });

        // Register test items
        itemRegistry.Register(new ItemProto
        {
            Id = "iron_sword_1",
            Tier = 1,
            Category = ItemCategory.Equipment,
            Equipment = new EquipmentData
            {
                Slot = EquipSlot.Weapon,
                Material = MaterialType.Iron,
                DamageType = DamageType.Slashing,
                BaseDamage = 12,
                BaseSpeed = 1.0f,
                CritChance = 0.05f
            }
        });

        // Register test dungeon
        dungeonRegistry.Register(new DungeonDefinition
        {
            Id = "goblin_caves",
            Tier = 1,
            Theme = DungeonTheme.GoblinCaves,
            Penalty = 20f,
            RandomEventModifierRange = 10f,
            RecommendedHeroLevel = 1
        });

        var trafficManager = new TrafficManager();
        var protoRegistry = new ProtoRegistry();
        protoRegistry.RegisterDefaults();
        var protoFactory = new ProtoFactory(protoRegistry, eventBus);
        var itemManager = new ItemManager(eventBus);
        var villagerSystem = new VillagerSystem(eventBus, itemManager);
        var terrain = new TerrainGrid(32, 32);
        terrain.GenerateDefault();
        var tileManager = new TileManager(terrain);
        var entityManager = new EntityManager(tileManager, villagerSystem);
        var pathTraffic = new PathTrafficSystem(trafficManager, entityManager, tileManager, eventBus);
        var tutorialSystem = new TutorialSystem(eventBus, protoFactory);
        var autoEquip = new AutoEquipSystem(eventBus, classRegistry, entityManager);
        var dungeonResolver = new DungeonResolver(dungeonRegistry, classRegistry, eventBus, entityManager, seed: 42);
        var fusionCalculator = new FusionCalculator(classRegistry, itemRegistry, itemManager, eventBus, entityManager);
        var appearanceApplier = new AppearanceApplier(eventBus, entityManager);

        var gatingLimits = new GatingLimits();
        var researchManager = new ResearchManager(eventBus);
        var commandBus = new Core.Commands.CommandBus();
        var pathManager = new PathNodeManager(entityManager, tileManager, villagerSystem, pathTraffic, eventBus, commandBus, tutorialSystem, gatingLimits, researchManager, itemManager);
        var pathGateManager = new PathGateManager(entityManager, tileManager, eventBus, commandBus, tutorialSystem, itemManager, researchManager);
        pathTraffic.SetPathGateManager(pathGateManager);

        var structureManager = new StructureManager(
            entityManager, pathGateManager, pathManager, villagerSystem, itemManager,
            itemRegistry, recipeRegistry,
            gatingLimits, eventBus, commandBus, tutorialSystem, researchManager);

        var worldStateManager = new WorldStateManager(researchManager, entityManager, itemManager, eventBus);
        var dungeonManager = new DungeonManager(entityManager, pathGateManager, villagerSystem, itemManager, eventBus);

        var sim = new SimulationTicker(
            eventBus,
            commandBus,
            pathTraffic, villagerSystem, tutorialSystem,
            entityManager, tileManager, pathManager, pathGateManager, itemManager, gatingLimits,
            structureManager, researchManager, worldStateManager,
            appearanceApplier, dungeonResolver, dungeonManager);

        return (sim, eventBus, classRegistry, itemRegistry, dungeonRegistry, recipeRegistry);
    }

    [Fact]
    public void HeroEntity_CanBeCreatedAndEquipped()
    {
        var hero = new HeroEntity(EntityId.Next())
        {
            Seed = 1,
            ClassId = "warrior",
            Level = 1,
            Position = new GridPosRPG(0, 0)
        };

        var sword = new EquippedItem
        {
            ProtoId = "iron_sword_1",
            Slot = EquipSlot.Weapon,
            Tier = 1,
            Damage = 12
        };

        hero.Equip(sword);

        Assert.Single(hero.Equipment);
        Assert.Equal("iron_sword_1", hero.GetEquippedInSlot(EquipSlot.Weapon)?.ProtoId);
        Assert.Equal(1f, hero.AverageGearTier);
    }

    [Fact]
    public void HeroEntity_EquipReplacesExistingSlot()
    {
        var hero = new HeroEntity(EntityId.Next()) { ClassId = "warrior" };

        hero.Equip(new EquippedItem { ProtoId = "iron_sword_1", Slot = EquipSlot.Weapon, Tier = 1 });
        hero.Equip(new EquippedItem { ProtoId = "steel_sword_2", Slot = EquipSlot.Weapon, Tier = 2 });

        Assert.Single(hero.Equipment);
        Assert.Equal("steel_sword_2", hero.GetEquippedInSlot(EquipSlot.Weapon)?.ProtoId);
    }

    [Fact]
    public void HeroEntity_UpgradeAllGear_IncreasesStats()
    {
        var hero = new HeroEntity(EntityId.Next()) { ClassId = "warrior" };
        hero.Equip(new EquippedItem
        {
            ProtoId = "iron_sword_1",
            Slot = EquipSlot.Weapon,
            Tier = 1,
            Damage = 12f,
            Defense = 0f,
            Speed = 1.0f,
            CritChance = 0.05f
        });

        hero.UpgradeAllGear();

        var weapon = hero.GetEquippedInSlot(EquipSlot.Weapon);
        Assert.NotNull(weapon);
        Assert.Equal(2, weapon.Tier);
        Assert.True(weapon.Damage > 12f);
    }

    [Fact]
    public void PathSegment_OutputPositionIsCorrect()
    {
        var segment = new PathSegmentLogic(EntityId.Next())
        {
            Position = new GridPosRPG(5, 5),
            Facing = Direction.East
        };

        Assert.Equal(new GridPosRPG(6, 5), segment.OutputPosition);
    }

    [Fact]
    public void GridPosition_NeighborDirections()
    {
        var pos = new GridPosRPG(3, 3);

        Assert.Equal(new GridPosRPG(3, 4), pos.Neighbor(Direction.North));
        Assert.Equal(new GridPosRPG(4, 3), pos.Neighbor(Direction.East));
        Assert.Equal(new GridPosRPG(3, 2), pos.Neighbor(Direction.South));
        Assert.Equal(new GridPosRPG(2, 3), pos.Neighbor(Direction.West));
    }

    [Fact]
    public void GridPosition_Equality()
    {
        var a = new GridPosRPG(1, 2);
        var b = new GridPosRPG(1, 2);
        var c = new GridPosRPG(3, 4);

        Assert.Equal(a, b);
        Assert.NotEqual(a, c);
        Assert.True(a == b);
        Assert.True(a != c);
    }

    [Fact]
    public void PathTraffic_HeroMovesAlongPath()
    {
        var (sim, _, _, _, _, _) = CreateTestSimulation();

        var seg1 = sim.PathNodeManager.AddPathSegment(new GridPosRPG(0, 0), Direction.East)!;
        var seg2 = sim.PathNodeManager.AddPathSegment(new GridPosRPG(1, 0), Direction.East)!;

        var hero = new HeroEntity(EntityId.Next())
        {
            ClassId = "warrior",
            Position = new GridPosRPG(0, 0),
            State = HeroState.OnPath,
            CurrentPathSegmentId = seg1.Id
        };
        seg1.TryAddOccupant(hero.Id);
        sim.EntityManager.AddHero(hero);

        // Simulate enough ticks for the hero to advance
        for (int i = 0; i < 120; i++)
        {
            sim.Update(SimulationTicker.FixedTimeStep);
        }

        // Hero should have moved to segment 2 or beyond
        Assert.True(hero.Position.X >= 1 || hero.CurrentPathSegmentId == seg2.Id || hero.CurrentPathSegmentId == null);
    }

    [Fact]
    public void AutoEquipSystem_HeroEquipsFromStructure()
    {
        var (sim, bus, classes, items, _, _) = CreateTestSimulation();

        var hero = new HeroEntity(EntityId.Next())
        {
            ClassId = "warrior",
            Position = new GridPosRPG(1, 0),
            State = HeroState.OnPath
        };
        sim.EntityManager.AddHero(hero);

        // Place a forge with output at hero's position
        var forge = new ForgeLogic(EntityId.Next());
        forge.Position = new GridPosRPG(1, 1);
        forge.OutputDirection = Direction.South;

        // Manually add an item to the forge output via ItemManager
        var swordItem = sim.ItemManager.CreateEquipment(
            "iron_sword_1", 1, EquipSlot.Weapon, 12f, 0f, 1.0f, 0.05f);
        forge.OutputQueue.Enqueue(swordItem);
        sim.EntityManager.AddStructure(forge, forge.Position);

        // One tick should trigger auto-equip
        sim.Update(SimulationTicker.FixedTimeStep);

        Assert.True(hero.Equipment.Count > 0);
        Assert.Equal("iron_sword_1", hero.GetEquippedInSlot(EquipSlot.Weapon)?.ProtoId);
    }

    [Fact]
    public void DungeonResolver_ResolvesSuccessOrFailure()
    {
        var (sim, bus, classes, _, dungeons, _) = CreateTestSimulation();

        var resolver = new DungeonResolver(dungeons, classes, bus, sim.EntityManager, seed: 42);
        var dungeon = dungeons.Get("goblin_caves")!;

        int successes = 0;
        int failures = 0;

        // Test with varying gear tiers to get a mix of outcomes
        for (int tier = 1; tier <= 4; tier++)
        {
            for (int i = 0; i < 25; i++)
            {
                var testHero = new HeroEntity(EntityId.Next()) { ClassId = "warrior", Level = 1 };
                testHero.Equip(new EquippedItem { ProtoId = "iron_sword_1", Slot = EquipSlot.Weapon, Tier = tier });

                bool result = resolver.ResolveDungeon(testHero, dungeon, new GridPosRPG(0, 0));
                if (result) { successes++; }
                else { failures++; }
            }
        }

        // With varying gear tiers, should have a mix of outcomes
        Assert.True(successes > 0, "Expected at least some successes");
        Assert.True(failures > 0, "Expected at least some failures");

        // Verify step-by-step log was populated
        var lastLog = resolver.LastRunLog;
        Assert.True(lastLog.TotalRooms > 0, "Expected dungeon run log to have rooms");
        Assert.True(lastLog.Steps.Count > 0, "Expected dungeon run log to have steps");
    }

    [Fact]
    public void FusionCalculator_FusesTwoSameLevelHeroes()
    {
        var (sim, bus, classes, items, _, _) = CreateTestSimulation();

        var altar = new FusionAltarLogic(EntityId.Next());
        altar.Position = new GridPosRPG(5, 5);
        altar.OutputDirection = Direction.East;
        sim.EntityManager.AddStructure(altar, altar.Position);

        var hero1 = new HeroEntity(EntityId.Next()) { ClassId = "warrior", Level = 5 };
        var hero2 = new HeroEntity(EntityId.Next()) { ClassId = "mage", Level = 5 };
        sim.EntityManager.AddHero(hero1);
        sim.EntityManager.AddHero(hero2);

        altar.QueueHero(hero1.Id);
        altar.QueueHero(hero2.Id);

        bool fusionEventFired = false;
        bus.Subscribe<HeroFusedEvent>(e => fusionEventFired = true);

        sim.Update(SimulationTicker.FixedTimeStep);

        Assert.True(fusionEventFired);
        Assert.Single(sim.EntityManager.Heroes);
        Assert.Equal("spellsword", sim.EntityManager.Heroes[0].ClassId);
        Assert.Equal(6, sim.EntityManager.Heroes[0].Level);
    }

    [Fact]
    public void SimulationTicker_PauseAndResume()
    {
        var (sim, _, _, _, _, _) = CreateTestSimulation();

        Assert.False(sim.IsPaused);

        sim.Pause();
        Assert.True(sim.IsPaused);

        var ticksBefore = sim.TickCount;
        sim.Update(1.0f);
        Assert.Equal(ticksBefore, sim.TickCount);

        sim.Resume();
        sim.Update(SimulationTicker.FixedTimeStep);
        Assert.True(sim.TickCount > ticksBefore);
    }

    [Fact]
    public void SimulationTicker_LongDeltaTime_IsCappedAtMaxCatchUp()
    {
        var (sim, bus, _, _, _, _) = CreateTestSimulation();

        sim.MaxCatchUpTicksPerFrame = 5;
        var ticksBefore = sim.TickCount;

        int lagDroppedCount = 0;
        int totalDropped = 0;
        bus.Subscribe<SimulationLagDroppedEvent>(e =>
        {
            lagDroppedCount++;
            totalDropped += e.DroppedTicks;
        });

        // 10 seconds = 600 ticks at 60fps, but cap is 5
        sim.Update(10f);

        Assert.Equal(ticksBefore + 5UL, sim.TickCount);
        Assert.Equal(1, lagDroppedCount);
        Assert.True(totalDropped > 0);
    }

    [Fact]
    public void EventBus_PublishAndSubscribe()
    {
        var bus = new EventBus();
        HeroSpawnedEvent? received = null;

        bus.Subscribe<HeroSpawnedEvent>(e => received = e);
        bus.Publish(new HeroSpawnedEvent(new EntityId(1), "warrior", new GridPosRPG(0, 0)));

        Assert.NotNull(received);
        Assert.Equal(1ul, received.Value.HeroId);
        Assert.Equal("warrior", received.Value.ClassId);
    }

    [Fact]
    public void EventBus_Unsubscribe()
    {
        var bus = new EventBus();
        int callCount = 0;
        Action<HeroSpawnedEvent> handler = _ => callCount++;

        bus.Subscribe(handler);
        bus.Publish(new HeroSpawnedEvent(new EntityId(1), "warrior", new GridPosRPG(0, 0)));
        Assert.Equal(1, callCount);

        bus.Unsubscribe(handler);
        bus.Publish(new HeroSpawnedEvent(new EntityId(2), "mage", new GridPosRPG(1, 0)));
        Assert.Equal(1, callCount);
    }

    [Fact]
    public void TrafficManager_BlocksWhenFull()
    {
        var tm = new TrafficManager(maxOccupantsPerSegment: 1);

        Assert.True(tm.CanEnterSegment(0, 100));
        tm.EnterSegment(0, 100);
        Assert.False(tm.CanEnterSegment(0, 200));
        Assert.True(tm.CanEnterSegment(0, 100)); // Same hero can re-enter

        tm.LeaveSegment(0, 100);
        Assert.True(tm.CanEnterSegment(0, 200));
    }

    [Fact]
    public void ItemInstance_ToEquipped_CopiesAllFields()
    {
        var bus = new EventBus();
        var im = new ItemManager(bus);
        var item = im.CreateEquipment(
            "iron_sword_1", 3, EquipSlot.Weapon, 30f, 5f, 1.2f, 0.1f, "fire");

        var equipped = item.ToEquipped();

        Assert.Equal(item.ProtoId, equipped.ProtoId);
        Assert.Equal(item.Slot, equipped.Slot);
        Assert.Equal(item.Tier, equipped.Tier);
        Assert.Equal(item.Damage, equipped.Damage);
        Assert.Equal(item.Defense, equipped.Defense);
        Assert.Equal(item.Speed, equipped.Speed);
        Assert.Equal(item.CritChance, equipped.CritChance);
        Assert.Equal(item.SpecialEffectId, equipped.SpecialEffectId);
    }

    [Fact]
    public void AppearanceApplier_UpdatesAppearanceFromGear()
    {
        var bus = new EventBus();
        var rm = new ItemManager(bus);
        var vs = new VillagerSystem(bus, rm);
        var terrain = new TerrainGrid(4, 4);
        terrain.GenerateDefault();
        var tileMgr = new TileManager(terrain);
        var entMgr = new EntityManager(tileMgr, vs);
        var applier = new AppearanceApplier(bus, entMgr);
        var hero = new HeroEntity(EntityId.Next()) { ClassId = "warrior", Level = 1 };

        hero.Equip(new EquippedItem { ProtoId = "iron_helm_1", Slot = EquipSlot.Helmet, Tier = 1 });
        hero.Equip(new EquippedItem { ProtoId = "iron_chestplate_1", Slot = EquipSlot.ChestArmor, Tier = 1 });
        hero.Equip(new EquippedItem { ProtoId = "iron_sword_1", Slot = EquipSlot.Weapon, Tier = 5 });

        applier.ApplyGearToAppearance(hero);

        Assert.Equal("iron_helm_1", hero.Appearance.Helmet);
        Assert.Equal("iron_chestplate_1", hero.Appearance.ChestArmor);
        Assert.Equal("iron_sword_1", hero.Appearance.WeaponSheath);
        Assert.Equal("tier5_glow", hero.Appearance.AuraEffect);
    }

    [Fact]
    public void PathNetwork_AutoLinks()
    {
        var (sim, _, _, _, _, _) = CreateTestSimulation();

        var seg1 = sim.PathNodeManager.AddPathSegment(new GridPosRPG(0, 0), Direction.East)!;
        var seg2 = sim.PathNodeManager.AddPathSegment(new GridPosRPG(1, 0), Direction.East)!;
        var seg3 = sim.PathNodeManager.AddPathSegment(new GridPosRPG(2, 0), Direction.East)!;

        Assert.Equal(seg2.Id, seg1.NextSegmentId);
        Assert.Equal(seg3.Id, seg2.NextSegmentId);
        Assert.Equal(seg1.Id, seg2.PrevSegmentId);
        Assert.Equal(seg2.Id, seg3.PrevSegmentId);
    }

    [Fact]
    public void Pathfinding_FindsPath()
    {
        var pathSegments = new Dictionary<ulong, PathSegmentLogic>();
        var pathPositionIndex = new Dictionary<GridPosRPG, ulong>();

        var s1 = new PathSegmentLogic(EntityId.Next()) { Position = new GridPosRPG(0, 0), Facing = Direction.East };
        var s2 = new PathSegmentLogic(EntityId.Next()) { Position = new GridPosRPG(1, 0), Facing = Direction.East };
        var s3 = new PathSegmentLogic(EntityId.Next()) { Position = new GridPosRPG(2, 0), Facing = Direction.East };

        s1.NextSegmentId = s2.Id;
        s2.NextSegmentId = s3.Id;

        pathSegments[s1.Id] = s1;
        pathSegments[s2.Id] = s2;
        pathSegments[s3.Id] = s3;
        pathPositionIndex[s1.Position] = s1.Id;
        pathPositionIndex[s2.Position] = s2.Id;
        pathPositionIndex[s3.Position] = s3.Id;

        var pathfinding = new Pathfinding();
        var path = pathfinding.FindPath(new GridPosRPG(0, 0), new GridPosRPG(2, 0), pathPositionIndex, pathSegments);

        Assert.NotNull(path);
        Assert.Equal(3, path.Count);
    }

    [Fact]
    public void SaveManager_SerializeAndDeserialize()
    {
        var (sim, _, _, _, _, _) = CreateTestSimulation();

        var hero = new HeroEntity(EntityId.Next())
        {
            ClassId = "warrior",
            Level = 10,
            Position = new GridPosRPG(3, 4),
            Morale = 1.5f
        };
        hero.Equip(new EquippedItem { ProtoId = "iron_sword_1", Slot = EquipSlot.Weapon, Tier = 3, Damage = 30 });
        sim.EntityManager.AddHero(hero);

        sim.PathNodeManager.AddPathSegment(new GridPosRPG(0, 0), Direction.East);
        sim.PathNodeManager.AddPathSegment(new GridPosRPG(1, 0), Direction.East);

        var saveManager = new SaveManager();
        var saveData = saveManager.CreateSaveData(sim);
        var json = saveManager.SerializeToJson(saveData);
        var loaded = saveManager.DeserializeFromJson(json);

        Assert.NotNull(loaded);
        Assert.Single(loaded.Heroes);
        Assert.Equal("warrior", loaded.Heroes[0].ClassId);
        Assert.Equal(10, loaded.Heroes[0].Level);
        Assert.Single(loaded.Heroes[0].Equipment);
        Assert.Equal(2, loaded.Paths.Count);
    }

    [Fact]
    public void GameBootstrapper_BootstrapsSuccessfully()
    {
        ServiceLocator.Clear();
        var bootstrapper = new GameBootstrapper();

        var sim = bootstrapper.Bootstrap();

        Assert.NotNull(sim);
        Assert.True(bootstrapper.Services.Get<ItemRegistry>().Count > 0);
        Assert.True(bootstrapper.Services.Get<ClassRegistry>().Count > 0);
        Assert.True(bootstrapper.Services.Get<DungeonRegistry>().Count > 0);
        Assert.True(bootstrapper.Services.Get<RecipeRegistry>().Count > 0);
    }

    [Fact]
    public void SpawnerStructure_SpawnsHero()
    {
        var spawner = new VillageSpawnerLogic(EntityId.Next())
        {
            SpawnInterval = 1.0f,
            Position = new GridPosRPG(0, 0)
        };

        var villager = spawner.SpawnVillager();

        Assert.NotNull(villager);
        Assert.Single(spawner.PendingVillagers);
    }

    [Fact]
    public void ClassRegistry_Queries()
    {
        var registry = new ClassRegistry();
        registry.Register(new ClassDefinition { Id = "warrior", Name = "Warrior", UnlockTier = 0 });
        registry.Register(new ClassDefinition { Id = "paladin", Name = "Paladin", UnlockTier = 3 });
        registry.Register(new ClassDefinition
        {
            Id = "spellsword", Name = "Spellsword", UnlockTier = 5, IsHybrid = true,
            FusionSourceClassIds = new() { "warrior", "mage" }
        });

        Assert.Equal(3, registry.Count);
        Assert.Equal(2, registry.GetUnlockedAtTier(3).Count());
        Assert.Single(registry.GetHybrids());
    }

    [Fact]
    public void MiningNode_ProducesResources()
    {
        var eventBus = new EventBus();
        var node = new ResourceNodeLogic(EntityId.Next())
        {
            ResourceId = "iron_ore",
            MaxYield = 100,
            CurrentYield = 100
        };
        var miner = new MiningRecipeEntity(EntityId.Next())
        {
            Position = new GridPosRPG(0, 0)
        };
        miner.TargetResourceId = "iron_ore";
        miner.GatherInterval = 1.0f;
        miner.MaxWorkerCapacity = 2;
        miner.AllocateSlots();
        miner.Initialize(node);

        // Accept a worker into the gathering structure
        int slot = miner.AcceptWorker(1, null);
        Assert.True(slot >= 0, "Worker should be accepted");

        // Tick past the gather interval
        miner.Tick(1.1f);

        Assert.True(miner.WorkerSlots[slot].CycleComplete, "Worker cycle should be complete");
        Assert.True(miner.WorkerSlots[slot].PendingOutputAmount > 0, "Should have pending output");

        // Simulate what StructureManager does: harvest from node
        int harvested = node.Harvest(miner.WorkerSlots[slot].PendingOutputAmount);
        Assert.True(harvested > 0, "Node should have been harvested");
        Assert.True(node.CurrentYield < 100, "Node should have been harvested");
    }

    [Fact]
    public void ServiceLocator_RegisterAndRetrieve()
    {
        ServiceLocator.Clear();

        var bus = new EventBus();
#pragma warning disable CS0618 // Self-test of the sanctioned fallback ServiceLocator API.
        ServiceLocator.Register(bus);

        Assert.True(ServiceLocator.IsRegistered<EventBus>());
        Assert.Same(bus, ServiceLocator.Get<EventBus>());
        Assert.Null(ServiceLocator.TryGet<TrafficManager>());
#pragma warning restore CS0618

        ServiceLocator.Clear();
    }

    [Fact]
    public void CreatePathLine_CreatesConnectedSegments()
    {
        var (sim, _, _, _, _, _) = CreateTestSimulation();

        var segments = sim.PathNodeManager.CreatePathLine(new GridPosRPG(0, 0), new GridPosRPG(3, 0));

        Assert.Equal(4, segments.Count);
        Assert.Equal(new GridPosRPG(0, 0), segments[0].Position);
        Assert.Equal(new GridPosRPG(3, 0), segments[3].Position);

        // All should face East for a horizontal line
        foreach (var seg in segments)
        {
            Assert.Equal(Direction.East, seg.Facing);
        }

        // Segments should be auto-linked
        Assert.NotNull(segments[0].NextSegmentId);
        Assert.Equal(segments[1].Id, segments[0].NextSegmentId);
    }

    [Fact]
    public void PlaceStructure_SetsPositionAndAdds()
    {
        var (sim, bus, classes, items, dungeons, _) = CreateTestSimulation();

        var spawner = new VillageSpawnerLogic(EntityId.Next());

        spawner.SpawnInterval = 5.0f;

        var pos = new GridPosRPG(3, 3);
        sim.EntityManager.AddStructure(spawner, pos);

        Assert.Equal(pos, spawner.Position);
        Assert.True(sim.EntityManager.Structures.ContainsKey(spawner.Id));
        Assert.True(sim.TileManager.GetStructureIdAt(pos).HasValue);
    }

    [Fact]
    public void SpawnerStructure_AutoAddsHeroesToSimulation()
    {
        var (sim, bus, classes, items, _, _) = CreateTestSimulation();

        // Place spawner
        var spawner = new VillageSpawnerLogic(EntityId.Next());

        spawner.SpawnInterval = 0.5f;
        spawner.OutputDirection = Direction.East;
        sim.EntityManager.AddStructure(spawner, new GridPosRPG(0, 0));

        // Create path segments for the villager to walk on
        sim.PathNodeManager.AddPathSegment(new GridPosRPG(2, 0), Direction.East);
        sim.PathNodeManager.AddPathSegment(new GridPosRPG(3, 0), Direction.East);

        // Place an Exit PathGate adjacent to spawner, facing toward the path
        sim.PathGateManager.AddPathGate(new GridPosRPG(1, 0), Direction.East);

        Assert.Empty(sim.VillagerSystem.Villagers);

        // Tick enough for the spawner to fire
        for (int i = 0; i < 60; i++)
        {
            sim.Update(SimulationTicker.FixedTimeStep);
        }

        // Villagers should have been auto-added to the simulation
        Assert.NotEmpty(sim.VillagerSystem.Villagers);
    }

    [Fact]
    public void SpawnerStructure_HeroSpawnedEventFires()
    {
        var (sim, bus, classes, _, _, _) = CreateTestSimulation();

        // Place spawner
        var spawner = new VillageSpawnerLogic(EntityId.Next());

        spawner.SpawnInterval = 0.5f;
        spawner.OutputDirection = Direction.East;
        sim.EntityManager.AddStructure(spawner, new GridPosRPG(0, 0));

        // Create path + Exit gate so spawner is unblocked
        sim.PathNodeManager.AddPathSegment(new GridPosRPG(2, 0), Direction.East);
        sim.PathGateManager.AddPathGate(new GridPosRPG(1, 0), Direction.East);

        VillagerSpawnedEvent? received = null;
        bus.Subscribe<VillagerSpawnedEvent>(e => received = e);

        for (int i = 0; i < 60; i++)
        {
            sim.Update(SimulationTicker.FixedTimeStep);
        }

        Assert.NotNull(received);
    }
}



