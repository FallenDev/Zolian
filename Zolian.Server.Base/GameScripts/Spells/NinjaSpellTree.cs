using Darkages.Common;
using Darkages.Enums;
using Darkages.GameScripts.Affects;
using Darkages.Network.Server;
using Darkages.ScriptingBase;
using Darkages.Sprites;
using Darkages.Sprites.Entity;
using Darkages.Types;

using MapFlags = Darkages.Enums.MapFlags;

namespace Darkages.GameScripts.Spells;

[Script("Deadly Poison")]
public class DeadlyPoison(Spell spell) : SpellScript(spell)
{
    private readonly GlobalSpellMethods _spellMethod = new();

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target)
    {
        if (sprite is not Aisling aisling) return;
        if (target == null) return;

        if (!Spell.CanUse())
        {
            aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Ninjutsu is not quite ready yet.");
            return;
        }

        if (target.SpellReflect)
        {
            if (sprite is Aisling caster)
            {
                caster.SendAnimationNearby(184, null, target.Serial);
                caster.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Your ninjutsu has been reflected!");
            }

            if (target is Aisling targetPlayer)
            {
                targetPlayer.SendAnimationNearby(184, null, target.Serial);
                targetPlayer.Client.SendServerMessage(ServerMessageType.OrangeBar1, $"You reflected {Spell.Template.Name}");
            }

            sprite = Spell.SpellReflect(target, sprite);
        }

        if (target.SpellNegate)
        {
            if (sprite is Aisling caster)
            {
                caster.SendAnimationNearby(64, null, target.Serial);
                caster.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Your ninjutsu has been deflected!");
            }

            if (target is not Aisling targetPlayer) return;
            targetPlayer.SendAnimationNearby(64, null, target.Serial);
            targetPlayer.Client.SendServerMessage(ServerMessageType.OrangeBar1, $"You deflected {Spell.Template.Name}");
            return;
        }

        if (aisling.CurrentMp - Spell.Template.ManaCost > 0)
        {
            aisling.CurrentMp -= Spell.Template.ManaCost;
            GlobalSpellMethods.Train(aisling.Client, Spell);
        }
        else
        {
            aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, $"{ServerSetup.Instance.Config.NoManaMessage}");
            return;
        }

        if (target is Aisling targetAisling && !targetAisling.Map.Flags.MapFlagIsSet(MapFlags.PlayerKill))
        {
            aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Too many witnesses, can't do it here.");
            return;
        }

        aisling.Client.Aisling.SendAnimationNearby(Spell.Template.TargetAnimation, null, target.Serial);
        var debuff = new DebuffDeadlyPoison();

        if (target is Monster)
        {
            debuff.OnApplied(target, debuff);
        }
        else
        {
            aisling.Client.EnqueueDebuffAppliedEvent(target, debuff);
        }
    }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (sprite is not Aisling playerAction) return;
        if (target == null) return;
        if (!Spell.CanUse())
        {
            if (sprite is Aisling aisling2)
                aisling2.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Ninjutsu is not quite ready yet.");
            return;
        }

        playerAction.ActionUsed = "Deadly Poison";
        var client = playerAction.Client;
        GlobalSpellMethods.Train(client, Spell);
        var success = GlobalSpellMethods.Execute(client, Spell);

        if (success)
        {
            OnSuccess(sprite, target);
        }
        else
        {
            GlobalSpellMethods.SpellOnFailed(playerAction, target, Spell);
        }
    }
}