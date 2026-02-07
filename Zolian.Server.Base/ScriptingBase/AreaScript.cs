using Darkages.Common;
using Darkages.Network.Client;
using Darkages.Sprites;
using Darkages.Sprites.Entity;
using Darkages.Types;

namespace Darkages.ScriptingBase;

public abstract class AreaScript(Area area)
{
    protected Area Area = area;
    public WorldServerTimer Timer { get; set; }

    public abstract void Update(TimeSpan elapsedTime);

    public virtual void OnMapEnter(WorldClient client) { }
    public virtual void OnMapExit(WorldClient client) { }

    public virtual void OnMapClick(WorldClient client, int x, int y)
    {
        //if (!client.Aisling.Map.ObjectGrid[x, y].ShouldRegisterClick || client.Aisling.Map.TileContent[x, y] != TileContent.Door) return;

        //foreach (var door in client.Aisling.Map.Doors)
        //{
        //    if (door.X != x && door.Y != y) continue;
        //    door.Closed = !door.Closed;
        //    client.Aisling.Map.ObjectGrid[x, y].LastDoorClicked = DateTime.UtcNow;
        //}

        //client.Aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendDoorsOnMap(client.Aisling.Map.Doors));
    }
    public virtual void OnPlayerWalk(WorldClient client, Position oldLocation, Position newLocation) { }
    public virtual void OnNpcWalk(Movable sprite) { }
    public virtual void OnItemDropped(WorldClient client, Item item, Position location) { }
    public virtual void OnGossip(WorldClient client, string message) { }
}