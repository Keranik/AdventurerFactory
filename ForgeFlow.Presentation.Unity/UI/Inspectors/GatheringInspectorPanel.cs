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
    /// Inspector panel for gathering structures (Forestry, Mining Node, etc.).
    /// Shows production stats, tool output table, node health, worker slots with
    /// per-worker progress bars.
    ///
    /// Uses the observer pattern: UI is built once, all dynamic data bound
    /// via <see cref="BaseInspectorPanel.Observe{T}"/>.
    /// </summary>
    internal sealed class GatheringInspectorPanel : BaseRecipeStructureInspector, ITickableWindow, IAutoRegisteredInspector
    {
        public override string WindowId => WindowIds.GatheringInspector;

        public GatheringInspectorPanel(SimulationTicker? simulation)
            : base("gathering_inspector", LocalizationKeys.InspectorTitle, 320f, 500f, simulation)
        {
        }

        public void Tick(float deltaTime)
        {
            UpdateBindings();
        }

        protected override void BuildStructureSpecificContent(VisualElement content, StructureBase entity)
        {
            if (entity is not GatheringLogicBase gathering)
            {
                return;
            }

            // ── Production Section (static from proto) ──
            AddSectionDivider(content);
            AddRichSectionHeader(content, "▸ " + L(LocalizationKeys.InspectorSectionProduction));

            AddRichPropertyRow(content, LocalizationKeys.InspectorTargetResource,
                CapitalizeFirst(gathering.TargetResourceId));
            AddRichPropertyRow(content, LocalizationKeys.InspectorRequiredBiome,
                gathering.RequiredBiome.ToString());
            AddRichPropertyRow(content, LocalizationKeys.InspectorGatherRate,
                $"{gathering.GatherAmountPerCycle} / {gathering.GatherInterval:F1}s");

            if (gathering.BiomeBonusMultiplier != 1.0f)
            {
                AddColoredPropertyRow(content, "Biome Bonus",
                    $"×{gathering.BiomeBonusMultiplier:F1}", "status.success");
            }

            // ── Node Health (dynamic) ──
            if (gathering.LinkedNode != null)
            {
                var node = gathering.LinkedNode;
                var nodeHealthBar = ForgeProgressBar.Create(LocalizationKeys.InspectorNodeHealth,
                    0f, node.MaxYield)
                    .Value(node.CurrentYield)
                    .MarginTop(6).MarginBottom(4)
                    .Build();
                content.Add(nodeHealthBar);

                var depletedWarning = ForgeLabel.CreateRaw("⚠ Depleted", ForgeLabelSize.Small)
                    .Color("status.error").Bold().MarginTop(2).Build();
                depletedWarning.style.display = node.IsDepleted ? DisplayStyle.Flex : DisplayStyle.None;
                content.Add(depletedWarning);

                Observe(() => node.CurrentYield, yield =>
                {
                    nodeHealthBar.Value = yield;
                });

                Observe(() => node.IsDepleted, depleted =>
                {
                    depletedWarning.style.display = depleted ? DisplayStyle.Flex : DisplayStyle.None;
                });
            }

            // ── Tool Output Table (static from proto) ──
            if (gathering.ToolOutputMap.Count > 0)
            {
                AddSubHeader(content, L(LocalizationKeys.InspectorToolOutputs) + ":");

                foreach (var kvp in gathering.ToolOutputMap)
                {
                    var toolLabel = ForgeLabel.CreateRaw(
                        string.IsNullOrEmpty(kvp.Key) ? "Bare Hands" : kvp.Key,
                        ForgeLabelSize.Small)
                        .Color("text.secondary").FlexGrow(1).Build();

                    var outputLabel = ForgeLabel.CreateRaw("→ " + CapitalizeFirst(kvp.Value),
                        ForgeLabelSize.Small)
                        .Color("status.warning").Bold().Build();

                    content.Add(ForgeContainer.Create()
                        .FlexDirection(FlexDirection.Row)
                        .JustifyContent(Justify.SpaceBetween)
                        .PaddingLeft(ThemePaddingNormal + 4).PaddingRight(ThemePaddingSmall)
                        .PaddingTop(2).PaddingBottom(2)
                        .BackgroundColor(NextRowColorKey())
                        .BorderRadius(AlternatingRowRadius)
                        .Child(toolLabel).Child(outputLabel)
                        .Build());
                }
            }

            // ── Workers Section ──
            AddSectionDivider(content);
            AddRichSectionHeader(content, "▸ " + L(LocalizationKeys.InspectorSectionWorkers));

            // Worker count row
            var workerCountKeyLabel = ForgeLabel.CreateRaw(
                L(LocalizationKeys.InspectorWorkers), ForgeLabelSize.Normal)
                .Color("text.secondary").FlexGrow(1).Build();
            var workerCountValueLabel = ForgeLabel.CreateRaw(
                $"{gathering.WorkerCount} / {gathering.MaxWorkerCapacity}", ForgeLabelSize.Normal)
                .Color("text.primary").Bold().TextAlign(TextAnchor.MiddleRight).Build();
            content.Add(ForgeContainer.Create()
                .FlexDirection(FlexDirection.Row)
                .JustifyContent(Justify.SpaceBetween)
                .PaddingTop(3).PaddingBottom(3)
                .PaddingLeft(ThemePaddingSmall).PaddingRight(ThemePaddingSmall)
                .BackgroundColor(NextRowColorKey())
                .BorderRadius(AlternatingRowRadius)
                .Child(workerCountKeyLabel).Child(workerCountValueLabel)
                .Build());

            // Worker capacity bar
            var workerCapacityBar = ForgeProgressBar.Create(LocalizationKeys.InspectorWorkers,
                0f, gathering.MaxWorkerCapacity)
                .Value(gathering.WorkerCount)
                .MarginTop(4).MarginBottom(4)
                .Build();
            content.Add(workerCapacityBar);

            Observe(() => gathering.WorkerCount, count =>
            {
                workerCountValueLabel.Text = $"{count} / {gathering.MaxWorkerCapacity}";
                workerCapacityBar.Value = count;
            });

            // ── Per-Worker Slot Rows (pre-created, visibility toggled) ──
            var slotRows = new VisualElement[gathering.MaxWorkerCapacity];
            var slotWorkerLabels = new ForgeLabel[gathering.MaxWorkerCapacity];
            var slotOutputLabels = new ForgeLabel[gathering.MaxWorkerCapacity];
            var slotProgressBars = new ForgeProgressBar[gathering.MaxWorkerCapacity];

            for (int slotIndex = 0; slotIndex < gathering.MaxWorkerCapacity; slotIndex++)
            {
                int i = slotIndex;
                var slots = gathering.WorkerSlots;

                var workerLabel = ForgeLabel.CreateRaw("", ForgeLabelSize.Small)
                    .Color("text.secondary").Width(80).Build();
                slotWorkerLabels[i] = workerLabel;

                var outputInfo = ForgeLabel.CreateRaw("", ForgeLabelSize.Small)
                    .Color("status.warning").Width(80).Build();
                slotOutputLabels[i] = outputInfo;

                var gatherBar = ForgeProgressBar.Create(
                    LocalizationKeys.InspectorGatherProgress,
                    0f, gathering.GatherInterval)
                    .Value(0f)
                    .Color("status.warning")
                    .FlexGrow(1).MarginLeft(4)
                    .Build();
                slotProgressBars[i] = gatherBar;

                var row = ForgeContainer.Create()
                    .FlexDirection(FlexDirection.Row)
                    .AlignItems(Align.Center)
                    .PaddingLeft(ThemePaddingSmall).PaddingRight(ThemePaddingSmall)
                    .PaddingTop(2).PaddingBottom(2)
                    .BackgroundColor(NextRowColorKey())
                    .BorderRadius(AlternatingRowRadius)
                    .Child(workerLabel).Child(outputInfo).Child(gatherBar)
                    .Build();
                slotRows[i] = row;

                // Apply initial state
                ApplySlotState(slots, i, row, workerLabel, outputInfo, gatherBar);
                content.Add(row);

                Observe(() => slots[i].IsOccupied, _ =>
                {
                    ApplySlotState(gathering.WorkerSlots, i, row, workerLabel, outputInfo, gatherBar);
                });

                Observe(() => slots[i].VillagerId, _ =>
                {
                    ApplySlotState(gathering.WorkerSlots, i, row, workerLabel, outputInfo, gatherBar);
                });

                Observe(() => slots[i].GatherProgress, progress =>
                {
                    gatherBar.Value = progress;
                });

                Observe(() => slots[i].CycleComplete, complete =>
                {
                    gatherBar.ColorKey = complete ? "status.success" : "status.warning";
                });
            }

            // ── Wait Queue & Full Warning ──
            var waitQueueKeyLabel = ForgeLabel.CreateRaw(
                L(LocalizationKeys.InspectorWorkerQueue), ForgeLabelSize.Normal)
                .Color("text.secondary").FlexGrow(1).Build();
            var waitQueueValueLabel = ForgeLabel.CreateRaw(
                gathering.WaitQueueCount.ToString(), ForgeLabelSize.Normal)
                .Color("status.warning").Bold().TextAlign(TextAnchor.MiddleRight).Build();
            var waitQueueRow = ForgeContainer.Create()
                .FlexDirection(FlexDirection.Row)
                .JustifyContent(Justify.SpaceBetween)
                .PaddingTop(3).PaddingBottom(3)
                .PaddingLeft(ThemePaddingSmall).PaddingRight(ThemePaddingSmall)
                .BackgroundColor(NextRowColorKey())
                .BorderRadius(AlternatingRowRadius)
                .Child(waitQueueKeyLabel).Child(waitQueueValueLabel)
                .Build();
            waitQueueRow.style.display = gathering.WaitQueueCount > 0 ? DisplayStyle.Flex : DisplayStyle.None;
            content.Add(waitQueueRow);

            var fullWarning = ForgeLabel.CreateRaw("⚠ Full — no slots", ForgeLabelSize.Small)
                .Color("status.error").Bold().MarginTop(2).Build();
            fullWarning.style.display = (!gathering.HasFreeSlot && gathering.WaitQueueCount == 0)
                ? DisplayStyle.Flex : DisplayStyle.None;
            content.Add(fullWarning);

            Observe(() => gathering.WaitQueueCount, count =>
            {
                waitQueueRow.style.display = count > 0 ? DisplayStyle.Flex : DisplayStyle.None;
                waitQueueValueLabel.Text = count.ToString();
            });

            Observe(() => gathering.HasFreeSlot, hasFree =>
            {
                fullWarning.style.display = (!hasFree && gathering.WaitQueueCount == 0)
                    ? DisplayStyle.Flex : DisplayStyle.None;
            });
        }

        private static void ApplySlotState(
            WorkerSlot[] slots,
            int i,
            VisualElement row,
            ForgeLabel workerLabel,
            ForgeLabel outputLabel,
            ForgeProgressBar gatherBar)
        {
            bool occupied = slots[i].IsOccupied;
            row.style.display = occupied ? DisplayStyle.Flex : DisplayStyle.None;
            if (occupied)
            {
                string toolIcon = string.IsNullOrEmpty(slots[i].ToolId) ? "✋" : "🔧";
                workerLabel.Text = $"{toolIcon} #{slots[i].VillagerId}";
                outputLabel.Text = $"→ {CapitalizeFirst(slots[i].OutputResourceId)}";
                gatherBar.Value = slots[i].GatherProgress;
                gatherBar.ColorKey = slots[i].CycleComplete ? "status.success" : "status.warning";
            }
        }

            }
        }
