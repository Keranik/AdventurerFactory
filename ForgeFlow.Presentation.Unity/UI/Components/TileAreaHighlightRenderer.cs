using ForgeFlow.Core.Entities;
using ForgeFlow.Core.Events;
using UnityEngine;

namespace ForgeFlow.Presentation.Unity.UI.Components
{
    /// <summary>
    /// Renders configurable tile area highlights in world space using pooled quad primitives.
    /// Supports border, pulsation, semi-transparent overlay, and line patterns.
    /// Object-pooled for performance, zero-alloc in the Update hot path,
    /// event-driven via <see cref="TileAreaHighlightEvent"/>.
    /// Presentation layer only — all colors resolved via <see cref="ForgeStyledVisualElement.GetThemeColor"/>.
    /// </summary>
    /// <remarks>
    /// <para>Usage (fluent configuration + manual show/hide):</para>
    /// <code>
    /// var renderer = go.AddComponent&lt;TileAreaHighlightRenderer&gt;();
    /// renderer
    ///     .WithBorderThickness(3)
    ///     .WithBorderColor("accent.cyan")
    ///     .WithPulsating(true)
    ///     .WithOverlayColor("highlight.overlay")
    ///     .WithOverlayAlpha(0.25f)
    ///     .WithLinePattern(TileAreaHighlightRenderer.HighlightLinePattern.Diagonal)
    ///     .Initialize(eventBus);
    ///
    /// renderer.Show("tutorial", GridAreaRPG.FromRect(origin, 3, 3));
    /// renderer.Hide("tutorial");
    /// </code>
    /// <para>Or event-driven via EventBus:</para>
    /// <code>
    /// eventBus.Publish(new TileAreaHighlightEvent("tutorial", area, show: true));
    /// eventBus.Publish(new TileAreaHighlightEvent("tutorial", default, show: false));
    /// </code>
    /// </remarks>
    internal sealed class TileAreaHighlightRenderer : MonoBehaviour
    {
        /// <summary>Line pattern drawn over the tile overlay.</summary>
        public enum HighlightLinePattern
        {
            None,
            Horizontal,
            Vertical,
            Diagonal,
            CautionStripes
        }

        // ── Configuration (snapshotted per-group on Show) ───────────

        private int _borderThickness = 2;
        private string _borderColorKey = "highlight.border";
        private bool _pulsating;
        private string _overlayColorKey = "highlight.overlay";
        private float _overlayAlpha = 0.25f;
        private HighlightLinePattern _linePattern = HighlightLinePattern.None;

        // ── State ───────────────────────────────────────────────────

        private EventBus? _eventBus;
        private readonly Dictionary<string, HighlightGroup> _activeGroups = new();
        private readonly Stack<PooledQuad> _quadPool = new();

        // ── Constants ───────────────────────────────────────────────

        private const float OverlayY = 0.02f;
        private const float BorderY = 0.03f;
        private const float PatternY = 0.04f;
        private const float PulseSpeed = 2.5f;
        private const float BorderWorldScale = 0.02f;
        private const int InitialPoolSize = 64;
        private const int MaxPoolSize = 512;
        private const float StripeWidth = 0.06f;
        private const int StripesPerTile = 4;

        // ── Internal Types ──────────────────────────────────────────

        private struct HighlightConfig
        {
            public int BorderThickness;
            public Color BorderColor;
            public bool Pulsating;
            public Color OverlayColor;
            public float OverlayAlpha;
            public HighlightLinePattern Pattern;
            public Color PatternColor;
        }

        private sealed class HighlightGroup
        {
            public HighlightConfig Config;
            public readonly List<PooledQuad> OverlayQuads = new();
            public readonly List<PooledQuad> BorderQuads = new();
            public readonly List<PooledQuad> PatternQuads = new();
        }

        private sealed class PooledQuad
        {
            public GameObject Go = null!;
            public MeshRenderer? Renderer;
        }

        // ── Fluent Configuration ────────────────────────────────────

        /// <summary>Sets the border thickness in abstract units (each unit ≈ 0.02 world units).</summary>
        public TileAreaHighlightRenderer WithBorderThickness(int thickness)
        {
            _borderThickness = thickness > 0 ? thickness : 0;
            return this;
        }

        /// <summary>Sets the border color via a ThemeService color key.</summary>
        public TileAreaHighlightRenderer WithBorderColor(string themeKey)
        {
            _borderColorKey = themeKey ?? "highlight.border";
            return this;
        }

        /// <summary>Enables or disables smooth sine-based pulsation on overlay quads.</summary>
        public TileAreaHighlightRenderer WithPulsating(bool enabled)
        {
            _pulsating = enabled;
            return this;
        }

        /// <summary>Sets the overlay fill color via a ThemeService color key.</summary>
        public TileAreaHighlightRenderer WithOverlayColor(string themeKey)
        {
            _overlayColorKey = themeKey ?? "highlight.overlay";
            return this;
        }

        /// <summary>Sets the base overlay alpha (0 = invisible, 1 = opaque). Clamped to [0,1].</summary>
        public TileAreaHighlightRenderer WithOverlayAlpha(float alpha)
        {
            _overlayAlpha = Mathf.Clamp01(alpha);
            return this;
        }

        /// <summary>Sets the optional stripe pattern drawn over the overlay.</summary>
        public TileAreaHighlightRenderer WithLinePattern(HighlightLinePattern pattern)
        {
            _linePattern = pattern;
            return this;
        }

        // ── Initialization ──────────────────────────────────────────

        /// <summary>
        /// Initializes the renderer and subscribes to <see cref="TileAreaHighlightEvent"/>.
        /// Call after fluent configuration.
        /// </summary>
        public void Initialize(EventBus eventBus)
        {
            _eventBus = eventBus;
            _eventBus.Subscribe<TileAreaHighlightEvent>(OnHighlightEvent);
            PrewarmPool(InitialPoolSize);
        }

        private void OnDestroy()
        {
            _eventBus?.Unsubscribe<TileAreaHighlightEvent>(OnHighlightEvent);
            HideAll();
            DrainPool();
        }

        // ── Public API ──────────────────────────────────────────────

        /// <summary>
        /// Shows a highlight group over the specified area.
        /// Replaces any existing group with the same ID.
        /// Uses the current fluent configuration, snapshotted at call time.
        /// </summary>
        public void Show(string groupId, GridAreaRPG area)
        {
            if (area.IsEmpty) { Hide(groupId); return; }

            Hide(groupId);

            var config = SnapshotConfig();
            var group = new HighlightGroup { Config = config };
            var tiles = area.GetAllTiles();

            // Build spatial lookup for smart border detection (outer edges only).
            HashSet<long>? tileHash = null;
            if (config.BorderThickness > 0)
            {
                tileHash = new HashSet<long>(tiles.Length);
                for (int i = 0; i < tiles.Length; i++)
                {
                    tileHash.Add(PackTile(tiles[i]));
                }
            }

            for (int i = 0; i < tiles.Length; i++)
            {
                var tile = tiles[i];
                var worldPos = new Vector3(tile.X, OverlayY, tile.Y);

                // Base overlay quad
                var overlay = AcquireQuad();
                ConfigureAsOverlay(overlay, worldPos, config);
                group.OverlayQuads.Add(overlay);

                // Border edges — only on sides adjacent to non-highlighted tiles
                if (tileHash != null)
                {
                    float bt = config.BorderThickness * BorderWorldScale;
                    CreateOuterBorders(group, tile, worldPos, bt, config.BorderColor, tileHash);
                }

                // Pattern stripes
                if (config.Pattern != HighlightLinePattern.None)
                {
                    CreatePatternStripes(group, new Vector3(tile.X, PatternY, tile.Y), config);
                }
            }

            _activeGroups[groupId] = group;
        }

        /// <summary>Shows a highlight for a single tile position.</summary>
        public void Show(string groupId, GridPosRPG position)
        {
            Show(groupId, GridAreaRPG.FromSingleTile(position));
        }

        /// <summary>Hides and releases a specific highlight group.</summary>
        public void Hide(string groupId)
        {
            if (!_activeGroups.TryGetValue(groupId, out var group)) { return; }
            ReleaseGroup(group);
            _activeGroups.Remove(groupId);
        }

        /// <summary>Hides all active highlight groups.</summary>
        public void HideAll()
        {
            foreach (var group in _activeGroups.Values)
            {
                ReleaseGroup(group);
            }
            _activeGroups.Clear();
        }

        /// <summary>Returns true if the specified group is currently visible.</summary>
        public bool IsVisible(string groupId) => _activeGroups.ContainsKey(groupId);

        /// <summary>Returns the number of currently active highlight groups.</summary>
        public int ActiveGroupCount => _activeGroups.Count;

        // ── Event Handler ───────────────────────────────────────────

        private void OnHighlightEvent(TileAreaHighlightEvent e)
        {
            if (e.Show)
            {
                Show(e.GroupId, e.Area);
            }
            else
            {
                Hide(e.GroupId);
            }
        }

        // ── Pulsation Update (hot path — zero-alloc) ────────────────

        private void Update()
        {
            if (_activeGroups.Count == 0) { return; }

            float time = Time.time;

            foreach (var kvp in _activeGroups)
            {
                var group = kvp.Value;
                if (!group.Config.Pulsating) { continue; }

                // Smooth sine-based alpha oscillation
                float t = (Mathf.Sin(time * PulseSpeed * Mathf.PI) + 1f) * 0.5f;
                float overlayAlpha = Mathf.Lerp(group.Config.OverlayAlpha * 0.3f, group.Config.OverlayAlpha, t);
                var baseColor = group.Config.OverlayColor;
                var pulseColor = new Color(baseColor.r, baseColor.g, baseColor.b, overlayAlpha);

                var overlays = group.OverlayQuads;
                for (int i = 0; i < overlays.Count; i++)
                {
                    SetQuadColor(overlays[i], pulseColor);
                }

                // Complementary pulse on pattern stripes
                if (group.Config.Pattern != HighlightLinePattern.None)
                {
                    float patternAlpha = Mathf.Clamp01(Mathf.Lerp(overlayAlpha * 0.6f, overlayAlpha * 1.4f, t));
                    var patternBase = group.Config.PatternColor;
                    var patternPulse = new Color(patternBase.r, patternBase.g, patternBase.b, patternAlpha);

                    var patterns = group.PatternQuads;
                    for (int i = 0; i < patterns.Count; i++)
                    {
                        SetQuadColor(patterns[i], patternPulse);
                    }
                }
            }
        }

        // ── Quad Configuration ──────────────────────────────────────

        private static void ConfigureAsOverlay(PooledQuad quad, Vector3 worldPos, in HighlightConfig config)
        {
            var tr = quad.Go.transform;
            tr.position = worldPos;
            tr.rotation = Quaternion.Euler(90f, 0f, 0f);
            tr.localScale = Vector3.one;

            var color = new Color(config.OverlayColor.r, config.OverlayColor.g, config.OverlayColor.b, config.OverlayAlpha);
            SetQuadColor(quad, color);
        }

        private void CreateOuterBorders(
            HighlightGroup group, GridPosRPG tile, Vector3 worldPos,
            float borderThickness, Color borderColor, HashSet<long> tileHash)
        {
            // North edge (tile.Y + 1 absent)
            if (!tileHash.Contains(PackTile(tile.X, tile.Y + 1)))
            {
                var quad = AcquireQuad();
                PositionBorderEdge(quad, worldPos, 0f, 0.5f, 1f + borderThickness, borderThickness, borderColor);
                group.BorderQuads.Add(quad);
            }
            // South edge (tile.Y - 1 absent)
            if (!tileHash.Contains(PackTile(tile.X, tile.Y - 1)))
            {
                var quad = AcquireQuad();
                PositionBorderEdge(quad, worldPos, 0f, -0.5f, 1f + borderThickness, borderThickness, borderColor);
                group.BorderQuads.Add(quad);
            }
            // East edge (tile.X + 1 absent)
            if (!tileHash.Contains(PackTile(tile.X + 1, tile.Y)))
            {
                var quad = AcquireQuad();
                PositionBorderEdge(quad, worldPos, 0.5f, 0f, borderThickness, 1f + borderThickness, borderColor);
                group.BorderQuads.Add(quad);
            }
            // West edge (tile.X - 1 absent)
            if (!tileHash.Contains(PackTile(tile.X - 1, tile.Y)))
            {
                var quad = AcquireQuad();
                PositionBorderEdge(quad, worldPos, -0.5f, 0f, borderThickness, 1f + borderThickness, borderColor);
                group.BorderQuads.Add(quad);
            }
        }

        private static void PositionBorderEdge(
            PooledQuad quad, Vector3 tileCenter,
            float offsetX, float offsetZ,
            float scaleX, float scaleZ,
            Color color)
        {
            var tr = quad.Go.transform;
            tr.position = new Vector3(tileCenter.x + offsetX, BorderY, tileCenter.z + offsetZ);
            tr.rotation = Quaternion.Euler(90f, 0f, 0f);
            tr.localScale = new Vector3(scaleX, scaleZ, 1f);
            SetQuadColor(quad, color);
        }

        private void CreatePatternStripes(HighlightGroup group, Vector3 worldPos, in HighlightConfig config)
        {
            var color = config.PatternColor;
            float spacing = 1f / (StripesPerTile + 1);

            switch (config.Pattern)
            {
                case HighlightLinePattern.Horizontal:
                    for (int s = 1; s <= StripesPerTile; s++)
                    {
                        var q = AcquireQuad();
                        float zOff = -0.5f + s * spacing;
                        PositionStripe(q, worldPos, 0f, zOff, 0.9f, StripeWidth, 0f, color);
                        group.PatternQuads.Add(q);
                    }
                    break;

                case HighlightLinePattern.Vertical:
                    for (int s = 1; s <= StripesPerTile; s++)
                    {
                        var q = AcquireQuad();
                        float xOff = -0.5f + s * spacing;
                        PositionStripe(q, worldPos, xOff, 0f, StripeWidth, 0.9f, 0f, color);
                        group.PatternQuads.Add(q);
                    }
                    break;

                case HighlightLinePattern.Diagonal:
                    for (int s = 1; s <= StripesPerTile; s++)
                    {
                        var q = AcquireQuad();
                        float off = -0.5f + s * spacing;
                        PositionStripe(q, worldPos, off * 0.5f, off * 0.5f, StripeWidth, 1.2f, 45f, color);
                        group.PatternQuads.Add(q);
                    }
                    break;

                case HighlightLinePattern.CautionStripes:
                    int halfStripes = StripesPerTile / 2;
                    if (halfStripes < 1) { halfStripes = 1; }
                    float cautionSpacing = 1f / (halfStripes + 1);
                    for (int s = 1; s <= halfStripes; s++)
                    {
                        float off = -0.5f + s * cautionSpacing;

                        var q1 = AcquireQuad();
                        PositionStripe(q1, worldPos, off * 0.4f, off * 0.4f, StripeWidth, 1.0f, 45f, color);
                        group.PatternQuads.Add(q1);

                        var q2 = AcquireQuad();
                        PositionStripe(q2, worldPos, -(off * 0.4f), off * 0.4f, StripeWidth, 1.0f, -45f, color);
                        group.PatternQuads.Add(q2);
                    }
                    break;
            }
        }

        private static void PositionStripe(
            PooledQuad quad, Vector3 tileCenter,
            float offsetX, float offsetZ,
            float scaleX, float scaleZ,
            float rotationY, Color color)
        {
            var tr = quad.Go.transform;
            tr.position = new Vector3(tileCenter.x + offsetX, tileCenter.y, tileCenter.z + offsetZ);
            tr.rotation = Quaternion.Euler(90f, rotationY, 0f);
            tr.localScale = new Vector3(scaleX, scaleZ, 1f);
            SetQuadColor(quad, color);
        }

        // ── Color Helpers ───────────────────────────────────────────

        private static void SetQuadColor(PooledQuad quad, Color color)
        {
            if (quad.Renderer != null && quad.Renderer.material != null)
            {
                quad.Renderer.material.color = color;
            }
        }

        // ── Config Snapshot ─────────────────────────────────────────

        private HighlightConfig SnapshotConfig()
        {
            var overlayColor = ForgeStyledVisualElement.GetThemeColor(_overlayColorKey);
            var borderColor = ForgeStyledVisualElement.GetThemeColor(_borderColorKey);
            return new HighlightConfig
            {
                BorderThickness = _borderThickness,
                BorderColor = borderColor,
                Pulsating = _pulsating,
                OverlayColor = overlayColor,
                OverlayAlpha = _overlayAlpha,
                Pattern = _linePattern,
                PatternColor = new Color(borderColor.r, borderColor.g, borderColor.b, _overlayAlpha * 0.8f)
            };
        }

        // ── Pool Management ─────────────────────────────────────────

        private PooledQuad AcquireQuad()
        {
            PooledQuad quad;
            if (_quadPool.Count > 0)
            {
                quad = _quadPool.Pop();
            }
            else
            {
                quad = CreateQuad();
            }
            quad.Go.SetActive(true);
            return quad;
        }

        private void ReleaseQuad(PooledQuad quad)
        {
            quad.Go.SetActive(false);
            if (_quadPool.Count < MaxPoolSize)
            {
                _quadPool.Push(quad);
            }
            else
            {
                DestroyQuad(quad);
            }
        }

        private void ReleaseGroup(HighlightGroup group)
        {
            for (int i = 0; i < group.OverlayQuads.Count; i++) { ReleaseQuad(group.OverlayQuads[i]); }
            for (int i = 0; i < group.BorderQuads.Count; i++) { ReleaseQuad(group.BorderQuads[i]); }
            for (int i = 0; i < group.PatternQuads.Count; i++) { ReleaseQuad(group.PatternQuads[i]); }
            group.OverlayQuads.Clear();
            group.BorderQuads.Clear();
            group.PatternQuads.Clear();
        }

        private void PrewarmPool(int count)
        {
            for (int i = 0; i < count; i++)
            {
                var quad = CreateQuad();
                quad.Go.SetActive(false);
                _quadPool.Push(quad);
            }
        }

        private void DrainPool()
        {
            while (_quadPool.Count > 0)
            {
                DestroyQuad(_quadPool.Pop());
            }
        }

        private PooledQuad CreateQuad()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = "TileHighlightQuad";
            go.transform.SetParent(transform);

            // Remove collider — not needed for visual overlay
            var col = go.GetComponent<Collider>();
            if (col != null) { Destroy(col); }

            var renderer = go.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.material = CreateTransparentMaterial(Color.clear);
            }

            return new PooledQuad { Go = go, Renderer = renderer };
        }

        private void DestroyQuad(PooledQuad quad)
        {
            if (quad.Renderer != null && quad.Renderer.material != null)
            {
                Destroy(quad.Renderer.material);
            }
            if (quad.Go != null)
            {
                Destroy(quad.Go);
            }
        }

        // ── Material Creation ───────────────────────────────────────

        /// <summary>
        /// Creates a transparent material using Sprites/Default shader (alpha-blended by default)
        /// so that overlay alpha works without manual shader keyword configuration.
        /// </summary>
        private static Material CreateTransparentMaterial(Color color)
        {
            var shader = Shader.Find("Sprites/Default")
                      ?? Shader.Find("Unlit/Color")
                      ?? Shader.Find("Standard");

            var mat = shader != null
                ? new Material(shader)
                : new Material(Shader.Find("Hidden/InternalErrorShader")!);

            mat.color = color;
            return mat;
        }

        // ── Tile Hashing ────────────────────────────────────────────

        private static long PackTile(GridPosRPG pos) => PackTile(pos.X, pos.Y);
        private static long PackTile(int x, int y) => ((long)x << 32) | (uint)y;
    }
}
