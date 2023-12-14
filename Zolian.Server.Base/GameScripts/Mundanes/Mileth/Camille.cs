using Chaos.Common.Definitions;

using Darkages.Common;
using Darkages.Enums;
using Darkages.Network.Client;
using Darkages.Network.Server;
using Darkages.ScriptingBase;
using Darkages.Sprites;
using Darkages.Types;

namespace Darkages.GameScripts.Mundanes.Mileth;

[Script("Camille's Greetings")]
public class Camille(WorldServer server, Mundane mundane) : MundaneScript(server, mundane)
{
    public override void OnClick(WorldClient client, uint serial)
    {
        base.OnClick(client, serial);
        TopMenu(client);
    }

    protected override void TopMenu(WorldClient client)
    {
        base.TopMenu(client);

        if (client.Aisling.Path != Class.Peasant)
        {
            var options = new List<Dialog.OptionsDataItem>();

            if (client.Aisling.QuestManager.PeteComplete)
                options.Add(new Dialog.OptionsDataItem(0x02, "{=qI'll take a pint."));

            if (client.Aisling.Level <= 50 && !client.Aisling.QuestManager.CamilleGreetingComplete)
            {
                options.Add(new Dialog.OptionsDataItem(0x04, "Good Morning, Camille"));
            }

            if (!client.Aisling.HasItem("Zolian Guide"))
            {
                options.Add(new Dialog.OptionsDataItem(0x06, "Lost my Guide"));
            }

            options.Add(new Dialog.OptionsDataItem(0x03, "Take care"));

            client.SendOptionsDialog(Mundane, "Welcome, we have warm beds and plenty of mead. Please sit down and relax.", options.ToArray());
        }
        else if (client.Aisling.Path == Class.Peasant)
        {
            var options = new List<Dialog.OptionsDataItem>
            {
                new (0x06, "How do I get back to the tutorial?"),
                new (0x03, "Nothing now.")
            };

            client.SendOptionsDialog(Mundane, "Ah, you don't look like you belong here, can I help you?", options.ToArray());
        }
    }

    public override void OnResponse(WorldClient client, ushort responseID, string args)
    {
        if (!AuthenticateUser(client)) return;

        if (responseID is > 0x01 and <= 0x06)
        {
            switch (responseID)
            {
                case 2:
                    {
                        if (client.Aisling.QuestManager.PeteComplete)
                        {
                            var item = new Item();
                            item = item.Create(client.Aisling, ServerSetup.Instance.GlobalItemTemplateCache["Mead"]);
                            var given = item.GiveTo(client.Aisling);
                            if (!given)
                            {
                                client.Aisling.BankManager.Items.TryAdd(item.ItemId, item);
                                client.SendServerMessage(ServerMessageType.ActiveMessage, "Issue with giving you the item directly, deposited to bank");
                            }
                            client.CloseDialog();
                        }
                        else
                        {
                            client.SendOptionsDialog(Mundane, "I'm embarrassed to say this, but it's spoiled?");
                        }
                        break;
                    }
                case 3:
                    client.CloseDialog();
                    break;
                case 4:
                    {
                        var options = new List<Dialog.OptionsDataItem>
                    {
                        new (0x02, "How about some of that mead?"),
                        new (0x05, "Yes, I enjoyed my stay.")
                    };

                        client.SendOptionsDialog(Mundane, "How was the room? Was the bed comfortable?", options.ToArray());
                        break;
                    }
                case 5:
                    client.Aisling.QuestManager.MilethReputation += 1;
                    client.Aisling.QuestManager.CamilleGreetingComplete = true;
                    client.SendOptionsDialog(Mundane, "We're happy to hear!");
                    client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=cYou feel refreshed.");
                    var legend = new Legend.LegendItem
                    {
                        Category = "LCamille1",
                        Time = DateTime.UtcNow,
                        Color = LegendColor.Invisible,
                        Icon = (byte)LegendIcon.Invisible,
                        Value = ""
                    };

                    client.Aisling.LegendBook.AddLegend(legend, client);
                    break;
                case 6:
                    var item2 = new Item();
                    item2 = item2.Create(client.Aisling, ServerSetup.Instance.GlobalItemTemplateCache["Zolian Guide"]);
                    var given2 = item2.GiveTo(client.Aisling);
                    if (!given2)
                    {
                        client.Aisling.BankManager.Items.TryAdd(item2.ItemId, item2);
                        client.SendServerMessage(ServerMessageType.ActiveMessage, "Issue with giving you the item directly, deposited to bank");
                    }

                    var guide = new List<Dialog.OptionsDataItem>
                    {
                        new (0x03, "Thank you")
                    };

                    client.SendOptionsDialog(Mundane, "Hmm, I know I had one somewhere around here. !? Here it is.", guide.ToArray());
                    break;
            }
        }
        else
        {
            client.TransitionToMap(3029, new Position(29, 25));
        }
    }
}