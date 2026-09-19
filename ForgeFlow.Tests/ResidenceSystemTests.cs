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
/// Comprehensive tests for the Residence / Home system.
/// Covers: HomeId assignment, MaxOccupants capacity, auto-assignment on spawn,
/// rest-at-home mechanic, inventory-ignore rule, rest rate comparison vs Inn,
/// resident-only enforcement, and event publishing.
/// </summary>
public class ResidenceSystemTests
{
    private static GameBootstrapper CreateBootstrapper()
    {
        var bootstrapper = new GameBootstrapper();
        bootstrapper.Bootstrap();
        return bootstrapper;
    }

    private static (GameBootstrapper boot, SimulationTicker sim) CreateNewGame(
        Difficulty difficulty = Difficulty.Normal)
    {
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

    // ── VillagerLogic.HomeId ─────────────────────────────────────────

    [Fact]
    public void VillagerLogic_HomeId_DefaultsToNull()
    {
        var villager = new VillagerLogic(EntityId.Next());
        Assert.Null(villager.HomeId);
    }

    [Fact]
    public void VillagerLogic_HomeId_CanBeSet()
    {
        var villager = new VillagerLogic(EntityId.Next());
        villager.HomeId = 42;
        Assert.Equal((ulong)42, villager.HomeId);
    }

    [Fact]
    public void VillagerLogic_Reset_ClearsHomeId()
    {
        var villager = new VillagerLogic(EntityId.Next());
        villager.HomeId = 99;
        villager.Reset();
        Assert.Null(villager.HomeId);
    }

    // ── VillageSpawnerProto ──────────────────────────────────────────

    [Fact]
    public void VillageSpawnerProto_HasMaxOccupants_Default4()
    {
        var proto = new VillageSpawnerProto();
        Assert.Equal(4, proto.MaxOccupants);
    }

    [Fact]
    public void VillageSpawnerProto_HasHomeRestDuration_Default3()
    {
        var proto = new VillageSpawnerProto();
        Assert.Equal(3.0f, proto.HomeRestDuration);
    }

    // ── VillageSpawnerLogic: Residence Properties ────────────────────

    [Fact]
    public void SpawnerLogic_InitializeFromProto_SetsMaxOccupants()
    {
        var proto = new VillageSpawnerProto { MaxOccupants = 6 };
        var logic = new VillageSpawnerLogic(EntityId.Next());
        logic.InitializeFromProto(proto);
        Assert.Equal(6, logic.MaxOccupants);
    }

    [Fact]
    public void SpawnerLogic_InitializeFromProto_SetsHomeRestDuration()
    {
        var proto = new VillageSpawnerProto { HomeRestDuration = 2.5f };
        var logic = new VillageSpawnerLogic(EntityId.Next());
        logic.InitializeFromProto(proto);
        Assert.Equal(2.5f, logic.HomeRestDuration);
    }

    [Fact]
    public void SpawnerLogic_Residents_StartsEmpty()
    {
        var spawner = new VillageSpawnerLogic(EntityId.Next());
        Assert.Empty(spawner.Residents);
    }

    [Fact]
    public void SpawnerLogic_CurrentOccupants_StartsEmpty()
    {
        var spawner = new VillageSpawnerLogic(EntityId.Next());
        Assert.Empty(spawner.CurrentOccupants);
    }

    // ── Resident Assignment ──────────────────────────────────────────

    [Fact]
    public void SpawnerLogic_AssignResident_AddsToResidentsList()
    {
        var spawner = new VillageSpawnerLogic(EntityId.Next()) { MaxOccupants = 4 };
        bool result = spawner.AssignResident(100);
        Assert.True(result);
        Assert.Single(spawner.Residents);
        Assert.Contains((ulong)100, spawner.Residents);
    }

    [Fact]
    public void SpawnerLogic_AssignResident_RejectWhenFull()
    {
        var spawner = new VillageSpawnerLogic(EntityId.Next()) { MaxOccupants = 1 };
        spawner.AssignResident(100);
        bool result = spawner.AssignResident(200);
        Assert.False(result);
        Assert.Single(spawner.Residents);
    }

    [Fact]
    public void SpawnerLogic_AssignResident_DuplicateReturnsTrueNoDuplicate()
    {
        var spawner = new VillageSpawnerLogic(EntityId.Next()) { MaxOccupants = 4 };
        spawner.AssignResident(100);
        bool result = spawner.AssignResident(100);
        Assert.True(result);
        Assert.Single(spawner.Residents);
    }

    [Fact]
    public void SpawnerLogic_RemoveResident_RemovesFromList()
    {
        var spawner = new VillageSpawnerLogic(EntityId.Next()) { MaxOccupants = 4 };
        spawner.AssignResident(100);
        bool result = spawner.RemoveResident(100);
        Assert.True(result);
        Assert.Empty(spawner.Residents);
    }

    [Fact]
    public void SpawnerLogic_RemoveResident_NonexistentReturnsFalse()
    {
        var spawner = new VillageSpawnerLogic(EntityId.Next()) { MaxOccupants = 4 };
        bool result = spawner.RemoveResident(999);
        Assert.False(result);
    }

    [Fact]
    public void SpawnerLogic_CanAssignResident_TrueWhenBelowCapacity()
    {
        var spawner = new VillageSpawnerLogic(EntityId.Next()) { MaxOccupants = 2 };
        Assert.True(spawner.CanAssignResident);
        spawner.AssignResident(100);
        Assert.True(spawner.CanAssignResident);
    }

    [Fact]
    public void SpawnerLogic_CanAssignResident_FalseWhenAtCapacity()
    {
        var spawner = new VillageSpawnerLogic(EntityId.Next()) { MaxOccupants = 1 };
        spawner.AssignResident(100);
        Assert.False(spawner.CanAssignResident);
    }

    // ── Rest Acceptance (ignores inventory) ──────────────────────────

    [Fact]
    public void SpawnerLogic_AcceptVillagerForRest_AcceptsAssignedResident()
    {
        var spawner = new VillageSpawnerLogic(EntityId.Next()) { MaxOccupants = 4 };
        var villager = new VillagerLogic(EntityId.Next());
        spawner.AssignResident(villager.Id);

        bool accepted = spawner.AcceptVillagerForRest(villager);
        Assert.True(accepted);
        Assert.Single(spawner.CurrentOccupants);
    }

    [Fact]
    public void SpawnerLogic_AcceptVillagerForRest_RejectsNonResident()
    {
        var spawner = new VillageSpawnerLogic(EntityId.Next()) { MaxOccupants = 4 };
        var villager = new VillagerLogic(EntityId.Next());
        // Not assigned as resident

        bool accepted = spawner.AcceptVillagerForRest(villager);
        Assert.False(accepted);
        Assert.Empty(spawner.CurrentOccupants);
    }

    [Fact]
    public void SpawnerLogic_AcceptVillagerForRest_AcceptsEvenWithItems()
    {
        var spawner = new VillageSpawnerLogic(EntityId.Next()) { MaxOccupants = 4 };
        var villager = new VillagerLogic(EntityId.Next());
        spawner.AssignResident(villager.Id);

        // Give the villager items — homes should NOT reject carrying villagers
        villager.TryPickUpItem(new ItemInstance { ProtoId = "sticks", Quantity = 5 });
        Assert.True(villager.IsCarryingItems);

        bool accepted = spawner.AcceptVillagerForRest(villager);
        Assert.True(accepted);
    }

    [Fact]
    public void SpawnerLogic_AcceptVillagerForRest_RejectsWhenFull()
    {
        var spawner = new VillageSpawnerLogic(EntityId.Next()) { MaxOccupants = 1 };
        var v1 = new VillagerLogic(EntityId.Next());
        var v2 = new VillagerLogic(EntityId.Next());
        spawner.AssignResident(v1.Id);
        spawner.AssignResident(v2.Id); // Will fail because MaxOccupants=1

        spawner.AcceptVillagerForRest(v1);
        // v2 is not a resident, so AcceptVillagerForRest will reject
        bool accepted = spawner.AcceptVillagerForRest(v2);
        Assert.False(accepted);
    }

    // ── Rest Mechanic ────────────────────────────────────────────────

    [Fact]
    public void SpawnerLogic_RestVillager_RestoresStamina()
    {
        var spawner = new VillageSpawnerLogic(EntityId.Next()) { HomeRestDuration = 3.0f };
        var villager = new VillagerLogic(EntityId.Next())
        {
            Stamina = 0f,
            MaxStamina = 100f
        };

        spawner.RestVillager(villager, 1.0f);
        Assert.True(villager.Stamina > 0f);
    }

    [Fact]
    public void SpawnerLogic_RestVillager_FullyRests_ReturnsTrueWhenComplete()
    {
        var spawner = new VillageSpawnerLogic(EntityId.Next()) { HomeRestDuration = 3.0f };
        var villager = new VillagerLogic(EntityId.Next())
        {
            Stamina = 0f,
            MaxStamina = 100f
        };

        // Rest for enough time to fully recover (100 / (100/3) = 3 seconds)
        bool done = spawner.RestVillager(villager, 3.0f);
        Assert.True(done);
        Assert.Equal(100f, villager.Stamina);
    }

    [Fact]
    public void SpawnerLogic_RestVillager_PartialRest_ReturnsFalse()
    {
        var spawner = new VillageSpawnerLogic(EntityId.Next()) { HomeRestDuration = 3.0f };
        var villager = new VillagerLogic(EntityId.Next())
        {
            Stamina = 0f,
            MaxStamina = 100f
        };

        bool done = spawner.RestVillager(villager, 1.0f);
        Assert.False(done);
        Assert.True(villager.Stamina > 0f);
        Assert.True(villager.Stamina < villager.MaxStamina);
    }

    [Fact]
    public void SpawnerLogic_RestVillager_RepairsEquippedTool()
    {
        var spawner = new VillageSpawnerLogic(EntityId.Next()) { HomeRestDuration = 3.0f };
        var villager = new VillagerLogic(EntityId.Next())
        {
            Stamina = 0f,
            MaxStamina = 100f
        };
        villager.EquipTool("axe", 50f);
        villager.EquippedToolDurability = 10f;

        spawner.RestVillager(villager, 3.0f);
        Assert.Equal(50f, villager.EquippedToolDurability);
    }

    [Fact]
    public void SpawnerLogic_RestVillager_DoesNotExceedMaxStamina()
    {
        var spawner = new VillageSpawnerLogic(EntityId.Next()) { HomeRestDuration = 1.0f };
        var villager = new VillagerLogic(EntityId.Next())
        {
            Stamina = 90f,
            MaxStamina = 100f
        };

        spawner.RestVillager(villager, 5.0f); // Way more than needed
        Assert.Equal(100f, villager.Stamina);
    }

    // ── Home Rest Rate vs Inn Rest Rate ──────────────────────────────

    [Fact]
    public void HomeRestRate_IsFasterThanInn()
    {
        // Home default: 3.0s, Inn default: 5.0s
        var home = new VillageSpawnerLogic(EntityId.Next()) { HomeRestDuration = 3.0f };
        var inn = new InnLogic(EntityId.Next());
        inn.SetRestRecipe("rest_basic", 5.0f);

        var villagerHome = new VillagerLogic(EntityId.Next()) { Stamina = 0f, MaxStamina = 100f };
        var villagerInn = new VillagerLogic(EntityId.Next()) { Stamina = 0f, MaxStamina = 100f };

        // Rest for 1 second each
        home.RestVillager(villagerHome, 1.0f);
        inn.RestVillager(villagerInn, 1.0f);

        // Home should restore more stamina per second
        Assert.True(villagerHome.Stamina > villagerInn.Stamina,
            $"Home stamina ({villagerHome.Stamina}) should be higher than Inn ({villagerInn.Stamina})");
    }

    // ── Inn Rejects Items, Home Doesn't ──────────────────────────────

    [Fact]
    public void Inn_RejectsVillagerWithItems()
    {
        var inn = new InnLogic(EntityId.Next()) { MaxOccupants = 4 };
        var villager = new VillagerLogic(EntityId.Next());
        villager.TryPickUpItem(new ItemInstance { ProtoId = "sticks", Quantity = 3 });

        bool accepted = inn.AcceptVillager(villager);
        Assert.False(accepted);
    }

    [Fact]
    public void Home_AcceptsVillagerWithItems()
    {
        var spawner = new VillageSpawnerLogic(EntityId.Next()) { MaxOccupants = 4 };
        var villager = new VillagerLogic(EntityId.Next());
        spawner.AssignResident(villager.Id);
        villager.TryPickUpItem(new ItemInstance { ProtoId = "sticks", Quantity = 3 });

        bool accepted = spawner.AcceptVillagerForRest(villager);
        Assert.True(accepted);
    }

    // ── Spawn Integration: HomeId Auto-Assigned ──────────────────────

    [Fact]
    public void StructureManager_SpawnedVillager_HasHomeIdSet()
    {
        EntityBase.ResetIdCounter();
        var (boot, sim) = CreateNewGame();
        var spawner = new VillageSpawnerLogic(EntityId.Next())
        {
            SpawnInterval = 1.0f,
            MaxVillagers = 5,
            MaxOccupants = 4
        };
        sim.StructureManager.AddProtoStructure(spawner, new GridPosRPG(5, 5));

        // Place exit gate and path for the spawner
        var gate = new PathGateLogic(EntityId.Next());
        gate.Position = new GridPosRPG(6, 5);
        gate.Facing = Direction.East;
        gate.LinkToStructure(spawner.Id, spawner.Position);
        sim.EntityManager.AddPathGate(gate);

        var seg = new PathSegmentLogic(EntityId.Next());
        seg.Position = new GridPosRPG(7, 5);
        sim.EntityManager.AddPathSegment(seg);

        // Manually spawn a villager
        var villager = spawner.SpawnVillager();
        Assert.NotNull(villager);

        // Simulate to dispatch
        for (int i = 0; i < 5; i++)
        {
            sim.Update(SimulationTicker.FixedTimeStep);
        }

        // Verify the dispatched villager has HomeId set
        var dispatched = sim.VillagerSystem.Villagers;
        Assert.NotEmpty(dispatched);
        var first = dispatched[0];
        Assert.Equal(spawner.Id, first.HomeId);
        Assert.Equal(spawner.Id, first.OwnerStructureId);
    }

    [Fact]
    public void StructureManager_SpawnedVillager_IsAssignedAsResident()
    {
        EntityBase.ResetIdCounter();
        var (boot, sim) = CreateNewGame();
        var spawner = new VillageSpawnerLogic(EntityId.Next())
        {
            SpawnInterval = 1.0f,
            MaxVillagers = 5,
            MaxOccupants = 4
        };
        sim.StructureManager.AddProtoStructure(spawner, new GridPosRPG(5, 5));

        // Place exit gate and path
        var gate = new PathGateLogic(EntityId.Next());
        gate.Position = new GridPosRPG(6, 5);
        gate.Facing = Direction.East;
        gate.LinkToStructure(spawner.Id, spawner.Position);
        sim.EntityManager.AddPathGate(gate);

        var seg = new PathSegmentLogic(EntityId.Next());
        seg.Position = new GridPosRPG(7, 5);
        sim.EntityManager.AddPathSegment(seg);

        var villager = spawner.SpawnVillager();
        Assert.NotNull(villager);

        for (int i = 0; i < 5; i++)
        {
            sim.Update(SimulationTicker.FixedTimeStep);
        }

        Assert.Contains(sim.VillagerSystem.Villagers[0].Id, spawner.Residents);
    }

    // ── Events ───────────────────────────────────────────────────────

    [Fact]
    public void StructureManager_SpawnedVillager_PublishesAssignedHomeEvent()
    {
        EntityBase.ResetIdCounter();
        var (boot, sim) = CreateNewGame();
        var spawner = new VillageSpawnerLogic(EntityId.Next())
        {
            SpawnInterval = 1.0f,
            MaxVillagers = 5,
            MaxOccupants = 4
        };
        sim.StructureManager.AddProtoStructure(spawner, new GridPosRPG(5, 5));

        var gate = new PathGateLogic(EntityId.Next());
        gate.Position = new GridPosRPG(6, 5);
        gate.Facing = Direction.East;
        gate.LinkToStructure(spawner.Id, spawner.Position);
        sim.EntityManager.AddPathGate(gate);

        var seg = new PathSegmentLogic(EntityId.Next());
        seg.Position = new GridPosRPG(7, 5);
        sim.EntityManager.AddPathSegment(seg);

        VillagerAssignedHomeEvent? capturedEvent = null;
        boot.Services.Get<EventBus>().Subscribe<VillagerAssignedHomeEvent>(e => capturedEvent = e);

        var villager = spawner.SpawnVillager();
        Assert.NotNull(villager);

        for (int i = 0; i < 5; i++)
        {
            sim.Update(SimulationTicker.FixedTimeStep);
        }

        Assert.NotNull(capturedEvent);
        Assert.Equal(spawner.Id, capturedEvent.Value.HomeId);
    }

    // ── CanAcceptOccupant ────────────────────────────────────────────

    [Fact]
    public void SpawnerLogic_CanAcceptOccupant_TrueWhenEmpty()
    {
        var spawner = new VillageSpawnerLogic(EntityId.Next()) { MaxOccupants = 2 };
        Assert.True(spawner.CanAcceptOccupant);
    }

    [Fact]
    public void SpawnerLogic_CanAcceptOccupant_FalseWhenFull()
    {
        var spawner = new VillageSpawnerLogic(EntityId.Next()) { MaxOccupants = 1 };
        var villager = new VillagerLogic(EntityId.Next());
        spawner.AssignResident(villager.Id);
        spawner.AcceptVillagerForRest(villager);
        Assert.False(spawner.CanAcceptOccupant);
    }

    // ── Edge Cases ───────────────────────────────────────────────────

    [Fact]
    public void SpawnerLogic_RestVillager_ZeroDuration_UsesDefault()
    {
        var spawner = new VillageSpawnerLogic(EntityId.Next()) { HomeRestDuration = 0f };
        var villager = new VillagerLogic(EntityId.Next()) { Stamina = 0f, MaxStamina = 100f };

        // Should not crash; uses fallback duration of 3.0
        spawner.RestVillager(villager, 3.0f);
        Assert.Equal(100f, villager.Stamina);
    }

    [Fact]
    public void SpawnerLogic_RestVillager_NoTool_DoesNotCrash()
    {
        var spawner = new VillageSpawnerLogic(EntityId.Next()) { HomeRestDuration = 3.0f };
        var villager = new VillagerLogic(EntityId.Next()) { Stamina = 0f, MaxStamina = 100f };
        // No tool equipped

        var exception = Record.Exception(() => spawner.RestVillager(villager, 1.0f));
        Assert.Null(exception);
    }

    [Fact]
    public void SpawnerLogic_MultipleResidents_IndependentRest()
    {
        var spawner = new VillageSpawnerLogic(EntityId.Next()) { MaxOccupants = 4, HomeRestDuration = 3.0f };
        var v1 = new VillagerLogic(EntityId.Next()) { Stamina = 0f, MaxStamina = 100f };
        var v2 = new VillagerLogic(EntityId.Next()) { Stamina = 50f, MaxStamina = 100f };

        spawner.AssignResident(v1.Id);
        spawner.AssignResident(v2.Id);
        spawner.AcceptVillagerForRest(v1);
        spawner.AcceptVillagerForRest(v2);

        Assert.Equal(2, spawner.CurrentOccupants.Count);

        // Rest both for 1 second
        spawner.RestVillager(v1, 1.0f);
        spawner.RestVillager(v2, 1.0f);

        // v1 started at 0, v2 at 50 — both should have gained ~33.3
        float expectedGain = 100f / 3.0f; // ~33.3
        Assert.True(Math.Abs(v1.Stamina - expectedGain) < 0.1f);
        Assert.True(Math.Abs(v2.Stamina - (50f + expectedGain)) < 0.1f);
    }
}
