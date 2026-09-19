using ForgeFlow.Core.Entities;

namespace ForgeFlow.Core.Proto;

/// <summary>
/// A single routing rule for automation gates. Combinator-style:
/// "If worker matches condition → route to OutputDirection."
/// </summary>
public sealed class GateRule
{
    public GateConditionType ConditionType { get; set; }
    public string ConditionValue { get; set; } = string.Empty;
    public Direction OutputDirection { get; set; } = Direction.East;

    /// <summary>Evaluates whether a worker matches this rule.</summary>
    public bool Matches(HeroEntity worker)
    {
        switch (ConditionType)
        {
            case GateConditionType.MinLevel:
                return int.TryParse(ConditionValue, out var minLvl) && worker.Level >= minLvl;

            case GateConditionType.MaxLevel:
                return int.TryParse(ConditionValue, out var maxLvl) && worker.Level <= maxLvl;

            case GateConditionType.HasTrait:
                for (int i = 0; i < worker.Traits.Count; i++)
                {
                    if (string.Equals(worker.Traits[i], ConditionValue, StringComparison.OrdinalIgnoreCase))
                        return true;
                }
                return false;

            case GateConditionType.HasAbility:
                for (int i = 0; i < worker.Abilities.Count; i++)
                {
                    if (string.Equals(worker.Abilities[i].Id, ConditionValue, StringComparison.OrdinalIgnoreCase))
                        return true;
                }
                return false;

            case GateConditionType.HasProfession:
                return Enum.TryParse<WorkerProfession>(ConditionValue, true, out var prof) && worker.Profession == prof;

            case GateConditionType.HasJobClass:
                return string.Equals(worker.ClassId, ConditionValue, StringComparison.OrdinalIgnoreCase);

            case GateConditionType.HasItem:
                for (int i = 0; i < worker.Equipment.Count; i++)
                {
                    if (string.Equals(worker.Equipment[i].ProtoId, ConditionValue, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
                return false;

            case GateConditionType.CarryingItem:
                return worker.CarryLoad > 0f
                    && !string.IsNullOrEmpty(ConditionValue)
                    && worker.CarryLoad >= 0.01f;

            case GateConditionType.HasToolType:
                var equipped = worker.GetEquippedInSlot(EquipSlot.Weapon);
                return equipped != null
                    && string.Equals(equipped.ProtoId, ConditionValue, StringComparison.OrdinalIgnoreCase);

            default:
                return false;
        }
    }
}
