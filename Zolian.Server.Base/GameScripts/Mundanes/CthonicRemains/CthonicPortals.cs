using Darkages.Common;
using Darkages.Network.Client;
using Darkages.Network.Server;
using Darkages.ScriptingBase;
using Darkages.Sprites;
using Darkages.Types;

namespace Darkages.GameScripts.Mundanes.CthonicRemains;

[Script("Cthonic Portals")]
public class CthonicPortals(WorldServer server, Mundane mundane) : MundaneScript(server, mundane)
{
    public override void OnClick(WorldClient client, uint serial)
    {
        var options = new List<Dialog.OptionsDataItem>();

        if (client.Aisling.QuestManager.CthonicRemainsExplorationLevel >= 0)
        {
            options.Add(new Dialog.OptionsDataItem(0x00, "Entrance"));
            options.Add(new Dialog.OptionsDataItem(0x01, "Base Camp"));
        }

        if (client.Aisling.QuestManager.AdventuresGuildReputation >= 3)
        {
            options.Add(new Dialog.OptionsDataItem(0x02, "Advanced Camp"));
        }

        if (client.Aisling.QuestManager.CthonicRemainsExplorationLevel >= 2)
        {
            options.Add(new Dialog.OptionsDataItem(0x03, "Forward Camp"));
        }

        client.SendOptionsDialog(Mundane, "Where would you like to travel?", options.ToArray());
    }

    public override void OnResponse(WorldClient client, ushort responseId, string args)
    {
        switch (responseId)
        {
            case 0x00:
                client.TransitionToMap(5001, new Position(65, 21));
                break;
            case 0x01:
                client.TransitionToMap(5015, new Position(63, 94));
                break;
            case 0x02:
                client.TransitionToMap(5031, new Position(23, 20));
                break;
            case 0x03:
                // ToDo: Fix this portal after map is created
                client.TransitionToMap(5050, new Position(10, 10));
                break;
        }
    }
}