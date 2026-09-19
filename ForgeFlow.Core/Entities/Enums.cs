namespace ForgeFlow.Core.Entities;

public enum DamageType
{
    Slashing,
    Piercing,
    Blunt,
    Fire,
    Ice,
    Lightning,
    Arcane,
    Holy,
    Void,
    Nature
}

public enum MaterialType
{
    Wood,
    Iron,
    Steel,
    Mithril,
    Dragonbone,
    Celestial,
    Void
}

public enum EquipSlot
{
    Weapon,
    Underlayer,
    ChestArmor,
    Helmet,
    Boots,
    Pauldrons,
    Cape,
    Shield,
    Accessory
}

public enum Direction
{
    North,
    East,
    South,
    West
}

/// <summary>Extension methods for the Direction enum.</summary>
public static class DirectionExtensions
{
    public static Direction Opposite(this Direction dir) => dir switch
    {
        Direction.North => Direction.South,
        Direction.East => Direction.West,
        Direction.South => Direction.North,
        Direction.West => Direction.East,
        _ => dir
    };

    public static Direction RotateClockwise(this Direction dir) => dir switch
    {
        Direction.North => Direction.East,
        Direction.East => Direction.South,
        Direction.South => Direction.West,
        Direction.West => Direction.North,
        _ => dir
    };

    public static Direction RotateCounterClockwise(this Direction dir) => dir switch
    {
        Direction.North => Direction.West,
        Direction.East => Direction.North,
        Direction.South => Direction.East,
        Direction.West => Direction.South,
        _ => dir
    };
}

public enum HeroState
{
    OnPath,
    EquippingGear,
    InDungeon,
    AwaitingFusion,
    Ghost,
    Retired,
    Exported,
    WornOut,
    ReturningToMaintenance
}

public enum DungeonTheme
{
    GoblinCaves,
    Crypts,
    DragonLairs,
    CosmicDungeons,
    InfiniteAbyss,
    VoidRift,
    RaidPortal
}

public enum GameState
{
    MainMenu,
    NewGameSetup,
    Loading,
    Playing,
    Paused,
    GameOver,
    Victory
}

public enum Difficulty
{
    Casual,
    Easy,
    Normal
}

/// <summary>Worker profession — determines wear-out condition and maintenance station.</summary>
public enum WorkerProfession
{
    None,
    Forester,
    Miner,
    HerbGatherer,
    Researcher,
    Warrior,
    Cleric,
    Ranger,
    Mage,
    Guard,
    Thief,
    Rogue
}

/// <summary>Reason a worker became worn out and must leave the building.</summary>
public enum WearOutReason
{
    None,
    ToolBroken,
    StaminaDepleted,
    CarryCapacityFull,
    ResearchComplete,
    FatiguedFromTraining,
    InventoryFull
}

/// <summary>Condition type used by automation check gates to route workers.</summary>
public enum GateConditionType
{
    MinLevel,
    MaxLevel,
    HasTrait,
    HasProfession,
    HasAbility,
    HasJobClass,
    HasItem,
    CarryingItem,
    HasToolType
}

/// <summary>Type of automation structure for routing workers.</summary>
[Obsolete]
public enum AutomationStructureType
{
    CheckGate,
    FilterSplitter,
    Balancer
}
