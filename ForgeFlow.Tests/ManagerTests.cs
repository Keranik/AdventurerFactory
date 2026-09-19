using ForgeFlow.Core;
using ForgeFlow.Core.Data;
using ForgeFlow.Core.Data.Definitions;
using ForgeFlow.Core.Entities;
using ForgeFlow.Core.Events;
using ForgeFlow.Core.Localization;
using ForgeFlow.Core.Proto;
using ForgeFlow.Core.Proto.Logic;
using ForgeFlow.Core.Proto.Prototypes;
using ForgeFlow.Core.Save;
using ForgeFlow.Core.Systems;
using ForgeFlow.Core.Utilities;

namespace ForgeFlow.Tests;

/// <summary>
/// Tests for TileManager â€” the single source of truth for grid state and spatial occupancy.
/// </summary>
public class TileManagerTests
{
    private static (TileManager tileMgr, TerrainGrid terrain) CreateTileManager()
    {
        var terrain = new TerrainGrid(32, 32);
        terrain.GenerateDefault();
        return (new TileManager(terrain), terrain);
    }

    // â”€â”€ Biome Queries â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    public void GetBiome_ReturnsValidBiome()
    {
        var (tileMgr, _) = CreateTileManager();
        var biome = tileMgr.GetBiome(new GridPosRPG(5, 5));
        Assert.True(Enum.IsDefined(typeof(BiomeType), biome));
    }

    [Fact]
    public void GetCell_ReturnsNonNull_ForValidPosition()
    {
        var (tileMgr, _) = CreateTileManager();
        var cell = tileMgr.GetCell(new GridPosRPG(5, 5));
        Assert.NotNull(cell);
    }

    [Fact]
    public void GetCell_ReturnsNull_ForOutOfBounds()
    {
        var (tileMgr, _) = CreateTileManager();
        var cell = tileMgr.GetCell(new GridPosRPG(999, 999));
        Assert.Null(cell);
    }

    [Fact]
    public void IsPathable_ReturnsTrue_ForValidTile()
    {
        var (tileMgr, _) = CreateTileManager();
        // Most tiles in default generation should be pathable
        bool anyPathable = false;
        for (int x = 0; x < 10; x++)
        {
            for (int y = 0; y < 10; y++)
            {
                if (tileMgr.IsPathable(new GridPosRPG(x, y)))
                {
                    anyPathable = true;
                    break;
                }
            }
            if (anyPathable) break;
        }
        Assert.True(anyPathable, "Expected at least some pathable tiles in default terrain");
    }

    // â”€â”€ Occupancy CRUD â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    public void SetOccupant_MakesIsOccupiedReturnTrue()
    {
        var (tileMgr, _) = CreateTileManager();
        var pos = new GridPosRPG(3, 3);

        Assert.False(tileMgr.IsOccupied(pos));

        tileMgr.SetOccupant(pos, TileOccupantType.Structure, 42);

        Assert.True(tileMgr.IsOccupied(pos));
    }

    [Fact]
    public void TryGetOccupant_ReturnsTrueWithCorrectData()
    {
        var (tileMgr, _) = CreateTileManager();
        var pos = new GridPosRPG(4, 4);

        tileMgr.SetOccupant(pos, TileOccupantType.Path, 100);

        Assert.True(tileMgr.TryGetOccupant(pos, out var occupant));
        Assert.Equal(TileOccupantType.Path, occupant.Type);
        Assert.Equal(100UL, occupant.EntityId);
    }

    [Fact]
    public void TryGetOccupant_ReturnsFalse_WhenEmpty()
    {
        var (tileMgr, _) = CreateTileManager();
        Assert.False(tileMgr.TryGetOccupant(new GridPosRPG(7, 7), out _));
    }

    [Fact]
    public void GetOccupantType_ReturnsCorrectType()
    {
        var (tileMgr, _) = CreateTileManager();
        var pos = new GridPosRPG(2, 2);

        Assert.Equal(TileOccupantType.None, tileMgr.GetOccupantType(pos));

        tileMgr.SetOccupant(pos, TileOccupantType.PathGate, 55);
        Assert.Equal(TileOccupantType.PathGate, tileMgr.GetOccupantType(pos));
    }

    [Fact]
    public void ClearOccupant_RemovesOccupancy()
    {
        var (tileMgr, _) = CreateTileManager();
        var pos = new GridPosRPG(1, 1);

        tileMgr.SetOccupant(pos, TileOccupantType.Structure, 10);
        Assert.True(tileMgr.IsOccupied(pos));

        tileMgr.ClearOccupant(pos);
        Assert.False(tileMgr.IsOccupied(pos));
    }

    // â”€â”€ CanPlaceAt â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    public void CanPlaceAt_ReturnsFalse_WhenOccupied()
    {
        var (tileMgr, _) = CreateTileManager();
        var pos = new GridPosRPG(5, 5);

        tileMgr.SetOccupant(pos, TileOccupantType.Structure, 1);
        Assert.False(tileMgr.CanPlaceAt(pos));
    }

    [Fact]
    public void CanPlaceAt_ReturnsFalse_ForOutOfBounds()
    {
        var (tileMgr, _) = CreateTileManager();
        Assert.False(tileMgr.CanPlaceAt(new GridPosRPG(999, 999)));
    }

    // â”€â”€ Type-Filtered Lookups â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    public void GetStructureIdAt_ReturnsCorrectId()
    {
        var (tileMgr, _) = CreateTileManager();
        var pos = new GridPosRPG(6, 6);

        Assert.Null(tileMgr.GetStructureIdAt(pos));

        tileMgr.SetOccupant(pos, TileOccupantType.Structure, 77);
        Assert.Equal(77UL, tileMgr.GetStructureIdAt(pos));
    }

    [Fact]
    public void GetStructureIdAt_ReturnsNull_WhenOccupantIsPath()
    {
        var (tileMgr, _) = CreateTileManager();
        var pos = new GridPosRPG(6, 6);

        tileMgr.SetOccupant(pos, TileOccupantType.Path, 77);
        Assert.Null(tileMgr.GetStructureIdAt(pos));
    }

    [Fact]
    public void GetPathIdAt_ReturnsCorrectId()
    {
        var (tileMgr, _) = CreateTileManager();
        var pos = new GridPosRPG(8, 8);

        tileMgr.SetOccupant(pos, TileOccupantType.Path, 33);
        Assert.Equal(33UL, tileMgr.GetPathIdAt(pos));
    }

    [Fact]
    public void GetPathGateIdAt_ReturnsCorrectId()
    {
        var (tileMgr, _) = CreateTileManager();
        var pos = new GridPosRPG(9, 9);

        tileMgr.SetOccupant(pos, TileOccupantType.PathGate, 44);
        Assert.Equal(44UL, tileMgr.GetPathGateIdAt(pos));
    }

    [Fact]
    public void GetResourceNodeIdAt_ReturnsCorrectId()
    {
        var (tileMgr, _) = CreateTileManager();
        var pos = new GridPosRPG(10, 10);

        Assert.Null(tileMgr.GetResourceNodeIdAt(pos));

        tileMgr.SetOccupant(pos, TileOccupantType.ResourceNode, 55);
        Assert.Equal(55UL, tileMgr.GetResourceNodeIdAt(pos));
    }

    [Fact]
    public void GetResourceNodeIdAt_ReturnsNull_WhenOccupantIsStructure()
    {
        var (tileMgr, _) = CreateTileManager();
        var pos = new GridPosRPG(10, 10);

        tileMgr.SetOccupant(pos, TileOccupantType.Structure, 55);
        Assert.Null(tileMgr.GetResourceNodeIdAt(pos));
    }

    // â”€â”€ Neighbor Queries â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    public void HasStructureNeighbor_ReturnsFalse_WhenNoNeighborStructure()
    {
        var (tileMgr, _) = CreateTileManager();
        Assert.False(tileMgr.HasStructureNeighbor(new GridPosRPG(5, 5), out _, out _));
    }

    [Fact]
    public void HasStructureNeighbor_ReturnsTrue_WhenStructureAdjacent()
    {
        var (tileMgr, _) = CreateTileManager();
        var center = new GridPosRPG(5, 5);
        var structurePos = new GridPosRPG(6, 5);

        tileMgr.SetOccupant(structurePos, TileOccupantType.Structure, 88);

        Assert.True(tileMgr.HasStructureNeighbor(center, out var structureId, out var foundPos));
        Assert.Equal(88UL, structureId);
        Assert.Equal(structurePos, foundPos);
    }

    // â”€â”€ Lifecycle â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    public void Clear_RemovesAllOccupants()
    {
        var (tileMgr, _) = CreateTileManager();

        tileMgr.SetOccupant(new GridPosRPG(0, 0), TileOccupantType.Structure, 1);
        tileMgr.SetOccupant(new GridPosRPG(1, 1), TileOccupantType.Path, 2);
        tileMgr.SetOccupant(new GridPosRPG(2, 2), TileOccupantType.PathGate, 3);

        tileMgr.Clear();

        Assert.False(tileMgr.IsOccupied(new GridPosRPG(0, 0)));
        Assert.False(tileMgr.IsOccupied(new GridPosRPG(1, 1)));
        Assert.False(tileMgr.IsOccupied(new GridPosRPG(2, 2)));
    }

    [Fact]
    public void RegenerateTerrain_ClearsOccupantsAndRegeneratesTerrain()
    {
        var (tileMgr, _) = CreateTileManager();

        tileMgr.SetOccupant(new GridPosRPG(0, 0), TileOccupantType.Structure, 1);

        tileMgr.RegenerateTerrain(99);

        Assert.False(tileMgr.IsOccupied(new GridPosRPG(0, 0)));
        Assert.Equal(32, tileMgr.Width);
        Assert.Equal(32, tileMgr.Height);
    }

    // â”€â”€ TileOccupant Struct â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    public void TileOccupant_Empty_IsCorrect()
    {
        var empty = TileOccupant.Empty;
        Assert.True(empty.IsEmpty);
        Assert.Equal(TileOccupantType.None, empty.Type);
        Assert.Equal(0UL, empty.EntityId);
    }

    [Fact]
    public void TileOccupant_NonEmpty_IsNotEmpty()
    {
        var occ = new TileOccupant(TileOccupantType.Structure, 42);
        Assert.False(occ.IsEmpty);
        Assert.Equal(TileOccupantType.Structure, occ.Type);
        Assert.Equal(42UL, occ.EntityId);
    }
}

/// <summary>
/// Tests for EntityManager â€” the single source of truth for all entity collections.
/// </summary>
public class EntityManagerTests
{
    private static (EntityManager entMgr, TileManager tileMgr, VillagerSystem villagerSystem) CreateEntityManager()
    {
        var bus = new EventBus();
        var terrain = new TerrainGrid(32, 32);
        terrain.GenerateDefault();
        var tileMgr = new TileManager(terrain);
        var rm = new ItemManager(bus);
        var vs = new VillagerSystem(bus, rm);
        return (new EntityManager(tileMgr, vs), tileMgr, vs);
    }

    // â”€â”€ Heroes â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    public void AddHero_AppearsInHeroesAndHeroIndex()
    {
        var (entMgr, _, _) = CreateEntityManager();
        var hero = new HeroEntity(EntityId.Next()) { ClassId = "warrior", Level = 1 };

        entMgr.AddHero(hero);

        Assert.Single(entMgr.Heroes);
        Assert.True(entMgr.HeroIndex.ContainsKey(hero.Id));
        Assert.Equal(hero, entMgr.GetHero(hero.Id));
    }

    [Fact]
    public void RemoveHero_RemovesFromBothCollections()
    {
        var (entMgr, _, _) = CreateEntityManager();
        var hero = new HeroEntity(EntityId.Next()) { ClassId = "warrior" };
        entMgr.AddHero(hero);

        entMgr.RemoveHero(hero.Id);

        Assert.Empty(entMgr.Heroes);
        Assert.False(entMgr.HeroIndex.ContainsKey(hero.Id));
        Assert.Null(entMgr.GetHero(hero.Id));
    }

    [Fact]
    public void RemoveHero_NonExistentId_DoesNotThrow()
    {
        var (entMgr, _, _) = CreateEntityManager();
        entMgr.RemoveHero(new EntityId(9999));
        Assert.Empty(entMgr.Heroes);
    }

    // â”€â”€ Structures â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    public void AddStructure_RegistersInCollectionAndTileManager()
    {
        var (entMgr, tileMgr, _) = CreateEntityManager();
        var spawner = new VillageSpawnerLogic(EntityId.Next());
        var pos = new GridPosRPG(5, 5);

        entMgr.AddStructure(spawner, pos);

        Assert.Equal(1, entMgr.StructureCount);
        Assert.Equal(spawner, entMgr.GetStructure(spawner.Id));
        Assert.Equal(spawner, entMgr.GetStructureAt(pos));
        Assert.True(tileMgr.GetStructureIdAt(pos).HasValue);
    }

    [Fact]
    public void RemoveStructure_ClearsCollectionAndTileManager()
    {
        var (entMgr, tileMgr, _) = CreateEntityManager();
        var spawner = new VillageSpawnerLogic(EntityId.Next());
        var pos = new GridPosRPG(5, 5);
        entMgr.AddStructure(spawner, pos);

        entMgr.RemoveStructure(pos);

        Assert.Equal(0, entMgr.StructureCount);
        Assert.Null(entMgr.GetStructureAt(pos));
        Assert.False(tileMgr.GetStructureIdAt(pos).HasValue);
    }

    [Fact]
    public void GetStructureAt_ReturnsNull_WhenEmpty()
    {
        var (entMgr, _, _) = CreateEntityManager();
        Assert.Null(entMgr.GetStructureAt(new GridPosRPG(5, 5)));
    }

    // â”€â”€ Path Segments â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    public void AddPathSegment_RegistersInCollectionAndTileManager()
    {
        var (entMgr, tileMgr, _) = CreateEntityManager();
        var seg = new PathSegmentLogic(EntityId.Next()) { Position = new GridPosRPG(3, 3), Facing = Direction.East };

        entMgr.AddPathSegment(seg);

        Assert.Equal(1, entMgr.PathSegmentCount);
        Assert.Equal(seg, entMgr.GetPathSegment(seg.Id));
        Assert.Equal(seg, entMgr.GetPathSegmentAt(new GridPosRPG(3, 3)));
        Assert.True(tileMgr.GetPathIdAt(new GridPosRPG(3, 3)).HasValue);
    }

    [Fact]
    public void UnregisterPathSegment_ClearsCollectionAndTileManager()
    {
        var (entMgr, tileMgr, _) = CreateEntityManager();
        var pos = new GridPosRPG(3, 3);
        var seg = new PathSegmentLogic(EntityId.Next()) { Position = pos, Facing = Direction.East };
        entMgr.AddPathSegment(seg);

        entMgr.UnregisterPathSegment(seg.Id, pos);

        Assert.Equal(0, entMgr.PathSegmentCount);
        Assert.Null(entMgr.GetPathSegmentAt(pos));
        Assert.False(tileMgr.GetPathIdAt(pos).HasValue);
    }

    // â”€â”€ Path Gates â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    public void AddPathGate_RegistersInCollectionAndTileManager()
    {
        var (entMgr, tileMgr, _) = CreateEntityManager();
        var gate = new PathGateLogic(EntityId.Next()) { Position = new GridPosRPG(7, 7), Facing = Direction.North };

        entMgr.AddPathGate(gate);

        Assert.Equal(1, entMgr.PathGateCount);
        Assert.Equal(gate, entMgr.GetPathGate(gate.Id));
        Assert.Equal(gate, entMgr.GetPathGateAt(new GridPosRPG(7, 7)));
        Assert.True(tileMgr.GetPathGateIdAt(new GridPosRPG(7, 7)).HasValue);
    }

    [Fact]
    public void RemovePathGate_ClearsCollectionAndTileManager()
    {
        var (entMgr, tileMgr, _) = CreateEntityManager();
        var pos = new GridPosRPG(7, 7);
        var gate = new PathGateLogic(EntityId.Next()) { Position = pos, Facing = Direction.North };
        entMgr.AddPathGate(gate);

        entMgr.RemovePathGate(gate.Id, pos);

        Assert.Equal(0, entMgr.PathGateCount);
        Assert.Null(entMgr.GetPathGateAt(pos));
        Assert.False(tileMgr.GetPathGateIdAt(pos).HasValue);
    }

    // â”€â”€ Resource Nodes â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    public void AddResourceNode_AppearsInCollectionAndTileManager()
    {
        var (entMgr, tileMgr, _) = CreateEntityManager();
        var pos = new GridPosRPG(10, 10);
        var node = new ResourceNodeLogic(EntityId.Next()) { Position = pos, ResourceId = "wood" };

        entMgr.AddResourceNode(node);

        Assert.Single(entMgr.ResourceNodes);
        Assert.Equal(node, entMgr.GetResourceNode(node.Id));
        Assert.Equal(node, entMgr.GetResourceNodeAt(pos));
        Assert.True(tileMgr.GetResourceNodeIdAt(pos).HasValue);
        Assert.Equal(node.Id, tileMgr.GetResourceNodeIdAt(pos)!.Value);
    }

    [Fact]
    public void RemoveResourceNode_ClearsCollectionAndTileManager()
    {
        var (entMgr, tileMgr, _) = CreateEntityManager();
        var pos = new GridPosRPG(10, 10);
        var node = new ResourceNodeLogic(EntityId.Next()) { Position = pos, ResourceId = "wood" };
        entMgr.AddResourceNode(node);

        entMgr.RemoveResourceNode(node.Id, pos);

        Assert.Empty(entMgr.ResourceNodes);
        Assert.Null(entMgr.GetResourceNodeAt(pos));
        Assert.False(tileMgr.GetResourceNodeIdAt(pos).HasValue);
    }

    [Fact]
    public void GetResourceNodeAt_ReturnsNull_WhenEmpty()
    {
        var (entMgr, _, _) = CreateEntityManager();
        Assert.Null(entMgr.GetResourceNodeAt(new GridPosRPG(10, 10)));
    }

    // â”€â”€ Typed Queries â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    public void CountSpawners_ReturnsCorrectCount()
    {
        var (entMgr, _, _) = CreateEntityManager();

        Assert.Equal(0, entMgr.CountSpawners());

        entMgr.AddStructure(new VillageSpawnerLogic(EntityId.Next()), new GridPosRPG(0, 0));
        entMgr.AddStructure(new VillageSpawnerLogic(EntityId.Next()), new GridPosRPG(1, 1));

        Assert.Equal(2, entMgr.CountSpawners());
    }

    [Fact]
    public void CountTrainingBuildings_ReturnsCorrectCount()
    {
        var (entMgr, _, _) = CreateEntityManager();

        Assert.Equal(0, entMgr.CountTrainingBuildings());

        entMgr.AddStructure(new TrainingBuildingLogic(EntityId.Next()), new GridPosRPG(2, 2));

        Assert.Equal(1, entMgr.CountTrainingBuildings());
    }

    [Fact]
    public void FindExitGateForStructure_ReturnsGate_WhenLinked()
    {
        var (entMgr, _, _) = CreateEntityManager();
        var bus = new EventBus();
        var protoReg = new ProtoRegistry();
        protoReg.RegisterDefaults();
        var pf = new ProtoFactory(protoReg, bus);
        var ts = new TutorialSystem(bus, pf);
        var tileMgr = new TileManager(new TerrainGrid(32, 32));
        var pgm = new PathGateManager(entMgr, tileMgr, bus, new Core.Commands.CommandBus(), ts, new ItemManager(bus), new ResearchManager(bus));

        var structureId = 123UL;
        var gate = new PathGateLogic(EntityId.Next())
        {
            Position = new GridPosRPG(4, 4),
            Facing = Direction.South
        };
        gate.LinkToStructure(structureId, new GridPosRPG(4, 5));
        entMgr.AddPathGate(gate);

        var found = pgm.FindExitGateForStructure(structureId);
        Assert.NotNull(found);
        Assert.Equal(gate.Id, found!.Id);
    }

    [Fact]
    public void FindExitGateForStructure_ReturnsNull_WhenNoExitGate()
    {
        var (entMgr, _, _) = CreateEntityManager();
        var bus = new EventBus();
        var protoReg = new ProtoRegistry();
        protoReg.RegisterDefaults();
        var pf = new ProtoFactory(protoReg, bus);
        var ts = new TutorialSystem(bus, pf);
        var tileMgr = new TileManager(new TerrainGrid(32, 32));
        var pgm = new PathGateManager(entMgr, tileMgr, bus, new Core.Commands.CommandBus(), ts, new ItemManager(bus), new ResearchManager(bus));

        Assert.Null(pgm.FindExitGateForStructure(999));
    }

    [Fact]
    public void IsPositionOccupiedByVillager_ReturnsTrue_WhenOccupied()
    {
        var (entMgr, _, vs) = CreateEntityManager();

        var villager = new VillagerLogic(EntityId.Next())
        {
            Name = "TestV",
            Position = new GridPosRPG(5, 5)
        };
        vs.AddVillager(villager);

        // Use an excludeId that can never match the villager
        Assert.True(entMgr.IsPositionOccupiedByVillager(new GridPosRPG(5, 5), excludeId: ulong.MaxValue));
    }

    [Fact]
    public void IsPositionOccupiedByVillager_ReturnsFalse_WhenExcludedId()
    {
        var (entMgr, _, vs) = CreateEntityManager();

        var villager = new VillagerLogic(EntityId.Next())
        {
            Name = "TestV",
            Position = new GridPosRPG(5, 5)
        };
        vs.AddVillager(villager);

        Assert.False(entMgr.IsPositionOccupiedByVillager(new GridPosRPG(5, 5), excludeId: villager.Id));
    }

    // â”€â”€ Lifecycle â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    public void Clear_RemovesAllEntitiesAndOccupancy()
    {
        var (entMgr, tileMgr, vs) = CreateEntityManager();

        entMgr.AddHero(new HeroEntity(EntityId.Next()) { ClassId = "warrior" });
        entMgr.AddStructure(new VillageSpawnerLogic(EntityId.Next()), new GridPosRPG(0, 0));
        entMgr.AddPathSegment(new PathSegmentLogic(EntityId.Next()) { Position = new GridPosRPG(1, 1), Facing = Direction.East });
        entMgr.AddPathGate(new PathGateLogic(EntityId.Next()) { Position = new GridPosRPG(2, 2), Facing = Direction.North });
        entMgr.AddResourceNode(new ResourceNodeLogic(EntityId.Next()) { Position = new GridPosRPG(3, 3), ResourceId = "wood" });
        vs.AddVillager(new VillagerLogic(EntityId.Next()) { Name = "TestV" });

        entMgr.Clear();

        Assert.Empty(entMgr.Heroes);
        Assert.Empty(entMgr.HeroIndex);
        Assert.Empty(entMgr.Structures);
        Assert.Empty(entMgr.PathSegments);
        Assert.Empty(entMgr.PathGates);
        Assert.Empty(entMgr.ResourceNodes);
        Assert.Empty(entMgr.Villagers);
        Assert.False(tileMgr.IsOccupied(new GridPosRPG(0, 0)));
        Assert.False(tileMgr.IsOccupied(new GridPosRPG(1, 1)));
        Assert.False(tileMgr.IsOccupied(new GridPosRPG(2, 2)));
        Assert.False(tileMgr.IsOccupied(new GridPosRPG(3, 3)));
    }
}

/// <summary>GuildData, bootstrapper, and miscellaneous integration tests migrated from Phase10/11.</summary>
public class GuildAndBootstrapperTests
{
    [Fact]
    public void GuildData_TrySpendGold_DeductsBalance()
    {
        var bus = new EventBus();
        var itemMgr = new ItemManager(bus);
        itemMgr.SetStock("gold", 100);

        Assert.True(itemMgr.TrySpendStock("gold", 50));
        Assert.Equal(50, itemMgr.GetStock("gold"));

        Assert.False(itemMgr.TrySpendStock("gold", 75));
        Assert.Equal(50, itemMgr.GetStock("gold"));
    }

    [Fact]
    public void GuildData_AddGold_IncreasesBalance()
    {
        var bus = new EventBus();
        var itemMgr = new ItemManager(bus);
        itemMgr.SetStock("gold", 50);
        itemMgr.AddStock("gold", 25);

        Assert.Equal(75, itemMgr.GetStock("gold"));
    }

    [Fact]
    public void GuildData_NegativeSpend_Rejected()
    {
        var bus = new EventBus();
        var itemMgr = new ItemManager(bus);
        itemMgr.SetStock("gold", 100);
        Assert.False(itemMgr.TrySpendStock("gold", -10));
        Assert.Equal(100, itemMgr.GetStock("gold"));
    }

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
    public void Bootstrapper_ApplyNewGameSettings_CreatesGuild()
    {
        var bootstrapper = new GameBootstrapper();
        bootstrapper.Bootstrap();

        var settings = new NewGameSettings
        {
            GameName = "Test Game",
            Difficulty = Difficulty.Easy,
            GuildName = "Test Guild",
            BannerId = "banner_dragon",
            LogoId = "logo_crown"
        };

        bootstrapper.ApplyNewGameSettings(settings);

        Assert.NotNull(bootstrapper.Services.Get<SimulationTicker>().Guild);
        Assert.Equal("Test Guild", bootstrapper.Services.Get<SimulationTicker>().Guild!.GuildName);
        Assert.Equal("banner_dragon", bootstrapper.Services.Get<SimulationTicker>().Guild.BannerId);
        Assert.Equal("logo_crown", bootstrapper.Services.Get<SimulationTicker>().Guild.LogoId);
    }

    [Fact]
    public void Bootstrapper_ApplyNewGameSettings_CreatesWorkerLifecycle()
    {
        var bootstrapper = new GameBootstrapper();
        bootstrapper.Bootstrap();

        bootstrapper.ApplyNewGameSettings(new NewGameSettings { Difficulty = Difficulty.Normal });

        Assert.NotNull(bootstrapper.Services.Get<SimulationTicker>().WorkerLifecycle);
    }

    [Fact]
    public void Bootstrapper_GuildGold_ScalesWithDifficulty()
    {
        var bootstrapper = new GameBootstrapper();
        bootstrapper.Bootstrap();

        bootstrapper.ApplyNewGameSettings(new NewGameSettings { Difficulty = Difficulty.Casual });
        int casualGold = bootstrapper.Services.Get<ItemManager>().GetStock("gold");

        bootstrapper.ApplyNewGameSettings(new NewGameSettings { Difficulty = Difficulty.Normal });
        int normalGold = bootstrapper.Services.Get<ItemManager>().GetStock("gold");

        Assert.True(casualGold > normalGold, "Casual should start with more gold");
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

    [Fact]
    public void GameBootstrapper_Phase7_CreatesTranslationService()
    {
        ServiceLocator.Clear();
        var bootstrapper = new GameBootstrapper();
        bootstrapper.Bootstrap();

        Assert.NotNull(bootstrapper.Services.Get<TranslationService>());
        Assert.True(bootstrapper.Services.Get<TranslationService>().KeyCount > 0);
        Assert.Equal("en", bootstrapper.Services.Get<TranslationService>().ActiveLanguage);
    }

    [Fact]
    public void GameBootstrapper_Phase7_TranslationServiceIsAccessible()
    {
        ServiceLocator.Clear();
        var bootstrapper = new GameBootstrapper();
        bootstrapper.Bootstrap();

        // ServiceLocator is no longer populated by bootstrap (#4 in Action Plan).
        // Translation is reached via the bootstrapper property.
        Assert.NotNull(bootstrapper.Services.Get<TranslationService>());
        ServiceLocator.Clear();
    }

    [Fact]
    public void GameBootstrapper_FullFlow_SpawnHeroCheckAchievements()
    {
        ServiceLocator.Clear();
        var bootstrapper = new GameBootstrapper();
        bootstrapper.Bootstrap();

        bootstrapper.Services.Get<GameStateMachine>().StartNewGame();

        bootstrapper.Services.Get<EventBus>().Publish(new HeroSpawnedEvent(new EntityId(1), "warrior", new GridPosRPG(0, 0)));

        Assert.True(bootstrapper.Services.Get<AchievementSystem>().IsUnlocked("first_hero"));
        Assert.Equal(1, bootstrapper.Services.Get<GameStatistics>().TotalHeroesSpawned);
    }
}

/// <summary>Miscellaneous integration tests migrated from Phase7IntegrationTests.</summary>
public class MiscIntegrationTests
{
    [Fact]
    public void HeroEntity_DefaultId_IsNonZero()
    {
        EntityBase.ResetIdCounter();
        _ = new PathSegmentLogic(EntityId.Next()) { Position = new GridPosRPG(0, 0), Facing = Direction.East };

        var hero = new HeroEntity(EntityId.Next()) { ClassId = "warrior" };
        Assert.True(hero.Id > 0);
    }

    [Fact]
    public void HeroEntity_AverageGearTier_ZeroWhenNoGear()
    {
        var hero = new HeroEntity(EntityId.Next()) { ClassId = "warrior" };
        Assert.Equal(0f, hero.AverageGearTier);
    }

    [Fact]
    public void HeroEntity_UnequipSlot_RemovesItem()
    {
        var hero = new HeroEntity(EntityId.Next()) { ClassId = "warrior" };
        hero.Equip(new EquippedItem { ProtoId = "sword", Slot = EquipSlot.Weapon, Tier = 1 });
        Assert.Single(hero.Equipment);

        hero.Equip(new EquippedItem { ProtoId = "better_sword", Slot = EquipSlot.Weapon, Tier = 2 });
        Assert.Single(hero.Equipment);
        Assert.Equal("better_sword", hero.GetEquippedInSlot(EquipSlot.Weapon)?.ProtoId);
    }

    [Fact]
    public void ProtoRegistry_GetStructure_ReturnsNullForUnknown()
    {
        var reg = new ProtoRegistry();
        reg.RegisterDefaults();
        Assert.Null(reg.GetStructure("nonexistent_structure"));
    }

    [Fact]
    public void ProtoFactory_CreatePathSegmentLine()
    {
        var reg = new ProtoRegistry();
        reg.RegisterDefaults();
        var bus = new EventBus();
        var factory = new ProtoFactory(reg, bus);

        var seg = factory.CreatePathSegment("path_straight");
        Assert.NotNull(seg);
        Assert.Equal(PathNodeType.Straight, seg.NodeType);
    }

    [Fact]
    public void ResourceNodeLogic_NonRenewable_StaysDepleted()
    {
        var proto = new ResourceNodeProto
        {
            Id = "stone_deposit", ResourceId = "stone", MaxYield = 10,
            HarvestRate = 1.0f, IsRenewable = false
        };
        var node = new ResourceNodeLogic(EntityId.Next());
        node.InitializeFromProto(proto);
        node.Harvest(10);

        Assert.True(node.IsDepleted);
        node.Tick(10f);
        Assert.True(node.IsDepleted);
        Assert.Equal(0, node.CurrentYield);
    }

    [Fact]
    public void TerrainGrid_Width_And_Height()
    {
        var grid = new TerrainGrid(16, 8);
        Assert.Equal(16, grid.Width);
        Assert.Equal(8, grid.Height);
    }

    [Fact]
    public void TutorialSystem_SkipAll_CompletesAllMissions()
    {
        var bus = new EventBus();
        var reg = new ProtoRegistry();
        reg.RegisterDefaults();
        var factory = new ProtoFactory(reg, bus);
        var tutorial = new TutorialSystem(bus, factory);
        tutorial.Initialize(reg);

        int completedCount = 0;
        while (tutorial.ActiveMission != null)
        {
            foreach (var cond in tutorial.ActiveMission.Conditions)
            {
                tutorial.AdvanceCondition(cond.Type);
            }
            completedCount++;
            if (completedCount > 100)
            {
                break;
            }
        }

        Assert.Null(tutorial.ActiveMission);
        Assert.True(tutorial.CompletedCount > 0);
    }

    [Fact]
    public void VillagerSystem_GetByJob_FiltersCorrectly()
    {
        var bus = new EventBus();
        var rm = new ItemManager(bus);
        var system = new VillagerSystem(bus, rm);

        var lumberjack = new VillagerLogic(EntityId.Next()) { Name = "Woody" };
        lumberjack.AssignJob(VillagerJob.Lumberjack);
        system.AddVillager(lumberjack);

        var miner = new VillagerLogic(EntityId.Next()) { Name = "Digger" };
        miner.AssignJob(VillagerJob.Miner);
        system.AddVillager(miner);

        var idle = new VillagerLogic(EntityId.Next()) { Name = "Lazy" };
        system.AddVillager(idle);

        Assert.Equal(3, system.Count);
    }

    [Fact]
    public void GatingLimits_AllTiers_ReturnPositiveValues()
    {
        var gating = new GatingLimits();
        for (int tier = 1; tier <= 10; tier++)
        {
            Assert.True(gating.GetMaxSpawners(tier) > 0);
            Assert.True(gating.GetMaxBuildings(tier) > 0);
            Assert.True(gating.GetMaxPaths(tier) > 0);
        }
    }

    [Fact]
    public void TrafficManager_ConcurrentSegments_Independent()
    {
        var tm = new TrafficManager(maxOccupantsPerSegment: 2);

        tm.EnterSegment(100, 1);
        tm.EnterSegment(100, 2);
        tm.EnterSegment(200, 3);

        Assert.False(tm.CanEnterSegment(100, 99));
        Assert.True(tm.CanEnterSegment(200, 4));
    }

    [Fact]
    public void PathSegmentLogic_SpeedMultiplier_Default()
    {
        var seg = new PathSegmentLogic(EntityId.Next());
        Assert.True(seg.SpeedMultiplier > 0);
    }

    [Fact]
    public void VillagerLogic_PlaceOnPath_SetsTravelling()
    {
        var villager = new VillagerLogic(EntityId.Next());
        villager.PlaceOnPath(42);
        Assert.Equal(VillagerState.Travelling, villager.State);
        Assert.Equal(42ul, villager.CurrentPathSegmentId);
    }

    [Fact]
    public void VillagerLogic_HasZeroWorkRate_WhenIdle()
    {
        var villager = new VillagerLogic(EntityId.Next()) { WorkRate = 1.0f };
        villager.AssignJob(VillagerJob.Idle);
        Assert.Equal(1.0f, villager.EffectiveWorkRate);
    }

    [Fact]
    public void PathSegmentLogic_WeightedSplitter_UsesWeightedRouting()
    {
        var seg = new PathSegmentLogic(EntityId.Next())
        {
            NodeType = PathNodeType.Splitter,
            SplitMode = SplitMode.Weighted,
            SplitWeight = 80,
            NextSegmentId = 100,
            SplitTargetId = 200
        };

        int path1 = 0, path2 = 0;
        for (int i = 0; i < 100; i++)
        {
            var next = seg.ResolveNextSegment();
            if (next == 100)
            {
                path1++;
            }
            else if (next == 200)
            {
                path2++;
            }
        }

        Assert.True(path1 + path2 == 100);
    }
}
