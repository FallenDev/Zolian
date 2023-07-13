using Darkages.Common;
using Darkages.Network.Client;
using Darkages.Network.Server;
using Darkages.Scripting;
using Darkages.Sprites;
using Darkages.Types;

namespace Darkages.GameScripts.Mundanes.Mehadi;

[Script("Donkan")]
public class Donkan : MundaneScript
{
    public Donkan(WorldServer server, Mundane mundane) : base(server, mundane) { }

    public override void OnClick(WorldClient client, uint serial)
    {
        base.OnClick(client, serial);
        TopMenu(client);
    }

    protected override void TopMenu(WorldClient client)
    {
        base.TopMenu(client);

        var options = new List<Dialog.OptionsDataItem>();

        if (client.Aisling.QuestManager.SwampCount == 0)
        {
            options.Add(new(0x01, "Ok.."));

            client.SendOptionsDialog(Mundane, "Oh, he doesn't like you. I know! Let's make waffles!", options.ToArray());
            return;
        }

        client.SendOptionsDialog(Mundane, "Hmmm, waffles!", options.ToArray());
    }

    public override void OnResponse(WorldClient client, ushort responseID, string args)
    {
        if (!AuthenticateUser(client)) return;

        var exp = Random.Shared.Next(1000, 5000);

        switch (responseID)
        {
            case 0x01:
            {
                var options = new List<Dialog.OptionsDataItem>
                {
                    new (0x03, "Go on.."),
                    new (0x02, "Oh? Take care")
                };

                client.SendOptionsDialog(Mundane, "You think waffles will make me like you? For your information, there's a lot more to me than people think. I have layers!", options.ToArray());
                break;
            }
            case 0x02:
                client.CloseDialog();
                break;
            case 0x03:
            {

                break;
            }
        }
    }
}