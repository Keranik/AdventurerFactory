namespace ForgeFlow.Core.Data;

/// <summary>
/// Common interface for all pure data-lookup registries and Db classes.
/// Provides a uniform way to query size and reset state.
/// </summary>
public interface IRegistry
{
    /// <summary>Total number of registered entries.</summary>
    int Count { get; }

    /// <summary>Removes all registered entries.</summary>
    void Clear();
}
