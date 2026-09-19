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
    /// Inspector panel for Inn structures. Shows rest rate, occupancy bar,
    /// and full-capacity warning.
    ///
    /// Uses the observer pattern: UI is built once, dynamic data bound
    /// via <see cref="BaseInspectorPanel.Observe{T}"/>.
    /// </summary>
    internal sealed class InnInspectorPanel : BaseActivityStructureInspector, ITickableWindow, IAutoRegisteredInspector
    {
        public override string WindowId => WindowIds.InnInspector;

        public InnInspectorPanel(SimulationTicker? simulation)
            : base("inn_inspector", LocalizationKeys.InspectorTitle, 320f, 350f, simulation)
        {
        }

        public void Tick(float deltaTime)
        {
            UpdateBindings();
        }

        protected override void BuildStructureSpecificContent(VisualElement content, StructureBase entity)
        {
            if (entity is not InnLogic inn)
            {
                return;
            }

            AddSectionDivider(content);
            AddRichSectionHeader(content, "▸ " + L(LocalizationKeys.InspectorSectionProduction));

            // Rest rate (static from recipe)
            AddRichPropertyRow(content, LocalizationKeys.InspectorRestRate,
                $"{inn.RestRecipeDuration:F1}s");

            // Shared occupancy section (count/max row + bar + full warning)
            var controls = AddOccupancySection(content,
                LocalizationKeys.InspectorOccupants,
                inn.CurrentOccupants.Count,
                inn.MaxOccupants,
                inn.CanAcceptVillager);

            Observe(() => inn.CurrentOccupants.Count, count =>
            {
                controls.ValueLabel.Text = $"{count} / {inn.MaxOccupants}";
                controls.Bar.Value = count;
            });

            Observe(() => inn.CanAcceptVillager, canAccept =>
            {
                controls.FullWarning.style.display = canAccept ? DisplayStyle.None : DisplayStyle.Flex;
            });
        }
    }
}
