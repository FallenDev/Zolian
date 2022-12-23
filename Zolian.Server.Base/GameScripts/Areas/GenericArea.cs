using Darkages.Network.Client;
using Darkages.Scripting;
using Darkages.Sprites;
using Darkages.Types;

namespace Darkages.GameScripts.Areas
{
    [Script("Generic Area")]
    public class GenericArea : AreaScript
    {
        private Sprite _aisling;

        public GenericArea(Area area) : base(area) => Area = area;
        public override void Update(TimeSpan elapsedTime) { }
        public override void OnMapEnter(GameClient client) => _aisling = client.Aisling;
        public override void OnMapExit(GameClient client) => _aisling = null;
        public override void OnMapClick(GameClient client, int x, int y) => _aisling ??= client.Aisling;
        public override void OnPlayerWalk(GameClient client, Position oldLocation, Position newLocation) { }
        public override void OnItemDropped(GameClient client, Item itemDropped, Position locationDropped) { }
        public override void OnGossip(GameClient client, string message) { }
    }
}