using ForgeFlow.Core.Entities;
using ForgeFlow.Core.Localization;
using ForgeFlow.Core.Proto;
using ForgeFlow.Core.Proto.Logic;
using ForgeFlow.Core.Systems;
using ForgeFlow.Presentation.Unity.UI.Components;
using UnityEngine;
using UnityEngine.UIElements;

namespace ForgeFlow.Presentation.Unity.UI.Inspectors
{
    /// <summary>
    /// Inspector panel for Dungeon Portal structures. Shows dungeon ID, survival chance,
    /// gold reward, run duration, occupancy bar, and per-occupant progress.
    ///
    /// Uses the observer pattern: UI is built once, dynamic data bound
    /// via <see cref="BaseInspectorPanel.Observe{T}"/>.
    /// </summary>
    internal sealed class DungeonPortalInspectorPanel : BaseActivityStructureInspector, ITickableWindow, IAutoRegisteredInspector
    {
        public override string WindowId => WindowIds.DungeonPortalInspector;

        public DungeonPortalInspectorPanel(SimulationTicker? simulation)
            : base("dungeon_portal_inspector", LocalizationKeys.DungeonPortalInspectorTitle, 320f, 400f, simulation)
        {
        }

        public void Tick(float deltaTime)
        {
            UpdateBindings();
        }

        protected override void BuildStructureSpecificContent(VisualElement content, StructureBase entity)
        {
            if (entity is not DungeonPortalLogic dungeon)
            {
                return;
            }

            // ── Dungeon Info Section ──
            AddSectionDivider(content);
            AddRichSectionHeader(content, "▸ " + L(LocalizationKeys.DungeonPortalSectionDungeon));

            AddRichPropertyRow(content, LocalizationKeys.DungeonPortalDungeonId, dungeon.DungeonId);
            AddRichPropertyRow(content, LocalizationKeys.DungeonPortalSurvivalChance,
                $"{dungeon.BaseSurvivalChance * 100f:F0}%");
            AddRichPropertyRow(content, LocalizationKeys.DungeonPortalGoldReward,
                dungeon.BaseGoldReward.ToString());
            AddRichPropertyRow(content, LocalizationKeys.DungeonPortalRunDuration,
                $"{dungeon.RunDuration:F1}s");

            // ── Occupancy Section ──
            AddSectionDivider(content);
            AddRichSectionHeader(content, "▸ " + L(LocalizationKeys.InspectorSectionProduction));

            var controls = AddOccupancySection(content,
                LocalizationKeys.InspectorOccupants,
                dungeon.CurrentOccupants.Count,
                dungeon.MaxOccupants,
                dungeon.CanAcceptVillager);

            Observe(() => dungeon.CurrentOccupants.Count, count =>
            {
                controls.ValueLabel.Text = $"{count} / {dungeon.MaxOccupants}";
                controls.Bar.Value = count;
            });

            Observe(() => dungeon.CanAcceptVillager, canAccept =>
            {
                controls.FullWarning.style.display = canAccept ? DisplayStyle.None : DisplayStyle.Flex;
            });
        }
    }
}
