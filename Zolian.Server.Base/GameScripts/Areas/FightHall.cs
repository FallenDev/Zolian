using Darkages.Network.Client;
using Darkages.Scripting;
using Darkages.Sprites;
using Darkages.Types;

namespace Darkages.GameScripts.Areas;

[Script("Fight Hall")]
public class FightHall : AreaScript
{
    private Sprite _aisling;

    public FightHall(Area area) : base(area)
    {
        Area = area;
    }

    public override void Update(TimeSpan elapsedTime)
    {
        if (_aisling == null) return;
        if (_aisling.Map.ID != 195) return;
        if (!_aisling.Client.Aisling.IsDead()) return;
        _aisling.Client.GhostFormToAisling();
        _aisling.aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "Be careful out there.");
    }

    public override void OnMapEnter(GameClient client)
    {
        _aisling = client.Aisling;
        if (_aisling == null) return;
        if (_aisling.Map.ID != 195) return;
        if (!_aisling.Client.Aisling.IsDead()) return;
        _aisling.Client.GhostFormToAisling();
        _aisling.aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "Be careful out there.");
    }

    public override void OnMapExit(GameClient client) { }
    public override void OnMapClick(GameClient client, int x, int y) { }
    public override void OnPlayerWalk(GameClient client, Position oldLocation, Position newLocation) { }
    public override void OnItemDropped(GameClient client, Item itemDropped, Position locationDropped) { }
    public override void OnGossip(GameClient client, string message) { }
}