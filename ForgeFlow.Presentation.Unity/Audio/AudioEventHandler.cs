using ForgeFlow.Core.Events;
using UnityEngine;

namespace ForgeFlow.Presentation.Unity.Audio
{

/// <summary>
/// Subscribes to Core events and plays the appropriate sound effects.
/// In the real Unity build, the AudioClip fields are assigned via
/// the Inspector from the project's audio assets.
/// </summary>
internal class AudioEventHandler : MonoBehaviour
{
#pragma warning disable CS0649 // Assigned by Unity Inspector via [SerializeField]
    [Header("Hero Sounds")]
    [SerializeField] private AudioClip? _heroSpawnClip;
    [SerializeField] private AudioClip? _gearEquipClip;
    [SerializeField] private AudioClip? _levelUpClip;

    [Header("Dungeon Sounds")]
    [SerializeField] private AudioClip? _dungeonSuccessFanfare;
    [SerializeField] private AudioClip? _dungeonFailureSadTrombone;
    [SerializeField] private AudioClip? _heroDeathClip;

    [Header("Factory Sounds")]
    [SerializeField] private AudioClip? _forgeClangs;
    [SerializeField] private AudioClip? _fusionCompleteClip;
    [SerializeField] private AudioClip? _beltHumLoop;
    [SerializeField] private AudioClip? _resourceProducedClip;

    [Header("Phase 10: Worker Lifecycle Sounds")]
    [SerializeField] private AudioClip? _workerWornOutClip;
    [SerializeField] private AudioClip? _abilityGainedClip;
    [SerializeField] private AudioClip? _goldEarnedClip;
#pragma warning restore CS0649

    [Header("Settings")]
    [SerializeField] [Range(0f, 1f)] private float _sfxVolume = 0.8f;
    [SerializeField] [Range(0f, 1f)] private float _musicVolume = 0.6f;

    private EventBus _eventBus = null!;
    private AudioSource? _sfxSource;
    private AudioSource? _musicSource;
    private AudioSource? _ambientSource;

    public void Initialize(EventBus eventBus)
    {
        _eventBus = eventBus;

        // In Unity, these would be AudioSource components on child GameObjects
        _sfxSource = gameObject.AddComponent<AudioSource>();
        _musicSource = gameObject.AddComponent<AudioSource>();
        _ambientSource = gameObject.AddComponent<AudioSource>();

        if (_musicSource != null)
        {
            _musicSource.loop = true;
            _musicSource.volume = _musicVolume;
        }

        if (_ambientSource != null)
        {
            _ambientSource.loop = true;
            _ambientSource.volume = 0.3f;
            _ambientSource.clip = _beltHumLoop;
        }

        SubscribeToEvents();
    }

    private void OnDestroy()
    {
        UnsubscribeFromEvents();
    }

    private void SubscribeToEvents()
    {
        _eventBus.Subscribe<HeroSpawnedEvent>(OnHeroSpawned);
        _eventBus.Subscribe<GearEquippedEvent>(OnGearEquipped);
        _eventBus.Subscribe<ItemCraftedEvent>(OnItemCrafted);
        _eventBus.Subscribe<ResourceProducedEvent>(OnResourceProduced);
        _eventBus.Subscribe<WorkerWornOutEvent>(OnWorkerWornOutAudio);
        _eventBus.Subscribe<AbilityGainedEvent>(OnAbilityGainedAudio);
        _eventBus.Subscribe<GoldChangedEvent>(OnGoldChangedAudio);
    }

    private void UnsubscribeFromEvents()
    {
        _eventBus.Unsubscribe<HeroSpawnedEvent>(OnHeroSpawned);
        _eventBus.Unsubscribe<GearEquippedEvent>(OnGearEquipped);
        _eventBus.Unsubscribe<ItemCraftedEvent>(OnItemCrafted);
        _eventBus.Unsubscribe<ResourceProducedEvent>(OnResourceProduced);
        _eventBus.Unsubscribe<WorkerWornOutEvent>(OnWorkerWornOutAudio);
        _eventBus.Unsubscribe<AbilityGainedEvent>(OnAbilityGainedAudio);
        _eventBus.Unsubscribe<GoldChangedEvent>(OnGoldChangedAudio);
    }

    private void OnHeroSpawned(HeroSpawnedEvent e)
    {
        PlaySfx(_heroSpawnClip);
    }

    private void OnGearEquipped(GearEquippedEvent e)
    {
        PlaySfx(_gearEquipClip);
    }

    private void OnItemCrafted(ItemCraftedEvent e)
    {
        PlaySfx(_forgeClangs);
    }

    private void OnResourceProduced(ResourceProducedEvent e)
    {
        PlaySfx(_resourceProducedClip);
    }

    // --- Public methods called by GameBootstrapperMono ---

    public void PlayDungeonSuccess()
    {
        PlaySfx(_dungeonSuccessFanfare);
        Debug.Log("[Audio] 🎺 Dungeon success fanfare!");
    }

    public void PlayDungeonFailure()
    {
        PlaySfx(_dungeonFailureSadTrombone);
        Debug.Log("[Audio] 🎺 Sad trombone...");
    }

    public void PlayHeroDeath()
    {
        PlaySfx(_heroDeathClip);
    }

    public void PlayFusionComplete()
    {
        PlaySfx(_fusionCompleteClip);
        Debug.Log("[Audio] ✨ Fusion complete!");
    }

    public void PlayLevelUp()
    {
        PlaySfx(_levelUpClip);
    }

    public void SetSfxVolume(float volume)
    {
        _sfxVolume = volume;
    }

    public void SetMusicVolume(float volume)
    {
        _musicVolume = volume;
        if (_musicSource != null) _musicSource.volume = _musicVolume;
    }

    private void PlaySfx(AudioClip? clip)
    {
        if (clip == null || _sfxSource == null) return;
        _sfxSource.PlayOneShot(clip, _sfxVolume);
    }

    // --- Phase 10: Worker lifecycle audio handlers ---

    private void OnWorkerWornOutAudio(WorkerWornOutEvent e)
    {
        PlaySfx(_workerWornOutClip);
        Debug.Log($"[Audio] ⚠ Worker #{e.WorkerId} worn out — {e.Reason}");
    }

    private void OnAbilityGainedAudio(AbilityGainedEvent e)
    {
        PlaySfx(_abilityGainedClip);
        Debug.Log($"[Audio] ★ Ability gained: {e.AbilityName}");
    }

    private void OnGoldChangedAudio(GoldChangedEvent e)
    {
        if (e.NewAmount > e.OldAmount)
        {
            PlaySfx(_goldEarnedClip);
        }
    }

    // --- Public methods for WorkerEffectSystem ---

    public void PlayWorkerWornOutSfx()
    {
        PlaySfx(_workerWornOutClip);
    }

    public void PlayAbilityGainedSfx()
    {
        PlaySfx(_abilityGainedClip);
    }
}
}
