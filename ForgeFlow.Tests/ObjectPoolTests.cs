using ForgeFlow.Core.Utilities;

namespace ForgeFlow.Tests;

/// <summary>Tests for ObjectPool and ListPool.</summary>
public class ObjectPoolTests
{
    private class PooledItem
    {
        public int Value { get; set; }
        public PooledItem() { }
    }

    [Fact]
    public void ObjectPool_Get_ReturnsItem()
    {
        var pool = new ObjectPool<PooledItem>(4);
        var item = pool.Get();
        Assert.NotNull(item);
    }

    [Fact]
    public void ObjectPool_Return_MakesItemAvailable()
    {
        var pool = new ObjectPool<PooledItem>(0);
        var item = pool.Get();
        item.Value = 42;
        pool.Return(item);

        Assert.Equal(1, pool.AvailableCount);
    }

    [Fact]
    public void ObjectPool_ResetAction_CalledOnReturn()
    {
        var pool = new ObjectPool<PooledItem>(0, resetAction: item => item.Value = 0);
        var item = pool.Get();
        item.Value = 42;
        pool.Return(item);

        var reused = pool.Get();
        Assert.Equal(0, reused.Value);
    }

    [Fact]
    public void ObjectPool_InitialCapacity_PreAllocates()
    {
        var pool = new ObjectPool<PooledItem>(8);
        Assert.Equal(8, pool.AvailableCount);
        Assert.Equal(8, pool.TotalCreated);
    }

    [Fact]
    public void ObjectPool_ExceedsInitial_CreatesNew()
    {
        var pool = new ObjectPool<PooledItem>(2);
        var items = new List<PooledItem>();
        for (int i = 0; i < 5; i++)
        {
            items.Add(pool.Get());
        }
        Assert.Equal(5, items.Count);
        Assert.True(pool.TotalCreated >= 5);
    }

    [Fact]
    public void ObjectPool_MaxSize_LimitsPool()
    {
        var pool = new ObjectPool<PooledItem>(0, maxSize: 2);
        var a = pool.Get();
        var b = pool.Get();
        var c = pool.Get();

        pool.Return(a);
        pool.Return(b);
        pool.Return(c);

        Assert.True(pool.AvailableCount <= 2);
    }

    [Fact]
    public void ObjectPool_Clear_EmptiesPool()
    {
        var pool = new ObjectPool<PooledItem>(8);
        Assert.Equal(8, pool.AvailableCount);

        pool.Clear();
        Assert.Equal(0, pool.AvailableCount);
    }

    [Fact]
    public void ListPool_Get_ReturnsEmptyList()
    {
        var pool = new ListPool<int>(4);
        var list = pool.Get();
        Assert.NotNull(list);
        Assert.Empty(list);
    }

    [Fact]
    public void ListPool_Return_ClearsList()
    {
        var pool = new ListPool<int>(0);
        var list = pool.Get();
        list.Add(1);
        list.Add(2);
        list.Add(3);
        pool.Return(list);

        var reused = pool.Get();
        Assert.Empty(reused);
    }
}
