
namespace ForgeFlow.Presentation.Unity.UI
{
    /// <summary>
    /// Z-order layers for the UIManager window system.
    /// Higher values render on top of lower values.
    /// </summary>
    public enum UILayer
    {
        Background = 0,
        HUD = 100,
        Gameplay = 200,
        Toolbar = 300,
        Menu = 400,
        Modal = 500,
        Tooltip = 600,
        Debug = 700
    }
}
