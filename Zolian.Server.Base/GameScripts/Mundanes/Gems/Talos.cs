using Darkages.Common;
using Darkages.Interfaces;
using Darkages.Network.Client;
using Darkages.Network.Formats.Models.ServerFormats;
using Darkages.Network.Server;
using Darkages.Scripting;
using Darkages.Sprites;

namespace Darkages.GameScripts.Mundanes.Gems
{
    [Script("Talos")]
    public class Talos : MundaneScript
    {
        public Talos(GameServer server, Mundane mundane) : base(server, mundane) { }

        public override void OnClick(GameServer server, GameClient client)
        {
            TopMenu(client);
        }

        public override void TopMenu(IGameClient client)
        {
            var options = new List<OptionsDataItem>
            {
                new(0x01, "Refine"),
                new(0x02, "Crush"),
                new(0x03, "{=bStop")
            };

            client.SendOptionsDialog(Mundane, $"Stone smithing level: {client.Aisling.QuestManager.StoneSmithing} ", options.ToArray());
        }

        public override void OnResponse(GameServer server, GameClient client, ushort responseID, string args)
        {
            var contains = false;

            foreach (var item in client.Aisling.Inventory.Items.Values)
            {
                if (item == null) continue;
                if (item.Template.Name == "Raw Talos") contains = true;
            }

            if (contains == false) return;

            switch (responseID)
            {
                case 1:
                    {
                        var options = new List<OptionsDataItem>
                        {
                            new (0x05, "Proceed"),
                            new (0x04, "{=q<- Back")
                        };

                        client.SendOptionsDialog(Mundane, "This process will attempt to refine the raw material.", options.ToArray());
                        break;
                    }
                case 2:
                    {
                        var options = new List<OptionsDataItem>
                        {
                            new (0x06, "Proceed"),
                            new (0x04, "{=q<- Back")
                        };

                        client.SendOptionsDialog(Mundane, "This process will reduce the raw material to its basic form.", options.ToArray());
                        break;
                    }
                case 3:
                    client.CloseDialog();
                    break;
                case 4:
                    TopMenu(client);
                    break;
                case 5:
                    if (RefineNode())
                    {
                        client.Aisling.Client.GiveItem("Refined Talos");
                        client.Aisling.Client.TakeAwayQuantity(client.Aisling, "Raw Talos", 1);
                        client.GiveExp(1000);
                        client.SendMessage(0x03, "Refining success! 1,000 exp");
                        client.CloseDialog();
                    }
                    else
                    {
                        client.Aisling.Client.TakeAwayQuantity(client.Aisling, "Raw Talos", 1);
                        client.SendMessage(0x03, "Refining process failed!");
                        client.CloseDialog();
                    }
                    break;
                case 6:
                    TopMenu(client);
                    break;
            }
        }

        private static bool RefineNode()
        {
            var tryRefine = Generator.RandomNumPercentGen();

            return tryRefine switch
            {
                >= 0 and <= .74 => false,
                > .74 and <= 1 => true,
                _ => false
            };
        }
    }
}
