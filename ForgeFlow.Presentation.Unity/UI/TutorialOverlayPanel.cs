using ForgeFlow.Core.Events;
using ForgeFlow.Core.Localization;
using ForgeFlow.Core.Systems;
using ForgeFlow.Presentation.Unity.UI.Components;
using UnityEngine;
using UnityEngine.UIElements;

namespace ForgeFlow.Presentation.Unity.UI
{
    /// <summary>
    /// Tutorial overlay panel. Shows active mission hint, progress bar, gating info,
    /// glowing animated borders and pulsing arrows that highlight the target UI element
    /// or world position the player must interact with.
    /// Implements <see cref="ITickableWindow"/> for per-frame pulse animation.
    /// Composes <see cref="ForgePanel"/> for a draggable card.
    /// </summary>
    internal sealed class TutorialOverlayPanel : ITickableWindow
    {
        public string WindowId => WindowIds.TutorialOverlay;

        private readonly ForgePanel _panel;
        private readonly ForgeContainer _contentContainer;
        private readonly ForgeProgressBar _progressBar;
        private readonly ForgeContainer _highlightBorder;
        private readonly ForgeLabel _arrowLabel;
        private readonly ForgeLabel _hintLabel;
        private readonly SimulationTicker? _simulation;
        private readonly EventBus? _eventBus;
        private readonly Action<TutorialCompletedEvent> _onTutorialCompleted;
        private readonly Action<ResearchUnlockedEvent> _onResearchUnlocked;
        private string _currentHighlightTarget = string.Empty;
        private float _pulsePhase;
        private bool _isVisible;

        public string CurrentHighlightTarget => _currentHighlightTarget;
        public event Action? OnSkipTutorial;

        public VisualElement Root => _panel;
        public bool IsVisible => _isVisible;

        public TutorialOverlayPanel(SimulationTicker? simulation, EventBus? eventBus)
        {
            _simulation = simulation;
            _eventBus = eventBus;
            _onTutorialCompleted = _ => Refresh();
            _onResearchUnlocked = _ => Refresh();
            _panel = new ForgePanel("tutorial_overlay", LocalizationKeys.TutorialTitle, 320f, 300f);
            _panel.style.left = 10;
            _panel.style.bottom = 10;
            _panel.style.top = StyleKeyword.Auto;
            _panel.style.height = StyleKeyword.Auto;
            _panel.Closed += Hide;

            _contentContainer = new ForgeContainer();
            _progressBar = ForgeProgressBar.Create(LocalizationKeys.TutorialProgress, 0f, 100f).Build();

            _hintLabel = ForgeLabel.CreateRaw("", ForgeLabelSize.Normal)
                .Color("status.warning")
                .WhiteSpace(WhiteSpace.Normal)
                .MarginBottom(4).Build();

            _arrowLabel = ForgeLabel.CreateRaw("\u25b6", ForgeLabelSize.Large)
                .Color("status.success")
                .FontSize(24)
                .TextAlign(TextAnchor.MiddleCenter)
                .MarginBottom(4).Build();

            _highlightBorder = new ForgeContainer();
            _highlightBorder.name = "TutorialHighlightBorder";
            var successColor = ForgeStyledVisualElement.GetThemeColor("status.success");
            ForgeStyledVisualElement.SetBorderOn(_highlightBorder, successColor, 3, 6);
            _highlightBorder.SetPadding(6);
            _highlightBorder.style.marginBottom = 6;

            _highlightBorder.Add(_arrowLabel);
            _highlightBorder.Add(_hintLabel);

            _panel.ContentContainer.Add(_highlightBorder);
            _panel.ContentContainer.Add(_contentContainer);
            _panel.ContentContainer.Add(_progressBar);

            _panel.ContentContainer.Add(ForgeButton.Create(LocalizationKeys.TutorialSkip, () => OnSkipTutorial?.Invoke())
                .Height(36).MarginTop(6).Build());

            SubscribeEvents();
        }

        public void SetHighlightTarget(string target, string hintText)
        {
            _currentHighlightTarget = target;
            _hintLabel.SetRawText(ForgeStyledVisualElement.GetLocalizedText(hintText));
        }

        public void AnimatePulse(float deltaTime)
        {
            _pulsePhase += deltaTime * 3f;
            float alpha = 0.5f + 0.5f * (float)Math.Sin(_pulsePhase);
            var glowColor = new Color(
                ForgeStyledVisualElement.GetThemeColor("status.success").r,
                ForgeStyledVisualElement.GetThemeColor("status.success").g,
                ForgeStyledVisualElement.GetThemeColor("status.success").b,
                alpha);
            _highlightBorder.style.borderTopColor = glowColor;
            _highlightBorder.style.borderBottomColor = glowColor;
            _highlightBorder.style.borderLeftColor = glowColor;
            _highlightBorder.style.borderRightColor = glowColor;

            string arrow = ((int)(_pulsePhase * 2) % 4) switch
            {
                0 => "\u25b6",
                1 => "\u25bc",
                2 => "\u25c0",
                3 => "\u25b2",
                _ => "\u25b6"
            };
            _arrowLabel.SetRawText(arrow);
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
            _contentContainer.Clear();
            if (_simulation == null) { return; }

            var ts = _simulation.TutorialSystem;
            var active = ts.ActiveMission;
            if (active != null)
            {
                _highlightBorder.style.display = DisplayStyle.Flex;

                for (int i = 0; i < active.Conditions.Count; i++)
                {
                    var cond = active.Conditions[i];
                    bool isCurrent = i == ts.ActiveStepIndex && !cond.IsMet;

                    var stepBuilder = ForgeLabel.CreateRaw(
                        $"  {(cond.IsMet ? "\u2713" : "\u25cb")} {cond.Type}: {cond.CurrentCount}/{cond.RequiredCount}",
                        ForgeLabelSize.Small);

                    if (cond.IsMet)
                    {
                        stepBuilder.Color("status.success");
                    }
                    else if (isCurrent)
                    {
                        stepBuilder.Color("status.success").Bold();
                    }

                    _contentContainer.Add(stepBuilder.Build());
                }

                _progressBar.Value = ts.TotalCount > 0 ? (float)ts.CompletedCount / ts.TotalCount * 100f : 100f;
            }
            else
            {
                _highlightBorder.style.display = DisplayStyle.None;
                _contentContainer.Add(new ForgeLabel(LocalizationKeys.TutorialComplete, ForgeLabelSize.Normal));
                _progressBar.Value = 100f;
            }

            var gating = _simulation.Gating;
            _contentContainer.Add(CreateRow(LocalizationKeys.GatingSpawnerLimit,
                $"{_simulation.StructureManager.CountSpawners()}/{gating.GetMaxSpawners(_simulation.ResearchManager.CurrentTier)}"));
            _contentContainer.Add(CreateRow(LocalizationKeys.GatingBuildingLimit,
                $"{_simulation.EntityManager.Structures.Count}/{gating.GetMaxBuildings(_simulation.ResearchManager.CurrentTier)}"));
        }

        public void Tick(float deltaTime)
        {
            AnimatePulse(deltaTime);
        }

        public void ApplyTheme()
        {
            _panel.ApplyTheme();
        }

        public void Dispose()
        {
            UnsubscribeEvents();
        }

        private void SubscribeEvents()
        {
            if (_eventBus == null) { return; }
            _eventBus.Subscribe<TutorialCompletedEvent>(_onTutorialCompleted);
            _eventBus.Subscribe<ResearchUnlockedEvent>(_onResearchUnlocked);
        }

        private void UnsubscribeEvents()
        {
            if (_eventBus == null) { return; }
            _eventBus.Unsubscribe<TutorialCompletedEvent>(_onTutorialCompleted);
            _eventBus.Unsubscribe<ResearchUnlockedEvent>(_onResearchUnlocked);
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
