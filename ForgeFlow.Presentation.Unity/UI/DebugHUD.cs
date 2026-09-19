using ForgeFlow.Core.Entities;
using ForgeFlow.Core.Systems;
using UnityEngine;

namespace ForgeFlow.Presentation.Unity.UI
{

/// <summary>
/// Dev-only debug overlay. Hidden by default in the real game flow.
/// Toggle with F12 key. Shows hero count, simulation time, tick count,
/// pause state, and average survival rate.
/// Uses OnGUI for zero-dependency debug text.
/// </summary>
internal class DebugHUD : MonoBehaviour
{
    private SimulationTicker _simulation = null!;
    private bool _showDebug;

    private int _totalDungeonAttempts;
    private int _totalDungeonSuccesses;

    public void Initialize(SimulationTicker simulation)
    {
        _simulation = simulation;
        _showDebug = false; // Hidden by default — no more debug-by-default
    }

    public void RecordDungeonResult(bool success)
    {
        _totalDungeonAttempts++;
        if (success)
        {
            _totalDungeonSuccesses++;
        }
    }

    // OnGUI is not a virtual override in our stubs, so we declare it directly.
    // In real Unity, MonoBehaviour.OnGUI is a magic method called by the engine.
#if !UNITY_STUBS || UNITY_2021_1_OR_NEWER
    private void OnGUI()
    {
        // F12 toggles debug HUD
        if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.F12)
        {
            _showDebug = !_showDebug;
        }

        if (_showDebug)
        {
            DrawHUD();
        }
    }
#endif

    public void DrawHUD()
    {
        if (_simulation == null) { return; }

        int heroCount = _simulation.EntityManager.Heroes.Count;
        int structureCount = _simulation.EntityManager.Structures.Count;
        int pathCount = _simulation.EntityManager.PathSegments.Count;
        float survivalPct = _totalDungeonAttempts > 0
            ? (_totalDungeonSuccesses / (float)_totalDungeonAttempts) * 100f
            : 0f;

        int ghostCount = _simulation.EntityManager.Heroes.Count(h => h.State == HeroState.Ghost);
        int onPathCount = _simulation.EntityManager.Heroes.Count(h => h.State == HeroState.OnPath);

        string pauseLabel = _simulation.IsPaused ? "  [PAUSED]" : "";
        int gold = _simulation.ItemManager.GetStock("gold");

        string text = $"=== ForgeFlow Debug (F12 toggle) ==={pauseLabel}\n" +
                       $"Gold: {gold}\n" +
                       $"Heroes: {heroCount} (path: {onPathCount}, ghost: {ghostCount})\n" +
                       $"Paths: {pathCount}  |  Structures: {structureCount}\n" +
                       $"Ticks: {_simulation.TickCount}\n" +
                       $"Dungeons: {_totalDungeonAttempts} attempts, {survivalPct:F1}% survival";

        GUI.Label(new Rect(10, 10, 350, 200), text);
    }
}
}
