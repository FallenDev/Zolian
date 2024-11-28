using Chaos.Networking.Entities.Server;

using Darkages.Enums;
using Darkages.GameScripts.Spells;
using Darkages.Network.Server;
using Darkages.ScriptingBase;
using Darkages.Sprites;
using Darkages.Sprites.Entity;
using Darkages.Types;

namespace Darkages.GameScripts.Skills;

[Script("Grief Eruption")]
public class Grief_Eruption(Skill skill) : SkillScript(skill)
{
    private bool _crit;
    private const int Strength = 6;

    protected override void OnFailed(Sprite sprite)
    {
        if (sprite is Aisling aisling)
            aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "Failed");
        else
            GlobalSkillMethods.OnFailed(sprite, Skill, null);
    }

    protected override void OnSuccess(Sprite sprite)
    {
        if (sprite is Aisling aisling)
            aisling.ActionUsed = "Grief Eruption";

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 40,
            BodyAnimation = (BodyAnimation)0x06,
            Sound = null,
            SourceId = sprite.Serial
        };

        try
        {
            if (sprite is not Damageable damageDealer) return;
            var enemy = damageDealer.DamageableGetInFront(6);

            if (enemy.Count == 0)
            {
                OnFailed(sprite);
                return;
            }

            _ = SendAnimations(damageDealer, enemy);
            Task.Delay(200).Wait();
            var target = enemy.FirstOrDefault();

            if (target is null || target.Serial == sprite.Serial)
            {
                OnFailed(sprite);
                return;
            }

            if (target is not Damageable damageable) return;

            if (target.SpellReflect)
            {
                damageDealer.SendAnimationNearby(184, null, target.Serial);
                if (sprite is Aisling aislingReflect)
                    aislingReflect.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Your elemental ability has been reflected!");
                target = Spell.SpellReflect(target, damageDealer);
            }

            if (target.SpellNegate)
            {
                damageDealer.SendAnimationNearby(64, null, target.Serial);
                if (sprite is Aisling aislingReflect)
                    aislingReflect.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Your elemental ability has been deflected!");
                return;
            }

            var dmgCalc = DamageCalc(damageDealer, target);
            dmgCalc += GlobalSpellMethods.WeaponDamageElementalProc(damageDealer, Strength);
            damageable.ApplyElementalSkillDamage(damageDealer, dmgCalc, ElementManager.Element.Sorrow, Skill);
            GlobalSkillMethods.OnSuccess(target, damageDealer, Skill, 0, _crit, action);
        }
        catch
        {
            ServerSetup.EventsLogger($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}");
            SentrySdk.CaptureMessage($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}", SentryLevel.Error);
        }
    }

    public override void OnCleanup() { }

    public override void OnUse(Sprite sprite)
    {
        if (sprite is Aisling aisling)
        {
            var client = aisling.Client;

            if (sprite.CurrentMp - 2500 > 0)
            {
                sprite.CurrentMp -= 2500;
            }
            else
            {
                client.SendServerMessage(ServerMessageType.OrangeBar1, $"{ServerSetup.Instance.Config.NoManaMessage}");
                OnFailed(sprite);
                return;
            }

            if (client.Aisling.EquipmentManager.Equipment[1]?.Item?.Template.Group is not ("Wands" or "Staves" or "Swords"))
            {
                if (client.Aisling.EquipmentManager.Equipment[3]?.Item?.Template.Group is not "Sources")
                {
                    client.SendServerMessage(ServerMessageType.ActiveMessage, "I'm unable to channel with this equipment!");
                    OnFailed(aisling);
                    return;
                }
            }

            var success = GlobalSkillMethods.OnUse(aisling, Skill);

            if (success)
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
            if (sprite.CurrentMp - 2500 > 0)
            {
                sprite.CurrentMp -= 2500;
            }

            OnSuccess(sprite);
        }
    }

    private static async Task SendAnimations(Sprite damageDealingSprite, IEnumerable<Sprite> enemy)
    {
        if (damageDealingSprite is not Damageable damageable) return;

        foreach (var position in damageable.GetTilesInFront(damageDealingSprite.Position.DistanceFrom(enemy.FirstOrDefault()?.Position)))
        {
            var vector = new Position(position.X, position.Y);
            await Task.Delay(200).ContinueWith(ct =>
            {
                damageable.SendAnimationNearby(120, vector);
            });
        }
    }

    private long DamageCalc(Sprite sprite, Sprite target)
    {
        _crit = false;
        long dmg;
        if (sprite is Aisling damageDealingAisling)
        {
            var client = damageDealingAisling.Client;
            var imp = 10 + Skill.Level;
            var dist = damageDealingAisling.Position.DistanceFrom(target.Position) == 0 ? 1 : damageDealingAisling.Position.DistanceFrom(target.Position);
            dmg = client.Aisling.Int * 35 + client.Aisling.Wis * 25 / dist;
            dmg += dmg * imp / 100;
        }
        else
        {
            if (sprite is not Monster damageMonster) return 0;
            dmg = damageMonster.Int * 12;
            dmg += damageMonster.Wis * 10;
        }

        var critCheck = GlobalSkillMethods.OnCrit(dmg);
        _crit = critCheck.Item1;
        return critCheck.Item2;
    }
}