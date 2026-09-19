using System.Text.Json;

namespace ForgeFlow.Core.Commands;

/// <summary>
/// A single recorded command dispatch entry: command type, payload JSON, result, and simulation tick.
/// Serializable for save data persistence and deterministic replay.
/// </summary>
public readonly struct CommandLogEntry
{
    /// <summary>The simple type name of the command (e.g. "PlaceStructureCommand").</summary>
    public string CommandTypeName { get; }

    /// <summary>Human-readable description of the command (from ToString()).</summary>
    public string Description { get; }

    /// <summary>JSON-serialized payload of the command for replay.</summary>
    public string? PayloadJson { get; }

    /// <summary>The result returned by the handler (or validator short-circuit).</summary>
    public CommandResult Result { get; }

    /// <summary>The simulation tick at which this command was dispatched.</summary>
    public ulong Tick { get; }

    public CommandLogEntry(string commandTypeName, string description, string? payloadJson, CommandResult result, ulong tick)
    {
        CommandTypeName = commandTypeName;
        Description = description;
        PayloadJson = payloadJson;
        Result = result;
        Tick = tick;
    }

    public override string ToString() =>
        Result.Success
            ? $"[Tick {Tick}] {CommandTypeName}: {Description} → OK"
            : $"[Tick {Tick}] {CommandTypeName}: {Description} → FAIL: {Result.Reason}";
}

/// <summary>
/// Records all dispatched commands and their results for debugging and replay.
/// Opt-in via <see cref="IsEnabled"/>: disabled by default. Uses a fixed-capacity
/// ring buffer to bound memory usage. Oldest entries are overwritten when full.
///
/// Attach to <see cref="CommandBus"/> via its <see cref="CommandBus.Log"/> property:
/// <code>
/// bus.Log = new CommandLog(capacity: 512);
/// bus.Log.IsEnabled = true;
/// </code>
/// </summary>
public sealed class CommandLog
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        IncludeFields = true
    };

    private readonly CommandLogEntry[] _entries;
    private readonly Func<CommandBus, CommandResult>?[] _replayers;
    private int _writeIndex;
    private int _count;
    private ulong _currentTick;

    /// <summary>When true, dispatched commands are recorded. Default: false.</summary>
    public bool IsEnabled { get; set; }

    /// <summary>Number of entries currently in the log.</summary>
    public int Count => _count;

    /// <summary>Maximum number of entries the log can hold before overwriting oldest.</summary>
    public int Capacity => _entries.Length;

    /// <summary>Creates a command log with the specified fixed capacity.</summary>
    public CommandLog(int capacity = 1024)
    {
        if (capacity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity must be positive.");
        }

        _entries = new CommandLogEntry[capacity];
        _replayers = new Func<CommandBus, CommandResult>?[capacity];
    }

    /// <summary>
    /// Sets the current simulation tick for subsequent log entries.
    /// Call this from <see cref="Systems.SimulationTicker"/> at the start of each tick.
    /// </summary>
    public void SetTick(ulong tick) => _currentTick = tick;

    /// <summary>
    /// Records a command dispatch. Called internally by <see cref="CommandBus.Dispatch{T}"/>.
    /// Only records when <see cref="IsEnabled"/> is true.
    /// Stores both a closure for in-memory replay and a JSON payload for persistence.
    /// </summary>
    internal void Record<T>(T command, CommandResult result) where T : IGameCommand
    {
        if (!IsEnabled)
        {
            return;
        }

        string? payloadJson = null;
        try
        {
            payloadJson = JsonSerializer.Serialize(command, typeof(T), JsonOptions);
        }
        catch
        {
            // If serialization fails, persist without payload — in-memory replay still works via closure.
        }

        int index = _writeIndex;
        _entries[index] = new CommandLogEntry(
            typeof(T).Name,
            command.ToString() ?? typeof(T).Name,
            payloadJson,
            result,
            _currentTick);
        _replayers[index] = bus => bus.Dispatch(command);

        _writeIndex = (_writeIndex + 1) % _entries.Length;
        if (_count < _entries.Length)
        {
            _count++;
        }
    }

    /// <summary>
    /// Returns the log entry at the given chronological index (0 = oldest).
    /// </summary>
    public CommandLogEntry GetEntry(int index)
    {
        if (index < 0 || index >= _count)
        {
            throw new ArgumentOutOfRangeException(nameof(index),
                $"Index {index} is out of range [0, {_count}).");
        }

        int oldest = _count < _entries.Length ? 0 : _writeIndex;
        return _entries[(oldest + index) % _entries.Length];
    }

    /// <summary>
    /// Returns all entries in chronological order (oldest first).
    /// Allocates a new array — intended for debugging, not hot paths.
    /// </summary>
    public CommandLogEntry[] GetAllEntries()
    {
        var result = new CommandLogEntry[_count];
        int oldest = _count < _entries.Length ? 0 : _writeIndex;
        for (int i = 0; i < _count; i++)
        {
            result[i] = _entries[(oldest + i) % _entries.Length];
        }
        return result;
    }

    /// <summary>
    /// Replays all logged commands on the given <see cref="CommandBus"/> in chronological order
    /// using in-memory closures. Returns the result of each replayed command.
    /// </summary>
    public CommandResult[] ReplayAll(CommandBus bus)
    {
        var results = new CommandResult[_count];
        int oldest = _count < _entries.Length ? 0 : _writeIndex;
        for (int i = 0; i < _count; i++)
        {
            var replayer = _replayers[(oldest + i) % _entries.Length];
            if (replayer != null)
            {
                results[i] = replayer(bus);
            }
            else
            {
                results[i] = CommandResult.Fail("No replay data available");
            }
        }
        return results;
    }

    /// <summary>
    /// Replays all logged commands from their persisted JSON payloads, using the
    /// <see cref="CommandLogRegistry"/> to deserialize and dispatch each entry.
    /// Use this for replay from save data where closures are not available.
    /// </summary>
    public CommandResult[] ReplayAllFromJson(CommandBus bus, CommandLogRegistry registry)
    {
        var results = new CommandResult[_count];
        int oldest = _count < _entries.Length ? 0 : _writeIndex;
        for (int i = 0; i < _count; i++)
        {
            var entry = _entries[(oldest + i) % _entries.Length];
            if (entry.PayloadJson != null && registry.TryResolve(entry.CommandTypeName, out var commandType))
            {
                try
                {
                    var command = JsonSerializer.Deserialize(entry.PayloadJson, commandType, JsonOptions);
                    if (command != null)
                    {
                        results[i] = bus.DispatchObject(command);
                        continue;
                    }
                }
                catch
                {
                    // Fall through to failure
                }
            }
            results[i] = CommandResult.Fail("No replay data available");
        }
        return results;
    }

    /// <summary>Clears all log entries and replay data.</summary>
    public void Clear()
    {
        Array.Clear(_entries, 0, _entries.Length);
        Array.Clear(_replayers, 0, _replayers.Length);
        _writeIndex = 0;
        _count = 0;
    }
}
