using Darkages.Network.Client;
using Darkages.ScriptingBase;
using Darkages.Sprites.Entity;
using Darkages.Types;

namespace Darkages.GameScripts.Areas.Pravat;

[Script("ChaosDarkness")]
public class ChaosDarkness : AreaScript
{
    public ChaosDarkness(Area area) : base(area) => Area = area;
    public override void Update(TimeSpan elapsedTime) { }
    
    public override void OnMapEnter(WorldClient client)
    {
        client.SendServerMessage(ServerMessageType.ActiveMessage, "The darkness of the Chaos has enveloped you...");
    }

    public override void OnMapExit(WorldClient client) { }
    public override void OnPlayerWalk(WorldClient client, Position oldLocation, Position newLocation) { }
    public override void OnItemDropped(WorldClient client, Item itemDropped, Position locationDropped) { }
    public override void OnGossip(WorldClient client, string message) { }
}