using Chaos.Common.Definitions;
using Darkages.Enums;
using Darkages.GameScripts.Affects;
using Darkages.Network.Client;
using Darkages.Scripting;
using Darkages.Sprites;
using Darkages.Types;
using MapFlags = Darkages.Enums.MapFlags;

namespace Darkages.GameScripts.Spells;

/// <summary>
/// Spectral Shield: Bonus to AC
/// </summary>
[Script("Spectral Shield")]
public class Spectral_Shield : SpellScript
{
    private readonly Spell _spell;
    private readonly Buff _buff = new buff_SpectralShield();
    private readonly GlobalSpellMethods _spellMethod;

    public Spectral_Shield(Spell spell) : base(spell)
    {
        _spell = spell;
        _spellMethod = new GlobalSpellMethods();
    }

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target) { }

    public override void OnUse(Sprite sprite, Sprite target)
    {

        if (target.HasBuff("Spectral Shield") || target.HasBuff("Defensive Stance"))
        {
            if (sprite is not Aisling aisling) return;
            _spellMethod.Train(aisling.Client, _spell);
            aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Another spell of similar nature is already applied.");
            return;
        }

        _spellMethod.EnhancementOnUse(sprite, target, _spell, _buff);
    }
}

/// <summary>
/// Aite
/// </summary>
[Script("Aite")]
public class Aite : SpellScript
{
    private readonly Spell _spell;
    private readonly Buff _buff = new buff_aite();
    private readonly GlobalSpellMethods _spellMethod;

    public Aite(Spell spell) : base(spell)
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
            if (sprite is not Aisling aisling) return;
            _spellMethod.Train(aisling.Client, _spell);
            aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Another spell of similar nature is already applied.");
            return;
        }

        _spellMethod.EnhancementOnUse(sprite, target, _spell, _buff);
    }
}

/// <summary>
/// Mor Dion
/// </summary>
[Script("Mor Dion")]
public class Mor_Dion : SpellScript
{
    private readonly Spell _spell;
    private readonly Buff _buff = new buff_MorDion();
    private readonly GlobalSpellMethods _spellMethod;

    public Mor_Dion(Spell spell) : base(spell)
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
            if (sprite is not Aisling aisling) return;
            _spellMethod.Train(aisling.Client, _spell);
            aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Another spell of similar nature is already applied.");
            return;
        }

        _spellMethod.EnhancementOnUse(sprite, target, _spell, _buff);
    }
}

/// <summary>
/// Dark Chain: Stun and Slight Damage
/// </summary>
[Script("Dark Chain")]
public class Dark_Chain : SpellScript
{
    private readonly Spell _spell;
    private readonly Debuff _debuff = new debuff_DarkChain();
    private readonly GlobalSpellMethods _spellMethod;

    public Dark_Chain(Spell spell) : base(spell)
    {
        _spell = spell;
        _spellMethod = new GlobalSpellMethods();
    }

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target) { }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (sprite is Aisling playerAction)
            playerAction.ActionUsed = "Dark Chain";

        _spellMethod.ElementalOnUse(sprite, target, _spell, 60);
        _spellMethod.AfflictionOnUse(sprite, target, _spell, _debuff);
    }
}

/// <summary>
/// Halt: Stop time for a target
/// </summary>
[Script("Halt")]
public class Halt : SpellScript
{
    private readonly Spell _spell;
    private readonly Debuff _debuff = new debuff_Halt();
    private readonly GlobalSpellMethods _spellMethod;

    public Halt(Spell spell) : base(spell)
    {
        _spell = spell;
        _spellMethod = new GlobalSpellMethods();
    }

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target) { }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (sprite is Aisling playerAction)
            playerAction.ActionUsed = "Halt";

        _spellMethod.AfflictionOnUse(sprite, target, _spell, _debuff);
    }
}

[Script("Pramh")]
public class Pramh : SpellScript
{
    private readonly Spell _spell;
    private readonly Debuff _debuff = new debuff_sleep();
    private readonly GlobalSpellMethods _spellMethod;

    public Pramh(Spell spell) : base(spell)
    {
        _spell = spell;
        _spellMethod = new GlobalSpellMethods();
    }

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target) { }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (sprite is Aisling playerAction)
            playerAction.ActionUsed = "Pramh";

        if (target.HasDebuff("Frozen"))
            target.RemoveDebuff("Frozen");

        if (target.HasDebuff("Sleep"))
        {
            if (sprite is not Aisling aisling) return;
            _spellMethod.Train(aisling.Client, _spell);
            aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Another spell of similar nature is already applied.");
            return;
        };

        _spellMethod.AfflictionOnUse(sprite, target, _spell, _debuff);
    }
}

/// <summary>
/// Detect: See monsters Advanced Stats
/// </summary>
[Script("Detect")]
public class Detect : SpellScript
{
    private readonly Spell _spell;
    private readonly GlobalSpellMethods _spellMethod;

    public Detect(Spell spell) : base(spell)
    {
        _spell = spell;
        _spellMethod = new GlobalSpellMethods();
    }

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target)
    {
        if (sprite is not Aisling aisling) return;
        if (target is not Monster monster) return;
        var client = aisling.Client;

        aisling.Cast(_spell, target);

        if (target.CurrentHp > 0)
        {
            target.Show(Scope.NearbyAislings, new ServerFormat19(_spell.Template.Sound));
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

        switch (_spell.Level)
        {
            case < 10:
                aisling.Client.SendServerMessage(ServerMessageType.ScrollWindow,
                    $"{title}\n\n{{=aLv: {colorLvl} {{=aHP: {halfHp}/{monster.MaximumHp} {{=aO: {colorA}{monster.OffenseElement} {{=aD: {colorB}{monster.DefenseElement}");
                break;
            case >= 11 and <= 40:
                aisling.Client.SendServerMessage(ServerMessageType.ScrollWindow,
                    $"{title}\n\n{{=aLv: {colorLvl} {{=aHP: {halfHp}/{monster.MaximumHp} {{=aO: {colorA}{monster.OffenseElement} {{=aD: {colorB}{monster.DefenseElement}\n" +
                    $"{{=c{monster.Template.BaseName} {{=aSize: {{=s{monster.Size} {{=aAC: {{=s{monster.Ac}");
                break;
            default:
                aisling.Client.SendServerMessage(ServerMessageType.ScrollWindow, $"{title}\n\n{{=aLv: {colorLvl} {{=aHP: {halfHp}/{monster.MaximumHp} {{=aO: {colorA}{monster.OffenseElement} {{=aD: {colorB}{monster.DefenseElement}\n" +
                                                 $"{{=c{monster.Template.BaseName} {{=aSize: {{=s{monster.Size} {{=aAC: {{=s{monster.Ac} {{=aRegen: {{=s{monster.Regen}\n" +
                                                 $"{{=aSTR:{{=s{monster.Str} {{=aINT:{{=s{monster.Int} {{=aWIS:{{=s{monster.Wis} {{=aCON:{{=s{monster.Con} {{=aDEX:{{=s{monster.Dex}\n" +
                                                 $"{{=aRace:{{=s{monster.Template.MonsterRace} {{=aFortitude:{{=s{monster.Fortitude} {{=aReflex:{{=s{monster.Reflex} {{=aWill:{{=s{monster.Will}");
                break;
        }
    }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (!_spell.CanUse()) return;

        if (sprite is Aisling aisling)
        {
            var client = aisling.Client;
            _spellMethod.Train(client, _spell);

            if (aisling.CurrentMp - _spell.Template.ManaCost > 0)
            {
                aisling.CurrentMp -= _spell.Template.ManaCost;
            }
            else
            {
                client.SendServerMessage(ServerMessageType.OrangeBar1, $"{ServerSetup.Instance.Config.NoManaMessage}");
                return;
            }

            if (aisling.CurrentMp < 0)
                aisling.CurrentMp = 0;

            var success = _spellMethod.Execute(client, _spell);

            if (success)
            {
                if (client.Aisling.IsInvisible && _spell.Template.PostQualifiers is PostQualifier.BreakInvisible or PostQualifier.Both)
                {
                    client.Aisling.IsInvisible = false;
                    client.UpdateDisplay();
                }

                OnSuccess(sprite, target);
            }
            else
            {
                _spellMethod.SpellOnFailed(aisling, target, _spell);
            }


            client.SendAttributes(StatUpdateType.Vitality);
        }
        else
        {
            if (sprite.CurrentMp - _spell.Template.ManaCost > 0)
            {
                sprite.CurrentMp -= _spell.Template.ManaCost;
            }

            if (sprite.CurrentMp < 0)
                sprite.CurrentMp = 0;

            OnSuccess(sprite, target);
        }
    }

    private string LevelColor(WorldClient client, Monster monster)
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
        if (monster.Template.Level <= client.Aisling.Level - 10)
            return $"{{=i{monster.Template.Level}{{=s";
        return $"{{=q{monster.Template.Level}{{=s";
    }
}

/// <summary>
/// Heal Minor Wounds: Heals 15% of target's health
/// </summary>
[Script("Heal Minor Wounds")]
public class Heal_Minor : SpellScript
{
    private readonly Spell _spell;
    private readonly GlobalSpellMethods _spellMethod;

    public Heal_Minor(Spell spell) : base(spell)
    {
        _spell = spell;
        _spellMethod = new GlobalSpellMethods();
    }

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target)
    {
        if (sprite is Aisling aisling)
        {
            aisling.CastSpell(_spell, );
            var healBase = target.MaximumHp * 0.15;

            target.CurrentHp += (int)healBase;
            if (target.CurrentHp > target.MaximumHp)
                target.CurrentHp = target.MaximumHp;

            aisling.Client.SendHealthBar(target, _spell.Template.Sound);
            if (target is Aisling)
                target.Client.SendAttributes(StatUpdateType.FullVitality);
        }
        else
        {
            var healBase = (int)(sprite.BaseHp * 0.10);

            if (sprite.CurrentHp >= sprite.MaximumHp) return;
            sprite.CurrentHp += healBase;

            if (sprite.CurrentHp > sprite.MaximumHp)
                sprite.CurrentHp = sprite.MaximumHp;
        }
    }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (!_spell.CanUse()) return;

        if (sprite is Aisling aisling)
        {
            var client = aisling.Client;
            _spellMethod.Train(client, _spell);

            if (aisling.CurrentMp - _spell.Template.ManaCost > 0)
            {
                aisling.CurrentMp -= _spell.Template.ManaCost;
            }
            else
            {
                client.SendServerMessage(ServerMessageType.OrangeBar1, $"{ServerSetup.Instance.Config.NoManaMessage}");
                return;
            }

            if (aisling.CurrentMp < 0)
                aisling.CurrentMp = 0;

            var success = _spellMethod.Execute(client, _spell);

            if (success)
            {
                if (client.Aisling.IsInvisible && _spell.Template.PostQualifiers is PostQualifier.BreakInvisible or PostQualifier.Both)
                {
                    client.Aisling.IsInvisible = false;
                    client.UpdateDisplay();
                }

                OnSuccess(sprite, target);
            }
            else
            {
                _spellMethod.SpellOnFailed(aisling, target, _spell);
            }


            client.SendAttributes(StatUpdateType.Vitality);
        }
        else
        {
            if (sprite.CurrentMp - _spell.Template.ManaCost > 0)
            {
                sprite.CurrentMp -= _spell.Template.ManaCost;
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
public class Heal_Major : SpellScript
{
    private readonly Spell _spell;
    private readonly GlobalSpellMethods _spellMethod;

    public Heal_Major(Spell spell) : base(spell)
    {
        _spell = spell;
        _spellMethod = new GlobalSpellMethods();
    }

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target)
    {
        if (sprite is Aisling aisling)
        {
            aisling.Cast(_spell, target);
            aisling.Show(Scope.NearbyAislings, new ServerFormat19(_spell.Template.Sound));

            var healBase = target.MaximumHp * 0.30;

            target.CurrentHp += (int)healBase;
            if (target.CurrentHp > target.MaximumHp)
                target.CurrentHp = target.MaximumHp;

            var healthBar = new ServerFormat13
            {
                Serial = target.Serial,
                Health = (ushort)(100 * target.CurrentHp / target.MaximumHp),
                Sound = 8
            };

            aisling.Show(Scope.NearbyAislings, healthBar);
            if (target is Aisling)
                target.Client.SendAttributes(StatUpdateType.FullVitality);
        }
        else
        {
            var healBase = (int)(sprite.BaseHp * 0.25);

            if (sprite.CurrentHp >= sprite.MaximumHp) return;
            sprite.CurrentHp += healBase;

            if (sprite.CurrentHp > sprite.MaximumHp)
                sprite.CurrentHp = sprite.MaximumHp;
        }
    }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (!_spell.CanUse()) return;

        if (sprite is Aisling aisling)
        {
            var client = aisling.Client;
            _spellMethod.Train(client, _spell);

            if (aisling.CurrentMp - _spell.Template.ManaCost > 0)
            {
                aisling.CurrentMp -= _spell.Template.ManaCost;
            }
            else
            {
                client.SendServerMessage(ServerMessageType.OrangeBar1, $"{ServerSetup.Instance.Config.NoManaMessage}");
                return;
            }

            if (aisling.CurrentMp < 0)
                aisling.CurrentMp = 0;

            var success = _spellMethod.Execute(client, _spell);

            if (success)
            {
                if (client.Aisling.IsInvisible && _spell.Template.PostQualifiers is PostQualifier.BreakInvisible or PostQualifier.Both)
                {
                    client.Aisling.IsInvisible = false;
                    client.UpdateDisplay();
                }

                OnSuccess(sprite, target);
            }
            else
            {
                _spellMethod.SpellOnFailed(aisling, target, _spell);
            }


            client.SendAttributes(StatUpdateType.Vitality);
        }
        else
        {
            if (sprite.CurrentMp - _spell.Template.ManaCost > 0)
            {
                sprite.CurrentMp -= _spell.Template.ManaCost;
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
public class Heal_Critical : SpellScript
{
    private readonly Spell _spell;
    private readonly GlobalSpellMethods _spellMethod;

    public Heal_Critical(Spell spell) : base(spell)
    {
        _spell = spell;
        _spellMethod = new GlobalSpellMethods();
    }

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target)
    {
        if (sprite is Aisling aisling)
        {
            aisling.Cast(_spell, target);
            aisling.Show(Scope.NearbyAislings, new ServerFormat19(_spell.Template.Sound));

            var healBase = target.MaximumHp * 0.65;

            target.CurrentHp += (int)healBase;
            if (target.CurrentHp > target.MaximumHp)
                target.CurrentHp = target.MaximumHp;

            var healthBar = new ServerFormat13
            {
                Serial = target.Serial,
                Health = (ushort)(100 * target.CurrentHp / target.MaximumHp),
                Sound = 8
            };

            aisling.Show(Scope.NearbyAislings, healthBar);
            if (target is Aisling)
                target.Client.SendAttributes(StatUpdateType.FullVitality);
        }
        else
        {
            var healBase = (int)(sprite.BaseHp * 0.45);

            if (sprite.CurrentHp >= sprite.MaximumHp) return;
            sprite.CurrentHp += healBase;

            if (sprite.CurrentHp > sprite.MaximumHp)
                sprite.CurrentHp = sprite.MaximumHp;
        }
    }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (!_spell.CanUse()) return;

        if (sprite is Aisling aisling)
        {
            var client = aisling.Client;
            _spellMethod.Train(client, _spell);

            if (aisling.CurrentMp - _spell.Template.ManaCost > 0)
            {
                aisling.CurrentMp -= _spell.Template.ManaCost;
            }
            else
            {
                client.SendServerMessage(ServerMessageType.OrangeBar1, $"{ServerSetup.Instance.Config.NoManaMessage}");
                return;
            }

            if (aisling.CurrentMp < 0)
                aisling.CurrentMp = 0;

            var success = _spellMethod.Execute(client, _spell);

            if (success)
            {
                if (client.Aisling.IsInvisible && _spell.Template.PostQualifiers is PostQualifier.BreakInvisible or PostQualifier.Both)
                {
                    client.Aisling.IsInvisible = false;
                    client.UpdateDisplay();
                }

                OnSuccess(sprite, target);
            }
            else
            {
                _spellMethod.SpellOnFailed(aisling, target, _spell);
            }


            client.SendAttributes(StatUpdateType.Vitality);
        }
        else
        {
            if (sprite.CurrentMp - _spell.Template.ManaCost > 0)
            {
                sprite.CurrentMp -= _spell.Template.ManaCost;
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
public class Dire_Aid : SpellScript
{
    private readonly Spell _spell;
    private readonly Buff _buff = new buff_SpectralShield();
    private readonly GlobalSpellMethods _spellMethod;

    public Dire_Aid(Spell spell) : base(spell)
    {
        _spell = spell;
        _spellMethod = new GlobalSpellMethods();
    }

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target)
    {
        if (sprite is Aisling aisling)
        {
            aisling.Cast(_spell, target);
            aisling.Show(Scope.NearbyAislings, new ServerFormat19(_spell.Template.Sound));

            var healBase = target.MaximumHp * 0.80;

            target.CurrentHp += (int)healBase;
            if (target.CurrentHp > target.MaximumHp)
                target.CurrentHp = target.MaximumHp;

            if (target.HasBuff("Spectral Shield") || target.HasBuff("Defensive Stance"))
            {
                if (target is Aisling)
                    aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Another spell of similar nature is applied.");
            }
            else
            {
                _spellMethod.EnhancementOnUse(sprite, target, _spell, _buff);
            }

            var healthBar = new ServerFormat13
            {
                Serial = target.Serial,
                Health = (ushort)(100 * target.CurrentHp / target.MaximumHp),
                Sound = 8
            };

            aisling.Show(Scope.NearbyAislings, healthBar);
            if (target is Aisling)
                target.Client.SendAttributes(StatUpdateType.FullVitality);
        }
        else
        {
            var healBase = (int)(sprite.BaseHp * 0.70);

            if (sprite.CurrentHp >= sprite.MaximumHp) return;
            sprite.CurrentHp += healBase;

            if (sprite.CurrentHp > sprite.MaximumHp)
                sprite.CurrentHp = sprite.MaximumHp;
        }
    }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (!_spell.CanUse()) return;

        if (sprite is Aisling aisling)
        {
            var client = aisling.Client;
            _spellMethod.Train(client, _spell);

            if (aisling.CurrentMp - _spell.Template.ManaCost > 0)
            {
                aisling.CurrentMp -= _spell.Template.ManaCost;
            }
            else
            {
                client.SendServerMessage(ServerMessageType.OrangeBar1, $"{ServerSetup.Instance.Config.NoManaMessage}");
                return;
            }

            if (aisling.CurrentMp < 0)
                aisling.CurrentMp = 0;

            var success = _spellMethod.Execute(client, _spell);

            if (success)
            {
                if (client.Aisling.IsInvisible && _spell.Template.PostQualifiers is PostQualifier.BreakInvisible or PostQualifier.Both)
                {
                    client.Aisling.IsInvisible = false;
                    client.UpdateDisplay();
                }

                OnSuccess(sprite, target);
            }
            else
            {
                _spellMethod.SpellOnFailed(aisling, target, _spell);
            }


            client.SendAttributes(StatUpdateType.Vitality);
        }
        else
        {
            if (sprite.CurrentMp - _spell.Template.ManaCost > 0)
            {
                sprite.CurrentMp -= _spell.Template.ManaCost;
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
public class Healing_Winds : SpellScript
{
    private readonly Spell _spell;
    private readonly GlobalSpellMethods _spellMethod;

    public Healing_Winds(Spell spell) : base(spell)
    {
        _spell = spell;
        _spellMethod = new GlobalSpellMethods();
    }

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target)
    {
        if (sprite is not Aisling aisling) return;

        aisling.Cast(_spell, target);
        aisling.Show(Scope.NearbyAislings, new ServerFormat19(_spell.Template.Sound));

        var healBase = aisling.MaximumHp * 0.25;

        if (aisling.GroupId != 0)
        {
            foreach (var partyMember in aisling.AislingsNearby().Where(i => i.GroupId == aisling.GroupId))
            {
                if (partyMember.Dead) continue;
                partyMember.CurrentHp += (int)healBase;
                if (partyMember.CurrentHp > partyMember.MaximumHp)
                    partyMember.CurrentHp = partyMember.MaximumHp;

                var healthBar = new ServerFormat13
                {
                    Serial = partyMember.Serial,
                    Health = (ushort)(100 * partyMember.CurrentHp / partyMember.MaximumHp),
                    Sound = 8
                };
                partyMember.Show(Scope.NearbyAislings, healthBar);
                partyMember.SendAnimation(267, partyMember, aisling);
                partyMember.Client.SendStats(StatusFlags.Health);
            }
        }
        else
        {
            aisling.CurrentHp += (int)healBase;
            if (aisling.CurrentHp > aisling.MaximumHp)
                aisling.CurrentHp = aisling.MaximumHp;

            var healthBar = new ServerFormat13
            {
                Serial = aisling.Serial,
                Health = (ushort)(100 * aisling.CurrentHp / aisling.MaximumHp),
                Sound = 8
            };
            aisling.Show(Scope.NearbyAislings, healthBar);
            aisling.Client.SendStats(StatusFlags.Health);
        }
    }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (!_spell.CanUse()) return;

        if (sprite is Aisling aisling)
        {
            var client = aisling.Client;
            _spellMethod.Train(client, _spell);

            if (aisling.CurrentMp - _spell.Template.ManaCost > 0)
            {
                aisling.CurrentMp -= _spell.Template.ManaCost;
            }
            else
            {
                client.SendServerMessage(ServerMessageType.OrangeBar1, $"{ServerSetup.Instance.Config.NoManaMessage}");
                return;
            }

            if (aisling.CurrentMp < 0)
                aisling.CurrentMp = 0;

            var success = _spellMethod.Execute(client, _spell);

            if (success)
            {
                if (client.Aisling.IsInvisible && _spell.Template.PostQualifiers is PostQualifier.BreakInvisible or PostQualifier.Both)
                {
                    client.Aisling.IsInvisible = false;
                    client.UpdateDisplay();
                }

                OnSuccess(sprite, target);
            }
            else
            {
                _spellMethod.SpellOnFailed(aisling, target, _spell);
            }


            client.SendAttributes(StatUpdateType.Vitality);
        }
        else
        {
            if (sprite.CurrentMp - _spell.Template.ManaCost > 0)
            {
                sprite.CurrentMp -= _spell.Template.ManaCost;
            }

            if (sprite.CurrentMp < 0)
                sprite.CurrentMp = 0;

            OnSuccess(sprite, target);
        }
    }
}

/// <summary>
/// Forestall: Remove debuff "Skulling" and prevent death
/// </summary>
[Script("Forestall")]
public class Forestall : SpellScript
{
    private readonly Spell _spell;
    private readonly GlobalSpellMethods _spellMethod;

    public Forestall(Spell spell) : base(spell)
    {
        _spell = spell;
        _spellMethod = new GlobalSpellMethods();
    }

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target)
    {
        if (sprite is not Aisling aisling) return;
        if (target is not Aisling savedAisling) return;
        aisling.Cast(_spell, target);
        aisling.Show(Scope.NearbyAislings, new ServerFormat19(_spell.Template.Sound));

        if (savedAisling.Skulled)
        {
            savedAisling.Debuffs.TryGetValue("Skulled", out var debuff);
            if (debuff != null)
            {
                debuff.Cancelled = true;
                debuff.OnEnded(savedAisling, debuff);
                savedAisling.Client.Revive();
            }
        }
    }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (!_spell.CanUse()) return;
        if (sprite is not Aisling aisling) return;
        var client = aisling.Client;
        _spellMethod.Train(client, _spell);

        if (aisling.CurrentMp - _spell.Template.ManaCost > 0)
        {
            aisling.CurrentMp -= _spell.Template.ManaCost;
        }
        else
        {
            client.SendServerMessage(ServerMessageType.OrangeBar1, $"{ServerSetup.Instance.Config.NoManaMessage}");
            return;
        }

        if (aisling.CurrentMp < 0)
            aisling.CurrentMp = 0;

        if (client.Aisling.IsInvisible && _spell.Template.PostQualifiers is PostQualifier.BreakInvisible or PostQualifier.Both)
        {
            client.Aisling.IsInvisible = false;
            client.UpdateDisplay();
        }

        OnSuccess(sprite, target);

        client.SendAttributes(StatUpdateType.Vitality);
    }
}

/// <summary>
/// Hell Grasp: Pull ally to the realm of the living
/// </summary>
[Script("Hell Grasp")]
public class Raise_Ally : SpellScript
{
    private readonly Spell _spell;
    private readonly GlobalSpellMethods _spellMethod;

    public Raise_Ally(Spell spell) : base(spell)
    {
        _spell = spell;
        _spellMethod = new GlobalSpellMethods();
    }

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target)
    {
        if (sprite is not Aisling aisling) return;
        aisling.Cast(_spell, target);
        aisling.Show(Scope.NearbyAislings, new ServerFormat19(_spell.Template.Sound));

        if (aisling.GroupId != 0)
        {
            foreach (var deadPartyMember in aisling.PartyMembers.Where(m => m is { Dead: true }))
            {
                deadPartyMember.Client.Revive();
                deadPartyMember.client.SendServerMessage(ServerMessageType.OrangeBar1, "I live again.");
                deadPartyMember.Client.SendStats(StatusFlags.MultiStat);
                deadPartyMember.Client.TransitionToMap(aisling.CurrentMapId, new Position(aisling.X, aisling.Y));
                Task.Delay(350).ContinueWith(ct => { deadPartyMember.Client.Aisling.Animate(304); });
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
        if (!_spell.CanUse()) return;
        if (sprite is not Aisling aisling) return;
        var client = aisling.Client;
        _spellMethod.Train(client, _spell);

        if (aisling.CurrentMp - _spell.Template.ManaCost > 0)
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

        if (client.Aisling.IsInvisible && _spell.Template.PostQualifiers is PostQualifier.BreakInvisible or PostQualifier.Both)
        {
            client.Aisling.IsInvisible = false;
            client.UpdateDisplay();
        }

        OnSuccess(sprite, target);


        client.SendAttributes(StatUpdateType.Vitality);
    }
}

/// <summary>
/// Turn Undead: Rends 10 AC, Slight Holy Damage, Turns Undead non-hostile, level up to 100
/// </summary>
[Script("Turn Undead")]
public class Turn_Undead : SpellScript
{
    private readonly Spell _spell;
    private readonly Debuff _debuff = new debuff_rending();
    private readonly GlobalSpellMethods _spellMethod;

    public Turn_Undead(Spell spell) : base(spell)
    {
        _spell = spell;
        _spellMethod = new GlobalSpellMethods();
    }

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target)
    {
        if (sprite is not Aisling aisling) return;

        aisling.Cast(_spell, target);
        aisling.Show(Scope.NearbyAislings, new ServerFormat19(_spell.Template.Sound));

        foreach (var monster in aisling.MonstersNearby())
        {
            if (!monster.Template.MonsterRace.MonsterRaceIsSet(MonsterRace.Undead)) continue;
            if (monster.Level >= 101) continue;
            _spellMethod.ElementalOnUse(sprite, target, _spell, 30);
            _spellMethod.AfflictionOnUse(sprite, target, _spell, _debuff);
            monster.Target = null;
            monster.Aggressive = false;
        }
    }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (!_spell.CanUse()) return;
        if (sprite is not Aisling playerAction) return;

        playerAction.ActionUsed = "Turn Undead";
        var client = playerAction.Client;
        _spellMethod.Train(client, _spell);
        var success = _spellMethod.Execute(client, _spell);

        if (success)
        {
            if (client.Aisling.IsInvisible && _spell.Template.PostQualifiers is PostQualifier.BreakInvisible or PostQualifier.Both)
            {
                client.Aisling.IsInvisible = false;
                client.UpdateDisplay();
            }

            OnSuccess(sprite, target);
        }
        else
        {
            _spellMethod.SpellOnFailed(playerAction, target, _spell);
        }
    }
}

/// <summary>
/// Turn Critter: Rends 10 AC, Slight Holy Damage, Turns Critters non-hostile, level up to 100
/// </summary>
[Script("Turn Critter")]
public class Turn_Critter : SpellScript
{
    private readonly Spell _spell;
    private readonly Debuff _debuff = new debuff_rending();
    private readonly GlobalSpellMethods _spellMethod;

    public Turn_Critter(Spell spell) : base(spell)
    {
        _spell = spell;
        _spellMethod = new GlobalSpellMethods();
    }

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target)
    {
        if (sprite is not Aisling aisling) return;

        aisling.Cast(_spell, target);
        aisling.Show(Scope.NearbyAislings, new ServerFormat19(_spell.Template.Sound));

        foreach (var monster in aisling.MonstersNearby())
        {
            if (!monster.Template.MonsterRace.MonsterRaceIsSet(MonsterRace.LowerBeing)) continue;
            if (monster.Level >= 101) continue;
            _spellMethod.ElementalOnUse(sprite, target, _spell, 30);
            _spellMethod.AfflictionOnUse(sprite, target, _spell, _debuff);
            monster.Target = null;
            monster.Aggressive = false;
        }
    }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (!_spell.CanUse()) return;
        if (sprite is not Aisling playerAction) return;

        playerAction.ActionUsed = "Turn Critter";
        var client = playerAction.Client;
        _spellMethod.Train(client, _spell);
        var success = _spellMethod.Execute(client, _spell);

        if (success)
        {
            if (client.Aisling.IsInvisible && _spell.Template.PostQualifiers is PostQualifier.BreakInvisible or PostQualifier.Both)
            {
                client.Aisling.IsInvisible = false;
                client.UpdateDisplay();
            }

            OnSuccess(sprite, target);
        }
        else
        {
            _spellMethod.SpellOnFailed(playerAction, target, _spell);
        }
    }
}

/// <summary>
/// Turn Greater Undead: Rends 10 AC, Holy Damage, Turns Undead non-hostile, level up to 250
/// </summary>
[Script("Turn Greater Undead")]
public class Turn_Greater_Undead : SpellScript
{
    private readonly Spell _spell;
    private readonly Debuff _debuff = new debuff_rending();
    private readonly GlobalSpellMethods _spellMethod;

    public Turn_Greater_Undead(Spell spell) : base(spell)
    {
        _spell = spell;
        _spellMethod = new GlobalSpellMethods();
    }

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target)
    {
        if (sprite is not Aisling aisling) return;

        aisling.Cast(_spell, target);
        aisling.Show(Scope.NearbyAislings, new ServerFormat19(_spell.Template.Sound));

        foreach (var monster in aisling.MonstersNearby())
        {
            if (!monster.Template.MonsterRace.MonsterRaceIsSet(MonsterRace.Undead)) continue;
            if (monster.Level >= 251) continue;
            _spellMethod.ElementalOnUse(sprite, target, _spell, 50);
            _spellMethod.AfflictionOnUse(sprite, target, _spell, _debuff);
            monster.Target = null;
            monster.Aggressive = false;
        }
    }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (!_spell.CanUse()) return;
        if (sprite is not Aisling playerAction) return;

        playerAction.ActionUsed = "Turn Undead";
        var client = playerAction.Client;
        _spellMethod.Train(client, _spell);
        var success = _spellMethod.Execute(client, _spell);

        if (success)
        {
            if (client.Aisling.IsInvisible && _spell.Template.PostQualifiers is PostQualifier.BreakInvisible or PostQualifier.Both)
            {
                client.Aisling.IsInvisible = false;
                client.UpdateDisplay();
            }

            OnSuccess(sprite, target);
        }
        else
        {
            _spellMethod.SpellOnFailed(playerAction, target, _spell);
        }
    }
}

/// <summary>
/// Turn Greater Critter: Rends 10 AC, Holy Damage, Turns Critters non-hostile, level up to 250
/// </summary>
[Script("Turn Greater Critter")]
public class Turn_Greater_Critter : SpellScript
{
    private readonly Spell _spell;
    private readonly Debuff _debuff = new debuff_rending();
    private readonly GlobalSpellMethods _spellMethod;

    public Turn_Greater_Critter(Spell spell) : base(spell)
    {
        _spell = spell;
        _spellMethod = new GlobalSpellMethods();
    }

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target)
    {
        if (sprite is not Aisling aisling) return;

        aisling.Cast(_spell, target);
        aisling.Show(Scope.NearbyAislings, new ServerFormat19(_spell.Template.Sound));

        foreach (var monster in aisling.MonstersNearby())
        {
            if (!monster.Template.MonsterRace.MonsterRaceIsSet(MonsterRace.LowerBeing)) continue;
            if (monster.Level >= 251) continue;
            _spellMethod.ElementalOnUse(sprite, target, _spell, 50);
            _spellMethod.AfflictionOnUse(sprite, target, _spell, _debuff);
            monster.Target = null;
            monster.Aggressive = false;
        }
    }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (!_spell.CanUse()) return;
        if (sprite is not Aisling playerAction) return;

        playerAction.ActionUsed = "Turn Critter";
        var client = playerAction.Client;
        _spellMethod.Train(client, _spell);
        var success = _spellMethod.Execute(client, _spell);

        if (success)
        {
            if (client.Aisling.IsInvisible && _spell.Template.PostQualifiers is PostQualifier.BreakInvisible or PostQualifier.Both)
            {
                client.Aisling.IsInvisible = false;
                client.UpdateDisplay();
            }

            OnSuccess(sprite, target);
        }
        else
        {
            _spellMethod.SpellOnFailed(playerAction, target, _spell);
        }
    }
}

[Script("Ao Puinsein")]
public class AoPuinsein : SpellScript
{
    private readonly Spell _spell;
    private readonly GlobalSpellMethods _spellMethod;

    public AoPuinsein(Spell spell) : base(spell)
    {
        _spell = spell;
        _spellMethod = new GlobalSpellMethods();
    }

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target)
    {
        if (sprite is not Aisling aisling) return;
        var client = aisling.Client;
        var cursed = target.HasDebuff("Ard Puinsein") || target.HasDebuff("Mor Puinsein") ||
                     target.HasDebuff("Puinsein") || target.HasDebuff("Beag Puinsein");

        client.SendServerMessage(ServerMessageType.OrangeBar1, $"Cast {Spell.Template.Name}");

        if (cursed)
            if (target is Aisling targetAisling)
                targetaisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, $"{aisling.Username} cured your ailment.");

        client.SendAnimation(Spell.Template.Animation, target, aisling);

        var action = new ServerFormat1A
        {
            Serial = aisling.Serial,
            Number = (byte)(client.Aisling.Path switch
            {
                Class.Cleric => 0x80,
                Class.Arcanus => 0x88,
                _ => 0x06
            }),
            Speed = 30
        };

        client.Aisling.Show(Scope.NearbyAislings, action);
        client.Aisling.Show(Scope.NearbyAislings, new ServerFormat19(Spell.Template.Sound));

        if (!cursed) return;
        var aoDebuff = target.GetDebuffName(i => i.Name.Contains("Puinsein"));
        if (aoDebuff == string.Empty) return;

        foreach (var debuffs in ServerSetup.Instance.GlobalDeBuffCache.Values)
        {
            if (debuffs.Name != aoDebuff) continue;
            debuffs.OnEnded(target, debuffs);
        }
    }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (!_spell.CanUse()) return;

        if (sprite is Aisling aisling)
        {
            var client = aisling.Client;
            _spellMethod.Train(client, _spell);

            if (aisling.CurrentMp - _spell.Template.ManaCost > 0)
            {
                aisling.CurrentMp -= _spell.Template.ManaCost;
            }
            else
            {
                client.SendServerMessage(ServerMessageType.OrangeBar1, $"{ServerSetup.Instance.Config.NoManaMessage}");
                return;
            }

            if (aisling.CurrentMp < 0)
                aisling.CurrentMp = 0;

            var success = _spellMethod.Execute(client, _spell);

            if (success)
            {
                if (client.Aisling.IsInvisible && _spell.Template.PostQualifiers is PostQualifier.BreakInvisible or PostQualifier.Both)
                {
                    client.Aisling.IsInvisible = false;
                    client.UpdateDisplay();
                }

                OnSuccess(sprite, target);
            }
            else
            {
                _spellMethod.SpellOnFailed(aisling, target, _spell);
            }


            client.SendAttributes(StatUpdateType.Vitality);
        }
        else
        {
            if (sprite.CurrentMp - _spell.Template.ManaCost > 0)
            {
                sprite.CurrentMp -= _spell.Template.ManaCost;
            }

            if (sprite.CurrentMp < 0)
                sprite.CurrentMp = 0;

            OnSuccess(sprite, target);
        }
    }
}

[Script("Ao Dall")]
public class AoDall : SpellScript
{
    private readonly Spell _spell;
    private readonly GlobalSpellMethods _spellMethod;

    public AoDall(Spell spell) : base(spell)
    {
        _spell = spell;
        _spellMethod = new GlobalSpellMethods();
    }

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target)
    {
        if (sprite is not Aisling aisling) return;
        var client = aisling.Client;
        var blind = target.HasDebuff("Blind");

        client.SendServerMessage(ServerMessageType.OrangeBar1, $"Cast {Spell.Template.Name}");

        if (blind)
            if (target is Aisling targetAisling)
                targetaisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, $"{aisling.Username} cured your ailment.");

        client.SendAnimation(Spell.Template.Animation, target, aisling);

        var action = new ServerFormat1A
        {
            Serial = aisling.Serial,
            Number = (byte)(client.Aisling.Path switch
            {
                Class.Cleric => 0x80,
                Class.Arcanus => 0x88,
                _ => 0x06
            }),
            Speed = 30
        };

        client.Aisling.Show(Scope.NearbyAislings, action);
        client.Aisling.Show(Scope.NearbyAislings, new ServerFormat19(Spell.Template.Sound));

        if (!blind) return;
        var aoDebuff = target.GetDebuffName(i => i.Name.Contains("Blind"));
        if (aoDebuff == string.Empty) return;

        foreach (var debuffs in ServerSetup.Instance.GlobalDeBuffCache.Values)
        {
            if (debuffs.Name != aoDebuff) continue;
            debuffs.OnEnded(target, debuffs);
        }
    }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (!_spell.CanUse()) return;

        if (sprite is Aisling aisling)
        {
            var client = aisling.Client;
            _spellMethod.Train(client, _spell);

            if (aisling.CurrentMp - _spell.Template.ManaCost > 0)
            {
                aisling.CurrentMp -= _spell.Template.ManaCost;
            }
            else
            {
                client.SendServerMessage(ServerMessageType.OrangeBar1, $"{ServerSetup.Instance.Config.NoManaMessage}");
                return;
            }

            if (aisling.CurrentMp < 0)
                aisling.CurrentMp = 0;

            var success = _spellMethod.Execute(client, _spell);

            if (success)
            {
                if (client.Aisling.IsInvisible && _spell.Template.PostQualifiers is PostQualifier.BreakInvisible or PostQualifier.Both)
                {
                    client.Aisling.IsInvisible = false;
                    client.UpdateDisplay();
                }

                OnSuccess(sprite, target);
            }
            else
            {
                _spellMethod.SpellOnFailed(aisling, target, _spell);
            }


            client.SendAttributes(StatUpdateType.Vitality);
        }
        else
        {
            if (sprite.CurrentMp - _spell.Template.ManaCost > 0)
            {
                sprite.CurrentMp -= _spell.Template.ManaCost;
            }

            if (sprite.CurrentMp < 0)
                sprite.CurrentMp = 0;

            OnSuccess(sprite, target);
        }
    }
}

[Script("Ao Beag Cradh")]
public class AoBeagCradh : SpellScript
{
    private readonly Spell _spell;
    private readonly GlobalSpellMethods _spellMethod;

    public AoBeagCradh(Spell spell) : base(spell)
    {
        _spell = spell;
        _spellMethod = new GlobalSpellMethods();
    }

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target)
    {
        if (sprite is not Aisling aisling) return;
        var client = aisling.Client;
        var cursed = target.HasDebuff("Beag Cradh");

        client.SendServerMessage(ServerMessageType.OrangeBar1, $"Cast {Spell.Template.Name}");

        if (cursed)
            if (target is Aisling targetAisling)
                targetaisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, $"{aisling.Username} cured your curse mark");

        client.SendAnimation(Spell.Template.Animation, target, aisling);

        var action = new ServerFormat1A
        {
            Serial = aisling.Serial,
            Number = (byte)(client.Aisling.Path switch
            {
                Class.Cleric => 0x80,
                Class.Arcanus => 0x88,
                _ => 0x06
            }),
            Speed = 30
        };

        client.Aisling.Show(Scope.NearbyAislings, action);
        client.Aisling.Show(Scope.NearbyAislings, new ServerFormat19(Spell.Template.Sound));

        if (!cursed) return;
        var aoDebuff = target.GetDebuffName(i => i.Name == "Beag Cradh");
        if (aoDebuff == string.Empty) return;

        foreach (var debuffs in ServerSetup.Instance.GlobalDeBuffCache.Values)
        {
            if (debuffs.Name != aoDebuff) continue;
            debuffs.OnEnded(target, debuffs);
        }
    }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (!_spell.CanUse()) return;

        if (sprite is Aisling aisling)
        {
            var client = aisling.Client;
            _spellMethod.Train(client, _spell);

            if (aisling.CurrentMp - _spell.Template.ManaCost > 0)
            {
                aisling.CurrentMp -= _spell.Template.ManaCost;
            }
            else
            {
                client.SendServerMessage(ServerMessageType.OrangeBar1, $"{ServerSetup.Instance.Config.NoManaMessage}");
                return;
            }

            if (aisling.CurrentMp < 0)
                aisling.CurrentMp = 0;

            var success = _spellMethod.Execute(client, _spell);

            if (success)
            {
                if (client.Aisling.IsInvisible && _spell.Template.PostQualifiers is PostQualifier.BreakInvisible or PostQualifier.Both)
                {
                    client.Aisling.IsInvisible = false;
                    client.UpdateDisplay();
                }

                OnSuccess(sprite, target);
            }
            else
            {
                _spellMethod.SpellOnFailed(aisling, target, _spell);
            }


            client.SendAttributes(StatUpdateType.Vitality);
        }
        else
        {
            if (sprite.CurrentMp - _spell.Template.ManaCost > 0)
            {
                sprite.CurrentMp -= _spell.Template.ManaCost;
            }

            if (sprite.CurrentMp < 0)
                sprite.CurrentMp = 0;

            OnSuccess(sprite, target);
        }
    }
}

[Script("Ao Cradh")]
public class AoCradh : SpellScript
{
    private readonly Spell _spell;
    private readonly GlobalSpellMethods _spellMethod;

    public AoCradh(Spell spell) : base(spell)
    {
        _spell = spell;
        _spellMethod = new GlobalSpellMethods();
    }

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target)
    {
        if (sprite is not Aisling aisling) return;
        var client = aisling.Client;
        var cursed = target.HasDebuff("Cradh");

        client.SendServerMessage(ServerMessageType.OrangeBar1, $"Cast {Spell.Template.Name}");

        if (cursed)
            if (target is Aisling targetAisling)
                targetaisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, $"{aisling.Username} cured your curse mark");

        client.SendAnimation(Spell.Template.Animation, target, aisling);

        var action = new ServerFormat1A
        {
            Serial = aisling.Serial,
            Number = (byte)(client.Aisling.Path switch
            {
                Class.Cleric => 0x80,
                Class.Arcanus => 0x88,
                _ => 0x06
            }),
            Speed = 30
        };

        client.Aisling.Show(Scope.NearbyAislings, action);
        client.Aisling.Show(Scope.NearbyAislings, new ServerFormat19(Spell.Template.Sound));

        if (!cursed) return;
        var aoDebuff = target.GetDebuffName(i => i.Name == "Cradh");
        if (aoDebuff == string.Empty) return;

        foreach (var debuffs in ServerSetup.Instance.GlobalDeBuffCache.Values)
        {
            if (debuffs.Name != aoDebuff) continue;
            debuffs.OnEnded(target, debuffs);
        }
    }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (!_spell.CanUse()) return;

        if (sprite is Aisling aisling)
        {
            var client = aisling.Client;
            _spellMethod.Train(client, _spell);

            if (aisling.CurrentMp - _spell.Template.ManaCost > 0)
            {
                aisling.CurrentMp -= _spell.Template.ManaCost;
            }
            else
            {
                client.SendServerMessage(ServerMessageType.OrangeBar1, $"{ServerSetup.Instance.Config.NoManaMessage}");
                return;
            }

            if (aisling.CurrentMp < 0)
                aisling.CurrentMp = 0;

            var success = _spellMethod.Execute(client, _spell);

            if (success)
            {
                if (client.Aisling.IsInvisible && _spell.Template.PostQualifiers is PostQualifier.BreakInvisible or PostQualifier.Both)
                {
                    client.Aisling.IsInvisible = false;
                    client.UpdateDisplay();
                }

                OnSuccess(sprite, target);
            }
            else
            {
                _spellMethod.SpellOnFailed(aisling, target, _spell);
            }


            client.SendAttributes(StatUpdateType.Vitality);
        }
        else
        {
            if (sprite.CurrentMp - _spell.Template.ManaCost > 0)
            {
                sprite.CurrentMp -= _spell.Template.ManaCost;
            }

            if (sprite.CurrentMp < 0)
                sprite.CurrentMp = 0;

            OnSuccess(sprite, target);
        }
    }
}

[Script("Ao Mor Cradh")]
public class AoMorCradh : SpellScript
{
    private readonly Spell _spell;
    private readonly GlobalSpellMethods _spellMethod;

    public AoMorCradh(Spell spell) : base(spell)
    {
        _spell = spell;
        _spellMethod = new GlobalSpellMethods();
    }

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target)
    {
        if (sprite is not Aisling aisling) return;
        var client = aisling.Client;
        var cursed = target.HasDebuff("Mor Cradh");

        client.SendServerMessage(ServerMessageType.OrangeBar1, $"Cast {Spell.Template.Name}");

        if (cursed)
            if (target is Aisling targetAisling)
                targetaisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, $"{aisling.Username} cured your curse mark");

        client.SendAnimation(Spell.Template.Animation, target, aisling);

        var action = new ServerFormat1A
        {
            Serial = aisling.Serial,
            Number = (byte)(client.Aisling.Path switch
            {
                Class.Cleric => 0x80,
                Class.Arcanus => 0x88,
                _ => 0x06
            }),
            Speed = 30
        };

        client.Aisling.Show(Scope.NearbyAislings, action);
        client.Aisling.Show(Scope.NearbyAislings, new ServerFormat19(Spell.Template.Sound));

        if (!cursed) return;
        var aoDebuff = target.GetDebuffName(i => i.Name == "Mor Cradh");
        if (aoDebuff == string.Empty) return;

        foreach (var debuffs in ServerSetup.Instance.GlobalDeBuffCache.Values)
        {
            if (debuffs.Name != aoDebuff) continue;
            debuffs.OnEnded(target, debuffs);
        }
    }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (!_spell.CanUse()) return;

        if (sprite is Aisling aisling)
        {
            var client = aisling.Client;
            _spellMethod.Train(client, _spell);

            if (aisling.CurrentMp - _spell.Template.ManaCost > 0)
            {
                aisling.CurrentMp -= _spell.Template.ManaCost;
            }
            else
            {
                client.SendServerMessage(ServerMessageType.OrangeBar1, $"{ServerSetup.Instance.Config.NoManaMessage}");
                return;
            }

            if (aisling.CurrentMp < 0)
                aisling.CurrentMp = 0;

            var success = _spellMethod.Execute(client, _spell);

            if (success)
            {
                if (client.Aisling.IsInvisible && _spell.Template.PostQualifiers is PostQualifier.BreakInvisible or PostQualifier.Both)
                {
                    client.Aisling.IsInvisible = false;
                    client.UpdateDisplay();
                }

                OnSuccess(sprite, target);
            }
            else
            {
                _spellMethod.SpellOnFailed(aisling, target, _spell);
            }


            client.SendAttributes(StatUpdateType.Vitality);
        }
        else
        {
            if (sprite.CurrentMp - _spell.Template.ManaCost > 0)
            {
                sprite.CurrentMp -= _spell.Template.ManaCost;
            }

            if (sprite.CurrentMp < 0)
                sprite.CurrentMp = 0;

            OnSuccess(sprite, target);
        }
    }
}

[Script("Ao Ard Cradh")]
public class AoArdCradh : SpellScript
{
    private readonly Spell _spell;
    private readonly GlobalSpellMethods _spellMethod;

    public AoArdCradh(Spell spell) : base(spell)
    {
        _spell = spell;
        _spellMethod = new GlobalSpellMethods();
    }

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target)
    {
        if (sprite is not Aisling aisling) return;
        var client = aisling.Client;
        var cursed = target.HasDebuff("Ard Cradh");

        client.SendServerMessage(ServerMessageType.OrangeBar1, $"Cast {Spell.Template.Name}");

        if (cursed)
            if (target is Aisling targetAisling)
                targetaisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, $"{aisling.Username} cured your curse mark");

        client.SendAnimation(Spell.Template.Animation, target, aisling);

        var action = new ServerFormat1A
        {
            Serial = aisling.Serial,
            Number = (byte)(client.Aisling.Path switch
            {
                Class.Cleric => 0x80,
                Class.Arcanus => 0x88,
                _ => 0x06
            }),
            Speed = 30
        };

        client.Aisling.Show(Scope.NearbyAislings, action);
        client.Aisling.Show(Scope.NearbyAislings, new ServerFormat19(Spell.Template.Sound));

        if (!cursed) return;
        var aoDebuff = target.GetDebuffName(i => i.Name == "Ard Cradh");
        if (aoDebuff == string.Empty) return;

        foreach (var debuffs in ServerSetup.Instance.GlobalDeBuffCache.Values)
        {
            if (debuffs.Name != aoDebuff) continue;
            debuffs.OnEnded(target, debuffs);
        }
    }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (!_spell.CanUse()) return;

        if (sprite is Aisling aisling)
        {
            var client = aisling.Client;
            _spellMethod.Train(client, _spell);

            if (aisling.CurrentMp - _spell.Template.ManaCost > 0)
            {
                aisling.CurrentMp -= _spell.Template.ManaCost;
            }
            else
            {
                client.SendServerMessage(ServerMessageType.OrangeBar1, $"{ServerSetup.Instance.Config.NoManaMessage}");
                return;
            }

            if (aisling.CurrentMp < 0)
                aisling.CurrentMp = 0;

            var success = _spellMethod.Execute(client, _spell);

            if (success)
            {
                if (client.Aisling.IsInvisible && _spell.Template.PostQualifiers is PostQualifier.BreakInvisible or PostQualifier.Both)
                {
                    client.Aisling.IsInvisible = false;
                    client.UpdateDisplay();
                }

                OnSuccess(sprite, target);
            }
            else
            {
                _spellMethod.SpellOnFailed(aisling, target, _spell);
            }


            client.SendAttributes(StatUpdateType.Vitality);
        }
        else
        {
            if (sprite.CurrentMp - _spell.Template.ManaCost > 0)
            {
                sprite.CurrentMp -= _spell.Template.ManaCost;
            }

            if (sprite.CurrentMp < 0)
                sprite.CurrentMp = 0;

            OnSuccess(sprite, target);
        }
    }
}

[Script("Ao Suain")]
public class AoSuain : SpellScript
{
    private readonly Spell _spell;
    private readonly GlobalSpellMethods _spellMethod;

    public AoSuain(Spell spell) : base(spell)
    {
        _spell = spell;
        _spellMethod = new GlobalSpellMethods();
    }

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target)
    {
        if (sprite is not Aisling aisling) return;
        var client = aisling.Client;
        var cursed = target.HasDebuff("Suain");

        client.SendServerMessage(ServerMessageType.OrangeBar1, $"Cast {Spell.Template.Name}");

        if (cursed)
            if (target is Aisling targetAisling)
                targetaisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, $"{aisling.Username} removed your paralysis");

        client.SendAnimation(Spell.Template.Animation, target, aisling);

        var action = new ServerFormat1A
        {
            Serial = aisling.Serial,
            Number = (byte)(client.Aisling.Path switch
            {
                Class.Cleric => 0x80,
                Class.Arcanus => 0x88,
                _ => 0x06
            }),
            Speed = 30
        };

        client.Aisling.Show(Scope.NearbyAislings, action);
        client.Aisling.Show(Scope.NearbyAislings, new ServerFormat19(Spell.Template.Sound));

        if (!cursed) return;
        var aoDebuff = target.GetDebuffName(i => i.Name == "Suain");
        if (aoDebuff == string.Empty) return;

        foreach (var debuffs in ServerSetup.Instance.GlobalDeBuffCache.Values)
        {
            if (debuffs.Name != aoDebuff) continue;
            debuffs.OnEnded(target, debuffs);
        }
    }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (!_spell.CanUse()) return;

        if (sprite is Aisling aisling)
        {
            var client = aisling.Client;
            _spellMethod.Train(client, _spell);

            if (aisling.CurrentMp - _spell.Template.ManaCost > 0)
            {
                aisling.CurrentMp -= _spell.Template.ManaCost;
            }
            else
            {
                client.SendServerMessage(ServerMessageType.OrangeBar1, $"{ServerSetup.Instance.Config.NoManaMessage}");
                return;
            }

            if (aisling.CurrentMp < 0)
                aisling.CurrentMp = 0;

            var success = _spellMethod.Execute(client, _spell);

            if (success)
            {
                if (client.Aisling.IsInvisible && _spell.Template.PostQualifiers is PostQualifier.BreakInvisible or PostQualifier.Both)
                {
                    client.Aisling.IsInvisible = false;
                    client.UpdateDisplay();
                }

                OnSuccess(sprite, target);
            }
            else
            {
                _spellMethod.SpellOnFailed(aisling, target, _spell);
            }


            client.SendAttributes(StatUpdateType.Vitality);
        }
        else
        {
            if (sprite.CurrentMp - _spell.Template.ManaCost > 0)
            {
                sprite.CurrentMp -= _spell.Template.ManaCost;
            }

            if (sprite.CurrentMp < 0)
                sprite.CurrentMp = 0;

            OnSuccess(sprite, target);
        }
    }
}

// Cleric Spells
// Sustain = prevent ally's death by keeping them at 1hp for 30 seconds then death is recast if not removed
// Phoenix Wave = red group, 24 hr cooldown
// Ray of Light = High Holy Damage
// Water of Life = Heal self based on missing health
// Booming Shield = stun any target that attacks you for a duration (3 seconds) (30 second cooldown)
