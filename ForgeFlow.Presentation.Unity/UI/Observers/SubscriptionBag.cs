using System;
using System.Collections.Generic;

namespace ForgeFlow.Presentation.Unity.UI.Observers
{
    /// <summary>
    /// Groups multiple <see cref="IInspectorBinding"/> instances so they can be
    /// updated in bulk and disposed together when the owning inspector is torn down.
    /// <para>
    /// Typical usage inside an inspector:
    /// <code>
    /// _bindings = new SubscriptionBag();
    /// _bindings.Watch(() => logic.StoredCount)
    ///          .OnChanged(count => _countLabel.text = count.ToString());
    /// </code>
    /// Then in the tick path: <c>_bindings.UpdateAll();</c>
    /// </para>
    /// </summary>
    internal sealed class SubscriptionBag : IDisposable
    {
        private readonly List<IInspectorBinding> _bindings = new();
        private bool _disposed;

        /// <summary>
        /// Registers a binding directly with a getter and change callback.
        /// The initial value is snapshot immediately; the callback fires only
        /// on subsequent changes.
        /// </summary>
        public InspectorBinding<T> Bind<T>(Func<T> getter, Action<T> onChange, IEqualityComparer<T>? comparer = null)
        {
            ThrowIfDisposed();
            var binding = new InspectorBinding<T>(getter, onChange, comparer);
            _bindings.Add(binding);
            return binding;
        }

        /// <summary>
        /// Fluent entry point. Returns a <see cref="BindingBuilder{T}"/> that
        /// lets the caller chain <c>.OnChanged(callback)</c>.
        /// </summary>
        public BindingBuilder<T> Watch<T>(Func<T> getter)
        {
            ThrowIfDisposed();
            return new BindingBuilder<T>(this, getter);
        }

        /// <summary>
        /// Evaluates every binding and fires callbacks for values that changed.
        /// Returns the number of bindings that detected a change.
        /// </summary>
        public int UpdateAll()
        {
            if (_disposed)
            {
                return 0;
            }

            int changed = 0;
            for (int i = 0; i < _bindings.Count; i++)
            {
                if (_bindings[i].Update())
                {
                    changed++;
                }
            }

            return changed;
        }

        /// <summary>
        /// Forces every binding to fire its callback with the current value,
        /// regardless of whether the value has changed.
        /// </summary>
        public void ForceUpdateAll()
        {
            if (_disposed)
            {
                return;
            }

            for (int i = 0; i < _bindings.Count; i++)
            {
                _bindings[i].ForceUpdate();
            }
        }

        /// <summary>Number of active bindings.</summary>
        public int Count => _bindings.Count;

        /// <summary>Disposes and removes all bindings without disposing the bag itself.</summary>
        public void Clear()
        {
            for (int i = 0; i < _bindings.Count; i++)
            {
                _bindings[i].Dispose();
            }

            _bindings.Clear();
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            Clear();
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(SubscriptionBag));
            }
        }
    }
}
