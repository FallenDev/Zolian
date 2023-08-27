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
    SnowTileSet = 1 << 7 // Actual Map Flag
}

public static class MapExtensions
{
    public static bool MapFlagIsSet(this MapFlags self, MapFlags flag) => (self & flag) == flag;
}