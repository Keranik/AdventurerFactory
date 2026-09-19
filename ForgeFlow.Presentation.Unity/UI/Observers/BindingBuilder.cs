using System;
using System.Collections.Generic;

namespace ForgeFlow.Presentation.Unity.UI.Observers
{
    /// <summary>
    /// Fluent builder returned by <see cref="SubscriptionBag.Watch{T}"/>.
    /// Allows the caller to chain <c>.OnChanged(callback)</c> to complete
    /// the binding registration.
    /// </summary>
    internal readonly struct BindingBuilder<T>
    {
        private readonly SubscriptionBag _bag;
        private readonly Func<T> _getter;

        internal BindingBuilder(SubscriptionBag bag, Func<T> getter)
        {
            _bag = bag;
            _getter = getter;
        }

        /// <summary>
        /// Completes the binding with a change callback. The callback receives
        /// the new value whenever it differs from the previously cached value.
        /// </summary>
        public InspectorBinding<T> OnChanged(Action<T> callback, IEqualityComparer<T>? comparer = null)
        {
            return _bag.Bind(_getter, callback, comparer);
        }
    }
}
