using ForgeFlow.Core.Proto.Prototypes;

namespace ForgeFlow.Core.Proto.Logic;

/// <summary>
/// Runtime logic for a tutorial mission. Tracks progress against conditions.
/// </summary>
public sealed class TutorialMissionLogic
{
    public string ProtoId { get; set; } = string.Empty;
    public int Order { get; set; }
    public string? PrerequisiteMissionId { get; set; }
    public TutorialMissionState State { get; set; } = TutorialMissionState.Locked;
    public List<TutorialCondition> Conditions { get; } = new();
    public Dictionary<string, int> Rewards { get; } = new();
    public string HintText { get; set; } = string.Empty;
    public string CelebrationMessage { get; set; } = "Objective complete!";
    /// <summary>Tutorial phase this mission belongs to (1 = Phase 1, etc.). 0 = unassigned.</summary>
    public int Phase { get; set; }

    public void InitializeFromProto(TutorialMissionProto proto)
    {
        ProtoId = proto.Id;
        Order = proto.Order;
        PrerequisiteMissionId = proto.PrerequisiteMissionId;
        HintText = proto.HintText;
        CelebrationMessage = proto.CelebrationMessage;
        Phase = proto.Phase;

        Conditions.Clear();
        foreach (var c in proto.Conditions)
        {
            Conditions.Add(new TutorialCondition
            {
                Type = c.Type,
                TargetId = c.TargetId,
                RequiredCount = c.RequiredCount,
                CurrentCount = 0,
                HighlightTarget = c.HighlightTarget,
                StepMessage = c.StepMessage,
                CompletionMessage = c.CompletionMessage,
                HighlightArea = c.HighlightArea
            });
        }

        foreach (var kvp in proto.Rewards)
        {
            Rewards[kvp.Key] = kvp.Value;
        }
    }

    /// <summary>
    /// Advances progress for a specific condition type.
    /// Returns true if the mission was just completed.
    /// </summary>
    public bool AdvanceCondition(TutorialConditionType type, string? targetId = null, int amount = 1)
    {
        if (State != TutorialMissionState.Active) return false;

        foreach (var condition in Conditions)
        {
            if (condition.Type == type && (targetId == null || condition.TargetId == null || condition.TargetId == targetId))
            {
                condition.CurrentCount += amount;
            }
        }

        if (AllConditionsMet())
        {
            State = TutorialMissionState.Completed;
            return true;
        }
        return false;
    }

    public bool AllConditionsMet()
    {
        foreach (var c in Conditions)
        {
            if (!c.IsMet) return false;
        }
        return true;
    }
}
