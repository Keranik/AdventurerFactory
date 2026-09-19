using ForgeFlow.Core.Events;
using ForgeFlow.Core.Systems;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ForgeFlow.Presentation.Unity.UI
{

/// <summary>
/// Research Panel UI: Canvas + TextMeshPro buttons for unlocking research tiers.
/// Only calls SimulationTicker.UnlockResearchTier() — all logic is in Core.
/// </summary>
internal class ResearchPanel : MonoBehaviour
{
    private SimulationTicker _simulation = null!;
    private EventBus _eventBus = null!;
    private GameObject? _panelRoot;
    private TextMeshProUGUI? _statusText;
    private bool _isVisible;

    public void Initialize(SimulationTicker simulation, EventBus eventBus)
    {
        _simulation = simulation;
        _eventBus = eventBus;
        _eventBus.Subscribe<ResearchUnlockedEvent>(OnResearchUnlocked);

        BuildUI();
    }

    private void OnDestroy()
    {
        _eventBus.Unsubscribe<ResearchUnlockedEvent>(OnResearchUnlocked);
    }

    private void BuildUI()
    {
        // Create canvas for the research panel
        _panelRoot = new GameObject("ResearchPanel");
        _panelRoot.transform.SetParent(transform);

        var canvas = _panelRoot.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 5;
        _panelRoot.AddComponent<CanvasScaler>();
        _panelRoot.AddComponent<GraphicRaycaster>();

        // Background
        var bgObj = new GameObject("Background");
        bgObj.transform.SetParent(_panelRoot.transform);
        var bgRect = bgObj.AddComponent<RectTransform>();
        bgRect.anchorMin = new Vector2(0.7f, 0.3f);
        bgRect.anchorMax = new Vector2(0.95f, 0.7f);

        // Title text
        var titleObj = new GameObject("Title");
        titleObj.transform.SetParent(bgObj.transform);
        var titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = "Research";
        titleText.fontSize = 20;
        titleText.raycastTarget = false;
        titleText.alignment = TextAlignmentOptions.Top;

        // Status text
        var statusObj = new GameObject("Status");
        statusObj.transform.SetParent(bgObj.transform);
        _statusText = statusObj.AddComponent<TextMeshProUGUI>();
        _statusText.fontSize = 14;
        _statusText.raycastTarget = false;
        UpdateStatusText();

        _panelRoot.SetActive(false);
        _isVisible = false;
    }

    public void ToggleVisibility()
    {
        _isVisible = !_isVisible;
        if (_panelRoot != null)
        {
            _panelRoot.SetActive(_isVisible);
        }
    }

    public void TryUnlockNextTier()
    {
        int nextTier = _simulation.ResearchManager.CurrentTier + 1;
        bool success = _simulation.ResearchManager.UnlockTier(nextTier);
        if (success)
        {
            Debug.Log($"[Research] Unlocked Tier {nextTier}!");
        }
        else
        {
            Debug.Log($"[Research] Cannot unlock Tier {nextTier} yet.");
        }
        UpdateStatusText();
    }

    public void TryUnlockTier(int tier)
    {
        bool success = _simulation.ResearchManager.UnlockTier(tier);
        if (success)
        {
            Debug.Log($"[Research] Unlocked Tier {tier}!");
        }
        else
        {
            Debug.Log($"[Research] Cannot unlock Tier {tier}.");
        }
        UpdateStatusText();
    }

    private void OnResearchUnlocked(ResearchUnlockedEvent e)
    {
        Debug.Log($"[Research] Event: Tier {e.Tier} unlocked!");
        UpdateStatusText();
    }

    private void UpdateStatusText()
    {
        if (_statusText != null)
        {
            _statusText.text = $"Current Tier: {_simulation.ResearchManager.CurrentTier}\n" +
                               $"Unlocked: {string.Join(", ", _simulation.ResearchManager.UnlockedTiers)}";
        }
    }
}
}
