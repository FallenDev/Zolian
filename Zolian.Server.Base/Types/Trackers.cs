﻿using Chaos.Collections.Common;
using Chaos.Collections.Time;
using Chaos.Geometry;

using Darkages.Infrastructure;
using Darkages.Interfaces;
using Darkages.Sprites;

namespace Darkages.Types;

public class Trackers : WorldServerTimer
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

    public Trackers(TimeSpan delay) : base(delay) { }
}

public sealed class AislingTrackers : Trackers
{
    public DateTime LastEquip { get; set; }
    public DateTime LastManualAction { get; set; }
    public DateTime LastRefresh { get; set; }
    public DateTime LastUnequip { get; set; }
    public DateTime LastEquipOrUnequip => LastEquip > LastUnequip ? LastEquip : LastUnequip;

    public AislingTrackers(TimeSpan delay) : base(delay) { }
}