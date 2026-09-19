using ForgeFlow.Core.Entities;
using ForgeFlow.Core.Proto.Logic;
using ForgeFlow.Presentation.Unity.UI.Components;
using UnityEngine;

namespace ForgeFlow.Presentation.Unity.Visuals
{
    /// <summary>
    /// Thin wrapper for PathGateLogic. Shows a flat arrow visual that
    /// points in the gate's facing direction. Color indicates
    /// Entrance (green) vs Exit (orange).
    /// Implements <see cref="IEntityIdentifier"/> so clicking a PathGate
    /// triggers the inspector via EntitySelectionController raycast.
    /// </summary>
    internal class PathGateMb : MonoBehaviour, IEntityIdentifier
    {
        private PathGateLogic? _logic;
        private GameObject? _arrowObj;
        private Material? _arrowMaterial;
        private static readonly Dictionary<string, string> EmptyProperties = new();

        public PathGateLogic? Logic => _logic;

        /// <inheritdoc />
        public ulong EntityId => _logic?.Id.Value ?? 0UL;

        /// <inheritdoc />
        public string EntityType => "PathGate";

        /// <inheritdoc />
        public Dictionary<string, string> InspectorProperties => EmptyProperties;

        public void Initialize(PathGateLogic logic)
        {
            _logic = logic;
            CreateVisual();
        }

        private void CreateVisual()
        {
            if (_logic == null) { return; }

            var prefab = PrefabRegistry.GetPrefab("PathGate");
            if (prefab != null)
            {
                _arrowObj = UnityEngine.Object.Instantiate(prefab);
                _arrowObj.name = _logic.IsEntrance ? "PathGate_Entrance" : "PathGate_Exit";
                _arrowObj.transform.SetParent(transform);
                _arrowObj.transform.localPosition = new Vector3(0, 0.05f, 0);

                // Tint all renderers to indicate entrance (green) vs exit (orange)
                var tint = _logic.IsEntrance
                    ? new Color(0.4f, 1f, 0.5f)
                    : new Color(1f, 0.6f, 0.2f);
                foreach (var renderer in _arrowObj.GetComponentsInChildren<MeshRenderer>())
                {
                    if (renderer != null)
                    {
                        renderer.material.color = tint;
                    }
                }

                // Cache first renderer material for potential runtime updates
                var tileRenderer = _arrowObj.GetComponentInChildren<MeshRenderer>();
                if (tileRenderer != null)
                {
                    _arrowMaterial = tileRenderer.material;
                }
            }
            else
            {
                // Fallback: runtime primitives
                _arrowObj = RuntimePlaceholderFactory.CreatePathGateVisual(_logic.IsEntrance);
                _arrowObj.transform.SetParent(transform);
                _arrowObj.transform.localPosition = new Vector3(0, 0.05f, 0);

                var tileRenderer = _arrowObj.GetComponentInChildren<MeshRenderer>();
                if (tileRenderer != null)
                {
                    _arrowMaterial = tileRenderer.material;
                }
            }

            ApplyFacingRotation();
        }

        private void ApplyFacingRotation()
        {
            if (_arrowObj == null || _logic == null) { return; }

            float yaw = _logic.Facing switch
            {
                Direction.North => 0f,
                Direction.East => 90f,
                Direction.South => 180f,
                Direction.West => 270f,
                _ => 0f
            };
            _arrowObj.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
        }

        private void Update()
        {
            if (_logic == null) { return; }
            transform.position = new Vector3(_logic.Position.X, 0f, _logic.Position.Y);

            // Sync arrow rotation from Core facing
            ApplyFacingRotation();
        }
    }
}
