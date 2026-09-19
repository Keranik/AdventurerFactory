using ForgeFlow.Core.Entities;
using ForgeFlow.Core.Proto.Prototypes;

namespace ForgeFlow.Core.Commands;

/// <summary>
/// Command to purchase and place a single path segment at a grid position.
/// Handled by <see cref="Systems.PathNodeManager"/>: validates gold,
/// deducts cost, places the segment, publishes events, and advances tutorials.
/// </summary>
public readonly struct DrawPathCommand : IGameCommand
{
    /// <summary>Grid position to place the path segment at.</summary>
    public GridPosRPG Position { get; }

    /// <summary>Direction the path segment faces.</summary>
    public Direction Facing { get; }

    /// <summary>Shape of the path node (Straight, T-junction, etc.).</summary>
    public PathNodeType NodeType { get; }

    public DrawPathCommand(GridPosRPG position, Direction facing, PathNodeType nodeType = PathNodeType.Straight)
    {
        Position = position;
        Facing = facing;
        NodeType = nodeType;
    }

    public override string ToString() => $"DrawPath at {Position} facing {Facing} ({NodeType})";
}
