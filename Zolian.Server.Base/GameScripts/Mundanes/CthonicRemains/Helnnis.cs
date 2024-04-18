using Darkages.Common;
using Darkages.Network.Client;
using Darkages.Network.Server;
using Darkages.ScriptingBase;
using Darkages.Sprites;
using Darkages.Types;

namespace Darkages.GameScripts.Mundanes.CthonicRemains;

[Script("Advent Guild Leader")]
public class Helnnis(WorldServer server, Mundane mundane) : MundaneScript(server, mundane)
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

        if (client.Aisling.QuestManager.JoinedTheAdventurersGuild)
        {
            if (client.Aisling.QuestManager.AdventuresGuildReputation <= 3)
            {
                options.Add(new Dialog.OptionsDataItem(0x00, "Hunting Tasks"));
                options.Add(new Dialog.OptionsDataItem(0x00, "Gathering Tasks"));
            }

            if (client.Aisling.QuestManager.AdventuresGuildReputation >= 4)
            {
                options.Add(new Dialog.OptionsDataItem(0x00, "Cthonic Depths Cleansing"));
            }

            switch (client.Aisling.QuestManager.CthonicRemainsExplorationLevel)
            {
                case 0:
                    options.Add(new Dialog.OptionsDataItem(0x00, "Depths 5 Exploration"));
                    break;
                case 1:
                    options.Add(new Dialog.OptionsDataItem(0x00, "Depths 12 Exploration"));
                    break;
                case 2:
                    options.Add(new Dialog.OptionsDataItem(0x00, "Visit Forward SpecOp Camp"));
                    break;
            }

            client.SendOptionsDialog(Mundane, "Hello again! Here to accept a job?", options.ToArray());
        }

        if (!client.Aisling.QuestManager.JoinedTheAdventurersGuild)
            options.Add(new Dialog.OptionsDataItem(0x00, "I wish to join the Adventurers Guild."));

        client.SendOptionsDialog(Mundane, "Ah, an able body! Come over here! We have much to discuss.", options.ToArray());
    }

    public override void OnResponse(WorldClient client, ushort responseId, string args)
    {
        if (!AuthenticateUser(client)) return;

        switch (responseId)
        {
            case 0x00:
                client.CloseDialog();
                break;
        }
    }

    public override void OnGossip(WorldClient client, string message) { }
}