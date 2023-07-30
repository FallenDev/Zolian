using Chaos.Common.Definitions;

using Darkages.Common;
using Darkages.Enums;
using Darkages.Scripting;
using Darkages.Sprites;
using Darkages.Types;

namespace Darkages.GameScripts.Spells;

[Script("Mor Stroich Pian Gar")]
public class Mor_Strioch_Pian_Gar : SpellScript
{
    private readonly Spell _spell;
    private readonly GlobalSpellMethods _spellMethod;

    public Mor_Strioch_Pian_Gar(Spell spell) : base(spell)
    {
        _spell = spell;
        _spellMethod = new GlobalSpellMethods();
    }

    public override void OnFailed(Sprite sprite, Sprite target)
    {
        if (sprite is Aisling aisling)
            aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, $"You're too weak to perform that action.");
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
        
        aisling.CastAnimation(_spell, null);
        aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(_spell.Template.Animation, aisling.Serial));

        foreach (var targetObj in targets)
        {
            if (targetObj.Serial == aisling.Serial) continue;

            if (targetObj.SpellNegate)
            {
                client.Aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(64, targetObj.Serial));
                client.SendServerMessage(ServerMessageType.OrangeBar1, "Your spell has been deflected!");
                
                if (targetObj is Aisling player)
                    player.Client.SendServerMessage(ServerMessageType.OrangeBar1, $"You deflected {_spell.Template.Name}");

                continue;
            }

            client.Aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(_spell.Template.TargetAnimation, targetObj.Serial));
            targetObj.ApplyElementalSpellDamage(aisling, damage, ElementManager.Element.Terror, _spell);
        }
    }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (!_spell.CanUse()) return;
        if (sprite is not Aisling aisling) return;
        var client = aisling.Client;
        _spellMethod.Train(client, _spell);
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
            
        if (aisling.CurrentMp < 0)
            aisling.CurrentMp = 0;
        if (aisling.CurrentHp < 0)
            aisling.CurrentHp = 1;

        var mR = Generator.RandNumGen100();

        if (mR > target.Will)
        {
            var success = _spellMethod.Execute(client, _spell);

            if (success)
            {
                if (client.Aisling.IsInvisible && _spell.Template.PostQualifiers is PostQualifier.BreakInvisible or PostQualifier.Both)
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
                }

                OnSuccess(aisling, target);
            }
            else
            {
                _spellMethod.SpellOnFailed(aisling, target, _spell);
            }
        }

        client.SendAttributes(StatUpdateType.Vitality);
    }
}

[Script("Ao Sith Gar")]
public class AoSithGar : SpellScript
{
    private readonly Spell _spell;
    private readonly GlobalSpellMethods _spellMethod;

    public AoSithGar(Spell spell) : base(spell)
    {
        _spell = spell;
        _spellMethod = new GlobalSpellMethods();
    }

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
            client.Aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(_spell.Template.TargetAnimation, targetObj.Serial));
            foreach (var debuff in targetObj.Debuffs.Values)
            {
                if (debuff.Name == "Skulled") continue;
                debuff.OnEnded(targetObj, debuff);
            }
        }

        foreach (var debuff in aisling.Debuffs.Values)
        {
            if (debuff.Name == "Skulled") continue;
            debuff.OnEnded(aisling, debuff);
        }
    }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (!_spell.CanUse()) return;
        if (sprite is not Aisling aisling) return;
        var client = aisling.Client;
        _spellMethod.Train(client, _spell);

        if (client.Aisling.IsInvisible && _spell.Template.PostQualifiers is PostQualifier.BreakInvisible or PostQualifier.Both)
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
        }

        OnSuccess(aisling, target);
        client.SendAttributes(StatUpdateType.Vitality);
    }
}