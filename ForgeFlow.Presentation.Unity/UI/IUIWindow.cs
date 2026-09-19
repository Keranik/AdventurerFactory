using UnityEngine.UIElements;

namespace ForgeFlow.Presentation.Unity.UI
{
    /// <summary>
    /// Contract for all windows managed by <see cref="UIManager"/>.
    /// Every panel, HUD overlay, menu, or modal implements this interface.
    /// </summary>
    public interface IUIWindow
    {
        /// <summary>Unique identifier for this window. Must match a <see cref="WindowIds"/> constant.</summary>
        string WindowId { get; }

        /// <summary>Root visual element rendered in the UI document.</summary>
        VisualElement Root { get; }

        /// <summary>Whether the window is currently visible.</summary>
        bool IsVisible { get; }

        /// <summary>Shows the window, optionally accepting context data.</summary>
        void Show(object? context = null);

        /// <summary>Hides the window.</summary>
        void Hide();

        /// <summary>Refreshes the window content (e.g. after data changes).</summary>
        void Refresh();

        /// <summary>Re-applies theme colors.</summary>
        void ApplyTheme();

        /// <summary>Disposes resources and unsubscribes from events.</summary>
        void Dispose();
    }

    /// <summary>
    /// Extended window contract for windows that need per-frame updates
    /// (animations, live data refresh, pulse effects).
    /// </summary>
    public interface ITickableWindow : IUIWindow
    {
        /// <summary>Called every frame while the window is visible.</summary>
        void Tick(float deltaTime);
    }
}
