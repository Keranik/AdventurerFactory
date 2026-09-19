using ForgeFlow.Core.Events;

namespace ForgeFlow.Core.Systems;

/// <summary>
/// Defines what a single hotbar slot does when activated.
/// </summary>
public enum HotbarActionType
{
    None,
    PlaceStructure,
    DrawPath,
    Demolish
}

/// <summary>
/// A single hotbar slot assignment. Serializable for save/load.
/// </summary>
public sealed class HotbarSlot
{
    public HotbarActionType ActionType { get; set; } = HotbarActionType.None;
    public string StructureCategory { get; set; } = string.Empty;

    public string DisplayLabel => ActionType switch
    {
        HotbarActionType.PlaceStructure => StructureCategory switch
        {
            "Spawner" => "⚔ Spawner",
            "Forge" => "⚒ Forge",
            "DungeonPortal" => "🚪 Portal",
            "FusionAltar" => "✦ Altar",
            "AppearanceWorkshop" => "🎨 Workshop",
            "Smelter" => "🔥 Smelter",
            "Forestry" => "🌲 Forestry",
            "MiningNode" => "⛏ Mine",
            "ManaExtractor" => "💎 Mana",
            "TrainingBuilding" => "📖 Training",
            "ToolStation" => "🔧 Tools",
            "Armory" => "🛡 Armory",
            "JobChanger" => "🔄 Jobs",
            "Academy" => "🎓 Academy",
            "VillageSpawner" => "🏠 Village",
            "Inn" => "🏨 Inn",
            "CraftStation" => "🔨 Craft",
            "Stockpile" => "📦 Stockpile",
            "PathGate" => "🚩 Gate",
            "FilterSplitter" => "⚙ Filter",
            _ => StructureCategory
        },
        HotbarActionType.DrawPath => "⇨ Path",
        HotbarActionType.Demolish => "🗑 Demolish",
        _ => ""
    };

    public int GoldCost => ActionType switch
    {
        HotbarActionType.PlaceStructure => EconomyConfig.GetStructureCost(StructureCategory),
        HotbarActionType.DrawPath => EconomyConfig.PathSegmentCost,
        HotbarActionType.Demolish => 0,
        _ => 0
    };

    public HotbarSlot Clone() => new()
    {
        ActionType = ActionType,
        StructureCategory = StructureCategory
    };
}

/// <summary>
/// All possible actions the player can assign to a hotbar slot.
/// Used to populate the slot picker popup.
/// </summary>
public static class HotbarCatalog
{
    public static readonly HotbarSlot DrawPath = new()
    {
        ActionType = HotbarActionType.DrawPath
    };

    public static readonly HotbarSlot Demolish = new()
    {
        ActionType = HotbarActionType.Demolish
    };

    public static HotbarSlot PlaceStructure(string category) => new()
    {
        ActionType = HotbarActionType.PlaceStructure,
        StructureCategory = category
    };

    /// <summary>
    /// Returns every action the player can currently assign.
    /// Filtered by research tier to avoid showing locked structures.
    /// </summary>
    public static List<HotbarSlot> GetAvailableActions(int researchTier)
    {
        var list = new List<HotbarSlot>
        {
            PlaceStructure("Spawner"),
            PlaceStructure("PathGate"),
            DrawPath,
            PlaceStructure("Forestry"),
            PlaceStructure("MiningNode"),
            PlaceStructure("Inn"),
            PlaceStructure("Stockpile"),
            PlaceStructure("CraftStation"),
            PlaceStructure("TrainingBuilding"),
            PlaceStructure("DungeonPortal"),
            PlaceStructure("FilterSplitter")
        };

        if (researchTier >= 2)
        {
            list.Add(PlaceStructure("Forge"));
            list.Add(PlaceStructure("Smelter"));
        }
        if (researchTier >= 3)
        {
            list.Add(PlaceStructure("FusionAltar"));
            list.Add(PlaceStructure("AppearanceWorkshop"));
        }
        if (researchTier >= 4)
        {
            list.Add(PlaceStructure("ManaExtractor"));
        }

        return list;
    }
}

/// <summary>
/// Manages the player's hotbar state. 10 slots (keys 1-9, 0).
/// Starts with tutorial defaults and allows full customization.
/// </summary>
public sealed class HotbarSystem : IGameSystem
{
    public const int SlotCount = 10;
    private readonly HotbarSlot[] _slots = new HotbarSlot[SlotCount];
    private readonly EventBus _eventBus;

    public HotbarSystem(EventBus eventBus)
    {
        _eventBus = eventBus;
        for (int i = 0; i < SlotCount; i++)
        {
            _slots[i] = new HotbarSlot();
        }
        ApplyTutorialDefaults();
        _eventBus.Subscribe<TutorialStepActivatedEvent>(OnTutorialStepActivated);
    }

    public HotbarSlot GetSlot(int index)
    {
        if (index < 0 || index >= SlotCount) return new HotbarSlot();
        return _slots[index];
    }

    public void SetSlot(int index, HotbarSlot slot)
    {
        if (index < 0 || index >= SlotCount) return;
        _slots[index] = slot;
        _eventBus.Publish(new HotbarChangedEvent(index, slot));
    }

    public void ClearSlot(int index)
    {
        SetSlot(index, new HotbarSlot());
    }

    /// <summary>
    /// Called when a tutorial step unlocks a new action. Adds it to
    /// the first empty slot so the player discovers it naturally.
    /// </summary>
    public void UnlockToFirstEmptySlot(HotbarSlot slot)
    {
        for (int i = 0; i < SlotCount; i++)
        {
            if (_slots[i].ActionType == HotbarActionType.None)
            {
                SetSlot(i, slot);
                return;
            }
        }
    }

    /// <summary>Provides read-only access to all slots for UI rendering.</summary>
    public ReadOnlySpan<HotbarSlot> Slots => _slots;

    /// <summary>Returns the slot array for serialization.</summary>
    public HotbarSlot[] GetSlotsForSave()
    {
        var result = new HotbarSlot[SlotCount];
        for (int i = 0; i < SlotCount; i++)
        {
            result[i] = _slots[i].Clone();
        }
        return result;
    }

    /// <summary>Restores slots from saved data.</summary>
    public void LoadSlots(HotbarSlot[] saved)
    {
        for (int i = 0; i < SlotCount && i < saved.Length; i++)
        {
            _slots[i] = saved[i];
        }
    }

    private void ApplyTutorialDefaults()
    {
        // Progressive unlock: start with only the Spawner.
        // Additional tools are unlocked via OnTutorialStepActivated as the
        // player progresses through the tutorial.
        _slots[0] = HotbarCatalog.PlaceStructure("Spawner");
        _slots[9] = HotbarCatalog.Demolish;
    }

    /// <summary>
    /// Called when a tutorial step is activated. If the step's HighlightTarget
    /// points to a hotbar tool, unlocks it in the first empty slot.
    /// Fully data-driven — no hardcoded mission IDs.
    /// </summary>
    private void OnTutorialStepActivated(TutorialStepActivatedEvent e)
    {
        var slot = ResolveHotbarSlot(e.HighlightTarget);
        if (slot == null) { return; }
        if (IsAlreadyOnHotbar(slot)) { return; }
        UnlockToFirstEmptySlot(slot);
    }

    private static HotbarSlot? ResolveHotbarSlot(string highlightTarget)
    {
        return highlightTarget switch
        {
            "hotbar_spawner" => HotbarCatalog.PlaceStructure("Spawner"),
            "hotbar_pathgate" => HotbarCatalog.PlaceStructure("PathGate"),
            "hotbar_path" => HotbarCatalog.DrawPath,
            "hotbar_forestry" => HotbarCatalog.PlaceStructure("Forestry"),
            "hotbar_miningnode" => HotbarCatalog.PlaceStructure("MiningNode"),
            "hotbar_inn" => HotbarCatalog.PlaceStructure("Inn"),
            "hotbar_stockpile" => HotbarCatalog.PlaceStructure("Stockpile"),
            "hotbar_craftstation" => HotbarCatalog.PlaceStructure("CraftStation"),
            "hotbar_trainingbuilding" => HotbarCatalog.PlaceStructure("TrainingBuilding"),
            "hotbar_dungeonportal" => HotbarCatalog.PlaceStructure("DungeonPortal"),
            "hotbar_filtersplitter" => HotbarCatalog.PlaceStructure("FilterSplitter"),
            _ => null
        };
    }

    private bool IsAlreadyOnHotbar(HotbarSlot slot)
    {
        for (int i = 0; i < SlotCount; i++)
        {
            if (_slots[i].ActionType == slot.ActionType &&
                _slots[i].StructureCategory == slot.StructureCategory)
            {
                return true;
            }
        }
        return false;
    }
}
