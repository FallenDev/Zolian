using Darkages.GameScripts.Affects;
using Darkages.Sprites;
using Darkages.Sprites.Entity;

namespace Darkages.Types;

public class Debuff
{
    public bool Cancelled { get; set; }
    public virtual byte Icon { get; set; }
    public virtual int Length { get; set; }
    public virtual string Name { get; set; }
    public virtual bool Affliction { get; set; }
    public int TimeLeft { get; set; }
    public Debuff DebuffSpell { get; set; }

    public virtual void OnApplied(Sprite affected, Debuff debuff) { }
    public virtual void OnDurationUpdate(Sprite affected, Debuff debuff) => debuff.TimeLeft--;
    public virtual void OnEnded(Sprite affected, Debuff debuff) { }
    public virtual void OnItemChange(Aisling affected, Debuff debuff) { }

    public Debuff ObtainDebuffName(Sprite affected, Debuff debuff)
    {
        if (affected is not Aisling) return null;

        DebuffSpell = debuff.Name switch
        {
            "Eclipse Seal" => new DebuffEclipseSeal(),
            "Sun Seal" => new DebuffSunSeal(),
            "Penta Seal" => new DebuffPentaSeal(),
            "Moon Seal" => new DebuffMoonSeal(),
            "Dark Seal" => new DebuffDarkSeal(),
            "Uas Cradh" => new DebuffUasCradh(),
            "Croich Ard Cradh" => new DebuffCriochArdCradh(),
            "Croich Mor Cradh" => new DebuffCriochMorCradh(),
            "Croich Cradh" => new DebuffCriochCradh(),
            "Croich Beag Cradh" => new DebuffCriochBeagCradh(),
            "Ard Cradh" => new DebuffArdcradh(),
            "Mor Cradh" => new DebuffMorcradh(),
            "Cradh" => new DebuffCradh(),
            "Beag Cradh" => new DebuffBeagcradh(),
            "Rending" => new DebuffRending(),
            "Corrosive Touch" => new DebuffCorrosiveTouch(),
            "Shield Bash" => new DebuffShieldBash(),
            "Titan's Cleave" => new DebuffTitansCleave(),
            "Retribution" => new DebuffRetribution(),
            "Stab'n Twist" => new DebuffStabnTwist(),
            "Hurricane" => new DebuffHurricane(),
            "Beag Suain" => new DebuffBeagsuain(),
            "Entice" => new DebuffCharmed(),
            "Frozen" => new DebuffFrozen(),
            "Adv Frozen" => new DebuffAdvFrozen(),
            "Halt" => new DebuffHalt(),
            "Sleep" => new DebuffSleep(),
            "Deep Sleep" => new DebuffDeepSleep(),
            "Bleeding" => new DebuffBleeding(),
            "Uas Puinsein" => new DebuffUasPoison(),
            "Ard Puinsein" => new DebuffArdPoison(),
            "Mor Puinsein" => new DebuffMorPoison(),
            "Puinsein" => new DebuffPoison(),
            "Beag Puinsein" => new DebuffBeagPoison(),
            "Blind" => new DebuffBlind(),
            "Skulled" => new DebuffReaping(),
            "Decay'n Ruin" => new DebuffHalt(),
            "Decay" => new DebuffDecay(),
            "Dark Chain" => new DebuffDarkChain(),
            "Silence" => new DebuffSilence(),
            "Wrath Consequences" => new DebuffWrathConsequences(),
            "Deadly Poison" => new DebuffDeadlyPoison(),
            "Amaterasu" => new DebuffAmaterasu(),
            _ => DebuffSpell
        };

        return DebuffSpell;
    }

    public void Update(Sprite affected)
    {
        if (TimeLeft > 0)
            OnDurationUpdate(affected, this);
        else
            OnEnded(affected, this);
    }
}