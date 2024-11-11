using Darkages.GameScripts.Affects;
using Darkages.Network.Server;
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
    private Debuff _debuff;
    private readonly GlobalSpellMethods _spellMethod = new();

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target) { }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        var playersNearby = sprite.AislingsNearby();
        foreach (var player in playersNearby)
        {
            _debuff = new DebuffCriochCradh();
            if (player == null) continue;
            _spellMethod.AfflictionOnUse(sprite, player, Spell, _debuff);
            _spellMethod.ElementalOnUse(sprite, player, Spell, 1500);
        }
    }
}

[Script("Cataclysmic Hell")]
public class CataclysmicHell(Spell spell) : SpellScript(spell)
{
    private Debuff _debuff;
    private readonly GlobalSpellMethods _spellMethod = new();

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target) { }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        var playersNearby = sprite.AislingsNearby();
        foreach (var player in playersNearby)
        {
            _debuff = new DebuffSunSeal();
            if (player == null) continue;
            _spellMethod.AfflictionOnUse(sprite, player, Spell, _debuff);
            _spellMethod.ElementalOnUse(sprite, player, Spell, 5000);
        }
    }
}

[Script("Liquid Hell")]
public class LiquidHell(Spell spell) : SpellScript(spell)
{
    private Debuff _debuff;
    private readonly GlobalSpellMethods _spellMethod = new();

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target) { }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        var playersNearby = sprite.AislingsNearby();
        foreach (var player in playersNearby)
        {
            _debuff = new DebuffCriochArdCradh();
            if (player == null) continue;
            _spellMethod.AfflictionOnUse(sprite, player, Spell, _debuff);
            _spellMethod.ElementalOnUse(sprite, player, Spell, 3500);
        }
    }
}

[Script("Heavens Fall")]
public class Heavensfall(Spell spell) : SpellScript(spell)
{
    private Debuff _debuff;
    private readonly GlobalSpellMethods _spellMethod = new();

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target) { }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        var playersNearby = sprite.AislingsNearby();
        foreach (var player in playersNearby)
        {
            _debuff = new DebuffSunSeal();
            if (player == null) continue;
            _spellMethod.AfflictionOnUse(sprite, player, Spell, _debuff);
            _spellMethod.ElementalOnUse(sprite, player, Spell, 4000);
        }
    }
}

[Script("Double XP")]
public class DoubleXp(Spell spell) : SpellScript(spell)
{
    private Buff _buff;
    private readonly GlobalSpellMethods _spellMethod = new();

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target) { }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        foreach (var player in ServerSetup.Instance.Game.Aislings)
        {
            _buff = new BuffDoubleExperience();
            if (player == null) continue;
            if (!player.LoggedIn) continue;
            _spellMethod.EnhancementOnSuccess(sprite, player, Spell, _buff);
        }
    }
}

[Script("Triple XP")]
public class TripleXp(Spell spell) : SpellScript(spell)
{
    private Buff _buff;
    private readonly GlobalSpellMethods _spellMethod = new();

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target) { }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        foreach (var player in ServerSetup.Instance.Game.Aislings)
        {
            _buff = new BuffTripleExperience();
            if (player == null) continue;
            if (!player.LoggedIn) continue;
            _spellMethod.EnhancementOnSuccess(sprite, player, Spell, _buff);
        }
    }
}