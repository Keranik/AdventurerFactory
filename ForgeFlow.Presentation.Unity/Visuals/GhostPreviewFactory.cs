using UnityEngine;

namespace ForgeFlow.Presentation.Unity.Visuals
{
    /// <summary>
    /// Generic factory for creating ghost (semi-transparent) preview GameObjects.
    /// Accepts any prefab or existing GameObject, strips colliders, applies
    /// ghost materials, and returns a <see cref="GhostInstance"/> with built-in
    /// validity coloring support.
    /// </summary>
    internal static class GhostPreviewFactory
    {
        /// <summary>
        /// Creates a ghost preview by cloning <paramref name="prefab"/>.
        /// Strips all colliders and applies semi-transparent ghost materials.
        /// </summary>
        /// <param name="prefab">The prefab to clone.</param>
        /// <param name="ghostAlpha">Opacity for the ghost materials (0–1).</param>
        /// <param name="invalidTint">Color applied when <see cref="GhostInstance.SetValidity"/> is false.</param>
        public static GhostInstance CreateGhostFromPrefab(
            GameObject prefab,
            float ghostAlpha = 0.35f,
            Color? invalidTint = null)
        {
            var instance = UnityEngine.Object.Instantiate(prefab);
            return SetupGhost(instance, ghostAlpha, invalidTint ?? new Color(0.8f, 0.15f, 0.15f));
        }

        /// <summary>
        /// Wraps an already-created GameObject as a ghost (NOT cloned).
        /// Strips all colliders and applies semi-transparent ghost materials.
        /// Use this for runtime-primitive fallbacks that are already instantiated.
        /// </summary>
        /// <param name="instance">The existing GameObject to convert in-place.</param>
        /// <param name="ghostAlpha">Opacity for the ghost materials (0–1).</param>
        /// <param name="invalidTint">Color applied when <see cref="GhostInstance.SetValidity"/> is false.</param>
        public static GhostInstance CreateGhostFromInstance(
            GameObject instance,
            float ghostAlpha = 0.35f,
            Color? invalidTint = null)
        {
            return SetupGhost(instance, ghostAlpha, invalidTint ?? new Color(0.8f, 0.15f, 0.15f));
        }

        private static GhostInstance SetupGhost(GameObject root, float ghostAlpha, Color invalidTint)
        {
            // Strip all colliders so the ghost doesn't interfere with raycasts
            foreach (var collider in root.GetComponentsInChildren<Collider>())
            {
                UnityEngine.Object.Destroy(collider);
            }

            var renderers = root.GetComponentsInChildren<Renderer>();
            var ghostColors = new Color[renderers.Length];

            for (int i = 0; i < renderers.Length; i++)
            {
                var originalMat = renderers[i].material;
                var original = originalMat.color;
                var ghostColor = new Color(original.r, original.g, original.b, ghostAlpha);

                // Clone the material so textures are preserved, then make it transparent
                var ghostMat = new Material(originalMat);
                ghostMat.color = ghostColor;
                SetMaterialTransparent(ghostMat);

                renderers[i].material = ghostMat;
                ghostColors[i] = ghostColor;
            }

            return new GhostInstance(root, renderers, ghostColors, invalidTint);
        }

        /// <summary>
        /// Switches a Standard shader material to Fade rendering mode so it
        /// supports transparency while preserving any existing textures.
        /// </summary>
        private static void SetMaterialTransparent(Material mat)
        {
            if (mat.HasProperty("_Mode"))
            {
                mat.SetFloat("_Mode", 2f); // Fade
                mat.SetInt("_SrcBlend", 5);  // SrcAlpha
                mat.SetInt("_DstBlend", 10); // OneMinusSrcAlpha
                mat.SetInt("_ZWrite", 0);
                mat.DisableKeyword("_ALPHATEST_ON");
                mat.EnableKeyword("_ALPHABLEND_ON");
                mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                mat.renderQueue = 3000;
            }
        }
    }

    /// <summary>
    /// Wraps a ghost preview GameObject with cached renderer references and
    /// validity coloring. Created exclusively by <see cref="GhostPreviewFactory"/>.
    /// </summary>
    internal sealed class GhostInstance
    {
        public GameObject Root { get; }

        private readonly Renderer[] _renderers;
        private readonly Color[] _ghostColors;
        private readonly Color _invalidTint;

        internal GhostInstance(GameObject root, Renderer[] renderers, Color[] ghostColors, Color invalidTint)
        {
            Root = root;
            _renderers = renderers;
            _ghostColors = ghostColors;
            _invalidTint = invalidTint;
        }

        /// <summary>Moves the ghost root to the given world position.</summary>
        public void SetPosition(Vector3 position) => Root.transform.position = position;

        /// <summary>Sets the ghost root rotation.</summary>
        public void SetRotation(Quaternion rotation) => Root.transform.rotation = rotation;

        /// <summary>
        /// Applies validity coloring to all renderers.
        /// <c>true</c> = original ghost color (green/semi-transparent).
        /// <c>false</c> = invalid tint (default red) with the same alpha.
        /// </summary>
        public void SetValidity(bool isValid)
        {
            for (int i = 0; i < _renderers.Length; i++)
            {
                if (_renderers[i] == null) { continue; }
                float alpha = i < _ghostColors.Length ? _ghostColors[i].a : 0.35f;
                _renderers[i].material.color = isValid
                    ? _ghostColors[i]
                    : new Color(_invalidTint.r, _invalidTint.g, _invalidTint.b, alpha);
            }
        }

        /// <summary>Destroys the ghost GameObject. Safe to call multiple times.</summary>
        public void Destroy()
        {
            if (Root != null)
            {
                UnityEngine.Object.Destroy(Root);
            }
        }
    }
}
