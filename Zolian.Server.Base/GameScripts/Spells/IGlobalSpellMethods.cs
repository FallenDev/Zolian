using Darkages.Interfaces;
using Darkages.Sprites;
using Darkages.Types;

namespace Darkages.GameScripts.Spells
{
    public interface IGlobalSpellMethods
    {
        bool Execute(IGameClient client, Spell spell);
        void Train(IGameClient client, Spell spell);
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
}
