using ForgeFlow.Core.Entities;
using ForgeFlow.Core.Localization;
using ForgeFlow.Core.Proto;
using ForgeFlow.Core.Systems;
using UnityEngine.UIElements;

namespace ForgeFlow.Presentation.Unity.UI.Inspectors
{
    /// <summary>
    /// Fallback inspector panel for structure types that don't have a dedicated inspector
    /// (e.g. DungeonPortal, FusionAltar, AppearanceWorkshop, ToolStation, Armory, etc.).
    /// Shows only the common structure header, status, and storage/queue sections.
    ///
    /// Uses the observer pattern via base class bindings for IsActive and queue counts.
    /// </summary>
    internal sealed class GenericStructureInspectorPanel : BaseStructureInspector, ITickableWindow, IAutoRegisteredInspector
    {
        public override string WindowId => WindowIds.StructureInspector;

        public GenericStructureInspectorPanel(SimulationTicker? simulation)
            : base("generic_structure_inspector", LocalizationKeys.InspectorTitle, 320f, 300f, simulation)
        {
        }

        public void Tick(float deltaTime)
        {
            UpdateBindings();
        }

        protected override void BuildStructureSpecificContent(VisualElement content, StructureBase entity)
        {
            // No type-specific content — the base class already shows header, status, and storage.
        }
    }
}
