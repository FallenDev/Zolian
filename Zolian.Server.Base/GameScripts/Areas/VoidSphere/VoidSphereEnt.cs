using Darkages.GameScripts.Affects;
using Darkages.Network.Client;
using Darkages.Network.Server;
using Darkages.ScriptingBase;
using Darkages.Sprites;
using Darkages.Sprites.Entity;
using Darkages.Types;
using System.Collections.Concurrent;
using System.Numerics;

namespace Darkages.GameScripts.Areas.VoidSphere;

[Script("VoidSphereEnt")]
public class VoidSphereEnt : AreaScript
{
    private readonly ConcurrentDictionary<long, Aisling> _playersOnMap = [];
    public VoidSphereEnt(Area area) : base(area) => Area = area;
    public override void Update(TimeSpan elapsedTime) { }
    public override void OnMapEnter(WorldClient client) => _playersOnMap.TryAdd(client.Aisling.Serial, client.Aisling);
    public override void OnMapExit(WorldClient client) => _playersOnMap.TryRemove(client.Aisling.Serial, out _);

    public override void OnPlayerWalk(WorldClient client, Position oldLocation, Position newLocation)
    {
        var vectorMap = new Vector2(newLocation.X, newLocation.Y);
        if (client.Aisling.Pos != vectorMap) return;
        _playersOnMap.TryAdd(client.Aisling.Serial, client.Aisling);

        switch (newLocation.X)
        {
            case 15 when newLocation.Y == 9:
            case 15 when newLocation.Y == 8:
                var npc = ServerSetup.Instance.GlobalMundaneCache.Values.First(npc => npc.Name == "Void Crystal");
                var script = npc.Scripts.Values.FirstOrDefault();
                script?.OnClick(client, npc.Serial);
                break;
        }

        if (client.Aisling.EquipmentManager.Equipment[18]?.Item.Template.Name == "Auto Spark") return;

        if (!(vectorMap.Y > 15) && !(vectorMap.Y < 3) && !(vectorMap.X > 15) && !(vectorMap.X < 3)) return;
        var debuff = new DebuffReaping();
        client.EnqueueDebuffAppliedEvent(client.Aisling, debuff);
        client.TransitionToMap(14757, new Position(13, 34));
        client.SendSound(0x9B, false);
    }

    public override void OnItemDropped(WorldClient client, Item itemDropped, Position locationDropped) { }
    public override void OnGossip(WorldClient client, string message) { }
}