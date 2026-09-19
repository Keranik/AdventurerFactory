using ForgeFlow.Core;
using ForgeFlow.Core.Commands;
using ForgeFlow.Core.Data;
using ForgeFlow.Core.Entities;
using ForgeFlow.Core.Events;
using ForgeFlow.Core.Modding;
using ForgeFlow.Core.Proto.Logic;
using ForgeFlow.Core.Save;
using ForgeFlow.Core.Systems;
using ForgeFlow.Core.Utilities;

namespace ForgeFlow.Tests;

/// <summary>
/// Tests covering the re-audit fixes applied after the initial 30-item audit:
///   #17 — CommandLog.SetTick is wired into SimulationTicker.FixedTick.
///   #30 — VillagerSystem enforces GatingLimits.MaxVillagersPerTier + publishes GatingBlockedEvent.
///   #14 — ModLoader publishes ModLoadReportEvent after LoadAllMods.
///   #26 — SaveManager.ValidateLoadedData walks heroes/villagers and publishes SaveLoadReportEvent.
/// </summary>
public class AuditReAuditFixTests
{
    public AuditReAuditFixTests()
    {
        EntityIdFactory.ResetForTesting();
    }

    // ── #17 ──────────────────────────────────────────────────────────

    [Fact]
    public void CommandLog_DispatchDuringTick_RecordsCurrentTick()
    {
        var boot = new GameBootstrapper();
        boot.Bootstrap();

        var log = new CommandLog(capacity: 16) { IsEnabled = true };
        boot.Services.Get<CommandBus>().Log = log;
        boot.Services.Get<CommandBus>().Register<TestCommand>(_ => CommandResult.Ok());

        // Dispatch only after several early-tick events so the recorded Tick
        // is strictly positive — proving SetTick is wired into FixedTick.
        int earlyTickSeen = 0;
        boot.Services.Get<EventBus>().Subscribe<SimulationEarlyTickEvent>(_ =>
        {
            earlyTickSeen++;
            if (earlyTickSeen == 3)
            {
                boot.Services.Get<CommandBus>().Dispatch(new TestCommand(42));
            }
        });

        boot.Services.Get<SimulationTicker>().Update(SimulationTicker.FixedTimeStep * 4f);

        Assert.True(log.Count >= 1);
        var entry = log.GetEntry(0);
        Assert.Equal("TestCommand", entry.CommandTypeName);
        // FixedTick sets tick to _tickCount (pre-increment), so entries on
        // the Nth early-tick event report Tick = N-1. N=3 → Tick=2.
        Assert.Equal(2UL, entry.Tick);
    }

    // ── #30 ──────────────────────────────────────────────────────────

    [Fact]
    public void VillagerSystem_OverTierCap_BlocksAddAndPublishesEvent()
    {
        var eventBus = new EventBus();
        var itemManager = new ItemManager(eventBus);
        var gating = new GatingLimits();
        var research = new ResearchManager(eventBus); // defaults to tier 1
        var sys = new VillagerSystem(eventBus, itemManager, gating, research);

        int blockedCount = 0;
        GatingBlockedEvent lastBlocked = default;
        eventBus.Subscribe<GatingBlockedEvent>(e =>
        {
            if (e.BlockedAction == "villager") { blockedCount++; lastBlocked = e; }
        });

        int tierMax = gating.GetMaxVillagers(research.CurrentTier);
        Assert.Equal(5, tierMax); // tier 1 baseline

        // Fill exactly to the cap — all should succeed.
        for (int i = 0; i < tierMax; i++)
        {
            Assert.True(sys.AddVillager(new VillagerLogic(EntityId.Next()) { Name = $"v{i}" }));
        }

        // The 6th add must be rejected and publish a GatingBlockedEvent.
        Assert.False(sys.AddVillager(new VillagerLogic(EntityId.Next()) { Name = "overflow" }));
        Assert.Equal(tierMax, sys.Count);
        Assert.Equal(1, blockedCount);
        Assert.Equal("villager", lastBlocked.BlockedAction);
        Assert.Equal(tierMax, lastBlocked.MaxAllowed);
    }

    [Fact]
    public void VillagerSystem_NoGating_AddsUnconditionally()
    {
        // Backward-compat: ctor without gating params = no cap enforcement.
        // Protects the 97 existing AddVillager call sites.
        var eventBus = new EventBus();
        var itemManager = new ItemManager(eventBus);
        var sys = new VillagerSystem(eventBus, itemManager);

        for (int i = 0; i < 50; i++)
        {
            Assert.True(sys.AddVillager(new VillagerLogic(EntityId.Next()) { Name = $"v{i}" }));
        }
        Assert.Equal(50, sys.Count);
    }

    // ── #14 ──────────────────────────────────────────────────────────

    [Fact]
    public void ModLoader_LoadAllMods_PublishesReportEvent()
    {
        var boot = new GameBootstrapper();
        boot.Bootstrap(); // No mods path → ModLoader is wired but LoadAllMods not yet called.

        int publishCount = 0;
        ModLoadReportEvent lastEvt = default;
        boot.Services.Get<EventBus>().Subscribe<ModLoadReportEvent>(e => { publishCount++; lastEvt = e; });

        // Empty mods directory — load should still publish a summary event (0 mods).
        var tempDir = Path.Combine(Path.GetTempPath(), "forgeflow-mod-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        try
        {
            boot.Services.Get<ModLoader>().LoadAllMods(tempDir);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }

        Assert.Equal(1, publishCount);
        Assert.Equal(0, lastEvt.ModsLoaded);
    }

    // ── #26 ──────────────────────────────────────────────────────────

    [Fact]
    public void SaveManager_ValidateLoadedData_DetectsOrphanedHeroClassAndItemIds()
    {
        var eventBus = new EventBus();
        int reportPublished = 0;
        SaveLoadReportEvent lastReport = default;
        eventBus.Subscribe<SaveLoadReportEvent>(e => { reportPublished++; lastReport = e; });

        var itemRegistry = new ItemRegistry();
        itemRegistry.Register(new ForgeFlow.Core.Proto.Prototypes.ItemProto { Id = "wood", DisplayName = "Wood" });

        // Leave classRegistry empty so every referenced hero class is "orphaned".
        var classRegistry = new ClassRegistry();

        var data = new SaveData
        {
            ResourceStocks = new Dictionary<string, int> { ["wood"] = 5, ["unobtanium"] = 1 },
            Heroes =
            {
                new HeroSaveData
                {
                    Id = 1,
                    ClassId = "ghost_class",
                    Equipment = { new EquippedItemSaveData { ProtoId = "mystery_sword" } }
                }
            },
            Villagers =
            {
                new VillagerSaveData { Id = 2, EquippedToolId = "bogus_axe" }
            }
        };

        var saveManager = new SaveManager();
        var report = saveManager.ValidateLoadedData(data, itemRegistry, classRegistry, eventBus);

        Assert.Contains("unobtanium", report.OrphanedItemIds);
        Assert.Contains("mystery_sword", report.OrphanedItemIds);
        Assert.Contains("bogus_axe", report.OrphanedItemIds);
        Assert.Contains("ghost_class", report.OrphanedClassIds);
        Assert.False(report.IsClean);
        Assert.Equal(1, reportPublished);
        Assert.False(lastReport.IsClean);
        Assert.True(lastReport.OrphanedIdCount >= 4);
    }
}
