using ForgeFlow.Core.Proto.Logic;
using ForgeFlow.Presentation.Unity.UI.Components;
using UnityEngine;

namespace ForgeFlow.Presentation.Unity.Visuals
{
    /// <summary>
    /// Thin wrapper for VillagerLogic logic.
    /// Implements <see cref="IEntityIdentifier"/> so that clicking a villager
    /// in the world triggers the inspector via EntitySelectionController.
    /// </summary>
    internal class VillagerMb : MonoBehaviour, IEntityIdentifier
    {
        private VillagerLogic? _logic;
        private GameObject? _visual;

        public VillagerLogic? Logic => _logic;

        public ulong EntityId => _logic?.Id.Value ?? 0UL;
        public string EntityType => "Villager";
        public Dictionary<string, string> InspectorProperties => new();

        public void Initialize(VillagerLogic logic)
        {
            _logic = logic;
            CreateVisual();
        }

        private void CreateVisual()
        {
            if (_logic == null) { return; }

            _visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            _visual.name = $"Villager_{_logic.Name}";
            _visual.transform.SetParent(transform);
            _visual.transform.localPosition = new Vector3(0, 0.3f, 0);
            _visual.transform.localScale = new Vector3(0.25f, 0.25f, 0.25f);

            var renderer = _visual.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.material = RuntimePlaceholderFactory.CreateLitMaterial(new Color(0.9f, 0.75f, 0.5f));
            }

            // Job indicator sphere
            var jobIndicator = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            jobIndicator.name = "JobIndicator";
            jobIndicator.transform.SetParent(transform);
            jobIndicator.transform.localPosition = new Vector3(0, 0.7f, 0);
            jobIndicator.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
        }

        private void Update()
        {
            if (_logic == null) { return; }
            transform.position = new Vector3(_logic.Position.X, 0f, _logic.Position.Y);
        }
    }
}
