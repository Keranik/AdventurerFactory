namespace ForgeFlow.Core.Utilities;

/// <summary>
/// Central ID allocator for all entity types.
/// Ensures globally unique IDs across EntityBase, ItemInstance,
/// and any future entity that needs a runtime ID.
/// Thread-safe via Interlocked.
/// </summary>
public static class EntityIdFactory
{
    private static long _nextId;

    /// <summary>Allocates the next globally unique ID. IDs start at 1; 0 is reserved as "no ID".</summary>
    public static ulong Next() => (ulong)Interlocked.Increment(ref _nextId);

    /// <summary>Returns the current counter value (the last allocated ID). Used for save persistence.</summary>
    public static ulong CurrentCounter => (ulong)Interlocked.Read(ref _nextId);

    /// <summary>
    /// Restores the counter from a saved value. Call during load before any entity is deserialized.
    /// </summary>
    public static void SetCounter(ulong value) => Interlocked.Exchange(ref _nextId, (long)value);

    /// <summary>
    /// Resets the counter to 0 for a fresh new game. Call from GameBootstrapper.ApplyNewGameSettings.
    /// </summary>
    public static void ResetForNewGame() => Interlocked.Exchange(ref _nextId, 0);

    /// <summary>
    /// Resets the counter to 0 so the next call to Next() returns 1.
    /// Only for use in tests — never call in production code.
    /// </summary>
    public static void ResetForTesting() => Interlocked.Exchange(ref _nextId, 0);
}
