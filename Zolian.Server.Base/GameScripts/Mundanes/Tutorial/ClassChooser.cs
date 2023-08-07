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
public class ClassChooser : MundaneScript
{
    public ClassChooser(WorldServer server, Mundane mundane) : base(server, mundane) { }

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
            client.Aisling.Path = (Class)responseID;
            client.Aisling.Stage = ClassStage.Class;

            switch (client.Aisling.Path)
            {
                case Class.Berserker:
                {
                    Berzerker(client);
                    break;
                }
                case Class.Defender:
                {
                    Defender(client);
                    break;
                }
                case Class.Monk:
                {
                    Monk(client);
                    break;
                }
                case Class.Assassin:
                {
                    Assassin(client);
                    break;
                }
                case Class.Cleric:
                {
                    Cleric(client);
                    break;
                }
                case Class.Arcanus:
                {
                    Arcanus(client);
                    break;
                }
                case Class.Peasant:
                    break;
            }

            var path = ClassStrings.ClassValue(client.Aisling.Path);

            if (path != "Peasant")
            {
                ClassWrapUp(client, path);
                await Task.Delay(350).ContinueWith(ct => { client.Aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(303, client.Aisling.Serial)); });
                await Task.Delay(350).ContinueWith(ct => { client.Aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendSound(97, false)); });
                await Task.Delay(750).ContinueWith(ct => { client.Aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(303, client.Aisling.Serial)); });
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
        client.SendAttributes(StatUpdateType.Primary);
        client.UpdateDisplay();
        await Task.Delay(500).ContinueWith(ct => {
            client.SendServerMessage(ServerMessageType.OrangeBar1, "You wake from your slumber.. music begins to fill the air.");
        });
        client.TransitionToMap(137, new Position(1, 4));
    }

    private static void Berzerker(IWorldClient client)
    {
        Skill.GiveTo(client.Aisling, "Onslaught", 1);
        var item = new Item();
        item = item.Create(client.Aisling,
            client.Aisling.Gender == Gender.Female
                ? ServerSetup.Instance.GlobalItemTemplateCache["Leather Bliaut"]
                : ServerSetup.Instance.GlobalItemTemplateCache["Leather Tunic"]);
        var item1 = new Item();
        item1 = item1.Create(client.Aisling, ServerSetup.Instance.GlobalItemTemplateCache["Eppe"], Item.Quality.Common, Item.Variance.None, Item.WeaponVariance.None);
        item.GiveTo(client.Aisling);
        item1.GiveTo(client.Aisling);
    }

    private static void Defender(IWorldClient client)
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
        item.GiveTo(client.Aisling);
        item1.GiveTo(client.Aisling);
        item2.GiveTo(client.Aisling);
    }

    private static void Assassin(IWorldClient client)
    {
        Skill.GiveTo(client.Aisling, "Stab", 1);
        var item = new Item();
        item = item.Create(client.Aisling,
            client.Aisling.Gender == Gender.Female
                ? ServerSetup.Instance.GlobalItemTemplateCache["Cotte"]
                : ServerSetup.Instance.GlobalItemTemplateCache["Scout Leather"]);
        var item1 = new Item();
        item1 = item1.Create(client.Aisling, ServerSetup.Instance.GlobalItemTemplateCache["Snow Dagger"], Item.Quality.Common, Item.Variance.None, Item.WeaponVariance.None);
        item.GiveTo(client.Aisling);
        item1.GiveTo(client.Aisling);
    }

    private static void Monk(IWorldClient client)
    {
        Skill.GiveTo(client.Aisling, "Punch", 1);
        var item = new Item();
        item = item.Create(client.Aisling,
            client.Aisling.Gender == Gender.Female
                ? ServerSetup.Instance.GlobalItemTemplateCache["Bodice"]
                : ServerSetup.Instance.GlobalItemTemplateCache["Dobok"]);
        var item1 = new Item();
        item1 = item1.Create(client.Aisling, ServerSetup.Instance.GlobalItemTemplateCache["Leather Bracer"], Item.Quality.Common, Item.Variance.None, Item.WeaponVariance.None);
        item.GiveTo(client.Aisling);
        item1.GiveTo(client.Aisling);
    }

    private static void Cleric(IWorldClient client)
    {
        Spell.GiveTo(client.Aisling, "Heal Minor Wounds", 1);
        var item = new Item();
        item = item.Create(client.Aisling,
            client.Aisling.Gender == Gender.Female
                ? ServerSetup.Instance.GlobalItemTemplateCache["Gorget Gown"]
                : ServerSetup.Instance.GlobalItemTemplateCache["Cowl"]);
        var item1 = new Item();
        item1 = item1.Create(client.Aisling, ServerSetup.Instance.GlobalItemTemplateCache["Stick"], Item.Quality.Common, Item.Variance.None, Item.WeaponVariance.None);
        item.GiveTo(client.Aisling);
        item1.GiveTo(client.Aisling);
    }

    private static void Arcanus(IWorldClient client)
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
        item.GiveTo(client.Aisling);
        item1.GiveTo(client.Aisling);
    }
}