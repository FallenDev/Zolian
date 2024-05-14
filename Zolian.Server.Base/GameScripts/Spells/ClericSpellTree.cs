using Chaos.Common.Definitions;

using Darkages.Common;
using Darkages.Enums;
using Darkages.GameScripts.Affects;
using Darkages.Network.Client.Abstractions;
using Darkages.ScriptingBase;
using Darkages.Sprites;
using Darkages.Types;

using static ServiceStack.Diagnostics.Events;

using MapFlags = Darkages.Enums.MapFlags;

namespace Darkages.GameScripts.Spells;

/// <summary>
/// Spectral Shield: Bonus to AC
/// </summary>
[Script("Spectral Shield")]
public class Spectral_Shield(Spell spell) : SpellScript(spell)
{
    private readonly Buff _buff = new buff_SpectralShield();
    private readonly GlobalSpellMethods _spellMethod = new();

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target) { }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (target == null) return;

        if (target.HasBuff("Spectral Shield") || target.HasBuff("Defensive Stance"))
        {
            if (sprite is not Aisling aisling) return;
            if (aisling.CurrentMp - Spell.Template.ManaCost > 0)
            {
                aisling.CurrentMp -= Spell.Template.ManaCost;
                _spellMethod.Train(aisling.Client, Spell);
            }
            else
            {
                aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, $"{ServerSetup.Instance.Config.NoManaMessage}");
                return;
            }

            aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Another spell of similar nature is already applied.");
            return;
        }

        _spellMethod.EnhancementOnUse(sprite, sprite is Monster ? sprite : target, Spell, _buff);
    }
}

/// <summary>
/// Aite
/// </summary>
[Script("Aite")]
public class Aite(Spell spell) : SpellScript(spell)
{
    private readonly Buff _buff = new buff_aite();
    private readonly GlobalSpellMethods _spellMethod = new();

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target) { }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (target == null) return;

        if (target.HasBuff("Aite") || target.HasBuff("Dia Aite"))
        {
            if (sprite is not Aisling aisling) return;
            if (aisling.CurrentMp - Spell.Template.ManaCost > 0)
            {
                aisling.CurrentMp -= Spell.Template.ManaCost;
                _spellMethod.Train(aisling.Client, Spell);
            }
            else
            {
                aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, $"{ServerSetup.Instance.Config.NoManaMessage}");
                return;
            }

            aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Another spell of similar nature is already applied.");
            return;
        }

        _spellMethod.EnhancementOnUse(sprite, sprite is Monster ? sprite : target, Spell, _buff);
    }
}

/// <summary>
/// Mor Dion
/// </summary>
[Script("Mor Dion")]
public class Mor_Dion(Spell spell) : SpellScript(spell)
{
    private readonly Buff _buff = new buff_MorDion();
    private readonly GlobalSpellMethods _spellMethod = new();

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target) { }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (target.Immunity)
        {
            if (sprite is not Aisling aisling) return;
            if (aisling.CurrentMp - Spell.Template.ManaCost > 0)
            {
                aisling.CurrentMp -= Spell.Template.ManaCost;
                _spellMethod.Train(aisling.Client, Spell);
            }
            else
            {
                aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, $"{ServerSetup.Instance.Config.NoManaMessage}");
                return;
            }

            aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Another spell of similar nature is already applied.");
            return;
        }

        _spellMethod.EnhancementOnUse(sprite, sprite is Monster ? sprite : target, Spell, _buff);
    }
}

/// <summary>
/// Dark Chain: Stun and Slight Damage
/// </summary>
[Script("Dark Chain")]
public class Dark_Chain(Spell spell) : SpellScript(spell)
{
    private readonly Debuff _debuff = new DebuffDarkChain();
    private readonly GlobalSpellMethods _spellMethod = new();

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target) { }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (sprite is Aisling playerAction)
            playerAction.ActionUsed = "Dark Chain";
        if (target == null) return;

        _spellMethod.ElementalOnUse(sprite, target, Spell, 60);
        _spellMethod.AfflictionOnUse(sprite, target, Spell, _debuff);
    }
}

/// <summary>
/// Halt: Stop time for a target
/// </summary>
[Script("Halt")]
public class Halt(Spell spell) : SpellScript(spell)
{
    private readonly Debuff _debuff = new DebuffHalt();
    private readonly GlobalSpellMethods _spellMethod = new();

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target) { }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (sprite is Aisling playerAction)
            playerAction.ActionUsed = "Halt";
        if (target == null) return;

        _spellMethod.AfflictionOnUse(sprite, target, Spell, _debuff);
    }
}

[Script("Pramh")]
public class Pramh(Spell spell) : SpellScript(spell)
{
    private readonly Debuff _debuff = new DebuffSleep();
    private readonly GlobalSpellMethods _spellMethod = new();

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target) { }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (sprite is Aisling playerAction)
            playerAction.ActionUsed = "Pramh";
        if (target == null) return;

        if (target.HasDebuff("Sleep"))
        {
            if (sprite is not Aisling aisling) return;
            if (aisling.CurrentMp - Spell.Template.ManaCost > 0)
            {
                aisling.CurrentMp -= Spell.Template.ManaCost;
                _spellMethod.Train(aisling.Client, Spell);
            }
            else
            {
                aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, $"{ServerSetup.Instance.Config.NoManaMessage}");
                return;
            }

            aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Another spell of similar nature is already applied.");
            return;
        };

        _spellMethod.AfflictionOnUse(sprite, target, Spell, _debuff);
    }
}

/// <summary>
/// Detect: See monsters Advanced Stats
/// </summary>
[Script("Detect")]
public class Detect(Spell spell) : SpellScript(spell)
{
    private readonly GlobalSpellMethods _spellMethod = new();

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target)
    {
        if (sprite is not Aisling aisling) return;
        if (target is not Monster monster) return;
        var client = aisling.Client;

        if (target.CurrentHp > 0)
        {
            aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(Spell.Template.TargetAnimation, null, target.Serial));
            aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendSound(Spell.Template.Sound, false));
        }
        else
        {
            aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(Spell.Template.TargetAnimation, target.Position));
            return;
        }

        var title = $"{"{=bDetect",33}";
        var colorA = "";
        var colorB = "";
        var colorLvl = LevelColor(client, monster);
        var halfHp = $"{{=s{monster.CurrentHp}";
        var halfGone = monster.MaximumHp * .5;

        colorA = monster.OffenseElement switch
        {
            ElementManager.Element.Void => "{=n",
            ElementManager.Element.Holy => "{=g",
            ElementManager.Element.None => "{=s",
            ElementManager.Element.Fire => "{=b",
            ElementManager.Element.Water => "{=e",
            ElementManager.Element.Wind => "{=c",
            ElementManager.Element.Earth => "{=d",
            ElementManager.Element.Terror => "{=j",
            ElementManager.Element.Rage => "{=j",
            ElementManager.Element.Sorrow => "{=j",
            _ => colorA
        };

        colorB = monster.DefenseElement switch
        {
            ElementManager.Element.Void => "{=n",
            ElementManager.Element.Holy => "{=g",
            ElementManager.Element.None => "{=s",
            ElementManager.Element.Fire => "{=b",
            ElementManager.Element.Water => "{=e",
            ElementManager.Element.Wind => "{=c",
            ElementManager.Element.Earth => "{=d",
            ElementManager.Element.Terror => "{=j",
            ElementManager.Element.Rage => "{=j",
            ElementManager.Element.Sorrow => "{=j",
            _ => colorB
        };

        if (monster.CurrentHp < halfGone)
        {
            halfHp = $"{{=b{monster.CurrentHp}{{=s";
        }

        switch (Spell.Level)
        {
            case < 10:
                aisling.Client.SendServerMessage(ServerMessageType.ScrollWindow, $"{title}\n\n{{=aLv: {colorLvl} {{=aHP: {halfHp}/{monster.MaximumHp} {{=aO: {colorA}{monster.OffenseElement} {{=aD: {colorB}{monster.DefenseElement}");
                break;
            case >= 11 and <= 40:
                aisling.Client.SendServerMessage(ServerMessageType.ScrollWindow, $"{title}\n\n{{=aLv: {colorLvl} {{=aHP: {halfHp}/{monster.MaximumHp} {{=aO: {colorA}{monster.OffenseElement} {{=aD: {colorB}{monster.DefenseElement}\n" + $"{{=c{monster.Template.BaseName} {{=aSize: {{=s{monster.Size} {{=aAC: {{=s{monster.SealedAc}");
                break;
            default:
                aisling.Client.SendServerMessage(ServerMessageType.ScrollWindow, $"{title}\n\n{{=aLv: {colorLvl} {{=aHP: {halfHp}/{monster.MaximumHp} {{=aO: {colorA}{monster.OffenseElement} {{=aD: {colorB}{monster.DefenseElement}\n" + $"{{=c{monster.Template.BaseName} {{=aSize: {{=s{monster.Size} {{=aAC: {{=s{monster.SealedAc} {{=aRegen: {{=s{monster.Regen}\n" + $"{{=aSTR:{{=s{monster.Str} {{=aINT:{{=s{monster.Int} {{=aWIS:{{=s{monster.Wis} {{=aCON:{{=s{monster.Con} {{=aDEX:{{=s{monster.Dex}\n" + $"{{=aRace:{{=s{monster.Template.MonsterRace} {{=aFortitude:{{=s{monster.Fortitude} {{=aReflex:{{=s{monster.Reflex} {{=aWill:{{=s{monster.Will}");
                break;
        }
    }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (target == null) return;
        if (!Spell.CanUse())
        {
            if (sprite is Aisling aisling)
                aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Ability is not quite ready yet.");
            return;
        }

        var mR = Generator.RandNumGen100();

        if (mR > target.Will)
        {
            if (sprite is not Aisling aisling) return;
            var client = aisling.Client;

            if (aisling.CurrentMp - Spell.Template.ManaCost > 0)
            {
                aisling.CurrentMp -= Spell.Template.ManaCost;
                _spellMethod.Train(client, Spell);
            }
            else
            {
                client.SendServerMessage(ServerMessageType.OrangeBar1, $"{ServerSetup.Instance.Config.NoManaMessage}");
                return;
            }

            if (aisling.CurrentMp < 0)
                aisling.CurrentMp = 0;

            var success = _spellMethod.Execute(client, Spell);

            if (success)
            {
                OnSuccess(sprite, target);
            }
            else
            {
                _spellMethod.SpellOnFailed(aisling, target, Spell);
            }

            client.SendAttributes(StatUpdateType.Vitality);
        }
        else
        {
            if (sprite is Aisling caster)
            {
                caster.Client.Aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(115, null, target.Serial));
                return;
            }

            _spellMethod.SpellOnFailed(sprite, target, Spell);
        }
    }

    private static string LevelColor(IWorldClient client, Monster monster)
    {
        if (monster.Template.Level >= client.Aisling.Level + 30)
            return "{=n???{=s";
        if (monster.Template.Level >= client.Aisling.Level + 15)
            return $"{{=b{monster.Template.Level}{{=s";
        if (monster.Template.Level >= client.Aisling.Level + 10)
            return $"{{=c{monster.Template.Level}{{=s";
        if (monster.Template.Level <= client.Aisling.Level - 30)
            return $"{{=k{monster.Template.Level}{{=s";
        if (monster.Template.Level <= client.Aisling.Level - 15)
            return $"{{=j{monster.Template.Level}{{=s";
        return monster.Template.Level <= client.Aisling.Level - 10 ? $"{{=i{monster.Template.Level}{{=s" : $"{{=q{monster.Template.Level}{{=s";
    }
}

/// <summary>
/// Heal Minor Wounds: Heals 15% of target's health
/// </summary>
[Script("Heal Minor Wounds")]
public class Heal_Minor(Spell spell) : SpellScript(spell)
{
    private readonly GlobalSpellMethods _spellMethod = new();

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target)
    {
        if (sprite is Aisling aisling)
        {
            if (target == null) return;

            if (target.CurrentHp > 0)
            {
                aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(Spell.Template.TargetAnimation, null, target.Serial));
                aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, client => client.SendSound(Spell.Template.Sound, false));

                var healBase = target.MaximumHp * 0.15;
                aisling.ThreatMeter += (long)healBase;
                target.CurrentHp += (long)healBase;

                if (target.CurrentHp > target.MaximumHp)
                    target.CurrentHp = target.MaximumHp;

                aisling.Client.SendHealthBar(target, Spell.Template.Sound);
                if (target is Aisling targetAisling)
                    targetAisling.Client.SendAttributes(StatUpdateType.FullVitality);
            }
            else
            {
                aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(Spell.Template.TargetAnimation, target.Position));
            }
        }
        else
        {
            sprite.PlayerNearby?.SendTargetedClientMethod(PlayerScope.NearbyAislings, client => client.SendAnimation(Spell.Template.TargetAnimation, null, target.Serial));
            sprite.PlayerNearby?.SendTargetedClientMethod(PlayerScope.NearbyAislings, client => client.SendBodyAnimation(sprite.Serial, BodyAnimation.Assail, 30));
            sprite.PlayerNearby?.SendTargetedClientMethod(PlayerScope.NearbyAislings, client => client.SendSound(Spell.Template.Sound, false));

            var healBase = (long)(sprite.BaseHp * 0.10);

            if (sprite.CurrentHp >= sprite.MaximumHp) return;
            sprite.CurrentHp += healBase;

            if (sprite.CurrentHp > sprite.MaximumHp)
                sprite.CurrentHp = sprite.MaximumHp;
        }
    }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (target == null) return;
        if (!Spell.CanUse())
        {
            if (sprite is Aisling aisling2)
                aisling2.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Ability is not quite ready yet.");
            return;
        }

        if (sprite is Aisling aisling)
        {
            var client = aisling.Client;

            if (aisling.CurrentMp - Spell.Template.ManaCost > 0)
            {
                aisling.CurrentMp -= Spell.Template.ManaCost;
                _spellMethod.Train(client, Spell);
            }
            else
            {
                client.SendServerMessage(ServerMessageType.OrangeBar1, $"{ServerSetup.Instance.Config.NoManaMessage}");
                return;
            }

            if (aisling.CurrentMp < 0)
                aisling.CurrentMp = 0;

            var success = _spellMethod.Execute(client, Spell);

            if (success)
            {
                OnSuccess(sprite, target);
            }
            else
            {
                _spellMethod.SpellOnFailed(aisling, target, Spell);
            }

            client.SendAttributes(StatUpdateType.Vitality);
        }
        else
        {
            if (sprite.CurrentMp - Spell.Template.ManaCost > 0)
            {
                sprite.CurrentMp -= Spell.Template.ManaCost;
            }

            if (sprite.CurrentMp < 0)
                sprite.CurrentMp = 0;

            OnSuccess(sprite, sprite);
        }
    }
}

/// <summary>
/// Heal Major Wounds: Heals 30% of target's health
/// </summary>
[Script("Heal Major Wounds")]
public class Heal_Major(Spell spell) : SpellScript(spell)
{
    private readonly GlobalSpellMethods _spellMethod = new();

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target)
    {
        if (sprite is Aisling aisling)
        {
            if (target == null) return;

            if (target.CurrentHp > 0)
            {
                aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(Spell.Template.TargetAnimation, null, target.Serial));
                aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, client => client.SendSound(Spell.Template.Sound, false));

                var healBase = target.MaximumHp * 0.30;
                aisling.ThreatMeter += (long)healBase;
                target.CurrentHp += (long)healBase;

                if (target.CurrentHp > target.MaximumHp)
                    target.CurrentHp = target.MaximumHp;

                aisling.Client.SendHealthBar(target, Spell.Template.Sound);
                if (target is Aisling targetAisling)
                    targetAisling.Client.SendAttributes(StatUpdateType.FullVitality);
            }
            else
            {
                aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(Spell.Template.TargetAnimation, target.Position));
            }
        }
        else
        {
            sprite.PlayerNearby?.SendTargetedClientMethod(PlayerScope.NearbyAislings, client => client.SendAnimation(Spell.Template.TargetAnimation, null, target.Serial));
            sprite.PlayerNearby?.SendTargetedClientMethod(PlayerScope.NearbyAislings, client => client.SendBodyAnimation(sprite.Serial, BodyAnimation.Assail, 30));
            sprite.PlayerNearby?.SendTargetedClientMethod(PlayerScope.NearbyAislings, client => client.SendSound(Spell.Template.Sound, false));

            var healBase = (long)(sprite.BaseHp * 0.25);

            if (sprite.CurrentHp >= sprite.MaximumHp) return;
            sprite.CurrentHp += healBase;

            if (sprite.CurrentHp > sprite.MaximumHp)
                sprite.CurrentHp = sprite.MaximumHp;
        }
    }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (target == null) return;
        if (!Spell.CanUse())
        {
            if (sprite is Aisling aisling2)
                aisling2.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Ability is not quite ready yet.");
            return;
        }

        if (sprite is Aisling aisling)
        {
            var client = aisling.Client;

            if (aisling.CurrentMp - Spell.Template.ManaCost > 0)
            {
                aisling.CurrentMp -= Spell.Template.ManaCost;
                _spellMethod.Train(client, Spell);
            }
            else
            {
                client.SendServerMessage(ServerMessageType.OrangeBar1, $"{ServerSetup.Instance.Config.NoManaMessage}");
                return;
            }

            if (aisling.CurrentMp < 0)
                aisling.CurrentMp = 0;

            var success = _spellMethod.Execute(client, Spell);

            if (success)
            {
                OnSuccess(sprite, target);
            }
            else
            {
                _spellMethod.SpellOnFailed(aisling, target, Spell);
            }

            client.SendAttributes(StatUpdateType.Vitality);
        }
        else
        {
            if (sprite.CurrentMp - Spell.Template.ManaCost > 0)
            {
                sprite.CurrentMp -= Spell.Template.ManaCost;
            }

            if (sprite.CurrentMp < 0)
                sprite.CurrentMp = 0;

            OnSuccess(sprite, sprite);
        }
    }
}

/// <summary>
/// Heal Critical Wounds: Heals 65% of target's health
/// </summary>
[Script("Heal Critical Wounds")]
public class Heal_Critical(Spell spell) : SpellScript(spell)
{
    private readonly GlobalSpellMethods _spellMethod = new();

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target)
    {
        if (sprite is Aisling aisling)
        {
            if (target == null) return;

            if (target.CurrentHp > 0)
            {
                aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(Spell.Template.TargetAnimation, null, target.Serial));
                aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, client => client.SendSound(Spell.Template.Sound, false));

                var healBase = target.MaximumHp * 0.65;
                aisling.ThreatMeter += (long)healBase;
                target.CurrentHp += (long)healBase;

                if (target.CurrentHp > target.MaximumHp)
                    target.CurrentHp = target.MaximumHp;

                aisling.Client.SendHealthBar(target, Spell.Template.Sound);
                if (target is Aisling targetAisling)
                    targetAisling.Client.SendAttributes(StatUpdateType.FullVitality);
            }
            else
            {
                aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(Spell.Template.TargetAnimation, target.Position));
            }
        }
        else
        {
            sprite.PlayerNearby?.SendTargetedClientMethod(PlayerScope.NearbyAislings, client => client.SendAnimation(Spell.Template.TargetAnimation, null, target.Serial));
            sprite.PlayerNearby?.SendTargetedClientMethod(PlayerScope.NearbyAislings, client => client.SendBodyAnimation(sprite.Serial, BodyAnimation.Assail, 30));
            sprite.PlayerNearby?.SendTargetedClientMethod(PlayerScope.NearbyAislings, client => client.SendSound(Spell.Template.Sound, false));

            var healBase = (long)(sprite.BaseHp * 0.45);

            if (sprite.CurrentHp >= sprite.MaximumHp) return;
            sprite.CurrentHp += healBase;

            if (sprite.CurrentHp > sprite.MaximumHp)
                sprite.CurrentHp = sprite.MaximumHp;
        }
    }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (target == null) return;
        if (!Spell.CanUse())
        {
            if (sprite is Aisling aisling2)
                aisling2.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Ability is not quite ready yet.");
            return;
        }

        if (sprite is Aisling aisling)
        {
            var client = aisling.Client;

            if (aisling.CurrentMp - Spell.Template.ManaCost > 0)
            {
                aisling.CurrentMp -= Spell.Template.ManaCost;
                _spellMethod.Train(client, Spell);
            }
            else
            {
                client.SendServerMessage(ServerMessageType.OrangeBar1, $"{ServerSetup.Instance.Config.NoManaMessage}");
                return;
            }

            if (aisling.CurrentMp < 0)
                aisling.CurrentMp = 0;

            var success = _spellMethod.Execute(client, Spell);

            if (success)
            {
                OnSuccess(sprite, target);
            }
            else
            {
                _spellMethod.SpellOnFailed(aisling, target, Spell);
            }

            client.SendAttributes(StatUpdateType.Vitality);
        }
        else
        {
            if (sprite.CurrentMp - Spell.Template.ManaCost > 0)
            {
                sprite.CurrentMp -= Spell.Template.ManaCost;
            }

            if (sprite.CurrentMp < 0)
                sprite.CurrentMp = 0;

            OnSuccess(sprite, sprite);
        }
    }
}

/// <summary>
/// Dire Aid: Heals 70% of target's health, casts spectral shield if not cast
/// </summary>
[Script("Dire Aid")]
public class Dire_Aid(Spell spell) : SpellScript(spell)
{
    private readonly Buff _buff = new buff_SpectralShield();
    private readonly GlobalSpellMethods _spellMethod = new();

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target)
    {
        if (sprite is Aisling aisling)
        {
            if (target == null) return;

            if (target.CurrentHp > 0)
            {
                aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(Spell.Template.TargetAnimation, null, target.Serial));
                aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, client => client.SendSound(Spell.Template.Sound, false));

                if (target.HasBuff("Spectral Shield") || target.HasBuff("Defensive Stance"))
                {
                    if (target is Aisling)
                        aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Another spell of similar nature is applied.");
                }
                else
                {
                    _spellMethod.EnhancementOnUse(sprite, target, Spell, _buff);
                }

                var healBase = target.MaximumHp * 0.80;
                aisling.ThreatMeter += (long)healBase;
                target.CurrentHp += (long)healBase;

                if (target.CurrentHp > target.MaximumHp)
                    target.CurrentHp = target.MaximumHp;

                aisling.Client.SendHealthBar(target, Spell.Template.Sound);
                if (target is Aisling targetAisling)
                    targetAisling.Client.SendAttributes(StatUpdateType.FullVitality);
            }
            else
            {
                aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(Spell.Template.TargetAnimation, target.Position));
            }
        }
        else
        {
            sprite.PlayerNearby?.SendTargetedClientMethod(PlayerScope.NearbyAislings, client => client.SendAnimation(Spell.Template.TargetAnimation, null, target.Serial));
            sprite.PlayerNearby?.SendTargetedClientMethod(PlayerScope.NearbyAislings, client => client.SendBodyAnimation(sprite.Serial, BodyAnimation.Assail, 30));
            sprite.PlayerNearby?.SendTargetedClientMethod(PlayerScope.NearbyAislings, client => client.SendSound(Spell.Template.Sound, false));

            var healBase = (long)(sprite.BaseHp * 0.70);

            if (sprite.CurrentHp >= sprite.MaximumHp) return;
            sprite.CurrentHp += healBase;

            if (sprite.CurrentHp > sprite.MaximumHp)
                sprite.CurrentHp = sprite.MaximumHp;
        }
    }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (target == null) return;
        if (!Spell.CanUse())
        {
            if (sprite is Aisling aisling2)
                aisling2.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Ability is not quite ready yet.");
            return;
        }

        if (sprite is Aisling aisling)
        {
            var client = aisling.Client;

            if (aisling.CurrentMp - Spell.Template.ManaCost > 0)
            {
                aisling.CurrentMp -= Spell.Template.ManaCost;
                _spellMethod.Train(client, Spell);
            }
            else
            {
                client.SendServerMessage(ServerMessageType.OrangeBar1, $"{ServerSetup.Instance.Config.NoManaMessage}");
                return;
            }

            if (aisling.CurrentMp < 0)
                aisling.CurrentMp = 0;

            var success = _spellMethod.Execute(client, Spell);

            if (success)
            {
                OnSuccess(sprite, target);
            }
            else
            {
                _spellMethod.SpellOnFailed(aisling, target, Spell);
            }

            client.SendAttributes(StatUpdateType.Vitality);
        }
        else
        {
            if (sprite.CurrentMp - Spell.Template.ManaCost > 0)
            {
                sprite.CurrentMp -= Spell.Template.ManaCost;
            }

            if (sprite.CurrentMp < 0)
                sprite.CurrentMp = 0;

            OnSuccess(sprite, sprite);
        }
    }
}

/// <summary>
/// Healing Winds: Heal group members based on current health
/// </summary>
[Script("Healing Winds")]
public class Healing_Winds(Spell spell) : SpellScript(spell)
{
    private readonly GlobalSpellMethods _spellMethod = new();

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target)
    {
        if (sprite is not Aisling aisling) return;

        if (target.CurrentHp > 0)
        {
            aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(Spell.Template.TargetAnimation, null, target.Serial));
            aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, client => client.SendSound(Spell.Template.Sound, false));
        }
        else
        {
            aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(Spell.Template.TargetAnimation, target.Position));
        }

        var healBase = aisling.MaximumHp * 0.25;

        if (aisling.GroupId != 0)
        {
            foreach (var partyMember in aisling.AislingsNearby().Where(i => i.GroupId == aisling.GroupId))
            {
                if (partyMember.Dead) continue;
                aisling.ThreatMeter += (long)healBase;
                partyMember.CurrentHp += (long)healBase;

                if (partyMember.CurrentHp > partyMember.MaximumHp)
                    partyMember.CurrentHp = partyMember.MaximumHp;

                partyMember.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendHealthBar(partyMember, 8));
                partyMember.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(267, null, partyMember.Serial));
                partyMember.Client.SendAttributes(StatUpdateType.Vitality);
            }
        }
        else
        {
            aisling.CurrentHp += (long)healBase;
            if (aisling.CurrentHp > aisling.MaximumHp)
                aisling.CurrentHp = aisling.MaximumHp;

            aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendHealthBar(aisling, 8));
            aisling.Client.SendAttributes(StatUpdateType.Vitality);
        }
    }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (target == null) return;
        if (!Spell.CanUse())
        {
            if (sprite is Aisling aisling2)
                aisling2.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Ability is not quite ready yet.");
            return;
        }

        if (sprite is Aisling aisling)
        {
            var client = aisling.Client;

            if (aisling.CurrentMp - Spell.Template.ManaCost > 0)
            {
                aisling.CurrentMp -= Spell.Template.ManaCost;
                _spellMethod.Train(client, Spell);
            }
            else
            {
                client.SendServerMessage(ServerMessageType.OrangeBar1, $"{ServerSetup.Instance.Config.NoManaMessage}");
                return;
            }

            if (aisling.CurrentMp < 0)
                aisling.CurrentMp = 0;

            var success = _spellMethod.Execute(client, Spell);

            if (success)
            {
                OnSuccess(sprite, target);
            }
            else
            {
                _spellMethod.SpellOnFailed(aisling, target, Spell);
            }

            client.SendAttributes(StatUpdateType.Vitality);
        }
        else
        {
            if (sprite.CurrentMp - Spell.Template.ManaCost > 0)
            {
                sprite.CurrentMp -= Spell.Template.ManaCost;
            }

            if (sprite.CurrentMp < 0)
                sprite.CurrentMp = 0;

            OnSuccess(sprite, sprite);
        }
    }
}

/// <summary>
/// Forestall: Remove debuff "Skulling" and prevent death
/// </summary>
[Script("Forestall")]
public class Forestall(Spell spell) : SpellScript(spell)
{
    private readonly GlobalSpellMethods _spellMethod = new();

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target)
    {
        if (sprite is not Aisling aisling) return;
        if (target is not Aisling savedAisling) return;
        if (target.CurrentHp > 0)
        {
            aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(Spell.Template.TargetAnimation, null, target.Serial));
            aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, client => client.SendSound(Spell.Template.Sound, false));
        }
        else
        {
            aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(Spell.Template.TargetAnimation, target.Position));
        }

        if (!savedAisling.Skulled) return;
        savedAisling.Debuffs.TryGetValue("Skulled", out var debuff);
        if (debuff == null) return;
        debuff.Cancelled = true;
        debuff.OnEnded(savedAisling, debuff);
        savedAisling.Client.Revive();
    }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (sprite is not Aisling aisling) return;
        var client = aisling.Client;
        if (target == null) return;
        if (!Spell.CanUse())
        {
            if (sprite is Aisling aisling2)
                aisling2.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Ability is not quite ready yet.");
            return;
        }

        if (aisling.CurrentMp - Spell.Template.ManaCost > 0)
        {
            aisling.CurrentMp -= Spell.Template.ManaCost;
            _spellMethod.Train(client, Spell);
        }
        else
        {
            client.SendServerMessage(ServerMessageType.OrangeBar1, $"{ServerSetup.Instance.Config.NoManaMessage}");
            return;
        }

        if (aisling.CurrentMp < 0)
            aisling.CurrentMp = 0;

        OnSuccess(sprite, target);
        client.SendAttributes(StatUpdateType.Vitality);
    }
}

/// <summary>
/// Hell Grasp: Pull ally to the realm of the living
/// </summary>
[Script("Hell Grasp")]
public class Raise_Ally(Spell spell) : SpellScript(spell)
{
    private readonly GlobalSpellMethods _spellMethod = new();

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target)
    {
        if (sprite is not Aisling aisling) return;
        if (target.CurrentHp > 0)
        {
            aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(Spell.Template.TargetAnimation, null, target.Serial));
            aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, client => client.SendSound(Spell.Template.Sound, false));
        }
        else
        {
            aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(Spell.Template.TargetAnimation, target.Position));
        }

        if (aisling.GroupId != 0 && aisling.GroupParty != null)
        {
            foreach (var deadPartyMember in aisling.GroupParty.PartyMembers.Values.Where(m => m is { Dead: true }))
            {
                deadPartyMember.Client.Revive();
                deadPartyMember.Client.SendServerMessage(ServerMessageType.OrangeBar1, "I live again.");
                deadPartyMember.Client.SendAttributes(StatUpdateType.Full);
                deadPartyMember.Client.TransitionToMap(aisling.CurrentMapId, new Position(aisling.X, aisling.Y));
                Task.Delay(350).ContinueWith(ct => { deadPartyMember.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(304, null, deadPartyMember.Serial)); });
                break;
            }
        }

        if (aisling.Map.Flags.MapFlagIsSet(MapFlags.Snow))
        {
            Task.Delay(30000).ContinueWith(ct =>
            {
                if (aisling.Map.Flags.MapFlagIsSet(MapFlags.Snow))
                    aisling.Map.Flags &= ~MapFlags.Snow;

                var players = aisling.AislingsNearby();
                foreach (var player in players)
                {
                    player.Client.ClientRefreshed();
                }
            });
        }
    }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (sprite is not Aisling aisling) return;
        if (target == null) return;
        if (!Spell.CanUse())
        {
            if (sprite is Aisling aisling2)
                aisling2.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Ability is not quite ready yet.");
            return;
        }

        var client = aisling.Client;
        _spellMethod.Train(client, Spell);

        if (aisling.CurrentMp - Spell.Template.ManaCost > 0)
        {
            aisling.CurrentMp = 0;
        }
        else
        {
            client.SendServerMessage(ServerMessageType.OrangeBar1, "Not enough ether to perform such a divine task.");
            return;
        }

        aisling.Map.Flags |= MapFlags.Snow;

        var players = aisling.AislingsNearby();
        foreach (var player in players)
        {
            player.Client.ClientRefreshed();
        }

        if (aisling.CurrentMp < 0)
            aisling.CurrentMp = 0;

        OnSuccess(sprite, target);
        client.SendAttributes(StatUpdateType.Vitality);
    }
}

/// <summary>
/// Turn Undead: Rends 10 AC, Slight Holy Damage, Turns Undead non-hostile, level up to 100
/// </summary>
[Script("Turn Undead")]
public class Turn_Undead(Spell spell) : SpellScript(spell)
{
    private readonly Debuff _debuff = new DebuffRending();
    private readonly GlobalSpellMethods _spellMethod = new();

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target)
    {
        if (sprite is not Aisling aisling) return;

        if (target.CurrentHp > 0)
        {
            aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(Spell.Template.TargetAnimation, null, target.Serial));
            aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, client => client.SendSound(Spell.Template.Sound, false));
        }
        else
        {
            aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(Spell.Template.TargetAnimation, target.Position));
        }

        foreach (var monster in aisling.MonstersNearby())
        {
            if (!monster.Template.MonsterRace.MonsterRaceIsSet(MonsterRace.Undead)) continue;
            if (monster.Level >= 101) continue;
            _spellMethod.ElementalOnUse(sprite, target, Spell, 30);
            _spellMethod.AfflictionOnUse(sprite, target, Spell, _debuff);
            monster.Target = null;
            monster.Aggressive = false;
        }
    }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (sprite is not Aisling playerAction) return;
        if (target == null) return;
        if (!Spell.CanUse())
        {
            if (sprite is Aisling aisling2)
                aisling2.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Ability is not quite ready yet.");
            return;
        }

        playerAction.ActionUsed = "Turn Undead";
        var client = playerAction.Client;
        _spellMethod.Train(client, Spell);
        var success = _spellMethod.Execute(client, Spell);
        var mR = Generator.RandNumGen100();

        if (mR > target.Will)
        {
            if (success)
            {
                OnSuccess(sprite, target);
            }
            else
            {
                _spellMethod.SpellOnFailed(playerAction, target, Spell);
            }
        }
        else
        {
            playerAction.Client.Aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(115, null, target.Serial));
        }
    }
}

/// <summary>
/// Turn Critter: Rends 10 AC, Slight Holy Damage, Turns Critters non-hostile, level up to 100
/// </summary>
[Script("Turn Critter")]
public class Turn_Critter(Spell spell) : SpellScript(spell)
{
    private readonly Debuff _debuff = new DebuffRending();
    private readonly GlobalSpellMethods _spellMethod = new();

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target)
    {
        if (sprite is not Aisling aisling) return;

        if (target.CurrentHp > 0)
        {
            aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(Spell.Template.TargetAnimation, null, target.Serial));
            aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, client => client.SendSound(Spell.Template.Sound, false));
        }
        else
        {
            aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(Spell.Template.TargetAnimation, target.Position));
        }

        foreach (var monster in aisling.MonstersNearby())
        {
            if (!monster.Template.MonsterRace.MonsterRaceIsSet(MonsterRace.LowerBeing)) continue;
            if (monster.Level >= 101) continue;
            _spellMethod.ElementalOnUse(sprite, target, Spell, 30);
            _spellMethod.AfflictionOnUse(sprite, target, Spell, _debuff);
            monster.Target = null;
            monster.Aggressive = false;
        }
    }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (sprite is not Aisling playerAction) return;
        if (target == null) return;
        if (!Spell.CanUse())
        {
            if (sprite is Aisling aisling2)
                aisling2.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Ability is not quite ready yet.");
            return;
        }

        playerAction.ActionUsed = "Turn Critter";
        var client = playerAction.Client;
        _spellMethod.Train(client, Spell);
        var success = _spellMethod.Execute(client, Spell);
        var mR = Generator.RandNumGen100();

        if (mR > target.Will)
        {
            if (success)
            {
                OnSuccess(sprite, target);
            }
            else
            {
                _spellMethod.SpellOnFailed(playerAction, target, Spell);
            }
        }
        else
        {
            playerAction.Client.Aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(115, null, target.Serial));
        }
    }
}

/// <summary>
/// Turn Greater Undead: Rends 10 AC, Holy Damage, Turns Undead non-hostile, level up to 250
/// </summary>
[Script("Turn Greater Undead")]
public class Turn_Greater_Undead(Spell spell) : SpellScript(spell)
{
    private readonly Debuff _debuff = new DebuffRending();
    private readonly GlobalSpellMethods _spellMethod = new();

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target)
    {
        if (sprite is not Aisling aisling) return;

        if (target.CurrentHp > 0)
        {
            aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(Spell.Template.TargetAnimation, null, target.Serial));
            aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, client => client.SendSound(Spell.Template.Sound, false));
        }
        else
        {
            aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(Spell.Template.TargetAnimation, target.Position));
        }

        foreach (var monster in aisling.MonstersNearby())
        {
            if (!monster.Template.MonsterRace.MonsterRaceIsSet(MonsterRace.Undead)) continue;
            if (monster.Level >= 251) continue;
            _spellMethod.ElementalOnUse(sprite, target, Spell, 50);
            _spellMethod.AfflictionOnUse(sprite, target, Spell, _debuff);
            monster.Target = null;
            monster.Aggressive = false;
        }
    }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (sprite is not Aisling playerAction) return;
        if (target == null) return;
        if (!Spell.CanUse())
        {
            if (sprite is Aisling aisling2)
                aisling2.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Ability is not quite ready yet.");
            return;
        }

        playerAction.ActionUsed = "Turn Undead";
        var client = playerAction.Client;
        _spellMethod.Train(client, Spell);
        var success = _spellMethod.Execute(client, Spell);
        var mR = Generator.RandNumGen100();

        if (mR > target.Will)
        {
            if (success)
            {
                OnSuccess(sprite, target);
            }
            else
            {
                _spellMethod.SpellOnFailed(playerAction, target, Spell);
            }
        }
        else
        {
            playerAction.Client.Aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(115, null, target.Serial));
        }
    }
}

/// <summary>
/// Turn Greater Critter: Rends 10 AC, Holy Damage, Turns Critters non-hostile, level up to 250
/// </summary>
[Script("Turn Greater Critter")]
public class Turn_Greater_Critter(Spell spell) : SpellScript(spell)
{
    private readonly Debuff _debuff = new DebuffRending();
    private readonly GlobalSpellMethods _spellMethod = new();

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target)
    {
        if (sprite is not Aisling aisling) return;

        if (target.CurrentHp > 0)
        {
            aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(Spell.Template.TargetAnimation, null, target.Serial));
            aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, client => client.SendSound(Spell.Template.Sound, false));
        }
        else
        {
            aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(Spell.Template.TargetAnimation, target.Position));
        }

        foreach (var monster in aisling.MonstersNearby())
        {
            if (!monster.Template.MonsterRace.MonsterRaceIsSet(MonsterRace.LowerBeing)) continue;
            if (monster.Level >= 251) continue;
            _spellMethod.ElementalOnUse(sprite, target, Spell, 50);
            _spellMethod.AfflictionOnUse(sprite, target, Spell, _debuff);
            monster.Target = null;
            monster.Aggressive = false;
        }
    }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (sprite is not Aisling playerAction) return;
        if (target == null) return;
        if (!Spell.CanUse())
        {
            if (sprite is Aisling aisling2)
                aisling2.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Ability is not quite ready yet.");
            return;
        }

        playerAction.ActionUsed = "Turn Critter";
        var client = playerAction.Client;
        _spellMethod.Train(client, Spell);
        var success = _spellMethod.Execute(client, Spell);
        var mR = Generator.RandNumGen100();

        if (mR > target.Will)
        {
            if (success)
            {
                OnSuccess(sprite, target);
            }
            else
            {
                _spellMethod.SpellOnFailed(playerAction, target, Spell);
            }
        }
        else
        {
            playerAction.Client.Aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(115, null, target.Serial));
        }
    }
}

[Script("Ao Puinsein")]
public class AoPuinsein(Spell spell) : SpellScript(spell)
{
    private readonly GlobalSpellMethods _spellMethod = new();

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target)
    {
        var cursed = target.HasDebuff("Ard Puinsein") || target.HasDebuff("Mor Puinsein") ||
                     target.HasDebuff("Puinsein") || target.HasDebuff("Beag Puinsein");
        var aoDebuff = target.GetDebuffName(i => i.Name.Contains("Puinsein"));

        if (sprite is not Aisling aisling)
        {
            if (!cursed) return;
            if (aoDebuff == string.Empty) return;

            foreach (var debuffs in ServerSetup.Instance.GlobalDeBuffCache.Values)
            {
                if (debuffs.Name != aoDebuff) continue;
                debuffs.OnEnded(target, debuffs);
            }

            return;
        }

        var client = aisling.Client;
        client.SendServerMessage(ServerMessageType.OrangeBar1, $"Cast {Spell.Template.Name}");

        if (cursed)
            if (target is Aisling targetAisling)
                targetAisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, $"{aisling.Username} cured your ailment.");

        aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(Spell.Template.TargetAnimation, null, target.Serial));
        aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendSound(Spell.Template.Sound, false));

        if (!cursed) return;
        if (aoDebuff == string.Empty) return;

        foreach (var debuffs in ServerSetup.Instance.GlobalDeBuffCache.Values)
        {
            if (debuffs.Name != aoDebuff) continue;
            debuffs.OnEnded(target, debuffs);
        }
    }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (target == null) return;
        if (!Spell.CanUse())
        {
            if (sprite is Aisling aisling2)
                aisling2.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Ability is not quite ready yet.");
            return;
        }

        if (sprite is Aisling aisling)
        {
            var client = aisling.Client;

            if (aisling.CurrentMp - Spell.Template.ManaCost > 0)
            {
                aisling.CurrentMp -= Spell.Template.ManaCost;
                _spellMethod.Train(client, Spell);
            }
            else
            {
                client.SendServerMessage(ServerMessageType.OrangeBar1, $"{ServerSetup.Instance.Config.NoManaMessage}");
                return;
            }

            if (aisling.CurrentMp < 0)
                aisling.CurrentMp = 0;

            var success = _spellMethod.Execute(client, Spell);

            if (success)
            {
                OnSuccess(sprite, target);
            }
            else
            {
                _spellMethod.SpellOnFailed(aisling, target, Spell);
            }

            client.SendAttributes(StatUpdateType.Vitality);
        }
        else
        {
            if (sprite.CurrentMp - Spell.Template.ManaCost > 0)
            {
                sprite.CurrentMp -= Spell.Template.ManaCost;
            }

            if (sprite.CurrentMp < 0)
                sprite.CurrentMp = 0;

            OnSuccess(sprite, sprite);
        }
    }
}

[Script("Ao Dall")]
public class AoDall(Spell spell) : SpellScript(spell)
{
    private readonly GlobalSpellMethods _spellMethod = new();

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target)
    {
        var blind = target.HasDebuff("Blind");
        if (!blind) return;
        var aoDebuff = target.GetDebuffName(i => i.Name.Contains("Blind"));
        if (aoDebuff == string.Empty) return;

        if (sprite is not Aisling aisling)
        {
            foreach (var debuffs in ServerSetup.Instance.GlobalDeBuffCache.Values)
            {
                if (debuffs.Name != aoDebuff) continue;
                debuffs.OnEnded(target, debuffs);
            }

            return;
        }

        var client = aisling.Client;
        client.SendServerMessage(ServerMessageType.OrangeBar1, $"Cast {Spell.Template.Name}");
        if (target is Aisling targetAisling)
            targetAisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, $"{aisling.Username} cured your ailment.");

        aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(Spell.Template.TargetAnimation, null, target.Serial));
        aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendSound(Spell.Template.Sound, false));

        foreach (var debuffs in ServerSetup.Instance.GlobalDeBuffCache.Values)
        {
            if (debuffs.Name != aoDebuff) continue;
            debuffs.OnEnded(target, debuffs);
        }
    }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (target == null) return;
        if (!Spell.CanUse())
        {
            if (sprite is Aisling aisling2)
                aisling2.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Ability is not quite ready yet.");
            return;
        }

        if (sprite is Aisling aisling)
        {
            var client = aisling.Client;

            if (aisling.CurrentMp - Spell.Template.ManaCost > 0)
            {
                aisling.CurrentMp -= Spell.Template.ManaCost;
                _spellMethod.Train(client, Spell);
            }
            else
            {
                client.SendServerMessage(ServerMessageType.OrangeBar1, $"{ServerSetup.Instance.Config.NoManaMessage}");
                return;
            }

            if (aisling.CurrentMp < 0)
                aisling.CurrentMp = 0;

            var success = _spellMethod.Execute(client, Spell);

            if (success)
            {
                OnSuccess(sprite, target);
            }
            else
            {
                _spellMethod.SpellOnFailed(aisling, target, Spell);
            }

            client.SendAttributes(StatUpdateType.Vitality);
        }
        else
        {
            if (sprite.CurrentMp - Spell.Template.ManaCost > 0)
            {
                sprite.CurrentMp -= Spell.Template.ManaCost;
            }

            if (sprite.CurrentMp < 0)
                sprite.CurrentMp = 0;

            OnSuccess(sprite, sprite);
        }
    }
}

[Script("Ao Beag Cradh")]
public class AoBeagCradh(Spell spell) : SpellScript(spell)
{
    private readonly GlobalSpellMethods _spellMethod = new();

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target)
    {
        var cursed = target.HasDebuff("Beag Cradh");
        if (!cursed) return;
        var aoDebuff = target.GetDebuffName(i => i.Name == "Beag Cradh");
        if (aoDebuff == string.Empty) return;

        if (sprite is not Aisling aisling)
        {
            foreach (var debuffs in ServerSetup.Instance.GlobalDeBuffCache.Values)
            {
                if (debuffs.Name != aoDebuff) continue;
                debuffs.OnEnded(target, debuffs);
            }

            return;
        }

        var client = aisling.Client;
        client.SendServerMessage(ServerMessageType.OrangeBar1, $"Cast {Spell.Template.Name}");
        if (target is Aisling targetAisling)
            targetAisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, $"{aisling.Username} cured your curse mark");

        aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(Spell.Template.TargetAnimation, null, target.Serial));
        aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendSound(Spell.Template.Sound, false));

        foreach (var debuffs in ServerSetup.Instance.GlobalDeBuffCache.Values)
        {
            if (debuffs.Name != aoDebuff) continue;
            debuffs.OnEnded(target, debuffs);
        }
    }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (target == null) return;
        if (!Spell.CanUse())
        {
            if (sprite is Aisling aisling2)
                aisling2.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Ability is not quite ready yet.");
            return;
        }

        if (sprite is Aisling aisling)
        {
            var client = aisling.Client;

            if (aisling.CurrentMp - Spell.Template.ManaCost > 0)
            {
                aisling.CurrentMp -= Spell.Template.ManaCost;
                _spellMethod.Train(client, Spell);
            }
            else
            {
                client.SendServerMessage(ServerMessageType.OrangeBar1, $"{ServerSetup.Instance.Config.NoManaMessage}");
                return;
            }

            if (aisling.CurrentMp < 0)
                aisling.CurrentMp = 0;

            var success = _spellMethod.Execute(client, Spell);

            if (success)
            {
                OnSuccess(sprite, target);
            }
            else
            {
                _spellMethod.SpellOnFailed(aisling, target, Spell);
            }

            client.SendAttributes(StatUpdateType.Vitality);
        }
        else
        {
            if (sprite.CurrentMp - Spell.Template.ManaCost > 0)
            {
                sprite.CurrentMp -= Spell.Template.ManaCost;
            }

            if (sprite.CurrentMp < 0)
                sprite.CurrentMp = 0;

            OnSuccess(sprite, sprite);
        }
    }
}

[Script("Ao Cradh")]
public class AoCradh(Spell spell) : SpellScript(spell)
{
    private readonly GlobalSpellMethods _spellMethod = new();

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target)
    {
        var cursed = target.HasDebuff("Cradh") || target.HasDebuff("Beag Cradh");
        if (!cursed) return;
        var aoDebuff = target.GetDebuffName(i => i.Name.Contains("Cradh"));
        if (aoDebuff == string.Empty) return;
        if (aoDebuff is not ("Cradh" or "Beag Cradh")) return;

        if (sprite is not Aisling aisling)
        {
            foreach (var debuffs in ServerSetup.Instance.GlobalDeBuffCache.Values)
            {
                if (debuffs.Name != aoDebuff) continue;
                debuffs.OnEnded(target, debuffs);
            }

            return;
        }

        var client = aisling.Client;
        client.SendServerMessage(ServerMessageType.OrangeBar1, $"Cast {Spell.Template.Name}");
        if (target is Aisling targetAisling)
            targetAisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, $"{aisling.Username} cured your curse mark");

        aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(Spell.Template.TargetAnimation, null, target.Serial));
        aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendSound(Spell.Template.Sound, false));

        foreach (var debuffs in ServerSetup.Instance.GlobalDeBuffCache.Values)
        {
            if (debuffs.Name != aoDebuff) continue;
            debuffs.OnEnded(target, debuffs);
        }
    }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (target == null) return;
        if (!Spell.CanUse())
        {
            if (sprite is Aisling aisling2)
                aisling2.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Ability is not quite ready yet.");
            return;
        }

        if (sprite is Aisling aisling)
        {
            var client = aisling.Client;

            if (aisling.CurrentMp - Spell.Template.ManaCost > 0)
            {
                aisling.CurrentMp -= Spell.Template.ManaCost;
                _spellMethod.Train(client, Spell);
            }
            else
            {
                client.SendServerMessage(ServerMessageType.OrangeBar1, $"{ServerSetup.Instance.Config.NoManaMessage}");
                return;
            }

            if (aisling.CurrentMp < 0)
                aisling.CurrentMp = 0;

            var success = _spellMethod.Execute(client, Spell);

            if (success)
            {
                OnSuccess(sprite, target);
            }
            else
            {
                _spellMethod.SpellOnFailed(aisling, target, Spell);
            }

            client.SendAttributes(StatUpdateType.Vitality);
        }
        else
        {
            if (sprite.CurrentMp - Spell.Template.ManaCost > 0)
            {
                sprite.CurrentMp -= Spell.Template.ManaCost;
            }

            if (sprite.CurrentMp < 0)
                sprite.CurrentMp = 0;

            OnSuccess(sprite, sprite);
        }
    }
}

[Script("Ao Mor Cradh")]
public class AoMorCradh(Spell spell) : SpellScript(spell)
{
    private readonly GlobalSpellMethods _spellMethod = new();

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target)
    {
        var cursed = target.HasDebuff("Mor Cradh") || target.HasDebuff("Cradh") || target.HasDebuff("Beag Cradh");
        if (!cursed) return;
        var aoDebuff = target.GetDebuffName(i => i.Name.Contains("Cradh"));
        if (aoDebuff == string.Empty) return;
        if (aoDebuff is not ("Mor Cradh" or "Cradh" or "Beag Cradh")) return;

        if (sprite is not Aisling aisling)
        {
            foreach (var debuffs in ServerSetup.Instance.GlobalDeBuffCache.Values)
            {
                if (debuffs.Name != aoDebuff) continue;
                debuffs.OnEnded(target, debuffs);
            }

            return;
        }

        var client = aisling.Client;
        client.SendServerMessage(ServerMessageType.OrangeBar1, $"Cast {Spell.Template.Name}");
        if (target is Aisling targetAisling)
            targetAisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, $"{aisling.Username} cured your curse mark");

        aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(Spell.Template.TargetAnimation, null, target.Serial));
        aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendSound(Spell.Template.Sound, false));

        foreach (var debuffs in ServerSetup.Instance.GlobalDeBuffCache.Values)
        {
            if (debuffs.Name != aoDebuff) continue;
            debuffs.OnEnded(target, debuffs);
        }
    }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (target == null) return;
        if (!Spell.CanUse())
        {
            if (sprite is Aisling aisling2)
                aisling2.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Ability is not quite ready yet.");
            return;
        }

        if (sprite is Aisling aisling)
        {
            var client = aisling.Client;

            if (aisling.CurrentMp - Spell.Template.ManaCost > 0)
            {
                aisling.CurrentMp -= Spell.Template.ManaCost;
                _spellMethod.Train(client, Spell);
            }
            else
            {
                client.SendServerMessage(ServerMessageType.OrangeBar1, $"{ServerSetup.Instance.Config.NoManaMessage}");
                return;
            }

            if (aisling.CurrentMp < 0)
                aisling.CurrentMp = 0;

            var success = _spellMethod.Execute(client, Spell);

            if (success)
            {
                OnSuccess(sprite, target);
            }
            else
            {
                _spellMethod.SpellOnFailed(aisling, target, Spell);
            }

            client.SendAttributes(StatUpdateType.Vitality);
        }
        else
        {
            if (sprite.CurrentMp - Spell.Template.ManaCost > 0)
            {
                sprite.CurrentMp -= Spell.Template.ManaCost;
            }

            if (sprite.CurrentMp < 0)
                sprite.CurrentMp = 0;

            OnSuccess(sprite, sprite);
        }
    }
}

[Script("Ao Ard Cradh")]
public class AoArdCradh(Spell spell) : SpellScript(spell)
{
    private readonly GlobalSpellMethods _spellMethod = new();

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target)
    {
        var cursed = target.HasDebuff("Ard Cradh") || target.HasDebuff("Mor Cradh") || target.HasDebuff("Cradh") || target.HasDebuff("Beag Cradh");
        if (!cursed) return;
        var aoDebuff = target.GetDebuffName(i => i.Name.Contains("Cradh"));
        if (aoDebuff == string.Empty) return;
        if (aoDebuff is not ("Ard Cradh" or "Mor Cradh" or "Cradh" or "Beag Cradh")) return;

        if (sprite is not Aisling aisling)
        {
            foreach (var debuffs in ServerSetup.Instance.GlobalDeBuffCache.Values)
            {
                if (debuffs.Name != aoDebuff) continue;
                debuffs.OnEnded(target, debuffs);
            }

            return;
        }

        var client = aisling.Client;
        client.SendServerMessage(ServerMessageType.OrangeBar1, $"Cast {Spell.Template.Name}");
        if (target is Aisling targetAisling)
            targetAisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, $"{aisling.Username} cured your curse mark");

        aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(Spell.Template.TargetAnimation, null, target.Serial));
        aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendSound(Spell.Template.Sound, false));

        foreach (var debuffs in ServerSetup.Instance.GlobalDeBuffCache.Values)
        {
            if (debuffs.Name != aoDebuff) continue;
            debuffs.OnEnded(target, debuffs);
        }
    }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (target == null) return;
        if (!Spell.CanUse())
        {
            if (sprite is Aisling aisling2)
                aisling2.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Ability is not quite ready yet.");
            return;
        }

        if (sprite is Aisling aisling)
        {
            var client = aisling.Client;

            if (aisling.CurrentMp - Spell.Template.ManaCost > 0)
            {
                aisling.CurrentMp -= Spell.Template.ManaCost;
                _spellMethod.Train(client, Spell);
            }
            else
            {
                client.SendServerMessage(ServerMessageType.OrangeBar1, $"{ServerSetup.Instance.Config.NoManaMessage}");
                return;
            }

            if (aisling.CurrentMp < 0)
                aisling.CurrentMp = 0;

            var success = _spellMethod.Execute(client, Spell);

            if (success)
            {
                OnSuccess(sprite, target);
            }
            else
            {
                _spellMethod.SpellOnFailed(aisling, target, Spell);
            }

            client.SendAttributes(StatUpdateType.Vitality);
        }
        else
        {
            if (sprite.CurrentMp - Spell.Template.ManaCost > 0)
            {
                sprite.CurrentMp -= Spell.Template.ManaCost;
            }

            if (sprite.CurrentMp < 0)
                sprite.CurrentMp = 0;

            OnSuccess(sprite, sprite);
        }
    }
}

[Script("Ao Suain")]
public class AoSuain(Spell spell) : SpellScript(spell)
{
    private readonly GlobalSpellMethods _spellMethod = new();

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target)
    {
        if (sprite is not Aisling aisling) return;
        var client = aisling.Client;

        switch (target.IsFrozen)
        {
            case false:
                client.SendServerMessage(ServerMessageType.OrangeBar1, $"Cast {Spell.Template.Name}, but it did nothing");
                return;
            case true:
                {
                    client.SendServerMessage(ServerMessageType.OrangeBar1, $"Cast {Spell.Template.Name}");

                    if (target is Aisling targetAisling)
                        targetAisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, $"{aisling.Username} removed your paralysis");
                    break;
                }
        }

        aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(Spell.Template.TargetAnimation, null, target.Serial));
        aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendSound(Spell.Template.Sound, false));

        if (target.HasDebuff("Frozen"))
            target.RemoveDebuff("Frozen");
        if (target.HasDebuff("Dark Chain"))
            target.RemoveDebuff("Dark Chain");
    }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (target == null) return;
        if (sprite is not Aisling aisling) return;
        var client = aisling.Client;

        if (!Spell.CanUse())
        {
            if (sprite is Aisling aisling2)
                aisling2.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Ability is not quite ready yet.");
            return;
        }

        if (aisling.CurrentMp - Spell.Template.ManaCost > 0)
        {
            aisling.CurrentMp -= Spell.Template.ManaCost;
            _spellMethod.Train(client, Spell);
        }
        else
        {
            client.SendServerMessage(ServerMessageType.OrangeBar1, $"{ServerSetup.Instance.Config.NoManaMessage}");
            return;
        }

        if (aisling.CurrentMp < 0)
            aisling.CurrentMp = 0;

        var success = _spellMethod.Execute(client, Spell);

        if (success)
        {
            OnSuccess(sprite, target);
        }
        else
        {
            _spellMethod.SpellOnFailed(aisling, target, Spell);
        }

        client.SendAttributes(StatUpdateType.Vitality);
    }
}

// Cleric Spells
// Sustain = prevent ally's death by keeping them at 1hp for 30 seconds then death is recast if not removed
// Phoenix Wave = red group, 24 hr cooldown
// Ray of Light = High Holy Damage
// Water of Life = Heal self based on missing health
// Booming Shield = stun any target that attacks you for a duration (3 seconds) (30 second cooldown)
