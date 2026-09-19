using ForgeFlow.Core.Entities;
using ForgeFlow.Core.Localization;
using ForgeFlow.Core.Proto;
using ForgeFlow.Core.Systems;
using ForgeFlow.Presentation.Unity.UI.Components;
using UnityEngine;
using UnityEngine.UIElements;
using EntityId = ForgeFlow.Core.Entities.EntityId;

namespace ForgeFlow.Presentation.Unity.UI.Inspectors
{
    /// <summary>
    /// Base class for all structure-type inspector panels. Provides the common
    /// structure header (icon + name + badges), status section (active, position, tier),
    /// and storage/queue section. Subclasses override <see cref="BuildStructureSpecificContent"/>
    /// to add type-specific UI.
    ///
    /// Uses the observer pattern: UI is built once and all live data is bound
    /// via <see cref="BaseInspectorPanel.Observe{T}"/>. Subclass Tick methods
    /// should call <see cref="BaseInspectorPanel.UpdateBindings"/> instead of Refresh.
    /// </summary>
    internal abstract class BaseStructureInspector : BaseEntityInspector
    {
        protected ulong InspectedStructureId;

        protected BaseStructureInspector(
            string panelId,
            string titleLocKey,
            float width,
            float height,
            SimulationTicker? simulation)
            : base(panelId, titleLocKey, width, height, simulation)
        {
        }

        /// <summary>Sets the structure to inspect and refreshes the panel.</summary>
        public void InspectStructure(EntityId structureId)
        {
            InspectedStructureId = structureId;
            Refresh();
        }

        protected override void BuildContent()
        {
            ResetRowIndex();

            if (!TryGetStructure(out var structure))
            {
                return;
            }

            var content = ForgeContainer.Create()
                .FlexDirection(FlexDirection.Column)
                .Build();

            int tier = structure is Structure fp ? fp.Tier : 0;

            // ── Header (static) ──
            BuildEntityHeader(
                content,
                GetStructureIcon(structure.GetCategoryName()),
                GetStructureDisplayName(structure.GetCategoryName()),
                GetStructureCategory(structure.GetCategoryName()),
                GetStructureCategoryColor(structure.GetCategoryName()),
                tier,
                structure.Id);

            // ── Status Section ──
            AddSectionDivider(content);
            AddRichSectionHeader(content, "▸ " + L(LocalizationKeys.InspectorSectionStatus));

            var activeValueLabel = ForgeLabel.CreateRaw(
                structure.IsActive ? "● Active" : "○ Inactive", ForgeLabelSize.Normal)
                .Color(structure.IsActive ? "status.success" : "text.disabled")
                .Bold().TextAlign(TextAnchor.MiddleRight).Build();
            var activeKeyLabel = ForgeLabel.CreateRaw(
                L(LocalizationKeys.InspectorActive), ForgeLabelSize.Normal)
                .Color("text.secondary").FlexGrow(1).Build();
            content.Add(ForgeContainer.Create()
                .FlexDirection(FlexDirection.Row)
                .JustifyContent(Justify.SpaceBetween)
                .PaddingTop(3).PaddingBottom(3)
                .PaddingLeft(ThemePaddingSmall).PaddingRight(ThemePaddingSmall)
                .BackgroundColor(NextRowColorKey())
                .BorderRadius(AlternatingRowRadius)
                .Child(activeKeyLabel).Child(activeValueLabel)
                .Build());

            Observe(() => structure.IsActive, active =>
            {
                activeValueLabel.Text = active ? "● Active" : "○ Inactive";
                activeValueLabel.ColorKey = active ? "status.success" : "text.disabled";
            });

            AddRichPropertyRow(content, LocalizationKeys.InspectorPosition, $"({structure.Position.X}, {structure.Position.Y})");
            if (tier > 0)
            {
                AddRichPropertyRow(content, LocalizationKeys.InspectorTier, $"T{tier}");
            }

            // ── Type-Specific Content ──
            BuildStructureSpecificContent(content, structure);

            // ── Storage / Queue Section (common) ──
            var storageSection = ForgeContainer.Create()
                .FlexDirection(FlexDirection.Column)
                .Build();
            content.Add(storageSection);

            BuildStorageSection(storageSection, structure);
            Observe(() => ComputeQueueFingerprint(structure), _ =>
            {
                BuildStorageSection(storageSection, structure);
            });

            ContentContainer.Add(content);
        }

        /// <summary>Override to populate type-specific content (production stats, workers, etc.).</summary>
        protected abstract void BuildStructureSpecificContent(VisualElement content, StructureBase entity);

        /// <summary>Tries to retrieve the inspected entity from the EntityManager (structures or routing nodes).</summary>
        protected bool TryGetStructure(out StructureBase entity)
        {
            entity = null!;
            if (Simulation?.EntityManager.Structures.TryGetValue(new EntityId(InspectedStructureId), out var m) == true)
            {
                entity = m;
                return true;
            }
            var node = Simulation?.EntityManager.GetRoutingNode(new EntityId(InspectedStructureId));
            if (node != null)
            {
                entity = node;
                return true;
            }
            var pathSeg = Simulation?.EntityManager.GetPathSegment(new EntityId(InspectedStructureId));
            if (pathSeg != null)
            {
                entity = pathSeg;
                return true;
            }
            return false;
        }

        private void BuildStorageSection(ForgeContainer container, StructureBase entity)
        {
            container.Clear();
            if (entity is not RecipeEntity recipe)
            {
                return;
            }

            bool hasQueues = recipe.OutputQueue.Count > 0 || recipe.InputQueue.Count > 0 || recipe.MaxOutputQueueSize > 0;
            if (!hasQueues)
            {
                return;
            }

            AddSectionDivider(container);
            AddRichSectionHeader(container, "▸ " + L(LocalizationKeys.InspectorSectionStorage));
            if (recipe.InputQueue.Count > 0)
            {
                AddRichPropertyRow(container, LocalizationKeys.InspectorInputQueue, recipe.InputQueue.Count.ToString());
            }
            AddRichPropertyRow(container, LocalizationKeys.InspectorOutputQueue,
                $"{recipe.OutputQueue.Count} / {recipe.MaxOutputQueueSize}");
            AddRichPropertyRow(container, LocalizationKeys.InspectorOutputDirection,
                $"→ {recipe.OutputDirection}");
        }

        private static int ComputeQueueFingerprint(StructureBase entity)
        {
            if (entity is not RecipeEntity recipe)
            {
                return 0;
            }
            unchecked
            {
                return (recipe.InputQueue.Count * 397) ^ (recipe.OutputQueue.Count * 31) ^ recipe.MaxOutputQueueSize;
            }
        }
    }
}
