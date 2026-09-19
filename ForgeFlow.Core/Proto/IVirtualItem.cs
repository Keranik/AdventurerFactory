namespace ForgeFlow.Core.Proto;

/// <summary>
/// Marker interface for items that exist only as virtual stock counts
/// (gold, mana, relics, bulk resources). Virtual items are never instantiated
/// as physical ItemInstance objects on the map — they are tracked by
/// ItemManager as simple integer quantities.
/// </summary>
public interface IVirtualItem
{
    bool IsVirtual { get; }
}
