using Chaos.Common.Definitions;

using Darkages.Common;
using Darkages.Network.Client;
using Darkages.Network.Server;
using Darkages.ScriptingBase;
using Darkages.Sprites;
using Darkages.Types;

namespace Darkages.GameScripts.Mundanes.Arena;

[Script("Arena Host")]
public class ArenaHost(WorldServer server, Mundane mundane) : MundaneScript(server, mundane)
{
    public override void OnClick(WorldClient client, uint serial)
    {
        client.EntryCheck = serial;
        TopMenu(client);
    }

    protected override void TopMenu(WorldClient client)
    {
        base.TopMenu(client);

        var options = new List<Dialog.OptionsDataItem>
        {
            new(0x01, "Enter North"),
            new(0x02, "Enter East"),
            new(0x03, "Enter South"),
            new(0x04, "Enter West"),
            new(0x05, "{=bExit")
        };

        client.SendOptionsDialog(Mundane, "Beyond this point, some fight for honor; others glory. Are you sure you're up for that? ", options.ToArray());
    }

    public override void OnResponse(WorldClient client, ushort responseID, string args)
    {
        if (Mundane.Serial != client.EntryCheck)
        {
            client.CloseDialog();
            return;
        }

        if (client.Aisling.Map.ID != 5232)
        {
            client.Disconnect();
            return;
        }

        switch (responseID)
        {
            case 1:
                {
                    var rand = Generator.RandNumGen100();

                    switch (rand)
                    {
                        case >= 0 and <= 24:
                            client.TransitionToMap(509, new Position(4, 4));
                            break;
                        case >= 25 and <= 49:
                            client.TransitionToMap(509, new Position(2, 6));
                            break;
                        case >= 50 and <= 74:
                            client.TransitionToMap(509, new Position(6, 2));
                            break;
                        case >= 75 and <= 100:
                            client.TransitionToMap(509, new Position(2, 2));
                            break;
                        default:
                            client.TransitionToMap(509, new Position(4, 4));
                            break;
                    }

                    client.CloseDialog();
                    client.SendAnimation(262, null, client.Aisling.Serial);
                    client.SendServerMessage(ServerMessageType.ActiveMessage, "Northern Arena");
                    break;
                }
            case 2:
                {
                    var rand = Generator.RandNumGen100();

                    switch (rand)
                    {
                        case >= 0 and <= 24:
                            client.TransitionToMap(509, new Position(51, 4));
                            break;
                        case >= 25 and <= 49:
                            client.TransitionToMap(509, new Position(49, 2));
                            break;
                        case >= 50 and <= 74:
                            client.TransitionToMap(509, new Position(53, 6));
                            break;
                        case >= 75 and <= 100:
                            client.TransitionToMap(509, new Position(53, 2));
                            break;
                        default:
                            client.TransitionToMap(509, new Position(51, 4));
                            break;
                    }

                    client.CloseDialog();
                    client.SendAnimation(262, null, client.Aisling.Serial);
                    client.SendServerMessage(ServerMessageType.ActiveMessage, "Eastern Arena");
                    break;
                }
            case 3:
                {
                    var rand = Generator.RandNumGen100();

                    switch (rand)
                    {
                        case >= 0 and <= 24:
                            client.TransitionToMap(509, new Position(51, 51));
                            break;
                        case >= 25 and <= 49:
                            client.TransitionToMap(509, new Position(49, 53));
                            break;
                        case >= 50 and <= 74:
                            client.TransitionToMap(509, new Position(53, 49));
                            break;
                        case >= 75 and <= 100:
                            client.TransitionToMap(509, new Position(53, 53));
                            break;
                        default:
                            client.TransitionToMap(509, new Position(51, 51));
                            break;
                    }

                    client.CloseDialog();
                    client.SendAnimation(262, null, client.Aisling.Serial);
                    client.SendServerMessage(ServerMessageType.ActiveMessage, "Southern Arena");
                    break;
                }
            case 4:
                {
                    var rand = Generator.RandNumGen100();

                    switch (rand)
                    {
                        case >= 0 and <= 24:
                            client.TransitionToMap(509, new Position(4, 51));
                            break;
                        case >= 25 and <= 49:
                            client.TransitionToMap(509, new Position(2, 53));
                            break;
                        case >= 50 and <= 74:
                            client.TransitionToMap(509, new Position(2, 49));
                            break;
                        case >= 75 and <= 100:
                            client.TransitionToMap(509, new Position(6, 53));
                            break;
                        default:
                            client.TransitionToMap(509, new Position(4, 51));
                            break;
                    }

                    client.CloseDialog();
                    client.SendAnimation(262, null, client.Aisling.Serial);
                    client.SendServerMessage(ServerMessageType.ActiveMessage, "Western Arena");
                    break;
                }
            case 5:
                {
                    client.CloseDialog();
                    break;
                }
        }
    }
}