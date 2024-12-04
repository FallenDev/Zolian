using Darkages.Network.Client;
using Darkages.ScriptingBase;
using Darkages.Sprites.Entity;
using Darkages.Types;
using System.Numerics;

namespace Darkages.GameScripts.Areas.Loures;

[Script("Library")]
public class Library : AreaScript
{
    public Library(Area area) : base(area) => Area = area;
    public override void Update(TimeSpan elapsedTime) { }
    public override void OnMapEnter(WorldClient client) { }
    public override void OnMapExit(WorldClient client) { }

    public override void OnPlayerWalk(WorldClient client, Position oldLocation, Position newLocation)
    {
        var vectorMap = new Vector2(newLocation.X, newLocation.Y);
        if (client.Aisling.Pos != vectorMap) return;

        switch (newLocation.X)
        {
            case 10 when newLocation.Y is 2 && client.Aisling.QuestManager.ArmorApothecaryAccepted && !client.Aisling.QuestManager.ArmorCodexDeciphered && !client.Aisling.HasItem("Aosda Transcriptions Volume: IV"):
                var book = new Item();
                book = book.Create(client.Aisling, "Aosda Transcriptions Volume: IV");
                book.GiveTo(client.Aisling);
                break;
        }
    }

    public override void OnItemDropped(WorldClient client, Item itemDropped, Position locationDropped) { }
    public override void OnGossip(WorldClient client, string message) { }
}