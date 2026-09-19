using ForgeFlow.Core.Entities;
using ForgeFlow.Core.Proto.Prototypes;
using ForgeFlow.Core.Utilities;

namespace ForgeFlow.Core.Proto.Logic;

/// <summary>
/// Per-worker slot inside a gathering structure. Fixed-size, struct-based for zero-alloc.
/// </summary>
public struct WorkerSlot
{
    public ulong VillagerId;
    public string? ToolId;
    public string OutputResourceId;
    public float GatherProgress;
    public int PendingOutputAmount;
    public bool IsOccupied;
    public bool CycleComplete;
}

/// <summary>
/// Base logic for all gathering structures. Headless, runs in Core.
/// Workers enter via PathGate, occupy a slot, and gather resources
/// on a per-worker timer. Tool-dependent output: what a gathering spot
/// produces depends on the tool the villager brings.
/// Zero-alloc hot path: fixed-size arrays, no collections in Tick().
/// </summary>
public class GatheringLogicBase : RecipeEntity, IEntryGated, IStructureTickHandler, IItemOutputStrategy
{
    public GatheringLogicBase(EntityId id) : base(id) { }

    public string TargetResourceId { get; set; } = string.Empty;
    public BiomeType RequiredBiome { get; set; } = BiomeType.Plains;
    public int GatherAmountPerCycle { get; set; } = 1;
    public float GatherInterval { get; set; } = 3.0f;
    public float BiomeBonusMultiplier { get; set; } = 1.0f;
    public int MaxWorkerCapacity { get; set; } = 5;
    public int StaminaCostPerCycle { get; set; } = 10;
    public Dictionary<string, string> ToolOutputMap { get; } = new();

    private ResourceNodeLogic? _linkedNode;

    // Fixed-size worker slots — zero-alloc hot path
    private WorkerSlot[] _workerSlots = new WorkerSlot[5];
    private int _workerCount;

    // Fixed circular buffer for wait queue — zero-alloc
    private const int DefaultWaitQueueSize = 8;
    private CircularBuffer<ulong> _waitQueue = new(DefaultWaitQueueSize);

    public ResourceNodeLogic? LinkedNode => _linkedNode;
    public int WorkerCount => _workerCount;
    public int WaitQueueCount => _waitQueue.Count;
    public bool HasFreeSlot => _workerCount < MaxWorkerCapacity;

    /// <summary>Read-only access to the worker slots array.</summary>
    public WorkerSlot[] WorkerSlots => _workerSlots;

    public void Initialize(ResourceNodeLogic? node)
    {
        _linkedNode = node;
    }

    public override void InitializeFromProto(StructureProtoBase proto)
    {
        base.InitializeFromProto(proto);
        if (proto is GatheringProtoBase gp)
        {
            TargetResourceId = gp.TargetResourceId;
            RequiredBiome = gp.RequiredBiome;
            GatherAmountPerCycle = gp.GatherAmountPerCycle;
            GatherInterval = gp.GatherInterval;
            BiomeBonusMultiplier = gp.BiomeBonusMultiplier;
            MaxWorkerCapacity = gp.MaxWorkerCapacity;
            StaminaCostPerCycle = gp.StaminaCostPerCycle;
            ToolOutputMap.Clear();
            foreach (var kvp in gp.ToolOutputMap)
            {
                ToolOutputMap[kvp.Key] = kvp.Value;
            }
        }
        AllocateSlots();
    }

    /// <summary>Pre-allocates fixed-size worker slots. Called after MaxWorkerCapacity is set.</summary>
    public void AllocateSlots()
    {
        if (_workerSlots.Length != MaxWorkerCapacity)
        {
            _workerSlots = new WorkerSlot[MaxWorkerCapacity];
        }
        else
        {
            Array.Clear(_workerSlots, 0, _workerSlots.Length);
        }
        _workerCount = 0;
        _waitQueue.Clear();
    }

    public override void Tick(float deltaTime)
    {
        if (!IsActive) { return; }

        for (int i = 0; i < _workerSlots.Length; i++)
        {
            if (!_workerSlots[i].IsOccupied) { continue; }
            if (_workerSlots[i].CycleComplete) { continue; }

            _workerSlots[i].GatherProgress += deltaTime;
            if (_workerSlots[i].GatherProgress >= GatherInterval)
            {
                _workerSlots[i].CycleComplete = true;
                _workerSlots[i].PendingOutputAmount = CalculateGatherAmount();
                OnWorkerCycleComplete(i);
            }
        }
    }

    /// <summary>Calculates the harvest amount per cycle. Override in subclasses for bonuses.</summary>
    protected virtual int CalculateGatherAmount()
    {
        return (int)(GatherAmountPerCycle * BiomeBonusMultiplier);
    }

    /// <summary>Hook called when a worker completes a gathering cycle. Override for side effects.</summary>
    protected virtual void OnWorkerCycleComplete(int slotIndex)
    {
    }

    /// <summary>
    /// Checks whether a villager is eligible to enter this gathering structure.
    /// Rejects villagers with a full inventory (they can't pick up output).
    /// Capacity and wait-queue checks remain in PathGateManager (side effects).
    /// </summary>
    public EntryCheckResult CheckEntry(VillagerLogic villager)
    {
        if (villager.IsInventoryFull)
        {
            return EntryCheckResult.Rejected(EntryRejectionReason.InventoryFull);
        }
        return EntryCheckResult.Accepted;
    }

    /// <summary>Accepts a worker into a free slot. Returns the slot index, or -1 if full.</summary>
    public int AcceptWorker(ulong villagerId, string? toolId)
    {
        if (_workerCount >= MaxWorkerCapacity) { return -1; }

        for (int i = 0; i < _workerSlots.Length; i++)
        {
            if (!_workerSlots[i].IsOccupied)
            {
                _workerSlots[i] = new WorkerSlot
                {
                    VillagerId = villagerId,
                    ToolId = toolId,
                    OutputResourceId = ResolveOutputForTool(toolId),
                    GatherProgress = 0f,
                    PendingOutputAmount = 0,
                    IsOccupied = true,
                    CycleComplete = false
                };
                _workerCount++;
                return i;
            }
        }
        return -1;
    }

    /// <summary>Removes a worker from their slot by index. Returns the villager ID, or 0 if invalid.</summary>
    public ulong RemoveWorker(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= _workerSlots.Length) { return 0; }
        if (!_workerSlots[slotIndex].IsOccupied) { return 0; }

        ulong id = _workerSlots[slotIndex].VillagerId;
        _workerSlots[slotIndex] = default;
        _workerCount--;
        return id;
    }

    /// <summary>Removes a worker by villager ID. Returns the slot index, or -1 if not found.</summary>
    public int RemoveWorkerById(ulong villagerId)
    {
        for (int i = 0; i < _workerSlots.Length; i++)
        {
            if (_workerSlots[i].IsOccupied && _workerSlots[i].VillagerId == villagerId)
            {
                _workerSlots[i] = default;
                _workerCount--;
                return i;
            }
        }
        return -1;
    }

    /// <summary>Adds a villager ID to the fixed circular wait queue. Returns false if full.</summary>
    public bool EnqueueWaiting(ulong villagerId)
    {
        return _waitQueue.Enqueue(villagerId);
    }

    /// <summary>Dequeues the next villager from the wait queue. Returns 0 if empty.</summary>
    public ulong DequeueWaiting()
    {
        return _waitQueue.TryDequeue(out ulong id) ? id : 0;
    }

    /// <inheritdoc />
    public void ProcessStructureTick(float dt, VillagerLookup villagerLookup, ref StructureTickOutput output)
    {
        for (int i = 0; i < _workerSlots.Length; i++)
        {
            if (!_workerSlots[i].IsOccupied || !_workerSlots[i].CycleComplete)
            {
                continue;
            }

            var villager = villagerLookup(_workerSlots[i].VillagerId);
            if (villager == null)
            {
                RemoveWorker(i);
                continue;
            }

            // Harvest from linked node
            int amount = _workerSlots[i].PendingOutputAmount;
            if (_linkedNode != null && !_linkedNode.IsDepleted)
            {
                amount = _linkedNode.Harvest(amount);
            }
            else if (_linkedNode != null)
            {
                amount = 0;
            }

            if (amount > 0)
            {
                output.PendingItems[output.ItemCount++] = new PendingItemOutput(
                    _workerSlots[i].OutputResourceId, amount, _workerSlots[i].VillagerId);
            }

            // Drain stamina per cycle
            villager.Stamina = Math.Max(0f, villager.Stamina - StaminaCostPerCycle);

            // Reset cycle data
            _workerSlots[i].CycleComplete = false;
            _workerSlots[i].PendingOutputAmount = 0;
            _workerSlots[i].GatherProgress = 0f;

            // Check if worker should leave (stamina depleted, tool broken, or node depleted)
            bool shouldLeave = villager.Stamina <= 0 || villager.IsToolBroken;
            if (_linkedNode != null && _linkedNode.IsDepleted)
            {
                shouldLeave = true;
            }

            if (shouldLeave)
            {
                ulong exitingId = _workerSlots[i].VillagerId;
                RemoveWorker(i);
                villager.TargetNodeId = null;
                villager.CurrentActivity = null;
                output.PendingExits[output.ExitCount++] = new PendingExit(exitingId);
            }
        }

        // Promote from wait queue when slots free up
        while (_waitQueue.Count > 0 && HasFreeSlot)
        {
            ulong waitingId = DequeueWaiting();
            if (waitingId == 0)
            {
                break;
            }

            var waitingVillager = villagerLookup(waitingId);
            if (waitingVillager != null)
            {
                AcceptWorker(waitingId, waitingVillager.EquippedToolId);
                waitingVillager.State = VillagerState.Working;
                waitingVillager.CurrentActivity = $"Gathering {TargetResourceId}";
            }
        }
    }

    /// <summary>
    /// Resolves the output resource based on the villager's equipped tool.
    /// Bare hands (null or empty) → default/basic resource (e.g. sticks).
    /// Axe → logs, Pickaxe → ore, etc.
    /// </summary>
    public string ResolveOutputForTool(string? toolId)
    {
        string key = toolId ?? string.Empty;
        if (ToolOutputMap.Count > 0 && ToolOutputMap.TryGetValue(key, out var resourceId))
        {
            return resourceId;
        }
        return TargetResourceId;
    }

    /// <summary>
    /// Maps a target resource ID to the appropriate villager job.
    /// Shared helper to avoid duplicate switch expressions.
    /// </summary>
    public static VillagerJob GetJobForResource(string targetResourceId) => targetResourceId switch
    {
        "wood" => VillagerJob.Lumberjack,
        "ore" => VillagerJob.Miner,
        "food" => VillagerJob.Farmer,
        "herbs" => VillagerJob.Farmer,
        _ => VillagerJob.Builder
    };

    /// <inheritdoc />
    public ItemOutputResult ProcessItem(in PendingItemOutput pending, Structure structure, ItemOutputContext ctx)
    {
        var carried = ctx.ItemManager.CreateItem(pending.ItemProtoId, Tier, pending.Quantity);
        if (pending.TargetVillagerId.HasValue &&
            ctx.VillagerIndex.TryGetValue(pending.TargetVillagerId.Value, out var gatherVillager))
        {
            gatherVillager.TryPickUpItem(carried);
        }
        ctx.ItemManager.AddStock(pending.ItemProtoId, pending.Quantity, structure.Position);
        return new ItemOutputResult(
            ItemOutputKind.Gathered, pending.ItemProtoId, pending.Quantity,
            pending.TargetVillagerId ?? 0);
    }
}
