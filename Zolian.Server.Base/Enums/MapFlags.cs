﻿namespace Darkages.Enums;

[Flags]
public enum MapFlags : uint
{
    Snow = 1,
    Rain = 2,
    NoMap = 64,
    Winter = 128,
    CanSummon = 256,
    CanLocate = 512,
    CanTeleport = 1024,
    CanUseSkill = 2048,
    CanUseSpell = 4096,
    ArenaTeam = 8192,
    PlayerKill = 16384,
    SendToHell = 32768,
    ShouldComa = 65536,

    Darkness = Snow | Rain,
    Default = CanSummon | CanLocate | CanTeleport | CanUseSkill | CanUseSpell | SendToHell | ShouldComa,
    NoSkillSpell = CanSummon | CanLocate | CanTeleport | ShouldComa,
    SafeZone = CanSummon | CanLocate | CanTeleport | CanUseSkill | CanUseSpell | ShouldComa
}

public static class MapExtensions
{
    public static bool MapFlagIsSet(this MapFlags self, MapFlags flag) => (self & flag) == flag;
}