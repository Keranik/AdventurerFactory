using System.Collections.Generic;
using ForgeFlow.Core.Commands;
using ForgeFlow.Core.Data;
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
    /// Inspector panel for CraftStation structures. Shows active recipe, craft progress bar,
    /// ingredient readiness, stored inputs breakdown, and a recipe picker button.
    ///
    /// Uses the observer pattern: UI is built once, dynamic data bound
    /// via <see cref="BaseInspectorPanel.Observe{T}"/>.
    /// </summary>
    internal sealed class CraftStationInspectorPanel : BaseRecipeStructureInspector, ITickableWindow, IAutoRegisteredInspector
    {
        public override string WindowId => WindowIds.CraftStationInspector;

        private const string PickerTrackingId = "craft_recipe_picker";

        private readonly RecipeRegistry? _recipeRegistry;
        private readonly ResearchManager? _researchManager;
        private readonly ITransientElementTracker? _transientTracker;
        private ForgeRecipePicker? _activePicker;
        private ForgePanel? _pickerDialog;

        public CraftStationInspectorPanel(
            SimulationTicker? simulation,
            RecipeRegistry? recipeRegistry = null,
            ResearchManager? researchManager = null,
            ITransientElementTracker? transientTracker = null)
            : base("craftstation_inspector", LocalizationKeys.InspectorTitle, 320f, 400f, simulation)
        {
            _recipeRegistry = recipeRegistry;
            _researchManager = researchManager;
            _transientTracker = transientTracker;
        }

        public void Tick(float deltaTime)
        {
            UpdateBindings();
        }

        public override void Hide()
        {
            ClosePickerDialog();
            base.Hide();
        }

        protected override void BuildStructureSpecificContent(VisualElement content, StructureBase entity)
        {
            if (entity is not CraftStationLogic craft)
            {
                return;
            }

            AddSectionDivider(content);
            AddRichSectionHeader(content, "▸ " + L(LocalizationKeys.InspectorSectionProduction));

            // ── Recipe picker button ──
            var selectRecipeBtn = ForgeButton.Create(L(LocalizationKeys.RecipePickerSelect))
                .FontSize(ThemeFontNormal)
                .MarginTop(4).MarginBottom(4)
                .PaddingLeft(ThemePaddingSmall).PaddingRight(ThemePaddingSmall)
                .OnClick(() => ShowRecipePicker(craft))
                .Build();
            content.Add(selectRecipeBtn);

            // ── Recipe display ──
            var recipeKeyLabel = ForgeLabel.CreateRaw(
                L(LocalizationKeys.InspectorCraftRecipe), ForgeLabelSize.Normal)
                .Color("text.secondary").FlexGrow(1).Build();
            var recipeValueLabel = ForgeLabel.CreateRaw("", ForgeLabelSize.Normal)
                .Color("text.primary").Bold().TextAlign(TextAnchor.MiddleRight).Build();
            content.Add(ForgeContainer.Create()
                .FlexDirection(FlexDirection.Row)
                .JustifyContent(Justify.SpaceBetween)
                .PaddingTop(3).PaddingBottom(3)
                .PaddingLeft(ThemePaddingSmall).PaddingRight(ThemePaddingSmall)
                .BackgroundColor(NextRowColorKey())
                .BorderRadius(AlternatingRowRadius)
                .Child(recipeKeyLabel).Child(recipeValueLabel)
                .Build());

            // ── Craft progress bar (visible only when recipe active) ──
            var progressBar = ForgeProgressBar.Create(LocalizationKeys.InspectorCraftProgress,
                0f, craft.ActiveRecipeDuration)
                .Value(craft.CraftTimer)
                .MarginTop(4)
                .Build();
            content.Add(progressBar);

            // ── Ingredient status row (visible only when recipe active) ──
            var ingredientKeyLabel = ForgeLabel.CreateRaw("Ingredients", ForgeLabelSize.Normal)
                .Color("text.secondary").FlexGrow(1).Build();
            var ingredientValueLabel = ForgeLabel.CreateRaw("", ForgeLabelSize.Normal)
                .Bold().TextAlign(TextAnchor.MiddleRight).Build();
            var ingredientRow = ForgeContainer.Create()
                .FlexDirection(FlexDirection.Row)
                .JustifyContent(Justify.SpaceBetween)
                .PaddingTop(3).PaddingBottom(3)
                .PaddingLeft(ThemePaddingSmall).PaddingRight(ThemePaddingSmall)
                .BackgroundColor(NextRowColorKey())
                .BorderRadius(AlternatingRowRadius)
                .Child(ingredientKeyLabel).Child(ingredientValueLabel)
                .Build();
            content.Add(ingredientRow);

            // ── No-recipe label (visible when no recipe) ──
            var noRecipeRow = ForgeContainer.Create()
                .FlexDirection(FlexDirection.Row)
                .JustifyContent(Justify.SpaceBetween)
                .PaddingTop(3).PaddingBottom(3)
                .PaddingLeft(ThemePaddingSmall).PaddingRight(ThemePaddingSmall)
                .Build();
            content.Add(noRecipeRow);

            // Apply initial state
            ApplyRecipeState(craft, recipeValueLabel, progressBar, ingredientRow,
                ingredientValueLabel, noRecipeRow);

            Observe(() => craft.ActiveRecipeId, _ =>
            {
                ApplyRecipeState(craft, recipeValueLabel, progressBar, ingredientRow,
                    ingredientValueLabel, noRecipeRow);
            });

            Observe(() => craft.CraftTimer, timer =>
            {
                progressBar.Value = timer;
            });

            Observe(() => craft.HasRequiredIngredients(), ready =>
            {
                ingredientValueLabel.Text = ready ? "✓ Ready" : "✗ Missing";
                ingredientValueLabel.ColorKey = ready ? "status.success" : "status.warning";
            });

            // ── Stored inputs (dynamic collection) ──
            var inputsContainer = ForgeContainer.Create()
                .FlexDirection(FlexDirection.Column)
                .Build();
            content.Add(inputsContainer);

            RebuildStoredInputs(inputsContainer, craft);

            Observe(() => ComputeInputFingerprint(craft.StoredInputs), _ =>
            {
                RebuildStoredInputs(inputsContainer, craft);
            });
        }

        private void ApplyRecipeState(
            CraftStationLogic craft,
            ForgeLabel recipeValueLabel,
            ForgeProgressBar progressBar,
            VisualElement ingredientRow,
            ForgeLabel ingredientValueLabel,
            VisualElement noRecipeRow)
        {
            bool hasRecipe = craft.ActiveRecipeId != null;
            recipeValueLabel.Text = hasRecipe ? craft.ActiveRecipeId! : L(LocalizationKeys.InspectorNoRecipe);
            recipeValueLabel.ColorKey = hasRecipe ? "text.primary" : "text.disabled";
            progressBar.style.display = hasRecipe ? DisplayStyle.Flex : DisplayStyle.None;
            ingredientRow.style.display = hasRecipe ? DisplayStyle.Flex : DisplayStyle.None;
            noRecipeRow.style.display = hasRecipe ? DisplayStyle.None : DisplayStyle.Flex;

            if (hasRecipe)
            {
                progressBar.Value = craft.CraftTimer;
                bool ready = craft.HasRequiredIngredients();
                ingredientValueLabel.Text = ready ? "✓ Ready" : "✗ Missing";
                ingredientValueLabel.ColorKey = ready ? "status.success" : "status.warning";
            }
        }

        private void RebuildStoredInputs(ForgeContainer container, CraftStationLogic craft)
        {
            container.Clear();

            if (craft.StoredInputs.Count > 0)
            {
                AddSubHeader(container, L(LocalizationKeys.InspectorStoredInputs) + ":");

                foreach (var kvp in craft.StoredInputs)
                {
                    if (kvp.Value > 0)
                    {
                        AddSmallPropertyRow(container, CapitalizeFirst(kvp.Key), $"×{kvp.Value}");
                    }
                }
            }
        }

        private static int ComputeInputFingerprint(Dictionary<string, int> inputs)
        {
            unchecked
            {
                int fingerprint = inputs.Count;
                foreach (var kvp in inputs)
                {
                    fingerprint ^= (kvp.Key.GetHashCode() * 397) ^ kvp.Value;
                }
                return fingerprint;
            }
        }

        private void ShowRecipePicker(CraftStationLogic craft)
        {
            ClosePickerDialog();

            if (_recipeRegistry == null) { return; }

            int currentTier = _researchManager?.CurrentTier ?? 0;

            var entries = new List<RecipeEntry>();
            foreach (var recipe in _recipeRegistry.GetByTier(currentTier))
            {
                if (string.IsNullOrEmpty(recipe.OutputItemId)) { continue; }
                entries.Add(new RecipeEntry(
                    recipe.Id,
                    CapitalizeFirst(recipe.OutputItemId),
                    new Color(0.3f, 0.5f, 0.7f, 1f)));
            }

            var scrollView = ForgeScrollView.Create()
                .FlexGrow(1).Build();

            _activePicker = ForgeRecipePicker.Create()
                .Recipes(entries)
                .OnRecipeSelected(recipeId =>
                {
                    Simulation?.CommandBus.Dispatch(new SetRecipeCommand(craft.Id, recipeId));
                    ClosePickerDialog();
                })
                .FlexGrow(1).Build();

            scrollView.AddContent(_activePicker);

            _pickerDialog = ForgePanel.Create("recipe_picker_dialog",
                    LocalizationKeys.RecipePickerTitle, 320f, 280f)
                .AsTransient(_transientTracker, PickerTrackingId)
                .DockTo(Panel, DockSide.Left)
                .OnClosed(ClosePickerDialog)
                .Content(scrollView)
                .Build();

            Panel.parent?.Add(_pickerDialog);
        }

        private void ClosePickerDialog()
        {
            if (_pickerDialog != null)
            {
                _pickerDialog.parent?.Remove(_pickerDialog);
                _pickerDialog = null;
                _activePicker = null;
            }
        }

        public override void Dispose()
        {
            ClosePickerDialog();
            base.Dispose();
        }
    }
}
