using Darkages.Enums;
using Darkages.Network.Client;
using Darkages.Scripting;
using Darkages.Sprites;
using Darkages.Types;

namespace Darkages.GameScripts.Areas;

[Script("MilethInnRoom")]
public class MilethInnRoom : AreaScript
{
    public MilethInnRoom(Area area) : base(area) => Area = area;

    public override void Update(TimeSpan elapsedTime) { }
    public override void OnMapEnter(GameClient client) { }
    public override void OnMapExit(GameClient client) { }

    public override void OnMapClick(GameClient client, int x, int y)
    {
        if ((x != 1 || y != 2) && (x != 2 || y != 2) && (x != 1 || y != 3) && (x != 2 || y != 3)) return;
        client.Aisling.CurrentHp = client.Aisling.MaximumHp;
        client.Aisling.CurrentMp = client.Aisling.MaximumMp;
        client.SendStats(StatusFlags.Health);
        client.SendMessage(0x02, "You feel well rested.");
    }

    public override void OnPlayerWalk(GameClient client, Position oldLocation, Position newLocation) { }
    public override void OnItemDropped(GameClient client, Item itemDropped, Position locationDropped) { }
    public override void OnGossip(GameClient client, string message) { }
}