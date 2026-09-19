using ForgeFlow.Core.Entities;
using ForgeFlow.Core.Proto.Prototypes;

namespace ForgeFlow.Core.Proto.Logic;

/// <summary>
/// Runtime logic for a resource node placed on the terrain.
/// Tracks remaining yield and regrowth. Headless — no Unity.
/// </summary>
public sealed class ResourceNodeLogic : StructureBase
{
    public ResourceNodeLogic(EntityId id) : base(id) { }

    /// <inheritdoc />
    public override IReadOnlyList<GridPosRPG> GetFootprint() => new[] { Position };

    public string ResourceId { get; set; } = string.Empty;
    public int CurrentYield { get; set; }
    public int MaxYield { get; set; } = 100;
    public float HarvestRate { get; set; } = 1.0f;
    public float RegrowthRate { get; set; } = 0.1f;
    public bool IsRenewable { get; set; } = true;
    public bool IsDepleted => CurrentYield <= 0;

    private float _regrowthAccumulator;

    public void InitializeFromProto(ResourceNodeProto proto)
    {
        ProtoId = proto.Id;
        ResourceId = proto.ResourceId;
        MaxYield = proto.MaxYield;
        CurrentYield = proto.MaxYield;
        HarvestRate = proto.HarvestRate;
        RegrowthRate = proto.RegrowthRate;
        IsRenewable = proto.IsRenewable;
    }

    /// <summary>
    /// Attempts to harvest resources from this node.
    /// Returns the amount actually harvested (may be less than requested).
    /// </summary>
    public int Harvest(int requested)
    {
        if (IsDepleted) return 0;

        int amount = Math.Min(requested, CurrentYield);
        CurrentYield -= amount;
        return amount;
    }

    public override void Tick(float deltaTime)
    {
        if (!IsActive || !IsRenewable || CurrentYield >= MaxYield) return;

        _regrowthAccumulator += RegrowthRate * deltaTime;
        if (_regrowthAccumulator >= 1.0f)
        {
            int regrown = (int)_regrowthAccumulator;
            CurrentYield = Math.Min(CurrentYield + regrown, MaxYield);
            _regrowthAccumulator -= regrown;
        }
    }
}
