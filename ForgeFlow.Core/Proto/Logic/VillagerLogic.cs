using ForgeFlow.Core.Entities;
using ForgeFlow.Core.Proto.Prototypes;

namespace ForgeFlow.Core.Proto.Logic;

/// <summary>
/// Runtime villager entity logic. Villagers exist in the village loop parallel
/// to heroes. They gather resources, build structures, and can be assigned jobs.
/// Supports path-based movement, class training, and walk-by building interaction.
/// Headless — runs anywhere.
/// </summary>
public sealed class VillagerLogic : NPCEntity, IResettable
{
    public VillagerLogic(EntityId id) : base(id) { }

    public string Name { get; set; } = string.Empty;
    public VillagerJob Profession { get; set; } = VillagerJob.Idle;
    public VillagerState State { get; set; } = VillagerState.Idle;
    public VillagerClass TrainedClass { get; set; } = VillagerClass.Untrained;
    public float WorkRate { get; set; } = 1.0f;
    public float WorkProgress { get; set; }
    public string? AssignedBuildingId { get; set; }
    public ulong? TargetNodeId { get; set; }

    // Equipment — weapon and armor IDs from ItemRegistry
    public string? EquippedWeaponId { get; set; }
    public string? EquippedArmorId { get; set; }

    // Training progress
    public float TrainingProgress { get; set; }
    public float TrainingRequired { get; set; } = 10.0f;
    public string? TrainingBuildingId { get; set; }

    // Tags for filtered splitter routing
    public string? RoutingTag { get; set; }

    /// <summary>Descriptive text for the villager's current activity (set by managers). Null when idle/travelling.</summary>
    public string? CurrentActivity { get; set; }

    /// <summary>Structure ID of the spawner that created this villager.</summary>
    public ulong? OwnerStructureId { get; set; }

    /// <summary>Structure ID of the residence (home) this villager is assigned to.</summary>
    public ulong? HomeId { get; set; }

    // --- Inventory / Carrying System ---
    /// <summary>Items the villager is currently carrying.</summary>
    public List<ItemInstance> Inventory { get; } = new();
    /// <summary>Maximum number of items the villager can carry.</summary>
    public int MaxInventorySlots { get; set; } = 4;
    /// <summary>Tool proto ID currently equipped by this villager (null = bare hands).</summary>
    public string? EquippedToolId { get; set; }
    /// <summary>Remaining durability of the equipped tool (0 = broken).</summary>
    public float EquippedToolDurability { get; set; }
    /// <summary>Max durability of the equipped tool.</summary>
    public float EquippedToolMaxDurability { get; set; } = 100f;
    /// <summary>Stamina cost drained per gathering/work cycle by StructureManager.</summary>
    public float StaminaCostPerCycle { get; set; } = 5f;

    private float _workAccumulator;
    private float _restTimer;

    public void InitializeFromProto(VillagerProto proto)
    {
        ProtoId = proto.Id;
        WorkRate = proto.BaseWorkRate;
        MovementSpeed = proto.BaseMovementSpeed;
        Stamina = proto.BaseStamina;
        MaxStamina = proto.BaseStamina;
    }

    /// <summary>Whether the villager has both a weapon and armor equipped.</summary>
    public bool HasWeaponAndArmor => EquippedWeaponId != null && EquippedArmorId != null;

    /// <summary>Whether the villager has a weapon equipped.</summary>
    public bool HasWeapon => EquippedWeaponId != null;

    /// <summary>Whether the villager has armor equipped.</summary>
    public bool HasArmor => EquippedArmorId != null;

    /// <summary>
    /// Whether the villager's class is a combat-capable class that can enter dungeons.
    /// Artisan is excluded — they are crafters, not fighters.
    /// </summary>
    public bool IsFightingClass => TrainedClass != VillagerClass.Untrained && TrainedClass != VillagerClass.Artisan;

    /// <summary>
    /// Whether the villager can enter a dungeon. Only requires a fighting class.
    /// Survival chance is much lower without weapon/armor — see DungeonPortalLogic.
    /// </summary>
    public bool CanEnterDungeon => IsFightingClass;

    /// <summary>Equips a weapon by item definition ID.</summary>
    public void EquipWeapon(string itemId) => EquippedWeaponId = itemId;

    /// <summary>Equips armor by item definition ID.</summary>
    public void EquipArmor(string itemId) => EquippedArmorId = itemId;

    /// <summary>Effective work rate including class bonuses.</summary>
    public float EffectiveWorkRate
    {
        get
        {
            float classBonus = TrainedClass switch
            {
                VillagerClass.Warrior => Profession == VillagerJob.Guard ? 0.5f : 0.1f,
                VillagerClass.Cleric => Profession == VillagerJob.Scholar ? 0.5f : 0.15f,
                VillagerClass.Mage => Profession == VillagerJob.Scholar ? 0.4f : 0.2f,
                VillagerClass.Ranger => Profession == VillagerJob.Lumberjack || Profession == VillagerJob.Farmer ? 0.4f : 0.1f,
                VillagerClass.Paladin => 0.25f,
                VillagerClass.Artisan => Profession == VillagerJob.Builder ? 0.6f : 0.15f,
                _ => 0f
            };
            return WorkRate + classBonus;
        }
    }

    public override void Tick(float deltaTime)
    {
        if (!IsActive) return;

        switch (State)
        {
            case VillagerState.Working:
                TickWorking(deltaTime);
                break;
            case VillagerState.Resting:
                TickResting(deltaTime);
                break;
            case VillagerState.Training:
                TickTraining(deltaTime);
                break;
            case VillagerState.Travelling:
                // Movement handled by PathTrafficSystem.MoveVillagersAlongPaths
                break;
            case VillagerState.Idle:
            case VillagerState.Injured:
            default:
                break;
        }
    }

    private void TickWorking(float deltaTime)
    {
        _workAccumulator += EffectiveWorkRate * deltaTime;

        if (Stamina <= 0f)
        {
            State = VillagerState.Resting;
            _restTimer = 0f;
        }
    }

    private void TickResting(float deltaTime)
    {
        _restTimer += deltaTime;
        if (_restTimer >= 2.0f)
        {
            _restTimer -= 2.0f;
            Stamina = Math.Min(Stamina + 10f, MaxStamina);
            if (Stamina >= MaxStamina / 2f)
            {
                State = Profession != VillagerJob.Idle ? VillagerState.Working : VillagerState.Idle;
            }
        }
    }

    private void TickTraining(float deltaTime)
    {
        TrainingProgress += deltaTime;
        if (TrainingProgress >= TrainingRequired)
        {
            // Training complete — class is assigned by TrainingBuildingLogic
            State = VillagerState.Idle;
            TrainingProgress = 0f;
            TrainingBuildingId = null;
        }
    }

    /// <summary>Assigns a profession and transitions the villager to Working state.</summary>
    public void AssignJob(VillagerJob job)
    {
        Profession = job;
        State = job != VillagerJob.Idle ? VillagerState.Working : VillagerState.Idle;
        _workAccumulator = 0f;
    }

    /// <summary>Begins training at a school building.</summary>
    public void BeginTraining(string buildingId, VillagerClass targetClass, float duration)
    {
        TrainingBuildingId = buildingId;
        TrainedClass = targetClass;
        TrainingRequired = duration;
        TrainingProgress = 0f;
        State = VillagerState.Training;
    }

    /// <summary>Puts the villager on a path segment for travel.</summary>
    public void PlaceOnPath(ulong segmentId)
    {
        CurrentPathSegmentId = segmentId;
        PathProgress = 0f;
        State = VillagerState.Travelling;
    }

    /// <summary>Collects accumulated work output and resets the accumulator.</summary>
    public float CollectWorkOutput()
    {
        float output = _workAccumulator;
        _workAccumulator = 0f;
        return output;
    }

    // --- Inventory helpers ---

    /// <summary>Whether the villager's inventory is full.</summary>
    public bool IsInventoryFull => Inventory.Count >= MaxInventorySlots;

    /// <summary>Whether the villager is carrying any items.</summary>
    public bool IsCarryingItems => Inventory.Count > 0;

    /// <summary>Adds an item to the villager's inventory. Returns false if full.</summary>
    public bool TryPickUpItem(ItemInstance item)
    {
        if (IsInventoryFull) return false;
        Inventory.Add(item);
        return true;
    }

    /// <summary>Removes and returns all items of a given proto ID.</summary>
    public List<ItemInstance> DropItems(string protoId)
    {
        var dropped = new List<ItemInstance>();
        for (int i = Inventory.Count - 1; i >= 0; i--)
        {
            if (Inventory[i].ProtoId == protoId)
            {
                dropped.Add(Inventory[i]);
                Inventory.RemoveAt(i);
            }
        }
        return dropped;
    }

    /// <summary>Removes and returns all carried items.</summary>
    public List<ItemInstance> DropAllItems()
    {
        var dropped = new List<ItemInstance>(Inventory);
        Inventory.Clear();
        return dropped;
    }

    /// <summary>
    /// Removes and returns carried items whose ProtoId is in the given set of recipe input keys.
    /// Non-matching items (e.g. crafted weapons) remain in the villager's inventory.
    /// </summary>
    public List<ItemInstance> DropItemsMatchingKeys(ICollection<string> recipeInputKeys)
    {
        var dropped = new List<ItemInstance>();
        for (int i = Inventory.Count - 1; i >= 0; i--)
        {
            if (recipeInputKeys.Contains(Inventory[i].ProtoId))
            {
                dropped.Add(Inventory[i]);
                Inventory.RemoveAt(i);
            }
        }
        return dropped;
    }

    /// <summary>
    /// Scans the inventory for the best weapon (highest Damage) and best armor
    /// (highest Defense, any armor slot) and equips them into EquippedWeaponId / EquippedArmorId.
    /// Called before dungeon entry so HasWeapon / HasArmor reflect carried gear.
    /// </summary>
    public void AutoEquipFromInventory()
    {
        ItemInstance? bestWeapon = null;
        ItemInstance? bestArmor = null;

        for (int i = 0; i < Inventory.Count; i++)
        {
            var item = Inventory[i];
            if (item.Slot == EquipSlot.Weapon && item.Damage > 0f)
            {
                if (bestWeapon == null || item.Damage > bestWeapon.Damage)
                {
                    bestWeapon = item;
                }
            }
            else if (item.Slot != default && item.Defense > 0f)
            {
                // Any non-weapon slot with defense counts as armor
                if (bestArmor == null || item.Defense > bestArmor.Defense)
                {
                    bestArmor = item;
                }
            }
        }

        if (bestWeapon != null)
        {
            EquippedWeaponId = bestWeapon.ProtoId;
        }

        if (bestArmor != null)
        {
            EquippedArmorId = bestArmor.ProtoId;
        }
    }

    /// <summary>Checks if the villager is carrying a specific item.</summary>
    public bool HasItem(string protoId)
    {
        for (int i = 0; i < Inventory.Count; i++)
        {
            if (Inventory[i].ProtoId == protoId) return true;
        }
        return false;
    }

    /// <summary>Counts how many of a specific item the villager is carrying.</summary>
    public int CountItem(string protoId)
    {
        int count = 0;
        for (int i = 0; i < Inventory.Count; i++)
        {
            if (Inventory[i].ProtoId == protoId) count += Inventory[i].Quantity;
        }
        return count;
    }

    /// <summary>Equips a tool by proto ID, setting durability to max.</summary>
    public void EquipTool(string? toolId, float maxDurability = 100f)
    {
        EquippedToolId = toolId;
        EquippedToolMaxDurability = maxDurability;
        EquippedToolDurability = maxDurability;
    }

    /// <summary>Whether the equipped tool is broken.</summary>
    public bool IsToolBroken => EquippedToolId != null && EquippedToolDurability <= 0f;

    /// <summary>
    /// Resets all mutable runtime state for object pooling / re-use.
    /// Preserves Id and ProtoId (identity). Zero-alloc: clears lists in-place.
    /// </summary>
    public void Reset()
    {
        Name = string.Empty;
        Level = 1;
        Profession = VillagerJob.Idle;
        State = VillagerState.Idle;
        TrainedClass = VillagerClass.Untrained;
        WorkRate = 1.0f;
        MovementSpeed = 2.0f;
        Stamina = 100f;
        MaxStamina = 100f;
        WorkProgress = 0f;
        AssignedBuildingId = null;
        TargetNodeId = null;
        Traits.Clear();
        EquippedWeaponId = null;
        EquippedArmorId = null;
        CurrentPathSegmentId = null;
        PathProgress = 0f;
        TrainingProgress = 0f;
        TrainingRequired = 10.0f;
        TrainingBuildingId = null;
        RoutingTag = null;
        CurrentActivity = null;
        OwnerStructureId = null;
        HomeId = null;
        Inventory.Clear();
        MaxInventorySlots = 4;
        EquippedToolId = null;
        EquippedToolDurability = 0f;
        EquippedToolMaxDurability = 100f;
        StaminaCostPerCycle = 5f;
        Position = default;
        IsActive = true;
    }
}
