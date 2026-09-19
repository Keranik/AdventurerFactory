using ForgeFlow.Core.Proto.Logic;
using ForgeFlow.Core.Proto.Prototypes;
using UnityEngine;

namespace ForgeFlow.Presentation.Unity.Visuals
{
    /// <summary>
    /// Thin wrapper for TrainingBuildingLogic.
    /// </summary>
    internal class TrainingBuildingMb : EntityBaseMb<TrainingBuildingLogic>
    {
        protected override void CreateVisual()
        {
            if (Logic == null) { return; }

            Visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Visual.name = $"School_{Logic.ProtoId}";
            Visual.transform.SetParent(transform);
            Visual.transform.localPosition = Vector3.zero;
            Visual.transform.localScale = new Vector3(0.9f, 0.8f, 0.9f);

            Color color = Logic.OutputClass switch
            {
                VillagerClass.Warrior => new Color(0.7f, 0.2f, 0.2f),
                VillagerClass.Cleric => new Color(0.9f, 0.9f, 0.5f),
                VillagerClass.Mage => new Color(0.3f, 0.3f, 0.8f),
                VillagerClass.Ranger => new Color(0.2f, 0.6f, 0.2f),
                VillagerClass.Artisan => new Color(0.7f, 0.5f, 0.2f),
                _ => new Color(0.5f, 0.5f, 0.5f)
            };

            var renderer = Visual.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.material = RuntimePlaceholderFactory.CreateLitMaterial(color);
            }

            // Class banner on top
            var banner = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            banner.name = "ClassBanner";
            banner.transform.SetParent(transform);
            banner.transform.localPosition = new Vector3(0, 0.6f, 0);
            banner.transform.localScale = new Vector3(0.15f, 0.15f, 0.15f);
        }
    }
}
