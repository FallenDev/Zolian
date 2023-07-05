using Darkages.Enums;
using Darkages.Interfaces;
using Darkages.Models;
using Darkages.Network.Client;
using Darkages.Network.Formats.Models.ServerFormats;
using Darkages.Network.Server;
using Darkages.Scripting;
using Darkages.Sprites;
using Darkages.Types;

namespace Darkages.GameScripts.Mundanes.Tutorial;

[Script("Class Chooser")]
public class ClassChooser : MundaneScript
{
    public ClassChooser(GameServer server, Mundane mundane) : base(server, mundane) { }

    public override void OnClick(GameClient client, int serial)
    {
        base.OnClick(client, serial);
        TopMenu(client);
    }

    protected override void TopMenu(IGameClient client)
    {
        base.TopMenu(client);

        if (client.Aisling.Path == Class.Peasant)
        {
            var options = new List<OptionsDataItem>
            {
                new (0x21, "I'm ready to choose a path."),
                new (0x22, "I'm not worthy of a path.")
            };
            client.SendOptionsDialog(Mundane,
                "Hmm... You look weak, are you sure you're ready? Unless you've set a path for yourself, you won't survive this world. Now is the time, you must make a choice!",
                options.ToArray());
        }
        else
        {
            client.SendOptionsDialog(Mundane, "Your path has been set. Only those who've mastered their path may look for anew.");

            Task.Delay(2000).ContinueWith(ct => { client.TransitionToMap(393, new Position(6, 4)); });
            client.SendMessage(0x02, "You don't belong here, begone!");
        }
    }

    public override async void OnResponse(GameClient client, ushort responseID, string args)
    {
        if (!AuthenticateUser(client)) return;

        if (responseID is < 0x01 or > 0x06)
        {
            switch (responseID)
            {
                case 33:
                {
                    var options = new List<OptionsDataItem>
                    {
                        new (0x01, "Berserker"),
                        new (0x02, "Defender"),
                        new (0x03, "Assassin"),
                        new (0x04, "Cleric"),
                        new (0x05, "Arcanus"),
                        new (0x06, "Monk")
                    };

                    client.SendOptionsDialog(Mundane, "Which will you be?", options.ToArray());
                    break;
                }
                case 34:
                    client.SendOptionsDialog(Mundane, "Come back when you're ready to decide.");
                    break;
            }
        }
        else
        {
            var aislingEquipped = client.Aisling.EquipmentManager.Equipment;
            client.Aisling.Path = (Class)responseID;
            client.Aisling.Stage = ClassStage.Class;

            switch (client.Aisling.Path)
            {
                case Class.Berserker:
                {
                    Berzerker(client, aislingEquipped);
                    break;
                }
                case Class.Defender:
                {
                    Defender(client, aislingEquipped);
                    break;
                }
                case Class.Monk:
                {
                    Monk(client, aislingEquipped);
                    break;
                }
                case Class.Assassin:
                {
                    Assassin(client, aislingEquipped);
                    break;
                }
                case Class.Cleric:
                {
                    Cleric(client, aislingEquipped);
                    break;
                }
                case Class.Arcanus:
                {
                    Arcanus(client, aislingEquipped);
                    break;
                }
                case Class.Peasant:
                    break;
            }

            var path = ClassStrings.ClassValue(client.Aisling.Path);

            if (path != "Peasant")
            {
                ClassWrapUp(client, path);
                await Task.Delay(350).ContinueWith(ct => { client.Aisling.Animate(303); });
                await Task.Delay(350).ContinueWith(ct => { client.SendSound(97, Scope.AislingsOnSameMap); });
                await Task.Delay(750).ContinueWith(ct => { client.Aisling.Animate(303); });
            }
            else
            {
                aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "An error occurred, try again.");
                TopMenu(client);
            }
        }
    }

    private async void ClassWrapUp(GameClient client, string path)
    {
        var legend = new Legend.LegendItem
        {
            Category = "Class",
            Time = DateTime.UtcNow,
            Color = LegendColor.Blue,
            Icon = (byte)LegendIcon.Victory,
            Value = $"Devoted to the path of {path}"
        };

        client.Aisling.LegendBook.AddLegend(legend, client);

        var legendItem = new Legend.LegendItem
        {
            Category = "Alpha Aisling",
            Time = DateTime.UtcNow,
            Color = LegendColor.Yellow,
            Icon = (byte)LegendIcon.Heart,
            Value = "Enduring Alpha Testing"
        };

        client.Aisling.LegendBook.AddLegend(legendItem, client);
        client.CloseDialog();
        client.Aisling.QuestManager.TutorialCompleted = true;
        client.Aisling.PastClass = client.Aisling.Path;
        client.LoadSkillBook();
        client.LoadSpellBook();
        client.LoadEquipment();
        client.SendStats(StatusFlags.StructA);
        client.UpdateDisplay();
        await Task.Delay(500).ContinueWith(ct => {
            client.SendMessage(0x02, "You wake from your slumber.. music begins to fill the air.");
        });
        client.TransitionToMap(137, new Position(1, 4));
    }

    private void Berzerker(IGameClient client, IDictionary<int, EquipmentSlot> aislingEquipped)
    {
        Skill.GiveTo(client.Aisling, "Onslaught", 1);

        var item = new Item();
        item = item.Create(client.Aisling,
            client.Aisling.Gender == Gender.Female
                ? ServerSetup.Instance.GlobalItemTemplateCache["Leather Bliaut"]
                : ServerSetup.Instance.GlobalItemTemplateCache["Leather Tunic"]);

        var item1 = new Item();
        item1 = item1.Create(client.Aisling, ServerSetup.Instance.GlobalItemTemplateCache["Eppe"], Item.Quality.Common, Item.Variance.None, Item.WeaponVariance.None);

        var equip = new Item
        {
            Template = item.Template,
            ItemId = item.ItemId,
            Slot = 2,
            Image = item.Template.Image,
            DisplayImage = item.Template.DisplayImage,
            Durability = item.Durability,
            Owner = item.Serial,
            ItemQuality = Item.Quality.Common,
            OriginalQuality = Item.Quality.Common,
            ItemVariance = Item.Variance.None,
            WeapVariance = Item.WeaponVariance.None,
            Enchantable = item.Template.Enchantable
        };

        var weightTest = item.Template.Weight + equip.Template.Weight + client.Aisling.CurrentWeight;

        if (weightTest > client.Aisling.MaximumWeight)
        {
            aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "You can not hold anymore.");
            return;
        }

        equip.GetDisplayName();
        equip.NoColorGetDisplayName();
        client.Aisling.EquipmentManager.Add(equip.Template.EquipmentSlot, equip);
        item1.GiveTo(client.Aisling);
    }

    private void Defender(IGameClient client, IDictionary<int, EquipmentSlot> aislingEquipped)
    {
        Skill.GiveTo(client.Aisling, "Assault", 1);

        var item = new Item();
        item = item.Create(client.Aisling, client.Aisling.Gender == Gender.Female
            ? ServerSetup.Instance.GlobalItemTemplateCache["Leather Donet"]
            : ServerSetup.Instance.GlobalItemTemplateCache["Leather Guard"]);
            
        var item1 = new Item();
        item1 = item1.Create(client.Aisling, ServerSetup.Instance.GlobalItemTemplateCache["Eppe"], Item.Quality.Common, Item.Variance.None, Item.WeaponVariance.None);
            
        var item2 = new Item();
        item2 = item2.Create(client.Aisling, ServerSetup.Instance.GlobalItemTemplateCache["Wooden Shield"], Item.Quality.Common, Item.Variance.None, Item.WeaponVariance.None);

        var equip = new Item
        {
            Template = item.Template,
            ItemId = item.ItemId,
            Slot = 2,
            Image = item.Template.Image,
            DisplayImage = item.Template.DisplayImage,
            Durability = item.Durability,
            Owner = item.Serial,
            ItemQuality = Item.Quality.Common,
            OriginalQuality = Item.Quality.Common,
            ItemVariance = Item.Variance.None,
            WeapVariance = Item.WeaponVariance.None,
            Enchantable = item.Template.Enchantable
        };

        var weightTest = item.Template.Weight + equip.Template.Weight + client.Aisling.CurrentWeight;

        if (weightTest > client.Aisling.MaximumWeight)
        {
            aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "You can not hold anymore.");
            return;
        }

        equip.GetDisplayName();
        equip.NoColorGetDisplayName();
        client.Aisling.EquipmentManager.Add(equip.Template.EquipmentSlot, equip);
        item1.GiveTo(client.Aisling);
        item2.GiveTo(client.Aisling);
    }

    private void Assassin(IGameClient client, IDictionary<int, EquipmentSlot> aislingEquipped)
    {
        Skill.GiveTo(client.Aisling, "Stab", 1);
        
        var item = new Item();
        item = item.Create(client.Aisling,
            client.Aisling.Gender == Gender.Female
                ? ServerSetup.Instance.GlobalItemTemplateCache["Cotte"]
                : ServerSetup.Instance.GlobalItemTemplateCache["Scout Leather"]);

        var item1 = new Item();
        item1 = item1.Create(client.Aisling, ServerSetup.Instance.GlobalItemTemplateCache["Snow Dagger"], Item.Quality.Common, Item.Variance.None, Item.WeaponVariance.None);

        var equip = new Item
        {
            Template = item.Template,
            ItemId = item.ItemId,
            Slot = 2,
            Image = item.Template.Image,
            DisplayImage = item.Template.DisplayImage,
            Durability = item.Durability,
            Owner = item.Serial,
            ItemQuality = Item.Quality.Common,
            OriginalQuality = Item.Quality.Common,
            ItemVariance = Item.Variance.None,
            WeapVariance = Item.WeaponVariance.None,
            Enchantable = item.Template.Enchantable
        };

        var weightTest = item.Template.Weight + equip.Template.Weight + client.Aisling.CurrentWeight;

        if (weightTest > client.Aisling.MaximumWeight)
        {
            aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "You can not hold anymore.");
            return;
        }

        equip.GetDisplayName();
        equip.NoColorGetDisplayName();
        client.Aisling.EquipmentManager.Add(equip.Template.EquipmentSlot, equip);
        item1.GiveTo(client.Aisling);
    }

    private void Monk(IGameClient client, IDictionary<int, EquipmentSlot> aislingEquipped)
    {
        Skill.GiveTo(client.Aisling, "Punch", 1);

        var item = new Item();
        item = item.Create(client.Aisling,
            client.Aisling.Gender == Gender.Female
                ? ServerSetup.Instance.GlobalItemTemplateCache["Bodice"]
                : ServerSetup.Instance.GlobalItemTemplateCache["Dobok"]);
            
        var item1 = new Item();
        item1 = item1.Create(client.Aisling, ServerSetup.Instance.GlobalItemTemplateCache["Leather Bracer"], Item.Quality.Common, Item.Variance.None, Item.WeaponVariance.None);

        var equip = new Item
        {
            Template = item.Template,
            ItemId = item.ItemId,
            Slot = 2,
            Image = item.Template.Image,
            DisplayImage = item.Template.DisplayImage,
            Durability = item.Durability,
            Owner = item.Serial,
            ItemQuality = Item.Quality.Common,
            OriginalQuality = Item.Quality.Common,
            ItemVariance = Item.Variance.None,
            WeapVariance = Item.WeaponVariance.None,
            Enchantable = item.Template.Enchantable
        };

        var weightTest = item.Template.Weight + equip.Template.Weight + client.Aisling.CurrentWeight;

        if (weightTest > client.Aisling.MaximumWeight)
        {
            aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "You can not hold anymore.");
            return;
        }

        equip.GetDisplayName();
        equip.NoColorGetDisplayName();
        client.Aisling.EquipmentManager.Add(equip.Template.EquipmentSlot, equip);
        item1.GiveTo(client.Aisling);
    }

    private void Cleric(IGameClient client, IDictionary<int, EquipmentSlot> aislingEquipped)
    {
        Spell.GiveTo(client.Aisling, "Heal Minor Wounds", 1);

        var item = new Item();
        item = item.Create(client.Aisling,
            client.Aisling.Gender == Gender.Female
                ? ServerSetup.Instance.GlobalItemTemplateCache["Gorget Gown"]
                : ServerSetup.Instance.GlobalItemTemplateCache["Cowl"]);

        var item1 = new Item();
        item1 = item1.Create(client.Aisling, ServerSetup.Instance.GlobalItemTemplateCache["Stick"], Item.Quality.Common, Item.Variance.None, Item.WeaponVariance.None);

        var equip = new Item
        {
            Template = item.Template,
            ItemId = item.ItemId,
            Slot = 2,
            Image = item.Template.Image,
            DisplayImage = item.Template.DisplayImage,
            Durability = item.Durability,
            Owner = item.Serial,
            ItemQuality = Item.Quality.Common,
            OriginalQuality = Item.Quality.Common,
            ItemVariance = Item.Variance.None,
            WeapVariance = Item.WeaponVariance.None,
            Enchantable = item.Template.Enchantable
        };

        var weightTest = item.Template.Weight + equip.Template.Weight + client.Aisling.CurrentWeight;

        if (weightTest > client.Aisling.MaximumWeight)
        {
            aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "You can not hold anymore.");
            return;
        }

        equip.GetDisplayName();
        equip.NoColorGetDisplayName();
        client.Aisling.EquipmentManager.Add(equip.Template.EquipmentSlot, equip);
        item1.GiveTo(client.Aisling);
    }

    private void Arcanus(IGameClient client, IDictionary<int, EquipmentSlot> aislingEquipped)
    {
        Spell.GiveTo(client.Aisling, "Beag Athar", 1);
        Spell.GiveTo(client.Aisling, "Beag Creag", 1);
        Spell.GiveTo(client.Aisling, "Beag Sal", 1);
        Spell.GiveTo(client.Aisling, "Beag Srad", 1);
        Spell.GiveTo(client.Aisling, "Beag Dorcha", 1);
        Spell.GiveTo(client.Aisling, "Beag Eadrom", 1);

        var item = new Item();
        item = item.Create(client.Aisling,
            client.Aisling.Gender == Gender.Female
                ? ServerSetup.Instance.GlobalItemTemplateCache["Magi Skirt"]
                : ServerSetup.Instance.GlobalItemTemplateCache["Gardcorp"]);

        var item1 = new Item();
        item1 = item1.Create(client.Aisling, ServerSetup.Instance.GlobalItemTemplateCache["Stick"], Item.Quality.Common, Item.Variance.None, Item.WeaponVariance.None);

        var equip = new Item
        {
            Template = item.Template,
            ItemId = item.ItemId,
            Slot = 2,
            Image = item.Template.Image,
            DisplayImage = item.Template.DisplayImage,
            Durability = item.Durability,
            Owner = item.Serial,
            ItemQuality = Item.Quality.Common,
            OriginalQuality = Item.Quality.Common,
            ItemVariance = Item.Variance.None,
            WeapVariance = Item.WeaponVariance.None,
            Enchantable = item.Template.Enchantable
        };

        var weightTest = item.Template.Weight + equip.Template.Weight + client.Aisling.CurrentWeight;

        if (weightTest > client.Aisling.MaximumWeight)
        {
            aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "You can not hold anymore.");
            return;
        }

        equip.GetDisplayName();
        equip.NoColorGetDisplayName();
        client.Aisling.EquipmentManager.Add(equip.Template.EquipmentSlot, equip);
        item1.GiveTo(client.Aisling);
    }
}