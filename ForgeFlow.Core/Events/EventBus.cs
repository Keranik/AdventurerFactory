namespace ForgeFlow.Core.Events;

public interface IGameEvent { }

public sealed class EventBus
{
    private readonly Dictionary<Type, object> _holders = new();

    /// <summary>
    /// Optional callback invoked when a handler throws an exception during Publish.
    /// Presentation can wire this to Debug.LogException; tests can wire to Assert.Fail.
    /// If null, exceptions are silently swallowed to protect remaining handlers.
    /// </summary>
    public Action<Exception>? OnHandlerException { get; set; }

    public void Subscribe<T>(Action<T> handler) where T : IGameEvent
    {
        GetOrCreateHolder<T>().Add(handler);
    }

    public void Unsubscribe<T>(Action<T> handler) where T : IGameEvent
    {
        if (_holders.TryGetValue(typeof(T), out var obj))
        {
            ((HandlerList<T>)obj).Remove(handler);
        }
    }

    public void Publish<T>(T gameEvent) where T : IGameEvent
    {
        if (_holders.TryGetValue(typeof(T), out var obj))
        {
            var holder = (HandlerList<T>)obj;
            var snapshot = holder.GetSnapshot();
            var count = snapshot.Length;

            for (int i = 0; i < count; i++)
            {
                try
                {
                    snapshot[i].Invoke(gameEvent);
                }
                catch (Exception ex)
                {
                    OnHandlerException?.Invoke(ex);
                }
            }
        }
    }

    public void Clear()
    {
        _holders.Clear();
    }

    private HandlerList<T> GetOrCreateHolder<T>() where T : IGameEvent
    {
        var type = typeof(T);
        if (!_holders.TryGetValue(type, out var obj))
        {
            obj = new HandlerList<T>();
            _holders[type] = obj;
        }
        return (HandlerList<T>)obj;
    }

    /// <summary>
    /// Copy-on-write typed handler storage. Mutations create a new array so that
    /// in-flight Publish iterations are never affected by Subscribe/Unsubscribe.
    /// </summary>
    private sealed class HandlerList<T> where T : IGameEvent
    {
        private static readonly Action<T>[] _empty = Array.Empty<Action<T>>();
        private Action<T>[] _handlers = _empty;

        public Action<T>[] GetSnapshot() => _handlers;

        public void Add(Action<T> handler)
        {
            var old = _handlers;
            var next = new Action<T>[old.Length + 1];
            Array.Copy(old, next, old.Length);
            next[old.Length] = handler;
            _handlers = next;
        }

        public void Remove(Action<T> handler)
        {
            var old = _handlers;
            int idx = Array.IndexOf(old, handler);
            if (idx < 0) { return; }

            if (old.Length == 1)
            {
                _handlers = _empty;
                return;
            }

            var next = new Action<T>[old.Length - 1];
            Array.Copy(old, 0, next, 0, idx);
            Array.Copy(old, idx + 1, next, idx, old.Length - idx - 1);
            _handlers = next;
        }
    }
}
