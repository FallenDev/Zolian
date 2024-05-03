using Chaos.Common.Definitions;

using Darkages.Common;
using Darkages.Enums;
using Darkages.Network.Client;
using Darkages.Network.Client.Abstractions;
using Darkages.Network.Server;
using Darkages.ScriptingBase;
using Darkages.Sprites;
using Darkages.Types;

using Gender = Darkages.Enums.Gender;

namespace Darkages.GameScripts.Mundanes.Tutorial;

[Script("Class Chooser")]
public class ClassChooser(WorldServer server, Mundane mundane) : MundaneScript(server, mundane)
{
    public override void OnClick(WorldClient client, uint serial)
    {
        base.OnClick(client, serial);
        TopMenu(client);
    }

    protected override void TopMenu(WorldClient client)
    {
        base.TopMenu(client);

        if (client.Aisling.Path == Class.Peasant)
        {
            var options = new List<Dialog.OptionsDataItem>
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
            client.SendServerMessage(ServerMessageType.OrangeBar1, "You don't belong here, begone!");
        }
    }

    public override async void OnResponse(WorldClient client, ushort responseID, string args)
    {
        if (!AuthenticateUser(client)) return;

        if (responseID is < 0x01 or > 0x06)
        {
            switch (responseID)
            {
                case 33:
                    {
                        var options = new List<Dialog.OptionsDataItem>
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
            var pathEnum = (BaseClass)responseID;
            client.Aisling.Stage = ClassStage.Class;

            switch (pathEnum)
            {
                case BaseClass.Berserker:
                    {
                        client.Aisling.Path = Class.Berserker;
                        Berzerker(client);
                        break;
                    }
                case BaseClass.Defender:
                    {
                        client.Aisling.Path = Class.Defender;
                        Defender(client);
                        break;
                    }
                case BaseClass.Monk:
                    {
                        client.Aisling.Path = Class.Monk;
                        Monk(client);
                        break;
                    }
                case BaseClass.Assassin:
                    {
                        client.Aisling.Path = Class.Assassin;
                        Assassin(client);
                        break;
                    }
                case BaseClass.Cleric:
                    {
                        client.Aisling.Path = Class.Cleric;
                        Cleric(client);
                        break;
                    }
                case BaseClass.Arcanus:
                    {
                        client.Aisling.Path = Class.Arcanus;
                        Arcanus(client);
                        break;
                    }
            }

            var path = ClassStrings.ClassValue(client.Aisling.Path);

            if (path != "Peasant")
            {
                ClassWrapUp(client, path);
                await Task.Delay(350).ContinueWith(ct => { client.Aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(303, null, client.Aisling.Serial)); });
                await Task.Delay(350).ContinueWith(ct => { client.Aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendSound(97, false)); });
                await Task.Delay(750).ContinueWith(ct => { client.Aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(303, null, client.Aisling.Serial)); });
            }
            else
            {
                client.SendServerMessage(ServerMessageType.ActiveMessage, "An error occurred, try again.");
                TopMenu(client);
            }
        }
    }

    private async void ClassWrapUp(WorldClient client, string path)
    {
        var legend = new Legend.LegendItem
        {
            Key = "StartingClass",
            IsPublic = true,
            Time = DateTime.UtcNow,
            Color = LegendColor.BlueG1,
            Icon = (byte)LegendIcon.Victory,
            Text = $"Devoted to the path of {path}"
        };

        client.Aisling.LegendBook.AddLegend(legend, client);

        var legendItem = new Legend.LegendItem
        {
            Key = "Beta Aisling",
            IsPublic = true,
            Time = DateTime.UtcNow,
            Color = LegendColor.TurquoiseG7,
            Icon = (byte)LegendIcon.Heart,
            Text = "Beta Tester"
        };

        client.Aisling.LegendBook.AddLegend(legendItem, client);
        client.CloseDialog();
        client.Aisling.QuestManager.TutorialCompleted = true;
        client.Aisling.PastClass = client.Aisling.Path;
        client.LoadSkillBook();
        client.LoadSpellBook();

        await Task.Delay(500).ContinueWith(ct =>
        {
            client.SendServerMessage(ServerMessageType.OrangeBar1, "You wake from your slumber.. music begins to fill the air.");
        });

        client.TransitionToMap(137, new Position(1, 4));
    }

    private static void Berzerker(IWorldClient client)
    {
        Skill.GiveTo(client.Aisling, "Onslaught");
        var item = new Item();
        item = item.Create(client.Aisling,
            client.Aisling.Gender == Gender.Female
                ? ServerSetup.Instance.GlobalItemTemplateCache["Leather Bliaut"]
                : ServerSetup.Instance.GlobalItemTemplateCache["Leather Tunic"]);
        item.GetDisplayName();
        item.NoColorGetDisplayName();
        client.Aisling.EquipmentManager.Add(item.Template.EquipmentSlot, item);

        var item1 = new Item();
        item1 = item1.Create(client.Aisling, ServerSetup.Instance.GlobalItemTemplateCache["Eppe"], Item.Quality.Common, Item.Variance.None, Item.WeaponVariance.None);
        item1.GiveTo(client.Aisling);
    }

    private static void Defender(IWorldClient client)
    {
        Skill.GiveTo(client.Aisling, "Assault");
        var item = new Item();
        item = item.Create(client.Aisling, client.Aisling.Gender == Gender.Female
            ? ServerSetup.Instance.GlobalItemTemplateCache["Leather Donet"]
            : ServerSetup.Instance.GlobalItemTemplateCache["Leather Guard"]);
        item.GetDisplayName();
        item.NoColorGetDisplayName();
        client.Aisling.EquipmentManager.Add(item.Template.EquipmentSlot, item);

        var item1 = new Item();
        item1 = item1.Create(client.Aisling, ServerSetup.Instance.GlobalItemTemplateCache["Eppe"], Item.Quality.Common, Item.Variance.None, Item.WeaponVariance.None);
        var item2 = new Item();
        item2 = item2.Create(client.Aisling, ServerSetup.Instance.GlobalItemTemplateCache["Wooden Shield"], Item.Quality.Common, Item.Variance.None, Item.WeaponVariance.None);
        item1.GiveTo(client.Aisling);
        item2.GiveTo(client.Aisling);
    }

    private static void Assassin(IWorldClient client)
    {
        Skill.GiveTo(client.Aisling, "Stab");
        var item = new Item();
        item = item.Create(client.Aisling,
            client.Aisling.Gender == Gender.Female
                ? ServerSetup.Instance.GlobalItemTemplateCache["Cotte"]
                : ServerSetup.Instance.GlobalItemTemplateCache["Scout Leather"]);
        item.GetDisplayName();
        item.NoColorGetDisplayName();
        client.Aisling.EquipmentManager.Add(item.Template.EquipmentSlot, item);

        var item1 = new Item();
        item1 = item1.Create(client.Aisling, ServerSetup.Instance.GlobalItemTemplateCache["Snow Dagger"], Item.Quality.Common, Item.Variance.None, Item.WeaponVariance.None);
        item1.GiveTo(client.Aisling);
    }

    private static void Monk(IWorldClient client)
    {
        Skill.GiveTo(client.Aisling, "Punch");
        var item = new Item();
        item = item.Create(client.Aisling,
            client.Aisling.Gender == Gender.Female
                ? ServerSetup.Instance.GlobalItemTemplateCache["Bodice"]
                : ServerSetup.Instance.GlobalItemTemplateCache["Dobok"]);
        item.GetDisplayName();
        item.NoColorGetDisplayName();
        client.Aisling.EquipmentManager.Add(item.Template.EquipmentSlot, item);

        var item1 = new Item();
        item1 = item1.Create(client.Aisling, ServerSetup.Instance.GlobalItemTemplateCache["Leather Bracer"], Item.Quality.Common, Item.Variance.None, Item.WeaponVariance.None);
        item1.GiveTo(client.Aisling);
    }

    private static void Cleric(IWorldClient client)
    {
        Spell.GiveTo(client.Aisling, "Heal Minor Wounds");
        var item = new Item();
        item = item.Create(client.Aisling,
            client.Aisling.Gender == Gender.Female
                ? ServerSetup.Instance.GlobalItemTemplateCache["Gorget Gown"]
                : ServerSetup.Instance.GlobalItemTemplateCache["Cowl"]);
        item.GetDisplayName();
        item.NoColorGetDisplayName();
        client.Aisling.EquipmentManager.Add(item.Template.EquipmentSlot, item);

        var item1 = new Item();
        item1 = item1.Create(client.Aisling, ServerSetup.Instance.GlobalItemTemplateCache["Stick"], Item.Quality.Common, Item.Variance.None, Item.WeaponVariance.None);
        item1.GiveTo(client.Aisling);
    }

    private static void Arcanus(IWorldClient client)
    {
        Spell.GiveTo(client.Aisling, "Beag Athar");
        Spell.GiveTo(client.Aisling, "Beag Creag");
        Spell.GiveTo(client.Aisling, "Beag Sal");
        Spell.GiveTo(client.Aisling, "Beag Srad");
        Spell.GiveTo(client.Aisling, "Beag Dorcha");
        Spell.GiveTo(client.Aisling, "Beag Eadrom");
        var item = new Item();
        item = item.Create(client.Aisling,
            client.Aisling.Gender == Gender.Female
                ? ServerSetup.Instance.GlobalItemTemplateCache["Magi Skirt"]
                : ServerSetup.Instance.GlobalItemTemplateCache["Gardcorp"]);
        item.GetDisplayName();
        item.NoColorGetDisplayName();
        client.Aisling.EquipmentManager.Add(item.Template.EquipmentSlot, item);

        var item1 = new Item();
        item1 = item1.Create(client.Aisling, ServerSetup.Instance.GlobalItemTemplateCache["Stick"], Item.Quality.Common, Item.Variance.None, Item.WeaponVariance.None);
        item1.GiveTo(client.Aisling);
    }

    public override void OnGossip(WorldClient client, string message) { }
}