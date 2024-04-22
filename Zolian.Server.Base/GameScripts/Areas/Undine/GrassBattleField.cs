using Darkages.Enums;
using Darkages.Network.Client;
using Darkages.ScriptingBase;
using Darkages.Sprites;
using Darkages.Types;

using System.Collections.Concurrent;
using System.Numerics;
using Chaos.Common.Definitions;

namespace Darkages.GameScripts.Areas.Undine;

[Script("Battlefield")]
public class Battlefield : AreaScript
{
    private readonly ConcurrentDictionary<long, Aisling> _playersOnMap = [];

    public Battlefield(Area area) : base(area) => Area = area;
    public override void Update(TimeSpan elapsedTime) { }

    public override void OnMapEnter(WorldClient client) => _playersOnMap.TryAdd(client.Aisling.Serial, client.Aisling);

    public override void OnMapExit(WorldClient client) => _playersOnMap.TryRemove(client.Aisling.Serial, out _);

    public override void OnPlayerWalk(WorldClient client, Position oldLocation, Position newLocation)
    {
        var vectorMap = new Vector2(newLocation.X, newLocation.Y);
        if (client.Aisling.Pos != vectorMap) return;
        _playersOnMap.TryAdd(client.Aisling.Serial, client.Aisling);

        if (client.Aisling.QuestManager.GivenTarnishedBreastplate) return;
        if (!client.Aisling.QuestManager.UnhappyEnding) return;
        if (vectorMap != new Vector2(27, 2)) return;
        var maleNotes = new Item();
        var femaleNotes = new Item();
        var tarnishedArmor = new Item();
        maleNotes = maleNotes.Create(client.Aisling, "Enclosed Letter E Sealed");
        maleNotes.GiveTo(client.Aisling);
        femaleNotes = femaleNotes.Create(client.Aisling, "Enclosed Letter C Sealed");
        femaleNotes.GiveTo(client.Aisling);
        tarnishedArmor = tarnishedArmor.Create(client.Aisling, "Rouel's Tarnished Armor");
        tarnishedArmor.GiveTo(client.Aisling);
        client.Aisling.QuestManager.GivenTarnishedBreastplate = true;
        client.Aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(75, new Position(vectorMap)));
        client.SendServerMessage(ServerMessageType.ActiveMessage, "Hmm, I found a few things just as Edgar said.");
    }
}