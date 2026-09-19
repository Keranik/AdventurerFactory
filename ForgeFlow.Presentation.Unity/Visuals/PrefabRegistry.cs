using UnityEngine;

namespace ForgeFlow.Presentation.Unity.Visuals
{
    /// <summary>
    /// Loads and caches prefab references from the Resources folder.
    /// Static class so any Mb wrapper or system can access prefabs without
    /// constructor injection (mirrors the existing RuntimePlaceholderFactory pattern).
    /// Falls back gracefully — callers must null-check GetPrefab results.
    /// </summary>
    internal static class PrefabRegistry
    {
        private static readonly Dictionary<string, GameObject> Prefabs = new();
        private static bool _initialized;

        /// <summary>
        /// Call once during startup (from FactoryEntryPoint.Initialize).
        /// Loads all known prefabs from Resources/. Safe to call multiple times.
        /// </summary>
        public static void Initialize()
        {
            if (_initialized) { return; }
            _initialized = true;

            // ── Paths ──
            TryLoad("PathStraight", "Entities/Prefabs/Paths/PathStraight");
            TryLoad("PathT", "Entities/Prefabs/Paths/PathT");
            TryLoad("PathGate", "Entities/Prefabs/Paths/PathGate");

            Debug.Log($"[PrefabRegistry] Initialized — {Prefabs.Count} prefab(s) loaded.");
        }

        /// <summary>
        /// Returns the cached prefab for the given key, or null if not loaded.
        /// Callers should fall back to RuntimePlaceholderFactory when null.
        /// </summary>
        public static GameObject? GetPrefab(string key)
        {
            Prefabs.TryGetValue(key, out var prefab);
            return prefab;
        }

        /// <summary>Returns true if a prefab is loaded for the given key.</summary>
        public static bool HasPrefab(string key) => Prefabs.ContainsKey(key);

        private static void TryLoad(string key, string resourcePath)
        {
            var prefab = Resources.Load<GameObject>(resourcePath);
            if (prefab != null)
            {
                Prefabs[key] = prefab;
            }
            else
            {
                Debug.LogWarning($"[PrefabRegistry] Prefab not found at Resources/{resourcePath}");
            }
        }
    }
}
