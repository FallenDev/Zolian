using Darkages.Enums;
using Darkages.Network.Client;
using Darkages.ScriptingBase;
using Darkages.Sprites;
using Darkages.Types;

using System.Collections.Concurrent;
using System.Numerics;

namespace Darkages.GameScripts.Areas;

[Script("Monk Meditation")]
public class MonkMeditation : AreaScript
{
    private readonly ConcurrentDictionary<long, Aisling> _playersOnMap = new();

    public MonkMeditation(Area area) : base(area) => Area = area;
    public override void Update(TimeSpan elapsedTime) { }

    public override void OnMapEnter(WorldClient client) => _playersOnMap.TryAdd(client.Aisling.Serial, client.Aisling);

    public override void OnMapExit(WorldClient client) => _playersOnMap.TryRemove(client.Aisling.Serial, out _);

    public override void OnPlayerWalk(WorldClient client, Position oldLocation, Position newLocation)
    {
        var vectorMap = new Vector2(newLocation.X, newLocation.Y);
        if (client.Aisling.Pos != vectorMap) return;
        _playersOnMap.TryAdd(client.Aisling.Serial, client.Aisling);

        if (client.Aisling.Path != Class.Monk && client.Aisling.PastClass != Class.Monk) return;
        var mundane = client.Aisling.MundanesNearby().First();
        var script = mundane?.Scripts.Values.First();
        if (script == null) return;

        switch (client.Aisling.QuestManager.BeltDegree)
        {
            case "White":
                script.OnResponse(client, 0x12, "Yellow");
                return;
            case "Yellow":
                script.OnResponse(client, 0x13, "Orange");
                return;
            case "Orange":
                script.OnResponse(client, 0x14, "Green");
                return;
            case "Green":
                script.OnResponse(client, 0x15, "Purple");
                return;
            case "Purple":
                script.OnResponse(client, 0x16, "Blue");
                return;
            case "Blue":
                script.OnResponse(client, 0x17, "Brown");
                return;
            case "Brown":
                script.OnResponse(client, 0x18, "Red");
                return;
            case "Red":
                script.OnResponse(client, 0x19, "Black");
                return;
            case "":
            case "Black":
                return;
        }
    }
}