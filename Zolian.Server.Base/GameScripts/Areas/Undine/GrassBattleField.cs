using Darkages.Network.Client;
using Darkages.ScriptingBase;
using Darkages.Types;
using System.Numerics;
using Darkages.Sprites.Entity;

namespace Darkages.GameScripts.Areas.Undine;

[Script("Battlefield")]
public class Battlefield : AreaScript
{
    public Battlefield(Area area) : base(area) => Area = area;
    public override void Update(TimeSpan elapsedTime) { }

    public override void OnMapEnter(WorldClient client) { }

    public override void OnMapExit(WorldClient client) { }

    public override void OnPlayerWalk(WorldClient client, Position oldLocation, Position newLocation)
    {
        var vectorMap = new Vector2(newLocation.X, newLocation.Y);
        if (client.Aisling.Pos != vectorMap) return;
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
        client.Aisling.SendAnimationNearby(75, new Position(vectorMap));
        client.SendServerMessage(ServerMessageType.ActiveMessage, "Hmm, I found a few things just as Edgar said.");
    }
}