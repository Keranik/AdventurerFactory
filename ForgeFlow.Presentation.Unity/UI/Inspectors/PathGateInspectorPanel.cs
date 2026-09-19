using ForgeFlow.Core.Entities;
using ForgeFlow.Core.Localization;
using ForgeFlow.Core.Proto;
using ForgeFlow.Core.Proto.Logic;
using ForgeFlow.Core.Systems;
using ForgeFlow.Presentation.Unity.UI.Components;
using UnityEngine;
using UnityEngine.UIElements;
using EntityId = ForgeFlow.Core.Entities.EntityId;

namespace ForgeFlow.Presentation.Unity.UI.Inspectors
{
    /// <summary>
    /// Inspector panel for PathGate entities. Shows the gate's facing direction,
    /// mode (Entrance/Exit), and which building it is connected to.
    /// </summary>
    internal sealed class PathGateInspectorPanel : BaseEntityInspector, ITickableWindow, IAutoRegisteredInspector
    {
        private ulong _inspectedGateId;

        public override string WindowId => WindowIds.PathGateInspector;

        public PathGateInspectorPanel(SimulationTicker? simulation)
            : base("path_gate_inspector", LocalizationKeys.PathGateInspectorTitle, 300f, 300f, simulation)
        {
        }

        /// <summary>Sets the gate to inspect and refreshes the panel.</summary>
        public void InspectGate(EntityId gateId)
        {
            _inspectedGateId = gateId;
            Refresh();
        }

        public void Tick(float deltaTime)
        {
            UpdateBindings();
        }

        protected override void BuildContent()
        {
            ResetRowIndex();

            var gate = Simulation?.EntityManager.GetPathGate(new EntityId(_inspectedGateId));
            if (gate == null)
            {
                return;
            }

            var content = ForgeContainer.Create()
                .FlexDirection(FlexDirection.Column)
                .Build();

            // ── Header ──
            string modeIcon = gate.IsEntrance ? "🟢" : "🟠";
            string modeName = gate.IsEntrance ? "Entrance" : "Exit";
            BuildEntityHeader(
                content,
                modeIcon,
                GetStructureDisplayName("PathGate"),
                GetStructureCategory("PathGate"),
                GetStructureCategoryColor("PathGate"),
                0,
                gate.Id);

            // ── Gate Properties Section ──
            AddSectionDivider(content);
            AddRichSectionHeader(content, "▸ " + L(LocalizationKeys.PathGateInspectorDescription));

            // Facing direction
            var facingLabel = ForgeLabel.CreateRaw($"→ {gate.Facing}", ForgeLabelSize.Normal)
                .Color("text.primary").Bold().TextAlign(TextAnchor.MiddleRight).Build();
            AddRichPropertyRow(content, LocalizationKeys.PathGateInspectorFacing, gate.Facing.ToString());

            Observe(() => gate.Facing.ToString(), newFacing =>
            {
                facingLabel.Text = $"→ {newFacing}";
            });

            // Mode (Entrance / Exit)
            string modeColorKey = gate.IsEntrance ? "status.success" : "status.warning";
            AddColoredPropertyRow(content, L(LocalizationKeys.PathGateInspectorMode), modeName, modeColorKey);

            // Linked building
            string linkedText = L(LocalizationKeys.PathGateInspectorNotLinked);
            string linkedColorKey = "text.disabled";
            if (gate.IsLinked && Simulation != null)
            {
                var linked = Simulation.EntityManager.GetStructureOrRoutingNode(new EntityId(gate.LinkedStructureId!.Value));
                if (linked != null)
                {
                    linkedText = $"{GetStructureDisplayName(linked.GetCategoryName())} #{linked.Id}";
                    linkedColorKey = "text.accent";
                }
                else
                {
                    linkedText = $"#{gate.LinkedStructureId}";
                    linkedColorKey = "text.primary";
                }
            }
            AddColoredPropertyRow(content, L(LocalizationKeys.PathGateInspectorLinkedBuilding), linkedText, linkedColorKey);

            // Description
            string description = gate.IsEntrance
                ? L(LocalizationKeys.PathGateDescEntrance)
                : L(LocalizationKeys.PathGateDescExit);
            var descLabel = ForgeLabel.CreateRaw(description, ForgeLabelSize.Small)
                .Color("text.secondary")
                .WhiteSpace(WhiteSpace.Normal)
                .Build();
            content.Add(ForgeContainer.Create()
                .FlexDirection(FlexDirection.Row)
                .PaddingTop(6).PaddingBottom(6)
                .PaddingLeft(ThemePaddingSmall).PaddingRight(ThemePaddingSmall)
                .BackgroundColor(NextRowColorKey())
                .BorderRadius(AlternatingRowRadius)
                .Child(descLabel)
                .Build());

            ContentContainer.Add(content);
        }
    }
}
