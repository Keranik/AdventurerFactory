using ForgeFlow.Core.Entities;
using ForgeFlow.Core.Localization;
using ForgeFlow.Core.Systems;
using ForgeFlow.Presentation.Unity.UI.Components;
using UnityEngine;
using UnityEngine.UIElements;

namespace ForgeFlow.Presentation.Unity.UI
{

/// <summary>
/// Bottom hotbar with customizable slots (like Factorio). Each slot
/// can hold a structure or path action. Click a filled slot
/// to activate it; click an empty slot to open the picker popup.
/// Starts with tutorial-relevant items and grows with progression.
/// Implements <see cref="IUIWindow"/> directly with a themed root element.
/// </summary>
internal sealed class GameplayToolbar : IUIWindow
{
    public string WindowId => WindowIds.GameplayToolbar;
    public event Action<string>? OnStructureSelected;
    public event Action? OnDrawPathMode;
    public event Action? OnDemolishMode;

    private readonly SimulationTicker? _simulation;
    private readonly HotbarSystem _hotbar;
    private readonly ForgeContainer _root;
    private readonly ForgeContainer _slotRow;
    private readonly ForgeButton[] _slotButtons = new ForgeButton[HotbarSystem.SlotCount];

    // Active slot tracking for highlight + toggle behavior
    private int _activeSlotIndex = -1;

    // Picker popup state
    private ForgeContainer? _pickerPopup;
    private int _pickerSlotIndex = -1;
    private bool _isVisible;

    public VisualElement Root => _root;
    public VisualElement ToolbarRoot => _root;
    public bool IsVisible => _isVisible;

    public GameplayToolbar(SimulationTicker simulation, HotbarSystem hotbar)
    {
        _simulation = simulation;
        _hotbar = hotbar;

        _root = ForgeContainer.Create()
            .Name(nameof(GameplayToolbar))
            .Build();

        _slotRow = ForgeContainer.Create()
            .FlexDirection(FlexDirection.Row)
            .JustifyContent(Justify.Center)
            .AlignItems(Align.Center)
            .Build();

        _root.Add(_slotRow);

        BuildSlots();

        // Position at the bottom center of screen
        _root.style.position = Position.Absolute;
        _root.style.bottom = 8;
        _root.style.left = new Length(50, LengthUnit.Percent);
        _root.style.translate = new Translate(new Length(-50, LengthUnit.Percent), 0);
        _root.style.width = StyleKeyword.Auto;
        _root.style.minWidth = 600;
        _root.SetPadding(0);
        _root.style.paddingTop = 6;
        _root.style.paddingBottom = 6;
        _root.style.paddingLeft = 12;
        _root.style.paddingRight = 12;

        // Consume pointer events so clicks on the toolbar don't pass through to the world
        _root.RegisterCallback<PointerDownEvent>(e => e.StopPropagation());
        _root.RegisterCallback<PointerUpEvent>(e => e.StopPropagation());
        _root.RegisterCallback<PointerMoveEvent>(e => e.StopPropagation());

        ApplyTheme();
    }

    private void BuildSlots()
    {
        for (int i = 0; i < HotbarSystem.SlotCount; i++)
        {
            int slotIndex = i;
            string keyLabel = slotIndex == 9 ? "0" : (slotIndex + 1).ToString();
            var slot = _hotbar.GetSlot(slotIndex);

            var btn = ForgeButton.Create(LocalizationKeys.HotbarSlot, () => OnSlotClicked(slotIndex))
                .Width(64).Height(56).FontSize(10).MarginRight(2).Build();

            // Right-click or middle-click clears the slot
            btn.RegisterCallback<PointerDownEvent>(e =>
            {
                if (e.button == 1 || e.button == 2)
                {
                    e.StopPropagation();
                    ClearHotbarSlot(slotIndex);
                }
            });

            UpdateSlotVisual(btn, slot, keyLabel);
            _slotRow.Add(btn);
            _slotButtons[i] = btn;
        }
    }

    private void UpdateSlotVisual(ForgeButton btn, HotbarSlot slot, string keyLabel)
    {
        if (slot.ActionType == HotbarActionType.None)
        {
            btn.Text = $"[{keyLabel}]\n\u2014";
        }
        else
        {
            int cost = slot.GoldCost;
            btn.Text = $"[{keyLabel}] {slot.DisplayLabel}\n({cost}g)";
        }
    }

    private void OnSlotClicked(int slotIndex)
    {
        var slot = _hotbar.GetSlot(slotIndex);
        if (slot.ActionType == HotbarActionType.None)
        {
            OpenPicker(slotIndex);
            return;
        }

        ClosePicker();

        // Toggle behavior: clicking the already-active slot deactivates it
        if (_activeSlotIndex == slotIndex)
        {
            ClearActiveSlot();
            return;
        }

        SetActiveSlot(slotIndex);
        ActivateSlot(slot);
    }

    /// <summary>Activates the action for a hotbar slot.</summary>
    public void ActivateSlot(HotbarSlot slot)
    {
        switch (slot.ActionType)
        {
            case HotbarActionType.PlaceStructure:
                OnStructureSelected?.Invoke(slot.StructureCategory);
                break;
            case HotbarActionType.DrawPath:
                OnDrawPathMode?.Invoke();
                break;
            case HotbarActionType.Demolish:
                OnDemolishMode?.Invoke();
                break;
        }
    }

    /// <summary>Activates the hotbar slot at the given index (0-9). Called from keyboard shortcuts.</summary>
    public void ActivateSlotByIndex(int index)
    {
        if (index < 0 || index >= HotbarSystem.SlotCount) { return; }
        OnSlotClicked(index);
    }

    /// <summary>
    /// Called when the tool mode exits (e.g. RMB cancel, Escape).
    /// Clears the active slot highlight without re-firing the slot action.
    /// </summary>
    public void OnToolModeExited()
    {
        ClearActiveSlot();
    }

    /// <summary>
    /// Clears the assignment of a hotbar slot so the player can reassign it.
    /// Triggered by right-click or middle-click on a filled slot.
    /// </summary>
    private void ClearHotbarSlot(int slotIndex)
    {
        var slot = _hotbar.GetSlot(slotIndex);
        if (slot.ActionType == HotbarActionType.None) { return; }

        // If this was the active slot, deactivate the tool mode too
        if (_activeSlotIndex == slotIndex)
        {
            ClearActiveSlot();
        }

        _hotbar.ClearSlot(slotIndex);
        RefreshSlotVisuals();
        ClosePicker();
    }

    private void SetActiveSlot(int index)
    {
        ClearActiveSlot();
        _activeSlotIndex = index;
        if (index >= 0 && index < _slotButtons.Length)
        {
            _slotButtons[index].style.borderTopWidth = 2;
            _slotButtons[index].style.borderBottomWidth = 2;
            _slotButtons[index].style.borderLeftWidth = 2;
            _slotButtons[index].style.borderRightWidth = 2;
            _slotButtons[index].style.borderTopColor = new Color(0.2f, 0.8f, 1.0f);
            _slotButtons[index].style.borderBottomColor = new Color(0.2f, 0.8f, 1.0f);
            _slotButtons[index].style.borderLeftColor = new Color(0.2f, 0.8f, 1.0f);
            _slotButtons[index].style.borderRightColor = new Color(0.2f, 0.8f, 1.0f);
        }
    }

    private void ClearActiveSlot()
    {
        if (_activeSlotIndex >= 0 && _activeSlotIndex < _slotButtons.Length)
        {
            _slotButtons[_activeSlotIndex].style.borderTopWidth = 0;
            _slotButtons[_activeSlotIndex].style.borderBottomWidth = 0;
            _slotButtons[_activeSlotIndex].style.borderLeftWidth = 0;
            _slotButtons[_activeSlotIndex].style.borderRightWidth = 0;
        }
        _activeSlotIndex = -1;
    }

    private void OpenPicker(int slotIndex)
    {
        ClosePicker();
        _pickerSlotIndex = slotIndex;

        int researchTier = _simulation?.ResearchManager.CurrentTier ?? 1;
        var available = HotbarCatalog.GetAvailableActions(researchTier);

        _pickerPopup = ForgeContainer.Create()
            .Position(Position.Absolute)
            .Bottom(70)
            .LeftPercent(50)
            .TranslateX(-50)
            .BackgroundColorRaw(new Color(0.12f, 0.12f, 0.16f, 0.95f))
            .BorderRadius(8)
            .Padding(8)
            .FlexDirection(FlexDirection.Row)
            .FlexWrap(Wrap.Wrap)
            .MaxWidth(500)
            .Build();

        // Consume events so clicks on the picker don't pass through
        _pickerPopup.RegisterCallback<PointerDownEvent>(e => e.StopPropagation());

        foreach (var action in available)
        {
            var actionCopy = action.Clone();
            var pickBtn = ForgeButton.Create(LocalizationKeys.HotbarSlot, () =>
            {
                _hotbar.SetSlot(_pickerSlotIndex, actionCopy);
                RefreshSlotVisuals();
                ClosePicker();
            })
            .Text($"{actionCopy.DisplayLabel}\n({actionCopy.GoldCost}g)")
            .Width(80).Height(48).FontSize(10).MarginRight(4).MarginBottom(4).Build();

            _pickerPopup.Add(pickBtn);
        }

        // Clear slot option
        var clearBtn = ForgeButton.Create("ui.hotbar.empty", () =>
        {
            _hotbar.ClearSlot(_pickerSlotIndex);
            RefreshSlotVisuals();
            ClosePicker();
        })
        .Text("\u2715 Clear")
        .Width(70).Height(48).FontSize(10).Danger().Build();
        _pickerPopup.Add(clearBtn);

        _root.parent?.Add(_pickerPopup);
    }

    private void ClosePicker()
    {
        if (_pickerPopup != null)
        {
            _pickerPopup.RemoveFromHierarchy();
            _pickerPopup = null;
        }
        _pickerSlotIndex = -1;
    }

    private void RefreshSlotVisuals()
    {
        for (int i = 0; i < HotbarSystem.SlotCount; i++)
        {
            string keyLabel = i == 9 ? "0" : (i + 1).ToString();
            var slot = _hotbar.GetSlot(i);
            UpdateSlotVisual(_slotButtons[i], slot, keyLabel);
        }
    }

    public void Show(object? context = null)
    {
        _isVisible = true;
        _root.style.display = DisplayStyle.Flex;
    }

    public void Hide()
    {
        _isVisible = false;
        _root.style.display = DisplayStyle.None;
    }

    public void Refresh()
    {
        if (_simulation?.Guild == null) { return; }

        // Update slot enabled state based on affordability
        for (int i = 0; i < HotbarSystem.SlotCount; i++)
        {
            var slot = _hotbar.GetSlot(i);
            if (slot.ActionType == HotbarActionType.None)
            {
                _slotButtons[i].SetEnabled(true); // Empty slots always clickable (opens picker)
                continue;
            }

            bool canAfford = slot.ActionType switch
            {
                HotbarActionType.PlaceStructure => _simulation!.ItemManager.GetStock("gold") >= EconomyConfig.GetStructureCost(slot.StructureCategory),
                HotbarActionType.DrawPath => _simulation!.ItemManager.GetStock("gold") >= EconomyConfig.PathSegmentCost,
                HotbarActionType.Demolish => true,
                _ => true
            };
            _slotButtons[i].SetEnabled(canAfford);
        }

        RefreshSlotVisuals();
    }

    public void ApplyTheme()
    {
        _root.style.backgroundColor = ForgeStyledVisualElement.GetThemeColor("bg.panel");
        var radius = ForgeStyledVisualElement.IsMinimalistMode ? 2 : 6;
        ForgeStyledVisualElement.SetBorderOn(_root,
            ForgeStyledVisualElement.GetThemeColor("border.normal"), 1, radius);
        _root.style.borderTopWidth = 2;
        _root.style.borderTopColor = ForgeStyledVisualElement.GetThemeColor("accent.primary");
    }

    public void Dispose()
    {
        ClosePicker();
    }
}
}
