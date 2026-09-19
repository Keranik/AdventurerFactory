using ForgeFlow.Core.Entities;
using ForgeFlow.Core.Proto.Logic;
using UnityEngine;

namespace ForgeFlow.Presentation.Unity.Visuals
{
    /// <summary>
    /// Thin wrapper for FilterSplitterLogic. Uses the PathT prefab to show a
    /// T-junction visual. Falls back to runtime primitives when the prefab is
    /// not loaded. Rotation is handled by <see cref="PathRendererSystem"/> which
    /// syncs the parent GO rotation from OutputDirection each frame.
    /// </summary>
    internal class FilterSplitterMb : EntityBaseMb<FilterSplitterLogic>
    {
        protected override void CreateVisual()
        {
            if (Logic == null) { return; }

            var prefab = PrefabRegistry.GetPrefab("PathT");
            if (prefab != null)
            {
                Visual = UnityEngine.Object.Instantiate(prefab);
                Visual.name = $"FilterSplitter_{Logic.ProtoId}";
                Visual.transform.SetParent(transform);
                Visual.transform.localPosition = Vector3.zero;

                // Ensure there is a collider so raycasts can hit the visual
                // and find IEntityIdentifier via GetComponentInParent.
                if (Visual.GetComponentInChildren<Collider>() == null)
                {
                    var col = Visual.AddComponent<BoxCollider>();
                    col.size = new Vector3(0.9f, 0.1f, 0.9f);
                }
            }
            else
            {
                // Fallback: runtime primitives
                Visual = new GameObject($"FilterSplitter_{Logic.ProtoId}");
                Visual.transform.SetParent(transform);
                Visual.transform.localPosition = Vector3.zero;

                var baseMat = RuntimePlaceholderFactory.CreateLitMaterial(new Color(0.5f, 0.7f, 0.3f));
                var chevronMat = RuntimePlaceholderFactory.CreateLitMaterial(new Color(0.85f, 0.9f, 0.45f));

                var slab = GameObject.CreatePrimitive(PrimitiveType.Cube);
                slab.name = "Slab";
                slab.transform.SetParent(Visual.transform);
                slab.transform.localPosition = Vector3.zero;
                slab.transform.localScale = new Vector3(0.9f, 0.08f, 0.9f);
                SetMaterial(slab, baseMat);

                CreateChevron(Visual.transform, chevronMat, new Vector3(0f, 0.06f, 0.2f), 0f);
                CreateChevron(Visual.transform, chevronMat, new Vector3(-0.2f, 0.06f, 0f), 90f);
                CreateChevron(Visual.transform, chevronMat, new Vector3(0.2f, 0.06f, 0f), -90f);
            }

            // Rotation is driven event-side by PathRendererSystem.OnEntityRotated
            // (subscribes to EntityRotatedEvent). The per-frame Update() below is a
            // defensive mirror in PathGateMb's style — it ensures the visual matches
            // Logic.OutputDirection even if the rotation event was missed (race during
            // load/save, late subscription, etc.). Cheap: 4 enum branches per frame.
            ApplyFacingRotation();
        }

        private void ApplyFacingRotation()
        {
            if (Logic == null || Visual == null) { return; }
            float yaw = Logic.OutputDirection switch
            {
                ForgeFlow.Core.Entities.Direction.North => 0f,
                ForgeFlow.Core.Entities.Direction.East => 90f,
                ForgeFlow.Core.Entities.Direction.South => 180f,
                ForgeFlow.Core.Entities.Direction.West => 270f,
                _ => 0f
            };
            transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
        }

        protected override void Update()
        {
            base.Update();
            ApplyFacingRotation();
        }

        private static void CreateChevron(Transform parent, Material mat, Vector3 localPos, float yRotation)
        {
            var chevron = GameObject.CreatePrimitive(PrimitiveType.Cube);
            chevron.name = "Chevron";
            chevron.transform.SetParent(parent);
            chevron.transform.localPosition = localPos;
            chevron.transform.localRotation = Quaternion.Euler(0f, yRotation + 45f, 0f);
            chevron.transform.localScale = new Vector3(0.12f, 0.04f, 0.12f);
            SetMaterial(chevron, mat);
        }

        private static void SetMaterial(GameObject go, Material mat)
        {
            var renderer = go.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.material = mat;
            }
        }
    }
}
