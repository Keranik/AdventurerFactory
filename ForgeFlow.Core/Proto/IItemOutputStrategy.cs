using ForgeFlow.Core.Entities;

namespace ForgeFlow.Core.Proto;

/// <summary>
/// Result of a single item-output operation performed by a structure.
/// The manager reads this to publish the appropriate event (Central Manager + Events rule).
/// </summary>
public readonly struct ItemOutputResult
{
    /// <summary>The kind of output that was produced — determines which event the manager publishes.</summary>
    public ItemOutputKind Kind { get; }

    /// <summary>The item proto ID that was produced.</summary>
    public string ItemProtoId { get; }

    /// <summary>Quantity produced (for stock tracking).</summary>
    public int Quantity { get; }

    /// <summary>Villager who should receive or who produced the item (may be 0).</summary>
    public ulong VillagerId { get; }

    /// <summary>The item instance created, if any.</summary>
    public ItemInstance? CreatedItem { get; }

    public ItemOutputResult(ItemOutputKind kind, string itemProtoId, int quantity, ulong villagerId = 0, ItemInstance? createdItem = null)
    {
        Kind = kind;
        ItemProtoId = itemProtoId;
        Quantity = quantity;
        VillagerId = villagerId;
        CreatedItem = createdItem;
    }
}

/// <summary>
/// Categorizes the item-output semantic so the manager knows which event to publish.
/// </summary>
public enum ItemOutputKind
{
    /// <summary>Gathering output — item given to villager, stock updated, publish GatheringWorkerOutputEvent.</summary>
    Gathered,

    /// <summary>Stockpile drop-off — stock updated, publish VillagerDroppedOffItemEvent.</summary>
    DroppedOff,

    /// <summary>Crafted item — publish ItemCraftedEvent.</summary>
    Crafted
}

/// <summary>
/// Context passed to <see cref="IItemOutputStrategy.ProcessItem"/> so that
/// Logic classes can create items without holding references to managers
/// (Central Manager + Events rule — Logic never holds EventBus or publishes events).
/// </summary>
public readonly struct ItemOutputContext
{
    public Systems.ItemManager ItemManager { get; }
    public Data.RecipeRegistry RecipeRegistry { get; }
    public Data.ItemRegistry ItemRegistry { get; }
    public IReadOnlyDictionary<ulong, Proto.Logic.VillagerLogic> VillagerIndex { get; }

    public ItemOutputContext(
        Systems.ItemManager itemManager,
        Data.RecipeRegistry recipeRegistry,
        Data.ItemRegistry itemRegistry,
        IReadOnlyDictionary<ulong, Proto.Logic.VillagerLogic> villagerIndex)
    {
        ItemManager = itemManager;
        RecipeRegistry = recipeRegistry;
        ItemRegistry = itemRegistry;
        VillagerIndex = villagerIndex;
    }

    /// <summary>
    /// Creates an item from a proto definition, handling equipment vs. simple items.
    /// Mirrors StructureManager.CreateItemFromProto but accessible to strategies.
    /// </summary>
    public ItemInstance? CreateItemFromProto(string protoId, GridPosRPG outputPosition)
    {
        var proto = ItemRegistry.Get(protoId);
        if (proto != null && proto.Equipment != null)
        {
            return ItemManager.CreateEquipment(
                protoId, proto.Tier, proto.Equipment.Slot,
                proto.Equipment.BaseDamage, proto.Equipment.BaseDefense,
                proto.Equipment.BaseSpeed, proto.Equipment.CritChance,
                proto.Equipment.SpecialEffectId, outputPosition);
        }

        int tier = proto?.Tier ?? 1;
        var item = ItemManager.CreateItem(protoId, tier);
        item.Position = outputPosition;
        item.IsOnPath = true;
        return item;
    }
}

/// <summary>
/// Interface for structures that need custom item-output semantics.
/// Implementations handle item creation and assignment; the manager
/// publishes events based on the returned <see cref="ItemOutputResult"/>.
/// Logic classes implement this but never publish events themselves.
/// </summary>
public interface IItemOutputStrategy
{
    /// <summary>
    /// Process a single pending item output. Create items, assign to villagers,
    /// and return a result describing what happened so the manager can publish events.
    /// </summary>
    ItemOutputResult ProcessItem(
        in PendingItemOutput pending,
        Structure structure,
        ItemOutputContext ctx);
}
