using ForgeFlow.Core;
using ForgeFlow.Core.Data;
using ForgeFlow.Core.Entities;
using ForgeFlow.Core.Events;
using ForgeFlow.Core.Proto;
using ForgeFlow.Core.Proto.Logic;
using ForgeFlow.Core.Proto.Prototypes;
using ForgeFlow.Core.Save;
using ForgeFlow.Core.Systems;

namespace ForgeFlow.Tests;

/// <summary>
/// Comprehensive tests for the Filter Splitter system:
/// - Multi-rule hero evaluation (EvaluateRoute)
/// - Single-item filter (FilteredItemId)
/// - Villager evaluation (EvaluateVillagerRoute)
/// - Pending filter change pattern
/// - InitializeFromProto
/// - GateRule.Matches for newly implemented CarryingItem/HasItem/HasToolType
/// - StructureManager filter splitter API
/// - FilterSplitterFilterChangedEvent publishing
/// </summary>
public class FilterSplitterTests
{
    // ── Hero Evaluation (EvaluateRoute) ──────────────────────────────

    [Fact]
    public void EvaluateRoute_NoRules_ReturnsDefaultDirection()
    {
        var splitter = new FilterSplitterLogic(EntityId.Next());
        splitter.DefaultDirection = Direction.East;

        var hero = new HeroEntity(EntityId.Next()) { Level = 5 };
        Assert.Equal(Direction.East, splitter.EvaluateRoute(hero));
    }

    [Fact]
    public void EvaluateRoute_FirstMatchWins()
    {
        var splitter = new FilterSplitterLogic(EntityId.Next());
        splitter.DefaultDirection = Direction.East;
        splitter.Rules.Add(new GateRule
        {
            ConditionType = GateConditionType.MinLevel,
            ConditionValue = "10",
            OutputDirection = Direction.North
        });
        splitter.Rules.Add(new GateRule
        {
            ConditionType = GateConditionType.MinLevel,
            ConditionValue = "5",
            OutputDirection = Direction.South
        });

        var hero = new HeroEntity(EntityId.Next()) { Level = 12 };
        Assert.Equal(Direction.North, splitter.EvaluateRoute(hero));
    }

    [Fact]
    public void EvaluateRoute_NoMatch_ReturnsDefault()
    {
        var splitter = new FilterSplitterLogic(EntityId.Next());
        splitter.DefaultDirection = Direction.West;
        splitter.Rules.Add(new GateRule
        {
            ConditionType = GateConditionType.MinLevel,
            ConditionValue = "50",
            OutputDirection = Direction.North
        });

        var hero = new HeroEntity(EntityId.Next()) { Level = 3 };
        Assert.Equal(Direction.West, splitter.EvaluateRoute(hero));
    }

    [Fact]
    public void EvaluateRoute_HasProfession_MatchesHero()
    {
        var splitter = new FilterSplitterLogic(EntityId.Next());
        splitter.Rules.Add(new GateRule
        {
            ConditionType = GateConditionType.HasProfession,
            ConditionValue = "Forester",
            OutputDirection = Direction.South
        });

        var hero = new HeroEntity(EntityId.Next()) { Profession = WorkerProfession.Forester };
        Assert.Equal(Direction.South, splitter.EvaluateRoute(hero));
    }

    // ── Single-Item Filter ──────────────────────────────────────────

    [Fact]
    public void FilteredItemId_DefaultsToNull()
    {
        var splitter = new FilterSplitterLogic(EntityId.Next());
        Assert.Null(splitter.FilteredItemId);
    }

    [Fact]
    public void SetFilteredItem_SetsFilteredItemId()
    {
        var splitter = new FilterSplitterLogic(EntityId.Next());
        splitter.SetFilteredItem("sticks");
        Assert.Equal("sticks", splitter.FilteredItemId);
    }

    [Fact]
    public void SetFilteredItem_SetsPendingFilterChange()
    {
        var splitter = new FilterSplitterLogic(EntityId.Next());
        Assert.False(splitter.PendingFilterChange);
        splitter.SetFilteredItem("sticks");
        Assert.True(splitter.PendingFilterChange);
    }

    [Fact]
    public void SetFilteredItem_SameItem_NoPendingChange()
    {
        var splitter = new FilterSplitterLogic(EntityId.Next());
        splitter.SetFilteredItem("sticks");
        splitter.PendingFilterChange = false;
        splitter.SetFilteredItem("sticks"); // same item
        Assert.False(splitter.PendingFilterChange);
    }

    [Fact]
    public void SetFilteredItem_DifferentItem_SetsPendingChange()
    {
        var splitter = new FilterSplitterLogic(EntityId.Next());
        splitter.SetFilteredItem("sticks");
        splitter.PendingFilterChange = false;
        splitter.SetFilteredItem("ore");
        Assert.True(splitter.PendingFilterChange);
        Assert.Equal("ore", splitter.FilteredItemId);
    }

    [Fact]
    public void ClearFilter_SetsNullAndPendingChange()
    {
        var splitter = new FilterSplitterLogic(EntityId.Next());
        splitter.SetFilteredItem("sticks");
        splitter.PendingFilterChange = false;
        splitter.ClearFilter();
        Assert.Null(splitter.FilteredItemId);
        Assert.True(splitter.PendingFilterChange);
    }

    [Fact]
    public void ClearFilter_WhenAlreadyNull_NoPendingChange()
    {
        var splitter = new FilterSplitterLogic(EntityId.Next());
        Assert.Null(splitter.FilteredItemId);
        splitter.ClearFilter();
        Assert.False(splitter.PendingFilterChange);
    }

    // ── Villager Evaluation (EvaluateVillagerRoute) ──────────────────

    [Fact]
    public void EvaluateVillagerRoute_NoFilterNoRules_ReturnsDefault()
    {
        var splitter = new FilterSplitterLogic(EntityId.Next());
        splitter.DefaultDirection = Direction.East;

        var villager = new VillagerLogic(EntityId.Next());
        Assert.Equal(Direction.East, splitter.EvaluateVillagerRoute(villager));
    }

    [Fact]
    public void EvaluateVillagerRoute_SingleItemFilter_MatchesCarriedItem()
    {
        var splitter = new FilterSplitterLogic(EntityId.Next());
        splitter.DefaultDirection = Direction.East;
        splitter.FilteredOutputDirection = Direction.North;
        splitter.SetFilteredItem("sticks");

        var villager = new VillagerLogic(EntityId.Next());
        villager.TryPickUpItem(new ItemInstance { ProtoId = "sticks" });

        Assert.Equal(Direction.North, splitter.EvaluateVillagerRoute(villager));
    }

    [Fact]
    public void EvaluateVillagerRoute_SingleItemFilter_NoMatch_ReturnsDefault()
    {
        var splitter = new FilterSplitterLogic(EntityId.Next());
        splitter.DefaultDirection = Direction.East;
        splitter.FilteredOutputDirection = Direction.North;
        splitter.SetFilteredItem("sticks");

        var villager = new VillagerLogic(EntityId.Next());
        villager.TryPickUpItem(new ItemInstance { ProtoId = "ore" });

        Assert.Equal(Direction.East, splitter.EvaluateVillagerRoute(villager));
    }

    [Fact]
    public void EvaluateVillagerRoute_SingleItemFilter_EmptyInventory_ReturnsDefault()
    {
        var splitter = new FilterSplitterLogic(EntityId.Next());
        splitter.DefaultDirection = Direction.East;
        splitter.SetFilteredItem("sticks");

        var villager = new VillagerLogic(EntityId.Next());
        Assert.Equal(Direction.East, splitter.EvaluateVillagerRoute(villager));
    }

    [Fact]
    public void EvaluateVillagerRoute_SingleItemFilter_CaseInsensitive()
    {
        var splitter = new FilterSplitterLogic(EntityId.Next());
        splitter.FilteredOutputDirection = Direction.South;
        splitter.SetFilteredItem("Sticks");

        var villager = new VillagerLogic(EntityId.Next());
        villager.TryPickUpItem(new ItemInstance { ProtoId = "sticks" });

        Assert.Equal(Direction.South, splitter.EvaluateVillagerRoute(villager));
    }

    [Fact]
    public void EvaluateVillagerRoute_SingleItemFilter_TakesPriorityOverRules()
    {
        var splitter = new FilterSplitterLogic(EntityId.Next());
        splitter.DefaultDirection = Direction.East;
        splitter.FilteredOutputDirection = Direction.North;
        splitter.SetFilteredItem("sticks");
        splitter.Rules.Add(new GateRule
        {
            ConditionType = GateConditionType.HasProfession,
            ConditionValue = "Lumberjack",
            OutputDirection = Direction.West
        });

        var villager = new VillagerLogic(EntityId.Next());
        villager.Profession = VillagerJob.Lumberjack;
        villager.TryPickUpItem(new ItemInstance { ProtoId = "sticks" });

        // Single-item filter wins over the matching profession rule
        Assert.Equal(Direction.North, splitter.EvaluateVillagerRoute(villager));
    }

    [Fact]
    public void EvaluateVillagerRoute_MultiRule_HasProfession()
    {
        var splitter = new FilterSplitterLogic(EntityId.Next());
        splitter.DefaultDirection = Direction.East;
        splitter.Rules.Add(new GateRule
        {
            ConditionType = GateConditionType.HasProfession,
            ConditionValue = "Lumberjack",
            OutputDirection = Direction.North
        });

        var villager = new VillagerLogic(EntityId.Next()) { Profession = VillagerJob.Lumberjack };
        Assert.Equal(Direction.North, splitter.EvaluateVillagerRoute(villager));
    }

    [Fact]
    public void EvaluateVillagerRoute_MultiRule_HasProfession_NoMatch()
    {
        var splitter = new FilterSplitterLogic(EntityId.Next());
        splitter.DefaultDirection = Direction.East;
        splitter.Rules.Add(new GateRule
        {
            ConditionType = GateConditionType.HasProfession,
            ConditionValue = "Miner",
            OutputDirection = Direction.North
        });

        var villager = new VillagerLogic(EntityId.Next()) { Profession = VillagerJob.Lumberjack };
        Assert.Equal(Direction.East, splitter.EvaluateVillagerRoute(villager));
    }

    [Fact]
    public void EvaluateVillagerRoute_MultiRule_HasJobClass()
    {
        var splitter = new FilterSplitterLogic(EntityId.Next());
        splitter.DefaultDirection = Direction.East;
        splitter.Rules.Add(new GateRule
        {
            ConditionType = GateConditionType.HasJobClass,
            ConditionValue = "Warrior",
            OutputDirection = Direction.South
        });

        var villager = new VillagerLogic(EntityId.Next()) { TrainedClass = VillagerClass.Warrior };
        Assert.Equal(Direction.South, splitter.EvaluateVillagerRoute(villager));
    }

    [Fact]
    public void EvaluateVillagerRoute_MultiRule_CarryingItem()
    {
        var splitter = new FilterSplitterLogic(EntityId.Next());
        splitter.DefaultDirection = Direction.East;
        splitter.Rules.Add(new GateRule
        {
            ConditionType = GateConditionType.CarryingItem,
            ConditionValue = "ore",
            OutputDirection = Direction.West
        });

        var villager = new VillagerLogic(EntityId.Next());
        villager.TryPickUpItem(new ItemInstance { ProtoId = "ore" });

        Assert.Equal(Direction.West, splitter.EvaluateVillagerRoute(villager));
    }

    [Fact]
    public void EvaluateVillagerRoute_MultiRule_CarryingItem_EmptyInventory()
    {
        var splitter = new FilterSplitterLogic(EntityId.Next());
        splitter.DefaultDirection = Direction.East;
        splitter.Rules.Add(new GateRule
        {
            ConditionType = GateConditionType.CarryingItem,
            ConditionValue = "ore",
            OutputDirection = Direction.West
        });

        var villager = new VillagerLogic(EntityId.Next());
        Assert.Equal(Direction.East, splitter.EvaluateVillagerRoute(villager));
    }

    [Fact]
    public void EvaluateVillagerRoute_MultiRule_HasToolType()
    {
        var splitter = new FilterSplitterLogic(EntityId.Next());
        splitter.DefaultDirection = Direction.East;
        splitter.Rules.Add(new GateRule
        {
            ConditionType = GateConditionType.HasToolType,
            ConditionValue = "iron_axe",
            OutputDirection = Direction.North
        });

        var villager = new VillagerLogic(EntityId.Next()) { EquippedToolId = "iron_axe" };
        Assert.Equal(Direction.North, splitter.EvaluateVillagerRoute(villager));
    }

    [Fact]
    public void EvaluateVillagerRoute_MultiRule_HasToolType_NoTool()
    {
        var splitter = new FilterSplitterLogic(EntityId.Next());
        splitter.DefaultDirection = Direction.East;
        splitter.Rules.Add(new GateRule
        {
            ConditionType = GateConditionType.HasToolType,
            ConditionValue = "iron_axe",
            OutputDirection = Direction.North
        });

        var villager = new VillagerLogic(EntityId.Next()) { EquippedToolId = null };
        Assert.Equal(Direction.East, splitter.EvaluateVillagerRoute(villager));
    }

    [Fact]
    public void EvaluateVillagerRoute_MultiRule_FirstMatchWins()
    {
        var splitter = new FilterSplitterLogic(EntityId.Next());
        splitter.DefaultDirection = Direction.East;
        splitter.Rules.Add(new GateRule
        {
            ConditionType = GateConditionType.HasProfession,
            ConditionValue = "Lumberjack",
            OutputDirection = Direction.North
        });
        splitter.Rules.Add(new GateRule
        {
            ConditionType = GateConditionType.HasJobClass,
            ConditionValue = "Warrior",
            OutputDirection = Direction.South
        });

        // Matches both rules — first rule wins
        var villager = new VillagerLogic(EntityId.Next())
        {
            Profession = VillagerJob.Lumberjack,
            TrainedClass = VillagerClass.Warrior
        };
        Assert.Equal(Direction.North, splitter.EvaluateVillagerRoute(villager));
    }

    [Fact]
    public void EvaluateVillagerRoute_MultiRule_UnsupportedConditionType_NoMatch()
    {
        var splitter = new FilterSplitterLogic(EntityId.Next());
        splitter.DefaultDirection = Direction.East;
        splitter.Rules.Add(new GateRule
        {
            ConditionType = GateConditionType.MinLevel, // Not applicable to villagers
            ConditionValue = "1",
            OutputDirection = Direction.North
        });

        var villager = new VillagerLogic(EntityId.Next());
        Assert.Equal(Direction.East, splitter.EvaluateVillagerRoute(villager));
    }

    // ── InitializeFromProto ─────────────────────────────────────────

    [Fact]
    public void InitializeFromProto_SetsFilteredItemIdFromProto()
    {
        var proto = new FilterSplitterProto
        {
            Id = "filter_splitter_basic",
            DefaultFilteredItemId = "sticks",
            DefaultFilteredOutputDirection = Direction.North
        };

        var splitter = new FilterSplitterLogic(EntityId.Next());
        splitter.InitializeFromProto(proto);

        Assert.Equal("sticks", splitter.FilteredItemId);
        Assert.Equal(Direction.North, splitter.FilteredOutputDirection);
        Assert.Equal("filter_splitter_basic", splitter.ProtoId);
    }

    [Fact]
    public void InitializeFromProto_NullFilter_LeavesFilterNull()
    {
        var proto = new FilterSplitterProto
        {
            Id = "filter_splitter_basic",
            DefaultFilteredItemId = null
        };

        var splitter = new FilterSplitterLogic(EntityId.Next());
        splitter.InitializeFromProto(proto);

        Assert.Null(splitter.FilteredItemId);
    }

    [Fact]
    public void InitializeFromProto_SetsBaseFields()
    {
        var proto = new FilterSplitterProto
        {
            Id = "filter_splitter_advanced",
            ProcessingDuration = 0.5f,
            Tier = 3
        };

        var splitter = new FilterSplitterLogic(EntityId.Next());
        splitter.InitializeFromProto(proto);

        Assert.Equal(0.5f, splitter.ProcessingDuration);
        // Tier is a structure-only property; routing entities don't carry it
    }

    // ── GateRule: HeroEntity CarryingItem/HasItem/HasToolType ────────

    [Fact]
    public void GateRule_HasItem_MatchesEquippedItem()
    {
        var rule = new GateRule
        {
            ConditionType = GateConditionType.HasItem,
            ConditionValue = "iron_sword"
        };

        var hero = new HeroEntity(EntityId.Next());
        hero.Equip(new EquippedItem { ProtoId = "iron_sword", Slot = EquipSlot.Weapon });

        Assert.True(rule.Matches(hero));
    }

    [Fact]
    public void GateRule_HasItem_NoMatchWhenNotEquipped()
    {
        var rule = new GateRule
        {
            ConditionType = GateConditionType.HasItem,
            ConditionValue = "iron_sword"
        };

        var hero = new HeroEntity(EntityId.Next());
        Assert.False(rule.Matches(hero));
    }

    [Fact]
    public void GateRule_HasItem_CaseInsensitive()
    {
        var rule = new GateRule
        {
            ConditionType = GateConditionType.HasItem,
            ConditionValue = "Iron_Sword"
        };

        var hero = new HeroEntity(EntityId.Next());
        hero.Equip(new EquippedItem { ProtoId = "iron_sword", Slot = EquipSlot.Weapon });

        Assert.True(rule.Matches(hero));
    }

    [Fact]
    public void GateRule_CarryingItem_MatchesWhenHasLoad()
    {
        var rule = new GateRule
        {
            ConditionType = GateConditionType.CarryingItem,
            ConditionValue = "anything"
        };

        var hero = new HeroEntity(EntityId.Next()) { CarryLoad = 10f };
        Assert.True(rule.Matches(hero));
    }

    [Fact]
    public void GateRule_CarryingItem_NoMatchWhenEmpty()
    {
        var rule = new GateRule
        {
            ConditionType = GateConditionType.CarryingItem,
            ConditionValue = "anything"
        };

        var hero = new HeroEntity(EntityId.Next()) { CarryLoad = 0f };
        Assert.False(rule.Matches(hero));
    }

    [Fact]
    public void GateRule_HasToolType_MatchesEquippedWeapon()
    {
        var rule = new GateRule
        {
            ConditionType = GateConditionType.HasToolType,
            ConditionValue = "iron_axe"
        };

        var hero = new HeroEntity(EntityId.Next());
        hero.Equip(new EquippedItem { ProtoId = "iron_axe", Slot = EquipSlot.Weapon });

        Assert.True(rule.Matches(hero));
    }

    [Fact]
    public void GateRule_HasToolType_NoMatchWhenNoWeapon()
    {
        var rule = new GateRule
        {
            ConditionType = GateConditionType.HasToolType,
            ConditionValue = "iron_axe"
        };

        var hero = new HeroEntity(EntityId.Next());
        Assert.False(rule.Matches(hero));
    }

    [Fact]
    public void GateRule_HasToolType_NoMatchWhenDifferentWeapon()
    {
        var rule = new GateRule
        {
            ConditionType = GateConditionType.HasToolType,
            ConditionValue = "iron_axe"
        };

        var hero = new HeroEntity(EntityId.Next());
        hero.Equip(new EquippedItem { ProtoId = "steel_sword", Slot = EquipSlot.Weapon });

        Assert.False(rule.Matches(hero));
    }

    // ── StructureManager Filter Splitter API ──────────────────────────

    private static GameBootstrapper CreateBootstrapper()
    {
        var bootstrapper = new GameBootstrapper();
        bootstrapper.Bootstrap();
        return bootstrapper;
    }

    private static (GameBootstrapper boot, SimulationTicker sim) CreateNewGame(
        Difficulty difficulty = Difficulty.Normal)
    {
        EntityBase.ResetIdCounter();
        var boot = CreateBootstrapper();
        var settings = new NewGameSettings
        {
            GameName = "TestGame",
            Difficulty = difficulty,
            Seed = 42,
            GuildName = "TestGuild"
        };
        boot.ApplyNewGameSettings(settings);
        return (boot, boot.Services.Get<SimulationTicker>());
    }

    [Fact]
    public void StructureManager_SetFilterSplitterItem_ReturnsTrueForValidSplitter()
    {
        var (_, sim) = CreateNewGame();

        var splitter = new FilterSplitterLogic(EntityId.Next());
        splitter.InitializeFromProto(new FilterSplitterProto { Id = "filter_splitter_basic" });
        sim.StructureManager.AddProtoStructure(splitter, new GridPosRPG(5, 5));

        bool result = sim.StructureManager.SetFilterSplitterItem(splitter.Id, "sticks");
        Assert.True(result);
        Assert.Equal("sticks", splitter.FilteredItemId);
    }

    [Fact]
    public void StructureManager_SetFilterSplitterItem_ReturnsFalseForInvalidId()
    {
        var (_, sim) = CreateNewGame();

        bool result = sim.StructureManager.SetFilterSplitterItem(999999, "sticks");
        Assert.False(result);
    }

    [Fact]
    public void StructureManager_SetFilterSplitterItem_ReturnsFalseForNonSplitter()
    {
        var (_, sim) = CreateNewGame();

        var stockpile = new StockpileLogic(EntityId.Next());
        sim.StructureManager.AddProtoStructure(stockpile, new GridPosRPG(5, 5));

        bool result = sim.StructureManager.SetFilterSplitterItem(stockpile.Id, "sticks");
        Assert.False(result);
    }

    [Fact]
    public void StructureManager_ClearFilterSplitterItem_ClearsFilter()
    {
        var (_, sim) = CreateNewGame();

        var splitter = new FilterSplitterLogic(EntityId.Next());
        splitter.InitializeFromProto(new FilterSplitterProto { Id = "filter_splitter_basic" });
        sim.StructureManager.AddProtoStructure(splitter, new GridPosRPG(5, 5));

        sim.StructureManager.SetFilterSplitterItem(splitter.Id, "sticks");
        bool result = sim.StructureManager.ClearFilterSplitterItem(splitter.Id);
        Assert.True(result);
        Assert.Null(splitter.FilteredItemId);
    }

    // ── FilterSplitterFilterChangedEvent Publishing ─────────────────

    [Fact]
    public void StructureManager_Tick_PublishesFilterChangedEvent()
    {
        var (boot, sim) = CreateNewGame();

        var splitter = new FilterSplitterLogic(EntityId.Next());
        splitter.InitializeFromProto(new FilterSplitterProto { Id = "filter_splitter_basic" });
        sim.StructureManager.AddProtoStructure(splitter, new GridPosRPG(8, 8));

        FilterSplitterFilterChangedEvent? received = null;
        boot.Services.Get<EventBus>().Subscribe<FilterSplitterFilterChangedEvent>(e => received = e);

        // Set the filter — pending change flag is set
        sim.StructureManager.SetFilterSplitterItem(splitter.Id, "ore");
        Assert.True(splitter.PendingFilterChange);

        // Tick should publish the event and clear the pending flag
        sim.StructureManager.Tick(0.1f);
        Assert.NotNull(received);
        Assert.Equal(splitter.Id, received.Value.SplitterId);
        Assert.False(splitter.PendingFilterChange);
    }

    [Fact]
    public void StructureManager_Tick_NoPendingChange_NoEvent()
    {
        var (boot, sim) = CreateNewGame();

        var splitter = new FilterSplitterLogic(EntityId.Next());
        splitter.InitializeFromProto(new FilterSplitterProto { Id = "filter_splitter_basic" });
        sim.StructureManager.AddProtoStructure(splitter, new GridPosRPG(8, 8));

        bool eventFired = false;
        boot.Services.Get<EventBus>().Subscribe<FilterSplitterFilterChangedEvent>(_ => eventFired = true);

        // No pending change — tick should not publish
        sim.StructureManager.Tick(0.1f);
        Assert.False(eventFired);
    }

    // ── FilterSplitterProto Defaults ────────────────────────────────

    [Fact]
    public void FilterSplitterProto_DefaultFilteredItemId_DefaultsToNull()
    {
        var proto = new FilterSplitterProto();
        Assert.Null(proto.DefaultFilteredItemId);
    }

    [Fact]
    public void FilterSplitterProto_DefaultFilteredOutputDirection_DefaultsToWest()
    {
        var proto = new FilterSplitterProto();
        Assert.Equal(Direction.West, proto.DefaultFilteredOutputDirection);
    }

    // ── Edge Cases ──────────────────────────────────────────────────

    [Fact]
    public void EvaluateVillagerRoute_MultipleItemsInInventory_MatchesAny()
    {
        var splitter = new FilterSplitterLogic(EntityId.Next());
        splitter.FilteredOutputDirection = Direction.North;
        splitter.SetFilteredItem("ore");

        var villager = new VillagerLogic(EntityId.Next());
        villager.TryPickUpItem(new ItemInstance { ProtoId = "sticks" });
        villager.TryPickUpItem(new ItemInstance { ProtoId = "ore" });
        villager.TryPickUpItem(new ItemInstance { ProtoId = "logs" });

        Assert.Equal(Direction.North, splitter.EvaluateVillagerRoute(villager));
    }

    [Fact]
    public void SetFilteredItem_Null_ClearsFilter()
    {
        var splitter = new FilterSplitterLogic(EntityId.Next());
        splitter.SetFilteredItem("sticks");
        splitter.PendingFilterChange = false;
        splitter.SetFilteredItem(null);
        Assert.Null(splitter.FilteredItemId);
        Assert.True(splitter.PendingFilterChange);
    }

    [Fact]
    public void ProcessingDuration_DefaultsTo01()
    {
        var splitter = new FilterSplitterLogic(EntityId.Next());
        Assert.Equal(0.1f, splitter.ProcessingDuration);
    }

    [Fact]
    public void Tick_WhenInactive_DoesNothing()
    {
        var splitter = new FilterSplitterLogic(EntityId.Next());
        splitter.IsActive = false;
        splitter.Tick(1.0f); // should not throw
    }
}

/// <summary>Tests for CheckGateLogic routing behavior and configuration.</summary>
public class CheckGateTests
{
    [Fact]
    public void CheckGate_RoutesMatchingWorkerToConfiguredDirection()
    {
        var gate = new CheckGateLogic(EntityId.Next());
        gate.Rule = new GateRule
        {
            ConditionType = GateConditionType.MinLevel,
            ConditionValue = "3",
            OutputDirection = Direction.North
        };
        gate.DefaultDirection = Direction.East;

        var highLevel = new HeroEntity(EntityId.Next()) { Level = 5 };
        var lowLevel = new HeroEntity(EntityId.Next()) { Level = 2 };

        Assert.Equal(Direction.North, gate.EvaluateRoute(highLevel));
        Assert.Equal(Direction.East, gate.EvaluateRoute(lowLevel));
    }

    [Fact]
    public void CheckGate_HasTrait_MatchesCorrectly()
    {
        var gate = new CheckGateLogic(EntityId.Next());
        gate.Rule = new GateRule
        {
            ConditionType = GateConditionType.HasTrait,
            ConditionValue = "brave",
            OutputDirection = Direction.South
        };
        gate.DefaultDirection = Direction.East;

        var withTrait = new HeroEntity(EntityId.Next());
        withTrait.Traits.Add("brave");

        var withoutTrait = new HeroEntity(EntityId.Next());

        Assert.Equal(Direction.South, gate.EvaluateRoute(withTrait));
        Assert.Equal(Direction.East, gate.EvaluateRoute(withoutTrait));
    }

    [Fact]
    public void CheckGate_HasAbility_MatchesCorrectly()
    {
        var gate = new CheckGateLogic(EntityId.Next());
        gate.Rule = new GateRule
        {
            ConditionType = GateConditionType.HasAbility,
            ConditionValue = "extra_attack",
            OutputDirection = Direction.West
        };
        gate.DefaultDirection = Direction.East;

        var withAbility = new HeroEntity(EntityId.Next()) { Profession = WorkerProfession.Warrior };
        withAbility.GainAbility(new WorkerAbility { Id = "extra_attack" });

        var without = new HeroEntity(EntityId.Next());

        Assert.Equal(Direction.West, gate.EvaluateRoute(withAbility));
        Assert.Equal(Direction.East, gate.EvaluateRoute(without));
    }

    [Fact]
    public void CheckGate_HasProfession_MatchesCorrectly()
    {
        var gate = new CheckGateLogic(EntityId.Next());
        gate.Rule = new GateRule
        {
            ConditionType = GateConditionType.HasProfession,
            ConditionValue = "Warrior",
            OutputDirection = Direction.North
        };
        gate.DefaultDirection = Direction.East;

        var warrior = new HeroEntity(EntityId.Next()) { Profession = WorkerProfession.Warrior };
        var miner = new HeroEntity(EntityId.Next()) { Profession = WorkerProfession.Miner };

        Assert.Equal(Direction.North, gate.EvaluateRoute(warrior));
        Assert.Equal(Direction.East, gate.EvaluateRoute(miner));
    }

    [Fact]
    public void CheckGate_ConfigurableViaPanel()
    {
        var gate = new CheckGateLogic(EntityId.Next());
        gate.Rule.ConditionType = GateConditionType.MinLevel;
        gate.Rule.ConditionValue = "5";
        gate.Rule.OutputDirection = Direction.South;
        gate.DefaultDirection = Direction.East;

        var worker = new HeroEntity(EntityId.Next()) { Level = 6 };
        var direction = gate.EvaluateRoute(worker);

        Assert.Equal(Direction.South, direction);
    }

    [Fact]
    public void CheckGate_DefaultDirection_WhenRuleDoesNotMatch()
    {
        var gate = new CheckGateLogic(EntityId.Next());
        gate.Rule.ConditionType = GateConditionType.MinLevel;
        gate.Rule.ConditionValue = "10";
        gate.Rule.OutputDirection = Direction.South;
        gate.DefaultDirection = Direction.East;

        var worker = new HeroEntity(EntityId.Next()) { Level = 3 };
        var direction = gate.EvaluateRoute(worker);

        Assert.Equal(Direction.East, direction);
    }
}

/// <summary>Tests for BalancerLogic round-robin output distribution.</summary>
public class BalancerTests
{
    [Fact]
    public void Balancer_DistributesRoundRobin()
    {
        var balancer = new BalancerLogic(EntityId.Next());
        balancer.OutputDirections.Clear();
        balancer.OutputDirections.Add(Direction.East);
        balancer.OutputDirections.Add(Direction.South);
        balancer.OutputDirections.Add(Direction.West);

        Assert.Equal(Direction.East, balancer.GetNextOutput());
        Assert.Equal(Direction.South, balancer.GetNextOutput());
        Assert.Equal(Direction.West, balancer.GetNextOutput());
        Assert.Equal(Direction.East, balancer.GetNextOutput());
    }

    [Fact]
    public void Balancer_ResetCounter_RestartsFromZero()
    {
        var balancer = new BalancerLogic(EntityId.Next());
        balancer.GetNextOutput();
        balancer.GetNextOutput();
        balancer.ResetCounter();

        Assert.Equal(Direction.East, balancer.GetNextOutput());
    }

    [Fact]
    public void Balancer_RoundRobin_EvenDistribution()
    {
        var balancer = new BalancerLogic(EntityId.Next());
        balancer.OutputDirections.Clear();
        balancer.OutputDirections.Add(Direction.North);
        balancer.OutputDirections.Add(Direction.South);
        balancer.OutputDirections.Add(Direction.East);
        balancer.ResetCounter();

        Assert.Equal(Direction.North, balancer.GetNextOutput());
        Assert.Equal(Direction.South, balancer.GetNextOutput());
        Assert.Equal(Direction.East, balancer.GetNextOutput());
        Assert.Equal(Direction.North, balancer.GetNextOutput());
    }

    [Fact]
    public void Balancer_AddRemoveOutputDirections()
    {
        var balancer = new BalancerLogic(EntityId.Next());
        balancer.OutputDirections.Clear();
        Assert.Empty(balancer.OutputDirections);

        balancer.OutputDirections.Add(Direction.West);
        Assert.Single(balancer.OutputDirections);
        Assert.Contains(Direction.West, balancer.OutputDirections);

        balancer.OutputDirections.Remove(Direction.West);
        Assert.Empty(balancer.OutputDirections);

        Assert.Equal(Direction.East, balancer.GetNextOutput());
    }
}

/// <summary>Tests for GateRule condition matching.</summary>
public class GateRuleTests
{
    [Fact]
    public void GateRule_MaxLevel_MatchesCorrectly()
    {
        var rule = new GateRule
        {
            ConditionType = GateConditionType.MaxLevel,
            ConditionValue = "3"
        };

        var low = new HeroEntity(EntityId.Next()) { Level = 2 };
        var high = new HeroEntity(EntityId.Next()) { Level = 5 };

        Assert.True(rule.Matches(low));
        Assert.False(rule.Matches(high));
    }

    [Fact]
    public void GateRule_HasJobClass_MatchesCorrectly()
    {
        var rule = new GateRule
        {
            ConditionType = GateConditionType.HasJobClass,
            ConditionValue = "warrior_class"
        };

        var matching = new HeroEntity(EntityId.Next()) { ClassId = "warrior_class" };
        var nonMatching = new HeroEntity(EntityId.Next()) { ClassId = "mage_class" };

        Assert.True(rule.Matches(matching));
        Assert.False(rule.Matches(nonMatching));
    }

    [Fact]
    public void GateRule_HasProfession_MatchesCorrectly()
    {
        var rule = new GateRule
        {
            ConditionType = GateConditionType.HasProfession,
            ConditionValue = "Warrior",
            OutputDirection = Direction.South
        };

        var warrior = new HeroEntity(EntityId.Next()) { Profession = WorkerProfession.Warrior };
        var forester = new HeroEntity(EntityId.Next()) { Profession = WorkerProfession.Forester };

        Assert.True(rule.Matches(warrior));
        Assert.False(rule.Matches(forester));
    }

    [Fact]
    public void GateRule_HasAbility_MatchesCorrectly()
    {
        var rule = new GateRule
        {
            ConditionType = GateConditionType.HasAbility,
            ConditionValue = "power_slash"
        };

        var heroWith = new HeroEntity(EntityId.Next()) { Profession = WorkerProfession.Warrior };
        heroWith.GainAbility(new WorkerAbility { Id = "power_slash", DisplayName = "Power Slash" });

        var heroWithout = new HeroEntity(EntityId.Next()) { Profession = WorkerProfession.Warrior };

        Assert.True(rule.Matches(heroWith));
        Assert.False(rule.Matches(heroWithout));
    }

    [Fact]
    public void GateRule_HasTrait_MatchesCorrectly()
    {
        var rule = new GateRule
        {
            ConditionType = GateConditionType.HasTrait,
            ConditionValue = "Brave"
        };

        var heroWith = new HeroEntity(EntityId.Next());
        heroWith.Traits.Add("Brave");

        var heroWithout = new HeroEntity(EntityId.Next());

        Assert.True(rule.Matches(heroWith));
        Assert.False(rule.Matches(heroWithout));
    }

    [Fact]
    public void GateRule_HasJobClass_MatchesCorrectly_Phase11()
    {
        var rule = new GateRule
        {
            ConditionType = GateConditionType.HasJobClass,
            ConditionValue = "warrior"
        };

        var warrior = new HeroEntity(EntityId.Next()) { ClassId = "warrior" };
        var mage = new HeroEntity(EntityId.Next()) { ClassId = "mage" };

        Assert.True(rule.Matches(warrior));
        Assert.False(rule.Matches(mage));
    }
}
