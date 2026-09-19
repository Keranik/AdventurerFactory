namespace ForgeFlow.Core.Commands;

/// <summary>
/// Command to set or clear the accepted product on a stockpile.
/// Pass null <see cref="ItemId"/> to accept all items.
/// Handled by <see cref="Systems.StructureManager"/>.
/// </summary>
public readonly struct SetStockpileFilterCommand : IGameCommand
{
    public ulong StockpileId { get; }
    public string? ItemId { get; }

    public SetStockpileFilterCommand(ulong stockpileId, string? itemId)
    {
        StockpileId = stockpileId;
        ItemId = itemId;
    }

    public override string ToString() =>
        ItemId != null
            ? $"SetStockpileFilter {StockpileId} to {ItemId}"
            : $"ClearStockpileFilter {StockpileId}";
}
