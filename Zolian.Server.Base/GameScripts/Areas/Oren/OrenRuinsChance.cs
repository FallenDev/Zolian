using Darkages.Network.Client;
using Darkages.ScriptingBase;
using Darkages.Types;
using System.Diagnostics;
using System.Numerics;
using Darkages.Sprites.Entity;

namespace Darkages.GameScripts.Areas.Oren;

[Script("Oren Ruins Chance")]
public class OrenRuinsChance : AreaScript
{
    private Vector2 _treasure;
    private readonly Stopwatch _update = new();

    public OrenRuinsChance(Area area) : base(area) => Area = area;

    public override void Update(TimeSpan elapsedTime)
    {
        if (!_update.IsRunning)
        {
            _treasure = new Vector2(Random.Shared.Next(1, Area.Width), Random.Shared.Next(1, Area.Height));
            _update.Start();
        }

        if (_update.Elapsed.TotalSeconds < 300) return;
        _update.Restart();
        _treasure = new Vector2(Random.Shared.Next(1, Area.Width), Random.Shared.Next(1, Area.Height));
    }

    public override void OnMapEnter(WorldClient client) { }

    public override void OnMapExit(WorldClient client) { }

    public override void OnMapClick(WorldClient client, int x, int y) { }

    public override void OnPlayerWalk(WorldClient client, Position oldLocation, Position newLocation)
    {
        var vectorMap = new Vector2(newLocation.X, newLocation.Y);
        if (client.Aisling.Pos != vectorMap) return;
        if (client.Aisling.EquipmentManager.Equipment[1]?.Item?.Template.Name != "Ruins Shovel") return;
        if (client.Aisling.WithinRangeOfTile(new Position(_treasure.X, _treasure.Y), 5))
            client.Aisling.SendAnimationNearby(160, new Position(_treasure.X, _treasure.Y));
        if (_treasure != vectorMap) return;

        var shovel = client.Aisling.EquipmentManager.Equipment[1]?.Item;
        if (shovel == null) return;
        _treasure = new Vector2(Random.Shared.Next(1, Area.Width), Random.Shared.Next(1, Area.Height));
        shovel.Durability = (uint)(shovel.Durability * .50);
        client.SendServerMessage(ServerMessageType.ActiveMessage, $"Wow... buried treasure!");
        var bottle = new Item();
        bottle = bottle.Create(client.Aisling, "Medium Treasure Chest");
        bottle.GiveTo(client.Aisling);
    }
}