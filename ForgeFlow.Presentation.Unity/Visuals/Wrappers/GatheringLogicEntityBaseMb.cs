using ForgeFlow.Core.Proto.Logic;
using UnityEngine;

namespace ForgeFlow.Presentation.Unity.Visuals
{
    /// <summary>
    /// Thin Unity MonoBehaviour wrapper for a GatheringLogicBase logic instance.
    /// Holds the visual GameObject and links it to the headless Core logic.
    /// </summary>
    internal class GatheringLogicEntityBaseMb : EntityBaseMb<GatheringLogicBase>
    {
        protected override void CreateVisual()
        {
            if (Logic == null) { return; }

            Visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Visual.name = $"GatheringRecipeEntity_{Logic.ProtoId}";
            Visual.transform.SetParent(transform);
            Visual.transform.localPosition = Vector3.zero;
            Visual.transform.localScale = new Vector3(0.8f, 0.6f, 0.8f);

            var renderer = Visual.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.material = RuntimePlaceholderFactory.CreateLitMaterial(new Color(0.4f, 0.6f, 0.3f));
            }
        }
    }
}
