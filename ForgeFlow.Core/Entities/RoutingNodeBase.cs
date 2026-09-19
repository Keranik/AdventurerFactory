using ForgeFlow.Core.Proto;
using ForgeFlow.Core.Proto.Prototypes;

namespace ForgeFlow.Core.Entities;

/// <summary>
/// Abstract base for routing nodes that direct villagers/workers along different paths
/// based on rules or load-balancing. Balancers, filter splitters, and check gates
/// inherit from this. Routing nodes do not process items or hold occupants —
/// they evaluate a routing decision and redirect traffic.
/// </summary>
public abstract class RoutingNodeBase : StructureBase
{
    protected RoutingNodeBase(EntityId id) : base(id) { }

    // ── Cached interface lookups (hot-path optimization) ──
    private IRoutingNodeTickHandler? _cachedRoutingTickHandler;
    /// <summary>Cached cast to IRoutingNodeTickHandler. Single field read after first access for implementing types.</summary>
    public IRoutingNodeTickHandler? AsRoutingTickHandler => _cachedRoutingTickHandler ??= this as IRoutingNodeTickHandler;

    /// <summary>Default output direction for fallback routing.</summary>
    public Direction OutputDirection { get; set; } = Direction.East;

    /// <summary>Duration value from proto data (not used for processing, kept for proto compat).</summary>
    public float ProcessingDuration { get; set; } = 0.1f;

    /// <inheritdoc />
    public override IReadOnlyList<GridPosRPG> GetFootprint() => [Position];

    /// <summary>Initializes routing node fields from a proto definition.</summary>
    public virtual void InitializeFromProto(StructureProtoBase proto)
    {
        ProtoId = proto.Id;
        ProcessingDuration = proto.ProcessingDuration;
    }
}
