using ForgeFlow.Core.Entities;
using ForgeFlow.Core.Events;

namespace ForgeFlow.Core.Systems;

/// <summary>
/// Defines a profession-specific wear-out condition.
/// Each profession wears out differently:
/// Forester/Miner → tool durability, HerbGatherer → carry capacity,
/// Researcher → research complete, Warrior/Cleric/etc → stamina from dungeon.
/// </summary>
public static class WearOutConditions
{
    /// <summary>
    /// Tool-use rate per second for tool-based professions.
    /// </summary>
    public static float GetToolWearRate(WorkerProfession profession) => profession switch
    {
        WorkerProfession.Forester => 2.0f,
        WorkerProfession.Miner => 2.5f,
        WorkerProfession.Guard => 1.0f,
        _ => 0f
    };

    /// <summary>
    /// Stamina drain rate per second for stamina-based professions.
    /// </summary>
    public static float GetStaminaDrainRate(WorkerProfession profession) => profession switch
    {
        WorkerProfession.HerbGatherer => 1.5f,
        WorkerProfession.Ranger => 1.2f,
        WorkerProfession.Warrior => 2.0f,
        WorkerProfession.Cleric => 1.0f,
        WorkerProfession.Mage => 1.8f,
        _ => 0f
    };

    /// <summary>
    /// Carry load gain per second for carry-based professions.
    /// </summary>
    public static float GetCarryGainRate(WorkerProfession profession) => profession switch
    {
        WorkerProfession.HerbGatherer => 3.0f,
        _ => 0f
    };

    /// <summary>Checks whether a worker should become worn out.</summary>
    public static WearOutReason Check(HeroEntity worker)
    {
        switch (worker.Profession)
        {
            case WorkerProfession.Forester:
            case WorkerProfession.Miner:
            case WorkerProfession.Guard:
                if (worker.ToolDurability <= 0f)
                    return WearOutReason.ToolBroken;
                break;

            case WorkerProfession.Warrior:
            case WorkerProfession.Cleric:
            case WorkerProfession.Ranger:
            case WorkerProfession.Mage:
                if (worker.Stamina <= 0f)
                    return WearOutReason.StaminaDepleted;
                break;

            case WorkerProfession.HerbGatherer:
                if (worker.CarryLoad >= worker.MaxCarryCapacity)
                    return WearOutReason.CarryCapacityFull;
                if (worker.Stamina <= 0f)
                    return WearOutReason.StaminaDepleted;
                break;

            case WorkerProfession.Researcher:
                // Research complete is signaled by external system
                break;
        }

        return WearOutReason.None;
    }
}

/// <summary>
/// Central system managing the worker lifecycle loop:
/// Work → Wear-Out → Leave via output → Maintenance → Return.
/// Also handles difficulty-based level downgrade/ability gain after job completion.
/// Zero-alloc hot path.
/// </summary>
public sealed class WorkerLifecycleSystem : IGameSystem, IDisposable
{
    private readonly EventBus _eventBus;
    private readonly EntityManager _entityManager;
    private readonly Random _random;
    private readonly Difficulty _difficulty;

    // Pool of ability names to award (deterministic by seed)
    private static readonly string[] AbilityPool =
    {
        "extra_attack", "critical_strike", "iron_will", "quick_reflexes",
        "power_surge", "defensive_stance", "mana_shield", "holy_light",
        "precise_aim", "berserker_rage", "arcane_mastery", "shield_bash",
        "double_strike", "healing_aura", "evasion", "fortify"
    };

    public WorkerLifecycleSystem(EventBus eventBus, EntityManager entityManager, Difficulty difficulty, int? seed = null)
    {
        _eventBus = eventBus;
        _entityManager = entityManager;
        _difficulty = difficulty;
        _random = seed.HasValue ? new Random(seed.Value) : new Random();

        _eventBus.Subscribe<SimulationLateTickEvent>(OnLateTick);
    }

    public void Dispose()
    {
        _eventBus.Unsubscribe<SimulationLateTickEvent>(OnLateTick);
    }

    private void OnLateTick(SimulationLateTickEvent e) => Tick(e.DeltaTime, _entityManager.Heroes);

    /// <summary>
    /// Ticks wear-out conditions for all workers currently assigned to buildings.
    /// Called each fixed tick from SimulationTicker.
    /// </summary>
    public void Tick(float deltaTime, IReadOnlyList<HeroEntity> heroes)
    {
        for (int i = 0; i < heroes.Count; i++)
        {
            var worker = heroes[i];
            if (worker.State != HeroState.OnPath && worker.State != HeroState.EquippingGear)
                continue;
            if (worker.Profession == WorkerProfession.None)
                continue;
            if (!worker.AssignedBuildingId.HasValue)
                continue;

            // Apply wear per tick
            ApplyWear(worker, deltaTime);

            // Check wear-out
            var reason = WearOutConditions.Check(worker);
            if (reason != WearOutReason.None)
            {
                worker.LastWearOutReason = reason;
                worker.State = HeroState.WornOut;
                _eventBus.Publish(new WorkerWornOutEvent(
                    worker.Id, reason, worker.Profession, worker.Position));
            }
        }
    }

    private static void ApplyWear(HeroEntity worker, float dt)
    {
        float toolWear = WearOutConditions.GetToolWearRate(worker.Profession);
        if (toolWear > 0f)
            worker.ToolDurability = Math.Max(0f, worker.ToolDurability - toolWear * dt);

        float staminaDrain = WearOutConditions.GetStaminaDrainRate(worker.Profession);
        if (staminaDrain > 0f)
            worker.Stamina = Math.Max(0f, worker.Stamina - staminaDrain * dt);

        float carryGain = WearOutConditions.GetCarryGainRate(worker.Profession);
        if (carryGain > 0f)
            worker.CarryLoad = Math.Min(worker.MaxCarryCapacity, worker.CarryLoad + carryGain * dt);
    }

    /// <summary>
    /// Resolves the outcome after a worker finishes a job (returns from work).
    /// Handles difficulty-based level downgrade and ability gain.
    /// Returns true if the worker survived (not killed by Normal difficulty).
    /// </summary>
    public bool ResolveJobCompletion(HeroEntity worker)
    {
        bool survived = true;
        int oldLevel = worker.Level;

        switch (_difficulty)
        {
            case Difficulty.Casual:
                // Never downgrade, always gain ability
                TryGainAbility(worker);
                break;

            case Difficulty.Easy:
                // May downgrade 1 level (30% chance), good ability chance (70%)
                if (_random.NextDouble() < 0.3 && worker.Level > 1)
                {
                    worker.Level--;
                    _eventBus.Publish(new WorkerLevelDowngradedEvent(worker.Id, oldLevel, worker.Level));
                }
                if (_random.NextDouble() < 0.7)
                {
                    TryGainAbility(worker);
                }
                break;

            case Difficulty.Normal:
                // Always downgrade or die. Ability only if no downgrade (rare).
                double roll = _random.NextDouble();
                if (roll < 0.15)
                {
                    // Death — worker is lost
                    worker.State = HeroState.Ghost;
                    survived = false;
                    _eventBus.Publish(new HeroDiedEvent(worker.Id, "job_completion", worker.Position));
                }
                else
                {
                    // Downgrade 1 level
                    if (worker.Level > 1)
                    {
                        worker.Level--;
                        _eventBus.Publish(new WorkerLevelDowngradedEvent(worker.Id, oldLevel, worker.Level));
                    }
                    else
                    {
                        // Level 1 — can't downgrade further, gain ability instead
                        TryGainAbility(worker);
                    }
                }
                break;
        }

        // On Normal: if they survived without downgrade (level 1 workers), gain ability
        // Already handled above

        return survived;
    }

    /// <summary>
    /// Called after successful dungeon completion. Only combat jobs can level through dungeons.
    /// Ability gain chance is difficulty-dependent.
    /// </summary>
    public void ResolveDungeonAbilityGain(HeroEntity worker)
    {
        if (!worker.IsCombatProfession) return;

        switch (_difficulty)
        {
            case Difficulty.Casual:
                TryGainAbility(worker);
                break;
            case Difficulty.Easy:
                if (_random.NextDouble() < 0.6)
                    TryGainAbility(worker);
                break;
            case Difficulty.Normal:
                // Only gain if no downgrade happened (handled by DungeonResolver)
                if (_random.NextDouble() < 0.25)
                    TryGainAbility(worker);
                break;
        }
    }

    private void TryGainAbility(HeroEntity worker)
    {
        string abilityId = AbilityPool[_random.Next(AbilityPool.Length)];

        // Don't duplicate abilities
        for (int i = 0; i < worker.Abilities.Count; i++)
        {
            if (worker.Abilities[i].Id == abilityId) return;
        }

        var ability = new WorkerAbility
        {
            Id = abilityId,
            DisplayName = abilityId.Replace('_', ' '),
            GainedAsProfession = worker.Profession,
            BonusValue = 0.1f + (float)_random.NextDouble() * 0.2f
        };

        worker.GainAbility(ability);
        _eventBus.Publish(new AbilityGainedEvent(worker.Id, ability.Id, ability.DisplayName));
    }

    }
