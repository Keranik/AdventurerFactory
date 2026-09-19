using ForgeFlow.Core.Events;
using ForgeFlow.Core.Localization;
using ForgeFlow.Core.Systems;
using ForgeFlow.Presentation.Unity.UI.Components;
using UnityEngine.UIElements;

namespace ForgeFlow.Presentation.Unity.UI
{
    /// <summary>
    /// Resource panel. Shows all resource stocks in a compact HUD.
    /// Positioned top-left. Composes <see cref="ForgePanel"/> for a draggable card.
    /// Event-driven: labels are built once and updated in-place when resource
    /// stock changes are notified via <see cref="GoldChangedEvent"/>,
    /// <see cref="ResourceProducedEvent"/>, or <see cref="ItemCraftedEvent"/>.
    /// </summary>
    internal sealed class ResourcePanel : IUIWindow
    {
        public string WindowId => WindowIds.ResourcePanel;

        private readonly ForgePanel _panel;
        private readonly SimulationTicker? _simulation;
        private readonly EventBus? _eventBus;
        private bool _isVisible;

        private static readonly string[] DisplayResources = { "gold", "wood", "ore", "food", "knowledge", "materials", "herbs" };
        private static readonly string[] DisplayKeys = { LocalizationKeys.ResourcesGold, LocalizationKeys.ResourcesWood, LocalizationKeys.ResourcesOre, LocalizationKeys.ResourcesFood, LocalizationKeys.ResourcesKnowledge, LocalizationKeys.ResourcesMaterials, LocalizationKeys.ResourcesHerbs };

        // Cached label references — keyed by resource ID for O(1) targeted updates.
        private readonly ForgeLabel[] _resourceLabels = new ForgeLabel[DisplayResources.Length];

        public VisualElement Root => _panel;
        public bool IsVisible => _isVisible;

        public ResourcePanel(SimulationTicker? simulation, EventBus? eventBus)
        {
            _simulation = simulation;
            _eventBus = eventBus;
            _panel = new ForgePanel("resource_panel", LocalizationKeys.ResourcesTitle, 200f, 160f);
            _panel.SetPosition(10f, 10f);
            _panel.style.height = StyleKeyword.Auto;
            _panel.Closed += Hide;

            BuildLabels();
            SubscribeEvents();
        }

        public void Show(object? context = null)
        {
            _isVisible = true;
            _panel.Show();
        }

        public void Hide()
        {
            _isVisible = false;
            _panel.Hide();
        }

        /// <summary>
        /// Forces a full refresh of all resource labels. Prefer letting events
        /// drive incremental updates; call this only when the stock state may
        /// have changed outside the event flow (e.g. after save/load).
        /// </summary>
        public void Refresh()
        {
            if (_simulation == null) { return; }

            for (int i = 0; i < DisplayResources.Length; i++)
            {
                int amount = _simulation.ItemManager.GetStock(DisplayResources[i]);
                SetLabelText(i, amount);
            }
        }

        public void ApplyTheme()
        {
            _panel.ApplyTheme();
        }

        public void Dispose()
        {
            UnsubscribeEvents();
        }

        // ── Private helpers ──────────────────────────────────────────

        private void BuildLabels()
        {
            var contentContainer = new ForgeContainer();
            _panel.ContentContainer.Add(contentContainer);

            for (int i = 0; i < DisplayResources.Length; i++)
            {
                int amount = _simulation?.ItemManager.GetStock(DisplayResources[i]) ?? 0;
                var label = ForgeLabel.CreateRaw(
                    FormatRow(DisplayKeys[i], amount),
                    ForgeLabelSize.Small)
                    .MarginBottom(2).Build();
                _resourceLabels[i] = label;
                contentContainer.Add(label);
            }
        }

        private void SubscribeEvents()
        {
            if (_eventBus == null) { return; }
            _eventBus.Subscribe<GoldChangedEvent>(OnGoldChanged);
            _eventBus.Subscribe<ResourceProducedEvent>(OnResourceProduced);
            _eventBus.Subscribe<ItemCraftedEvent>(OnItemCrafted);
        }

        private void UnsubscribeEvents()
        {
            if (_eventBus == null) { return; }
            _eventBus.Unsubscribe<GoldChangedEvent>(OnGoldChanged);
            _eventBus.Unsubscribe<ResourceProducedEvent>(OnResourceProduced);
            _eventBus.Unsubscribe<ItemCraftedEvent>(OnItemCrafted);
        }

        private void OnGoldChanged(GoldChangedEvent e)
        {
            UpdateResourceLabel("gold");
        }

        private void OnResourceProduced(ResourceProducedEvent e)
        {
            UpdateResourceLabel(e.ResourceId);
        }

        private void OnItemCrafted(ItemCraftedEvent e)
        {
            // Crafting may consume inputs and produce outputs — refresh all stocks.
            Refresh();
        }

        private void UpdateResourceLabel(string resourceId)
        {
            if (_simulation == null) { return; }

            for (int i = 0; i < DisplayResources.Length; i++)
            {
                if (DisplayResources[i] == resourceId)
                {
                    int amount = _simulation.ItemManager.GetStock(resourceId);
                    SetLabelText(i, amount);
                    return;
                }
            }
        }

        private void SetLabelText(int index, int amount)
        {
            _resourceLabels[index].SetRawText(FormatRow(DisplayKeys[index], amount));
        }

        private static string FormatRow(string locKey, int amount)
        {
            return $"{ForgeStyledVisualElement.GetLocalizedText(locKey)}: {amount}";
        }
    }
}
