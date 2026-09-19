using ForgeFlow.Core.Entities;

namespace ForgeFlow.Core.Proto.Prototypes;

/// <summary>
/// Abstract proto base for routing structures that direct villagers/workers along
/// different paths based on rules or load-balancing.
/// Mirrors <see cref="RoutingNodeBase"/> in the Logic layer.
/// CheckGateProto, FilterSplitterProto, and BalancerProto inherit from this.
/// </summary>
public abstract class RoutingProtoBase : StructureProtoBase { }
