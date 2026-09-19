using ForgeFlow.Core.Entities;
using ForgeFlow.Presentation.Unity.UI.Components;
using UnityEngine;

namespace ForgeFlow.Presentation.Unity.Visuals
{
    /// <summary>
    /// Generic abstract base for all entity Mb wrappers.
    /// Handles the common pattern: store a Logic reference, create visuals,
    /// and sync Unity transform position from Core entity position each frame.
    /// Implements <see cref="IEntityIdentifier"/> so that clicking any entity
    /// in the world triggers the inspector via EntitySelectionController.
    /// </summary>
    internal abstract class EntityBaseMb<TLogic> : MonoBehaviour, IEntityIdentifier where TLogic : EntityBase
    {
        private TLogic? _logic;
        private GameObject? _visual;
        private static readonly Dictionary<string, string> EmptyProperties = new();
        private GridPosRPG _lastSyncedPosition;

        public TLogic? Logic => _logic;

        /// <inheritdoc />
        public ulong EntityId => _logic?.Id.Value ?? 0UL;

        /// <inheritdoc />
        public string EntityType => _logic is StructureBase sb ? sb.GetCategoryName() : typeof(TLogic).Name;

        /// <inheritdoc />
        public Dictionary<string, string> InspectorProperties => EmptyProperties;

        protected GameObject? Visual
        {
            get => _visual;
            set => _visual = value;
        }

        public void Initialize(TLogic logic)
        {
            _logic = logic;
            CreateVisual();
        }

        protected abstract void CreateVisual();

        protected virtual void Update()
        {
            if (_logic == null) { return; }
            if (_logic.Position.Equals(_lastSyncedPosition)) { return; }
            _lastSyncedPosition = _logic.Position;
            transform.position = new Vector3(_logic.Position.X, transform.position.y, _logic.Position.Y);
        }
    }
}
