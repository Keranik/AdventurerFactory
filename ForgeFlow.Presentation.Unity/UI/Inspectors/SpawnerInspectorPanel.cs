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
    /// Inspector panel for VillageSpawner structures. Shows spawn interval,
    /// max villagers, and pending dispatch count.
    ///
    /// Uses the observer pattern: UI is built once, pending dispatch
    /// count bound via <see cref="BaseInspectorPanel.Observe{T}"/>.
    /// </summary>
    internal sealed class SpawnerInspectorPanel : BaseActivityStructureInspector, ITickableWindow, IAutoRegisteredInspector
    {
        public override string WindowId => WindowIds.SpawnerInspector;

        public SpawnerInspectorPanel(SimulationTicker? simulation)
            : base("spawner_inspector", LocalizationKeys.InspectorTitle, 320f, 300f, simulation)
        {
        }

        public void Tick(float deltaTime)
        {
            UpdateBindings();
        }

        protected override void BuildStructureSpecificContent(VisualElement content, StructureBase entity)
        {
            if (entity is not VillageSpawnerLogic spawner)
            {
                return;
            }

            AddSectionDivider(content);
            AddRichSectionHeader(content, "▸ " + L(LocalizationKeys.InspectorSectionProduction));

            // Static properties
            AddRichPropertyRow(content, LocalizationKeys.InspectorSpawnInterval,
                $"{spawner.SpawnInterval:F1}s");
            AddRichPropertyRow(content, LocalizationKeys.InspectorSpawnMax,
                spawner.MaxVillagers.ToString());

            // Pending dispatch (dynamic, conditionally visible)
            var pendingKeyLabel = ForgeLabel.CreateRaw(
                "Pending Dispatch", ForgeLabelSize.Normal)
                .Color("text.secondary").FlexGrow(1).Build();
            var pendingValueLabel = ForgeLabel.CreateRaw(
                spawner.PendingVillagers.Count.ToString(), ForgeLabelSize.Normal)
                .Color("status.warning").Bold().TextAlign(TextAnchor.MiddleRight).Build();
            var pendingRow = ForgeContainer.Create()
                .FlexDirection(FlexDirection.Row)
                .JustifyContent(Justify.SpaceBetween)
                .PaddingTop(3).PaddingBottom(3)
                .PaddingLeft(ThemePaddingSmall).PaddingRight(ThemePaddingSmall)
                .BackgroundColor(NextRowColorKey())
                .BorderRadius(AlternatingRowRadius)
                .Child(pendingKeyLabel).Child(pendingValueLabel)
                .Build();
            pendingRow.style.display = spawner.PendingVillagers.Count > 0
                ? DisplayStyle.Flex : DisplayStyle.None;
            content.Add(pendingRow);

            Observe(() => spawner.PendingVillagers.Count, count =>
            {
                pendingRow.style.display = count > 0 ? DisplayStyle.Flex : DisplayStyle.None;
                pendingValueLabel.Text = count.ToString();
            });
        }
    }
}
