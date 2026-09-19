using ForgeFlow.Core.Entities;
using ForgeFlow.Core.Systems;
using UnityEngine.UIElements;

namespace ForgeFlow.Presentation.Unity.UI.Inspectors
{
    /// <summary>
    /// Intermediate base for inspectors of recipe-processing structures
    /// (CraftStation, Forge, Gathering, FusionAltar, and future recipe-driven structures).
    /// Mirrors <see cref="RecipeEntity"/> in the Logic layer.
    ///
    /// Subclasses override <see cref="BaseStructureInspector.BuildStructureSpecificContent"/>
    /// to add type-specific production/recipe UI.
    /// </summary>
    internal abstract class BaseRecipeStructureInspector : BaseStructureInspector
    {
        protected BaseRecipeStructureInspector(
            string panelId,
            string titleLocKey,
            float width,
            float height,
            SimulationTicker? simulation)
            : base(panelId, titleLocKey, width, height, simulation)
        {
        }

        /// <summary>
        /// Tries to retrieve the inspected structure as a <see cref="RecipeEntity"/>.
        /// Returns false if the structure is not a recipe-processing structure.
        /// </summary>
        protected bool TryGetRecipeStructure(out RecipeEntity recipe)
        {
            recipe = null!;
            if (TryGetStructure(out var entity) && entity is RecipeEntity r)
            {
                recipe = r;
                return true;
            }
            return false;
        }
    }
}
