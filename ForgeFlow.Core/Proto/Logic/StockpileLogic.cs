using ForgeFlow.Core.Entities;
using ForgeFlow.Core.Proto.Prototypes;

namespace ForgeFlow.Core.Proto.Logic;

/// <summary>
/// Stockpile / Warehouse — storage for resources.
/// Villagers drop off items here. Other villagers can pick them up.
/// Acts as a buffer in the production chain.
/// Supports an optional single-product assignment: when AcceptedItemId is set,
/// only that item can be deposited. When null, all items are accepted.
/// </summary>
public sealed class StockpileLogic : StockpileEntity, IEntryGated, IStructureTickHandler, IItemOutputStrategy
{
    /// <summary>Villager IDs waiting to drop off items. StructureManager reads and clears each tick.</summary>
    public List<ulong> PendingVillagerIds { get; } = new();

    /// <summary>
    /// Optional single-product assignment. Null = accept all items (default).
    /// Non-null = only accept items whose ProtoId matches this value.
    /// Configurable at runtime via the Stockpile Inspector / ProductPicker.
    /// </summary>
    public string? AcceptedItemId { get; private set; }

    /// <summary>Set by SetAcceptedItem/ClearAcceptedItem. Manager reads and clears.</summary>
    public bool PendingFilterChange { get; set; }

    public StockpileLogic(EntityId id) : base(id)
    {
        ProcessingDuration = 0f;
    }

    public override void InitializeFromProto(StructureProtoBase proto)
    {
        base.InitializeFromProto(proto);
        if (proto is StockpileProto stockpileProto)
        {
            MaxCapacity = stockpileProto.MaxCapacity;
            AcceptedItemId = stockpileProto.AcceptedItemId;
        }
    }

    /// <summary>
    /// Checks whether a villager is eligible to enter this stockpile.
    /// Rejects empty-handed villagers, villagers with no matching items (if filter set), and when full.
    /// </summary>
    public EntryCheckResult CheckEntry(VillagerLogic villager)
    {
        if (!villager.IsCarryingItems)
        {
            return EntryCheckResult.Rejected(EntryRejectionReason.NotCarryingItems);
        }

        if (AcceptedItemId != null)
        {
            bool hasMatchingItem = false;
            for (int i = 0; i < villager.Inventory.Count; i++)
            {
                if (villager.Inventory[i].ProtoId == AcceptedItemId)
                {
                    hasMatchingItem = true;
                    break;
                }
            }
            if (!hasMatchingItem)
            {
                return EntryCheckResult.Rejected(EntryRejectionReason.NotCarryingRequiredItems);
            }
        }

        if (!HasRoom)
        {
            return EntryCheckResult.Rejected(EntryRejectionReason.StructureFull);
        }

        return EntryCheckResult.Accepted;
    }

    /// <summary>Whether a specific item is accepted by this stockpile's product assignment.</summary>
    public bool AcceptsItem(string itemId)
    {
        if (AcceptedItemId == null)
        {
            return true;
        }
        return AcceptedItemId == itemId;
    }

    /// <inheritdoc />
    public void ProcessStructureTick(float dt, VillagerLookup villagerLookup, ref StructureTickOutput output)
    {
        for (int i = 0; i < PendingVillagerIds.Count; i++)
        {
            var villager = villagerLookup(PendingVillagerIds[i]);
            if (villager == null)
            {
                continue;
            }

            // Unload carried items into the Stockpile (filter-aware)
            if (villager.IsCarryingItems)
            {
                var droppedItems = villager.DropAllItems();
                foreach (var item in droppedItems)
                {
                    if (HasRoom && AcceptsItem(item.ProtoId))
                    {
                        Deposit(item.ProtoId, item.Quantity);
                        output.PendingItems[output.ItemCount++] = new PendingItemOutput(
                            item.ProtoId, item.Quantity, PendingVillagerIds[i]);
                    }
                    else
                    {
                        // Item rejected by filter or stockpile full — give back to villager
                        villager.TryPickUpItem(item);
                    }
                }
            }

            villager.CurrentActivity = null;
            output.PendingExits[output.ExitCount++] = new PendingExit(PendingVillagerIds[i]);
        }
        PendingVillagerIds.Clear();

        if (PendingFilterChange)
        {
            PendingFilterChange = false;
            output.ConfigChanged = true;
        }
    }

    /// <summary>Deposits a resource into the stockpile. Checks both capacity and product assignment.</summary>
    public bool Deposit(string resourceId, int quantity = 1)
    {
        if (!AcceptsItem(resourceId))
        {
            return false;
        }
        if (TotalStored + quantity > MaxCapacity)
        {
            return false;
        }
        StoredResources.TryGetValue(resourceId, out var current);
        StoredResources[resourceId] = current + quantity;
        return true;
    }

    /// <summary>Withdraws a resource from the stockpile.</summary>
    public int Withdraw(string resourceId, int maxQuantity = 1)
    {
        if (!StoredResources.TryGetValue(resourceId, out var available))
        {
            return 0;
        }
        int taken = Math.Min(available, maxQuantity);
        StoredResources[resourceId] = available - taken;
        if (StoredResources[resourceId] <= 0)
        {
            StoredResources.Remove(resourceId);
        }
        return taken;
    }

    /// <summary>Checks how much of a specific resource is stored.</summary>
    public int GetCount(string resourceId)
    {
        return StoredResources.TryGetValue(resourceId, out var count) ? count : 0;
    }

    /// <summary>Assigns a single accepted product. Null = accept all items.</summary>
    public void SetAcceptedItem(string? itemId)
    {
        if (AcceptedItemId == itemId)
        {
            return;
        }
        AcceptedItemId = itemId;
        PendingFilterChange = true;
    }

    /// <summary>Clears the product assignment so the stockpile accepts all items again.</summary>
    public void ClearAcceptedItem()
    {
        if (AcceptedItemId != null)
        {
            AcceptedItemId = null;
            PendingFilterChange = true;
        }
    }

    public override void Tick(float deltaTime)
    {
        // Stockpile is passive — no tick processing needed
    }

    /// <inheritdoc />
    public ItemOutputResult ProcessItem(in PendingItemOutput pending, Structure structure, ItemOutputContext ctx)
    {
        ctx.ItemManager.AddStock(pending.ItemProtoId, pending.Quantity, structure.Position);
        return new ItemOutputResult(
            ItemOutputKind.DroppedOff, pending.ItemProtoId, pending.Quantity,
            pending.TargetVillagerId ?? 0);
    }
}
