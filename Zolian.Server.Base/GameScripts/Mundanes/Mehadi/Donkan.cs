using Darkages.Common;
using Darkages.Network.Client;
using Darkages.Network.Server;
using Darkages.ScriptingBase;
using Darkages.Sprites.Entity;
using Darkages.Types;

namespace Darkages.GameScripts.Mundanes.Mehadi;

[Script("Donkan")]
public class Donkan(WorldServer server, Mundane mundane) : MundaneScript(server, mundane)
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

        if (client.Aisling.QuestManager.SwampCount == 0 && client.Aisling.ExpLevel >= 41)
        {
            options.Add(new(0x01, "Ok.."));
            client.SendOptionsDialog(Mundane, "Oh, he doesn't like you. I know! Let's make waffles!", options.ToArray());
            return;
        }

        if (client.Aisling.HasItem("Maple Syrup") && client.Aisling.QuestManager.SwampCount == 3)
        {
            options.Add(new(0x04, "Here"));
        }

        client.SendOptionsDialog(Mundane, "Hmmm, waffles!", options.ToArray());
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
                        new (0x03, "I'm on it"),
                        new (0x02, "Maybe some other time")
                    };

                    client.SendOptionsDialog(Mundane, "In West Woodlands there is a type of maple tree near the Dwarven Village. Grab some {=cMaple Syrup {=aand bring it to me!", options.ToArray());
                    break;
                }
            case 0x02:
                client.CloseDialog();
                break;
            case 0x03:
                {
                    client.Aisling.QuestManager.SwampCount++;
                    client.CloseDialog();
                    break;
                }
            case 0x04:
                {
                    var options = new List<Dialog.OptionsDataItem>
                    {
                        new (0x02, "Thank you")
                    };

                    var item = client.Aisling.HasItemReturnItem("Maple Syrup");

                    if (item != null)
                    {
                        client.Aisling.QuestManager.SwampCount++;
                        client.Aisling.Inventory.RemoveFromInventory(client, item);
                        client.GiveItem("Maple Glazed Waffles");
                        client.SendAttributes(StatUpdateType.WeightGold);
                        client.CloseDialog();
                    }
                    else
                    {
                        client.SendOptionsDialog(Mundane, "Remember, the maple syrup is in West Woodlands; Near the Dwarven Village");
                        break;
                    }

                    client.SendOptionsDialog(Mundane, "Ohhh, he's going to like you! Here are some waffles as promised!", options.ToArray());
                    break;
                }
        }
    }

    public override void OnGossip(WorldClient client, string message) { }
}