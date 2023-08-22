using Chaos.Common.Definitions;
using Chaos.Networking.Entities.Server;

using Darkages.Enums;
using Darkages.GameScripts.Affects;
using Darkages.ScriptingBase;
using Darkages.Sprites;
using Darkages.Types;
using MapFlags = Darkages.Enums.MapFlags;

namespace Darkages.GameScripts.Skills;

// Cleric Skills
// Blink = Teleport 
[Script("Blink")]
public class Blink : SkillScript
{
    private readonly Skill _skill;
    private readonly GlobalSkillMethods _skillMethod;

    public Blink(Skill skill) : base(skill)
    {
        _skill = skill;
        _skillMethod = new GlobalSkillMethods();
    }

    public override void OnFailed(Sprite sprite)
    {
        if (sprite is Aisling aisling)
            aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "No suitable targets nearby.");
    }

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling damageDealingSprite) return;
        var client = damageDealingSprite.Client;

        damageDealingSprite.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(76, null, damageDealingSprite.Serial));
        _skillMethod.Train(client, _skill);
        damageDealingSprite.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendSound(_skill.Template.Sound, false));
    }

    public override void OnUse(Sprite sprite)
    {
        if (!_skill.CanUse()) return;
        if (sprite is not Aisling aisling) return;

        aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Use the Cleric's Feather (Drag & Drop on map)");
    }

    public override void ItemOnDropped(Sprite sprite, Position pos, Area map)
    {
        if (sprite is not Aisling damageDealingSprite) return;
        var client = damageDealingSprite.Client;
        damageDealingSprite.ActionUsed = "Blink";
        if (map.Flags.MapFlagIsSet(MapFlags.PlayerKill))
        {
            client.SendServerMessage(ServerMessageType.ActiveMessage, "This does not work here");
            return;
        }

        damageDealingSprite.FacingFarAway(pos.X, pos.Y, out var direction);
        damageDealingSprite.Direction = (byte)direction;
        SendPortAnimation(damageDealingSprite, pos);
        client.WarpTo(pos, false);
        OnSuccess(damageDealingSprite);
    }

    public void SendPortAnimation(Sprite sprite, Position pos)
    {
        if (sprite is not Aisling damageDealingSprite) return;
        var orgPos = sprite.Pos;
        var xDiff = orgPos.X - pos.X;
        var yDiff = orgPos.Y - pos.Y;
        var xGap = Math.Abs(xDiff);
        var yGap = Math.Abs(yDiff);
        var xDiffHold = 0;
        var yDiffHold = 0;

        for (var i = 0; i < yGap; i++)
        {
            switch (yDiff)
            {
                case < 0:
                    yDiffHold++;
                    break;
                case > 0:
                    yDiffHold--;
                    break;
            }

            var newPos = orgPos with { Y = orgPos.Y + yDiffHold };
            damageDealingSprite.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(197, new Position(newPos.X, newPos.Y)));
        }

        for (var i = 0; i < xGap; i++)
        {
            switch (xDiff)
            {
                case < 0:
                    xDiffHold++;
                    break;
                case > 0:
                    xDiffHold--;
                    break;
            }

            var newPos = orgPos with { X = orgPos.X + xDiffHold };
            damageDealingSprite.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(197, new Position(newPos.X, newPos.Y)));
        }
    }
}

[Script("Smite")]
public class Smite : SkillScript
{
    private readonly Skill _skill;
    private Sprite _target;
    private bool _crit;
    private bool _success;
    private readonly GlobalSkillMethods _skillMethod;

    public Smite(Skill skill) : base(skill)
    {
        _skill = skill;
        _skillMethod = new GlobalSkillMethods();
    }

    public override void OnFailed(Sprite sprite)
    {
        if (sprite is not Aisling damageDealingAisling) return;
        var client = damageDealingAisling.Client;

        client.SendServerMessage(ServerMessageType.OrangeBar1, "Failed to purify.");
        damageDealingAisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(76, null, damageDealingAisling.Serial));
    }

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
        aisling.ActionUsed = "Smite";

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 20,
            BodyAnimation = BodyAnimation.Assail,
            Sound = null,
            SourceId = aisling.Serial
        };

        var enemy = aisling.GetHorizontalInFront();
        enemy.AddRange(aisling.GetInFrontToSide());

        if (enemy.Count == 0)
        {
            _skillMethod.FailedAttempt(aisling, _skill, action);
            OnFailed(aisling);
            return;
        }

        foreach (var i in enemy.Where(i => aisling.Serial != i.Serial).Where(i => i.Attackable))
        {
            _target = i;

            if (_target is Monster)
            {
                if (_target.HasDebuff("Beag Suain"))
                    _target.RemoveDebuff("Beag Suain");

                var debuff = new debuff_frozen();
                {
                    if (_target.HasDebuff(debuff.Name))
                        _target.RemoveDebuff(debuff.Name);

                    _skillMethod.ApplyPhysicalDebuff(aisling.Client, debuff, _target, _skill);
                }
            }

            var dmgCalc = DamageCalc(sprite);
            _skillMethod.OnSuccessWithoutAction(_target, aisling, _skill, dmgCalc, _crit);
        }

        aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendBodyAnimation(action.SourceId, action.BodyAnimation, action.AnimationSpeed));
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
                case Aisling player:
                    {
                        if (player.HasDebuff("Beag Suain"))
                            player.RemoveDebuff("Beag Suain");

                        var debuff = new debuff_frozen();
                        {
                            if (player.HasDebuff(debuff.Name))
                                player.RemoveDebuff(debuff.Name);

                            _skillMethod.ApplyPhysicalDebuff(player.Client, debuff, player, _skill);
                        }

                        var dmgCalc = DamageCalc(sprite);
                        _skillMethod.OnSuccessWithoutAction(player, sprite, _skill, dmgCalc, _crit);
                        break;
                    }
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
            var imp = 50 + _skill.Level;
            dmg = client.Aisling.Str * 3 + client.Aisling.Int * 2;
            dmg += dmg * imp / 100;
        }
        else
        {
            if (sprite is not Monster damageMonster) return 0;
            dmg = damageMonster.Str * 3 + damageMonster.Int * 2;
        }

        var critCheck = _skillMethod.OnCrit(dmg);
        _crit = critCheck.Item1;
        return critCheck.Item2;
    }
}
// Almighty Strike 
// Consecrated Strike
// Soaking Hands = Increase your healing power for a short duration
// Remedy = remove all debuffs on self
// Wrath = Devastates target with massive dark elemental aligned attack
// Recite Scripture = Damage enemy with opposite element of their defense