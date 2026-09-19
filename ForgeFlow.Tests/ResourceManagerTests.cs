using ForgeFlow.Core.Entities;
using ForgeFlow.Core.Events;
using ForgeFlow.Core.Systems;

namespace ForgeFlow.Tests;

/// <summary>
/// Tests for ItemManager — the single source of truth for all virtual stock and physical item operations.
/// </summary>
public class ItemManagerTests
{
    private static (ItemManager im, EventBus bus) CreateItemManager()
    {
        var bus = new EventBus();
        var im = new ItemManager(bus);
        return (im, bus);
    }

    // ── GetStock ─────────────────────────────────────────────────────

    [Fact]
    public void GetStock_ReturnsZero_WhenResourceNotPresent()
    {
        var (im, _) = CreateItemManager();
        Assert.Equal(0, im.GetStock("wood"));
    }

    [Fact]
    public void GetStock_ReturnsCorrectAmount_AfterAddStock()
    {
        var (im, _) = CreateItemManager();
        im.AddStock("wood", 10);
        Assert.Equal(10, im.GetStock("wood"));
    }

    // ── HasStock ─────────────────────────────────────────────────────

    [Fact]
    public void HasStock_ReturnsFalse_WhenInsufficientStock()
    {
        var (im, _) = CreateItemManager();
        im.AddStock("wood", 5);
        Assert.False(im.HasStock("wood", 10));
    }

    [Fact]
    public void HasStock_ReturnsTrue_WhenExactStock()
    {
        var (im, _) = CreateItemManager();
        im.AddStock("wood", 10);
        Assert.True(im.HasStock("wood", 10));
    }

    [Fact]
    public void HasStock_ReturnsTrue_WhenExcessStock()
    {
        var (im, _) = CreateItemManager();
        im.AddStock("wood", 15);
        Assert.True(im.HasStock("wood", 10));
    }

    [Fact]
    public void HasStock_ReturnsFalse_WhenResourceNotPresent()
    {
        var (im, _) = CreateItemManager();
        Assert.False(im.HasStock("gold", 1));
    }

    // ── AddStock ─────────────────────────────────────────────────────

    [Fact]
    public void AddStock_AccumulatesMultipleAdds()
    {
        var (im, _) = CreateItemManager();
        im.AddStock("ore", 5);
        im.AddStock("ore", 3);
        Assert.Equal(8, im.GetStock("ore"));
    }

    [Fact]
    public void AddStock_PublishesResourceProducedEvent()
    {
        var (im, bus) = CreateItemManager();
        ResourceProducedEvent? received = null;
        bus.Subscribe<ResourceProducedEvent>(e => received = e);

        im.AddStock("wood", 7);

        Assert.NotNull(received);
        Assert.Equal("wood", received.Value.ResourceId);
        Assert.Equal(7, received.Value.Quantity);
    }

    [Fact]
    public void AddStock_DoesNothing_WhenAmountIsZeroOrNegative()
    {
        var (im, bus) = CreateItemManager();
        ResourceProducedEvent? received = null;
        bus.Subscribe<ResourceProducedEvent>(e => received = e);

        im.AddStock("wood", 0);
        im.AddStock("wood", -5);

        Assert.Null(received);
        Assert.Equal(0, im.GetStock("wood"));
    }

    [Fact]
    public void AddStock_DoesNothing_WhenResourceIdIsNullOrEmpty()
    {
        var (im, _) = CreateItemManager();
        im.AddStock(null!, 5);
        im.AddStock("", 5);
        Assert.Empty(im.VirtualStocks);
    }

    // ── RemoveStock ──────────────────────────────────────────────────

    [Fact]
    public void RemoveStock_DeductsCorrectly()
    {
        var (im, _) = CreateItemManager();
        im.AddStock("food", 20);
        var result = im.RemoveStock("food", 8);
        Assert.True(result);
        Assert.Equal(12, im.GetStock("food"));
    }

    [Fact]
    public void RemoveStock_ReturnsFalse_WhenInsufficientStock()
    {
        var (im, _) = CreateItemManager();
        im.AddStock("food", 5);
        var result = im.RemoveStock("food", 10);
        Assert.False(result);
        Assert.Equal(5, im.GetStock("food"));
    }

    [Fact]
    public void RemoveStock_ReturnsFalse_WhenResourceNotPresent()
    {
        var (im, _) = CreateItemManager();
        Assert.False(im.RemoveStock("gold", 1));
    }

    [Fact]
    public void RemoveStock_ReturnsFalse_WhenAmountIsZeroOrNegative()
    {
        var (im, _) = CreateItemManager();
        im.AddStock("food", 10);
        Assert.False(im.RemoveStock("food", 0));
        Assert.False(im.RemoveStock("food", -3));
        Assert.Equal(10, im.GetStock("food"));
    }

    // ── SetStock ─────────────────────────────────────────────────────

    [Fact]
    public void SetStock_SetsExactValue()
    {
        var (im, _) = CreateItemManager();
        im.SetStock("gold", 999);
        Assert.Equal(999, im.GetStock("gold"));
    }

    [Fact]
    public void SetStock_OverwritesPreviousValue()
    {
        var (im, _) = CreateItemManager();
        im.AddStock("gold", 50);
        im.SetStock("gold", 100);
        Assert.Equal(100, im.GetStock("gold"));
    }

    [Fact]
    public void SetStock_DoesNothing_WhenResourceIdIsNullOrEmpty()
    {
        var (im, _) = CreateItemManager();
        im.SetStock(null!, 50);
        im.SetStock("", 50);
        Assert.Empty(im.VirtualStocks);
    }

    // ── Clear ────────────────────────────────────────────────────────

    [Fact]
    public void Clear_RemovesAllStocks()
    {
        var (im, _) = CreateItemManager();
        im.AddStock("wood", 10);
        im.AddStock("ore", 20);
        im.AddStock("food", 30);

        im.Clear();

        Assert.Empty(im.VirtualStocks);
        Assert.Equal(0, im.GetStock("wood"));
    }

    // ── InitializeStartingStocks ─────────────────────────────────────

    [Fact]
    public void InitializeStartingStocks_NormalDifficulty_SetsDefaultValues()
    {
        var (im, _) = CreateItemManager();
        im.InitializeStartingStocks(Difficulty.Normal, 1.0f, 500);

        Assert.Equal(50, im.GetStock("wood"));
        Assert.Equal(30, im.GetStock("ore"));
        Assert.Equal(40, im.GetStock("food"));
        Assert.Equal(500, im.GetStock("gold"));
    }

    [Fact]
    public void InitializeStartingStocks_CasualDifficulty_TripleValues()
    {
        var (im, _) = CreateItemManager();
        im.InitializeStartingStocks(Difficulty.Casual, 1.0f, 500);

        Assert.Equal(150, im.GetStock("wood"));
        Assert.Equal(90, im.GetStock("ore"));
        Assert.Equal(120, im.GetStock("food"));
    }

    [Fact]
    public void InitializeStartingStocks_EasyDifficulty_DoubleValues()
    {
        var (im, _) = CreateItemManager();
        im.InitializeStartingStocks(Difficulty.Easy, 1.0f, 500);

        Assert.Equal(100, im.GetStock("wood"));
        Assert.Equal(60, im.GetStock("ore"));
        Assert.Equal(80, im.GetStock("food"));
    }

    [Fact]
    public void InitializeStartingStocks_WithMultiplier_ScalesValues()
    {
        var (im, _) = CreateItemManager();
        im.InitializeStartingStocks(Difficulty.Normal, 2.0f, 500);

        Assert.Equal(100, im.GetStock("wood"));
        Assert.Equal(60, im.GetStock("ore"));
        Assert.Equal(80, im.GetStock("food"));
    }

    [Fact]
    public void InitializeStartingStocks_ClearsExistingStocks()
    {
        var (im, _) = CreateItemManager();
        im.AddStock("diamonds", 999);
        im.InitializeStartingStocks(Difficulty.Normal, 1.0f, 500);

        Assert.Equal(0, im.GetStock("diamonds"));
        Assert.Equal(4, im.VirtualStocks.Count); // wood, ore, food, gold
    }

    // ── ReturnVillagerInventory ──────────────────────────────────────

    [Fact]
    public void ReturnVillagerInventory_AddsItemsToStocks()
    {
        var (im, _) = CreateItemManager();
        var inventory = new List<ItemInstance>
        {
            new ItemInstance { ProtoId = "wood", Quantity = 5 },
            new ItemInstance { ProtoId = "ore", Quantity = 3 }
        };

        im.ReturnVillagerInventory(inventory);

        Assert.Equal(5, im.GetStock("wood"));
        Assert.Equal(3, im.GetStock("ore"));
        Assert.Empty(inventory);
    }

    [Fact]
    public void ReturnVillagerInventory_ClearsInventory()
    {
        var (im, _) = CreateItemManager();
        var inventory = new List<ItemInstance>
        {
            new ItemInstance { ProtoId = "food", Quantity = 2 }
        };

        im.ReturnVillagerInventory(inventory);
        Assert.Empty(inventory);
    }

    [Fact]
    public void ReturnVillagerInventory_SkipsEmptyProtoIds()
    {
        var (im, _) = CreateItemManager();
        var inventory = new List<ItemInstance>
        {
            new ItemInstance { ProtoId = "", Quantity = 5 },
            new ItemInstance { ProtoId = "ore", Quantity = 2 }
        };

        im.ReturnVillagerInventory(inventory);

        Assert.Equal(0, im.GetStock(""));
        Assert.Equal(2, im.GetStock("ore"));
    }

    [Fact]
    public void ReturnVillagerInventory_AccumulatesWithExistingStock()
    {
        var (im, _) = CreateItemManager();
        im.AddStock("wood", 10);

        var inventory = new List<ItemInstance>
        {
            new ItemInstance { ProtoId = "wood", Quantity = 5 }
        };

        im.ReturnVillagerInventory(inventory);
        Assert.Equal(15, im.GetStock("wood"));
    }

    // ── TrySpendResources ────────────────────────────────────────────

    [Fact]
    public void TrySpendResources_DeductsAll_WhenSufficient()
    {
        var (im, _) = CreateItemManager();
        im.AddStock("wood", 20);
        im.AddStock("ore", 15);

        var cost = new Dictionary<string, int> { { "wood", 5 }, { "ore", 10 } };
        var result = im.TrySpendResources(cost);

        Assert.True(result);
        Assert.Equal(15, im.GetStock("wood"));
        Assert.Equal(5, im.GetStock("ore"));
    }

    [Fact]
    public void TrySpendResources_ReturnsFalse_WhenInsufficient()
    {
        var (im, _) = CreateItemManager();
        im.AddStock("wood", 3);
        im.AddStock("ore", 15);

        var cost = new Dictionary<string, int> { { "wood", 5 }, { "ore", 10 } };
        var result = im.TrySpendResources(cost);

        Assert.False(result);
        // No partial deduction — values unchanged
        Assert.Equal(3, im.GetStock("wood"));
        Assert.Equal(15, im.GetStock("ore"));
    }

    [Fact]
    public void TrySpendResources_ReturnsFalse_WhenResourceMissing()
    {
        var (im, _) = CreateItemManager();
        im.AddStock("wood", 10);

        var cost = new Dictionary<string, int> { { "wood", 5 }, { "mythril", 1 } };
        var result = im.TrySpendResources(cost);

        Assert.False(result);
        Assert.Equal(10, im.GetStock("wood"));
    }

    [Fact]
    public void TrySpendResources_HandlesEmptyCost()
    {
        var (im, _) = CreateItemManager();
        im.AddStock("wood", 10);

        var cost = new Dictionary<string, int>();
        var result = im.TrySpendResources(cost);

        Assert.True(result);
        Assert.Equal(10, im.GetStock("wood"));
    }

    // ── Integration: VirtualStocks dictionary proxy ──────────────────

    [Fact]
    public void VirtualStocks_Dictionary_IsCanonicalStore()
    {
        var (im, _) = CreateItemManager();
        im.AddStock("gold", 100);

        Assert.Same(im.VirtualStocks, im.VirtualStocks);
        Assert.True(im.VirtualStocks.ContainsKey("gold"));
        Assert.Equal(100, im.VirtualStocks["gold"]);
    }

    // ── Physical Item Pool Operations ────────────────────────────────

    [Fact]
    public void CreateItem_ReturnsInstanceWithCorrectProtoId()
    {
        var (im, _) = CreateItemManager();
        var item = im.CreateItem("iron_ore", tier: 1, quantity: 3);

        Assert.Equal("iron_ore", item.ProtoId);
        Assert.Equal(1, item.Tier);
        Assert.Equal(3, item.Quantity);
        Assert.True(item.InstanceId > 0);
    }

    [Fact]
    public void DestroyItem_ReturnsItemToPool()
    {
        var (im, _) = CreateItemManager();
        var item = im.CreateItem("wood");
        im.DestroyItem(item);

        Assert.True(im.PoolAvailable >= 1);
    }

    [Fact]
    public void CreateEquipment_SetsEquipmentFields()
    {
        var (im, _) = CreateItemManager();
        var item = im.CreateEquipment("iron_sword_1", tier: 1, EquipSlot.Weapon,
            damage: 12f, defense: 0f, speed: 1.0f, critChance: 0.05f, "spark_effect");

        Assert.Equal("iron_sword_1", item.ProtoId);
        Assert.Equal(EquipSlot.Weapon, item.Slot);
        Assert.Equal(12f, item.Damage);
        Assert.Equal(0.05f, item.CritChance);
        Assert.Equal("spark_effect", item.SpecialEffectId);
        Assert.True(item.IsOnPath);
    }

    [Fact]
    public void CreateScrap_HalvesDamageAndDefense()
    {
        var (im, _) = CreateItemManager();
        var scrap = im.CreateScrap("iron_sword_1", tier: 1, EquipSlot.Weapon,
            damage: 20f, defense: 10f, speed: 1.0f, critChance: 0.1f,
            position: default);

        Assert.True(scrap.IsScrap);
        Assert.Equal(10f, scrap.Damage);  // halved
        Assert.Equal(5f, scrap.Defense);  // halved
    }
}
