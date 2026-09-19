using System.Runtime.CompilerServices;

namespace ForgeFlow.Core.Utilities;

/// <summary>
/// Fixed-size, zero-alloc circular buffer for high-performance wait queues.
/// Value type (struct) to avoid allocations in hot paths.
/// </summary>
public struct CircularBuffer<T>
{
    private readonly T[] _buffer;
    private int _head;
    private int _tail;
    private int _count;

    /// <summary>Number of items currently in the buffer.</summary>
    public int Count => _count;

    /// <summary>Maximum capacity of the buffer.</summary>
    public int Capacity => _buffer?.Length ?? 0;

    /// <summary>True when the buffer has reached its maximum capacity.</summary>
    public bool IsFull => _count >= Capacity;

    /// <summary>True when the buffer contains no items.</summary>
    public bool IsEmpty => _count == 0;

    /// <summary>Creates a new circular buffer with the specified fixed capacity.</summary>
    public CircularBuffer(int capacity)
    {
        if (capacity <= 0) { throw new ArgumentOutOfRangeException(nameof(capacity)); }

        _buffer = new T[capacity];
        _head = 0;
        _tail = 0;
        _count = 0;
    }

    /// <summary>Adds an item to the tail of the buffer. Returns false if full.</summary>
    public bool Enqueue(T item)
    {
        if (_count >= _buffer.Length) { return false; }

        _buffer[_tail] = item;
        _tail = (_tail + 1) % _buffer.Length;
        _count++;
        return true;
    }

    /// <summary>Removes and returns the item at the head of the buffer. Returns false if empty.</summary>
    public bool TryDequeue(out T item)
    {
        if (_count == 0)
        {
            item = default!;
            return false;
        }

        item = _buffer[_head];
        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
        {
            _buffer[_head] = default!;
        }
        _head = (_head + 1) % _buffer.Length;
        _count--;
        return true;
    }

    /// <summary>Returns the item at the head without removing it. Returns false if empty.</summary>
    public bool TryPeek(out T item)
    {
        if (_count == 0)
        {
            item = default!;
            return false;
        }

        item = _buffer[_head];
        return true;
    }

    /// <summary>
    /// Resets the buffer to empty without reallocating.
    /// For reference types, clears the backing array to allow GC collection.
    /// For value types, the JIT eliminates the clear branch entirely (zero-cost).
    /// </summary>
    public void Clear()
    {
        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>() && _buffer != null)
        {
            Array.Clear(_buffer, 0, _buffer.Length);
        }
        _head = 0;
        _tail = 0;
        _count = 0;
    }
}
