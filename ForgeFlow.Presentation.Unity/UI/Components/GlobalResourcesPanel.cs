using ForgeFlow.Core.Events;
using ForgeFlow.Core.Systems;
using UnityEngine.UIElements;

namespace ForgeFlow.Presentation.Unity.UI.Components
{
    /// <summary>
    /// Self-contained top-bar component showing global resources: Gold, Wood, Ore, Food.
    /// Encapsulates its own styling, spacing, and dynamic resizing.
    /// Subscribes to <see cref="GoldChangedEvent"/> for instant gold updates
    /// and supports periodic refresh for resource stocks.
    /// </summary>
    internal sealed class GlobalResourcesPanel : ForgeStyledVisualElement
    {
        private readonly SimulationTicker _simulation;
        private readonly EventBus _eventBus;

        private readonly ForgeLabel _goldLabel;
        private readonly ForgeLabel _woodLabel;
        private readonly ForgeLabel _oreLabel;
        private readonly ForgeLabel _foodLabel;

        public GlobalResourcesPanel(SimulationTicker simulation, EventBus eventBus)
        {
            _simulation = simulation;
            _eventBus = eventBus;

            name = nameof(GlobalResourcesPanel);
            style.flexDirection = FlexDirection.Row;
            style.alignItems = Align.Center;
            style.justifyContent = Justify.FlexEnd;
            style.flexGrow = 1;
            style.flexShrink = 1;

            _goldLabel = ForgeLabel.CreateRaw("\u2b21 Gold: 0", ForgeLabelSize.Normal)
                .FontSize(14).Color("status.warning").Bold().MarginRight(16).Build();

            _woodLabel = ForgeLabel.CreateRaw("\ud83c\udf32 Wood: 0", ForgeLabelSize.Small)
                .Color("text.primary").MarginRight(12).Build();

            _oreLabel = ForgeLabel.CreateRaw("\u26cf Ore: 0", ForgeLabelSize.Small)
                .Color("text.primary").MarginRight(12).Build();

            _foodLabel = ForgeLabel.CreateRaw("\ud83c\udf5e Food: 0", ForgeLabelSize.Small)
                .Color("text.primary").Build();

            Add(_goldLabel);
            Add(_woodLabel);
            Add(_oreLabel);
            Add(_foodLabel);

            _eventBus.Subscribe<GoldChangedEvent>(OnGoldChanged);
        }

        /// <summary>Refreshes all displayed resource values from current simulation state.</summary>
        public void Refresh()
        {
            var im = _simulation.ItemManager;
            int gold = im.GetStock("gold");
            _goldLabel.SetRawText($"\u2b21 Gold: {gold}");

            _woodLabel.SetRawText($"\ud83c\udf32 Wood: {im.GetStock("wood")}");
            _oreLabel.SetRawText($"\u26cf Ore: {im.GetStock("ore")}");
            _foodLabel.SetRawText($"\ud83c\udf5e Food: {im.GetStock("food")}");
        }

        public void Dispose()
        {
            _eventBus.Unsubscribe<GoldChangedEvent>(OnGoldChanged);
        }

        public override void ApplyTheme()
        {
            _goldLabel.ApplyTheme();
            _woodLabel.ApplyTheme();
            _oreLabel.ApplyTheme();
            _foodLabel.ApplyTheme();

            // Re-apply color overrides after theme reset
            _goldLabel.style.color = C("status.warning");
        }

        private void OnGoldChanged(GoldChangedEvent e)
        {
            _goldLabel.SetRawText($"\u2b21 Gold: {e.NewAmount}");
        }
    }
}
