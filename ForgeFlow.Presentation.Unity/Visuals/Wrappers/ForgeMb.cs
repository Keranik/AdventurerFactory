using ForgeFlow.Core.Proto.Logic;
using UnityEngine;

namespace ForgeFlow.Presentation.Unity.Visuals
{
    /// <summary>
    /// Thin wrapper for ForgeLogic. Shows a forge-themed placeholder visual.
    /// </summary>
    internal class ForgeMb : EntityBaseMb<ForgeLogic>
    {
        protected override void CreateVisual()
        {
            if (Logic == null) { return; }

            Visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Visual.name = $"Forge_{Logic.ProtoId}";
            Visual.transform.SetParent(transform);
            Visual.transform.localPosition = Vector3.zero;
            Visual.transform.localScale = new Vector3(0.85f, 0.75f, 0.85f);

            var renderer = Visual.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.material = RuntimePlaceholderFactory.CreateLitMaterial(new Color(0.8f, 0.35f, 0.15f));
            }
        }
    }
}
