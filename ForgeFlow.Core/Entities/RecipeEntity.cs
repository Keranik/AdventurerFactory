using ForgeFlow.Core.Proto;
using ForgeFlow.Core.Proto.Prototypes;

namespace ForgeFlow.Core.Entities;

/// <summary>
/// Abstract base for structures that process recipes (input items → output items).
/// CraftStation, Forge, GatheringLogicBase, FusionAltar, and future recipe-driven
/// structures inherit from this. Owns I/O queues, processing timer, and output capacity.
/// </summary>
public abstract class RecipeEntity : Structure, IItemOutput
{
    protected RecipeEntity(EntityId id) : base(id) { }

    // ── Recipe state ──

    /// <summary>Whether a recipe is actively set and ready to process.</summary>
    public bool HasActiveRecipe => ActiveRecipeId != null;

    /// <summary>ID of the currently active recipe, or null if no recipe is set.</summary>
    public string? ActiveRecipeId { get; set; }

    /// <summary>Countdown timer for the current processing cycle.</summary>
    public float ProcessingTimer { get; set; }

    /// <summary>Queue of items waiting to be processed.</summary>
    public Queue<ItemInstance> InputQueue { get; } = new();

    /// <summary>Queue of finished items waiting to be picked up.</summary>
    public Queue<ItemInstance> OutputQueue { get; } = new();

    /// <summary>Maximum number of items that can sit in the output queue.</summary>
    public int MaxOutputQueueSize { get; set; } = 5;

    /// <summary>True when the output queue has reached its capacity limit.</summary>
    public bool OutputQueueFull => OutputQueue.Count >= MaxOutputQueueSize;

    /// <summary>Attempts to dequeue the next finished item from the output queue.</summary>
    public bool TryDequeueOutput(out ItemInstance? item)
    {
        if (OutputQueue.Count > 0)
        {
            item = OutputQueue.Dequeue();
            return true;
        }
        item = null;
        return false;
    }

    /// <summary>Enqueues an item into the input queue for processing.</summary>
    public bool TryEnqueueInput(ItemInstance item)
    {
        InputQueue.Enqueue(item);
        return true;
    }

    /// <summary>Initializes recipe-specific fields from a proto definition.</summary>
    public override void InitializeFromProto(StructureProtoBase proto)
    {
        base.InitializeFromProto(proto);
        MaxOutputQueueSize = proto.MaxOutputQueueSize;
    }
}
