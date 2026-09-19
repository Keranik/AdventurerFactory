using ForgeFlow.Core.Localization;
using ForgeFlow.Core.Systems;
using ForgeFlow.Presentation.Unity.UI.Components;
using UnityEngine;
using UnityEngine.UIElements;

namespace ForgeFlow.Presentation.Unity.UI
{
    /// <summary>
    /// Achievements panel. Full-screen overlay with scrollable achievement list,
    /// progress bars, and hidden/unlocked states.
    /// </summary>
    internal sealed class AchievementsPanel : IUIWindow
    {
        public string WindowId => WindowIds.Achievements;
        public event Action? OnBack;

        private readonly ForgeFullScreenOverlay _overlay;
        private readonly ForgeContainer _contentContainer;
        private readonly AchievementSystem? _achievements;
        private bool _isVisible;

        public VisualElement Root => _overlay;
        public bool IsVisible => _isVisible;

        public AchievementsPanel(AchievementSystem? achievements)
        {
            _achievements = achievements;
            _overlay = new ForgeFullScreenOverlay(new Color(0f, 0f, 0f, 0.6f));
            _contentContainer = new ForgeContainer();

            _overlay.ContentArea.Add(ForgeLabel.Create(LocalizationKeys.AchievementsTitle, ForgeLabelSize.Header)
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
            if (_achievements == null) { return; }

            var scrollView = ForgeScrollView.Create(ScrollViewMode.Vertical)
                .MaxHeight(600).Width(500).Build();
            ApplyScrollCardTheme(scrollView);

            var inner = scrollView.Inner;

            inner.Add(ForgeLabel.CreateRaw(
                $"{ForgeStyledVisualElement.GetLocalizedText(LocalizationKeys.AchievementsUnlocked)}: " +
                $"{_achievements.UnlockedCount}/{_achievements.TotalCount}",
                ForgeLabelSize.Normal).MarginBottom(8).Build());

            foreach (var kvp in _achievements.Definitions)
            {
                var def = kvp.Value;
                var progress = _achievements.GetProgress(def.Id);
                if (progress == null) { continue; }

                if (def.IsHidden && !progress.IsUnlocked)
                {
                    inner.Add(ForgeLabel.Create(LocalizationKeys.AchievementsHidden, ForgeLabelSize.Normal)
                        .MarginBottom(6).Build());
                    continue;
                }

                var container = ForgeContainer.Create()
                    .MarginBottom(8).PaddingLeft(6).PaddingTop(4).PaddingBottom(4)
                    .Build();
                container.style.borderLeftWidth = 2;
                container.style.borderLeftColor = progress.IsUnlocked
                    ? ForgeStyledVisualElement.GetThemeColor("status.success")
                    : ForgeStyledVisualElement.GetThemeColor("border.normal");

                container.Add(ForgeLabel.CreateRaw(def.DisplayName, ForgeLabelSize.Normal)
                    .Color(progress.IsUnlocked ? "status.success" : "text.primary").Build());

                container.Add(ForgeLabel.CreateRaw(def.Description, ForgeLabelSize.Small)
                    .Color("text.secondary").Build());

                if (!progress.IsUnlocked && progress.TargetValue > 1)
                {
                    container.Add(ForgeProgressBar.Create(
                        $"{progress.CurrentValue}/{progress.TargetValue}", 0f, 100f)
                        .Value(progress.Progress * 100f).Build());
                }

                inner.Add(container);
            }

            inner.Add(ForgeButton.Create(LocalizationKeys.SettingsBack, () => OnBack?.Invoke())
                .Primary().Height(38).MarginTop(8).Build());

            _contentContainer.Add(scrollView);
        }

        private static void ApplyScrollCardTheme(ForgeScrollView sv)
        {
            sv.style.backgroundColor = ForgeStyledVisualElement.GetThemeColor("bg.panel");
            sv.style.paddingTop = 16;
            sv.style.paddingBottom = 16;
            sv.style.paddingLeft = 20;
            sv.style.paddingRight = 20;
            ForgeStyledVisualElement.SetBorderOn(sv, ForgeStyledVisualElement.GetThemeColor("border.normal"), 1, 8);
            sv.style.borderTopWidth = 2;
            sv.style.borderTopColor = ForgeStyledVisualElement.GetThemeColor("accent.primary");
        }
    }
}
