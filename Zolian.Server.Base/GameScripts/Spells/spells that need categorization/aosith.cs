using Darkages.Enums;
using Darkages.Network.Formats.Models.ServerFormats;
using Darkages.Scripting;
using Darkages.Sprites;
using Darkages.Types;

namespace Darkages.GameScripts.Spells.cures;

[Script("ao sith")]
public class ao_sith : SpellScript
{
    private readonly Random rand = new Random();

    public ao_sith(Spell spell) : base(spell)
    {
    }

    public override void OnFailed(Sprite sprite, Sprite target)
    {
        if (sprite is Aisling)
            (sprite as Aisling)
                .Client
                .SendMessage(0x02, "failed.");
    }

    public override void OnSuccess(Sprite sprite, Sprite target)
    {
        if (target == null)
            return;

        target.RemoveBuffsAndDebuffs();
        target.SendAnimation(Spell.Template.Animation, target, sprite);

        if (sprite is Aisling)
        {
            var client = (sprite as Aisling).Client;

            client.SendMessage(0x02, $"you cast {Spell.Template.Name}");

            var action = new ServerFormat1A
            {
                Serial = sprite.Serial,
                Number = (byte) (client.Aisling.Path == Class.Cleric ? 0x80 :
                    client.Aisling.Path == Class.Arcanus ? 0x88 : 0x06),
                Speed = 30
            };

            var hpbar = new ServerFormat13
            {
                Serial = client.Aisling.Serial,
                Health = 255,
                Sound = Spell.Template.Sound
            };

            client.Aisling.Show(Scope.NearbyAislings, action);
            client.Aisling.Show(Scope.NearbyAislings, hpbar);

            if (target is Aisling)
            {
                (target as Aisling).Client.SendStats(StatusFlags.MultiStat);

                (target as Aisling).Client
                    .SendMessage(0x02,
                        $"{client.Aisling.Username} Attacks you with {Spell.Template.Name}.");
            }
        }
        else
        {
            if (target is Aisling)
            {
                (target as Aisling).Client.SendStats(StatusFlags.MultiStat);
                (target as Aisling).Client
                    .SendMessage(0x02,
                        $"{(sprite is Monster ? (sprite as Monster).Template.Name : (sprite as Mundane).Template.Name) ?? "Monster"} Attacks you with {Spell.Template.Name}.");
            }

            var action = new ServerFormat1A
            {
                Serial = sprite.Serial,
                Number = 1,
                Speed = 30
            };

            var hpbar = new ServerFormat13
            {
                Serial = target.Serial,
                Health = 255,
                Sound = Spell.Template.Sound
            };

            sprite.Show(Scope.NearbyAislings, action);
            target.Show(Scope.NearbyAislings, hpbar);
        }
    }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (sprite is Aisling aisling)
        {
            aisling.Client.TrainSpell(Spell);

            if (sprite.CurrentMp - Spell.Template.ManaCost > 0)
                sprite.CurrentMp -= Spell.Template.ManaCost;

            if (sprite.CurrentMp < 0)
                sprite.CurrentMp = 0;

            (sprite as Aisling).Client.SendMessage(0x02, ServerSetup.Instance.Config.NoManaMessage);
        }

        // ToDo: Rand Lock using Gen
        var success = true;//Spell.RollDice(rand);

        if (success)
            OnSuccess(sprite, target);
        else
            OnFailed(sprite, target);

        if (sprite is Aisling)
            (sprite as Aisling)
                .Client
                .SendStats(StatusFlags.StructB);
    }
}