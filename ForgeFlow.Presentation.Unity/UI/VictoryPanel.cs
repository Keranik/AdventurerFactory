using ForgeFlow.Core.Localization;
using ForgeFlow.Core.Systems;
using ForgeFlow.Presentation.Unity.UI.Components;
using UnityEngine;
using UnityEngine.UIElements;

namespace ForgeFlow.Presentation.Unity.UI
{
    /// <summary>
    /// Victory screen. Full-screen centered overlay with congratulations,
    /// journey stats, and themed buttons.
    /// </summary>
    internal sealed class VictoryPanel : IUIWindow
    {
        public string WindowId => WindowIds.Victory;
        public event Action? OnContinuePlaying;
        public event Action? OnReturnToMenu;

        private readonly ForgeFullScreenOverlay _overlay;
        private readonly ForgeContainer _card;
        private readonly ForgeContainer _statsContainer;
        private bool _isVisible;

        public VisualElement Root => _overlay;
        public bool IsVisible => _isVisible;

        public VictoryPanel()
        {
            _overlay = new ForgeFullScreenOverlay(new Color(0f, 0f, 0f, 0.7f));
            _card = new ForgeContainer();
            _statsContainer = ForgeContainer.Create()
                .MarginTop(8).MarginBottom(16).Build();

            _card.Add(ForgeLabel.Create(LocalizationKeys.VictoryCongrats, ForgeLabelSize.Normal)
                .Color("status.success").MarginBottom(4).Build());
            _card.Add(ForgeLabel.Create(LocalizationKeys.VictoryTitle, ForgeLabelSize.Header)
                .FontSize(36).Color("status.warning").MarginBottom(8).Build());
            _card.Add(ForgeLabel.Create(LocalizationKeys.VictoryMessage, ForgeLabelSize.Normal)
                .MarginBottom(8).Build());
            _card.Add(_statsContainer);
            _card.Add(ForgeButton.Create(LocalizationKeys.VictoryContinue, () => OnContinuePlaying?.Invoke())
                .Primary().Height(40).Width(300).MarginBottom(8).BorderWidth(2).Build());
            _card.Add(ForgeButton.Create(LocalizationKeys.VictoryMenu, () => OnReturnToMenu?.Invoke())
                .Secondary().Height(40).Width(300).BorderWidth(2).Build());

            _overlay.ContentArea.Add(_card);
            ApplyTheme();
        }

        public void Show(object? context = null) { _isVisible = true; _overlay.Show(); }
        public void Hide() { _isVisible = false; _overlay.Hide(); }
        public void Refresh() { }
        public void Dispose() { }

        public void SetStats(GameStatistics? stats)
        {
            _statsContainer.Clear();
            if (stats == null) { return; }

            _statsContainer.Add(ForgeLabel.Create(LocalizationKeys.VictoryStats, ForgeLabelSize.Header)
                .FontSize(18).MarginBottom(6).Build());

            _statsContainer.Add(CreateRow(LocalizationKeys.StatsHeroesSpawned, stats.TotalHeroesSpawned.ToString()));
            _statsContainer.Add(CreateRow(LocalizationKeys.StatsDungeonsCleared,
                $"{stats.TotalDungeonsCleared}/{stats.TotalDungeonsAttempted}"));
            _statsContainer.Add(CreateRow(LocalizationKeys.StatsPrestigeResets, stats.TotalPrestigeResets.ToString()));

            int hours = (int)(stats.PlayTimeSeconds / 3600);
            int minutes = (int)((stats.PlayTimeSeconds % 3600) / 60);
            _statsContainer.Add(CreateRow(LocalizationKeys.StatsPlayTime, $"{hours}h {minutes}m"));
        }

        public void ApplyTheme()
        {
            _overlay.ApplyTheme();
            ApplyCardTheme();
        }

        private void ApplyCardTheme()
        {
            _card.style.width = 440;
            _card.style.backgroundColor = ForgeStyledVisualElement.GetThemeColor("bg.panel");
            _card.style.paddingTop = 28;
            _card.style.paddingBottom = 28;
            _card.style.paddingLeft = 36;
            _card.style.paddingRight = 36;
            _card.style.alignItems = Align.Center;
            ForgeStyledVisualElement.SetBorderOn(_card, ForgeStyledVisualElement.GetThemeColor("border.normal"), 1, 10);
            _card.style.borderTopWidth = 3;
            _card.style.borderTopColor = ForgeStyledVisualElement.GetThemeColor("status.warning");
        }

        private static ForgeLabel CreateRow(string locKey, string value)
        {
            return ForgeLabel.CreateRaw(
                $"{ForgeStyledVisualElement.GetLocalizedText(locKey)}: {value}",
                ForgeLabelSize.Small)
                .MarginBottom(2).Build();
        }
    }
}
