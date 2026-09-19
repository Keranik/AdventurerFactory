using ForgeFlow.Core.Entities;
using ForgeFlow.Core.Proto.Prototypes;

namespace ForgeFlow.Core.Proto.Logic;

/// <summary>
/// Runtime logic for a PathGate entity. PathGates are independent, player-placed
/// entities that sit adjacent to paths and buildings. They direct villagers
/// off the path into a building (Entrance) or back onto the path from a
/// building (Exit).
///
/// The PathGate is completely separate from the path system. It does not
/// modify PathSegmentLogic or add any NodeTypes. PathTrafficSystem detects
/// nearby PathGates via spatial lookup when a villager arrives at a segment.
///
/// See Project Bible §6.2.
/// </summary>
public sealed class PathGateLogic : StructureBase, IRotatable
{
    public PathGateLogic(EntityId id) : base(id) { }

    /// <inheritdoc />
    public override IReadOnlyList<GridPosRPG> GetFootprint() => new[] { Position };

    /// <summary>Direction this gate is facing. Determines Entrance vs Exit relative to the linked building.</summary>
    public Direction Facing { get; set; } = Direction.North;

    /// <summary>The structure/building this gate is linked to.</summary>
    public ulong? LinkedStructureId { get; set; }

    /// <summary>Current mode — Entrance or Exit. Derived from facing vs building position.</summary>
    public PathGateMode Mode { get; set; } = PathGateMode.Entrance;

    /// <summary>Manhattan distance range for linking to a building.</summary>
    public int GateRange { get; set; } = 1;

    /// <summary>Position the gate is facing toward (one tile in Facing direction).</summary>
    public GridPosRPG FacingPosition => Position.Neighbor(Facing);

    /// <summary>Whether this gate is currently linked to a valid building.</summary>
    public bool IsLinked => LinkedStructureId.HasValue;

    /// <summary>Whether this gate acts as an entrance (villagers leave path → enter building).</summary>
    public bool IsEntrance => Mode == PathGateMode.Entrance;

    /// <summary>Whether this gate acts as an exit (villagers leave building → rejoin path).</summary>
    public bool IsExit => Mode == PathGateMode.Exit;

    /// <summary>
    /// Initializes this gate from a proto definition.
    /// </summary>
    public void InitializeFromProto(PathGateProto proto)
    {
        ProtoId = proto.Id;
        GateRange = proto.GateRange;
    }

    /// <summary>
    /// Links this gate to a building and determines mode based on facing direction.
    /// If the gate faces toward the building position, it is an Entrance.
    /// If the gate faces away from the building position, it is an Exit.
    /// </summary>
    public void LinkToStructure(ulong structureId, GridPosRPG structurePosition)
    {
        LinkedStructureId = structureId;

        // If our facing direction points toward the building, we are an Entrance.
        // If it points away, we are an Exit.
        var facingTarget = FacingPosition;
        int distTowardBuilding = facingTarget.ManhattanTo(structurePosition);
        int distFromSelf = Position.ManhattanTo(structurePosition);

        Mode = distTowardBuilding < distFromSelf
            ? PathGateMode.Entrance
            : PathGateMode.Exit;
    }

    /// <summary>
    /// Rotates the gate clockwise and recomputes mode if linked.
    /// Called when the player presses R during placement.
    /// </summary>
    public void Rotate(GridPosRPG? structurePosition = null)
    {
        Facing = Facing.RotateClockwise();

        if (LinkedStructureId.HasValue && structurePosition.HasValue)
        {
            LinkToStructure(LinkedStructureId.Value, structurePosition.Value);
        }
    }

    /// <summary>
    /// PathGate is passive — it does no processing on its own.
    /// Detection happens in PathTrafficSystem via spatial lookup.
    /// </summary>
    public override void Tick(float deltaTime)
    {
        // Intentionally empty. PathGate is a passive spatial marker.
    }
}
