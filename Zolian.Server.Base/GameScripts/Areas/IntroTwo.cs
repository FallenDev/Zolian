using Darkages.Network.Client;
using Darkages.Scripting;
using Darkages.Sprites;
using Darkages.Types;

namespace Darkages.GameScripts.Areas
{
    [Script("Intro Two")]
    public class IntroTwo : AreaScript
    {
        private Sprite _aisling;

        public IntroTwo(Area area) : base(area) => Area = area;
        public override void Update(TimeSpan elapsedTime) { }

        public override void OnMapEnter(GameClient client)
        {
            _aisling = client.Aisling;
            _aisling.Client.SendMessage(0x08, "{=bDeath{=a... was that all that was left? \n\nYears after the conflict, which consisted of law and chaos, the Anaman Pact attempted to right its wrong. Surely {=bChadul {=awas here to destroy the world? \n\nHowever, when he revived. {=bChadul {=ainstead took a bit of power from each of the Gods, he then infused it, nurtured it, into something that exists in the very being of everything. \n\n-There is rebirth in death.- {=cSgrios muttered with a grin");
        }

        public override void OnMapExit(GameClient client) { }
        public override void OnMapClick(GameClient client, int x, int y) { }
        public override void OnPlayerWalk(GameClient client, Position oldLocation, Position newLocation) { }
        public override void OnItemDropped(GameClient client, Item itemDropped, Position locationDropped) { }
        public override void OnGossip(GameClient client, string message) { }
    }
}