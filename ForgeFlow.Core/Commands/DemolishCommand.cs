namespace ForgeFlow.Core.Commands;

using ForgeFlow.Core.Entities;

/// <summary>
/// Command to demolish any entity at a given grid position.
/// Cross-cutting: resolves entity type (PathGate → RoutingNode → Structure → PathSegment)
/// and delegates to the appropriate manager. Handled by <see cref="Systems.StructureManager"/>.
/// </summary>
public readonly struct DemolishCommand : IGameCommand
{
    public GridPosRPG Position { get; }

    public DemolishCommand(GridPosRPG position)
    {
        Position = position;
    }

    public override string ToString() => $"Demolish at {Position}";
}
