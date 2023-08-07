using Chaos.Common.Definitions;
using Darkages.Network.Client;
using Darkages.ScriptingBase;
using Darkages.Sprites;
using Darkages.Types;

namespace Darkages.GameScripts.Areas;

[Script("MilethInnRoom")]
public class MilethInnRoom : AreaScript
{
    public MilethInnRoom(Area area) : base(area) => Area = area;

    public override void Update(TimeSpan elapsedTime) { }
    public override void OnMapEnter(WorldClient client) { }
    public override void OnMapExit(WorldClient client) { }

    public override void OnMapClick(WorldClient client, int x, int y)
    {
        if ((x != 1 || y != 2) && (x != 2 || y != 2) && (x != 1 || y != 3) && (x != 2 || y != 3)) return;
        client.Aisling.CurrentHp = client.Aisling.MaximumHp;
        client.Aisling.CurrentMp = client.Aisling.MaximumMp;
        client.SendAttributes(StatUpdateType.FullVitality);
        client.SendServerMessage(ServerMessageType.OrangeBar1, "You feel well rested.");
    }

    public override void OnPlayerWalk(WorldClient client, Position oldLocation, Position newLocation) { }
    public override void OnItemDropped(WorldClient client, Item itemDropped, Position locationDropped) { }
    public override void OnGossip(WorldClient client, string message) { }
}