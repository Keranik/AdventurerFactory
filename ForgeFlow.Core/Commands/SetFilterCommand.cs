namespace ForgeFlow.Core.Commands;

/// <summary>
/// Command to set or clear the single-item filter on a filter splitter.
/// Pass null <see cref="ItemId"/> to clear the filter.
/// Handled by <see cref="Systems.StructureManager"/>.
/// </summary>
public readonly struct SetFilterCommand : IGameCommand
{
    public ulong SplitterId { get; }
    public string? ItemId { get; }

    public SetFilterCommand(ulong splitterId, string? itemId)
    {
        SplitterId = splitterId;
        ItemId = itemId;
    }

    public override string ToString() =>
        ItemId != null
            ? $"SetFilter splitter {SplitterId} to {ItemId}"
            : $"ClearFilter splitter {SplitterId}";
}
