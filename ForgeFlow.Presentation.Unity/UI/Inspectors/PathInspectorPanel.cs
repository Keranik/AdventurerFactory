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
    /// Simple inspector panel for path segments and routing nodes
    /// (Filter Splitter, Balancer, CheckGate, regular path).
    /// Shows entity name, facing direction, and a short description.
    /// </summary>
    internal sealed class PathInspectorPanel : BaseStructureInspector, ITickableWindow, IAutoRegisteredInspector
    {
        public override string WindowId => WindowIds.PathInspector;

        public PathInspectorPanel(SimulationTicker? simulation)
            : base("path_inspector", LocalizationKeys.PathInspectorTitle, 300f, 260f, simulation)
        {
        }

        public void Tick(float deltaTime)
        {
            UpdateBindings();
        }

        protected override void BuildStructureSpecificContent(VisualElement content, StructureBase entity)
        {
            // ── Path Details Section ──
            AddSectionDivider(content);
            AddRichSectionHeader(content, "▸ " + L(LocalizationKeys.PathInspectorDescription));

            // Facing direction
            string facing = GetFacing(entity);
            var facingLabel = ForgeLabel.CreateRaw($"→ {facing}", ForgeLabelSize.Normal)
                .Color("text.primary").Bold().TextAlign(TextAnchor.MiddleRight).Build();
            AddRichPropertyRow(content, LocalizationKeys.PathInspectorFacing, facing);

            Observe(() => GetFacing(entity), newFacing =>
            {
                facingLabel.Text = $"→ {newFacing}";
            });

            // Node type (for path segments)
            if (entity is PathSegmentLogic seg)
            {
                AddRichPropertyRow(content, LocalizationKeys.PathInspectorNodeType, CapitalizeFirst(seg.NodeType.ToString()));
            }

            // Description
            string description = GetDescription(entity);
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
        }

        private static string GetFacing(StructureBase entity) => entity switch
        {
            PathSegmentLogic seg => seg.Facing.ToString(),
            RoutingNodeBase rn => rn.OutputDirection.ToString(),
            _ => "—"
        };

        private string GetDescription(StructureBase entity) => entity switch
        {
            FilterSplitterLogic => L(LocalizationKeys.PathDescFilterSplitter),
            BalancerLogic => L(LocalizationKeys.PathDescBalancer),
            CheckGateLogic => L(LocalizationKeys.PathDescCheckGate),
            PathSegmentLogic => L(LocalizationKeys.PathDescRegular),
            _ => entity.GetCategoryName()
        };
    }
}
