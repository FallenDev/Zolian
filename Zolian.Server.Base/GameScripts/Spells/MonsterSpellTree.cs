﻿using Darkages.GameScripts.Affects;
using Darkages.ScriptingBase;
using Darkages.Sprites;
using Darkages.Types;

namespace Darkages.GameScripts.Spells;

[Script("Decay")]
public class Decay(Spell spell) : SpellScript(spell)
{
    private readonly Debuff _debuff = new DebuffDecay();
    private readonly GlobalSpellMethods _spellMethod = new();

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target) { }

    public override void OnUse(Sprite sprite, Sprite target) => _spellMethod.AfflictionOnUse(sprite, target, Spell, _debuff);
}

[Script("Omega Rising")]
public class OmegaRising(Spell spell) : SpellScript(spell)
{
    private readonly Debuff _debuff = new DebuffCriochCradh();
    private readonly GlobalSpellMethods _spellMethod = new();

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target) { }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        var playersNearby = sprite.AislingsNearby();
        foreach (var player in playersNearby)
        {
            if (player == null) continue;
            _spellMethod.AfflictionOnUse(sprite, player, Spell, _debuff);
            _spellMethod.ElementalOnUse(sprite, player, Spell, 700);
        }
    }
}

[Script("Liquid Hell")]
public class LiquidHell(Spell spell) : SpellScript(spell)
{
    private readonly Debuff _debuff = new DebuffCriochArdCradh();
    private readonly GlobalSpellMethods _spellMethod = new();

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target) { }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        var playersNearby = sprite.AislingsNearby();
        foreach (var player in playersNearby)
        {
            if (player == null) continue;
            _spellMethod.AfflictionOnUse(sprite, player, Spell, _debuff);
            _spellMethod.ElementalOnUse(sprite, player, Spell, 1500);
        }
    }
}

[Script("Heavens Fall")]
public class Heavensfall(Spell spell) : SpellScript(spell)
{
    private readonly Debuff _debuff = new DebuffSunSeal();
    private readonly GlobalSpellMethods _spellMethod = new();

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target) { }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        var playersNearby = sprite.AislingsNearby();
        foreach (var player in playersNearby)
        {
            if (player == null) continue;
            _spellMethod.AfflictionOnUse(sprite, player, Spell, _debuff);
            _spellMethod.ElementalOnUse(sprite, player, Spell, 2000);
        }
    }
}