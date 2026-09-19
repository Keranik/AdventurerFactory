using UnityEngine.UIElements;

namespace ForgeFlow.Presentation.Unity.UI
{
    /// <summary>
    /// Abstraction for registering transient UI elements (dialogs, popups, tooltips)
    /// so that <see cref="IUIFocusProvider.IsPointerOverUI"/> returns <c>true</c>
    /// while the pointer is over them. Implemented by <see cref="UIManager"/>.
    /// Keeps <see cref="Components.ForgePanel"/> decoupled from the concrete manager.
    /// </summary>
    internal interface ITransientElementTracker
    {
        /// <summary>
        /// Registers pointer-enter/leave tracking on a transient element so it
        /// participates in the pointer-over-UI check. Call
        /// <see cref="UnregisterTransientElement"/> when the element is removed.
        /// </summary>
        void RegisterTransientElement(string trackingId, VisualElement element);

        /// <summary>
        /// Removes tracking state for a transient element previously registered
        /// via <see cref="RegisterTransientElement"/>. Safe to call if never registered.
        /// </summary>
        void UnregisterTransientElement(string trackingId);
    }
}
