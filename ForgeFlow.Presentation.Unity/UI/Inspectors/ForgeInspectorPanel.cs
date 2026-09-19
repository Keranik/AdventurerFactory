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
    /// Inspector panel for Forge structures. Shows active recipe and processing progress bar.
    ///
    /// Uses the observer pattern: UI is built once, dynamic data bound
    /// via <see cref="BaseInspectorPanel.Observe{T}"/>.
    /// </summary>
    internal sealed class ForgeInspectorPanel : BaseRecipeStructureInspector, ITickableWindow, IAutoRegisteredInspector
    {
        public override string WindowId => WindowIds.ForgeInspector;

        public ForgeInspectorPanel(SimulationTicker? simulation)
            : base("forge_inspector", LocalizationKeys.InspectorTitle, 320f, 350f, simulation)
        {
        }

        public void Tick(float deltaTime)
        {
            UpdateBindings();
        }

        protected override void BuildStructureSpecificContent(VisualElement content, StructureBase entity)
        {
            if (entity is not ForgeLogic forge)
            {
                return;
            }

            AddSectionDivider(content);
            AddRichSectionHeader(content, "▸ " + L(LocalizationKeys.InspectorSectionProduction));

            // Recipe row (dynamic)
            var recipeKeyLabel = ForgeLabel.CreateRaw(
                L(LocalizationKeys.InspectorCraftRecipe), ForgeLabelSize.Normal)
                .Color("text.secondary").FlexGrow(1).Build();
            var recipeValueLabel = ForgeLabel.CreateRaw("", ForgeLabelSize.Normal)
                .Bold().TextAlign(TextAnchor.MiddleRight).Build();
            content.Add(ForgeContainer.Create()
                .FlexDirection(FlexDirection.Row)
                .JustifyContent(Justify.SpaceBetween)
                .PaddingTop(3).PaddingBottom(3)
                .PaddingLeft(ThemePaddingSmall).PaddingRight(ThemePaddingSmall)
                .BackgroundColor(NextRowColorKey())
                .BorderRadius(AlternatingRowRadius)
                .Child(recipeKeyLabel).Child(recipeValueLabel)
                .Build());

            // Processing progress bar (visible only when processing)
            var progressBar = ForgeProgressBar.Create(LocalizationKeys.InspectorCraftProgress,
                0f, forge.ProcessingDuration)
                .Value(forge.ProcessingTimer)
                .MarginTop(4)
                .Build();
            content.Add(progressBar);

            // Apply initial state
            ApplyForgeState(forge, recipeValueLabel, progressBar);

            Observe(() => forge.ActiveRecipeId, _ =>
            {
                ApplyForgeState(forge, recipeValueLabel, progressBar);
            });

            Observe(() => forge.ProcessingTimer, timer =>
            {
                progressBar.Value = timer;
                progressBar.style.display = timer > 0 ? DisplayStyle.Flex : DisplayStyle.None;
            });
        }

        private void ApplyForgeState(ForgeLogic forge, ForgeLabel recipeValueLabel, ForgeProgressBar progressBar)
        {
            bool hasRecipe = !string.IsNullOrEmpty(forge.ActiveRecipeId);
            recipeValueLabel.Text = hasRecipe
                ? CapitalizeFirst(forge.ActiveRecipeId)
                : L(LocalizationKeys.InspectorNoRecipe);
            recipeValueLabel.ColorKey = hasRecipe ? "text.primary" : "text.disabled";
            progressBar.style.display = forge.ProcessingTimer > 0 ? DisplayStyle.Flex : DisplayStyle.None;
            progressBar.Value = forge.ProcessingTimer;
        }
    }
}
