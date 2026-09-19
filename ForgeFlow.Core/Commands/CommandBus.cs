namespace ForgeFlow.Core.Commands;

/// <summary>
/// Marker interface for all game commands (player-initiated actions).
/// Commands are directed, imperative, and handled by exactly one handler.
/// Contrast with <see cref="Events.IGameEvent"/> which is broadcast to many listeners.
/// </summary>
public interface IGameCommand { }

/// <summary>
/// Result of dispatching a command — success or failure with an optional reason.
/// </summary>
public readonly struct CommandResult
{
    public bool Success { get; }
    public string? Reason { get; }

    private CommandResult(bool success, string? reason)
    {
        Success = success;
        Reason = reason;
    }

    public static CommandResult Ok() => new CommandResult(true, null);
    public static CommandResult Fail(string reason) => new CommandResult(false, reason);

    public override string ToString() =>
        Success ? "CommandResult(OK)" : $"CommandResult(FAIL: {Reason})";
}

/// <summary>
/// Single-handler command dispatch bus. Each command type has exactly one handler
/// that returns a <see cref="CommandResult"/>. Complementary to <see cref="Events.EventBus"/>
/// which handles broadcast notifications.
///
/// Contract:
/// - One handler per command type (duplicate registration throws).
/// - Dispatch with no handler throws (catches wiring bugs at dev time).
/// - Synchronous dispatch returns a result so callers know success/failure.
/// </summary>
public sealed class CommandBus
{
    private readonly Dictionary<Type, Delegate> _handlers = new();
    private readonly List<ICommandValidator> _validators = new();
    private readonly List<Action> _undoStack = new();

    /// <summary>
    /// Optional callback invoked when a handler throws an exception during Dispatch.
    /// If null, the exception propagates to the caller.
    /// </summary>
    public Action<Exception>? OnHandlerException { get; set; }

    /// <summary>
    /// Optional command log for debugging and replay. When attached and enabled,
    /// all dispatched commands and their results are recorded.
    /// </summary>
    public CommandLog? Log { get; set; }

    /// <summary>
    /// Maximum number of undo actions to retain. Default: 50.
    /// When exceeded, the oldest undo action is discarded.
    /// </summary>
    public int MaxUndoDepth { get; set; } = 50;

    /// <summary>Number of undo actions currently on the stack.</summary>
    public int UndoCount => _undoStack.Count;

    /// <summary>True if there are undo actions available.</summary>
    public bool CanUndo => _undoStack.Count > 0;

    /// <summary>
    /// Register the single handler for command type <typeparamref name="T"/>.
    /// Throws <see cref="InvalidOperationException"/> if a handler is already registered.
    /// </summary>
    public void Register<T>(Func<T, CommandResult> handler) where T : IGameCommand
    {
        var type = typeof(T);
        if (_handlers.ContainsKey(type))
        {
            throw new InvalidOperationException(
                $"CommandBus: A handler is already registered for {type.Name}. " +
                "Each command type must have exactly one handler.");
        }
        _handlers[type] = handler;
    }

    /// <summary>
    /// Unregister the handler for command type <typeparamref name="T"/>.
    /// Safe to call even if no handler is registered.
    /// </summary>
    public void Unregister<T>() where T : IGameCommand
    {
        _handlers.Remove(typeof(T));
    }

    /// <summary>
    /// Dispatch a command to its registered handler. Returns the handler's result.
    /// <list type="number">
    /// <item>Runs all registered <see cref="ICommandValidator"/>s — first rejection short-circuits.</item>
    /// <item>Invokes the registered handler.</item>
    /// <item>Records the result in <see cref="Log"/> (if attached and enabled).</item>
    /// </list>
    /// Throws <see cref="InvalidOperationException"/> if no handler is registered.
    /// If the handler throws and <see cref="OnHandlerException"/> is set, the callback
    /// is invoked and <see cref="CommandResult.Fail"/> is returned.
    /// </summary>
    public CommandResult Dispatch<T>(T command) where T : IGameCommand
    {
        // 1. Run validators — first rejection short-circuits dispatch
        for (int i = 0; i < _validators.Count; i++)
        {
            var rejection = _validators[i].Validate(command);
            if (rejection.HasValue)
            {
                Log?.Record(command, rejection.Value);
                return rejection.Value;
            }
        }

        // 2. Find handler
        var type = typeof(T);
        if (!_handlers.TryGetValue(type, out var handler))
        {
            throw new InvalidOperationException(
                $"CommandBus: No handler registered for {type.Name}. " +
                "Every command must have exactly one handler.");
        }

        // 3. Execute handler
        try
        {
            var result = ((Func<T, CommandResult>)handler).Invoke(command);
            Log?.Record(command, result);
            return result;
        }
        catch (Exception ex)
        {
            if (OnHandlerException != null)
            {
                OnHandlerException.Invoke(ex);
                var failResult = CommandResult.Fail($"Handler threw: {ex.Message}");
                Log?.Record(command, failResult);
                return failResult;
            }
            throw;
        }
    }

    /// <summary>
    /// Returns true if a handler is registered for command type <typeparamref name="T"/>.
    /// </summary>
    public bool HasHandler<T>() where T : IGameCommand
    {
        return _handlers.ContainsKey(typeof(T));
    }

    // ── Validators ────────────────────────────────────────────────────

    /// <summary>
    /// Adds a validator that runs before every command handler.
    /// Validators execute in registration order; the first rejection short-circuits dispatch.
    /// </summary>
    public void AddValidator(ICommandValidator validator)
    {
        _validators.Add(validator);
    }

    /// <summary>
    /// Removes a previously added validator.
    /// </summary>
    public void RemoveValidator(ICommandValidator validator)
    {
        _validators.Remove(validator);
    }

    // ── Undo ─────────────────────────────────────────────────────────

    /// <summary>
    /// Pushes an undo action onto the stack. Called by command handlers after successful
    /// execution to make their command undoable. The action should reverse the command's effects.
    /// When the stack exceeds <see cref="MaxUndoDepth"/>, the oldest action is discarded.
    /// </summary>
    public void PushUndo(Action undoAction)
    {
        if (_undoStack.Count >= MaxUndoDepth && _undoStack.Count > 0)
        {
            _undoStack.RemoveAt(0);
        }
        _undoStack.Add(undoAction);
    }

    /// <summary>
    /// Pops and executes the most recent undo action.
    /// Returns <see cref="CommandResult.Ok"/> on success, or <see cref="CommandResult.Fail"/>
    /// if the stack is empty or the undo action throws.
    /// </summary>
    public CommandResult Undo()
    {
        if (_undoStack.Count == 0)
        {
            return CommandResult.Fail("Nothing to undo");
        }

        var action = _undoStack[_undoStack.Count - 1];
        _undoStack.RemoveAt(_undoStack.Count - 1);
        try
        {
            action();
            return CommandResult.Ok();
        }
        catch (Exception ex)
        {
            if (OnHandlerException != null)
            {
                OnHandlerException.Invoke(ex);
                return CommandResult.Fail($"Undo threw: {ex.Message}");
            }
            throw;
        }
    }

    /// <summary>Removes all undo actions from the stack.</summary>
    public void ClearUndoStack()
    {
        _undoStack.Clear();
    }

    /// <summary>
    /// Dispatches a command from a boxed object. Used by <see cref="CommandLog.ReplayAll"/>
    /// for replay from deserialized JSON. Looks up the handler by runtime type.
    /// </summary>
    public CommandResult DispatchObject(object command)
    {
        var type = command.GetType();
        if (!_handlers.TryGetValue(type, out var handler))
        {
            return CommandResult.Fail($"No handler registered for {type.Name}");
        }

        try
        {
            return ((Delegate)handler).DynamicInvoke(command) is CommandResult result
                ? result
                : CommandResult.Fail("Handler returned unexpected type");
        }
        catch (Exception ex)
        {
            if (OnHandlerException != null)
            {
                OnHandlerException.Invoke(ex);
                return CommandResult.Fail($"Handler threw: {ex.Message}");
            }
            throw;
        }
    }

    // ── Reset ────────────────────────────────────────────────────────

    /// <summary>
    /// Removes all registered handlers, validators, and undo actions.
    /// </summary>
    public void Clear()
    {
        _handlers.Clear();
        _validators.Clear();
        _undoStack.Clear();
    }
}
