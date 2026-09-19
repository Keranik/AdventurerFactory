using ForgeFlow.Core.Proto;

namespace ForgeFlow.Core.Entities;

/// <summary>
/// Represents a single ability gained through dungeon combat.
/// Abilities are job-specific and lost on job swap.
/// </summary>
public sealed class WorkerAbility
{
    public string Id { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public WorkerProfession GainedAsProfession { get; set; }
    public float BonusValue { get; set; }
}

public sealed class HeroEntity : PlayerEntity, IResettable
{
    public HeroEntity(EntityId id) : base(id) { }

    public ulong Seed { get; set; }
    public string ClassId { get; set; } = string.Empty;
    public HeroState State { get; set; } = HeroState.OnPath;
    public List<EquippedItem> Equipment { get; } = new();
    public AppearanceTemplate Appearance { get; set; } = new();
    public AppearanceTemplate CurrentTemplate { get; set; } = new();
    public float Morale { get; set; } = 1.0f;
    public int DungeonSuccessStreak { get; set; }
    public int DungeonFailStreak { get; set; }
    public string? AssignedDungeonId { get; set; }

    // --- Phase 10: Worker lifecycle ---
    public WorkerProfession Profession { get; set; } = WorkerProfession.None;
    public List<WorkerAbility> Abilities { get; } = new();
    public float ToolDurability { get; set; } = 100f;
    public float MaxToolDurability { get; set; } = 100f;
    public float CarryLoad { get; set; }
    public float MaxCarryCapacity { get; set; } = 50f;
    public WearOutReason LastWearOutReason { get; set; } = WearOutReason.None;
    public ulong? AssignedBuildingId { get; set; }
    public ulong? MaintenanceTargetId { get; set; }
    public int GoldEarned { get; set; }
    public string? GuildId { get; set; }

    public float AverageGearTier
    {
        get
        {
            if (Equipment.Count == 0) return 0f;
            float sum = 0f;
            for (int i = 0; i < Equipment.Count; i++)
            {
                sum += Equipment[i].Tier;
            }
            return sum / Equipment.Count;
        }
    }

    public EquippedItem? GetEquippedInSlot(EquipSlot slot)
    {
        for (int i = 0; i < Equipment.Count; i++)
        {
            if (Equipment[i].Slot == slot)
            {
                return Equipment[i];
            }
        }
        return null;
    }

    public bool HasSlotFilled(EquipSlot slot)
    {
        for (int i = 0; i < Equipment.Count; i++)
        {
            if (Equipment[i].Slot == slot)
            {
                return true;
            }
        }
        return false;
    }

    public void Equip(EquippedItem item)
    {
        var existing = GetEquippedInSlot(item.Slot);
        if (existing != null)
        {
            Equipment.Remove(existing);
        }
        Equipment.Add(item);
    }

    public List<EquippedItem> UnequipAll()
    {
        var items = new List<EquippedItem>(Equipment);
        Equipment.Clear();
        return items;
    }

    public void UpgradeAllGear()
    {
        foreach (var item in Equipment)
        {
            item.Tier++;
            item.Damage *= 1.15f;
            item.Defense *= 1.15f;
            item.Speed *= 1.05f;
            item.CritChance = Math.Min(item.CritChance * 1.05f, 0.5f);
        }
    }

    /// <summary>Checks if this worker is a combat profession eligible for dungeon leveling.</summary>
    public bool IsCombatProfession => Profession is WorkerProfession.Warrior or WorkerProfession.Cleric
        or WorkerProfession.Ranger or WorkerProfession.Mage or WorkerProfession.Guard;

    /// <summary>Adds an ability gained through dungeon combat. Keyed to current profession.</summary>
    public void GainAbility(WorkerAbility ability)
    {
        ability.GainedAsProfession = Profession;
        Abilities.Add(ability);
    }

    /// <summary>Removes all abilities gained in a specific profession (called on job swap).</summary>
    public int StripAbilitiesForProfession(WorkerProfession profession)
    {
        int removed = Abilities.RemoveAll(a => a.GainedAsProfession == profession);
        return removed;
    }

    /// <summary>Swaps to a new profession, losing abilities from the old one.</summary>
    public int SwapProfession(WorkerProfession newProfession)
    {
        var oldProfession = Profession;
        int lost = 0;
        if (oldProfession != WorkerProfession.None && oldProfession != newProfession)
        {
            lost = StripAbilitiesForProfession(oldProfession);
        }
        Profession = newProfession;
        return lost;
    }

    /// <summary>Resets wear-out state after maintenance.</summary>
    public void RefreshAfterMaintenance()
    {
        ToolDurability = MaxToolDurability;
        Stamina = MaxStamina;
        CarryLoad = 0f;
        LastWearOutReason = WearOutReason.None;
        if (State == HeroState.WornOut || State == HeroState.ReturningToMaintenance)
            State = HeroState.OnPath;
    }

    /// <summary>Heroes are ticked by WorkerLifecycleSystem and PathTrafficSystem, not individually.</summary>
    public override void Tick(float deltaTime) { }

    /// <summary>
    /// Resets all mutable runtime state for object pooling / re-use.
    /// Preserves Id and ProtoId (identity). Zero-alloc: clears lists in-place.
    /// </summary>
    public void Reset()
    {
        Seed = 0;
        Level = 1;
        ClassId = string.Empty;
        State = HeroState.OnPath;
        Position = default;
        PathProgress = 0f;
        Equipment.Clear();
        Appearance = new AppearanceTemplate();
        CurrentTemplate = new AppearanceTemplate();
        Morale = 1.0f;
        DungeonSuccessStreak = 0;
        DungeonFailStreak = 0;
        Traits.Clear();
        CurrentPathSegmentId = null;
        AssignedDungeonId = null;
        Profession = WorkerProfession.None;
        Abilities.Clear();
        ToolDurability = 100f;
        MaxToolDurability = 100f;
        Stamina = 100f;
        MaxStamina = 100f;
        CarryLoad = 0f;
        MaxCarryCapacity = 50f;
        LastWearOutReason = WearOutReason.None;
        AssignedBuildingId = null;
        MaintenanceTargetId = null;
        GoldEarned = 0;
        GuildId = null;
        IsActive = true;
    }
}
