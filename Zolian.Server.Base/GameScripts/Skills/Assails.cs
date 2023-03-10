using Darkages.Common;
using Darkages.Enums;
using Darkages.Network.Formats.Models.ServerFormats;
using Darkages.Scripting;
using Darkages.Sprites;
using Darkages.Types;

namespace Darkages.GameScripts.Skills;

// Aisling Str * 2 | Monster Str * 3, Dex * 1.2
[Script("Assail")]
public class Assail : SkillScript
{
    private readonly Skill _skill;
    private Sprite _target;
    private const int CRIT_DMG = 2;
    private bool _crit;
    private bool _success;
    private readonly GlobalSkillMethods _skillMethod;

    public Assail(Skill skill) : base(skill)
    {
        _skill = skill;
        _skillMethod = new GlobalSkillMethods();
    }

    public override void OnFailed(Sprite sprite)
    {
        if (_target is not { Alive: true }) return;
        switch (sprite)
        {
            case Aisling damageDealingAisling:
                {
                    var client = damageDealingAisling.Client;

                    client.SendMessage(0x02, "You've lost bearing and missed...");
                    if (_target == null) return;
                    if (_target.NextTo((int)damageDealingAisling.Pos.X, (int)damageDealingAisling.Pos.Y) && damageDealingAisling.Facing((int)_target.Pos.X, (int)_target.Pos.Y, out var direction))
                        damageDealingAisling.Show(Scope.NearbyAislings, new ServerFormat29(_skill.Template.MissAnimation, _target.Pos));

                    break;
                }
            case Monster damageDealingMonster:
                {
                    var client = damageDealingMonster.Client;

                    client.Aisling.Show(Scope.NearbyAislings, new ServerFormat29(_skill.Template.MissAnimation, _target.Pos));

                    break;
                }
        }
    }

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling damageDealingSprite) return;
        var client = damageDealingSprite.Client;
        damageDealingSprite.ActionUsed = "Assail";

        var action = new ServerFormat1A
        {
            Serial = client.Aisling.Serial,
            Number = (byte)(client.Aisling.Path == Class.Defender
                ? client.Aisling.UsingTwoHanded ? 0x81 : 0x01
                : 0x01),
            Speed = 20
        };

        var enemy = client.Aisling.DamageableGetInFront();

        if (enemy.Count == 0)
        {
            _skillMethod.FailedAttempt(client, damageDealingSprite, _skill, action);
            OnFailed(damageDealingSprite);
            return;
        }

        foreach (var i in enemy.Where(i => client.Aisling.Serial != i.Serial).Where(i => i.Attackable))
        {
            _target = Skill.Reflect(i, damageDealingSprite, _skill);

            var dmgCalc = DamageCalc(sprite);

            _target.ApplyDamage(damageDealingSprite, dmgCalc, _skill);

            damageDealingSprite.Show(Scope.NearbyAislings, new ServerFormat29(_skill.Template.TargetAnimation, _target.Pos));

            _skillMethod.Train(client, _skill);

            if (!_crit) continue;
            damageDealingSprite.Animate(387);
            _crit = false;
        }

        damageDealingSprite.Show(Scope.NearbyAislings, action);
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

            var imp = 10 + _skill.Level;
            var dmg = client.Aisling.Str * 2;

            dmg += dmg * imp / 100;

            var critRoll = Generator.RandNumGen100();
            {
                if (critRoll < 95) return dmg;
                dmg *= CRIT_DMG;
                _crit = true;
            }

            return dmg;
        }
        else
        {
            if (sprite is not Monster damageMonster) return 0;

            var dmg = damageMonster.Str * 3;
            dmg += (int)(damageMonster.Dex * 1.2);

            var critRoll = Generator.RandNumGen100();
            {
                if (critRoll < 95) return dmg;
                dmg *= CRIT_DMG;
                _crit = true;
            }

            return dmg;
        }
    }
}

// Aisling Str * 2.5 | Monster Str * 3, Dex * 1.2
[Script("Assault")]
public class Assault : SkillScript
{
    private readonly Skill _skill;
    private Sprite _target;
    private const int CRIT_DMG = 2;
    private bool _crit;
    private bool _success;
    private readonly GlobalSkillMethods _skillMethod;

    public Assault(Skill skill) : base(skill)
    {
        _skill = skill;
        _skillMethod = new GlobalSkillMethods();
    }

    public override void OnFailed(Sprite sprite)
    {
        if (_target is not { Alive: true }) return;
        switch (sprite)
        {
            case Aisling damageDealingAisling:
                {
                    var client = damageDealingAisling.Client;

                    client.SendMessage(0x02, "You've lost bearing and missed...");
                    if (_target == null) return;
                    if (_target.NextTo((int)damageDealingAisling.Pos.X, (int)damageDealingAisling.Pos.Y) && damageDealingAisling.Facing((int)_target.Pos.X, (int)_target.Pos.Y, out var direction))
                        damageDealingAisling.Show(Scope.NearbyAislings, new ServerFormat29(_skill.Template.MissAnimation, _target.Pos));

                    break;
                }
            case Monster damageDealingMonster:
                {
                    var client = damageDealingMonster.Client;

                    client.Aisling.Show(Scope.NearbyAislings, new ServerFormat29(_skill.Template.MissAnimation, _target.Pos));

                    break;
                }
        }
    }

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling damageDealingSprite) return;
        var client = damageDealingSprite.Client;
        damageDealingSprite.ActionUsed = "Assault";

        var action = new ServerFormat1A
        {
            Serial = client.Aisling.Serial,
            Number = (byte)(client.Aisling.Path == Class.Defender
                ? client.Aisling.UsingTwoHanded ? 0x81 : 0x01
                : 0x01),
            Speed = 20
        };

        var enemy = client.Aisling.DamageableGetInFront();

        if (enemy.Count == 0)
        {
            _skillMethod.FailedAttempt(client, damageDealingSprite, _skill, action);
            OnFailed(damageDealingSprite);
            return;
        }

        foreach (var i in enemy.Where(i => client.Aisling.Serial != i.Serial).Where(i => i.Attackable))
        {
            _target = Skill.Reflect(i, damageDealingSprite, _skill);

            var dmgCalc = DamageCalc(sprite);

            _target.ApplyDamage(damageDealingSprite, dmgCalc, _skill);

            damageDealingSprite.Show(Scope.NearbyAislings, new ServerFormat29(_skill.Template.TargetAnimation, _target.Pos));

            _skillMethod.Train(client, _skill);

            if (!_crit) continue;
            damageDealingSprite.Animate(387);
            _crit = false;
        }

        damageDealingSprite.Show(Scope.NearbyAislings, action);
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

            var imp = 10 + _skill.Level;
            var dmg = (int)(client.Aisling.Str * 2.5);

            dmg += dmg * imp / 100;

            var critRoll = Generator.RandNumGen100();
            {
                if (critRoll < 95) return dmg;
                dmg *= CRIT_DMG;
                _crit = true;
            }

            return dmg;
        }
        else
        {
            if (sprite is not Monster damageMonster) return 0;

            var dmg = damageMonster.Str * 3;
            dmg += (int)(damageMonster.Dex * 1.2);

            var critRoll = Generator.RandNumGen100();
            {
                if (critRoll < 95) return dmg;
                dmg *= CRIT_DMG;
                _crit = true;
            }

            return dmg;
        }
    }
}

// Aisling Dex * 2.5 | Monster Dex * 3, Str * 1.2
[Script("Onslaught")]
public class Onslaught : SkillScript
{
    private readonly Skill _skill;
    private Sprite _target;
    private const int CRIT_DMG = 2;
    private bool _crit;
    private bool _success;
    private readonly GlobalSkillMethods _skillMethod;

    public Onslaught(Skill skill) : base(skill)
    {
        _skill = skill;
        _skillMethod = new GlobalSkillMethods();
    }

    public override void OnFailed(Sprite sprite)
    {
        if (_target is not { Alive: true }) return;
        switch (sprite)
        {
            case Aisling damageDealingAisling:
                {
                    var client = damageDealingAisling.Client;

                    client.SendMessage(0x02, "You've lost bearing and missed...");
                    if (_target == null) return;
                    if (_target.NextTo((int)damageDealingAisling.Pos.X, (int)damageDealingAisling.Pos.Y) && damageDealingAisling.Facing((int)_target.Pos.X, (int)_target.Pos.Y, out var direction))
                        damageDealingAisling.Show(Scope.NearbyAislings, new ServerFormat29(_skill.Template.MissAnimation, _target.Pos));

                    break;
                }
            case Monster damageDealingMonster:
                {
                    var client = damageDealingMonster.Client;

                    client.Aisling.Show(Scope.NearbyAislings, new ServerFormat29(_skill.Template.MissAnimation, _target.Pos));

                    break;
                }
        }
    }

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling damageDealingSprite) return;
        var client = damageDealingSprite.Client;
        damageDealingSprite.ActionUsed = "Onslaught";

        var action = new ServerFormat1A
        {
            Serial = client.Aisling.Serial,
            Number = (byte)(client.Aisling.Path == Class.Defender
                ? client.Aisling.UsingTwoHanded ? 0x81 : 0x01
                : 0x01),
            Speed = 20
        };

        var enemy = client.Aisling.DamageableGetInFront();

        if (enemy.Count == 0)
        {
            _skillMethod.FailedAttempt(client, damageDealingSprite, _skill, action);
            OnFailed(damageDealingSprite);
            return;
        }

        foreach (var i in enemy.Where(i => client.Aisling.Serial != i.Serial).Where(i => i.Attackable))
        {
            _target = Skill.Reflect(i, damageDealingSprite, _skill);

            var dmgCalc = DamageCalc(sprite);

            _target.ApplyDamage(damageDealingSprite, dmgCalc, _skill);

            damageDealingSprite.Show(Scope.NearbyAislings, new ServerFormat29(_skill.Template.TargetAnimation, _target.Pos));

            _skillMethod.Train(client, _skill);

            if (!_crit) continue;
            damageDealingSprite.Animate(387);
            _crit = false;
        }

        damageDealingSprite.Show(Scope.NearbyAislings, action);
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

            var imp = 10 + _skill.Level;
            var dmg = (int)(client.Aisling.Dex * 2.5);

            dmg += dmg * imp / 100;

            var critRoll = Generator.RandNumGen100();
            {
                if (critRoll < 95) return dmg;
                dmg *= CRIT_DMG;
                _crit = true;
            }

            return dmg;
        }
        else
        {
            if (sprite is not Monster damageMonster) return 0;

            var dmg = (int)(damageMonster.Str * 1.2);
            dmg += damageMonster.Dex * 3;

            var critRoll = Generator.RandNumGen100();
            {
                if (critRoll < 95) return dmg;
                dmg *= CRIT_DMG;
                _crit = true;
            }

            return dmg;
        }
    }
}

// Aisling Str * 3 | Monster Str * 3, Dex * 1.2
[Script("Clobber")]
public class Clobber : SkillScript
{
    private readonly Skill _skill;
    private Sprite _target;
    private const int CRIT_DMG = 2;
    private bool _crit;
    private bool _success;
    private readonly GlobalSkillMethods _skillMethod;

    public Clobber(Skill skill) : base(skill)
    {
        _skill = skill;
        _skillMethod = new GlobalSkillMethods();
    }

    public override void OnFailed(Sprite sprite)
    {
        if (_target is not { Alive: true }) return;
        switch (sprite)
        {
            case Aisling damageDealingAisling:
                {
                    var client = damageDealingAisling.Client;

                    client.SendMessage(0x02, "You've lost bearing and missed...");
                    if (_target == null) return;
                    if (_target.NextTo((int)damageDealingAisling.Pos.X, (int)damageDealingAisling.Pos.Y) && damageDealingAisling.Facing((int)_target.Pos.X, (int)_target.Pos.Y, out var direction))
                        damageDealingAisling.Show(Scope.NearbyAislings, new ServerFormat29(_skill.Template.MissAnimation, _target.Pos));

                    break;
                }
            case Monster damageDealingMonster:
                {
                    var client = damageDealingMonster.Client;

                    client.Aisling.Show(Scope.NearbyAislings, new ServerFormat29(_skill.Template.MissAnimation, _target.Pos));

                    break;
                }
        }
    }

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling damageDealingSprite) return;
        var client = damageDealingSprite.Client;
        damageDealingSprite.ActionUsed = "Clobber";

        var action = new ServerFormat1A
        {
            Serial = client.Aisling.Serial,
            Number = (byte)(client.Aisling.Path == Class.Berserker | client.Aisling.Path == Class.Defender
                ? client.Aisling.UsingTwoHanded ? 0x81 : 0x01
                : 0x01),
            Speed = 20
        };

        var enemy = client.Aisling.DamageableGetInFront();

        if (enemy.Count == 0)
        {
            _skillMethod.FailedAttempt(client, damageDealingSprite, _skill, action);
            OnFailed(damageDealingSprite);
            return;
        }

        foreach (var i in enemy.Where(i => client.Aisling.Serial != i.Serial).Where(i => i.Attackable))
        {
            _target = Skill.Reflect(i, damageDealingSprite, _skill);

            var dmgCalc = DamageCalc(sprite);

            _target.ApplyDamage(damageDealingSprite, dmgCalc, _skill);

            damageDealingSprite.Show(Scope.NearbyAislings, new ServerFormat29(_skill.Template.TargetAnimation, _target.Pos));

            _skillMethod.Train(client, _skill);

            if (!_crit) continue;
            damageDealingSprite.Animate(387);
            _crit = false;
        }

        damageDealingSprite.Show(Scope.NearbyAislings, action);
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

            var imp = 10 + _skill.Level;
            var dmg = client.Aisling.Str * 3;

            dmg += dmg * imp / 100;

            var critRoll = Generator.RandNumGen100();
            {
                if (critRoll < 95) return dmg;
                dmg *= CRIT_DMG;
                _crit = true;
            }

            return dmg;
        }
        else
        {
            if (sprite is not Monster damageMonster) return 0;

            var dmg = damageMonster.Str * 3;
            dmg += (int)(damageMonster.Dex * 1.2);

            var critRoll = Generator.RandNumGen100();
            {
                if (critRoll < 95) return dmg;
                dmg *= CRIT_DMG;
                _crit = true;
            }

            return dmg;
        }
    }
}

// Aisling Dex * 3 | Monster Str * 1.2, Dex * 3
[Script("Clobber x2")]
public class ClobberX2 : SkillScript
{
    private readonly Skill _skill;
    private Sprite _target;
    private const int CRIT_DMG = 2;
    private bool _crit;
    private bool _success;
    private readonly GlobalSkillMethods _skillMethod;

    public ClobberX2(Skill skill) : base(skill)
    {
        _skill = skill;
        _skillMethod = new GlobalSkillMethods();
    }

    public override void OnFailed(Sprite sprite)
    {
        if (_target is not { Alive: true }) return;
        switch (sprite)
        {
            case Aisling damageDealingAisling:
                {
                    var client = damageDealingAisling.Client;

                    client.SendMessage(0x02, "You've lost bearing and missed...");
                    if (_target == null) return;
                    if (_target.NextTo((int)damageDealingAisling.Pos.X, (int)damageDealingAisling.Pos.Y) && damageDealingAisling.Facing((int)_target.Pos.X, (int)_target.Pos.Y, out var direction))
                        damageDealingAisling.Show(Scope.NearbyAislings, new ServerFormat29(_skill.Template.MissAnimation, _target.Pos));

                    break;
                }
            case Monster damageDealingMonster:
                {
                    var client = damageDealingMonster.Client;

                    client.Aisling.Show(Scope.NearbyAislings, new ServerFormat29(_skill.Template.MissAnimation, _target.Pos));

                    break;
                }
        }
    }

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling damageDealingSprite) return;
        var client = damageDealingSprite.Client;
        damageDealingSprite.ActionUsed = "Clobber x2";

        var action = new ServerFormat1A
        {
            Serial = client.Aisling.Serial,
            Number = (byte)(client.Aisling.Path == Class.Berserker | client.Aisling.Path == Class.Defender
                ? client.Aisling.UsingTwoHanded ? 0x81 : 0x01
                : 0x01),
            Speed = 20
        };

        var enemy = client.Aisling.DamageableGetInFront();

        if (enemy.Count == 0)
        {
            _skillMethod.FailedAttempt(client, damageDealingSprite, _skill, action);
            OnFailed(damageDealingSprite);
            return;
        }

        foreach (var i in enemy.Where(i => client.Aisling.Serial != i.Serial).Where(i => i.Attackable))
        {
            _target = Skill.Reflect(i, damageDealingSprite, _skill);

            var dmgCalc = DamageCalc(sprite);

            _target.ApplyDamage(damageDealingSprite, dmgCalc, _skill);

            damageDealingSprite.Show(Scope.NearbyAislings, new ServerFormat29(_skill.Template.TargetAnimation, _target.Pos));

            _skillMethod.Train(client, _skill);

            if (!_crit) continue;
            damageDealingSprite.Animate(387);
            _crit = false;
        }

        damageDealingSprite.Show(Scope.NearbyAislings, action);
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

            var imp = 10 + _skill.Level;
            var dmg = client.Aisling.Dex * 3;

            dmg += dmg * imp / 100;

            var critRoll = Generator.RandNumGen100();
            {
                if (critRoll < 95) return dmg;
                dmg *= CRIT_DMG;
                _crit = true;
            }

            return dmg;
        }
        else
        {
            if (sprite is not Monster damageMonster) return 0;

            var dmg = damageMonster.Str * 3;
            dmg += (int)(damageMonster.Dex * 1.2);

            var critRoll = Generator.RandNumGen100();
            {
                if (critRoll < 95) return dmg;
                dmg *= CRIT_DMG;
                _crit = true;
            }

            return dmg;
        }
    }
}

// Aisling Str * 3 | Monster Str * 3, Dex * 1.2
[Script("Thrust")]
public class Thrust : SkillScript
{
    private readonly Skill _skill;
    private Sprite _target;
    private const int CRIT_DMG = 2;
    private bool _crit;
    private bool _success;
    private readonly GlobalSkillMethods _skillMethod;

    public Thrust(Skill skill) : base(skill)
    {
        _skill = skill;
        _skillMethod = new GlobalSkillMethods();
    }

    public override void OnFailed(Sprite sprite)
    {
        if (_target is not { Alive: true }) return;
        switch (sprite)
        {
            case Aisling damageDealingAisling:
                {
                    var client = damageDealingAisling.Client;

                    client.SendMessage(0x02, "You've lost bearing and missed...");
                    if (_target == null) return;
                    if (_target.NextTo((int)damageDealingAisling.Pos.X, (int)damageDealingAisling.Pos.Y) && damageDealingAisling.Facing((int)_target.Pos.X, (int)_target.Pos.Y, out var direction))
                        damageDealingAisling.Show(Scope.NearbyAislings, new ServerFormat29(_skill.Template.MissAnimation, _target.Pos));

                    break;
                }
            case Monster damageDealingMonster:
                {
                    var client = damageDealingMonster.Client;

                    client.Aisling.Show(Scope.NearbyAislings, new ServerFormat29(_skill.Template.MissAnimation, _target.Pos));

                    break;
                }
        }
    }

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling damageDealingSprite) return;
        var client = damageDealingSprite.Client;
        damageDealingSprite.ActionUsed = "Thrust";

        var action = new ServerFormat1A
        {
            Serial = client.Aisling.Serial,
            Number = 0x01,
            Speed = 20
        };

        var enemy = client.Aisling.DamageableGetInFront(2);

        if (enemy.Count == 0)
        {
            _skillMethod.FailedAttempt(client, damageDealingSprite, _skill, action);
            OnFailed(damageDealingSprite);
            return;
        }

        foreach (var i in enemy.Where(i => client.Aisling.Serial != i.Serial).Where(i => i.Attackable))
        {
            _target = Skill.Reflect(i, damageDealingSprite, _skill);

            var dmgCalc = DamageCalc(sprite);

            _target.ApplyDamage(damageDealingSprite, dmgCalc, _skill);

            damageDealingSprite.Show(Scope.NearbyAislings, new ServerFormat29(_skill.Template.TargetAnimation, _target.Pos));

            _skillMethod.Train(client, _skill);

            if (!_crit) continue;
            damageDealingSprite.Animate(387);
            _crit = false;
        }

        damageDealingSprite.Show(Scope.NearbyAislings, action);
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
            var enemy = sprite.MonsterGetInFront(2);

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

            var imp = 10 + _skill.Level;
            var dmg = client.Aisling.Str * 3;

            dmg += dmg * imp / 100;

            var critRoll = Generator.RandNumGen100();
            {
                if (critRoll < 95) return dmg;
                dmg *= CRIT_DMG;
                _crit = true;
            }

            return dmg;
        }
        else
        {
            if (sprite is not Monster damageMonster) return 0;

            var dmg = damageMonster.Str * 3;
            dmg += (int)(damageMonster.Dex * 1.2);

            var critRoll = Generator.RandNumGen100();
            {
                if (critRoll < 95) return dmg;
                dmg *= CRIT_DMG;
                _crit = true;
            }

            return dmg;
        }
    }
}

// Aisling Str * 4, Dex * 1.2 | Monster Str * 4, Dex * 1.2
[Script("Wallop")]
public class Wallop : SkillScript
{
    private readonly Skill _skill;
    private Sprite _target;
    private const int CRIT_DMG = 2;
    private bool _crit;
    private bool _success;
    private readonly GlobalSkillMethods _skillMethod;

    public Wallop(Skill skill) : base(skill)
    {
        _skill = skill;
        _skillMethod = new GlobalSkillMethods();
    }

    public override void OnFailed(Sprite sprite)
    {
        if (_target is not { Alive: true }) return;
        switch (sprite)
        {
            case Aisling damageDealingAisling:
                {
                    var client = damageDealingAisling.Client;

                    client.SendMessage(0x02, "You've lost bearing and missed...");
                    if (_target == null) return;
                    if (_target.NextTo((int)damageDealingAisling.Pos.X, (int)damageDealingAisling.Pos.Y) && damageDealingAisling.Facing((int)_target.Pos.X, (int)_target.Pos.Y, out var direction))
                        damageDealingAisling.Show(Scope.NearbyAislings, new ServerFormat29(_skill.Template.MissAnimation, _target.Pos));

                    break;
                }
            case Monster damageDealingMonster:
                {
                    var client = damageDealingMonster.Client;

                    client.Aisling.Show(Scope.NearbyAislings, new ServerFormat29(_skill.Template.MissAnimation, _target.Pos));

                    break;
                }
        }
    }

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling damageDealingSprite) return;
        var client = damageDealingSprite.Client;
        damageDealingSprite.ActionUsed = "Wallop";

        var action = new ServerFormat1A
        {
            Serial = client.Aisling.Serial,
            Number = (byte)(client.Aisling.Path == Class.Defender
                ? client.Aisling.UsingTwoHanded ? 0x81 : 0x01
                : 0x01),
            Speed = 20
        };

        var enemy = client.Aisling.DamageableGetInFront();

        if (enemy.Count == 0)
        {
            _skillMethod.FailedAttempt(client, damageDealingSprite, _skill, action);
            OnFailed(damageDealingSprite);
            return;
        }

        foreach (var i in enemy.Where(i => client.Aisling.Serial != i.Serial).Where(i => i.Attackable))
        {
            _target = Skill.Reflect(i, damageDealingSprite, _skill);

            var dmgCalc = DamageCalc(sprite);

            _target.ApplyDamage(damageDealingSprite, dmgCalc, _skill);

            damageDealingSprite.Show(Scope.NearbyAislings, new ServerFormat29(_skill.Template.TargetAnimation, _target.Pos));

            _skillMethod.Train(client, _skill);

            if (!_crit) continue;
            damageDealingSprite.Animate(387);
            _crit = false;
        }

        damageDealingSprite.Show(Scope.NearbyAislings, action);
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

            var imp = 10 + _skill.Level;
            var dmg = client.Aisling.Str * 4;
            dmg += (int)(client.Aisling.Dex * 1.2);

            dmg += dmg * imp / 100;

            var critRoll = Generator.RandNumGen100();
            {
                if (critRoll < 95) return dmg;
                dmg *= CRIT_DMG;
                _crit = true;
            }

            return dmg;
        }
        else
        {
            if (sprite is not Monster damageMonster) return 0;

            var dmg = damageMonster.Str * 4;
            dmg += (int)(damageMonster.Dex * 1.2);

            var critRoll = Generator.RandNumGen100();
            {
                if (critRoll < 95) return dmg;
                dmg *= CRIT_DMG;
                _crit = true;
            }

            return dmg;
        }
    }
}

// Aisling Str * 5, Dex * 3 | Monster Str * 5, Dex * 3
[Script("Thrash")]
public class Thrash : SkillScript
{
    private readonly Skill _skill;
    private Sprite _target;
    private const int CRIT_DMG = 2;
    private bool _crit;
    private bool _success;
    private readonly GlobalSkillMethods _skillMethod;

    public Thrash(Skill skill) : base(skill)
    {
        _skill = skill;
        _skillMethod = new GlobalSkillMethods();
    }

    public override void OnFailed(Sprite sprite)
    {
        if (_target is not { Alive: true }) return;
        switch (sprite)
        {
            case Aisling damageDealingAisling:
                {
                    var client = damageDealingAisling.Client;

                    client.SendMessage(0x02, "You've lost bearing and missed...");
                    if (_target == null) return;
                    if (_target.NextTo((int)damageDealingAisling.Pos.X, (int)damageDealingAisling.Pos.Y) && damageDealingAisling.Facing((int)_target.Pos.X, (int)_target.Pos.Y, out var direction))
                        damageDealingAisling.Show(Scope.NearbyAislings, new ServerFormat29(_skill.Template.MissAnimation, _target.Pos));

                    break;
                }
            case Monster damageDealingMonster:
                {
                    var client = damageDealingMonster.Client;

                    client.Aisling.Show(Scope.NearbyAislings, new ServerFormat29(_skill.Template.MissAnimation, _target.Pos));

                    break;
                }
        }
    }

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling damageDealingSprite) return;
        var client = damageDealingSprite.Client;
        damageDealingSprite.ActionUsed = "Thrash";

        var action = new ServerFormat1A
        {
            Serial = client.Aisling.Serial,
            Number = (byte)(client.Aisling.Path == Class.Defender
                ? client.Aisling.UsingTwoHanded ? 0x81 : 0x01
                : 0x01),
            Speed = 20
        };

        var enemy = client.Aisling.DamageableGetInFront();

        if (enemy.Count == 0)
        {
            _skillMethod.FailedAttempt(client, damageDealingSprite, _skill, action);
            OnFailed(damageDealingSprite);
            return;
        }

        foreach (var i in enemy.Where(i => client.Aisling.Serial != i.Serial).Where(i => i.Attackable))
        {
            _target = Skill.Reflect(i, damageDealingSprite, _skill);

            var dmgCalc = DamageCalc(sprite);

            _target.ApplyDamage(damageDealingSprite, dmgCalc, _skill);

            damageDealingSprite.Show(Scope.NearbyAislings, new ServerFormat29(_skill.Template.TargetAnimation, _target.Pos));

            _skillMethod.Train(client, _skill);

            if (!_crit) continue;
            damageDealingSprite.Animate(387);
            _crit = false;
        }

        damageDealingSprite.Show(Scope.NearbyAislings, action);
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

            var imp = 10 + _skill.Level;
            var dmg = client.Aisling.Str * 5;
            dmg += client.Aisling.Dex * 3;

            dmg += dmg * imp / 100;

            var critRoll = Generator.RandNumGen100();
            {
                if (critRoll < 95) return dmg;
                dmg *= CRIT_DMG;
                _crit = true;
            }

            return dmg;
        }
        else
        {
            if (sprite is not Monster damageMonster) return 0;

            var dmg = damageMonster.Str * 5;
            dmg += damageMonster.Dex * 3;

            var critRoll = Generator.RandNumGen100();
            {
                if (critRoll < 95) return dmg;
                dmg *= CRIT_DMG;
                _crit = true;
            }

            return dmg;
        }
    }
}

// Aisling Str * 2, Con * 2 | Monster Str * 2, Con * 2
[Script("Punch")]
public class Punch : SkillScript
{
    private readonly Skill _skill;
    private Sprite _target;
    private const int CRIT_DMG = 2;
    private bool _crit;
    private bool _success;
    private readonly GlobalSkillMethods _skillMethod;

    public Punch(Skill skill) : base(skill)
    {
        _skill = skill;
        _skillMethod = new GlobalSkillMethods();
    }

    public override void OnFailed(Sprite sprite)
    {
        if (_target is not { Alive: true }) return;
        switch (sprite)
        {
            case Aisling damageDealingAisling:
                {
                    var client = damageDealingAisling.Client;

                    client.SendMessage(0x02, "Your strike missed...");
                    if (_target == null) return;
                    if (_target.NextTo((int)damageDealingAisling.Pos.X, (int)damageDealingAisling.Pos.Y) && damageDealingAisling.Facing((int)_target.Pos.X, (int)_target.Pos.Y, out var direction))
                        damageDealingAisling.Show(Scope.NearbyAislings, new ServerFormat29(_skill.Template.MissAnimation, _target.Pos));

                    break;
                }
            case Monster damageDealingMonster:
                {
                    var client = damageDealingMonster.Client;

                    client.Aisling.Show(Scope.NearbyAislings, new ServerFormat29(_skill.Template.MissAnimation, _target.Pos));

                    break;
                }
        }
    }

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling damageDealingSprite) return;
        var client = damageDealingSprite.Client;
        damageDealingSprite.ActionUsed = "Punch";

        var action = new ServerFormat1A
        {
            Serial = client.Aisling.Serial,
            Number = 0x84,
            Speed = 20
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

            _target.ApplyDamage(damageDealingSprite, dmgCalc, _skill);

            damageDealingSprite.Show(Scope.NearbyAislings, new ServerFormat29(_skill.Template.TargetAnimation, _target.Pos));

            _skillMethod.Train(client, _skill);

            if (!_crit) continue;
            damageDealingSprite.Animate(387);
            _crit = false;
        }

        damageDealingSprite.Show(Scope.NearbyAislings, action);
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
            var dmg = client.Aisling.Str * 2 + client.Aisling.Con * 2;

            dmg += dmg * imp / 100;

            var critRoll = Generator.RandNumGen100();
            {
                if (critRoll < 95) return dmg;
                dmg *= CRIT_DMG;
                _crit = true;
            }

            return dmg;
        }
        else
        {
            if (sprite is not Monster damageMonster) return 0;

            var dmg = damageMonster.Str * 2;
            dmg += damageMonster.Con * 2;

            var critRoll = Generator.RandNumGen100();
            {
                if (critRoll < 95) return dmg;
                dmg *= CRIT_DMG;
                _crit = true;
            }

            return dmg;
        }
    }
}

// Aisling Str * 4, Con * 3 | Monster Str * 4, Con * 3
[Script("Double Punch")]
public class DoublePunch : SkillScript
{
    private readonly Skill _skill;
    private Sprite _target;
    private const int CRIT_DMG = 2;
    private bool _crit;
    private bool _success;
    private readonly GlobalSkillMethods _skillMethod;

    public DoublePunch(Skill skill) : base(skill)
    {
        _skill = skill;
        _skillMethod = new GlobalSkillMethods();
    }

    public override void OnFailed(Sprite sprite)
    {
        if (_target is not { Alive: true }) return;
        switch (sprite)
        {
            case Aisling damageDealingAisling:
                {
                    var client = damageDealingAisling.Client;

                    client.SendMessage(0x02, "Double punch missed.");
                    if (_target == null) return;
                    if (_target.NextTo((int)damageDealingAisling.Pos.X, (int)damageDealingAisling.Pos.Y) && damageDealingAisling.Facing((int)_target.Pos.X, (int)_target.Pos.Y, out var direction))
                        damageDealingAisling.Show(Scope.NearbyAislings, new ServerFormat29(_skill.Template.MissAnimation, _target.Pos));

                    break;
                }
            case Monster damageDealingMonster:
                {
                    var client = damageDealingMonster.Client;

                    client.Aisling.Show(Scope.NearbyAislings, new ServerFormat29(_skill.Template.MissAnimation, _target.Pos));

                    break;
                }
        }
    }

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling damageDealingSprite) return;
        var client = damageDealingSprite.Client;
        damageDealingSprite.ActionUsed = "Double Punch";

        var action = new ServerFormat1A
        {
            Serial = client.Aisling.Serial,
            Number = 0x84,
            Speed = 20
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

            _target.ApplyDamage(damageDealingSprite, dmgCalc, _skill);
            _target.ApplyDamage(damageDealingSprite, dmgCalc, _skill);

            damageDealingSprite.Show(Scope.NearbyAislings, new ServerFormat29(_skill.Template.TargetAnimation, _target.Pos));

            _skillMethod.Train(client, _skill);

            if (!_crit) continue;
            damageDealingSprite.Animate(387);
            _crit = false;
        }

        damageDealingSprite.Show(Scope.NearbyAislings, action);
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
            var dmg = client.Aisling.Str * 4 + client.Aisling.Con * 3;

            dmg += dmg * imp / 100;

            var critRoll = Generator.RandNumGen100();
            {
                if (critRoll < 95) return dmg;
                dmg *= CRIT_DMG;
                _crit = true;
            }

            return dmg;
        }
        else
        {
            if (sprite is not Monster damageMonster) return 0;

            var dmg = damageMonster.Str * 4;
            dmg += damageMonster.Con * 3;

            var critRoll = Generator.RandNumGen100();
            {
                if (critRoll < 95) return dmg;
                dmg *= CRIT_DMG;
                _crit = true;
            }

            return dmg;
        }
    }
}

// Aisling Dex * 5, * Distance | Monster Str * 3, Dex * 2
[Script("Throw")]
public class Throw : SkillScript
{
    private readonly Skill _skill;
    private Sprite _target;
    private const int CRIT_DMG = 2;
    private bool _crit;
    private bool _success;
    private readonly GlobalSkillMethods _skillMethod;

    public Throw(Skill skill) : base(skill)
    {
        _skill = skill;
        _skillMethod = new GlobalSkillMethods();
    }

    public override void OnFailed(Sprite sprite)
    {
        if (_target is not { Alive: true }) return;
        switch (sprite)
        {
            case Aisling damageDealingAisling:
                {
                    var client = damageDealingAisling.Client;

                    client.SendMessage(0x02, "You've lost bearing and missed...");
                    if (_target == null) return;
                    if (_target.NextTo((int)damageDealingAisling.Pos.X, (int)damageDealingAisling.Pos.Y) && damageDealingAisling.Facing((int)_target.Pos.X, (int)_target.Pos.Y, out var direction))
                        damageDealingAisling.Show(Scope.NearbyAislings, new ServerFormat29(_skill.Template.MissAnimation, _target.Pos));

                    break;
                }
            case Monster damageDealingMonster:
                {
                    var client = damageDealingMonster.Client;

                    client.Aisling.Show(Scope.NearbyAislings, new ServerFormat29(_skill.Template.MissAnimation, _target.Pos));

                    break;
                }
        }
    }

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling damageDealingSprite) return;
        var client = damageDealingSprite.Client;
        damageDealingSprite.ActionUsed = "Throw";

        var enemy = client.Aisling.DamageableGetInFront(3);
        var action = new ServerFormat1A
        {
            Serial = client.Aisling.Serial,
            Number = 0x01,
            Speed = 30
        };

        if (enemy.Count == 0)
        {
            _skillMethod.FailedAttempt(client, damageDealingSprite, _skill, action);
            OnFailed(damageDealingSprite);
            return;
        }

        foreach (var i in enemy.Where(i => client.Aisling.Serial != i.Serial).Where(i => i.Attackable))
        {
            _target = Skill.Reflect(i, damageDealingSprite, _skill);

            var thrown = _skillMethod.Thrown(client, _skill, _crit);

            var animation = new ServerFormat29
            {
                CasterSerial = (uint)damageDealingSprite.Serial,
                TargetSerial = (uint)i.Serial,
                CasterEffect = (ushort)thrown,
                TargetEffect = (ushort)thrown,
                Speed = 100
            };

            var dmgCalc = DamageCalc(sprite);

            _target.ApplyDamage(damageDealingSprite, dmgCalc, _skill);

            damageDealingSprite.Show(Scope.NearbyAislings, new ServerFormat29(_skill.Template.TargetAnimation, _target.Pos));

            _skillMethod.Train(client, _skill);

            damageDealingSprite.Show(Scope.NearbyAislings, animation);

            if (!_crit) continue;
            damageDealingSprite.Animate(387);
            _crit = false;
        }

        damageDealingSprite.Show(Scope.NearbyAislings, action);
    }

    public override void OnUse(Sprite sprite)
    {
        if (!_skill.CanUse()) return;

        if (sprite is Aisling aisling)
        {
            var client = aisling.Client;

            if (client.Aisling.EquipmentManager.Equipment[1]?.Item?.Template.Group is not ("Glaives" or "Daggers"))
            {
                OnFailed(aisling);
                return;
            }

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
            var enemy = sprite.MonsterGetInFront(3);

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

            var imp = 10 + _skill.Level;
            var dmg = client.Aisling.Dex * 5 * damageDealingAisling.Position.DistanceFrom(_target.Position);

            dmg += dmg * imp / 100;

            var critRoll = Generator.RandNumGen100();
            {
                if (critRoll < 95) return dmg;
                dmg *= CRIT_DMG;
                _crit = true;
            }

            return dmg;
        }
        else
        {
            if (sprite is not Monster damageMonster) return 0;

            var dmg = damageMonster.Str * 3;
            dmg += (int)(damageMonster.Dex * 2);

            var critRoll = Generator.RandNumGen100();
            {
                if (critRoll < 95) return dmg;
                dmg *= CRIT_DMG;
                _crit = true;
            }

            return dmg;
        }
    }
}

// Aisling Str * 2, Dex * 4, * Distance (Max 5) | Monster Str * 3, Dex * 5
[Script("Aim")]
public class Aim : SkillScript
{
    private readonly Skill _skill;
    private Sprite _target;
    private const int CRIT_DMG = 2;
    private bool _crit;
    private bool _success;
    private readonly GlobalSkillMethods _skillMethod;

    public Aim(Skill skill) : base(skill)
    {
        _skill = skill;
        _skillMethod = new GlobalSkillMethods();
    }

    public override void OnFailed(Sprite sprite)
    {
        if (_target is not { Alive: true }) return;
        switch (sprite)
        {
            case Aisling damageDealingAisling:
                {
                    var client = damageDealingAisling.Client;

                    client.SendMessage(0x02, "You've lost focus.");
                    if (_target == null) return;
                    if (_target.NextTo((int)damageDealingAisling.Pos.X, (int)damageDealingAisling.Pos.Y) && damageDealingAisling.Facing((int)_target.Pos.X, (int)_target.Pos.Y, out var direction))
                        damageDealingAisling.Show(Scope.NearbyAislings, new ServerFormat29(_skill.Template.MissAnimation, _target.Pos));

                    break;
                }
            case Monster damageDealingMonster:
                {
                    var client = damageDealingMonster.Client;

                    client.Aisling.Show(Scope.NearbyAislings, new ServerFormat29(_skill.Template.MissAnimation, _target.Pos));

                    break;
                }
        }
    }

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling damageDealingSprite) return;
        var client = damageDealingSprite.Client;
        damageDealingSprite.ActionUsed = "Aim";

        var enemy = client.Aisling.DamageableGetInFront(5);
        var action = new ServerFormat1A
        {
            Serial = client.Aisling.Serial,
            Number = 0x8E,
            Speed = 30
        };

        if (enemy.Count == 0)
        {
            _skillMethod.FailedAttempt(client, damageDealingSprite, _skill, action);
            OnFailed(damageDealingSprite);
            return;
        }

        foreach (var i in enemy.Where(i => client.Aisling.Serial != i.Serial).Where(i => i.Attackable))
        {
            _target = Skill.Reflect(i, damageDealingSprite, _skill);

            var thrown = _skillMethod.Thrown(client, _skill, _crit);

            var animation = new ServerFormat29
            {
                CasterSerial = (uint)damageDealingSprite.Serial,
                TargetSerial = (uint)i.Serial,
                CasterEffect = (ushort)thrown,
                TargetEffect = (ushort)thrown,
                Speed = 100
            };

            var dmgCalc = DamageCalc(sprite);

            _target.ApplyDamage(damageDealingSprite, dmgCalc, _skill);

            damageDealingSprite.Show(Scope.NearbyAislings, new ServerFormat29(_skill.Template.TargetAnimation, _target.Pos));

            _skillMethod.Train(client, _skill);

            damageDealingSprite.Show(Scope.NearbyAislings, animation);

            if (!_crit) continue;
            damageDealingSprite.Animate(387);
            _crit = false;
        }

        damageDealingSprite.Show(Scope.NearbyAislings, action);
    }

    public override void OnUse(Sprite sprite)
    {
        if (!_skill.CanUse()) return;

        if (sprite is Aisling aisling)
        {
            var client = aisling.Client;

            if (client.Aisling.EquipmentManager.Equipment[1]?.Item?.Template.Group is not ("Bows"))
            {
                OnFailed(aisling);
                return;
            }

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
            var enemy = sprite.MonsterGetInFront(3);

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

            var imp = 10 + _skill.Level;
            var dmg = client.Aisling.Str * 2 + client.Aisling.Dex * 4 * damageDealingAisling.Position.DistanceFrom(_target.Position);

            dmg += dmg * imp / 100;

            var critRoll = Generator.RandNumGen100();
            {
                if (critRoll < 95) return dmg;
                dmg *= CRIT_DMG;
                _crit = true;
            }

            return dmg;
        }
        else
        {
            if (sprite is not Monster damageMonster) return 0;

            var dmg = damageMonster.Str * 3;
            dmg += damageMonster.Dex * 5;

            var critRoll = Generator.RandNumGen100();
            {
                if (critRoll < 95) return dmg;
                dmg *= CRIT_DMG;
                _crit = true;
            }

            return dmg;
        }
    }
}

[Script("Two-Handed Attack")]
public class Two_Handed_Attack : SkillScript
{
    private readonly Skill _skill;
    private Sprite _target;
    private const int CRIT_DMG = 2;
    private bool _crit;
    private bool _success;
    private readonly GlobalSkillMethods _skillMethod;

    public Two_Handed_Attack(Skill skill) : base(skill)
    {
        _skill = skill;
        _skillMethod = new GlobalSkillMethods();
    }

    public override void OnFailed(Sprite sprite)
    {
        if (_target is not { Alive: true }) return;
        switch (sprite)
        {
            case Aisling damageDealingAisling:
                {
                    var client = damageDealingAisling.Client;

                    client.SendMessage(0x02, "You've lost bearing and missed...");
                    if (_target == null) return;
                    if (_target.NextTo((int)damageDealingAisling.Pos.X, (int)damageDealingAisling.Pos.Y) && damageDealingAisling.Facing((int)_target.Pos.X, (int)_target.Pos.Y, out var direction))
                        damageDealingAisling.Show(Scope.NearbyAislings, new ServerFormat29(_skill.Template.MissAnimation, _target.Pos));

                    break;
                }
            case Monster damageDealingMonster:
                {
                    var client = damageDealingMonster.Client;

                    client.Aisling.Show(Scope.NearbyAislings, new ServerFormat29(_skill.Template.MissAnimation, _target.Pos));

                    break;
                }
        }
    }

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling damageDealingSprite) return;
        var client = damageDealingSprite.Client;
        damageDealingSprite.ActionUsed = "Two-handed Attack";

        var action = new ServerFormat1A
        {
            Serial = client.Aisling.Serial,
            Number = (byte)(client.Aisling.Path == Class.Defender
                ? client.Aisling.TwoHandedBasher ? 0x81 : 0x01
                : 0x01),
            Speed = 20
        };

        var enemy = client.Aisling.GetInFrontToSide();

        if (enemy.Count == 0)
        {
            _skillMethod.FailedAttempt(client, damageDealingSprite, _skill, action);
            OnFailed(damageDealingSprite);
            return;
        }

        foreach (var i in enemy.Where(i => client.Aisling.Serial != i.Serial).Where(i => i.Attackable))
        {
            _target = Skill.Reflect(i, damageDealingSprite, _skill);

            var dmgCalc = DamageCalc(sprite);

            _target.ApplyDamage(damageDealingSprite, dmgCalc, _skill);

            damageDealingSprite.Show(Scope.NearbyAislings, new ServerFormat29(_skill.Template.TargetAnimation, _target.Pos));

            _skillMethod.Train(client, _skill);

            if (!_crit) continue;
            damageDealingSprite.Animate(387);
            _crit = false;
        }

        damageDealingSprite.Show(Scope.NearbyAislings, action);
    }

    public override void OnUse(Sprite sprite)
    {
        if (!_skill.CanUse()) return;

        if (sprite is Aisling aisling)
        {
            if (aisling.EquipmentManager.Equipment[1] == null) return;
            if (!aisling.EquipmentManager.Equipment[1].Item.Template.Flags.FlagIsSet(ItemFlags.TwoHanded) || aisling.EquipmentManager.Equipment[1].Item.Template.Flags.FlagIsSet(ItemFlags.TwoHandedStaff))
            {
                return;
            }

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

            var imp = 10 + _skill.Level;
            var dmg = client.Aisling.Str;

            dmg += dmg * imp / 100;

            var critRoll = Generator.RandNumGen100();
            {
                if (critRoll < 95) return dmg;
                dmg *= CRIT_DMG;
                _crit = true;
            }

            return dmg;
        }
        else
        {
            if (sprite is not Monster damageMonster) return 0;

            var dmg = damageMonster.Str;

            var critRoll = Generator.RandNumGen100();
            {
                if (critRoll < 95) return dmg;
                dmg *= CRIT_DMG;
                _crit = true;
            }

            return dmg;
        }
    }
}

[Script("Kobudo")]
public class Kobudo : SkillScript
{
    private readonly Skill _skill;
    private Sprite _target;
    private const int CRIT_DMG = 2;
    private bool _crit;
    private bool _success;
    private readonly GlobalSkillMethods _skillMethod;

    public Kobudo(Skill skill) : base(skill)
    {
        _skill = skill;
        _skillMethod = new GlobalSkillMethods();
    }

    public override void OnFailed(Sprite sprite)
    {
        if (_target is not { Alive: true }) return;
        switch (sprite)
        {
            case Aisling damageDealingAisling:
                {
                    var client = damageDealingAisling.Client;

                    client.SendMessage(0x02, "You've lost bearing and missed...");
                    if (_target == null) return;
                    if (_target.NextTo((int)damageDealingAisling.Pos.X, (int)damageDealingAisling.Pos.Y) && damageDealingAisling.Facing((int)_target.Pos.X, (int)_target.Pos.Y, out var direction))
                        damageDealingAisling.Show(Scope.NearbyAislings, new ServerFormat29(_skill.Template.MissAnimation, _target.Pos));

                    break;
                }
            case Monster damageDealingMonster:
                {
                    var client = damageDealingMonster.Client;

                    client.Aisling.Show(Scope.NearbyAislings, new ServerFormat29(_skill.Template.MissAnimation, _target.Pos));

                    break;
                }
        }
    }

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling damageDealingSprite) return;
        var client = damageDealingSprite.Client;
        damageDealingSprite.ActionUsed = "Kobudo";

        var action = new ServerFormat1A
        {
            Serial = client.Aisling.Serial,
            Number = 0x01,
            Speed = 20
        };

        var enemy = client.Aisling.GetInFrontToSide();

        if (enemy.Count == 0)
        {
            _skillMethod.FailedAttempt(client, damageDealingSprite, _skill, action);
            OnFailed(damageDealingSprite);
            return;
        }

        foreach (var i in enemy.Where(i => client.Aisling.Serial != i.Serial).Where(i => i.Attackable))
        {
            _target = Skill.Reflect(i, damageDealingSprite, _skill);

            var dmgCalc = DamageCalc(sprite);

            _target.ApplyDamage(damageDealingSprite, dmgCalc, _skill);

            damageDealingSprite.Show(Scope.NearbyAislings, new ServerFormat29(_skill.Template.TargetAnimation, _target.Pos));

            _skillMethod.Train(client, _skill);

            if (!_crit) continue;
            damageDealingSprite.Animate(387);
            _crit = false;
        }

        damageDealingSprite.Show(Scope.NearbyAislings, action);
    }

    public override void OnUse(Sprite sprite)
    {
        if (!_skill.CanUse()) return;

        if (sprite is Aisling aisling)
        {
            if (aisling.EquipmentManager.Equipment[1] == null) return;
            if (!aisling.EquipmentManager.Equipment[1].Item.Template.Flags.FlagIsSet(ItemFlags.TwoHanded) || aisling.EquipmentManager.Equipment[1].Item.Template.Flags.FlagIsSet(ItemFlags.TwoHandedStaff))
            {
                return;
            }

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

            var imp = 10 + _skill.Level;
            var dmg = client.Aisling.Str;

            dmg += dmg * imp / 100;

            var critRoll = Generator.RandNumGen100();
            {
                if (critRoll < 95) return dmg;
                dmg *= CRIT_DMG;
                _crit = true;
            }

            return dmg;
        }
        else
        {
            if (sprite is not Monster damageMonster) return 0;

            var dmg = damageMonster.Str;

            var critRoll = Generator.RandNumGen100();
            {
                if (critRoll < 95) return dmg;
                dmg *= CRIT_DMG;
                _crit = true;
            }

            return dmg;
        }
    }
}

[Script("Advanced Staff Train")]
public class AdvancedStaffTraining : SkillScript
{
    private readonly Skill _skill;
    private Sprite _target;
    private const int CRIT_DMG = 2;
    private bool _crit;
    private bool _success;
    private readonly GlobalSkillMethods _skillMethod;

    public AdvancedStaffTraining(Skill skill) : base(skill)
    {
        _skill = skill;
        _skillMethod = new GlobalSkillMethods();
    }

    public override void OnFailed(Sprite sprite)
    {
        if (_target is not { Alive: true }) return;
        switch (sprite)
        {
            case Aisling damageDealingAisling:
                {
                    var client = damageDealingAisling.Client;

                    client.SendMessage(0x02, "You've lost bearing and missed...");
                    if (_target == null) return;
                    if (_target.NextTo((int)damageDealingAisling.Pos.X, (int)damageDealingAisling.Pos.Y) && damageDealingAisling.Facing((int)_target.Pos.X, (int)_target.Pos.Y, out var direction))
                        damageDealingAisling.Show(Scope.NearbyAislings, new ServerFormat29(_skill.Template.MissAnimation, _target.Pos));

                    break;
                }
            case Monster damageDealingMonster:
                {
                    var client = damageDealingMonster.Client;

                    client.Aisling.Show(Scope.NearbyAislings, new ServerFormat29(_skill.Template.MissAnimation, _target.Pos));

                    break;
                }
        }
    }

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling damageDealingSprite) return;
        var client = damageDealingSprite.Client;
        damageDealingSprite.ActionUsed = "Advanced Staff Training";

        var action = new ServerFormat1A
        {
            Serial = client.Aisling.Serial,
            Number = 0x01,
            Speed = 20
        };

        var enemy = client.Aisling.GetInFrontToSide();

        if (enemy.Count == 0)
        {
            _skillMethod.FailedAttempt(client, damageDealingSprite, _skill, action);
            OnFailed(damageDealingSprite);
            return;
        }

        foreach (var i in enemy.Where(i => client.Aisling.Serial != i.Serial).Where(i => i.Attackable))
        {
            _target = Skill.Reflect(i, damageDealingSprite, _skill);

            var dmgCalc = DamageCalc(sprite);

            _target.ApplyDamage(damageDealingSprite, dmgCalc, _skill);

            damageDealingSprite.Show(Scope.NearbyAislings, new ServerFormat29(_skill.Template.TargetAnimation, _target.Pos));

            _skillMethod.Train(client, _skill);

            if (!_crit) continue;
            damageDealingSprite.Animate(387);
            _crit = false;
        }

        damageDealingSprite.Show(Scope.NearbyAislings, action);
    }

    public override void OnUse(Sprite sprite)
    {
        if (!_skill.CanUse()) return;

        if (sprite is Aisling aisling)
        {
            if (aisling.EquipmentManager.Equipment[1] == null) return;
            if (!aisling.EquipmentManager.Equipment[1].Item.Template.Flags.FlagIsSet(ItemFlags.TwoHanded) || aisling.EquipmentManager.Equipment[1].Item.Template.Flags.FlagIsSet(ItemFlags.TwoHandedStaff))
            {
                return;
            }

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

            var imp = 10 + _skill.Level;
            var dmg = client.Aisling.Str;

            dmg += dmg * imp / 100;

            var critRoll = Generator.RandNumGen100();
            {
                if (critRoll < 95) return dmg;
                dmg *= CRIT_DMG;
                _crit = true;
            }

            return dmg;
        }
        else
        {
            if (sprite is not Monster damageMonster) return 0;

            var dmg = damageMonster.Str;

            var critRoll = Generator.RandNumGen100();
            {
                if (critRoll < 95) return dmg;
                dmg *= CRIT_DMG;
                _crit = true;
            }

            return dmg;
        }
    }
}

[Script("Dual Wield")]
public class DualWield : SkillScript
{
    private readonly Skill _skill;
    private Sprite _target;
    private const int CRIT_DMG = 2;
    private bool _crit;
    private bool _success;
    private readonly GlobalSkillMethods _skillMethod;

    public DualWield(Skill skill) : base(skill)
    {
        _skill = skill;
        _skillMethod = new GlobalSkillMethods();
    }

    public override void OnFailed(Sprite sprite)
    {
        if (_target is not { Alive: true }) return;
        switch (sprite)
        {
            case Aisling damageDealingAisling:
                {
                    var client = damageDealingAisling.Client;

                    client.SendMessage(0x02, "You've lost bearing and missed...");
                    if (_target == null) return;
                    if (_target.NextTo((int)damageDealingAisling.Pos.X, (int)damageDealingAisling.Pos.Y) && damageDealingAisling.Facing((int)_target.Pos.X, (int)_target.Pos.Y, out var direction))
                        damageDealingAisling.Show(Scope.NearbyAislings, new ServerFormat29(_skill.Template.MissAnimation, _target.Pos));

                    break;
                }
            case Monster damageDealingMonster:
                {
                    var client = damageDealingMonster.Client;

                    client.Aisling.Show(Scope.NearbyAislings, new ServerFormat29(_skill.Template.MissAnimation, _target.Pos));

                    break;
                }
        }
    }

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling damageDealingSprite) return;
        var client = damageDealingSprite.Client;
        damageDealingSprite.ActionUsed = "Dual Wield";

        var action = new ServerFormat1A
        {
            Serial = client.Aisling.Serial,
            Number = 0x01,
            Speed = 20
        };

        var enemy = client.Aisling.GetInFrontToSide();

        if (enemy.Count == 0)
        {
            _skillMethod.FailedAttempt(client, damageDealingSprite, _skill, action);
            OnFailed(damageDealingSprite);
            return;
        }

        foreach (var i in enemy.Where(i => client.Aisling.Serial != i.Serial).Where(i => i.Attackable))
        {
            _target = Skill.Reflect(i, damageDealingSprite, _skill);

            var dmgCalc = DamageCalc(sprite);

            _target.ApplyDamage(damageDealingSprite, dmgCalc, _skill);

            damageDealingSprite.Show(Scope.NearbyAislings, new ServerFormat29(_skill.Template.TargetAnimation, _target.Pos));

            _skillMethod.Train(client, _skill);

            if (!_crit) continue;
            damageDealingSprite.Animate(387);
            _crit = false;
        }

        damageDealingSprite.Show(Scope.NearbyAislings, action);
    }

    public override void OnUse(Sprite sprite)
    {
        if (!_skill.CanUse()) return;
        
        if (sprite is Aisling aisling)
        {
            if (aisling.EquipmentManager.Equipment[3] != null)
            {
                if (!aisling.EquipmentManager.Equipment[3].Item.Template.Flags.FlagIsSet(ItemFlags.DualWield)) return;
            }
            else
            {
                return;
            }

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

            var imp = 10 + _skill.Level;
            var dmg = client.Aisling.Dex;

            dmg += dmg * imp / 100;

            var critRoll = Generator.RandNumGen100();
            {
                if (critRoll < 95) return dmg;
                dmg *= CRIT_DMG;
                _crit = true;
            }

            return dmg;
        }
        else
        {
            if (sprite is not Monster damageMonster) return 0;

            var dmg = damageMonster.Str;

            var critRoll = Generator.RandNumGen100();
            {
                if (critRoll < 95) return dmg;
                dmg *= CRIT_DMG;
                _crit = true;
            }

            return dmg;
        }
    }
}

[Script("Ambidextrous")]
public class Ambidextrous : SkillScript
{
    private readonly Skill _skill;
    private Sprite _target;
    private const int CRIT_DMG = 2;
    private bool _crit;
    private bool _success;
    private readonly GlobalSkillMethods _skillMethod;

    public Ambidextrous(Skill skill) : base(skill)
    {
        _skill = skill;
        _skillMethod = new GlobalSkillMethods();
    }

    public override void OnFailed(Sprite sprite)
    {
        if (_target is not { Alive: true }) return;
        switch (sprite)
        {
            case Aisling damageDealingAisling:
                {
                    var client = damageDealingAisling.Client;

                    client.SendMessage(0x02, "You've lost bearing and missed...");
                    if (_target == null) return;
                    if (_target.NextTo((int)damageDealingAisling.Pos.X, (int)damageDealingAisling.Pos.Y) && damageDealingAisling.Facing((int)_target.Pos.X, (int)_target.Pos.Y, out var direction))
                        damageDealingAisling.Show(Scope.NearbyAislings, new ServerFormat29(_skill.Template.MissAnimation, _target.Pos));

                    break;
                }
            case Monster damageDealingMonster:
                {
                    var client = damageDealingMonster.Client;

                    client.Aisling.Show(Scope.NearbyAislings, new ServerFormat29(_skill.Template.MissAnimation, _target.Pos));

                    break;
                }
        }
    }

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling damageDealingSprite) return;
        var client = damageDealingSprite.Client;
        damageDealingSprite.ActionUsed = "Ambidextrous";

        var action = new ServerFormat1A
        {
            Serial = client.Aisling.Serial,
            Number = (byte)(client.Aisling.Path == Class.Assassin
                ? client.Aisling.DualWield ? 0x87 : 0x01
                : 0x01),
            Speed = 20
        };

        var enemy = client.Aisling.GetInFrontToSide();

        if (enemy.Count == 0)
        {
            _skillMethod.FailedAttempt(client, damageDealingSprite, _skill, action);
            OnFailed(damageDealingSprite);
            return;
        }

        foreach (var i in enemy.Where(i => client.Aisling.Serial != i.Serial).Where(i => i.Attackable))
        {
            _target = Skill.Reflect(i, damageDealingSprite, _skill);

            var dmgCalc = DamageCalc(sprite);

            _target.ApplyDamage(damageDealingSprite, dmgCalc, _skill);

            damageDealingSprite.Show(Scope.NearbyAislings, new ServerFormat29(_skill.Template.TargetAnimation, _target.Pos));

            _skillMethod.Train(client, _skill);

            if (!_crit) continue;
            damageDealingSprite.Animate(387);
            _crit = false;
        }

        damageDealingSprite.Show(Scope.NearbyAislings, action);
    }

    public override void OnUse(Sprite sprite)
    {
        if (!_skill.CanUse()) return;

        if (sprite is Aisling aisling)
        {
            if (aisling.EquipmentManager.Equipment[3] != null)
            {
                if (!aisling.EquipmentManager.Equipment[3].Item.Template.Flags.FlagIsSet(ItemFlags.DualWield)) return;
            }
            else
            {
                return;
            }

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

            var imp = 10 + _skill.Level;
            var dmg = client.Aisling.Dex;

            dmg += dmg * imp / 100;

            var critRoll = Generator.RandNumGen100();
            {
                if (critRoll < 95) return dmg;
                dmg *= CRIT_DMG;
                _crit = true;
            }

            return dmg;
        }
        else
        {
            if (sprite is not Monster damageMonster) return 0;

            var dmg = damageMonster.Str;

            var critRoll = Generator.RandNumGen100();
            {
                if (critRoll < 95) return dmg;
                dmg *= CRIT_DMG;
                _crit = true;
            }

            return dmg;
        }
    }
}
