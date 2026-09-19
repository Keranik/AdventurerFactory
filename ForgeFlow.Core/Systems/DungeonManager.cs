using ForgeFlow.Core.Entities;
using ForgeFlow.Core.Events;
using ForgeFlow.Core.Proto.Logic;
using ForgeFlow.Core.Proto.Prototypes;

namespace ForgeFlow.Core.Systems;

/// <summary>
/// Central manager for villager dungeon runs. Subscribes to <see cref="SimulationTickEvent"/>
/// and processes all <see cref="DungeonPortalLogic"/> structures each tick:
/// advances occupant timers, reads pending results, handles survival (gold + level + loot + exit)
/// or death (remove villager), and publishes the appropriate events.
/// Follows the Central Manager + Events pattern: DungeonPortalLogic never publishes events.
/// </summary>
public sealed class DungeonManager : IGameSystem, IDisposable
{
    private readonly EntityManager _entityManager;
    private readonly PathGateManager _pathGateManager;
    private readonly VillagerSystem _villagerSystem;
    private readonly ItemManager _itemManager;
    private readonly EventBus _eventBus;

    public DungeonManager(
        EntityManager entityManager,
        PathGateManager pathGateManager,
        VillagerSystem villagerSystem,
        ItemManager itemManager,
        EventBus eventBus)
    {
        _entityManager = entityManager;
        _pathGateManager = pathGateManager;
        _villagerSystem = villagerSystem;
        _itemManager = itemManager;
        _eventBus = eventBus;

        _eventBus.Subscribe<SimulationTickEvent>(OnTick);
    }

    public void Dispose()
    {
        _eventBus.Unsubscribe<SimulationTickEvent>(OnTick);
    }

    private void OnTick(SimulationTickEvent e)
    {
        TickAllPortals(e.DeltaTime);
    }

    /// <summary>
    /// Iterates all structures looking for <see cref="DungeonPortalLogic"/> instances
    /// with active occupants, advances their timers, and processes pending results.
    /// </summary>
    private void TickAllPortals(float dt)
    {
        var structures = _entityManager.StructuresOrdered;
        for (int i = 0; i < structures.Count; i++)
        {
            var structure = structures[i];
            if (structure is DungeonPortalLogic dungeon && dungeon.CurrentOccupants.Count > 0)
            {
                TickDungeonOccupants(dungeon, dt);
            }
        }
    }

    /// <summary>
    /// Ticks all villagers inside a dungeon portal. Advances dungeon run timers
    /// and processes completed runs: survivors get gold + loot + exit via gate;
    /// dead villagers are removed from the simulation.
    /// Central Manager + Events pattern: Logic resolves survival → Manager
    /// handles state transitions, rewards, cleanup, and event publishing.
    /// </summary>
    private void TickDungeonOccupants(DungeonPortalLogic dungeon, float dt)
    {
        dungeon.TickOccupants(dt);

        for (int i = 0; i < dungeon.PendingResults.Count; i++)
        {
            var result = dungeon.PendingResults[i];
            if (!_villagerSystem.VillagerIndex.TryGetValue(result.VillagerId, out var villager))
            {
                continue;
            }

            villager.CurrentActivity = null;

            if (result.Survived)
            {
                // Award gold to the guild
                if (result.GoldReward > 0)
                {
                    _itemManager.AddStock("gold", result.GoldReward, dungeon.Position);
                }

                // Level up the surviving villager
                villager.Level++;

                // Award loot item if rolled
                if (result.LootItemId != null)
                {
                    var lootItem = _itemManager.CreateItem(result.LootItemId);
                    villager.TryPickUpItem(lootItem);
                }

                _eventBus.Publish(new VillagerDungeonCompletedEvent(
                    new EntityId(result.VillagerId), dungeon.Id, result.DungeonId, result.GoldReward, result.LootItemId));

                // Eject survivor via exit gate
                if (_pathGateManager.ExitVillagerViaGate(villager, dungeon.Id))
                {
                    villager.State = VillagerState.Travelling;
                    _eventBus.Publish(new VillagerLeftBuildingEvent(
                        new EntityId(result.VillagerId), dungeon.Id, dungeon.ProtoId));
                }
                else
                {
                    villager.State = VillagerState.EnteringBuilding;
                    dungeon.WaitingToExitIds.Add(new EntityId(result.VillagerId));
                }
            }
            else
            {
                // Villager died in the dungeon
                villager.State = VillagerState.Dead;
                villager.IsActive = false;
                _eventBus.Publish(new VillagerDiedInDungeonEvent(
                    new EntityId(result.VillagerId), dungeon.Id, result.DungeonId));
                _villagerSystem.RemoveVillager(result.VillagerId);
            }
        }

        dungeon.PendingResults.Clear();
    }
}
