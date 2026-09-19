namespace ForgeFlow.Core.Proto.Prototypes;

/// <summary>
/// Abstract proto base for structures that process recipes (input items → output items).
/// Mirrors <see cref="ForgeFlow.Core.Entities.RecipeEntity"/> in the Logic layer.
/// CraftStationProto, ForgeProto, FusionAltarProto, and GatheringProtoBase inherit from this.
/// </summary>
public abstract class RecipeProtoBase : StructureProtoBase { }
