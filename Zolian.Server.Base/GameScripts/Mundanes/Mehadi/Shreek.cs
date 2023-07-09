using Darkages.Interfaces;
using Darkages.Network.Client;
using Darkages.Network.Formats.Models.ServerFormats;
using Darkages.Network.Server;
using Darkages.Scripting;
using Darkages.Sprites;

namespace Darkages.GameScripts.Mundanes.Mehadi;

[Script("Shreek")]
public class Shreek : MundaneScript
{
    public Shreek(WorldServer server, Mundane mundane) : base(server, mundane) { }

    public override void OnClick(WorldClient client, int serial)
    {
        base.OnClick(client, serial);
        TopMenu(client);
    }

    protected override void TopMenu(WorldClient client)
    {
        base.TopMenu(client);

        var options = new List<OptionsDataItem>();

        if (client.Aisling.HasItem("Maple Syrup"))
        {
            options.Add(new(0x01, "Here"));
        }

        client.SendOptionsDialog(Mundane, "*grumbles* Still in my swamp eh?", options.ToArray());
    }

    public override void OnResponse(WorldClient client, ushort responseID, string args)
    {
        if (!AuthenticateUser(client)) return;

        var exp = Random.Shared.Next(1000, 5000);

        switch (responseID)
        {
            case 0x01:
            {
                var options = new List<OptionsDataItem>
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