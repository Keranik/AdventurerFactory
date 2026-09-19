
using ForgeFlow.Presentation.Unity.Input;

namespace ForgeFlow.Presentation.Unity.UI
{
    /// <summary>
    /// Single authority for UI focus state, consumed by the input layer.
    /// <see cref="InputControllerStack"/> queries this interface instead of
    /// poking into UI Toolkit internals (Pick, UIDocument, VisualElement).
    /// Implemented by <see cref="UIManager"/>.
    /// </summary>
    internal interface IUIFocusProvider
    {
        /// <summary>True when the pointer is over any visible UI window.</summary>
        bool IsPointerOverUI { get; }

        /// <summary>True when a text field or other keyboard-consuming element has focus.</summary>
        bool HasKeyboardFocus { get; }

        /// <summary>True when a modal dialog is open (blocks all gameplay input).</summary>
        bool IsModalOpen { get; }
    }
}
