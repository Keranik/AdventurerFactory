using ForgeFlow.Core.Localization;
using ForgeFlow.Core.Systems;
using ForgeFlow.Presentation.Unity.UI.Components;
using UnityEngine;
using UnityEngine.UIElements;

namespace ForgeFlow.Presentation.Unity.UI
{
    /// <summary>
    /// Game statistics panel. Shows aggregate stats from GameStatistics.
    /// Full-screen centered overlay.
    /// </summary>
    internal sealed class GameStatisticsPanel : IUIWindow
    {
        public string WindowId => WindowIds.Statistics;
        public event Action? OnBack;

        private readonly ForgeFullScreenOverlay _overlay;
        private readonly ForgeContainer _contentContainer;
        private readonly GameStatistics? _stats;
        private bool _isVisible;

        public VisualElement Root => _overlay;
        public bool IsVisible => _isVisible;

        public GameStatisticsPanel(GameStatistics? stats)
        {
            _stats = stats;
            _overlay = new ForgeFullScreenOverlay(new Color(0f, 0f, 0f, 0.6f));
            _contentContainer = new ForgeContainer();

            _overlay.ContentArea.Add(ForgeLabel.Create(LocalizationKeys.StatsTitle, ForgeLabelSize.Header)
                .FontSize(26).MarginBottom(12).Build());
            _overlay.ContentArea.Add(_contentContainer);
            ApplyTheme();
        }

        public void Show(object? context = null) { _isVisible = true; _overlay.Show(); }
        public void Hide() { _isVisible = false; _overlay.Hide(); }
        public void Dispose() { }

        public void ApplyTheme()
        {
            _overlay.ApplyTheme();
        }

        public void Refresh()
        {
            _contentContainer.Clear();
            if (_stats == null) { return; }

            _contentContainer.Add(CreateRow(LocalizationKeys.StatsHeroesSpawned, _stats.TotalHeroesSpawned.ToString()));
            _contentContainer.Add(CreateRow(LocalizationKeys.StatsHeroesDied, _stats.TotalHeroesDied.ToString()));
            _contentContainer.Add(CreateRow(LocalizationKeys.StatsDungeonsCleared,
                $"{_stats.TotalDungeonsCleared}/{_stats.TotalDungeonsAttempted}"));
            _contentContainer.Add(CreateRow(LocalizationKeys.StatsFusions, _stats.TotalFusions.ToString()));
            _contentContainer.Add(CreateRow(LocalizationKeys.StatsVillagersSpawned, _stats.TotalVillagersSpawned.ToString()));
            _contentContainer.Add(CreateRow(LocalizationKeys.StatsVillagersTrained, _stats.TotalVillagersTrained.ToString()));
            _contentContainer.Add(CreateRow(LocalizationKeys.StatsResourcesProduced, _stats.TotalResourcesProduced.ToString()));
            _contentContainer.Add(CreateRow(LocalizationKeys.StatsPrestigeResets, _stats.TotalPrestigeResets.ToString()));

            int hours = (int)(_stats.PlayTimeSeconds / 3600);
            int minutes = (int)((_stats.PlayTimeSeconds % 3600) / 60);
            _contentContainer.Add(CreateRow(LocalizationKeys.StatsPlayTime, $"{hours}h {minutes}m"));

            if (_stats.ResourcesProducedByType.Count > 0)
            {
                var resFoldBuilder = ForgeFoldout.Create(LocalizationKeys.StatsResourcesProduced, false);
                var resContent = new ForgeContainer();
                foreach (var kvp in _stats.ResourcesProducedByType)
                {
                    resContent.Add(CreateRow(kvp.Key, kvp.Value.ToString()));
                }
                _contentContainer.Add(resFoldBuilder.Content(resContent).Build());
            }

            _contentContainer.Add(ForgeButton.Create(LocalizationKeys.SettingsBack, () => OnBack?.Invoke())
                .Height(36).MarginTop(6).Build());
        }

        private static ForgeLabel CreateRow(string locKey, string value)
        {
            return ForgeLabel.CreateRaw(
                $"{ForgeStyledVisualElement.GetLocalizedText(locKey)}: {value}",
                ForgeLabelSize.Small).MarginBottom(2).Build();
        }
    }
}
