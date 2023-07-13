using Chaos.Common.Definitions;
using Darkages.Scripting;
using Darkages.Sprites;
using Darkages.Types;

namespace Darkages.GameScripts.Spells.attributes;

[Script("Deireas Faileas")]
public class deireasfaileas : SpellScript
{
    public deireasfaileas(Spell spell) : base(spell) { }

    public override void OnFailed(Sprite sprite, Sprite target)
    {
        if (sprite is not Aisling aisling) return;
        var client = aisling.Client;
        client.SendServerMessage(ServerMessageType.OrangeBar1, "Failed to cast.");
    }

    public override void OnSuccess(Sprite sprite, Sprite target)
    {
    }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (target is not Aisling aisling) return;
        if (aisling.HasBuff("Deireas Faileas"))
        {
            aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Offensive spells are now being deflected.");
            return;
        }

        aisling.HasManaFor(Spell)?.Cast(Spell, target)
            ?.ApplyBuff("buff_spell_reflect").Cast<Aisling>()
            ?.UpdateStats(Spell)
            ?.TrainSpell(Spell);
    }
}