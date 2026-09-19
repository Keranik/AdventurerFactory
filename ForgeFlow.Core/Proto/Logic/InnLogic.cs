using ForgeFlow.Core.Entities;
using ForgeFlow.Core.Proto.Prototypes;

namespace ForgeFlow.Core.Proto.Logic;

/// <summary>
/// Inn — restores villager stamina and allows rest.
/// Villagers entering via a building-input path node rest here.
/// Rejects villagers who are still carrying items (must drop off first).
/// Rest timing is recipe-driven via SetRestRecipe (Central Manager + Events pattern).
/// </summary>
public sealed class InnLogic : ActivityEntity, IEntryGated, IStructureTickHandler
{
    public new int MaxOccupants { get; set; } = 4;
    public List<ulong> CurrentOccupants { get; } = new();
    public string? RestRecipeId { get; private set; }
    public float RestRecipeDuration { get; private set; } = 5.0f;

    public InnLogic(EntityId id) : base(id)
    {
        ProcessingDuration = 5.0f;
    }

    public override void InitializeFromProto(StructureProtoBase proto)
    {
        base.InitializeFromProto(proto);
        if (proto is InnProto innProto)
        {
            MaxOccupants = innProto.MaxOccupants;
            RestRecipeId = innProto.RestRecipeId;
        }
    }

    /// <summary>
    /// Sets the rest recipe with resolved data from the registry.
    /// Called by StructureManager after structure placement.
    /// </summary>
    public void SetRestRecipe(string recipeId, float duration)
    {
        RestRecipeId = recipeId;
        RestRecipeDuration = duration > 0f ? duration : 5.0f;
    }

    /// <summary>
    /// Checks whether a villager is eligible to enter this inn.
    /// Rejects when full or when the villager is carrying items (must drop off first).
    /// </summary>
    public EntryCheckResult CheckEntry(VillagerLogic villager)
    {
        if (!CanAcceptVillager)
        {
            return EntryCheckResult.Rejected(EntryRejectionReason.StructureFull);
        }
        if (villager.IsCarryingItems)
        {
            return EntryCheckResult.Rejected(EntryRejectionReason.NotCarryingRequiredItems);
        }
        return EntryCheckResult.Accepted;
    }

    /// <summary>
    /// Attempts to accept a villager for rest. Rejects villagers carrying items.
    /// Returns true if the villager was accepted.
    /// The caller (manager) is responsible for setting villager state.
    /// </summary>
    public bool AcceptVillager(VillagerLogic villager)
    {
        if (CurrentOccupants.Count >= MaxOccupants) { return false; }
        if (villager.IsCarryingItems) { return false; }

        CurrentOccupants.Add(villager.Id);
        return true;
    }

    /// <summary>
    /// Restores a villager's stamina using the rest recipe duration.
    /// Returns true when fully rested. The caller (StructureManager) is responsible
    /// for removing the villager from CurrentOccupants and setting the appropriate state.
    /// </summary>
    public bool RestVillager(VillagerLogic villager, float deltaTime)
    {
        float restRate = villager.MaxStamina / RestRecipeDuration;
        villager.Stamina = Math.Min(villager.MaxStamina, villager.Stamina + restRate * deltaTime);
        if (villager.EquippedToolId != null)
        {
            villager.EquippedToolDurability = villager.EquippedToolMaxDurability;
        }

        return villager.Stamina >= villager.MaxStamina;
    }

    public bool CanAcceptVillager => CurrentOccupants.Count < MaxOccupants;

    /// <inheritdoc />
    public void ProcessStructureTick(float dt, VillagerLookup villagerLookup, ref StructureTickOutput output)
    {
        for (int i = CurrentOccupants.Count - 1; i >= 0; i--)
        {
            var villager = villagerLookup(CurrentOccupants[i]);
            if (villager == null)
            {
                CurrentOccupants.RemoveAt(i);
                continue;
            }

            if (RestVillager(villager, dt))
            {
                CurrentOccupants.RemoveAt(i);
                villager.CurrentActivity = null;
                output.PendingExits[output.ExitCount++] = new PendingExit(villager.Id);
            }
        }
    }

    public override void Tick(float deltaTime)
    {
        if (!IsActive) return;
    }
}
