namespace ForgeFlow.Core.Proto;

/// <summary>
/// Mutable output struct populated by <see cref="IRoutingNodeTickHandler.ProcessRoutingTick"/>.
/// Pre-allocated once on the manager and reused every tick to avoid per-tick allocations.
/// Routing nodes are simpler than structures — they only signal config changes, not
/// occupant exits or item outputs.
/// </summary>
public struct RoutingTickOutput
{
    /// <summary>If true, the manager should publish a config-change event for this node.</summary>
    public bool ConfigChanged;

    /// <summary>Resets all flags for reuse.</summary>
    public void Reset()
    {
        ConfigChanged = false;
    }
}

/// <summary>
/// Implemented by routing node Logic classes that have per-tick processing needs.
/// The <see cref="Systems.StructureManager"/> calls this interface each tick,
/// then reads the <see cref="RoutingTickOutput"/> to publish appropriate events.
/// <para>
/// This follows the same pattern as <see cref="IStructureTickHandler"/> and
/// <see cref="IEntryGated"/>: nodes declare behavior via an interface, the manager orchestrates.
/// </para>
/// </summary>
public interface IRoutingNodeTickHandler
{
    /// <summary>
    /// Processes this routing node's pending state for the current tick.
    /// Populates <paramref name="output"/> with flags for the manager to handle.
    /// Must not publish events or interact with systems directly.
    /// </summary>
    /// <param name="output">Pre-allocated output struct. Caller resets before each call.</param>
    void ProcessRoutingTick(ref RoutingTickOutput output);
}
