using Darkages.GameScripts.Affects;
using Darkages.Scripting;
using Darkages.Sprites;
using Darkages.Types;

namespace Darkages.GameScripts.Spells;

/// <summary>
/// Dion
/// </summary>
[Script("Dion")]
public class Dion : SpellScript
{
    private readonly Spell _spell;
    private readonly Buff _buff = new buff_dion();
    private readonly GlobalSpellMethods _spellMethod;

    public Dion(Spell spell) : base(spell)
    {
        _spell = spell;
        _spellMethod = new GlobalSpellMethods();
    }

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target) { }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (target.Immunity)
        {
            if (sprite is Aisling aisling)
                aisling.Client.SendMessage(0x02, "Another spell of similar nature is already applied.");
            return;
        }

        _spellMethod.EnhancementOnUse(sprite, target, _spell, _buff);
    }
}