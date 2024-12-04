using Darkages.GameScripts.Affects;
using Darkages.Network.Client;
using Darkages.ScriptingBase;
using Darkages.Sprites.Entity;
using Darkages.Types;
using System.Numerics;

namespace Darkages.GameScripts.Areas.VoidSphere;

[Script("VoidSphereApex")]
public class VoidSphereApex : AreaScript
{
    public VoidSphereApex(Area area) : base(area) => Area = area;
    public override void Update(TimeSpan elapsedTime) { }
    public override void OnMapEnter(WorldClient client) { }
    public override void OnMapExit(WorldClient client) { }

    public override void OnPlayerWalk(WorldClient client, Position oldLocation, Position newLocation)
    {
        var vectorMap = new Vector2(newLocation.X, newLocation.Y);
        if (client.Aisling.Pos != vectorMap) return;

        if (vectorMap is { Y: 3, X: 32 or 33 or 34 })
        {
            if (client.Aisling.HasKilled("Gait Gunner", 5) && client.Aisling.ExpLevel >= 350)
            {
                if (client.Aisling.GroupId != 0 && client.Aisling.GroupParty != null)
                {
                    foreach (var player in client.Aisling.GroupParty.PartyMembers.Values.Where(player => player.Map.ID == 1504))
                    {
                        player.Client.TransitionToMap(1505, new Position(11, 21));
                    }

                    return;
                }

                client.TransitionToMap(1505, new Position(11, 21));
                return;
            }

            client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=bYou are not yet worthy to enter.");
            return;
        }

        if (client.Aisling.EquipmentManager.Equipment[18]?.Item?.Template.Name == "Auto Spark") return;

        // Off-Map Kill
        if (!(vectorMap.Y > 35) && !(vectorMap.Y < 3) && !(vectorMap.X > 35) && !(vectorMap.X < 3)) return;
        var debuff = new DebuffReaping();
        client.EnqueueDebuffAppliedEvent(client.Aisling, debuff);
        client.TransitionToMap(14757, new Position(13, 34));
        client.SendSound(0x9B, false);
    }

    public override void OnItemDropped(WorldClient client, Item itemDropped, Position locationDropped) { }
    public override void OnGossip(WorldClient client, string message) { }
}