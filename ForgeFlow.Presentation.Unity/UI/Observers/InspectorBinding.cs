using System;
using System.Collections.Generic;

namespace ForgeFlow.Presentation.Unity.UI.Observers
{
    /// <summary>
    /// A single data binding that monitors a value via a getter function
    /// and invokes a callback only when the value actually changes.
    /// <para>
    /// The initial value is snapshot on construction so the callback is NOT
    /// fired until the value differs from that snapshot.
    /// </para>
    /// </summary>
    internal sealed class InspectorBinding<T> : IInspectorBinding
    {
        private readonly Func<T> _getter;
        private readonly Action<T> _onChange;
        private readonly IEqualityComparer<T> _comparer;
        private T _cachedValue;
        private bool _disposed;

        internal InspectorBinding(Func<T> getter, Action<T> onChange, IEqualityComparer<T>? comparer = null)
        {
            _getter = getter ?? throw new ArgumentNullException(nameof(getter));
            _onChange = onChange ?? throw new ArgumentNullException(nameof(onChange));
            _comparer = comparer ?? EqualityComparer<T>.Default;
            _cachedValue = _getter();
        }

        /// <inheritdoc />
        public bool Update()
        {
            if (_disposed)
            {
                return false;
            }

            T current = _getter();
            if (_comparer.Equals(_cachedValue, current))
            {
                return false;
            }

            _cachedValue = current;
            _onChange(current);
            return true;
        }

        /// <inheritdoc />
        public void ForceUpdate()
        {
            if (_disposed)
            {
                return;
            }

            _cachedValue = _getter();
            _onChange(_cachedValue);
        }

        /// <summary>Returns the most recently cached value without evaluating the getter.</summary>
        internal T CachedValue => _cachedValue;

        /// <inheritdoc />
        public void Dispose()
        {
            _disposed = true;
        }
    }
}
