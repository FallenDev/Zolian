using Chaos.Common.Definitions;

using Darkages.Common;
using Darkages.Enums;
using Darkages.GameScripts.Formulas;
using Darkages.Network.Client;
using Darkages.Network.Server;
using Darkages.ScriptingBase;
using Darkages.Sprites;
using Darkages.Types;

namespace Darkages.GameScripts.Mundanes.Mileth;

[Script("Death Tree")]
public class DeathTree(WorldServer server, Mundane mundane) : MundaneScript(server, mundane)
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

        if (client.Aisling.QuestManager.TagorDungeonAccess && client.Aisling.QuestManager.DreamWalking)
        {
            if (!client.Aisling.QuestManager.DreamWalkingSlayed)
                options.Add(new Dialog.OptionsDataItem(0x06, "Dream Walker"));

            if (!client.Aisling.QuestManager.ReleasedTodesbaum)
                options.Add(new Dialog.OptionsDataItem(0x0A, "Freedom"));
        }
        else
        {
            if (client.Aisling.Stage is >= ClassStage.Dedicated and < ClassStage.Master && client.Aisling.HasItem("Necra Scribblings"))
                options.Add(new Dialog.OptionsDataItem(0x02, "Necra Scribblings"));
        }

        client.SendOptionsDialog(Mundane,
            client.Aisling.Level < 120
                ? "You don't seem capable, but looks can be deceiving"
                : "Aisling, what you seek is but a stones throw away", options.ToArray());
    }

    public override void OnResponse(WorldClient client, ushort responseID, string args)
    {
        if (!AuthenticateUser(client)) return;
        var randX1 = Generator.RandNumGen20();
        var randX2 = Generator.RandNumGen20();
        var randX3 = Generator.RandNumGen20();
        var randY1 = Generator.RandNumGen20();
        var randY2 = Generator.RandNumGen20();
        var randY3 = Generator.RandNumGen20();

        var randX4 = Generator.RandNumGen20();
        var randX5 = Generator.RandNumGen20();
        var randX6 = Generator.RandNumGen20();
        var randY4 = Generator.RandNumGen20();
        var randY5 = Generator.RandNumGen20();
        var randY6 = Generator.RandNumGen20();
        var exp = Random.Shared.Next(35000000, 50000000);

        switch (responseID)
        {
            case 0x02:
                {
                    var options = new List<Dialog.OptionsDataItem>
                    {
                        new(0x03, "*Throws book*"),
                        new(0x05, "Nah, I think I'll keep it")
                    };

                    client.SendOptionsDialog(Mundane, $"Throw me the book and I'll grant you access to {{=qTagor Dungeon", options.ToArray());
                }
                break;
            case 0x03:
                {
                    var item = client.Aisling.HasItemReturnItem("Necra Scribblings");
                    client.Aisling.Inventory.RemoveFromInventory(client, item);
                    client.SendPublicMessage(Mundane.Serial, PublicMessageType.Shout, $"{Mundane.Name}: Ahh! Only a few more! And I can be...");
                    client.Aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(1, new Position(9, 8)));
                    client.CloseDialog();

                    #region Animation Show

                    Task.Delay(350).ContinueWith(ct =>
                    {
                        client.Aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(50, null, Mundane.Serial));
                    });
                    Task.Delay(350).ContinueWith(ct =>
                    {
                        client.Aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(15, new Position(randX1, randY1)));
                    });
                    Task.Delay(650).ContinueWith(ct =>
                    {
                        client.Aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(15, new Position(randX2, randY2)));
                    });
                    Task.Delay(950).ContinueWith(ct =>
                    {
                        client.Aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(15, new Position(randX3, randY3)));
                    });
                    Task.Delay(1250).ContinueWith(ct =>
                    {
                        client.Aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(15, new Position(randX4, randY4)));
                    });
                    Task.Delay(1550).ContinueWith(ct =>
                    {
                        client.Aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(15, new Position(randX5, randY5)));
                    });
                    Task.Delay(1850).ContinueWith(ct =>
                    {
                        client.Aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(15, new Position(randX6, randY6)));
                    });
                    Task.Delay(1850).ContinueWith(ct =>
                    {
                        client.Aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(15, new Position(randX6, randY6)));
                    });
                    Task.Delay(1850).ContinueWith(ct =>
                    {
                        client.Aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(15, new Position(randX6, randY6)));
                    });
                    Task.Delay(1850).ContinueWith(ct =>
                    {
                        client.Aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(15, new Position(randX6, randY6)));
                    });
                    Task.Delay(1850).ContinueWith(ct =>
                    {
                        client.Aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(15, new Position(randX6, randY6)));
                    });
                    Task.Delay(3500).ContinueWith(ct =>
                    {
                        client.SendPublicMessage(Mundane.Serial, PublicMessageType.Shout, $"{Mundane.Name}: ...whole again, one day");
                    });

                    #endregion

                    client.Aisling.QuestManager.TagorDungeonAccess = true;
                    client.SendServerMessage(ServerMessageType.ActiveMessage, $"You can now access {{=qTagor Dungeon");
                    var legend = new Legend.LegendItem
                    {
                        Key = "LTodesbaum1",
                        IsPublic = true,
                        Time = DateTime.UtcNow,
                        Color = LegendColor.Yellow,
                        Icon = (byte)LegendIcon.Victory,
                        Text = "Granted access to Tagor Dungeon"
                    };

                    client.Aisling.LegendBook.AddLegend(legend, client);
                }
                break;
            case 0x05:
                client.SendPublicMessage(Mundane.Serial, PublicMessageType.Normal, $"{Mundane.Name}: So be it, if that changes, I'll be here");
                client.CloseDialog();
                break;
            case 0x06:
                {
                    client.SendPublicMessage(Mundane.Serial, PublicMessageType.Normal, $"{Mundane.Name}: The dream walker.... I remember that name.");
                    var options = new List<Dialog.OptionsDataItem>
                    {
                        new(0x07, "Why?"),
                        new(0x05, "No time for this.")
                    };

                    client.SendOptionsDialog(Mundane,
                        $"The dream walker, he bound me where I stand. I was once a noble wizard of the high court of Zolian. Because of him, or should I say them. I am forever here tormented by terrors.", options.ToArray());
                }
                break;
            case 0x07:
                {
                    var options = new List<Dialog.OptionsDataItem>
                    {
                        new(0x08, "I am"),
                        new(0x05, "I think I'll pass")
                    };

                    client.SendOptionsDialog(Mundane,
                        $"I tried to lift the Dream Sword from it's pedestal. Upon touching it, I was sent to a location in this crypt that I never saw before. However, you may be different. You're not thinking of trying it are you?", options.ToArray());
                }
                break;
            case 0x08:
                {
                    var options = new List<Dialog.OptionsDataItem>
                    {
                        new(0x09, "I shall see you again"),
                    };

                    client.SendOptionsDialog(Mundane, $"Very Well, if you manage to slay the Dream Walker. Perhaps I can take some of his essence and free myself. Talk to me, if you return.", options.ToArray());
                }
                break;
            case 0x09:
                {
                    client.CloseDialog();
                    client.WarpToAndRefresh(new Position(9, 9));
                    Task.Delay(150).ContinueWith(ct =>
                    {
                        client.Aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(63, client.Aisling.Position));
                    });
                    Task.Delay(350).ContinueWith(ct =>
                    {
                        client.SendPublicMessage(Mundane.Serial, PublicMessageType.Normal, $"{Mundane.Name}: Touch the sword.");
                    });
                }
                break;
            case 0x0A:
                {
                    if (client.Aisling.HasKilled("Dream Walker", 1) && !client.Aisling.QuestManager.ReleasedTodesbaum)
                    {
                        client.Aisling.QuestManager.LouresReputation += 3;
                        client.Aisling.QuestManager.ReleasedTodesbaum = true;
                        client.GiveExp(exp);
                        client.SendServerMessage(ServerMessageType.ActiveMessage, $"You've gained {exp} experience.");
                        var item = new Item();
                        // Hallowed Dresden
                        item = item.Create(client.Aisling, "Hallowed Dresden", NpcShopExtensions.DungeonHighQuality(), ItemQualityVariance.DetermineVariance(), Item.WeaponVariance.None);
                        var received = item.GiveTo(client.Aisling, false);
                        if (!received)
                            client.Aisling.BankManager.Items.TryAdd(item.ItemId, item);
                        client.SendAttributes(StatUpdateType.ExpGold);
                        var legend = new Legend.LegendItem
                        {
                            Key = "LTodesbaum2",
                            IsPublic = true,
                            Time = DateTime.UtcNow,
                            Color = LegendColor.TurquoiseG7,
                            Icon = (byte)LegendIcon.Victory,
                            Text = "Freed Todesbaum"
                        };

                        client.Aisling.LegendBook.AddLegend(legend, client);
                        client.SendOptionsDialog(Mundane, $"I shall see you again {client.Aisling.Username}, thank you.");
                    }
                    else
                    {
                        client.SendOptionsDialog(Mundane, "I can't feel any essence on you.", new Dialog.OptionsDataItem(0x05, "Not yet, give me some time."));
                    }
                }
                break;
        }
    }

    public override void OnGossip(WorldClient client, string message) { }
}