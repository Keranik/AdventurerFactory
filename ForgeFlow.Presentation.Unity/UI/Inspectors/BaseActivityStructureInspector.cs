using ForgeFlow.Core.Entities;
using ForgeFlow.Core.Localization;
using ForgeFlow.Core.Systems;
using ForgeFlow.Presentation.Unity.UI.Components;
using UnityEngine;
using UnityEngine.UIElements;

namespace ForgeFlow.Presentation.Unity.UI.Inspectors
{
    /// <summary>
    /// Intermediate base for inspectors of activity structures — structures that accept
    /// entities (villagers/workers) inside, perform a time-based activity, and release them.
    /// Inn, Spawner, TrainingBuilding, Academy, ToolStation, Armory, JobChanger,
    /// DungeonPortal, and AppearanceWorkshop inspectors inherit from this.
    /// Mirrors <see cref="ActivityEntity"/> in the Logic layer.
    ///
    /// Provides a shared helper for building occupancy sections (count/max row + progress bar + full warning).
    /// </summary>
    internal abstract class BaseActivityStructureInspector : BaseStructureInspector
    {
        protected BaseActivityStructureInspector(
            string panelId,
            string titleLocKey,
            float width,
            float height,
            SimulationTicker? simulation)
            : base(panelId, titleLocKey, width, height, simulation)
        {
        }

        /// <summary>
        /// Tries to retrieve the inspected structure as an <see cref="ActivityEntity"/>.
        /// Returns false if the structure is not an activity structure.
        /// </summary>
        protected bool TryGetActivityStructure(out ActivityEntity activity)
        {
            activity = null!;
            if (TryGetStructure(out var entity) && entity is ActivityEntity a)
            {
                activity = a;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Builds a standard occupancy section: label row (count/max), progress bar, and full warning.
        /// The returned labels and bar can be bound to observers for live updates.
        /// </summary>
        protected OccupancyControls AddOccupancySection(
            VisualElement content,
            string occupantsLocKey,
            int currentCount,
            int maxCount,
            bool canAccept)
        {
            var occKeyLabel = ForgeLabel.CreateRaw(
                L(occupantsLocKey), ForgeLabelSize.Normal)
                .Color("text.secondary").FlexGrow(1).Build();
            var occValueLabel = ForgeLabel.CreateRaw(
                $"{currentCount} / {maxCount}", ForgeLabelSize.Normal)
                .Color("text.primary").Bold().TextAlign(TextAnchor.MiddleRight).Build();
            content.Add(ForgeContainer.Create()
                .FlexDirection(FlexDirection.Row)
                .JustifyContent(Justify.SpaceBetween)
                .PaddingTop(3).PaddingBottom(3)
                .PaddingLeft(ThemePaddingSmall).PaddingRight(ThemePaddingSmall)
                .BackgroundColor(NextRowColorKey())
                .BorderRadius(AlternatingRowRadius)
                .Child(occKeyLabel).Child(occValueLabel)
                .Build());

            var occupancyBar = ForgeProgressBar.Create(occupantsLocKey,
                0f, maxCount)
                .Value(currentCount)
                .MarginTop(4)
                .Build();
            content.Add(occupancyBar);

            var fullWarning = ForgeLabel.CreateRaw("⚠ Full — no vacancies", ForgeLabelSize.Small)
                .Color("status.error").Bold().MarginTop(2).Build();
            fullWarning.style.display = canAccept ? DisplayStyle.None : DisplayStyle.Flex;
            content.Add(fullWarning);

            return new OccupancyControls(occValueLabel, occupancyBar, fullWarning);
        }

        /// <summary>Groups the UI controls created by <see cref="AddOccupancySection"/> for binding.</summary>
        protected readonly struct OccupancyControls
        {
            public readonly ForgeLabel ValueLabel;
            public readonly ForgeProgressBar Bar;
            public readonly ForgeLabel FullWarning;

            public OccupancyControls(ForgeLabel valueLabel, ForgeProgressBar bar, ForgeLabel fullWarning)
            {
                ValueLabel = valueLabel;
                Bar = bar;
                FullWarning = fullWarning;
            }
        }
    }
}
