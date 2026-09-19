using ForgeFlow.Core.Proto;
using ForgeFlow.Core.Proto.Prototypes;

namespace ForgeFlow.Core.Entities;

/// <summary>
/// Abstract base for structure entities that occupy tiles on the grid.
/// Villagers enter and exit these via PathGates. Carries the common
/// structure identity (OutputDirection, Tier, ProcessingDuration) and
/// exit handling (<see cref="IStructureWithExits"/>).
/// Domain bases (RecipeEntity, ActivityEntity, StockpileEntity) add
/// their specialized state on top.
/// </summary>
public abstract class Structure : StructureBase, IStructureWithExits
{
    protected Structure(EntityId id) : base(id) { }

    // ── IStructureWithExits ──
    public List<EntityId> WaitingToExitIds { get; } = new();
    public bool HasWaitingExits => WaitingToExitIds.Count > 0;

    // ── Cached interface lookups (hot-path optimization) ──
    private IEntryGated? _cachedEntryGated;
    /// <summary>Cached cast to IEntryGated. Single field read after first access for implementing types.</summary>
    public IEntryGated? AsEntryGated => _cachedEntryGated ??= this as IEntryGated;

    private IPreEntryAction? _cachedPreEntryAction;
    /// <summary>Cached cast to IPreEntryAction. Single field read after first access for implementing types.</summary>
    public IPreEntryAction? AsPreEntryAction => _cachedPreEntryAction ??= this as IPreEntryAction;

    private IStructureTickHandler? _cachedTickHandler;
    /// <summary>Cached cast to IStructureTickHandler. Single field read after first access for implementing types.</summary>
    public IStructureTickHandler? AsTickHandler => _cachedTickHandler ??= this as IStructureTickHandler;

    private IItemOutputStrategy? _cachedItemOutputStrategy;
    private bool _itemOutputStrategyCached;
    /// <summary>Cached cast to IItemOutputStrategy. Single field read after first access for implementing types.</summary>
    public IItemOutputStrategy? AsItemOutputStrategy
    {
        get
        {
            if (!_itemOutputStrategyCached)
            {
                _cachedItemOutputStrategy = this as IItemOutputStrategy;
                _itemOutputStrategyCached = true;
            }
            return _cachedItemOutputStrategy;
        }
    }

    // ── Structure Identity ──
    public Direction OutputDirection { get; set; } = Direction.East;
    public int Tier { get; set; } = 1;
    public float ProcessingDuration { get; set; } = 2.0f;
    public GridPosRPG OutputPosition => Position.Neighbor(OutputDirection);

    /// <inheritdoc />
    public override IReadOnlyList<GridPosRPG> GetFootprint() => [Position];

    /// <summary>Initializes common structure fields from a proto definition.</summary>
    public virtual void InitializeFromProto(StructureProtoBase proto)
    {
        ProtoId = proto.Id;
        ProcessingDuration = proto.ProcessingDuration;
        Tier = proto.Tier;
    }
}
