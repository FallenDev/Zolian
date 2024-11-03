using Darkages.Common;
using Darkages.Enums;
using Darkages.Network.Client;
using Darkages.Network.Server;
using Darkages.ScriptingBase;
using Darkages.Sprites.Entity;
using Darkages.Types;

using Nation = Darkages.Enums.Nation;

namespace Darkages.GameScripts.Mundanes.Generic;

[Script("Inn Keeper")]
public class InnKeeper(WorldServer server, Mundane mundane) : MundaneScript(server, mundane)
{
    public override void OnClick(WorldClient client, uint serial)
    {
        base.OnClick(client, serial);
        TopMenu(client);
    }

    protected override void TopMenu(WorldClient client)
    {
        base.TopMenu(client);

        var opts = new List<Dialog.OptionsDataItem>();

        if (client.Aisling.Nation == Nation.Exile ||
            client.Aisling.Nation.PlayerNationFlagIsSet(Nation.Purgatory))
            opts.Add(new Dialog.OptionsDataItem(0x01, "Become a citizen"));

        if (client.Aisling.Nation != Nation.Exile &&
            !client.Aisling.Nation.PlayerNationFlagIsSet(Nation.Purgatory))
            opts.Add(new Dialog.OptionsDataItem(0x02, "Renounce my citizenship"));

        client.SendOptionsDialog(Mundane, "Hello! Please, make yourself at home. We have various rooms for our tired travelers.", opts.ToArray());
    }

    public override void OnResponse(WorldClient client, ushort responseID, string args)
    {
        if (!AuthenticateUser(client)) return;

        switch (responseID)
        {
            case 0x01:
                {
                    switch (Mundane.Map.ID)
                    {
                        case 413:
                            client.Aisling.Nation = Nation.Suomi;
                            break;
                        case 3909:
                            client.Aisling.Nation = Nation.Noes;
                            break;
                        case 136:
                            client.Aisling.Nation = Nation.Mileth;
                            break;
                        case 1960:
                            client.Aisling.Nation = Nation.Tagor;
                            break;
                        case 498:
                            client.Aisling.Nation = Nation.Rucesion;
                            break;
                        case 1302:
                            client.Aisling.Nation = Nation.Illuminati;
                            break;
                        case 150:
                            client.Aisling.Nation = Nation.Piet;
                            break;
                        case 169:
                            client.Aisling.Nation = Nation.Abel;
                            break;
                        case 433:
                            client.Aisling.Nation = Nation.Undine;
                            break;
                    }
                }
                break;
            case 0x02:
                client.Aisling.Nation = Nation.Exile;
                break;
        }

        client.Aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(1, c.Aisling.Position));
    }
}