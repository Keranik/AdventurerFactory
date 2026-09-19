using ForgeFlow.Core.Data;
using ForgeFlow.Core.Entities;
using ForgeFlow.Core.Events;
using ForgeFlow.Core.Proto;
using ForgeFlow.Core.Proto.Logic;

namespace ForgeFlow.Core.Systems;

public sealed class FusionCalculator : IGameSystem, IDisposable
{
    private readonly ClassRegistry _classRegistry;
    private readonly ItemRegistry _itemRegistry;
    private readonly ItemManager _itemManager;
    private readonly EventBus _eventBus;
    private readonly EntityManager _entityManager;

    public FusionCalculator(ClassRegistry classRegistry, ItemRegistry itemRegistry, ItemManager itemManager, EventBus eventBus, EntityManager entityManager)
    {
        _classRegistry = classRegistry;
        _itemRegistry = itemRegistry;
        _itemManager = itemManager;
        _eventBus = eventBus;
        _entityManager = entityManager;

        _eventBus.Subscribe<SimulationTickEvent>(OnTick);
    }

    public void Dispose()
    {
        _eventBus.Unsubscribe<SimulationTickEvent>(OnTick);
    }

    private void OnTick(SimulationTickEvent e) => Tick(_entityManager);

    public void Tick(EntityManager entityManager)
    {
        foreach (var kvp in entityManager.Structures)
        {
            if (kvp.Value is not FusionAltarLogic altar) continue;
            if (!altar.IsActive || !altar.IsReadyToFuse) continue;

            if (altar.QueuedHeroIds.Count >= altar.RequiredInputCount)
            {
                FuseHeroes(altar, entityManager);
            }
            else if (altar.QueuedItems.Count >= altar.RequiredInputCount)
            {
                FuseItems(altar);
            }
        }
    }

    private void FuseHeroes(
        FusionAltarLogic altar,
        EntityManager entityManager)
    {
        var heroIndex = entityManager.HeroIndex;

        var sourceHeroes = new List<HeroEntity>();
        foreach (var id in altar.QueuedHeroIds)
        {
            if (heroIndex.TryGetValue(new EntityId(id), out var hero))
            {
                sourceHeroes.Add(hero);
            }
        }

        if (sourceHeroes.Count < 2) return;

        var hero1 = sourceHeroes[0];
        var hero2 = sourceHeroes[1];

        if (hero1.Level != hero2.Level) return;

        string resultClassId = DetermineHybridClass(hero1.ClassId, hero2.ClassId);
        int resultLevel = hero1.Level + 1;

        var fusedHero = new HeroEntity(EntityId.Next())
        {
            ClassId = resultClassId,
            Level = resultLevel,
            Position = altar.OutputPosition,
            State = HeroState.OnPath,
            Morale = Math.Max(hero1.Morale, hero2.Morale)
        };

        var bestGear = MergeBestGear(hero1, hero2);
        foreach (var item in bestGear)
        {
            fusedHero.Equip(item);
        }

        entityManager.FuseHeroes(hero1.Id, hero2.Id, fusedHero);

        altar.QueuedHeroIds.Clear();

        _eventBus.Publish(new HeroFusedEvent(hero1.Id, hero2.Id, fusedHero.Id, resultClassId, resultLevel));
    }

    private void FuseItems(FusionAltarLogic altar)
    {
        if (altar.QueuedItems.Count < 2) return;

        var item1 = altar.QueuedItems[0];
        var item2 = altar.QueuedItems[1];

        int fusedTier = Math.Max(item1.Tier, item2.Tier) + 1;
        var fusedItem = _itemManager.CreateItem(item1.ProtoId, fusedTier);
        fusedItem.Slot = item1.Slot;
        fusedItem.Damage = (item1.Damage + item2.Damage) * 0.65f;
        fusedItem.Defense = (item1.Defense + item2.Defense) * 0.65f;
        fusedItem.Speed = Math.Max(item1.Speed, item2.Speed);
        fusedItem.CritChance = Math.Min((item1.CritChance + item2.CritChance) * 0.6f, 0.5f);
        fusedItem.Position = altar.OutputPosition;
        fusedItem.IsOnPath = true;

        altar.OutputQueue.Enqueue(fusedItem);

        // Return consumed items to the pool
        _itemManager.DestroyItem(item1);
        _itemManager.DestroyItem(item2);
        altar.QueuedItems.RemoveRange(0, 2);

        _eventBus.Publish(new ItemCraftedEvent(fusedItem.ProtoId, altar.Position));
    }

    private string DetermineHybridClass(string classId1, string classId2)
    {
        if (classId1 == classId2) return classId1;

        var sortedPair = string.Compare(classId1, classId2, StringComparison.Ordinal) < 0
            ? $"{classId1}_{classId2}"
            : $"{classId2}_{classId1}";

        foreach (var hybrid in _classRegistry.GetHybrids())
        {
            if (hybrid.FusionSourceClassIds.Count >= 2)
            {
                var ids = hybrid.FusionSourceClassIds;
                string a = ids[0];
                string b = ids[1];
                var key = string.Compare(a, b, StringComparison.Ordinal) < 0
                    ? $"{a}_{b}"
                    : $"{b}_{a}";
                if (key == sortedPair) return hybrid.Id;
            }
        }

        return classId1;
    }

    private List<EquippedItem> MergeBestGear(HeroEntity hero1, HeroEntity hero2)
    {
        var bestBySlot = new Dictionary<EquipSlot, EquippedItem>();

        for (int i = 0; i < hero1.Equipment.Count; i++)
        {
            var item = hero1.Equipment[i];
            if (!bestBySlot.TryGetValue(item.Slot, out var existing) || item.Tier > existing.Tier)
            {
                bestBySlot[item.Slot] = item.Clone();
            }
        }

        for (int i = 0; i < hero2.Equipment.Count; i++)
        {
            var item = hero2.Equipment[i];
            if (!bestBySlot.TryGetValue(item.Slot, out var existing) || item.Tier > existing.Tier)
            {
                bestBySlot[item.Slot] = item.Clone();
            }
        }

        var result = new List<EquippedItem>(bestBySlot.Count);
        foreach (var kvp in bestBySlot)
        {
            result.Add(kvp.Value);
        }
        return result;
    }
}
