namespace ForgeFlow.Core.Commands;

using ForgeFlow.Core.Entities;

/// <summary>
/// Command to rotate an entity at a given grid position 90° clockwise.
/// Cross-cutting: resolves entity type (PathSegment, PathGate, RoutingNode)
/// and delegates to the appropriate manager. Handled by <see cref="Systems.StructureManager"/>.
/// </summary>
public readonly struct RotateEntityCommand : IGameCommand
{
    public GridPosRPG Position { get; }

    public RotateEntityCommand(GridPosRPG position)
    {
        Position = position;
    }

    public override string ToString() => $"Rotate at {Position}";
}
