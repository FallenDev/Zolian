using Chaos.Common.Definitions;

using Darkages.Common;
using Darkages.Enums;
using Darkages.Network.Client;
using Darkages.Network.Server;
using Darkages.ScriptingBase;
using Darkages.Sprites;
using Darkages.Types;

namespace Darkages.GameScripts.Mundanes.Mileth;

[Script("Sabonim")]
public class Sabonim(WorldServer server, Mundane mundane) : MundaneScript(server, mundane)
{
    public override void OnClick(WorldClient client, uint serial)
    {
        base.OnClick(client, serial);
        TopMenu(client);
    }

    protected override void TopMenu(WorldClient client)
    {
        base.TopMenu(client);

        var options = new List<Dialog.OptionsDataItem>();

        if (client.Aisling.Path == Class.Monk || client.Aisling.PastClass == Class.Monk || client.Aisling.GameMaster)
        {
            options.Add(new(0x01, "How can I improve?"));

            switch (client.Aisling.QuestManager.BeltDegree)
            {
                case "White":
                    options.Add(new(0x02, "Attainment of the Yellow degree"));
                    break;
                case "Yellow":
                    options.Add(new(0x03, "Attainment of the Orange degree"));
                    break;
                case "Orange":
                    options.Add(new(0x04, "Attainment of the Green degree"));
                    break;
                case "Green":
                    options.Add(new(0x05, "Attainment of the Purple degree"));
                    break;
                case "Purple":
                    options.Add(new(0x06, "Attainment of the Blue degree"));
                    break;
                case "Blue":
                    options.Add(new(0x07, "Attainment of the Brown degree"));
                    break;
                case "Brown":
                    options.Add(new(0x08, "Attainment of the Red degree"));
                    break;
                case "Red":
                    options.Add(new(0x09, "Attainment of the Black degree"));
                    break;
                case "Black":
                    options.Add(new(0x0A, "Beyond our degrees"));
                    break;
                case "":
                    options.Add(new(0x0B, "Attain my first degree"));
                    break;
            }
        }

        client.SendOptionsDialog(Mundane, "Breathe softly, the spirits can guide you", options.ToArray());
    }

    public override void OnResponse(WorldClient client, ushort responseID, string args)
    {
        if (!AuthenticateUser(client)) return;

        switch (responseID)
        {
            // Starting info
            case 0x01:
                break;
            // Belt attainments
            case 0x02:
                if (client.Aisling.ExpLevel >= 20)
                {
                    client.SendOptionsDialog(Mundane, "Step to the meditation spot in front of me to begin (5, 4)");
                    return;
                }
                
                client.SendOptionsDialog(Mundane, "You are not ready for this attainment yet.");
                break;
            case 0x12:
                if (!args.Equals("Yellow")) return;

                break;
            case 0x03:
                if (client.Aisling.ExpLevel >= 40)
                {
                    client.SendOptionsDialog(Mundane, "Step to the meditation spot in front of me to begin (13, 9)");
                    return;
                }
                
                client.SendOptionsDialog(Mundane, "You are not ready for this attainment yet.");
                break;
            case 0x13:
                if (!args.Equals("Orange")) return;

                break;
            case 0x04:
                if (client.Aisling.ExpLevel >= 60)
                {
                    client.SendOptionsDialog(Mundane, "Step to the meditation spot in front of me to begin (4, 11)");
                    return;
                }
                
                client.SendOptionsDialog(Mundane, "You are not ready for this attainment yet.");
                break;
            case 0x14:
                if (!args.Equals("Green")) return;

                break;
            case 0x05:
                if (client.Aisling.ExpLevel >= 80)
                {
                    client.SendOptionsDialog(Mundane, "Step to the meditation spot in front of me to begin (9, 3)");
                    return;
                }
                
                client.SendOptionsDialog(Mundane, "You are not ready for this attainment yet.");
                break;
            case 0x15:
                if (!args.Equals("Purple")) return;

                break;
            case 0x06:
                if (client.Aisling.ExpLevel >= 99)
                {
                    client.SendOptionsDialog(Mundane, "Step to the meditation spot in front of me to begin (11, 12)");
                    return;
                }
                
                client.SendOptionsDialog(Mundane, "You are not ready for this attainment yet.");
                break;
            case 0x16:
                if (!args.Equals("Blue")) return;

                break;
            case 0x07:
                if (client.Aisling.ExpLevel >= 110)
                {
                    client.SendOptionsDialog(Mundane, "Step to the meditation spot in front of me to begin (3, 7)");
                    return;
                }
                
                client.SendOptionsDialog(Mundane, "You are not ready for this attainment yet.");
                break;
            case 0x17:
                if (!args.Equals("Brown")) return;

                break;
            case 0x08:
                if (client.Aisling.ExpLevel >= 125)
                {
                    client.SendOptionsDialog(Mundane, "Step to the meditation spot in front of me to begin (12, 5)");
                    return;
                }
                
                client.SendOptionsDialog(Mundane, "You are not ready for this attainment yet.");
                break;
            case 0x18:
                if (!args.Equals("Red")) return;

                break;
            case 0x09:
                if (client.Aisling.ExpLevel >= 200)
                {
                    client.SendOptionsDialog(Mundane, "Step to the meditation spot in front of me to begin (7, 13)");
                    return;
                }
                
                client.SendOptionsDialog(Mundane, "You are not ready for this attainment yet.");
                break;
            case 0x19:
                if (!args.Equals("Black")) return;

                break;
            case 0x0A:
                client.SendOptionsDialog(Mundane, "Ah, fellow Black Belt. You have attained all this old stream can afford to you. However, if you so seek it. There is a druid outside of our stream that knows teachings beyond our own.");
                break;
            case 0x0B:
                var optionsB = new List<Dialog.OptionsDataItem>
                {
                    new(0x1B, "Yes, Sabonim")
                };

                client.SendOptionsDialog(Mundane, "Let's meditate together, would you like to learn?", optionsB.ToArray());
                break;
            case 0x1B:
                client.Aisling.QuestManager.BeltDegree = "White";
                client.SendOptionsDialog(Mundane, "Good, there is a lot to learn in breathing exercises alone.");
                client.SendAnimation(1, null, client.Aisling.Serial);
                client.SendBodyAnimation(client.Aisling.Serial, BodyAnimation.HandsUp, 40);
                client.SendServerMessage(ServerMessageType.ActiveMessage, "You've earned your White belt!");
                client.GiveItem("White Belt");
                var item = new Legend.LegendItem
                {
                    Category = "Training",
                    Time = DateTime.UtcNow,
                    Color = LegendColor.White,
                    Icon = (byte)LegendIcon.Monk,
                    Value = "White Belt Attainment"
                };

                client.Aisling.LegendBook.AddLegend(item, client);
                break;
        }
    }

    public override void OnGossip(WorldClient client, string message) { }
}