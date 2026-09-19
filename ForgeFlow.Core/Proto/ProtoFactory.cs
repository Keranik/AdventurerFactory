using ForgeFlow.Core.Entities;
using ForgeFlow.Core.Events;
using ForgeFlow.Core.Proto.Logic;
using ForgeFlow.Core.Proto.Prototypes;

namespace ForgeFlow.Core.Proto;

/// <summary>
/// Factory that creates Logic instances from Proto definitions.
/// Proto → Logic instance. The Presentation layer then attaches the Mb.
/// </summary>
public sealed class ProtoFactory
{
    private readonly ProtoRegistry _registry;
    private readonly EventBus _eventBus;

    public ProtoFactory(ProtoRegistry registry, EventBus eventBus)
    {
        _registry = registry;
        _eventBus = eventBus;
    }

    /// <summary>
    /// Creates a gathering structure logic instance from a registered proto ID.
    /// Returns null if the proto is not found.
    /// </summary>
    public GatheringLogicBase? CreateGatheringRecipeEntity(string protoId, ResourceNodeLogic? linkedNode = null)
    {
        if (!_registry.TryGetStructure(protoId, out var proto)) return null;

        GatheringLogicBase gathering;

        if (proto is ForestryRecipeEntityProto)
        {
            gathering = new ForestryRecipeEntity(EntityId.Next());
        }
        else if (proto is MiningRecipeEntityProto)
        {
            gathering = new MiningRecipeEntity(EntityId.Next());
        }
        else
        {
            gathering = new GatheringLogicBase(EntityId.Next());
        }

        gathering.InitializeFromProto(proto);
        gathering.Initialize(linkedNode);
        return gathering;
    }

    /// <summary>Creates a resource node logic instance from a registered proto ID.</summary>
    public ResourceNodeLogic? CreateResourceNode(string protoId)
    {
        var proto = _registry.GetResourceNode(protoId);
        if (proto == null) return null;

        var node = new ResourceNodeLogic(EntityId.Next());
        node.InitializeFromProto(proto);
        return node;
    }

    /// <summary>Creates a path segment logic instance from a registered proto ID.</summary>
    public PathSegmentLogic? CreatePathSegment(string protoId)
    {
        var proto = _registry.GetPathSegment(protoId);
        if (proto == null) return null;

        var segment = new PathSegmentLogic(EntityId.Next());
        segment.InitializeFromProto(proto);
        return segment;
    }

    /// <summary>Creates a villager entity from a registered proto ID.</summary>
    public VillagerLogic? CreateVillager(string protoId)
    {
        var proto = _registry.GetVillager(protoId);
        if (proto == null) return null;

        var villager = new VillagerLogic(EntityId.Next());
        villager.InitializeFromProto(proto);
        return villager;
    }

    /// <summary>Creates a village spawner structure from a registered proto ID.</summary>
    public VillageSpawnerLogic? CreateVillageSpawner(string protoId)
    {
        if (!_registry.TryGetStructure(protoId, out var proto)) return null;
        if (proto is not VillageSpawnerProto) return null;

        var spawner = new VillageSpawnerLogic(EntityId.Next());
        spawner.InitializeFromProto(proto);
        return spawner;
    }

    /// <summary>Creates a tutorial mission logic instance from a registered proto ID.</summary>
    public TutorialMissionLogic? CreateTutorialMission(string protoId)
    {
        var proto = _registry.GetTutorial(protoId);
        if (proto == null) return null;

        var mission = new TutorialMissionLogic();
        mission.InitializeFromProto(proto);
        return mission;
    }

    /// <summary>Creates a training building logic instance from a registered proto ID.</summary>
    public TrainingBuildingLogic? CreateTrainingBuilding(string protoId)
    {
        if (!_registry.TryGetStructure(protoId, out var proto)) return null;
        if (proto is not TrainingBuildingProto) return null;

        var school = new TrainingBuildingLogic(EntityId.Next());
        school.InitializeFromProto(proto);
        return school;
    }

    /// <summary>Creates an Inn logic instance from a registered proto ID.</summary>
    public InnLogic? CreateInn(string protoId)
    {
        if (!_registry.TryGetStructure(protoId, out var proto)) return null;
        if (proto is not InnProto) return null;

        var inn = new InnLogic(EntityId.Next());
        inn.InitializeFromProto(proto);
        return inn;
    }

    /// <summary>Creates a Craft Station logic instance from a registered proto ID.</summary>
    public CraftStationLogic? CreateCraftStation(string protoId)
    {
        if (!_registry.TryGetStructure(protoId, out var proto)) return null;
        if (proto is not CraftStationProto) return null;

        var station = new CraftStationLogic(EntityId.Next());
        station.InitializeFromProto(proto);
        return station;
    }

    /// <summary>Creates a Stockpile logic instance from a registered proto ID.</summary>
    public StockpileLogic? CreateStockpile(string protoId)
    {
        if (!_registry.TryGetStructure(protoId, out var proto)) return null;
        if (proto is not StockpileProto) return null;

        var stockpile = new StockpileLogic(EntityId.Next());
        stockpile.InitializeFromProto(proto);
        return stockpile;
    }

    /// <summary>Creates a Forge logic instance from a registered proto ID.</summary>
    public ForgeLogic? CreateForge(string protoId)
    {
        if (!_registry.TryGetStructure(protoId, out var proto)) return null;
        if (proto is not ForgeProto) return null;

        var forge = new ForgeLogic(EntityId.Next());
        forge.InitializeFromProto(proto);
        return forge;
    }

    /// <summary>Creates a Dungeon Portal logic instance from a registered proto ID.</summary>
    public DungeonPortalLogic? CreateDungeonPortal(string protoId, Data.DungeonRegistry dungeonRegistry)
    {
        if (!_registry.TryGetStructure(protoId, out var proto)) return null;
        if (proto is not DungeonPortalProto) return null;

        var portal = new DungeonPortalLogic(EntityId.Next());
        portal.InitializeFromProto(proto);
        portal.Initialize(dungeonRegistry);
        return portal;
    }

    /// <summary>Creates a Fusion Altar logic instance from a registered proto ID.</summary>
    public FusionAltarLogic? CreateFusionAltar(string protoId)
    {
        if (!_registry.TryGetStructure(protoId, out var proto)) return null;
        if (proto is not FusionAltarProto) return null;

        var altar = new FusionAltarLogic(EntityId.Next());
        altar.InitializeFromProto(proto);
        return altar;
    }

    /// <summary>Creates an Appearance Workshop logic instance from a registered proto ID.</summary>
    public AppearanceWorkshopLogic? CreateAppearanceWorkshop(string protoId)
    {
        if (!_registry.TryGetStructure(protoId, out var proto)) return null;
        if (proto is not AppearanceWorkshopProto) return null;

        var workshop = new AppearanceWorkshopLogic(EntityId.Next());
        workshop.InitializeFromProto(proto);
        return workshop;
    }

    /// <summary>Creates a PathGate logic instance from a registered proto ID.</summary>
    public PathGateLogic? CreatePathGate(string protoId)
    {
        var proto = _registry.GetPathGate(protoId);
        if (proto == null) return null;

        var gate = new PathGateLogic(EntityId.Next());
        gate.InitializeFromProto(proto);
        return gate;
    }

    /// <summary>Creates a Tool Station logic instance from a registered proto ID.</summary>
    public ToolStationLogic? CreateToolStation(string protoId)
    {
        if (!_registry.TryGetStructure(protoId, out var proto)) return null;
        if (proto is not ToolStationProto) return null;

        var station = new ToolStationLogic(EntityId.Next());
        station.InitializeFromProto(proto);
        return station;
    }

    /// <summary>Creates an Armory logic instance from a registered proto ID.</summary>
    public ArmoryLogic? CreateArmory(string protoId)
    {
        if (!_registry.TryGetStructure(protoId, out var proto)) return null;
        if (proto is not ArmoryProto) return null;

        var armory = new ArmoryLogic(EntityId.Next());
        armory.InitializeFromProto(proto);
        return armory;
    }

    /// <summary>Creates a Job Changer logic instance from a registered proto ID.</summary>
    public JobChangerLogic? CreateJobChanger(string protoId)
    {
        if (!_registry.TryGetStructure(protoId, out var proto)) return null;
        if (proto is not JobChangerProto) return null;

        var changer = new JobChangerLogic(EntityId.Next());
        changer.InitializeFromProto(proto);
        return changer;
    }

    /// <summary>Creates an Academy logic instance from a registered proto ID.</summary>
    public AcademyLogic? CreateAcademy(string protoId)
    {
        if (!_registry.TryGetStructure(protoId, out var proto)) return null;
        if (proto is not AcademyProto) return null;

        var academy = new AcademyLogic(EntityId.Next());
        academy.InitializeFromProto(proto);
        return academy;
    }

    /// <summary>Creates a Check Gate logic instance from a registered proto ID.</summary>
    public CheckGateLogic? CreateCheckGate(string protoId)
    {
        if (!_registry.TryGetStructure(protoId, out var proto)) return null;
        if (proto is not CheckGateProto) return null;

        var gate = new CheckGateLogic(EntityId.Next());
        gate.InitializeFromProto(proto);
        return gate;
    }

    /// <summary>Creates a Filter Splitter logic instance from a registered proto ID.</summary>
    public FilterSplitterLogic? CreateFilterSplitter(string protoId)
    {
        if (!_registry.TryGetStructure(protoId, out var proto)) return null;
        if (proto is not FilterSplitterProto) return null;

        var splitter = new FilterSplitterLogic(EntityId.Next());
        splitter.InitializeFromProto(proto);
        return splitter;
    }

    /// <summary>Creates a Balancer logic instance from a registered proto ID.</summary>
    public BalancerLogic? CreateBalancer(string protoId)
    {
        if (!_registry.TryGetStructure(protoId, out var proto)) return null;
        if (proto is not BalancerProto) return null;

        var balancer = new BalancerLogic(EntityId.Next());
        balancer.InitializeFromProto(proto);
        return balancer;
    }
}
