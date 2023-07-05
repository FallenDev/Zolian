using Chaos.Collections.Common;
using Chaos.Collections.Time;
using Chaos.Geometry;
using Darkages.Interfaces;
using Darkages.Sprites;

namespace Darkages.Types;

public class Trackers : IDeltaUpdatable
{
    public CounterCollection Counters { get; init; }
    public EnumCollection Enums { get; init; }
    public FlagCollection Flags { get; init; }
    public Sprite? LastDamagedBy { get; set; }
    public string? LastMapInstanceId { get; set; }
    public Location? LastPosition { get; set; }
    public DateTime? LastSkillUse { get; set; }
    public DateTime? LastSpellUse { get; set; }
    public DateTime? LastTalk { get; set; }
    public DateTime? LastTurn { get; set; }
    public Skill? LastUsedSkill { get; set; }
    public Spell? LastUsedSpell { get; set; }
    public DateTime? LastWalk { get; set; }
    public TimedEventCollection TimedEvents { get; init; }

    public Trackers()
    {
        Counters = new CounterCollection();
        Enums = new EnumCollection();
        Flags = new FlagCollection();
        TimedEvents = new TimedEventCollection();
    }

    /// <inheritdoc />
    public void Update(TimeSpan delta) => TimedEvents.Update(delta);
}

public sealed class AislingTrackers : Trackers
{
    public DateTime? LastEquip { get; set; }
    public DateTime? LastManualAction { get; set; }
    public DateTime? LastRefresh { get; set; }
    public DateTime? LastUnequip { get; set; }
    public DateTime? LastEquipOrUnequip => LastEquip > LastUnequip ? LastEquip : LastUnequip;
}