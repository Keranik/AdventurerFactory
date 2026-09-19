namespace ForgeFlow.Core.Proto.Prototypes;

/// <summary>
/// Proto for a stockpile/warehouse. Defines max storage capacity and optional product assignment.
/// When AcceptedItemId is null, all items are accepted. When set, only that item
/// can be deposited.
/// </summary>
public sealed class StockpileProto : StockpileProtoBase
{
    /// <summary>
    /// Optional single product this stockpile accepts.
    /// Null = accept all items (default). Non-null = only accept the specified item.
    /// Configurable at runtime via the Stockpile Inspector.
    /// </summary>
    public string? AcceptedItemId { get; set; }
}
