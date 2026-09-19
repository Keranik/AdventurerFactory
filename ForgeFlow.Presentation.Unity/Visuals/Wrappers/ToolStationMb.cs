using ForgeFlow.Core.Proto.Logic;
using UnityEngine;

namespace ForgeFlow.Presentation.Unity.Visuals
{
    /// <summary>
    /// Thin wrapper for ToolStationLogic. Shows a tool-station placeholder visual.
    /// </summary>
    internal class ToolStationMb : EntityBaseMb<ToolStationLogic>
    {
        protected override void CreateVisual()
        {
            if (Logic == null) { return; }

            Visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Visual.name = $"ToolStation_{Logic.ProtoId}";
            Visual.transform.SetParent(transform);
            Visual.transform.localPosition = Vector3.zero;
            Visual.transform.localScale = new Vector3(0.8f, 0.6f, 0.8f);

            var renderer = Visual.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.material = RuntimePlaceholderFactory.CreateLitMaterial(new Color(0.6f, 0.6f, 0.5f));
            }
        }
    }
}
