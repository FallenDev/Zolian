using Darkages.Network.Client;
using Darkages.Scripting;
using Darkages.Sprites;
using Darkages.Types;

namespace Darkages.GameScripts.Areas
{
    [Script("Intro Three")]
    public class IntroThree : AreaScript
    {
        private Sprite _aisling;

        public IntroThree(Area area) : base(area) => Area = area;
        public override void Update(TimeSpan elapsedTime) { }

        public override void OnMapEnter(GameClient client)
        {
            _aisling = client.Aisling;
            _aisling.Client.SendMessage(0x08, "{=wRebirth{=a... its what's left, when ashes and the decay stops. Then can life begin anew. \n\nSee for your eyes that today is a new light. Today a new beginning, today a new spark. \n\n\n{=qWelcome to Zolian.");
        }

        public override void OnMapExit(GameClient client) { }
        public override void OnMapClick(GameClient client, int x, int y) { }
        public override void OnPlayerWalk(GameClient client, Position oldLocation, Position newLocation) { }
        public override void OnItemDropped(GameClient client, Item itemDropped, Position locationDropped) { }
        public override void OnGossip(GameClient client, string message) { }
    }
}