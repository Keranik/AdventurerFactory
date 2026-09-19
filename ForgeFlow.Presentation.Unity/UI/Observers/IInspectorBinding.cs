using System;

namespace ForgeFlow.Presentation.Unity.UI.Observers
{
    /// <summary>
    /// Type-erased contract for a single data binding that monitors a value
    /// and fires a callback when the value changes. Used by <see cref="SubscriptionBag"/>
    /// to store heterogeneous bindings in a flat list.
    /// </summary>
    internal interface IInspectorBinding : IDisposable
    {
        /// <summary>
        /// Evaluates the getter, compares with the cached value, and fires
        /// the callback if the value changed.
        /// Returns <c>true</c> if the value changed.
        /// </summary>
        bool Update();

        /// <summary>
        /// Forces the callback to fire with the current value regardless
        /// of whether the value has changed since the last update.
        /// </summary>
        void ForceUpdate();
    }
}
