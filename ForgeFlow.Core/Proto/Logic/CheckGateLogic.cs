using ForgeFlow.Core.Entities;
using ForgeFlow.Core.Proto.Prototypes;

namespace ForgeFlow.Core.Proto.Logic;

/// <summary>
/// Check gate — a single-condition routing structure.
/// Workers matching the rule go to the configured output direction;
/// workers that don't match continue straight through (default direction).
/// Like a simple Factorio decider combinator for people routing.
/// </summary>
public sealed class CheckGateLogic : RoutingNodeBase
{
    public GateRule Rule { get; set; } = new();
    public Direction DefaultDirection { get; set; } = Direction.East;

    public CheckGateLogic(EntityId id) : base(id)
    {
        ProcessingDuration = 0.1f;
    }

    public override void Tick(float deltaTime)
    {
        if (!IsActive) return;
    }

    /// <summary>
    /// Evaluates a hero worker and returns the direction they should be routed.
    /// </summary>
    public Direction EvaluateRoute(HeroEntity worker)
    {
        return Rule.Matches(worker) ? Rule.OutputDirection : DefaultDirection;
    }

    /// <summary>
    /// Evaluates a villager and returns the direction they should be routed.
    /// Uses the shared RuleMatchesVillager helper from FilterSplitterLogic.
    /// </summary>
    public Direction EvaluateVillagerRoute(VillagerLogic villager)
    {
        return FilterSplitterLogic.RuleMatchesVillager(Rule, villager)
            ? Rule.OutputDirection
            : DefaultDirection;
    }
}
