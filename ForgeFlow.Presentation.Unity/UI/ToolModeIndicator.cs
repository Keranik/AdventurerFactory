using ForgeFlow.Presentation.Unity.Input;
using ForgeFlow.Presentation.Unity.UI.Components;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace ForgeFlow.Presentation.Unity.UI
{

/// <summary>
/// Floating bar displayed just above the gameplay toolbar.
/// Shows the active tool mode name and dynamically lists all active
/// shortcut keys with their real bound key labels (via InputSystem binding display).
/// Subscribes to <see cref="InputControllerStack.ToolModeChanged"/>.
/// Implements <see cref="IUIWindow"/> for UIManager integration.
/// </summary>
internal sealed class ToolModeIndicator : IUIWindow
{
    private readonly ForgeContainer _root;
    private readonly ForgeLabel _modeLabel;
    private readonly ForgeContainer _shortcutRow;
    private bool _isVisible;

    private FactoryInputActions? _inputActions;

    public string WindowId => WindowIds.ToolModeIndicator;
    public VisualElement Root => _root;
    public bool IsVisible => _isVisible;

    public ToolModeIndicator()
    {
        _root = ForgeContainer.Create()
            .Position(Position.Absolute)
            .Bottom(104)
            .LeftPercent(50)
            .FlexDirection(FlexDirection.Row)
            .AlignItems(Align.Center)
            .JustifyContent(Justify.Center)
            .BorderRadius(6)
            .PaddingTop(4).PaddingBottom(4)
            .PaddingLeft(10).PaddingRight(10)
            .MinHeight(28)
            .Display(DisplayStyle.None)
            .Build();
        _root.style.backgroundColor = new Color(0.08f, 0.08f, 0.12f, 0.85f);
        _root.style.translate = new Translate(new Length(-50, LengthUnit.Percent), 0);

        _modeLabel = ForgeLabel.CreateRaw("", ForgeLabelSize.Normal)
            .Color("status.info").Bold().MarginRight(12).Build();
        _root.Add(_modeLabel);

        _shortcutRow = ForgeContainer.Create()
            .FlexDirection(FlexDirection.Row)
            .AlignItems(Align.Center)
            .Build();
        _root.Add(_shortcutRow);
    }

    public void SetInputActions(FactoryInputActions inputActions)
    {
        _inputActions = inputActions;
    }

    /// <summary>
    /// Called when tool mode changes. Rebuilds the indicator bar.
    /// </summary>
    public void OnToolModeChanged(ToolModeInfo info)
    {
        if (info.ModeName == null)
        {
            _root.style.display = DisplayStyle.None;
            return;
        }

        _root.style.display = DisplayStyle.Flex;
        _modeLabel.SetRawText(info.ItemDisplayName ?? info.ModeName);

        _shortcutRow.Clear();

        if (info.Shortcuts == null) return;

        for (int i = 0; i < info.Shortcuts.Count; i++)
        {
            var shortcut = info.Shortcuts[i];
            string keyLabel = ResolveKeyLabel(shortcut.InputActionName);
            string hintText = ForgeStyledVisualElement.GetLocalizedText(shortcut.HintLocalizationKey);

            var badge = CreateShortcutBadge(keyLabel, hintText);
            _shortcutRow.Add(badge);
        }
    }

    private string ResolveKeyLabel(string actionName)
    {
        if (_inputActions == null) return actionName;

        var action = _inputActions.FindAction(actionName);
        if (action == null || action.bindings.Count == 0) return actionName;

        var binding = action.bindings[0];
        string displayString = InputControlPath.ToHumanReadableString(
            binding.effectivePath,
            InputControlPath.HumanReadableStringOptions.OmitDevice);

        return string.IsNullOrEmpty(displayString) ? actionName : displayString;
    }

    private static ForgeContainer CreateShortcutBadge(string keyLabel, string hintText)
    {
        var keyText = ForgeLabel.CreateRaw(keyLabel, ForgeLabelSize.Small)
            .Color("accent.primary").Bold().Build();

        var keyBadge = ForgeContainer.Create()
            .BorderRadius(3)
            .PaddingTop(1).PaddingBottom(1)
            .PaddingLeft(5).PaddingRight(5)
            .MarginRight(4)
            .Child(keyText)
            .Build();
        keyBadge.style.backgroundColor = new Color(0.25f, 0.25f, 0.35f, 0.9f);

        var hint = ForgeLabel.CreateRaw(hintText, ForgeLabelSize.Small)
            .Color("text.secondary").Build();

        return ForgeContainer.Create()
            .FlexDirection(FlexDirection.Row)
            .AlignItems(Align.Center)
            .MarginRight(10)
            .Child(keyBadge)
            .Child(hint)
            .Build();
    }

    public void Show(object? context = null)
    {
        _isVisible = true;
        // Don't force display — OnToolModeChanged controls actual visibility
    }

    public void Hide()
    {
        _isVisible = false;
        _root.style.display = DisplayStyle.None;
    }

    public void Refresh() { }
    public void ApplyTheme() { }
    public void Dispose() { }
}

}
