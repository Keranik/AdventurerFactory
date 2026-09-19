namespace ForgeFlow.Core.Utilities;

/// <summary>
/// Generic object pool for zero-alloc hot paths.
/// Thread-safe via lock. Grows on demand, never shrinks.
/// Used by PathTrafficSystem and SimulationTicker for scratch lists.
/// </summary>
public sealed class ObjectPool<T> where T : class, new()
{
    private readonly Stack<T> _pool;
    private readonly Action<T>? _resetAction;
    private readonly int _maxSize;
    private int _totalCreated;

    public int AvailableCount => _pool.Count;
    public int TotalCreated => _totalCreated;

    public ObjectPool(int initialCapacity = 16, int maxSize = 4096, Action<T>? resetAction = null)
    {
        _pool = new Stack<T>(initialCapacity);
        _maxSize = maxSize;
        _resetAction = resetAction;

        for (int i = 0; i < initialCapacity; i++)
        {
            _pool.Push(new T());
            _totalCreated++;
        }
    }

    /// <summary>Gets an object from the pool or creates a new one.</summary>
    public T Get()
    {
        lock (_pool)
        {
            if (_pool.Count > 0)
            {
                return _pool.Pop();
            }
        }

        _totalCreated++;
        return new T();
    }

    /// <summary>Returns an object to the pool.</summary>
    public void Return(T item)
    {
        _resetAction?.Invoke(item);

        lock (_pool)
        {
            if (_pool.Count < _maxSize)
            {
                _pool.Push(item);
            }
        }
    }

    /// <summary>Clears the pool.</summary>
    public void Clear()
    {
        lock (_pool)
        {
            _pool.Clear();
        }
    }
}

/// <summary>
/// Pool specifically for List&lt;T&gt; instances used as scratch buffers.
/// Automatically clears lists on return.
/// </summary>
public sealed class ListPool<T>
{
    private readonly Stack<List<T>> _pool;
    private readonly int _maxSize;

    public ListPool(int initialCapacity = 8, int maxSize = 256)
    {
        _pool = new Stack<List<T>>(initialCapacity);
        _maxSize = maxSize;

        for (int i = 0; i < initialCapacity; i++)
        {
            _pool.Push(new List<T>(16));
        }
    }

    /// <summary>Gets a clean list from the pool.</summary>
    public List<T> Get()
    {
        lock (_pool)
        {
            if (_pool.Count > 0)
            {
                return _pool.Pop();
            }
        }
        return new List<T>(16);
    }

    /// <summary>Clears and returns a list to the pool.</summary>
    public void Return(List<T> list)
    {
        list.Clear();
        lock (_pool)
        {
            if (_pool.Count < _maxSize)
            {
                _pool.Push(list);
            }
        }
    }
}
