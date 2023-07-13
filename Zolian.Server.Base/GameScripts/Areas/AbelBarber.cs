using Chaos.Common.Definitions;
using Darkages.Common;
using Darkages.Network.Client;
using Darkages.Scripting;
using Darkages.Sprites;
using Darkages.Types;

namespace Darkages.GameScripts.Areas;

[Script("Abel Barber Map")]
public class AbelBarber : AreaScript
{
    public AbelBarber(Area area) : base(area) => Area = area;
    public override void Update(TimeSpan elapsedTime) { }

    public override void OnMapEnter(WorldClient client)
    {
        if (client.Aisling.Map.ID == client.Aisling.LastMapId) return;
        client.Aisling.OldStyle = client.Aisling.HairStyle;
        client.Aisling.OldColor = client.Aisling.HairColor;
    }

    public override void OnMapExit(WorldClient client)
    {
        if (client.Aisling.Coloring != 0)
        {
            client.Aisling.Coloring = 0;
            client.Aisling.HairColor = client.Aisling.OldColor;
            client.UpdateDisplay();
            aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "Think you'd get it for free?");
        }

        if (client.Aisling.Styling == 0) return;
        client.Aisling.Styling = 0;
        client.Aisling.HairStyle = client.Aisling.OldStyle;
        client.UpdateDisplay();
        aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "Think you'd get it for free?");
    }

    public override void OnMapClick(WorldClient client, int x, int y) { }
    public override void OnPlayerWalk(WorldClient client, Position oldLocation, Position newLocation) { }
    public override void OnItemDropped(WorldClient client, Item itemDropped, Position locationDropped) { }

    public override void OnGossip(WorldClient client, string message)
    {
        if (message.StringContains("fag"))
        {
            client.Aisling.HairStyle = 46;
            client.Aisling.HairColor = 40;
            client.UpdateDisplay();
            aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "Not here honey!");
        }

        if (message.StringContains("gay"))
        {
            client.Aisling.HairStyle = 46;
            client.Aisling.HairColor = 40;
            client.UpdateDisplay();
            aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "Not here honey!");
        }
    }
}