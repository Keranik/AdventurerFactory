using ForgeFlow.Core.Entities;
using UnityEngine;

namespace ForgeFlow.Presentation.Unity.Visuals
{

/// <summary>
/// Visual representation of a single hero on screen.
/// Attached to a GameObject by <see cref="VisualHeroManager"/>.
/// Handles smooth movement, gear visuals, state effects, auras, and appearance changes.
/// </summary>
internal class VisualHero : MonoBehaviour
{
    public enum EffectType
    {
        DungeonSuccess,
        DungeonFailure,
        Death,
        LevelUp,
        FusionGlow,
        GearEquip
    }

    public ulong HeroId { get; set; }
    public string ClassId { get; set; } = string.Empty;
    public Vector3 TargetPosition { get; set; }

    // Visual sub-objects
    private readonly Dictionary<EquipSlot, string> _currentGear = new();
    private HeroState _lastState = HeroState.OnPath;
    private int _lastLevel;
    private bool _hasAura;

    // Placeholder child objects (populated by BuildPlaceholderVisuals)
    private GameObject? _bodyObj;
    private GameObject? _swordObj;
    private GameObject? _armorIndicator;
    private GameObject? _levelIndicator;
    private GameObject? _ghostEffect;
    private ParticleSystem? _auraParticles;

    private void Start()
    {
        TargetPosition = transform.position;
    }

    /// <summary>
    /// Creates placeholder geometry using RuntimePlaceholderFactory.
    /// Called by VisualHeroManager after instantiation.
    /// </summary>
    public void BuildPlaceholderVisuals()
    {
        var placeholder = RuntimePlaceholderFactory.CreateVisualHeroPlaceholder(ClassId);

        // Re-parent all children from the factory-created root to this transform
        var children = new List<Transform>();
        for (int i = 0; i < placeholder.transform.localScale.magnitude; i++) { } // no-op

        // Manually transfer known children
        _bodyObj = FindChildByName(placeholder, "Body");
        _swordObj = FindChildByName(placeholder, "Sword");
        _armorIndicator = FindChildByName(placeholder, "Armor");
        _levelIndicator = FindChildByName(placeholder, "LevelGlow");
        _ghostEffect = FindChildByName(placeholder, "GhostEffect");

        if (_bodyObj != null) _bodyObj.transform.SetParent(transform);
        if (_swordObj != null) _swordObj.transform.SetParent(transform);
        if (_armorIndicator != null) _armorIndicator.transform.SetParent(transform);
        if (_levelIndicator != null) _levelIndicator.transform.SetParent(transform);
        if (_ghostEffect != null) _ghostEffect.transform.SetParent(transform);

        Destroy(placeholder);

        // Add a simple aura particle system for Level 3+ heroes
        var auraObj = new GameObject("AuraParticles");
        auraObj.transform.SetParent(transform);
        auraObj.transform.localPosition = new Vector3(0, 0.5f, 0);
        _auraParticles = auraObj.AddComponent<ParticleSystem>();
        auraObj.SetActive(false);
    }

    private static GameObject? FindChildByName(GameObject parent, string childName)
    {
        // Stub-compatible: iterate known structure
        // In real Unity this would use transform.Find()
        return null; // stubs don't support real transform hierarchy
    }

    /// <summary>
    /// Smoothly interpolates this hero's world position toward the
    /// simulation target. Called each render frame by the manager.
    /// </summary>
    public void SmoothMove(float deltaTime, float smoothing)
    {
        transform.position = Vector3.Lerp(transform.position, TargetPosition, deltaTime * smoothing);

        var delta = TargetPosition - transform.position;
        if (delta.magnitude > 0.01f)
        {
            transform.rotation = Quaternion.Euler(0, Mathf.Atan2(delta.x, delta.z) * Mathf.Rad2Deg, 0);
        }
    }

    /// <summary>
    /// Called when the hero equips or changes a piece of gear.
    /// </summary>
    public void OnGearChanged(string itemId, EquipSlot slot)
    {
        _currentGear[slot] = itemId;

        switch (slot)
        {
            case EquipSlot.Weapon:
                if (_swordObj != null) _swordObj.SetActive(true);
                break;
            case EquipSlot.ChestArmor:
            case EquipSlot.Helmet:
                if (_armorIndicator != null) _armorIndicator.SetActive(true);
                break;
        }

        Debug.Log($"[Visual] Hero {HeroId} equipped {itemId} in {slot}");
    }

    /// <summary>
    /// Applies appearance template changes from Core: palette colors, part toggles, aura particles.
    /// </summary>
    public void ApplyAppearanceFromTemplate(AppearanceTemplate template)
    {
        // Apply palette colors to body material
        if (_bodyObj != null && template.ColorPalette.Count > 0)
        {
            var bodyRenderer = _bodyObj.GetComponent<MeshRenderer>();
            if (bodyRenderer != null && bodyRenderer.material != null)
            {
                if (template.ColorPalette.TryGetValue("primary", out var primaryHex))
                {
                    bodyRenderer.material.color = HexToColor(primaryHex);
                }
            }
        }

        // Toggle cape/helmet/boots parts based on appearance data
        if (template.Cape != null && _armorIndicator != null)
        {
            _armorIndicator.SetActive(true);
        }

        // Aura effect for Level 3+ based on template
        if (template.AuraEffect != null && _auraParticles != null)
        {
            _auraParticles.gameObject.SetActive(true);
            _auraParticles.Play();
        }
        else if (_auraParticles != null)
        {
            _auraParticles.Stop();
            _auraParticles.gameObject.SetActive(false);
        }

        Debug.Log($"[Visual] Hero {HeroId} appearance updated: template={template.Name}");
    }

    /// <summary>
    /// Updates state-driven visuals: ghost transparency, aura, level glow.
    /// </summary>
    public void UpdateStateVisuals(HeroState state, int level, float morale)
    {
        if (state != _lastState)
        {
            OnStateChanged(_lastState, state);
            _lastState = state;
        }

        if (level != _lastLevel && _lastLevel > 0)
        {
            PlayEffect(EffectType.LevelUp);
            if (_levelIndicator != null)
            {
                _levelIndicator.SetActive(level >= 2);
                float scale = 0.1f + (level * 0.02f);
                _levelIndicator.transform.localScale = new Vector3(scale, scale, scale);
            }
        }
        _lastLevel = level;

        // Aura particles at level 3+
        bool shouldHaveAura = level >= 3;
        if (shouldHaveAura != _hasAura)
        {
            _hasAura = shouldHaveAura;
            if (_auraParticles != null)
            {
                _auraParticles.gameObject.SetActive(_hasAura);
                if (_hasAura) _auraParticles.Play();
                else _auraParticles.Stop();
            }
            Debug.Log($"[Visual] Hero {HeroId} aura {(_hasAura ? "ON" : "OFF")} (level {level})");
        }
    }

    public void PlayEffect(EffectType effect)
    {
        Debug.Log($"[Visual] Hero {HeroId} → effect {effect}");
    }

    private void OnStateChanged(HeroState oldState, HeroState newState)
    {
        switch (newState)
        {
            case HeroState.Ghost:
                if (_ghostEffect != null) _ghostEffect.SetActive(true);
                if (_bodyObj != null)
                {
                    var r = _bodyObj.GetComponent<MeshRenderer>();
                    if (r != null && r.material != null)
                    {
                        r.material.color = new Color(0.5f, 0.5f, 0.8f, 0.3f);
                    }
                }
                Debug.Log($"[Visual] Hero {HeroId} became a ghost");
                break;

            case HeroState.OnPath when oldState == HeroState.InDungeon:
                PlayEffect(EffectType.DungeonSuccess);
                break;

            case HeroState.AwaitingFusion:
                PlayEffect(EffectType.FusionGlow);
                break;
        }
    }

    private static Color HexToColor(string hex)
    {
        hex = hex.TrimStart('#');
        if (hex.Length < 6) return Color.white;
        float r = int.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber) / 255f;
        float g = int.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber) / 255f;
        float b = int.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber) / 255f;
        return new Color(r, g, b);
    }
}
}
