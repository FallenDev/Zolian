using Chaos.Common.Definitions;
using Chaos.Networking.Entities.Server;

using Darkages.Common;
using Darkages.Enums;
using Darkages.Network.Client;
using Darkages.Sprites;
using Darkages.Types;

namespace Darkages.GameScripts.Skills;

public class GlobalSkillMethods : IGlobalSkillMethods
{
    private static bool Attempt(WorldClient client, Skill skill)
    {
        if (client.Aisling.CantAttack) return false;

        var success = Generator.RandNumGen100();

        if (skill.Level == 100)
        {
            return success >= 2;
        }

        return success switch
        {
            <= 25 when skill.Level <= 29 => false,
            <= 15 when skill.Level <= 49 => false,
            <= 10 when skill.Level <= 74 => false,
            <= 5 when skill.Level <= 99 => false,
            _ => true
        };
    }

    public int DistanceTo(Position spritePos, Position inputPos)
    {
        var spriteX = spritePos.X;
        var spriteY = spritePos.Y;
        var inputX = inputPos.X;
        var inputY = inputPos.Y;
        var diffX = Math.Abs(spriteX - inputX);
        var diffY = Math.Abs(spriteY - inputY);

        return diffX + diffY;
    }

    public void ApplyPhysicalDebuff(WorldClient client, Debuff debuff, Sprite target, Skill skill)
    {
        if (client != null)
        {
            var dmg = 0;

            if (!debuff.Name.Contains("Beag Suain"))
            {
                var knockOutDmg = Generator.RandNumGen100();

                if (knockOutDmg >= 98)
                {
                    dmg += knockOutDmg * client.Aisling.Str * 3;
                }
                else
                {
                    dmg += knockOutDmg * client.Aisling.Str * 1;
                }

                target.ApplyDamage(client.Aisling, dmg, skill);
            }
        }

        if (client != null)
            client.EnqueueDebuffAppliedEvent(target, debuff);
        else if (target is Aisling targetPlayer)
            targetPlayer.Client.EnqueueDebuffAppliedEvent(target, debuff);
        else
            debuff.OnApplied(target, debuff);

        if (target is not Monster) return;
        var animationPick = client?.Aisling.Path == Class.Defender
            ? client.Aisling.UsingTwoHanded
                ? BodyAnimation.Swipe
                : BodyAnimation.TwoHandAtk
            : BodyAnimation.Assail;

        client?.Aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendBodyAnimation(client.Aisling.Serial, animationPick, 20));
    }

    public void ApplyPhysicalBuff(Sprite target, Buff buff)
    {
        if (target is Aisling aisling)
        {
            var animationPick = aisling.Path == Class.Defender
                ? aisling.UsingTwoHanded
                    ? BodyAnimation.Swipe
                    : BodyAnimation.TwoHandAtk
                : BodyAnimation.Assail;

            aisling.Client.EnqueueBuffAppliedEvent(aisling, buff);
            aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendBodyAnimation(aisling.Serial, animationPick, 20));
            return;
        }

        buff.OnApplied(target, buff);
    }

    public Sprite[] GetInCone(Sprite sprite)
    {
        var objs = new List<Sprite>();
        var front = sprite.GetInFrontToSide();

        if (!front.Any()) return objs.ToArray();
        objs.AddRange(front.Where(monster => monster.TileType == TileContent.Monster && monster.Alive));

        return objs.ToArray();
    }

    public void Step(Sprite sprite, int savedXStep, int savedYStep)
    {
        if (sprite is not Aisling damageDealingSprite) return;
        var warpPos = new Position(savedXStep, savedYStep);
        damageDealingSprite.Client.WarpTo(warpPos);
        damageDealingSprite.Client.CheckWarpTransitions(damageDealingSprite.Client, savedXStep, savedYStep);
        damageDealingSprite.Client.SendRemoveObject(damageDealingSprite.Serial);
        damageDealingSprite.Client.UpdateDisplay();
        damageDealingSprite.Client.LastMovement = DateTime.UtcNow;
    }

    public void Train(WorldClient client, Skill skill) => client.TrainSkill(skill);

    public bool OnUse(Aisling aisling, Skill skill)
    {
        var client = aisling.Client;
        aisling.UsedSkill(skill);

        if (client.Aisling.IsInvisible && (skill.Template.PostQualifiers.QualifierFlagIsSet(PostQualifier.BreakInvisible) || skill.Template.PostQualifiers.QualifierFlagIsSet(PostQualifier.Both)))
        {
            if (client.Aisling.Buffs.TryRemove("Hide", out var hide))
            {
                hide.OnEnded(client.Aisling, hide);
            }

            if (client.Aisling.Buffs.TryRemove("Shadowfade", out var shadowFade))
            {
                shadowFade.OnEnded(client.Aisling, shadowFade);
            }

            client.UpdateDisplay();
            return Attempt(client, skill);
        }

        return Attempt(client, skill);
    }

    public void OnSuccess(Sprite enemy, Sprite attacker, Skill skill, long dmg, bool crit, BodyAnimationArgs action)
    {
        var target = enemy;

        // Damage
        if (dmg > 0)
        {
            target = Skill.Reflect(target, attacker, skill);
            target.ApplyDamage(attacker, dmg, skill);
        }

        // Training
        if (attacker is Aisling aisling)
            Train(aisling.Client, skill);

        // Animation
        attacker.PlayerNearby?.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(skill.Template.TargetAnimation, null, enemy.Serial));
        attacker.PlayerNearby?.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendBodyAnimation(action.SourceId, action.BodyAnimation, action.AnimationSpeed, action.Sound));
        skill.LastUsedSkill = DateTime.UtcNow;
        if (!crit) return;
        attacker.PlayerNearby?.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(387, null, attacker.Serial));
    }

    public void OnSuccessWithoutAction(Sprite enemy, Sprite attacker, Skill skill, long dmg, bool crit)
    {
        var target = enemy;

        // Damage
        if (dmg > 0)
        {
            target = Skill.Reflect(target, attacker, skill);
            target.ApplyDamage(attacker, dmg, skill);
        }

        // Training
        if (attacker is Aisling aisling)
            Train(aisling.Client, skill);

        // Animation
        attacker.PlayerNearby?.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(skill.Template.TargetAnimation, null, enemy.Serial));
        skill.LastUsedSkill = DateTime.UtcNow;
        if (!crit) return;
        attacker.PlayerNearby?.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(387, null, attacker.Serial));
    }

    public void OnSuccessWithoutActionAnimation(Sprite enemy, Sprite attacker, Skill skill, long dmg, bool crit)
    {
        var target = enemy;

        // Damage
        if (dmg > 0)
        {
            target = Skill.Reflect(target, attacker, skill);
            target.ApplyDamage(attacker, dmg, skill);
        }

        // Training
        if (attacker is Aisling aisling)
            Train(aisling.Client, skill);

        skill.LastUsedSkill = DateTime.UtcNow;
        if (!crit) return;
        attacker.PlayerNearby?.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(387, null, attacker.Serial));
    }

    public int Thrown(WorldClient client, Skill skill, bool crit)
    {
        if (client.Aisling.EquipmentManager.Equipment[1].Item?.Template.Group is not ("Glaives" or "Shuriken" or "Daggers" or "Bows")) return 10015;
        return client.Aisling.EquipmentManager.Equipment[1].Item.Template.Group switch
        {
            "Glaives" => 10012,
            "Shuriken" => 10011,
            "Daggers" => 10009,
            "Bows" => crit ? 10002 : 10000,
            "Apple" => 10010,
            _ => 10015
        };
        // 10006,7,8 = ice arrows, 10003,4,5 = fire arrows
    }

    public void FailedAttempt(Sprite sprite, Skill skill, BodyAnimationArgs action)
    {
        sprite.PlayerNearby?.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendBodyAnimation(action.SourceId, action.BodyAnimation, action.AnimationSpeed, action.Sound));
    }

    public (bool, long) OnCrit(long dmg)
    {
        var critRoll = Generator.RandNumGen100();
        if (critRoll < 99) return (false, dmg);
        dmg *= 2;
        return (true, dmg);
    }
}