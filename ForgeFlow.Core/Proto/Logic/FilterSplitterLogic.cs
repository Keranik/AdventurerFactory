using ForgeFlow.Core.Entities;
using ForgeFlow.Core.Proto.Prototypes;

namespace ForgeFlow.Core.Proto.Logic;

/// <summary>
/// Filter splitter — routes workers based on multiple rules to different outputs.
/// Supports two routing modes:
///   1. Single-item filter: villagers carrying a specific item go to FilteredOutputDirection.
///   2. Multi-rule: first matching GateRule wins.
/// Heroes are evaluated via GateRule.Matches(HeroEntity).
/// Villagers are evaluated locally (matching carried items, equipped tools, profession, class).
/// Unmatched entities go to DefaultDirection.
/// </summary>
public sealed class FilterSplitterLogic : RoutingNodeBase, IRoutingNodeTickHandler
{
    public List<GateRule> Rules { get; } = new();
    public Direction DefaultDirection { get; set; } = Direction.East;

    // ── Single-Item Filter ──────────────────────────────────────────

    /// <summary>
    /// Optional single-item filter. When set, villagers carrying this item
    /// are routed to FilteredOutputDirection. Null = filter inactive.
    /// </summary>
    public string? FilteredItemId { get; private set; }

    /// <summary>Output direction for villagers matching the single-item filter.</summary>
    public Direction FilteredOutputDirection { get; set; } = Direction.West;

    /// <summary>Set by SetFilteredItem/ClearFilter. Manager reads and clears.</summary>
    public bool PendingFilterChange { get; set; }

    public FilterSplitterLogic(EntityId id) : base(id)
    {
        ProcessingDuration = 0.1f;
        // T-junction defaults: stem faces North (input from South),
        // right arm = East (default), left arm = West (filtered).
        OutputDirection = Direction.North;
        DefaultDirection = Direction.East;
        FilteredOutputDirection = Direction.West;
    }

    public override void InitializeFromProto(StructureProtoBase proto)
    {
        base.InitializeFromProto(proto);
        if (proto is FilterSplitterProto filterProto)
        {
            FilteredItemId = filterProto.DefaultFilteredItemId;
            FilteredOutputDirection = filterProto.DefaultFilteredOutputDirection;
        }
    }

    public override void Tick(float deltaTime)
    {
        if (!IsActive) return;
    }

    // ── Filter Management ───────────────────────────────────────────

    /// <summary>Assigns a single-item filter. Pass null to clear.</summary>
    public void SetFilteredItem(string? itemId)
    {
        if (FilteredItemId == itemId)
        {
            return;
        }
        FilteredItemId = itemId;
        PendingFilterChange = true;
    }

    /// <summary>Clears the single-item filter.</summary>
    public void ClearFilter()
    {
        if (FilteredItemId != null)
        {
            FilteredItemId = null;
            PendingFilterChange = true;
        }
    }

    // ── Hero Evaluation ─────────────────────────────────────────────

    /// <summary>
    /// Evaluates a hero worker against all rules. First match wins.
    /// </summary>
    public Direction EvaluateRoute(HeroEntity worker)
    {
        for (int i = 0; i < Rules.Count; i++)
        {
            if (Rules[i].Matches(worker))
            {
                return Rules[i].OutputDirection;
            }
        }
        return DefaultDirection;
    }

    // ── Villager Evaluation ─────────────────────────────────────────

    /// <summary>
    /// Evaluates a villager against the single-item filter and multi-rule list.
    /// Single-item filter takes priority over rules.
    /// </summary>
    public Direction EvaluateVillagerRoute(VillagerLogic villager)
    {
        // Single-item filter takes priority
        if (!string.IsNullOrEmpty(FilteredItemId))
        {
            if (VillagerCarriesItem(villager, FilteredItemId))
            {
                return FilteredOutputDirection;
            }
            return DefaultDirection;
        }

        // Multi-rule fallback
        for (int i = 0; i < Rules.Count; i++)
        {
            if (RuleMatchesVillager(Rules[i], villager))
            {
                return Rules[i].OutputDirection;
            }
        }
        return DefaultDirection;
    }

    // ── Villager Matching Helpers (internal, shared with CheckGateLogic) ──

    internal static bool VillagerCarriesItem(VillagerLogic villager, string itemId)
    {
        for (int i = 0; i < villager.Inventory.Count; i++)
        {
            if (string.Equals(villager.Inventory[i].ProtoId, itemId, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }

    internal static bool RuleMatchesVillager(GateRule rule, VillagerLogic villager)
    {
        switch (rule.ConditionType)
        {
            case GateConditionType.HasProfession:
                return Enum.TryParse<VillagerJob>(rule.ConditionValue, true, out var job)
                    && villager.Profession == job;

            case GateConditionType.HasJobClass:
                return Enum.TryParse<VillagerClass>(rule.ConditionValue, true, out var cls)
                    && villager.TrainedClass == cls;

            case GateConditionType.CarryingItem:
                return villager.IsCarryingItems
                    && VillagerCarriesItem(villager, rule.ConditionValue);

            case GateConditionType.HasItem:
                return VillagerCarriesItem(villager, rule.ConditionValue);

            case GateConditionType.HasToolType:
                return villager.EquippedToolId != null
                    && string.Equals(villager.EquippedToolId, rule.ConditionValue, StringComparison.OrdinalIgnoreCase);

            default:
                return false;
        }
    }

    // ── IRoutingNodeTickHandler ──────────────────────────────────────

    /// <inheritdoc />
    public void ProcessRoutingTick(ref RoutingTickOutput output)
    {
        if (PendingFilterChange)
        {
            PendingFilterChange = false;
            output.ConfigChanged = true;
        }
    }
}
