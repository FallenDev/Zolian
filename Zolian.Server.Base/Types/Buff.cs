using Darkages.GameScripts.Affects;
using Darkages.Sprites;
using Darkages.Sprites.Entity;

namespace Darkages.Types;

public class Buff
{
    public bool Cancelled { get; set; }
    public virtual byte Icon { get; set; }
    public virtual int Length { get; set; }
    public virtual string Name { get; set; }
    public virtual bool Affliction { get; set; }
    public int TimeLeft { get; set; }
    public Buff BuffSpell { get; set; }

    public virtual void OnApplied(Sprite affected, Buff buff) { }
    public virtual void OnDurationUpdate(Sprite affected, Buff buff) => buff.TimeLeft--;
    public virtual void OnEnded(Sprite affected, Buff buff) { }
    public virtual void OnItemChange(Aisling affected, Buff buff) { }

    public Buff ObtainBuffName(Sprite affected, Buff buff)
    {
        if (affected is not Aisling) return null;

        BuffSpell = buff.Name switch
        {
            "Dia Aite" => new buff_DiaAite(),
            "Aite" => new buff_aite(),
            "Claw Fist" => new buff_clawfist(),
            "Ard Dion" => new buff_ArdDion(),
            "Mor Dion" => new buff_MorDion(),
            "Dion" => new buff_dion(),
            "Stone Skin" => new buff_StoneSkin(),
            "Iron Skin" => new buff_IronSkin(),
            "Wings of Protection" => new buff_wingsOfProtect(),
            "Perfect Defense" => new buff_PerfectDefense(),
            "Asgall" => new buff_skill_reflect(),
            "Deireas Faileas" => new buff_spell_reflect(),
            "Spectral Shield" => new buff_SpectralShield(),
            "Defensive Stance" => new buff_DefenseUp(),
            "Adrenaline" => new buff_DexUp(),
            "Atlantean Weapon" => new buff_randWeaponElement(),
            "Elemental Bane" => new buff_ElementalBane(),
            "Dia Haste" => new buff_Dia_Haste(),
            "Hastenga" => new buff_Hastenga(),
            "Hasten" => new buff_Hasten(),
            "Haste" => new buff_Haste(),
            "Hide" => new buff_hide(),
            "Blend" => new buff_advHide(),
            "Shadowfade" => new buff_ShadowFade(),
            "Gryphons Grace" => new buff_GryphonsGrace(),
            "Orcish Strength" => new buff_OrcishStrength(),
            "Feywild Nectar" => new buff_FeywildNectar(),
            "Drunken Fist" => new buff_drunkenFist(),
            "Ninth Gate Release" => new buff_ninthGate(),
            "Berserker Rage" => new buff_berserk(),
            "Briarthorn Aura" => new aura_BriarThorn(),
            "Laws of Aosda" => new aura_LawsOfAosda(),
            "Ard Fas Nadur" => new BuffArdFasNadur(),
            "Mor Fas Nadur" => new BuffMorFasNadur(),
            "Fas Nadur" => new BuffFasNadur(),
            "Fas Spiorad" => new BuffFasSpiorad(),
            "Vampirisim" => new BuffVampirisim(),
            "Lycanisim" => new BuffLycanisim(),
            "Double XP" => new BuffDoubleExperience(),
            "Triple XP" => new BuffTripleExperience(),
            "Secured Position" => new aura_SecuredPosition(),
            "Hardened Hands" => new BuffHardenedHands(),
            "Rasen Shoheki" => new buff_RasenShoheki(),
            _ => BuffSpell
        };

        return BuffSpell;
    }

    public void Update(Sprite affected)
    {
        if (TimeLeft > 0)
            OnDurationUpdate(affected, this);
        else
            OnEnded(affected, this);
    }
}