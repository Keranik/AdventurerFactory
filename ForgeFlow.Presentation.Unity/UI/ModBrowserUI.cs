using ForgeFlow.Core;
using ForgeFlow.Core.Data;
using ForgeFlow.Core.Events;
using ForgeFlow.Core.Modding;
using ForgeFlow.Core.Systems;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ForgeFlow.Presentation.Unity.UI
{

/// <summary>
/// Mod browser UI panel — scans the Mods folder and displays a simple
/// list of discovered mods with Load buttons. Forwards all mod loading
/// to Core's ModLoader via GameBootstrapper.ModLoader.
/// </summary>
internal class ModBrowserUI : MonoBehaviour
{
    private SimulationTicker _simulation = null!;
    private GameBootstrapper _bootstrapper = null!;
    private EventBus _eventBus = null!;
    private string _modsBasePath = string.Empty;

    private GameObject? _panelRoot;
    private TextMeshProUGUI? _statusText;
    private bool _isOpen;

    public bool IsOpen => _isOpen;

    public void Initialize(SimulationTicker simulation, GameBootstrapper bootstrapper,
                           EventBus eventBus, string modsBasePath)
    {
        _simulation = simulation;
        _bootstrapper = bootstrapper;
        _eventBus = eventBus;
        _modsBasePath = modsBasePath;

        _eventBus.Subscribe<ModLoadedEvent>(OnModLoaded);
        BuildUI();
        Close();
    }

    private void OnDestroy()
    {
        _eventBus.Unsubscribe<ModLoadedEvent>(OnModLoaded);
    }

    private void BuildUI()
    {
        _panelRoot = new GameObject("ModBrowserPanel");
        _panelRoot.transform.SetParent(transform);

        var canvas = _panelRoot.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 25;
        _panelRoot.AddComponent<CanvasScaler>();
        _panelRoot.AddComponent<GraphicRaycaster>();

        // Background
        var bgObj = new GameObject("Background");
        bgObj.transform.SetParent(_panelRoot.transform);
        var bgRect = bgObj.AddComponent<RectTransform>();
        bgRect.anchorMin = new Vector2(0.2f, 0.15f);
        bgRect.anchorMax = new Vector2(0.8f, 0.85f);
        bgObj.AddComponent<Image>().color = new Color(0.08f, 0.08f, 0.12f, 0.95f);

        // Title
        var titleObj = new GameObject("Title");
        titleObj.transform.SetParent(bgObj.transform);
        var titleRect = titleObj.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.05f, 0.9f);
        titleRect.anchorMax = new Vector2(0.7f, 0.98f);
        var titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = "Mod Browser";
        titleText.fontSize = 20;

        // Status
        var statusObj = new GameObject("Status");
        statusObj.transform.SetParent(bgObj.transform);
        var statusRect = statusObj.AddComponent<RectTransform>();
        statusRect.anchorMin = new Vector2(0.05f, 0.82f);
        statusRect.anchorMax = new Vector2(0.95f, 0.9f);
        _statusText = statusObj.AddComponent<TextMeshProUGUI>();
        _statusText.fontSize = 12;
        _statusText.text = $"Mods folder: {_modsBasePath}";

        // Scan and list mods
        float yPos = 0.75f;
        if (Directory.Exists(_modsBasePath))
        {
            var modFolders = Directory.GetDirectories(_modsBasePath);
            foreach (var folder in modFolders)
            {
                var modName = Path.GetFileName(folder);
                CreateModEntry(bgObj.transform, modName, folder, yPos);
                yPos -= 0.08f;
            }
            if (modFolders.Length == 0)
            {
                CreateInfoLabel(bgObj.transform, "No mods found. Drop mod folders into the Mods directory.", yPos);
            }
        }
        else
        {
            CreateInfoLabel(bgObj.transform, "Mods folder not found.", yPos);
        }

        // Close button
        var closeObj = new GameObject("CloseButton");
        closeObj.transform.SetParent(bgObj.transform);
        var closeRect = closeObj.AddComponent<RectTransform>();
        closeRect.anchorMin = new Vector2(0.8f, 0.9f);
        closeRect.anchorMax = new Vector2(0.95f, 0.98f);
        closeObj.AddComponent<Image>().color = new Color(0.6f, 0.1f, 0.1f);
        var closeBtn = closeObj.AddComponent<Button>();
        closeBtn.onClick.AddListener(Close);
        var closeTxt = new GameObject("Text").AddComponent<TextMeshProUGUI>();
        closeTxt.transform.SetParent(closeObj.transform);
        closeTxt.text = "Close";
        closeTxt.fontSize = 14;
        closeTxt.alignment = TextAlignmentOptions.Center;
    }

    private void CreateModEntry(Transform parent, string modName, string folderPath, float yPos)
    {
        // Mod info
        var infoObj = new GameObject($"Mod_{modName}");
        infoObj.transform.SetParent(parent);
        var infoRect = infoObj.AddComponent<RectTransform>();
        infoRect.anchorMin = new Vector2(0.05f, yPos);
        infoRect.anchorMax = new Vector2(0.7f, yPos + 0.06f);
        var infoText = infoObj.AddComponent<TextMeshProUGUI>();

        bool hasItems = File.Exists(Path.Combine(folderPath, "Items.json"));
        bool hasClasses = File.Exists(Path.Combine(folderPath, "Classes.json"));
        bool hasRecipes = File.Exists(Path.Combine(folderPath, "Recipes.json"));
        string contents = string.Join(", ",
            new[] { hasItems ? "Items" : null, hasClasses ? "Classes" : null, hasRecipes ? "Recipes" : null }
        );
        infoText.text = $"{modName} [{contents}]";
        infoText.fontSize = 12;

        // Load button
        var btnObj = new GameObject($"Load_{modName}");
        btnObj.transform.SetParent(parent);
        var btnRect = btnObj.AddComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(0.72f, yPos);
        btnRect.anchorMax = new Vector2(0.95f, yPos + 0.06f);
        btnObj.AddComponent<Image>().color = new Color(0.2f, 0.4f, 0.2f);
        var btn = btnObj.AddComponent<Button>();
        var capturedName = modName;
        btn.onClick.AddListener(() => LoadMod(capturedName));
        var btnText = new GameObject("Text").AddComponent<TextMeshProUGUI>();
        btnText.transform.SetParent(btnObj.transform);
        btnText.text = "Load";
        btnText.fontSize = 12;
        btnText.alignment = TextAlignmentOptions.Center;
    }

    private void CreateInfoLabel(Transform parent, string msg, float yPos)
    {
        var obj = new GameObject("Info");
        obj.transform.SetParent(parent);
        var rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.05f, yPos);
        rect.anchorMax = new Vector2(0.95f, yPos + 0.06f);
        var text = obj.AddComponent<TextMeshProUGUI>();
        text.text = msg;
        text.fontSize = 12;
        text.color = new Color(0.6f, 0.6f, 0.6f);
    }

    private void LoadMod(string modName)
    {
        var modPath = Path.Combine(_modsBasePath, modName);
        if (!Directory.Exists(modPath))
        {
            Debug.LogWarning($"[ModBrowser] Mod folder not found: {modPath}");
            return;
        }

        _bootstrapper.Services.Get<ModLoader>().LoadMod(modPath);

        var entry = new Core.Data.ModBrowserEntry
        {
            ModName = modName,
            FolderPath = modPath,
            IsLoaded = true,
            HasItems = File.Exists(Path.Combine(modPath, "Items.json")),
            HasClasses = File.Exists(Path.Combine(modPath, "Classes.json")),
            HasRecipes = File.Exists(Path.Combine(modPath, "Recipes.json")),
            HasDungeons = File.Exists(Path.Combine(modPath, "Dungeons.json")),
            HasDlls = Directory.GetFiles(modPath, "*.dll").Length > 0
        };

        _bootstrapper.Services.Get<ModBrowserRegistry>().Register(entry);
        _eventBus.Publish(new ModLoadedEvent(modName));
        Debug.Log($"[ModBrowser] Loaded mod: {modName}");
    }

    private void OnModLoaded(ModLoadedEvent e)
    {
        UpdateStatus();
    }

    private void UpdateStatus()
    {
        if (_statusText != null)
        {
            var count = _bootstrapper.Services.Get<ModBrowserRegistry>().Count;
            _statusText.text = $"Mods folder: {_modsBasePath} | Loaded: {count}";
        }
    }

    public void Open()
    {
        _isOpen = true;
        if (_panelRoot != null) _panelRoot.SetActive(true);
    }

    public void Close()
    {
        _isOpen = false;
        if (_panelRoot != null) _panelRoot.SetActive(false);
    }

    public void ToggleOpen()
    {
        if (_isOpen) Close(); else Open();
    }
}
}
