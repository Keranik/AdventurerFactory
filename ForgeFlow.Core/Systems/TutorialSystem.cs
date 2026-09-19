using ForgeFlow.Core.Events;
using ForgeFlow.Core.Proto;
using ForgeFlow.Core.Proto.Logic;
using ForgeFlow.Core.Proto.Prototypes;

namespace ForgeFlow.Core.Systems;

/// <summary>
/// Manages tutorial/progression missions. Listens to game events
/// and advances mission conditions. Awards rewards on completion.
/// Publishes TutorialStepActivatedEvent for the presentation layer
/// to render visual highlights (glowing borders, arrows).
/// </summary>
public sealed class TutorialSystem : IGameSystem
{
    private readonly EventBus _eventBus;
    private readonly ProtoFactory _factory;
    private readonly List<TutorialMissionLogic> _missions = new();
    private TutorialMissionLogic? _activeMission;
    private EntityManager? _entityManager;

    public TutorialMissionLogic? ActiveMission => _activeMission;
    public IReadOnlyList<TutorialMissionLogic> AllMissions => _missions;

    /// <summary>Index of the currently active step within the active mission (for multi-condition missions).</summary>
    public int ActiveStepIndex { get; private set; }

    /// <summary>Whether a Phase 1 tutorial mission is currently active.</summary>
    public bool IsPhase1Active => _activeMission != null && _activeMission.Phase == 1;

    /// <summary>
    /// Returns the max villager count override during Phase 1 (1 villager only).
    /// Returns null when Phase 1 is not active, meaning no override.
    /// </summary>
    public int? Phase1VillagerLimit => IsPhase1Active ? 1 : null;

    public TutorialSystem(EventBus eventBus, ProtoFactory factory)
    {
        _eventBus = eventBus;
        _factory = factory;
    }

    /// <summary>
    /// Sets the EntityManager reference for checking pre-existing world state
    /// when tutorial steps activate. Called during bootstrap.
    /// </summary>
    public void SetEntityManager(EntityManager entityManager)
    {
        _entityManager = entityManager;
    }

    /// <summary>
    /// Initializes all tutorial missions from registered protos and activates the first one.
    /// </summary>
    public void Initialize(ProtoRegistry registry)
    {
        _missions.Clear();
        _activeMission = null;
        ActiveStepIndex = 0;

        var tutorials = new List<TutorialMissionProto>(registry.GetAllTutorials());
        tutorials.Sort((a, b) => a.Order.CompareTo(b.Order));
        foreach (var proto in tutorials)
        {
            var mission = _factory.CreateTutorialMission(proto.Id);
            if (mission != null)
            {
                _missions.Add(mission);
            }
        }

        ActivateNext();
    }

    /// <summary>
    /// Advances the active mission for a given condition type.
    /// Returns true if a mission was just completed.
    /// Publishes TutorialStepCompletedEvent when individual steps finish.
    /// </summary>
    public bool AdvanceCondition(TutorialConditionType type, string? targetId = null, int amount = 1)
    {
        if (_activeMission == null) return false;

        int previousStepIndex = ActiveStepIndex;
        bool completed = _activeMission.AdvanceCondition(type, targetId, amount);

        if (completed)
        {
            // Publish step completion for the last step
            _eventBus.Publish(new TutorialStepCompletedEvent(
                _activeMission.ProtoId, previousStepIndex,
                _activeMission.CelebrationMessage));
            _eventBus.Publish(new TutorialCompletedEvent(_activeMission.ProtoId, _activeMission.Order));
            ActivateNext();
            return true;
        }

        // Check if the current step index changed (a step was just completed)
        UpdateActiveStepIndex();
        if (ActiveStepIndex != previousStepIndex && previousStepIndex < _activeMission.Conditions.Count)
        {
            var completedStep = _activeMission.Conditions[previousStepIndex];
            if (completedStep.IsMet)
            {
                _eventBus.Publish(new TutorialStepCompletedEvent(
                    _activeMission.ProtoId, previousStepIndex,
                    GetStepCompletionMessage(previousStepIndex)));
            }
        }

        return false;
    }

    /// <summary>
    /// Awards rewards from a completed mission into the given resource stocks.
    /// </summary>
    public void CollectRewards(TutorialMissionLogic mission, ItemManager itemManager)
    {
        foreach (var kvp in mission.Rewards)
        {
            itemManager.AddStock(kvp.Key, kvp.Value);
        }
    }

    /// <summary>
    /// Returns the highlight target string for the current tutorial step.
    /// Uses the per-condition HighlightTarget if set, otherwise falls back
    /// to a default based on condition type.
    /// </summary>
    public string GetHighlightTarget()
    {
        if (_activeMission == null) return string.Empty;
        if (ActiveStepIndex >= _activeMission.Conditions.Count) return string.Empty;

        var condition = _activeMission.Conditions[ActiveStepIndex];

        // Per-step highlight takes priority
        if (!string.IsNullOrEmpty(condition.HighlightTarget))
            return condition.HighlightTarget;

        // Fallback defaults
        return condition.Type switch
        {
            TutorialConditionType.PlaceStructure => "structure_toolbar",
            TutorialConditionType.PlaceSpecificStructure => condition.TargetId ?? "structure_toolbar",
            TutorialConditionType.BuildPath => "path_toolbar",
            TutorialConditionType.SpawnHero => "spawner_button",
            TutorialConditionType.CompleteDungeon => "dungeon_portal",
            TutorialConditionType.ReachResearchTier => "research_panel",
            TutorialConditionType.BuildVillageBuilding => "village_panel",
            TutorialConditionType.SpawnVillager => "village_spawner",
            TutorialConditionType.GatherResource => "resource_node",
            TutorialConditionType.EquipHero => "forge_structure",
            TutorialConditionType.FuseHeroes => "fusion_altar",
            TutorialConditionType.ConnectPathToStructure => "path_toolbar",
            TutorialConditionType.VillagerReachBuilding => "building_input",
            TutorialConditionType.VillagerPickUpItem => "gathering_spot",
            TutorialConditionType.VillagerDropOffItem => "building_input",
            TutorialConditionType.VillagerEnterInn => "inn_structure",
            TutorialConditionType.VillagerTrainClass => "training_building",
            TutorialConditionType.VillagerEnterDungeon => "dungeon_portal",
            TutorialConditionType.CraftItem => "craft_station",
            TutorialConditionType.PlaceBuildingInput => "building_input_tool",
            TutorialConditionType.PlaceBuildingOutput => "building_output_tool",
            TutorialConditionType.UseBalancer => "balancer_structure",
            TutorialConditionType.WatchVillagerGather => "gathering_spot",
            TutorialConditionType.PlacePathGateEntrance => "hotbar_pathgate",
            TutorialConditionType.PlacePathGateExit => "hotbar_pathgate",
            TutorialConditionType.WatchVillagerRest => "inn_structure",
            TutorialConditionType.WatchFullLoop => "full_loop",
            TutorialConditionType.SelectStockpileProduct => "stockpile_structure",
            TutorialConditionType.SelectCraftStationRecipe => "craft_station",
            TutorialConditionType.ConfigureFilterSplitter => "filter_splitter",
            _ => string.Empty
        };
    }

    /// <summary>
    /// Returns the highlight area key for the current tutorial step (e.g. "biome_forest").
    /// Empty string means no area highlight.
    /// </summary>
    public string GetHighlightArea()
    {
        if (_activeMission == null) return string.Empty;
        if (ActiveStepIndex >= _activeMission.Conditions.Count) return string.Empty;
        return _activeMission.Conditions[ActiveStepIndex].HighlightArea ?? string.Empty;
    }

    /// <summary>
    /// Returns the step message for the current active step.
    /// This is the explicit instruction shown to the player.
    /// </summary>
    public string GetCurrentStepMessage()
    {
        if (_activeMission == null) return string.Empty;
        if (ActiveStepIndex >= _activeMission.Conditions.Count) return string.Empty;

        var condition = _activeMission.Conditions[ActiveStepIndex];
        return !string.IsNullOrEmpty(condition.StepMessage) ? condition.StepMessage : _activeMission.HintText;
    }

    /// <summary>
    /// Returns the celebration message for a step that was just completed.
    /// </summary>
    public string GetStepCompletionMessage(int stepIndex)
    {
        if (_activeMission == null) return string.Empty;
        if (stepIndex >= _activeMission.Conditions.Count) return _activeMission.CelebrationMessage;

        var condition = _activeMission.Conditions[stepIndex];
        return !string.IsNullOrEmpty(condition.CompletionMessage) ? condition.CompletionMessage : "Objective complete!";
    }

    private void ActivateNext()
    {
        foreach (var mission in _missions)
        {
            if (mission.State == TutorialMissionState.Locked)
            {
                // Check prerequisite
                if (mission.PrerequisiteMissionId == null ||
                    IsPrerequisiteComplete(mission.PrerequisiteMissionId))
                {
                    mission.State = TutorialMissionState.Active;
                    _activeMission = mission;
                    ActiveStepIndex = 0;
                    PublishStepActivated();
                    CheckPreSatisfiedConditions();
                    return;
                }
            }
        }

        _activeMission = null; // All missions complete
        ActiveStepIndex = 0;
    }

    private void UpdateActiveStepIndex()
    {
        if (_activeMission == null) return;
        for (int i = 0; i < _activeMission.Conditions.Count; i++)
        {
            if (!_activeMission.Conditions[i].IsMet)
            {
                ActiveStepIndex = i;
                PublishStepActivated();
                return;
            }
        }
    }

    private void PublishStepActivated()
    {
        if (_activeMission == null) return;
        _eventBus.Publish(new TutorialStepActivatedEvent(
            _activeMission.ProtoId,
            ActiveStepIndex,
            GetHighlightTarget(),
            GetCurrentStepMessage(),
            GetHighlightArea()));
    }

    public bool AllComplete
    {
        get
        {
            if (_missions.Count == 0) { return false; }
            for (int i = 0; i < _missions.Count; i++)
            {
                if (_missions[i].State != TutorialMissionState.Completed) { return false; }
            }
            return true;
        }
    }

    public int CompletedCount
    {
        get
        {
            int count = 0;
            for (int i = 0; i < _missions.Count; i++)
            {
                if (_missions[i].State == TutorialMissionState.Completed) { count++; }
            }
            return count;
        }
    }

    public int TotalCount => _missions.Count;

    /// <summary>
    /// Checks if the newly activated mission's conditions are already satisfied
    /// by existing world state (e.g. PathGates placed out of order before reaching
    /// the tutorial step). Compares existing entity counts against cumulative
    /// requirements from previously completed missions to avoid double-counting.
    /// </summary>
    private void CheckPreSatisfiedConditions()
    {
        if (_activeMission == null || _entityManager == null) { return; }

        bool anyAdvanced = false;
        foreach (var condition in _activeMission.Conditions)
        {
            if (condition.IsMet) { continue; }

            if (condition.Type == TutorialConditionType.PlacePathGateExit ||
                condition.Type == TutorialConditionType.PlacePathGateEntrance)
            {
                bool isExit = condition.Type == TutorialConditionType.PlacePathGateExit;
                int existingCount = CountExistingPathGates(isExit);
                int consumedByCompleted = CountCompletedConditionsOfType(condition.Type);
                int surplus = existingCount - consumedByCompleted;

                if (surplus >= condition.RequiredCount)
                {
                    condition.CurrentCount = condition.RequiredCount;
                    anyAdvanced = true;
                }
            }
        }

        if (anyAdvanced && _activeMission.AllConditionsMet())
        {
            var completedMission = _activeMission;
            completedMission.State = TutorialMissionState.Completed;
            _eventBus.Publish(new TutorialStepCompletedEvent(
                completedMission.ProtoId, 0, completedMission.CelebrationMessage));
            _eventBus.Publish(new TutorialCompletedEvent(completedMission.ProtoId, completedMission.Order));
            ActivateNext();
        }
    }

    private int CountExistingPathGates(bool exitOnly)
    {
        int count = 0;
        var gates = _entityManager!.PathGatesOrdered;
        for (int i = 0; i < gates.Count; i++)
        {
            var gate = gates[i];
            if (exitOnly && gate.IsExit) { count++; }
            else if (!exitOnly && gate.IsEntrance) { count++; }
        }
        return count;
    }

    private int CountCompletedConditionsOfType(TutorialConditionType type)
    {
        int total = 0;
        foreach (var mission in _missions)
        {
            if (mission.State != TutorialMissionState.Completed) { continue; }
            foreach (var condition in mission.Conditions)
            {
                if (condition.Type == type)
                {
                    total += condition.RequiredCount;
                }
            }
        }
        return total;
    }

    private bool IsPrerequisiteComplete(string prerequisiteId)
    {
        for (int i = 0; i < _missions.Count; i++)
        {
            if (_missions[i].ProtoId == prerequisiteId &&
                _missions[i].State == TutorialMissionState.Completed)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Restores tutorial progress from save data. Call after Initialize().
    /// Marks completed missions, then activates the correct mission.
    /// </summary>
    public void LoadFromSave(List<string> completedIds, string? activeMissionId)
    {
        foreach (var mission in _missions)
        {
            if (completedIds.Contains(mission.ProtoId))
            {
                mission.State = TutorialMissionState.Completed;
            }
        }

        _activeMission = null;
        ActiveStepIndex = 0;

        if (activeMissionId != null)
        {
            foreach (var mission in _missions)
            {
                if (mission.ProtoId == activeMissionId)
                {
                    mission.State = TutorialMissionState.Active;
                    _activeMission = mission;
                    break;
                }
            }
        }
        else
        {
            // No explicit active mission — try to find the next unlockable one
            ActivateNext();
        }
    }
}
