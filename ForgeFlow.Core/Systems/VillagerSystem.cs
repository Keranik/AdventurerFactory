using ForgeFlow.Core.Entities;
using ForgeFlow.Core.Events;
using ForgeFlow.Core.Proto;
using ForgeFlow.Core.Proto.Logic;
using ForgeFlow.Core.Proto.Prototypes;

namespace ForgeFlow.Core.Systems;

/// <summary>
/// Manages the villager simulation loop. Ticks all villagers,
/// collects their work output, and converts it to resources.
/// Self-subscribes to EarlyTick.
/// </summary>
public sealed class VillagerSystem : IGameSystem, IDisposable
{
    private readonly EventBus _eventBus;
    private readonly ItemManager _itemManager;
    private readonly GatingLimits? _gatingLimits;
    private readonly ResearchManager? _researchManager;

    public List<VillagerLogic> Villagers { get; } = new();
    public Dictionary<ulong, VillagerLogic> VillagerIndex { get; } = new();

    /// <summary>
    /// Cache-friendly struct array kept strictly aligned with <see cref="Villagers"/>.
    /// Index <c>i</c> in this array always matches index <c>i</c> in <see cref="Villagers"/>.
    /// Used by <see cref="PathTrafficSystem"/> for its movement hot-path.
    /// </summary>
    public VillagerMovementState[] MovementStateBuffer = new VillagerMovementState[64];
    public int MovementStateCount;

    // Auxiliary index: villager ID → index in Villagers/MovementStateBuffer for O(1) swap-remove.
    private readonly Dictionary<ulong, int> _movementIndexById = new();

    public VillagerSystem(
        EventBus eventBus,
        ItemManager itemManager,
        GatingLimits? gatingLimits = null,
        ResearchManager? researchManager = null)
    {
        _eventBus = eventBus;
        _itemManager = itemManager;
        _gatingLimits = gatingLimits;
        _researchManager = researchManager;

        _eventBus.Subscribe<SimulationEarlyTickEvent>(OnEarlyTick);
    }

    public void Dispose()
    {
        _eventBus.Unsubscribe<SimulationEarlyTickEvent>(OnEarlyTick);
    }

    private void OnEarlyTick(SimulationEarlyTickEvent e) => Tick(e.DeltaTime, _itemManager);

    /// <summary>
    /// Adds a villager to the simulation. Returns <c>false</c> and publishes
    /// <see cref="GatingBlockedEvent"/> when the tier villager cap is exceeded
    /// (only enforced when both <see cref="GatingLimits"/> and <see cref="ResearchManager"/>
    /// are injected — tests that construct a <see cref="VillagerSystem"/> directly
    /// run without gating).
    /// </summary>
    public bool AddVillager(VillagerLogic villager)
    {
        if (_gatingLimits != null && _researchManager != null)
        {
            int tier = _researchManager.CurrentTier;
            int tierMax = _gatingLimits.GetMaxVillagers(tier);
            int hardCap = tierMax * _gatingLimits.HardCapMultiplier;
            if (Villagers.Count >= hardCap)
            {
                _eventBus.Publish(new GatingBlockedEvent("villager", Villagers.Count, hardCap, tier));
                return false;
            }
            if (Villagers.Count >= tierMax)
            {
                _eventBus.Publish(new GatingBlockedEvent("villager", Villagers.Count, tierMax, tier));
                return false;
            }
        }

        Villagers.Add(villager);
        VillagerIndex[villager.Id] = villager;

        // Grow struct array if needed (doubles capacity)
        if (MovementStateCount >= MovementStateBuffer.Length)
        {
            var grown = new VillagerMovementState[MovementStateBuffer.Length * 2];
            Array.Copy(MovementStateBuffer, grown, MovementStateCount);
            MovementStateBuffer = grown;
        }
        MovementStateBuffer[MovementStateCount] = VillagerMovementState.FromLogic(villager);
        _movementIndexById[villager.Id] = MovementStateCount;
        MovementStateCount++;
        return true;
    }

    public void RemoveVillager(ulong villagerId)
    {
        if (!VillagerIndex.TryGetValue(villagerId, out var villager)) { return; }

        // O(1) removal from Villagers list
        if (!_movementIndexById.TryGetValue(villagerId, out int idx)) { return; }
        int lastIdx = Villagers.Count - 1;

        if (idx < lastIdx)
        {
            // Swap with last in the Villagers list
            Villagers[idx] = Villagers[lastIdx];
            // Swap with last in struct array
            MovementStateBuffer[idx] = MovementStateBuffer[MovementStateCount - 1];
            // Update index for the swapped entity
            _movementIndexById[Villagers[idx].Id] = idx;
        }

        Villagers.RemoveAt(lastIdx);
        MovementStateCount--;
        VillagerIndex.Remove(villagerId);
        _movementIndexById.Remove(villagerId);
    }

    /// <summary>O(1) swap-remove helper — kept for any future internal list operations.</summary>
    private static void SwapRemove<T>(List<T> list, T item)
    {
        int idx = list.IndexOf(item);
        if (idx < 0) { return; }
        int last = list.Count - 1;
        if (idx < last)
        {
            list[idx] = list[last];
        }
        list.RemoveAt(last);
    }

    /// <summary>Assigns a job to a villager by ID. Publishes a VillagerJobAssignedEvent.</summary>
    public bool AssignVillagerJob(ulong villagerId, VillagerJob job)
    {
        if (!VillagerIndex.TryGetValue(villagerId, out var villager)) { return false; }
        villager.AssignJob(job);
        _eventBus.Publish(new VillagerJobAssignedEvent(new EntityId(villagerId), job.ToString()));
        return true;
    }

    /// <summary>
    /// Ticks all villagers and converts accumulated work to resources.
    /// Villagers bound to a gathering structure (TargetNodeId set) are
    /// managed by StructureManager — skip them here to avoid double production.
    /// </summary>
    public void Tick(float deltaTime, ItemManager itemManager)
    {
        foreach (var villager in Villagers)
        {
            // Structure-bound workers are ticked by StructureManager
            if (villager.TargetNodeId.HasValue) { continue; }

            villager.Tick(deltaTime);

            if (villager.State == VillagerState.Working)
            {
                // Drain stamina for unbound workers (Central Manager + Events pattern)
                villager.Stamina = Math.Max(0f, villager.Stamina - villager.StaminaCostPerCycle * deltaTime);

                float output = villager.CollectWorkOutput();
                if (output >= 1.0f)
                {
                    int produced = (int)output;
                    string resourceId = GetResourceForJob(villager.Profession);
                    if (!string.IsNullOrEmpty(resourceId))
                    {
                        itemManager.AddStock(resourceId, produced, villager.Position);
                    }
                }
            }
        }
    }

    private static string GetResourceForJob(VillagerJob job) => job switch
    {
        VillagerJob.Lumberjack => "wood",
        VillagerJob.Miner => "ore",
        VillagerJob.Farmer => "food",
        VillagerJob.Builder => "materials",
        VillagerJob.Scholar => "knowledge",
        VillagerJob.Merchant => "gold",
        _ => string.Empty
    };

    public void Clear()
    {
        Villagers.Clear();
        VillagerIndex.Clear();
        _movementIndexById.Clear();
        MovementStateCount = 0;
    }

    public int Count => Villagers.Count;
}
