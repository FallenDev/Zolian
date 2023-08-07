using Chaos.Common.Definitions;
using Darkages.Common;
using Darkages.Network.Client;
using Darkages.ScriptingBase;
using Darkages.Sprites;
using Darkages.Types;

namespace Darkages.GameScripts.Areas;

[Script("Abel Barber Map")]
public class AbelBarber : AreaScript
{
    public AbelBarber(Area area) : base(area) => Area = area;
    public override void Update(TimeSpan elapsedTime) { }

    public override void OnMapEnter(WorldClient client) { }

    public override void OnMapExit(WorldClient client)
    {
        client.Aisling.HairColor = client.Aisling.OldColor;
        client.Aisling.HairStyle = client.Aisling.OldStyle;
        client.UpdateDisplay();
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
            client.SendServerMessage(ServerMessageType.ActiveMessage, "Not here honey!");
        }

        if (message.StringContains("gay"))
        {
            client.Aisling.HairStyle = 46;
            client.Aisling.HairColor = 40;
            client.UpdateDisplay();
            client.SendServerMessage(ServerMessageType.ActiveMessage, "Not here honey!");
        }
    }
}