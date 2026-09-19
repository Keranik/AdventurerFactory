using ForgeFlow.Core.Proto;
using ForgeFlow.Core.Proto.Prototypes;
using UnityEngine;

namespace ForgeFlow.Presentation.Unity.Visuals
{
    /// <summary>
    /// Thin wrapper for TerrainCell visuals.
    /// </summary>
    internal class TerrainCellMb : MonoBehaviour
    {
        private TerrainCell? _cell;
        private GameObject? _visual;

        public TerrainCell? Cell => _cell;

        public void Initialize(TerrainCell cell, TerrainBiomeProto? biomeProto = null)
        {
            _cell = cell;
            CreateVisual(biomeProto);
        }

        /// <summary>
        /// Re-reads the underlying <see cref="TerrainCell.Biome"/> and updates the
        /// visual color and object name to match. Zero-alloc aside from the material
        /// swap (unavoidable for color change). Called by <see cref="TerrainVisualManager.RefreshCell"/>.
        /// </summary>
        public void RefreshVisual(TerrainBiomeProto? biomeProto = null)
        {
            if (_cell == null || _visual == null) { return; }

            _visual.name = $"Terrain_{_cell.Biome}";

            var color = GetBiomeColor(_cell.Biome, biomeProto);
            var renderer = _visual.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.material.color = color;
            }
        }

        private void CreateVisual(TerrainBiomeProto? biomeProto)
        {
            if (_cell == null) { return; }

            // Quad is a single upward-facing plane — no side walls means
            // no z-fighting between adjacent tiles.
            _visual = GameObject.CreatePrimitive(PrimitiveType.Quad);
            _visual.name = $"Terrain_{_cell.Biome}";
            _visual.transform.SetParent(transform);
            _visual.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            _visual.transform.localPosition = Vector3.zero;
            _visual.transform.localScale = Vector3.one;

            var color = GetBiomeColor(_cell.Biome, biomeProto);

            // All terrain sits at y = 0 so paths/structures always render on top
            transform.position = new Vector3(_cell.Position.X, 0f, _cell.Position.Y);

            var renderer = _visual.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.material = RuntimePlaceholderFactory.CreateLitMaterial(color);
            }
        }

        private static Color GetBiomeColor(BiomeType biome, TerrainBiomeProto? biomeProto)
        {
            if (biomeProto != null)
            {
                return new Color(biomeProto.ColorR, biomeProto.ColorG, biomeProto.ColorB);
            }

            return biome switch
            {
                BiomeType.Forest => new Color(0.18f, 0.42f, 0.12f),
                BiomeType.Mountain => new Color(0.55f, 0.50f, 0.45f),
                BiomeType.Hills => new Color(0.55f, 0.60f, 0.35f),
                BiomeType.Swamp => new Color(0.30f, 0.38f, 0.22f),
                BiomeType.Desert => new Color(0.82f, 0.72f, 0.45f),
                BiomeType.Tundra => new Color(0.72f, 0.78f, 0.82f),
                BiomeType.Volcanic => new Color(0.35f, 0.18f, 0.12f),
                _ => new Color(0.49f, 0.73f, 0.37f)
            };
        }
    }
}
