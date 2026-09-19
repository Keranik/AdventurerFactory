using ForgeFlow.Core.Entities;

namespace ForgeFlow.Core.Commands;

/// <summary>
/// Command to purchase and place a PathGate at a grid position.
/// Handled by <see cref="Systems.PathGateManager"/>: validates gold,
/// deducts cost, places the gate, publishes events, and advances tutorials.
/// </summary>
public readonly struct PlacePathGateCommand : IGameCommand
{
    /// <summary>Grid position to place the PathGate at.</summary>
    public GridPosRPG Position { get; }

    /// <summary>Direction the PathGate faces (determines entrance vs exit).</summary>
    public Direction Facing { get; }

    public PlacePathGateCommand(GridPosRPG position, Direction facing)
    {
        Position = position;
        Facing = facing;
    }

    public override string ToString() => $"PlacePathGate at {Position} facing {Facing}";
}
