using ForgeFlow.Core.Entities;
using ForgeFlow.Core.Events;
using ForgeFlow.Core.Localization;
using ForgeFlow.Core.Systems;
using ForgeFlow.Presentation.Unity.Audio;
using ForgeFlow.Presentation.Unity.UI.Components;
using UnityEngine;
using EntityId = ForgeFlow.Core.Entities.EntityId;

namespace ForgeFlow.Presentation.Unity.Visuals
{
    /// <summary>
    /// Phase 11: Visual effect system for worker lifecycle events.
    /// Creates particles, floating text, and triggers sounds in response to Core events.
    /// All effects are purely in the Presentation layer — driven by EventBus subscriptions.
    /// </summary>
    internal sealed class WorkerEffectSystem : MonoBehaviour
    {
        private EventBus _eventBus = null!;
        private SimulationTicker _simulation = null!;
        private AudioEventHandler? _audioHandler;

        private readonly List<FloatingText> _floatingTexts = new();
        private readonly List<ParticleBurst> _particleBursts = new();

        public void Initialize(EventBus eventBus, SimulationTicker simulation, AudioEventHandler? audioHandler)
        {
            _eventBus = eventBus;
            _simulation = simulation;
            _audioHandler = audioHandler;

            _eventBus.Subscribe<WorkerWornOutEvent>(OnWorkerWornOut);
            _eventBus.Subscribe<AbilityGainedEvent>(OnAbilityGained);
            _eventBus.Subscribe<GoldChangedEvent>(OnGoldChanged);
            _eventBus.Subscribe<AbilityLostEvent>(OnAbilityLost);
            _eventBus.Subscribe<WorkerLevelDowngradedEvent>(OnWorkerLevelDowngraded);
            _eventBus.Subscribe<WorkerRoutedEvent>(OnWorkerRouted);
        }

        private void OnDestroy()
        {
            _eventBus.Unsubscribe<WorkerWornOutEvent>(OnWorkerWornOut);
            _eventBus.Unsubscribe<AbilityGainedEvent>(OnAbilityGained);
            _eventBus.Unsubscribe<GoldChangedEvent>(OnGoldChanged);
            _eventBus.Unsubscribe<AbilityLostEvent>(OnAbilityLost);
            _eventBus.Unsubscribe<WorkerLevelDowngradedEvent>(OnWorkerLevelDowngraded);
            _eventBus.Unsubscribe<WorkerRoutedEvent>(OnWorkerRouted);
        }

        // ── Event Handlers ──────────────────────────────────────────

        private void OnWorkerWornOut(WorkerWornOutEvent e)
        {
            var worldPos = GridToWorld(e.BuildingPosition);

            // Floating text — red warning
            string text = ForgeStyledVisualElement.GetLocalizedText(LocalizationKeys.NotificationWorkerWornOut);
            SpawnFloatingText(worldPos, string.Format(text, e.WorkerId), new Color(1f, 0.3f, 0.2f));

            // Particle burst — orange/red sparks
            SpawnParticleBurst(worldPos, new Color(1f, 0.4f, 0.1f), 20);

            // Sound
            _audioHandler?.PlayWorkerWornOutSfx();

            Debug.Log($"[WorkerEffects] Worker #{e.WorkerId} worn out — {e.Reason} at ({e.BuildingPosition.X},{e.BuildingPosition.Y})");
        }

        private void OnAbilityGained(AbilityGainedEvent e)
        {
            var worker = FindWorker(e.WorkerId);
            if (worker == null) return;

            var worldPos = GridToWorld(worker.Position);

            // Golden particle burst
            SpawnParticleBurst(worldPos, new Color(1f, 0.85f, 0.2f), 30);

            // Floating text — golden
            string text = ForgeStyledVisualElement.GetLocalizedText(LocalizationKeys.NotificationAbilityGained);
            SpawnFloatingText(worldPos, string.Format(text, e.AbilityName), new Color(1f, 0.85f, 0.3f));

            // Sound
            _audioHandler?.PlayAbilityGainedSfx();

            Debug.Log($"[WorkerEffects] Worker #{e.WorkerId} gained ability: {e.AbilityName}");
        }

        private void OnGoldChanged(GoldChangedEvent e)
        {
            int diff = e.NewAmount - e.OldAmount;
            if (diff > 0)
            {
                // Gold earned — show floating text at screen center-ish
                string text = ForgeStyledVisualElement.GetLocalizedText(LocalizationKeys.NotificationGoldEarned);
                var pos = new Vector3(0, 3f, 0); // Above origin
                SpawnFloatingText(pos, string.Format(text, diff), new Color(1f, 0.85f, 0.2f));
            }
        }

        private void OnAbilityLost(AbilityLostEvent e)
        {
            var worker = FindWorker(e.WorkerId);
            if (worker == null) return;

            var worldPos = GridToWorld(worker.Position);

            // Red particle puff
            SpawnParticleBurst(worldPos, new Color(0.8f, 0.2f, 0.2f), 15);
            SpawnFloatingText(worldPos, string.Format(ForgeStyledVisualElement.GetLocalizedText(LocalizationKeys.NotificationAbilitiesLost), e.AbilitiesLost), new Color(1f, 0.3f, 0.3f));
        }

        private void OnWorkerLevelDowngraded(WorkerLevelDowngradedEvent e)
        {
            var worker = FindWorker(e.WorkerId);
            if (worker == null) return;

            var worldPos = GridToWorld(worker.Position);
            SpawnFloatingText(worldPos, string.Format(ForgeStyledVisualElement.GetLocalizedText(LocalizationKeys.NotificationLevelDowngrade), e.OldLevel, e.NewLevel), new Color(1f, 0.5f, 0.2f));
        }

        private void OnWorkerRouted(WorkerRoutedEvent e)
        {
            var worker = FindWorker(e.WorkerId);
            if (worker == null) return;

            var worldPos = GridToWorld(worker.Position);

            // Small directional arrow particle
            SpawnParticleBurst(worldPos, new Color(0.3f, 0.7f, 1f), 8);
        }

        // ── Effect Spawning ─────────────────────────────────────────

        private void SpawnFloatingText(Vector3 position, string text, Color color)
        {
            _floatingTexts.Add(new FloatingText
            {
                Position = position,
                Text = text,
                Color = color,
                Lifetime = 2.0f,
                Timer = 0f
            });
        }

        private void SpawnParticleBurst(Vector3 position, Color color, int count)
        {
            _particleBursts.Add(new ParticleBurst
            {
                Position = position,
                Color = color,
                Count = count,
                Timer = 0f,
                Lifetime = 1.5f,
                GameObject = CreateParticleBurstObject(position, color, count)
            });
        }

        private GameObject CreateParticleBurstObject(Vector3 position, Color color, int count)
        {
            var go = new GameObject("ParticleBurst");
            go.transform.position = position;

            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = 1.0f;
            main.startSpeed = 3f;
            main.startSize = 0.15f;
            main.startColor = color;
            main.maxParticles = count;
            main.loop = false;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = ps.emission;
            emission.enabled = true;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.3f;

            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0, 1, 1, 0));

            // Set renderer material
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            if (renderer != null)
            {
                renderer.material = RuntimePlaceholderFactory.CreateLitMaterial(color);
            }

            ps.Play();

            return go;
        }

        // ── Update Loop ─────────────────────────────────────────────

        private void Update()
        {
            float dt = Time.deltaTime;

            // Update floating texts
            for (int i = _floatingTexts.Count - 1; i >= 0; i--)
            {
                var ft = _floatingTexts[i];
                ft.Timer += dt;
                ft.Position += Vector3.up * dt * 1.5f;

                if (ft.Timer >= ft.Lifetime)
                {
                    _floatingTexts.RemoveAt(i);
                }
                else
                {
                    _floatingTexts[i] = ft;
                }
            }

            // Clean up expired particle bursts
            for (int i = _particleBursts.Count - 1; i >= 0; i--)
            {
                var pb = _particleBursts[i];
                pb.Timer += dt;

                if (pb.Timer >= pb.Lifetime)
                {
                    if (pb.GameObject != null)
                        Destroy(pb.GameObject);
                    _particleBursts.RemoveAt(i);
                }
                else
                {
                    _particleBursts[i] = pb;
                }
            }
        }

        private void OnGUI()
        {
            // Render floating texts using OnGUI (simple, works without UI Toolkit overlay)
            if (Camera.main == null) return;

            var cam = Camera.main;
            foreach (var ft in _floatingTexts)
            {
                var screenPos = cam.WorldToScreenPoint(ft.Position);
                if (screenPos.z < 0) continue; // Behind camera

                float alpha = 1f - (ft.Timer / ft.Lifetime);
                var guiColor = new Color(ft.Color.r, ft.Color.g, ft.Color.b, alpha);

                var style = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 16,
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = guiColor }
                };

                float y = Screen.height - screenPos.y; // Unity GUI Y is inverted
                GUI.Label(new Rect(screenPos.x - 100, y - 20, 200, 40), ft.Text, style);
            }
        }

        // ── Helpers ─────────────────────────────────────────────────

        private HeroEntity? FindWorker(ulong workerId)
        {
            _simulation.EntityManager.HeroIndex.TryGetValue(new EntityId(workerId), out var worker);
            return worker;
        }

        private static Vector3 GridToWorld(GridPosRPG pos)
        {
            return new Vector3(pos.X, 0.5f, pos.Y);
        }

        // ── Data Structures ─────────────────────────────────────────

        private struct FloatingText
        {
            public Vector3 Position;
            public string Text;
            public Color Color;
            public float Lifetime;
            public float Timer;
        }

        private struct ParticleBurst
        {
            public Vector3 Position;
            public Color Color;
            public int Count;
            public float Timer;
            public float Lifetime;
            public GameObject? GameObject;
        }
    }
}
