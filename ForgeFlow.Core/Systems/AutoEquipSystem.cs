using ForgeFlow.Core.Data;
using ForgeFlow.Core.Entities;
using ForgeFlow.Core.Events;
using ForgeFlow.Core.Proto;

namespace ForgeFlow.Core.Systems;

public sealed class AutoEquipSystem : IGameSystem, IDisposable
{
    private static readonly Direction[] CardinalDirections =
        { Direction.North, Direction.East, Direction.South, Direction.West };

    private readonly EventBus _eventBus;
    private readonly ClassRegistry _classRegistry;
    private readonly EntityManager _entityManager;

    public AutoEquipSystem(EventBus eventBus, ClassRegistry classRegistry, EntityManager entityManager)
    {
        _eventBus = eventBus;
        _classRegistry = classRegistry;
        _entityManager = entityManager;

        _eventBus.Subscribe<SimulationTickEvent>(OnTick);
    }

    public void Dispose()
    {
        _eventBus.Unsubscribe<SimulationTickEvent>(OnTick);
    }

    private void OnTick(SimulationTickEvent e) => Tick(_entityManager.Heroes, _entityManager);

    public void Tick(
        IReadOnlyList<HeroEntity> heroes,
        EntityManager entityManager)
    {
        foreach (var hero in heroes)
        {
            if (hero.State != HeroState.OnPath) continue;

            foreach (var adjacentDir in CardinalDirections)
            {
                var adjacentPos = hero.Position.Neighbor(adjacentDir);
                var structure = entityManager.GetStructureAt(adjacentPos);
                if (structure is not IItemOutput adjacentOutput || adjacentOutput.OutputQueue.Count == 0) continue;

                TryEquipFromOutput(hero, adjacentOutput);
            }

            var onTopStructure = entityManager.GetStructureAt(hero.Position);
            if (onTopStructure is IItemOutput topOutput && topOutput.OutputQueue.Count > 0)
            {
                TryEquipFromOutput(hero, topOutput);
            }
        }
    }

    private void TryEquipFromOutput(HeroEntity hero, IItemOutput output)
    {
        if (output.OutputQueue.Count == 0) return;

        var peeked = output.OutputQueue.Peek();

        if (hero.HasSlotFilled(peeked.Slot))
        {
            var existing = hero.GetEquippedInSlot(peeked.Slot);
            if (existing != null && existing.Tier >= peeked.Tier)
                return;
        }

        if (IsGearCompatible(hero, peeked))
        {
            output.OutputQueue.Dequeue();
            hero.Equip(peeked.ToEquipped());
            _eventBus.Publish(new GearEquippedEvent(hero.Id, peeked.ProtoId, peeked.Slot, hero.Position));
        }
    }

    private bool IsGearCompatible(HeroEntity hero, ItemInstance item)
    {
        var classDef = _classRegistry.Get(hero.ClassId);
        if (classDef == null) return true;

        if (classDef.PreferredGearSlots.Count > 0 &&
            !classDef.PreferredGearSlots.Contains(item.Slot))
        {
            return false;
        }

        return true;
    }
}
