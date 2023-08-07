using Darkages.GameScripts.Affects;
using Darkages.ScriptingBase;
using Darkages.Sprites;
using Darkages.Types;

namespace Darkages.GameScripts.Spells;

[Script("Decay")]
public class Decay : SpellScript
{
    private readonly Spell _spell;
    private readonly Debuff _debuff = new debuff_decay();
    private readonly GlobalSpellMethods _spellMethod;

    public Decay(Spell spell) : base(spell)
    {
        _spell = spell;
        _spellMethod = new GlobalSpellMethods();
    }

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target) { }

    public override void OnUse(Sprite sprite, Sprite target) => _spellMethod.AfflictionOnUse(sprite, target, _spell, _debuff);
}