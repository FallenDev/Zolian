using Darkages.Common;
using Darkages.Enums;
using Darkages.Network.Client;
using Darkages.Network.Server;
using Darkages.ScriptingBase;
using Darkages.Sprites;
using Darkages.Sprites.Entity;
using Darkages.Types;

namespace Darkages.GameScripts.Mundanes.Gems;

[Script("DarkIron")]
public class DarkIron(WorldServer server, Mundane mundane) : MundaneScript(server, mundane)
{
    public override void OnClick(WorldClient client, uint serial)
    {
        client.EntryCheck = serial;
        TopMenu(client);
    }

    protected override void TopMenu(WorldClient client)
    {
        base.TopMenu(client);
        CheckRank(client);

        var options = new List<Dialog.OptionsDataItem>
        {
            new(0x01, "Proceed")
        };

        client.SendOptionsDialog(Mundane, $"{{=qMining{{=a: {client.Aisling.QuestManager.StoneSmithing}\n" +
                                          $"{{=qRefine{{=a: Chance to refine raw stone to an Ore\n" +
                                          $"*Higher Mining levels result in a higher chance at an Ore being refined!", options.ToArray());
    }

    public override void OnResponse(WorldClient client, ushort responseID, string args)
    {
        if (Mundane.Serial != client.EntryCheck)
        {
            client.CloseDialog();
            return;
        }

        var contains = false;

        foreach (var item in client.Aisling.Inventory.Items.Values)
        {
            if (item == null) continue;
            if (item.Template.Name == "Raw Dark Iron") contains = true;
        }

        if (contains == false)
        {
            client.CloseDialog();
            return;
        }

        switch (responseID)
        {
            case 1:
                {
                    var options = new List<Dialog.OptionsDataItem>();

                    if (client.Aisling.ExpLevel >= 50)
                        options.Add(new Dialog.OptionsDataItem(0x05, "{=bRefine"));
                    else
                    {
                        client.SendOptionsDialog(Mundane, "This ore is too high level for you. (Insight: 50)");
                        return;
                    }

                    client.SendOptionsDialog(Mundane, "This process will refine the raw material for a chance at an Ore", options.ToArray());
                    break;
                }
            case 5:
                if (RefineNode(client.Aisling) && client.Aisling.ExpLevel >= 50)
                {
                    client.Aisling.Client.GiveItem("Refined Dark Iron");
                    client.Aisling.QuestManager.StoneSmithing++;
                    client.Aisling.Client.TakeAwayQuantity(client.Aisling, "Raw Dark Iron", 1);
                    client.GiveExp(66000);
                    client.SendServerMessage(ServerMessageType.ActiveMessage, "Refining success! 66,000 exp");
                    client.CloseDialog();
                }
                else
                {
                    client.Aisling.Client.TakeAwayQuantity(client.Aisling, "Raw Dark Iron", 1);
                    client.SendServerMessage(ServerMessageType.ActiveMessage, "Refining process failed!");
                    client.CloseDialog();
                }
                break;
        }
    }

    private static bool RefineNode(Aisling player)
    {
        var tryRefine = Generator.RandomNumPercentGen();

        switch (player.QuestManager.StoneSmithingTier)
        {
            case "Novice": // 25
                tryRefine += .05;
                break;
            case "Apprentice": // 75
                tryRefine += .07;
                break;
            case "Journeyman": // 150
                tryRefine += .12;
                break;
            case "Expert": // 225
                tryRefine += .15;
                break;
            case "Artisan":
                tryRefine += .20;
                break;
        }

        return tryRefine switch
        {
            >= 0 and <= .60 => false,
            > .60 => true,
            _ => false
        };
    }

    private static void CheckRank(WorldClient client)
    {
        switch (client.Aisling.QuestManager.StoneSmithing)
        {
            case >= 0 and <= 24:
                if (!client.Aisling.LegendBook.Has("Mining: Novice"))
                {
                    client.Aisling.QuestManager.StoneSmithingTier = "Novice";

                    var legend = new Legend.LegendItem
                    {
                        Key = "LMineS1",
                        IsPublic = true,
                        Time = DateTime.UtcNow,
                        Color = LegendColor.BlueG1,
                        Icon = (byte)LegendIcon.Victory,
                        Text = "Mining: Novice"
                    };

                    client.Aisling.LegendBook.AddLegend(legend, client);
                }
                break;
            case <= 74:
                if (!client.Aisling.LegendBook.Has("Mining: Apprentice"))
                {
                    client.Aisling.QuestManager.StoneSmithingTier = "Apprentice";

                    var legend = new Legend.LegendItem
                    {
                        Key = "LMineS2",
                        IsPublic = true,
                        Time = DateTime.UtcNow,
                        Color = LegendColor.BlueG1,
                        Icon = (byte)LegendIcon.Victory,
                        Text = "Mining: Apprentice"
                    };

                    client.Aisling.LegendBook.AddLegend(legend, client);
                }
                break;
            case <= 149:
                if (!client.Aisling.LegendBook.Has("Mining: Journeyman"))
                {
                    client.Aisling.QuestManager.StoneSmithingTier = "Journeyman";

                    var legend = new Legend.LegendItem
                    {
                        Key = "LMineS3",
                        IsPublic = true,
                        Time = DateTime.UtcNow,
                        Color = LegendColor.BlueG1,
                        Icon = (byte)LegendIcon.Victory,
                        Text = "Mining: Journeyman"
                    };

                    client.Aisling.LegendBook.AddLegend(legend, client);
                }
                break;
            case <= 224:
                if (!client.Aisling.LegendBook.Has("Mining: Expert"))
                {
                    client.Aisling.QuestManager.StoneSmithingTier = "Expert";

                    var legend = new Legend.LegendItem
                    {
                        Key = "LMineS4",
                        IsPublic = true,
                        Time = DateTime.UtcNow,
                        Color = LegendColor.BlueG1,
                        Icon = (byte)LegendIcon.Victory,
                        Text = "Mining: Expert"
                    };

                    client.Aisling.LegendBook.AddLegend(legend, client);
                }
                break;
            case <= 299:
                if (!client.Aisling.LegendBook.Has("Mining: Artisan"))
                {
                    client.Aisling.QuestManager.StoneSmithingTier = "Artisan";

                    var legend = new Legend.LegendItem
                    {
                        Key = "LMineS5",
                        IsPublic = true,
                        Time = DateTime.UtcNow,
                        Color = LegendColor.BlueG1,
                        Icon = (byte)LegendIcon.Victory,
                        Text = "Mining: Artisan"
                    };

                    client.Aisling.LegendBook.AddLegend(legend, client);
                }
                break;
        }
    }
}