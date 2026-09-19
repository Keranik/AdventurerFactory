using ForgeFlow.Core.Proto.Logic;
using UnityEngine;

namespace ForgeFlow.Presentation.Unity.Visuals
{
    /// <summary>
    /// Thin wrapper for ForestryRecipeEntity logic. Adds forestry-specific visuals.
    /// </summary>
    internal class ForestryMb : EntityBaseMb<ForestryRecipeEntity>
    {
        private GameObject? _saplingIndicator;

        protected override void CreateVisual()
        {
            if (Logic == null) { return; }

            Visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Visual.name = $"ForestryRecipeEntity_{Logic.ProtoId}";
            Visual.transform.SetParent(transform);
            Visual.transform.localPosition = Vector3.zero;
            Visual.transform.localScale = new Vector3(0.8f, 0.7f, 0.8f);

            var renderer = Visual.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.material = RuntimePlaceholderFactory.CreateLitMaterial(new Color(0.2f, 0.5f, 0.15f));
            }

            // Sapling indicator
            _saplingIndicator = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            _saplingIndicator.name = "SaplingIndicator";
            _saplingIndicator.transform.SetParent(transform);
            _saplingIndicator.transform.localPosition = new Vector3(0, 0.5f, 0);
            _saplingIndicator.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
            _saplingIndicator.SetActive(Logic.CanPlantSaplings);
        }
    }
}
