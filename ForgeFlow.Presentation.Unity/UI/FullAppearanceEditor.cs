using ForgeFlow.Core;
using ForgeFlow.Core.Data;
using ForgeFlow.Core.Data.Definitions;
using ForgeFlow.Core.Entities;
using ForgeFlow.Core.Events;
using ForgeFlow.Core.Systems;
using ForgeFlow.Presentation.Unity.Visuals;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ForgeFlow.Presentation.Unity.UI
{

/// <summary>
/// Full Appearance Editor panel — runtime-created Canvas UI that lets
/// the player select parts, palettes, and templates for a selected hero.
/// All mutations go through Core's AppearanceApplier API.
/// </summary>
internal class FullAppearanceEditor : MonoBehaviour
{
    private SimulationTicker _simulation = null!;
    private GameBootstrapper _bootstrapper = null!;
    private VisualHeroManager _heroManager = null!;
    private EventBus _eventBus = null!;

    private GameObject? _panelRoot;
    private TextMeshProUGUI? _heroNameText;
    private TextMeshProUGUI? _statusText;
    private ulong? _selectedHeroSeed;
    private AppearanceEditorData? _editorData;
    private bool _isOpen;

    public bool IsOpen => _isOpen;

    public void Initialize(SimulationTicker simulation, GameBootstrapper bootstrapper,
                           VisualHeroManager heroManager, EventBus eventBus)
    {
        _simulation = simulation;
        _bootstrapper = bootstrapper;
        _heroManager = heroManager;
        _eventBus = eventBus;
        BuildUI();
        Close();
    }

    private void BuildUI()
    {
        _panelRoot = new GameObject("AppearanceEditorPanel");
        _panelRoot.transform.SetParent(transform);

        var canvas = _panelRoot.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 20;
        _panelRoot.AddComponent<CanvasScaler>();
        _panelRoot.AddComponent<GraphicRaycaster>();

        // Background panel
        var bgObj = new GameObject("Background");
        bgObj.transform.SetParent(_panelRoot.transform);
        var bgRect = bgObj.AddComponent<RectTransform>();
        bgRect.anchorMin = new Vector2(0.05f, 0.1f);
        bgRect.anchorMax = new Vector2(0.95f, 0.9f);
        var bgImage = bgObj.AddComponent<Image>();
        bgImage.color = new Color(0.1f, 0.1f, 0.15f, 0.95f);

        // Title
        var titleObj = new GameObject("Title");
        titleObj.transform.SetParent(bgObj.transform);
        var titleRect = titleObj.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.05f, 0.9f);
        titleRect.anchorMax = new Vector2(0.7f, 0.98f);
        var titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = "Appearance Editor";
        titleText.fontSize = 22;

        // Hero name
        var nameObj = new GameObject("HeroName");
        nameObj.transform.SetParent(bgObj.transform);
        var nameRect = nameObj.AddComponent<RectTransform>();
        nameRect.anchorMin = new Vector2(0.05f, 0.82f);
        nameRect.anchorMax = new Vector2(0.5f, 0.9f);
        _heroNameText = nameObj.AddComponent<TextMeshProUGUI>();
        _heroNameText.text = "No hero selected";
        _heroNameText.fontSize = 16;

        // Status text
        var statusObj = new GameObject("Status");
        statusObj.transform.SetParent(bgObj.transform);
        var statusRect = statusObj.AddComponent<RectTransform>();
        statusRect.anchorMin = new Vector2(0.05f, 0.02f);
        statusRect.anchorMax = new Vector2(0.95f, 0.08f);
        _statusText = statusObj.AddComponent<TextMeshProUGUI>();
        _statusText.fontSize = 12;
        _statusText.text = "";

        // Parts column — list available hair/face/cape parts as buttons
        float yPos = 0.75f;
        var registry = _bootstrapper.Services.Get<AppearanceRegistry>();

        // Hair section
        CreateSectionLabel(bgObj.transform, "Hair", new Vector2(0.05f, yPos), new Vector2(0.3f, yPos + 0.06f));
        yPos -= 0.07f;
        foreach (var part in registry.GetPartsByCategory("hair"))
        {
            CreatePartButton(bgObj.transform, part, yPos);
            yPos -= 0.055f;
        }

        // Face section
        yPos -= 0.02f;
        CreateSectionLabel(bgObj.transform, "Face", new Vector2(0.05f, yPos), new Vector2(0.3f, yPos + 0.06f));
        yPos -= 0.07f;
        foreach (var part in registry.GetPartsByCategory("face"))
        {
            CreatePartButton(bgObj.transform, part, yPos);
            yPos -= 0.055f;
        }

        // Palette column (right side)
        float pyPos = 0.75f;
        CreateSectionLabel(bgObj.transform, "Palettes", new Vector2(0.55f, pyPos), new Vector2(0.95f, pyPos + 0.06f));
        pyPos -= 0.07f;
        foreach (var palette in registry.GetAllPalettes())
        {
            CreatePaletteButton(bgObj.transform, palette, pyPos);
            pyPos -= 0.055f;
        }

        // Templates column (center)
        float tyPos = 0.75f;
        CreateSectionLabel(bgObj.transform, "Templates", new Vector2(0.33f, tyPos), new Vector2(0.53f, tyPos + 0.06f));
        tyPos -= 0.07f;
        foreach (var template in registry.GetAllTemplates())
        {
            CreateTemplateButton(bgObj.transform, template, tyPos);
            tyPos -= 0.055f;
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

    private void CreateSectionLabel(Transform parent, string label, Vector2 anchorMin, Vector2 anchorMax)
    {
        var obj = new GameObject($"Label_{label}");
        obj.transform.SetParent(parent);
        var rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        var text = obj.AddComponent<TextMeshProUGUI>();
        text.text = $"── {label} ──";
        text.fontSize = 14;
        text.color = new Color(0.8f, 0.8f, 0.3f);
    }

    private void CreatePartButton(Transform parent, AppearancePartDefinition part, float yPos)
    {
        var obj = new GameObject($"Part_{part.Id}");
        obj.transform.SetParent(parent);
        var rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.05f, yPos);
        rect.anchorMax = new Vector2(0.3f, yPos + 0.05f);
        obj.AddComponent<Image>().color = new Color(0.2f, 0.2f, 0.3f);
        var btn = obj.AddComponent<Button>();
        var partId = part.Id;
        btn.onClick.AddListener(() => ApplyPart(partId));
        var txt = new GameObject("Text").AddComponent<TextMeshProUGUI>();
        txt.transform.SetParent(obj.transform);
        txt.text = part.RequiredTier > 0 ? $"{part.DisplayName} (T{part.RequiredTier})" : part.DisplayName;
        txt.fontSize = 11;
        txt.alignment = TextAlignmentOptions.Center;
    }

    private void CreatePaletteButton(Transform parent, ColorPaletteDefinition palette, float yPos)
    {
        var obj = new GameObject($"Palette_{palette.Id}");
        obj.transform.SetParent(parent);
        var rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.55f, yPos);
        rect.anchorMax = new Vector2(0.95f, yPos + 0.05f);
        obj.AddComponent<Image>().color = new Color(0.2f, 0.25f, 0.2f);
        var btn = obj.AddComponent<Button>();
        var paletteId = palette.Id;
        btn.onClick.AddListener(() => ApplyPalette(paletteId));
        var txt = new GameObject("Text").AddComponent<TextMeshProUGUI>();
        txt.transform.SetParent(obj.transform);
        txt.text = palette.RequiredTier > 0 ? $"{palette.DisplayName} (T{palette.RequiredTier})" : palette.DisplayName;
        txt.fontSize = 11;
        txt.alignment = TextAlignmentOptions.Center;
    }

    private void CreateTemplateButton(Transform parent, AppearanceTemplateDefinition template, float yPos)
    {
        var obj = new GameObject($"Template_{template.Id}");
        obj.transform.SetParent(parent);
        var rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.33f, yPos);
        rect.anchorMax = new Vector2(0.53f, yPos + 0.05f);
        obj.AddComponent<Image>().color = new Color(0.25f, 0.2f, 0.3f);
        var btn = obj.AddComponent<Button>();
        var templateId = template.Id;
        btn.onClick.AddListener(() => ApplyTemplate(templateId));
        var txt = new GameObject("Text").AddComponent<TextMeshProUGUI>();
        txt.transform.SetParent(obj.transform);
        txt.text = template.RequiredTier > 0 ? $"{template.DisplayName} (T{template.RequiredTier})" : template.DisplayName;
        txt.fontSize = 11;
        txt.alignment = TextAlignmentOptions.Center;
    }

    // --- Public API ---

    public void Open(ulong heroSeed)
    {
        _selectedHeroSeed = heroSeed;
        _editorData = _simulation.AppearanceApplier.OpenAppearanceEditor(heroSeed, _simulation.EntityManager.Heroes);

        if (_editorData == null)
        {
            Debug.LogWarning($"[AppearanceEditor] Hero seed {heroSeed} not found");
            return;
        }

        if (_heroNameText != null)
        {
            _heroNameText.text = $"Editing Hero Seed: {heroSeed}";
        }

        _isOpen = true;
        if (_panelRoot != null) _panelRoot.SetActive(true);
        Debug.Log($"[AppearanceEditor] Opened for hero seed {heroSeed}");
    }

    public void Close()
    {
        _isOpen = false;
        _selectedHeroSeed = null;
        _editorData = null;
        if (_panelRoot != null) _panelRoot.SetActive(false);
    }

    private HeroEntity? GetSelectedHero()
    {
        if (_selectedHeroSeed == null) return null;
        foreach (var hero in _simulation.EntityManager.Heroes)
        {
            if (hero.Seed == _selectedHeroSeed.Value) return hero;
        }
        return null;
    }

    private void ApplyPart(string partId)
    {
        var hero = GetSelectedHero();
        if (hero == null) return;

        var registry = _bootstrapper.Services.Get<AppearanceRegistry>();
        if (registry.TryGetPart(partId, out var part) && part.RequiredTier > _simulation.ResearchManager.CurrentTier)
        {
            SetStatus($"Requires Research Tier {part.RequiredTier}");
            return;
        }

        var applier = _simulation.AppearanceApplier;
        applier.SetRegistry(registry);
        applier.ApplyPart(hero, partId);
        _eventBus.Publish(new AppearanceChangedEvent(hero.Id, "part", partId));
        RefreshVisual(hero);
        SetStatus($"Applied part: {partId}");
    }

    private void ApplyPalette(string paletteId)
    {
        var hero = GetSelectedHero();
        if (hero == null) return;

        var registry = _bootstrapper.Services.Get<AppearanceRegistry>();
        if (registry.TryGetPalette(paletteId, out var palette) && palette.RequiredTier > _simulation.ResearchManager.CurrentTier)
        {
            SetStatus($"Requires Research Tier {palette.RequiredTier}");
            return;
        }

        var applier = _simulation.AppearanceApplier;
        applier.SetRegistry(registry);
        applier.ApplyPalette(hero, paletteId);
        _eventBus.Publish(new AppearanceChangedEvent(hero.Id, "palette", paletteId));
        RefreshVisual(hero);
        SetStatus($"Applied palette: {paletteId}");
    }

    private void ApplyTemplate(string templateId)
    {
        var hero = GetSelectedHero();
        if (hero == null) return;

        var registry = _bootstrapper.Services.Get<AppearanceRegistry>();
        if (registry.TryGetTemplate(templateId, out var template) && template.RequiredTier > _simulation.ResearchManager.CurrentTier)
        {
            SetStatus($"Requires Research Tier {template.RequiredTier}");
            return;
        }

        var applier = _simulation.AppearanceApplier;
        applier.SetRegistry(registry);
        applier.ApplyTemplate(hero, templateId);
        _eventBus.Publish(new AppearanceChangedEvent(hero.Id, "template", templateId));
        RefreshVisual(hero);
        SetStatus($"Applied template: {templateId}");
    }

    private void RefreshVisual(HeroEntity hero)
    {
        if (_heroManager.VisualHeroes.TryGetValue(hero.Id, out var visualHero))
        {
            visualHero.ApplyAppearanceFromTemplate(hero.CurrentTemplate);
        }
    }

    private void SetStatus(string msg)
    {
        if (_statusText != null) _statusText.text = msg;
        Debug.Log($"[AppearanceEditor] {msg}");
    }
}
}
