using Darkages.GameScripts.Affects;
using Darkages.Network.Server;
using Darkages.ScriptingBase;
using Darkages.Sprites;
using Darkages.Sprites.Entity;
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

[Script("Deep Sleep")]
public class DeepSleep(Spell spell) : SpellScript(spell)
{
    private readonly Debuff _debuff = new DebuffDeepSleep();
    private readonly GlobalSpellMethods _spellMethod = new();

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target) { }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (sprite is Aisling playerAction)
            playerAction.ActionUsed = "Deep Sleep";
        if (target == null) return;

        if (target.HasDebuff("Sleep") || target.HasDebuff("Deep Sleep"))
        {
            if (sprite is not Aisling aisling) return;
            if (aisling.CurrentMp - Spell.Template.ManaCost > 0)
            {
                aisling.CurrentMp -= Spell.Template.ManaCost;
                GlobalSpellMethods.Train(aisling.Client, Spell);
            }
            else
            {
                aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, $"{ServerSetup.Instance.Config.NoManaMessage}");
                return;
            }

            aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Another spell of similar nature is already applied.");
            return;
        }

        _spellMethod.AfflictionOnUse(sprite, target, Spell, _debuff);
    }
}

[Script("Comet")]
public class Comet(Spell spell) : SpellScript(spell)
{
    private readonly GlobalSpellMethods _spellMethod = new();

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target) { }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (sprite is Aisling playerAction)
            playerAction.ActionUsed = "Comet";

        if (target is not Damageable identifiable) return;
        var enemies = identifiable.DamageableGetBehind();
        var behindTwo = identifiable.DamageableGetBehind(2);
        var toSide = identifiable.GetInFrontToSide();
        var toSideTwo = identifiable.GetInFrontToSide(2);
        enemies.AddRange(behindTwo);
        enemies.AddRange(toSide);
        enemies.AddRange(toSideTwo);
        enemies.Add(target);

        foreach (var enemy in enemies.Where(e => e != null))
            _spellMethod.ElementalOnUse(sprite, enemy, Spell, GlobalSpellMethods.Tir);
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
            GlobalSpellMethods.EnhancementOnSuccess(sprite, player, Spell, _buff);
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
            GlobalSpellMethods.EnhancementOnSuccess(sprite, player, Spell, _buff);
        }
    }
}