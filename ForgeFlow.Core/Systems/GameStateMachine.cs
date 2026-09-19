using ForgeFlow.Core.Entities;
using ForgeFlow.Core.Events;

namespace ForgeFlow.Core.Systems;

/// <summary>
/// Controls the high-level game flow: MainMenu → NewGameSetup → Playing → Paused → GameOver/Victory.
/// Pure Core — no Unity dependency. Publishes state change events.
/// </summary>
public sealed class GameStateMachine : IGameSystem
{
    private readonly EventBus _eventBus;
    private readonly SimulationTicker _simulation;

    /// <summary>Accumulated real seconds with 0 heroes and 0 structures while playing.</summary>
    private float _loseTimerSeconds;

    /// <summary>Seconds of 0 heroes + 0 structures required to trigger lose condition.</summary>
    public const float LoseTimeoutSeconds = 30f;

    public GameState Current { get; private set; } = GameState.MainMenu;
    public GameState Previous { get; private set; } = GameState.MainMenu;
    public float LoseTimerSeconds => _loseTimerSeconds;

    public GameStateMachine(EventBus eventBus, SimulationTicker simulation)
    {
        _eventBus = eventBus;
        _simulation = simulation;
    }

    /// <summary>Transitions to a new state if the transition is valid.</summary>
    public bool TransitionTo(GameState next)
    {
        if (!IsValidTransition(Current, next)) return false;

        Previous = Current;
        Current = next;

        ApplySideEffects(next);
        _eventBus.Publish(new GameStateChangedEvent(Previous, Current));
        return true;
    }

    /// <summary>Opens the new-game setup screen.</summary>
    public bool StartNewGameSetup()
    {
        return TransitionTo(GameState.NewGameSetup);
    }

    /// <summary>Starts a new game from the setup screen or main menu.</summary>
    public bool StartNewGame()
    {
        _loseTimerSeconds = 0f;
        return TransitionTo(GameState.Playing);
    }

    /// <summary>Toggles between Playing and Paused.</summary>
    public bool TogglePause()
    {
        return Current switch
        {
            GameState.Playing => TransitionTo(GameState.Paused),
            GameState.Paused => TransitionTo(GameState.Playing),
            _ => false
        };
    }

    /// <summary>Returns to the main menu from any in-game state.</summary>
    public bool ReturnToMainMenu()
    {
        if (Current == GameState.MainMenu) return false;
        _loseTimerSeconds = 0f;
        return TransitionTo(GameState.MainMenu);
    }

    /// <summary>Triggers game over (hero factory destroyed, etc.).</summary>
    public bool TriggerGameOver()
    {
        if (Current != GameState.Playing) return false;
        _eventBus.Publish(new EndConditionMetEvent(false, "No heroes and no structures for 30 seconds."));
        return TransitionTo(GameState.GameOver);
    }

    /// <summary>Triggers victory (all dungeon tiers cleared, etc.).</summary>
    public bool TriggerVictory()
    {
        if (Current != GameState.Playing) return false;
        _eventBus.Publish(new EndConditionMetEvent(true, "Reached tier 10 with 3 prestige resets!"));
        return TransitionTo(GameState.Victory);
    }

    /// <summary>Checks if the player has met any win/lose condition. Call every frame with deltaTime.</summary>
    public void EvaluateEndConditions(float deltaTime = 0f)
    {
        if (Current != GameState.Playing) return;

        // Win condition: research tier 10 + at least 3 prestige resets
        if (_simulation.ResearchManager.CurrentTier >= 10 && _simulation.WorldStateManager.PrestigeCount >= 3)
        {
            TriggerVictory();
            return;
        }

        // Lose condition: 0 heroes and 0 structures for >30 real seconds
        bool hasHeroes = _simulation.EntityManager.Heroes.Count > 0;
        bool hasStructures = _simulation.EntityManager.Structures.Count > 0;

        if (!hasHeroes && !hasStructures)
        {
            _loseTimerSeconds += deltaTime;
            if (_loseTimerSeconds >= LoseTimeoutSeconds)
            {
                TriggerGameOver();
            }
        }
        else
        {
            _loseTimerSeconds = 0f;
        }
    }

    private static bool IsValidTransition(GameState from, GameState to)
    {
        return (from, to) switch
        {
            (GameState.MainMenu, GameState.NewGameSetup) => true,
            (GameState.MainMenu, GameState.Loading) => true,
            (GameState.MainMenu, GameState.Playing) => true,
            (GameState.NewGameSetup, GameState.Playing) => true,
            (GameState.NewGameSetup, GameState.MainMenu) => true,
            (GameState.Loading, GameState.Playing) => true,
            (GameState.Playing, GameState.Paused) => true,
            (GameState.Playing, GameState.GameOver) => true,
            (GameState.Playing, GameState.Victory) => true,
            (GameState.Paused, GameState.Playing) => true,
            (GameState.Paused, GameState.MainMenu) => true,
            (GameState.GameOver, GameState.MainMenu) => true,
            (GameState.Victory, GameState.MainMenu) => true,
            (GameState.Victory, GameState.Playing) => true,
            _ => false
        };
    }

    private void ApplySideEffects(GameState state)
    {
        switch (state)
        {
            case GameState.Playing:
                _simulation.Resume();
                break;
            case GameState.Paused:
            case GameState.MainMenu:
            case GameState.NewGameSetup:
            case GameState.GameOver:
            case GameState.Victory:
                _simulation.Pause();
                break;
        }
    }
}
