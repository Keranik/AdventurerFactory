using ForgeFlow.Core.Events;
using ForgeFlow.Core.Systems;
using ForgeFlow.Presentation.Unity.Input;
using UnityEngine;

namespace ForgeFlow.Presentation.Unity.UI.Components
{
    /// <summary>
    /// Entity selection support component. All input handling is routed through
    /// <see cref="EntitySelectionController"/> in the <see cref="InputControllerStack"/>.
    /// This MonoBehaviour is retained for the <see cref="IEntityIdentifier"/> interface
    /// and as a future hook for event-driven selection state tracking.
    /// </summary>
    internal sealed class EntitySelector : MonoBehaviour
    {
        private Camera _camera = null!;
        private EventBus _eventBus = null!;
        private SimulationTicker _simulation = null!;

        public void Initialize(Camera camera, EventBus eventBus, SimulationTicker simulation)
        {
            _camera = camera;
            _eventBus = eventBus;
            _simulation = simulation;
        }
    }

    /// <summary>
    /// Interface for MonoBehaviours that wrap a Core entity,
    /// providing ID and type for the inspector.
    /// </summary>
    internal interface IEntityIdentifier
    {
        ulong EntityId { get; }
        string EntityType { get; }
        Dictionary<string, string> InspectorProperties { get; }
    }
}
