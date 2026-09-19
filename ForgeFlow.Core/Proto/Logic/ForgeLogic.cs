using ForgeFlow.Core.Entities;
using ForgeFlow.Core.Proto.Prototypes;

namespace ForgeFlow.Core.Proto.Logic;

/// <summary>
/// Forge logic. Processes input items into crafted output using recipes.
/// Item creation is handled by StructureManager (Central Manager + Events pattern).
/// </summary>
public sealed class ForgeLogic : RecipeEntity, IStructureTickHandler, IItemOutputStrategy
{
    public new string ActiveRecipeId { get; set; } = string.Empty;
    private bool _isProcessing;

    /// <summary>Set when forging timer completes. Manager reads and clears to create output item and publish event.</summary>
    public bool PendingForgingComplete { get; set; }

    public ForgeLogic(EntityId id) : base(id)
    {
    }

    public override void InitializeFromProto(StructureProtoBase proto)
    {
        base.InitializeFromProto(proto);
        if (proto is ForgeProto fp)
        {
            ActiveRecipeId = fp.DefaultRecipeId;
        }
    }

    /// <inheritdoc />
    public void ProcessStructureTick(float dt, VillagerLookup villagerLookup, ref StructureTickOutput output)
    {
        if (PendingForgingComplete)
        {
            PendingForgingComplete = false;
            if (!string.IsNullOrEmpty(ActiveRecipeId))
            {
                output.PendingItems[output.ItemCount++] = new PendingItemOutput(ActiveRecipeId, 1);
            }
        }
    }

    public override void Tick(float deltaTime)
    {
        if (!IsActive) return;

        if (_isProcessing)
        {
            ProcessingTimer += deltaTime;
            if (ProcessingTimer >= ProcessingDuration)
            {
                ProcessingTimer = 0f;
                _isProcessing = false;
                PendingForgingComplete = true;
            }
        }
        else if (InputQueue.Count > 0 && !OutputQueueFull && !string.IsNullOrEmpty(ActiveRecipeId))
        {
            _isProcessing = true;
            ProcessingTimer = 0f;
        }
    }

    /// <inheritdoc />
    public ItemOutputResult ProcessItem(in PendingItemOutput pending, Structure structure, ItemOutputContext ctx)
    {
        var recipe = ctx.RecipeRegistry.Get(ActiveRecipeId);
        if (recipe != null)
        {
            var item = ctx.CreateItemFromProto(recipe.OutputItemId, OutputPosition);
            if (item != null)
            {
                OutputQueue.Enqueue(item);
            }
            return new ItemOutputResult(ItemOutputKind.Crafted, recipe.OutputItemId, pending.Quantity);
        }
        return new ItemOutputResult(ItemOutputKind.Crafted, pending.ItemProtoId, pending.Quantity);
    }
}
