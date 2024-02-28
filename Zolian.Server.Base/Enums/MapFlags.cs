namespace Darkages.Enums;

[Flags]
public enum MapFlags : byte
{
    Default = 0,
    Snow = 1, // Actual Map Flag
    Rain = 1 << 1, // Actual Map Flag
    Darkness = Snow | Rain, // Actual Map Flag
    ArenaTeam = 1 << 2,
    PlayerKill = 1 << 3,
    CantTeleport = 1 << 4,
    EvilReaches = CantTeleport | Rain | PlayerKill,
    CanUseAbilities = 1 << 5,
    NoTabMap = 1 << 6, // Actual Map Flag
    SnowTileSet = 1 << 7, // Actual Map Flag
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

    Misery = GloomBloom | Betrayal
}

public static class MapExtensions
{
    public static bool MapFlagIsSet(this MapFlags self, MapFlags flag) => (self & flag) == flag;
    public static bool MapNodeFlagIsSet(this MiningNodes self, MiningNodes flag) => (self & flag) == flag;
    public static bool MapFlowerFlagIsSet(this WildFlowers self, WildFlowers flag) => (self & flag) == flag;
}