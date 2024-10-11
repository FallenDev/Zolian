using Darkages.Network.Client;
using Darkages.ScriptingBase;
using Darkages.Sprites;
using Darkages.Types;

using System.Collections.Concurrent;

namespace Darkages.GameScripts.Areas.Mileth;

[Script("Royal Crypts")]
public class RoyalCrypts : AreaScript
{
    private readonly ConcurrentDictionary<long, Aisling> _playersOnMap = [];

    public RoyalCrypts(Area area) : base(area) => Area = area;
    public override void Update(TimeSpan elapsedTime) { }

    public override void OnMapEnter(WorldClient client) => _playersOnMap.TryAdd(client.Aisling.Serial, client.Aisling);

    public override void OnMapExit(WorldClient client) => _playersOnMap.TryRemove(client.Aisling.Serial, out _);

    public override void OnMapClick(WorldClient client, int x, int y)
    {
        base.OnMapClick(client, x, y);

        if (x == 13 && y == 0 || x == 14 && y == 0 || x == 15 && y == 1)
        {
            client.SendServerMessage(ServerMessageType.ScrollWindow, "~ Deep down in the crypt, there is a tree ~\n\n" +
                                                                    "{=bTodesbaum{=a who longs to be free\n" +
                                                                    "Waiting quietly in the {=bmourning room{=a:\n" +
                                                                    "below the floor of the last {=broyal tomb\n" +
                                                                    "{=aThis tree of death holds a 'key'\n" +
                                                                    "Not to be confused with {=qthe royal decree{=a.\n" +
                                                                    "A book, for exchange, is what he will accept\n" +
                                                                    "With bloody robes, the {=blich lord {=aknows where it is kept\n\n" +
                                                                    "Hunt this lich until what you hold is bound,\n" +
                                                                    "talk to {=bTodesbaum{=a, throw him the {=bscribblings {=ayou have found.\n" +
                                                                    "After the storm, he seems to unravel...\n" +
                                                                    "Deeper now into {=qNecropolis {=ayou can travel.");
        }
    }

    public override void OnPlayerWalk(WorldClient client, Position oldLocation, Position newLocation) => _playersOnMap.TryAdd(client.Aisling.Serial, client.Aisling);
}