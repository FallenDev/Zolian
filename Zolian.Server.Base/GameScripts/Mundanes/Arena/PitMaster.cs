using Chaos.Common.Definitions;

using Darkages.Common;
using Darkages.Enums;
using Darkages.Network.Client;
using Darkages.Network.Server;
using Darkages.ScriptingBase;
using Darkages.Sprites;
using Darkages.Types;

namespace Darkages.GameScripts.Mundanes.Arena;

[Script("Pit Master")]
public class PitMaster(WorldServer server, Mundane mundane) : MundaneScript(server, mundane)
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

        if (client.Aisling.IsDead())
            options.Add(new Dialog.OptionsDataItem(0x30, "{=qPlease revive me"));

        client.SendOptionsDialog(Mundane, "This is a place to settle things.", options.ToArray());
    }

    public override void OnResponse(WorldClient client, ushort responseID, string args)
    {
        if (!AuthenticateUser(client)) return;

        switch (responseID)
        {
            case 0x30:
                {
                    if (client.Aisling.IsDead())
                    {
                        foreach (var player in client.Aisling.AislingsNearby())
                        {
                            player?.Client.SendServerMessage(ServerMessageType.OrangeBar5, $"{client.Aisling.Username} revived.");
                        }

                        client.Recover();
                        client.TransitionToMap(3100, new Position(9, 19));
                        client.CloseDialog();
                        Task.Delay(350).ContinueWith(ct =>
                        {
                            client.Aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(1, null, client.Aisling.Serial));
                        });
                    }
                    break;
                }
        }
    }

    public override void OnGossip(WorldClient client, string message) { }
}