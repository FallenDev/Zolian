using Chaos.Common.Definitions;

using Darkages.Enums;
using Darkages.Network.Client;
using Darkages.Network.Server;
using Darkages.ScriptingBase;
using Darkages.Sprites;

namespace Darkages.GameScripts.Mundanes.Generic;

[Script("Beggar")]
public class Beggar(WorldServer server, Mundane mundane) : MundaneScript(server, mundane)
{
    public override void OnClick(WorldClient client, uint serial) { }
    public override void OnResponse(WorldClient client, ushort responseID, string args) { }

    public override void OnItemDropped(WorldClient client, Item item)
    {
        client.Aisling.SendTargetedClientMethod(Darkages.Enums.PlayerScope.NearbyAislings, c => c.SendPublicMessage(Mundane.Serial, PublicMessageType.Normal, "Beggar: I'll make good use of it, that I will"));
        client.Aisling.Inventory.RemoveFromInventory(client, item);
    }

    public override void OnGoldDropped(WorldClient client, uint gold)
    {
        if (client.Aisling.GoldPoints >= gold)
        {
            if (long.MaxValue - client.Aisling.ExpTotal < gold)
            {
                client.SendServerMessage(ServerMessageType.ActiveMessage, "Your experience box is full, ascend to carry more");
                return;
            }

            if (uint.MaxValue - client.Aisling.ExpTotal < gold)
            {
                client.SendServerMessage(ServerMessageType.ActiveMessage, $"{Mundane.Name}: Woah, are you trying to make me rich?");
                return;
            }

            client.Aisling.GoldPoints -= gold;
            client.GiveExp(gold);
            client.SendAttributes(StatUpdateType.ExpGold);
            client.SendServerMessage(ServerMessageType.ActiveMessage, $"Gained {gold} experience!");
            client.Aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendPublicMessage(Mundane.Serial, PublicMessageType.Normal, "Beggar: Blessed the stars!!!"));
            return;
        }

        client.Aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendPublicMessage(Mundane.Serial, PublicMessageType.Normal, "Beggar: Ya makin a foll of meh?"));
    }
}