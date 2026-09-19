using ForgeFlow.Core.Entities;
using ForgeFlow.Core.Localization;
using ForgeFlow.Core.Proto;
using ForgeFlow.Core.Proto.Logic;
using ForgeFlow.Core.Systems;
using ForgeFlow.Presentation.Unity.UI.Components;
using UnityEngine;
using UnityEngine.UIElements;

namespace ForgeFlow.Presentation.Unity.UI.Inspectors
{
    /// <summary>
    /// Inspector panel for TrainingBuilding structures. Shows output class,
    /// training duration, trainee capacity bar, and full-capacity warning.
    ///
    /// Uses the observer pattern: UI is built once, dynamic data bound
    /// via <see cref="BaseInspectorPanel.Observe{T}"/>.
    /// </summary>
    internal sealed class TrainingInspectorPanel : BaseActivityStructureInspector, ITickableWindow, IAutoRegisteredInspector
    {
        public override string WindowId => WindowIds.TrainingInspector;

        public TrainingInspectorPanel(SimulationTicker? simulation)
            : base("training_inspector", LocalizationKeys.InspectorTitle, 320f, 350f, simulation)
        {
        }

        public void Tick(float deltaTime)
        {
            UpdateBindings();
        }

        protected override void BuildStructureSpecificContent(VisualElement content, StructureBase entity)
        {
            if (entity is not TrainingBuildingLogic training)
            {
                return;
            }

            AddSectionDivider(content);
            AddRichSectionHeader(content, "▸ " + L(LocalizationKeys.InspectorSectionTraining));

            // Static properties
            AddRichPropertyRow(content, LocalizationKeys.InspectorOutputClass,
                training.OutputClass.ToString());
            AddRichPropertyRow(content, LocalizationKeys.InspectorTrainingDuration,
                $"{training.TrainingDuration:F0}s");

            // Trainee count (dynamic)
            var traineeKeyLabel = ForgeLabel.CreateRaw(
                L(LocalizationKeys.InspectorTrainees), ForgeLabelSize.Normal)
                .Color("text.secondary").FlexGrow(1).Build();
            var traineeValueLabel = ForgeLabel.CreateRaw(
                $"{training.TraineeCount} / {training.MaxTrainees}", ForgeLabelSize.Normal)
                .Color("text.primary").Bold().TextAlign(TextAnchor.MiddleRight).Build();
            content.Add(ForgeContainer.Create()
                .FlexDirection(FlexDirection.Row)
                .JustifyContent(Justify.SpaceBetween)
                .PaddingTop(3).PaddingBottom(3)
                .PaddingLeft(ThemePaddingSmall).PaddingRight(ThemePaddingSmall)
                .BackgroundColor(NextRowColorKey())
                .BorderRadius(AlternatingRowRadius)
                .Child(traineeKeyLabel).Child(traineeValueLabel)
                .Build());

            // Trainee capacity bar (dynamic)
            var traineeBar = ForgeProgressBar.Create(LocalizationKeys.InspectorTrainees,
                0f, training.MaxTrainees)
                .Value(training.TraineeCount)
                .MarginTop(4)
                .Build();
            content.Add(traineeBar);

            // Full warning (dynamic)
            var fullWarning = ForgeLabel.CreateRaw("⚠ Full — no slots", ForgeLabelSize.Small)
                .Color("status.error").Bold().MarginTop(2).Build();
            fullWarning.style.display = training.CanAcceptTrainee ? DisplayStyle.None : DisplayStyle.Flex;
            content.Add(fullWarning);

            Observe(() => training.TraineeCount, count =>
            {
                traineeValueLabel.Text = $"{count} / {training.MaxTrainees}";
                traineeBar.Value = count;
            });

            Observe(() => training.CanAcceptTrainee, canAccept =>
            {
                fullWarning.style.display = canAccept ? DisplayStyle.None : DisplayStyle.Flex;
            });
        }
    }
}
