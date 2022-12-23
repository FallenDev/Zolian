using Darkages.Common;
using Darkages.Enums;
using Darkages.GameScripts.Affects;
using Darkages.Network.Formats.Models.ServerFormats;
using Darkages.Scripting;
using Darkages.Sprites;
using Darkages.Types;

namespace Darkages.GameScripts.Skills;

// Assassin Skills
[Script("Stab")]
public class Stab : SkillScript
{
    private readonly Skill _skill;
    private Sprite _target;
    private const int CritDmg = 3;
    private bool _crit;
    private bool _success;
    private readonly GlobalSkillMethods _skillMethod;

    public Stab(Skill skill) : base(skill)
    {
        _skill = skill;
        _skillMethod = new GlobalSkillMethods();
    }

    public override void OnFailed(Sprite sprite)
    {
        if (sprite is not Aisling damageDealingAisling) return;
        var client = damageDealingAisling.Client;

        client.SendMessage(0x02, "Stab missed the mark.");
        client.Aisling.Show(Scope.NearbyAislings, new ServerFormat29(_skill.Template.MissAnimation, damageDealingAisling.Pos));
    }

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling damageDealingSprite) return;
        var client = damageDealingSprite.Client;
        damageDealingSprite.ActionUsed = "Stab";

        var action = new ServerFormat1A
        {
            Serial = client.Aisling.Serial,
            Number = (byte)(client.Aisling.Path == Class.Assassin
                ? 0x86
                : 0x01),
            Speed = 25
        };

        var enemy = client.Aisling.DamageableGetInFront();

        if (enemy.Count == 0)
        {
            _skillMethod.FailedAttempt(client, damageDealingSprite, _skill, action);
            OnFailed(damageDealingSprite);
            return;
        }

        foreach (var i in enemy.Where(i => i.Attackable))
        {
            _target = Skill.Reflect(i, damageDealingSprite, _skill);
            if (_target == null) continue;

            var dmgCalc = DamageCalc(damageDealingSprite);

            if (damageDealingSprite.Invisible)
                dmgCalc *= 2;

            if (client.Aisling.Invisible && _skill.Template.PostQualifiers is PostQualifier.BreakInvisible or PostQualifier.Both)
            {
                client.Aisling.Invisible = false;
                client.UpdateDisplay();
            }

            _target.ApplyDamage(damageDealingSprite, dmgCalc, _skill);

            damageDealingSprite.Client.Aisling.Show(Scope.NearbyAislings, new ServerFormat29(_skill.Template.TargetAnimation, _target.Pos));
            _skillMethod.Train(client, _skill);

            if (!_crit) continue;
            client.Aisling.Animate(387);
            _crit = false;
        }

        client.Aisling.Show(Scope.NearbyAislings, action);
    }

    public override void OnUse(Sprite sprite)
    {
        if (!_skill.CanUse()) return;

        if (sprite is Aisling aisling)
        {
            _success = _skillMethod.OnUse(aisling, _skill);

            if (_success)
            {
                OnSuccess(aisling);
            }
            else
            {
                OnFailed(aisling);
            }
        }
        else
        {
            var enemy = sprite.MonsterGetInFront();

            var action = new ServerFormat1A
            {
                Serial = sprite.Serial,
                Number = 0x01,
                Speed = 30
            };

            if (enemy == null) return;

            foreach (var i in enemy.Where(i => i != null && sprite.Serial != i.Serial && i.Attackable))
            {
                _target = Skill.Reflect(i, sprite, _skill);

                var dmgCalc = DamageCalc(sprite);

                _target.ApplyDamage(sprite, dmgCalc, _skill);

                if (_skill.Template.TargetAnimation > 0)
                    if (_target is Monster or Mundane or Aisling)
                        sprite.Show(Scope.NearbyAislings,
                            new ServerFormat29((uint)sprite.Serial, (uint)_target.Serial,
                                _skill.Template.TargetAnimation, 0, 100));

                sprite.Show(Scope.NearbyAislings, action);
            }
        }
    }

    private int DamageCalc(Sprite sprite)
    {
        if (sprite is Aisling damageDealingAisling)
        {
            var client = damageDealingAisling.Client;

            var imp = 50 + _skill.Level;
            var dmg = client.Aisling.Str * 2 + client.Aisling.Dex * 3;

            dmg += dmg * imp / 100;

            var critRoll = Generator.RandNumGen100();
            {
                if (critRoll < 95) return dmg;
                dmg *= CritDmg;
                _crit = true;
            }

            return dmg;
        }
        else
        {
            if (sprite is not Monster damageMonster) return 0;

            var dmg = damageMonster.Str * 2;
            dmg += damageMonster.Dex * 3;

            var critRoll = Generator.RandNumGen100();
            {
                if (critRoll < 95) return dmg;
                dmg *= CritDmg;
                _crit = true;
            }

            return dmg;
        }
    }
}

[Script("Stab Twice")]
public class Stab_Twice : SkillScript
{
    private readonly Skill _skill;
    private Sprite _target;
    private const int CritDmg = 3;
    private bool _crit;
    private bool _success;
    private readonly GlobalSkillMethods _skillMethod;

    public Stab_Twice(Skill skill) : base(skill)
    {
        _skill = skill;
        _skillMethod = new GlobalSkillMethods();
    }

    public override void OnFailed(Sprite sprite)
    {
        if (sprite is not Aisling damageDealingAisling) return;
        var client = damageDealingAisling.Client;

        client.SendMessage(0x02, "Double Stab missed the mark.");
        client.Aisling.Show(Scope.NearbyAislings, new ServerFormat29(_skill.Template.MissAnimation, damageDealingAisling.Pos));
    }

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling damageDealingSprite) return;
        var client = damageDealingSprite.Client;
        damageDealingSprite.ActionUsed = "Stab Twice";

        var action = new ServerFormat1A
        {
            Serial = client.Aisling.Serial,
            Number = (byte)(client.Aisling.Path == Class.Assassin
                ? 0x87
                : 0x01),
            Speed = 25
        };

        var enemy = client.Aisling.DamageableGetInFront();

        if (enemy.Count == 0)
        {
            _skillMethod.FailedAttempt(client, damageDealingSprite, _skill, action);
            OnFailed(damageDealingSprite);
            return;
        }

        foreach (var i in enemy.Where(i => i.Attackable))
        {
            _target = Skill.Reflect(i, damageDealingSprite, _skill);
            if (_target == null) continue;

            var dmgCalc = DamageCalc(damageDealingSprite);

            if (damageDealingSprite.Invisible)
                dmgCalc *= 2;

            if (client.Aisling.Invisible && _skill.Template.PostQualifiers is PostQualifier.BreakInvisible or PostQualifier.Both)
            {
                client.Aisling.Invisible = false;
                client.UpdateDisplay();
            }

            _target.ApplyDamage(damageDealingSprite, dmgCalc, _skill);
            _target.ApplyDamage(damageDealingSprite, dmgCalc, _skill);

            damageDealingSprite.Client.Aisling.Show(Scope.NearbyAislings, new ServerFormat29(_skill.Template.TargetAnimation, _target.Pos));
            _skillMethod.Train(client, _skill);

            if (!_crit) continue;
            client.Aisling.Animate(387);
            _crit = false;
        }

        client.Aisling.Show(Scope.NearbyAislings, action);
    }

    public override void OnUse(Sprite sprite)
    {
        if (!_skill.CanUse()) return;

        if (sprite is Aisling aisling)
        {
            _success = _skillMethod.OnUse(aisling, _skill);

            if (_success)
            {
                OnSuccess(aisling);
            }
            else
            {
                OnFailed(aisling);
            }
        }
        else
        {
            var enemy = sprite.MonsterGetInFront(4);

            var action = new ServerFormat1A
            {
                Serial = sprite.Serial,
                Number = 0x01,
                Speed = 30
            };

            if (enemy == null) return;

            foreach (var i in enemy.Where(i => i != null && sprite.Serial != i.Serial && i.Attackable))
            {
                _target = Skill.Reflect(i, sprite, _skill);

                var dmgCalc = DamageCalc(sprite);

                _target.ApplyDamage(sprite, dmgCalc, _skill);

                if (_skill.Template.TargetAnimation > 0)
                    if (_target is Monster or Mundane or Aisling)
                        sprite.Show(Scope.NearbyAislings,
                            new ServerFormat29((uint)sprite.Serial, (uint)_target.Serial,
                                _skill.Template.TargetAnimation, 0, 100));

                sprite.Show(Scope.NearbyAislings, action);
            }
        }
    }

    private int DamageCalc(Sprite sprite)
    {
        if (sprite is Aisling damageDealingAisling)
        {
            var client = damageDealingAisling.Client;

            var imp = 50 + _skill.Level;
            var dmg = client.Aisling.Str * 1 + client.Aisling.Dex * 3;

            dmg += dmg * imp / 100;

            var critRoll = Generator.RandNumGen100();
            {
                if (critRoll < 95) return dmg;
                dmg *= CritDmg;
                _crit = true;
            }

            return dmg;
        }
        else
        {
            if (sprite is not Monster damageMonster) return 0;

            var dmg = damageMonster.Str * 1;
            dmg += damageMonster.Dex * 3;

            var critRoll = Generator.RandNumGen100();
            {
                if (critRoll < 95) return dmg;
                dmg *= CritDmg;
                _crit = true;
            }

            return dmg;
        }
    }
}

[Script("Stab'n Twist")]
public class Stab_and_Twist : SkillScript
{
    private readonly Skill _skill;
    private Sprite _target;
    private const int CritDmg = 3;
    private bool _crit;
    private bool _success;
    private readonly GlobalSkillMethods _skillMethod;

    public Stab_and_Twist(Skill skill) : base(skill)
    {
        _skill = skill;
        _skillMethod = new GlobalSkillMethods();
    }

    public override void OnFailed(Sprite sprite)
    {
        if (sprite is not Aisling damageDealingAisling) return;
        var client = damageDealingAisling.Client;

        client.SendMessage(0x02, "Stab'n Twist missed the mark.");
        client.Aisling.Show(Scope.NearbyAislings, new ServerFormat29(_skill.Template.MissAnimation, damageDealingAisling.Pos));
    }

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling damageDealingSprite) return;
        var client = damageDealingSprite.Client;
        damageDealingSprite.ActionUsed = "Stab'n Twist";

        var action = new ServerFormat1A
        {
            Serial = client.Aisling.Serial,
            Number = (byte)(client.Aisling.Path == Class.Assassin
                ? 0x86
                : 0x01),
            Speed = 25
        };

        var enemy = client.Aisling.DamageableGetInFront();

        if (enemy.Count == 0)
        {
            _skillMethod.FailedAttempt(client, damageDealingSprite, _skill, action);
            OnFailed(damageDealingSprite);
            return;
        }

        foreach (var i in enemy.Where(i => i.Attackable))
        {
            _target = Skill.Reflect(i, damageDealingSprite, _skill);
            if (_target == null) continue;

            var dmgCalc = DamageCalc(damageDealingSprite);

            if (damageDealingSprite.Invisible)
                dmgCalc *= 2;

            if (client.Aisling.Invisible && _skill.Template.PostQualifiers is PostQualifier.BreakInvisible or PostQualifier.Both)
            {
                client.Aisling.Invisible = false;
                client.UpdateDisplay();
            }

            var debuff = new debuff_rend();
            if (!_target.HasDebuff(debuff.Name)) debuff.OnApplied(_target, debuff);
            damageDealingSprite.Client.Send(new ServerFormat08(damageDealingSprite, StatusFlags.StructD));

            _target.ApplyDamage(damageDealingSprite, dmgCalc, _skill);

            damageDealingSprite.Client.Aisling.Show(Scope.NearbyAislings, new ServerFormat29(_skill.Template.TargetAnimation, _target.Pos));
            _skillMethod.Train(client, _skill);

            if (!_crit) continue;
            client.Aisling.Animate(387);
            _crit = false;
        }

        client.Aisling.Show(Scope.NearbyAislings, action);
    }

    public override void OnUse(Sprite sprite)
    {
        if (!_skill.CanUse()) return;

        if (sprite is Aisling aisling)
        {
            _success = _skillMethod.OnUse(aisling, _skill);

            if (_success)
            {
                OnSuccess(aisling);
            }
            else
            {
                OnFailed(aisling);
            }
        }
        else
        {
            var enemy = sprite.MonsterGetInFront(4);

            var action = new ServerFormat1A
            {
                Serial = sprite.Serial,
                Number = 0x01,
                Speed = 30
            };

            if (enemy == null) return;

            foreach (var i in enemy.Where(i => i != null && sprite.Serial != i.Serial && i.Attackable))
            {
                _target = Skill.Reflect(i, sprite, _skill);

                var dmgCalc = DamageCalc(sprite);

                _target.ApplyDamage(sprite, dmgCalc, _skill);

                if (_skill.Template.TargetAnimation > 0)
                    if (_target is Monster or Mundane or Aisling)
                        sprite.Show(Scope.NearbyAislings,
                            new ServerFormat29((uint)sprite.Serial, (uint)_target.Serial,
                                _skill.Template.TargetAnimation, 0, 100));

                sprite.Show(Scope.NearbyAislings, action);
            }
        }
    }

    private int DamageCalc(Sprite sprite)
    {
        if (sprite is Aisling damageDealingAisling)
        {
            var client = damageDealingAisling.Client;

            var imp = 50 + _skill.Level;
            var dmg = client.Aisling.Str * 2 + client.Aisling.Dex * 5;

            dmg += dmg * imp / 100;

            var critRoll = Generator.RandNumGen100();
            {
                if (critRoll < 95) return dmg;
                dmg *= CritDmg;
                _crit = true;
            }

            return dmg;
        }
        else
        {
            if (sprite is not Monster damageMonster) return 0;

            var dmg = damageMonster.Str * 2;
            dmg += damageMonster.Dex * 5;

            var critRoll = Generator.RandNumGen100();
            {
                if (critRoll < 95) return dmg;
                dmg *= CritDmg;
                _crit = true;
            }

            return dmg;
        }
    }
}

[Script("Sneak")]
public class Sneak : SkillScript
{
    private readonly Skill _skill;
    private bool _success;
    private readonly GlobalSkillMethods _skillMethod;

    public Sneak(Skill skill) : base(skill)
    {
        _skill = skill;
        _skillMethod = new GlobalSkillMethods();
    }

    public override void OnFailed(Sprite sprite)
    {
        if (sprite is not Aisling damageDealingAisling) return;
        var client = damageDealingAisling.Client;
        client.SendMessage(0x02, "Failed to meld into the shadows.");
    }

    public override void OnSuccess(Sprite sprite)
    {
        switch (sprite)
        {
            case Aisling damageDealingSprite:
                {
                    var client = damageDealingSprite.Client;

                    if (client.Aisling.Dead || client.Aisling.Invisible) return;
                    var buff = new buff_hide();
                    buff.OnApplied(client.Aisling, buff);
                    _skillMethod.Train(client, _skill);
                    client.Aisling.Show(Scope.NearbyAislings, new ServerFormat29(_skill.Template.TargetAnimation, damageDealingSprite.Pos));
                    break;
                }
            case Monster monster:
                {
                    // ToDo: Add logic so monsters can hide here
                    break;
                }
        }
    }

    public override void OnUse(Sprite sprite)
    {
        if (!_skill.CanUse()) return;

        if (sprite is Aisling aisling)
        {
            _success = _skillMethod.OnUse(aisling, _skill);

            if (_success)
            {
                OnSuccess(aisling);
            }
            else
            {
                OnFailed(aisling);
            }
        }
        else
        {
            OnSuccess(sprite);
        }
    }
}

[Script("Flurry")]
public class Flurry : SkillScript
{
    private readonly Skill _skill;
    private Sprite _target;
    private const int CritDmg = 3;
    private bool _crit;
    private bool _success;
    private readonly GlobalSkillMethods _skillMethod;

    public Flurry(Skill skill) : base(skill)
    {
        _skill = skill;
        _skillMethod = new GlobalSkillMethods();
    }

    public override void OnFailed(Sprite sprite)
    {
        if (sprite is not Aisling damageDealingAisling) return;
        var client = damageDealingAisling.Client;

        client.SendMessage(0x02, "Missed.");
        client.Aisling.Show(Scope.NearbyAislings, new ServerFormat29(_skill.Template.MissAnimation, damageDealingAisling.Pos));
    }

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling damageDealingSprite) return;
        var client = damageDealingSprite.Client;
        damageDealingSprite.ActionUsed = "Flurry";

        var action = new ServerFormat1A
        {
            Serial = client.Aisling.Serial,
            Number = (byte)(client.Aisling.Path == Class.Assassin
                ? 0x87
                : 0x01),
            Speed = 25
        };

        var enemy = _skillMethod.GetInCone(damageDealingSprite);

        if (enemy.Length == 0)
        {
            _skillMethod.FailedAttempt(client, damageDealingSprite, _skill, action);
            OnFailed(damageDealingSprite);
            return;
        }

        foreach (var i in enemy.Where(i => i.Attackable))
        {
            _target = Skill.Reflect(i, damageDealingSprite, _skill);
            if (_target == null) continue;

            var dmgCalc = DamageCalc(damageDealingSprite);

            if (damageDealingSprite.Invisible)
                dmgCalc *= 2;

            if (client.Aisling.Invisible && _skill.Template.PostQualifiers is PostQualifier.BreakInvisible or PostQualifier.Both)
            {
                client.Aisling.Invisible = false;
                client.UpdateDisplay();
            }

            _target.ApplyDamage(damageDealingSprite, dmgCalc, _skill);
            _target.ApplyDamage(damageDealingSprite, dmgCalc, _skill);
            _target.ApplyDamage(damageDealingSprite, dmgCalc, _skill);
            _target.ApplyDamage(damageDealingSprite, dmgCalc, _skill);

            damageDealingSprite.Client.Aisling.Show(Scope.NearbyAislings, new ServerFormat29(_skill.Template.TargetAnimation, _target.Pos));
            _skillMethod.Train(client, _skill);

            if (!_crit) continue;
            client.Aisling.Animate(387);
            _crit = false;
        }

        client.Aisling.Show(Scope.NearbyAislings, action);
    }

    public override void OnUse(Sprite sprite)
    {
        if (!_skill.CanUse()) return;

        if (sprite is Aisling aisling)
        {
            _success = _skillMethod.OnUse(aisling, _skill);

            if (_success)
            {
                OnSuccess(aisling);
            }
            else
            {
                OnFailed(aisling);
            }
        }
        else
        {
            var enemy = _skillMethod.GetInCone(sprite);

            var action = new ServerFormat1A
            {
                Serial = sprite.Serial,
                Number = 0x01,
                Speed = 30
            };

            if (enemy == null) return;

            foreach (var i in enemy.Where(i => i != null && sprite.Serial != i.Serial && i.Attackable))
            {
                _target = Skill.Reflect(i, sprite, _skill);

                var dmgCalc = DamageCalc(sprite);

                _target.ApplyDamage(sprite, dmgCalc, _skill);
                _target.ApplyDamage(sprite, dmgCalc, _skill);
                _target.ApplyDamage(sprite, dmgCalc, _skill);
                _target.ApplyDamage(sprite, dmgCalc, _skill);

                if (_skill.Template.TargetAnimation > 0)
                    if (_target is Monster or Mundane or Aisling)
                        sprite.Show(Scope.NearbyAislings,
                            new ServerFormat29((uint)sprite.Serial, (uint)_target.Serial,
                                _skill.Template.TargetAnimation, 0, 100));

                sprite.Show(Scope.NearbyAislings, action);
            }
        }
    }

    private int DamageCalc(Sprite sprite)
    {
        if (sprite is Aisling damageDealingAisling)
        {
            var client = damageDealingAisling.Client;

            var imp = 50 + _skill.Level;
            var dmg = client.Aisling.Dex * 2 + client.Aisling.Int * 1;

            dmg += dmg * imp / 100;

            var critRoll = Generator.RandNumGen100();
            {
                if (critRoll < 95) return dmg;
                dmg *= CritDmg;
                _crit = true;
            }

            return dmg;
        }
        else
        {
            if (sprite is not Monster damageMonster) return 0;

            var dmg = damageMonster.Dex * 2;
            dmg += damageMonster.Int * 1;

            var critRoll = Generator.RandNumGen100();
            {
                if (critRoll < 95) return dmg;
                dmg *= CritDmg;
                _crit = true;
            }

            return dmg;
        }
    }
}

[Script("Entice")]
public class Enticed : SkillScript
{
    private readonly Skill _skill;
    private bool _success;
    private readonly GlobalSkillMethods _skillMethod;

    public Enticed(Skill skill) : base(skill)
    {
        _skill = skill;
        _skillMethod = new GlobalSkillMethods();
    }

    public override void OnFailed(Sprite sprite)
    {
        if (sprite is not Aisling damageDealingAisling) return;
        var client = damageDealingAisling.Client;

        client.SendMessage(0x02, "Failed.");
        client.Aisling.Show(Scope.NearbyAislings, new ServerFormat29(_skill.Template.MissAnimation, damageDealingAisling.Pos));
    }

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling damageDealingSprite) return;
        var client = damageDealingSprite.Client;
        damageDealingSprite.ActionUsed = "Enticed";

        var enemy = client.Aisling.DamageableGetInFront();

        if (enemy == null)
        {
            OnFailed(damageDealingSprite);
            return;
        }

        if (enemy.Count == 0)
        {
            OnFailed(damageDealingSprite);
            return;
        }

        if (enemy.First() is Aisling { EnticeImmunity: true } charmImmCheck)
        {
            charmImmCheck.Show(Scope.NearbyAislings, new ServerFormat0D
            {
                Serial = charmImmCheck.Serial,
                Text = "Not interested!",
                Type = 0x03
            });

            charmImmCheck.Client.SendMessage(0x03, "{=qYou are immune to Entice");
            return;
        }

        foreach (var i in enemy.Where(i => i.Attackable))
        {
            var target = Skill.Reflect(i, damageDealingSprite, _skill);
            if (target == null) continue;

            if (target.HasDebuff("Beag Suain"))
                target.RemoveDebuff("Beag Suain");
            if (target.HasDebuff("Frozen"))
                target.RemoveDebuff("Frozen");

            var debuff = new debuff_charmed();
            {
                if (target.HasDebuff(debuff.Name))
                    target.RemoveDebuff(debuff.Name);

                _skillMethod.ApplyPhysicalDebuff(client, debuff, target, _skill);
            }

            _skillMethod.Train(client, _skill);
        }
    }

    public override void OnUse(Sprite sprite)
    {
        if (!_skill.CanUse()) return;

        if (sprite is Aisling aisling)
        {
            _success = _skillMethod.OnUse(aisling, _skill);

            if (_success)
            {
                OnSuccess(aisling);
            }
            else
            {
                OnFailed(aisling);
            }
        }
        else
        {
            var target = sprite.Target;

            switch (target)
            {
                case null:
                    return;
                case Aisling damageDealingTarget:
                    {
                        if (damageDealingTarget.EnticeImmunity)
                        {
                            damageDealingTarget.Show(Scope.NearbyAislings, new ServerFormat0D
                            {
                                Serial = damageDealingTarget.Serial,
                                Text = "Not interested!",
                                Type = 0x03
                            });

                            damageDealingTarget.Client.SendMessage(0x03, "{=qYou are immune to Entice");
                            return;
                        }

                        var debuff = new debuff_charmed();
                        {
                            if (!damageDealingTarget.HasDebuff(debuff.Name))
                                _skillMethod.ApplyPhysicalDebuff(damageDealingTarget.Client, debuff, target, _skill);
                        }
                        break;
                    }
            }
        }
    }
}

[Script("Double-Edged Dance")]
public class Double_Edged_Dance : SkillScript
{
    private readonly Skill _skill;
    private Sprite _target;
    private const int CritDmg = 3;
    private bool _crit;
    private bool _success;
    private readonly GlobalSkillMethods _skillMethod;

    public Double_Edged_Dance(Skill skill) : base(skill)
    {
        _skill = skill;
        _skillMethod = new GlobalSkillMethods();
    }

    public override void OnFailed(Sprite sprite)
    {
        if (sprite is not Aisling damageDealingAisling) return;
        var client = damageDealingAisling.Client;

        client.SendMessage(0x02, "Dance had no effect.");
        client.Aisling.Show(Scope.NearbyAislings, new ServerFormat29(_skill.Template.MissAnimation, damageDealingAisling.Pos));
    }

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling damageDealingSprite) return;
        var client = damageDealingSprite.Client;
        damageDealingSprite.ActionUsed = "Double-Edged Dance";

        var action = new ServerFormat1A
        {
            Serial = client.Aisling.Serial,
            Number = (byte)(client.Aisling.Path == Class.Assassin
                ? 0x86
                : 0x01),
            Speed = 25
        };

        var enemy = client.Aisling.GetInFrontToSide();
        var enemyBehind = client.Aisling.DamageableGetBehind(2);
        enemy.AddRange(enemyBehind);

        if (enemy.Count == 0)
        {
            _skillMethod.FailedAttempt(client, damageDealingSprite, _skill, action);
            OnFailed(damageDealingSprite);
            return;
        }

        foreach (var i in enemy.Where(i => i.Attackable))
        {
            _target = Skill.Reflect(i, damageDealingSprite, _skill);
            if (_target == null) continue;

            var dmgCalc = DamageCalc(damageDealingSprite);

            _target.ApplyDamage(damageDealingSprite, dmgCalc, _skill);

            damageDealingSprite.Client.Aisling.Show(Scope.NearbyAislings, new ServerFormat29(_skill.Template.TargetAnimation, _target.Pos));
            _skillMethod.Train(client, _skill);

            if (!_crit) continue;
            client.Aisling.Animate(387);
            _crit = false;
        }

        client.Aisling.Show(Scope.NearbyAislings, action);
    }

    public override void OnUse(Sprite sprite)
    {
        if (!_skill.CanUse()) return;

        if (sprite is Aisling aisling)
        {
            _success = _skillMethod.OnUse(aisling, _skill);

            if (_success)
            {
                OnSuccess(aisling);
            }
            else
            {
                OnFailed(aisling);
            }
        }
        else
        {
            var enemy = sprite.GetInFrontToSide(2);
            var enemyBehind = sprite.DamageableGetBehind(2);
            enemy.AddRange(enemyBehind);

            var action = new ServerFormat1A
            {
                Serial = sprite.Serial,
                Number = 0x01,
                Speed = 30
            };

            if (enemy.Count == 0) return;

            foreach (var i in enemy.Where(i => i != null && sprite.Serial != i.Serial && i.Attackable))
            {
                _target = Skill.Reflect(i, sprite, _skill);

                var dmgCalc = DamageCalc(sprite);

                _target.ApplyDamage(sprite, dmgCalc, _skill);

                if (_skill.Template.TargetAnimation > 0)
                    if (_target is Monster or Mundane or Aisling)
                        sprite.Show(Scope.NearbyAislings,
                            new ServerFormat29((uint)sprite.Serial, (uint)_target.Serial,
                                _skill.Template.TargetAnimation, 0, 100));

                sprite.Show(Scope.NearbyAislings, action);
            }
        }
    }

    private int DamageCalc(Sprite sprite)
    {
        if (sprite is Aisling damageDealingAisling)
        {
            var client = damageDealingAisling.Client;

            var imp = 50 + _skill.Level;
            var dmg = client.Aisling.Dex * 7 + client.Aisling.Str * 2;

            dmg += dmg * imp / 100;

            var critRoll = Generator.RandNumGen100();
            {
                if (critRoll < 95) return dmg;
                dmg *= CritDmg;
                _crit = true;
            }

            return dmg;
        }
        else
        {
            if (sprite is not Monster damageMonster) return 0;

            var dmg = damageMonster.Str * 2;
            dmg += damageMonster.Dex * 7;

            var critRoll = Generator.RandNumGen100();
            {
                if (critRoll < 95) return dmg;
                dmg *= CritDmg;
                _crit = true;
            }

            return dmg;
        }
    }
}

[Script("Ebb'n Flow")]
public class Ebb_and_Flow : SkillScript
{
    private readonly Skill _skill;
    private Sprite _target;
    private const int CritDmg = 3;
    private bool _crit;
    private bool _success;
    private readonly GlobalSkillMethods _skillMethod;

    public Ebb_and_Flow(Skill skill) : base(skill)
    {
        _skill = skill;
        _skillMethod = new GlobalSkillMethods();
    }

    public override void OnFailed(Sprite sprite)
    {
        if (sprite is not Aisling damageDealingAisling) return;
        var client = damageDealingAisling.Client;

        client.SendMessage(0x02, "I need to focus.");
        client.Aisling.Show(Scope.NearbyAislings, new ServerFormat29(_skill.Template.MissAnimation, damageDealingAisling.Pos));
    }

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling damageDealingSprite) return;
        var client = damageDealingSprite.Client;
        damageDealingSprite.ActionUsed = "Ebb'n Flow";

        var action = new ServerFormat1A
        {
            Serial = client.Aisling.Serial,
            Number = (byte)(client.Aisling.Path == Class.Assassin
                ? 0x86
                : 0x01),
            Speed = 25
        };

        var enemy = client.Aisling.DamageableGetInFront(3);
        var enemyBehind = client.Aisling.DamageableGetBehind(3);
        enemy.AddRange(enemyBehind);

        if (enemy.Count == 0)
        {
            _skillMethod.FailedAttempt(client, damageDealingSprite, _skill, action);
            OnFailed(damageDealingSprite);
            return;
        }

        foreach (var i in enemy.Where(i => i.Attackable))
        {
            _target = Skill.Reflect(i, damageDealingSprite, _skill);
            if (_target == null) continue;

            var dmgCalc = DamageCalc(damageDealingSprite);

            _target.ApplyDamage(damageDealingSprite, dmgCalc, _skill);

            damageDealingSprite.Client.Aisling.Show(Scope.NearbyAislings, new ServerFormat29(_skill.Template.TargetAnimation, _target.Pos));
            _skillMethod.Train(client, _skill);

            if (!_crit) continue;
            client.Aisling.Animate(387);
            _crit = false;
        }

        client.Aisling.Show(Scope.NearbyAislings, action);
    }

    public override void OnUse(Sprite sprite)
    {
        if (!_skill.CanUse()) return;

        if (sprite is Aisling aisling)
        {
            _success = _skillMethod.OnUse(aisling, _skill);

            if (_success)
            {
                OnSuccess(aisling);
            }
            else
            {
                OnFailed(aisling);
            }
        }
        else
        {
            var enemy = sprite.GetAllInFront(3);
            var enemyBehind = sprite.DamageableGetBehind(3);
            enemy.AddRange(enemyBehind);

            var action = new ServerFormat1A
            {
                Serial = sprite.Serial,
                Number = 0x01,
                Speed = 30
            };

            if (enemy.Count == 0) return;

            foreach (var i in enemy.Where(i => i != null && sprite.Serial != i.Serial && i.Attackable))
            {
                _target = Skill.Reflect(i, sprite, _skill);

                var dmgCalc = DamageCalc(sprite);

                _target.ApplyDamage(sprite, dmgCalc, _skill);

                if (_skill.Template.TargetAnimation > 0)
                    if (_target is Monster or Mundane or Aisling)
                        sprite.Show(Scope.NearbyAislings,
                            new ServerFormat29((uint)sprite.Serial, (uint)_target.Serial,
                                _skill.Template.TargetAnimation, 0, 100));

                sprite.Show(Scope.NearbyAislings, action);
            }
        }
    }

    private int DamageCalc(Sprite sprite)
    {
        if (sprite is Aisling damageDealingAisling)
        {
            var client = damageDealingAisling.Client;

            var imp = 50 + _skill.Level;
            var dmg = client.Aisling.Con * 4 + client.Aisling.Dex * 8;

            dmg += dmg * imp / 100;

            var critRoll = Generator.RandNumGen100();
            {
                if (critRoll < 95) return dmg;
                dmg *= CritDmg;
                _crit = true;
            }

            return dmg;
        }
        else
        {
            if (sprite is not Monster damageMonster) return 0;

            var dmg = damageMonster.Con * 4;
            dmg += damageMonster.Dex * 8;

            var critRoll = Generator.RandNumGen100();
            {
                if (critRoll < 95) return dmg;
                dmg *= CritDmg;
                _crit = true;
            }

            return dmg;
        }
    }
}

[Script("Shadow Step")]
public class Shadow_Step : SkillScript
{
    private readonly Skill _skill;
    private Sprite _target;
    private const int CRIT_DMG = 2;
    private bool _crit;
    private IList<Sprite> _enemyList;
    private bool _success;
    private readonly GlobalSkillMethods _skillMethod;

    public Shadow_Step(Skill skill) : base(skill)
    {
        _skill = skill;
        _skillMethod = new GlobalSkillMethods();
    }

    public override void OnFailed(Sprite sprite)
    {
        if (sprite is not Aisling damageDealingAisling) return;
        var client = damageDealingAisling.Client;

        client.SendMessage(0x02, "*Stumbled*");
        if (_target == null) return;
        client.Aisling.Show(Scope.NearbyAislings, new ServerFormat29(_skill.Template.MissAnimation, _target.Pos));
    }

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling damageDealingSprite) return;
        var client = damageDealingSprite.Client;
        damageDealingSprite.ActionUsed = "Shadow Step";

        var dmgCalc = DamageCalc(damageDealingSprite);

        var action = new ServerFormat1A
        {
            Serial = client.Aisling.Serial,
            Number = 0x82,
            Speed = 20
        };

        if (_target == null)
        {
            _skillMethod.FailedAttempt(client, damageDealingSprite, _skill, action);
            OnFailed(damageDealingSprite);
            return;
        }

        if (_target.Serial == damageDealingSprite.Serial)
        {
            OnFailed(damageDealingSprite);
            return;
        }

        var targetPos = damageDealingSprite.GetFromAllSidesEmpty(damageDealingSprite, _target);

        if (targetPos == null || targetPos == _target.Position)
        {
            OnFailed(damageDealingSprite);
            return;
        }

        var oldPos = damageDealingSprite.Pos;

        _skillMethod.Step(damageDealingSprite, targetPos.X, targetPos.Y);
        _target.ApplyDamage(damageDealingSprite, dmgCalc, _skill);

        damageDealingSprite.Facing(_target.X, _target.Y, out var direction);
        damageDealingSprite.Client.Aisling.Show(Scope.NearbyAislings, new ServerFormat29(63, oldPos));
        damageDealingSprite.Direction = (byte)direction;
        damageDealingSprite.Turn();
        _skillMethod.Train(client, _skill);

        if (_crit)
        {
            client.Aisling.Animate(387);
            _crit = false;
        }

        client.Aisling.Show(Scope.NearbyAislings, action);
    }

    public override void OnUse(Sprite sprite)
    {
        if (!_skill.CanUse()) return;
        if (sprite is not Aisling aisling) return;

        Target(aisling);
    }

    private int DamageCalc(Sprite sprite)
    {
        if (sprite is not Aisling damageDealingAisling) return 0;
        var client = damageDealingAisling.Client;

        var imp = _skill.Level * 2;
        var dmg = client.Aisling.Int * 2 + client.Aisling.Dex * 5;

        dmg += dmg * imp / 100;

        var critRoll = Generator.RandNumGen100();
        {
            if (critRoll < 95) return dmg;
            dmg *= CRIT_DMG;
            _crit = true;
        }

        return dmg;
    }

    private void Target(Sprite sprite)
    {
        if (sprite is not Aisling damageDealingSprite) return;
        var client = damageDealingSprite.Client;

        _enemyList = client.Aisling.DamageableGetInFront();
        _target = _enemyList.FirstOrDefault();

        if (_target == null)
        {
            OnFailed(damageDealingSprite);
        }
        else
        {
            _success = _skillMethod.OnUse(damageDealingSprite, _skill);

            if (_success)
            {
                OnSuccess(damageDealingSprite);
            }
            else
            {
                OnFailed(damageDealingSprite);
            }
        }
    }
}