namespace ForgeFlow.Core.Proto.Prototypes;

/// <summary>
/// The mode of a PathGate — determined by its facing direction relative
/// to the linked building. Entrance pulls villagers off the path into
/// the building; Exit releases them back onto the path.
/// </summary>
public enum PathGateMode
{
    /// <summary>Villagers leave the path and enter the linked building.</summary>
    Entrance,
    /// <summary>Villagers exit the linked building and rejoin the path.</summary>
    Exit
}

/// <summary>
/// Proto definition for a PathGate entity. JSON-driven, moddable.
/// A PathGate is a standalone rotatable entity placed adjacent to paths.
/// Facing toward a building = Entrance, facing away = Exit.
/// See Project Bible §6.2.
/// </summary>
public sealed class PathGateProto : ProtoBase
{
    /// <summary>Manhattan distance range within which this gate can link to a building.</summary>
    public int GateRange { get; set; } = 1;
}
