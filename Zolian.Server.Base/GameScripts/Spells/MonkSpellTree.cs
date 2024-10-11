using Darkages.GameScripts.Affects;
using Darkages.ScriptingBase;
using Darkages.Sprites;
using Darkages.Types;

namespace Darkages.GameScripts.Spells;

/// <summary>
/// Dion
/// </summary>
[Script("Dion")]
public class Dion(Spell spell) : SpellScript(spell)
{
    private readonly Buff _buff = new buff_dion();
    private readonly GlobalSpellMethods _spellMethod = new();

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target) { }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (target.Immunity)
        {
            if (sprite is not Aisling aisling) return;
            _spellMethod.Train(aisling.Client, Spell);
            aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "You've already cast that spell.");
            return;
        }

        _spellMethod.EnhancementOnUse(sprite, sprite is Monster ? sprite : target, Spell, _buff);
    }
}