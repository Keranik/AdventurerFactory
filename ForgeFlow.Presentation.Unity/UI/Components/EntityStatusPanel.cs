using ForgeFlow.Core.Entities;
using ForgeFlow.Core.Systems;
using UnityEngine.UIElements;

namespace ForgeFlow.Presentation.Unity.UI.Components
{
    /// <summary>
    /// Self-contained top-bar component showing entity status:
    /// Villager count, Hero count, Building count / cap, and worn-out worker count.
    /// Encapsulates its own styling, spacing, and dynamic resizing.
    /// Supports periodic refresh from <see cref="SimulationTicker"/> data.
    /// </summary>
    internal sealed class EntityStatusPanel : ForgeStyledVisualElement
    {
        private readonly SimulationTicker _simulation;

        private readonly ForgeLabel _villagerLabel;
        private readonly ForgeLabel _heroLabel;
        private readonly ForgeLabel _buildingLabel;
        private readonly ForgeLabel _wornOutLabel;

        public EntityStatusPanel(SimulationTicker simulation)
        {
            _simulation = simulation;

            name = nameof(EntityStatusPanel);
            style.flexDirection = FlexDirection.Row;
            style.alignItems = Align.Center;
            style.justifyContent = Justify.FlexStart;
            style.flexGrow = 1;
            style.flexShrink = 1;

            _villagerLabel = ForgeLabel.CreateRaw("\u263a Villagers: 0", ForgeLabelSize.Small)
                .Color("status.info").MarginRight(12).Build();

            _heroLabel = ForgeLabel.CreateRaw("\u2694 Heroes: 0", ForgeLabelSize.Small)
                .Color("text.accent").MarginRight(12).Build();

            _buildingLabel = ForgeLabel.CreateRaw("\ud83c\udfe0 Buildings: 0", ForgeLabelSize.Small)
                .Color("text.primary").MarginRight(12).Build();

            _wornOutLabel = ForgeLabel.CreateRaw("\u26a0 Worn Out: 0", ForgeLabelSize.Small)
                .Color("status.error").Build();

            Add(_villagerLabel);
            Add(_heroLabel);
            Add(_buildingLabel);
            Add(_wornOutLabel);
        }

        /// <summary>Refreshes all displayed entity counts from current simulation state.</summary>
        public void Refresh()
        {
            int villagerCount = _simulation.VillagerSystem.Villagers.Count;
            int heroCount = _simulation.EntityManager.Heroes.Count;
            int structureCount = _simulation.EntityManager.Structures.Count;

            int currentTier = _simulation.ResearchManager.CurrentTier;
            int maxBuildings = _simulation.Gating.GetMaxBuildings(currentTier);

            int wornOutCount = 0;
            var heroes = _simulation.EntityManager.Heroes;
            for (int i = 0; i < heroes.Count; i++)
            {
                if (heroes[i].State == HeroState.WornOut || heroes[i].State == HeroState.ReturningToMaintenance)
                {
                    wornOutCount++;
                }
            }

            _villagerLabel.SetRawText($"\u263a Villagers: {villagerCount}");
            _heroLabel.SetRawText($"\u2694 Heroes: {heroCount}");
            _buildingLabel.SetRawText($"\ud83c\udfe0 Buildings: {structureCount}/{maxBuildings}");
            _wornOutLabel.SetRawText($"\u26a0 Worn Out: {wornOutCount}");
        }

        public override void ApplyTheme()
        {
            _villagerLabel.ApplyTheme();
            _heroLabel.ApplyTheme();
            _buildingLabel.ApplyTheme();
            _wornOutLabel.ApplyTheme();

            // Re-apply color overrides after theme reset
            _villagerLabel.style.color = C("status.info");
            _heroLabel.style.color = C("text.accent");
            _wornOutLabel.style.color = C("status.error");
        }
    }
}
