using ForgeFlow.Core.Entities;
using ForgeFlow.Core.Proto;
using ForgeFlow.Core.Proto.Logic;

namespace ForgeFlow.Tests;

/// <summary>Tests for CheckGateLogic and BalancerLogic — routing and round-robin distribution.</summary>
public class AutomationRoutingTests
{
    // ─── CheckGateLogic ───────────────────────────────────────────────

    [Fact]
    public void CheckGate_RoutesMatchingWorkerToConfiguredDirection()
    {
        var gate = new CheckGateLogic(EntityId.Next());
        gate.Rule = new GateRule
        {
            ConditionType = GateConditionType.MinLevel,
            ConditionValue = "3",
            OutputDirection = Direction.North
        };
        gate.DefaultDirection = Direction.East;

        var highLevel = new HeroEntity(EntityId.Next()) { Level = 5 };
        var lowLevel = new HeroEntity(EntityId.Next()) { Level = 2 };

        Assert.Equal(Direction.North, gate.EvaluateRoute(highLevel));
        Assert.Equal(Direction.East, gate.EvaluateRoute(lowLevel));
    }

    [Fact]
    public void CheckGate_HasTrait_MatchesCorrectly()
    {
        var gate = new CheckGateLogic(EntityId.Next());
        gate.Rule = new GateRule
        {
            ConditionType = GateConditionType.HasTrait,
            ConditionValue = "brave",
            OutputDirection = Direction.South
        };
        gate.DefaultDirection = Direction.East;

        var withTrait = new HeroEntity(EntityId.Next());
        withTrait.Traits.Add("brave");
        var withoutTrait = new HeroEntity(EntityId.Next());

        Assert.Equal(Direction.South, gate.EvaluateRoute(withTrait));
        Assert.Equal(Direction.East, gate.EvaluateRoute(withoutTrait));
    }

    [Fact]
    public void CheckGate_HasAbility_MatchesCorrectly()
    {
        var gate = new CheckGateLogic(EntityId.Next());
        gate.Rule = new GateRule
        {
            ConditionType = GateConditionType.HasAbility,
            ConditionValue = "extra_attack",
            OutputDirection = Direction.West
        };
        gate.DefaultDirection = Direction.East;

        var withAbility = new HeroEntity(EntityId.Next()) { Profession = WorkerProfession.Warrior };
        withAbility.GainAbility(new WorkerAbility { Id = "extra_attack" });
        var without = new HeroEntity(EntityId.Next());

        Assert.Equal(Direction.West, gate.EvaluateRoute(withAbility));
        Assert.Equal(Direction.East, gate.EvaluateRoute(without));
    }

    [Fact]
    public void CheckGate_HasProfession_MatchesCorrectly()
    {
        var gate = new CheckGateLogic(EntityId.Next());
        gate.Rule = new GateRule
        {
            ConditionType = GateConditionType.HasProfession,
            ConditionValue = "Warrior",
            OutputDirection = Direction.North
        };
        gate.DefaultDirection = Direction.East;

        var warrior = new HeroEntity(EntityId.Next()) { Profession = WorkerProfession.Warrior };
        var miner = new HeroEntity(EntityId.Next()) { Profession = WorkerProfession.Miner };

        Assert.Equal(Direction.North, gate.EvaluateRoute(warrior));
        Assert.Equal(Direction.East, gate.EvaluateRoute(miner));
    }

    [Fact]
    public void CheckGate_ConfigurableViaPanel()
    {
        var gate = new CheckGateLogic(EntityId.Next());
        gate.Rule.ConditionType = GateConditionType.MinLevel;
        gate.Rule.ConditionValue = "5";
        gate.Rule.OutputDirection = Direction.South;
        gate.DefaultDirection = Direction.East;

        var worker = new HeroEntity(EntityId.Next()) { Level = 6 };

        Assert.Equal(Direction.South, gate.EvaluateRoute(worker));
    }

    [Fact]
    public void CheckGate_DefaultDirection_WhenRuleDoesNotMatch()
    {
        var gate = new CheckGateLogic(EntityId.Next());
        gate.Rule.ConditionType = GateConditionType.MinLevel;
        gate.Rule.ConditionValue = "10";
        gate.Rule.OutputDirection = Direction.South;
        gate.DefaultDirection = Direction.East;

        var worker = new HeroEntity(EntityId.Next()) { Level = 3 };

        Assert.Equal(Direction.East, gate.EvaluateRoute(worker));
    }

    // ─── BalancerLogic ────────────────────────────────────────────────

    [Fact]
    public void Balancer_DistributesRoundRobin()
    {
        var balancer = new BalancerLogic(EntityId.Next());
        balancer.OutputDirections.Clear();
        balancer.OutputDirections.Add(Direction.East);
        balancer.OutputDirections.Add(Direction.South);
        balancer.OutputDirections.Add(Direction.West);

        Assert.Equal(Direction.East, balancer.GetNextOutput());
        Assert.Equal(Direction.South, balancer.GetNextOutput());
        Assert.Equal(Direction.West, balancer.GetNextOutput());
        Assert.Equal(Direction.East, balancer.GetNextOutput());
    }

    [Fact]
    public void Balancer_ResetCounter_RestartsFromZero()
    {
        var balancer = new BalancerLogic(EntityId.Next());
        balancer.GetNextOutput();
        balancer.GetNextOutput();
        balancer.ResetCounter();

        Assert.Equal(Direction.East, balancer.GetNextOutput());
    }

    [Fact]
    public void Balancer_RoundRobin_EvenDistribution()
    {
        var balancer = new BalancerLogic(EntityId.Next());
        balancer.OutputDirections.Clear();
        balancer.OutputDirections.Add(Direction.North);
        balancer.OutputDirections.Add(Direction.South);
        balancer.OutputDirections.Add(Direction.East);
        balancer.ResetCounter();

        Assert.Equal(Direction.North, balancer.GetNextOutput());
        Assert.Equal(Direction.South, balancer.GetNextOutput());
        Assert.Equal(Direction.East, balancer.GetNextOutput());
        Assert.Equal(Direction.North, balancer.GetNextOutput());
    }

    [Fact]
    public void Balancer_AddRemoveOutputDirections()
    {
        var balancer = new BalancerLogic(EntityId.Next());
        balancer.OutputDirections.Clear();
        Assert.Empty(balancer.OutputDirections);

        balancer.OutputDirections.Add(Direction.West);
        Assert.Single(balancer.OutputDirections);

        balancer.OutputDirections.Remove(Direction.West);
        Assert.Empty(balancer.OutputDirections);

        Assert.Equal(Direction.East, balancer.GetNextOutput()); // fallback
    }
}
