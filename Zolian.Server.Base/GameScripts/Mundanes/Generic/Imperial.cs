using Darkages.Common;
using Darkages.GameScripts.Mundanes.Evermore;
using Darkages.Network.Client;
using Darkages.Network.Server;
using Darkages.ScriptingBase;
using Darkages.Sprites.Entity;

namespace Darkages.GameScripts.Mundanes.Generic;

[Script("Imperial")]
public class Imperial(WorldServer server, Mundane mundane) : MundaneScript(server, mundane)
{
    public override void OnClick(WorldClient client, uint serial)
    {
        base.OnClick(client, serial);
        TopMenu(client);
    }

    protected override void TopMenu(WorldClient client)
    {
        base.TopMenu(client);
        client.SendOptionsDialog(Mundane, $"Who goes there!? Oh hey, {client.Aisling.Username} what can I do for you?");
    }

    public override void OnResponse(WorldClient client, ushort responseId, string args) { }

    public override void OnGossip(WorldClient client, string message) { }

    public override async void OnItemDropped(WorldClient client, Item item)
    {
        if (item.Template.Name.Equals("Nightshade Venom"))
        {
            client.Aisling.Inventory.RemoveRange(client, item, 1);
            client.SendPublicMessage(Mundane.Serial, PublicMessageType.Shout, "Imperial: What is this?!");
            await Task.Delay(2000);
            EvermoreQuestHelper.AddKillMark(client);
            Mundane.Remove();
            return;
        }
    }
}