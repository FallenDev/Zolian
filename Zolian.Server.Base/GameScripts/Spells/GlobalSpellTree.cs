using Darkages.Enums;
using Darkages.GameScripts.Affects;
using Darkages.ScriptingBase;
using Darkages.Sprites;
using Darkages.Types;

namespace Darkages.GameScripts.Spells;

[Script("Leap")]
public class Leap(Spell spell) : SpellScript(spell)
{
    private readonly Spell _spell = spell;
    private readonly GlobalSpellMethods _spellMethod = new();

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target) { }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (sprite.CantMove) return;
    }
}

/// <summary>
/// Aite
/// </summary>
[Script("Dia Aite")]
public class DiaAite(Spell spell) : SpellScript(spell)
{
    private readonly Buff _buff = new buff_DiaAite();
    private readonly GlobalSpellMethods _spellMethod = new();

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target) { }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (target.HasBuff("Aite") || target.HasBuff("Dia Aite"))
        {
            if (sprite is Aisling aisling)
                aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Another spell of similar nature is already applied.");
            return;
        }

        _spellMethod.EnhancementOnUse(sprite, sprite is Monster ? sprite : target, Spell, _buff);
    }
}

[Script("Mine")]
public class Mine(Spell spell) : SpellScript(spell)
{
    private readonly GlobalSpellMethods _spellMethod = new();

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target) { }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (sprite is not Aisling aisling) return;
        if (aisling.Map.ID is not (624 or 625)) return;
        if (aisling.EquipmentManager.Equipment[1]?.Item?.Template.Name is not "Pickaxe") return;

        var miningExperience = aisling.QuestManager.StoneSmithingTier switch
        {
            "Novice" => 0,
            "Apprentice" => 1,
            "Journeyman" => 2,
            "Expert" => 3,
            "Artisan" => 4,
            _ => 0
        };

        switch (aisling.Map.ID)
        {
            case 624:
                {
                    var rand = Random.Shared.Next(0, 100) + miningExperience;
                    var ore = Random.Shared.Next(0, 100);
                    if (rand >= 90)
                    {
                        if (ore >= 51)
                        {
                            aisling.Client.GiveItem("Raw Talos");
                            aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, $"You've found some Talos!");
                        }
                        else
                        {
                            aisling.Client.GiveItem("Raw Copper");
                            aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, $"You've found some Copper!");
                        }
                    }

                    aisling.Client.SendServerMessage(ServerMessageType.OrangeBar2, $"Dig.. Dig.. Dig..");
                }
                break;
            case 625:
                {
                    var rand = Random.Shared.Next(0, 100) + miningExperience;
                    var ore = Random.Shared.Next(0, 100);
                    if (rand >= 95)
                    {
                        if (ore >= 51)
                        {
                            aisling.Client.GiveItem("Raw Hybrasyl");
                            aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, $"You've found some Hybrasyl!");
                        }
                        else
                        {
                            aisling.GiveGold((uint)Random.Shared.Next(1000, 10000));
                            aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, $"You've found some Gold!");
                        }
                    }

                    aisling.Client.SendServerMessage(ServerMessageType.OrangeBar2, $"Dig.. Dig.. Dig..");
                }
                break;
        }

        aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(Spell.Template.TargetAnimation, null, aisling.Serial));
    }
}