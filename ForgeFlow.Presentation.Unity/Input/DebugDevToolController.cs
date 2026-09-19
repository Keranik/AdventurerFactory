using ForgeFlow.Core.Entities;
using ForgeFlow.Core.Localization;
using ForgeFlow.Core.Proto;
using ForgeFlow.Core.Systems;
using ForgeFlow.Presentation.Unity.Visuals;
using UnityEngine;

namespace ForgeFlow.Presentation.Unity.Input
{

/// <summary>
/// Debug/dev-tool input controller demonstrating the full custom key
/// registration capability (Phase 2 — <see cref="IControllerWithCustomActions"/>).
/// Activated via a toggle (F5 or programmatic). Provides three dev shortcuts:
///   G — Give 500 gold
///   T — Cycle terrain biome at cursor
///   K — Kill all villagers (reset workforce)
/// </summary>
internal sealed class DebugDevToolController : IControllerWithShortcuts, IControllerWithCustomActions
{
    private readonly SimulationTicker _simulation;

    private readonly ShortcutDescriptor[] _shortcuts;
    private readonly string[] _additionalActions;

    /// <summary>
    /// Optional callback invoked after a terrain cell's biome changes (debug only).
    /// Presentation wires this to <see cref="TerrainVisualManager.RefreshCell"/>.
    /// </summary>
    public Action<GridPosRPG>? OnTerrainCellChanged { get; set; }

    public string Name => "DebugDevTool";
    public int Priority => 300; // Highest priority when active
    public bool IsActive { get; set; }
    public string? CursorHint => IsActive ? "debug" : null;

    /// <summary>Current cursor grid position, updated by the stack each frame.</summary>
    private GridPosRPG _cursorPos;

    public DebugDevToolController(SimulationTicker simulation)
    {
        _simulation = simulation;

        _shortcuts = new[]
        {
            new ShortcutDescriptor("debug_gold", "DebugGiveGold", LocalizationKeys.ToolHintDebugGold),
            new ShortcutDescriptor("debug_terrain", "DebugCycleTerrain", LocalizationKeys.ToolHintDebugTerrain),
            new ShortcutDescriptor("debug_kill", "DebugKillAll", LocalizationKeys.ToolHintDebugKillAll),
            new ShortcutDescriptor("cancel", "CancelKey", LocalizationKeys.ToolHintExitMode),
        };

        _additionalActions = new[]
        {
            "DebugGiveGold",
            "DebugCycleTerrain",
            "DebugKillAll"
        };
    }

    // ── IControllerWithShortcuts ────────────────────────────────

    public IReadOnlyList<ShortcutDescriptor> GetActiveShortcuts()
    {
        for (int i = 0; i < _shortcuts.Length; i++)
        {
            _shortcuts[i].IsActive = true;
        }
        return _shortcuts;
    }

    // ── IControllerWithCustomActions ────────────────────────────

    public IReadOnlyList<string> AdditionalActionNames => _additionalActions;

    public bool HandleCustomAction(string actionName)
    {
        if (!IsActive) return false;

        switch (actionName)
        {
            case "DebugGiveGold":
                GiveGold();
                return true;
            case "DebugCycleTerrain":
                CycleTerrain();
                return true;
            case "DebugKillAll":
                KillAllVillagers();
                return true;
            default:
                return false;
        }
    }

    // ── Standard IInputController methods ───────────────────────

    public bool HandleLeftClickDown(GridPosRPG gridPos) => false;
    public bool HandleLeftClickUp(GridPosRPG gridPos) => false;
    public bool HandleRightClick(GridPosRPG gridPos)
    {
        if (!IsActive) return false;
        // RMB exits debug mode
        IsActive = false;
        return true;
    }
    public bool HandleRotate() => false;
    public bool HandleCancel()
    {
        if (!IsActive) return false;
        IsActive = false;
        return true;
    }

    public void UpdateController(float deltaTime, GridPosRPG cursorGridPos)
    {
        _cursorPos = cursorGridPos;
    }

    public void OnDeactivated() { }

    // ── Debug actions ───────────────────────────────────────────

    private void GiveGold()
    {
        _simulation.ItemManager.AddStock("gold", 500);
        Debug.Log($"[Debug] Gave 500 gold. Balance: {_simulation.ItemManager.GetStock("gold")}");
    }

    private void CycleTerrain()
    {
        var cell = _simulation.TileManager.GetCell(_cursorPos);
        if (cell == null)
        {
            Debug.Log($"[Debug] No tile at {_cursorPos}");
            return;
        }

        BiomeType[] biomes = { BiomeType.Plains, BiomeType.Forest, BiomeType.Mountain, BiomeType.Hills, BiomeType.Swamp, BiomeType.Desert };
        var current = cell.Biome;
        int idx = Array.IndexOf(biomes, current);
        int next = (idx + 1) % biomes.Length;
        cell.Biome = biomes[next];
        OnTerrainCellChanged?.Invoke(_cursorPos);
        Debug.Log($"[Debug] Terrain at {_cursorPos}: {current} → {biomes[next]}");
    }

    private void KillAllVillagers()
    {
        int count = _simulation.VillagerSystem.Villagers.Count;
        _simulation.VillagerSystem.Villagers.Clear();
        Debug.Log($"[Debug] Removed {count} villagers.");
    }
}

}
