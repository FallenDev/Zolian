using Darkages.GameScripts.Affects;
using Darkages.Network.Client;
using Darkages.ScriptingBase;
using Darkages.Sprites;
using Darkages.Sprites.Entity;
using Darkages.Types;

using System.Collections.Concurrent;
using System.Numerics;

namespace Darkages.GameScripts.Areas.VoidSphere;

[Script("VoidSphereWhole")]
public class VoidSphereWhole : AreaScript
{
    public VoidSphereWhole(Area area) : base(area) => Area = area;
    public override void Update(TimeSpan elapsedTime) { }
    public override void OnMapEnter(WorldClient client) { }
    public override void OnMapExit(WorldClient client) { }

    public override void OnPlayerWalk(WorldClient client, Position oldLocation, Position newLocation)
    {
        var vectorMap = new Vector2(newLocation.X, newLocation.Y);
        if (client.Aisling.Pos != vectorMap) return;
        if (client.Aisling.EquipmentManager.Equipment[18]?.Item?.Template.Name == "Auto Spark") return;

        if (!(vectorMap.Y > 35) && !(vectorMap.Y < 3) && !(vectorMap.X > 35) && !(vectorMap.X < 3)) return;
        var debuff = new DebuffReaping();
        client.EnqueueDebuffAppliedEvent(client.Aisling, debuff);
        client.TransitionToMap(14757, new Position(13, 34));
        client.SendSound(0x9B, false);
    }

    public override void OnItemDropped(WorldClient client, Item itemDropped, Position locationDropped) { }
    public override void OnGossip(WorldClient client, string message) { }
}