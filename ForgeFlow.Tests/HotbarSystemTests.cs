using ForgeFlow.Core;
using ForgeFlow.Core.Events;
using ForgeFlow.Core.Systems;
using ForgeFlow.Core.Utilities;

namespace ForgeFlow.Tests;

/// <summary>Tests for HotbarSystem, HotbarCatalog, and HotbarSlot.</summary>
public class HotbarSystemTests
{
    [Fact]
    public void HotbarSystem_StartsWithTutorialDefaults()
    {
        var bus = new EventBus();
        var hotbar = new HotbarSystem(bus);

        Assert.Equal(HotbarActionType.PlaceStructure, hotbar.GetSlot(0).ActionType);
        Assert.Equal("Spawner", hotbar.GetSlot(0).StructureCategory);
        Assert.Equal(HotbarActionType.None, hotbar.GetSlot(1).ActionType);
        Assert.Equal(HotbarActionType.None, hotbar.GetSlot(2).ActionType);
        Assert.Equal(HotbarActionType.None, hotbar.GetSlot(3).ActionType);
        Assert.Equal(HotbarActionType.None, hotbar.GetSlot(4).ActionType);
    }

    [Fact]
    public void HotbarSystem_SetSlot_UpdatesAndPublishesEvent()
    {
        var bus = new EventBus();
        var hotbar = new HotbarSystem(bus);

        HotbarChangedEvent? received = null;
        bus.Subscribe<HotbarChangedEvent>(e => received = e);

        var slot = HotbarCatalog.PlaceStructure("FusionAltar");
        hotbar.SetSlot(7, slot);

        Assert.Equal(HotbarActionType.PlaceStructure, hotbar.GetSlot(7).ActionType);
        Assert.Equal("FusionAltar", hotbar.GetSlot(7).StructureCategory);
        Assert.NotNull(received);
        Assert.Equal(7, received.Value.SlotIndex);
    }

    [Fact]
    public void HotbarSystem_ClearSlot_ResetsToNone()
    {
        var bus = new EventBus();
        var hotbar = new HotbarSystem(bus);

        hotbar.ClearSlot(0);

        Assert.Equal(HotbarActionType.None, hotbar.GetSlot(0).ActionType);
    }

    [Fact]
    public void HotbarSystem_UnlockToFirstEmptySlot_FindsEmpty()
    {
        var bus = new EventBus();
        var hotbar = new HotbarSystem(bus);

        var slot = HotbarCatalog.PlaceStructure("ManaExtractor");
        hotbar.UnlockToFirstEmptySlot(slot);

        Assert.Equal(HotbarActionType.PlaceStructure, hotbar.GetSlot(1).ActionType);
        Assert.Equal("ManaExtractor", hotbar.GetSlot(1).StructureCategory);
    }

    [Fact]
    public void HotbarSystem_GetSlotsForSave_ReturnsClone()
    {
        var bus = new EventBus();
        var hotbar = new HotbarSystem(bus);

        var saved = hotbar.GetSlotsForSave();
        saved[0].ActionType = HotbarActionType.None;

        Assert.Equal(HotbarActionType.PlaceStructure, hotbar.GetSlot(0).ActionType);
    }

    [Fact]
    public void HotbarCatalog_GetAvailableActions_GrowsWithResearchTier()
    {
        var tier1 = HotbarCatalog.GetAvailableActions(1);
        var tier2 = HotbarCatalog.GetAvailableActions(2);
        var tier3 = HotbarCatalog.GetAvailableActions(3);

        Assert.True(tier2.Count > tier1.Count);
        Assert.True(tier3.Count > tier2.Count);
    }

    [Fact]
    public void HotbarSlot_DisplayLabel_IsNotEmpty()
    {
        var pathSlot = new HotbarSlot { ActionType = HotbarActionType.DrawPath };
        var structureSlot = HotbarCatalog.PlaceStructure("Spawner");
        var emptySlot = new HotbarSlot();

        Assert.NotEmpty(pathSlot.DisplayLabel);
        Assert.NotEmpty(structureSlot.DisplayLabel);
        Assert.Empty(emptySlot.DisplayLabel);
    }

    [Fact]
    public void GameBootstrapper_HasHotbarSystem()
    {
        ServiceLocator.Clear();
        var bootstrapper = new GameBootstrapper();
        bootstrapper.Bootstrap();

        Assert.NotNull(bootstrapper.Services.Get<HotbarSystem>());
        Assert.Equal(HotbarActionType.PlaceStructure, bootstrapper.Services.Get<HotbarSystem>().GetSlot(0).ActionType);
    }
}
