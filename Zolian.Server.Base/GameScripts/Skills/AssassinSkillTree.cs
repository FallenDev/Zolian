using Chaos.Networking.Entities.Server;

using Darkages.Enums;
using Darkages.GameScripts.Affects;
using Darkages.ScriptingBase;
using Darkages.Sprites;
using Darkages.Sprites.Entity;
using Darkages.Types;

namespace Darkages.GameScripts.Skills;

// Assassin Skills
[Script("Stab")]
public class Stab(Skill skill) : SkillScript(skill)
{
    private Sprite _target;
    private bool _crit;
    private bool _success;

    public override void OnFailed(Sprite sprite) => GlobalSkillMethods.OnFailed(sprite, Skill, _target);

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
        aisling.ActionUsed = "Stab";

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 40,
            BodyAnimation = BodyAnimation.Stab,
            Sound = null,
            SourceId = sprite.Serial
        };

        var enemy = aisling.DamageableGetInFront().FirstOrDefault();
        _target = enemy;

        if (_target == null || _target.Serial == aisling.Serial || !_target.Attackable)
        {
            GlobalSkillMethods.FailedAttemptBodyAnimation(aisling, action);
            OnFailed(aisling);
            return;
        }

        var dmgCalc = DamageCalc(sprite);
        if (aisling.IsInvisible)
            dmgCalc *= 2;

        GlobalSkillMethods.OnSuccess(_target, aisling, Skill, dmgCalc, _crit, action);
    }

    public override void OnUse(Sprite sprite)
    {
        if (!Skill.CanUse()) return;

        if (sprite is Aisling aisling)
        {
            _success = GlobalSkillMethods.OnUse(aisling, Skill);

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
            var action = new BodyAnimationArgs
            {
                AnimationSpeed = 30,
                BodyAnimation = BodyAnimation.Assail,
                Sound = null,
                SourceId = sprite.Serial
            };

            if (sprite is not Identifiable identifiable) return;
            var enemy = identifiable.MonsterGetInFront().FirstOrDefault();
            _target = enemy;

            if (_target == null || _target.Serial == sprite.Serial || !_target.Attackable)
            {
                GlobalSkillMethods.FailedAttemptBodyAnimation(sprite, action);
                OnFailed(sprite);
                return;
            }

            var dmgCalc = DamageCalc(sprite);
            GlobalSkillMethods.OnSuccess(_target, sprite, Skill, dmgCalc, _crit, action);
        }
    }

    private long DamageCalc(Sprite sprite)
    {
        _crit = false;
        long dmg;
        if (sprite is Aisling damageDealingAisling)
        {
            var client = damageDealingAisling.Client;
            var imp = 50 + Skill.Level;
            dmg = client.Aisling.Str * 2 + client.Aisling.Dex * 3;
            dmg += dmg * imp / 100;
        }
        else
        {
            if (sprite is not Monster damageMonster) return 0;
            dmg = damageMonster.Str * 2;
            dmg += damageMonster.Dex * 3;
        }

        var critCheck = GlobalSkillMethods.OnCrit(dmg);
        _crit = critCheck.Item1;
        return critCheck.Item2;
    }
}

[Script("Stab Twice")]
public class Stab_Twice(Skill skill) : SkillScript(skill)
{
    private Sprite _target;
    private bool _crit;
    private bool _success;

    public override void OnFailed(Sprite sprite) => GlobalSkillMethods.OnFailed(sprite, Skill, _target);

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
        aisling.ActionUsed = "Stab Twice";

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 40,
            BodyAnimation = BodyAnimation.DoubleStab,
            Sound = null,
            SourceId = sprite.Serial
        };

        var enemy = aisling.DamageableGetInFront().FirstOrDefault();
        _target = enemy;

        if (_target == null || _target.Serial == aisling.Serial || !_target.Attackable)
        {
            GlobalSkillMethods.FailedAttemptBodyAnimation(aisling, action);
            OnFailed(aisling);
            return;
        }

        var dmgCalc = DamageCalc(sprite);
        if (aisling.IsInvisible)
            dmgCalc *= 2;

        GlobalSkillMethods.OnSuccessWithoutActionAnimation(_target, aisling, Skill, dmgCalc, _crit);
        GlobalSkillMethods.OnSuccess(_target, aisling, Skill, dmgCalc, _crit, action);
    }

    public override void OnUse(Sprite sprite)
    {
        if (!Skill.CanUse()) return;

        if (sprite is Aisling aisling)
        {
            _success = GlobalSkillMethods.OnUse(aisling, Skill);

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
            var action = new BodyAnimationArgs
            {
                AnimationSpeed = 30,
                BodyAnimation = BodyAnimation.Assail,
                Sound = null,
                SourceId = sprite.Serial
            };

            if (sprite is not Identifiable identifiable) return;
            var enemy = identifiable.MonsterGetInFront().FirstOrDefault();
            _target = enemy;

            if (_target == null || _target.Serial == sprite.Serial || !_target.Attackable)
            {
                GlobalSkillMethods.FailedAttemptBodyAnimation(sprite, action);
                OnFailed(sprite);
                return;
            }

            var dmgCalc = DamageCalc(sprite);
            GlobalSkillMethods.OnSuccessWithoutActionAnimation(_target, sprite, Skill, dmgCalc, _crit);
            GlobalSkillMethods.OnSuccess(_target, sprite, Skill, dmgCalc, _crit, action);
        }
    }

    private long DamageCalc(Sprite sprite)
    {
        _crit = false;
        long dmg;
        if (sprite is Aisling damageDealingAisling)
        {
            var client = damageDealingAisling.Client;
            var imp = 50 + Skill.Level;
            dmg = client.Aisling.Str * 1 + client.Aisling.Dex * 3;
            dmg += dmg * imp / 100;
        }
        else
        {
            if (sprite is not Monster damageMonster) return 0;
            dmg = damageMonster.Str * 1;
            dmg += damageMonster.Dex * 3;
        }

        var critCheck = GlobalSkillMethods.OnCrit(dmg);
        _crit = critCheck.Item1;
        return critCheck.Item2;
    }
}

[Script("Stab'n Twist")]
public class Stab_and_Twist(Skill skill) : SkillScript(skill)
{
    private Sprite _target;
    private bool _crit;
    private bool _success;

    public override void OnFailed(Sprite sprite) => GlobalSkillMethods.OnFailed(sprite, Skill, _target);

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
        aisling.ActionUsed = "Stab'n Twist";

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 40,
            BodyAnimation = BodyAnimation.Stab,
            Sound = null,
            SourceId = sprite.Serial
        };

        var enemy = aisling.DamageableGetInFront();

        if (enemy.Count == 0)
        {
            GlobalSkillMethods.FailedAttemptBodyAnimation(aisling, action);
            OnFailed(aisling);
            return;
        }

        foreach (var i in enemy.Where(i => aisling.Serial != i.Serial).Where(i => i.Attackable))
        {
            _target = i;
            var dmgCalc = DamageCalc(sprite);
            if (aisling.IsInvisible)
                dmgCalc *= 2;

            var debuff = new DebuffStabnTwist();

            if (!_target.HasDebuff(debuff.Name))
                aisling.Client.EnqueueDebuffAppliedEvent(_target, debuff);

            if (_target is Aisling targetPlayer)
                targetPlayer.Client.SendAttributes(StatUpdateType.Secondary);

            GlobalSkillMethods.OnSuccessWithoutAction(_target, aisling, Skill, dmgCalc, _crit);
        }

        aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendBodyAnimation(action.SourceId, action.BodyAnimation, action.AnimationSpeed));
    }

    public override void OnUse(Sprite sprite)
    {
        if (!Skill.CanUse()) return;

        if (sprite is Aisling aisling)
        {
            _success = GlobalSkillMethods.OnUse(aisling, Skill);

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
            var action = new BodyAnimationArgs
            {
                AnimationSpeed = 30,
                BodyAnimation = BodyAnimation.Assail,
                Sound = null,
                SourceId = sprite.Serial
            };

            if (sprite is not Identifiable identifiable) return;
            var enemy = identifiable.MonsterGetInFront().FirstOrDefault();
            _target = enemy;

            if (_target == null || _target.Serial == sprite.Serial || !_target.Attackable)
            {
                GlobalSkillMethods.FailedAttemptBodyAnimation(sprite, action);
                OnFailed(sprite);
                return;
            }

            var debuff = new DebuffStabnTwist();

            if (_target is Aisling targetPlayer)
            {
                if (!_target.HasDebuff(debuff.Name))
                {
                    targetPlayer.Client.EnqueueDebuffAppliedEvent(_target, debuff);
                    targetPlayer.Client.SendAttributes(StatUpdateType.Secondary);
                }
            }
            else
            {
                if (!_target.HasDebuff(debuff.Name))
                    debuff.OnApplied(_target, debuff);
            }

            var dmg = DamageCalc(sprite);
            GlobalSkillMethods.OnSuccess(_target, sprite, Skill, dmg, false, action);
        }
    }

    private long DamageCalc(Sprite sprite)
    {
        _crit = false;
        long dmg;
        if (sprite is Aisling damageDealingAisling)
        {
            var client = damageDealingAisling.Client;
            var imp = 50 + Skill.Level;
            dmg = client.Aisling.Str * 2 + client.Aisling.Dex * 5;
            dmg += dmg * imp / 100;
        }
        else
        {
            if (sprite is not Monster damageMonster) return 0;
            dmg = damageMonster.Str * 2;
            dmg += damageMonster.Dex * 5;
        }

        var critCheck = GlobalSkillMethods.OnCrit(dmg);
        _crit = critCheck.Item1;
        return critCheck.Item2;
    }
}

[Script("Sneak")]
public class Sneak(Skill skill) : SkillScript(skill)
{
    private bool _success;

    public override void OnFailed(Sprite sprite)
    {
        if (sprite is not Aisling damageDealingAisling) return;
        damageDealingAisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Failed to blend in.");
    }

    public override void OnSuccess(Sprite sprite)
    {
        switch (sprite)
        {
            case Aisling aisling:
                {
                    var action = new BodyAnimationArgs
                    {
                        AnimationSpeed = 40,
                        BodyAnimation = BodyAnimation.HandsUp,
                        Sound = null,
                        SourceId = sprite.Serial
                    };

                    if (aisling.Dead || aisling.IsInvisible)
                    {
                        GlobalSkillMethods.FailedAttemptBodyAnimation(aisling, action);
                        OnFailed(aisling);
                        return;
                    }

                    var buff = new buff_hide();
                    aisling.Client.EnqueueBuffAppliedEvent(aisling, buff);
                    GlobalSkillMethods.Train(aisling.Client, Skill);
                    aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(Skill.Template.TargetAnimation, null, aisling.Serial));
                    break;
                }
            case Monster monster:
                {
                    var buff = new buff_hide();
                    buff.OnApplied(monster, buff);
                    break;
                }
        }
    }

    public override void OnUse(Sprite sprite)
    {
        if (!Skill.CanUse()) return;

        if (sprite is Aisling aisling)
        {
            _success = GlobalSkillMethods.OnUse(aisling, Skill);

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
public class Flurry(Skill skill) : SkillScript(skill)
{
    private Sprite _target;
    private bool _crit;
    private bool _success;

    public override void OnFailed(Sprite sprite) => GlobalSkillMethods.OnFailed(sprite, Skill, _target);

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
        aisling.ActionUsed = "Flurry";

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 40,
            BodyAnimation = BodyAnimation.DoubleStab,
            Sound = null,
            SourceId = sprite.Serial
        };

        var enemy = aisling.GetInFrontToSide();

        if (enemy.Count == 0)
        {
            GlobalSkillMethods.FailedAttemptBodyAnimation(aisling, action);
            OnFailed(aisling);
            return;
        }

        foreach (var i in enemy.Where(i => aisling.Serial != i.Serial).Where(i => i.Attackable))
        {
            _target = i;
            var dmgCalc = DamageCalc(sprite);
            if (aisling.IsInvisible)
                dmgCalc *= 2;

            GlobalSkillMethods.OnSuccessWithoutActionAnimation(_target, aisling, Skill, dmgCalc, _crit);
            GlobalSkillMethods.OnSuccessWithoutActionAnimation(_target, aisling, Skill, dmgCalc, _crit);
            GlobalSkillMethods.OnSuccessWithoutActionAnimation(_target, aisling, Skill, dmgCalc, _crit);
            GlobalSkillMethods.OnSuccessWithoutActionAnimation(_target, aisling, Skill, dmgCalc, _crit);
            aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(365, _target.Position));
        }

        aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendBodyAnimation(action.SourceId, action.BodyAnimation, action.AnimationSpeed));
    }

    public override void OnUse(Sprite sprite)
    {
        if (!Skill.CanUse()) return;

        if (sprite is Aisling aisling)
        {
            _success = GlobalSkillMethods.OnUse(aisling, Skill);

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
            if (sprite is not Identifiable identifiable) return;
            var enemy = identifiable.GetInFrontToSide();

            var action = new BodyAnimationArgs
            {
                AnimationSpeed = 30,
                BodyAnimation = BodyAnimation.Assail,
                Sound = null,
                SourceId = sprite.Serial
            };

            if (enemy.Count == 0) return;

            foreach (var i in enemy.Where(i => i != null && sprite.Serial != i.Serial && i.Attackable))
            {
                _target = Skill.Reflect(i, sprite, Skill);
                if (_target is not Damageable damageable) continue;
                var dmgCalc = DamageCalc(sprite);

                damageable.ApplyDamage(sprite, dmgCalc, Skill);
                damageable.ApplyDamage(sprite, dmgCalc, Skill);
                damageable.ApplyDamage(sprite, dmgCalc, Skill);
                damageable.ApplyDamage(sprite, dmgCalc, Skill);

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
            var imp = 50 + Skill.Level;
            dmg = client.Aisling.Dex * 2 + client.Aisling.Int * 1;
            dmg += dmg * imp / 100;
        }
        else
        {
            if (sprite is not Monster damageMonster) return 0;
            dmg = damageMonster.Dex * 2;
            dmg += damageMonster.Int * 1;
        }

        var critCheck = GlobalSkillMethods.OnCrit(dmg);
        _crit = critCheck.Item1;
        return critCheck.Item2;
    }
}

[Script("Entice")]
public class Enticed(Skill skill) : SkillScript(skill)
{
    private bool _success;

    public override void OnFailed(Sprite sprite)
    {
        if (sprite is not Aisling damageDealingAisling) return;
        damageDealingAisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "... What?");
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

        if (enemy.FirstOrDefault() is Aisling { EnticeImmunity: true } charmImmCheck)
        {
            charmImmCheck.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendPublicMessage(charmImmCheck.Serial, PublicMessageType.Normal, "Not Interested!"));
            charmImmCheck.Client.SendServerMessage(ServerMessageType.ActiveMessage, "{=qYou are immune to Entice");
            return;
        }

        foreach (var i in enemy.Where(i => i.Attackable))
        {
            var target = Skill.Reflect(i, damageDealingSprite, Skill);
            if (target == null) continue;

            if (target.HasDebuff("Beag Suain"))
                target.RemoveDebuff("Beag Suain");
            if (target.HasDebuff("Frozen"))
                target.RemoveDebuff("Frozen");

            var debuff = new DebuffCharmed();
            {
                if (target.HasDebuff(debuff.Name))
                    target.RemoveDebuff(debuff.Name);

                GlobalSkillMethods.ApplyPhysicalDebuff(client, debuff, target, Skill);
            }

            GlobalSkillMethods.Train(client, Skill);
        }
    }

    public override void OnUse(Sprite sprite)
    {
        if (!Skill.CanUse()) return;

        if (sprite is Aisling aisling)
        {
            _success = GlobalSkillMethods.OnUse(aisling, Skill);

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
                            damageDealingTarget.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendPublicMessage(damageDealingTarget.Serial, PublicMessageType.Normal, "Not Interested!"));
                            damageDealingTarget.Client.SendServerMessage(ServerMessageType.ActiveMessage, "{=qYou are immune to Entice");
                            return;
                        }

                        var debuff = new DebuffCharmed();
                        {
                            if (!damageDealingTarget.HasDebuff(debuff.Name))
                                GlobalSkillMethods.ApplyPhysicalDebuff(damageDealingTarget.Client, debuff, target, Skill);
                        }
                        break;
                    }
            }
        }
    }
}

[Script("Double-Edged Dance")]
public class Double_Edged_Dance(Skill skill) : SkillScript(skill)
{
    private Sprite _target;
    private bool _crit;
    private bool _success;

    public override void OnFailed(Sprite sprite) => GlobalSkillMethods.OnFailed(sprite, Skill, _target);

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
        aisling.ActionUsed = "Double-Edged Dance";

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 40,
            BodyAnimation = BodyAnimation.Stab,
            Sound = null,
            SourceId = sprite.Serial
        };

        var enemy = aisling.GetInFrontToSide();
        var enemyBehind = aisling.DamageableGetBehind(2);
        enemy.AddRange(enemyBehind);

        if (enemy.Count == 0)
        {
            GlobalSkillMethods.FailedAttemptBodyAnimation(aisling, action);
            OnFailed(aisling);
            return;
        }

        foreach (var i in enemy.Where(i => aisling.Serial != i.Serial).Where(i => i.Attackable))
        {
            _target = i;
            var dmgCalc = DamageCalc(sprite);
            if (aisling.IsInvisible)
                dmgCalc *= 2;

            GlobalSkillMethods.OnSuccessWithoutAction(_target, aisling, Skill, dmgCalc, _crit);
        }

        aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendBodyAnimation(action.SourceId, action.BodyAnimation, action.AnimationSpeed));
    }

    public override void OnUse(Sprite sprite)
    {
        if (!Skill.CanUse()) return;

        if (sprite is Aisling aisling)
        {
            _success = GlobalSkillMethods.OnUse(aisling, Skill);

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
            if (sprite is not Identifiable identifiable) return;
            var enemy = identifiable.GetInFrontToSide(2);
            var enemyBehind = identifiable.DamageableGetBehind(2);
            enemy.AddRange(enemyBehind);

            var action = new BodyAnimationArgs
            {
                AnimationSpeed = 30,
                BodyAnimation = BodyAnimation.Assail,
                Sound = null,
                SourceId = sprite.Serial
            };

            if (enemy.Count == 0) return;

            foreach (var i in enemy.Where(i => i != null && sprite.Serial != i.Serial && i.Attackable))
            {
                _target = Skill.Reflect(i, sprite, Skill);
                if (_target is not Damageable damageable) continue;
                var dmgCalc = DamageCalc(sprite);

                damageable.ApplyDamage(sprite, dmgCalc, Skill);

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
            var imp = 50 + Skill.Level;
            dmg = client.Aisling.Dex * 7 + client.Aisling.Str * 2;
            dmg += dmg * imp / 100;
        }
        else
        {
            if (sprite is not Monster damageMonster) return 0;
            dmg = damageMonster.Str * 2;
            dmg += damageMonster.Dex * 7;
        }

        var critCheck = GlobalSkillMethods.OnCrit(dmg);
        _crit = critCheck.Item1;
        return critCheck.Item2;
    }
}

[Script("Ebb'n Flow")]
public class Ebb_and_Flow(Skill skill) : SkillScript(skill)
{
    private Sprite _target;
    private bool _crit;
    private bool _success;

    public override void OnFailed(Sprite sprite) => GlobalSkillMethods.OnFailed(sprite, Skill, _target);

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
        aisling.ActionUsed = "Ebb'n Flow";

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 40,
            BodyAnimation = BodyAnimation.DoubleStab,
            Sound = null,
            SourceId = sprite.Serial
        };

        var enemy = aisling.DamageableGetInFront(3);
        var enemyBehind = aisling.DamageableGetBehind(3);
        enemy.AddRange(enemyBehind);

        if (enemy.Count == 0)
        {
            GlobalSkillMethods.FailedAttemptBodyAnimation(aisling, action);
            OnFailed(aisling);
            return;
        }

        foreach (var i in enemy.Where(i => aisling.Serial != i.Serial).Where(i => i.Attackable))
        {
            _target = i;
            var dmgCalc = DamageCalc(sprite);
            if (aisling.IsInvisible)
                dmgCalc *= 2;

            GlobalSkillMethods.OnSuccessWithoutAction(_target, aisling, Skill, dmgCalc, _crit);
        }

        aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendBodyAnimation(action.SourceId, action.BodyAnimation, action.AnimationSpeed));
    }

    public override void OnUse(Sprite sprite)
    {
        if (!Skill.CanUse()) return;

        if (sprite is Aisling aisling)
        {
            _success = GlobalSkillMethods.OnUse(aisling, Skill);

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
            if (sprite is not Identifiable identifiable) return;
            var enemy = identifiable.GetAllInFront(3);
            var enemyBehind = identifiable.DamageableGetBehind(3);
            enemy.AddRange(enemyBehind);

            var action = new BodyAnimationArgs
            {
                AnimationSpeed = 30,
                BodyAnimation = BodyAnimation.Assail,
                Sound = null,
                SourceId = sprite.Serial
            };

            if (enemy.Count == 0) return;

            foreach (var i in enemy.Where(i => i != null && sprite.Serial != i.Serial && i.Attackable))
            {
                _target = Skill.Reflect(i, sprite, Skill);
                if (_target is not Damageable damageable) continue;
                var dmgCalc = DamageCalc(sprite);

                damageable.ApplyDamage(sprite, dmgCalc, Skill);

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
            var imp = 50 + Skill.Level;
            dmg = client.Aisling.Con * 4 + client.Aisling.Dex * 8;
            dmg += dmg * imp / 100;
        }
        else
        {
            if (sprite is not Monster damageMonster) return 0;
            dmg = damageMonster.Con * 4;
            dmg += damageMonster.Dex * 8;
        }

        var critCheck = GlobalSkillMethods.OnCrit(dmg);
        _crit = critCheck.Item1;
        return critCheck.Item2;
    }
}

[Script("Shadow Step")]
public class Shadow_Step(Skill skill) : SkillScript(skill)
{
    private Sprite _target;
    private bool _crit;
    private IList<Sprite> _enemyList;
    private bool _success;

    public override void OnFailed(Sprite sprite) => GlobalSkillMethods.OnFailed(sprite, Skill, _target);

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
        aisling.ActionUsed = "Shadow Step";

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 30,
            BodyAnimation = BodyAnimation.HandsUp,
            Sound = null,
            SourceId = sprite.Serial
        };

        var targetPos = aisling.GetFromAllSidesEmpty(_target);

        if (_target == null || _target.Serial == aisling.Serial || targetPos == _target.Position)
        {
            GlobalSkillMethods.FailedAttemptBodyAnimation(aisling, action);
            OnFailed(aisling);
            return;
        }

        var oldPos = new Position(aisling.Pos);
        aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(63, oldPos));

        var dmgCalc = DamageCalc(aisling);
        if (aisling.IsInvisible)
            dmgCalc *= 2;
        GlobalSkillMethods.Step(aisling, targetPos.X, targetPos.Y);
        aisling.Facing(_target.X, _target.Y, out var direction);
        aisling.Direction = (byte)direction;
        aisling.Turn();
        GlobalSkillMethods.OnSuccess(_target, aisling, Skill, dmgCalc, _crit, action);
    }

    public override void OnUse(Sprite sprite)
    {
        if (!Skill.CanUse()) return;
        if (sprite is not Aisling aisling) return;

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
            dmg = client.Aisling.Int * 2 + client.Aisling.Dex * 5;
            dmg += dmg * imp / 100;
        }
        else
        {
            if (sprite is not Monster damageMonster) return 0;
            dmg = damageMonster.Int * 2 + damageMonster.Dex * 5;
        }

        var critCheck = GlobalSkillMethods.OnCrit(dmg);
        _crit = critCheck.Item1;
        return critCheck.Item2;
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
            _success = GlobalSkillMethods.OnUse(damageDealingSprite, Skill);

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