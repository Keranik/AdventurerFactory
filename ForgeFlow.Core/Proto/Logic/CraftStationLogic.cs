using ForgeFlow.Core.Entities;
using ForgeFlow.Core.Proto.Prototypes;

namespace ForgeFlow.Core.Proto.Logic;

/// <summary>
/// Craft station — villager-driven crafting structure.
/// Villagers carry raw materials to the station. On entry, the villager deposits
/// their carried items into StoredInputs. If enough ingredients are available to
/// satisfy the active recipe, the depositing villager stays and crafts the item.
/// After crafting completes, the villager picks up the crafted item and exits.
/// If not enough ingredients, the villager drops off what they have and exits immediately.
/// Item creation is handled by StructureManager (Central Manager + Events pattern).
/// Recipe data is resolved and cached via SetActiveRecipe().
/// </summary>
public sealed class CraftStationLogic : RecipeEntity, IEntryGated, IStructureTickHandler
{
    public List<string> AvailableRecipeIds { get; } = new();
    public new string? ActiveRecipeId { get; private set; }
    public Dictionary<string, int> ActiveRecipeInputs { get; } = new();
    public float ActiveRecipeDuration { get; private set; } = 4.0f;
    public string? ActiveRecipeOutputItemId { get; private set; }
    public Dictionary<string, int> StoredInputs { get; } = new();
    public float CraftTimer { get; set; }

    /// <summary>Set when a craft completes. Manager reads and clears to create output item and publish event.</summary>
    public bool PendingCraftComplete { get; set; }

    /// <summary>Villager IDs that just entered and need deposit processing by the manager.</summary>
    public List<ulong> PendingVillagerIds { get; } = new();

    /// <summary>The villager currently crafting at this station, or null if idle.</summary>
    public ulong? CraftingVillagerId { get; set; }

    /// <summary>Whether a villager is actively crafting at this station.</summary>
    public bool IsCrafting => CraftingVillagerId.HasValue;

    public CraftStationLogic(EntityId id) : base(id)
    {
        ProcessingDuration = 4.0f;
    }

    public override void InitializeFromProto(StructureProtoBase proto)
    {
        base.InitializeFromProto(proto);
    }

    /// <summary>
    /// Checks whether a villager is eligible to enter this craft station.
    /// Rejects when no recipe is set, or when the villager carries no items matching recipe inputs.
    /// </summary>
    public EntryCheckResult CheckEntry(VillagerLogic villager)
    {
        if (string.IsNullOrEmpty(ActiveRecipeId))
        {
            return EntryCheckResult.Rejected(EntryRejectionReason.NotEligible);
        }

        bool hasRelevantItem = false;
        for (int i = 0; i < villager.Inventory.Count; i++)
        {
            if (ActiveRecipeInputs.ContainsKey(villager.Inventory[i].ProtoId))
            {
                hasRelevantItem = true;
                break;
            }
        }
        if (!hasRelevantItem)
        {
            return EntryCheckResult.Rejected(EntryRejectionReason.NotCarryingRequiredItems);
        }

        return EntryCheckResult.Accepted;
    }

    /// <summary>
    /// Sets the active recipe with resolved data from the registry.
    /// Called by StructureManager or configuration code.
    /// </summary>
    public void SetActiveRecipe(string recipeId, Dictionary<string, int> inputs, float duration, string outputItemId)
    {
        ActiveRecipeId = recipeId;
        ActiveRecipeInputs.Clear();
        foreach (var kvp in inputs)
        {
            ActiveRecipeInputs[kvp.Key] = kvp.Value;
        }
        ActiveRecipeDuration = duration;
        ActiveRecipeOutputItemId = outputItemId;
        CraftTimer = 0f;
    }

    /// <summary>Clears the active recipe.</summary>
    public void ClearActiveRecipe()
    {
        ActiveRecipeId = null;
        ActiveRecipeInputs.Clear();
        ActiveRecipeDuration = 4.0f;
        ActiveRecipeOutputItemId = null;
        CraftTimer = 0f;
        CraftingVillagerId = null;
    }

    /// <inheritdoc />
    public void ProcessStructureTick(float dt, VillagerLookup villagerLookup, ref StructureTickOutput output)
    {
        // Process pending deposits
        for (int i = 0; i < PendingVillagerIds.Count; i++)
        {
            var villager = villagerLookup(PendingVillagerIds[i]);
            if (villager == null)
            {
                continue;
            }

            // Deposit only recipe-relevant items — non-recipe items (e.g. crafted weapons) stay in inventory
            if (villager.IsCarryingItems && ActiveRecipeInputs.Count > 0)
            {
                var droppedItems = villager.DropItemsMatchingKeys(ActiveRecipeInputs.Keys);
                foreach (var item in droppedItems)
                {
                    DepositItem(item.ProtoId, item.Quantity);
                }
            }

            // Only start crafting if: recipe is satisfied, nobody is already crafting,
            // and the villager has inventory space for the crafted output
            bool canCarryOutput = !villager.IsInventoryFull;
            if (canCarryOutput && !IsCrafting && HasRequiredIngredients() && TryStartCrafting())
            {
                CraftingVillagerId = PendingVillagerIds[i];
                villager.State = VillagerState.Working;
                villager.CurrentActivity = $"Crafting {ActiveRecipeOutputItemId}";
            }
            else
            {
                // Not enough ingredients, someone else is crafting, or inventory full — eject immediately
                villager.CurrentActivity = null;
                output.PendingExits[output.ExitCount++] = new PendingExit(PendingVillagerIds[i]);
            }
        }
        PendingVillagerIds.Clear();

        // Completed craft
        if (PendingCraftComplete)
        {
            PendingCraftComplete = false;
            if (!string.IsNullOrEmpty(ActiveRecipeOutputItemId) && CraftingVillagerId.HasValue)
            {
                ulong crafterId = CraftingVillagerId.Value;
                CraftingVillagerId = null;

                var crafter = villagerLookup(crafterId);
                if (crafter != null)
                {
                    crafter.CurrentActivity = null;
                }

                output.PendingItems[output.ItemCount++] = new PendingItemOutput(
                    ActiveRecipeOutputItemId, 1, crafterId);
                output.PendingExits[output.ExitCount++] = new PendingExit(crafterId);
            }
        }
    }

    /// <summary>
    /// Accepts items dropped off by a villager.
    /// </summary>
    public void DepositItem(string resourceId, int quantity = 1)
    {
        StoredInputs.TryGetValue(resourceId, out var current);
        StoredInputs[resourceId] = current + quantity;
    }

    /// <summary>
    /// Checks if all required ingredients for the active recipe are available.
    /// </summary>
    public bool HasRequiredIngredients()
    {
        if (ActiveRecipeInputs.Count == 0) return false;
        foreach (var req in ActiveRecipeInputs)
        {
            StoredInputs.TryGetValue(req.Key, out var have);
            if (have < req.Value) return false;
        }
        return true;
    }

    /// <summary>
    /// Begins crafting if ingredients are available. Consumes inputs.
    /// Returns true if crafting started.
    /// </summary>
    public bool TryStartCrafting()
    {
        if (ActiveRecipeInputs.Count == 0 || !HasRequiredIngredients()) return false;

        foreach (var req in ActiveRecipeInputs)
        {
            StoredInputs[req.Key] -= req.Value;
        }
        CraftTimer = 0f;
        return true;
    }

    /// <summary>
    /// Tick only progresses the craft timer when a villager is actively crafting.
    /// The structure does not craft on its own — a villager must be present.
    /// </summary>
    public override void Tick(float deltaTime)
    {
        if (!IsActive) return;
        if (!CraftingVillagerId.HasValue) return;
        if (string.IsNullOrEmpty(ActiveRecipeId)) return;

        CraftTimer += deltaTime;
        if (CraftTimer >= ActiveRecipeDuration)
        {
            CraftTimer = 0f;
            PendingCraftComplete = true;
        }
    }
}
