using Darkages.Common;
using Darkages.Network.Client;
using Darkages.Scripting;
using Darkages.Sprites;
using Darkages.Types;

namespace Darkages.GameScripts.Areas
{
    [Script("Abel Barber Map")]
    public class AbelBarber : AreaScript
    {
        public AbelBarber(Area area) : base(area) => Area = area;
        public override void Update(TimeSpan elapsedTime) { }

        public override void OnMapEnter(GameClient client)
        {
            if (client.Aisling.Map.ID == client.Aisling.LastMapId) return;
            client.Aisling.OldStyle = client.Aisling.HairStyle;
            client.Aisling.OldColor = client.Aisling.HairColor;
        }

        public override void OnMapExit(GameClient client)
        {
            if (client.Aisling.Coloring != 0)
            {
                client.Aisling.Coloring = 0;
                client.Aisling.HairColor = client.Aisling.OldColor;
                client.UpdateDisplay();
                client.SendMessage(0x03, "Think you'd get it for free?");
            }

            if (client.Aisling.Styling == 0) return;
            client.Aisling.Styling = 0;
            client.Aisling.HairStyle = client.Aisling.OldStyle;
            client.UpdateDisplay();
            client.SendMessage(0x03, "Think you'd get it for free?");
        }

        public override void OnMapClick(GameClient client, int x, int y) { }
        public override void OnPlayerWalk(GameClient client, Position oldLocation, Position newLocation) { }
        public override void OnItemDropped(GameClient client, Item itemDropped, Position locationDropped) { }

        public override void OnGossip(GameClient client, string message)
        {
            if (message.StringContains("fag"))
            {
                client.Aisling.HairStyle = 46;
                client.Aisling.HairColor = 40;
                client.UpdateDisplay();
                client.SendMessage(0x03, "Not here honey!");
            }

            if (message.StringContains("gay"))
            {
                client.Aisling.HairStyle = 46;
                client.Aisling.HairColor = 40;
                client.UpdateDisplay();
                client.SendMessage(0x03, "Not here honey!");
            }
        }
    }
}