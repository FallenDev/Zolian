using Darkages.Enums;
using Darkages.GameScripts.Affects;
using Darkages.ScriptingBase;
using Darkages.Sprites;
using Darkages.Types;

namespace Darkages.GameScripts.Skills;

// Sprint towards nearest enemy dealing concentrated damage, otherwise rush forward 4 steps
[Script("Iron Sprint")]
public class IronSprint(Skill skill) : SkillScript(skill)
{
    private Sprite _target;
    private bool _crit;
    private IEnumerable<Sprite> _enemyList;
    private bool _success;
    private readonly GlobalSkillMethods _skillMethod = new();

    public override void OnFailed(Sprite sprite)
    {
        if (sprite is not Aisling damageDealingAisling) return;
        var client = damageDealingAisling.Client;

        client.SendServerMessage(ServerMessageType.OrangeBar1, "*listens to birds chirp nearby* I may have failed, but I won't falter");
    }

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
        var client = aisling.Client;
        aisling.ActionUsed = "Iron Sprint";

        if (_target == null)
        {
            OnFailed(aisling);
            return;
        }

        var dmgCalc = DamageCalc(aisling);
        var targetPos = aisling.GetFromAllSidesEmpty(_target);
        if (targetPos == null || targetPos == _target.Position)
        {
            OnFailed(aisling);
            return;
        }

        _skillMethod.Step(aisling, targetPos.X, targetPos.Y);
        aisling.Facing(_target.X, _target.Y, out var direction);
        aisling.Direction = (byte)direction;
        aisling.Turn();
        _target.ApplyDamage(aisling, dmgCalc, Skill);
        _skillMethod.Train(client, Skill);
        aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(Skill.Template.TargetAnimation, null, _target.Serial));

        if (!_crit) return;
        aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(387, null, sprite.Serial));
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
            var imp = Skill.Level * 3;
            dmg = client.Aisling.Str * 15 + client.Aisling.Dex * 15;
            dmg += dmg * imp / 100;
        }
        else
        {
            if (sprite is not Monster damageMonster) return 0;
            dmg = damageMonster.Str * 15 + damageMonster.Dex * 15;
        }

        var critCheck = _skillMethod.OnCrit(dmg);
        _crit = critCheck.Item1;
        return critCheck.Item2;
    }

    private void Target(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;

        _enemyList = GetObjects(aisling.Map, i => i != null && i.WithinRangeOf(aisling, 8), Get.AislingDamage).Where(i => i.Serial != aisling.Serial);
        _enemyList = _enemyList.OrderBy(i => i.DistanceFrom(aisling.X, aisling.Y)).ToList();
        _target = _enemyList.FirstOrDefault();
        _target = Skill.Reflect(_target, sprite, Skill);

        if (_target == null)
        {
            var mapCheck = aisling.Map.ID;
            var wallPosition = aisling.GetPendingChargePositionNoTarget(4, aisling);
            var wallPos = _skillMethod.DistanceTo(aisling.Position, wallPosition);

            if (mapCheck != aisling.Map.ID) return;
            if (!(wallPos > 0)) OnFailed(aisling);

            if (aisling.Position != wallPosition)
            {
                _skillMethod.Step(aisling, wallPosition.X, wallPosition.Y);
            }

            if (wallPos <= 2)
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