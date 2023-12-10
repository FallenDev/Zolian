using Chaos.Common.Definitions;

using Darkages.Network.Client;
using Darkages.ScriptingBase;
using Darkages.Sprites;
using Darkages.Types;

using System.Collections.Concurrent;

namespace Darkages.GameScripts.Areas;

[Script("Intro Three")]
public class IntroThree : AreaScript
{
    private readonly ConcurrentDictionary<long, Aisling> _playersOnMap = new();
    public IntroThree(Area area) : base(area) => Area = area;
    public override void Update(TimeSpan elapsedTime) { }

    public override void OnMapEnter(WorldClient client)
    {
        _playersOnMap.TryAdd(client.Aisling.Serial, client.Aisling);
        client.SendServerMessage(ServerMessageType.ScrollWindow, "{=wRebirth{=a... its what's left, when ashes and the decay stops. Then can life begin anew. \n\nSee for your eyes that today is a new light. Today a new beginning, today a new spark. \n\n\n{=qWelcome to Zolian.");
        var item = new Item();
        client.SendServerMessage(ServerMessageType.ActiveMessage, "{=qA book suddenly appears in your inventory");
        item = item.Create(client.Aisling, "Zolian Guide");
        item.GiveTo(client.Aisling, false);
    }

    public override void OnMapExit(WorldClient client) => _playersOnMap.TryRemove(client.Aisling.Serial, out _);
    public override void OnMapClick(WorldClient client, int x, int y) { }
    public override void OnPlayerWalk(WorldClient client, Position oldLocation, Position newLocation) => _playersOnMap.TryAdd(client.Aisling.Serial, client.Aisling);
    public override void OnItemDropped(WorldClient client, Item itemDropped, Position locationDropped) { }
    public override void OnGossip(WorldClient client, string message) { }
}