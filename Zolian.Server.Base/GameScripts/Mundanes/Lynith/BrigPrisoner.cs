using Darkages.Common;
using Darkages.Network.Client;
using Darkages.Network.Server;
using Darkages.ScriptingBase;
using Darkages.Sprites.Entity;
using Darkages.Types;

namespace Darkages.GameScripts.Mundanes.Lynith;

[Script("BrigPrisoner")]
public class BrigPrisoner(WorldServer server, Mundane mundane) : MundaneScript(server, mundane)
{
    public override void OnClick(WorldClient client, uint serial)
    {
        base.OnClick(client, serial);
        TopMenu(client);
    }

    protected override void TopMenu(WorldClient client)
    {
        base.TopMenu(client);
        var options = new List<Dialog.OptionsDataItem> { new(0x01, "How does one escape?") };
        client.SendOptionsDialog(Mundane, "*sighs* How many years has it been?", options.ToArray());
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
                        new (0x02, "Got it")
                    };

                    client.SendOptionsDialog(Mundane, "I see you've ended up like me. The Brig door is locked, you will need to find a way to obtain a key. If you're like me, and weak. I suggest praying.", options.ToArray());
                    break;
                }
            case 0x02:
                client.CloseDialog();
                break;
        }
    }

    public override void OnGossip(WorldClient client, string message) { }
}