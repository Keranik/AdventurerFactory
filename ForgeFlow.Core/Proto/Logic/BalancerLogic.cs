using ForgeFlow.Core.Entities;

namespace ForgeFlow.Core.Proto.Logic;

/// <summary>
/// Balancer — evenly distributes workers across output directions in round-robin fashion.
/// No conditions — purely load-balancing.
/// </summary>
public sealed class BalancerLogic : RoutingNodeBase
{
    public List<Direction> OutputDirections { get; } = new() { Direction.East, Direction.South };
    private int _nextOutputIndex;

    public BalancerLogic(EntityId id) : base(id)
    {
        ProcessingDuration = 0.1f;
    }

    public override void Tick(float deltaTime)
    {
        if (!IsActive) return;
    }

    /// <summary>Returns the next output direction in round-robin order.</summary>
    public Direction GetNextOutput()
    {
        if (OutputDirections.Count == 0) return Direction.East;
        var dir = OutputDirections[_nextOutputIndex % OutputDirections.Count];
        _nextOutputIndex++;
        return dir;
    }

    /// <summary>Resets the round-robin counter.</summary>
    public void ResetCounter() => _nextOutputIndex = 0;
}
