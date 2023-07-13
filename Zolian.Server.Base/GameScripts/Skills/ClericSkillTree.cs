using Chaos.Common.Definitions;
using Darkages.Enums;
using Darkages.GameScripts.Affects;
using Darkages.Scripting;
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

        damageDealingSprite.Client.Aisling.Show(Scope.NearbyAislings, new ServerFormat29(76, damageDealingSprite.Pos));

        _skillMethod.Train(client, _skill);

        damageDealingSprite.Show(Scope.NearbyAislings, new ServerFormat19(_skill.Template.Sound));
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
            aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "This does not work here");
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
            var action = new ServerFormat29(197, newPos);
            damageDealingSprite.Show(Scope.NearbyAislings, action);
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
            var action = new ServerFormat29(197, newPos);
            damageDealingSprite.Show(Scope.NearbyAislings, action);
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
        client.Aisling.Show(Scope.NearbyAislings, new ServerFormat29(_skill.Template.MissAnimation, damageDealingAisling.Pos));
    }

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
        aisling.ActionUsed = "Smite";

        var action = new ServerFormat1A
        {
            Serial = aisling.Serial,
            Number = (byte)(aisling.Path == Class.Defender
                ? aisling.UsingTwoHanded ? 0x81 : 0x01
                : 0x01),
            Speed = 20
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

        aisling.Show(Scope.NearbyAislings, action);
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
                        if (_target.HasDebuff("Beag Suain"))
                            _target.RemoveDebuff("Beag Suain");

                        var debuff = new debuff_frozen();
                        {
                            if (_target.HasDebuff(debuff.Name))
                                _target.RemoveDebuff(debuff.Name);

                            _skillMethod.ApplyPhysicalDebuff(player.Client, debuff, _target, _skill);
                        }

                        var dmgCalc = DamageCalc(sprite);
                        _skillMethod.OnSuccessWithoutAction(_target, sprite, _skill, dmgCalc, _crit);
                        break;
                    }
            }
        }
    }

    private int DamageCalc(Sprite sprite)
    {
        _crit = false;
        int dmg;
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