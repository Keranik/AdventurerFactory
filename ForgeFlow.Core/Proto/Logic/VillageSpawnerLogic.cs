using ForgeFlow.Core.Entities;
using ForgeFlow.Core.Proto.Prototypes;

namespace ForgeFlow.Core.Proto.Logic;

/// <summary>
/// Village spawner logic. Periodically spawns villager entities.
/// Also acts as a residence (home) — villagers assigned to this home
/// can return to rest. Homes ignore inventory limits (unlike the Inn)
/// and rest at a higher rate.
/// </summary>
public sealed class VillageSpawnerLogic : ActivityEntity, IEntryGated, IStructureTickHandler
{
    public VillageSpawnerLogic(EntityId id) : base(id) { }

    public float SpawnInterval { get; set; } = 10.0f;
    public int MaxVillagers { get; set; } = 5;
    public string DefaultVillagerProtoId { get; set; } = "villager_basic";
    public List<VillagerLogic> PendingVillagers { get; } = new();

    // --- Residence / Home System ---

    /// <summary>Maximum number of villagers that can be assigned as residents.</summary>
    public new int MaxOccupants { get; set; } = 4;

    /// <summary>Duration in seconds for a full rest at home. Lower = faster than Inn.</summary>
    public float HomeRestDuration { get; set; } = 3.0f;

    /// <summary>IDs of villagers permanently assigned to this home.</summary>
    public List<ulong> Residents { get; } = new();

    /// <summary>IDs of villagers currently resting inside this home.</summary>
    public List<ulong> CurrentOccupants { get; } = new();

    private float _spawnTimer;
    private int _totalSpawned;

    public override void InitializeFromProto(StructureProtoBase proto)
    {
        base.InitializeFromProto(proto);
        if (proto is VillageSpawnerProto vsp)
        {
            SpawnInterval = vsp.SpawnInterval;
            MaxVillagers = vsp.MaxVillagers;
            DefaultVillagerProtoId = vsp.DefaultVillagerProtoId;
            MaxOccupants = vsp.MaxOccupants;
            HomeRestDuration = vsp.HomeRestDuration;
        }
    }

    public override void Tick(float deltaTime)
    {
        if (!IsActive) return;

        _spawnTimer += deltaTime;
        if (_spawnTimer >= SpawnInterval)
        {
            _spawnTimer -= SpawnInterval;
            SpawnVillager();
        }
    }

    public VillagerLogic? SpawnVillager()
    {
        if (_totalSpawned >= MaxVillagers) return null;

        var villager = new VillagerLogic(EntityId.Next())
        {
            Name = $"Villager_{_totalSpawned}",
            Position = OutputPosition,
            State = VillagerState.Idle
        };

        PendingVillagers.Add(villager);
        _totalSpawned++;
        return villager;
    }

    /// <summary>Decrements the spawn counter so a new villager can be spawned later.</summary>
    public void DecrementSpawnCount()
    {
        if (_totalSpawned > 0) _totalSpawned--;
    }

    // --- Residence Methods ---

    /// <summary>
    /// Checks whether a villager is eligible to enter this home.
    /// Rejects when full or when the villager is not a registered resident.
    /// </summary>
    public EntryCheckResult CheckEntry(VillagerLogic villager)
    {
        if (!CanAcceptOccupant)
        {
            return EntryCheckResult.Rejected(EntryRejectionReason.StructureFull);
        }
        if (!Residents.Contains(villager.Id))
        {
            return EntryCheckResult.Rejected(EntryRejectionReason.NotResident);
        }
        return EntryCheckResult.Accepted;
    }

    /// <summary>Whether this home has room for another resident assignment.</summary>
    public bool CanAssignResident => Residents.Count < MaxOccupants;

    /// <summary>Whether there is room for another villager to rest inside.</summary>
    public new bool CanAcceptOccupant => CurrentOccupants.Count < MaxOccupants;

    /// <summary>
    /// Assigns a villager as a permanent resident of this home.
    /// Returns false if the home is at capacity.
    /// </summary>
    public bool AssignResident(ulong villagerId)
    {
        if (Residents.Count >= MaxOccupants) { return false; }
        if (Residents.Contains(villagerId)) { return true; }
        Residents.Add(villagerId);
        return true;
    }

    /// <summary>
    /// Removes a villager from this home's resident list.
    /// </summary>
    public bool RemoveResident(ulong villagerId)
    {
        return Residents.Remove(villagerId);
    }

    /// <summary>
    /// Accepts a villager to rest inside this home.
    /// Unlike the Inn, homes ignore inventory — villagers can rest even while carrying items.
    /// Only assigned residents (or any villager if the home has capacity) are accepted.
    /// Returns true if accepted.
    /// </summary>
    public bool AcceptVillagerForRest(VillagerLogic villager)
    {
        if (CurrentOccupants.Count >= MaxOccupants) { return false; }
        if (!Residents.Contains(villager.Id)) { return false; }
        CurrentOccupants.Add(villager.Id);
        return true;
    }

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
                output.PendingExits[output.ExitCount++] = new PendingExit(villager.Id, StructureExitReason.RestedAtHome);
            }
        }
    }

    /// <summary>
    /// Restores a villager's stamina at the home rest rate (faster than Inn).
    /// Also repairs equipped tools. Returns true when fully rested.
    /// </summary>
    public bool RestVillager(VillagerLogic villager, float deltaTime)
    {
        float effectiveDuration = HomeRestDuration > 0f ? HomeRestDuration : 3.0f;
        float restRate = villager.MaxStamina / effectiveDuration;
        villager.Stamina = Math.Min(villager.MaxStamina, villager.Stamina + restRate * deltaTime);
        if (villager.EquippedToolId != null)
        {
            villager.EquippedToolDurability = villager.EquippedToolMaxDurability;
        }

        return villager.Stamina >= villager.MaxStamina;
    }
}
