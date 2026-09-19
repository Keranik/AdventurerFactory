using ForgeFlow.Core.Data;
using ForgeFlow.Core.Entities;
using ForgeFlow.Core.Events;
using ForgeFlow.Core.Save;
using ForgeFlow.Core.Systems;

namespace ForgeFlow.Tests;

/// <summary>Tests for GuildData — banners, logos, and guild identity events.</summary>
public class GuildDataTests
{
    [Fact]
    public void GuildData_AvailableBanners_HasEntries()
    {
        Assert.True(GuildData.AvailableBanners.Length >= 4);
        Assert.Contains("banner_default", GuildData.AvailableBanners);
    }

    [Fact]
    public void GuildData_AvailableLogos_HasEntries()
    {
        Assert.True(GuildData.AvailableLogos.Length >= 4);
        Assert.Contains("logo_sword", GuildData.AvailableLogos);
    }

    [Fact]
    public void GuildData_ShowsBanner_AndLogo()
    {
        var guild = new GuildData
        {
            GuildName = "Dragon Knights",
            BannerId = "banner_dragon",
            LogoId = "logo_shield"
        };

        Assert.Equal("Dragon Knights", guild.GuildName);
        Assert.Equal("banner_dragon", guild.BannerId);
        Assert.Equal("logo_shield", guild.LogoId);
    }

    [Fact]
    public void GuildCreatedEvent_HasCorrectFields()
    {
        var evt = new GuildCreatedEvent("My Guild", "banner_lion");

        Assert.Equal("My Guild", evt.GuildName);
        Assert.Equal("banner_lion", evt.BannerId);
    }

    [Fact]
    public void GuildData_GuildCreatedEvent_PublishesCorrectly()
    {
        var eventBus = new EventBus();
        string capturedName = "";
        string capturedBanner = "";

        eventBus.Subscribe<GuildCreatedEvent>(e =>
        {
            capturedName = e.GuildName;
            capturedBanner = e.BannerId;
        });

        var guild = new GuildData { GuildName = "Test Guild", BannerId = "banner_dragon" };
        eventBus.Publish(new GuildCreatedEvent(guild.GuildName, guild.BannerId));

        Assert.Equal("Test Guild", capturedName);
        Assert.Equal("banner_dragon", capturedBanner);
    }

    [Fact]
    public void GuildData_GoldDisplay_ShowsBalance()
    {
        var bus = new EventBus();
        var itemMgr = new ItemManager(bus);
        itemMgr.SetStock("gold", 500);

        Assert.Equal(500, itemMgr.GetStock("gold"));
    }

    [Fact]
    public void GuildData_GoldChangedEvent_UpdatesDisplay()
    {
        var eventBus = new EventBus();
        int capturedNewAmount = 0;

        eventBus.Subscribe<GoldChangedEvent>(e => capturedNewAmount = e.NewAmount);

        var itemMgr = new ItemManager(eventBus);
        itemMgr.SetStock("gold", 100);
        int oldAmount = itemMgr.GetStock("gold");
        itemMgr.AddStock("gold", 50);
        eventBus.Publish(new GoldChangedEvent(oldAmount, itemMgr.GetStock("gold"), "dungeon_reward"));

        Assert.Equal(150, capturedNewAmount);
        Assert.Equal(150, itemMgr.GetStock("gold"));
    }

    [Fact]
    public void NewGameSettings_ContainsGuildFields()
    {
        var settings = new NewGameSettings
        {
            GuildName = "Eagles",
            BannerId = "banner_eagle",
            LogoId = "logo_star"
        };

        Assert.Equal("Eagles", settings.GuildName);
        Assert.Equal("banner_eagle", settings.BannerId);
        Assert.Equal("logo_star", settings.LogoId);
    }
}
