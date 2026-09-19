using ForgeFlow.Core.Data;
using ForgeFlow.Core.Entities;
using ForgeFlow.Core.Events;
using ForgeFlow.Core.Proto;
using ForgeFlow.Core.Proto.Logic;

namespace ForgeFlow.Core.Systems;

/// <summary>Type of encounter within a dungeon room.</summary>
public enum EncounterType
{
    MobRoom,
    TrapRoom,
    MiniBoss,
    BossFight,
    TreasureRoom,
    RestShrine
}

/// <summary>
/// A single step/room within a dungeon run. Struct for zero-alloc hot-path usage.
/// Each step has its own encounter type, difficulty, and outcome.
/// </summary>
public readonly struct DungeonEncounterStep
{
    public int RoomIndex { get; }
    public EncounterType Type { get; }
    public float Difficulty { get; }
    public float HeroRoll { get; }
    public float Threshold { get; }
    public bool Survived { get; }
    public int DamageDealt { get; }
    public int DamageTaken { get; }
    public string? LootDropId { get; }

    public DungeonEncounterStep(
        int roomIndex, EncounterType type, float difficulty,
        float heroRoll, float threshold, bool survived,
        int damageDealt, int damageTaken, string? lootDropId)
    {
        RoomIndex = roomIndex;
        Type = type;
        Difficulty = difficulty;
        HeroRoll = heroRoll;
        Threshold = threshold;
        Survived = survived;
        DamageDealt = damageDealt;
        DamageTaken = damageTaken;
        LootDropId = lootDropId;
    }
}

/// <summary>
/// Full run log for a dungeon attempt. Contains every room/step result.
/// Reused via object pool to avoid allocation in hot paths.
/// </summary>
public sealed class DungeonRunLog
{
    public ulong HeroId { get; set; }
    public string DungeonId { get; set; } = string.Empty;
    public bool OverallSuccess { get; set; }
    public int RoomsCleared { get; set; }
    public int TotalRooms { get; set; }
    public int TotalDamageDealt { get; set; }
    public int TotalDamageTaken { get; set; }
    public readonly List<DungeonEncounterStep> Steps = new();

    public void Reset()
    {
        HeroId = 0;
        DungeonId = string.Empty;
        OverallSuccess = false;
        RoomsCleared = 0;
        TotalRooms = 0;
        TotalDamageDealt = 0;
        TotalDamageTaken = 0;
        Steps.Clear();
    }
}

public sealed class DungeonResolver : IGameSystem, IDisposable
{
    private readonly DungeonRegistry _dungeonRegistry;
    private readonly ClassRegistry _classRegistry;
    private readonly EventBus _eventBus;
    private readonly EntityManager _entityManager;
    private readonly Random _random;

    // Zero-alloc: pooled run log, reused each resolution
    private readonly DungeonRunLog _runLog = new();

    // Cached encounter layout per dungeon tier (avoids re-creating each tick)
    private readonly Dictionary<int, EncounterType[]> _cachedLayouts = new();

    /// <summary>Optional lifecycle system for difficulty-based ability gain.</summary>
    public WorkerLifecycleSystem? LifecycleSystem { get; set; }

    public DungeonRunLog LastRunLog => _runLog;

    public DungeonResolver(
        DungeonRegistry dungeonRegistry,
        ClassRegistry classRegistry,
        EventBus eventBus,
        EntityManager entityManager,
        int? seed = null)
    {
        _dungeonRegistry = dungeonRegistry;
        _classRegistry = classRegistry;
        _eventBus = eventBus;
        _entityManager = entityManager;
        _random = seed.HasValue ? new Random(seed.Value) : new Random();

        _eventBus.Subscribe<SimulationTickEvent>(OnTick);
    }

    public void Dispose()
    {
        _eventBus.Unsubscribe<SimulationTickEvent>(OnTick);
    }

    private void OnTick(SimulationTickEvent e) => Tick(_entityManager.Heroes, _entityManager);

    public void Tick(
        IReadOnlyList<HeroEntity> heroes,
        EntityManager entityManager)
    {
        for (int i = 0; i < heroes.Count; i++)
        {
            var hero = heroes[i];
            if (hero.State != HeroState.OnPath) continue;

            var structure = entityManager.GetStructureAt(hero.Position);
            if (structure is not DungeonPortalLogic portal) continue;

            var dungeonDef = portal.GetDungeonDefinition();
            if (dungeonDef == null) continue;

            ResolveDungeon(hero, dungeonDef, portal.Position);
        }
    }

    public bool ResolveDungeon(
        HeroEntity hero,
        Data.Definitions.DungeonDefinition dungeon,
        GridPosRPG portalPosition)
    {
        _runLog.Reset();
        _runLog.HeroId = hero.Id;
        _runLog.DungeonId = dungeon.Id;

        float classBonus = 0f;
        var classDef = _classRegistry.Get(hero.ClassId);
        if (classDef != null)
        {
            classBonus = classDef.ClassBonus;
        }

        // Determine room layout based on dungeon tier
        var layout = GetEncounterLayout(dungeon.Tier);
        _runLog.TotalRooms = layout.Length;

        float heroPower = (hero.AverageGearTier * 12f) + classBonus + (hero.Morale * 5f);
        int heroHp = 100 + (hero.Level * 15);
        bool alive = true;

        for (int roomIdx = 0; roomIdx < layout.Length; roomIdx++)
        {
            if (!alive) break;

            var encounterType = layout[roomIdx];
            float roomDifficulty = CalculateRoomDifficulty(dungeon, encounterType, roomIdx);
            float randomMod = (float)(_random.NextDouble() * 2 - 1) * dungeon.RandomEventModifierRange;
            float heroRoll = heroPower + randomMod;
            float threshold = roomDifficulty;

            bool survived;
            int damageDealt = 0;
            int damageTaken = 0;
            string? lootDrop = null;

            switch (encounterType)
            {
                case EncounterType.MobRoom:
                    survived = heroRoll > threshold;
                    damageDealt = survived ? (int)(roomDifficulty * 0.8f) : (int)(roomDifficulty * 0.4f);
                    damageTaken = survived ? (int)(roomDifficulty * 0.3f) : (int)(roomDifficulty * 0.9f);
                    break;

                case EncounterType.TrapRoom:
                    float trapAvoidance = heroPower * 0.6f + (float)_random.NextDouble() * 20f;
                    survived = trapAvoidance > threshold * 0.7f;
                    damageTaken = survived ? (int)(roomDifficulty * 0.15f) : (int)(roomDifficulty * 0.6f);
                    break;

                case EncounterType.MiniBoss:
                    survived = heroRoll * 0.9f > threshold;
                    damageDealt = survived ? (int)(roomDifficulty * 1.2f) : (int)(roomDifficulty * 0.5f);
                    damageTaken = survived ? (int)(roomDifficulty * 0.5f) : (int)(roomDifficulty * 1.5f);
                    if (survived && dungeon.PossibleDropItemIds.Count > 0)
                    {
                        int dropIdx = _random.Next(dungeon.PossibleDropItemIds.Count);
                        lootDrop = dungeon.PossibleDropItemIds[dropIdx];
                    }
                    break;

                case EncounterType.BossFight:
                    float bossRoll = heroPower + (hero.DungeonSuccessStreak * 2f) + randomMod;
                    survived = bossRoll > threshold * 1.1f;
                    damageDealt = survived ? (int)(roomDifficulty * 1.8f) : (int)(roomDifficulty * 0.6f);
                    damageTaken = survived ? (int)(roomDifficulty * 0.7f) : (int)(roomDifficulty * 2.0f);
                    if (survived && dungeon.PossibleDropItemIds.Count > 0)
                    {
                        int dropIdx = _random.Next(dungeon.PossibleDropItemIds.Count);
                        lootDrop = dungeon.PossibleDropItemIds[dropIdx];
                    }
                    break;

                case EncounterType.TreasureRoom:
                    survived = true;
                    if (dungeon.PossibleDropItemIds.Count > 0)
                    {
                        int dropIdx = _random.Next(dungeon.PossibleDropItemIds.Count);
                        lootDrop = dungeon.PossibleDropItemIds[dropIdx];
                    }
                    break;

                case EncounterType.RestShrine:
                    survived = true;
                    heroHp = Math.Min(heroHp + 25, 100 + hero.Level * 15);
                    break;

                default:
                    survived = heroRoll > threshold;
                    break;
            }

            heroHp -= damageTaken;
            if (heroHp <= 0) survived = false;
            if (!survived) alive = false;

            var step = new DungeonEncounterStep(
                roomIdx, encounterType, roomDifficulty,
                heroRoll, threshold, survived,
                damageDealt, damageTaken, lootDrop);

            _runLog.Steps.Add(step);
            _runLog.TotalDamageDealt += damageDealt;
            _runLog.TotalDamageTaken += damageTaken;

            if (survived) _runLog.RoomsCleared++;

            _eventBus.Publish(new DungeonEncounterStepEvent(
                hero.Id, dungeon.Id, roomIdx, encounterType, survived, damageDealt, damageTaken, lootDrop));
        }

        _runLog.OverallSuccess = alive;

        if (alive)
        {
            hero.Level++;
            hero.UpgradeAllGear();
            hero.DungeonSuccessStreak++;
            hero.DungeonFailStreak = 0;
            hero.Morale = Math.Min(hero.Morale + 0.1f, 2.0f);
            hero.State = HeroState.OnPath;

            // Phase 10: Gold reward based on dungeon tier
            int goldReward = EconomyConfig.GetDungeonGoldReward(dungeon.Tier);
            hero.GoldEarned += goldReward;

            // Phase 10: Difficulty-based ability gain from dungeon
            LifecycleSystem?.ResolveDungeonAbilityGain(hero);

            _eventBus.Publish(new DungeonCompletedEvent(hero.Id, dungeon.Id, true, hero.Level, portalPosition));
            _eventBus.Publish(new GoldChangedEvent(0, goldReward, "dungeon_reward"));
        }
        else
        {
            hero.State = HeroState.Ghost;
            hero.DungeonFailStreak++;
            hero.DungeonSuccessStreak = 0;

            _eventBus.Publish(new DungeonCompletedEvent(hero.Id, dungeon.Id, false, hero.Level, portalPosition));
            _eventBus.Publish(new HeroDiedEvent(hero.Id, dungeon.Id, portalPosition));
        }

        return alive;
    }

    private EncounterType[] GetEncounterLayout(int tier)
    {
        if (_cachedLayouts.TryGetValue(tier, out var cached)) return cached;

        // Tiers 1-2: 3 rooms, Tiers 3-4: 5 rooms, Tier 5+: 7 rooms
        EncounterType[] layout;
        if (tier <= 2)
        {
            layout = new[]
            {
                EncounterType.MobRoom,
                EncounterType.TrapRoom,
                EncounterType.BossFight
            };
        }
        else if (tier <= 4)
        {
            layout = new[]
            {
                EncounterType.MobRoom,
                EncounterType.TrapRoom,
                EncounterType.MiniBoss,
                EncounterType.RestShrine,
                EncounterType.BossFight
            };
        }
        else
        {
            layout = new[]
            {
                EncounterType.MobRoom,
                EncounterType.TrapRoom,
                EncounterType.MobRoom,
                EncounterType.MiniBoss,
                EncounterType.TreasureRoom,
                EncounterType.RestShrine,
                EncounterType.BossFight
            };
        }

        _cachedLayouts[tier] = layout;
        return layout;
    }

    private float CalculateRoomDifficulty(Data.Definitions.DungeonDefinition dungeon, EncounterType type, int roomIdx)
    {
        float baseDifficulty = dungeon.Penalty * 0.5f + (dungeon.Tier * 3f) + (roomIdx * 2f);
        float typeMultiplier = type switch
        {
            EncounterType.MobRoom => 1.0f,
            EncounterType.TrapRoom => 0.8f,
            EncounterType.MiniBoss => 1.3f,
            EncounterType.BossFight => 1.5f,
            EncounterType.TreasureRoom => 0.0f,
            EncounterType.RestShrine => 0.0f,
            _ => 1.0f
        };
        return baseDifficulty * typeMultiplier;
    }
}
