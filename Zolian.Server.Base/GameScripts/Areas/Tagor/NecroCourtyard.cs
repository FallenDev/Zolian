using Darkages.Common;
using Darkages.Enums;
using Darkages.GameScripts.Affects;
using Darkages.Network.Client;
using Darkages.ScriptingBase;
using Darkages.Sprites;
using Darkages.Types;

using System.Collections.Concurrent;
using System.Numerics;

namespace Darkages.GameScripts.Areas.Tagor;

[Script("Necro Courtyard")]
public class NecroCourtyard : AreaScript
{
    private readonly ConcurrentDictionary<long, Aisling> _playersOnMap = [];
    private Debuff _debuff1;
    private Debuff _debuff2;

    public NecroCourtyard(Area area) : base(area) => Area = area;
    public override void Update(TimeSpan elapsedTime) { }

    public override void OnMapEnter(WorldClient client) => _playersOnMap.TryAdd(client.Aisling.Serial, client.Aisling);

    public override void OnMapExit(WorldClient client) => _playersOnMap.TryRemove(client.Aisling.Serial, out _);

    public override void OnPlayerWalk(WorldClient client, Position oldLocation, Position newLocation)
    {
        var vectorMap = new Vector2(newLocation.X, newLocation.Y);
        if (client.Aisling.Pos != vectorMap) return;
        _playersOnMap.TryAdd(client.Aisling.Serial, client.Aisling);

        if (ReflexCheck(client.Aisling)) return;
        _debuff1 = new DebuffArdPoison();
        _debuff2 = new DebuffDecay();

        // Poison Pool 1
        if (vectorMap == new Vector2(15, 6) ||
            vectorMap == new Vector2(16, 6) ||
            vectorMap == new Vector2(17, 6) ||
            vectorMap == new Vector2(16, 7) ||
            vectorMap == new Vector2(16, 5))
        {
            client.EnqueueDebuffAppliedEvent(client.Aisling, _debuff1);
            client.EnqueueDebuffAppliedEvent(client.Aisling, _debuff2);

            foreach (var buff in client.Aisling.Buffs.Values)
            {
                buff?.OnEnded(client.Aisling, buff);
            }

            client.Aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(75, new Position(vectorMap)));
        }

        // Poison Pool 2
        if (vectorMap != new Vector2(21, 22) &&
            vectorMap != new Vector2(22, 22) &&
            vectorMap != new Vector2(23, 22) &&
            vectorMap != new Vector2(22, 23) &&
            vectorMap != new Vector2(22, 21)) return;

        client.EnqueueDebuffAppliedEvent(client.Aisling, _debuff1);
        client.EnqueueDebuffAppliedEvent(client.Aisling, _debuff2);
        foreach (var buff in client.Aisling.Buffs.Values)
        {
            buff?.OnEnded(client.Aisling, buff);
        }

        client.Aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(75, new Position(vectorMap)));
    }

    private static bool ReflexCheck(Aisling aisling)
    {
        var check = Generator.RandNumGen100();
        return !(check > aisling.Reflex);
    }
}