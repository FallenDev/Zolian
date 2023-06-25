using Darkages.Common;
using Darkages.Enums;
using Darkages.Network.Formats.Models.ServerFormats;
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
            aisling.Client.SendMessage(0x02, $"You're too weak to perform that action.");
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

        foreach (var targetObj in targets)
        {
            if (targetObj.Serial == aisling.Serial) continue;

            if (target.SpellNegate)
            {
                target.Animate(64);
                client.SendMessage(0x02, "Your spell has been deflected!");
                if (target is Aisling)
                    target.Client.SendMessage(0x02, $"You deflected {_spell.Template.Name}.");

                continue;
            }

            aisling.Cast(_spell, target);
            client.Aisling.Show(Scope.NearbyAislings, new ServerFormat29(_spell.Template.TargetAnimation, targetObj.Pos));
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
                if (client.Aisling.Invisible && _spell.Template.PostQualifiers is PostQualifier.BreakInvisible or PostQualifier.Both)
                {
                    client.Aisling.Invisible = false;
                    client.UpdateDisplay();
                }

                OnSuccess(aisling, target);
            }
            else
            {
                _spellMethod.SpellOnFailed(aisling, target, _spell);
            }
        }

        client.SendStats(StatusFlags.StructB);
    }
}