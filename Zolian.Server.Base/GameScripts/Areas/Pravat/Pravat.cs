using Darkages.Network.Client;
using Darkages.Network.Server;
using Darkages.ScriptingBase;
using Darkages.Sprites;
using Darkages.Types;

namespace Darkages.GameScripts.Areas.Pravat;

[Script("Pravat")]
public class Pravat : AreaScript
{
    public Pravat(Area area) : base(area) => Area = area;
    public override void Update(TimeSpan elapsedTime) { }
    public override void OnMapEnter(WorldClient client) { }
    public override void OnMapExit(WorldClient client) { }

    public override void OnMapClick(WorldClient client, int x, int y)
    {
        base.OnMapClick(client, x, y);

        var updateBoardFound = ServerSetup.Instance.GlobalBoardPostCache.TryGetValue(1, out var updateBoard);

        if (x == 24 && y == 15 || x == 25 && y == 16)
        {
            if (updateBoardFound)
                client.SendBoard(updateBoard);
        }
    }

    public override void OnPlayerWalk(WorldClient client, Position oldLocation, Position newLocation) { }
    public override void OnItemDropped(WorldClient client, Item itemDropped, Position locationDropped) { }
    public override void OnGossip(WorldClient client, string message) { }
}