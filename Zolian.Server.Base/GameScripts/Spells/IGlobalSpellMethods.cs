using Darkages.Network.Client;
using Darkages.Sprites;
using Darkages.Types;

namespace Darkages.GameScripts.Spells;

public interface IGlobalSpellMethods
{
    bool Execute(WorldClient client, Spell spell);
    void Train(WorldClient client, Spell spell);
    long WeaponDamageElementalProc(Sprite sprite, int weaponProc);
    long AislingSpellDamageCalc(Sprite sprite, long baseDmg, Spell spell, double exp);
    long MonsterElementalDamageProc(Sprite sprite, long baseDmg, Spell spell, double exp);
    void ElementalOnSuccess(Sprite sprite, Sprite target, Spell spell, double exp);
    void ElementalOnFailed(Sprite sprite, Sprite target, Spell spell);
    void ElementalOnUse(Sprite sprite, Sprite target, Spell spell, double exp = 1);
    void AfflictionOnSuccess(Sprite sprite, Sprite target, Spell spell, Debuff debuff);
    void PoisonOnSuccess(Sprite sprite, Sprite target, Spell spell, Debuff debuff);
    void SpellOnSuccess(Sprite sprite, Sprite target, Spell spell);
    void SpellOnFailed(Sprite sprite, Sprite target, Spell spell);
    void AfflictionOnUse(Sprite sprite, Sprite target, Spell spell, Debuff debuff);
    void EnhancementOnUse(Sprite sprite, Sprite target, Spell spell, Buff buff);
    void EnhancementOnSuccess(Sprite sprite, Sprite target, Spell spell, Buff buff);
    void Step(Sprite sprite, int savedXStep, int savedYStep);
    int DistanceTo(Position spritePos, Position inputPos);
}