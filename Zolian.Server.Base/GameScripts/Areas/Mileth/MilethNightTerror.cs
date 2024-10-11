using Darkages.Common;
using Darkages.Enums;
using Darkages.Network.Client;
using Darkages.ScriptingBase;
using Darkages.Sprites;
using Darkages.Types;

using System.Collections.Concurrent;

namespace Darkages.GameScripts.Areas.Mileth;

[Script("Shrouded Crypt 3")]
public class MilethNightTerror : AreaScript
{
    private readonly ConcurrentDictionary<long, Aisling> _playersOnMap = [];
    private WorldServerTimer AnimTimer { get; }
    private bool _animate;

    public MilethNightTerror(Area area) : base(area)
    {
        Area = area;
        AnimTimer = new WorldServerTimer(TimeSpan.FromMilliseconds(1 + 5000));
    }

    public override void Update(TimeSpan elapsedTime)
    {
        if (_playersOnMap.IsEmpty)
            _animate = false;

        if (_animate)
            HandleMapAnimations(elapsedTime);
    }

    public override void OnMapEnter(WorldClient client)
    {
        _playersOnMap.TryAdd(client.Aisling.Serial, client.Aisling);

        if (!_playersOnMap.IsEmpty)
            _animate = true;
    }

    public override void OnMapExit(WorldClient client)
    {
        _playersOnMap.TryRemove(client.Aisling.Serial, out _);

        if (_playersOnMap.IsEmpty)
            _animate = false;
    }

    public override void OnPlayerWalk(WorldClient client, Position oldLocation, Position newLocation)
    {
        _playersOnMap.TryAdd(client.Aisling.Serial, client.Aisling);

        if (newLocation.X == 46 && newLocation.Y == 1 && client.Aisling.QuestManager.NightTerror && !client.Aisling.QuestManager.NightTerrorSlayed)
        {
            if (client.Aisling.HasKilled("Night Terror", 1))
            {
                client.SendServerMessage(ServerMessageType.OrangeBar1, "You feel a sense of dread.");
                return;
            }

            client.TransitionToMap(3024, new Position(7, 14));
            client.SendServerMessage(ServerMessageType.OrangeBar1, "Something pulls you in.");
        }
    }

    public override void OnItemDropped(WorldClient client, Item itemDropped, Position locationDropped) { }

    private void HandleMapAnimations(TimeSpan elapsedTime)
    {
        var a = AnimTimer.Update(elapsedTime);
        if (!a) return;
        if (_playersOnMap.IsEmpty) return;

        _playersOnMap.Values.First()?.SendTargetedClientMethod(PlayerScope.NearbyAislings, client => client.SendAnimation(214, new Position(46, 1)));
    }

    public override void OnGossip(WorldClient client, string message) { }
}