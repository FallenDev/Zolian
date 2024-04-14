namespace Darkages.Enums;

[Flags]
public enum MapFlags : byte
{
    Default = 0,
    Snow = 1, // Actual Map Flag
    Rain = 2, // Actual Map Flag
    Darkness = Snow | Rain, // Actual Map Flag
    ArenaTeam = 4,
    CantUseItems = 6,
    CantDropItems = 7,
    PlayerKill = 8,
    CantTeleport = 16,
    EvilReaches = CantTeleport | Rain | PlayerKill,
    CantUseAbilities = 32,
    NoTabMap = 64, // Actual Map Flag
    Jail = CantUseAbilities | CantUseItems | CantDropItems,
    SnowTileSet = 128, // Actual Map Flag
    SnowingSnowTileSet = SnowTileSet | Snow,
    RainingSnowTileSet = SnowTileSet | Rain,
    DarknessSnowTileSet = SnowTileSet | Darkness,
    ArenaSnowTileSet = SnowTileSet | PlayerKill,
    NoTabSnowTileSet = SnowTileSet | NoTabMap
}

/// <summary>
/// Set Default as 1 due to Flags 0 = 0, always true
/// </summary>
[Flags]
public enum MiningNodes : byte
{
    Default = 1,
    Talos = 1 << 1,
    Copper = 1 << 2,
    DarkIron = 1 << 3,
    Hybrasyl = 1 << 4,
    CobaltSteel = 1 << 5,
    Obsidian = 1 << 6,

    CopperMine = Talos | Copper,
    DarkIronMine = Copper | DarkIron,
    DarkIronWoods = Talos | DarkIron,
    HybrasylWoods = Copper | Hybrasyl,
    HybrasylMine = DarkIron | Hybrasyl,
    CobaltWoods = DarkIron | CobaltSteel,
    CobaltCaverns = Talos | CobaltSteel,
    ObsidianRuins = Hybrasyl | Obsidian,
    ObsidianDepths = CobaltSteel | Obsidian
}

[Flags]
public enum WildFlowers : byte
{
    Default = 1,
    GloomBloom = 1 << 1,
    Betrayal = 1 << 2,
    Bocan = 1 << 3,
    Cactus = 1 << 4,
    Prahed = 1 << 5,
    Aiten = 1 << 6,
    Reict = 1 << 7,

    Misery = GloomBloom | Betrayal,
    Sunset = Betrayal | Aiten
}

[Flags]
public enum ChestType : byte
{
    Default = 1,
    Normal = 1 << 1,
    Rare = 1 << 2,
    Epic = 1 << 3,
    Legendary = 1 << 4,
    Mythical = 1 << 5,
    Godly = 1 << 6,
    Divine = 1 << 7
}

public static class MapExtensions
{
    public static bool MapFlagIsSet(this MapFlags self, MapFlags flag) => (self & flag) == flag;
    public static bool MapNodeFlagIsSet(this MiningNodes self, MiningNodes flag) => (self & flag) == flag;
    public static bool MapFlowerFlagIsSet(this WildFlowers self, WildFlowers flag) => (self & flag) == flag;
    public static bool MapChestFlagIsSet(this ChestType self, ChestType flag) => (self & flag) == flag;
}