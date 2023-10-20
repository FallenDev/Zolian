using Chaos.Common.Definitions;

using Darkages.Common;
using Darkages.Enums;
using Darkages.GameScripts.Affects;
using Darkages.ScriptingBase;
using Darkages.Sprites;
using Darkages.Types;

namespace Darkages.GameScripts.Spells;

[Script("Mor Stroich Pian Gar")]
public class Mor_Strioch_Pian_Gar(Spell spell) : SpellScript(spell)
{
    private readonly GlobalSpellMethods _spellMethod = new();

    public override void OnFailed(Sprite sprite, Sprite target)
    {
        if (sprite is Aisling aisling)
            aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, $"You're too weak to perform that action.");
    }

    public override void OnSuccess(Sprite sprite, Sprite target)
    {
        if (sprite is not Aisling aisling) return;
        var client = aisling.Client;
        aisling.ActionUsed = "Mor Stroich Pian Gar";
        var targets = GetObjects(aisling.Map, i => i.WithinRangeOf(aisling), Get.Monsters);

        // Damage Calc
        var manaSap = (int)(aisling.MaximumMp * .33);
        var healthSap = (int)(aisling.MaximumHp * .33);
        var damage = (int)((healthSap + manaSap) * 0.01) * 200;

        aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(spell.Template.Animation, null, aisling.Serial));

        foreach (var targetObj in targets)
        {
            if (targetObj.Serial == aisling.Serial) continue;

            if (targetObj.SpellNegate)
            {
                client.Aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(64, null, targetObj.Serial));
                client.SendServerMessage(ServerMessageType.OrangeBar1, "Your spell has been deflected!");

                if (targetObj is Aisling player)
                    player.Client.SendServerMessage(ServerMessageType.OrangeBar1, $"You deflected {spell.Template.Name}");

                continue;
            }

            var mR = Generator.RandNumGen100();

            if (mR > targetObj.Will)
            {
                client.Aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(spell.Template.TargetAnimation, targetObj.Position, targetObj.Serial));
                targetObj.ApplyElementalSpellDamage(aisling, damage, ElementManager.Element.Terror, spell);
            }
            else
            {
                client.Aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(115, null, targetObj.Serial));
            }
        }
    }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (!spell.CanUse()) return;
        if (sprite is not Aisling aisling) return;
        var client = aisling.Client;
        var manaLoss = (int)(aisling.MaximumMp * .33);
        var healthLoss = (int)(aisling.MaximumHp * .33);
        var healthBoundsCheck = aisling.CurrentHp - healthLoss;
        var manaBoundsCheck = aisling.CurrentMp - manaLoss;

        if (healthBoundsCheck >= 0 && manaBoundsCheck >= 0)
        {
            aisling.CurrentHp -= healthLoss;
            aisling.CurrentMp -= manaLoss;
        }
        else
        {
            OnFailed(sprite, target);
            return;
        }

        _spellMethod.Train(client, spell);

        if (aisling.CurrentMp < 0)
            aisling.CurrentMp = 0;
        if (aisling.CurrentHp < 0)
            aisling.CurrentHp = 1;

        var success = _spellMethod.Execute(client, spell);

        if (success)
        {
            OnSuccess(aisling, target);
        }
        else
        {
            _spellMethod.SpellOnFailed(aisling, target, spell);
        }

        client.SendAttributes(StatUpdateType.Vitality);
    }
}

[Script("Ao Sith Gar")]
public class AoSithGar(Spell spell) : SpellScript(spell)
{
    private readonly GlobalSpellMethods _spellMethod = new();

    public override void OnFailed(Sprite sprite, Sprite target)
    {
        if (sprite is Aisling aisling)
            aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "You're too weak to perform that action.");
    }

    public override void OnSuccess(Sprite sprite, Sprite target)
    {
        if (sprite is not Aisling aisling) return;
        var client = aisling.Client;
        aisling.ActionUsed = "Ao Sith Gar";

        var manaSap = (int)(aisling.MaximumMp * .85);

        if (aisling.CurrentMp < manaSap)
        {
            OnFailed(aisling, target);
            return;
        }

        aisling.CurrentMp -= manaSap;

        foreach (var targetObj in aisling.AislingsNearby())
        {
            if (targetObj.GroupParty != aisling.GroupParty) continue;
            if (targetObj.Serial == aisling.Serial) continue;
            client.Aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(spell.Template.TargetAnimation, null, targetObj.Serial));
            foreach (var debuff in targetObj.Debuffs.Values)
            {
                if (debuff.Name == "Skulled") continue;
                debuff.OnEnded(targetObj, debuff);
            }

            foreach (var buff in targetObj.Buffs.Values)
            {
                buff.OnEnded(targetObj, buff);
            }
        }

        foreach (var debuff in aisling.Debuffs.Values)
        {
            if (debuff.Name == "Skulled") continue;
            debuff.OnEnded(aisling, debuff);
        }

        foreach (var buff in aisling.Buffs.Values)
        {
            buff.OnEnded(aisling, buff);
        }
    }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (!spell.CanUse()) return;
        if (sprite is not Aisling aisling) return;
        var client = aisling.Client;
        _spellMethod.Train(client, spell);
        OnSuccess(aisling, target);
        client.SendAttributes(StatUpdateType.Vitality);
    }
}

[Script("Deireas Faileas")]
public class DeireasFaileas(Spell spell) : SpellScript(spell)
{
    private readonly Buff _buff = new buff_spell_reflect();
    private readonly GlobalSpellMethods _spellMethod = new();

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target) { }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (sprite.CantCast)
        {
            if (sprite is Aisling aisling)
                aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Incapacitated.");
            return;
        }

        if (sprite.HasBuff("Deireas Faileas"))
        {
            if (sprite is not Aisling aisling) return;
            _spellMethod.Train(aisling.Client, spell);
            aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Another spell of similar nature is already applied.");
            return;
        }

        _spellMethod.EnhancementOnUse(sprite, sprite is Monster ? sprite : target, spell, _buff);
    }
}