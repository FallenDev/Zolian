using Darkages.Common;
using Darkages.Enums;
using Darkages.Interfaces;
using Darkages.Network.Formats.Models.ServerFormats;
using Darkages.Network.Server;
using Darkages.Sprites;
using Darkages.Types;

namespace Darkages.GameScripts.Skills;

public class GlobalSkillMethods : IGlobalSkillMethods
{
    private static bool Attempt(IGameClient client, Skill skill)
    {
        if (!client.Aisling.CanAttack) return false;

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

    public void ApplyPhysicalDebuff(IGameClient client, Debuff debuff, Sprite target, Skill skill)
    {
        ServerFormat1A action = null;

        if (target is Monster)
        {
            action = new ServerFormat1A
            {
                Serial = client.Aisling.Serial,
                Number = (byte)(client.Aisling.Path == Class.Defender
                    ? client.Aisling.UsingTwoHanded ? 0x8B : 0x01
                    : 0x01),
                Speed = 20
            };
        }

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

        debuff.OnApplied(target, debuff);

        if (target is Monster)
        {
            client.Aisling.Show(Scope.NearbyAislings, action);
        }
    }

    public void ApplyPhysicalBuff(Sprite target, Buff buff)
    {
        if (target is Aisling aisling)
        {
            var action = new ServerFormat1A
            {
                Serial = aisling.Client.Aisling.Serial,
                Number = (byte)(aisling.Client.Aisling.Path == Class.Defender
                    ? aisling.Client.Aisling.UsingTwoHanded ? 0x8B : 0x06
                    : 0x06),
                Speed = 20
            };

            aisling.Client.Aisling.Show(Scope.NearbyAislings, action);
        }

        buff.OnApplied(target, buff);
    }

    public Sprite[] GetInCone(Sprite sprite)
    {
        var objs = new List<Sprite>();
        var front = sprite.GetInFrontToSide();

        if (!front.Any()) return objs.ToArray();
        objs.AddRange(front.Where(monster => monster.EntityType == TileContent.Monster && monster.Alive));

        return objs.ToArray();
    }

    public void Step(Sprite sprite, int savedXStep, int savedYStep)
    {
        if (sprite is not Aisling damageDealingSprite) return;
        var warpPos = new Position(savedXStep, savedYStep);
        damageDealingSprite.Client.WarpTo(warpPos, true);
        GameServer.CheckWarpTransitions(damageDealingSprite.Client);
        damageDealingSprite.UpdateAddAndRemove();
        damageDealingSprite.Client.UpdateDisplay();
        damageDealingSprite.Client.LastMovement = DateTime.Now;
    }

    public void Train(IGameClient client, Skill skill) => client.TrainSkill(skill);

    public bool OnUse(Aisling aisling, Skill skill)
    {
        var client = aisling.Client;
        aisling.UsedSkill(skill);

        if (client.Aisling.Invisible && skill.Template.PostQualifiers is PostQualifier.BreakInvisible or PostQualifier.Both)
        {
            client.Aisling.Invisible = false;
            client.UpdateDisplay();
            return Attempt(client, skill);
        }

        return Attempt(client, skill);
    }

    public void OnSuccess(Sprite enemy, Sprite attacker, Skill skill, int dmg, bool crit, ServerFormat1A action)
    {
        var target = enemy;

        // Damage
        target = Skill.Reflect(target, attacker, skill);

        if (dmg > 0)
            target.ApplyDamage(attacker, dmg, skill);

        // Training
        if (attacker is Aisling aisling)
            Train(aisling.Client, skill);

        // Animation
        attacker.Show(Scope.NearbyAislings, new ServerFormat29(skill.Template.TargetAnimation, target.Pos));
        attacker.Show(Scope.NearbyAislings, action);
        skill.LastUsedSkill = DateTime.Now;
        if (!crit) return;
        attacker.Animate(387);
    }

    public void OnSuccessWithoutAction(Sprite enemy, Sprite attacker, Skill skill, int dmg, bool crit)
    {
        var target = enemy;

        // Damage
        target = Skill.Reflect(target, attacker, skill);

        if (dmg > 0)
            target.ApplyDamage(attacker, dmg, skill);

        // Training
        if (attacker is Aisling aisling)
            Train(aisling.Client, skill);

        // Animation
        attacker.Show(Scope.NearbyAislings, new ServerFormat29(skill.Template.TargetAnimation, target.Pos));
        skill.LastUsedSkill = DateTime.Now;
        if (!crit) return;
        attacker.Animate(387);
    }

    public int Thrown(IGameClient client, Skill skill, bool crit)
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

    public void FailedAttempt(Sprite sprite, Skill skill, ServerFormat1A action)
    {
        sprite.Show(Scope.NearbyAislings, new ServerFormat19(skill.Template.Sound));
        sprite.Show(Scope.NearbyAislings, action);
    }

    public (bool, int) OnCrit(int dmg)
    {
        var critRoll = Generator.RandNumGen100();
        if (critRoll < 99) return (false, dmg);
        dmg *= 2;
        return (true, dmg);
    }
}