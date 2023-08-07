using Darkages.Common;
using Darkages.Network.Client;
using Darkages.Network.Server;
using Darkages.ScriptingBase;
using Darkages.Sprites;
using Darkages.Types;

namespace Darkages.GameScripts.Mundanes.Mileth;

[Script("Fiona")]
public class Fiona : MundaneScript
{
    public Fiona(WorldServer server, Mundane mundane) : base(server, mundane) { }

    public override void OnClick(WorldClient client, uint serial)
    {
        base.OnClick(client, serial);
        TopMenu(client);
    }

    protected override void TopMenu(WorldClient client)
    {
        base.TopMenu(client);

        var options = new List<Dialog.OptionsDataItem>();

        if (!client.Aisling.QuestManager.FionaDance)
        {
            options.Add(new (0x01, "Why not."));
        }

        options.Add(new (0x02, "Huh?"));

        client.SendOptionsDialog(Mundane, "Wanna Dance?", options.ToArray());
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
                    new (0x03, "*smile warmly*"),
                };

                client.SendOptionsDialog(Mundane, $"You know, you're not too bad.. Thank you.", options.ToArray());
            }
                break;
            case 0x02:
            {
                client.SendOptionsDialog(Mundane, "Too bad.");
            }
                break;
            case 0x03:
            {
                client.Aisling.QuestManager.FionaDance = true;
                client.Aisling.QuestManager.MilethReputation += 1;
                client.CloseDialog();
            }
                break;
        }
    }
}