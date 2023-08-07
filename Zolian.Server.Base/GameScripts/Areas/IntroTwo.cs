using Chaos.Common.Definitions;
using Darkages.Network.Client;
using Darkages.ScriptingBase;
using Darkages.Sprites;
using Darkages.Types;

namespace Darkages.GameScripts.Areas;

[Script("Intro Two")]
public class IntroTwo : AreaScript
{
    private Aisling _aisling;

    public IntroTwo(Area area) : base(area) => Area = area;
    public override void Update(TimeSpan elapsedTime) { }

    public override void OnMapEnter(WorldClient client)
    {
        _aisling = client.Aisling;
        _aisling.Client.SendServerMessage(ServerMessageType.ScrollWindow, "{=bDeath{=a... was that all that was left? \n\nYears after the conflict, which consisted of law and chaos, the Anaman Pact attempted to right its wrong. Surely {=bChadul {=awas here to destroy the world? \n\nHowever, when he revived. {=bChadul {=ainstead took a bit of power from each of the Gods, he then infused it, nurtured it, into something that exists in the very being of everything. \n\n-There is rebirth in death.- {=cSgrios muttered with a grin");
    }

    public override void OnMapExit(WorldClient client) { }
    public override void OnMapClick(WorldClient client, int x, int y) { }
    public override void OnPlayerWalk(WorldClient client, Position oldLocation, Position newLocation) { }
    public override void OnItemDropped(WorldClient client, Item itemDropped, Position locationDropped) { }
    public override void OnGossip(WorldClient client, string message) { }
}