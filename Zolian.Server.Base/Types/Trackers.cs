﻿using Chaos.Geometry;

using Darkages.Common;
using Darkages.Sprites;

namespace Darkages.Types;

public class Trackers(TimeSpan delay) : WorldServerTimer(delay)
{
    public Sprite LastDamagedBy { get; set; }
    public string LastMapInstanceId { get; set; }
    public Location LastPosition { get; set; }
    public DateTime LastSkillUse { get; set; }
    public DateTime LastSpellUse { get; set; }
    public DateTime LastTalk { get; set; }
    public DateTime LastTurn { get; set; }
    public Skill LastUsedSkill { get; set; }
    public Spell LastUsedSpell { get; set; }
}

public sealed class AislingTrackers(TimeSpan delay) : Trackers(delay)
{
    public DateTime LastEquip { get; set; }
    public DateTime LastManualAction { get; set; }
    public DateTime LastWalk { get; set; }
    public DateTime LastRefresh { get; set; }
    public DateTime LastUnequip { get; set; }
    public DateTime LastEquipOrUnequip => LastEquip > LastUnequip ? LastEquip : LastUnequip;
}