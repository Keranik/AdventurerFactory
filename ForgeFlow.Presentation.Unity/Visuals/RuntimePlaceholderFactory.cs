using ForgeFlow.Core.Entities;
using ForgeFlow.Core.Proto;
using ForgeFlow.Core.Proto.Prototypes;
using UnityEngine;

namespace ForgeFlow.Presentation.Unity.Visuals
{

/// <summary>
/// Creates all placeholder visuals at runtime using Unity primitives
/// and simple colored materials. No manual prefabs required.
/// </summary>
internal static class RuntimePlaceholderFactory
{
    private static readonly Dictionary<string, Color> ClassColors = new()
    {
        { "warrior", new Color(0.2f, 0.5f, 1.0f) },
        { "rogue", new Color(0.1f, 0.8f, 0.2f) },
        { "mage", new Color(0.7f, 0.2f, 0.9f) },
        { "paladin", new Color(1.0f, 0.85f, 0.2f) },
        { "archer", new Color(0.6f, 0.4f, 0.2f) },
        { "necromancer", new Color(0.3f, 0.1f, 0.3f) },
        { "spellsword", new Color(0.4f, 0.4f, 0.9f) },
    };

    private static readonly Dictionary<string, Color> StructureColors = new()
    {
        { "Spawner", new Color(0.2f, 0.8f, 0.2f) },
        { "Forge", new Color(1.0f, 0.4f, 0.1f) },
        { "Smelter", new Color(0.8f, 0.2f, 0.1f) },
        { "DungeonPortal", new Color(0.5f, 0.1f, 0.8f) },
        { "FusionAltar", new Color(0.9f, 0.9f, 0.2f) },
        { "AppearanceWorkshop", new Color(0.2f, 0.9f, 0.9f) },
        { "MiningNode", new Color(0.5f, 0.4f, 0.3f) },
        { "ManaExtractor", new Color(0.2f, 0.6f, 0.9f) },
        { "Forestry", new Color(0.1f, 0.6f, 0.2f) },
        { "Inn", new Color(0.9f, 0.7f, 0.3f) },
        { "CraftStation", new Color(0.6f, 0.5f, 0.8f) },
        { "Stockpile", new Color(0.5f, 0.5f, 0.4f) },
        { "TrainingBuilding", new Color(0.3f, 0.4f, 0.9f) },
        { "PathGate", new Color(0.4f, 0.8f, 0.3f) },
        { "FilterSplitter", new Color(0.9f, 0.5f, 0.2f) },
    };

    internal static Material CreateLitMaterial(Color color)
    {
        var shader = Shader.Find("Standard")
                  ?? Shader.Find("Unlit/Color")
                  ?? Shader.Find("Hidden/InternalErrorShader");

        var mat = shader != null ? new Material(shader) : new Material(Shader.Find("Sprites/Default")!);
        mat.color = color;
        return mat;
    }

    private static Material CreateEmissiveMaterial(Color baseColor, float emissionIntensity)
    {
        var mat = CreateLitMaterial(baseColor);
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", baseColor * emissionIntensity);
        return mat;
    }

    private static Color Darken(Color c, float amount)
    {
        return new Color(
            Mathf.Max(0f, c.r - amount),
            Mathf.Max(0f, c.g - amount),
            Mathf.Max(0f, c.b - amount),
            c.a);
    }

    private static Color Brighten(Color c, float amount)
    {
        return new Color(
            Mathf.Min(1f, c.r + amount),
            Mathf.Min(1f, c.g + amount),
            Mathf.Min(1f, c.b + amount),
            c.a);
    }

    /// <summary>
    /// Creates a hero placeholder: Capsule body + base disc + child objects (Sword, Armor, LevelGlow, GhostEffect).
    /// </summary>
    public static GameObject CreateVisualHeroPlaceholder(string classId)
    {
        var root = new GameObject($"Hero_{classId}");
        var bodyColor = ClassColors.GetValueOrDefault(classId, Color.white);

        // Ground disc for visual grounding
        var basePlate = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        basePlate.name = "BasePlate";
        basePlate.transform.SetParent(root.transform);
        basePlate.transform.localPosition = new Vector3(0, 0.01f, 0);
        basePlate.transform.localScale = new Vector3(0.35f, 0.01f, 0.35f);
        var baseRenderer = basePlate.GetComponent<MeshRenderer>();
        if (baseRenderer != null)
        {
            baseRenderer.material = CreateEmissiveMaterial(Darken(bodyColor, 0.15f), 0.3f);
        }

        // Body = Capsule
        var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.name = "Body";
        body.transform.SetParent(root.transform);
        body.transform.localPosition = new Vector3(0, 0.45f, 0);
        body.transform.localScale = new Vector3(0.28f, 0.35f, 0.28f);
        var bodyRenderer = body.GetComponent<MeshRenderer>();
        if (bodyRenderer != null)
        {
            bodyRenderer.material = CreateLitMaterial(bodyColor);
        }

        // Shoulder pads (small cubes on each side for silhouette)
        var shoulderL = GameObject.CreatePrimitive(PrimitiveType.Cube);
        shoulderL.name = "ShoulderL";
        shoulderL.transform.SetParent(root.transform);
        shoulderL.transform.localPosition = new Vector3(-0.16f, 0.6f, 0);
        shoulderL.transform.localScale = new Vector3(0.08f, 0.06f, 0.12f);
        var slRenderer = shoulderL.GetComponent<MeshRenderer>();
        if (slRenderer != null)
        {
            slRenderer.material = CreateLitMaterial(Darken(bodyColor, 0.1f));
        }

        var shoulderR = GameObject.CreatePrimitive(PrimitiveType.Cube);
        shoulderR.name = "ShoulderR";
        shoulderR.transform.SetParent(root.transform);
        shoulderR.transform.localPosition = new Vector3(0.16f, 0.6f, 0);
        shoulderR.transform.localScale = new Vector3(0.08f, 0.06f, 0.12f);
        var srRenderer = shoulderR.GetComponent<MeshRenderer>();
        if (srRenderer != null)
        {
            srRenderer.material = CreateLitMaterial(Darken(bodyColor, 0.1f));
        }

        // Sword child (hidden until equipped)
        var sword = GameObject.CreatePrimitive(PrimitiveType.Cube);
        sword.name = "Sword";
        sword.transform.SetParent(root.transform);
        sword.transform.localPosition = new Vector3(0.22f, 0.55f, 0);
        sword.transform.localScale = new Vector3(0.04f, 0.38f, 0.04f);
        sword.SetActive(false);
        var swordRenderer = sword.GetComponent<MeshRenderer>();
        if (swordRenderer != null)
        {
            swordRenderer.material = CreateLitMaterial(new Color(0.8f, 0.82f, 0.9f));
        }

        // Sword guard (crossbar, hidden until equipped)
        var guard = GameObject.CreatePrimitive(PrimitiveType.Cube);
        guard.name = "SwordGuard";
        guard.transform.SetParent(sword.transform);
        guard.transform.localPosition = new Vector3(0, -0.35f, 0);
        guard.transform.localScale = new Vector3(3f, 0.2f, 1.5f);
        var guardRenderer = guard.GetComponent<MeshRenderer>();
        if (guardRenderer != null)
        {
            guardRenderer.material = CreateLitMaterial(new Color(0.6f, 0.55f, 0.3f));
        }

        // Armor indicator (small sphere on chest)
        var armor = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        armor.name = "Armor";
        armor.transform.SetParent(root.transform);
        armor.transform.localPosition = new Vector3(0, 0.5f, 0.14f);
        armor.transform.localScale = new Vector3(0.14f, 0.14f, 0.08f);
        armor.SetActive(false);
        var armorRenderer = armor.GetComponent<MeshRenderer>();
        if (armorRenderer != null)
        {
            armorRenderer.material = CreateLitMaterial(new Color(0.5f, 0.5f, 0.55f));
        }

        // Level glow (emissive sphere above head)
        var levelGlow = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        levelGlow.name = "LevelGlow";
        levelGlow.transform.SetParent(root.transform);
        levelGlow.transform.localPosition = new Vector3(0, 0.95f, 0);
        levelGlow.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
        levelGlow.SetActive(false);
        var levelRenderer = levelGlow.GetComponent<MeshRenderer>();
        if (levelRenderer != null)
        {
            levelRenderer.material = CreateEmissiveMaterial(new Color(1f, 1f, 0.3f), 0.8f);
        }

        // Ghost effect (transparent overlay sphere, hidden by default)
        var ghostEffect = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        ghostEffect.name = "GhostEffect";
        ghostEffect.transform.SetParent(root.transform);
        ghostEffect.transform.localPosition = new Vector3(0, 0.45f, 0);
        ghostEffect.transform.localScale = new Vector3(0.42f, 0.72f, 0.42f);
        ghostEffect.SetActive(false);
        var ghostRenderer = ghostEffect.GetComponent<MeshRenderer>();
        if (ghostRenderer != null)
        {
            ghostRenderer.material = CreateLitMaterial(new Color(0.5f, 0.5f, 0.8f, 0.25f));
        }

        return root;
    }

    /// <summary>
    /// Creates a structure preview placeholder: building silhouette ghost with edge glow
    /// and peaked roof. PathGate gets a gate-frame style preview.
    /// </summary>
    public static GameObject CreateStructurePreviewPlaceholder(string category)
    {
        if (category == "PathGate")
        {
            return CreatePathGatePreview();
        }

        if (category == "FilterSplitter")
        {
            return CreateFilterSplitterPreview();
        }

        var root = new GameObject($"Ghost_{category}");
        var color = StructureColors.GetValueOrDefault(category, Color.white);
        var ghostColor = new Color(color.r, color.g, color.b, 0.35f);
        var edgeColor = new Color(color.r, color.g, color.b, 0.55f);

        // Foundation outline (flat plate)
        var foundation = GameObject.CreatePrimitive(PrimitiveType.Cube);
        foundation.name = "Foundation";
        foundation.transform.SetParent(root.transform);
        foundation.transform.localPosition = new Vector3(0, -0.38f, 0);
        foundation.transform.localScale = new Vector3(0.92f, 0.04f, 0.92f);
        var foundRenderer = foundation.GetComponent<MeshRenderer>();
        if (foundRenderer != null)
        {
            foundRenderer.material = CreateEmissiveMaterial(edgeColor, 0.4f);
        }

        // Main walls (semi-transparent body)
        var walls = GameObject.CreatePrimitive(PrimitiveType.Cube);
        walls.name = "Walls";
        walls.transform.SetParent(root.transform);
        walls.transform.localPosition = new Vector3(0, 0f, 0);
        walls.transform.localScale = new Vector3(0.78f, 0.7f, 0.78f);
        var wallRenderer = walls.GetComponent<MeshRenderer>();
        if (wallRenderer != null)
        {
            wallRenderer.material = CreateLitMaterial(ghostColor);
        }

        // Peaked roof (rotated cube forming diamond cross-section)
        var roof = GameObject.CreatePrimitive(PrimitiveType.Cube);
        roof.name = "Roof";
        roof.transform.SetParent(root.transform);
        roof.transform.localPosition = new Vector3(0, 0.45f, 0);
        roof.transform.localRotation = Quaternion.Euler(0, 0, 45f);
        roof.transform.localScale = new Vector3(0.42f, 0.42f, 0.84f);
        var roofRenderer = roof.GetComponent<MeshRenderer>();
        if (roofRenderer != null)
        {
            roofRenderer.material = CreateLitMaterial(new Color(color.r * 0.7f, color.g * 0.7f, color.b * 0.7f, 0.4f));
        }

        // Edge glow strips (vertical corners)
        AddGhostEdgeStrip(root, edgeColor, new Vector3(-0.39f, 0f, -0.39f));
        AddGhostEdgeStrip(root, edgeColor, new Vector3(0.39f, 0f, -0.39f));
        AddGhostEdgeStrip(root, edgeColor, new Vector3(-0.39f, 0f, 0.39f));
        AddGhostEdgeStrip(root, edgeColor, new Vector3(0.39f, 0f, 0.39f));

        return root;
    }

    private static void AddGhostEdgeStrip(GameObject parent, Color color, Vector3 position)
    {
        var strip = GameObject.CreatePrimitive(PrimitiveType.Cube);
        strip.name = "EdgeStrip";
        strip.transform.SetParent(parent.transform);
        strip.transform.localPosition = position;
        strip.transform.localScale = new Vector3(0.03f, 0.72f, 0.03f);
        var renderer = strip.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            renderer.material = CreateEmissiveMaterial(color, 0.6f);
        }
    }

    /// <summary>
    /// Creates a PathGate ghost preview shaped like a path tile so it blends
    /// naturally with path segments. A threshold bar marks the gate boundary
    /// and a single chevron shows flow direction. The root is rotated by
    /// StructurePlacer; +Z = forward / flow direction.
    /// </summary>
    internal static GameObject CreatePathGatePreview()
    {
        var root = new GameObject("Ghost_PathGate");
        var tileColor = new Color(0.3f, 0.65f, 0.35f, 0.4f);
        var edgeColor = new Color(0.4f, 0.75f, 0.45f, 0.55f);
        var thresholdColor = new Color(1f, 0.9f, 0.3f, 0.65f);
        var chevronColor = new Color(0.5f, 0.95f, 0.55f, 0.7f);

        // Path-style tile base
        var tile = GameObject.CreatePrimitive(PrimitiveType.Cube);
        tile.name = "Tile";
        tile.transform.SetParent(root.transform);
        tile.transform.localPosition = Vector3.zero;
        tile.transform.localScale = new Vector3(0.9f, 0.08f, 0.9f);
        var tileRenderer = tile.GetComponent<MeshRenderer>();
        if (tileRenderer != null)
        {
            tileRenderer.material = CreateLitMaterial(tileColor);
        }

        // Edge rails (match path segment style)
        var railL = GameObject.CreatePrimitive(PrimitiveType.Cube);
        railL.name = "RailL";
        railL.transform.SetParent(root.transform);
        railL.transform.localPosition = new Vector3(-0.43f, 0.06f, 0);
        railL.transform.localScale = new Vector3(0.04f, 0.06f, 0.88f);
        var rlRenderer = railL.GetComponent<MeshRenderer>();
        if (rlRenderer != null)
        {
            rlRenderer.material = CreateEmissiveMaterial(edgeColor, 0.4f);
        }

        var railR = GameObject.CreatePrimitive(PrimitiveType.Cube);
        railR.name = "RailR";
        railR.transform.SetParent(root.transform);
        railR.transform.localPosition = new Vector3(0.43f, 0.06f, 0);
        railR.transform.localScale = new Vector3(0.04f, 0.06f, 0.88f);
        var rrRenderer = railR.GetComponent<MeshRenderer>();
        if (rrRenderer != null)
        {
            rrRenderer.material = CreateEmissiveMaterial(edgeColor, 0.4f);
        }

        // Threshold bar at -Z end (the gate/door boundary line)
        var threshold = GameObject.CreatePrimitive(PrimitiveType.Cube);
        threshold.name = "Threshold";
        threshold.transform.SetParent(root.transform);
        threshold.transform.localPosition = new Vector3(0, 0.07f, -0.3f);
        threshold.transform.localScale = new Vector3(0.7f, 0.06f, 0.06f);
        var thRenderer = threshold.GetComponent<MeshRenderer>();
        if (thRenderer != null)
        {
            thRenderer.material = CreateEmissiveMaterial(thresholdColor, 0.6f);
        }

        // Single chevron at +Z end (flow direction indicator)
        AddChevron(root, chevronColor, new Vector3(0, 0.06f, 0.22f));

        return root;
    }

    /// <summary>
    /// Creates a Filter Splitter ghost preview shaped like a path tile with
    /// three directional dots on edges to indicate the split directions.
    /// Looks like a regular path segment but with split-route markers.
    /// </summary>
    internal static GameObject CreateFilterSplitterPreview()
    {
        var root = new GameObject("Ghost_FilterSplitter");
        var tileColor = new Color(0.65f, 0.45f, 0.15f, 0.4f);
        var edgeColor = new Color(0.8f, 0.55f, 0.2f, 0.55f);
        var dotColor = new Color(1f, 0.75f, 0.25f, 0.7f);

        // Path-style tile base (flat, same size as a regular path)
        var tile = GameObject.CreatePrimitive(PrimitiveType.Cube);
        tile.name = "Tile";
        tile.transform.SetParent(root.transform);
        tile.transform.localPosition = Vector3.zero;
        tile.transform.localScale = new Vector3(0.9f, 0.08f, 0.9f);
        var tileRenderer = tile.GetComponent<MeshRenderer>();
        if (tileRenderer != null)
        {
            tileRenderer.material = CreateLitMaterial(tileColor);
        }

        // Edge rails (match path segment style)
        var railL = GameObject.CreatePrimitive(PrimitiveType.Cube);
        railL.name = "RailL";
        railL.transform.SetParent(root.transform);
        railL.transform.localPosition = new Vector3(-0.43f, 0.06f, 0);
        railL.transform.localScale = new Vector3(0.04f, 0.06f, 0.88f);
        var rlRenderer = railL.GetComponent<MeshRenderer>();
        if (rlRenderer != null)
        {
            rlRenderer.material = CreateEmissiveMaterial(edgeColor, 0.4f);
        }

        var railR = GameObject.CreatePrimitive(PrimitiveType.Cube);
        railR.name = "RailR";
        railR.transform.SetParent(root.transform);
        railR.transform.localPosition = new Vector3(0.43f, 0.06f, 0);
        railR.transform.localScale = new Vector3(0.04f, 0.06f, 0.88f);
        var rrRenderer = railR.GetComponent<MeshRenderer>();
        if (rrRenderer != null)
        {
            rrRenderer.material = CreateEmissiveMaterial(edgeColor, 0.4f);
        }

        // Direction dot on +Z edge (forward output)
        var dotFwd = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        dotFwd.name = "DotForward";
        dotFwd.transform.SetParent(root.transform);
        dotFwd.transform.localPosition = new Vector3(0, 0.08f, 0.38f);
        dotFwd.transform.localScale = new Vector3(0.12f, 0.06f, 0.12f);
        var dfRenderer = dotFwd.GetComponent<MeshRenderer>();
        if (dfRenderer != null)
        {
            dfRenderer.material = CreateEmissiveMaterial(dotColor, 0.7f);
        }

        // Direction dot on -X edge (left output / filtered output)
        var dotLeft = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        dotLeft.name = "DotLeft";
        dotLeft.transform.SetParent(root.transform);
        dotLeft.transform.localPosition = new Vector3(-0.38f, 0.08f, 0);
        dotLeft.transform.localScale = new Vector3(0.12f, 0.06f, 0.12f);
        var dlRenderer = dotLeft.GetComponent<MeshRenderer>();
        if (dlRenderer != null)
        {
            dlRenderer.material = CreateEmissiveMaterial(dotColor, 0.7f);
        }

        // Direction dot on +X edge (right output)
        var dotRight = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        dotRight.name = "DotRight";
        dotRight.transform.SetParent(root.transform);
        dotRight.transform.localPosition = new Vector3(0.38f, 0.08f, 0);
        dotRight.transform.localScale = new Vector3(0.12f, 0.06f, 0.12f);
        var drRenderer = dotRight.GetComponent<MeshRenderer>();
        if (drRenderer != null)
        {
            drRenderer.material = CreateEmissiveMaterial(dotColor, 0.7f);
        }

        return root;
    }

    /// <summary>
    /// Returns the Y-axis rotation angle for the given direction.
    /// North = 0°, East = 90°, South = 180°, West = 270°.
    /// </summary>
    internal static float DirectionToYaw(Direction dir) => dir switch
    {
        Direction.North => 0f,
        Direction.East => 90f,
        Direction.South => 180f,
        Direction.West => 270f,
        _ => 0f
    };

    /// <summary>
    /// Creates a structure visual: building with foundation, walls, peaked roof,
    /// emissive type indicator, and a clear output direction arrow.
    /// </summary>
    public static GameObject CreateStructureVisual(string category, Direction outputDirection)
    {
        var root = new GameObject($"{category}");
        var color = StructureColors.GetValueOrDefault(category, Color.white);

        // Foundation plate (wider dark base)
        var foundation = GameObject.CreatePrimitive(PrimitiveType.Cube);
        foundation.name = "Foundation";
        foundation.transform.SetParent(root.transform);
        foundation.transform.localPosition = new Vector3(0, -0.48f, 0);
        foundation.transform.localScale = new Vector3(0.95f, 0.06f, 0.95f);
        var foundRenderer = foundation.GetComponent<MeshRenderer>();
        if (foundRenderer != null)
        {
            foundRenderer.material = CreateLitMaterial(Darken(color, 0.2f));
        }

        // Main walls
        var walls = GameObject.CreatePrimitive(PrimitiveType.Cube);
        walls.name = "Walls";
        walls.transform.SetParent(root.transform);
        walls.transform.localPosition = Vector3.zero;
        walls.transform.localScale = new Vector3(0.8f, 0.85f, 0.8f);
        var wallRenderer = walls.GetComponent<MeshRenderer>();
        if (wallRenderer != null)
        {
            wallRenderer.material = CreateLitMaterial(color);
        }

        // Peaked roof (rotated cube forming diamond cross-section)
        var roof = GameObject.CreatePrimitive(PrimitiveType.Cube);
        roof.name = "Roof";
        roof.transform.SetParent(root.transform);
        roof.transform.localPosition = new Vector3(0, 0.55f, 0);
        roof.transform.localRotation = Quaternion.Euler(0, 0, 45f);
        roof.transform.localScale = new Vector3(0.45f, 0.45f, 0.88f);
        var roofRenderer = roof.GetComponent<MeshRenderer>();
        if (roofRenderer != null)
        {
            roofRenderer.material = CreateLitMaterial(Darken(color, 0.12f));
        }

        // Emissive type indicator on roof ridge
        var indicator = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        indicator.name = "TypeIndicator";
        indicator.transform.SetParent(root.transform);
        indicator.transform.localPosition = new Vector3(0, 0.82f, 0);
        indicator.transform.localScale = new Vector3(0.18f, 0.18f, 0.18f);
        var indRenderer = indicator.GetComponent<MeshRenderer>();
        if (indRenderer != null)
        {
            indRenderer.material = CreateEmissiveMaterial(Brighten(color, 0.25f), 0.7f);
        }

        // Output direction arrow (shaft + tip)
        var offset = DirectionToOffset(outputDirection);
        var arrowShaft = GameObject.CreatePrimitive(PrimitiveType.Cube);
        arrowShaft.name = "OutputArrow";
        arrowShaft.transform.SetParent(root.transform);
        arrowShaft.transform.localPosition = new Vector3(offset.x * 0.45f, -0.3f, offset.z * 0.45f);
        arrowShaft.transform.localScale = new Vector3(
            offset.x != 0 ? 0.2f : 0.08f,
            0.08f,
            offset.z != 0 ? 0.2f : 0.08f);
        var shaftRenderer = arrowShaft.GetComponent<MeshRenderer>();
        if (shaftRenderer != null)
        {
            shaftRenderer.material = CreateEmissiveMaterial(new Color(1f, 0.9f, 0.3f), 0.5f);
        }

        var arrowTip = GameObject.CreatePrimitive(PrimitiveType.Cube);
        arrowTip.name = "OutputArrowTip";
        arrowTip.transform.SetParent(root.transform);
        arrowTip.transform.localPosition = new Vector3(offset.x * 0.58f, -0.3f, offset.z * 0.58f);
        arrowTip.transform.localScale = new Vector3(
            offset.x != 0 ? 0.1f : 0.16f,
            0.1f,
            offset.z != 0 ? 0.1f : 0.16f);
        var tipRenderer = arrowTip.GetComponent<MeshRenderer>();
        if (tipRenderer != null)
        {
            tipRenderer.material = CreateEmissiveMaterial(new Color(1f, 0.9f, 0.3f), 0.5f);
        }

        return root;
    }

    /// <summary>
    /// Creates a path segment placeholder: flat tile with raised edge rails,
    /// center flow line, and dual chevron direction indicators.
    /// Color varies by node type (straight=teal, splitter=yellow).
    /// </summary>
    public static GameObject CreatePathSegmentPlaceholder(PathNodeType nodeType = PathNodeType.Straight)
    {
        var color = nodeType switch
        {
            PathNodeType.Straight => new Color(0.15f, 0.55f, 0.55f),
            PathNodeType.Corner => new Color(0.2f, 0.5f, 0.6f),
            PathNodeType.Splitter => new Color(0.7f, 0.7f, 0.2f),
            PathNodeType.Merger => new Color(0.5f, 0.5f, 0.2f),
            PathNodeType.Endpoint => new Color(0.6f, 0.2f, 0.2f),
            _ => new Color(0.3f, 0.5f, 0.5f)
        };

        var root = new GameObject("PathSegment");
        var edgeColor = Brighten(color, 0.2f);
        var chevronColor = Brighten(color, 0.35f);

        // Main tile surface
        var tile = GameObject.CreatePrimitive(PrimitiveType.Cube);
        tile.name = "Tile";
        tile.transform.SetParent(root.transform);
        tile.transform.localPosition = Vector3.zero;
        tile.transform.localScale = new Vector3(0.9f, 0.08f, 0.9f);
        var tileRenderer = tile.GetComponent<MeshRenderer>();
        if (tileRenderer != null)
        {
            tileRenderer.material = CreateEmissiveMaterial(color, 0.3f);
        }

        // Left edge rail
        var railL = GameObject.CreatePrimitive(PrimitiveType.Cube);
        railL.name = "RailL";
        railL.transform.SetParent(root.transform);
        railL.transform.localPosition = new Vector3(-0.43f, 0.06f, 0);
        railL.transform.localScale = new Vector3(0.04f, 0.06f, 0.88f);
        var rlRenderer = railL.GetComponent<MeshRenderer>();
        if (rlRenderer != null)
        {
            rlRenderer.material = CreateEmissiveMaterial(edgeColor, 0.5f);
        }

        // Right edge rail
        var railR = GameObject.CreatePrimitive(PrimitiveType.Cube);
        railR.name = "RailR";
        railR.transform.SetParent(root.transform);
        railR.transform.localPosition = new Vector3(0.43f, 0.06f, 0);
        railR.transform.localScale = new Vector3(0.04f, 0.06f, 0.88f);
        var rrRenderer = railR.GetComponent<MeshRenderer>();
        if (rrRenderer != null)
        {
            rrRenderer.material = CreateEmissiveMaterial(edgeColor, 0.5f);
        }

        // Center flow line (thin strip along +Z)
        var flowLine = GameObject.CreatePrimitive(PrimitiveType.Cube);
        flowLine.name = "FlowLine";
        flowLine.transform.SetParent(root.transform);
        flowLine.transform.localPosition = new Vector3(0, 0.05f, 0);
        flowLine.transform.localScale = new Vector3(0.04f, 0.02f, 0.7f);
        var flRenderer = flowLine.GetComponent<MeshRenderer>();
        if (flRenderer != null)
        {
            flRenderer.material = CreateEmissiveMaterial(edgeColor, 0.4f);
        }

        // Chevron 1 (front) — two angled bars forming a > pointing +Z
        AddChevron(root, chevronColor, new Vector3(0, 0.06f, 0.18f));

        // Chevron 2 (rear) — second chevron behind the first
        AddChevron(root, chevronColor, new Vector3(0, 0.06f, -0.12f));

        return root;
    }

    private static void AddChevron(GameObject parent, Color color, Vector3 center)
    {
        var barL = GameObject.CreatePrimitive(PrimitiveType.Cube);
        barL.name = "ChevronL";
        barL.transform.SetParent(parent.transform);
        barL.transform.localPosition = center + new Vector3(-0.06f, 0, 0.04f);
        barL.transform.localRotation = Quaternion.Euler(0, 30f, 0);
        barL.transform.localScale = new Vector3(0.03f, 0.03f, 0.16f);
        var blRenderer = barL.GetComponent<MeshRenderer>();
        if (blRenderer != null)
        {
            blRenderer.material = CreateEmissiveMaterial(color, 0.6f);
        }

        var barR = GameObject.CreatePrimitive(PrimitiveType.Cube);
        barR.name = "ChevronR";
        barR.transform.SetParent(parent.transform);
        barR.transform.localPosition = center + new Vector3(0.06f, 0, 0.04f);
        barR.transform.localRotation = Quaternion.Euler(0, -30f, 0);
        barR.transform.localScale = new Vector3(0.03f, 0.03f, 0.16f);
        var brRenderer = barR.GetComponent<MeshRenderer>();
        if (brRenderer != null)
        {
            brRenderer.material = CreateEmissiveMaterial(color, 0.6f);
        }
    }

    /// <summary>
    /// Creates a semi-transparent path ghost preview for showing where the next
    /// path segment will be placed. Features a thin border frame and center dot.
    /// Color is updated by the caller for validity (green = valid, red = blocked).
    /// </summary>
    public static GameObject CreatePathGhostPreview()
    {
        var root = new GameObject("PathGhost");

        // Main tile surface (the renderer whose color the caller updates for validity)
        var tile = GameObject.CreatePrimitive(PrimitiveType.Cube);
        tile.name = "Tile";
        tile.transform.SetParent(root.transform);
        tile.transform.localPosition = Vector3.zero;
        tile.transform.localScale = new Vector3(0.85f, 0.06f, 0.85f);

        // Remove collider so the ghost doesn't interfere with raycasts
        var tileCollider = tile.GetComponent<Collider>();
        if (tileCollider != null)
        {
            UnityEngine.Object.Destroy(tileCollider);
        }

        var renderer = tile.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            renderer.material = CreateLitMaterial(new Color(0.2f, 0.8f, 0.2f, 0.35f));
        }

        // Border frame strips (no colliders)
        var borderColor = new Color(0.3f, 0.9f, 0.3f, 0.5f);
        AddGhostBorderStrip(root, borderColor, new Vector3(0, 0.04f, 0.42f), new Vector3(0.84f, 0.03f, 0.03f));
        AddGhostBorderStrip(root, borderColor, new Vector3(0, 0.04f, -0.42f), new Vector3(0.84f, 0.03f, 0.03f));
        AddGhostBorderStrip(root, borderColor, new Vector3(-0.42f, 0.04f, 0), new Vector3(0.03f, 0.03f, 0.84f));
        AddGhostBorderStrip(root, borderColor, new Vector3(0.42f, 0.04f, 0), new Vector3(0.03f, 0.03f, 0.84f));

        // Direction indicator — small arrow-like shape offset toward +Z
        // so the player can see which way this path segment will face
        var dirShaft = GameObject.CreatePrimitive(PrimitiveType.Cube);
        dirShaft.name = "DirShaft";
        dirShaft.transform.SetParent(root.transform);
        dirShaft.transform.localPosition = new Vector3(0, 0.05f, 0.08f);
        dirShaft.transform.localScale = new Vector3(0.06f, 0.03f, 0.28f);
        var shaftCollider = dirShaft.GetComponent<Collider>();
        if (shaftCollider != null)
        {
            UnityEngine.Object.Destroy(shaftCollider);
        }
        var shaftRenderer = dirShaft.GetComponent<MeshRenderer>();
        if (shaftRenderer != null)
        {
            shaftRenderer.material = CreateLitMaterial(new Color(0.4f, 1f, 0.4f, 0.55f));
        }

        // Arrow tip at the front of the direction indicator
        var dirTip = GameObject.CreatePrimitive(PrimitiveType.Cube);
        dirTip.name = "DirTip";
        dirTip.transform.SetParent(root.transform);
        dirTip.transform.localPosition = new Vector3(0, 0.05f, 0.26f);
        dirTip.transform.localScale = new Vector3(0.14f, 0.03f, 0.06f);
        var tipCollider = dirTip.GetComponent<Collider>();
        if (tipCollider != null)
        {
            UnityEngine.Object.Destroy(tipCollider);
        }
        var tipRenderer = dirTip.GetComponent<MeshRenderer>();
        if (tipRenderer != null)
        {
            tipRenderer.material = CreateLitMaterial(new Color(0.4f, 1f, 0.4f, 0.55f));
        }

        return root;
    }

    private static void AddGhostBorderStrip(GameObject parent, Color color, Vector3 position, Vector3 scale)
    {
        var strip = GameObject.CreatePrimitive(PrimitiveType.Cube);
        strip.name = "BorderStrip";
        strip.transform.SetParent(parent.transform);
        strip.transform.localPosition = position;
        strip.transform.localScale = scale;
        var collider = strip.GetComponent<Collider>();
        if (collider != null)
        {
            UnityEngine.Object.Destroy(collider);
        }
        var renderer = strip.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            renderer.material = CreateEmissiveMaterial(color, 0.5f);
        }
    }

    /// <summary>
    /// Creates a placed PathGate visual that looks like a path tile, blending
    /// naturally with adjacent path segments. The gate type (entrance vs exit)
    /// determines the layout of the threshold bar and chevron:
    ///   Entrance: chevron at -Z → threshold at +Z  ("come in here")
    ///   Exit:     threshold at -Z → chevron at +Z   ("path starts here, go out")
    /// The root is rotated externally to match the gate's facing direction.
    /// </summary>
    public static GameObject CreatePathGateVisual(bool isEntrance)
    {
        var root = new GameObject(isEntrance ? "PathGate_Entrance" : "PathGate_Exit");

        var gateColor = isEntrance
            ? new Color(0.2f, 0.6f, 0.3f)   // green = entrance
            : new Color(0.75f, 0.45f, 0.12f); // orange = exit
        var edgeColor = Brighten(gateColor, 0.15f);
        var thresholdColor = isEntrance
            ? new Color(0.25f, 0.8f, 0.4f)    // bright green threshold
            : new Color(0.95f, 0.65f, 0.15f); // bright amber threshold
        var chevronColor = Brighten(gateColor, 0.3f);

        // Path-style tile base
        var tile = GameObject.CreatePrimitive(PrimitiveType.Cube);
        tile.name = "Tile";
        tile.transform.SetParent(root.transform);
        tile.transform.localPosition = Vector3.zero;
        tile.transform.localScale = new Vector3(0.9f, 0.08f, 0.9f);
        var tileRenderer = tile.GetComponent<MeshRenderer>();
        if (tileRenderer != null)
        {
            tileRenderer.material = CreateEmissiveMaterial(gateColor, 0.25f);
        }

        // Edge rails (match path segment style)
        var railL = GameObject.CreatePrimitive(PrimitiveType.Cube);
        railL.name = "RailL";
        railL.transform.SetParent(root.transform);
        railL.transform.localPosition = new Vector3(-0.43f, 0.06f, 0);
        railL.transform.localScale = new Vector3(0.04f, 0.06f, 0.88f);
        var rlRenderer = railL.GetComponent<MeshRenderer>();
        if (rlRenderer != null)
        {
            rlRenderer.material = CreateEmissiveMaterial(edgeColor, 0.4f);
        }

        var railR = GameObject.CreatePrimitive(PrimitiveType.Cube);
        railR.name = "RailR";
        railR.transform.SetParent(root.transform);
        railR.transform.localPosition = new Vector3(0.43f, 0.06f, 0);
        railR.transform.localScale = new Vector3(0.04f, 0.06f, 0.88f);
        var rrRenderer = railR.GetComponent<MeshRenderer>();
        if (rrRenderer != null)
        {
            rrRenderer.material = CreateEmissiveMaterial(edgeColor, 0.4f);
        }

        // Center flow line
        var flowLine = GameObject.CreatePrimitive(PrimitiveType.Cube);
        flowLine.name = "FlowLine";
        flowLine.transform.SetParent(root.transform);
        flowLine.transform.localPosition = new Vector3(0, 0.05f, 0);
        flowLine.transform.localScale = new Vector3(0.04f, 0.02f, 0.5f);
        var flRenderer = flowLine.GetComponent<MeshRenderer>();
        if (flRenderer != null)
        {
            flRenderer.material = CreateEmissiveMaterial(edgeColor, 0.35f);
        }

        // Threshold bar and chevron placement depends on entrance vs exit.
        // Entrance: chevron at -Z (come in) → threshold at +Z (door)
        // Exit:     threshold at -Z (door)   → chevron at +Z (go out)
        float thresholdZ = isEntrance ? 0.3f : -0.3f;
        float chevronZ = isEntrance ? -0.2f : 0.2f;

        // Threshold bar (the gate/door boundary line)
        var threshold = GameObject.CreatePrimitive(PrimitiveType.Cube);
        threshold.name = "Threshold";
        threshold.transform.SetParent(root.transform);
        threshold.transform.localPosition = new Vector3(0, 0.07f, thresholdZ);
        threshold.transform.localScale = new Vector3(0.7f, 0.08f, 0.06f);
        var thRenderer = threshold.GetComponent<MeshRenderer>();
        if (thRenderer != null)
        {
            thRenderer.material = CreateEmissiveMaterial(thresholdColor, 0.6f);
        }

        // Small pillar accents on threshold ends
        var pillarL = GameObject.CreatePrimitive(PrimitiveType.Cube);
        pillarL.name = "ThresholdCapL";
        pillarL.transform.SetParent(root.transform);
        pillarL.transform.localPosition = new Vector3(-0.35f, 0.12f, thresholdZ);
        pillarL.transform.localScale = new Vector3(0.06f, 0.1f, 0.06f);
        var plRenderer = pillarL.GetComponent<MeshRenderer>();
        if (plRenderer != null)
        {
            plRenderer.material = CreateEmissiveMaterial(thresholdColor, 0.5f);
        }

        var pillarR = GameObject.CreatePrimitive(PrimitiveType.Cube);
        pillarR.name = "ThresholdCapR";
        pillarR.transform.SetParent(root.transform);
        pillarR.transform.localPosition = new Vector3(0.35f, 0.12f, thresholdZ);
        pillarR.transform.localScale = new Vector3(0.06f, 0.1f, 0.06f);
        var prRenderer = pillarR.GetComponent<MeshRenderer>();
        if (prRenderer != null)
        {
            prRenderer.material = CreateEmissiveMaterial(thresholdColor, 0.5f);
        }

        // Single chevron (direction indicator)
        AddChevron(root, chevronColor, new Vector3(0, 0.06f, chevronZ));

        return root;
    }

    private static Vector3 DirectionToOffset(Direction dir) => dir switch
    {
        Direction.North => new Vector3(0, 0, 1),
        Direction.East => new Vector3(1, 0, 0),
        Direction.South => new Vector3(0, 0, -1),
        Direction.West => new Vector3(-1, 0, 0),
        _ => Vector3.zero
    };
}
}
