using ForgeFlow.Core.Localization;
using ForgeFlow.Presentation.Unity.UI.Components;
using UnityEngine;
using UnityEngine.UIElements;

namespace ForgeFlow.Presentation.Unity.UI
{
    /// <summary>
    /// Game over screen. Full-screen centered overlay with failure message,
    /// reason, and "Try Again" / "Return to Menu" buttons.
    /// </summary>
    internal sealed class GameOverPanel : IUIWindow
    {
        public string WindowId => WindowIds.GameOver;
        public event Action? OnReturnToMenu;
        public event Action? OnTryAgain;

        private readonly ForgeFullScreenOverlay _overlay;
        private readonly ForgeContainer _card;
        private readonly ForgeLabel _reasonLabel;
        private bool _isVisible;

        public VisualElement Root => _overlay;
        public bool IsVisible => _isVisible;

        public GameOverPanel()
        {
            _overlay = new ForgeFullScreenOverlay(new Color(0f, 0f, 0f, 0.7f));
            _card = new ForgeContainer();

            _reasonLabel = ForgeLabel.CreateRaw("", ForgeLabelSize.Normal)
                .Color("status.error")
                .MarginBottom(16)
                .WhiteSpace(WhiteSpace.Normal).Build();

            _card.Add(ForgeLabel.Create(LocalizationKeys.GameOverTitle, ForgeLabelSize.Header)
                .FontSize(32).Color("status.error").MarginBottom(8).Build());
            _card.Add(ForgeLabel.Create(LocalizationKeys.GameOverMessage, ForgeLabelSize.Normal)
                .MarginBottom(12).Build());
            _card.Add(_reasonLabel);
            _card.Add(ForgeButton.Create(LocalizationKeys.GameOverTryAgain, () => OnTryAgain?.Invoke())
                .Primary().Height(40).Width(280).MarginBottom(8).BorderWidth(2).Build());
            _card.Add(ForgeButton.Create(LocalizationKeys.GameOverRestart, () => OnReturnToMenu?.Invoke())
                .Secondary().Height(40).Width(280).BorderWidth(2).Build());

            _overlay.ContentArea.Add(_card);
            ApplyTheme();
        }

        public void Show(object? context = null) { _isVisible = true; _overlay.Show(); }
        public void Hide() { _isVisible = false; _overlay.Hide(); }
        public void Refresh() { }
        public void Dispose() { }

        public void SetReason(string reason)
        {
            _reasonLabel.SetRawText(reason);
        }

        public void ApplyTheme()
        {
            _overlay.ApplyTheme();
            ApplyCardTheme();
        }

        private void ApplyCardTheme()
        {
            _card.style.width = 420;
            _card.style.backgroundColor = ForgeStyledVisualElement.GetThemeColor("bg.panel");
            _card.style.paddingTop = 28;
            _card.style.paddingBottom = 28;
            _card.style.paddingLeft = 36;
            _card.style.paddingRight = 36;
            _card.style.alignItems = Align.Center;
            ForgeStyledVisualElement.SetBorderOn(_card, ForgeStyledVisualElement.GetThemeColor("border.normal"), 1, 10);
            _card.style.borderTopWidth = 3;
            _card.style.borderTopColor = ForgeStyledVisualElement.GetThemeColor("status.error");
        }
    }
}
