using ForgeFlow.Core.Proto.Logic;
using UnityEngine;

namespace ForgeFlow.Presentation.Unity.Visuals
{
    /// <summary>
    /// Thin wrapper for ResourceNodeLogic.
    /// </summary>
    internal class ResourceNodeMb : EntityBaseMb<ResourceNodeLogic>
    {
        protected override void CreateVisual()
        {
            if (Logic == null) { return; }

            // Trees = cylinders, ore = cubes, herbs = spheres
            PrimitiveType shape;
            Color color;
            if (Logic.ResourceId == "wood")
            {
                shape = PrimitiveType.Cylinder;
                color = new Color(0.3f, 0.5f, 0.15f);
            }
            else if (Logic.ResourceId == "ore")
            {
                shape = PrimitiveType.Cube;
                color = new Color(0.5f, 0.45f, 0.4f);
            }
            else
            {
                shape = PrimitiveType.Sphere;
                color = new Color(0.3f, 0.7f, 0.4f);
            }

            Visual = GameObject.CreatePrimitive(shape);
            Visual.name = $"ResourceNode_{Logic.ProtoId}";
            Visual.transform.SetParent(transform);
            Visual.transform.localPosition = new Vector3(0, 0.3f, 0);
            Visual.transform.localScale = new Vector3(0.4f, 0.6f, 0.4f);

            var renderer = Visual.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.material = RuntimePlaceholderFactory.CreateLitMaterial(color);
            }
        }

        protected override void Update()
        {
            base.Update();

            // Fade when depleted
            if (Logic != null && Logic.IsDepleted && Visual != null)
            {
                Visual.transform.localScale = new Vector3(0.2f, 0.1f, 0.2f);
            }
        }
    }
}
