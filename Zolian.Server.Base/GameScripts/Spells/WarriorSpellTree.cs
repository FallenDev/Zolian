using Darkages.GameScripts.Affects;
using Darkages.Scripting;
using Darkages.Sprites;
using Darkages.Types;

namespace Darkages.GameScripts.Spells;
// Berserker Spells

// Defender Spells
[Script("Asgall")]
public class Asgall : SpellScript
{
    private readonly Spell _spell;
    private readonly Buff _buff = new buff_skill_reflect();
    private readonly GlobalSpellMethods _spellMethod;

    public Asgall(Spell spell) : base(spell)
    {
        _spell = spell;
        _spellMethod = new GlobalSpellMethods();
    }

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target) { }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (!sprite.CanCast)
        {
            if (sprite is Aisling aisling)
                aisling.Client.SendMessage(0x02, "Incapacitated.");
            return;
        };

        if (sprite.HasBuff("Asgall"))
        {
            if (sprite is not Aisling aisling) return;
            _spellMethod.Train(aisling.Client, _spell);
            aisling.Client.SendMessage(0x02, "Another spell of similar nature is already applied.");
            return;
        }

        _spellMethod.EnhancementOnUse(sprite, target, _spell, _buff);
    }
}

[Script("Defensive Stance")]
public class Defensive_Stance : SpellScript
{
    private readonly Spell _spell;
    private readonly Buff _buff = new buff_DefenseUp();
    private readonly GlobalSpellMethods _spellMethod;

    public Defensive_Stance(Spell spell) : base(spell)
    {
        _spell = spell;
        _spellMethod = new GlobalSpellMethods();
    }

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target) { }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (!sprite.CanCast)
        {
            if (sprite is Aisling aisling)
                aisling.Client.SendMessage(0x02, "Incapacitated.");
            return;
        };

        if (sprite.HasBuff("Defensive Stance"))
        {
            if (sprite is not Aisling aisling) return;
            _spellMethod.Train(aisling.Client, _spell);
            aisling.Client.SendMessage(0x02, "Another spell of similar nature is already applied.");
            return;
        }

        _spellMethod.EnhancementOnUse(sprite, target, _spell, _buff);
    }
}

// Master Spells

[Script("Perfect Defense")]
public class Perfect_Defense : SpellScript
{
    private readonly Spell _spell;
    private readonly Buff _buff = new buff_PerfectDefense();
    private readonly GlobalSpellMethods _spellMethod;

    public Perfect_Defense(Spell spell) : base(spell)
    {
        _spell = spell;
        _spellMethod = new GlobalSpellMethods();
    }

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target) { }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (!sprite.CanCast)
        {
            if (sprite is Aisling aisling)
                aisling.Client.SendMessage(0x02, "Incapacitated.");
            return;
        };

        if (sprite.HasBuff("Perfect Defense") || sprite.HasBuff("Deireas Faileas"))
        {
            if (sprite is not Aisling aisling) return;
            _spellMethod.Train(aisling.Client, _spell);
            aisling.Client.SendMessage(0x02, "Another spell of similar nature is already applied.");
            return;
        }

        _spellMethod.EnhancementOnUse(sprite, target, _spell, _buff);
    }
}