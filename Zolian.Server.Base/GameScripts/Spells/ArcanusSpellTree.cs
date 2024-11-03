using Darkages.Common;
using Darkages.Enums;
using Darkages.GameScripts.Affects;
using Darkages.Network.Server;
using Darkages.ScriptingBase;
using Darkages.Sprites;
using Darkages.Sprites.Entity;
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
        var manaSap = (long)(aisling.MaximumMp * .33);
        var healthSap = (long)(aisling.MaximumHp * .33);
        var damage = (long)((healthSap + manaSap) * 0.01) * 200;

        aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(Spell.Template.Animation, null, aisling.Serial));

        foreach (var targetObj in targets)
        {
            if (targetObj.Serial == aisling.Serial) continue;
            if (targetObj is not Damageable damageable) continue;

            if (targetObj.SpellNegate)
            {
                client.Aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(64, null, targetObj.Serial));
                client.SendServerMessage(ServerMessageType.OrangeBar1, "Your spell has been deflected!");

                if (targetObj is Aisling player)
                    player.Client.SendServerMessage(ServerMessageType.OrangeBar1, $"You deflected {Spell.Template.Name}");

                continue;
            }

            var mR = Generator.RandNumGen100();

            if (mR > targetObj.Will)
            {
                client.Aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(Spell.Template.TargetAnimation, targetObj.Position, targetObj.Serial));
                damageable.ApplyElementalSpellDamage(aisling, damage, ElementManager.Element.Terror, Spell);
            }
            else
            {
                client.Aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(115, null, targetObj.Serial));
            }
        }
    }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (!Spell.CanUse()) return;
        if (sprite is not Aisling aisling) return;
        var client = aisling.Client;
        var manaLoss = (long)(aisling.MaximumMp * .33);
        var healthLoss = (long)(aisling.MaximumHp * .33);
        var healthBoundsCheck = aisling.CurrentHp - healthLoss;
        var manaBoundsCheck = aisling.CurrentMp - manaLoss;

        if (healthBoundsCheck >= 1 && manaBoundsCheck >= 1)
        {
            aisling.CurrentHp -= healthLoss;
            aisling.CurrentMp -= manaLoss;
        }
        else
        {
            OnFailed(sprite, target);
            return;
        }

        _spellMethod.Train(client, Spell);

        if (aisling.CurrentMp < 0)
            aisling.CurrentMp = 0;
        if (aisling.CurrentHp < 0)
            aisling.CurrentHp = 1;

        var success = _spellMethod.Execute(client, Spell);

        if (success)
        {
            OnSuccess(aisling, target);
        }
        else
        {
            _spellMethod.SpellOnFailed(aisling, target, Spell);
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

        var manaSap = (long)(aisling.MaximumMp * .85);

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
            client.Aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(Spell.Template.TargetAnimation, null, targetObj.Serial));
            foreach (var debuff in targetObj.Debuffs.Values)
            {
                if (debuff.Affliction) continue;
                if (debuff.Name is "Skulled") continue;
                debuff.OnEnded(targetObj, debuff);
            }

            foreach (var buff in targetObj.Buffs.Values)
            {
                if (buff.Affliction) continue;
                if (buff.Name is "Double XP" or "Triple XP" or "Dia Haste") continue;
                buff.OnEnded(targetObj, buff);
            }
        }

        foreach (var debuff in aisling.Debuffs.Values)
        {
            if (debuff.Affliction) continue;
            if (debuff.Name is "Skulled") continue;
            debuff.OnEnded(aisling, debuff);
        }

        foreach (var buff in aisling.Buffs.Values)
        {
            if (buff.Affliction) continue;
            if (buff.Name is "Double XP" or "Triple XP" or "Dia Haste") continue;
            buff.OnEnded(aisling, buff);
        }
    }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (!Spell.CanUse()) return;
        if (sprite is Aisling aisling)
        {
            var client = aisling.Client;
            _spellMethod.Train(client, Spell);
            OnSuccess(aisling, target);
            client.SendAttributes(StatUpdateType.Vitality);
            return;
        }

        foreach (var targetObj in sprite.AislingsNearby())
        {
            if (targetObj == null) continue;
            targetObj.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(Spell.Template.TargetAnimation, targetObj.Position));
            foreach (var debuff in targetObj.Debuffs.Values)
            {
                if (debuff.Affliction) continue;
                if (debuff.Name == "Skulled") continue;
                debuff.OnEnded(targetObj, debuff);
            }

            foreach (var buff in targetObj.Buffs.Values)
            {
                if (buff.Affliction) continue;
                buff.OnEnded(targetObj, buff);
            }
        }
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
            _spellMethod.Train(aisling.Client, Spell);
            aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Another spell of similar nature is already applied.");
            return;
        }

        _spellMethod.EnhancementOnUse(sprite, sprite is Monster ? sprite : target, Spell, _buff);
    }
}

[Script("Ard Fas Nadur")]
public class Ard_Fas_Nadur(Spell spell) : SpellScript(spell)
{
    private readonly Buff _buff = new BuffArdFasNadur();
    private readonly GlobalSpellMethods _spellMethod = new();

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target) { }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (target == null) return;

        switch (sprite)
        {
            case Aisling playerAction:
                playerAction.ActionUsed = "Ard Fas Nadur";
                break;
            case Monster monsterAction:
                target = monsterAction;
                break;
        }

        if (target.HasBuff("Ard Fas Nadur"))
        {
            if (sprite is not Aisling aisling) return;
            _spellMethod.Train(aisling.Client, Spell);
            aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "You've already cast that spell.");
            return;
        }

        if (target.HasBuff("Mor Fas Nadur") || target.HasBuff("Fas Nadur") || target.HasBuff("Beag Fas Nadur"))
        {
            if (sprite is not Aisling aisling) return;
            _spellMethod.Train(aisling.Client, Spell);
            aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "A lessor version has already been cast.");
            return;
        }

        _spellMethod.EnhancementOnUse(sprite, target, Spell, _buff);
    }
}

[Script("Mor Fas Nadur")]
public class Mor_Fas_Nadur(Spell spell) : SpellScript(spell)
{
    private readonly Buff _buff = new BuffMorFasNadur();
    private readonly GlobalSpellMethods _spellMethod = new();

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target) { }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (target == null) return;

        switch (sprite)
        {
            case Aisling playerAction:
                playerAction.ActionUsed = "Mor Fas Nadur";
                break;
            case Monster monsterAction:
                target = monsterAction;
                break;
        }

        if (target.HasBuff("Ard Fas Nadur"))
        {
            if (sprite is not Aisling aisling) return;
            _spellMethod.Train(aisling.Client, Spell);
            aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "A more potent version has already been cast.");
            return;
        }

        if (target.HasBuff("Mor Fas Nadur"))
        {
            if (sprite is not Aisling aisling) return;
            _spellMethod.Train(aisling.Client, Spell);
            aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "You've already cast that spell.");
            return;
        }

        if (target.HasBuff("Fas Nadur") || target.HasBuff("Beag Fas Nadur"))
        {
            if (sprite is not Aisling aisling) return;
            _spellMethod.Train(aisling.Client, Spell);
            aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "A lessor version has already been cast.");
            return;
        }

        _spellMethod.EnhancementOnUse(sprite, target, Spell, _buff);
    }
}

[Script("Fas Nadur")]
public class Fas_Nadur(Spell spell) : SpellScript(spell)
{
    private readonly Buff _buff = new BuffFasNadur();
    private readonly GlobalSpellMethods _spellMethod = new();

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target) { }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (target == null) return;

        switch (sprite)
        {
            case Aisling playerAction:
                playerAction.ActionUsed = "Fas Nadur";
                break;
            case Monster monsterAction:
                target = monsterAction;
                break;
        }

        if (target.HasBuff("Ard Fas Nadur") || target.HasBuff("Mor Fas Nadur"))
        {
            if (sprite is not Aisling aisling) return;
            _spellMethod.Train(aisling.Client, Spell);
            aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "A more potent version has already been cast.");
            return;
        }

        if (target.HasBuff("Fas Nadur"))
        {
            if (sprite is not Aisling aisling) return;
            _spellMethod.Train(aisling.Client, Spell);
            aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "You've already cast that spell.");
            return;
        }

        if (target.HasBuff("Beag Fas Nadur"))
        {
            if (sprite is not Aisling aisling) return;
            _spellMethod.Train(aisling.Client, Spell);
            aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "A lessor version has already been cast.");
            return;
        }

        _spellMethod.EnhancementOnUse(sprite, target, Spell, _buff);
    }
}

[Script("Fas Spiorad")]
public class Fas_Spiorad(Spell spell) : SpellScript(spell)
{
    private readonly Buff _buff = new BuffFasSpiorad();
    private readonly GlobalSpellMethods _spellMethod = new();

    public override void OnFailed(Sprite sprite, Sprite target)
    {
        if (sprite is Aisling aisling)
            aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, $"Your body is too weak.");
    }

    public override void OnSuccess(Sprite sprite, Sprite target)
    {
        if (target == null) return;
        if (sprite is not Aisling aisling) return;
        var client = aisling.Client;

        if (!Spell.CanUse())
        {
            if (sprite is Aisling aisling2)
                aisling2.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Ability is not quite ready yet.");
            return;
        }

        if (aisling.CurrentMp - Spell.Template.ManaCost > 0)
        {
            aisling.CurrentMp -= Spell.Template.ManaCost;
            _spellMethod.Train(client, Spell);
        }
        else
        {
            client.SendServerMessage(ServerMessageType.OrangeBar1, $"{ServerSetup.Instance.Config.NoManaMessage}");
            return;
        }

        var success = _spellMethod.Execute(client, Spell);

        if (success)
        {
            _spellMethod.EnhancementOnUse(sprite, target, Spell, _buff);
        }
        else
        {
            _spellMethod.SpellOnFailed(aisling, target, Spell);
        }

        client.SendAttributes(StatUpdateType.Vitality);
    }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (!Spell.CanUse())
        {
            if (sprite is Aisling aisling2)
                aisling2.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Ability is not quite ready yet.");
            return;
        }

        if (target == null) return;
        if (target.HasBuff("Fas Spiorad"))
        {
            if (sprite is not Aisling aisling) return;
            _spellMethod.Train(aisling.Client, Spell);
            aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "Your body is too weak.");
            return;
        }

        var healthCheck = (int)(target.MaximumHp * 0.50);

        if (healthCheck > 0)
        {
            _spellMethod.EnhancementOnUse(sprite, target, Spell, _buff);
        }
        else
        {
            OnFailed(sprite, target);
        }
    }
}