using Darkages.Enums;
using Darkages.Network.Client;
using Darkages.Network.Server;
using Darkages.ScriptingBase;
using Darkages.Types;
using System.Numerics;

namespace Darkages.GameScripts.Areas.EasternWoodlands;

[Script("Monk Meditation")]
public class MonkMeditation : AreaScript
{
    public MonkMeditation(Area area) : base(area) => Area = area;
    public override void Update(TimeSpan elapsedTime) { }

    public override void OnMapEnter(WorldClient client) { }

    public override void OnMapExit(WorldClient client) { }

    public override void OnPlayerWalk(WorldClient client, Position oldLocation, Position newLocation)
    {
        var vectorMap = new Vector2(newLocation.X, newLocation.Y);
        if (client.Aisling.Pos != vectorMap) return;
        if (client.Aisling.Path != Class.Monk && client.Aisling.PastClass != Class.Monk) return;
        var monkNpc = ServerSetup.Instance.GlobalMundaneCache.FirstOrDefault(i => i.Value is { Name: "Sabonim Xi" });
        if (monkNpc.Value == null) return;
        var monkScript = monkNpc.Value.Scripts.FirstOrDefault();
        if (monkScript.Value == null) return;
        var script = monkScript.Value;

        switch (client.Aisling.QuestManager.BeltDegree)
        {
            case "White":
                if (vectorMap == new Vector2(5, 4))
                    script.OnResponse(client, 0x12, "Yellow");
                return;
            case "Yellow":
                if (vectorMap == new Vector2(13, 9))
                    script.OnResponse(client, 0x13, "Orange");
                return;
            case "Orange":
                if (vectorMap == new Vector2(4, 11))
                    script.OnResponse(client, 0x14, "Green");
                return;
            case "Green":
                if (vectorMap == new Vector2(9, 3))
                    script.OnResponse(client, 0x15, "Purple");
                return;
            case "Purple":
                if (vectorMap == new Vector2(11, 12))
                    script.OnResponse(client, 0x16, "Blue");
                return;
            case "Blue":
                if (vectorMap == new Vector2(3, 7))
                    script.OnResponse(client, 0x17, "Brown");
                return;
            case "Brown":
                if (vectorMap == new Vector2(12, 5))
                    script.OnResponse(client, 0x18, "Red");
                return;
            case "Red":
                if (vectorMap == new Vector2(7, 13))
                    script.OnResponse(client, 0x19, "Black");
                return;
            case "":
            case "Black":
                return;
        }
    }
}