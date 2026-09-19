using ForgeFlow.Core.Entities;
using ForgeFlow.Core.Proto.Logic;
using ForgeFlow.Core.Utilities;

namespace ForgeFlow.Tests;

/// <summary>
/// Tests for EntityIdFactory — the unified ID generation system.
/// Verifies that all entity types share a single counter and produce
/// globally unique, non-zero IDs.
/// </summary>
public class EntityIdFactoryTests
{
    public EntityIdFactoryTests() => EntityIdFactory.ResetForTesting();

    [Fact]
    public void Next_ReturnsSequentialIds()
    {
        var a = EntityIdFactory.Next();
        var b = EntityIdFactory.Next();
        var c = EntityIdFactory.Next();

        Assert.Equal(1UL, a);
        Assert.Equal(2UL, b);
        Assert.Equal(3UL, c);
    }

    [Fact]
    public void Next_NeverReturnsZero()
    {
        for (int i = 0; i < 100; i++)
        {
            Assert.True(EntityIdFactory.Next() > 0);
        }
    }

    [Fact]
    public void ResetForTesting_ResetsToZero_NextReturnsOne()
    {
        EntityIdFactory.Next();
        EntityIdFactory.Next();
        EntityIdFactory.ResetForTesting();

        Assert.Equal(1UL, EntityIdFactory.Next());
    }

    [Fact]
    public void HeroEntity_UsesSharedCounter()
    {
        var firstId = EntityIdFactory.Next(); // Gets 1
        var hero = new HeroEntity(EntityId.Next());      // Should get 2

        Assert.Equal(1UL, firstId);
        Assert.Equal(2UL, hero.Id);
    }

    [Fact]
    public void EntityBase_UsesSharedCounter()
    {
        var hero = new HeroEntity(EntityId.Next());       // Gets 1
        var segment = new PathSegmentLogic(EntityId.Next()) // Gets 2
        {
            Position = new GridPosRPG(0, 0),
            Facing = Direction.East
        };

        Assert.Equal(1UL, hero.Id);
        Assert.Equal(2UL, segment.Id);
    }

    [Fact]
    public void ItemInstance_UsesSharedCounter()
    {
        var hero = new HeroEntity(EntityId.Next());   // Gets 1
        var item = new ItemInstance { InstanceId = EntityIdFactory.Next() };  // Gets 2

        Assert.Equal(1UL, hero.Id);
        Assert.Equal(2UL, item.InstanceId);
    }

    [Fact]
    public void AllEntityTypes_ProduceGloballyUniqueIds()
    {
        var hero1 = new HeroEntity(EntityId.Next());       // 1
        var segment = new PathSegmentLogic(EntityId.Next())  // 2
        {
            Position = new GridPosRPG(0, 0),
            Facing = Direction.East
        };
        var item = new ItemInstance { InstanceId = EntityIdFactory.Next() };  // 3
        var hero2 = new HeroEntity(EntityId.Next());       // 4

        Assert.Equal(1UL, hero1.Id);
        Assert.Equal(2UL, segment.Id);
        Assert.Equal(3UL, item.InstanceId);
        Assert.Equal(4UL, hero2.Id);

        // No collisions
        var ids = new[] { hero1.Id, segment.Id, item.InstanceId, hero2.Id };
        Assert.Equal(ids.Length, new HashSet<ulong>(ids).Count);
    }

    [Fact]
    public void EntityBase_ResetIdCounter_ResetsSharedAllocator()
    {
        new HeroEntity(EntityId.Next());
        new ItemInstance();
        EntityBase.ResetIdCounter();

        var hero = new HeroEntity(EntityId.Next());
        Assert.Equal(1UL, hero.Id);
    }

    [Fact]
    public void HeroEntity_ResetIdCounter_ResetsSharedAllocator()
    {
        new PathSegmentLogic(EntityId.Next()) { Position = new GridPosRPG(0, 0), Facing = Direction.East };
        _ = EntityIdFactory.Next(); // simulate ItemInstance consuming an ID
        HeroEntity.ResetIdCounter();

        var item = new ItemInstance { InstanceId = EntityIdFactory.Next() };
        Assert.Equal(1UL, item.InstanceId);
    }

    [Fact]
    public void ItemInstance_ResetIdCounter_ResetsSharedAllocator()
    {
        new HeroEntity(EntityId.Next());
        new HeroEntity(EntityId.Next());
        ItemInstance.ResetIdCounter();

        var segment = new PathSegmentLogic(EntityId.Next())
        {
            Position = new GridPosRPG(0, 0),
            Facing = Direction.East
        };
        Assert.Equal(1UL, segment.Id);
    }

    [Fact]
    public void CurrentCounter_ReflectsLastAllocatedId()
    {
        EntityIdFactory.Next();
        EntityIdFactory.Next();
        EntityIdFactory.Next();

        Assert.Equal(3UL, EntityIdFactory.CurrentCounter);
    }

    [Fact]
    public void SetCounter_RestoresCounterFromSave()
    {
        EntityIdFactory.SetCounter(42);

        Assert.Equal(42UL, EntityIdFactory.CurrentCounter);
        Assert.Equal(43UL, EntityIdFactory.Next());
    }

    [Fact]
    public void ResetForNewGame_ResetsToZero()
    {
        EntityIdFactory.Next();
        EntityIdFactory.Next();
        EntityIdFactory.ResetForNewGame();

        Assert.Equal(0UL, EntityIdFactory.CurrentCounter);
        Assert.Equal(1UL, EntityIdFactory.Next());
    }

    [Fact]
    public void SaveLoad_PreservesCounter()
    {
        // Simulate allocating IDs during gameplay
        EntityIdFactory.Next(); // 1
        EntityIdFactory.Next(); // 2
        EntityIdFactory.Next(); // 3

        // Save the counter
        ulong savedCounter = EntityIdFactory.CurrentCounter;
        Assert.Equal(3UL, savedCounter);

        // Simulate loading into a fresh session
        EntityIdFactory.ResetForTesting();
        EntityIdFactory.SetCounter(savedCounter);

        // Next ID after load should continue from where we left off
        Assert.Equal(4UL, EntityIdFactory.Next());
    }
}
