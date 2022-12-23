using Darkages.GameScripts.Affects;
using Darkages.Scripting;
using Darkages.Sprites;
using Darkages.Types;

namespace Darkages.GameScripts.Spells;

[Script("Leap")]
public class Leap : SpellScript
{
    private readonly Spell _spell;
    private readonly GlobalSpellMethods _spellMethod;

    public Leap(Spell spell) : base(spell)
    {
        _spell = spell;
        _spellMethod = new GlobalSpellMethods();
    }

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target) { }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (!sprite.CanMove) return;



    }
}

/// <summary>
/// Aite
/// </summary>
[Script("Dia Aite")]
public class DiaAite : SpellScript
{
    private readonly Spell _spell;
    private readonly Buff _buff = new buff_DiaAite();
    private readonly GlobalSpellMethods _spellMethod;

    public DiaAite(Spell spell) : base(spell)
    {
        _spell = spell;
        _spellMethod = new GlobalSpellMethods();
    }

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target) { }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (target.HasBuff("Aite") || target.HasBuff("Dia Aite"))
        {
            if (sprite is Aisling aisling)
                aisling.Client.SendMessage(0x02, "Another spell of similar nature is already applied.");
            return;
        }

        _spellMethod.EnhancementOnUse(sprite, target, _spell, _buff);
    }
}
