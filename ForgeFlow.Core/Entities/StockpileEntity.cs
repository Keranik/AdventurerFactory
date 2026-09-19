namespace ForgeFlow.Core.Entities;

/// <summary>
/// Abstract base for storage entities that act as resource buffers.
/// Villagers drop off and pick up items here.
/// Stockpile/Warehouse and future storage types inherit from this.
/// </summary>
public abstract class StockpileEntity : Structure
{
    protected StockpileEntity(EntityId id) : base(id) { }

    // ── Stockpile state ──

    /// <summary>Maximum total items this storage can hold.</summary>
    public int MaxCapacity { get; set; } = 100;

    /// <summary>Resources stored, keyed by resource/item ID.</summary>
    public Dictionary<string, int> StoredResources { get; } = new();

    /// <summary>Total items currently stored across all resource types.</summary>
    public int TotalStored
    {
        get
        {
            int total = 0;
            foreach (var kvp in StoredResources)
            {
                total += kvp.Value;
            }
            return total;
        }
    }

    /// <summary>Whether the storage has room for more items.</summary>
    public bool HasRoom => TotalStored < MaxCapacity;
}
