using Chaos.Common.Definitions;
using Chaos.Networking.Entities.Server;

using Darkages.Common;
using Darkages.Enums;
using Darkages.GameScripts.Affects;
using Darkages.ScriptingBase;
using Darkages.Sprites;
using Darkages.Types;

namespace Darkages.GameScripts.Skills;

[Script("Rescue")]
public class Rescue(Skill skill) : SkillScript(skill)
{
    private Sprite _target;
    private readonly GlobalSkillMethods _skillMethod = new();

    public override void OnFailed(Sprite sprite)
    {
        if (_target is not { Alive: true }) return;
        if (sprite.NextTo(_target.Position.X, _target.Position.Y) && sprite.Facing(_target.Position.X, _target.Position.Y, out _))
            sprite.PlayerNearby?.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(Skill.Template.MissAnimation, null, _target.Serial));
    }

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
        var client = aisling.Client;
        aisling.ActionUsed = "Rescue";

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 40,
            BodyAnimation = BodyAnimation.Assail,
            Sound = null,
            SourceId = sprite.Serial
        };

        var enemy = client.Aisling.DamageableGetInFront();

        if (enemy.Count == 0)
        {
            _skillMethod.FailedAttempt(aisling, Skill, action);
            OnFailed(aisling);
            return;
        }

        foreach (var i in enemy.Where(i => i.Attackable))
        {
            _target = Skill.Reflect(i, sprite, Skill);
            if (_target == null) continue;

            switch (_target)
            {
                case Aisling reviveAisling:
                    {
                        if (reviveAisling.Skulled)
                        {
                            reviveAisling.Debuffs.TryGetValue("Skulled", out var debuff);
                            if (debuff != null)
                            {
                                debuff.Cancelled = true;
                                debuff.OnEnded(reviveAisling, debuff);
                                reviveAisling.Client.Revive();
                            }
                        }

                        if (reviveAisling.HasDebuff("Beag Suain"))
                            reviveAisling.Debuffs.TryRemove("Beag Suain", out _);

                        _target.ApplyDamage(aisling, 0, Skill);
                        reviveAisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(Skill.Template.TargetAnimation, null, _target.Serial));
                        break;
                    }
                case Monster _:
                    _target.ApplyDamage(aisling, 3 * aisling.Int, Skill);
                    aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(Skill.Template.TargetAnimation, null, _target.Serial));
                    break;
            }
        }
        _skillMethod.Train(client, Skill);

        aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendBodyAnimation(action.SourceId, action.BodyAnimation, action.AnimationSpeed));
    }

    public override void OnUse(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
        if (!Skill.CanUse()) return;
        aisling.Client.SendCooldown(true, Skill.Slot, Skill.Template.Cooldown);

        var success = Generator.RandNumGen100();

        if (success < 3)
        {
            OnFailed(aisling);
            return;
        }

        OnSuccess(aisling);
    }
}

[Script("Wind Blade")]
public class Wind_Blade(Skill skill) : SkillScript(skill)
{
    private Sprite _target;
    private bool _crit;
    private bool _success;
    private readonly GlobalSkillMethods _skillMethod = new();

    public override void OnFailed(Sprite sprite)
    {
        if (_target is not { Alive: true }) return;
        if (sprite.NextTo(_target.Position.X, _target.Position.Y) && sprite.Facing(_target.Position.X, _target.Position.Y, out _))
            sprite.PlayerNearby?.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(Skill.Template.MissAnimation, null, _target.Serial));
    }

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
        aisling.ActionUsed = "Wind Blade";

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 40,
            BodyAnimation = BodyAnimation.TwoHandAtk,
            Sound = null,
            SourceId = sprite.Serial
        };

        var enemy = aisling.DamageableGetInFront(3);

        if (enemy.Count == 0)
        {
            _skillMethod.FailedAttempt(aisling, Skill, action);
            OnFailed(aisling);
            return;
        }

        foreach (var i in enemy.Where(i => aisling.Serial != i.Serial).Where(i => i.Attackable))
        {
            _target = i;
            var dmgCalc = DamageCalc(sprite);
            _skillMethod.OnSuccessWithoutAction(_target, aisling, Skill, dmgCalc, _crit);
        }

        aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendBodyAnimation(action.SourceId, action.BodyAnimation, action.AnimationSpeed));
    }

    public override void OnUse(Sprite sprite)
    {
        if (!Skill.CanUse()) return;

        if (sprite is Aisling aisling)
        {
            _success = _skillMethod.OnUse(aisling, Skill);

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

            var action = new BodyAnimationArgs
            {
                AnimationSpeed = 40,
                BodyAnimation = BodyAnimation.Assail,
                Sound = null,
                SourceId = sprite.Serial
            };

            if (enemy.Count == 0) return;

            foreach (var i in enemy.Where(i => i != null && sprite.Serial != i.Serial && i.Attackable))
            {
                _target = Skill.Reflect(i, sprite, Skill);

                var dmgCalc = DamageCalc(sprite);

                _target.ApplyDamage(sprite, dmgCalc, Skill);

                if (Skill.Template.TargetAnimation > 0)
                    if (_target is Monster or Mundane or Aisling)
                        sprite.PlayerNearby?.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(Skill.Template.TargetAnimation, null, _target.Serial, 170));

                sprite.PlayerNearby?.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendBodyAnimation(action.SourceId, action.BodyAnimation, action.AnimationSpeed));

                if (!_crit) return;
                sprite.PlayerNearby?.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(387, null, sprite.Serial));
            }
        }
    }

    private long DamageCalc(Sprite sprite)
    {
        _crit = false;
        long dmg;
        if (sprite is Aisling damageDealingAisling)
        {
            var client = damageDealingAisling.Client;
            var imp = 40 + Skill.Level;
            dmg = client.Aisling.Str * 3 + client.Aisling.Dex * 2;
            dmg += dmg * imp / 100;
        }
        else
        {
            if (sprite is not Monster damageMonster) return 0;
            dmg = damageMonster.Str * 3;
            dmg += damageMonster.Dex * 2;
        }

        var critCheck = _skillMethod.OnCrit(dmg);
        _crit = critCheck.Item1;
        return critCheck.Item2;
    }
}

[Script("Beag Suain")]
public class Beag_Suain(Skill skill) : SkillScript(skill)
{
    private Sprite _target;
    private bool _success;
    private readonly GlobalSkillMethods _skillMethod = new();

    public override void OnFailed(Sprite sprite)
    {
        if (sprite is not Aisling damageDealingAisling) return;
        var client = damageDealingAisling.Client;

        client.SendServerMessage(ServerMessageType.OrangeBar1, "Failed to incapacitate.");
        if (_target is not { Alive: true }) return;
        if (sprite.NextTo(_target.Position.X, _target.Position.Y) && sprite.Facing(_target.Position.X, _target.Position.Y, out _))
            sprite.PlayerNearby?.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(Skill.Template.MissAnimation, null, _target.Serial));
    }

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
        aisling.ActionUsed = "Beag Suain";

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 40,
            BodyAnimation = BodyAnimation.Assail,
            Sound = null,
            SourceId = sprite.Serial
        };

        var enemy = aisling.DamageableGetInFront().FirstOrDefault();
        _target = enemy;

        if (_target == null || _target.Serial == aisling.Serial || !_target.Attackable)
        {
            _skillMethod.FailedAttempt(aisling, Skill, action);
            OnFailed(aisling);
            return;
        }

        var debuff = new DebuffBeagsuain();
        {
            if (_target.HasDebuff(debuff.Name))
                _target.RemoveDebuff(debuff.Name);

            _skillMethod.ApplyPhysicalDebuff(aisling.Client, debuff, _target, Skill);
        }

        _skillMethod.OnSuccess(_target, aisling, Skill, 0, false, action);
    }

    public override void OnUse(Sprite sprite)
    {
        if (!Skill.CanUse()) return;

        if (sprite is Aisling aisling)
        {
            _success = _skillMethod.OnUse(aisling, Skill);

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
                        var debuff = new DebuffBeagsuain();
                        {
                            if (!damageDealingTarget.HasDebuff(debuff.Name))
                                _skillMethod.ApplyPhysicalDebuff(damageDealingTarget.Client, debuff, target, Skill);
                        }
                        break;
                    }
            }
        }
    }
}

[Script("Vampiric Slash")]
public class Vampiric_Slash(Skill skill) : SkillScript(skill)
{
    private Sprite _target;
    private bool _crit;
    private bool _success;
    private readonly GlobalSkillMethods _skillMethod = new();

    public override void OnFailed(Sprite sprite)
    {
        if (_target is not { Alive: true }) return;
        if (sprite.NextTo(_target.Position.X, _target.Position.Y) && sprite.Facing(_target.Position.X, _target.Position.Y, out _))
            sprite.PlayerNearby?.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(Skill.Template.MissAnimation, null, _target.Serial));
    }

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
        aisling.ActionUsed = "Vampiric Slash";

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 40,
            BodyAnimation = BodyAnimation.Swipe,
            Sound = null,
            SourceId = sprite.Serial
        };

        var enemy = aisling.DamageableGetInFront(2);

        if (enemy.Count == 0)
        {
            _skillMethod.FailedAttempt(aisling, Skill, action);
            OnFailed(aisling);
            return;
        }

        foreach (var i in enemy.Where(i => aisling.Serial != i.Serial).Where(i => i.Attackable))
        {
            _target = i;
            var dmgCalc = DamageCalc(sprite);
            aisling.CurrentHp += (int)dmgCalc;
            _skillMethod.OnSuccessWithoutAction(_target, aisling, Skill, dmgCalc, _crit);
        }

        aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendBodyAnimation(action.SourceId, action.BodyAnimation, action.AnimationSpeed));
    }

    public override void OnUse(Sprite sprite)
    {
        if (!Skill.CanUse()) return;

        if (sprite is Aisling aisling)
        {
            _success = _skillMethod.OnUse(aisling, Skill);

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

            var action = new BodyAnimationArgs
            {
                AnimationSpeed = 40,
                BodyAnimation = BodyAnimation.Assail,
                Sound = null,
                SourceId = sprite.Serial
            };

            if (enemy.Count == 0) return;

            foreach (var i in enemy.Where(i => i != null && sprite.Serial != i.Serial && i.Attackable))
            {
                _target = Skill.Reflect(i, sprite, Skill);

                var dmgCalc = DamageCalc(sprite);
                var healthAbsorb = dmgCalc * 5;
                sprite.CurrentHp += (int)healthAbsorb;
                _target.ApplyDamage(sprite, dmgCalc, Skill);

                if (Skill.Template.TargetAnimation > 0)
                    if (_target is Monster or Mundane or Aisling)
                        sprite.PlayerNearby?.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(Skill.Template.TargetAnimation, null, _target.Serial, 170));

                sprite.PlayerNearby?.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendBodyAnimation(action.SourceId, action.BodyAnimation, action.AnimationSpeed));

                if (!_crit) return;
                sprite.PlayerNearby?.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(387, null, sprite.Serial));
            }
        }
    }

    private long DamageCalc(Sprite sprite)
    {
        _crit = false;
        long dmg;
        if (sprite is Aisling damageDealingAisling)
        {
            var client = damageDealingAisling.Client;
            var imp = 60 + Skill.Level;
            dmg = client.Aisling.Int * 5 + client.Aisling.Wis * 3;
            dmg += dmg * imp / 100;
        }
        else
        {
            if (sprite is not Monster damageMonster) return 0;
            dmg = damageMonster.Int * 3;
            dmg += damageMonster.Wis * 2;
        }

        var critCheck = _skillMethod.OnCrit(dmg);
        _crit = critCheck.Item1;
        return critCheck.Item2;
    }
}

[Script("Charge")]
public class Charge(Skill skill) : SkillScript(skill)
{
    private Sprite _target;
    private bool _crit;
    private IList<Sprite> _enemyList;
    private bool _success;
    private readonly GlobalSkillMethods _skillMethod = new();

    public override void OnFailed(Sprite sprite)
    {
        if (sprite is not Aisling damageDealingAisling) return;
        var client = damageDealingAisling.Client;

        client.SendServerMessage(ServerMessageType.OrangeBar1, "*Stumbled*");
        if (_target is not { Alive: true }) return;
        if (sprite.NextTo(_target.Position.X, _target.Position.Y) && sprite.Facing(_target.Position.X, _target.Position.Y, out _))
            sprite.PlayerNearby?.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(Skill.Template.MissAnimation, null, _target.Serial));
    }

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
        var client = aisling.Client;
        aisling.ActionUsed = "Charge";
        
        if (_target == null)
        {
            OnFailed(aisling);
            return;
        }

        var dmgCalc = DamageCalc(aisling);
        var position = _target.Position;
        var mapCheck = aisling.Map.ID;
        var wallPosition = aisling.GetPendingChargePosition(7, aisling);
        var targetPos = _skillMethod.DistanceTo(aisling.Position, position);
        var wallPos = _skillMethod.DistanceTo(aisling.Position, wallPosition);

        if (mapCheck != aisling.Map.ID) return;

        if (targetPos <= wallPos)
        {
            switch (aisling.Direction)
            {
                case 0:
                    position.Y++;
                    break;
                case 1:
                    position.X--;
                    break;
                case 2:
                    position.Y--;
                    break;
                case 3:
                    position.X++;
                    break;
            }

            if (aisling.Position != position)
            {
                _skillMethod.Step(aisling, position.X, position.Y);
            }

            _target.ApplyDamage(aisling, dmgCalc, Skill);
            _skillMethod.Train(client, Skill);
            aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(Skill.Template.TargetAnimation, null, _target.Serial));

            if (!_crit) return;
            aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(387, null, sprite.Serial));
        }
        else
        {
            _skillMethod.Step(aisling, wallPosition.X, wallPosition.Y);

            var stunned = new DebuffBeagsuain();
            aisling.Client.EnqueueDebuffAppliedEvent(aisling, stunned);
            aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(208, null, aisling.Serial));
        }
    }

    public override void OnUse(Sprite sprite)
    {
        if (!Skill.CanUse()) return;
        if (sprite is not Aisling aisling) return;

        var client = aisling.Client;

        if (client.Aisling.CantAttack)
        {
            OnFailed(aisling);
            return;
        }

        Target(aisling);
    }

    private long DamageCalc(Sprite sprite)
    {
        _crit = false;
        long dmg;
        if (sprite is Aisling damageDealingAisling)
        {
            var client = damageDealingAisling.Client;
            var imp = Skill.Level * 2;
            dmg = client.Aisling.Str * 5 + client.Aisling.Con * 4;
            dmg += dmg * imp / 100;
        }
        else
        {
            if (sprite is not Monster damageMonster) return 0;
            dmg = damageMonster.Str * 5 + damageMonster.Con * 4;
        }

        var critCheck = _skillMethod.OnCrit(dmg);
        _crit = critCheck.Item1;
        return critCheck.Item2;
    }

    private void Target(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
        var client = aisling.Client;

        _enemyList = client.Aisling.DamageableGetInFront(7);
        _target = _enemyList.FirstOrDefault();
        _target = Skill.Reflect(_target, sprite, Skill);

        if (_target == null)
        {
            var mapCheck = aisling.Map.ID;
            var wallPosition = aisling.GetPendingChargePositionNoTarget(7, aisling);
            var wallPos = _skillMethod.DistanceTo(aisling.Position, wallPosition);

            if (mapCheck != aisling.Map.ID) return;
            if (!(wallPos > 0)) OnFailed(aisling);

            if (aisling.Position != wallPosition)
            {
                _skillMethod.Step(aisling, wallPosition.X, wallPosition.Y);
            }

            if (wallPos <= 6)
            {
                var stunned = new DebuffBeagsuain();
                aisling.Client.EnqueueDebuffAppliedEvent(aisling, stunned);
                aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(208, null, aisling.Serial));
            }

            aisling.UsedSkill(Skill);
        }
        else
        {
            _success = _skillMethod.OnUse(aisling, Skill);

            if (_success)
            {
                OnSuccess(aisling);
            }
            else
            {
                OnFailed(aisling);
            }
        }
    }
}

[Script("Beag Suain Ia Gar")]
public class Beag_Suain_Ia_Gar(Skill skill) : SkillScript(skill)
{
    private bool _success;
    private readonly GlobalSkillMethods _skillMethod = new();

    public override void OnFailed(Sprite sprite)
    {
        if (sprite is not Aisling damageDealingAisling) return;
        var client = damageDealingAisling.Client;

        client.SendServerMessage(ServerMessageType.OrangeBar1, "Failed to incapacitate.");
        sprite.PlayerNearby?.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(Skill.Template.MissAnimation, null, sprite.Serial));
    }

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
        var client = aisling.Client;
        aisling.ActionUsed = "Beag Suain Ia Gar";

        var list = sprite.MonstersNearby().ToList();

        if (list.Count == 0)
        {
            OnFailed(aisling);
            return;
        }

        foreach (var target in list.Where(i => i.Attackable))
        {
            var debuff = new DebuffBeagsuaingar();
            {
                if (target.HasDebuff(debuff.Name))
                    target.RemoveDebuff(debuff.Name);

                _skillMethod.ApplyPhysicalDebuff(client, debuff, target, Skill);
            }
        }

        _skillMethod.Train(client, Skill);
    }

    public override void OnUse(Sprite sprite)
    {
        if (!Skill.CanUse()) return;

        if (sprite is Aisling aisling)
        {
            _success = _skillMethod.OnUse(aisling, Skill);

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
                        var debuff = new DebuffBeagsuaingar();
                        {
                            if (!damageDealingTarget.HasDebuff(debuff.Name))
                                _skillMethod.ApplyPhysicalDebuff(damageDealingTarget.Client, debuff, target, Skill);
                        }
                        break;
                    }
            }
        }
    }
}

[Script("Raise Threat")]
public class Raise_Threat(Skill skill) : SkillScript(skill)
{
    private bool _success;
    private readonly GlobalSkillMethods _skillMethod = new();

    public override void OnFailed(Sprite sprite)
    {
        if (sprite is not Aisling damageDealingAisling) return;
        var client = damageDealingAisling.Client;

        client.SendServerMessage(ServerMessageType.OrangeBar1, "Failed.");
        sprite.PlayerNearby?.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(Skill.Template.MissAnimation, null, sprite.Serial));
    }

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
        var client = aisling.Client;
        aisling.ActionUsed = "Raise Threat";

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 40,
            BodyAnimation = BodyAnimation.HandsUp,
            Sound = null,
            SourceId = sprite.Serial
        };

        aisling.ThreatMeter *= 4;

        var enemies = aisling.MonstersNearby();
        foreach (var monster in enemies.Where(e => e is { IsAlive: true }))
        {
            monster.Target = aisling;
        }

        _skillMethod.Train(client, Skill);

        aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendBodyAnimation(action.SourceId, action.BodyAnimation, action.AnimationSpeed));
    }

    public override void OnUse(Sprite sprite)
    {
        if (!Skill.CanUse()) return;
        if (sprite is not Aisling aisling) return;

        _success = _skillMethod.OnUse(aisling, Skill);

        if (_success)
        {
            OnSuccess(aisling);
        }
        else
        {
            OnFailed(aisling);
        }
    }
}

[Script("Draconic Leash")]
public class Draconic_Leash(Skill skill) : SkillScript(skill)
{
    private readonly GlobalSkillMethods _skillMethod = new();

    public override void OnFailed(Sprite sprite)
    {
        if (sprite is not Aisling damageDealingAisling) return;
        var client = damageDealingAisling.Client;

        client.SendServerMessage(ServerMessageType.OrangeBar1, "Failed.");
        sprite.PlayerNearby?.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(Skill.Template.MissAnimation, null, sprite.Serial));
    }

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
        var client = aisling.Client;
        aisling.ActionUsed = "Draconic Leash";

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 40,
            BodyAnimation = BodyAnimation.HandsUp,
            Sound = null,
            SourceId = sprite.Serial
        };

        var monstersNearby = aisling.MonstersNearby();
        var monsters = monstersNearby.Where(mSprite => !mSprite.Template.MonsterType.MonsterTypeIsSet(MonsterType.Boss)).Where(mSprite => !mSprite.Template.MonsterRace.MonsterRaceIsSet(MonsterRace.Dummy)).ToList();

        if (monsters.Count == 0)
        {
            OnFailed(aisling);
            _skillMethod.Train(client, Skill);
            return;
        }

        var monster = monsters.RandomIEnum();

        if (monster != null)
        {
            aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(Skill.Template.TargetAnimation, monster.Position));
            monster.Pos = aisling.Pos;
            monster.UpdateAddAndRemove();
        }

        _skillMethod.Train(client, Skill);
        aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(139, null, aisling.Serial));
        aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendBodyAnimation(action.SourceId, action.BodyAnimation, action.AnimationSpeed));
    }

    public override void OnUse(Sprite sprite)
    {
        if (!Skill.CanUse()) return;
        if (sprite is not Aisling aisling) return;
        OnSuccess(aisling);
    }
}

[Script("Taunt")]
public class Taunt(Skill skill) : SkillScript(skill)
{
    private readonly GlobalSkillMethods _skillMethod = new();

    public override void OnFailed(Sprite sprite)
    {
        if (sprite is not Aisling damageDealingAisling) return;
        var client = damageDealingAisling.Client;

        client.SendServerMessage(ServerMessageType.OrangeBar1, "Failed.");
        sprite.PlayerNearby?.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(Skill.Template.MissAnimation, null, sprite.Serial));
    }

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
        var client = aisling.Client;
        aisling.ActionUsed = "Taunt";

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 70,
            BodyAnimation = BodyAnimation.Tears,
            Sound = null,
            SourceId = sprite.Serial
        };

        var targets = aisling.GetInFrontToSide();

        if (targets.Count == 0)
        {
            OnFailed(aisling);
            return;
        }

        foreach (var target in targets)
        {
            if (target is not Monster monster) continue;
            monster.TargetRecord.TaggedAislings.Clear();
            monster.TargetRecord.TaggedAislings.TryAdd(client.Aisling.Serial, (450000, aisling, true, false));
            monster.Target = aisling;
            aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(Skill.Template.TargetAnimation, monster.Position));
        }

        _skillMethod.Train(client, Skill);
        aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendBodyAnimation(action.SourceId, action.BodyAnimation, action.AnimationSpeed));
    }

    public override void OnUse(Sprite sprite)
    {
        if (!Skill.CanUse()) return;
        if (sprite is not Aisling aisling) return;
        OnSuccess(aisling);
    }
}

[Script("Briarthorn Aura")]
public class Briarthorn(Skill skill) : SkillScript(skill)
{
    private readonly GlobalSkillMethods _skillMethod = new();

    public override void OnFailed(Sprite sprite)
    {
        if (sprite is not Aisling damageDealingAisling) return;
        var client = damageDealingAisling.Client;

        client.SendServerMessage(ServerMessageType.OrangeBar1, "Failed.");
        sprite.PlayerNearby?.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(Skill.Template.MissAnimation, null, sprite.Serial));
    }

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
        var client = aisling.Client;
        aisling.ActionUsed = "Briarthorns";

        var hasLawAura = aisling.Buffs.TryGetValue("Laws of Aosda", out var laws);
        if (hasLawAura)
            laws.OnEnded(aisling, laws);

        var buff = new aura_BriarThorn();
        client.EnqueueBuffAppliedEvent(aisling, buff);
        _skillMethod.Train(client, Skill);
    }

    public override void OnUse(Sprite sprite)
    {
        if (!Skill.CanUse()) return;
        if (sprite is not Aisling aisling) return;
        if (aisling.HasBuff("Briarthorn Aura"))
        {
            aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=cAura: Already active");
            return;
        }

        OnSuccess(aisling);
    }
}

[Script("Laws of Aosda")]
public class LawsOfAosda(Skill skill) : SkillScript(skill)
{
    private readonly GlobalSkillMethods _skillMethod = new();

    public override void OnFailed(Sprite sprite)
    {
        if (sprite is not Aisling damageDealingAisling) return;
        var client = damageDealingAisling.Client;

        client.SendServerMessage(ServerMessageType.OrangeBar1, "Failed.");
        sprite.PlayerNearby?.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(Skill.Template.MissAnimation, null, sprite.Serial));
    }

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
        var client = aisling.Client;
        aisling.ActionUsed = "Laws of Aosda";

        var hasBriarAura = aisling.Buffs.TryGetValue("Briarthorn Aura", out var briar);
        if (hasBriarAura)
            briar.OnEnded(aisling, briar);

        var buff = new aura_LawsOfAosda();
        client.EnqueueBuffAppliedEvent(aisling, buff);
        _skillMethod.Train(client, Skill);
    }

    public override void OnUse(Sprite sprite)
    {
        if (!Skill.CanUse()) return;
        if (sprite is not Aisling aisling) return;
        OnSuccess(aisling);
    }
}

[Script("Shield Bash")]
public class ShieldBash(Skill skill) : SkillScript(skill)
{
    private Sprite _target;
    private bool _crit;
    private bool _success;
    private readonly GlobalSkillMethods _skillMethod = new();

    public override void OnFailed(Sprite sprite)
    {
        if (_target is not { Alive: true }) return;
        if (sprite.NextTo(_target.Position.X, _target.Position.Y) && sprite.Facing(_target.Position.X, _target.Position.Y, out _))
            sprite.PlayerNearby?.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(Skill.Template.MissAnimation, null, _target.Serial));
    }

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 30,
            BodyAnimation = BodyAnimation.Assail,
            Sound = null,
            SourceId = aisling.Serial
        };

        var enemy = aisling.DamageableGetInFront(2);

        if (enemy.Count == 0)
        {
            _skillMethod.FailedAttempt(aisling, Skill, action);
            OnFailed(aisling);
            return;
        }

        aisling.ActionUsed = "Shield Bash";

        foreach (var i in enemy.Where(i => aisling.Serial != i.Serial).Where(i => i.Attackable))
        {
            _target = i;
            var dmgCalc = DamageCalc(sprite);
            if (aisling.BlessedShield)
                dmgCalc *= 2;
            _skillMethod.OnSuccessWithoutAction(_target, aisling, Skill, dmgCalc, _crit);
        }

        aisling.BlessedShield = false;
        aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendBodyAnimation(action.SourceId, action.BodyAnimation, action.AnimationSpeed));
    }

    public override void OnUse(Sprite sprite)
    {
        if (!Skill.CanUse()) return;
        if (sprite is not Aisling aisling) return;
        var client = aisling.Client;

        if (client.Aisling.EquipmentManager.Equipment[3]?.Item?.Template.Group is not ("Shields"))
        {
            OnFailed(aisling);
            return;
        }

        _success = _skillMethod.OnUse(aisling, Skill);

        if (_success)
        {
            OnSuccess(aisling);
        }
        else
        {
            OnFailed(aisling);
        }
    }

    private long DamageCalc(Sprite sprite)
    {
        _crit = false;
        long dmg = 0;
        if (sprite is Aisling damageDealingAisling)
        {
            var client = damageDealingAisling.Client;
            var imp = 40 + Skill.Level;
            dmg = client.Aisling.Str * 18 + client.Aisling.Dex * 10 * Math.Max(damageDealingAisling.Position.DistanceFrom(_target.Position), 2);
            dmg += dmg * imp / 100;
        }

        var critCheck = _skillMethod.OnCrit(dmg);
        _crit = critCheck.Item1;
        return critCheck.Item2;
    }
}

[Script("Blessed Shield")]
public class BlessedShield(Skill skill) : SkillScript(skill)
{
    public override void OnFailed(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
        aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Must have a shield equipped to Bless");
    }

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
        aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(295, null, aisling.Serial));
        aisling.BlessedShield = true;
    }

    public override void OnUse(Sprite sprite)
    {
        if (!Skill.CanUse()) return;
        if (sprite is not Aisling aisling) return;
        var client = aisling.Client;

        if (client.Aisling.EquipmentManager.Equipment[3]?.Item?.Template.Group is not ("Shields"))
        {
            OnFailed(aisling);
            return;
        }

        OnSuccess(aisling);
    }
}

[Script("Wrath Blow")]
public class WrathBlow(Skill skill) : SkillScript(skill)
{
    private Sprite _target;
    private bool _success;
    private readonly GlobalSkillMethods _skillMethod = new();

    public override void OnFailed(Sprite sprite)
    {
        if (sprite is not Aisling damageDealingAisling) return;
        var client = damageDealingAisling.Client;

        client.SendServerMessage(ServerMessageType.OrangeBar1, "Failed");
        if (_target is not { Alive: true }) return;
        if (sprite.NextTo(_target.Position.X, _target.Position.Y) && sprite.Facing(_target.Position.X, _target.Position.Y, out _))
            sprite.PlayerNearby?.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(Skill.Template.MissAnimation, null, _target.Serial));
    }

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
        aisling.ActionUsed = "Wrath Blow";

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 40,
            BodyAnimation = BodyAnimation.Assail,
            Sound = null,
            SourceId = sprite.Serial
        };

        var enemy = aisling.DamageableGetInFront().FirstOrDefault();
        _target = enemy;

        if (_target == null || _target.Serial == aisling.Serial || !_target.Attackable)
        {
            _skillMethod.FailedAttempt(aisling, Skill, action);
            OnFailed(aisling);
            return;
        }

        var debuff = new DebuffWrathConsequences();
        {
            if (_target.HasDebuff(debuff.Name))
                _target.RemoveDebuff(debuff.Name);

            _skillMethod.ApplyPhysicalDebuff(aisling.Client, debuff, _target, Skill);
        }

        _target.Target = aisling;
        var dmgCalc = DamageCalc(sprite);
        _target.ApplyDamage(aisling, dmgCalc, Skill);
        aisling.ThreatMeter += 15000000000;
        _skillMethod.OnSuccess(_target, aisling, Skill, 0, false, action);
    }

    public override void OnUse(Sprite sprite)
    {
        if (!Skill.CanUse()) return;
        if (sprite is not Aisling aisling) return;
        _success = _skillMethod.OnUse(aisling, Skill);

        if (_success)
        {
            OnSuccess(aisling);
        }
        else
        {
            OnFailed(aisling);
        }
    }

    private long DamageCalc(Sprite sprite)
    {
        long dmg = 0;
        if (sprite is not Aisling damageDealingAisling) return dmg;
        var client = damageDealingAisling.Client;
        var imp = 50 + Skill.Level;
        dmg = client.Aisling.Str * 12 + client.Aisling.Int * 3;
        dmg += dmg * imp / 100;

        return dmg;
    }
}