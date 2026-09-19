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
    /// Inspector panel for filter splitter routing structures. Shows the current
    /// single-item filter state, routing directions, and the configured rule list.
    /// Provides buttons to assign/clear the item filter via <see cref="StructureManager"/>
    /// and a ProductPicker dialog identical to the Stockpile pattern.
    ///
    /// Uses the observer pattern: UI is built once in
    /// <see cref="BuildStructureSpecificContent"/> and all live data is bound
    /// via <see cref="BaseInspectorPanel.Observe{T}"/>. Per-tick updates
    /// evaluate only the bindings (zero-alloc when nothing changes).
    /// </summary>
    internal sealed class FilterSplitterInspectorPanel : BaseStructureInspector, ITickableWindow, IAutoRegisteredInspector
    {
        public override string WindowId => WindowIds.FilterSplitterInspector;

        private const string PickerTrackingId = "filter_splitter_product_picker";

        private readonly ItemRegistry? _itemRegistry;
        private readonly ITransientElementTracker? _transientTracker;
        private ForgeProductPicker? _activePicker;
        private ForgePanel? _pickerDialog;

        public FilterSplitterInspectorPanel(
            SimulationTicker? simulation,
            ItemRegistry? itemRegistry,
            ITransientElementTracker? transientTracker = null)
            : base("filter_splitter_inspector", LocalizationKeys.FilterSplitterInspectorTitle, 320f, 520f, simulation)
        {
            _itemRegistry = itemRegistry;
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
            if (entity is not FilterSplitterLogic splitter)
            {
                return;
            }

            // ── Routing Section ──────────────────────────────────────────
            AddSectionDivider(content);
            AddRichSectionHeader(content, "▸ " + L(LocalizationKeys.FilterSplitterSectionRouting));

            var defaultDirLabel = ForgeLabel.CreateRaw(
                $"→ {splitter.DefaultDirection}", ForgeLabelSize.Normal)
                .Color("text.primary").Bold().TextAlign(TextAnchor.MiddleRight).Build();
            var defaultDirKeyLabel = ForgeLabel.CreateRaw(
                L(LocalizationKeys.FilterSplitterDefaultDirection), ForgeLabelSize.Normal)
                .Color("text.secondary").FlexGrow(1).Build();
            content.Add(ForgeContainer.Create()
                .FlexDirection(FlexDirection.Row)
                .JustifyContent(Justify.SpaceBetween)
                .PaddingTop(3).PaddingBottom(3)
                .PaddingLeft(ThemePaddingSmall).PaddingRight(ThemePaddingSmall)
                .BackgroundColor(NextRowColorKey())
                .BorderRadius(AlternatingRowRadius)
                .Child(defaultDirKeyLabel).Child(defaultDirLabel)
                .Build());

            AddRichPropertyRow(content, LocalizationKeys.InspectorOutputDirection,
                $"→ {splitter.OutputDirection}");

            // ── Item Filter Section ──────────────────────────────────────
            AddSectionDivider(content);
            AddRichSectionHeader(content, "▸ " + L(LocalizationKeys.FilterSplitterSectionFilter));

            // "No item filter set" label — visible when no filter assigned
            var noFilterLabel = ForgeLabel.CreateRaw(
                L(LocalizationKeys.FilterSplitterNoFilter), ForgeLabelSize.Small)
                .Color("text.secondary").PaddingLeft(ThemePaddingSmall).MarginBottom(2)
                .Build();
            content.Add(noFilterLabel);

            // Filter display row — visible when a filter is assigned
            var filterSwatch = ForgeContainer.Create()
                .Width(14).Height(14)
                .MarginRight(4).FlexShrink(0)
                .BorderWidth(1).BorderRadius(2)
                .Build();
            var filterNameLabel = ForgeLabel.CreateRaw("", ForgeLabelSize.Small)
                .Color("text.primary").FlexGrow(1).Build();
            var filterRow = ForgeContainer.Create()
                .FlexDirection(FlexDirection.Row)
                .AlignItems(Align.Center)
                .PaddingLeft(ThemePaddingSmall).PaddingRight(ThemePaddingSmall)
                .PaddingTop(2).PaddingBottom(2)
                .BackgroundColor(NextRowColorKey())
                .BorderRadius(AlternatingRowRadius)
                .Child(filterSwatch).Child(filterNameLabel)
                .Build();
            content.Add(filterRow);

            // Filter output direction row — visible when filter assigned
            var filterDirValueLabel = ForgeLabel.CreateRaw(
                $"→ {splitter.FilteredOutputDirection}", ForgeLabelSize.Normal)
                .Color("status.warning").Bold().TextAlign(TextAnchor.MiddleRight).Build();
            var filterDirKeyLabel = ForgeLabel.CreateRaw(
                L(LocalizationKeys.FilterSplitterFilterDirection), ForgeLabelSize.Normal)
                .Color("text.secondary").FlexGrow(1).Build();
            var filterDirRow = ForgeContainer.Create()
                .FlexDirection(FlexDirection.Row)
                .JustifyContent(Justify.SpaceBetween)
                .PaddingTop(3).PaddingBottom(3)
                .PaddingLeft(ThemePaddingSmall).PaddingRight(ThemePaddingSmall)
                .BackgroundColor(NextRowColorKey())
                .BorderRadius(AlternatingRowRadius)
                .Child(filterDirKeyLabel).Child(filterDirValueLabel)
                .Build();
            content.Add(filterDirRow);

            // Assign Filter button
            var assignButton = ForgeButton.Create(LocalizationKeys.FilterSplitterAssignFilter, () =>
            {
                if (TryGetStructure(out var m) && m is FilterSplitterLogic s)
                {
                    OpenPickerDialog(s);
                }
            }).MarginTop(4).Build();
            content.Add(assignButton);

            // Clear Filter button — hidden when no filter assigned
            var clearButton = ForgeButton.Create(LocalizationKeys.FilterSplitterClearFilter, () =>
            {
                Simulation?.CommandBus.Dispatch(new SetFilterCommand(InspectedStructureId, null));
            }).Secondary().MarginTop(2).Build();
            content.Add(clearButton);

            // Set initial filter display state
            ApplyFilterState(splitter.FilteredItemId, noFilterLabel, filterRow,
                filterNameLabel, filterSwatch, filterDirRow, filterDirValueLabel, clearButton);

            // Binding: react to filter assignment changes
            Observe(() => splitter.FilteredItemId, id =>
            {
                ApplyFilterState(id, noFilterLabel, filterRow,
                    filterNameLabel, filterSwatch, filterDirRow, filterDirValueLabel, clearButton);
            });

            Observe(() => splitter.FilteredOutputDirection, dir =>
            {
                filterDirValueLabel.Text = $"→ {dir}";
            });

            // ── Rules Section ────────────────────────────────────────────
            AddSectionDivider(content);
            AddRichSectionHeader(content, "▸ " + L(LocalizationKeys.FilterSplitterSectionRules));

            var rulesContainer = ForgeContainer.Create()
                .FlexDirection(FlexDirection.Column)
                .Build();
            content.Add(rulesContainer);

            RebuildRulesSection(rulesContainer, splitter);

            Observe(() => ComputeRulesFingerprint(splitter.Rules), _ =>
            {
                RebuildRulesSection(rulesContainer, splitter);
            });
        }

        // ── Filter State ─────────────────────────────────────────────

        /// <summary>
        /// Applies the correct visibility and content for the item filter section
        /// based on the current <paramref name="filteredItemId"/>.
        /// Called both on initial build and from the observer callback.
        /// </summary>
        private void ApplyFilterState(
            string? filteredItemId,
            ForgeLabel noFilterLabel,
            ForgeContainer filterRow,
            ForgeLabel filterNameLabel,
            ForgeContainer filterSwatch,
            ForgeContainer filterDirRow,
            ForgeLabel filterDirValueLabel,
            ForgeButton clearButton)
        {
            bool hasFilter = filteredItemId != null;
            noFilterLabel.style.display = hasFilter ? DisplayStyle.None : DisplayStyle.Flex;
            filterRow.style.display = hasFilter ? DisplayStyle.Flex : DisplayStyle.None;
            filterDirRow.style.display = hasFilter ? DisplayStyle.Flex : DisplayStyle.None;
            clearButton.style.display = hasFilter ? DisplayStyle.Flex : DisplayStyle.None;

            if (hasFilter)
            {
                filterNameLabel.Text = GetItemDisplayName(filteredItemId!);
                filterSwatch.style.backgroundColor = GetItemColor(filteredItemId!);
            }
        }

        // ── Picker Dialog ─────────────────────────────────────────────

        private void OpenPickerDialog(FilterSplitterLogic splitter)
        {
            ClosePickerDialog();

            var scrollView = ForgeScrollView.Create()
                .FlexGrow(1).Build();

            _activePicker = ForgeProductPicker.Create(ProductPickerStyle.MediumWithText)
                .Products(BuildProductList(splitter))
                .OnItemSelected(id =>
                {
                    Simulation?.CommandBus.Dispatch(new SetFilterCommand(InspectedStructureId, id));
                    ClosePickerDialog();
                })
                .FlexGrow(1).Build();

            scrollView.AddContent(_activePicker);

            _pickerDialog = ForgePanel.Create("filter_picker_dialog",
                    LocalizationKeys.ProductPickerTitle, 320f, 400f)
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

        // ── Rules Section Helpers ─────────────────────────────────────

        /// <summary>
        /// Rebuilds the rules list inside the given container.
        /// Called on initial build and whenever the rules fingerprint changes.
        /// </summary>
        private void RebuildRulesSection(ForgeContainer container, FilterSplitterLogic splitter)
        {
            container.Clear();

            AddRichPropertyRow(container, LocalizationKeys.FilterSplitterRuleCount,
                splitter.Rules.Count.ToString());

            if (splitter.Rules.Count == 0)
            {
                container.Add(
                    ForgeLabel.CreateRaw(L(LocalizationKeys.FilterSplitterNoRules), ForgeLabelSize.Small)
                        .Color("text.secondary").PaddingLeft(ThemePaddingSmall).MarginBottom(2)
                        .Build());
            }
            else
            {
                for (int i = 0; i < splitter.Rules.Count; i++)
                {
                    var rule = splitter.Rules[i];
                    string conditionText = FormatCondition(rule.ConditionType, rule.ConditionValue);
                    string directionText = $"→ {rule.OutputDirection}";

                    var condLabel = ForgeLabel.CreateRaw(conditionText, ForgeLabelSize.Small)
                        .Color("text.secondary").FlexGrow(1).Build();
                    var dirLabel = ForgeLabel.CreateRaw(directionText, ForgeLabelSize.Small)
                        .Color("status.warning").Bold().TextAlign(TextAnchor.MiddleRight).Build();

                    container.Add(ForgeContainer.Create()
                        .FlexDirection(FlexDirection.Row)
                        .JustifyContent(Justify.SpaceBetween)
                        .PaddingLeft(ThemePaddingNormal + 4).PaddingRight(ThemePaddingSmall)
                        .PaddingTop(2).PaddingBottom(2)
                        .Child(condLabel).Child(dirLabel)
                        .Build());
                }
            }
        }

        /// <summary>
        /// Computes a lightweight fingerprint of the rules list to detect changes
        /// without full comparison each tick.
        /// </summary>
        private static int ComputeRulesFingerprint(List<GateRule> rules)
        {
            unchecked
            {
                int fingerprint = rules.Count;
                for (int i = 0; i < rules.Count; i++)
                {
                    fingerprint ^= ((int)rules[i].ConditionType * 397)
                        ^ (rules[i].ConditionValue.GetHashCode() * 31)
                        ^ ((int)rules[i].OutputDirection * 17);
                }
                return fingerprint;
            }
        }

        /// <summary>Formats a gate condition into a human-readable string.</summary>
        private static string FormatCondition(GateConditionType type, string value) => type switch
        {
            GateConditionType.MinLevel => $"Level ≥ {value}",
            GateConditionType.MaxLevel => $"Level ≤ {value}",
            GateConditionType.HasTrait => $"Has Trait: {CapitalizeFirst(value)}",
            GateConditionType.HasProfession => $"Profession: {CapitalizeFirst(value)}",
            GateConditionType.HasAbility => $"Has Ability: {CapitalizeFirst(value)}",
            GateConditionType.HasJobClass => $"Class: {CapitalizeFirst(value)}",
            GateConditionType.HasItem => $"Has Item: {CapitalizeFirst(value)}",
            GateConditionType.CarryingItem => $"Carrying: {CapitalizeFirst(value)}",
            GateConditionType.HasToolType => $"Tool: {CapitalizeFirst(value)}",
            _ => $"{type}: {value}"
        };

        // ── Item Helpers ──────────────────────────────────────────────

        private List<ProductEntry> BuildProductList(FilterSplitterLogic splitter)
        {
            var entries = new List<ProductEntry>();
            if (_itemRegistry == null)
            {
                return entries;
            }

            foreach (var item in _itemRegistry.GetAll())
            {
                bool selected = splitter.FilteredItemId == item.Id;
                entries.Add(new ProductEntry(
                    item.Id,
                    item.DisplayName.Length > 0 ? item.DisplayName : item.Id,
                    GetItemColor(item.Id),
                    selected));
            }

            return entries;
        }

        private string GetItemDisplayName(string itemId)
        {
            if (_itemRegistry != null && _itemRegistry.TryGet(itemId, out var proto))
            {
                return proto.DisplayName.Length > 0 ? proto.DisplayName : itemId;
            }
            return itemId;
        }

        private static Color GetItemColor(string itemId)
        {
            int hash = itemId.GetHashCode();
            float h = ((hash & 0x7FFFFFFF) % 360) / 360f;
            return Color.HSVToRGB(h, 0.5f, 0.7f);
        }

        public override void Dispose()
        {
            ClosePickerDialog();
            base.Dispose();
        }
    }
}
