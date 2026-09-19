using System.Diagnostics.CodeAnalysis;
using ForgeFlow.Core.Proto;
using ForgeFlow.Core.Proto.Prototypes;

namespace ForgeFlow.Core.Data;

public sealed class ItemRegistry : IRegistry
{
    private readonly Dictionary<string, ItemProto> _items = new();

    public void Register(ItemProto item)
    {
        _items[item.Id] = item;
    }

    public ItemProto? Get(string id)
    {
        return _items.TryGetValue(id, out var item) ? item : null;
    }

    public bool TryGet(string id, [NotNullWhen(true)] out ItemProto? item)
    {
        return _items.TryGetValue(id, out item);
    }

    public IEnumerable<ItemProto> GetAll() => _items.Values;

    public IEnumerable<ItemProto> GetByTier(int tier)
    {
        foreach (var item in _items.Values)
        {
            if (item.Tier == tier) yield return item;
        }
    }

    public IEnumerable<ItemProto> GetBySlot(Entities.EquipSlot slot)
    {
        foreach (var item in _items.Values)
        {
            if (item.Equipment != null && item.Equipment.Slot == slot) yield return item;
        }
    }

    public void Clear() => _items.Clear();
    public int Count => _items.Count;
}
