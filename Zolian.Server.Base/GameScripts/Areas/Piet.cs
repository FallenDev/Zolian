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
    public override void OnMapEnter(GameClient client) => _aisling = client.Aisling;
    public override void OnMapExit(GameClient client) => _aisling = null;

    public override void OnMapClick(GameClient client, int x, int y)
    {
        if (x == 48 && y == 22 || x == 48 && y == 21 || x == 47 && y == 21)
        {
            client.SendMessage(0x03, "Door seems to be locked.");
        }
    }

    public override void OnPlayerWalk(GameClient client, Position oldLocation, Position newLocation) { }
    public override void OnItemDropped(GameClient client, Item itemDropped, Position locationDropped) { }
    public override void OnGossip(GameClient client, string message) { }
}