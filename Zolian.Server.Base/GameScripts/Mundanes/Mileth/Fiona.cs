using Darkages.Interfaces;
using Darkages.Network.Client;
using Darkages.Network.Formats.Models.ServerFormats;
using Darkages.Network.Server;
using Darkages.Scripting;
using Darkages.Sprites;

namespace Darkages.GameScripts.Mundanes.Mileth
{
    [Script("Fiona")]
    public class Fiona : MundaneScript
    {
        public Fiona(GameServer server, Mundane mundane) : base(server, mundane) { }

        public override void OnClick(GameServer server, GameClient client)
        {
            TopMenu(client);
        }

        public override void TopMenu(IGameClient client)
        {
            var options = new List<OptionsDataItem>();

            if (!client.Aisling.QuestManager.FionaDance)
            {
                options.Add(new (0x01, "Why not."));
            }

            options.Add(new (0x02, "Huh?"));

            client.SendOptionsDialog(Mundane, "Wanna Dance?", options.ToArray());
        }

        public override void OnResponse(GameServer server, GameClient client, ushort responseID, string args)
        {
            if (client.Aisling.Map.ID != Mundane.Map.ID)
            {
                client.Dispose();
                return;
            }

            switch (responseID)
            {
                case 0x01:
                    {
                        var options = new List<OptionsDataItem>
                        {
                            new (0x03, "*smile warmly*"),
                        };

                        client.SendOptionsDialog(Mundane, $"You know, you're not too bad.. Thank you.", options.ToArray());
                    }
                    break;
                case 0x02:
                    {
                        client.SendOptionsDialog(Mundane, "Too bad.");
                    }
                    break;
                case 0x03:
                    {
                        client.Aisling.QuestManager.FionaDance = true;
                        client.Aisling.QuestManager.MilethReputation += 1;
                        client.CloseDialog();
                    }
                    break;
            }
        }
    }
}