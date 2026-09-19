using ForgeFlow.Core;
using ForgeFlow.Core.Save;
using ForgeFlow.Core.Systems;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace ForgeFlow.Presentation.Unity.UI
{

/// <summary>
/// In-game UI overlay: Canvas + TextMeshPro showing hero/structure count,
/// tick/path count, and Save/Load buttons that call Core's SaveManager.
/// </summary>
internal class InGameUI : MonoBehaviour
{
    private SimulationTicker _simulation = null!;
    private GameBootstrapper _bootstrapper = null!;
    private FactoryEntryPoint _entryPoint = null!;

    private TextMeshProUGUI? _resourceText;
    private TextMeshProUGUI? _heroCountText;
    private GameObject? _canvasRoot;

    public void Initialize(SimulationTicker simulation, GameBootstrapper bootstrapper,
                           FactoryEntryPoint entryPoint)
    {
        _simulation = simulation;
        _bootstrapper = bootstrapper;
        _entryPoint = entryPoint;

        BuildUI();
    }

    private void BuildUI()
    {
        // Main HUD canvas
        var canvasObj = new GameObject("InGameCanvas");
        canvasObj.transform.SetParent(transform);
        _canvasRoot = canvasObj;
        var canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();

        // EventSystem for button interaction
        var eventSysObj = new GameObject("EventSystem");
        eventSysObj.transform.SetParent(canvasObj.transform);
        eventSysObj.AddComponent<EventSystem>();
        eventSysObj.AddComponent<InputSystemUIInputModule>();

        // Hero count text (top-left)
        var heroCountObj = new GameObject("HeroCount");
        heroCountObj.transform.SetParent(canvasObj.transform);
        var heroCountRect = heroCountObj.AddComponent<RectTransform>();
        heroCountRect.anchorMin = new Vector2(0.01f, 0.9f);
        heroCountRect.anchorMax = new Vector2(0.2f, 0.98f);
        _heroCountText = heroCountObj.AddComponent<TextMeshProUGUI>();
        _heroCountText.fontSize = 16;
        _heroCountText.raycastTarget = false;
        _heroCountText.text = "Heroes: 0";

        // Resource text (bottom-left)
        var resourceObj = new GameObject("Resources");
        resourceObj.transform.SetParent(canvasObj.transform);
        var resourceRect = resourceObj.AddComponent<RectTransform>();
        resourceRect.anchorMin = new Vector2(0.01f, 0.01f);
        resourceRect.anchorMax = new Vector2(0.3f, 0.08f);
        _resourceText = resourceObj.AddComponent<TextMeshProUGUI>();
        _resourceText.fontSize = 14;
        _resourceText.raycastTarget = false;
        _resourceText.text = "Tick: 0";

        // Start hidden — SetVisible(true) is called when entering Playing state
        _canvasRoot.SetActive(false);
    }

    public void SetVisible(bool visible)
    {
        _canvasRoot?.SetActive(visible);
    }

    private void Update()
    {
        UpdateDisplayTexts();
    }

    private void UpdateDisplayTexts()
    {
        if (_heroCountText != null)
        {
            _heroCountText.text = $"Heroes: {_simulation.EntityManager.Heroes.Count} | Structures: {_simulation.EntityManager.Structures.Count}";
        }

        if (_resourceText != null)
        {
            _resourceText.text = $"Tick: {_simulation.TickCount} | Paths: {_simulation.EntityManager.PathSegments.Count}";
        }
    }

    public void SaveGame()
    {
        _entryPoint.SaveGame("quicksave");
    }

    public void LoadGame()
    {
        var savePath = Path.Combine(Application.persistentDataPath, "quicksave.json");
        var data = _bootstrapper.Services.Get<SaveManager>().LoadFromFile(savePath);
        if (data != null)
        {
            Debug.Log($"[UI] Game loaded from {savePath} (Tick {data.TickCount})");
        }
        else
        {
            Debug.LogWarning("[UI] No save file found");
        }
    }
}
}
