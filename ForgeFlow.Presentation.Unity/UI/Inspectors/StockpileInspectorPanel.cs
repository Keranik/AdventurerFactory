using System.Collections.Generic;
using ForgeFlow.Core.Commands;
using ForgeFlow.Core.Data;
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
    /// Dedicated stockpile inspector panel. Shows capacity, stored items,
    /// single-product assignment with a button that opens a ProductPicker dialog.
    /// Inherits the uniform rich header / status / storage sections from
    /// <see cref="BaseStructureInspector"/>.
    ///
    /// Uses the observer pattern: the visual structure is built once in
    /// <see cref="BuildMachineSpecificContent"/> and all live data is bound
    /// via <see cref="BaseInspectorPanel.Observe{T}"/>. Per-tick updates
    /// evaluate only the bindings (zero-alloc when nothing changes). Buttons
    /// and dialogs remain stable across frames.
    /// </summary>
    internal sealed class StockpileInspectorPanel : BaseStructureInspector, ITickableWindow, IAutoRegisteredInspector
    {
        public override string WindowId => WindowIds.StockpileInspector;

        private const string PickerTrackingId = "stockpile_product_picker";

        private readonly ItemRegistry? _itemRegistry;
        private readonly ITransientElementTracker? _transientTracker;
        private ForgeProductPicker? _activePicker;
        private ForgePanel? _pickerDialog;

        public StockpileInspectorPanel(SimulationTicker? simulation, ItemRegistry? itemRegistry, ITransientElementTracker? transientTracker = null)
            : base("stockpile_inspector", LocalizationKeys.StockpileInspectorTitle, 320f, 500f, simulation)
        {
            _itemRegistry = itemRegistry;
            _transientTracker = transientTracker;
        }

        public void Tick(float deltaTime)
        {
            UpdateBindings();
        }

        public override void Hide()
        {
            ClosePickerDialog();
            base.Hide();
        }

        protected override void BuildStructureSpecificContent(VisualElement content, StructureBase entity)
        {
            if (entity is not StockpileLogic stockpile)
            {
                return;
            }

            // ── Capacity Section ──────────────────────────────────────
            AddSectionDivider(content);
            AddRichSectionHeader(content, "▸ " + L(LocalizationKeys.StockpileCapacity));

            var capacityBar = ForgeProgressBar.Create(LocalizationKeys.StockpileCapacity,
                0f, stockpile.MaxCapacity)
                .Value(stockpile.TotalStored)
                .MarginTop(4).MarginBottom(4)
                .Build();
            content.Add(capacityBar);

            var capacityKeyLabel = ForgeLabel.CreateRaw(
                L(LocalizationKeys.StockpileCapacity), ForgeLabelSize.Normal)
                .Color("text.secondary").FlexGrow(1).Build();
            var capacityValueLabel = ForgeLabel.CreateRaw(
                $"{stockpile.TotalStored} / {stockpile.MaxCapacity}", ForgeLabelSize.Normal)
                .Color("text.primary").Bold().TextAlign(TextAnchor.MiddleRight).Build();
            content.Add(ForgeContainer.Create()
                .FlexDirection(FlexDirection.Row)
                .JustifyContent(Justify.SpaceBetween)
                .PaddingTop(3).PaddingBottom(3)
                .PaddingLeft(ThemePaddingSmall).PaddingRight(ThemePaddingSmall)
                .BackgroundColor(NextRowColorKey())
                .BorderRadius(AlternatingRowRadius)
                .Child(capacityKeyLabel).Child(capacityValueLabel)
                .Build());

            Observe(() => stockpile.TotalStored, count =>
            {
                capacityBar.Value = count;
                capacityValueLabel.Text = $"{count} / {stockpile.MaxCapacity}";
            });

            // ── Stored Resources Section ──────────────────────────────
            AddSectionDivider(content);
            AddRichSectionHeader(content, "▸ " + L(LocalizationKeys.InspectorSectionStorage));

            var resourceContainer = ForgeContainer.Create()
                .FlexDirection(FlexDirection.Column)
                .Build();
            content.Add(resourceContainer);

            RebuildResourceRows(resourceContainer, stockpile);

            Observe(() => ComputeResourceFingerprint(stockpile.StoredResources), _ =>
            {
                RebuildResourceRows(resourceContainer, stockpile);
            });

            // ── Product Assignment Section ────────────────────────────
            AddSectionDivider(content);
            AddRichSectionHeader(content, "▸ " + L(LocalizationKeys.StockpileProduct));

            // "Accepts all items" label — visible when no product assigned
            var acceptsAllLabel = ForgeLabel.CreateRaw(
                L(LocalizationKeys.StockpileAcceptsAll), ForgeLabelSize.Small)
                .Color("text.secondary").PaddingLeft(ThemePaddingSmall).MarginBottom(2)
                .Build();
            content.Add(acceptsAllLabel);

            // Product display row — visible when a product is assigned
            var productSwatch = ForgeContainer.Create()
                .Width(14).Height(14)
                .MarginRight(4).FlexShrink(0)
                .BorderWidth(1).BorderRadius(2)
                .Build();
            var productNameLabel = ForgeLabel.CreateRaw("", ForgeLabelSize.Small)
                .Color("text.primary").FlexGrow(1).Build();
            var productRow = ForgeContainer.Create()
                .FlexDirection(FlexDirection.Row)
                .AlignItems(Align.Center)
                .PaddingLeft(ThemePaddingSmall).PaddingRight(ThemePaddingSmall)
                .PaddingTop(2).PaddingBottom(2)
                .BackgroundColor(NextRowColorKey())
                .BorderRadius(AlternatingRowRadius)
                .Child(productSwatch).Child(productNameLabel)
                .Build();
            content.Add(productRow);

            // Assign Product button — always present, never destroyed
            var assignButton = ForgeButton.Create(LocalizationKeys.StockpileAssignProduct, () =>
            {
                if (TryGetStructure(out var m) && m is StockpileLogic s)
                {
                    OpenPickerDialog(s);
                }
            }).MarginTop(4).Build();
            content.Add(assignButton);

            // Clear Product button — hidden when no product assigned
            var clearButton = ForgeButton.Create(LocalizationKeys.StockpileClearProduct, () =>
            {
                Simulation?.CommandBus.Dispatch(new SetStockpileFilterCommand(InspectedStructureId, null));
            }).Secondary().MarginTop(2).Build();
            content.Add(clearButton);

            // Set initial product display state
            ApplyProductState(stockpile.AcceptedItemId, acceptsAllLabel, productRow,
                productNameLabel, productSwatch, clearButton);

            // Binding: react to product assignment changes
            Observe(() => stockpile.AcceptedItemId, id =>
            {
                ApplyProductState(id, acceptsAllLabel, productRow,
                    productNameLabel, productSwatch, clearButton);
            });
        }

        // ── Product State ─────────────────────────────────────────────

        /// <summary>
        /// Applies the correct visibility and content for the product assignment
        /// section based on the current <paramref name="acceptedItemId"/>.
        /// Called both on initial build and from the observer callback.
        /// </summary>
        private void ApplyProductState(
            string? acceptedItemId,
            ForgeLabel acceptsAllLabel,
            ForgeContainer productRow,
            ForgeLabel productNameLabel,
            ForgeContainer productSwatch,
            ForgeButton clearButton)
        {
            bool hasProduct = acceptedItemId != null;
            acceptsAllLabel.style.display = hasProduct ? DisplayStyle.None : DisplayStyle.Flex;
            productRow.style.display = hasProduct ? DisplayStyle.Flex : DisplayStyle.None;
            clearButton.style.display = hasProduct ? DisplayStyle.Flex : DisplayStyle.None;

            if (hasProduct)
            {
                productNameLabel.Text = GetItemDisplayName(acceptedItemId!);
                productSwatch.style.backgroundColor = GetItemColor(acceptedItemId!);
            }
        }

        // ── Picker Dialog ─────────────────────────────────────────────

        private void OpenPickerDialog(StockpileLogic stockpile)
        {
            ClosePickerDialog();

            var scrollView = ForgeScrollView.Create()
                .FlexGrow(1).Build();

            _activePicker = ForgeProductPicker.Create(ProductPickerStyle.MediumWithText)
                .Products(BuildProductList(stockpile))
                .OnItemSelected(id =>
                {
                    Simulation?.CommandBus.Dispatch(new SetStockpileFilterCommand(InspectedStructureId, id));
                    ClosePickerDialog();
                })
                .FlexGrow(1).Build();

            scrollView.AddContent(_activePicker);

            _pickerDialog = ForgePanel.Create("product_picker_dialog",
                    LocalizationKeys.ProductPickerTitle, 320f, 400f)
                .AsTransient(_transientTracker, PickerTrackingId)
                .DockTo(Panel, DockSide.Left)
                .OnClosed(ClosePickerDialog)
                .Content(scrollView)
                .Build();

            Panel.parent?.Add(_pickerDialog);
        }

        private void ClosePickerDialog()
        {
            if (_pickerDialog != null)
            {
                _pickerDialog.parent?.Remove(_pickerDialog);
                _pickerDialog = null;
                _activePicker = null;
            }
        }

        // ── Resource Row Helpers ──────────────────────────────────────

        /// <summary>
        /// Rebuilds only the resource rows inside the given container.
        /// Called on initial build and whenever the resource fingerprint changes.
        /// </summary>
        private void RebuildResourceRows(ForgeContainer container, StockpileLogic stockpile)
        {
            container.Clear();

            if (stockpile.StoredResources.Count == 0)
            {
                container.Add(
                    ForgeLabel.CreateRaw(L(LocalizationKeys.StockpileEmpty), ForgeLabelSize.Small)
                        .Color("text.secondary").PaddingLeft(ThemePaddingSmall).MarginBottom(2)
                        .Build());
            }
            else
            {
                foreach (var kvp in stockpile.StoredResources)
                {
                    if (kvp.Value > 0)
                    {
                        AddSmallPropertyRow(container, CapitalizeFirst(kvp.Key), kvp.Value.ToString());
                    }
                }
            }
        }

        /// <summary>
        /// Computes a lightweight, order-independent fingerprint of the stored
        /// resources dictionary. Used to detect changes without full dictionary
        /// comparison each tick. Zero-alloc: iterates the dictionary struct
        /// enumerator only.
        /// </summary>
        private static int ComputeResourceFingerprint(Dictionary<string, int> resources)
        {
            unchecked
            {
                int fingerprint = resources.Count;
                foreach (var kvp in resources)
                {
                    fingerprint ^= (kvp.Key.GetHashCode() * 397) ^ kvp.Value;
                }
                return fingerprint;
            }
        }

        // ── Item Helpers ──────────────────────────────────────────────

        private List<ProductEntry> BuildProductList(StockpileLogic stockpile)
        {
            var entries = new List<ProductEntry>();
            if (_itemRegistry == null)
            {
                return entries;
            }

            foreach (var item in _itemRegistry.GetAll())
            {
                bool selected = stockpile.AcceptedItemId == item.Id;
                entries.Add(new ProductEntry(
                    item.Id,
                    item.DisplayName.Length > 0 ? item.DisplayName : item.Id,
                    GetItemColor(item.Id),
                    selected));
            }

            return entries;
        }

        private string GetItemDisplayName(string itemId)
        {
            if (_itemRegistry != null && _itemRegistry.TryGet(itemId, out var proto))
            {
                return proto.DisplayName.Length > 0 ? proto.DisplayName : itemId;
            }
            return itemId;
        }

        private static Color GetItemColor(string itemId)
        {
            // Deterministic color from item ID hash for visual distinction
            int hash = itemId.GetHashCode();
            float h = ((hash & 0x7FFFFFFF) % 360) / 360f;
            return Color.HSVToRGB(h, 0.5f, 0.7f);
        }

        public override void Dispose()
        {
            ClosePickerDialog();
            base.Dispose();
        }
    }
}
