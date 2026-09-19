using System;
using ForgeFlow.Core.Localization;
using ForgeFlow.Presentation.Unity.UI.Components;
using UnityEngine;
using UnityEngine.UIElements;

namespace ForgeFlow.Presentation.Unity.UI
{
    /// <summary>
    /// Pause menu overlay. Full-screen semi-transparent centered card with
    /// resume/save/load/settings/main menu.
    /// </summary>
    internal sealed class PauseMenuPanel : IUIWindow
    {
        public string WindowId => WindowIds.PauseMenu;
        public event Action? OnResume;
        public event Action? OnSaveGame;
        public event Action? OnLoadGame;
        public event Action? OnOpenSettings;
        public event Action? OnMainMenu;

        private readonly ForgeFullScreenOverlay _overlay;
        private readonly ForgeContainer _card;
        private bool _isVisible;

        public VisualElement Root => _overlay;
        public bool IsVisible => _isVisible;

        public PauseMenuPanel()
        {
            _overlay = new ForgeFullScreenOverlay(new Color(0f, 0f, 0f, 0.6f));
            _card = new ForgeContainer();
            _overlay.ContentArea.Add(_card);
            BuildUI();
            ApplyTheme();
        }

        public void Show(object? context = null) { _isVisible = true; _overlay.Show(); }
        public void Hide() { _isVisible = false; _overlay.Hide(); }
        public void Refresh() { }
        public void Dispose() { }

        public void ApplyTheme()
        {
            _overlay.ApplyTheme();
            ApplyCardTheme();
        }

        private void BuildUI()
        {
            _card.Add(ForgeLabel.Create(LocalizationKeys.PauseTitle, ForgeLabelSize.Header)
                .FontSize(30).Color("status.warning").MarginBottom(24)
                .TextAlign(TextAnchor.MiddleCenter).Build());

            // Resume — primary action
            _card.Add(ForgeButton.Create(LocalizationKeys.PauseResume, () => OnResume?.Invoke())
                .Primary().Height(40).Width(280).MarginBottom(8).BorderWidth(2).Build());

            // Save/Load row
            var saveLoadRow = ForgeContainer.Create()
                .FlexDirection(FlexDirection.Row)
                .JustifyContent(Justify.Center)
                .MarginBottom(8)
                .Build();
            saveLoadRow.Add(ForgeButton.Create(LocalizationKeys.PauseQuickSave, () => OnSaveGame?.Invoke())
                .Height(38).Width(134).MarginRight(6).BorderWidth(1).Build());
            saveLoadRow.Add(ForgeButton.Create(LocalizationKeys.PauseQuickLoad, () => OnLoadGame?.Invoke())
                .Secondary().Height(38).Width(134).BorderWidth(1).Build());
            _card.Add(saveLoadRow);

            _card.Add(ForgeButton.Create(LocalizationKeys.PauseSettings, () => OnOpenSettings?.Invoke())
                .Height(38).Width(280).MarginBottom(8).BorderWidth(2).Build());

            // Separator
            var sep = ForgeContainer.Create()
                .Height(1).Width(180)
                .BackgroundColor("border.normal")
                .MarginTop(8).MarginBottom(8)
                .Build();
            _card.Add(sep);

            _card.Add(ForgeButton.Create(LocalizationKeys.PauseMainMenu, () => OnMainMenu?.Invoke())
                .Danger().Height(38).Width(280).BorderWidth(2).Build());
        }

        private void ApplyCardTheme()
        {
            _card.style.width = 360;
            _card.style.backgroundColor = ForgeStyledVisualElement.GetThemeColor("bg.panel");
            _card.style.paddingTop = 28;
            _card.style.paddingBottom = 28;
            _card.style.paddingLeft = 36;
            _card.style.paddingRight = 36;
            _card.style.alignItems = Align.Center;
            ForgeStyledVisualElement.SetBorderOn(_card, ForgeStyledVisualElement.GetThemeColor("border.normal"), 1, 8);
            _card.style.borderTopWidth = 3;
            _card.style.borderTopColor = ForgeStyledVisualElement.GetThemeColor("status.warning");
        }
    }
}
