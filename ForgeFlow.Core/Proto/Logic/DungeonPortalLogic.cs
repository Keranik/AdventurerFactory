using ForgeFlow.Core.Data;
using ForgeFlow.Core.Entities;
using ForgeFlow.Core.Proto.Prototypes;

namespace ForgeFlow.Core.Proto.Logic;

/// <summary>
/// Dungeon portal logic. Accepts villagers who have a fighting class.
/// Weapon and armor are not required to enter but drastically improve survival.
/// Runs a timed dungeon crawl, and resolves survival/death/loot.
/// Follows the Central Manager + Events pattern:
/// Logic stores pending results ? DungeonManager reads and publishes events.
/// </summary>
public sealed class DungeonPortalLogic : ActivityEntity, IEntryGated, IPreEntryAction
{
    public DungeonPortalLogic(EntityId id) : base(id)
    {
        ProcessingDuration = 15.0f;
    }

    public string DungeonId { get; set; } = string.Empty;
    public float BaseSurvivalChance { get; set; } = 0.1f;
    public int BaseGoldReward { get; set; } = 50;
    public float RunDuration { get; set; } = 15.0f;

    private DungeonRegistry? _dungeonRegistry;
    private Random _rng = new();

    // ?? Occupant Tracking ?????????????????????????????????????????
    public new int MaxOccupants { get; set; } = 4;
    public List<ulong> CurrentOccupants { get; } = new();
    public Dictionary<ulong, float> OccupantTimers { get; } = new();

    /// <summary>Tracks whether each occupant had a weapon when they entered.</summary>
    public Dictionary<ulong, bool> OccupantHasWeapon { get; } = new();
    /// <summary>Tracks whether each occupant had armor when they entered.</summary>
    public Dictionary<ulong, bool> OccupantHasArmor { get; } = new();

    /// <summary>
    /// Survival chance multiplier applied when a villager enters without a weapon.
    /// Base × this value. Default: 0.25 (quarter the base chance).
    /// </summary>
    public float NoWeaponSurvivalMultiplier { get; set; } = 0.25f;

    /// <summary>
    /// Survival chance multiplier applied when a villager enters without armor.
    /// Stacks multiplicatively with NoWeaponSurvivalMultiplier. Default: 0.5.
    /// </summary>
    public float NoArmorSurvivalMultiplier { get; set; } = 0.5f;

    /// <summary>Pending results for the manager to read and clear after publishing events.</summary>
    public List<DungeonRunResult> PendingResults { get; } = new();

    public bool CanAcceptVillager => CurrentOccupants.Count < MaxOccupants;

    public void Initialize(DungeonRegistry dungeonRegistry)
    {
        _dungeonRegistry = dungeonRegistry;
    }

    /// <summary>Seeds the internal RNG for deterministic testing.</summary>
    public void SetSeed(int seed)
    {
        _rng = new Random(seed);
    }

    public override void InitializeFromProto(StructureProtoBase proto)
    {
        base.InitializeFromProto(proto);
        if (proto is DungeonPortalProto dp)
        {
            DungeonId = dp.DefaultDungeonId;
            BaseSurvivalChance = dp.BaseSurvivalChance;
            BaseGoldReward = dp.BaseGoldReward;
            RunDuration = dp.RunDuration;
            MaxOccupants = dp.MaxOccupants;
        }
    }

    public Data.Definitions.DungeonDefinition? GetDungeonDefinition()
    {
        return _dungeonRegistry?.Get(DungeonId);
    }

    /// <summary>
    /// Checks whether a villager is eligible to enter this dungeon portal.
    /// Rejects when full or when the villager is not a fighting class.
    /// </summary>
    public EntryCheckResult CheckEntry(VillagerLogic villager)
    {
        if (!CanAcceptVillager)
        {
            return EntryCheckResult.Rejected(EntryRejectionReason.StructureFull);
        }
        if (!villager.CanEnterDungeon)
        {
            return EntryCheckResult.Rejected(EntryRejectionReason.WrongClass);
        }
        return EntryCheckResult.Accepted;
    }

    /// <summary>
    /// Pre-entry action: auto-equips the best weapon and armor from the villager's inventory.
    /// Called by PathGateManager after CheckEntry succeeds, before the accept step.
    /// </summary>
    public void PrepareVillagerForEntry(VillagerLogic villager)
    {
        villager.AutoEquipFromInventory();
    }

    /// <summary>
    /// Attempts to accept a villager into the dungeon portal.
    /// Returns true if the villager was accepted.
    /// The caller (manager) is responsible for setting villager state.
    /// </summary>
    public bool AcceptVillager(ulong villagerId, bool hasWeapon = false, bool hasArmor = false)
    {
        if (CurrentOccupants.Count >= MaxOccupants) { return false; }
        CurrentOccupants.Add(villagerId);
        OccupantTimers[villagerId] = 0f;
        OccupantHasWeapon[villagerId] = hasWeapon;
        OccupantHasArmor[villagerId] = hasArmor;
        return true;
    }

    /// <summary>
    /// Ticks dungeon run timers. When a run completes, resolves survival
    /// and stores the result in PendingResults for the manager to read.
    /// </summary>
    public void TickOccupants(float deltaTime)
    {
        for (int i = CurrentOccupants.Count - 1; i >= 0; i--)
        {
            ulong villagerId = CurrentOccupants[i];
            if (!OccupantTimers.TryGetValue(villagerId, out float timer))
            {
                CurrentOccupants.RemoveAt(i);
                continue;
            }

            timer += deltaTime;
            OccupantTimers[villagerId] = timer;

            if (timer >= RunDuration)
            {
                bool hasWeapon = OccupantHasWeapon.TryGetValue(villagerId, out bool w) && w;
                bool hasArmor = OccupantHasArmor.TryGetValue(villagerId, out bool a) && a;
                float survivalChance = ComputeSurvivalChance(hasWeapon, hasArmor);
                bool survived = _rng.NextDouble() < survivalChance;
                int goldReward = survived ? ComputeGoldReward() : 0;
                string? lootItemId = survived ? RollLoot() : null;

                PendingResults.Add(new DungeonRunResult(
                    villagerId, survived, goldReward, lootItemId, DungeonId));

                CurrentOccupants.RemoveAt(i);
                OccupantTimers.Remove(villagerId);
                OccupantHasWeapon.Remove(villagerId);
                OccupantHasArmor.Remove(villagerId);
            }
        }
    }

    /// <summary>Returns the run progress (0.0–1.0) for a specific occupant.</summary>
    public float GetRunProgress(ulong villagerId)
    {
        if (OccupantTimers.TryGetValue(villagerId, out float timer))
        {
            return RunDuration > 0f ? Math.Min(timer / RunDuration, 1f) : 1f;
        }
        return 0f;
    }

    private float ComputeSurvivalChance(bool hasWeapon, bool hasArmor)
    {
        var def = GetDungeonDefinition();
        float baseChance = def != null ? def.BaseSurvivalChance : BaseSurvivalChance;

        float multiplier = 1f;
        if (!hasWeapon) { multiplier *= NoWeaponSurvivalMultiplier; }
        if (!hasArmor) { multiplier *= NoArmorSurvivalMultiplier; }

        return Math.Clamp(baseChance * multiplier, 0f, 1f);
    }

    private int ComputeGoldReward()
    {
        var def = GetDungeonDefinition();
        if (def != null)
        {
            return def.BaseGoldReward;
        }
        return BaseGoldReward;
    }

    private string? RollLoot()
    {
        var def = GetDungeonDefinition();
        if (def == null || def.PossibleDropItemIds.Count == 0) { return null; }
        int idx = _rng.Next(def.PossibleDropItemIds.Count);
        return def.PossibleDropItemIds[idx];
    }

    public override void Tick(float deltaTime)
    {
        // Occupant ticking handled by DungeonManager (Central Manager + Events pattern).
    }
}

/// <summary>
/// Result of a single villager's dungeon run. Created by DungeonPortalLogic,
/// read and cleared by DungeonManager after publishing the appropriate events.
/// </summary>
public readonly struct DungeonRunResult
{
    public ulong VillagerId { get; }
    public bool Survived { get; }
    public int GoldReward { get; }
    public string? LootItemId { get; }
    public string DungeonId { get; }

    public DungeonRunResult(ulong villagerId, bool survived, int goldReward, string? lootItemId, string dungeonId)
    {
        VillagerId = villagerId;
        Survived = survived;
        GoldReward = goldReward;
        LootItemId = lootItemId;
        DungeonId = dungeonId;
    }
}
