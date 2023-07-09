using Darkages.Network.Client;
using Darkages.Scripting;
using Darkages.Sprites;
using Darkages.Types;

namespace Darkages.GameScripts.Areas;

[Script("Piet")]
public class Piet : AreaScript
{
    private Sprite _aisling;

    public Piet(Area area) : base(area) => Area = area;
    public override void Update(TimeSpan elapsedTime) { }
    public override void OnMapEnter(WorldClient client) => _aisling = client.Aisling;
    public override void OnMapExit(WorldClient client) => _aisling = null;

    public override void OnMapClick(WorldClient client, int x, int y)
    {
        if (x == 48 && y == 22 || x == 48 && y == 21 || x == 47 && y == 21)
        {
            aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "Door seems to be locked.");
        }
    }

    public override void OnPlayerWalk(WorldClient client, Position oldLocation, Position newLocation) { }
    public override void OnItemDropped(WorldClient client, Item itemDropped, Position locationDropped) { }
    public override void OnGossip(WorldClient client, string message) { }
}