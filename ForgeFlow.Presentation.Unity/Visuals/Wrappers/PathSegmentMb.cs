using ForgeFlow.Core.Proto.Logic;
using ForgeFlow.Core.Proto.Prototypes;
using UnityEngine;

namespace ForgeFlow.Presentation.Unity.Visuals
{
    /// <summary>
    /// Thin wrapper for PathSegmentLogic. Shows path visuals.
    /// </summary>
    internal class PathSegmentMb : EntityBaseMb<PathSegmentLogic>
    {
        protected override void CreateVisual()
        {
            if (Logic == null) { return; }

            var prefab = PrefabRegistry.GetPrefab("PathStraight");
            if (prefab != null && Logic.NodeType == PathNodeType.Straight)
            {
                Visual = UnityEngine.Object.Instantiate(prefab);
                Visual.name = $"PathSegment_{Logic.NodeType}";
                Visual.transform.SetParent(transform);
                Visual.transform.localPosition = Vector3.zero;
            }
            else
            {
                Visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Visual.name = $"PathSegment_{Logic.NodeType}";
                Visual.transform.SetParent(transform);
                Visual.transform.localPosition = Vector3.zero;
                Visual.transform.localScale = new Vector3(0.9f, 0.08f, 0.9f);

                Color color = Logic.NodeType switch
                {
                    PathNodeType.Splitter => new Color(0.6f, 0.6f, 0.2f),
                    PathNodeType.Merger => new Color(0.2f, 0.6f, 0.6f),
                    _ => new Color(0.35f, 0.35f, 0.4f)
                };

                var renderer = Visual.GetComponent<MeshRenderer>();
                if (renderer != null)
                {
                    renderer.material = RuntimePlaceholderFactory.CreateLitMaterial(color);
                }
            }
        }
    }
}
