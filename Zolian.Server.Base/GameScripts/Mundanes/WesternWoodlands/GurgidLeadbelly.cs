using Chaos.Common.Definitions;

using Darkages.Common;
using Darkages.Network.Client;
using Darkages.Network.Server;
using Darkages.ScriptingBase;
using Darkages.Sprites;
using Darkages.Types;

namespace Darkages.GameScripts.Mundanes.WesternWoodlands;

[Script("Gurgid")]
public class GurgidLeadbelly(WorldServer server, Mundane mundane) : MundaneScript(server, mundane)
{
    public override void OnClick(WorldClient client, uint serial)
    {
        base.OnClick(client, serial);
        TopMenu(client);
    }

    protected override void TopMenu(WorldClient client)
    {
        base.TopMenu(client);

        var options = new List<Dialog.OptionsDataItem>();

        if (client.Aisling.QuestManager.SwampCount == 1)
        {
            options.Add(new(0x01, "Maple Tree?"));
            client.SendOptionsDialog(Mundane, $"pfft! {client.Aisling.Race}, what do ye want?!", options.ToArray());
            return;
        }

        if (client.Aisling.QuestManager.SwampCount == 2)
        {
            options.Add(new(0x04, "Your mead"));
            client.SendOptionsDialog(Mundane, "Brought me mead?", options.ToArray());
            return;
        }

        client.SendOptionsDialog(Mundane, "I need a wife.. goblins killed the last one", options.ToArray());
    }

    public override void OnResponse(WorldClient client, ushort responseID, string args)
    {
        if (!AuthenticateUser(client)) return;

        switch (responseID)
        {
            case 0x01:
                {
                    var options = new List<Dialog.OptionsDataItem>
                    {
                        new (0x03, "Sure, I'll be back"),
                        new (0x02, "Mead?")
                    };

                    client.SendOptionsDialog(Mundane, "So yer here for my trees eh? I'll tell yeh what! Bring me a wee bit of mead, and we'll have a deal", options.ToArray());
                    break;
                }
            case 0x02:
                client.SendOptionsDialog(Mundane, "No mead! No Syrup!");
                break;
            case 0x03:
                {
                    client.Aisling.QuestManager.SwampCount++;
                    client.SendOptionsDialog(Mundane, "Off with ya!");
                    break;
                }
            case 0x04:
                {
                    if (client.Aisling.HasInInventory("Mead", 7, out _))
                    {
                        client.Aisling.QuestManager.SwampCount++;
                        client.TakeAwayQuantity(client.Aisling, "Mead", 7);
                        client.GiveItem("Maple Syrup");
                        client.SendAttributes(StatUpdateType.WeightGold);
                        client.CloseDialog();
                    }
                    else
                    {
                        client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=bGurgid needs {{=q7 {{=bmeads");
                        client.SendOptionsDialog(Mundane, "No mead! No Syrup!");
                        break;
                    }

                    client.SendOptionsDialog(Mundane, $"So {client.Aisling.Race}s can do something, here is your syrup, now off with ya!");
                    break;
                }
        }
    }

    public override void OnGossip(WorldClient client, string message) { }
}