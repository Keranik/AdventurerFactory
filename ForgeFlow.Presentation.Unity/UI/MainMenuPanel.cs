using ForgeFlow.Core.Localization;
using ForgeFlow.Presentation.Unity.UI.Components;
using UnityEngine;
using UnityEngine.UIElements;

namespace ForgeFlow.Presentation.Unity.UI
{
    /// <summary>
    /// Main menu panel. Full-screen overlay with centered title card.
    /// Buttons: New Game, Continue, Settings, Achievements, Statistics, Mod Browser, Quit.
    /// Composes <see cref="ForgeFullScreenOverlay"/> for the overlay root.
    /// </summary>
    internal sealed class MainMenuPanel : IUIWindow
    {
        public string WindowId => WindowIds.MainMenu;
        public event Action? OnNewGame;
        public event Action? OnContinue;
        public event Action? OnOpenSettings;
        public event Action? OnOpenAchievements;
        public event Action? OnOpenStatistics;
        public event Action? OnOpenModBrowser;
        public event Action? OnQuit;

        private readonly ForgeFullScreenOverlay _overlay;
        private readonly ForgeContainer _card;
        private bool _isVisible;

        public VisualElement Root => _overlay;
        public bool IsVisible => _isVisible;

        public MainMenuPanel()
        {
            _overlay = new ForgeFullScreenOverlay("bg.primary");
            _card = new ForgeContainer();
            _card.name = "MainMenuCard";
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
            // Title
            _card.Add(ForgeLabel.Create(LocalizationKeys.MainMenuTitle, ForgeLabelSize.Header)
                .FontSize(38).MarginBottom(4)
                .TextAlign(TextAnchor.MiddleCenter).Build());

            // Subtitle
            _card.Add(ForgeLabel.Create(LocalizationKeys.GameSubtitle, ForgeLabelSize.Normal)
                .Color("text.secondary").MarginBottom(32)
                .TextAlign(TextAnchor.MiddleCenter).Build());

            // Primary action — New Game
            _card.Add(ForgeButton.Create(LocalizationKeys.MainMenuNewGame, () => OnNewGame?.Invoke())
                .Name(LocalizationKeys.MainMenuNewGame).Primary()
                .Height(44).Width(280).MarginBottom(8).BorderWidth(2).Build());
            _card.Add(CreateMenuButton(LocalizationKeys.MainMenuContinue, () => OnContinue?.Invoke()));

            // Separator
            var sep = ForgeContainer.Create()
                .Height(1).Width(200)
                .BackgroundColor("border.normal")
                .MarginTop(8).MarginBottom(8)
                .Build();
            _card.Add(sep);

            _card.Add(CreateMenuButton(LocalizationKeys.MainMenuSettings, () => OnOpenSettings?.Invoke()));
            _card.Add(CreateMenuButton(LocalizationKeys.MainMenuAchievements, () => OnOpenAchievements?.Invoke()));
            _card.Add(CreateMenuButton(LocalizationKeys.MainMenuStatistics, () => OnOpenStatistics?.Invoke()));
            _card.Add(CreateMenuButton(LocalizationKeys.MainMenuModBrowser, () => OnOpenModBrowser?.Invoke()));

            // Quit — danger variant
            _card.Add(ForgeButton.Create(LocalizationKeys.MainMenuQuit, () => OnQuit?.Invoke())
                .Name(LocalizationKeys.MainMenuQuit).Danger()
                .Height(40).Width(280).MarginBottom(8).MarginTop(16).BorderWidth(2).Build());

            // Version tag
            _card.Add(ForgeLabel.Create(LocalizationKeys.MainMenuVersion, ForgeLabelSize.Small)
                .Color("text.secondary").MarginTop(8)
                .TextAlign(TextAnchor.MiddleCenter).Build());
        }

        private void ApplyCardTheme()
        {
            _card.style.width = 420;
            _card.style.backgroundColor = ForgeStyledVisualElement.GetThemeColor("bg.panel");
            _card.style.paddingTop = 40;
            _card.style.paddingBottom = 32;
            _card.style.paddingLeft = 44;
            _card.style.paddingRight = 44;
            _card.style.alignItems = Align.Center;
            _card.style.borderTopWidth = 3;
            _card.style.borderBottomWidth = 1;
            _card.style.borderLeftWidth = 1;
            _card.style.borderRightWidth = 1;
            _card.style.borderTopColor = ForgeStyledVisualElement.GetThemeColor("accent.primary");
            _card.style.borderBottomColor = ForgeStyledVisualElement.GetThemeColor("border.normal");
            _card.style.borderLeftColor = ForgeStyledVisualElement.GetThemeColor("border.normal");
            _card.style.borderRightColor = ForgeStyledVisualElement.GetThemeColor("border.normal");
            ForgeStyledVisualElement.SetBorderOn(_card, ForgeStyledVisualElement.GetThemeColor("border.normal"), 1, 12);
            _card.style.borderTopWidth = 3;
            _card.style.borderTopColor = ForgeStyledVisualElement.GetThemeColor("accent.primary");
        }

        private static ForgeButton CreateMenuButton(string locKey, Action onClick)
        {
            return ForgeButton.Create(locKey, onClick)
                .Name(locKey).Height(40).Width(280).MarginBottom(8).BorderWidth(2).Build();
        }
    }
}
