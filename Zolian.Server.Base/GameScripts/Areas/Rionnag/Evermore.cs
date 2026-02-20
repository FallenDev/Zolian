using Darkages.Network.Client;
using Darkages.ScriptingBase;
using Darkages.Sprites.Entity;
using Darkages.Types;

using System.Numerics;

namespace Darkages.GameScripts.Areas.Rionnag;

[Script("Evermore")]
public class Evermore : AreaScript
{
    public Evermore(Area area) : base(area) => Area = area;
    public override void Update(TimeSpan elapsedTime) { }
    public override void OnMapEnter(WorldClient client) { }
    public override void OnMapExit(WorldClient client) { }

    public override void OnPlayerWalk(WorldClient client, Position oldLocation, Position newLocation)
    {
        var vectorMap = new Vector2(newLocation.X, newLocation.Y);
        if (client.Aisling.Pos != vectorMap) return;
        if (client.Aisling.Pos is { X: 24, Y: 13 })
        {
            if (client.Aisling.QuestManager.AssassinsGuildReputation < 4)
            {
                client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=aAssassin: {{=bYou are not permitted to enter");
                client.WarpToAndRefresh(new Position(16, 17));
                Task.Delay(300).Wait();
                client.Aisling.SendAnimationNearby(198, new Position(16, 17));
                return;
            }

            client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=bYou've placed your hand on the bloodied writing.");
            client.TransitionToMap(289, new Position(55, 21));
            Task.Delay(2500).Wait();
            client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=aAssassin: {{=bWelcome Friend... You have earned our favor.");
        }

        if (client.Aisling.Pos is { X: 8, Y: 17} or { X: 8, Y:16} or { X:9, Y:16} or { X: 9, Y:17})
        {
            client.WarpToAndRefresh(new Position(23, 17));
            client.Aisling.SendAnimationNearby(198, new Position(23, 17));
        }
    }

    public override void OnItemDropped(WorldClient client, Item itemDropped, Position locationDropped) { }
    public override void OnGossip(WorldClient client, string message) { }
}