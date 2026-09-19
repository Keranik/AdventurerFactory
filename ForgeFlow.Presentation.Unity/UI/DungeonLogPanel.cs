using ForgeFlow.Core.Localization;
using ForgeFlow.Core.Systems;
using ForgeFlow.Presentation.Unity.UI.Components;
using UnityEngine.UIElements;

namespace ForgeFlow.Presentation.Unity.UI
{
    /// <summary>
    /// Dungeon run log panel. Shows step-by-step encounter results from the last dungeon run.
    /// Composes <see cref="ForgePanel"/> for a draggable card.
    /// </summary>
    internal sealed class DungeonLogPanel : IUIWindow
    {
        public string WindowId => WindowIds.DungeonLog;

        private readonly ForgePanel _panel;
        private readonly ForgeScrollView _scrollView;
        private readonly SimulationTicker? _simulation;
        private bool _isVisible;

        public VisualElement Root => _panel;
        public bool IsVisible => _isVisible;

        public DungeonLogPanel(SimulationTicker? simulation)
        {
            _simulation = simulation;
            _panel = new ForgePanel("dungeon_log", LocalizationKeys.DungeonTitle, 320f, 400f);
            _panel.style.right = 300;
            _panel.style.top = 10;
            _panel.style.left = StyleKeyword.Auto;
            _panel.Closed += Hide;

            _scrollView = ForgeScrollView.Create(ScrollViewMode.Vertical).Build();
            _panel.ContentContainer.Add(_scrollView);
        }

        public void Show(object? context = null)
        {
            _isVisible = true;
            _panel.Show();
        }

        public void Hide()
        {
            _isVisible = false;
            _panel.Hide();
        }

        public void Refresh()
        {
            _scrollView.ClearContent();
            if (_simulation == null) { return; }

            var log = _simulation.DungeonResolver.LastRunLog;
            if (log.Steps.Count == 0) { return; }

            var resultText = log.OverallSuccess
                ? ForgeStyledVisualElement.GetLocalizedText(LocalizationKeys.DungeonSuccess)
                : ForgeStyledVisualElement.GetLocalizedText(LocalizationKeys.DungeonFailed2);
            _scrollView.AddContent(ForgeLabel.CreateRaw(
                string.Format(ForgeStyledVisualElement.GetLocalizedText(LocalizationKeys.DungeonHeader), log.DungeonId, resultText),
                ForgeLabelSize.Normal)
                .Color(log.OverallSuccess ? "status.success" : "status.error").Build());
            _scrollView.AddContent(ForgeLabel.CreateRaw(
                string.Format(ForgeStyledVisualElement.GetLocalizedText(LocalizationKeys.DungeonRoomsCleared), log.RoomsCleared, log.TotalRooms), ForgeLabelSize.Small).Build());
            _scrollView.AddContent(ForgeLabel.CreateRaw(
                string.Format(ForgeStyledVisualElement.GetLocalizedText(LocalizationKeys.DungeonDamageSummary), log.TotalDamageDealt, log.TotalDamageTaken),
                ForgeLabelSize.Small).MarginBottom(4).Build());

            foreach (var step in log.Steps)
            {
                var stepText =
                    $"  Room {step.RoomIndex + 1} [{step.Type}]: " +
                    $"{(step.Survived ? ForgeStyledVisualElement.GetLocalizedText(LocalizationKeys.DungeonSurvived) : ForgeStyledVisualElement.GetLocalizedText(LocalizationKeys.DungeonDefeated))} " +
                    $"(DMG: {step.DamageDealt}/{step.DamageTaken})" +
                    $"{(step.LootDropId != null ? $" Loot: {step.LootDropId}" : "")}";

                _scrollView.AddContent(ForgeLabel.CreateRaw(stepText, ForgeLabelSize.Small)
                    .Color(step.Survived ? "status.success" : "status.error").Build());
            }
        }

        public void ApplyTheme()
        {
            _panel.ApplyTheme();
        }

        public void Dispose() { }
    }
}
