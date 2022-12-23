using Darkages.Network.Client;
using Darkages.Scripting;
using Darkages.Sprites;
using Darkages.Types;

namespace Darkages.GameScripts.Areas
{
    [Script("Pravat")]
    public class Pravat : AreaScript
    {
        public Pravat(Area area) : base(area) => Area = area;
        public override void Update(TimeSpan elapsedTime) { }
        public override void OnMapEnter(GameClient client) { }
        public override void OnMapExit(GameClient client) { }

        public override void OnMapClick(GameClient client, int x, int y)
        {
            if (x == 24 && y == 15 || x == 25 && y == 16)
            {
                client.OpenBoard("Server Updates");
            }
        }

        public override void OnPlayerWalk(GameClient client, Position oldLocation, Position newLocation) { }
        public override void OnItemDropped(GameClient client, Item itemDropped, Position locationDropped) { }
        public override void OnGossip(GameClient client, string message) { }
    }
}