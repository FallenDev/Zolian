using Darkages.Enums;
using Darkages.Network.Formats.Models.ServerFormats;
using Darkages.Scripting;
using Darkages.Sprites;
using Darkages.Types;

namespace Darkages.GameScripts.Skills;

[Script("Identify Weapon")]
public class IdentifyWeapon : SkillScript
{
    private readonly Skill _skill;

    public IdentifyWeapon(Skill skill) : base(skill)
    {
        _skill = skill;
    }

    public override void OnFailed(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
        var client = aisling.Client;

        client.SendMessage(0x02, "Hmm, can't seem to identify that.");
    }

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
        var client = aisling.Client;

        var action = new ServerFormat1A
        {
            Serial = client.Aisling.Serial,
            Number = 0x91,
            Speed = 50
        };

        try
        {
            var itemToBeIdentified = aisling.Inventory.Items[1];

            if (itemToBeIdentified == null)
            {
                OnFailed(aisling);
                return;
            }

            if (itemToBeIdentified.Template.ScriptName != "Weapon")
            {
                OnFailed(aisling);
                return;
            }

            var mainHandOnly = !itemToBeIdentified.Template.Flags.FlagIsSet(ItemFlags.DualWield) & !itemToBeIdentified.Template.Flags.FlagIsSet(ItemFlags.TwoHanded) & !itemToBeIdentified.Template.Flags.FlagIsSet(ItemFlags.TwoHandedStaff);
            var dualWield = itemToBeIdentified.Template.Flags.FlagIsSet(ItemFlags.DualWield);
            var twoHander = itemToBeIdentified.Template.Flags.FlagIsSet(ItemFlags.TwoHanded) | itemToBeIdentified.Template.Flags.FlagIsSet(ItemFlags.TwoHandedStaff);
            var answerMainHand = mainHandOnly switch
            {
                true => "Yes",
                false => "No"
            };
            var answerDualWield = dualWield switch
            {
                true => "Yes",
                false => "No"
            };
            var answerTwoHander = twoHander switch
            {
                true => "Yes",
                false => "No"
            };

            var classString = itemToBeIdentified.Template.Class != Class.Peasant ? ClassStrings.ClassValue(itemToBeIdentified.Template.Class) : "All";
            var name = itemToBeIdentified.DisplayName;
            client.SendMessage(0x08, $"{{=sWeapon{{=b:{{=c {name}\n" +
                                     $"{{=uMain hand only{{=b:{{=c {answerMainHand}\n" +
                                     $"{{=uCan Dual Wield{{=b:{{=c {answerDualWield}\n" +
                                     $"{{=uTwo-Handed{{=b:{{=c {answerTwoHander}\n" +
                                     $"{{=uDmg Min{{=b:{{=c {itemToBeIdentified.Template.DmgMin}  {{=uDmg Max{{=b:{{=c {itemToBeIdentified.Template.DmgMax}\n" +
                                     $"{{=uLevel{{=b:{{=c {itemToBeIdentified.Template.LevelRequired}  {{=uClass{{=b:{{=c {classString}\n" +
                                     "{=sStats Buff{=b:\n" +
                                     $"{{=uStr{{=b:{{=c {itemToBeIdentified.Template.StrModifer} {{=uInt{{=b:{{=c {itemToBeIdentified.Template.IntModifer} {{=uWis{{=b:{{=c {itemToBeIdentified.Template.WisModifer} {{=uCon{{=b:{{=c {itemToBeIdentified.Template.ConModifer} {{=uDex{{=b:{{=c {itemToBeIdentified.Template.DexModifer}\n" +
                                     $"{{=uHealth{{=b:{{=c {itemToBeIdentified.Template.HealthModifer} {{=uMana{{=b:{{=c {itemToBeIdentified.Template.ManaModifer}\n" +
                                     $"{{=uArmor{{=b:{{=c {itemToBeIdentified.Template.AcModifer} {{=uReflex{{=b:{{=c {itemToBeIdentified.Template.HitModifer} {{=uDamage{{=b:{{=c {itemToBeIdentified.Template.DmgModifer} {{=uRegen{{=b:{{=c {itemToBeIdentified.Template.RegenModifer}");
        }
        catch
        {
            OnFailed(aisling);
            return;
        }

        aisling.Client.Send(new ServerFormat2C(_skill.Slot, _skill.Icon, _skill.Name));
        aisling.Client.Aisling.Show(Scope.NearbyAislings, new ServerFormat29(_skill.Template.TargetAnimation, aisling.Pos));
        client.Aisling.Show(Scope.NearbyAislings, action);
    }

    public override void OnUse(Sprite sprite)
    {
        if (!_skill.CanUse()) return;
        if (sprite is not Aisling aisling) return;

        OnSuccess(aisling);
    }
}

[Script("Identify Armor")]
public class IdentifyArmor : SkillScript
{
    private readonly Skill _skill;

    public IdentifyArmor(Skill skill) : base(skill)
    {
        _skill = skill;
    }

    public override void OnFailed(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
        var client = aisling.Client;

        client.SendMessage(0x02, "Hmm, can't seem to identify that.");
    }

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
        var client = aisling.Client;

        var action = new ServerFormat1A
        {
            Serial = client.Aisling.Serial,
            Number = 0x91,
            Speed = 50
        };

        try
        {
            var itemToBeIdentified = aisling.Inventory.Items[1];

            if (itemToBeIdentified == null)
            {
                OnFailed(aisling);
                return;
            }

            if (itemToBeIdentified.Template.ScriptName is not ("Armor" or "Helmet"))
            {
                OnFailed(aisling);
                return;
            }

            var classString = itemToBeIdentified.Template.Class != Class.Peasant ? ClassStrings.ClassValue(itemToBeIdentified.Template.Class) : "All";
            var name = itemToBeIdentified.DisplayName;
            client.SendMessage(0x08, $"{{=sArmor{{=b:{{=c {name}\n" +
                                     $"{{=uLevel{{=b:{{=c {itemToBeIdentified.Template.LevelRequired}  {{=uClass{{=b:{{=c {classString}\n" +
                                     "{=sStats Buff{=b:\n" +
                                     $"{{=uStr{{=b:{{=c {itemToBeIdentified.Template.StrModifer} {{=uInt{{=b:{{=c {itemToBeIdentified.Template.IntModifer} {{=uWis{{=b:{{=c {itemToBeIdentified.Template.WisModifer} {{=uCon{{=b:{{=c {itemToBeIdentified.Template.ConModifer} {{=uDex{{=b:{{=c {itemToBeIdentified.Template.DexModifer}\n" +
                                     $"{{=uHealth{{=b:{{=c {itemToBeIdentified.Template.HealthModifer} {{=uMana{{=b:{{=c {itemToBeIdentified.Template.ManaModifer}\n" +
                                     $"{{=uArmor{{=b:{{=c {itemToBeIdentified.Template.AcModifer} {{=uReflex{{=b:{{=c {itemToBeIdentified.Template.HitModifer} {{=uDamage{{=b:{{=c {itemToBeIdentified.Template.DmgModifer} {{=uRegen{{=b:{{=c {itemToBeIdentified.Template.RegenModifer}");
        }
        catch
        {
            OnFailed(aisling);
            return;
        }

        aisling.Client.Send(new ServerFormat2C(_skill.Slot, _skill.Icon, _skill.Name));
        aisling.Client.Aisling.Show(Scope.NearbyAislings, new ServerFormat29(_skill.Template.TargetAnimation, aisling.Pos));
        client.Aisling.Show(Scope.NearbyAislings, action);
    }

    public override void OnUse(Sprite sprite)
    {
        if (!_skill.CanUse()) return;
        if (sprite is not Aisling aisling) return;

        OnSuccess(aisling);
    }
}

[Script("Inspect Item")]
public class InspectItem : SkillScript
{
    private readonly Skill _skill;

    public InspectItem(Skill skill) : base(skill)
    {
        _skill = skill;
    }

    public override void OnFailed(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
        var client = aisling.Client;

        client.SendMessage(0x02, "Hmm, can't seem to identify that.");
    }

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
        var client = aisling.Client;

        var action = new ServerFormat1A
        {
            Serial = client.Aisling.Serial,
            Number = 0x91,
            Speed = 50
        };

        try
        {
            var itemToBeIdentified = aisling.Inventory.Items[1];

            if (itemToBeIdentified == null)
            {
                OnFailed(aisling);
                return;
            }

            var classString = itemToBeIdentified.Template.Class != Class.Peasant ? ClassStrings.ClassValue(itemToBeIdentified.Template.Class) : "All";
            var canBeEnchanted = itemToBeIdentified.Template.Enchantable;
            var enchanted = canBeEnchanted switch
            {
                true => "Yes",
                false => "No"
            };

            var name = itemToBeIdentified.DisplayName;
            client.SendMessage(0x08, $"{{=sItem{{=b:{{=c {name}\n" +
                                     $"{{=uLevel{{=b:{{=c {itemToBeIdentified.Template.LevelRequired}  {{=uGender{{=b:{{=c {itemToBeIdentified.Template.Gender}\n" +
                                     $"{{=uWeight{{=b:{{=c {itemToBeIdentified.Template.CarryWeight}  {{=uWorth{{=b:{{=c {itemToBeIdentified.Template.Value}\n" +
                                     $"{{=uEnchantable{{=b:{{=c {enchanted} {{=uClass{{=b:{{=c {classString}\n" +
                                     $"{{=uDrop Rate{{=b:{{=c {itemToBeIdentified.Template.DropRate}");
        }
        catch
        {
            OnFailed(aisling);
            return;
        }

        aisling.Client.Send(new ServerFormat2C(_skill.Slot, _skill.Icon, _skill.Name));
        aisling.Client.Aisling.Show(Scope.NearbyAislings, new ServerFormat29(_skill.Template.TargetAnimation, aisling.Pos));
        client.Aisling.Show(Scope.NearbyAislings, action);
    }

    public override void OnUse(Sprite sprite)
    {
        if (!_skill.CanUse()) return;
        if (sprite is not Aisling aisling) return;

        OnSuccess(aisling);
    }
}
