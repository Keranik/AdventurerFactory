using ForgeFlow.Core.Entities;
using ForgeFlow.Core.Events;
using ForgeFlow.Core.Utilities;

namespace ForgeFlow.Core.Systems;

/// <summary>
/// Single source of truth for all item operations: virtual stock tracking
/// (gold, mana, wood, ore, etc.), physical item creation/destruction via
/// ObjectPool, and villager inventory return. Replaces the old ResourceManager
/// and GuildData gold operations with a unified API.
/// </summary>
public sealed class ItemManager : IGameSystem, IDisposable
{
    private readonly EventBus _eventBus;

    /// <summary>Pool for zero-alloc ItemInstance creation in hot paths.</summary>
    private readonly ObjectPool<ItemInstance> _itemPool;

    /// <summary>
    /// Virtual stock dictionary. Tracks all countable resources:
    /// gold, mana, wood, ore, food, etc. Items flagged as IVirtualItem
    /// are never physically instantiated — they live here as counts.
    /// </summary>
    public Dictionary<string, int> VirtualStocks { get; } = new();

    public ItemManager(EventBus eventBus)
    {
        _eventBus = eventBus;
        _itemPool = new ObjectPool<ItemInstance>(
            initialCapacity: 32,
            maxSize: 2048,
            resetAction: item => item.Reset());
    }

    public void Dispose()
    {
        _itemPool.Clear();
    }

    // ── Virtual Stock Operations ─────────────────────────────────────

    /// <summary>Gets the current stock of a virtual resource. Returns 0 if not present.</summary>
    public int GetStock(string resourceId)
    {
        VirtualStocks.TryGetValue(resourceId, out var amount);
        return amount;
    }

    /// <summary>Returns true if the stock of the given resource is at least the specified amount.</summary>
    public bool HasStock(string resourceId, int amount)
    {
        return GetStock(resourceId) >= amount;
    }

    /// <summary>
    /// Adds stock to a virtual resource. Publishes a ResourceProducedEvent.
    /// </summary>
    public void AddStock(string resourceId, int amount, GridPosRPG sourcePosition = default)
    {
        if (string.IsNullOrEmpty(resourceId) || amount <= 0) { return; }

        VirtualStocks.TryGetValue(resourceId, out var current);
        VirtualStocks[resourceId] = current + amount;
        _eventBus.Publish(new ResourceProducedEvent(resourceId, amount, sourcePosition));
    }

    /// <summary>
    /// Removes stock from a virtual resource. Returns false if insufficient stock.
    /// Does not publish events — use TrySpendStock for transactional deductions.
    /// </summary>
    public bool RemoveStock(string resourceId, int amount)
    {
        if (string.IsNullOrEmpty(resourceId) || amount <= 0) { return false; }

        if (!VirtualStocks.TryGetValue(resourceId, out var current) || current < amount)
        {
            return false;
        }

        VirtualStocks[resourceId] = current - amount;
        return true;
    }

    /// <summary>Sets a virtual stock to an exact value. Used for initialization and save loading.</summary>
    public void SetStock(string resourceId, int amount)
    {
        if (string.IsNullOrEmpty(resourceId)) { return; }
        VirtualStocks[resourceId] = amount;
    }

    /// <summary>
    /// Attempts to spend a specific virtual resource. Returns false if insufficient.
    /// Publishes a GoldChangedEvent when the resource is "gold".
    /// </summary>
    public bool TrySpendStock(string resourceId, int amount)
    {
        if (amount < 0) { return false; }
        if (!HasStock(resourceId, amount)) { return false; }

        int oldAmount = GetStock(resourceId);
        VirtualStocks[resourceId] = oldAmount - amount;

        if (resourceId == "gold")
        {
            _eventBus.Publish(new GoldChangedEvent(oldAmount, oldAmount - amount, "spend"));
        }
        return true;
    }

    /// <summary>
    /// Atomically checks and deducts multiple virtual resources.
    /// Returns false if any resource is insufficient — no partial deductions occur.
    /// </summary>
    public bool TrySpendResources(Dictionary<string, int> cost)
    {
        foreach (var kvp in cost)
        {
            if (!VirtualStocks.TryGetValue(kvp.Key, out var stock) || stock < kvp.Value)
            {
                return false;
            }
        }

        foreach (var kvp in cost)
        {
            VirtualStocks[kvp.Key] -= kvp.Value;
        }
        return true;
    }

    /// <summary>Clears all virtual stocks.</summary>
    public void Clear()
    {
        VirtualStocks.Clear();
    }

    /// <summary>
    /// Initializes starting virtual stocks based on difficulty and multiplier.
    /// Called during new game setup.
    /// </summary>
    public void InitializeStartingStocks(Difficulty difficulty, float resourceMultiplier, int startingGold)
    {
        Clear();

        float difficultyMultiplier = difficulty switch
        {
            Difficulty.Casual => 3.0f,
            Difficulty.Easy => 2.0f,
            _ => 1.0f
        };
        float totalMultiplier = resourceMultiplier * difficultyMultiplier;

        VirtualStocks["wood"] = (int)(50 * totalMultiplier);
        VirtualStocks["ore"] = (int)(30 * totalMultiplier);
        VirtualStocks["food"] = (int)(40 * totalMultiplier);
        VirtualStocks["gold"] = startingGold;
    }

    // ── Physical Item Operations ─────────────────────────────────────

    /// <summary>
    /// Creates a new physical ItemInstance from the pool with the given proto ID.
    /// The instance gets a fresh unique ID from EntityIdFactory.
    /// </summary>
    public ItemInstance CreateItem(string protoId, int tier = 1, int quantity = 1)
    {
        var item = _itemPool.Get();
        item.InstanceId = EntityIdFactory.Next();
        item.ProtoId = protoId;
        item.Tier = tier;
        item.Quantity = quantity;
        return item;
    }

    /// <summary>
    /// Creates a physical ItemInstance pre-populated with equipment stats.
    /// </summary>
    public ItemInstance CreateEquipment(string protoId, int tier, EquipSlot slot,
        float damage, float defense, float speed, float critChance,
        string? specialEffectId = null, GridPosRPG position = default)
    {
        var item = CreateItem(protoId, tier);
        item.Slot = slot;
        item.Damage = damage;
        item.Defense = defense;
        item.Speed = speed;
        item.CritChance = critChance;
        item.SpecialEffectId = specialEffectId;
        item.Position = position;
        item.IsOnPath = true;
        return item;
    }

    /// <summary>
    /// Creates a scrap item from dropped gear (half stats).
    /// </summary>
    public ItemInstance CreateScrap(string protoId, int tier, EquipSlot slot,
        float damage, float defense, float speed, float critChance,
        GridPosRPG position)
    {
        var item = CreateEquipment(protoId, tier, slot, damage * 0.5f, defense * 0.5f, speed, critChance, position: position);
        item.IsScrap = true;
        return item;
    }

    /// <summary>
    /// Returns a physical ItemInstance to the pool for reuse.
    /// </summary>
    public void DestroyItem(ItemInstance item)
    {
        _itemPool.Return(item);
    }

    /// <summary>
    /// Returns all items from a villager's inventory back to virtual stocks.
    /// Clears the inventory after processing.
    /// </summary>
    public void ReturnVillagerInventory(List<ItemInstance> inventory)
    {
        for (int i = 0; i < inventory.Count; i++)
        {
            var item = inventory[i];
            if (!string.IsNullOrEmpty(item.ProtoId))
            {
                VirtualStocks.TryGetValue(item.ProtoId, out var current);
                VirtualStocks[item.ProtoId] = current + item.Quantity;
            }
            _itemPool.Return(item);
        }
        inventory.Clear();
    }

    // ── Pool Diagnostics ─────────────────────────────────────────────

    /// <summary>Number of items currently available in the pool.</summary>
    public int PoolAvailable => _itemPool.AvailableCount;

    /// <summary>Total items ever created by the pool.</summary>
    public int PoolTotalCreated => _itemPool.TotalCreated;
}
